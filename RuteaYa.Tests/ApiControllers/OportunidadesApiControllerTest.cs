using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.WebApi.Controllers;
using System.Security.Claims;
using Xunit;

public class OportunidadesApiControllerTests
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
    public async Task ObtenerPorViaje_DeberiaRetornarOk_CuandoCandidatosSonListados()
    {
        // 1. Arrange
        var mockService = new Mock<IOportunidadService>();
        mockService.Setup(s => s.ObtenerCandidatosAsync(1, 1, "ganancia"))
                   .ReturnsAsync(new List<OportunidadDto> { new OportunidadDto { EnvioId = 10, Descripcion = "Paquete test" } });

        var controller = new OportunidadesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.ObtenerPorViaje(1, "ganancia");

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsType<List<OportunidadDto>>(okResult.Value);
        Assert.Single(lista);
    }

    [Fact]
    public async Task ObtenerPorViaje_DeberiaRetornarNotFound_CuandoViajeNoExiste()
    {
        // 1. Arrange
        var mockService = new Mock<IOportunidadService>();
        mockService.Setup(s => s.ObtenerCandidatosAsync(99, 1, "ganancia"))
                   .Throws(new KeyNotFoundException("El viaje no existe, está inactivo o no te pertenece."));

        var controller = new OportunidadesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.ObtenerPorViaje(99, "ganancia");

        // 3. Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task AceptarEnvio_DeberiaRetornarOk_CuandoSeAceptaExitosamente()
    {
        // 1. Arrange
        var mockService = new Mock<IOportunidadService>();
        mockService.Setup(s => s.AceptarEnvioAsync(1, 5, 1))
                   .ReturnsAsync(ResultadoOperacion<bool>.Ok(true));

        var controller = new OportunidadesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.AceptarEnvio(1, 5);

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task QuitarEnvio_DeberiaRetornarBadRequest_CuandoFallaLaQuita()
    {
        // 1. Arrange
        var mockService = new Mock<IOportunidadService>();
        mockService.Setup(s => s.QuitarEnvioAsync(1, 5, 1))
                   .ReturnsAsync(ResultadoOperacion<bool>.Falla("El envío no está asignado a este viaje."));

        var controller = new OportunidadesApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.QuitarEnvio(1, 5);

        // 3. Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}