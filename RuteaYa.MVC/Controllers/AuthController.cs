using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.MVC.Models;
using RuteaYa.DTOs;

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
            return View();
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
            var response = await client.PostAsJsonAsync("api/auth/registro", dto);


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
    }

    public class RespuestaError
    {
        public string? Mensaje { get; set; }
    }
}
