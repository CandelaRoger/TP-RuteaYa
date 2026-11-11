using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RuteaYa.MVC.Controllers;

[Authorize]
public class OportunidadesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OportunidadesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(int viajeId)
    {
        var client = _httpClientFactory.CreateClient("RuteaYaApi");

        // Recuperamos el JWT del usuario autenticado.
        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Auth");
        }

        // Enviamos el token a nuestra WebApi.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Consultamos las oportunidades del viaje seleccionado.
            var oportunidades = await client.GetFromJsonAsync<List<OportunidadDto>>(
                $"api/oportunidades/viaje/{viajeId}");

            ViewBag.ViajeId = viajeId;

            return View(oportunidades ?? new List<OportunidadDto>());
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudieron obtener las oportunidades.";

            return View(new List<OportunidadDto>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aceptar(int viajeId, int envioId)
    {
        var client = _httpClientFactory.CreateClient("RuteaYaApi");

        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var respuesta = await client.PostAsync(
                $"api/oportunidades/viaje/{viajeId}/aceptar/{envioId}",
                null);

            if (respuesta.IsSuccessStatusCode)
            {
                TempData["Exito"] = "Envío aceptado correctamente.";
            }
            else
            {
                var error = await respuesta.Content
                    .ReadFromJsonAsync<MensajeApi>();

                TempData["Error"] = error?.Mensaje
                    ?? "No se pudo aceptar el envío.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo conectar con la API.";
        }
        catch (System.Text.Json.JsonException)
        {
            TempData["Error"] = "La API devolvió una respuesta inesperada.";
        }

        return RedirectToAction(nameof(Index), new { viajeId });
    }

    [HttpGet]
    public async Task<IActionResult> Asignados(int viajeId)
    {
        var client = _httpClientFactory.CreateClient("RuteaYaApi");

        // Recuperar el JWT del usuario autenticado
        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        ViewBag.ViajeId = viajeId;

        try
        {
            // Consultar los paquetes asignados a este viaje
            var asignados = await client
                .GetFromJsonAsync<List<OportunidadDto>>(
                    $"api/oportunidades/viaje/{viajeId}/asignados");

            return View(asignados ?? new List<OportunidadDto>());
        }
        catch (HttpRequestException)
        {
            TempData["Error"] =
                "No se pudieron obtener los envíos asignados.";

            return View(new List<OportunidadDto>());
        }
    }


    private class MensajeApi
    {
        public string? Mensaje { get; set; }
    }

}