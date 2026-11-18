using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica
{
    public interface ITransporteService
    {
        Task<List<PaqueteTransporteDto>> ObtenerPaquetesAceptadosPorUsuarioAsync(int usuarioId);
        Task<(bool Exito, string Mensaje)> CambiarEstadoEnvioAsync(int usuarioId, int envioId, string nuevoEstado);
    }
    public class TransporteService : ITransporteService
    {
        private readonly RuteaYaDbContext _context;

        public TransporteService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaqueteTransporteDto>> ObtenerPaquetesAceptadosPorUsuarioAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .SelectMany(u => u.Viajes)
                .SelectMany(v => v.Envios.Select(e => new PaqueteTransporteDto
                {
                    EnvioId = e.Id,
                    ViajeId = v.Id,
                    Descripcion = e.Descripcion,
                    OrigenDir = e.OrigenDir,
                    DestinoDir = e.DestinoDir,
                    MontoOfrecido = e.MontoOfrecido,
                    Estado = e.Estado
                }))
                .ToListAsync();
        }

        public async Task<(bool Exito, string Mensaje)> CambiarEstadoEnvioAsync(int usuarioId, int envioId, string nuevoEstado)
        {
            // 1. Buscar el envío incluyendo su viaje para validar que pertenezca al conductor
            var envio = await _context.Envios
                .Include(e => e.Viaje)
                .FirstOrDefaultAsync(e => e.Id == envioId);

            if (envio == null || envio.Viaje == null || envio.Viaje.UsuarioId != usuarioId)
            {
                return (false, "El paquete no existe o no pertenece a tus viajes.");
            }

            // 2. Validar transición estricta de estados
            bool transicionValida = (envio.Estado, nuevoEstado) switch
            {
                ("Conductor asignado", "Retirado") => true,
                ("Retirado", "En Camino") => true,
                ("En Camino", "Entregado") => true,
                _ => false
            };

            if (!transicionValida)
            {
                return (false, $"Transición inválida: no se puede pasar de '{envio.Estado}' a '{nuevoEstado}'.");
            }

            // 3. Guardar el nuevo estado
            envio.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return (true, "Estado actualizado correctamente.");
        }


    }
}
