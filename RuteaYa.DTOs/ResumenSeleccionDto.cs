namespace RuteaYa.DTOs
{
    public class ResumenSeleccionDto
    {
        public int ViajeId { get; set; }
        public string ViajeOrigenDir { get; set; } = string.Empty;
        public string ViajeDestinoDir { get; set; } = string.Empty;

        // Envíos que siguen disponibles y cumplen las reglas, con su desvío individual
        public List<OportunidadDto> Envios { get; set; } = new();

        public decimal GananciaTotal { get; set; }
        public decimal PesoTotal { get; set; }
        public double DesvioTotalKm { get; set; }
        public int CantidadPaquetes { get; set; }

        // Envíos que ya no están disponibles (otro conductor los tomó, se cancelaron...)
        public List<ProblemaSeleccionDto> Conflictos { get; set; } = new();

        // Restricciones que la selección no cumple (peso, paquetes, desvío, fecha...)
        public List<string> Errores { get; set; } = new();

        public bool PuedeConfirmar =>
            Envios.Count > 0 && Conflictos.Count == 0 && Errores.Count == 0;
    }
}