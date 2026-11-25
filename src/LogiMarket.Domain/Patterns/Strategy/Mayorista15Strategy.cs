using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.Strategy;

public class Mayorista15Strategy : IDescuentoStrategy
{
    public string Nombre => "MAYORISTA (15%)";
    public decimal Porcentaje => 0.15m;
    public int Prioridad => 100; // Máxima prioridad si aplica

    public bool Aplica(decimal subtotal, int unidadesFisicasTotales, CategoriaCliente categoria)
    {
        return unidadesFisicasTotales >= 10;
    }

    public decimal CalcularDescuento(decimal subtotal)
    {
        return Math.Round(subtotal * Porcentaje, 2, MidpointRounding.AwayFromZero);
    }
}
