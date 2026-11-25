using LogiMarket.Domain.Patterns.Composite;

namespace LogiMarket.Domain.Patterns.Iterator;

public class KitFisicoIterator : IKitComponenteIterator
{
    private readonly List<(string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)> _elementos;
    private int _posicion = 0;

    public KitFisicoIterator(IElementoCatalogo elementoRaiz, int multiplicador = 1)
    {
        _elementos = elementoRaiz.ObtenerDesgloseFisico(multiplicador)
            .OrderBy(e => e.Sku)
            .ToList();
    }

    public bool HasNext()
    {
        return _posicion < _elementos.Count;
    }

    public (string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario) Next()
    {
        if (!HasNext())
            throw new InvalidOperationException("No hay más componentes en la iteración.");

        return _elementos[_posicion++];
    }

    public void Reset()
    {
        _posicion = 0;
    }
}
