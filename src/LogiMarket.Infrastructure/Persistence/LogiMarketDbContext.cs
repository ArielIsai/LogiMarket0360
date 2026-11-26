using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence;

public class LogiMarketDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
    public DbSet<PedidoDetalleComponente> PedidoDetalleComponentes => Set<PedidoDetalleComponente>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<EventoOutbox> EventosOutbox => Set<EventoOutbox>();

    public LogiMarketDbContext(DbContextOptions<LogiMarketDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Username).IsUnique();
            b.Property(u => u.Username).HasMaxLength(50).IsRequired();
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.Salt).IsRequired();
            b.Property(u => u.NombreCompleto).HasMaxLength(150);
        });

        // Cliente
        modelBuilder.Entity<Cliente>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Codigo).IsUnique();
            b.HasIndex(c => c.Identificacion).IsUnique();
            b.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
            b.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            b.Property(c => c.Identificacion).HasMaxLength(30).IsRequired();
        });

        // Producto
        modelBuilder.Entity<Producto>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.Sku).IsUnique();
            b.Property(p => p.Sku).HasMaxLength(20).IsRequired();
            b.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            b.Property(p => p.Precio).HasPrecision(18, 2);
            b.Property(p => p.PesoKg).HasPrecision(18, 2);
        });

        // Pedido
        modelBuilder.Entity<Pedido>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.Codigo).IsUnique();
            b.HasIndex(p => p.IdempotencyKey).IsUnique();
            b.Property(p => p.Codigo).HasMaxLength(50).IsRequired();
            b.Property(p => p.Subtotal).HasPrecision(18, 2);
            b.Property(p => p.Descuento).HasPrecision(18, 2);
            b.Property(p => p.TarifaEnvio).HasPrecision(18, 2);
            b.Property(p => p.Total).HasPrecision(18, 2);

            b.HasOne(p => p.Cliente)
             .WithMany()
             .HasForeignKey(p => p.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(p => p.Lineas)
             .WithOne()
             .HasForeignKey(l => l.PedidoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PedidoDetalle
        modelBuilder.Entity<PedidoDetalle>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Sku).HasMaxLength(20).IsRequired();
            b.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            b.Property(d => d.Subtotal).HasPrecision(18, 2);

            b.HasMany(d => d.ComponentesFisicos)
             .WithOne()
             .HasForeignKey(c => c.PedidoDetalleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PedidoDetalleComponente
        modelBuilder.Entity<PedidoDetalleComponente>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.SkuFisico).HasMaxLength(20).IsRequired();
            b.Property(c => c.PrecioUnitarioSnapshot).HasPrecision(18, 2);
            b.Property(c => c.PesoUnitarioKgSnapshot).HasPrecision(18, 2);
        });

        // Pago
        modelBuilder.Entity<Pago>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.CodigoTransaccion).IsUnique();
            b.HasIndex(p => p.IdempotencyKey).IsUnique();
            b.Property(p => p.Monto).HasPrecision(18, 2);
        });

        // Despacho
        modelBuilder.Entity<Despacho>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.NumeroGuia).IsUnique();
        });

        // Devolución: Restricción de unicidad estricta (una sola devolución por pedido)
        modelBuilder.Entity<Devolucion>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.PedidoId).IsUnique();
            b.Property(d => d.ReembolsoMonto).HasPrecision(18, 2);
        });

        // Movimiento de Inventario
        modelBuilder.Entity<MovimientoInventario>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.ProductoSku);
        });

        // Auditoría
        modelBuilder.Entity<Auditoria>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.Fecha);
        });

        // Evento Outbox
        modelBuilder.Entity<EventoOutbox>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.Procesado);
        });
    }
}
