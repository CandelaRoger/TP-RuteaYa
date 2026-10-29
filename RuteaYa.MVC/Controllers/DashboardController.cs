using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;
using System.Diagnostics;

namespace RuteaYa.MVC.Controllers;

public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public async Task<IActionResult> Index()
    {
        int usuarioId = 1; //sesion de pruebs
        DashboardDto? dashboardData = null;

        try
        {
            
            var client = _httpClientFactory.CreateClient("RuteaYaApi");

           
            dashboardData = await client.GetFromJsonAsync<DashboardDto>($"api/dashboard/{usuarioId}");
        }
        catch (Exception ex)
        {
            
            ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API.");
        }

        
        dashboardData ??= new DashboardDto();

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
