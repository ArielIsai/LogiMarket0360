using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogiMarket.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LogiMarketDbContext _context;
    private IDbContextTransaction? _transaccion;

    public UnitOfWork(LogiMarketDbContext context)
    {
        _context = context;
    }

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaccion == null)
            _transaccion = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaccion != null)
        {
            await _transaccion.CommitAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaccion != null)
        {
            await _transaccion.RollbackAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }
    }
}

public class EfProductoRepository : IProductoRepository
{
    private readonly LogiMarketDbContext _context;
    public EfProductoRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Producto?> GetByIdAsync(int id) => await _context.Productos.FindAsync(id);
    public async Task<Producto?> GetBySkuAsync(string sku) =>
        await _context.Productos.FirstOrDefaultAsync(p => p.Sku.ToUpper() == sku.Trim().ToUpper());
    public async Task<IReadOnlyList<Producto>> GetAllAsync() => await _context.Productos.ToListAsync();
    public async Task<IReadOnlyList<Producto>> BuscarAsync(string criterio) =>
        await _context.Productos.Where(p => p.Sku.Contains(criterio) || p.Nombre.Contains(criterio) || p.Categoria.Contains(criterio)).ToListAsync();
    public async Task AddAsync(Producto entity) => await _context.Productos.AddAsync(entity);
    public Task UpdateAsync(Producto entity) { _context.Productos.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var p = await GetByIdAsync(id); if (p != null) _context.Productos.Remove(p); }
}

public class EfClienteRepository : IClienteRepository
{
    private readonly LogiMarketDbContext _context;
    public EfClienteRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Cliente?> GetByIdAsync(int id) => await _context.Clientes.FindAsync(id);
    public async Task<Cliente?> GetByCodigoAsync(string codigo) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigo.Trim().ToUpper());
    public async Task<Cliente?> GetByIdentificacionAsync(string identificacion) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Identificacion.Trim() == identificacion.Trim());
    public async Task<IReadOnlyList<Cliente>> GetAllAsync() => await _context.Clientes.ToListAsync();
    public async Task<IReadOnlyList<Cliente>> BuscarAsync(string criterio) =>
        await _context.Clientes.Where(c => c.Codigo.Contains(criterio) || c.Nombre.Contains(criterio) || c.Identificacion.Contains(criterio)).ToListAsync();
    public async Task AddAsync(Cliente entity) => await _context.Clientes.AddAsync(entity);
    public Task UpdateAsync(Cliente entity) { _context.Clientes.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var c = await GetByIdAsync(id); if (c != null) _context.Clientes.Remove(c); }
}

public class EfPedidoRepository : IPedidoRepository
{
    private readonly LogiMarketDbContext _context;
    public EfPedidoRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Pedido?> GetByIdAsync(int id) => await _context.Pedidos.FindAsync(id);

    public async Task<Pedido?> GetByCodigoAsync(string codigo) =>
        await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Lineas)
                .ThenInclude(l => l.ComponentesFisicos)
            .FirstOrDefaultAsync(p => p.Codigo.ToUpper() == codigo.Trim().ToUpper());

    public async Task<Pedido?> GetByIdWithDetailsAsync(int id) =>
        await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Lineas)
                .ThenInclude(l => l.ComponentesFisicos)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Pedido>> GetAllWithDetailsAsync() =>
        await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Lineas)
                .ThenInclude(l => l.ComponentesFisicos)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();

    public async Task<Pedido?> GetByIdempotencyKeyAsync(string idempotencyKey) =>
        await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Lineas)
                .ThenInclude(l => l.ComponentesFisicos)
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);

    public async Task<IReadOnlyList<Pedido>> GetByEstadoAsync(EstadoPedidoEnum estado) =>
        await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Lineas)
            .Where(p => p.Estado == estado)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();

    public async Task<IReadOnlyList<Pedido>> GetAllAsync() => await GetAllWithDetailsAsync();
    public async Task AddAsync(Pedido entity) => await _context.Pedidos.AddAsync(entity);
    public Task UpdateAsync(Pedido entity) { _context.Pedidos.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var p = await GetByIdAsync(id); if (p != null) _context.Pedidos.Remove(p); }
}

public class EfPagoRepository : IPagoRepository
{
    private readonly LogiMarketDbContext _context;
    public EfPagoRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Pago?> GetByIdAsync(int id) => await _context.Pagos.FindAsync(id);
    public async Task<Pago?> GetByPedidoIdAsync(int pedidoId) =>
        await _context.Pagos.FirstOrDefaultAsync(p => p.PedidoId == pedidoId);
    public async Task<Pago?> GetByIdempotencyKeyAsync(string idempotencyKey) =>
        await _context.Pagos.FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
    public async Task<IReadOnlyList<Pago>> GetAllAsync() => await _context.Pagos.ToListAsync();
    public async Task AddAsync(Pago entity) => await _context.Pagos.AddAsync(entity);
    public Task UpdateAsync(Pago entity) { _context.Pagos.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var p = await GetByIdAsync(id); if (p != null) _context.Pagos.Remove(p); }
}

