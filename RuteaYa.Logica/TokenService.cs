using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RuteaYa.Entidades;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RuteaYa.Logica
{
    public class TokenService : ITokenService
    {

        private readonly IConfiguration _configuration;
        private readonly RuteaYaDbContext _context;

        public TokenService(IConfiguration configuration, RuteaYaDbContext ruteaYaDbContext)
        {
            _configuration = configuration;
            _context = ruteaYaDbContext;
        }


        public string GenerateToken(Usuario usuario)
        {
            var settings = GetJwtSettings();
            var claims = CreateClaims(usuario);
            var descriptor = CreateTokenDescriptor(claims, settings);

            return WriteToken(descriptor);
        }
        public async Task RevocarTokenAsyncr(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var revocado = new TokenRevocado
            {
                Token = token,
                FechaExpiracion = jwt.ValidTo // Guardamos cuándo vence realmente
            };

            _context.TokensRevocados.Add(revocado);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EstaRevocadoAsync(string token)
        {
            return await _context.TokensRevocados.AnyAsync(t => t.Token == token);
        }
        private JwtSettings GetJwtSettings()
        {
            var section = _configuration.GetSection("JwtSettings");
            var secretKey = section["SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("La clave secreta del JWT no está configurada.");

            return new JwtSettings(
                secretKey,
                section["Issuer"],
                section["Audience"],
                double.Parse(section["ExpireMinutes"] ?? "120"));
        }

        private static Claim[] CreateClaims(Usuario usuario)
        {
            return new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Surname, usuario.Apellido),
                new Claim(ClaimTypes.Email, usuario.Email)
            };
        }

        private static SecurityTokenDescriptor CreateTokenDescriptor(
            IEnumerable<Claim> claims,
            JwtSettings settings)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(settings.ExpireMinutes),
                Issuer = settings.Issuer,
                Audience = settings.Audience,
                SigningCredentials = credentials
            };
        }

        private static string WriteToken(SecurityTokenDescriptor descriptor)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);

            return handler.WriteToken(token);
        }

        private sealed record JwtSettings(
            string SecretKey,
            string? Issuer,
            string? Audience,
            double ExpireMinutes);
    }

    public interface ITokenService
    {
        string GenerateToken(Usuario usuario);
        Task RevocarTokenAsyncr(string token);
        Task<bool> EstaRevocadoAsync(string token);

    }
}
