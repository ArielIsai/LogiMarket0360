using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Patterns.TemplateMethod;

namespace LogiMarket.Application.Patterns.Command;

public class ProcesarDevolucionCommand : ICommand
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IDevolucionRepository _devolucionRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly int _pedidoId;
    private readonly MotivoDevolucionEnum _motivo;
    private readonly int _diasDesdeEntrega;
    private readonly bool _productoSinUso;
    private readonly string _usuario;

    public string Nombre => "ProcesarDevolucionCommand";
    public string ParametrosResumen => $"PedidoId: {_pedidoId}, Motivo: {_motivo}, Dias: {_diasDesdeEntrega}, SinUso: {_productoSinUso}";

    public ProcesarDevolucionCommand(
        IPedidoRepository pedidoRepo,
        IDevolucionRepository devolucionRepo,
        IProductoRepository productoRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IAuditoriaRepository auditoriaRepo,
        IUnitOfWork unitOfWork,
        int pedidoId,
        MotivoDevolucionEnum motivo,
        int diasDesdeEntrega,
        bool productoSinUso,
        string usuario)
    {
        _pedidoRepo = pedidoRepo;
        _devolucionRepo = devolucionRepo;
        _productoRepo = productoRepo;
        _movimientoRepo = movimientoRepo;
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
        _pedidoId = pedidoId;
        _motivo = motivo;
        _diasDesdeEntrega = diasDesdeEntrega;
        _productoSinUso = productoSinUso;
        _usuario = usuario;
    }

    public async Task<ResultadoOperacionDto> ExecuteAsync()
    {
        var pedido = await _pedidoRepo.GetByIdWithDetailsAsync(_pedidoId);
        if (pedido == null)
            return new ResultadoOperacionDto(false, $"El pedido {_pedidoId} no existe.");

        var devolucionPrevia = await _devolucionRepo.GetByPedidoIdAsync(_pedidoId);
        bool yaFueDevuelto = devolucionPrevia != null;

        // Seleccionar la plantilla Template Method adecuada
        ProcesarDevolucionTemplate template = _motivo == MotivoDevolucionEnum.Defecto
            ? new DevolucionPorDefecto()
            : new DevolucionPorArrepentimiento();

        var resultadoTemplate = template.Procesar(pedido, _diasDesdeEntrega, _productoSinUso, yaFueDevuelto);

        if (!resultadoTemplate.Exitoso)
        {
            return new ResultadoOperacionDto(false, resultadoTemplate.Mensaje);
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var dev = resultadoTemplate.DevolucionGenerada!;
            await _devolucionRepo.AddAsync(dev);

            // Si procede reintegro de existencias, devolver stock
            if (resultadoTemplate.ReintegrarStock)
            {
                var desgloseFisico = pedido.ObtenerDesgloseFisicoTotal();
                foreach (var (sku, cantidad) in desgloseFisico)
                {
                    var prod = await _productoRepo.GetBySkuAsync(sku);
                    if (prod != null)
                    {
                        var stockPrevio = prod.Stock;
                        prod.ReintegrarStock(cantidad);
                        await _productoRepo.UpdateAsync(prod);

                        await _movimientoRepo.AddAsync(new MovimientoInventario(
                            prod.Sku,
                            TipoMovimientoInventario.Devolucion,
                            cantidad,
                            stockPrevio,
                            prod.Stock,
                            $"DEV-{pedido.Codigo}",
                            $"Reintegro por devolución ({_motivo})"
                        ));
                    }
                }
            }

            await _auditoriaRepo.AddAsync(new Auditoria(
                _usuario,
                "ProcesarDevolucion",
                "Devolucion",
                pedido.Id.ToString(),
                $"Devolución procesada por motivo {_motivo}. Reembolso: USD {resultadoTemplate.MontoReembolso:F2}."
            ));

            await _unitOfWork.CommitAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new ResultadoOperacionDto(true, resultadoTemplate.Mensaje, resultadoTemplate);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ResultadoOperacionDto(false, $"Error al registrar la devolución: {ex.Message}");
        }
    }
}
