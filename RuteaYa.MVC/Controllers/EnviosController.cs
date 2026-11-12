using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;
using System.Net.Http.Headers;

namespace RuteaYa.MVC.Controllers
{
    [Authorize] 
    public class EnviosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EnviosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filtro = "Todos")
        {
            var client = await CrearClienteConAuthAsync();
            var response = await client.GetAsync($"api/enviosApi?estado={filtro}");

            List<EnvioDto> envios = new();
            if (response.IsSuccessStatusCode)
            {
                envios = await response.Content.ReadFromJsonAsync<List<EnvioDto>>() ?? new();
            }

            ViewBag.FiltroActual = filtro;
            return View(envios);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CrearEnvioViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Crear(CrearEnvioViewModel model)
        {
            if (model.Fecha.Date < DateTime.Today)
            {
                ModelState.AddModelError("Fecha", "La fecha no puede ser anterior a hoy.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var dto = new CrearEnvioDto
            {
                Origen = model.Origen,
                Destino = model.Destino,
                Fecha = model.Fecha,
                Peso = model.Peso,
                Tamanio = model.Tamanio,
                Descripcion = model.Descripcion,
                MontoOfrecido = model.MontoOfrecido
            };

            var client = await CrearClienteConAuthAsync();
            var response = await client.PostAsJsonAsync("api/enviosApi", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = $"Envío '{model.Descripcion}' publicado con éxito.";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadFromJsonAsync<RespuestaErrorApi>();
            ModelState.AddModelError(string.Empty, error?.Mensaje ?? "No se pudo publicar el envío.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            var client = await CrearClienteConAuthAsync();
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/enviosApi/{id}/cancelar");
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = "Envío cancelado con éxito.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<HttpClient> CrearClienteConAuthAsync()
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var token = await HttpContext.GetTokenAsync("jwt");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}