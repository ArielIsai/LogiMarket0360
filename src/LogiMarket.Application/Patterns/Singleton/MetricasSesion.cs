namespace LogiMarket.Application.Patterns.Singleton;

public class MetricasSesion
{
    private static readonly object _lock = new();
    private static MetricasSesion? _instancia;

    public int PedidosConfirmados { get; private set; }
    public decimal VentasTotalesSesion { get; private set; }
    public int DespachosRegistrados { get; private set; }
    public int DevolucionesProcesadas { get; private set; }
    public int CobrosRechazados { get; private set; }
    public int EventosOutboxProcesados { get; private set; }
    public DateTime InicioSesion { get; } = DateTime.UtcNow;

    // Constructor público para uso en Microsoft.Extensions.DependencyInjection como Singleton
    public MetricasSesion() { }

    // Acceso estático tradicional GoF si se requiere instancia directa
    public static MetricasSesion ObtenerInstancia()
    {
        if (_instancia == null)
        {
            lock (_lock)
            {
                _instancia ??= new MetricasSesion();
            }
        }
        return _instancia;
    }

    public void RegistrarVentaExitosa(decimal monto)
    {
        lock (_lock)
        {
            PedidosConfirmados++;
            VentasTotalesSesion += monto;
        }
    }

    public void RegistrarDespacho()
    {
        lock (_lock)
        {
            DespachosRegistrados++;
        }
    }

    public void RegistrarDevolucion(decimal montoReembolsado)
    {
        lock (_lock)
        {
            DevolucionesProcesadas++;
            VentasTotalesSesion -= montoReembolsado;
        }
    }

    public void RegistrarCobroRechazado()
    {
        lock (_lock)
        {
            CobrosRechazados++;
        }
    }

    public void RegistrarEventoOutboxProcesado()
    {
        lock (_lock)
        {
            EventosOutboxProcesados++;
        }
    }

    public void Resetear()
    {
        lock (_lock)
        {
            PedidosConfirmados = 0;
            VentasTotalesSesion = 0m;
            DespachosRegistrados = 0;
            DevolucionesProcesadas = 0;
            CobrosRechazados = 0;
            EventosOutboxProcesados = 0;
        }
    }
}
