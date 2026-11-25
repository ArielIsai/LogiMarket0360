using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Strategy;

namespace LogiMarket.Domain.Patterns.Builder;

public class PedidoBuilder
{
    private string? _codigo;
    private Cliente? _cliente;
    private TipoEntregaEnum _tipoEntrega = TipoEntregaEnum.Estandar;
    private decimal _tarifaEnvio = 5.00m;
    private readonly List<(IElementoCatalogo Elemento, int Cantidad)> _lineas = new();
    private readonly CalculadoraDescuentoContext _calculadoraDescuento = new();

    public PedidoBuilder ConCodigo(string codigo)
    {
        _codigo = codigo;
        return this;
    }

    public PedidoBuilder ConCliente(Cliente cliente)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        return this;
    }

    public PedidoBuilder ConEntrega(TipoEntregaEnum tipoEntrega, decimal tarifa)
    {
        _tipoEntrega = tipoEntrega;
        _tarifaEnvio = tarifa;
        return this;
    }

    public PedidoBuilder AgregarLinea(IElementoCatalogo elemento, int cantidad = 1)
    {
        if (elemento == null)
            throw new ArgumentNullException(nameof(elemento));
        if (cantidad <= 0)
            throw new DomainException($"La cantidad para la línea debe ser mayor a 0. Recibido: {cantidad}");

        _lineas.Add((elemento, cantidad));
        return this;
    }

    public Pedido Build()
    {
        if (string.IsNullOrWhiteSpace(_codigo))
            throw new DomainException("No se puede construir el pedido sin un código identificador.");
        if (_cliente == null)
            throw new DomainException("No se puede construir el pedido sin asignar un cliente.");
        if (_lineas.Count == 0)
            throw new DomainException("No se puede construir el pedido sin al menos una línea de producto o kit.");

        var pedido = new Pedido(_codigo, _cliente, _tipoEntrega, _tarifaEnvio);

        foreach (var (elemento, cantidad) in _lineas)
        {
            var precioUnitario = elemento.ObtenerPrecio();
            var linea = new PedidoDetalle(elemento.Sku, elemento.Nombre, elemento.EsKit, cantidad, precioUnitario);

            // Obtener desglose de componentes físicos instantáneos
            var desgloseFisico = elemento.ObtenerDesgloseFisico(cantidad);
            foreach (var item in desgloseFisico)
            {
                linea.ComponentesFisicos.Add(new PedidoDetalleComponente(
                    item.Sku,
                    item.Nombre,
                    item.Cantidad,
                    item.PrecioUnitario,
                    item.PesoUnitario
                ));
            }

            pedido.Lineas.Add(linea);
        }

        // Calcular descuentos según reglas del negocio
        var unidadesTotales = pedido.ObtenerTotalUnidadesFisicas();
        var subtotal = pedido.Lineas.Sum(l => l.Subtotal);
        var (estrategia, descuento) = _calculadoraDescuento.EvaluarDescuento(subtotal, unidadesTotales, _cliente.Categoria);

        pedido.RecalcularTotales(descuento, estrategia.Nombre);
        return pedido;
    }
}
