using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace RuteaYa.MVC.Controllers;

[Authorize] 
public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public async Task<IActionResult> Index()
    {
        
        // Buscamos el ID tanto por ClaimTypes.NameIdentifier como por "sub" (JWT standard)
        var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(usuarioIdStr) || !int.TryParse(usuarioIdStr, out int usuarioId))
        {
            return RedirectToAction("Login", "Auth");
        }

        DashboardDto? dashboardData = null;
        
        try
        {
            
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var token = await HttpContext.GetTokenAsync("jwt");

            if (!string.IsNullOrEmpty(token))
            {
                // Adjuntamos el token en la cabecera (Bearer) para que la API sepa quién hace la petición
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            }
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            dashboardData = await client.GetFromJsonAsync<DashboardDto>($"api/dashboard/{usuarioId}", options);

        }
        catch (Exception ex)
        {
            
            ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API.");
        }

        

        return View(dashboardData);
    }

 
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
