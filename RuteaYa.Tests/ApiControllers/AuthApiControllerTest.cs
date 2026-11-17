using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Logica;
using RuteaYa.WebApi.Controllers;
using Xunit;

public class AuthApiControllerTests
{
    [Fact]
    public async Task Login_DeberiaRetornarOk_CuandoCredencialesSonValidas()
    {
        // 1. Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService.Setup(s => s.Login(It.IsAny<LoginDto>()))
                       .ReturnsAsync(new ResultadoLogin { Exitoso = true, Token = "jwt-token-valido" });

        var controller = new AuthApiController(mockAuthService.Object);
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
        mockAuthService.Setup(s => s.Login(It.IsAny<LoginDto>()))
                       .ReturnsAsync(new ResultadoLogin { Exitoso = false, MensajeError = "Credenciales inválidas." });

        var controller = new AuthApiController(mockAuthService.Object);
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
        mockAuthService.Setup(s => s.RegistrarUsuarioAsync(It.IsAny<RegistroDto>()))
                       .ReturnsAsync(new Resultado { Exitoso = true });

        var controller = new AuthApiController(mockAuthService.Object);
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
        mockAuthService.Setup(s => s.RegistrarUsuarioAsync(It.IsAny<RegistroDto>()))
                       .ReturnsAsync(new Resultado { Exitoso = false, MensajeError = "El email ya se encuentra registrado." });

        var controller = new AuthApiController(mockAuthService.Object);
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
}