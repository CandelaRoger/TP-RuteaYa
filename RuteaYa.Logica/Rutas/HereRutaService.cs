
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Globalization;

namespace RuteaYa.Logica.Rutas
{
    public class HereRutaService : IRutaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public HereRutaService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["Here:ApiKey"]
                ?? throw new InvalidOperationException(
                    "No se configuró la API Key de HERE.");
        }

        public async Task<Coordenadas?> GeocodificarAsync(string direccion)
        {
            var url = "https://geocode.search.hereapi.com/v1/geocode" +
                      $"?q={Uri.EscapeDataString(direccion)}" +
                      $"&apiKey={Uri.EscapeDataString(_apiKey)}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            using var documento = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            var items = documento.RootElement.GetProperty("items");

            if (items.GetArrayLength() == 0)
                return null;

            var posicion = items[0].GetProperty("position");

            return new Coordenadas
            {
                Latitud = posicion.GetProperty("lat").GetDouble(),
                Longitud = posicion.GetProperty("lng").GetDouble()
            };
        }

        public async Task<ResultadoRuta?> CalcularRutaAsync(
            List<Coordenadas> puntos)
        {
            if (puntos == null || puntos.Count < 2)
            {
                throw new ArgumentException(
                    "Se necesitan al menos un origen y un destino.");
            }

            var origen = puntos.First();
            var destino = puntos.Last();

            var parametros = new List<string>
            {
                "transportMode=car",

                $"origin={origen.Latitud.ToString(CultureInfo.InvariantCulture)},{origen.Longitud.ToString(CultureInfo.InvariantCulture)}",

                $"destination={destino.Latitud.ToString(CultureInfo.InvariantCulture)},{destino.Longitud.ToString(CultureInfo.InvariantCulture)}",

                "return=summary,polyline"
            };

            foreach (var punto in puntos.Skip(1).Take(puntos.Count - 2))
            {
                parametros.Add(
                    $"via={punto.Latitud.ToString(CultureInfo.InvariantCulture)},{punto.Longitud.ToString(CultureInfo.InvariantCulture)}");
            }

            parametros.Add(
                $"apiKey={Uri.EscapeDataString(_apiKey)}");

            var url = "https://router.hereapi.com/v8/routes?" +
                      string.Join("&", parametros);

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var contenido = await response.Content.ReadAsStringAsync();

            using var documento = JsonDocument.Parse(contenido);

            var rutas = documento.RootElement.GetProperty("routes");

            if (rutas.GetArrayLength() == 0)
                return null;

            var secciones = rutas[0].GetProperty("sections");

            double distanciaTotal = 0;
            double duracionTotal = 0;

            var puntosRuta = new List<Coordenadas>();

            foreach (var seccion in secciones.EnumerateArray())
            {
                var resumen = seccion.GetProperty("summary");

                distanciaTotal += resumen.GetProperty("length").GetDouble();

                duracionTotal += resumen.GetProperty("duration").GetDouble();

                var polyline = seccion.GetProperty("polyline").GetString();

                if (!string.IsNullOrEmpty(polyline))
                {
                    puntosRuta.AddRange(
                        DecodificarPolyline(polyline));
                }
            }

            return new ResultadoRuta
            {
                DistanciaKm = distanciaTotal / 1000,
                DuracionMinutos = duracionTotal / 60,
                PuntosRuta = puntosRuta
            };
        }

        private List<Coordenadas> DecodificarPolyline(string polyline)
        {
            const string alfabeto =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

            if (string.IsNullOrEmpty(polyline))
                throw new FormatException("La Polyline está vacía.");

            int indice = 0;

            long LeerVarint()
            {
                long resultado = 0;
                int desplazamiento = 0;

                while (true)
                {
                    if (indice >= polyline.Length)
                    {
                        throw new FormatException(
                            "Polyline incompleta.");
                    }

                    int valor = alfabeto.IndexOf(polyline[indice++]);

                    if (valor < 0)
                    {
                        throw new FormatException(
                            "Carácter inválido en Polyline.");
                    }

                    resultado |=
                        (long)(valor & 0x1F) << desplazamiento;

                    if ((valor & 0x20) == 0)
                        break;

                    desplazamiento += 5;

                    if (desplazamiento > 60)
                    {
                        throw new FormatException(
                            "Varint demasiado grande.");
                    }
                }

                return resultado;
            }

            long DecodificarValor()
            {
                long valor = LeerVarint();

                return (valor & 1) != 0
                    ? ~(valor >> 1)
                    : valor >> 1;
            }

            long version = LeerVarint();

            if (version != 1)
            {
                throw new FormatException(
                    $"Versión de Polyline no compatible: {version}");
            }

            long header = LeerVarint();

            long precision = header & 15;

            long terceraDimension =
                (header >> 4) & 7;

            long precisionTerceraDimension =
                (header >> 7) & 15;

            double factor = Math.Pow(10, precision);

            double factorTerceraDimension =
                Math.Pow(10, precisionTerceraDimension);

            long latitud = 0;
            long longitud = 0;

            var coordenadas = new List<Coordenadas>();

            while (indice < polyline.Length)
            {
                latitud += DecodificarValor();

                longitud += DecodificarValor();

                if (terceraDimension != 0)
                {
                    long terceraCoordenada = DecodificarValor();

                  
                    _ = terceraCoordenada / factorTerceraDimension;
                }

                coordenadas.Add(new Coordenadas
                {
                    Latitud = latitud / factor,
                    Longitud = longitud / factor
                });
            }

            return coordenadas;
        }
    }
}
