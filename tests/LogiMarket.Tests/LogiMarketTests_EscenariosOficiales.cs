using LogiMarket.Application.Patterns.ChainOfResponsibility;
using LogiMarket.Application.Patterns.Facade;
using LogiMarket.Application.Patterns.Observer;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Builder;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Infrastructure.Patterns.Adapters;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMarket.Tests;

public class LogiMarketTests_EscenariosOficiales
{
    [Fact(DisplayName = "P01: Creación de DB y seed idempotente")]
    public async Task P01_CreacionDbYSeedIdempotente()
    {
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var productos = await db.Productos.ToListAsync();
            var usuarios = await db.Usuarios.ToListAsync();
            var clientes = await db.Clientes.ToListAsync();

            Assert.Equal(5, productos.Count);
            Assert.Equal(4, usuarios.Count);
            Assert.Equal(4, clientes.Count);

            // Re-ejecutar seed para verificar idempotencia estricta
            await DbInitializer.InicializarAsync(db);

            Assert.Equal(5, await db.Productos.CountAsync());
            Assert.Equal(4, await db.Usuarios.CountAsync());
            Assert.Equal(4, await db.Clientes.CountAsync());
        }
    }

    [Fact(DisplayName = "P02: Escenario oficial 1 - Ana VIP compra KIT_TELETRABAJO x1 ESTÁNDAR (USD 59.00)")]
    public async Task P02_AnaVip_KitTeletrabajo_Estandar()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var ana = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var p01 = await db.Productos.FirstAsync(p => p.Sku == "P01");
            var p02 = await db.Productos.FirstAsync(p => p.Sku == "P02");
            var p03 = await db.Productos.FirstAsync(p => p.Sku == "P03");

            // Construir KIT_TELETRABAJO = (P01 + P02) + P03 = $60.00
            var kitOficina = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
            kitOficina.AgregarComponente(new ProductoSimple(p01), 1);
            kitOficina.AgregarComponente(new ProductoSimple(p02), 1);

            var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
            kitTeletrabajo.AgregarComponente(kitOficina, 1);
            kitTeletrabajo.AgregarComponente(new ProductoSimple(p03), 1);

            // Construir pedido con PedidoBuilder
            var pedido = new PedidoBuilder()
                .ConCodigo("PED-ANA-01")
                .ConCliente(ana)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(kitTeletrabajo, 1)
                .Build();

            // Cálculo esperado: Subtotal 60.00 - Descuento VIP (10%) 6.00 + Flete 5.00 = 59.00
            Assert.Equal(60.00m, pedido.Subtotal);
            Assert.Equal(6.00m, pedido.Descuento);
            Assert.Equal(5.00m, pedido.TarifaEnvio);
            Assert.Equal(59.00m, pedido.Total);

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            // Confirmar pedido a través de la Fachada
            var resultado = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");

            Assert.True(resultado.Exitoso);
            Assert.Equal("Pagado", resultado.EstadoFinal);

            // Verificar existencias descontadas
            var p01Post = await db.Productos.FirstAsync(p => p.Sku == "P01");
            var p02Post = await db.Productos.FirstAsync(p => p.Sku == "P02");
            var p03Post = await db.Productos.FirstAsync(p => p.Sku == "P03");

            Assert.Equal(7, p01Post.Stock); // 8 -> 7
            Assert.Equal(4, p02Post.Stock); // 5 -> 4
            Assert.Equal(19, p03Post.Stock); // 20 -> 19

            // Verificar que se creó exactamente un pago
            var pagos = await db.Pagos.Where(p => p.PedidoId == pedido.Id).ToListAsync();
            Assert.Single(pagos);
            Assert.True(pagos[0].Aprobado);
        }
    }

    [Fact(DisplayName = "P03: Escenario oficial 2 - Luis NORMAL compra 2 P04 + 2 P05 EXPRÉS (USD 102.00)")]
    public async Task P03_LuisNormal_P04_P05_Expres()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var luis = await db.Clientes.FirstAsync(c => c.Codigo == "C002");
            var p04 = await db.Productos.FirstAsync(p => p.Sku == "P04");
            var p05 = await db.Productos.FirstAsync(p => p.Sku == "P05");

            var pedido = new PedidoBuilder()
                .ConCodigo("PED-LUIS-01")
                .ConCliente(luis)
                .ConEntrega(TipoEntregaEnum.Expres, 12.00m)
                .AgregarLinea(new ProductoSimple(p04), 2) // 2 * 40 = 80
                .AgregarLinea(new ProductoSimple(p05), 2) // 2 * 5 = 10
                .Build();

            // Cálculo esperado: Subtotal 90.00 - Descuento Normal 0.00 + Flete 12.00 = 102.00
            Assert.Equal(90.00m, pedido.Subtotal);
            Assert.Equal(0.00m, pedido.Descuento);
            Assert.Equal(12.00m, pedido.TarifaEnvio);
            Assert.Equal(102.00m, pedido.Total);

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            var resultado = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");
            Assert.True(resultado.Exitoso);

            var p04Post = await db.Productos.FirstAsync(p => p.Sku == "P04");
            var p05Post = await db.Productos.FirstAsync(p => p.Sku == "P05");

            Assert.Equal(2, p04Post.Stock); // 4 -> 2
            Assert.Equal(28, p05Post.Stock); // 30 -> 28
        }
    }

    [Fact(DisplayName = "P04: Escenario oficial 3 - Carmen MAYORISTA compra P03 x10 ESTÁNDAR (USD 90.00)")]
    public async Task P04_CarmenMayorista_P03x10_Estandar()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var carmen = await db.Clientes.FirstAsync(c => c.Codigo == "C003");
            var p03 = await db.Productos.FirstAsync(p => p.Sku == "P03");

            var pedido = new PedidoBuilder()
                .ConCodigo("PED-CARMEN-01")
                .ConCliente(carmen)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(new ProductoSimple(p03), 10) // 10 * 10 = 100
                .Build();

            // Cálculo: Subtotal 100.00 - Mayorista 15% (15.00) + Flete 5.00 = 90.00
            Assert.Equal(100.00m, pedido.Subtotal);
            Assert.Equal(15.00m, pedido.Descuento);
            Assert.Equal(90.00m, pedido.Total);

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            var resultado = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");
            Assert.True(resultado.Exitoso);

            var p03Post = await db.Productos.FirstAsync(p => p.Sku == "P03");
            Assert.Equal(10, p03Post.Stock); // 20 -> 10
        }
    }

    [Fact(DisplayName = "P05: Escenario oficial 4 - KIT_TELETRABAJO x6 con stock inicial rechaza por P02 insuficiente")]
    public async Task P05_KitTeletrabajo_x6_RechazoPorFaltaStock()
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

            // 6 kits requieren 6 P02 (pero solo hay 5 en stock)
            var pedido = new PedidoBuilder()
                .ConCodigo("PED-FAIL-01")
                .ConCliente(ana)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(kitTeletrabajo, 6)
                .Build();

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            var resultado = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");

            Assert.False(resultado.Exitoso);
            Assert.Contains("Existencias insuficientes para 'P02", resultado.Mensaje);

            // Existencias deben permanecer intactas
            var p02Post = await db.Productos.FirstAsync(p => p.Sku == "P02");
            Assert.Equal(5, p02Post.Stock);

            // No debe existir pago
            Assert.False(await db.Pagos.AnyAsync(p => p.PedidoId == pedido.Id));
        }
    }

    [Fact(DisplayName = "P06: Simular error de cobro en pedido válido conserva inventario")]
    public async Task P06_SimularErrorCobroConservaInventario()
    {
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var ana = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var p01 = await db.Productos.FirstAsync(p => p.Sku == "P01");

            var pedido = new PedidoBuilder()
                .ConCodigo("PED-COBRO-FAIL")
                .ConCliente(ana)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(new ProductoSimple(p01), 1)
                .Build();

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            // Pasarela que simula rechazo
            var pasarelaFalla = new PasarelaModernaSimulada(); // simulador que podemos forzar rechazo vía Proxy de rol no autorizado
            IPasarelaPago proxy = new PagoAutorizadoProxy(pasarelaFalla);

            var pipeline = new PipelineValidacionPedido(new EfClienteRepository(db), new EfProductoRepository(db));
            var facade = new ConfirmacionPedidoFacade(
                new EfPedidoRepository(db),
                new EfProductoRepository(db),
                new EfPagoRepository(db),
                new EfMovimientoInventarioRepository(db),
                new EfEventoOutboxRepository(db),
                new EfAuditoriaRepository(db),
                new UnitOfWork(db),
                pipeline,
                proxy,
                new PublicadorEventos()
            );

            // Invocar con rol CONSULTA (provoca rechazo del cobro en el Proxy)
            var res = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Consulta", "V2");

            Assert.False(res.Exitoso);
            Assert.Contains("Acceso denegado", res.Mensaje);

            // Stock de P01 permanece intacto en 8
            var p01Post = await db.Productos.FirstAsync(p => p.Sku == "P01");
            Assert.Equal(8, p01Post.Stock);
            Assert.False(await db.Pagos.AnyAsync(p => p.PedidoId == pedido.Id));
        }
    }

    [Fact(DisplayName = "P07: Repetir confirmación con misma clave de idempotencia previene cobro doble")]
    public async Task P07_RepetirConfirmacionIdempotente()
    {
        var (facade, db, _, _, conn) = TestHelpers.CrearSujetoPruebaConfirmacion();
        using (conn)
        {
            var ana = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var p01 = await db.Productos.FirstAsync(p => p.Sku == "P01");

            var pedido = new PedidoBuilder()
                .ConCodigo("PED-IDEMPOTENTE")
                .ConCliente(ana)
                .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
                .AgregarLinea(new ProductoSimple(p01), 1)
                .Build();

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            // Primer cobro
            var res1 = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");
            Assert.True(res1.Exitoso);

            // Reintento idéntico
            var res2 = await facade.ConfirmarPedidoAsync(pedido.Id, pedido.IdempotencyKey, "Ventas", "Legacy");
            Assert.True(res2.Exitoso);
            Assert.Contains("Operación idempotente", res2.Mensaje);

            // Stock se descontó exactamente una vez (8 -> 7)
            var p01Post = await db.Productos.FirstAsync(p => p.Sku == "P01");
            Assert.Equal(7, p01Post.Stock);

            // Solo existe un registro de pago
            var pagos = await db.Pagos.Where(p => p.PedidoId == pedido.Id).ToListAsync();
            Assert.Single(pagos);
        }
    }

    [Fact(DisplayName = "P08: Pedido PAGADO deniega cancelación y mutación de estado")]
    public void P08_PedidoPagado_DeniegaCancelacion()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var pedido = new Pedido("PED-08", cliente, TipoEntregaEnum.Estandar, 5.00m);
        pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));

        pedido.Validar();
        pedido.Pagar();

        // Intentar cancelar un pedido PAGADO debe lanzar excepción de dominio
        var ex = Assert.Throws<DomainException>(() => pedido.Cancelar());
        Assert.Contains("No se permite cancelar un pedido PAGADO", ex.Message);
    }

    [Fact(DisplayName = "P09: Despacho y entrega solo permitidos en secuencia y por BODEGA")]
    public async Task P09_DespachoYEntregaSecuenciaPermitida()
    {
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var ana = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var pedido = new Pedido("PED-09", ana, TipoEntregaEnum.Estandar, 5.00m);
            pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));
            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            var pedidoRepo = new EfPedidoRepository(db);
            var uow = new UnitOfWork(db);
            var audRepo = new EfAuditoriaRepository(db);

            // Intentar despachar pedido en BORRADOR sin pago -> Rechazo
            var cmdDespachoInvalido = new Application.Patterns.Command.DespacharPedidoCommand(
                pedidoRepo, uow, audRepo, pedido.Id, "GUIA-09", "bodega.demo", RolUsuario.Bodega
            );
            var resInvalido = await cmdDespachoInvalido.ExecuteAsync();
            Assert.False(resInvalido.Exitoso);

            // Transicionar válidamente a Pagado
            pedido.Validar();
            pedido.Pagar();
            await db.SaveChangesAsync();

            // Despachar con Bodega -> Exitoso
            var cmdDespachoValido = new Application.Patterns.Command.DespacharPedidoCommand(
                pedidoRepo, uow, audRepo, pedido.Id, "GUIA-09", "bodega.demo", RolUsuario.Bodega
            );
            var resValido = await cmdDespachoValido.ExecuteAsync();
            Assert.True(resValido.Exitoso);
            Assert.Equal(EstadoPedidoEnum.Despachado, pedido.Estado);

            // Entregar pedido
            pedido.Entregar();
            Assert.Equal(EstadoPedidoEnum.Entregado, pedido.Estado);
        }
    }

    [Fact(DisplayName = "P10: Persistencia de SQLite tras cerrar y reabrir contexto")]
    public async Task P10_PersistenciaSqliteReapertura()
    {
        var (db1, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var p01 = await db1.Productos.FirstAsync(p => p.Sku == "P01");
            p01.Stock = 99;
            await db1.SaveChangesAsync();

            // Simular cierre de contexto y reapertura sobre la misma conexión/base
            var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
                .UseSqlite(conn)
                .Options;

            using var db2 = new LogiMarketDbContext(options);
            var p01Reabierto = await db2.Productos.FirstAsync(p => p.Sku == "P01");
            Assert.Equal(99, p01Reabierto.Stock);
        }
    }
}
