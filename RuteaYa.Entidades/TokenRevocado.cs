using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Entidades
{
    public class TokenRevocado
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
    }
}
