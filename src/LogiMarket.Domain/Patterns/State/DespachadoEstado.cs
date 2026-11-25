using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class DespachadoEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Despachado;

    public void Validar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: El pedido ya está DESPACHADO.");
    }

    public void Pagar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: El pedido ya fue pagado previamente.");
    }

    public void Despachar(Pedido pedido)
    {
        throw new DomainException("Transición redundante: El pedido ya se encuentra DESPACHADO.");
    }

    public void Entregar(Pedido pedido)
    {
        pedido.TransicionarEstado(new EntregadoEstado());
    }

    public void Cancelar(Pedido pedido)
    {
        throw new DomainException("Transición prohibida: No se permite cancelar un pedido DESPACHADO.");
    }
}
