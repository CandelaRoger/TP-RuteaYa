
namespace RuteaYa.Logica.Rutas
{
    public interface IRutaService
    {
        Task<Coordenadas?> GeocodificarAsync(string direccion);

        Task<ResultadoRuta?> CalcularRutaAsync(
            List<Coordenadas> puntos
        );
    }
}
