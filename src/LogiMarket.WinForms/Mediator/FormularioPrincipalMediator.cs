namespace LogiMarket.WinForms.Mediator;

public interface IFormularioMediator
{
    void Notificar(string evento, object? datos = null);
    void RegistrarColega(string nombre, Action<string, object?> accion);
}

public class FormularioPrincipalMediator : IFormularioMediator
{
    private readonly Dictionary<string, Action<string, object?>> _colegas = new();

    public void RegistrarColega(string nombre, Action<string, object?> accion)
    {
        _colegas[nombre] = accion;
    }

    public void Notificar(string evento, object? datos = null)
    {
        // Distribuye las notificaciones a todos los colegas registrados sin acoplamiento directo entre vistas
        foreach (var colega in _colegas.Values)
        {
            colega(evento, datos);
        }
    }
}
