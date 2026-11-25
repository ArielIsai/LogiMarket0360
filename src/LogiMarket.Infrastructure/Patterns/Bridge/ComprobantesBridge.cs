using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Infrastructure.Patterns.Bridge;

// Implementor 1: Salida en Formato Vista Previa (Texto estructurado para pantalla)
public class SalidaVistaPrevia : ISalidaDocumento
{
    public string Formato => "VistaPrevia";

    public string EmitirDocumento(string titulo, string contenido, string rutaDestino)
    {
        var separador = new string('=', 60);
        return $"{separador}\n[VISTA PREVIA EN PANTALLA] {titulo.ToUpper()}\n{separador}\n{contenido}\n{separador}\n(Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})";
    }
}

// Implementor 2: Salida en Archivo de Texto TXT Real en disco
public class SalidaArchivoTxt : ISalidaDocumento
{
    public string Formato => "ArchivoTXT";

    public string EmitirDocumento(string titulo, string contenido, string rutaDestino)
    {
        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            var nombreArchivo = $"{titulo.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            rutaDestino = Path.Combine(Path.GetTempPath(), nombreArchivo);
        }

        var separador = new string('-', 60);
        var cuerpoCompleto = $"{separador}\n{titulo.ToUpper()}\n{separador}\n{contenido}\n{separador}\nGenerado el: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n";

        File.WriteAllText(rutaDestino, cuerpoCompleto);
        return $"Archivo TXT generado exitosamente en: {rutaDestino}";
    }
}

// Abstracción Base del Puente (Bridge)
public abstract class ComprobanteDocumento
{
    protected readonly ISalidaDocumento Salida;

    protected ComprobanteDocumento(ISalidaDocumento salida)
    {
        Salida = salida;
    }

    public abstract string Generar(string? rutaDestino = null);
}

// Abstracción Refinada 1: Comprobante de Venta
public class ComprobanteVenta : ComprobanteDocumento
{
    private readonly Pedido _pedido;

    public ComprobanteVenta(Pedido pedido, ISalidaDocumento salida) : base(salida)
    {
        _pedido = pedido;
    }

    public override string Generar(string? rutaDestino = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"COMPROBANTE DE VENTA: {_pedido.Codigo}");
        sb.AppendLine($"Fecha: {_pedido.FechaCreacion:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Cliente: {_pedido.Cliente?.Nombre ?? "N/A"} ({_pedido.Cliente?.Categoria})");
        sb.AppendLine($"Estado: {_pedido.Estado}");
        sb.AppendLine($"Tipo de Entrega: {_pedido.TipoEntrega} (Tarifa: USD {_pedido.TarifaEnvio:F2})");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine("DETALLE DE PRODUCTOS / KITS:");
        foreach (var l in _pedido.Lineas)
        {
            sb.AppendLine($"  - [{l.Sku}] {l.Nombre} x{l.Cantidad} @ USD {l.PrecioUnitario:F2} = USD {l.Subtotal:F2}");
        }
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Subtotal: USD {_pedido.Subtotal:F2}");
        sb.AppendLine($"Descuento ({_pedido.EstrategiaDescuentoAplicada}): -USD {_pedido.Descuento:F2}");
        sb.AppendLine($"Envío: +USD {_pedido.TarifaEnvio:F2}");
        sb.AppendLine($"TOTAL FINAL: USD {_pedido.Total:F2}");

        return Salida.EmitirDocumento($"Venta_{_pedido.Codigo}", sb.ToString(), rutaDestino ?? string.Empty);
    }
}

// Abstracción Refinada 2: Comprobante de Devolución
public class ComprobanteDevolucion : ComprobanteDocumento
{
    private readonly Devolucion _devolucion;
    private readonly Pedido _pedido;

    public ComprobanteDevolucion(Devolucion devolucion, Pedido pedido, ISalidaDocumento salida) : base(salida)
    {
        _devolucion = devolucion;
        _pedido = pedido;
    }

    public override string Generar(string? rutaDestino = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"COMPROBANTE DE DEVOLUCIÓN: DEV-{_pedido.Codigo}");
        sb.AppendLine($"Pedido Original: {_pedido.Codigo}");
        sb.AppendLine($"Fecha Solicitud: {_devolucion.FechaSolicitud:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Motivo: {_devolucion.Motivo}");
        sb.AppendLine($"Días desde Entrega: {_devolucion.DiasDesdeEntrega}");
        sb.AppendLine($"Producto sin uso: {(_devolucion.ProductoSinUso ? "SÍ" : "NO")}");
        sb.AppendLine($"Stock reintegrado: {(_devolucion.StockReintegrado ? "SÍ" : "NO")}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"MONTO REEMBOLSABLE (Sin tarifa de flete): USD {_devolucion.ReembolsoMonto:F2}");
        sb.AppendLine($"Observaciones: {_devolucion.Observaciones}");

        return Salida.EmitirDocumento($"Devolucion_{_pedido.Codigo}", sb.ToString(), rutaDestino ?? string.Empty);
    }
}
