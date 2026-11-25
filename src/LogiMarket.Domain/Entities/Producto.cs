using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public decimal PesoKg { get; set; }
    public bool Activo { get; set; } = true;

    public Producto() { }

    public Producto(string sku, string nombre, string categoria, decimal precio, int stock, decimal pesoKg)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU del producto es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del producto es obligatorio.");
        if (precio <= 0)
            throw new DomainException($"El precio debe ser un valor monetario positivo. Valor recibido: {precio}");
        if (stock < 0)
            throw new DomainException($"El stock no puede ser negativo. Valor recibido: {stock}");
        if (pesoKg < 0)
            throw new DomainException($"El peso no puede ser negativo. Valor recibido: {pesoKg}");

        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Categoria = categoria.Trim();
        Precio = precio;
        Stock = stock;
        PesoKg = pesoKg;
    }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException($"La cantidad a descontar debe ser mayor que cero. Recibido: {cantidad}");
        if (Stock < cantidad)
            throw new DomainException($"Stock insuficiente para el producto {Sku} ({Nombre}). Stock disponible: {Stock}, solicitado: {cantidad}");

        Stock -= cantidad;
    }

    public void ReintegrarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException($"La cantidad a reintegrar debe ser mayor que cero. Recibido: {cantidad}");

        Stock += cantidad;
    }
}
