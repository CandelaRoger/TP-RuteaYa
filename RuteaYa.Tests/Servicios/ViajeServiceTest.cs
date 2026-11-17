using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using RuteaYa.Logica.Rutas;
using Xunit;

public class ViajeServiceTests
{
    [Fact]
    public async Task CrearViajeAsync_DeberiaFalla_CuandoUsuarioNoExiste()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockRutaService = new Mock<IRutaService>();
        using var context = new RuteaYaDbContext(options);
        var service = new ViajeService(context, mockRutaService.Object);

        var dto = new CrearViajeDto
        {
            Origen = "Belgrano, CABA",
            Destino = "Lomas de Zamora",
            HoraSalida = new TimeSpan(8, 0, 0),
            HoraLlegada = new TimeSpan(9, 0, 0)
        };

        // 2. Act
        var resultado = await service.CrearViajeAsync(dto, 99);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El usuario con ID 99 no existe.", resultado.MensajeError);
    }

    [Fact]
    public async Task CrearViajeAsync_DeberiaFalla_CuandoOrigenYDestinoSonIguales()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockRutaService = new Mock<IRutaService>();

        //se crea un usuario válido en la base de datos en memoria
        using var contextTest = new RuteaYaDbContext(options);
        contextTest.Usuarios.Add(new Usuario { Id = 1, Nombre = "Test", Email = "test@test.com" });
        await contextTest.SaveChangesAsync();

        var service = new ViajeService(contextTest, mockRutaService.Object);

        var dto = new CrearViajeDto
        {
            Origen = "Lanús",
            Destino = "Lanús", 
            HoraSalida = new TimeSpan(8, 0, 0),
            HoraLlegada = new TimeSpan(9, 0, 0)
        };

        // 2. Act
        var resultado = await service.CrearViajeAsync(dto, 1);

        // 3. Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal("El origen y el destino deben ser diferentes.", resultado.MensajeError);
    }
    [Fact]
    public async Task CrearViajeAsync_DeberiaCrearViajeExitosamente_CuandoDatosYMapaSonValidos()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new RuteaYaDbContext(options);
        context.Usuarios.Add(new Usuario { Id = 1, Nombre = "Conductor", Email = "conductor@test.com" });
        await context.SaveChangesAsync();

        var mockRutaService = new Mock<IRutaService>();

        // se simula la geocodificación del origen y destino
        mockRutaService.Setup(r => r.GeocodificarAsync("Belgrano, CABA"))
                       .ReturnsAsync(new Coordenadas { Latitud = -34.56, Longitud = -58.45 });
        mockRutaService.Setup(r => r.GeocodificarAsync("Lomas de Zamora"))
                       .ReturnsAsync(new Coordenadas { Latitud = -34.75, Longitud = -58.40 });

        // se simula el cálculo de la ruta
        mockRutaService.Setup(r => r.CalcularRutaAsync(It.IsAny<List<Coordenadas>>()))
                       .ReturnsAsync(new ResultadoRuta
                       {
                           DistanciaKm = 15.5,
                           DuracionMinutos = 45,
                           PuntosRuta = new List<Coordenadas>() 
                       });

        var service = new ViajeService(context, mockRutaService.Object);

        var dto = new CrearViajeDto
        {
            Origen = "Belgrano, CABA",
            Destino = "Lomas de Zamora",
            HoraSalida = new TimeSpan(8, 0, 0),
            HoraLlegada = new TimeSpan(9, 0, 0),
            MaxPaquetes = 3,
            PesoMaxKg = 10,
            DesvioMaxKm = 3,
            DiasSemana = new List<byte> { 1, 3, 5 } 
        };

        // 2. Act
        var resultado = await service.CrearViajeAsync(dto, 1);

        // 3. Assert
        Assert.True(resultado.Exitoso);
        Assert.NotNull(resultado.Datos);
        Assert.Equal("Belgrano, CABA", resultado.Datos.OrigenDir);
        Assert.Equal("Lomas de Zamora", resultado.Datos.DestinoDir);
        Assert.Equal(15.5m, resultado.Datos.DistanciaKm);
        Assert.True(resultado.Datos.Activo);
    }

    [Fact]
    public async Task CambiarEstadoActivoAsync_DeberiaAlternarEstadoDelViaje()
    {
        // 1. Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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
        var service = new ViajeService(context, mockRutaService.Object);

        // 2. Act
        var resultado = await service.CambiarEstadoActivoAsync(1, 1);

        // 3. Assert
        Assert.True(resultado.Exitoso);
        Assert.False(resultado.Datos); 
    }
}