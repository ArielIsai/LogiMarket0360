namespace LogiMarket.Domain.Patterns.Iterator;

public interface IKitComponenteIterator
{
    bool HasNext();
    (string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario) Next();
    void Reset();
}
