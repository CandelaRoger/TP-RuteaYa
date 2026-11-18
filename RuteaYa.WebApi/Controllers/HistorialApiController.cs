
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HistorialController : ControllerBase
    {
        private readonly IHistorialService _historialService;

        public HistorialController(IHistorialService historialService)
        {
            _historialService = historialService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] string? estado = "Todos")
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized("Token inválido o sin identificador de usuario.");
            }

            if (fechaInicio.HasValue && fechaFin.HasValue &&
                fechaInicio.Value.Date > fechaFin.Value.Date)
            {
                return BadRequest("La fecha inicial no puede ser posterior a la fecha final.");
            }

            var historial = await _historialService.ObtenerHistorialUsuarioAsync(
                usuarioId,
                fechaInicio,
                fechaFin,
                estado);

            return Ok(historial);
        }
    }
}
