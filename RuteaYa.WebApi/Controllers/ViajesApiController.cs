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
    public class ViajesApiController : ControllerBase
    {
        private readonly IViajeService _viajeService;

        public ViajesApiController(IViajeService viajeService)
        {
            _viajeService = viajeService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerViajes([FromQuery] string? filtro = "Todos")
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var viajes = await _viajeService.ObtenerViajesPorUsuarioAsync(uid, filtro);
            return Ok(viajes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var viaje = await _viajeService.ObtenerViajePorIdAsync(id, uid);
            if (viaje == null)
                return NotFound(new { mensaje = "El viaje no fue encontrado o no te pertenece." });

            return Ok(viaje);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearViajeDto dto)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var resultado = await _viajeService.CrearViajeAsync(dto, uid);
            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            return Ok(resultado.Datos);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CrearViajeDto dto)
        {
            string? usuarioIdClaim = ExtraerIdDelClaim();

            if (!int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "Inicie sesión nuevamente."
                });
            }

            var resultado = await _viajeService.ActualizarViajeAsync(
                id, dto, usuarioId);

            if (!resultado.Exitoso)
            {
                return BadRequest(new
                {
                    mensaje = resultado.MensajeError
                });
            }

            return Ok(resultado.Datos);
        }

        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            string? userIdClaim = ExtraerIdDelClaim();
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesión nuevamente." });
            }

            var resultado = await _viajeService.CambiarEstadoActivoAsync(id, uid);
            if (!resultado.Exitoso)
                return NotFound(new { mensaje = resultado.MensajeError });

            return Ok(new { mensaje = "Estado del viaje actualizado correctamente.", activo = resultado.Datos });
        }

        private string? ExtraerIdDelClaim()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
        }
    }
}