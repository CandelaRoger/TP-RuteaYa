using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;

namespace RuteaYa.Logica
{
    public class ViajeService : IViajeService
    {
        private readonly RuteaYaDbContext _context;

        public ViajeService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoOperacion<ViajeDto>> CrearViajeAsync(CrearViajeDto dto, int usuarioId)

        {
            // 0. Validar que el usuario exista en la base de datos
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!existeUsuario)
                return ResultadoOperacion<ViajeDto>.Falla($"El usuario con ID {usuarioId} no existe. Por favor registrate primero.");

            // 1. Validaciones básicas obligatorias (Reglas del PDF)
            if (string.IsNullOrWhiteSpace(dto.Origen))
                return ResultadoOperacion<ViajeDto>.Falla("El origen es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Destino))
                return ResultadoOperacion<ViajeDto>.Falla("El destino es obligatorio.");

            if (dto.Origen.Trim().Equals(dto.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacion<ViajeDto>.Falla("El origen y el destino deben ser diferentes.");

            if (dto.DiasSemana == null || dto.DiasSemana.Count == 0)
                return ResultadoOperacion<ViajeDto>.Falla("Debe seleccionar al menos un día de la semana.");

            if (dto.HoraLlegada <= dto.HoraSalida)
                return ResultadoOperacion<ViajeDto>.Falla("La hora de llegada debe ser posterior a la de salida.");

            if (dto.MaxPaquetes <= 0)
                return ResultadoOperacion<ViajeDto>.Falla("La cantidad máxima de paquetes debe ser mayor a cero.");

            if (dto.PesoMaxKg <= 0)
                return ResultadoOperacion<ViajeDto>.Falla("El peso máximo debe ser mayor a cero.");

            if (dto.DesvioMaxKm < 0)
                return ResultadoOperacion<ViajeDto>.Falla("El desvío máximo no puede ser negativo.");

            // 2. Validaciones de paradas intermedias
            var paradasLimpias = dto.Paradas?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList() ?? new List<string>();

            var paradasUnicas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in paradasLimpias)
            {
                if (p.Equals(dto.Origen.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    p.Equals(dto.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion<ViajeDto>.Falla($"La parada '{p}' no puede coincidir con el origen o destino.");
                }

                if (!paradasUnicas.Add(p))
                {
                    return ResultadoOperacion<ViajeDto>.Falla($"La parada '{p}' está repetida.");
                }
            }

            // 3. Cálculo de duración en minutos según horarios
            int duracionMinutos = (int)(dto.HoraLlegada - dto.HoraSalida).TotalMinutes;

            // 4. Estimación de distancia simulada (Mock para cuando no hay API de Google Maps)
            // Calculamos 18.4 km base + 3 km por cada parada intermedia
            decimal distanciaEstimada = 18.4m + (paradasLimpias.Count * 3.0m);

            // 5. Crear la entidad Viaje
            var viaje = new Viaje
            {
                UsuarioId = usuarioId,
                OrigenDir = dto.Origen.Trim(),
                OrigenLat = -34.5622m, // Coordenadas de prueba (CABA / Belgrano)
                OrigenLng = -58.4563m,
                DestinoDir = dto.Destino.Trim(),
                DestinoLat = -34.7618m, // Coordenadas de prueba (GBA / Lomas de Zamora)
                DestinoLng = -58.4042m,
                HoraSalida = dto.HoraSalida,
                HoraLlegada = dto.HoraLlegada,
                MaxPaquetes = dto.MaxPaquetes,
                PesoMaxKg = dto.PesoMaxKg,
                DesvioMaxKm = dto.DesvioMaxKm,
                DistanciaKm = distanciaEstimada,
                DuracionMin = duracionMinutos,
                Activo = true
            };

            // 6. Agregar Paradas con orden secuencial (1, 2, 3...)
            int orden = 1;
            foreach (var paradaTexto in paradasLimpias)
            {
                viaje.Paradas.Add(new Parada
                {
                    Orden = orden++,
                    Direccion = paradaTexto,
                    Lat = -34.6037m,
                    Lng = -58.3816m
                });
            }

            // 7. Agregar Días de la semana
            foreach (var dia in dto.DiasSemana.Distinct())
            {
                viaje.DiasSemana.Add(new ViajeDia
                {
                    DiaSemana = dia
                });
            }

            // 8. Guardar en Base de Datos
            _context.Viajes.Add(viaje);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<ViajeDto>.Ok(MapearAViajeDto(viaje));
        }

        public async Task<List<ViajeDto>> ObtenerViajesPorUsuarioAsync(int usuarioId, string? filtro = "Todos")
        {
            var query = _context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.DiasSemana)
                .Where(v => v.UsuarioId == usuarioId);

            if (filtro == "Activos")
                query = query.Where(v => v.Activo);
            else if (filtro == "Inactivos")
                query = query.Where(v => !v.Activo);

            var viajes = await query.OrderByDescending(v => v.Id).ToListAsync();

            return viajes.Select(MapearAViajeDto).ToList();
        }

        public async Task<ViajeDto?> ObtenerViajePorIdAsync(int viajeId, int usuarioId)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.DiasSemana)
                .FirstOrDefaultAsync(v => v.Id == viajeId && v.UsuarioId == usuarioId);

            return viaje == null ? null : MapearAViajeDto(viaje);
        }

