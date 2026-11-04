using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;

namespace RuteaYa.Logica
{
    public class EnvioService : IEnvioService
    {
        private readonly RuteaYaDbContext _context;

        public EnvioService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoOperacion<EnvioDto>> CrearEnvioAsync(CrearEnvioDto dto, int usuarioId)
        {
            if (usuarioId <= 0)
                return ResultadoOperacion<EnvioDto>.Falla("Identificador de usuario inválido.");

            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!existeUsuario)
                return ResultadoOperacion<EnvioDto>.Falla("El usuario autenticado no existe en el sistema.");

            if (string.IsNullOrWhiteSpace(dto.Origen))
                return ResultadoOperacion<EnvioDto>.Falla("El origen es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Destino))
                return ResultadoOperacion<EnvioDto>.Falla("El destino es obligatorio.");

            if (dto.Origen.Trim().Equals(dto.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacion<EnvioDto>.Falla("El origen y el destino deben ser diferentes.");

            if (dto.Fecha.Date < DateTime.Today)
                return ResultadoOperacion<EnvioDto>.Falla("La fecha no puede ser anterior a la fecha actual.");

            if (dto.Peso <= 0)
                return ResultadoOperacion<EnvioDto>.Falla("El peso debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(dto.Tamanio))
                return ResultadoOperacion<EnvioDto>.Falla("Debe indicar el tamaño del paquete.");

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                return ResultadoOperacion<EnvioDto>.Falla("La descripción del paquete es obligatoria.");

            if (dto.MontoOfrecido <= 0)
                return ResultadoOperacion<EnvioDto>.Falla("El monto ofrecido debe ser mayor a cero.");

            var envio = new Envio
            {
                UsuarioId = usuarioId,
                OrigenDir = dto.Origen.Trim(),
                OrigenLat = -34.5833m, 
                OrigenLng = -58.4233m,
                DestinoDir = dto.Destino.Trim(),
                DestinoLat = -34.6037m,
                DestinoLng = -58.3816m,
                Fecha = dto.Fecha.Date,
                Peso = dto.Peso,
                Tamanio = dto.Tamanio.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                MontoOfrecido = dto.MontoOfrecido,
                Estado = "Buscando conductor" 
            };

            _context.Envios.Add(envio);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<EnvioDto>.Ok(MapearAEnvioDto(envio));
        }

        public async Task<List<EnvioDto>> ObtenerEnviosPorUsuarioAsync(int usuarioId, string? estado = "Todos")
        {
            var query = _context.Envios.Where(e => e.UsuarioId == usuarioId);

            if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Estado == estado);
            }

            var envios = await query.OrderByDescending(e => e.Id).ToListAsync();

            return envios.Select(MapearAEnvioDto).ToList();
        }

        public async Task<EnvioDto?> ObtenerEnvioPorIdAsync(int envioId, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == envioId && e.UsuarioId == usuarioId);

            return envio == null ? null : MapearAEnvioDto(envio);
        }

        public async Task<ResultadoOperacion<bool>> CancelarEnvioAsync(int envioId, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == envioId && e.UsuarioId == usuarioId);

            if (envio == null)
                return ResultadoOperacion<bool>.Falla("El paquete no fue encontrado o no te pertenece.");

            if (envio.Estado == "Cancelado")
                return ResultadoOperacion<bool>.Falla("El envío ya se encuentra cancelado.");

            if (envio.Estado == "Retirado" || envio.Estado == "En camino" || envio.Estado == "Entregado")
                return ResultadoOperacion<bool>.Falla($"No se puede cancelar un paquete que está '{envio.Estado}'.");

            envio.Estado = "Cancelado";
            await _context.SaveChangesAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

        private static EnvioDto MapearAEnvioDto(Envio e)
        {
            return new EnvioDto
            {
                Id = e.Id,
                UsuarioId = e.UsuarioId,
                OrigenDir = e.OrigenDir,
                DestinoDir = e.DestinoDir,
                Fecha = e.Fecha,
                Peso = e.Peso,
                Tamanio = e.Tamanio,
                Descripcion = e.Descripcion,
                MontoOfrecido = e.MontoOfrecido,
                Estado = e.Estado
            };
        }
    }
}