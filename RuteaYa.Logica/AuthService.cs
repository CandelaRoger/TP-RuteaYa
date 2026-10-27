using RuteaYa.DTOs;
using RuteaYa.Entidades;
using Microsoft.EntityFrameworkCore;


namespace RuteaYa.Logica
{
    public interface IAuthService
    {
        Task<ResultadoRegistro> RegistrarUsuarioAsync(RegistroDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly RuteaYaDbContext _context;

        public AuthService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoRegistro> RegistrarUsuarioAsync(RegistroDto dto)
        {
            // 1. Validar email único (Regla del TP)
            bool existeEmail = await UsuarioRegistrado(dto.Email);

            if (existeEmail)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    MensajeError = "El email ya se encuentra registrado."
                };
            }
            // 2. Crear entidad con PasswordHash
            Usuario usuario = CrearUsuario(dto);

            // Persistir cambios
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return new ResultadoRegistro { Exitoso = true };
        }

        private static Usuario CrearUsuario(RegistroDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                // Usar BCrypt.Net-Next o PasswordHasher de ASP.NET Core
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreadoEn = DateTime.UtcNow
            };
            return usuario;
        }

        private async Task<bool> UsuarioRegistrado(string email)
        {
            return await _context.Usuarios
               .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }

    public class ResultadoRegistro
    {
        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }
    }
}
