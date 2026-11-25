namespace LogiMarket.Domain.Entities;

public class Pago
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string CodigoTransaccion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public string MetodoPago { get; set; } = "TarjetaSimulada";
    public string Proveedor { get; set; } = string.Empty;
    public bool Aprobado { get; set; }
    public string MensajeRespuesta { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;

    public Pago() { }

    public Pago(int pedidoId, string codigoTransaccion, decimal monto, string proveedor, bool aprobado, string mensajeRespuesta, string idempotencyKey)
    {
        PedidoId = pedidoId;
        CodigoTransaccion = codigoTransaccion;
        Monto = monto;
        Proveedor = proveedor;
        Aprobado = aprobado;
        MensajeRespuesta = mensajeRespuesta;
        IdempotencyKey = idempotencyKey;
    }
}
