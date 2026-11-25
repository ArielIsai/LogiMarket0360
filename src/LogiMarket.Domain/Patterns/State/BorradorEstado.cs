using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.State;

public class BorradorEstado : IPedidoEstado
{
    public EstadoPedidoEnum Estado => EstadoPedidoEnum.Borrador;

    public void Validar(Pedido pedido)
    {
        if (pedido.Lineas.Count == 0)
            throw new DomainException("No se puede validar un pedido sin líneas de detalle.");

        pedido.TransicionarEstado(new ValidadoEstado());
    }

    public void Pagar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se puede pagar un pedido en estado BORRADOR sin antes validarlo.");
    }

    public void Despachar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se puede despachar un pedido en estado BORRADOR.");
    }

    public void Entregar(Pedido pedido)
    {
        throw new DomainException("Transición inválida: No se puede entregar un pedido en estado BORRADOR.");
    }

    public void Cancelar(Pedido pedido)
    {
        pedido.TransicionarEstado(new CanceladoEstado());
    }
}
