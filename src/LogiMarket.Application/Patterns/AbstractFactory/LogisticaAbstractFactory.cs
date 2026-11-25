namespace LogiMarket.Application.Patterns.AbstractFactory;

// Contratos de los productos de la familia
public interface ICalculadorTarifa
{
    string TipoFamilia { get; }
    decimal CalcularTarifa(decimal pesoTotalKg);
}

public interface ICalculadorPlazo
{
    int DiasHabiles { get; }
    DateTime CalcularFechaEntregaEstimada(DateTime fechaSalida);
}

public interface IConstructorGuia
{
    string GenerarNumeroGuia(string codigoPedido);
}

// Interfaz Fábrica Abstracta
public interface ILogisticaAbstractFactory
{
    string NombreFamilia { get; }
    ICalculadorTarifa CrearCalculadorTarifa();
    ICalculadorPlazo CrearCalculadorPlazo();
    IConstructorGuia CrearConstructorGuia();
}

// Familia 1: ESTÁNDAR (USD 5.00 / 3 días / Guía STD)
public class CalculadorTarifaEstandar : ICalculadorTarifa
{
    public string TipoFamilia => "ESTÁNDAR";
    public decimal CalcularTarifa(decimal pesoTotalKg) => 5.00m;
}

public class CalculadorPlazoEstandar : ICalculadorPlazo
{
    public int DiasHabiles => 3;
    public DateTime CalcularFechaEntregaEstimada(DateTime fechaSalida) => fechaSalida.AddDays(3);
}

public class ConstructorGuiaEstandar : IConstructorGuia
{
    public string GenerarNumeroGuia(string codigoPedido) => $"GUIA-STD-{codigoPedido}-{DateTime.UtcNow:yyyyMMdd}";
}

public class LogisticaEstandarFactory : ILogisticaAbstractFactory
{
    public string NombreFamilia => "ESTÁNDAR";
    public ICalculadorTarifa CrearCalculadorTarifa() => new CalculadorTarifaEstandar();
    public ICalculadorPlazo CrearCalculadorPlazo() => new CalculadorPlazoEstandar();
    public IConstructorGuia CrearConstructorGuia() => new ConstructorGuiaEstandar();
}

// Familia 2: EXPRÉS (USD 12.00 / 1 día / Guía EXP)
public class CalculadorTarifaExpres : ICalculadorTarifa
{
    public string TipoFamilia => "EXPRÉS";
    public decimal CalcularTarifa(decimal pesoTotalKg) => 12.00m;
}

public class CalculadorPlazoExpres : ICalculadorPlazo
{
    public int DiasHabiles => 1;
    public DateTime CalcularFechaEntregaEstimada(DateTime fechaSalida) => fechaSalida.AddDays(1);
}

public class ConstructorGuiaExpres : IConstructorGuia
{
    public string GenerarNumeroGuia(string codigoPedido) => $"GUIA-EXP-{codigoPedido}-{DateTime.UtcNow:yyyyMMdd}";
}

public class LogisticaExpresFactory : ILogisticaAbstractFactory
{
    public string NombreFamilia => "EXPRÉS";
    public ICalculadorTarifa CrearCalculadorTarifa() => new CalculadorTarifaExpres();
    public ICalculadorPlazo CrearCalculadorPlazo() => new CalculadorPlazoExpres();
    public IConstructorGuia CrearConstructorGuia() => new ConstructorGuiaExpres();
}
