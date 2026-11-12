using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;

namespace RuteaYa.MVC.Controllers
{
    [Authorize]
    public class ViajesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ViajesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filtro = "Activos")
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var response = await client.GetAsync($"api/viajesApi?filtro={filtro}");

            List<ViajeDto> viajes = new();
            if (response.IsSuccessStatusCode)
            {
                viajes = await response.Content.ReadFromJsonAsync<List<ViajeDto>>() ?? new();
            }

            ViewBag.FiltroActual = filtro;
            return View(viajes);
        }


        [HttpGet]
        public async Task<IActionResult> BuscarDirecciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
                return Content("[]", "application/json");

            var client = _httpClientFactory.CreateClient("RuteaYaApi");

            var url = $"api/Rutas/sugerencias?texto={Uri.EscapeDataString(texto)}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var contenido = await response.Content.ReadAsStringAsync();

            return Content(contenido, "application/json");
        }


        [HttpGet]
       
        public IActionResult Crear()
        {
            return View(new CrearViajeViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");

            var response = await client.GetAsync($"api/viajesApi/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var viaje = await response.Content.ReadFromJsonAsync<ViajeDto>();

            if (viaje == null)
                return NotFound();

            ViewBag.HereApiKey = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Here:ApiKey"];

            return View(viaje);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");

            var response = await client.GetAsync($"api/viajesApi/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var viaje = await response.Content.ReadFromJsonAsync<ViajeDto>();

            if (viaje == null)
                return NotFound();

            var model = new CrearViajeViewModel
            {
                Origen = viaje.OrigenDir,
                Destino = viaje.DestinoDir,

                ParadasTexto = string.Join(", ",
                    viaje.Paradas
                        .OrderBy(p => p.Orden)
                        .Select(p => p.Direccion)),

                DiasSeleccionados = viaje.DiasSemana.ToList(),

                HoraSalida = viaje.HoraSalida,
                HoraLlegada = viaje.HoraLlegada,
                MaxPaquetes = viaje.MaxPaquetes,
                PesoMaxKg = viaje.PesoMaxKg,
                DesvioMaxKm = viaje.DesvioMaxKm
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CrearViajeViewModel model)
        {
            if (model.DiasSeleccionados == null || model.DiasSeleccionados.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar al menos un día de la semana.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var dto = new CrearViajeDto
            {
                Origen = model.Origen,
                Destino = model.Destino,

                Paradas = string.IsNullOrWhiteSpace(model.ParadasTexto)
                    ? new List<string>()
                    : model.ParadasTexto.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries).ToList(),

                DiasSemana = model.DiasSeleccionados,
                HoraSalida = model.HoraSalida,
                HoraLlegada = model.HoraLlegada,
                MaxPaquetes = model.MaxPaquetes,
                PesoMaxKg = model.PesoMaxKg,
                DesvioMaxKm = model.DesvioMaxKm
            };

            var client = _httpClientFactory.CreateClient("RuteaYaApi");

            var response = await client.PutAsJsonAsync(
                $"api/viajesApi/{id}", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = "Viaje actualizado correctamente.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tu sesión expiró. Por favor, vuelve a iniciar sesión.");
                return View(model);
            }

            var error = await response.Content.ReadFromJsonAsync<RespuestaErrorApi>();

            ModelState.AddModelError(
                string.Empty,
                error?.Mensaje ?? "No se pudo actualizar el viaje.");

            return View(model);
        } 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearViajeViewModel model)
        {
            if (model.DiasSeleccionados == null || model.DiasSeleccionados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar al menos un día de la semana.");
            }

            if (!ModelState.IsValid)
                return View(model);

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
            var response = await client.PostAsJsonAsync("api/viajesApi", dto);

            if (response.IsSuccessStatusCode)
            {
                TempData["MensajeExito"] = "Nuevo viaje creado con éxito";
                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError(string.Empty, "Tu sesión expiro. Por favor, vuelve a iniciar sesión.");
                return View(model);
            }
            var error = await response.Content.ReadFromJsonAsync<RespuestaErrorApi>();
            ModelState.AddModelError(string.Empty, error?.Mensaje ?? "No se pudo crear el viaje.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/viajesApi/{id}/desactivar");
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