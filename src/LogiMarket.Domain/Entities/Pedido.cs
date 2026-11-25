using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.State;

namespace LogiMarket.Domain.Entities;

public class Pedido
{
    private IPedidoEstado _estadoActual = null!;

    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaPago { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public DateTime? FechaEntrega { get; set; }

    private EstadoPedidoEnum _estado = EstadoPedidoEnum.Borrador;
    public EstadoPedidoEnum Estado
    {
        get => _estado;
        set
        {
            _estado = value;
            ActualizarObjetoEstado();
        }
    }
    public TipoEntregaEnum TipoEntrega { get; set; } = TipoEntregaEnum.Estandar;
    public string GuiaDespacho { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal TarifaEnvio { get; set; }
    public decimal Total { get; set; }
    public string EstrategiaDescuentoAplicada { get; set; } = string.Empty;

    public List<PedidoDetalle> Lineas { get; set; } = new();

    public Pedido()
    {
        ActualizarObjetoEstado();
    }

    public Pedido(string codigo, Cliente cliente, TipoEntregaEnum tipoEntrega, decimal tarifaEnvio)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("El código del pedido es obligatorio.");
        if (cliente == null)
            throw new DomainException("El cliente es obligatorio para el pedido.");

        Codigo = codigo.Trim().ToUpperInvariant();
        Cliente = cliente;
        ClienteId = cliente.Id;
        TipoEntrega = tipoEntrega;
        TarifaEnvio = tarifaEnvio;
        Estado = EstadoPedidoEnum.Borrador;
        ActualizarObjetoEstado();
    }

    public void ActualizarObjetoEstado()
    {
        _estadoActual = Estado switch
        {
            EstadoPedidoEnum.Borrador => new BorradorEstado(),
            EstadoPedidoEnum.Validado => new ValidadoEstado(),
            EstadoPedidoEnum.Pagado => new PagadoEstado(),
            EstadoPedidoEnum.Despachado => new DespachadoEstado(),
            EstadoPedidoEnum.Entregado => new EntregadoEstado(),
            EstadoPedidoEnum.Cancelado => new CanceladoEstado(),
            _ => new BorradorEstado()
        };
    }

    public void TransicionarEstado(IPedidoEstado nuevoEstado)
    {
        _estadoActual = nuevoEstado;
        Estado = nuevoEstado.Estado;
    }

    public void Validar()
    {
        _estadoActual.Validar(this);
    }

    public void Pagar()
    {
        _estadoActual.Pagar(this);
        FechaPago = DateTime.UtcNow;
    }

    public void Despachar(string numeroGuia)
    {
        if (string.IsNullOrWhiteSpace(numeroGuia))
            throw new DomainException("El número de guía es obligatorio para despachar el pedido.");

        _estadoActual.Despachar(this);
        GuiaDespacho = numeroGuia.Trim();
        FechaDespacho = DateTime.UtcNow;
    }

    public void Entregar()
    {
        _estadoActual.Entregar(this);
        FechaEntrega = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        _estadoActual.Cancelar(this);
    }

    public int ObtenerTotalUnidadesFisicas()
    {
        return Lineas
            .SelectMany(l => l.ComponentesFisicos)
            .Sum(c => c.CantidadFisicaTotal);
    }

    public Dictionary<string, int> ObtenerDesgloseFisicoTotal()
    {
        var dict = new Dictionary<string, int>();
        foreach (var linea in Lineas)
        {
            foreach (var comp in linea.ComponentesFisicos)
            {
                if (dict.ContainsKey(comp.SkuFisico))
                    dict[comp.SkuFisico] += comp.CantidadFisicaTotal;
                else
                    dict[comp.SkuFisico] = comp.CantidadFisicaTotal;
            }
        }
        return dict;
    }

    public void RecalcularTotales(decimal descuento, string nombreEstrategia)
    {
        Subtotal = Math.Round(Lineas.Sum(l => l.Subtotal), 2, MidpointRounding.AwayFromZero);
        Descuento = Math.Round(descuento, 2, MidpointRounding.AwayFromZero);
        EstrategiaDescuentoAplicada = nombreEstrategia;
        Total = Math.Round(Subtotal - Descuento + TarifaEnvio, 2, MidpointRounding.AwayFromZero);
    }
}
