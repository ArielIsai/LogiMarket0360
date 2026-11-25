using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Patterns.TemplateMethod;

public class DevolucionResultado
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public decimal MontoReembolso { get; set; }
    public bool ReintegrarStock { get; set; }
    public Devolucion? DevolucionGenerada { get; set; }
}
