using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class ParadaDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }
}