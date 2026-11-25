namespace LogiMarket.Infrastructure.Patterns.Flyweight;

// Estado Intrínseco (Compartido e Inmutable)
public class MetadatosSkuFlyweight
{
    public string Sku { get; }
    public string Nombre { get; }
    public string Categoria { get; }
    public string DescripcionTecnica { get; }

    public MetadatosSkuFlyweight(string sku, string nombre, string categoria, string descripcionTecnica)
    {
        Sku = sku;
        Nombre = nombre;
        Categoria = categoria;
        DescripcionTecnica = descripcionTecnica;
    }
}

// Estado Extrínseco (Varía por fila de DataGridView o consulta)
public class RenglonVisualCatalogo
{
    // Referencia al Flyweight compartido (Estado Intrínseco)
    public MetadatosSkuFlyweight Metadatos { get; }

    // Datos Extrínsecos independientes por fila
    public int FilaIndex { get; set; }
    public int StockDisponible { get; set; }
    public int CantidadSeleccionada { get; set; }
    public decimal PrecioVenta { get; set; }

    public RenglonVisualCatalogo(MetadatosSkuFlyweight metadatos, int filaIndex, int stockDisponible, int cantidadSeleccionada, decimal precioVenta)
    {
        Metadatos = metadatos;
        FilaIndex = filaIndex;
        StockDisponible = stockDisponible;
        CantidadSeleccionada = cantidadSeleccionada;
        PrecioVenta = precioVenta;
    }
}

// Fábrica Flyweight
public class CatalogoVisualFlyweightFactory
{
    private readonly Dictionary<string, MetadatosSkuFlyweight> _flyweights = new();

    public int TotalInstanciasCreadas => _flyweights.Count;

    public MetadatosSkuFlyweight ObtenerMetadatos(string sku, string nombre, string categoria, string descripcion)
    {
        sku = sku.Trim().ToUpperInvariant();
        if (!_flyweights.TryGetValue(sku, out var flyweight))
        {
            flyweight = new MetadatosSkuFlyweight(sku, nombre, categoria, descripcion);
            _flyweights[sku] = flyweight;
        }
        return flyweight;
    }

    public List<RenglonVisualCatalogo> GenerarRenglonesDemostracion(int cantidadFilas = 1000)
    {
        var skus = new[]
        {
            ("P01", "Mouse USB", "Periféricos", "Sensor óptico 1600 DPI con cable trenzado"),
            ("P02", "Teclado", "Periféricos", "Mecánico con switches táctiles"),
            ("P03", "Memoria USB", "Almacenamiento", "USB 3.2 Gen 1 de 64 GB"),
            ("P04", "Webcam", "Video", "Resolución 1080p con micrófono integrado"),
            ("P05", "Alfombrilla", "Accesorios", "Superficie de microfibra antiderrapante")
        };

        var lista = new List<RenglonVisualCatalogo>(cantidadFilas);
        var rnd = new Random(42);

        for (int i = 0; i < cantidadFilas; i++)
        {
            var (sku, nombre, cat, desc) = skus[i % skus.Length];
            var metadatos = ObtenerMetadatos(sku, nombre, cat, desc);

            lista.Add(new RenglonVisualCatalogo(
                metadatos,
                filaIndex: i + 1,
                stockDisponible: rnd.Next(1, 50),
                cantidadSeleccionada: rnd.Next(1, 5),
                precioVenta: (decimal)rnd.Next(5, 100)
            ));
        }

        return lista;
    }
}
