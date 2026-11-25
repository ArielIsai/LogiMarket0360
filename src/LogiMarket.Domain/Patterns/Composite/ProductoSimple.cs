using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Visitor;

namespace LogiMarket.Domain.Patterns.Composite;

public class ProductoSimple : IElementoCatalogo
{
    public string Sku { get; }
    public string Nombre { get; }
    public decimal Precio { get; }
    public decimal PesoKg { get; }
    public bool EsKit => false;

    public ProductoSimple(string sku, string nombre, decimal precio, decimal pesoKg)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio.");
        if (precio <= 0)
            throw new DomainException($"El precio debe ser positivo. Recibido: {precio}");
        if (pesoKg < 0)
            throw new DomainException($"El peso no puede ser negativo. Recibido: {pesoKg}");

        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Precio = precio;
        PesoKg = pesoKg;
    }

    public ProductoSimple(Producto producto)
        : this(producto.Sku, producto.Nombre, producto.Precio, producto.PesoKg)
    {
    }

    public decimal ObtenerPrecio() => Precio;

    public decimal ObtenerPeso() => PesoKg;

    public IReadOnlyList<(string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)> ObtenerDesgloseFisico(int multiplicador = 1)
    {
        if (multiplicador <= 0)
            throw new DomainException($"El multiplicador debe ser mayor a 0. Recibido: {multiplicador}");

        return new List<(string Sku, string Nombre, int Cantidad, decimal PrecioUnitario, decimal PesoUnitario)>
        {
            (Sku, Nombre, multiplicador, Precio, PesoKg)
        };
    }

    public void Aceptar(ICatalogoVisitor visitor, int cantidad = 1)
    {
        visitor.VisitarProducto(this, cantidad);
    }
}
