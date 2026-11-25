using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.Patterns.Command;

public class CancelarPedidoCommand : ICommand
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly int _pedidoId;
    private readonly string _usuario;

    public string Nombre => "CancelarPedidoCommand";
    public string ParametrosResumen => $"PedidoId: {_pedidoId}, Usuario: {_usuario}";

    public CancelarPedidoCommand(IPedidoRepository pedidoRepo, IUnitOfWork unitOfWork, IAuditoriaRepository auditoriaRepo, int pedidoId, string usuario)
    {
        _pedidoRepo = pedidoRepo;
        _unitOfWork = unitOfWork;
        _auditoriaRepo = auditoriaRepo;
        _pedidoId = pedidoId;
        _usuario = usuario;
    }

    public async Task<ResultadoOperacionDto> ExecuteAsync()
    {
        var pedido = await _pedidoRepo.GetByIdWithDetailsAsync(_pedidoId);
        if (pedido == null)
            return new ResultadoOperacionDto(false, $"El pedido con ID {_pedidoId} no existe.");

        try
        {
            pedido.Cancelar();
            await _pedidoRepo.UpdateAsync(pedido);

            await _auditoriaRepo.AddAsync(new Auditoria(
                _usuario, "CancelarPedido", "Pedido", pedido.Id.ToString(), $"Pedido {pedido.Codigo} cancelado correctamente."
            ));

            await _unitOfWork.CommitAsync();
            return new ResultadoOperacionDto(true, $"Pedido {pedido.Codigo} cancelado con éxito.");
        }
        catch (DomainException ex)
        {
            return new ResultadoOperacionDto(false, $"Rechazo: {ex.Message}");
        }
    }
}
