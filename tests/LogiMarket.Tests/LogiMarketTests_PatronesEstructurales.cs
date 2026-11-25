using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Infrastructure.Patterns.Adapters;
using LogiMarket.Infrastructure.Patterns.Bridge;
using LogiMarket.Infrastructure.Patterns.Flyweight;
using Xunit;

namespace LogiMarket.Tests;

public class LogiMarketTests_PatronesEstructurales
{
    [Fact(DisplayName = "P16: Adapter - Proveedor legado adaptado a IPasarelaPago")]
    public async Task P16_Adapter_ProveedorLegado()
    {
        var simuladorLegado = new PasarelaLegadaSimulada();
        IPasarelaPago adapter = new AdaptadorPasarelaLegada(simuladorLegado);
        IPasarelaPago moderno = new PasarelaModernaSimulada();

        // Ambos cumplen el mismo contrato con dólares
        var resLegado = await adapter.CobrarAsync(59.00m, "KEY-LEGACY-01", "Ventas");
        var resModerno = await moderno.CobrarAsync(59.00m, "KEY-MODERNA-01", "Ventas");

        Assert.True(resLegado.Aprobado);
        Assert.True(resModerno.Aprobado);
        Assert.StartsWith("TX-LEGACY", resLegado.CodigoTransaccion);
        Assert.StartsWith("TX-V2", resModerno.CodigoTransaccion);
    }

    [Fact(DisplayName = "P17: Bridge - Cuatro combinaciones independientes de comprobante y medio de salida")]
    public void P17_Bridge_CuatroCombinaciones()
    {
        var cliente = new Cliente("C001", "Ana", "123", CategoriaCliente.Vip, "123", "a@test.com");
        var pedido = new Pedido("PED-BRIDGE", cliente, TipoEntregaEnum.Estandar, 5.00m);
        pedido.Lineas.Add(new PedidoDetalle("P01", "Mouse", false, 1, 20.00m));
        pedido.RecalcularTotales(2.00m, "VIP");

        var devolucion = new Devolucion(pedido.Id, MotivoDevolucionEnum.Defecto, 10, false, true, 18.00m, true, "OK");

        ISalidaDocumento vistaPrevia = new SalidaVistaPrevia();
        ISalidaDocumento archivoTxt = new SalidaArchivoTxt();

        // Combinación 1: Venta x VistaPrevia
        var v1 = new ComprobanteVenta(pedido, vistaPrevia).Generar();
        Assert.Contains("[VISTA PREVIA EN PANTALLA]", v1);
        Assert.Contains("PED-BRIDGE", v1);

        // Combinación 2: Venta x ArchivoTXT
        var rutaTxtVenta = Path.Combine(Path.GetTempPath(), "test_venta.txt");
        var v2 = new ComprobanteVenta(pedido, archivoTxt).Generar(rutaTxtVenta);
        Assert.Contains("Archivo TXT generado", v2);
        Assert.True(File.Exists(rutaTxtVenta));
        File.Delete(rutaTxtVenta);

        // Combinación 3: Devolución x VistaPrevia
        var v3 = new ComprobanteDevolucion(devolucion, pedido, vistaPrevia).Generar();
        Assert.Contains("COMPROBANTE DE DEVOLUCIÓN", v3);

        // Combinación 4: Devolución x ArchivoTXT
        var rutaTxtDev = Path.Combine(Path.GetTempPath(), "test_dev.txt");
        var v4 = new ComprobanteDevolucion(devolucion, pedido, archivoTxt).Generar(rutaTxtDev);
        Assert.Contains("Archivo TXT generado", v4);
        Assert.True(File.Exists(rutaTxtDev));
        File.Delete(rutaTxtDev);
    }

