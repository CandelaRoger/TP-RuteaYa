using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RuteaYa.DTOs
{
    public class CrearViajeDto
    {
        [Required(ErrorMessage = "El origen es obligatorio.")]
        public string Origen { get; set; } = string.Empty;

        [Required(ErrorMessage = "El destino es obligatorio.")]
        public string Destino { get; set; } = string.Empty;

        public List<string> Paradas { get; set; } = new();

        [Required(ErrorMessage = "Debe seleccionar al menos un día.")]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un día.")]
        public List<byte> DiasSemana { get; set; } = new();

        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }

        public int MaxPaquetes { get; set; }
        public decimal PesoMaxKg { get; set; }
        public decimal DesvioMaxKm { get; set; }
    }
}