using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using RuteaYa.Logica.Rutas;
using Xunit;

public class OportunidadServiceTests
{
    [Fact]
    public async Task ObtenerCandidatosAsync_DeberiaLanzarKeyNotFound_CuandoViajeNoExisteOInactivo()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
     .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
     .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
     .Options;

        using var context = new RuteaYaDbContext(options);
        var mockRutaService = new Mock<IRutaService>();
        var service = new OportunidadService(context, mockRutaService.Object);

        // 2. Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await service.ObtenerCandidatosAsync(99, 1);
        });
    }

    [Fact]
    public async Task AceptarEnvioAsync_DeberiaFalla_CuandoEnvioNoEstaDisponible()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
    .Options;

        using var context = new RuteaYaDbContext(options);

        // Creamos un viaje activo
        context.Viajes.Add(new Viaje
        {
            Id = 1,
            UsuarioId = 1,
            OrigenDir = "A",
            DestinoDir = "B",
            MaxPaquetes = 3,
            PesoMaxKg = 10m,
            DesvioMaxKm = 5m,
            DistanciaKm = 10m,
            DuracionMin = 20,
            Activo = true
        });

        // Creamos un envío que ya fue asignado o cancelado (no disponible)
        context.Envios.Add(new Envio
        {
            Id = 1,
            UsuarioId = 2,
            OrigenDir = "C",
            DestinoDir = "D",
            Estado = "Conductor asignado", 
            Fecha = DateTime.Today,
            Peso = 2m,
            MontoOfrecido = 3000m
        });

        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new OportunidadService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.AceptarEnvioAsync(1, 1, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El envío ya no está disponible.", resultado.MensajeError);
    }

    [Fact]
    public async Task AceptarEnvioAsync_DeberiaFalla_CuandoElUsuarioEsElMismoDueñoDelEnvio()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
    .Options;

        using var context = new RuteaYaDbContext(options);

        context.Viajes.Add(new Viaje
        {
            Id = 1,
            UsuarioId = 1, 
            OrigenDir = "A",
            DestinoDir = "B",
            MaxPaquetes = 3,
            PesoMaxKg = 10m,
            DesvioMaxKm = 5m,
            DistanciaKm = 10m,
            DuracionMin = 20,
            Activo = true
        });

        context.Envios.Add(new Envio
        {
            Id = 1,
            UsuarioId = 1, 
            OrigenDir = "C",
            DestinoDir = "D",
            Estado = "Buscando conductor",
            Fecha = DateTime.Today,
            Peso = 2m,
            MontoOfrecido = 3000m
        });

        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new OportunidadService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.AceptarEnvioAsync(1, 1, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("No podés aceptar un envío creado por vos mismo.", resultado.MensajeError);
    }

    [Fact]
    public async Task QuitarEnvioAsync_DeberiaFalla_CuandoEnvioNoEstaAsignadoAlViaje()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
    .Options;

        using var context = new RuteaYaDbContext(options);

        context.Viajes.Add(new Viaje
        {
            Id = 1,
            UsuarioId = 1,
            OrigenDir = "A",
            DestinoDir = "B",
            Activo = true
        });

        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();
        var service = new OportunidadService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.QuitarEnvioAsync(1, 99, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El envío no está asignado a este viaje.", resultado.MensajeError);
    }
}