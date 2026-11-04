using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class EnvioDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Peso { get; set; }
        public string Tamanio { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal MontoOfrecido { get; set; }
        public string Estado { get; set; } = string.Empty; 

        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    }
}