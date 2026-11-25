using LogiMarket.Application.Patterns.ChainOfResponsibility;
using LogiMarket.Application.Patterns.Facade;
using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Patterns.Observer;
using LogiMarket.Application.Patterns.Singleton;
using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Patterns.Adapters;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Tests;

public static class TestHelpers
{
    public static (LogiMarketDbContext Context, SqliteConnection Connection) CrearContextoSqliteMemoria()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LogiMarketDbContext(options);
        context.Database.EnsureCreated();

        DbInitializer.InicializarAsync(context).GetAwaiter().GetResult();
        return (context, connection);
    }

    public static (
        ConfirmacionPedidoFacade Facade,
        LogiMarketDbContext Db,
        PublicadorEventos Publicador,
        MetricasSesion Metricas,
        SqliteConnection Conn
    ) CrearSujetoPruebaConfirmacion()
    {
        var (db, conn) = CrearContextoSqliteMemoria();

        var pedidoRepo = new EfPedidoRepository(db);
        var productoRepo = new EfProductoRepository(db);
        var pagoRepo = new EfPagoRepository(db);
        var movRepo = new EfMovimientoInventarioRepository(db);
        var outboxRepo = new EfEventoOutboxRepository(db);
        var auditoriaRepo = new EfAuditoriaRepository(db);
        var clienteRepo = new EfClienteRepository(db);
        var uow = new UnitOfWork(db);

        var pipeline = new PipelineValidacionPedido(clienteRepo, productoRepo);

        // Pasarela con Adapter, Decorator y Proxy
        var legacySimulator = new PasarelaLegadaSimulada();
        IPasarelaPago adapter = new AdaptadorPasarelaLegada(legacySimulator);
        IPasarelaPago decorator = new AuditoriaPagoDecorator(adapter);
        IPasarelaPago proxy = new PagoAutorizadoProxy(decorator);

        // Observer
        var publicador = new PublicadorEventos();
        var metricas = new MetricasSesion();
        publicador.Suscribir<EventoPedidoPagado>(new MetricasObserver(metricas));
        publicador.Suscribir<EventoPedidoPagado>(new AuditoriaObserver(auditoriaRepo, uow));

        var facade = new ConfirmacionPedidoFacade(
            pedidoRepo,
            productoRepo,
            pagoRepo,
            movRepo,
            outboxRepo,
            auditoriaRepo,
            uow,
            pipeline,
            proxy,
            publicador
        );

        return (facade, db, publicador, metricas, conn);
    }
}
