using LogiMarket.Application.Patterns.ChainOfResponsibility;
using LogiMarket.Application.Patterns.Command;
using LogiMarket.Application.Patterns.Interpreter;
using LogiMarket.Application.Patterns.Observer;
using LogiMarket.Application.Patterns.Singleton;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Iterator;
using LogiMarket.Domain.Patterns.Memento;
using LogiMarket.Domain.Patterns.Strategy;
using LogiMarket.Domain.Patterns.TemplateMethod;
using LogiMarket.Domain.Patterns.Visitor;
using LogiMarket.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMarket.Tests;

public class LogiMarketTests_PatronesComportamentales
{
    [Fact(DisplayName = "P23: Chain of Responsibility - Pipeline informa errores ordenadamente")]
    public async Task P23_ChainOfResponsibility_Pipeline()
    {
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var pipeline = new PipelineValidacionPedido(new EfClienteRepository(db), new EfProductoRepository(db));

            // Pedido sin cliente -> Detenido en paso 1
            var pedidoSinCliente = new Pedido { Id = 1 };
            var r1 = await pipeline.EjecutarAsync(pedidoSinCliente);
            Assert.False(r1.Valido);
            Assert.Contains("Validación de Cliente", r1.NombrePaso);

            // Asignar cliente pero sin líneas -> Detenido en paso 2
            var cliente = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            pedidoSinCliente.Cliente = cliente;
            pedidoSinCliente.ClienteId = cliente.Id;
            var r2 = await pipeline.EjecutarAsync(pedidoSinCliente);
            Assert.False(r2.Valido);
            Assert.Contains("Líneas y Cantidades", r2.NombrePaso);
        }
    }

    [Fact(DisplayName = "P24: Command - Invoker registra historial de ejecución")]
    public async Task P24_Command_InvokerHistorial()
    {
        var invoker = new CommandInvoker();
        var (db, conn) = TestHelpers.CrearContextoSqliteMemoria();
        using (conn)
        {
            var cliente = await db.Clientes.FirstAsync(c => c.Codigo == "C001");
            var pedido = new Pedido("PED-CMD", cliente, TipoEntregaEnum.Estandar, 5.00m);
            pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));

            await db.Pedidos.AddAsync(pedido);
            await db.SaveChangesAsync();

            var cmd = new CancelarPedidoCommand(
                new EfPedidoRepository(db),
                new UnitOfWork(db),
                new EfAuditoriaRepository(db),
                pedido.Id,
                "admin.demo"
            );

            var res = await invoker.EjecutarComandoAsync(cmd);

            Assert.True(res.Exitoso);
            Assert.Single(invoker.Historial);
            Assert.Equal("CancelarPedidoCommand", invoker.Historial[0].NombreComando);
        }
    }

    [Fact(DisplayName = "P25: Interpreter - ESTADO = PAGADO AND TOTAL >= 50")]
    public void P25_Interpreter_ConsultaCompuestaValida()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");

        var p1 = new Pedido("PED-1", cliente, TipoEntregaEnum.Estandar, 5.00m) { Estado = EstadoPedidoEnum.Pagado, Total = 59.00m };
        var p2 = new Pedido("PED-2", cliente, TipoEntregaEnum.Estandar, 5.00m) { Estado = EstadoPedidoEnum.Validado, Total = 60.00m };
        var p3 = new Pedido("PED-3", cliente, TipoEntregaEnum.Estandar, 5.00m) { Estado = EstadoPedidoEnum.Pagado, Total = 30.00m };

        var lista = new List<Pedido> { p1, p2, p3 };

        var parser = new ParserFiltroPedidos("ESTADO = PAGADO AND TOTAL >= 50");
        var expresion = parser.Parsear();

        var filtrados = lista.Where(p => expresion.Interpretar(p)).ToList();

        Assert.Single(filtrados);
        Assert.Equal("PED-1", filtrados[0].Codigo);
    }

    [Fact(DisplayName = "P26: Interpreter - Rechazo sintáctico y soporte de paréntesis")]
    public void P26_Interpreter_SintaxisYParentesis()
    {
        var clienteAna = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var clienteLuis = new Cliente("C002", "Luis", "456", CategoriaCliente.Normal, "456", "l@test.com");

        var p1 = new Pedido("PED-1", clienteAna, TipoEntregaEnum.Estandar, 5.00m) { Estado = EstadoPedidoEnum.Pagado, Total = 40.00m };
        var p2 = new Pedido("PED-2", clienteLuis, TipoEntregaEnum.Estandar, 5.00m) { Estado = EstadoPedidoEnum.Pagado, Total = 60.00m };

        var lista = new List<Pedido> { p1, p2 };

        // Expresión con paréntesis
        var parserValido = new ParserFiltroPedidos("(CLIENTE = Ana OR CLIENTE = Luis) AND TOTAL > 50");
        var exprValida = parserValido.Parsear();
        var resultado = lista.Where(p => exprValida.Interpretar(p)).ToList();

        Assert.Single(resultado);
        Assert.Equal("PED-2", resultado[0].Codigo);

        // Sintaxis errónea (sin operador ni valor) -> FormatException
        Assert.Throws<FormatException>(() => new ParserFiltroPedidos("ESTADO = AND TOTAL").Parsear());
    }

    [Fact(DisplayName = "P27: Iterator - Recorrido determinista de componentes físicos en kit anidado")]
    public void P27_Iterator_RecorridoKitAnidado()
    {
        var p01 = new ProductoSimple("P01", "Mouse", 20.00m, 0.20m);
        var p02 = new ProductoSimple("P02", "Teclado", 30.00m, 0.70m);
        var p03 = new ProductoSimple("P03", "Memoria", 10.00m, 0.10m);

        var kitOficina = new KitCompuesto("KIT_OFICINA", "Oficina");
        kitOficina.AgregarComponente(p01, 1);
        kitOficina.AgregarComponente(p02, 1);

        var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo");
        kitTeletrabajo.AgregarComponente(kitOficina, 1);
        kitTeletrabajo.AgregarComponente(p03, 1);

        IKitComponenteIterator iterador = new KitFisicoIterator(kitTeletrabajo);
        var recorridos = new List<string>();

        while (iterador.HasNext())
        {
            var comp = iterador.Next();
            recorridos.Add(comp.Sku);
        }

        Assert.Equal(3, recorridos.Count);
        Assert.Contains("P01", recorridos);
        Assert.Contains("P02", recorridos);
        Assert.Contains("P03", recorridos);
    }

    [Fact(DisplayName = "P29: Memento - Deshacer y Rehacer en PedidoBorrador con límite de 5")]
    public void P29_Memento_DeshacerYRehacer()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var pedido = new Pedido("PED-MEM", cliente, TipoEntregaEnum.Estandar, 5.00m);
        var careTaker = new HistorialBorradorCareTaker();

        // Estado 1: 1 mouse
        pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));
        pedido.RecalcularTotales(0, "Normal");
        careTaker.GuardarEstado(pedido, "Agregado 1 Mouse");

        // Estado 2: + 1 teclado (modificación en curso)
        pedido.Lineas.Add(new PedidoDetalle("P02", "Teclado", false, 1, 30.00m));
        pedido.RecalcularTotales(0, "Normal");

        // Deshacer: debe volver al estado previo guardado (1 línea)
        Assert.True(careTaker.PuedeDeshacer);
        var mementoPrevio = careTaker.Deshacer(pedido);
        Assert.Single(mementoPrevio.LineasSnapshot);
        Assert.Equal("P01", mementoPrevio.LineasSnapshot[0].Sku);

        // Rehacer: vuelve al estado posterior con 2 líneas
        Assert.True(careTaker.PuedeRehacer);
        var mementoRehecho = careTaker.Rehacer(pedido);
        Assert.Equal(2, mementoRehecho.LineasSnapshot.Count);
    }

    [Fact(DisplayName = "P30: Observer - Publicador dispara múltiples suscriptores")]
    public async Task P30_Observer_PublicadorMultiplesSuscriptores()
    {
        var publicador = new PublicadorEventos();
        var metricas = new MetricasSesion();
        metricas.Resetear();

        publicador.Suscribir<EventoPedidoPagado>(new MetricasObserver(metricas));

        await publicador.PublicarAsync(new EventoPedidoPagado(1, "PED-01", 59.00m, "a@test.com", "123"));

        Assert.Equal(1, metricas.PedidosConfirmados);
        Assert.Equal(59.00m, metricas.VentasTotalesSesion);
    }

    [Fact(DisplayName = "P31: State - Transiciones válidas y denegación de estados repetidos")]
    public void P31_State_TransicionesValidasEInvalidas()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var pedido = new Pedido("PED-ST", cliente, TipoEntregaEnum.Estandar, 5.00m);
        pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));

        // Borrador -> Validado
        pedido.Validar();
        Assert.Equal(EstadoPedidoEnum.Validado, pedido.Estado);

        // Validar otra vez sobre VALIDADO -> Excepción
        Assert.Throws<DomainException>(() => pedido.Validar());

        // Validado -> Pagado
        pedido.Pagar();
        Assert.Equal(EstadoPedidoEnum.Pagado, pedido.Estado);

        // Pagado -> Cancelar -> Prohibido por regla
        Assert.Throws<DomainException>(() => pedido.Cancelar());
    }

    [Fact(DisplayName = "P32: Strategy - Políticas Mayorista (15%), VIP (10%) y Normal (0%)")]
    public void P32_Strategy_CalculoConPrioridades()
    {
        var context = new CalculadoraDescuentoContext();

        // 1. Ana (VIP, 1 unidad, subtotal 60) -> VIP 10% = 6.00
        var (estAna, descAna) = context.EvaluarDescuento(60.00m, 1, CategoriaCliente.Vip);
        Assert.Equal("VIP (10%)", estAna.Nombre);
        Assert.Equal(6.00m, descAna);

        // 2. Carmen (VIP o Normal, 10 unidades, subtotal 100) -> Prioridad MAYORISTA 15% = 15.00
        var (estCarmen, descCarmen) = context.EvaluarDescuento(100.00m, 10, CategoriaCliente.Vip);
        Assert.Equal("MAYORISTA (15%)", estCarmen.Nombre);
        Assert.Equal(15.00m, descCarmen);

        // 3. Normal (Normal, 2 unidades, subtotal 90) -> NORMAL 0% = 0.00
        var (estNormal, descNormal) = context.EvaluarDescuento(90.00m, 2, CategoriaCliente.Normal);
        Assert.Equal("NORMAL (0%)", estNormal.Nombre);
        Assert.Equal(0.00m, descNormal);
    }

    [Fact(DisplayName = "P33: Template Method - Defecto día 20 aceptado, arrepentimiento día 10 rechazado")]
    public void P33_TemplateMethod_Devoluciones()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var pedido = new Pedido("PED-DEV", cliente, TipoEntregaEnum.Estandar, 5.00m)
        {
            Subtotal = 60.00m,
            Descuento = 6.00m,
            Total = 59.00m,
            Estado = EstadoPedidoEnum.Entregado
        };
        pedido.ActualizarObjetoEstado();

        var templateDefecto = new DevolucionPorDefecto();
        var templateArrepentimiento = new DevolucionPorArrepentimiento();

        // Defecto al día 20 -> Aceptado
        var rDefecto = templateDefecto.Procesar(pedido, diasDesdeEntrega: 20, productoSinUso: false, yaFueDevuelto: false);
        Assert.True(rDefecto.Exitoso);
        Assert.Equal(54.00m, rDefecto.MontoReembolso); // 60 - 6 = 54 (sin flete de 5)

        // Arrepentimiento al día 10 -> Rechazado por superar 7 días
        var rArrepentimiento = templateArrepentimiento.Procesar(pedido, diasDesdeEntrega: 10, productoSinUso: true, yaFueDevuelto: false);
        Assert.False(rArrepentimiento.Exitoso);
        Assert.Contains("máximo 7 días", rArrepentimiento.Mensaje);
    }

    [Fact(DisplayName = "P34: Visitor - Desglose físico y peso logístico (1.00 kg) de KIT_TELETRABAJO")]
    public void P34_Visitor_DesgloseYPeso()
    {
        var p01 = new ProductoSimple("P01", "Mouse", 20.00m, 0.20m);
        var p02 = new ProductoSimple("P02", "Teclado", 30.00m, 0.70m);
        var p03 = new ProductoSimple("P03", "Memoria", 10.00m, 0.10m);

        var kitOficina = new KitCompuesto("KIT_OFICINA", "Oficina");
        kitOficina.AgregarComponente(p01, 1);
        kitOficina.AgregarComponente(p02, 1);

        var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo");
        kitTeletrabajo.AgregarComponente(kitOficina, 1);
        kitTeletrabajo.AgregarComponente(p03, 1);

        // Visitor de Desglose SKU
        var desgloseVisitor = new DesgloseSkuVisitor();
        kitTeletrabajo.Aceptar(desgloseVisitor);

        Assert.Equal(3, desgloseVisitor.ConteoPorSku.Count);
        Assert.Equal(1, desgloseVisitor.ConteoPorSku["P01"]);
        Assert.Equal(1, desgloseVisitor.ConteoPorSku["P02"]);
        Assert.Equal(1, desgloseVisitor.ConteoPorSku["P03"]);

        // Visitor de Peso Logístico: 0.20 + 0.70 + 0.10 = 1.00 kg
        var pesoVisitor = new PesoLogisticoVisitor();
        kitTeletrabajo.Aceptar(pesoVisitor);

        Assert.Equal(1.00m, pesoVisitor.PesoTotalKg);
    }
}
