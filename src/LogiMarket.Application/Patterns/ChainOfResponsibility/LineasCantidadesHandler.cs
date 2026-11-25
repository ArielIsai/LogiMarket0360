using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Patterns.ChainOfResponsibility;

public class LineasCantidadesHandler : ValidadorPedidoBaseHandler
{
    public override string NombreEslabon => "2. Validación de Líneas y Cantidades";

    protected override Task<ResultadoValidacionPaso> EjecutarValidacionLocalAsync(Pedido pedido)
    {
        if (pedido.Lineas == null || pedido.Lineas.Count == 0)
        {
            return Task.FromResult(new ResultadoValidacionPaso(
                false, NombreEslabon, "El pedido no contiene ninguna línea de producto o kit."));
        }

        foreach (var linea in pedido.Lineas)
        {
            if (linea.Cantidad <= 0)
            {
                return Task.FromResult(new ResultadoValidacionPaso(
                    false, NombreEslabon, $"La línea '{linea.Sku} - {linea.Nombre}' tiene una cantidad no válida ({linea.Cantidad}). Debe ser mayor a 0."));
            }

            if (linea.PrecioUnitario <= 0)
            {
                return Task.FromResult(new ResultadoValidacionPaso(
                    false, NombreEslabon, $"La línea '{linea.Sku}' tiene un precio unitario no válido ({linea.PrecioUnitario})."));
            }
        }

        return Task.FromResult(new ResultadoValidacionPaso(true, NombreEslabon));
    }
}
