using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica;
using RuteaYa.DTOs;
namespace RuteaYa.WebApi.Controllers
{
  
        [ApiController]
        [Route("api/[controller]")]
        public class AuthApiController : Controller
        {
            private readonly IAuthService _authService;

            public AuthApiController(IAuthService authService)
            {
                _authService = authService;
            }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var resultado = await _authService.Login(dto);
            if (!resultado.Exitoso)
            {
                // Devuelve HTTP 401 Unauthorized si las credenciales son incorrectas
                return Unauthorized(resultado);
            }
            // Devuelve HTTP 200 OK con el token si la operación fue exitosa
            return Ok(resultado);
        }



            [HttpPost("registro")]
            public async Task<IActionResult> Registrar([FromBody] RegistroDto dto)
            {
               
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
