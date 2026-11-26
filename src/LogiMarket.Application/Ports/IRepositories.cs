using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Ports;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public interface IProductoRepository : IRepository<Producto>
{
    Task<Producto?> GetBySkuAsync(string sku);
    Task<IReadOnlyList<Producto>> BuscarAsync(string criterio);
}

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByCodigoAsync(string codigo);
    Task<Cliente?> GetByIdentificacionAsync(string identificacion);
    Task<IReadOnlyList<Cliente>> BuscarAsync(string criterio);
}

public interface IPedidoRepository : IRepository<Pedido>
{
    Task<Pedido?> GetByCodigoAsync(string codigo);
    Task<Pedido?> GetByIdWithDetailsAsync(int id);
    Task<IReadOnlyList<Pedido>> GetAllWithDetailsAsync();
    Task<Pedido?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IReadOnlyList<Pedido>> GetByEstadoAsync(EstadoPedidoEnum estado);
}

public interface IPagoRepository : IRepository<Pago>
{
    Task<Pago?> GetByPedidoIdAsync(int pedidoId);
    Task<Pago?> GetByIdempotencyKeyAsync(string idempotencyKey);
}

public interface IMovimientoInventarioRepository : IRepository<MovimientoInventario>
{
    Task<IReadOnlyList<MovimientoInventario>> GetHistorialAsync(string? sku = null);
}

public interface IDevolucionRepository : IRepository<Devolucion>
{
    Task<Devolucion?> GetByPedidoIdAsync(int pedidoId);
}

public interface IAuditoriaRepository : IRepository<Auditoria>
{
    Task<IReadOnlyList<Auditoria>> GetUltimosAsync(int cantidad = 100);
}

public interface IEventoOutboxRepository : IRepository<EventoOutbox>
{
    Task<IReadOnlyList<EventoOutbox>> GetPendientesAsync(int limite = 50);
}

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByUsernameAsync(string username);
}

public interface IUnitOfWork
{
    Task<int> CommitAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
