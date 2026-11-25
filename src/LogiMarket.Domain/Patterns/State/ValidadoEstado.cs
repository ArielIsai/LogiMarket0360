using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class ValidadoEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Validado;

    public void Validar(Pedido pedido)
    {
        throw new DomainException("Transición redundante: El pedido ya se encuentra en estado VALIDADO.");
    }

    public void Pagar(Pedido pedido)
    {
        pedido.TransicionarEstado(new PagadoEstado());
    }

    public void Despachar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se permite despachar antes de pagar el pedido.");
    }

    public void Entregar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se puede entregar un pedido no pagado.");
    }

    public void Cancelar(Pedido pedido)
    {
        pedido.TransicionarEstado(new CanceladoEstado());
    }
}
