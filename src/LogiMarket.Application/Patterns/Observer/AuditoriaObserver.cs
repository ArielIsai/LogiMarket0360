using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.Observer;

public class AuditoriaObserver :
    ISuscriptorEvento<EventoPedidoPagado>,
    ISuscriptorEvento<EventoDespachoRegistrado>,
    ISuscriptorEvento<EventoDevolucionAceptada>
{
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AuditoriaObserver(IAuditoriaRepository auditoriaRepo, IUnitOfWork unitOfWork)
    {
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task ManejarAsync(EventoPedidoPagado evento)
    {
        await _auditoriaRepo.AddAsync(new Auditoria(
            "SistemaEventos", "EventoPedidoPagado", "Pedido", evento.PedidoId.ToString(), $"Notificación de pago exitoso para pedido {evento.Codigo} por USD {evento.Total:F2}."
        ));
        await _unitOfWork.CommitAsync();
    }

    public async Task ManejarAsync(EventoDespachoRegistrado evento)
    {
        await _auditoriaRepo.AddAsync(new Auditoria(
            "SistemaEventos", "EventoDespachoRegistrado", "Pedido", evento.PedidoId.ToString(), $"Notificación de despacho para pedido {evento.Codigo} con guía {evento.Guia}."
        ));
        await _unitOfWork.CommitAsync();
    }

    public async Task ManejarAsync(EventoDevolucionAceptada evento)
    {
        await _auditoriaRepo.AddAsync(new Auditoria(
            "SistemaEventos", "EventoDevolucionAceptada", "Pedido", evento.PedidoId.ToString(), $"Notificación de devolución aceptada para pedido {evento.Codigo}. Reembolso: USD {evento.Reembolso:F2}."
        ));
        await _unitOfWork.CommitAsync();
    }
}
