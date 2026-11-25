using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class PagadoEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Pagado;

    public void Validar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se puede validar un pedido que ya está PAGADO.");
    }

    public void Pagar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: El pedido ya fue pagado. Se previene cobro duplicado.");
    }

    public void Despachar(Pedido pedido)
    {
        pedido.TransicionarEstado(new DespachadoEstado());
    }

    public void Entregar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: Debe despacharse el pedido antes de registrar su entrega.");
    }

    public void Cancelar(Pedido pedido)
    {
        throw new DomainException("Transición prohibida por regla de negocio: No se permite cancelar un pedido PAGADO.");
    }
}
