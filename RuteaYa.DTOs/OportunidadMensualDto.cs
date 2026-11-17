
namespace RuteaYa.DTOs
{
    public class OportunidadMensualDto
    {
        public int ViajeId { get; set; }

        public int EnvioId { get; set; }

        public DateTime Fecha { get; set; }

        public string Origen { get; set; } = string.Empty;

        public string Destino { get; set; } = string.Empty;

        public decimal MontoOfrecido { get; set; }

        public double DesvioKm { get; set; }
    }
}
