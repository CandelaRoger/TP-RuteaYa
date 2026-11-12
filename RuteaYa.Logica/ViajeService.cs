using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica.Rutas;

namespace RuteaYa.Logica
{
    public class ViajeService : IViajeService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IRutaService _rutaService;


        public ViajeService(RuteaYaDbContext context, IRutaService rutaService)
        {
            _context = context;
            _rutaService = rutaService;

        }

        public async Task<ResultadoOperacion<ViajeDto>> CrearViajeAsync(
            CrearViajeDto dto, int usuarioId)
        {
            var existeUsuario = await _context.Usuarios
                .AnyAsync(u => u.Id == usuarioId);

            if (!existeUsuario)
                return ResultadoOperacion<ViajeDto>.Falla(
                    $"El usuario con ID {usuarioId} no existe.");

            var errorValidacion = ValidarViaje(dto);

            if (errorValidacion != null)
                return ResultadoOperacion<ViajeDto>.Falla(errorValidacion);

            var paradasLimpias = dto.Paradas?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList() ?? new List<string>();

            Coordenadas? coordenadasOrigen;

            try
            {
                coordenadasOrigen = await _rutaService.GeocodificarAsync(
                    dto.Origen.Trim());
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo conectar con el servicio de mapas.");
            }

            if (coordenadasOrigen == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo encontrar la dirección de origen.");

            var coordenadasParadas = new List<Coordenadas>();

            foreach (var parada in paradasLimpias)
            {
                Coordenadas? coordenadas;

                try
                {
                    coordenadas = await _rutaService.GeocodificarAsync(parada);
                }
                catch (HttpRequestException)
                {
                    return ResultadoOperacion<ViajeDto>.Falla(
                        "No se pudo conectar con el servicio de mapas.");
                }

                if (coordenadas == null)
                {
                    return ResultadoOperacion<ViajeDto>.Falla(
                        $"No se pudo encontrar la dirección de la parada '{parada}'.");
                }

                coordenadasParadas.Add(coordenadas);
            }

            Coordenadas? coordenadasDestino;

            try
            {
                coordenadasDestino = await _rutaService.GeocodificarAsync(
                    dto.Destino.Trim());
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo conectar con el servicio de mapas.");
            }

            if (coordenadasDestino == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo encontrar la dirección de destino.");

            var puntosRuta = new List<Coordenadas>
    {
        coordenadasOrigen
    };

            puntosRuta.AddRange(coordenadasParadas);
            puntosRuta.Add(coordenadasDestino);

            ResultadoRuta? resultadoRuta;

            try
            {
                resultadoRuta = await _rutaService.CalcularRutaAsync(
                    puntosRuta);
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo calcular la ruta mediante el servicio de mapas.");
            }

            if (resultadoRuta == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo calcular el recorrido solicitado.");

            var viaje = new Viaje
            {
                UsuarioId = usuarioId,

                OrigenDir = dto.Origen.Trim(),
                OrigenLat = (decimal)coordenadasOrigen.Latitud,
                OrigenLng = (decimal)coordenadasOrigen.Longitud,

                DestinoDir = dto.Destino.Trim(),
                DestinoLat = (decimal)coordenadasDestino.Latitud,
                DestinoLng = (decimal)coordenadasDestino.Longitud,

                HoraSalida = dto.HoraSalida,
                HoraLlegada = dto.HoraLlegada,

                MaxPaquetes = dto.MaxPaquetes,
                PesoMaxKg = dto.PesoMaxKg,
                DesvioMaxKm = dto.DesvioMaxKm,

                DistanciaKm = Math.Round(
                    (decimal)resultadoRuta.DistanciaKm, 2),

                DuracionMin = (int)Math.Ceiling(
                    resultadoRuta.DuracionMinutos),

                Activo = true
            };

            for (int i = 0; i < paradasLimpias.Count; i++)
            {
                viaje.Paradas.Add(new Parada
                {
                    Orden = i + 1,
                    Direccion = paradasLimpias[i],
                    Lat = (decimal)coordenadasParadas[i].Latitud,
                    Lng = (decimal)coordenadasParadas[i].Longitud
                });
            }

            foreach (var dia in dto.DiasSemana.Distinct())
            {
                viaje.DiasSemana.Add(new ViajeDia
                {
                    DiaSemana = dia
                });
            }

            _context.Viajes.Add(viaje);

            await _context.SaveChangesAsync();

            return ResultadoOperacion<ViajeDto>.Ok(
                MapearAViajeDto(viaje));
        }



        public async Task<ResultadoOperacion<ViajeDto>> ActualizarViajeAsync(
            int viajeId,
            CrearViajeDto dto,
            int usuarioId)
        {
            
            var viaje = await _context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.DiasSemana)
                .FirstOrDefaultAsync(v => v.Id == viajeId && v.UsuarioId == usuarioId);

            if (viaje == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "El viaje no fue encontrado o no te pertenece.");

            var errorValidacion = ValidarViaje(dto);

            if (errorValidacion != null)
                return ResultadoOperacion<ViajeDto>.Falla(errorValidacion);

            var paradas = dto.Paradas?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList() ?? new List<string>();

            Coordenadas? origen;

            try
            {
                origen = await _rutaService.GeocodificarAsync(dto.Origen.Trim());
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo conectar con el servicio de mapas.");
            }

            if (origen == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se encontró el origen.");

            var coordenadasParadas = new List<Coordenadas>();

            foreach (var parada in paradas)
            {
                Coordenadas? coordenadas;

                try
                {
                    coordenadas = await _rutaService.GeocodificarAsync(parada);
                }
                catch (HttpRequestException)
                {
                    return ResultadoOperacion<ViajeDto>.Falla(
                        "No se pudo conectar con el servicio de mapas.");
                }

                if (coordenadas == null)
                    return ResultadoOperacion<ViajeDto>.Falla(
                        $"No se encontró la parada '{parada}'.");

                coordenadasParadas.Add(coordenadas);
            }

            Coordenadas? destino;

            try
            {
                destino = await _rutaService.GeocodificarAsync(dto.Destino.Trim());
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo conectar con el servicio de mapas.");
            }

            if (destino == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se encontró el destino.");

            var puntos = new List<Coordenadas> { origen };
            puntos.AddRange(coordenadasParadas);
            puntos.Add(destino);

            ResultadoRuta? resultadoRuta;

            try
            {
                resultadoRuta = await _rutaService.CalcularRutaAsync(puntos);
            }
            catch (HttpRequestException)
            {
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo calcular la nueva ruta.");
            }

            if (resultadoRuta == null)
                return ResultadoOperacion<ViajeDto>.Falla(
                    "No se pudo calcular el recorrido solicitado.");

            viaje.OrigenDir = dto.Origen.Trim();
            viaje.OrigenLat = (decimal)origen.Latitud;
            viaje.OrigenLng = (decimal)origen.Longitud;

            viaje.DestinoDir = dto.Destino.Trim();
            viaje.DestinoLat = (decimal)destino.Latitud;
            viaje.DestinoLng = (decimal)destino.Longitud;

            viaje.HoraSalida = dto.HoraSalida;
            viaje.HoraLlegada = dto.HoraLlegada;
            viaje.MaxPaquetes = dto.MaxPaquetes;
            viaje.PesoMaxKg = dto.PesoMaxKg;
            viaje.DesvioMaxKm = dto.DesvioMaxKm;

            viaje.DistanciaKm = Math.Round(
                (decimal)resultadoRuta.DistanciaKm, 2);

            viaje.DuracionMin = (int)Math.Ceiling(
                resultadoRuta.DuracionMinutos);

            _context.RemoveRange(viaje.Paradas);
            viaje.Paradas.Clear();

            for (int i = 0; i < paradas.Count; i++)
            {
                viaje.Paradas.Add(new Parada
                {
                    Orden = i + 1,
                    Direccion = paradas[i],
                    Lat = (decimal)coordenadasParadas[i].Latitud,
                    Lng = (decimal)coordenadasParadas[i].Longitud
                });
            }

            _context.RemoveRange(viaje.DiasSemana);
            viaje.DiasSemana.Clear();

            foreach (var dia in dto.DiasSemana.Distinct())
            {
                viaje.DiasSemana.Add(new ViajeDia
                {
                    DiaSemana = dia
                });
            }

            
            await _context.SaveChangesAsync();

            return ResultadoOperacion<ViajeDto>.Ok(
                MapearAViajeDto(viaje));
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

            if (viaje == null)
                return null;

            var dto = MapearAViajeDto(viaje);

            var puntos = new List<Coordenadas>
    {
        new()
        {
            Latitud = (double)viaje.OrigenLat,
            Longitud = (double)viaje.OrigenLng
        }
    };

            puntos.AddRange(viaje.Paradas
                .OrderBy(p => p.Orden)
                .Select(p => new Coordenadas
                {
                    Latitud = (double)p.Lat,
                    Longitud = (double)p.Lng
                }));

            puntos.Add(new Coordenadas
            {
                Latitud = (double)viaje.DestinoLat,
                Longitud = (double)viaje.DestinoLng
            });

            var resultadoRuta = await _rutaService.CalcularRutaAsync(puntos);

            if (resultadoRuta != null)
            {
                dto.PuntosRuta = resultadoRuta.PuntosRuta
                    .Select(p => new CoordenadaDto
                    {
                        Latitud = p.Latitud,
                        Longitud = p.Longitud
                    })
                    .ToList();
            }

            return dto;
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
                OrigenLat = viaje.OrigenLat,
                OrigenLng = viaje.OrigenLng,
                DestinoDir = viaje.DestinoDir,
                DestinoLat = viaje.DestinoLat,
                DestinoLng = viaje.DestinoLng,
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
  

    
private string? ValidarViaje(CrearViajeDto dto)
{
    if (dto.Origen.Trim().Equals(
        dto.Destino.Trim(),
        StringComparison.OrdinalIgnoreCase))
    {
        return "El origen y el destino deben ser diferentes.";
    }

    if (dto.HoraLlegada <= dto.HoraSalida)
        return "La hora de llegada debe ser posterior a la de salida.";

    var paradasLimpias = dto.Paradas?
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p.Trim())
        .ToList() ?? new List<string>();

    var paradasUnicas = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    foreach (var parada in paradasLimpias)
    {
        if (parada.Equals(dto.Origen.Trim(),
                StringComparison.OrdinalIgnoreCase) ||
            parada.Equals(dto.Destino.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return $"La parada '{parada}' no puede coincidir con el origen o destino.";
        }

        if (!paradasUnicas.Add(parada))
            return $"La parada '{parada}' está repetida.";
    }

    return null;
}

}
}