public class EfMovimientoInventarioRepository : IMovimientoInventarioRepository
{
    private readonly LogiMarketDbContext _context;
    public EfMovimientoInventarioRepository(LogiMarketDbContext context) => _context = context;

    public async Task<MovimientoInventario?> GetByIdAsync(int id) => await _context.MovimientosInventario.FindAsync(id);
    public async Task<IReadOnlyList<MovimientoInventario>> GetAllAsync() =>
        await _context.MovimientosInventario.OrderByDescending(m => m.Fecha).ToListAsync();
    public async Task<IReadOnlyList<MovimientoInventario>> GetHistorialAsync(string? sku = null)
    {
        var query = _context.MovimientosInventario.AsQueryable();
        if (!string.IsNullOrWhiteSpace(sku))
            query = query.Where(m => m.ProductoSku.ToUpper() == sku.Trim().ToUpper());
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }
    public async Task AddAsync(MovimientoInventario entity) => await _context.MovimientosInventario.AddAsync(entity);
    public Task UpdateAsync(MovimientoInventario entity) { _context.MovimientosInventario.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var m = await GetByIdAsync(id); if (m != null) _context.MovimientosInventario.Remove(m); }
}

public class EfDevolucionRepository : IDevolucionRepository
{
    private readonly LogiMarketDbContext _context;
    public EfDevolucionRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Devolucion?> GetByIdAsync(int id) => await _context.Devoluciones.FindAsync(id);
    public async Task<Devolucion?> GetByPedidoIdAsync(int pedidoId) =>
        await _context.Devoluciones.FirstOrDefaultAsync(d => d.PedidoId == pedidoId);
    public async Task<IReadOnlyList<Devolucion>> GetAllAsync() =>
        await _context.Devoluciones.OrderByDescending(d => d.FechaSolicitud).ToListAsync();
    public async Task AddAsync(Devolucion entity) => await _context.Devoluciones.AddAsync(entity);
    public Task UpdateAsync(Devolucion entity) { _context.Devoluciones.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var d = await GetByIdAsync(id); if (d != null) _context.Devoluciones.Remove(d); }
}

public class EfAuditoriaRepository : IAuditoriaRepository
{
    private readonly LogiMarketDbContext _context;
    public EfAuditoriaRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Auditoria?> GetByIdAsync(int id) => await _context.Auditorias.FindAsync(id);
    public async Task<IReadOnlyList<Auditoria>> GetAllAsync() =>
        await _context.Auditorias.OrderByDescending(a => a.Fecha).ToListAsync();
    public async Task<IReadOnlyList<Auditoria>> GetUltimosAsync(int cantidad = 100) =>
        await _context.Auditorias.OrderByDescending(a => a.Fecha).Take(cantidad).ToListAsync();
    public async Task AddAsync(Auditoria entity) => await _context.Auditorias.AddAsync(entity);
    public Task UpdateAsync(Auditoria entity) { _context.Auditorias.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var a = await GetByIdAsync(id); if (a != null) _context.Auditorias.Remove(a); }
}

public class EfEventoOutboxRepository : IEventoOutboxRepository
{
    private readonly LogiMarketDbContext _context;
    public EfEventoOutboxRepository(LogiMarketDbContext context) => _context = context;

    public async Task<EventoOutbox?> GetByIdAsync(int id) => await _context.EventosOutbox.FindAsync(id);
    public async Task<IReadOnlyList<EventoOutbox>> GetAllAsync() => await _context.EventosOutbox.ToListAsync();
    public async Task<IReadOnlyList<EventoOutbox>> GetPendientesAsync(int limite = 50) =>
        await _context.EventosOutbox.Where(e => !e.Procesado).OrderBy(e => e.FechaCreacion).Take(limite).ToListAsync();
    public async Task AddAsync(EventoOutbox entity) => await _context.EventosOutbox.AddAsync(entity);
    public Task UpdateAsync(EventoOutbox entity) { _context.EventosOutbox.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var e = await GetByIdAsync(id); if (e != null) _context.EventosOutbox.Remove(e); }
}

public class EfUsuarioRepository : IUsuarioRepository
{
    private readonly LogiMarketDbContext _context;
    public EfUsuarioRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Usuario?> GetByIdAsync(int id) => await _context.Usuarios.FindAsync(id);
    public async Task<Usuario?> GetByUsernameAsync(string username) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username.Trim().ToLowerInvariant());
    public async Task<IReadOnlyList<Usuario>> GetAllAsync() => await _context.Usuarios.ToListAsync();
    public async Task AddAsync(Usuario entity) => await _context.Usuarios.AddAsync(entity);
    public Task UpdateAsync(Usuario entity) { _context.Usuarios.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var u = await GetByIdAsync(id); if (u != null) _context.Usuarios.Remove(u); }
}
