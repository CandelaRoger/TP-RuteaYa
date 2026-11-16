namespace RuteaYa.DTOs
{
    public class ResultadoDesvioDto
    {
        public double DistanciaHabitualKm { get; set; }
        public double DistanciaAlternativaKm { get; set; }
        public double DesvioKm { get; set; }
        public double DuracionHabitualMin { get; set; }
        public double DuracionAlternativaMin { get; set; }
        public double TiempoAdicionalMin { get; set; }
        public bool Compatible { get; set; }
        public List<CoordenadaDto> PuntosRutaAlternativa { get; set; } = new();
    }
}