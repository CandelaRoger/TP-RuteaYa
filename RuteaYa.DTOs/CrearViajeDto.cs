using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class CrearViajeDto
    {
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;

        // lista de paradas intermedias
        public List<string> Paradas { get; set; } = new();

        // 1 = Lunes, 2 = Martes, 3 = Miercoles, 4 = Jueves, 5 = Viernes, 6 = Sabado, 7 = Domingo
        public List<byte> DiasSemana { get; set; } = new();

        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }

        public int MaxPaquetes { get; set; }
        public decimal PesoMaxKg { get; set; }
        public decimal DesvioMaxKm { get; set; }
    }
}