using LogiMarket.Application.Patterns.AbstractFactory;
using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Patterns.Singleton;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Builder;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Prototype;
using Xunit;

namespace LogiMarket.Tests;

public class LogiMarketTests_PatronesCreacionales
{
    [Fact(DisplayName = "P11: Factory Method - Notificadores EMAIL y SMS")]
    public async Task P11_FactoryMethod_EmailYSms()
    {
        INotificadorCreator emailCreator = new EmailNotificadorCreator();
        INotificadorCreator smsCreator = new SmsNotificadorCreator();

        var notifEmail = emailCreator.CrearNotificador();
        var notifSms = smsCreator.CrearNotificador();

        Assert.Equal(TipoAvisoEnum.Email, notifEmail.Tipo);
        Assert.Equal(TipoAvisoEnum.Sms, notifSms.Tipo);

        await notifEmail.EnviarAvisoAsync("ana@test.com", "Pedido recibido", "Aviso");
        await notifSms.EnviarAvisoAsync("0991112233", "Pedido recibido", "Aviso");

        var emailConcreto = Assert.IsType<EmailNotificador>(notifEmail);
        var smsConcreto = Assert.IsType<SmsNotificador>(notifSms);

        Assert.Single(emailConcreto.BandejaSalida);
        Assert.Single(smsConcreto.BandejaSalida);
        Assert.Contains("EMAIL a ana@test.com", emailConcreto.BandejaSalida[0]);
        Assert.Contains("SMS a 0991112233", smsConcreto.BandejaSalida[0]);
    }

    [Fact(DisplayName = "P12: Abstract Factory - Familias ESTÁNDAR y EXPRÉS")]
    public void P12_AbstractFactory_FamiliasLogistica()
    {
        ILogisticaAbstractFactory estandarFactory = new LogisticaEstandarFactory();
        ILogisticaAbstractFactory expresFactory = new LogisticaExpresFactory();

        var tarifaEstandar = estandarFactory.CrearCalculadorTarifa();
        var plazoEstandar = estandarFactory.CrearCalculadorPlazo();
        var guiaEstandar = estandarFactory.CrearConstructorGuia();

        var tarifaExpres = expresFactory.CrearCalculadorTarifa();
        var plazoExpres = expresFactory.CrearCalculadorPlazo();
        var guiaExpres = expresFactory.CrearConstructorGuia();

        Assert.Equal(5.00m, tarifaEstandar.CalcularTarifa(1.5m));
        Assert.Equal(3, plazoEstandar.DiasHabiles);
        Assert.StartsWith("GUIA-STD-PED-01", guiaEstandar.GenerarNumeroGuia("PED-01"));

        Assert.Equal(12.00m, tarifaExpres.CalcularTarifa(1.5m));
        Assert.Equal(1, plazoExpres.DiasHabiles);
        Assert.StartsWith("GUIA-EXP-PED-02", guiaExpres.GenerarNumeroGuia("PED-02"));
    }

    [Fact(DisplayName = "P13: Builder - Bloqueo de pedido incompleto y agregado consistente")]
    public void P13_Builder_PedidoIncompletoRechazado()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var producto = new ProductoSimple("P01", "Mouse", 20.00m, 0.20m);

        // Sin cliente -> Rechazo
        Assert.Throws<DomainException>(() =>
            new PedidoBuilder().ConCodigo("PED-TEST").AgregarLinea(producto, 1).Build());

        // Sin líneas -> Rechazo
        Assert.Throws<DomainException>(() =>
            new PedidoBuilder().ConCodigo("PED-TEST").ConCliente(cliente).Build());

        // Completo -> Éxito con agregados e instantáneas
        var pedido = new PedidoBuilder()
            .ConCodigo("PED-TEST")
            .ConCliente(cliente)
            .ConEntrega(TipoEntregaEnum.Estandar, 5.00m)
            .AgregarLinea(producto, 2)
            .Build();

        Assert.Equal("PED-TEST", pedido.Codigo);
        Assert.Single(pedido.Lineas);
        Assert.Equal(40.00m, pedido.Subtotal);
        Assert.Equal(4.00m, pedido.Descuento); // 10% VIP
        Assert.Equal(41.00m, pedido.Total); // 40 - 4 + 5 = 41
    }

    [Fact(DisplayName = "P14: Prototype - Clonación profunda de pedido borrador")]
    public void P14_Prototype_ClonacionProfunda()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var p01 = new ProductoSimple("P01", "Mouse", 20.00m, 0.20m);

        var original = new PedidoBuilder()
            .ConCodigo("PED-PLANTILLA")
            .ConCliente(cliente)
            .AgregarLinea(p01, 2)
            .Build();

        var clon = PedidoBorradorPrototipo.ClonarProfundo(original, "PED-CLON-01");

        // Deben ser instancias distintas con claves distintas
        Assert.NotSame(original, clon);
        Assert.NotSame(original.Lineas, clon.Lineas);
        Assert.NotEqual(original.IdempotencyKey, clon.IdempotencyKey);
        Assert.Equal("PED-CLON-01", clon.Codigo);

        // Modificar cantidad en clon no altera el original
        clon.Lineas[0].Cantidad = 10;
        Assert.Equal(2, original.Lineas[0].Cantidad);
    }

    [Fact(DisplayName = "P15: Singleton - MetricasSesion compartida y thread-safe")]
    public void P15_Singleton_MetricasSesionThreadSafe()
    {
        var instancia1 = MetricasSesion.ObtenerInstancia();
        var instancia2 = MetricasSesion.ObtenerInstancia();

        Assert.Same(instancia1, instancia2);

        instancia1.Resetear();

        // Ejecutar incrementos concurrentes desde múltiples hilos
        Parallel.For(0, 100, _ =>
        {
            instancia2.RegistrarVentaExitosa(10m);
        });

        Assert.Equal(100, instancia1.PedidosConfirmados);
        Assert.Equal(1000m, instancia1.VentasTotalesSesion);
    }
}
