using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class ViajeDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public int MaxPaquetes { get; set; }
        public decimal PesoMaxKg { get; set; }
        public decimal DesvioMaxKm { get; set; }
        public decimal DistanciaKm { get; set; }
        public int DuracionMin { get; set; }
        public bool Activo { get; set; }

        public List<byte> DiasSemana { get; set; } = new();
        public string DiasTexto { get; set; } = string.Empty;     // Ej: "Lun, Mié, Vie"
        public string HorarioTexto { get; set; } = string.Empty;  // Ej: "08:00 – 08:55"

        public List<ParadaDto> Paradas { get; set; } = new();
    }
}