using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransportesApiController : Controller
    {
        private readonly ITransporteService _transporteService;

        public TransportesApiController(ITransporteService transporteService)
        {
            _transporteService = transporteService;
        }

        [HttpGet("mis-paquetes")]
        public async Task<ActionResult<List<PaqueteTransporteDto>>> GetMisPaquetes()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized("Token inválido o sin identificador de usuario.");
            }

            var paquetes = await _transporteService.ObtenerPaquetesAceptadosPorUsuarioAsync(usuarioId);
            return Ok(paquetes);
        }

        [HttpPut("cambiar-estado")]
        public async Task<IActionResult> CambiarEstado([FromBody] ActualizarEstadoEnvioDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized("Token inválido.");
            }

            var resultado = await _transporteService.CambiarEstadoEnvioAsync(usuarioId, dto.EnvioId, dto.NuevoEstado);

            if (!resultado.Exito)
            {
                return BadRequest(resultado.Mensaje);
            }

            return Ok();
        }
    }
}
