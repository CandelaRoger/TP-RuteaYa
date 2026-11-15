namespace RuteaYa.Logica.Rutas
{
    public class MockRutaService : IRutaService
    {
        public Task<Coordenadas?> GeocodificarAsync(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
                return Task.FromResult<Coordenadas?>(null);

            var coordenadas = new Coordenadas
            {
                Latitud = -34.6037,
                Longitud = -58.3816
            };

            return Task.FromResult<Coordenadas?>(coordenadas);
        }

        public Task<List<SugerenciaDireccion>> BuscarDireccionesAsync(
            string texto)
        {
            var sugerencias = new List<SugerenciaDireccion>();

            if (!string.IsNullOrWhiteSpace(texto) && texto.Trim().Length >= 3)
            {
                sugerencias.Add(new SugerenciaDireccion
                {
                    Titulo = "Plaza de Mayo",
                    Direccion = "Plaza de Mayo, Buenos Aires, Argentina",
                    Latitud = -34.6083,
                    Longitud = -58.3702
                });

                sugerencias.Add(new SugerenciaDireccion
                {
                    Titulo = "Obelisco",
                    Direccion = "Obelisco, Buenos Aires, Argentina",
                    Latitud = -34.6037,
                    Longitud = -58.3816
                });
            }

            return Task.FromResult(sugerencias);
        }

        public Task<ResultadoRuta?> CalcularRutaAsync(
            List<Coordenadas> puntos)
        {
            if (puntos == null || puntos.Count < 2)
            {
                throw new ArgumentException(
                    "Se necesitan al menos un origen y un destino.");
            }

            double distanciaTotal = 0;

            for (int i = 0; i < puntos.Count - 1; i++)
            {
                double diferenciaLatitud =
                    puntos[i + 1].Latitud - puntos[i].Latitud;

                double diferenciaLongitud =
                    puntos[i + 1].Longitud - puntos[i].Longitud;

                double distancia =
                    Math.Sqrt(
                        Math.Pow(diferenciaLatitud, 2) +
                        Math.Pow(diferenciaLongitud, 2));

                distanciaTotal += distancia * 111;
            }

            return Task.FromResult<ResultadoRuta?>(
                new ResultadoRuta
                {
                    DistanciaKm = distanciaTotal,
                    DuracionMinutos = distanciaTotal / 30 * 60,
                    PuntosRuta = puntos
                });
        }
    }
}