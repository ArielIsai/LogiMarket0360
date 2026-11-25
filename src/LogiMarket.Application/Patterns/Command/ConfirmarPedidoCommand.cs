using LogiMarket.Application.DTOs;
using LogiMarket.Application.Patterns.Facade;

namespace LogiMarket.Application.Patterns.Command;

public class ConfirmarPedidoCommand : ICommand
{
    private readonly ConfirmacionPedidoFacade _facade;
    private readonly int _pedidoId;
    private readonly string _claveIdempotencia;
    private readonly string _rolUsuario;
    private readonly string _proveedor;

    public string Nombre => "ConfirmarPedidoCommand";
    public string ParametrosResumen => $"PedidoId: {_pedidoId}, Idempotencia: {_claveIdempotencia}, Rol: {_rolUsuario}, Proveedor: {_proveedor}";

    public ConfirmarPedidoCommand(
        ConfirmacionPedidoFacade facade,
        int pedidoId,
        string claveIdempotencia,
        string rolUsuario,
        string proveedor)
    {
        _facade = facade;
        _pedidoId = pedidoId;
        _claveIdempotencia = claveIdempotencia;
        _rolUsuario = rolUsuario;
        _proveedor = proveedor;
    }

    public async Task<ResultadoOperacionDto> ExecuteAsync()
    {
        var res = await _facade.ConfirmarPedidoAsync(_pedidoId, _claveIdempotencia, _rolUsuario, _proveedor);
        return new ResultadoOperacionDto(res.Exitoso, res.Mensaje, res);
    }
}
