using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.DTOs;
using RuteaYa.MVC.Models;
using System.Net;

namespace RuteaYa.MVC.Controllers
{
    [Authorize]
    public class TransportesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;

        public TransportesController(IHttpClientFactory httpClientFactory, IMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var response = await client.GetAsync("api/transportesApi/mis-paquetes");

            var viewModel = new MisTransportesViewModel();

            if (response.IsSuccessStatusCode)
            {
                var dtos = await response.Content.ReadFromJsonAsync<List<PaqueteTransporteDto>>()
                           ?? new List<PaqueteTransporteDto>();

                viewModel.Transportes = _mapper.Map<List<TransporteItemViewModel>>(dtos);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Si el token guardado en la cookie expiró en la API
                return RedirectToAction("Login", "Auth");
            }

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int envioId, string nuevoEstado)
        {
            var client = _httpClientFactory.CreateClient("RuteaYaApi");
            var dto = new ActualizarEstadoEnvioDto
            {
                EnvioId = envioId,
                NuevoEstado = nuevoEstado
            };

            var response = await client.PutAsJsonAsync("api/TransportesApi/cambiar-estado", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(errorMsg)
                    ? "No se pudo actualizar el estado del paquete."
                    : errorMsg;
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
