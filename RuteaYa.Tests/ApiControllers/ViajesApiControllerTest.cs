using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.WebApi.Controllers;
using System.Security.Claims;
using Xunit;

public class ViajesApiControllerTests
{
    private static void ConfigurarUsuarioAutenticado(ControllerBase controller, string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuthentication"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task ObtenerViajes_DeberiaRetornarOk_CuandoUsuarioEstaAutenticado()
    {
        // 1. Arrange
        var mockService = new Mock<IViajeService>();
        mockService.Setup(s => s.ObtenerViajesPorUsuarioAsync(1, "Todos"))
                   .ReturnsAsync(new List<ViajeDto> { new ViajeDto { Id = 1, OrigenDir = "Lanús", DestinoDir = "CABA" } });

        var controller = new ViajesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.ObtenerViajes("Todos");

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsType<List<ViajeDto>>(okResult.Value);
        Assert.Single(lista);
    }

    [Fact]
    public async Task ObtenerPorId_DeberiaRetornarNotFound_CuandoViajeNoExiste()
    {
        // 1. Arrange
        var mockService = new Mock<IViajeService>();
        mockService.Setup(s => s.ObtenerViajePorIdAsync(99, 1))
                   .ReturnsAsync((ViajeDto?)null);

        var controller = new ViajesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.ObtenerPorId(99);

        // 3. Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task Crear_DeberiaRetornarOk_CuandoDatosDelViajeSonValidos()
    {
        // 1. Arrange
        var mockService = new Mock<IViajeService>();
        mockService.Setup(s => s.CrearViajeAsync(It.IsAny<CrearViajeDto>(), 1))
                   .ReturnsAsync(ResultadoOperacion<ViajeDto>.Ok(new ViajeDto { Id = 5, OrigenDir = "Quilmes", DestinoDir = "Retiro" }));

        var controller = new ViajesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        var dto = new CrearViajeDto
        {
            Origen = "Quilmes",
            Destino = "Retiro",
            HoraSalida = new TimeSpan(7, 0, 0),
            HoraLlegada = new TimeSpan(8, 30, 0)
        };

        // 2. Act
        var resultado = await controller.Crear(dto);

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var viajeCreado = Assert.IsType<ViajeDto>(okResult.Value);
        Assert.Equal(5, viajeCreado.Id);
    }

    [Fact]
    public async Task CambiarEstado_DeberiaRetornarOk_CuandoSeModificaExitosamente()
    {
        // 1. Arrange
        var mockService = new Mock<IViajeService>();
        mockService.Setup(s => s.CambiarEstadoActivoAsync(1, 1))
                   .ReturnsAsync(ResultadoOperacion<bool>.Ok(false)); // Pasa a inactivo

        var controller = new ViajesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.CambiarEstado(1);

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        Assert.NotNull(okResult.Value);
    }
}