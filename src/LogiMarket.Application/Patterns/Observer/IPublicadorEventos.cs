namespace LogiMarket.Application.Patterns.Observer;

public record EventoPedidoPagado(int PedidoId, string Codigo, decimal Total, string CorreoCliente, string TelefonoCliente);
public record EventoDespachoRegistrado(int PedidoId, string Codigo, string Guia, string CorreoCliente);
public record EventoDevolucionAceptada(int PedidoId, string Codigo, decimal Reembolso, string Motivo);

public interface ISuscriptorEvento<in T>
{
    Task ManejarAsync(T evento);
}

public interface IPublicadorEventos
{
    void Suscribir<T>(ISuscriptorEvento<T> suscriptor);
    Task PublicarAsync<T>(T evento);
}
