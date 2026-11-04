using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace RuteaYa.MVC.Models
{
    public class CrearEnvioViewModel
    {
        [Required(ErrorMessage = "El origen es obligatorio.")]
        public string Origen { get; set; } = string.Empty;

        [Required(ErrorMessage = "El destino es obligatorio.")]
        public string Destino { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha límite es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Range(0.1, 1000, ErrorMessage = "El peso debe ser mayor a cero.")]
        public decimal Peso { get; set; } = 1;

        [Required(ErrorMessage = "Debe elegir el tamaño del paquete.")]
        public string Tamanio { get; set; } = "Pequeño"; 

        [Required(ErrorMessage = "La descripción del paquete es obligatoria.")]
        public string Descripcion { get; set; } = string.Empty;

        [Range(1, 1000000, ErrorMessage = "El monto ofrecido debe ser mayor a cero.")]
        public decimal MontoOfrecido { get; set; } = 4000;
    }
}