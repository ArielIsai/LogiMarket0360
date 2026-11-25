namespace LogiMarket.Domain.Patterns.Visitor;

using LogiMarket.Domain.Patterns.Composite;

public interface ICatalogoVisitor
{
    void VisitarProducto(ProductoSimple producto, int cantidad = 1);
    void VisitarKit(KitCompuesto kit, int cantidad = 1);
}
