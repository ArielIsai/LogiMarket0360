using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.Strategy;

public class CalculadoraDescuentoContext
{
    private readonly List<IDescuentoStrategy> _estrategias;

    public CalculadoraDescuentoContext(IEnumerable<IDescuentoStrategy>? estrategias = null)
    {
        _estrategias = estrategias?.ToList() ?? new List<IDescuentoStrategy>
        {
            new Mayorista15Strategy(),
            new Vip10Strategy(),
            new SinDescuentoStrategy()
        };
    }

    public void RegistrarEstrategia(IDescuentoStrategy estrategia)
    {
        _estrategias.Add(estrategia);
    }

    public (IDescuentoStrategy EstrategiaSeleccionada, decimal MontoDescuento) EvaluarDescuento(
        decimal subtotal,
        int unidadesFisicasTotales,
        CategoriaCliente categoria)
    {
        // Ordenar por prioridad descendente y tomar la primera que aplique
        var seleccionada = _estrategias
            .Where(e => e.Aplica(subtotal, unidadesFisicasTotales, categoria))
            .OrderByDescending(e => e.Prioridad)
            .FirstOrDefault() ?? new SinDescuentoStrategy();

        var descuento = seleccionada.CalcularDescuento(subtotal);
        return (seleccionada, descuento);
    }
}
