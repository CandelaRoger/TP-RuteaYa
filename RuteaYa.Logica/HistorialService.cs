using RuteaYa.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace RuteaYa.Logica
{
    public interface IHistorialService
    {
        Task<HistorialViewModel> ObtenerHistorialUsuarioAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado);
    }
    public class HistorialService: IHistorialService
    {
        private readonly RuteaYaDbContext _context;

        public HistorialService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<HistorialViewModel> ObtenerHistorialUsuarioAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            // 1. Obtener viajes del usuario
            var viajesQuery = _context.Viajes
                .Where(v => v.UsuarioId == usuarioId);

            var viajes = await viajesQuery.OrderByDescending(v => v.Id).ToListAsync();

            // 2. Obtener envíos del usuario
            var enviosQuery = _context.Envios
                .Include(e => e.Viaje)
                .Where(e => e.Viaje != null &&
                            e.Viaje.UsuarioId == usuarioId);

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                enviosQuery = enviosQuery.Where(e => e.Estado == estado);
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                enviosQuery = enviosQuery.Where(e => e.Fecha >= fechaInicio.Value && e.Fecha <= fechaFin.Value);
            }

            var envios = await enviosQuery.OrderByDescending(e => e.Fecha).ToListAsync();

            // 3. Calcular ganancias (ej: envíos entregados o con conductor asignado)
            decimal gananciaTotal = envios
                                    .Where(e => e.Estado == "Entregado")
                                    .Sum(e => e.MontoOfrecido);
            // 4. Armar la lista unificada para la tabla de operaciones
            var operaciones = new List<ItemHistorialDto>();

            foreach (var v in viajes)
            {
                operaciones.Add(new ItemHistorialDto
                {
                    Tipo = "Viaje",
                    Ruta = $"{v.OrigenDir} → {v.DestinoDir}",
                    Detalle = $"Viaje regular (Max. {v.MaxPaquetes} paq.)",
                    Monto = 0m, // O el cálculo de ganancia por viaje si aplica
                    EstadoBadge = v.Activo ? "bg-primary" : "bg-secondary"
                });
            }

            foreach (var e in envios)
            {
                operaciones.Add(new ItemHistorialDto
                {
                    Tipo = "Envío",
                    Ruta = $"{e.OrigenDir} → {e.DestinoDir}",
                    Detalle = $"{e.Descripcion} • {e.Estado}",
                    Monto = e.MontoOfrecido,
                    EstadoBadge = "bg-info"
                });
            }

            return new HistorialViewModel
            {
                TotalViajes = viajes.Count,
                TotalEnvios = envios.Count,
                GananciaTotal = gananciaTotal,
                Operaciones = operaciones.OrderByDescending(o => o.Monto).ToList() 
            };
        }
    }
}
