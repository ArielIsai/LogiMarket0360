using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Ports;

namespace LogiMarket.Application.Patterns.Observer;

public class NotificacionObserver :
    ISuscriptorEvento<EventoPedidoPagado>,
    ISuscriptorEvento<EventoDespachoRegistrado>
{
    private INotificadorCreator _creator;

    public NotificacionObserver(INotificadorCreator creator)
    {
        _creator = creator;
    }

    public void CambiarCanalNotificacion(INotificadorCreator nuevoCreator)
    {
        _creator = nuevoCreator;
    }

    public async Task ManejarAsync(EventoPedidoPagado evento)
    {
        var notificador = _creator.CrearNotificador();
        var destinatario = notificador.Tipo == Domain.Enums.TipoAvisoEnum.Email ? evento.CorreoCliente : evento.TelefonoCliente;
        await notificador.EnviarAvisoAsync(
            destinatario,
            $"Su pedido {evento.Codigo} por USD {evento.Total:F2} ha sido cobrado con éxito.",
            "Confirmación de Pedido"
        );
    }

    public async Task ManejarAsync(EventoDespachoRegistrado evento)
    {
        var notificador = _creator.CrearNotificador();
        await notificador.EnviarAvisoAsync(
            evento.CorreoCliente,
            $"Su pedido {evento.Codigo} va en camino con guía {evento.Guia}.",
            "Despacho de Pedido"
        );
    }
}
