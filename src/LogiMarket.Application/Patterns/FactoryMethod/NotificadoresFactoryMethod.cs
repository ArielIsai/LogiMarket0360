using LogiMarket.Application.Ports;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Patterns.FactoryMethod;

public interface INotificadorCreator
{
    TipoAvisoEnum Tipo { get; }
    INotificador CrearNotificador();
}

public class EmailNotificador : INotificador
{
    private readonly List<string> _bandejaSalida = new();
    public TipoAvisoEnum Tipo => TipoAvisoEnum.Email;
    public IReadOnlyList<string> BandejaSalida => _bandejaSalida;

    public Task EnviarAvisoAsync(string destinatario, string mensaje, string asunto)
    {
        var registro = $"[EMAIL a {destinatario}] Asunto: {asunto} | Cuerpo: {mensaje} (Enviado: {DateTime.UtcNow:HH:mm:ss})";
        _bandejaSalida.Add(registro);
        return Task.CompletedTask;
    }
}

public class EmailNotificadorCreator : INotificadorCreator
{
    public TipoAvisoEnum Tipo => TipoAvisoEnum.Email;

    public INotificador CrearNotificador()
    {
        return new EmailNotificador();
    }
}

public class SmsNotificador : INotificador
{
    private readonly List<string> _bandejaSalida = new();
    public TipoAvisoEnum Tipo => TipoAvisoEnum.Sms;
    public IReadOnlyList<string> BandejaSalida => _bandejaSalida;

    public Task EnviarAvisoAsync(string destinatario, string mensaje, string asunto)
    {
        var registro = $"[SMS a {destinatario}] {asunto.ToUpperInvariant()}: {mensaje} ({DateTime.UtcNow:HH:mm:ss})";
        _bandejaSalida.Add(registro);
        return Task.CompletedTask;
    }
}

public class SmsNotificadorCreator : INotificadorCreator
{
    public TipoAvisoEnum Tipo => TipoAvisoEnum.Sms;

    public INotificador CrearNotificador()
    {
        return new SmsNotificador();
    }
}
