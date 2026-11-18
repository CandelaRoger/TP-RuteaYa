using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class ActualizarEstadoEnvioDto
    {
        public int EnvioId { get; set; }
        public string NuevoEstado { get; set; } = string.Empty;
    }
}
