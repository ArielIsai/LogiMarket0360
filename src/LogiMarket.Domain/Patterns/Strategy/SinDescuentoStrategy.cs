using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.Strategy;

public class SinDescuentoStrategy : IDescuentoStrategy
{
    public string Nombre => "NORMAL (0%)";
    public decimal Porcentaje => 0.00m;
    public int Prioridad => 0; // Mínima prioridad (default)

    public bool Aplica(decimal subtotal, int unidadesFisicasTotales, CategoriaCliente categoria)
    {
        return true;
    }

    public decimal CalcularDescuento(decimal subtotal)
    {
        return 0.00m;
    }
}
