using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnviosController : ControllerBase
    {
        private readonly IEnvioService _envioService;

        public EnviosController(IEnvioService envioService)
        {
            _envioService = envioService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEnvios([FromQuery] string? estado = "Todos")
        {
            int? usuarioId = ObtenerUsuarioIdActual();
            if (!usuarioId.HasValue)
                return Unauthorized(new { mensaje = "Debe enviar un token de autenticación válido." });

            var envios = await _envioService.ObtenerEnviosPorUsuarioAsync(usuarioId.Value, estado);
            return Ok(envios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            int? usuarioId = ObtenerUsuarioIdActual();
            if (!usuarioId.HasValue)
                return Unauthorized(new { mensaje = "Debe enviar un token de autenticación válido." });

            var envio = await _envioService.ObtenerEnvioPorIdAsync(id, usuarioId.Value);

            if (envio == null)
                return NotFound(new { mensaje = "El envío no fue encontrado o no te pertenece." });

            return Ok(envio);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearEnvioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int? usuarioId = ObtenerUsuarioIdActual();
            if (!usuarioId.HasValue)
                return Unauthorized(new { mensaje = "Debe enviar un token de autenticación válido para publicar un envío." });

            var resultado = await _envioService.CrearEnvioAsync(dto, usuarioId.Value);

            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Datos!.Id }, resultado.Datos);
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            int? usuarioId = ObtenerUsuarioIdActual();
            if (!usuarioId.HasValue)
                return Unauthorized(new { mensaje = "Debe enviar un token de autenticación válido." });

            var resultado = await _envioService.CancelarEnvioAsync(id, usuarioId.Value);

            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return Ok(new { mensaje = "Envío cancelado con éxito." });
        }

        private int? ObtenerUsuarioIdActual()
        {
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authHeaderStr = authHeader.ToString();
                if (authHeaderStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var tokenStr = authHeaderStr.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(tokenStr);
                        var subClaim = jwt.Claims.FirstOrDefault(c =>
                            c.Type == JwtRegisteredClaimNames.Sub ||
                            c.Type == ClaimTypes.NameIdentifier)?.Value;

                        if (int.TryParse(subClaim, out int idToken))
                            return idToken;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }
    }
}