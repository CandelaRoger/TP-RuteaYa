
using RuteaYa.DTOs;
using Microsoft.EntityFrameworkCore;

namespace RuteaYa.Logica
{
    public interface IHistorialService
    {
        Task<HistorialViewModel> ObtenerHistorialUsuarioAsync(
            int usuarioId,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? estado);
    }

    public class HistorialService : IHistorialService
    {
        private readonly RuteaYaDbContext _context;

        public HistorialService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<HistorialViewModel> ObtenerHistorialUsuarioAsync(
            int usuarioId,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? estado)
        {
            var enviosQuery = _context.Envios
                .AsNoTracking()
                .Where(e => e.ViajeId.HasValue &&
                            e.Viaje != null &&
                            e.Viaje.UsuarioId == usuarioId);

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                enviosQuery = enviosQuery.Where(e => e.Estado == estado);
            }

            if (fechaInicio.HasValue)
            {
                var inicio = fechaInicio.Value.Date;
                enviosQuery = enviosQuery.Where(e => e.Fecha >= inicio);
            }

            if (fechaFin.HasValue)
            {
                var fin = fechaFin.Value.Date.AddDays(1);
                enviosQuery = enviosQuery.Where(e => e.Fecha < fin);
            }

            var envios = await enviosQuery
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            var enviosEntregados = envios
                .Where(e => e.Estado == "Entregado")
                .ToList();

            int totalEnvios = enviosEntregados.Count;

            decimal gananciaTotal = enviosEntregados
                .Sum(e => e.MontoOfrecido);

            int totalViajes = enviosEntregados
                .Where(e => e.ViajeId.HasValue)
                .Select(e => e.ViajeId!.Value)
                .Distinct()
                .Count();

            var operaciones = envios.Select(e => new ItemHistorialDto
            {
                Tipo = "Envío",
                Ruta = $"{e.OrigenDir} → {e.DestinoDir}",
                Detalle = e.Descripcion,
                Monto = e.Estado == "Entregado" ? e.MontoOfrecido : 0m,
                Estado = e.Estado,
                EstadoBadge = e.Estado switch
                {
                    "Buscando conductor" => "bg-secondary",
                    "Conductor asignado" => "bg-warning text-dark",
                    "Retirado" => "bg-info text-dark",
                    "En Camino" => "bg-primary",
                    "Entregado" => "bg-success",
                    "Cancelado" => "bg-danger",
                    _ => "bg-secondary"
                },
                Fecha = e.Fecha
            }).ToList();

            return new HistorialViewModel
            {
                TotalViajes = totalViajes,
                TotalEnvios = totalEnvios,
                GananciaTotal = gananciaTotal,
                Operaciones = operaciones
            };
        }
    }
}
