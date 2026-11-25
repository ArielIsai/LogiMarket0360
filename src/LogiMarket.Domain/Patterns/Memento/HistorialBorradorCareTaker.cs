using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.Memento;

public class HistorialBorradorCareTaker
{
    private const int MaxCapacidad = 5;
    private readonly Stack<PedidoBorradorMemento> _deshacerStack = new();
    private readonly Stack<PedidoBorradorMemento> _rehacerStack = new();
    private bool _bloqueado = false;

    public bool PuedeDeshacer => !_bloqueado && _deshacerStack.Count > 0;
    public bool PuedeRehacer => !_bloqueado && _rehacerStack.Count > 0;

    public void GuardarEstado(Pedido pedido, string descripcion)
    {
        if (_bloqueado)
            throw new DomainException("El historial de estados está bloqueado tras la confirmación o pago del pedido.");
        if (pedido.Estado != EstadoPedidoEnum.Borrador)
            throw new DomainException("Solo se pueden capturar mementos de pedidos en estado BORRADOR.");

        if (_deshacerStack.Count >= MaxCapacidad)
        {
            // Descartar el más antiguo para mantener máximo 5
            var lista = _deshacerStack.ToList();
            lista.RemoveAt(lista.Count - 1);
            _deshacerStack.Clear();
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                _deshacerStack.Push(lista[i]);
            }
        }

        _deshacerStack.Push(new PedidoBorradorMemento(pedido, descripcion));
        _rehacerStack.Clear();
    }

    public PedidoBorradorMemento Deshacer(Pedido pedidoActual)
    {
        if (!PuedeDeshacer)
            throw new DomainException("No hay estados previos para deshacer.");

        // Guardar el estado actual en rehacer
        _rehacerStack.Push(new PedidoBorradorMemento(pedidoActual, "Estado antes de deshacer"));

        return _deshacerStack.Pop();
    }

    public PedidoBorradorMemento Rehacer(Pedido pedidoActual)
    {
        if (!PuedeRehacer)
            throw new DomainException("No hay estados posteriores para rehacer.");

        _deshacerStack.Push(new PedidoBorradorMemento(pedidoActual, "Estado antes de rehacer"));

        return _rehacerStack.Pop();
    }

    public void BloquearYLimpiar()
    {
        _deshacerStack.Clear();
        _rehacerStack.Clear();
        _bloqueado = true;
    }
}
