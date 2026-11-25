namespace LogiMarket.Domain.Entities;

public class PedidoDetalle
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsKit { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public List<PedidoDetalleComponente> ComponentesFisicos { get; set; } = new();

    public PedidoDetalle() { }

    public PedidoDetalle(string sku, string nombre, bool esKit, int cantidad, decimal precioUnitario)
    {
        Sku = sku;
        Nombre = nombre;
        EsKit = esKit;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        Subtotal = Math.Round(cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);
    }
}
