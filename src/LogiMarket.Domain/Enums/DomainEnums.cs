namespace LogiMarket.Domain.Enums;

public enum RolUsuario
{
    Admin,
    Ventas,
    Bodega,
    Consulta
}

public enum CategoriaCliente
{
    Normal,
    Vip
}

public enum EstadoPedidoEnum
{
    Borrador,
    Validado,
    Pagado,
    Despachado,
    Entregado,
    Cancelado
}

public enum TipoEntregaEnum
{
    Estandar,
    Expres
}

public enum TipoMovimientoInventario
{
    Ingreso,
    Venta,
    Devolucion,
    Ajuste
}

public enum MotivoDevolucionEnum
{
    Defecto,
    Arrepentimiento
}

public enum TipoAvisoEnum
{
    Email,
    Sms
}
