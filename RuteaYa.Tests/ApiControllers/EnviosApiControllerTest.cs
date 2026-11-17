using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.WebApi.Controllers;
using System.Security.Claims;
using Xunit;

public class EnviosApiControllerTests
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
    public async Task ObtenerEnvios_DeberiaRetornarOk_CuandoUsuarioEstaAutenticado()
    {
        // 1. Arrange
        var mockService = new Mock<IEnvioService>();
        mockService.Setup(s => s.ObtenerEnviosPorUsuarioAsync(1, "Todos"))
                   .ReturnsAsync(new List<EnvioDto> { new EnvioDto { Id = 1, Descripcion = "Paquete test" } });

        var controller = new EnviosApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1"); // Simulamos que el usuario ID 1 está logueado

        // 2. Act
        var resultado = await controller.ObtenerEnvios("Todos");

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsType<List<EnvioDto>>(okResult.Value);
        Assert.Single(lista);
    }

    [Fact]
    public async Task ObtenerPorId_DeberiaRetornarNotFound_CuandoEnvioNoExiste()
    {
        // 1. Arrange
        var mockService = new Mock<IEnvioService>();
        mockService.Setup(s => s.ObtenerEnvioPorIdAsync(99, 1))
                   .ReturnsAsync((EnvioDto?)null);

        var controller = new EnviosApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.ObtenerPorId(99);

        // 3. Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task Crear_DeberiaRetornarCreated_CuandoDatosSonValidos()
    {
        // 1. Arrange
        var mockService = new Mock<IEnvioService>();
        mockService.Setup(s => s.CrearEnvioAsync(It.IsAny<CrearEnvioDto>(), 1))
                   .ReturnsAsync(ResultadoOperacion<EnvioDto>.Ok(new EnvioDto { Id = 10, Descripcion = "Caja" }));

        var controller = new EnviosApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        var dto = new CrearEnvioDto
        {
            Origen = "Palermo",
            Destino = "Recoleta",
            Fecha = DateTime.Today.AddDays(1),
            Peso = 2m,
            Tamanio = "Mediano",
            Descripcion = "Caja",
            MontoOfrecido = 3000m
        };

        // 2. Act
        var resultado = await controller.Crear(dto);

        // 3. Assert: Verifica que devuelva un CreatedAtActionResult (Código 201)
        var createdResult = Assert.IsType<CreatedAtActionResult>(resultado);
        var envioCreado = Assert.IsType<EnvioDto>(createdResult.Value);
        Assert.Equal(10, envioCreado.Id);
    }

    [Fact]
    public async Task Cancelar_DeberiaRetornarBadRequest_CuandoFallaLaReglaDeNegocio()
    {
        // 1. Arrange: Simulamos que el servicio rechaza la cancelación (ej: paquete ya retirado)
        var mockService = new Mock<IEnvioService>();
        mockService.Setup(s => s.CancelarEnvioAsync(1, 1))
                   .ReturnsAsync(ResultadoOperacion<bool>.Falla("No se puede cancelar un paquete que está 'Retirado'."));

        var controller = new EnviosApiController(mockService.Object);
        ConfigurarUsuarioAutenticado(controller, "1");

        // 2. Act
        var resultado = await controller.Cancelar(1);

        // 3. Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}