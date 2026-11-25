using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public string ProductoSku { get; set; } = string.Empty;
    public TipoMovimientoInventario TipoMovimiento { get; set; }
    public int Cantidad { get; set; }
    public int StockPrevio { get; set; }
    public int StockNuevo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Referencia { get; set; } = string.Empty; // ej. PED-001, DEV-001, AJUSTE-001
    public string Motivo { get; set; } = string.Empty;

    public MovimientoInventario() { }

    public MovimientoInventario(string productoSku, TipoMovimientoInventario tipoMovimiento, int cantidad, int stockPrevio, int stockNuevo, string referencia, string motivo)
    {
        ProductoSku = productoSku;
        TipoMovimiento = tipoMovimiento;
        Cantidad = cantidad;
        StockPrevio = stockPrevio;
        StockNuevo = stockNuevo;
        Referencia = referencia;
        Motivo = motivo;
    }
}
