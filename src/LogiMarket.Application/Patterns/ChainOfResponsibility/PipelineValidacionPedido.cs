using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public class PipelineValidacionPedido
{
    private readonly IValidadorPedidoHandler _cadena;

    public PipelineValidacionPedido(IClienteRepository clienteRepository, IProductoRepository productoRepository)
    {
        var paso1 = new ClienteActivoHandler(clienteRepository);
        var paso2 = new LineasCantidadesHandler();
        var paso3 = new StockDisponibleHandler(productoRepository);
        var paso4 = new CondicionesConfirmacionHandler();

        paso1.SetSiguiente(paso2)
             .SetSiguiente(paso3)
             .SetSiguiente(paso4);

        _cadena = paso1;
    }

    public async Task<ResultadoValidacionPaso> EjecutarAsync(Pedido pedido)
    {
        return await _cadena.ValidarAsync(pedido);
    }
}
