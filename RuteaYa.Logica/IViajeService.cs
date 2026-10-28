using System;
using System.Collections.Generic;
using System.Text;

using RuteaYa.DTOs;

namespace RuteaYa.Logica
{
    public interface IViajeService
    {
        Task<ResultadoOperacion<ViajeDto>> CrearViajeAsync(CrearViajeDto dto, int usuarioId);
        Task<List<ViajeDto>> ObtenerViajesPorUsuarioAsync(int usuarioId, string? filtro = "Todos");
        Task<ViajeDto?> ObtenerViajePorIdAsync(int viajeId, int usuarioId);
        Task<ResultadoOperacion<bool>> CambiarEstadoActivoAsync(int viajeId, int usuarioId);
    }

    public class ResultadoOperacion<T>
    {
        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }
        public T? Datos { get; set; }

        public static ResultadoOperacion<T> Ok(T datos) => new() { Exitoso = true, Datos = datos };
        public static ResultadoOperacion<T> Falla(string mensaje) => new() { Exitoso = false, MensajeError = mensaje };
    }
}