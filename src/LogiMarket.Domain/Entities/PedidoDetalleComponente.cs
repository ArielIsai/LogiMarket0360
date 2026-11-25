namespace LogiMarket.Domain.Entities;

public class PedidoDetalleComponente
{
    public int Id { get; set; }
    public int PedidoDetalleId { get; set; }
    public string SkuFisico { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public int CantidadFisicaTotal { get; set; }
    public decimal PrecioUnitarioSnapshot { get; set; }
    public decimal PesoUnitarioKgSnapshot { get; set; }

    public PedidoDetalleComponente() { }

    public PedidoDetalleComponente(string skuFisico, string nombreFisico, int cantidadFisicaTotal, decimal precioUnitario, decimal pesoUnitarioKg)
    {
        SkuFisico = skuFisico;
        NombreFisico = nombreFisico;
        CantidadFisicaTotal = cantidadFisicaTotal;
        PrecioUnitarioSnapshot = precioUnitario;
        PesoUnitarioKgSnapshot = pesoUnitarioKg;
    }
}
