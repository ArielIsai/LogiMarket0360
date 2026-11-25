using System.Text.Json;
using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Infrastructure.Outbox;

public class ProcesadorOutbox
{
    private readonly IEventoOutboxRepository _outboxRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificadorCreator _notificadorCreator;

    public ProcesadorOutbox(
        IEventoOutboxRepository outboxRepo,
        IUnitOfWork unitOfWork,
        INotificadorCreator notificadorCreator)
    {
        _outboxRepo = outboxRepo;
        _unitOfWork = unitOfWork;
        _notificadorCreator = notificadorCreator;
    }

    public async Task<int> ProcesarPendientesAsync(bool simularFallo = false)
    {
        var pendientes = await _outboxRepo.GetPendientesAsync();
        int procesados = 0;

        foreach (var evento in pendientes)
        {
            try
            {
                if (simularFallo)
                {
                    evento.Intentos++;
                    evento.Error = "Fallo simulado inducido en pasarela de comunicación externa.";
                    await _outboxRepo.UpdateAsync(evento);
                    await _unitOfWork.CommitAsync();
                    continue;
                }

                // Despachar notificación correspondiente
                var notificador = _notificadorCreator.CrearNotificador();
                await notificador.EnviarAvisoAsync(
                    "notificaciones@logimarket.test",
                    $"Evento durable procesado: {evento.TipoEvento}. Payload: {evento.PayloadJson}",
                    $"Procesamiento Outbox {evento.TipoEvento}"
                );

                evento.Procesado = true;
                evento.FechaProcesado = DateTime.UtcNow;
                evento.Error = string.Empty;
                await _outboxRepo.UpdateAsync(evento);

                procesados++;
            }
            catch (Exception ex)
            {
                evento.Intentos++;
                evento.Error = ex.Message;
                await _outboxRepo.UpdateAsync(evento);
            }
        }

        await _unitOfWork.CommitAsync();
        return procesados;
    }
}
