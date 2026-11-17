using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.WebApi.Controllers;
using System.Security.Claims;
using Xunit;

public class AuthApiControllerTests
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
    public async Task Login_DeberiaRetornarOk_CuandoCredencialesSonValidas()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockTokenService = new Mock<ITokenService>(); 

        mockAuthService.Setup(s => s.Login(It.IsAny<LoginDto>()))
                       .ReturnsAsync(new ResultadoLogin { Exitoso = true, Token = "jwt-token-valido" });

        var controller = new AuthApiController(mockAuthService.Object, mockTokenService.Object);
        var dto = new LoginDto { Email = "test@test.com", Password = "Password123*" };

        // 2. Act
        var resultado = await controller.Login(dto);

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var resLogin = Assert.IsType<ResultadoLogin>(okResult.Value);
        Assert.True(resLogin.Exitoso);
        Assert.Equal("jwt-token-valido", resLogin.Token);
    }

    [Fact]
    public async Task Login_DeberiaRetornarUnauthorized_CuandoCredencialesSonInvalidas()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockTokenService = new Mock<ITokenService>();

        mockAuthService.Setup(s => s.Login(It.IsAny<LoginDto>()))
                       .ReturnsAsync(new ResultadoLogin { Exitoso = false, MensajeError = "Credenciales inválidas." });

        var controller = new AuthApiController(mockAuthService.Object, mockTokenService.Object);
        var dto = new LoginDto { Email = "test@test.com", Password = "WrongPassword" };

        // 2. Act
        var resultado = await controller.Login(dto);

        // 3. Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public async Task Registrar_DeberiaRetornarCreated_CuandoRegistroEsExitoso()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockTokenService = new Mock<ITokenService>();

        mockAuthService.Setup(s => s.RegistrarUsuarioAsync(It.IsAny<RegistroDto>()))
                       .ReturnsAsync(new Resultado { Exitoso = true });

        var controller = new AuthApiController(mockAuthService.Object, mockTokenService.Object);
        var dto = new RegistroDto
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await controller.Registrar(dto);

        // 3. Assert
        var statusResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(201, statusResult.StatusCode);
    }

    [Fact]
    public async Task Registrar_DeberiaRetornarConflict_CuandoEmailYaExiste()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockTokenService = new Mock<ITokenService>();

        mockAuthService.Setup(s => s.RegistrarUsuarioAsync(It.IsAny<RegistroDto>()))
                       .ReturnsAsync(new Resultado { Exitoso = false, MensajeError = "El email ya se encuentra registrado." });

        var controller = new AuthApiController(mockAuthService.Object, mockTokenService.Object);
        var dto = new RegistroDto
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "duplicado@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await controller.Registrar(dto);

        // 3. Assert
        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task Logout_DeberiaRetornarOk_CuandoTokenEsProporcionado()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockTokenService = new Mock<ITokenService>();

        var controller = new AuthApiController(mockAuthService.Object, mockTokenService.Object);

        ConfigurarUsuarioAutenticado(controller, "1");
        controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = "Bearer fake-jwt-token-123";

        // 2. Act
        var resultado = await controller.Logout();

        // 3. Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado);
        
        mockTokenService.Verify(t => t.RevocarTokenAsyncr("fake-jwt-token-123"), Times.Once);
    }
}