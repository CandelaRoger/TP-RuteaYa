using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Entidades
{
    public class ViajeDia
    {
        public int ViajeId { get; set; }

        // 1 = Lunes, 2 = Martes, 3 = Miercoles, 4 = Jueves, 5 = Viernes, 6 = Sabado, 7 = Domingo
        public byte DiaSemana { get; set; }

        // Propiedad de navegación
        public Viaje Viaje { get; set; } = null!;
    }
}