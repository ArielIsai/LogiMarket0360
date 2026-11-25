using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Visitor;

namespace LogiMarket.Application.Services;

public class CatalogoService
{
    private readonly IProductoRepository _productoRepo;
    private static readonly Dictionary<string, KitCompuesto> _kitsEnMemoria = new();

    public CatalogoService(IProductoRepository productoRepo)
    {
        _productoRepo = productoRepo;
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarProductosAsync()
    {
        var productos = await _productoRepo.GetAllAsync();
        return productos.Select(p => new ProductoDto(
            p.Id, p.Sku, p.Nombre, p.Categoria, p.Precio, p.Stock, p.PesoKg, p.Activo
        )).ToList();
    }

    public async Task<IElementoCatalogo?> ObtenerElementoCatalogoAsync(string sku)
    {
        sku = sku.Trim().ToUpperInvariant();

        if (_kitsEnMemoria.Count == 0)
        {
            await InicializarKitsOficialesAsync();
        }

        // Si es un kit registrado
        if (_kitsEnMemoria.TryGetValue(sku, out var kit))
            return kit;

        // Si es un producto de catálogo
        var producto = await _productoRepo.GetBySkuAsync(sku);
        if (producto != null)
            return new ProductoSimple(producto);

        return null;
    }

    public void RegistrarKit(KitCompuesto kit)
    {
        _kitsEnMemoria[kit.Sku] = kit;
    }

    public IReadOnlyList<KitCompuesto> ListarKits()
    {
        if (_kitsEnMemoria.Count == 0)
        {
            InicializarKitsOficialesAsync().GetAwaiter().GetResult();
        }
        return _kitsEnMemoria.Values.ToList();
    }

    public async Task InicializarKitsOficialesAsync()
    {
        var p01 = await _productoRepo.GetBySkuAsync("P01");
        var p02 = await _productoRepo.GetBySkuAsync("P02");
        var p03 = await _productoRepo.GetBySkuAsync("P03");

        if (p01 != null && p02 != null)
        {
            // KIT_OFICINA = P01 ($20) + P02 ($30) = $50.00
            var kitOficina = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
            kitOficina.AgregarComponente(new ProductoSimple(p01), 1);
            kitOficina.AgregarComponente(new ProductoSimple(p02), 1);
            RegistrarKit(kitOficina);

            if (p03 != null)
            {
                // KIT_TELETRABAJO = KIT_OFICINA ($50) + P03 ($10) = $60.00
                var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
                kitTeletrabajo.AgregarComponente(kitOficina, 1);
                kitTeletrabajo.AgregarComponente(new ProductoSimple(p03), 1);
                RegistrarKit(kitTeletrabajo);
            }
        }
    }
}
