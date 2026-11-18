using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;

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

        // GET: api/historial/{usuarioId}
        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> GetHistorial(int usuarioId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] string? estado = "Todos")
        {
            var historial = await _historialService.ObtenerHistorialUsuarioAsync(usuarioId, fechaInicio, fechaFin, estado);
            return Ok(historial);
        }
    }
}