
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/ganancias-mensuales")]
    [Authorize]
    public class GananciasMensualesApiController : ControllerBase
    {
        private readonly IGananciasMensualesService _gananciasService;

        public GananciasMensualesApiController(
            IGananciasMensualesService gananciasService)
        {
            _gananciasService = gananciasService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerResumen([FromQuery] string criterio = "ganancia")
        {
            var usuarioIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(usuarioIdClaim, out int usuarioId))
                return Unauthorized();

            var resumen = await _gananciasService.ObtenerResumenAsync(
                usuarioId, criterio);

            return Ok(resumen);
        }
    }
}
