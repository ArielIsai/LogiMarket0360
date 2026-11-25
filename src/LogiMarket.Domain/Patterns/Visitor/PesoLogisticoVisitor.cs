using LogiMarket.Domain.Patterns.Composite;

namespace LogiMarket.Domain.Patterns.Visitor;

public class PesoLogisticoVisitor : ICatalogoVisitor
{
    public decimal PesoTotalKg { get; private set; } = 0m;

    public void VisitarProducto(ProductoSimple producto, int cantidad = 1)
    {
        PesoTotalKg += producto.PesoKg * cantidad;
    }

    public void VisitarKit(KitCompuesto kit, int cantidad = 1)
    {
        foreach (var (elemento, cantHijo) in kit.Componentes)
        {
            elemento.Aceptar(this, cantHijo * cantidad);
        }
    }

    public void Reset()
    {
        PesoTotalKg = 0m;
    }
}
