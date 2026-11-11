
using RuteaYa.DTOs;

namespace RuteaYa.Logica
{
    public interface IOportunidadService
    {
        Task<List<OportunidadDto>> ObtenerCandidatosAsync(
            int viajeId,
            int usuarioId);

        Task<ResultadoOperacion<bool>> AceptarEnvioAsync(
            int viajeId,
            int envioId,
            int usuarioId);
        Task<List<OportunidadDto>> ObtenerAsignadosAsync(
    int viajeId,
    int usuarioId);
    }

}
