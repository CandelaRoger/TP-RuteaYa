using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class HistorialViewModel
    {
        public int TotalViajes { get; set; }
        public int TotalEnvios { get; set; }
        public decimal GananciaTotal { get; set; }
        public List<ItemHistorialDto> Operaciones { get; set; } = new();
    }

   
    public class ItemHistorialDto
    {
        public string Tipo { get; set; } = "Viaje"; 
        public string Ruta { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string EstadoBadge { get; set; } = "bg-primary";
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
