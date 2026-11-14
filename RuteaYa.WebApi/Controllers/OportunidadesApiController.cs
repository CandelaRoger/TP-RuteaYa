
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica;
using System.Security.Claims;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/oportunidades")]
    [Authorize]
    public class OportunidadesApiController : ControllerBase
    {
        private readonly IOportunidadService _service;

        public OportunidadesApiController(
            IOportunidadService service)
        {
            _service = service;
        }

        [HttpGet("viaje/{viajeId}")]
        public async Task<IActionResult> ObtenerPorViaje(
            int viajeId)
        {
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claim, out int usuarioId))
                return Unauthorized();

            try
            {
                var candidatos =
                    await _service.ObtenerCandidatosAsync(
                        viajeId, usuarioId);

                return Ok(candidatos);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPost("viaje/{viajeId}/aceptar/{envioId}")]
        public async Task<IActionResult> AceptarEnvio(
            int viajeId,
            int envioId)
        {
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out int usuarioId))
                return Unauthorized(new
                {
                    mensaje = "Inicie sesión nuevamente."
                });

            var resultado = await _service.AceptarEnvioAsync(
                viajeId,
                envioId,
                usuarioId);

            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return Ok(new
            {
                mensaje = "Envío aceptado correctamente."
            });
        }

        [HttpGet("viaje/{viajeId}/asignados")]
        public async Task<IActionResult> ObtenerAsignados(int viajeId)
        {
            // Obtener el usuario autenticado desde el JWT
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(claim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "Inicie sesión nuevamente."
                });
            }

            try
            {
                // Consultar los paquetes asignados al viaje
                var asignados = await _service.ObtenerAsignadosAsync(
                    viajeId,
                    usuarioId);

                return Ok(asignados);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpGet("viaje/{viajeId}/capacidad")]
        public async Task<IActionResult> ObtenerCapacidad(int viajeId)
        {
            // Obtener el ID del usuario autenticado
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(claim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "Inicie sesión nuevamente."
                });
            }

            try
            {
                // Consultar la capacidad del viaje
                var capacidad = await _service.ObtenerCapacidadAsync(
                    viajeId,
                    usuarioId);

                return Ok(capacidad);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPost("viaje/{viajeId}/quitar/{envioId}")]
        public async Task<IActionResult> QuitarEnvio(
            int viajeId,
            int envioId)
        {
            // Obtener el usuario autenticado
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(claim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "Inicie sesión nuevamente."
                });
            }

            // Quitar el envío del viaje
            var resultado = await _service.QuitarEnvioAsync(
                viajeId,
                envioId,
                usuarioId);

            if (!resultado.Exitoso)
            {
                return BadRequest(new
                {
                    mensaje = resultado.MensajeError
                });
            }

            return Ok(new
            {
                mensaje = "El envío fue quitado correctamente."
            });
        }


    }

}
