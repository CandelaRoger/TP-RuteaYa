using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using Xunit;

public class GananciasMensualesServiceTest
{
    [Fact]
    public async Task ObtenerResumenAsync_DeberiaCalcularTotalEstimadoCorrectamente_SumandoActualesYPotenciales()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockOportunidad = new Mock<IOportunidadService>();
        mockOportunidad
            .Setup(m => m.ObtenerCandidatosPorFechaAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>()))
            .ReturnsAsync(new List<OportunidadDto>());

        using (var context = new RuteaYaDbContext(options))
        {
            var viaje = new Viaje
            {
                Id = 1,
                UsuarioId = 10,
                OrigenDir = "Origen",
                DestinoDir = "Destino",
                Activo = true,
                MaxPaquetes = 3,
                PesoMaxKg = 10,
                DesvioMaxKm = 5
            };
            context.Viajes.Add(viaje);

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            context.Envios.Add(new Envio
            {
                Id = 1,
                UsuarioId = 20,
                ViajeId = 1,
                Viaje = viaje,
                MontoOfrecido = 5000m,
                Estado = "Entregado",
                Fecha = inicioMes.AddDays(2),
                OrigenDir = "A",
                DestinoDir = "B",
                Descripcion = "Test",
                Tamanio = "Mediano",
                Peso = 2
            });

            await context.SaveChangesAsync();
        }

        using (var context = new RuteaYaDbContext(options))
        {
            var service = new GananciasMensualesService(context, mockOportunidad.Object);

            var resultado = await service.ObtenerResumenAsync(10, "ganancia");

            Assert.NotNull(resultado);
            Assert.Equal(5000m, resultado.GananciasActuales);
            Assert.Equal(0m, resultado.GananciasPotenciales);
            Assert.Equal(5000m, resultado.TotalEstimado);
            Assert.Equal("ganancia", resultado.CriterioSeleccionado);
        }
    }

    [Fact]
    public async Task ObtenerResumenAsync_DeberiaRespetarCriterioSeleccionado()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockOportunidad = new Mock<IOportunidadService>();

        using (var context = new RuteaYaDbContext(options))
        {
            var service = new GananciasMensualesService(context, mockOportunidad.Object);

            var resultado = await service.ObtenerResumenAsync(10, "desvio");

            Assert.NotNull(resultado);
            Assert.Equal("desvio", resultado.CriterioSeleccionado);
        }
    }
}
