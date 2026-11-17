using RuteaYa.DTOs;

namespace RuteaYa.Logica
{
    public interface IGananciasMensualesService
    {
        Task<ResumenGananciasMensualDto> ObtenerResumenAsync(int usuarioId);
    }
}