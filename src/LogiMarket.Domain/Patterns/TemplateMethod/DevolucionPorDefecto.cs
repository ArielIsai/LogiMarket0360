using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.TemplateMethod;

public class DevolucionPorDefecto : ProcesarDevolucionTemplate
{
    public override MotivoDevolucionEnum Motivo => MotivoDevolucionEnum.Defecto;

    protected override bool VerificarPlazoElegible(int diasDesdeEntrega, out string mensaje)
    {
        if (diasDesdeEntrega <= 30)
        {
            mensaje = "Plazo de hasta 30 días cumplido.";
            return true;
        }

        mensaje = $"El plazo para devolución por defecto es de hasta 30 días tras la entrega. Días transcurridos: {diasDesdeEntrega}.";
        return false;
    }

    protected override bool InspeccionarCondicion(bool productoSinUso, out string mensaje)
    {
        // En defecto técnico se acepta aunque haya sido abierto/probado
        mensaje = "Condición de defecto de fábrica aceptada.";
        return true;
    }

    protected override bool DebeReintegrarStock()
    {
        // Por defecto se reintegra para control o cuarentena
        return true;
    }
}
