using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class Envio
    {
        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; } 

        [Required]
        [MaxLength(200)]
        public string OrigenDir { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal OrigenLat { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal OrigenLng { get; set; }

        [Required]
        [MaxLength(200)]
        public string DestinoDir { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal DestinoLat { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal DestinoLng { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Peso { get; set; }

        [Required]
        [MaxLength(50)]
        public string Tamanio { get; set; } = string.Empty; 

        [Required]
        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoOfrecido { get; set; }

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Buscando conductor";

        public Usuario Usuario { get; set; } = null!;
    }
}
