using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnviosApiController : ControllerBase
    {
        private readonly IEnvioService _envioService;

        public EnviosApiController(IEnvioService envioService)
        {
            _envioService = envioService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEnvios([FromQuery] string? estado = "Todos")
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var envios = await _envioService.ObtenerEnviosPorUsuarioAsync(uid, estado);
            return Ok(envios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var envio = await _envioService.ObtenerEnvioPorIdAsync(id, uid);
            if (envio == null)
                return NotFound(new { mensaje = "El envío no fue encontrado o no te pertenece." });

            return Ok(envio);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearEnvioDto dto)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var resultado = await _envioService.CrearEnvioAsync(dto, uid);
            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Datos!.Id }, resultado.Datos);
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var resultado = await _envioService.CancelarEnvioAsync(id, uid);
            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return Ok(new { mensaje = "Envío cancelado con éxito." });
        }

        private string? ExtraerIdDelClaim()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
        }
    }
}