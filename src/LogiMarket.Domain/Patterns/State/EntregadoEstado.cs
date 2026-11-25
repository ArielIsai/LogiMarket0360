using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class EntregadoEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Entregado;

    public void Validar(Pedido pedido) => throw new DomainException("El pedido ya fue entregado y no admite validación.");
    public void Pagar(Pedido pedido) => throw new DomainException("El pedido ya fue pagado y entregado.");
    public void Despachar(Pedido pedido) => throw new DomainException("El pedido ya fue despachado y entregado.");
    public void Entregar(Pedido pedido) => throw new DomainException("Transición redundante: El pedido ya se encuentra ENTREGADO.");
    public void Cancelar(Pedido pedido) => throw new DomainException("No se permite cancelar un pedido ENTREGADO. Debe tramitarse como devolución total.");
}
