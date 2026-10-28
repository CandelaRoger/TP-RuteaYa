using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuteaYa.Entidades
{
    public class Parada
    {
        [Key]
        public int Id { get; set; }

        public int ViajeId { get; set; }

        public int Orden { get; set; }

        [Required]
        [MaxLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal Lat { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Lng { get; set; }

        // Propiedad de navegación
        public Viaje Viaje { get; set; } = null!;
    }
}