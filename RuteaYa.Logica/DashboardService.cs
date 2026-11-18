using RuteaYa.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using static RuteaYa.DTOs.DashboardDto;

namespace RuteaYa.Logica
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(int usuarioId);
    }
    public class DashboardService : IDashboardService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IGananciasMensualesService _gananciasMensualesService;


        public DashboardService(RuteaYaDbContext context, IGananciasMensualesService gananciasMensualesService)
        {
            _context = context;
            _gananciasMensualesService = gananciasMensualesService;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            string nombreCompleto = usuario != null ? usuario.Nombre : "Usuario";

            var viajesActivos = await _context.Viajes
                .Include(v => v.DiasSemana)
                .Where(v => v.UsuarioId == usuarioId && v.Activo)
                .ToListAsync();

            
            var enviosPendientes = await _context.Envios
                .Where(e => e.UsuarioId == usuarioId && e.Estado != "Entregado" && e.Estado != "Cancelado")
                .ToListAsync();

            
            var oportunidadesCount = await _context.Envios
                .Where(e => e.Estado == "Buscando conductor" && e.UsuarioId != usuarioId)
                .CountAsync();

            var resumenGanancias =
                await _gananciasMensualesService.ObtenerResumenAsync(usuarioId);

            return new DashboardDto
            {
                CantProximosViajes = viajesActivos.Count,
                CantEnviosPendientes = enviosPendientes.Count,
                CantOportunidades = oportunidadesCount,
                PotencialDelMes = resumenGanancias.GananciasPotenciales,
                NombreUsuario = nombreCompleto,
                ProximosViajes = viajesActivos.Select(v => new ViajeResumenDto
                {
                    Id = v.Id,
                    Origen = v.OrigenDir,   
                    Destino = v.DestinoDir,
                    Horario = $"{v.HoraSalida:hh\\:mm} - {v.HoraLlegada:hh\\:mm}",
                    Dias = string.Join(", ", v.DiasSemana.Select(d => d.DiaSemana)),
                    Activo = v.Activo
                }).Take(2).ToList(),
                EnviosPendientes = enviosPendientes.Select(e => new EnvioResumenDto
                {
                    Id = e.Id,
                    Origen = e.OrigenDir,
                    Destino = e.DestinoDir,
                    Descripcion = e.Descripcion,
                    Monto = e.MontoOfrecido,
                    Estado = e.Estado
                }).Take(2).ToList()
            };
        }
    }
}
