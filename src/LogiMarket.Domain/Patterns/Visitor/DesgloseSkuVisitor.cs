using LogiMarket.Domain.Patterns.Composite;

namespace LogiMarket.Domain.Patterns.Visitor;

public class DesgloseSkuVisitor : ICatalogoVisitor
{
    private readonly Dictionary<string, int> _conteoPorSku = new();

    public IReadOnlyDictionary<string, int> ConteoPorSku => _conteoPorSku;

    public void VisitarProducto(ProductoSimple producto, int cantidad = 1)
    {
        if (_conteoPorSku.ContainsKey(producto.Sku))
            _conteoPorSku[producto.Sku] += cantidad;
        else
            _conteoPorSku[producto.Sku] = cantidad;
    }

    public void VisitarKit(KitCompuesto kit, int cantidad = 1)
    {
        foreach (var (elemento, cantHijo) in kit.Componentes)
        {
            elemento.Aceptar(this, cantHijo * cantidad);
        }
    }
}
