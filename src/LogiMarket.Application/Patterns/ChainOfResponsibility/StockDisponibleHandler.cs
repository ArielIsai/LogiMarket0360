using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public class StockDisponibleHandler : ValidadorPedidoBaseHandler
{
    private readonly IProductoRepository _productoRepository;

    public override string NombreEslabon => "3. Validación de Existencias (Stock)";

    public StockDisponibleHandler(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    protected override async Task<ResultadoValidacionPaso> EjecutarValidacionLocalAsync(Pedido pedido)
    {
        // Obtener el consolidado físico de todos los SKU requeridos (expandiendo kits)
        var desgloseFisico = pedido.ObtenerDesgloseFisicoTotal();

        foreach (var (sku, cantidadRequerida) in desgloseFisico)
        {
            var producto = await _productoRepository.GetBySkuAsync(sku);
            if (producto == null)
            {
                return new ResultadoValidacionPaso(
                    false, NombreEslabon, $"El producto físico con SKU '{sku}' requerido por el pedido no existe en el catálogo.");
            }

            if (!producto.Activo)
            {
                return new ResultadoValidacionPaso(
                    false, NombreEslabon, $"El producto '{sku} - {producto.Nombre}' está deshabilitado en el catálogo.");
            }

            if (producto.Stock < cantidadRequerida)
            {
                return new ResultadoValidacionPaso(
                    false,
                    NombreEslabon,
                    $"Existencias insuficientes para '{sku} - {producto.Nombre}'. Se requieren {cantidadRequerida} unidad(es), pero solo existen {producto.Stock} en inventario.");
            }
        }

        return new ResultadoValidacionPaso(true, NombreEslabon);
    }
}
