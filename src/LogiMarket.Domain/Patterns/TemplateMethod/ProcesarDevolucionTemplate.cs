using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.TemplateMethod;

public abstract class ProcesarDevolucionTemplate
{
    public abstract MotivoDevolucionEnum Motivo { get; }

    // Método plantilla que define el algoritmo invariante
    public DevolucionResultado Procesar(Pedido pedido, int diasDesdeEntrega, bool productoSinUso, bool yaFueDevuelto)
    {
        if (pedido == null)
            throw new ArgumentNullException(nameof(pedido));

        // 1. Verificar estado del pedido y si ya fue devuelto
        if (pedido.Estado != EstadoPedidoEnum.Entregado)
        {
            return new DevolucionResultado
            {
                Exitoso = false,
                Mensaje = $"Rechazo: Solo se admiten devoluciones de pedidos en estado ENTREGADO. Estado actual: {pedido.Estado}."
            };
        }

        if (yaFueDevuelto)
        {
            return new DevolucionResultado
            {
                Exitoso = false,
                Mensaje = "Rechazo: El pedido ya cuenta con una devolución registrada previamente. No se admiten devoluciones duplicadas."
            };
        }

        // 2. Verificar elegibilidad de plazo según la política concreta
        if (!VerificarPlazoElegible(diasDesdeEntrega, out string motivoPlazo))
        {
            return new DevolucionResultado
            {
                Exitoso = false,
                Mensaje = $"Rechazo por plazo: {motivoPlazo}"
            };
        }

        // 3. Inspeccionar condición del producto según la política concreta
        if (!InspeccionarCondicion(productoSinUso, out string motivoCondicion))
        {
            return new DevolucionResultado
            {
                Exitoso = false,
                Mensaje = $"Rechazo por condición: {motivoCondicion}"
            };
        }

        // 4. Calcular importe de reembolso (subtotal productos - descuento, SIN tarifa de envío)
        var reembolso = CalcularReembolso(pedido);

        // 5. Determinar si se reintegra stock
        var reintegra = DebeReintegrarStock();

        // 6. Generar registro de devolución
        var devolucion = new Devolucion(
            pedido.Id,
            Motivo,
            diasDesdeEntrega,
            productoSinUso,
            aprobada: true,
            reembolsoMonto: reembolso,
            stockReintegrado: reintegra,
            observaciones: $"Devolución aprobada por motivo {Motivo}. Plazo: {diasDesdeEntrega} días."
        );

        return new DevolucionResultado
        {
            Exitoso = true,
            Mensaje = $"Devolución aprobada exitosamente por motivo {Motivo}.",
            MontoReembolso = reembolso,
            ReintegrarStock = reintegra,
            DevolucionGenerada = devolucion
        };
    }

    protected abstract bool VerificarPlazoElegible(int diasDesdeEntrega, out string mensaje);
    protected abstract bool InspeccionarCondicion(bool productoSinUso, out string mensaje);
    protected virtual bool DebeReintegrarStock() => true;

    protected virtual decimal CalcularReembolso(Pedido pedido)
    {
        // En LogiMarket 360 se reembolsa el subtotal menos el descuento; el flete NO se devuelve
        var monto = pedido.Subtotal - pedido.Descuento;
        return Math.Round(monto, 2, MidpointRounding.AwayFromZero);
    }
}
