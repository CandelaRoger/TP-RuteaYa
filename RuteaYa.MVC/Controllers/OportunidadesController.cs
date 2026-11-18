using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using System.Net;
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

    public async Task<IActionResult> Index(int viajeId, string orden = "ganancia")
    {
        var client = _httpClientFactory.CreateClient("RuteaYaApi");
        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        ViewBag.ViajeId = viajeId;
        ViewBag.Orden = orden;

        try
        {
            var oportunidades = await client.GetFromJsonAsync<List<OportunidadDto>>(
                $"api/oportunidades/viaje/{viajeId}?orden={Uri.EscapeDataString(orden)}");

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

        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        ViewBag.ViajeId = viajeId;

        try
        {

            var asignados = await client
                .GetFromJsonAsync<List<OportunidadDto>>(
                    $"api/oportunidades/viaje/{viajeId}/asignados");

            var capacidad = await client
                .GetFromJsonAsync<CapacidadViajeDto>(
                    $"api/oportunidades/viaje/{viajeId}/capacidad");

            // Enviar la capacidad a la vista
            ViewBag.Capacidad = capacidad;

            return View(asignados ?? new List<OportunidadDto>());

        }
        catch (HttpRequestException)
        {
            TempData["Error"] =
                "No se pudieron obtener los envíos asignados.";

            return View(new List<OportunidadDto>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int viajeId, int envioId)
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
                $"api/oportunidades/viaje/{viajeId}/quitar/{envioId}",
                null);

            if (respuesta.IsSuccessStatusCode)
            {
                TempData["Exito"] =
                    "El envío fue quitado correctamente.";
            }
            else
            {
                try
                {
                    var error = await respuesta.Content
                        .ReadFromJsonAsync<MensajeApi>();

                    TempData["Error"] = error?.Mensaje
                        ?? "No se pudo quitar el envío.";
                }
                catch (System.Text.Json.JsonException)
                {
                    TempData["Error"] =
                        "No se pudo quitar el envío.";
                }
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] =
                "No se pudo conectar con la API.";
        }

        return RedirectToAction(nameof(Asignados), new { viajeId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmar(int viajeId, List<int> envioIds)
    {
        if (envioIds == null || envioIds.Count == 0)
        {
            TempData["Error"] = "Seleccioná al menos un envío para continuar.";
            return RedirectToAction(nameof(Index), new { viajeId });
        }

        var client = await CrearClienteAutenticadoAsync();
        if (client == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            var respuesta = await client.PostAsJsonAsync(
                $"api/oportunidades/viaje/{viajeId}/seleccion/resumen",
                new SeleccionEnviosDto { EnvioIds = envioIds });

            if (respuesta.IsSuccessStatusCode)
            {
                var resumen = await respuesta.Content.ReadFromJsonAsync<ResumenSeleccionDto>();

                if (resumen != null)
                    return View(resumen);
            }
            else
            {
                TempData["Error"] = await LeerMensajeAsync(
                    respuesta, "No se pudo armar el resumen de la selección.");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int viajeId, List<int> envioIds)
    {
        envioIds ??= new List<int>();

        var client = await CrearClienteAutenticadoAsync();
        if (client == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            var respuesta = await client.PostAsJsonAsync(
                $"api/oportunidades/viaje/{viajeId}/seleccion/confirmar",
                new SeleccionEnviosDto { EnvioIds = envioIds });

            if (respuesta.IsSuccessStatusCode)
            {
                TempData["Exito"] = envioIds.Count == 1
                    ? "Envío reservado correctamente."
                    : $"Reservaste {envioIds.Count} envíos correctamente.";

                return RedirectToAction(nameof(Asignados), new { viajeId });
            }

            TempData["Error"] = await LeerMensajeAsync(
                respuesta, "No se pudo reservar la selección.");

            // Conflicto: se vuelve al resumen, que se recalcula sin los envíos tomados
            if (respuesta.StatusCode == HttpStatusCode.Conflict)
            {
                var url = (Url.Action(nameof(Confirmar), new { viajeId }) ?? "/Oportunidades/Confirmar")
                          + "&" + string.Join("&", envioIds.Select(id => $"envioIds={id}"));

                return LocalRedirect(url);
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo conectar con la API.";
        }

        return RedirectToAction(nameof(Index), new { viajeId });
    }

    private async Task<HttpClient?> CrearClienteAutenticadoAsync()
    {
        var token = await HttpContext.GetTokenAsync("jwt");

        if (string.IsNullOrEmpty(token))
            return null;

        var client = _httpClientFactory.CreateClient("RuteaYaApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<string> LeerMensajeAsync(HttpResponseMessage respuesta, string porDefecto)
    {
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<MensajeApi>();
            return error?.Mensaje ?? porDefecto;
        }
        catch (System.Text.Json.JsonException)
        {
            return porDefecto;
        }
    }

    private class MensajeApi
    {
        public string? Mensaje { get; set; }
    }

}