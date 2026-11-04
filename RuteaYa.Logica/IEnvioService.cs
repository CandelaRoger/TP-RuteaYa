using System;
using System.Collections.Generic;
using System.Text;

using RuteaYa.DTOs;

namespace RuteaYa.Logica
{
    public interface IEnvioService
    {
        Task<ResultadoOperacion<EnvioDto>> CrearEnvioAsync(CrearEnvioDto dto, int usuarioId);
        Task<List<EnvioDto>> ObtenerEnviosPorUsuarioAsync(int usuarioId, string? estado = "Todos");
        Task<EnvioDto?> ObtenerEnvioPorIdAsync(int envioId, int usuarioId);
        Task<ResultadoOperacion<bool>> CancelarEnvioAsync(int envioId, int usuarioId);
    }
}
