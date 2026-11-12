
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Rutas;

namespace RuteaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RutasController : ControllerBase
    {
        private readonly IRutaService _rutaService;

        public RutasController(IRutaService rutaService)
        {
            _rutaService = rutaService;
        }

        [HttpGet("geocodificar")]
        public async Task<IActionResult> Geocodificar(
            [FromQuery] string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
                return BadRequest("La dirección es obligatoria.");

            var coordenadas = await _rutaService.GeocodificarAsync(direccion);

            if (coordenadas == null)
                return NotFound("No se encontraron coordenadas para esa dirección.");

            return Ok(coordenadas);
        }

        [HttpPost("calcular")]
        public async Task<IActionResult> CalcularRuta(
            [FromBody] List<Coordenadas> puntos)
        {
            var resultado = await _rutaService.CalcularRutaAsync(puntos);

            if (resultado == null)
                return NotFound("No se pudo calcular la ruta.");

            return Ok(resultado);
        }
    }
}
