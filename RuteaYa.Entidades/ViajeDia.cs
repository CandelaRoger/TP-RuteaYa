using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Entidades
{
    public class ViajeDia
    {
        public int ViajeId { get; set; }
        public byte DiaSemana { get; set; }
        public Viaje Viaje { get; set; } = null!;
    }
}