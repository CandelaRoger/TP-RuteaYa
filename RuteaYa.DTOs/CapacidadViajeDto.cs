
namespace RuteaYa.DTOs
{
    public class CapacidadViajeDto
    {
        public int ViajeId { get; set; }

        public int MaxPaquetes { get; set; }
        public int PaquetesOcupados { get; set; }
        public int PaquetesDisponibles { get; set; }

        public decimal PesoMaxKg { get; set; }
        public decimal PesoOcupadoKg { get; set; }
        public decimal PesoDisponibleKg { get; set; }
    }
}
