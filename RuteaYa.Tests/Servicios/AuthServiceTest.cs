using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using Xunit;

public class AuthServiceTests
{
    [Fact]
    public async Task RegistrarUsuarioAsync_DeberiaRegistrarExitosamente_CuandoEmailNoExiste()
    {
        // 1. Arrange
        // Base de datos en memoria y Mock de ITokenService
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTokenService = new Mock<ITokenService>();

        using var context = new RuteaYaDbContext(options);
        var authService = new AuthService(context, mockTokenService.Object);

        var dto = new RegistroDto
        {
            Nombre = "Ana",
            Apellido = "García",
            Email = "ana@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await authService.RegistrarUsuarioAsync(dto);

        // 3. Assert
        Assert.True(resultado.Exitoso);
        Assert.Null(resultado.MensajeError);

        var usuarioEnDb = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == "ana@test.com");
        Assert.NotNull(usuarioEnDb);
        Assert.Equal("Ana", usuarioEnDb.Nombre);
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_DeberiaRetornarFallo_QuandoEmailYaExiste()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTokenService = new Mock<ITokenService>();

        using (var context = new RuteaYaDbContext(options))
        {
            
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Existente",
                Apellido = "Usuario",
                Email = "duplicado@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123*")
            });
            await context.SaveChangesAsync();
        }

        using var contextTest = new RuteaYaDbContext(options);
        var authService = new AuthService(contextTest, mockTokenService.Object);

        var dto = new RegistroDto
        {
            Nombre = "Otro",
            Apellido = "Usuario",
            Email = "duplicado@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await authService.RegistrarUsuarioAsync(dto);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El email ya se encuentra registrado.", resultado.MensajeError);
    }

    [Fact]
    public async Task Login_DeberiaRetornarToken_CuandoCredencialesSonValidas()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTokenService = new Mock<ITokenService>();
        //  TokenService devuelve un token falso pero válido para la prueba
        mockTokenService.Setup(t => t.GenerateToken(It.IsAny<Usuario>()))
                        .Returns("fake-jwt-token-123");

        using (var context = new RuteaYaDbContext(options))
        {
            context.Usuarios.Add(new Usuario
            {
                Id = 1,
                Nombre = "Carlos",
                Apellido = "Ruiz",
                Email = "carlos@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123*")
            });
            await context.SaveChangesAsync();
        }

        using var contextTest = new RuteaYaDbContext(options);
        var authService = new AuthService(contextTest, mockTokenService.Object);

        var dto = new LoginDto
        {
            Email = "carlos@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await authService.Login(dto);

        // 3. Assert
        Assert.True(resultado.Exitoso);
        Assert.Equal("fake-jwt-token-123", resultado.Token);
    }

    [Fact]
    public async Task Login_DeberiaRetornarFallo_CuandoEmailNoExiste()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTokenService = new Mock<ITokenService>();

        using var context = new RuteaYaDbContext(options);
        var authService = new AuthService(context, mockTokenService.Object);

        var dto = new LoginDto
        {
            Email = "noexiste@test.com",
            Password = "Password123*"
        };

        // 2. Act
        var resultado = await authService.Login(dto);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("Credenciales inválidas.", resultado.MensajeError);
        Assert.Null(resultado.Token);
    }

    [Fact]
    public async Task Login_DeberiaRetornarFallo_CuandoPasswordEsIncorrecta()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTokenService = new Mock<ITokenService>();

        using (var context = new RuteaYaDbContext(options))
        {
            context.Usuarios.Add(new Usuario
            {
                Id = 1,
                Nombre = "Carlos",
                Apellido = "Ruiz",
                Email = "carlos@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123*")
            });
            await context.SaveChangesAsync();
        }

        using var contextTest = new RuteaYaDbContext(options);
        var authService = new AuthService(contextTest, mockTokenService.Object);

        var dto = new LoginDto
        {
            Email = "carlos@test.com",
            Password = "WrongPassword*"
        };

        // 2. Act
        var resultado = await authService.Login(dto);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("Credenciales inválidas.", resultado.MensajeError);
        Assert.Null(resultado.Token);
    }
}