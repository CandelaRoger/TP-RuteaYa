using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.MVC.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace RuteaYa.MVC.Controllers
{
    public class AuthController : Controller
    {

        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;


        public AuthController(IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }


        [HttpGet]
        public IActionResult Registro()
        {
            return View("registro");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View("login");
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await EnviarLoginAsync(model);
            if (response.IsSuccessStatusCode)
                return await CompletarLoginAsync(response, model);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return await MostrarErrorDeCredencialesAsync(response, model);

            ModelState.AddModelError(string.Empty, "Ocurrió un error al Iniciar sesión.");
            return View(model);
        }

            [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            //  Mapeo con AutoMapper de ViewModel a DTO
            var dto = _mapper.Map<RegistroDto>(model);

            // Llamada a la Web API con HttpClient
            // Se utiliza IHttpClientFactory para crear un cliente HTTP y un puerto fijo para la API
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var response = await client.PostAsJsonAsync("api/authApi/registro", dto);


            if (response.IsSuccessStatusCode)
            {
                // Redirigir al Login
                return RedirectToAction("Login");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadFromJsonAsync<RespuestaError>();
                ModelState.AddModelError("Email", error?.Mensaje ?? "El email se encuentra registrado.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Ocurrió un error al procesar el registro.");
            return View(model);
        }
        private async Task<HttpResponseMessage> EnviarLoginAsync(LoginViewModel model)
        {
            var loginDto = _mapper.Map<LoginDto>(model);
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            return await client.PostAsJsonAsync("api/authApi/login", loginDto);
        }

        private async Task<IActionResult> CompletarLoginAsync(HttpResponseMessage response, LoginViewModel model)
        {
            var result = await response.Content.ReadFromJsonAsync<ResultadoLogin>();
            var token = result?.Token;

            if (string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(string.Empty, "Error al procesar la respuesta del servidor.");
                return View(model);
            }

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var identity = new ClaimsIdentity(
                jwtToken.Claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var properties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "jwt", Value = token } });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);

            return RedirectToAction("Index", "Dashboard");
        }

        private async Task<IActionResult> MostrarErrorDeCredencialesAsync(
            HttpResponseMessage response,
            LoginViewModel model)
        {
            var error = await response.Content.ReadFromJsonAsync<ResultadoLogin>();
            ModelState.AddModelError("Password", error?.MensajeError ?? "Credenciales inválidas.");
            return View(model);
        }

    }

    public class RespuestaError
    {
        public string? Mensaje { get; set; }
    }
}
