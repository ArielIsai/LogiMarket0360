using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.Strategy;

public interface IDescuentoStrategy
{
    string Nombre { get; }
    decimal Porcentaje { get; }
    int Prioridad { get; } // Mayor número = mayor prioridad
    bool Aplica(decimal subtotal, int unidadesFisicasTotales, CategoriaCliente categoria);
    decimal CalcularDescuento(decimal subtotal);
}
