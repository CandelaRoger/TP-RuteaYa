namespace RuteaYa.MVC.Models
{
    public class MisTransportesViewModel
    {
        public List<TransporteItemViewModel> Transportes { get; set; } = new();
    }

    public class TransporteItemViewModel
    {
        public int EnvioId { get; set; }
        public int ViajeId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public decimal MontoOfrecido { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
