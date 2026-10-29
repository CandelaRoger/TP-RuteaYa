using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.DTOs
{
    public class DashboardDto
    {
        public string NombreUsuario { get; set; }
        public int CantProximosViajes { get; set; }
        public int  CantEnviosPendientes { get; set; }
        public int CantOportunidades { get; set; }
        public decimal PotencialDelMes { get; set; }

        //listas resumidas
         public List<ViajeResumenDto> ProximosViajes { get; set; }
         public List<EnvioResumenDto> EnviosPendientes {get; set;}

        public class ViajeResumenDto
        {
            public int Id { get; set; }
            public string Origen { get; set; }
            public string Destino { get; set; }
            public string Dias { get; set; }
            public string Horario { get; set; }
            public bool Activo { get; set; }
        }

        public class EnvioResumenDto
        {
            public int Id { get; set; }
            public string Origen { get; set; }
            public string Destino { get; set; }
            public string Descripcion { get; set; }
            public decimal Monto { get; set; }
            public string Estado { get; set; }
        }
    }
}
