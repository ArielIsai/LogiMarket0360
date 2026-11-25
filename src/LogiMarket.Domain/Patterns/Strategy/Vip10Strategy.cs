using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.Strategy;

public class Vip10Strategy : IDescuentoStrategy
{
    public string Nombre => "VIP (10%)";
    public decimal Porcentaje => 0.10m;
    public int Prioridad => 50;

    public bool Aplica(decimal subtotal, int unidadesFisicasTotales, CategoriaCliente categoria)
    {
        return categoria == CategoriaCliente.Vip;
    }

    public decimal CalcularDescuento(decimal subtotal)
    {
        return Math.Round(subtotal * Porcentaje, 2, MidpointRounding.AwayFromZero);
    }
}
