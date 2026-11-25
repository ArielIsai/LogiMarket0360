using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.State;

public interface IPedidoEstado
{
    EstadoPedidoEnum Estado { get; }
    void Validar(Pedido pedido);
    void Pagar(Pedido pedido);
    void Despachar(Pedido pedido);
    void Entregar(Pedido pedido);
    void Cancelar(Pedido pedido);
}
