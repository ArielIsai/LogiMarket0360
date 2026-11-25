using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.Patterns.Command;

public class DespacharPedidoCommand : ICommand
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly int _pedidoId;
    private readonly string _numeroGuia;
    private readonly string _usuario;
    private readonly RolUsuario _rolUsuario;

    public string Nombre => "DespacharPedidoCommand";
    public string ParametrosResumen => $"PedidoId: {_pedidoId}, Guia: {_numeroGuia}, Rol: {_rolUsuario}";

    public DespacharPedidoCommand(
        IPedidoRepository pedidoRepo,
        IUnitOfWork unitOfWork,
        IAuditoriaRepository auditoriaRepo,
        int pedidoId,
        string numeroGuia,
        string usuario,
        RolUsuario rolUsuario)
    {
        _pedidoRepo = pedidoRepo;
        _unitOfWork = unitOfWork;
        _auditoriaRepo = auditoriaRepo;
        _pedidoId = pedidoId;
        _numeroGuia = numeroGuia;
        _usuario = usuario;
        _rolUsuario = rolUsuario;
    }

    public async Task<ResultadoOperacionDto> ExecuteAsync()
    {
        if (_rolUsuario != RolUsuario.Bodega && _rolUsuario != RolUsuario.Admin)
        {
            return new ResultadoOperacionDto(false, "Acceso denegado: Solo el rol BODEGA o ADMIN puede despachar pedidos.");
        }

        var pedido = await _pedidoRepo.GetByIdWithDetailsAsync(_pedidoId);
        if (pedido == null)
            return new ResultadoOperacionDto(false, $"El pedido {_pedidoId} no existe.");

        try
        {
            pedido.Despachar(_numeroGuia);
            await _pedidoRepo.UpdateAsync(pedido);

            await _auditoriaRepo.AddAsync(new Auditoria(
                _usuario, "DespacharPedido", "Pedido", pedido.Id.ToString(), $"Pedido {pedido.Codigo} despachado con guía {_numeroGuia}."
            ));

            await _unitOfWork.CommitAsync();
            return new ResultadoOperacionDto(true, $"Pedido {pedido.Codigo} despachado exitosamente con guía {_numeroGuia}.");
        }
        catch (DomainException ex)
        {
            return new ResultadoOperacionDto(false, $"Rechazo: {ex.Message}");
        }
    }
}
