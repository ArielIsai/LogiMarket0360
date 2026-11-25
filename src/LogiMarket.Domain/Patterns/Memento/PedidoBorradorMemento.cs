using LogiMarket.Domain.Entities;

namespace LogiMarket.Domain.Patterns.Memento;

public class PedidoBorradorMemento
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string Descripcion { get; }
    public decimal Subtotal { get; }
    public decimal Descuento { get; }
    public decimal Total { get; }
    public List<LineaMemento> LineasSnapshot { get; }

    public record LineaMemento(string Sku, string Nombre, bool EsKit, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

    public PedidoBorradorMemento(Pedido pedido, string descripcion)
    {
        Descripcion = descripcion;
        Subtotal = pedido.Subtotal;
        Descuento = pedido.Descuento;
        Total = pedido.Total;
        LineasSnapshot = pedido.Lineas.Select(l => new LineaMemento(
            l.Sku, l.Nombre, l.EsKit, l.Cantidad, l.PrecioUnitario, l.Subtotal
        )).ToList();
    }
}
