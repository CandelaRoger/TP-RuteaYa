
using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;

namespace RuteaYa.Logica
{
    public class GananciasMensualesService : IGananciasMensualesService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IOportunidadService _oportunidadService;

        public GananciasMensualesService( RuteaYaDbContext context, IOportunidadService oportunidadService)
        {
            _context = context;
            _oportunidadService = oportunidadService;
        }


        public async Task<ResumenGananciasMensualDto> ObtenerResumenAsync(
            int usuarioId,
            string criterio = "ganancia")
        {
            var criterioNormalizado = string.IsNullOrWhiteSpace(criterio) ? "ganancia" : criterio.Trim().ToLowerInvariant();
            var ordenOportunidad = criterioNormalizado switch
            {
                "desvio" => "desvio",
                "equilibrio" => "compatibilidad",
                "compatibilidad" => "compatibilidad",
                _ => "ganancia"
            };

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioMesSiguiente = inicioMes.AddMonths(1);
            var gananciasActuales = await _context.Envios
                .Where(e =>
                    e.ViajeId != null &&
                    e.Viaje != null &&
                    e.Viaje.UsuarioId == usuarioId &&
                    e.Estado != "Cancelado" &&
                    e.Fecha >= inicioMes &&
                    e.Fecha < inicioMesSiguiente)
                .SumAsync(e => (decimal?)e.MontoOfrecido) ?? 0m;

            var viajes = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.DiasSemana)
                .Where(v => v.UsuarioId == usuarioId && v.Activo)
                .ToListAsync();

            
            var enviosYaAsignados = await _context.Envios
                .Where(e =>
                    e.ViajeId != null &&
                    e.Viaje != null &&
                    e.Viaje.UsuarioId == usuarioId &&
                    e.Estado != "Cancelado")
                .Select(e => e.Id)
                .ToListAsync();

            var enviosContabilizados = new HashSet<int>(enviosYaAsignados);

            var fechasViajes = new List<(int ViajeId, DateTime Fecha)>();

            for (var fecha = hoy < inicioMes ? inicioMes : hoy;
                 fecha < inicioMesSiguiente;
                 fecha = fecha.AddDays(1))
            {
                foreach (var viaje in viajes)
                {
                    if (viaje.DiasSemana.Any(
                        d => d.DiaSemana == (byte)fecha.DayOfWeek))
                    {
                        fechasViajes.Add((viaje.Id, fecha));
                    }
                }
            }

            decimal gananciasPotenciales = 0m;
            int enviosPosibles = 0;
            int viajesAnalizados = viajes.Count;

            var gananciasPorSemana = new Dictionary<int, decimal>();

            string mejorRecorrido = string.Empty;
            decimal mayorGananciaPorRecorrido = 0m;

            foreach (var fechaViaje in fechasViajes)
            {
                var viaje = viajes.First(v => v.Id == fechaViaje.ViajeId);

                var asignados = await _context.Envios
                    .AsNoTracking()
                    .Where(e =>
                        e.ViajeId == viaje.Id &&
                        e.Fecha >= fechaViaje.Fecha.Date &&
                        e.Fecha < fechaViaje.Fecha.Date.AddDays(1) &&
                        e.Estado != "Cancelado")
                    .Select(e => new
                    {
                        e.Peso,
                        e.DesvioKm
                    })
                    .ToListAsync();

                int paquetesLibres = viaje.MaxPaquetes - asignados.Count;

                decimal pesoLibre =
                    viaje.PesoMaxKg - asignados.Sum(e => e.Peso);

                double desvioLibre =
                    (double)(viaje.DesvioMaxKm - asignados.Sum(e => e.DesvioKm));

                if (paquetesLibres <= 0 || pesoLibre <= 0 || desvioLibre <= 0)
                    continue;


                var oportunidades =
                await _oportunidadService.ObtenerCandidatosPorFechaAsync(viaje.Id, usuarioId, fechaViaje.Fecha, ordenOportunidad);


                decimal gananciaRecorrido = 0m;
                double desvioSeleccionado = 0;

                foreach (var oportunidad in oportunidades)
                {
                    if (enviosContabilizados.Contains(oportunidad.EnvioId))
                        continue;

                    if (paquetesLibres <= 0 ||
                        oportunidad.Peso > pesoLibre)
                    {
                        continue;
                    }

                    if (desvioSeleccionado + oportunidad.DesvioKm > desvioLibre)
                        continue;

                    enviosContabilizados.Add(oportunidad.EnvioId);

                    gananciasPotenciales += oportunidad.MontoOfrecido;
                    gananciaRecorrido += oportunidad.MontoOfrecido;

                    enviosPosibles++;
                    paquetesLibres--;
                    pesoLibre -= oportunidad.Peso;
                    desvioSeleccionado += oportunidad.DesvioKm;
                }

                int semana = ((fechaViaje.Fecha.Day - 1) / 7) + 1;

                if (!gananciasPorSemana.ContainsKey(semana))
                    gananciasPorSemana[semana] = 0m;

                gananciasPorSemana[semana] += gananciaRecorrido;

                if (gananciaRecorrido > mayorGananciaPorRecorrido)
                {
                    mayorGananciaPorRecorrido = gananciaRecorrido;

                    mejorRecorrido =
                        $"{viaje.OrigenDir} → {viaje.DestinoDir} " +
                        $"({fechaViaje.Fecha:dd/MM})";
                }
            }

            var resumen = new ResumenGananciasMensualDto
            {
                CriterioSeleccionado = criterioNormalizado,
                GananciasActuales = gananciasActuales,
                GananciasPotenciales = gananciasPotenciales,
                ViajesAnalizados = viajesAnalizados,
                EnviosPosibles = enviosPosibles,

                PromedioPorViaje = viajesAnalizados > 0
                    ? gananciasPotenciales / viajesAnalizados
                    : 0m,

                MejorRecorrido = mejorRecorrido,

                GananciasPorSemana = gananciasPorSemana
                    .OrderBy(g => g.Key)
                    .Select(g => new GananciaSemanalDto
                    {
                        Semana = g.Key,
                        Ganancia = g.Value
                    })
                    .ToList()
            };

            return resumen;
        }

    }
}
