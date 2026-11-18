
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;

namespace RuteaYa.MVC.Controllers
{
    [Authorize]
    public class HistorialController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HistorialController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? estado = "Todos")
        {
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.Estado = estado ?? "Todos";

            if (fechaInicio.HasValue && fechaFin.HasValue &&
                fechaInicio.Value.Date > fechaFin.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La fecha inicial no puede ser posterior a la fecha final.");

                return View(new HistorialViewModel());
            }

            var historialData = new HistorialViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("RuteaYaApi");
                var token = await HttpContext.GetTokenAsync("jwt");

                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Auth");
                }

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var parametros = new List<string>();

                if (fechaInicio.HasValue)
                {
                    parametros.Add(
                        $"fechaInicio={fechaInicio.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                }

                if (fechaFin.HasValue)
                {
                    parametros.Add(
                        $"fechaFin={fechaFin.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    parametros.Add($"estado={Uri.EscapeDataString(estado)}");
                }

                var url = "api/historial";

                if (parametros.Any())
                {
                    url += "?" + string.Join("&", parametros);
                }

                var response = await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.IsSuccessStatusCode)
                {
                    historialData =
                        await response.Content.ReadFromJsonAsync<HistorialViewModel>()
                        ?? new HistorialViewModel();
                }
                else
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo obtener el historial.");
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo conectar con el servidor de la API.");
            }

            return View(historialData);
        }
    }
}
