using LogiMarket.Application.DTOs;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Infrastructure.Patterns.Adapters;

// --- 1. PATRÓN ADAPTER ---
// Clase Legada incompatible: cobra en centavos (long) y requiere string de referencia
public class PasarelaLegadaSimulada
{
    public string CobrarCentavos(long montoCentavos, string referencia)
    {
        if (montoCentavos <= 0)
            throw new ArgumentException("El monto en centavos debe ser positivo.");

        // Simula cobro bancario legado exitoso
        var codigoTx = $"TX-LEGACY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6]}";
        return $"{codigoTx}|APROBADO|MontoCentavos:{montoCentavos}|Ref:{referencia}";
    }
}

// Adaptador que convierte IPasarelaPago -> PasarelaLegadaSimulada
public class AdaptadorPasarelaLegada : IPasarelaPago
{
    private readonly PasarelaLegadaSimulada _pasarelaLegada;

    public string NombreProveedor => "Pasarela Legacy (Simulada)";

    public AdaptadorPasarelaLegada(PasarelaLegadaSimulada pasarelaLegada)
    {
        _pasarelaLegada = pasarelaLegada;
    }

    public Task<ResultadoPagoDto> CobrarAsync(decimal monto, string claveIdempotencia, string usuarioRol)
    {
        // Conversión de dólares a centavos requerida por el sistema legado
        long centavos = (long)Math.Round(monto * 100m, MidpointRounding.AwayFromZero);

        try
        {
            var respuestaRaw = _pasarelaLegada.CobrarCentavos(centavos, claveIdempotencia);
            var partes = respuestaRaw.Split('|');
            var codigoTx = partes.Length > 0 ? partes[0] : Guid.NewGuid().ToString("N");

            return Task.FromResult(new ResultadoPagoDto(
                true,
                codigoTx,
                NombreProveedor,
                "Cobro simulado aprobado vía Adaptador Legacy.",
                monto
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ResultadoPagoDto(
                false,
                string.Empty,
                NombreProveedor,
                $"Error en pasarela legada: {ex.Message}",
                monto
            ));
        }
    }
}

// Proveedor Moderno Alternativo (segunda implementación intercambiable por DI)
public class PasarelaModernaSimulada : IPasarelaPago
{
    public string NombreProveedor => "Pasarela V2 Moderna (Simulada)";

    public Task<ResultadoPagoDto> CobrarAsync(decimal monto, string claveIdempotencia, string usuarioRol)
    {
        var codigoTx = $"TX-V2-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        return Task.FromResult(new ResultadoPagoDto(
            true,
            codigoTx,
            NombreProveedor,
            "Cobro aprobado exitosamente mediante API REST V2 simulada.",
            monto
        ));
    }
}

// --- 2. PATRÓN DECORATOR ---
// AuditoriaPagoDecorator: Envoltorio que añade trazas INICIO -> PASARELA -> FIN/ERROR con ID de correlación
public class AuditoriaPagoDecorator : IPasarelaPago
{
    private readonly IPasarelaPago _innerPasarela;
    private readonly List<string> _trazasAuditoria = new();

    public string NombreProveedor => _innerPasarela.NombreProveedor;
    public IReadOnlyList<string> TrazasAuditoria => _trazasAuditoria.AsReadOnly();

    public AuditoriaPagoDecorator(IPasarelaPago innerPasarela)
    {
        _innerPasarela = innerPasarela;
    }

    public async Task<ResultadoPagoDto> CobrarAsync(decimal monto, string claveIdempotencia, string usuarioRol)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var logInicio = $"[CORRELATION-{correlationId}] INICIO_COBRO | Monto: USD {monto:F2} | Clave: {claveIdempotencia} | Proveedor: {NombreProveedor} | Rol: {usuarioRol}";
        _trazasAuditoria.Add(logInicio);

        try
        {
            var resultado = await _innerPasarela.CobrarAsync(monto, claveIdempotencia, usuarioRol);

            var logFin = $"[CORRELATION-{correlationId}] FIN_COBRO | Aprobado: {resultado.Aprobado} | CodigoTx: {resultado.CodigoTransaccion} | Mensaje: {resultado.Mensaje}";
            _trazasAuditoria.Add(logFin);

            return resultado;
        }
        catch (Exception ex)
        {
            var logError = $"[CORRELATION-{correlationId}] ERROR_COBRO | Excepción: {ex.Message}";
            _trazasAuditoria.Add(logError);
            throw;
        }
    }
}

// --- 3. PATRÓN PROXY ---
// PagoAutorizadoProxy: Verifica permisos de rol (VENTAS o ADMIN) antes de invocar la cadena Decorator/Adapter
public class PagoAutorizadoProxy : IPasarelaPago
{
    private readonly IPasarelaPago _innerPasarela;

    public string NombreProveedor => _innerPasarela.NombreProveedor;

    public PagoAutorizadoProxy(IPasarelaPago innerPasarela)
    {
        _innerPasarela = innerPasarela;
    }

    public async Task<ResultadoPagoDto> CobrarAsync(decimal monto, string claveIdempotencia, string usuarioRol)
    {
        // Verificación estricta de autorización antes de alcanzar pasarela o auditoría de cobro
        bool esAutorizado = usuarioRol.Equals("Ventas", StringComparison.OrdinalIgnoreCase) ||
                           usuarioRol.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (!esAutorizado)
        {
            // Rechazo directo del Proxy: no se alcanza al Decorator ni al Adapter
            return new ResultadoPagoDto(
                false,
                string.Empty,
                NombreProveedor,
                $"Acceso denegado: El rol '{usuarioRol}' no tiene autorización para realizar cobros. Se requiere VENTAS o ADMIN.",
                monto
            );
        }

        return await _innerPasarela.CobrarAsync(monto, claveIdempotencia, usuarioRol);
    }
}
