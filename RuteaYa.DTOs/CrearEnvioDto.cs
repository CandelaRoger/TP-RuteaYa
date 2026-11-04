using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class CrearEnvioDto
    {
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Peso { get; set; }
        public string Tamanio { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal MontoOfrecido { get; set; }
    }
}