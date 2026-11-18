using Microsoft.EntityFrameworkCore;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Logica;
using RuteaYa.Logica.Rutas;

public class OportunidadServiceSeleccionTests
{
    private static RuteaYaDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new RuteaYaDbContext(options);
    }

    private static OportunidadService CrearServicio(RuteaYaDbContext context)
    {
        // El mapa siempre devuelve 11 km: con 10 km habituales, cada envío desvía 1 km
        var mockRuta = new Mock<IRutaService>();
        mockRuta.Setup(r => r.CalcularRutaAsync(It.IsAny<List<Coordenadas>>()))
                .ReturnsAsync(new ResultadoRuta { DistanciaKm = 11, DuracionMinutos = 25 });

        return new OportunidadService(context, mockRuta.Object);
    }

    private static DateTime FechaDelViaje => DateTime.Today.AddDays(1);

    private static byte DiaDelViaje(DateTime f) =>
        (byte)(f.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)f.DayOfWeek);

    private static async Task SembrarViajeAsync(
        RuteaYaDbContext context, int maxPaquetes = 3, decimal pesoMax = 10m)
    {
        context.Viajes.Add(new Viaje
        {
            Id = 1,
            UsuarioId = 1,
            OrigenDir = "A",
            DestinoDir = "B",
            MaxPaquetes = maxPaquetes,
            PesoMaxKg = pesoMax,
            DesvioMaxKm = 5m,
            DistanciaKm = 10m,
            DuracionMin = 20,
            Activo = true,
            DiasSemana = new List<ViajeDia> { new() { DiaSemana = DiaDelViaje(FechaDelViaje) } }
        });

        await context.SaveChangesAsync();
    }

    private static Envio NuevoEnvio(int id, decimal peso = 2m, string estado = "Buscando conductor") => new()
    {
        Id = id,
        UsuarioId = 2,
        OrigenDir = $"O{id}",
        DestinoDir = $"D{id}",
        Estado = estado,
        Fecha = FechaDelViaje,
        Peso = peso,
        MontoOfrecido = 3000m,
        Tamanio = "Pequeño",
        Descripcion = "Paquete"
    };

    [Fact]
    public async Task Previsualizar_DeberiaInformarConflicto_CuandoUnEnvioYaFueTomado()
    {
        using var context = CrearContexto();
        await SembrarViajeAsync(context);

        context.Envios.Add(NuevoEnvio(1));
        var tomado = NuevoEnvio(2, estado: "Conductor asignado");
        tomado.ViajeId = 99; // lo tomó otro conductor
        context.Envios.Add(tomado);
        await context.SaveChangesAsync();

        var resultado = await CrearServicio(context)
            .PrevisualizarSeleccionAsync(1, new List<int> { 1, 2 }, 1);

        Assert.True(resultado.Exitoso);
        Assert.Single(resultado.Datos!.Envios);
        Assert.Single(resultado.Datos.Conflictos);
        Assert.False(resultado.Datos.PuedeConfirmar);
        Assert.Equal(3000m, resultado.Datos.GananciaTotal);
        Assert.Equal(1, resultado.Datos.CantidadPaquetes);
    }

    [Fact]
    public async Task Confirmar_DeberiaDevolverConflicto_YNoReservarNada_CuandoUnEnvioYaFueTomado()
    {
        using var context = CrearContexto();
        await SembrarViajeAsync(context);

        context.Envios.Add(NuevoEnvio(1));
        var tomado = NuevoEnvio(2, estado: "Conductor asignado");
        tomado.ViajeId = 99;
        context.Envios.Add(tomado);
        await context.SaveChangesAsync();

        var resultado = await CrearServicio(context)
            .ConfirmarSeleccionAsync(1, new List<int> { 1, 2 }, 1);

        Assert.False(resultado.Exitoso);
        Assert.True(resultado.EsConflicto);

        var envio1 = await context.Envios.FindAsync(1);
        Assert.Equal("Buscando conductor", envio1!.Estado);
        Assert.Null(envio1.ViajeId);
        Assert.Empty(context.Paradas);
    }

    [Fact]
    public async Task Confirmar_DeberiaAsignarTodos_YCrearLasParadas()
    {
        using var context = CrearContexto();
        await SembrarViajeAsync(context);

        context.Envios.Add(NuevoEnvio(1, peso: 2m));
        context.Envios.Add(NuevoEnvio(2, peso: 3m));
        await context.SaveChangesAsync();

        var resultado = await CrearServicio(context)
            .ConfirmarSeleccionAsync(1, new List<int> { 1, 2 }, 1);

        Assert.True(resultado.Exitoso);

        var envios = context.Envios.ToList();
        Assert.All(envios, e =>
        {
            Assert.Equal("Conductor asignado", e.Estado);
            Assert.Equal(1, e.ViajeId);
            Assert.Equal(1m, e.DesvioKm);
        });

        var ordenes = context.Paradas.Select(p => p.Orden).OrderBy(o => o).ToList();
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, ordenes);
    }

    [Fact]
    public async Task Confirmar_DeberiaFallar_CuandoSeSuperaElPesoMaximo()
    {
        using var context = CrearContexto();
        await SembrarViajeAsync(context, pesoMax: 4m);

        context.Envios.Add(NuevoEnvio(1, peso: 2m));
        context.Envios.Add(NuevoEnvio(2, peso: 3m));
        await context.SaveChangesAsync();

        var resultado = await CrearServicio(context)
            .ConfirmarSeleccionAsync(1, new List<int> { 1, 2 }, 1);

        Assert.False(resultado.Exitoso);
        Assert.False(resultado.EsConflicto);
        Assert.Contains("peso máximo", resultado.MensajeError!);
        Assert.All(context.Envios.ToList(), e => Assert.Null(e.ViajeId));
    }

    [Fact]
    public async Task Confirmar_DeberiaDevolverNoEncontrado_CuandoElViajeEsDeOtroUsuario()
    {
        using var context = CrearContexto();
        await SembrarViajeAsync(context);
        context.Envios.Add(NuevoEnvio(1));
        await context.SaveChangesAsync();

        var resultado = await CrearServicio(context)
            .ConfirmarSeleccionAsync(1, new List<int> { 1 }, usuarioId: 99);

        Assert.False(resultado.Exitoso);
        Assert.True(resultado.EsNoEncontrado);
    }
}