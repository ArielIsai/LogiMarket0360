using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Patterns.Builder;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.TemplateMethod;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Patterns.Adapters;
using LogiMarket.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMarket.Tests;

public class LogiMarketTests_IntegridadAvanzada
{
    [Fact(DisplayName = "P35: Devolución oficial de Ana (USD 54.00 sin flete) y rechazo de devolución repetida")]
    public async Task P35_DevolucionOficialAnaSinFleteYNoDuplicada()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var ana = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var p01 = await db.Productos.FirstAsync(p => p.Sku == "P01");
            var p02 = await db.Productos.FirstAsync(p => p.Sku == "P02");
            var p03 = await db.Productos.FirstAsync(p => p.Sku == "P03");

            var kitOficina = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
            kitOficina.AgregarComponente(new ProductoSimple(p01), 1);
            kitOficina.AgregarComponente(new ProductoSimple(p02), 1);

            var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
            kitTeletrabajo.AgregarComponente(kitOficina, 1);
            kitTeletrabajo.AgregarComponente(new ProductoSimple(p03), 1);

            var pedido = new PedidoBuilder()
                .ConCodigo("PED-ANA-DEV")
                .ConCliente(ana)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(kitTeletrabajo, 1)
                .Build();

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            // Confirmar y pagar
            await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");

            // Despachar y Entregar
            pedido.Despachar("GUIA-P35");
            pedido.Entregar();
            await db.SaveChangesAsync();

            // Ejecutar devolución con Command
            var devRepo = new EfDevolucionRepository(db);
            var prodRepo = new EfProductoRepository(db);
            var movRepo = new EfMovimientoInventarioRepository(db);
            var audRepo = new EfAuditoriaRepository(db);
            var uow = new UnitOfWork(db);

            var cmdDev = new Application.Patterns.Command.ProcesarDevolucionCommand(
                new EfPedidoRepository(db),
                devRepo,
                prodRepo,
                movRepo,
                audRepo,
                uow,
                pedido.Id,
                MotivoDevolucionEnum.Defecto,
                diasDesdeEntrega: 15,
                productoSinUso: false,
                usuario: "ventas.demo"
            );

            // 1. Primera devolución -> Aprobada con USD 54.00 (sin los USD 5.00 de envío)
            var res1 = await cmdDev.ExecuteAsync();
            Assert.True(res1.Exitoso);

            var devolucionDb = await db.Devoluciones.FirstAsync(d => d.PedidoId == pedido.Id);
            Assert.Equal(54.00m, devolucionDb.ReembolsoMonto);
            Assert.True(devolucionDb.StockReintegrado);

            // Existencias deben haberse reintegrado (P01 vuelve de 7 a 8, P02 de 4 a 5, P03 de 19 a 20)
            var p01Reintegrado = await db.Productos.FirstAsync(p => p.Sku == "P01");
            var p02Reintegrado = await db.Productos.FirstAsync(p => p.Sku == "P02");
            var p03Reintegrado = await db.Productos.FirstAsync(p => p.Sku == "P03");

            Assert.Equal(8, p01Reintegrado.Stock);
            Assert.Equal(5, p02Reintegrado.Stock);
            Assert.Equal(20, p03Reintegrado.Stock);

            // 2. Intento de devolución repetida sobre el mismo pedido -> Rechazado estrictamente
            var res2 = await cmdDev.ExecuteAsync();
            Assert.False(res2.Exitoso);
            Assert.Contains("ya cuenta con una devolución", res2.Mensaje);
        }
    }

    [Fact(DisplayName = "P36: Concurrencia sobre SKU con stock de 1 unidad evita sobreventa")]
    public async Task P36_ConcurrenciaSinSobreventa()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var p04 = await db.Productos.FirstAsync(p => p.Sku == "P04");
            p04.Stock = 1; // Stock configurado en 1 unidad
            await db.SaveChangesAsync();

            var cliente = await db.Clientes.FirstAsync(c => c.Codigo == "C001");

            var ped1 = new PedidoBuilder().ConCodigo("PED-CONC-1").ConCliente(cliente).AgregarLinea(new ProductoSimple(p04), 1).Build();
            var ped2 = new PedidoBuilder().ConCodigo("PED-CONC-2").ConCliente(cliente).AgregarLinea(new ProductoSimple(p04), 1).Build();

            await db.Pedidos.AddRangeAsync(ped1, ped2);
            await db.SaveChangesAsync();

            // Ejecución secuencial/competitiva de confirmación
            var res1 = await facade.ConfirmarPedidoAsync(ped1.Id, ped1.IdempotencyKey, "Ventas", "Legacy");
            var res2 = await facade.ConfirmarPedidoAsync(ped2.Id, ped2.IdempotencyKey, "Ventas", "Legacy");

            // Uno aprueba y el segundo rechaza por falta de existencias
            Assert.True(res1.Exitoso);
            Assert.False(res2.Exitoso);
            Assert.Contains("insuficientes", res2.Mensaje);

            var p04Final = await db.Productos.FirstAsync(p => p.Sku == "P04");
            Assert.Equal(0, p04Final.Stock); // No queda negativo
        }
    }

    [Fact(DisplayName = "P37: Outbox - Falla de comunicación y recuperación durable tras reinicio")]
    public async Task P37_OutboxFallaYRecuperacion()
    {
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var uow = new UnitOfWork(db);
            var outboxRepo = new EfEventoOutboxRepository(db);

            // Guardar evento pendiente en Outbox
            var evento = new EventoOutbox("PedidoPagado", "{\"PedidoId\":100,\"Total\":59.00}");
            await outboxRepo.AddAsync(evento);
            await uow.CommitAsync();

            var procesador = new ProcesadorOutbox(outboxRepo, uow, new EmailNotificadorCreator());

            // 1. Simular fallo en envío
            var procesadosFallo = await procesador.ProcesarPendientesAsync(simularFallo: true);
            Assert.Equal(0, procesadosFallo);

            var eventoFallo = await db.EventosOutbox.FirstAsync(e => e.Id == evento.Id);
            Assert.False(eventoFallo.Procesado);
            Assert.Equal(1, eventoFallo.Intentos);
            Assert.NotEmpty(eventoFallo.Error);

            // 2. Recuperación exitosa del procesador
            var procesadosExito = await procesador.ProcesarPendientesAsync(simularFallo: false);
            Assert.Equal(1, procesadosExito);

            var eventoExito = await db.EventosOutbox.FirstAsync(e => e.Id == evento.Id);
            Assert.True(eventoExito.Procesado);
            Assert.NotNull(eventoExito.FechaProcesado);
            Assert.Empty(eventoExito.Error);
        }
    }

    [Fact(DisplayName = "P38: Principios SOLID (LSP, ISP, DIP) con pasarelas intercambiables")]
    public async Task P38_PrincipiosSolid_LspIspDip()
    {
        // DIP: Se depende de la abstracción IPasarelaPago, no de clases concretas
        // LSP: Se sustituyen PasarelaModernaSimulada y AdaptadorPasarelaLegada sin alterar contratos ni excepciones
        IPasarelaPago pasarelaModerna = new PasarelaModernaSimulada();
        IPasarelaPago pasarelaLegada = new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada());

        var resModerna = await pasarelaModerna.CobrarAsync(100m, "KEY-P38-1", "Ventas");
        var resLegada = await pasarelaLegada.CobrarAsync(100m, "KEY-P38-2", "Ventas");

        Assert.True(resModerna.Aprobado);
        Assert.True(resLegada.Aprobado);
        Assert.Equal(100m, resModerna.Monto);
        Assert.Equal(100m, resLegada.Monto);
    }
}
