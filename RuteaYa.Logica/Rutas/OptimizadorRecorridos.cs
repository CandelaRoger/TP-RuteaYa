
using RuteaYa.Entidades;

namespace RuteaYa.Logica.Rutas
{
    // Guarda la ruta elegida y el orden de las paradas.
    public class MejorRecorrido
    {
        public ResultadoRuta Ruta { get; set; } = null!;

        public List<Coordenadas> PuntosOrdenados { get; set; } = new();
    }

    public class OptimizadorRecorridos
    {
        private readonly IRutaService _rutaService;

        public OptimizadorRecorridos(IRutaService rutaService)
        {
            _rutaService = rutaService;
        }

        public async Task<MejorRecorrido?> CalcularMejorRutaAsync(
            List<Coordenadas> puntosOriginales,
            List<Envio> envios)
        {
            if (puntosOriginales.Count < 2)
                return null;

            // El primer punto es el origen del conductor.
            var origen = puntosOriginales[0];

            // El último punto es su destino.
            var destino = puntosOriginales[^1];

            // Las paradas originales mantienen su orden.
            var paradasOriginales = puntosOriginales
                .Skip(1)
                .Take(puntosOriginales.Count - 2)
                .ToList();

            // Cada envío tiene un retiro y una entrega.
            var retiros = envios.Select(e => new Coordenadas
            {
                Latitud = (double)e.OrigenLat,
                Longitud = (double)e.OrigenLng
            }).ToList();

            var entregas = envios.Select(e => new Coordenadas
            {
                Latitud = (double)e.DestinoLat,
                Longitud = (double)e.DestinoLng
            }).ToList();

            MejorRecorrido? mejor = null;

            var recorridoActual = new List<Coordenadas> { origen };

            var retirados = new bool[envios.Count];
            var entregados = new bool[envios.Count];

            // Función recursiva: prueba todos los órdenes válidos.
            async Task ExplorarAsync(int indiceParadaOriginal)
            {
                bool terminaronParadas =
                    indiceParadaOriginal == paradasOriginales.Count;

                bool terminaronEnvios = entregados.All(x => x);

                // Si ya visitamos todas las ubicaciones,
                // agregamos el destino y consultamos HERE.
                if (terminaronParadas && terminaronEnvios)
                {
                    var recorridoCompleto =
                        new List<Coordenadas>(recorridoActual)
                        {
                            destino
                        };

                    var resultado = await _rutaService
                        .CalcularRutaAsync(recorridoCompleto);

                    if (resultado == null)
                        return;

                    if (mejor == null ||
                        resultado.DistanciaKm < mejor.Ruta.DistanciaKm)
                    {
                        mejor = new MejorRecorrido
                        {
                            Ruta = resultado,
                            PuntosOrdenados = recorridoCompleto
                        };
                    }

                    return;
                }

                // OPCIÓN 1: visitar la siguiente parada original.
                if (indiceParadaOriginal < paradasOriginales.Count)
                {
                    recorridoActual.Add(
                        paradasOriginales[indiceParadaOriginal]);

                    await ExplorarAsync(indiceParadaOriginal + 1);

                    recorridoActual.RemoveAt(recorridoActual.Count - 1);
                }

                // OPCIÓN 2: retirar cualquiera de los paquetes
                // que todavía no fueron retirados.
                for (int i = 0; i < envios.Count; i++)
                {
                    if (retirados[i])
                        continue;

                    retirados[i] = true;
                    recorridoActual.Add(retiros[i]);

                    await ExplorarAsync(indiceParadaOriginal);

                    recorridoActual.RemoveAt(recorridoActual.Count - 1);
                    retirados[i] = false;
                }

                // OPCIÓN 3: entregar un paquete,
                // siempre que ya haya sido retirado.
                for (int i = 0; i < envios.Count; i++)
                {
                    if (!retirados[i] || entregados[i])
                        continue;

                    entregados[i] = true;
                    recorridoActual.Add(entregas[i]);

                    await ExplorarAsync(indiceParadaOriginal);

                    recorridoActual.RemoveAt(recorridoActual.Count - 1);
                    entregados[i] = false;
                }
            }

            await ExplorarAsync(0);

            return mejor;
        }
    }
}
