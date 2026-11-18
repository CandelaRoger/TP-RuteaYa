
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RuteaYa.MVC.Controllers
{
    [Authorize]
    public class GananciasMensualesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GananciasMensualesController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string criterio = "ganancia")
        {
            var criterioNormalizado = string.IsNullOrWhiteSpace(criterio) ? "ganancia" : criterio.Trim().ToLowerInvariant();
            var client = _httpClientFactory.CreateClient("RuteaYaApi");

            var token = await HttpContext.GetTokenAsync("jwt");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var viajes = await client.GetFromJsonAsync<List<ViajeDto>>(
            "api/viajesApi?filtro=Activos");

            ViewBag.ViajeId = viajes?.FirstOrDefault()?.Id;

            try
            {
                var resumen = await client.GetFromJsonAsync<ResumenGananciasMensualDto>(
                $"api/ganancias-mensuales?criterio={Uri.EscapeDataString(criterioNormalizado)}");

                if (resumen != null)
                {
                    resumen.CriterioSeleccionado = criterioNormalizado;
                }

                return View(resumen ?? new ResumenGananciasMensualDto { CriterioSeleccionado = criterioNormalizado });
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "No se pudo obtener el resumen de ganancias.";

                return View(new ResumenGananciasMensualDto { CriterioSeleccionado = criterioNormalizado });
            }
            catch (System.Text.Json.JsonException)
            {
                TempData["Error"] =
                    "La API devolvió una respuesta inesperada.";

                return View(new ResumenGananciasMensualDto { CriterioSeleccionado = criterioNormalizado });
            }
        }
    }
}
