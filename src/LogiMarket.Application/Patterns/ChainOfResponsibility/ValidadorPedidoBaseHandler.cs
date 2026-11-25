using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public record ResultadoValidacionPaso(bool Valido, string NombrePaso, string? Error = null);

public interface IValidadorPedidoHandler
{
    IValidadorPedidoHandler SetSiguiente(IValidadorPedidoHandler siguiente);
    Task<ResultadoValidacionPaso> ValidarAsync(Pedido pedido);
}

public abstract class ValidadorPedidoBaseHandler : IValidadorPedidoHandler
{
    protected IValidadorPedidoHandler? Siguiente;

    public abstract string NombreEslabon { get; }

    public IValidadorPedidoHandler SetSiguiente(IValidadorPedidoHandler siguiente)
    {
        Siguiente = siguiente;
        return siguiente;
    }

    public async Task<ResultadoValidacionPaso> ValidarAsync(Pedido pedido)
    {
        var resultado = await EjecutarValidacionLocalAsync(pedido);
        if (!resultado.Valido)
        {
            // El primer error detiene la cadena
            return resultado;
        }

        if (Siguiente != null)
        {
            return await Siguiente.ValidarAsync(pedido);
        }

        return new ResultadoValidacionPaso(true, NombreEslabon);
    }

    protected abstract Task<ResultadoValidacionPaso> EjecutarValidacionLocalAsync(Pedido pedido);
}
