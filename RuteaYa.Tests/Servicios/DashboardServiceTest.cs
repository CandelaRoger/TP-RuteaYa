
using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using Xunit;

public class DashboardServiceTest
{
    private class GananciasMensualesServiceFake : IGananciasMensualesService
    {
        public Task<ResumenGananciasMensualDto> ObtenerResumenAsync(int usuarioId, string criterio = "ganancia")
        {
            return Task.FromResult(new ResumenGananciasMensualDto
            {
                GananciasPotenciales = 84500m
            });
        }
    }

    [Fact]
    public async Task GetDashboardDataAsync_DeberiaRetornarDtoConNombreYContadores_CuandoUsuarioExiste()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new RuteaYaDbContext(options))
        {
            context.Usuarios.Add(new Usuario
            {
                Id = 1,
                Nombre = "UsuarioTest"
            });

            await context.SaveChangesAsync();
        }

        using (var context = new RuteaYaDbContext(options))
        {
            var gananciasFake = new GananciasMensualesServiceFake();
            var service = new DashboardService(context, gananciasFake);

            var resultado = await service.GetDashboardDataAsync(1);

            Assert.NotNull(resultado);
            Assert.Equal("UsuarioTest", resultado.NombreUsuario);
            Assert.Equal(0, resultado.CantProximosViajes);
            Assert.Equal(0, resultado.CantEnviosPendientes);
            Assert.Equal(84500m, resultado.PotencialDelMes);
        }
    }

    [Fact]
    public async Task GetDashboardDataAsync_DeberiaRetornarUsuarioGenerico_CuandoUsuarioNoExiste()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new RuteaYaDbContext(options))
        {
            var gananciasFake = new GananciasMensualesServiceFake();
            var service = new DashboardService(context, gananciasFake);

            var resultado = await service.GetDashboardDataAsync(99);

            Assert.NotNull(resultado);
            Assert.Equal("Usuario", resultado.NombreUsuario);
        }
    }
}
