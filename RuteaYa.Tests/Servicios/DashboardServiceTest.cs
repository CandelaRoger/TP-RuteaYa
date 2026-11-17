using Microsoft.EntityFrameworkCore;
using RuteaYa.DTOs;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using Xunit;

public class DashboardServiceTest
{
    [Fact]
    public async Task GetDashboardDataAsync_DeberiaRetornarDtoConNombreYContadores_CuandoUsuarioExiste()
    {
        // 1. Arrange 
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        //  datos iniciales en la BD en memoria
        using (var context = new RuteaYaDbContext(options))
        {
            context.Usuarios.Add(new Usuario
            {
                Id = 1,
                Nombre = "UsuarioTest"
            });

            await context.SaveChangesAsync();
        }

        // 2. Act 
        using (var context = new RuteaYaDbContext(options))
        {
            var service = new DashboardService(context);

            
            var resultado = await service.GetDashboardDataAsync(1);

            // 3. Assert 
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
        // Arrange
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new RuteaYaDbContext(options))
        {
            var service = new DashboardService(context);

            // Act
            var resultado = await service.GetDashboardDataAsync(99);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("Usuario", resultado.NombreUsuario);
        }
    }
}