using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Entidades
{ 
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        public string Apellido { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
    }
}
