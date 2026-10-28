using System.ComponentModel.DataAnnotations;

namespace RuteaYa.MVC.Models
{
    public class CrearViajeViewModel
    {
        [Required(ErrorMessage = "El origen es obligatorio.")]
        public string Origen { get; set; } = string.Empty;

        [Required(ErrorMessage = "El destino es obligatorio.")]
        public string Destino { get; set; } = string.Empty;

        public string? ParadasTexto { get; set; } // Paradas separadas por coma, ej: "Caballito, Flores"

        public List<byte> DiasSeleccionados { get; set; } = new();

        [Required(ErrorMessage = "La hora de salida es obligatoria.")]
        public TimeSpan HoraSalida { get; set; } = new TimeSpan(8, 0, 0);

        [Required(ErrorMessage = "La hora estimada de llegada es obligatoria.")]
        public TimeSpan HoraLlegada { get; set; } = new TimeSpan(9, 0, 0);

        [Range(1, 100, ErrorMessage = "La capacidad de paquetes debe ser mayor a cero.")]
        public int MaxPaquetes { get; set; } = 3;

        [Range(0.1, 1000, ErrorMessage = "El peso máximo debe ser mayor a cero.")]
        public decimal PesoMaxKg { get; set; } = 10;

        [Range(0, 100, ErrorMessage = "El desvío máximo no puede ser negativo.")]
        public decimal DesvioMaxKm { get; set; } = 5;
    }
}