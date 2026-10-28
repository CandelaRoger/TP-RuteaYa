using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuteaYa.Entidades
{
    public class Viaje
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

        public TimeSpan HoraSalida { get; set; }

        public TimeSpan HoraLlegada { get; set; }

        public int MaxPaquetes { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal PesoMaxKg { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal DesvioMaxKm { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanciaKm { get; set; }

        public int DuracionMin { get; set; }

        public bool Activo { get; set; } = true;

        // Propiedades de navegación (Relaciones)
        public Usuario Usuario { get; set; } = null!;

        public ICollection<Parada> Paradas { get; set; } = new List<Parada>();

        public ICollection<ViajeDia> DiasSemana { get; set; } = new List<ViajeDia>();
    }
}