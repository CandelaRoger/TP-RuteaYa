using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using System.Security.Claims;

namespace RuteaYa.MVC.Controllers;

[Authorize]
public class HistorialController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HistorialController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, string? estado = "Todos")
    {
        var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(usuarioIdStr) || !int.TryParse(usuarioIdStr, out int usuarioId))
        {
            return RedirectToAction("Login", "Auth");
        }

        HistorialViewModel? historialData = null;

        try
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var token = await HttpContext.GetTokenAsync("jwt");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Llamada a la API construida en el paso 1
            historialData = await client.GetFromJsonAsync<HistorialViewModel>(
                $"api/historial/{usuarioId}?estado={estado}", options);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API para obtener el historial.");
        }

        return View(historialData ?? new HistorialViewModel());
    }
}