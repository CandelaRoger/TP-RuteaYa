using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using RuteaYa.Logica.Rutas;
using Xunit;

public class EnvioServiceTests
{
    [Fact]
    public async Task CrearEnvioAsync_DeberiaFalla_CuandoOrigenYDestinoSonIguales()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new RuteaYaDbContext(options);
        context.Usuarios.Add(new Usuario { Id = 1, Nombre = "Remitente", Email = "remitente@test.com" });
        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new EnvioService(context, mockRutaService.Object);

        var dto = new CrearEnvioDto
        {
            Origen = "Palermo",
            Destino = "Palermo", 
            Fecha = DateTime.Today.AddDays(1),
            Peso = 2.5m,
            Tamanio = "Pequeño",
            Descripcion = "Zapatillas",
            MontoOfrecido = 4000m
        };

        // 2. Act
        var resultado = await service.CrearEnvioAsync(dto, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El origen y el destino deben ser diferentes.", resultado.MensajeError);
    }

    [Fact]
    public async Task CrearEnvioAsync_DeberiaCrearEnvioExitosamente_CuandoDatosYGeocodificacionSonValidos()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new RuteaYaDbContext(options);
        context.Usuarios.Add(new Usuario { Id = 1, Nombre = "Remitente", Email = "remitente@test.com" });
        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        
        mockRutaService.Setup(r => r.GeocodificarAsync("Plaza Italia"))
                       .ReturnsAsync(new Coordenadas { Latitud = -34.58, Longitud = -58.42 });
        mockRutaService.Setup(r => r.GeocodificarAsync("Obelisco"))
                       .ReturnsAsync(new Coordenadas { Latitud = -34.60, Longitud = -58.38 });

        var service = new EnvioService(context, mockRutaService.Object);

        var dto = new CrearEnvioDto
        {
            Origen = "Plaza Italia",
            Destino = "Obelisco",
            Fecha = DateTime.Today.AddDays(2),
            Peso = 3.0m,
            Tamanio = "Mediano",
            Descripcion = "Caja de libros",
            MontoOfrecido = 5000m
        };

        // 2. Act
        var resultado = await service.CrearEnvioAsync(dto, 1);

        // 3. Assert
        Assert.True(resultado.Exitoso);
        Assert.NotNull(resultado.Datos);
        Assert.Equal("Plaza Italia", resultado.Datos.OrigenDir);
        Assert.Equal("Obelisco", resultado.Datos.DestinoDir);
        Assert.Equal("Buscando conductor", resultado.Datos.Estado);
        Assert.Equal(5000m, resultado.Datos.MontoOfrecido);
    }

    [Fact]
    public async Task CancelarEnvioAsync_DeberiaFalla_CuandoElEnvioYaEstaRetirado()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new RuteaYaDbContext(options);
        context.Envios.Add(new Envio
        {
            Id = 1,
            UsuarioId = 1,
            OrigenDir = "A",
            DestinoDir = "B",
            Estado = "Retirado", 
            Fecha = DateTime.Today,
            Peso = 1m,
            Tamanio = "Pequeño",
            Descripcion = "Paquete",
            MontoOfrecido = 1000m
        });
        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new EnvioService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.CancelarEnvioAsync(1, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("No se puede cancelar un paquete que está 'Retirado'", resultado.MensajeError);
    }

    [Fact]
    public async Task CancelarEnvioAsync_DeberiaExito_CuandoElEnvioEstaBuscandoConductor()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new RuteaYaDbContext(options);
        context.Envios.Add(new Envio
        {
            Id = 1,
            UsuarioId = 1,
            OrigenDir = "A",
            DestinoDir = "B",
            Estado = "Buscando conductor",
            Fecha = DateTime.Today,
            Peso = 1m,
            Tamanio = "Pequeño",
            Descripcion = "Paquete",
            MontoOfrecido = 1000m
        });
        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new EnvioService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.CancelarEnvioAsync(1, 1);

        // 3. Assert
        Assert.True(resultado.Exitoso);

        // Verificamos que el estado en la base de datos haya cambiado a Cancelado
        var envioActualizado = await context.Envios.FindAsync(1);
        Assert.Equal("Cancelado", envioActualizado?.Estado);
    }
}