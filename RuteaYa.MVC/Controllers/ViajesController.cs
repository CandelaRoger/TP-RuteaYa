using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;

namespace RuteaYa.MVC.Controllers
{
    public class ViajesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ViajesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Viajes?filtro=Activos
        [HttpGet]
        public async Task<IActionResult> Index(string filtro = "Activos")
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var response = await client.GetAsync($"api/viajes?filtro={filtro}");

            List<ViajeDto> viajes = new();
            if (response.IsSuccessStatusCode)
            {
                viajes = await response.Content.ReadFromJsonAsync<List<ViajeDto>>() ?? new();
            }

            ViewBag.FiltroActual = filtro;
            return View(viajes);
        }

        // GET: /Viajes/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CrearViajeViewModel());
        }

        // POST: /Viajes/Crear
        [HttpPost]
        public async Task<IActionResult> Crear(CrearViajeViewModel model)
        {
            if (model.DiasSeleccionados == null || model.DiasSeleccionados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar al menos un día de la semana.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Mapeo manual del ViewModel al DTO que viaja a la API
            var dto = new CrearViajeDto
            {
                Origen = model.Origen,
                Destino = model.Destino,
                Paradas = string.IsNullOrWhiteSpace(model.ParadasTexto)
                    ? new List<string>()
                    : model.ParadasTexto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                DiasSemana = model.DiasSeleccionados ?? new List<byte>(),
                HoraSalida = model.HoraSalida,
                HoraLlegada = model.HoraLlegada,
                MaxPaquetes = model.MaxPaquetes,
                PesoMaxKg = model.PesoMaxKg,
                DesvioMaxKm = model.DesvioMaxKm
            };

            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var response = await client.PostAsJsonAsync("api/viajes", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = $"Viaje {model.Origen} → {model.Destino} creado con éxito.";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadFromJsonAsync<RespuestaErrorApi>();
            ModelState.AddModelError(string.Empty, error?.Mensaje ?? "No se pudo crear el viaje.");
            return View(model);
        }

        // POST: /Viajes/Desactivar/5
        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/viajes/{id}/desactivar");
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = "Estado del viaje actualizado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class RespuestaErrorApi
    {
        public string? Mensaje { get; set; }
    }
}