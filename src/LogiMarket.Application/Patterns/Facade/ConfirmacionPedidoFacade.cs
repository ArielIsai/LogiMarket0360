using System.Text.Json;
using LogiMarket.Application.DTOs;
using LogiMarket.Application.Patterns.ChainOfResponsibility;
using LogiMarket.Application.Patterns.Observer;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Patterns.Facade;

public class ConfirmacionPedidoFacade
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly IPagoRepository _pagoRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IEventoOutboxRepository _outboxRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PipelineValidacionPedido _pipelineValidacion;
    private readonly IPasarelaPago _pasarelaPago;
    private readonly IPublicadorEventos _publicadorEventos;

    public ConfirmacionPedidoFacade(
        IPedidoRepository pedidoRepo,
        IProductoRepository productoRepo,
        IPagoRepository pagoRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IEventoOutboxRepository outboxRepo,
        IAuditoriaRepository auditoriaRepo,
        IUnitOfWork unitOfWork,
        PipelineValidacionPedido pipelineValidacion,
        IPasarelaPago pasarelaPago,
        IPublicadorEventos publicadorEventos)
    {
        _pedidoRepo = pedidoRepo;
        _productoRepo = productoRepo;
        _pagoRepo = pagoRepo;
        _movimientoRepo = movimientoRepo;
        _outboxRepo = outboxRepo;
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
        _pipelineValidacion = pipelineValidacion;
        _pasarelaPago = pasarelaPago;
        _publicadorEventos = publicadorEventos;
    }

    public async Task<ResultadoConfirmacionDto> ConfirmarPedidoAsync(
        int pedidoId,
        string claveIdempotencia,
        string rolUsuario,
        string proveedor)
    {
        // 1. Verificación de Idempotencia: ¿Ya se procesó este cobro previamente?
        var pagoPrevio = await _pagoRepo.GetByIdempotencyKeyAsync(claveIdempotencia);
        if (pagoPrevio != null && pagoPrevio.Aprobado)
        {
            var ped = await _pedidoRepo.GetByIdWithDetailsAsync(pedidoId);
            return new ResultadoConfirmacionDto(
                true,
                "Operación idempotente: El cobro ya fue procesado con anterioridad con esta clave. No se duplica la transacción.",
                pagoPrevio.CodigoTransaccion,
                pagoPrevio.Monto,
                ped?.Estado.ToString() ?? EstadoPedidoEnum.Pagado.ToString()
            );
        }

        var pedido = await _pedidoRepo.GetByIdWithDetailsAsync(pedidoId);
        if (pedido == null)
        {
            return new ResultadoConfirmacionDto(false, $"El pedido {pedidoId} no existe.", null, 0, "NoExiste");
        }

        // Si el pedido ya está pagado (protección contra doble cobro)
        if (pedido.Estado == EstadoPedidoEnum.Pagado)
        {
            return new ResultadoConfirmacionDto(
                true,
                "El pedido ya se encuentra en estado PAGADO. Se previene cobro doble.",
                null,
                pedido.Total,
                pedido.Estado.ToString()
            );
        }

        // 2. Ejecutar validaciones encadenadas (Chain of Responsibility)
        var resultadoValidacion = await _pipelineValidacion.EjecutarAsync(pedido);
        if (!resultadoValidacion.Valido)
        {
            await _auditoriaRepo.AddAsync(new Auditoria(
                rolUsuario, "ValidacionFallida", "Pedido", pedido.Id.ToString(), $"Fallo en {resultadoValidacion.NombrePaso}: {resultadoValidacion.Error}"
            ));
            await _unitOfWork.CommitAsync();

            return new ResultadoConfirmacionDto(
                false,
                $"Rechazo en validación ({resultadoValidacion.NombrePaso}): {resultadoValidacion.Error}",
                null,
                pedido.Total,
                pedido.Estado.ToString()
            );
        }

        // Transición de Borrador a Validado tras pasar la validación
        if (pedido.Estado == EstadoPedidoEnum.Borrador)
        {
            pedido.Validar();
            await _pedidoRepo.UpdateAsync(pedido);
            await _unitOfWork.CommitAsync();
        }

        // 3. Procesar cobro simulado a través del puerto IPasarelaPago (Proxy -> Decorator -> Adapter)
        var resultadoPago = await _pasarelaPago.CobrarAsync(pedido.Total, claveIdempotencia, rolUsuario);
        if (!resultadoPago.Aprobado)
        {
            await _auditoriaRepo.AddAsync(new Auditoria(
                rolUsuario, "CobroRechazado", "Pago", pedido.Id.ToString(), $"Rechazo de pasarela: {resultadoPago.Mensaje}"
            ));
            await _unitOfWork.CommitAsync();

            return new ResultadoConfirmacionDto(
                false,
                $"Cobro no autorizado o rechazado: {resultadoPago.Mensaje}",
                null,
                pedido.Total,
                pedido.Estado.ToString()
            );
        }

        // 4. Transacción atómica: descontar stock, registrar movimientos, cambiar estado, pago y Outbox
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Descontar inventario por SKU físicos finales
            var desgloseFisico = pedido.ObtenerDesgloseFisicoTotal();
            foreach (var (sku, cantidad) in desgloseFisico)
            {
                var prod = await _productoRepo.GetBySkuAsync(sku);
                if (prod == null)
                    throw new InvalidOperationException($"SKU físico {sku} no encontrado.");

                var stockPrevio = prod.Stock;
                prod.DescontarStock(cantidad);
                await _productoRepo.UpdateAsync(prod);

                await _movimientoRepo.AddAsync(new MovimientoInventario(
                    prod.Sku,
                    TipoMovimientoInventario.Venta,
                    cantidad,
                    stockPrevio,
                    prod.Stock,
                    pedido.Codigo,
                    $"Venta confirmada por pedido {pedido.Codigo}"
                ));
            }

            // Transicionar estado a Pagado
            pedido.Pagar();
            await _pedidoRepo.UpdateAsync(pedido);

            // Registrar pago
            var pago = new Pago(
                pedido.Id,
                resultadoPago.CodigoTransaccion,
                resultadoPago.Monto,
                resultadoPago.Proveedor,
                aprobado: true,
                resultadoPago.Mensaje,
                claveIdempotencia
            );
            await _pagoRepo.AddAsync(pago);

            // Registrar evento en Outbox
            var eventoPayload = JsonSerializer.Serialize(new
            {
                PedidoId = pedido.Id,
                Codigo = pedido.Codigo,
                Cliente = pedido.Cliente.Nombre,
                Total = pedido.Total,
                FechaPago = pedido.FechaPago
            });

            var eventoOutbox = new EventoOutbox("PedidoPagado", eventoPayload);
            await _outboxRepo.AddAsync(eventoOutbox);

            await _auditoriaRepo.AddAsync(new Auditoria(
                rolUsuario, "ConfirmarPedido", "Pedido", pedido.Id.ToString(), $"Pedido {pedido.Codigo} confirmado y pagado exitosamente. Transacción: {resultadoPago.CodigoTransaccion}."
            ));

            await _unitOfWork.CommitAsync();
            await _unitOfWork.CommitTransactionAsync();

            // 5. Notificar a observadores en memoria tras confirmar la transacción
            await _publicadorEventos.PublicarAsync(new EventoPedidoPagado(pedido.Id, pedido.Codigo, pedido.Total, pedido.Cliente.Correo, pedido.Cliente.Contacto));

            return new ResultadoConfirmacionDto(
                true,
                $"Pedido {pedido.Codigo} confirmado y cobrado con éxito.",
                resultadoPago.CodigoTransaccion,
                pedido.Total,
                pedido.Estado.ToString()
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ResultadoConfirmacionDto(false, $"Error crítico durante la transacción: {ex.Message}", null, 0, pedido.Estado.ToString());
        }
    }
}
