using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class CanceladoEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Cancelado;

    public void Validar(Pedido pedido) => throw new DomainException("Un pedido cancelado no puede ser validado.");
    public void Pagar(Pedido pedido) => throw new DomainException("Un pedido cancelado no puede ser pagado.");
    public void Despachar(Pedido pedido) => throw new DomainException("Un pedido cancelado no puede ser despachado.");
    public void Entregar(Pedido pedido) => throw new DomainException("Un pedido cancelado no puede ser entregado.");
    public void Cancelar(Pedido pedido) => throw new DomainException("Transición redundante: El pedido ya se encuentra CANCELADO.");
}
