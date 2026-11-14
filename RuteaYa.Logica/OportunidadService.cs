using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
namespace RuteaYa.Logica
{
    public class OportunidadService : IOportunidadService
    {
        private readonly RuteaYaDbContext _context;

        public OportunidadService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<List<OportunidadDto>> ObtenerCandidatosAsync(
            int viajeId,
            int usuarioId)
        {
            // 1. Buscar el viaje del conductor
            var viaje = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.DiasSemana)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe, está inactivo o no te pertenece.");
            }

            // 2. Buscar envíos disponibles
            var envios = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.Estado == "Buscando conductor" &&
                    e.ViajeId == null &&
                    e.UsuarioId != usuarioId &&
                    e.Fecha >= DateTime.Today &&
                    e.Peso <= viaje.PesoMaxKg)
                .ToListAsync();

            // 3. Obtener días habilitados del viaje
            var diasPermitidos = viaje.DiasSemana
                .Select(d => (int)d.DiaSemana)
                .ToHashSet();

            // 4. Filtrar por día de la semana
            var candidatos = envios
                .Where(e =>
                {
                    int dia = (int)e.Fecha.DayOfWeek;

                    // En C# domingo es 0,
                    // pero en RuteaYa domingo es 7
                    if (dia == 0)
                        dia = 7;

                    return diasPermitidos.Contains(dia);
                })
                .Select(e => new OportunidadDto
                {
                    EnvioId = e.Id,
                    OrigenDir = e.OrigenDir,
                    DestinoDir = e.DestinoDir,
                    Fecha = e.Fecha,
                    Peso = e.Peso,
                    Tamanio = e.Tamanio,
                    Descripcion = e.Descripcion,
                    MontoOfrecido = e.MontoOfrecido
                })
                .ToList();

            return candidatos;
        }

        public async Task<ResultadoOperacion<bool>> AceptarEnvioAsync(
            int viajeId,
            int envioId,
            int usuarioId)
        {
            // 1. Iniciar una transacción
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            // 2. Buscar el viaje y comprobar que pertenece al conductor
            var viaje = await _context.Viajes
                .Include(v => v.DiasSemana)
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId &&
                    v.Activo);

            if (viaje == null)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no existe, está inactivo o no te pertenece.");

            // 3. Buscar el envío disponible
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e =>
                    e.Id == envioId &&
                    e.Estado == "Buscando conductor" &&
                    e.ViajeId == null);

            if (envio == null)
                return ResultadoOperacion<bool>.Falla(
                    "El envío ya no está disponible.");
            if (envio.UsuarioId == usuarioId)
            {
                return ResultadoOperacion<bool>.Falla(
                    "No podés aceptar un envío creado por vos mismo.");
            }
            // 4. Comprobar la fecha
            if (envio.Fecha.Date < DateTime.Today)
                return ResultadoOperacion<bool>.Falla(
                    "No se pueden aceptar envíos con fechas anteriores.");

            // 5. Comprobar los días habilitados
            int dia = (int)envio.Fecha.DayOfWeek;

            if (dia == 0)
                dia = 7;

            if (!viaje.DiasSemana.Any(d => (int)d.DiaSemana == dia))
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no está disponible para ese día.");

            // 6. Calcular la capacidad ya utilizada
            var enviosAsignados = await _context.Envios
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Estado != "Cancelado")
                .ToListAsync();

            int cantidadPaquetes = enviosAsignados.Count;
            decimal pesoOcupado = enviosAsignados.Sum(e => e.Peso);

            // 7. Validar cantidad máxima de paquetes
            if (cantidadPaquetes + 1 > viaje.MaxPaquetes)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje alcanzó la cantidad máxima de paquetes.");

            // 8. Validar peso máximo
            if (pesoOcupado + envio.Peso > viaje.PesoMaxKg)
                return ResultadoOperacion<bool>.Falla(
                    "El envío supera el peso disponible del viaje.");


            // 9. Asignar el envío al viaje
            envio.ViajeId = viajeId;
            envio.Estado = "Asignado";

            // Buscar el último orden de parada
            int ultimoOrden = await _context.Paradas
                .Where(p => p.ViajeId == viajeId)
                .Select(p => (int?)p.Orden)
                .MaxAsync() ?? 0;

            // 10. Crear parada de retiro
            var paradaRetiro = new Parada
            {
                ViajeId = viajeId,
                EnvioId = envio.Id,
                Tipo = "Retiro",
                Direccion = envio.OrigenDir,
                Lat = envio.OrigenLat,
                Lng = envio.OrigenLng,
                Orden = ultimoOrden + 1
            };

            // 11. Crear parada de entrega
            var paradaEntrega = new Parada
            {
                ViajeId = viajeId,
                EnvioId = envio.Id,
                Tipo = "Entrega",
                Direccion = envio.DestinoDir,
                Lat = envio.DestinoLat,
                Lng = envio.DestinoLng,
                Orden = ultimoOrden + 2
            };

            _context.Paradas.AddRange(
                paradaRetiro,
                paradaEntrega);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<bool>.Ok(true);

        }

        public async Task<List<OportunidadDto>> ObtenerAsignadosAsync(
            int viajeId,
            int usuarioId)
        {
            // 1. Verificar que el viaje pertenece al conductor
            var existeViaje = await _context.Viajes
                .AsNoTracking()
                .AnyAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (!existeViaje)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe o no te pertenece.");
            }

            // 2. Buscar los envíos asignados a ese viaje
            var envios = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Estado != "Cancelado")
                .OrderBy(e => e.Fecha)
                .Select(e => new OportunidadDto
                {
                    EnvioId = e.Id,
                    OrigenDir = e.OrigenDir,
                    DestinoDir = e.DestinoDir,
                    Fecha = e.Fecha,
                    Peso = e.Peso,
                    Tamanio = e.Tamanio,
                    Descripcion = e.Descripcion,
                    MontoOfrecido = e.MontoOfrecido
                })
                .ToListAsync();

            return envios;
        }

        public async Task<CapacidadViajeDto> ObtenerCapacidadAsync(
            int viajeId,
            int usuarioId)
        {
            // 1. Buscar el viaje y verificar su propietario
            var viaje = await _context.Viajes
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (viaje == null)
            {
                throw new KeyNotFoundException(
                    "El viaje no existe o no te pertenece.");
            }

            // 2. Calcular los paquetes y el peso ocupado
            var ocupacion = await _context.Envios
                .AsNoTracking()
                .Where(e =>
                    e.ViajeId == viajeId &&
                    e.Estado != "Cancelado")
                .GroupBy(e => e.ViajeId)
                .Select(g => new
                {
                    Cantidad = g.Count(),
                    PesoTotal = g.Sum(e => e.Peso)
                })
                .FirstOrDefaultAsync();

            int paquetesOcupados = ocupacion?.Cantidad ?? 0;
            decimal pesoOcupado = ocupacion?.PesoTotal ?? 0m;

            // 3. Calcular la capacidad restante
            return new CapacidadViajeDto
            {
                ViajeId = viaje.Id,

                MaxPaquetes = viaje.MaxPaquetes,
                PaquetesOcupados = paquetesOcupados,
                PaquetesDisponibles = Math.Max(
                    0, viaje.MaxPaquetes - paquetesOcupados),

                PesoMaxKg = viaje.PesoMaxKg,
                PesoOcupadoKg = pesoOcupado,
                PesoDisponibleKg = Math.Max(
                    0m, viaje.PesoMaxKg - pesoOcupado)
            };
        }


        public async Task<ResultadoOperacion<bool>> QuitarEnvioAsync(
            int viajeId,
            int envioId,
            int usuarioId)
        {
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            // Verificar que el viaje pertenezca al conductor
            var viaje = await _context.Viajes
                .FirstOrDefaultAsync(v =>
                    v.Id == viajeId &&
                    v.UsuarioId == usuarioId);

            if (viaje == null)
                return ResultadoOperacion<bool>.Falla(
                    "El viaje no existe o no te pertenece.");

            // Buscar el envío asignado
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e =>
                    e.Id == envioId &&
                    e.ViajeId == viajeId &&
                    e.Estado == "Asignado");

            if (envio == null)
                return ResultadoOperacion<bool>.Falla(
                    "El envío no está asignado a este viaje.");

            // Buscar las paradas pertenecientes al envío
            var paradas = await _context.Paradas
                .Where(p =>
                    p.ViajeId == viajeId &&
                    p.EnvioId == envioId)
                .ToListAsync();

            // Eliminar retiro y entrega
            _context.Paradas.RemoveRange(paradas);

            // Devolver el envío a su estado anterior
            envio.ViajeId = null;
            envio.Estado = "Buscando conductor";

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

    }
}
