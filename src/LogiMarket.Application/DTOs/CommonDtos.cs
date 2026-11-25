using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.DTOs;

public record ProductoDto(int Id, string Sku, string Nombre, string Categoria, decimal Precio, int Stock, decimal PesoKg, bool Activo);

public record ClienteDto(int Id, string Codigo, string Nombre, string Identificacion, CategoriaCliente Categoria, string Contacto, string Correo, bool Activo);

public record PedidoLineaDto(string Sku, string Nombre, bool EsKit, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record PedidoDto(
    int Id,
    string Codigo,
    int ClienteId,
    string ClienteNombre,
    CategoriaCliente ClienteCategoria,
    DateTime FechaCreacion,
    DateTime? FechaPago,
    DateTime? FechaDespacho,
    DateTime? FechaEntrega,
    EstadoPedidoEnum Estado,
    TipoEntregaEnum TipoEntrega,
    string GuiaDespacho,
    decimal Subtotal,
    decimal Descuento,
    decimal TarifaEnvio,
    decimal Total,
    string EstrategiaDescuento,
    List<PedidoLineaDto> Lineas
);

public record ResultadoOperacionDto(bool Exitoso, string Mensaje, object? Datos = null);

public record ResultadoPagoDto(bool Aprobado, string CodigoTransaccion, string Proveedor, string Mensaje, decimal Monto);

public record ResultadoConfirmacionDto(bool Exitoso, string Mensaje, string? CodigoTransaccion, decimal TotalPagado, string EstadoFinal);
