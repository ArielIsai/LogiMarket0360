namespace LogiMarket.Domain.Entities;

public class EventoOutbox
{
    public int Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty; // ej. PedidoPagado, DespachoRegistrado, DevolucionAceptada
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Procesado { get; set; } = false;
    public DateTime? FechaProcesado { get; set; }
    public string Error { get; set; } = string.Empty;
    public int Intentos { get; set; } = 0;

    public EventoOutbox() { }

    public EventoOutbox(string tipoEvento, string payloadJson)
    {
        TipoEvento = tipoEvento;
        PayloadJson = payloadJson;
        FechaCreacion = DateTime.UtcNow;
        Procesado = false;
    }
}
