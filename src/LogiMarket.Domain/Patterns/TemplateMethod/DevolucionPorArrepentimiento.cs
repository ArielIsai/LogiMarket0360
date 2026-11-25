using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.TemplateMethod;

public class DevolucionPorArrepentimiento : ProcesarDevolucionTemplate
{
    public override MotivoDevolucionEnum Motivo => MotivoDevolucionEnum.Arrepentimiento;

    protected override bool VerificarPlazoElegible(int diasDesdeEntrega, out string mensaje)
    {
        if (diasDesdeEntrega <= 7)
        {
            mensaje = "Plazo de hasta 7 días cumplido.";
            return true;
        }

        mensaje = $"El plazo para devolución por arrepentimiento es de máximo 7 días tras la entrega. Días transcurridos: {diasDesdeEntrega}.";
        return false;
    }

    protected override bool InspeccionarCondicion(bool productoSinUso, out string mensaje)
    {
        if (productoSinUso)
        {
            mensaje = "El producto se encuentra en perfecto estado y sin uso.";
            return true;
        }

        mensaje = "Rechazado: La devolución por arrepentimiento exige que el producto esté completamente sin uso.";
        return false;
    }

    protected override bool DebeReintegrarStock() => true;
}
