using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Entities;

public class Devolucion
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public MotivoDevolucionEnum Motivo { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public int DiasDesdeEntrega { get; set; }
    public bool ProductoSinUso { get; set; }
    public bool Aprobada { get; set; }
    public decimal ReembolsoMonto { get; set; }
    public bool StockReintegrado { get; set; }
    public string Observaciones { get; set; } = string.Empty;

    public Devolucion() { }

    public Devolucion(int pedidoId, MotivoDevolucionEnum motivo, int diasDesdeEntrega, bool productoSinUso, bool aprobada, decimal reembolsoMonto, bool stockReintegrado, string observaciones)
    {
        PedidoId = pedidoId;
        Motivo = motivo;
        DiasDesdeEntrega = diasDesdeEntrega;
        ProductoSinUso = productoSinUso;
        Aprobada = aprobada;
        ReembolsoMonto = reembolsoMonto;
        StockReintegrado = stockReintegrado;
        Observaciones = observaciones;
    }
}
