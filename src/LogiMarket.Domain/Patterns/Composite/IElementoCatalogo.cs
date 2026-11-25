using LogiMarket.Domain.Patterns.Visitor;

namespace LogiMarket.Domain.Patterns.Composite;

public interface IElementoCatalogo
{
    string Sku { get; }
    string Nombre { get; }
    bool EsKit { get; }
    decimal ObtenerPrecio();
    decimal ObtenerPeso();
    IReadOnlyList<(string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)> ObtenerDesgloseFisico(int multiplicador = 1);
    void Aceptar(ICatalogoVisitor visitor, int cantidad = 1);
}
