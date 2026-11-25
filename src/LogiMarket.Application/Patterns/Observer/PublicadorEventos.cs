namespace LogiMarket.Application.Patterns.Observer;

public class PublicadorEventos : IPublicadorEventos
{
    private readonly Dictionary<Type, List<object>> _suscriptores = new();

    public void Suscribir<T>(ISuscriptorEvento<T> suscriptor)
    {
        var tipo = typeof(T);
        if (!_suscriptores.ContainsKey(tipo))
        {
            _suscriptores[tipo] = new List<object>();
        }
        _suscriptores[tipo].Add(suscriptor);
    }

    public async Task PublicarAsync<T>(T evento)
    {
        var tipo = typeof(T);
        if (_suscriptores.TryGetValue(tipo, out var lista))
        {
            foreach (var suscriptor in lista.OfType<ISuscriptorEvento<T>>())
            {
                await suscriptor.ManejarAsync(evento);
            }
        }
    }
}
