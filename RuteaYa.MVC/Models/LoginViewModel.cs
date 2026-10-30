using System.ComponentModel.DataAnnotations;

namespace RuteaYa.MVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        
        public string Password { get; set; } = string.Empty;
    }
}
