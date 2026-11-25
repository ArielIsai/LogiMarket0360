using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public class CondicionesConfirmacionHandler : ValidadorPedidoBaseHandler
{
    public override string NombreEslabon => "4. Validación de Condiciones de Confirmación";

    protected override Task<ResultadoValidacionPaso> EjecutarValidacionLocalAsync(Pedido pedido)
    {
        if (pedido.Estado != EstadoPedidoEnum.Validado && pedido.Estado != EstadoPedidoEnum.Borrador)
        {
            return Task.FromResult(new ResultadoValidacionPaso(
                false, NombreEslabon, $"Estado inválido para confirmación. El pedido se encuentra en estado '{pedido.Estado}'."));
        }

        if (pedido.Total <= 0)
        {
            return Task.FromResult(new ResultadoValidacionPaso(
                false, NombreEslabon, $"El importe total del pedido ({pedido.Total}) debe ser mayor a 0 para proceder al cobro."));
        }

        return Task.FromResult(new ResultadoValidacionPaso(true, NombreEslabon));
    }
}
