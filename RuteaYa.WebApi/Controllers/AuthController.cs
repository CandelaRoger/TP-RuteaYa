using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica;
using RuteaYa.DTOs;
namespace RuteaYa.WebApi.Controllers
{
  
        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : Controller
        {
            private readonly IAuthService _authService;

            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

            [HttpPost("registro")]
            public async Task<IActionResult> Registrar([FromBody] RegistroDto dto)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState); // Devuelve HTTP 400 Bad Request
                }

                var resultado = await _authService.RegistrarUsuarioAsync(dto);

                if (!resultado.Exitoso)
                {
                    // Devuelve HTTP 409 Conflict si el email ya existe
                    return Conflict(new { mensaje = resultado.MensajeError });
                }

                // Devuelve HTTP 201 Created si la operación fue exitosa
                return StatusCode(201, new { mensaje = "Usuario registrado correctamente" });
            }

         }
}
