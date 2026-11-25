namespace LogiMarket.Domain.Entities;

public class Despacho
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string NumeroGuia { get; set; } = string.Empty;
    public DateTime FechaDespacho { get; set; } = DateTime.UtcNow;
    public DateTime FechaEstimadaEntrega { get; set; }
    public DateTime? FechaRealEntrega { get; set; }
    public string Transportista { get; set; } = "LogiMarket Express";
    public string Estado { get; set; } = "EnTransito";

    public Despacho() { }

    public Despacho(int pedidoId, string numeroGuia, DateTime fechaEstimadaEntrega)
    {
        PedidoId = pedidoId;
        NumeroGuia = numeroGuia;
        FechaEstimadaEntrega = fechaEstimadaEntrega;
    }
}
