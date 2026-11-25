using LogiMarket.Application.DTOs;

namespace LogiMarket.Application.Patterns.Command;

public interface ICommand
{
    string Nombre { get; }
    string ParametrosResumen { get; }
    Task<ResultadoOperacionDto> ExecuteAsync();
}

public record RegistroComando(
    string NombreComando,
    string Parametros,
    bool Exitoso,
    string MensajeResultado,
    DateTime Timestamp
);

public class CommandInvoker
{
    private readonly List<RegistroComando> _historial = new();

    public IReadOnlyList<RegistroComando> Historial => _historial.AsReadOnly();

    public async Task<ResultadoOperacionDto> EjecutarComandoAsync(ICommand comando)
    {
        var resultado = await comando.ExecuteAsync();

        _historial.Add(new RegistroComando(
            comando.Nombre,
            comando.ParametrosResumen,
            resultado.Exitoso,
            resultado.Mensaje,
            DateTime.UtcNow
        ));

        return resultado;
    }
}
