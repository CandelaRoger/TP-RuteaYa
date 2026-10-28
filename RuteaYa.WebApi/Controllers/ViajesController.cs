using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ViajesController : ControllerBase
    {
        private readonly IViajeService _viajeService;

        public ViajesController(IViajeService viajeService)
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
            [FromBody] CrearViajeDto dto,
            [FromHeader(Name = "X-Usuario-Id")] int? usuarioId = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int uid = usuarioId ?? 1;
            var resultado = await _viajeService.CrearViajeAsync(dto, uid);

            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.MensajeError });

            // 201 Created con la URL del recurso creado y los datos del viaje
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Datos!.Id }, resultado.Datos);
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