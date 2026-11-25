using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public class ClienteActivoHandler : ValidadorPedidoBaseHandler
{
    private readonly IClienteRepository _clienteRepository;

    public override string NombreEslabon => "1. Validación de Cliente Activo";

    public ClienteActivoHandler(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    protected override async Task<ResultadoValidacionPaso> EjecutarValidacionLocalAsync(Pedido pedido)
    {
        if (pedido.ClienteId <= 0 && pedido.Cliente == null)
        {
            return new ResultadoValidacionPaso(false, NombreEslabon, "El pedido no tiene asignado un cliente válido.");
        }

        var cliente = pedido.Cliente ?? await _clienteRepository.GetByIdAsync(pedido.ClienteId);
        if (cliente == null)
        {
            return new ResultadoValidacionPaso(false, NombreEslabon, $"El cliente con ID {pedido.ClienteId} no existe en el sistema.");
        }

        if (!cliente.Activo)
        {
            return new ResultadoValidacionPaso(false, NombreEslabon, $"El cliente '{cliente.Nombre}' (Código: {cliente.Codigo}) se encuentra inactivo y no puede registrar pedidos.");
        }

        return new ResultadoValidacionPaso(true, NombreEslabon);
    }
}
