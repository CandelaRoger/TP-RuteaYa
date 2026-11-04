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

        // GET: api/viajes?filtro=Activos
        [HttpGet]
        public async Task<IActionResult> ObtenerViajes(
            [FromQuery] string? filtro = "Todos",
            [FromHeader(Name = "X-Usuario-Id")] int? usuarioId = null)
        {
            int id = usuarioId ?? 1; // Si no viene header, usamos el usuario 1 para pruebas en Swagger
            var viajes = await _viajeService.ObtenerViajesPorUsuarioAsync(id, filtro);
            return Ok(viajes);
        }

        // GET: api/viajes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(
            int id,
            [FromHeader(Name = "X-Usuario-Id")] int? usuarioId = null)
        {
            int uid = usuarioId ?? 1;
            var viaje = await _viajeService.ObtenerViajePorIdAsync(id, uid);

            if (viaje == null)
                return NotFound(new { mensaje = "El viaje no fue encontrado o no te pertenece." });

            return Ok(viaje);
        }

        // POST: api/viajes
        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearViajeDto dto
            )
        {
            string? userIdClaim = ExtraerIdDelClaim();

            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Unauthorized(new { mensaje = "Inicie sesion nuevamente" });
            }
            var resultado = await _viajeService.CrearViajeAsync(dto, uid);

            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            // 200
            return Ok(resultado.Datos);
        }

        private string? ExtraerIdDelClaim()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // PATCH: api/viajes/5/desactivar
        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> CambiarEstado(
            int id,
            [FromHeader(Name = "X-Usuario-Id")] int? usuarioId = null)
        {
            int uid = usuarioId ?? 1;
            var resultado = await _viajeService.CambiarEstadoActivoAsync(id, uid);

            if (!resultado.Exitoso)
                return NotFound(new { mensaje = resultado.MensajeError });

            return Ok(new { mensaje = "Estado del viaje actualizado correctamente.", activo = resultado.Datos });
        }
    }
}