    [Fact(DisplayName = "P18: Composite - Kits anidados con precios y detección de ciclos")]
    public void P18_Composite_KitsAnidadosYAntiCiclo()
    {
        var p01 = new ProductoSimple("P01", "Mouse", 20.00m, 0.20m);
        var p02 = new ProductoSimple("P02", "Teclado", 30.00m, 0.70m);
        var p03 = new ProductoSimple("P03", "Memoria USB", 10.00m, 0.10m);

        // KIT_OFICINA = P01 + P02 = $50.00
        var kitOficina = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
        kitOficina.AgregarComponente(p01, 1);
        kitOficina.AgregarComponente(p02, 1);

        Assert.Equal(50.00m, kitOficina.ObtenerPrecio());

        // KIT_TELETRABAJO = KIT_OFICINA + P03 = $60.00
        var kitTeletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
        kitTeletrabajo.AgregarComponente(kitOficina, 1);
        kitTeletrabajo.AgregarComponente(p03, 1);

        Assert.Equal(60.00m, kitTeletrabajo.ObtenerPrecio());

        // Detección de ciclo directo: kitOficina agregar kitOficina
        Assert.Throws<DomainException>(() => kitOficina.AgregarComponente(kitOficina));

        // Detección de ciclo indirecto: kitOficina agregar kitTeletrabajo
        Assert.Throws<DomainException>(() => kitOficina.AgregarComponente(kitTeletrabajo));
    }

    [Fact(DisplayName = "P19: Decorator - Trazas de auditoría INICIO y FIN sin alterar resultado")]
    public async Task P19_Decorator_AuditoriaPago()
    {
        var pasarela = new PasarelaModernaSimulada();
        var decorator = new AuditoriaPagoDecorator(pasarela);

        var res = await decorator.CobrarAsync(50.00m, "KEY-DEC-01", "Ventas");

        Assert.True(res.Aprobado);
        Assert.Equal(2, decorator.TrazasAuditoria.Count);
        Assert.Contains("INICIO_COBRO", decorator.TrazasAuditoria[0]);
        Assert.Contains("FIN_COBRO", decorator.TrazasAuditoria[1]);
    }

    [Fact(DisplayName = "P21: Flyweight - 1.000 filas comparten instancias de metadatos (ReferenceEquals true)")]
    public void P21_Flyweight_ComparticionInstancias()
    {
        var factory = new CatalogoVisualFlyweightFactory();
        var filas = factory.GenerarRenglonesDemostracion(1000);

        Assert.Equal(1000, filas.Count);
        // Solo deben haberse instanciado 5 objetos de metadatos para las 1000 filas
        Assert.Equal(5, factory.TotalInstanciasCreadas);

        // La fila 0 y la fila 5 corresponden a P01; sus metadatos deben ser la misma referencia
        Assert.True(ReferenceEquals(filas[0].Metadatos, filas[5].Metadatos));
    }

    [Fact(DisplayName = "P22: Proxy - Rol CONSULTA o BODEGA deniega cobro antes del Decorator")]
    public async Task P22_Proxy_DenegacionRolNoAutorizado()
    {
        var pasarela = new PasarelaModernaSimulada();
        var decorator = new AuditoriaPagoDecorator(pasarela);
        var proxy = new PagoAutorizadoProxy(decorator);

        // Intento con CONSULTA
        var resConsulta = await proxy.CobrarAsync(50.00m, "KEY-P22", "Consulta");
        Assert.False(resConsulta.Aprobado);
        Assert.Contains("Acceso denegado", resConsulta.Mensaje);

        // Intento con BODEGA
        var resBodega = await proxy.CobrarAsync(50.00m, "KEY-P22", "Bodega");
        Assert.False(resBodega.Aprobado);

        // El decorador NO debe tener ningún log porque el Proxy detuvo la llamada antes
        Assert.Empty(decorator.TrazasAuditoria);

        // Intento con VENTAS -> Autorizado
        var resVentas = await proxy.CobrarAsync(50.00m, "KEY-P22", "Ventas");
        Assert.True(resVentas.Aprobado);
        Assert.NotEmpty(decorator.TrazasAuditoria);
    }
}