        public async Task<ResultadoOperacion<bool>> CambiarEstadoActivoAsync(int viajeId, int usuarioId)
        {
            var viaje = await _context.Viajes
                .FirstOrDefaultAsync(v => v.Id == viajeId && v.UsuarioId == usuarioId);

            if (viaje == null)
                return ResultadoOperacion<bool>.Falla("El viaje no fue encontrado o no te pertenece.");

            viaje.Activo = !viaje.Activo;
            await _context.SaveChangesAsync();

            return ResultadoOperacion<bool>.Ok(viaje.Activo);
        }

        // Método auxiliar para transformar Entidad en DTO y armar textos legibles
        private static ViajeDto MapearAViajeDto(Viaje viaje)
        {
            var diasNombres = new Dictionary<byte, string>
            {
                { 1, "Lun" }, { 2, "Mar" }, { 3, "Mié" },
                { 4, "Jue" }, { 5, "Vie" }, { 6, "Sáb" }, { 7, "Dom" }
            };

            var diasOrdenados = viaje.DiasSemana
                .OrderBy(d => d.DiaSemana)
                .Select(d => d.DiaSemana)
                .ToList();

            var textoDias = diasOrdenados.Count > 0
                ? string.Join(", ", diasOrdenados.Select(d => diasNombres.TryGetValue(d, out var n) ? n : d.ToString()))
                : "Sin días asignados";

            return new ViajeDto
            {
                Id = viaje.Id,
                UsuarioId = viaje.UsuarioId,
                OrigenDir = viaje.OrigenDir,
                DestinoDir = viaje.DestinoDir,
                HoraSalida = viaje.HoraSalida,
                HoraLlegada = viaje.HoraLlegada,
                MaxPaquetes = viaje.MaxPaquetes,
                PesoMaxKg = viaje.PesoMaxKg,
                DesvioMaxKm = viaje.DesvioMaxKm,
                DistanciaKm = viaje.DistanciaKm,
                DuracionMin = viaje.DuracionMin,
                Activo = viaje.Activo,
                DiasSemana = diasOrdenados,
                DiasTexto = textoDias,
                HorarioTexto = $"{viaje.HoraSalida:hh\\:mm} – {viaje.HoraLlegada:hh\\:mm}",
                Paradas = viaje.Paradas
                    .OrderBy(p => p.Orden)
                    .Select(p => new ParadaDto
                    {
                        Id = p.Id,
                        Orden = p.Orden,
                        Direccion = p.Direccion,
                        Lat = p.Lat,
                        Lng = p.Lng
                    }).ToList()
            };
        }
    }
}