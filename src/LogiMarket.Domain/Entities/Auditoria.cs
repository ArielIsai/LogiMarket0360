namespace LogiMarket.Domain.Entities;

public class Auditoria
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Detalle { get; set; } = string.Empty;

    public Auditoria() { }

    public Auditoria(string usuario, string accion, string entidad, string entidadId, string detalle)
    {
        Usuario = usuario;
        Accion = accion;
        Entidad = entidad;
        EntidadId = entidadId;
        Detalle = detalle;
    }
}
