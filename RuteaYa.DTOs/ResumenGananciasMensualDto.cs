
namespace RuteaYa.DTOs
{
    public class ResumenGananciasMensualDto
    {
        public decimal GananciasActuales { get; set; }

        public decimal GananciasPotenciales { get; set; }

        public decimal TotalEstimado => GananciasActuales + GananciasPotenciales;

        public string CriterioSeleccionado { get; set; } = "ganancia";

        public int ViajesAnalizados { get; set; }

        public int EnviosPosibles { get; set; }

        public decimal PromedioPorViaje { get; set; }

        public List<GananciaSemanalDto> GananciasPorSemana { get; set; } = new();

        public string MejorRecorrido { get; set; } = string.Empty;
    }

    public class GananciaSemanalDto
    {
        public int Semana { get; set; }

        public decimal Ganancia { get; set; }
    }
}
