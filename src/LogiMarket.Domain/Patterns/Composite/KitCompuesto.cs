using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Visitor;

namespace LogiMarket.Domain.Patterns.Composite;

public class KitCompuesto : IElementoCatalogo
{
    private readonly List<(IElementoCatalogo Elemento, int Cantidad)> _componentes = new();

    public string Sku { get; }
    public string Nombre { get; }
    public bool EsKit => true;

    public IReadOnlyList<(IElementoCatalogo Elemento, int Cantidad)> Componentes => _componentes.AsReadOnly();

    public KitCompuesto(string sku, string nombre)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU o código del kit es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del kit es obligatorio.");

        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
    }

    public void AgregarComponente(IElementoCatalogo elemento, int cantidad = 1)
    {
        if (elemento == null)
            throw new ArgumentNullException(nameof(elemento));
        if (cantidad <= 0)
            throw new DomainException($"La cantidad del componente en el kit debe ser mayor a 0. Recibido: {cantidad}");

        // Verificación anti-ciclo estricta
        if (ContieneReferenciaCircular(elemento, Sku))
        {
            throw new DomainException($"Referencia circular detectada: El kit '{Sku}' no puede contenerse a sí mismo directa o indirectamente.");
        }

        // Si ya existe en este nivel, acumular cantidad
        var index = _componentes.FindIndex(c => c.Elemento.Sku == elemento.Sku);
        if (index >= 0)
        {
            var actual = _componentes[index];
            _componentes[index] = (actual.Elemento, actual.Cantidad + cantidad);
        }
        else
        {
            _componentes.Add((elemento, cantidad));
        }
    }

    private static bool ContieneReferenciaCircular(IElementoCatalogo candidato, string skuObjetivo)
    {
        if (candidato.Sku.Equals(skuObjetivo, StringComparison.OrdinalIgnoreCase))
            return true;

        if (candidato is KitCompuesto kitHijo)
        {
            foreach (var comp in kitHijo.Componentes)
            {
                if (ContieneReferenciaCircular(comp.Elemento, skuObjetivo))
                    return true;
            }
        }

        return false;
    }

    public decimal ObtenerPrecio()
    {
        return _componentes.Sum(c => c.Elemento.ObtenerPrecio() * c.Cantidad);
    }

    public decimal ObtenerPeso()
    {
        return _componentes.Sum(c => c.Elemento.ObtenerPeso() * c.Cantidad);
    }

    public IReadOnlyList<(string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)> ObtenerDesgloseFisico(int multiplicador = 1)
    {
        if (multiplicador <= 0)
            throw new DomainException($"El multiplicador debe ser mayor a 0. Recibido: {multiplicador}");

        var diccionario = new Dictionary<string, (string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)>();

        foreach (var (elemento, cantidad) in _componentes)
        {
            var desgloseHijo = elemento.ObtenerDesgloseFisico(cantidad * multiplicador);
            foreach (var item in desgloseHijo)
            {
                if (diccionario.TryGetValue(item.Sku, out var actual))
                {
                    diccionario[item.Sku] = (item.Nombre, actual.Cantidad + item.Cantidad, item.PrecioUnitario, item.PesoUnitario);
                }
                else
                {
                    diccionario[item.Sku] = (item.Nombre, item.Cantidad, item.PrecioUnitario, item.PesoUnitario);
                }
            }
        }

        return diccionario.Select(kvp => (kvp.Key, kvp.Value.Nombre, kvp.Value.Cantidad, kvp.Value.PrecioUnitario, kvp.Value.PesoUnitario)).ToList();
    }

    public void Aceptar(ICatalogoVisitor visitor, int cantidad = 1)
    {
        visitor.VisitarKit(this, cantidad);
    }
}
