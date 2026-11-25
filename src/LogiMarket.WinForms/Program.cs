using LogiMarket.Application.Patterns.ChainOfResponsibility;
using LogiMarket.Application.Patterns.Facade;
using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Patterns.Observer;
using LogiMarket.Application.Patterns.Singleton;
using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Infrastructure.Patterns.Adapters;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Repositories;
using LogiMarket.WinForms.Mediator;
using LogiMarket.WinForms.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        // 0. Captura global de excepciones no controladas para evitar el diálogo crudo de .NET
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[ThreadException] {e.Exception}");
            MessageBox.Show(
                $"Ha ocurrido una situación no controlada:\n\n{e.Exception.Message}",
                "LogiMarket 360 - Notificación del Sistema",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[UnhandledException] {ex}");
            MessageBox.Show(
                $"Error crítico no recuperable:\n\n{ex?.Message ?? "Error desconocido"}",
                "LogiMarket 360 - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();

        // Inicializar persistencia y catálogo seed
        using (var scope = ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LogiMarketDbContext>();
            DbInitializer.InicializarAsync(db).GetAwaiter().GetResult();

            var catalogo = scope.ServiceProvider.GetRequiredService<CatalogoService>();
            catalogo.InicializarKitsOficialesAsync().GetAwaiter().GetResult();
        }

        var mainForm = ServiceProvider.GetRequiredService<Form1>();
        System.Windows.Forms.Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Persistencia SQLite
        services.AddDbContext<LogiMarketDbContext>(options =>
        {
            options.UseSqlite("Data Source=logimarket360.db");
        });

        // 2. Repositorios y Unit of Work
        services.AddScoped<IProductoRepository, EfProductoRepository>();
        services.AddScoped<IClienteRepository, EfClienteRepository>();
        services.AddScoped<IPedidoRepository, EfPedidoRepository>();
        services.AddScoped<IPagoRepository, EfPagoRepository>();
        services.AddScoped<IMovimientoInventarioRepository, EfMovimientoInventarioRepository>();
        services.AddScoped<IDevolucionRepository, EfDevolucionRepository>();
        services.AddScoped<IAuditoriaRepository, EfAuditoriaRepository>();
        services.AddScoped<IEventoOutboxRepository, EfEventoOutboxRepository>();
        services.AddScoped<IUsuarioRepository, EfUsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3. Patrones y Servicios
        services.AddSingleton<ISesionContext, SesionContext>();
        services.AddSingleton<MetricasSesion>();
        services.AddSingleton<IFormularioMediator, FormularioPrincipalMediator>();

        services.AddScoped<AuthService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<PipelineValidacionPedido>();

        // Observer
        services.AddSingleton<IPublicadorEventos>(sp =>
        {
            var pub = new PublicadorEventos();
            var metricas = sp.GetRequiredService<MetricasSesion>();
            pub.Suscribir<EventoPedidoPagado>(new MetricasObserver(metricas));
            pub.Suscribir<EventoDespachoRegistrado>(new MetricasObserver(metricas));
            pub.Suscribir<EventoDevolucionAceptada>(new MetricasObserver(metricas));
            return pub;
        });

        // Factory Method
        services.AddSingleton<INotificadorCreator, EmailNotificadorCreator>();

        // Adapter, Decorator y Proxy
        services.AddScoped<IPasarelaPago>(sp =>
        {
            var legacySimulator = new PasarelaLegadaSimulada();
            IPasarelaPago adapter = new AdaptadorPasarelaLegada(legacySimulator);
            IPasarelaPago decorator = new AuditoriaPagoDecorator(adapter);
            IPasarelaPago proxy = new PagoAutorizadoProxy(decorator);
            return proxy;
        });

        // Facade
        services.AddScoped<ConfirmacionPedidoFacade>();

        // 4. Vistas WinForms
        services.AddTransient<UcAcceso>();
        services.AddTransient<UcPanelGeneral>();
        services.AddTransient<UcCatalogoKits>();
        services.AddTransient<UcClientes>();
        services.AddTransient<UcCarritoBorrador>();
        services.AddTransient<UcPedidosHistorial>();
        services.AddTransient<UcCobro>();
        services.AddTransient<UcDespacho>();
        services.AddTransient<UcDevoluciones>();
        services.AddTransient<UcInventario>();
        services.AddTransient<UcReportes>();
        services.AddTransient<UcAuditoriaSimulacion>();

        // Ventana principal
        services.AddTransient<Form1>();
    }
}
