
namespace RuteaYa.Logica.Rutas
{
    public interface IRutaService
    {
        Task<Coordenadas?> GeocodificarAsync(string direccion);

        Task<ResultadoRuta?> CalcularRutaAsync(
            List<Coordenadas> puntos
        );

        Task<List<SugerenciaDireccion>> BuscarDireccionesAsync(
            string texto
        );
    }
}
