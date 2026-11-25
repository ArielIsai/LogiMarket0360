using LogiMarket.Application.Patterns.Singleton;

namespace LogiMarket.Application.Patterns.Observer;

public class MetricasObserver :
    ISuscriptorEvento<EventoPedidoPagado>,
    ISuscriptorEvento<EventoDespachoRegistrado>,
    ISuscriptorEvento<EventoDevolucionAceptada>
{
    private readonly MetricasSesion _metricas;

    public MetricasObserver(MetricasSesion metricas)
    {
        _metricas = metricas;
    }

    public Task ManejarAsync(EventoPedidoPagado evento)
    {
        _metricas.RegistrarVentaExitosa(evento.Total);
        return Task.CompletedTask;
    }

    public Task ManejarAsync(EventoDespachoRegistrado evento)
    {
        _metricas.RegistrarDespacho();
        return Task.CompletedTask;
    }

    public Task ManejarAsync(EventoDevolucionAceptada evento)
    {
        _metricas.RegistrarDevolucion(evento.Reembolso);
        return Task.CompletedTask;
    }
}
