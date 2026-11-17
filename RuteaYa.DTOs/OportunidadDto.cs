using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class OportunidadDto
    {public int EnvioId { get; set; }

        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public decimal Peso { get; set; }

        public string Tamanio { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal MontoOfrecido { get; set; }

        public double DistanciaHabitualKm { get; set; }
        public double DistanciaConEnvioKm { get; set; }
        public double DesvioKm { get; set; }
        public double TiempoAdicionalMin { get; set; }
        public int TiempoHabitualMin { get; set; }

        public decimal CapacidadRestanteKg { get; set; }

        public int PaquetesRestantes { get; set; }
        public string Compatibilidad { get; set; } = string.Empty;
    }
}

