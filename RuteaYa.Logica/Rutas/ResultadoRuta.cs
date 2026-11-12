
namespace RuteaYa.Logica.Rutas
{
    public class ResultadoRuta
    {
        public double DistanciaKm { get; set; }
        public double DuracionMinutos { get; set; }
        public List<Coordenadas> PuntosRuta { get; set; } = new();
    }
}
