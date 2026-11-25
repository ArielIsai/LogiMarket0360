using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Patterns.Prototype;

public static class PedidoBorradorPrototipo
{
    public static Pedido ClonarProfundo(Pedido plantilla, string nuevoCodigo)
    {
        if (plantilla == null)
            throw new ArgumentNullException(nameof(plantilla));
        if (plantilla.Estado != EstadoPedidoEnum.Borrador)
            throw new DomainException("Solo se pueden clonar como prototipo pedidos en estado BORRADOR.");
        if (string.IsNullOrWhiteSpace(nuevoCodigo))
            throw new DomainException("El nuevo código para el clon es obligatorio.");

        var clon = new Pedido(
            nuevoCodigo,
            plantilla.Cliente,
            plantilla.TipoEntrega,
            plantilla.TarifaEnvio
        )
        {
            Estado = EstadoPedidoEnum.Borrador,
            IdempotencyKey = Guid.NewGuid().ToString("N") // Nueva clave de idempotencia
        };

        // Copia profunda de las líneas y de sus colecciones de componentes anidados
        foreach (var linea in plantilla.Lineas)
        {
            var nuevaLinea = new PedidoDetalle(
                linea.Sku,
                linea.Nombre,
                linea.EsKit,
                linea.Cantidad,
                linea.PrecioUnitario
            );

            foreach (var comp in linea.ComponentesFisicos)
            {
                nuevaLinea.ComponentesFisicos.Add(new PedidoDetalleComponente(
                    comp.SkuFisico,
                    comp.NombreFisico,
                    comp.CantidadFisicaTotal,
                    comp.PrecioUnitarioSnapshot,
                    comp.PesoUnitarioKgSnapshot
                ));
            }

            clon.Lineas.Add(nuevaLinea);
        }

        clon.RecalcularTotales(plantilla.Descuento, plantilla.EstrategiaDescuentoAplicada);
        return clon;
    }
}
