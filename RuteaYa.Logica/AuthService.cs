using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using RuteaYa.DTOs;
using RuteaYa.Entidades;


namespace RuteaYa.Logica
{
    public interface IAuthService
    {
        Task<ResultadoLogin> Login(LoginDto dto);
        Task<Resultado> RegistrarUsuarioAsync(RegistroDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly RuteaYaDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthService(RuteaYaDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<Resultado> RegistrarUsuarioAsync(RegistroDto dto)
        {
            bool existeEmail = await UsuarioRegistrado(dto.Email);

            if (existeEmail)
            {
                return new Resultado
                {
                    Exitoso = false,
                    MensajeError = "El email ya se encuentra registrado."
                };
            }
            Usuario usuario = CrearUsuario(dto);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return new Resultado { Exitoso = true };
        }

        private static Usuario CrearUsuario(RegistroDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
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

        public async Task<ResultadoLogin> Login(LoginDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                return new ResultadoLogin { Exitoso = false, MensajeError = "Credenciales inválidas." };

            bool passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);

            if (!passwordValida)
                return new ResultadoLogin { Exitoso = false, MensajeError = "Credenciales inválidas." };

            var tokenString = _tokenService.GenerateToken(usuario);

            return new ResultadoLogin { Exitoso = true, Token = tokenString };
        }
    }

    public class Resultado
    {
        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }
    }
    public class ResultadoLogin : Resultado
    {
        public string? Token { get; set; }
    }


}
