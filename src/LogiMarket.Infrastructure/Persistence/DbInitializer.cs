using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InicializarAsync(LogiMarketDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Usuarios de prueba (con hash PBKDF2 y salt)
        if (!await context.Usuarios.AnyAsync())
        {
            var (adminHash, adminSalt) = AuthService.GenerarHash("admin123");
            var (ventasHash, ventasSalt) = AuthService.GenerarHash("ventas123");
            var (bodegaHash, bodegaSalt) = AuthService.GenerarHash("bodega123");
            var (consultaHash, consultaSalt) = AuthService.GenerarHash("consulta123");

            context.Usuarios.AddRange(
                new Usuario("admin.demo", adminHash, adminSalt, RolUsuario.Admin, "Administrador del Sistema"),
                new Usuario("ventas.demo", ventasHash, ventasSalt, RolUsuario.Ventas, "Ejecutivo de Ventas"),
                new Usuario("bodega.demo", bodegaHash, bodegaSalt, RolUsuario.Bodega, "Operador de Bodega"),
                new Usuario("consulta.demo", consultaHash, consultaSalt, RolUsuario.Consulta, "Auditor de Consulta")
            );
            await context.SaveChangesAsync();
        }

        // 2. Seed Productos oficiales de control
        if (!await context.Productos.AnyAsync())
        {
            context.Productos.AddRange(
                new Producto("P01", "Mouse USB", "Periféricos", 20.00m, 8, 0.20m),
                new Producto("P02", "Teclado", "Periféricos", 30.00m, 5, 0.70m),
                new Producto("P03", "Memoria USB", "Almacenamiento", 10.00m, 20, 0.10m),
                new Producto("P04", "Webcam", "Video", 40.00m, 4, 0.35m),
                new Producto("P05", "Alfombrilla", "Accesorios", 5.00m, 30, 0.10m)
            );
            await context.SaveChangesAsync();
        }

        // 3. Seed Clientes iniciales
        if (!await context.Clientes.AnyAsync())
        {
            context.Clientes.AddRange(
                new Cliente("C001", "Ana López", "1801234567", CategoriaCliente.Vip, "0991112233", "ana.lopez@ejemplo.test"),
                new Cliente("C002", "Luis Ruiz", "1809876543", CategoriaCliente.Normal, "0994445566", "luis.ruiz@ejemplo.test"),
                new Cliente("C003", "Carmen Mora", "1805556677", CategoriaCliente.Normal, "0997778899", "carmen.mora@ejemplo.test"),
                new Cliente("C004", "Sofía Díaz", "1803332211", CategoriaCliente.Normal, "0990001122", "sofia.diaz@ejemplo.test")
            );
            await context.SaveChangesAsync();
        }
    }

    public static async Task RestablecerBasePruebasAsync(LogiMarketDbContext context)
    {
        // Limpiar tablas operativas
        context.Devoluciones.RemoveRange(context.Devoluciones);
        context.Despachos.RemoveRange(context.Despachos);
        context.Pagos.RemoveRange(context.Pagos);
        context.PedidoDetalleComponentes.RemoveRange(context.PedidoDetalleComponentes);
        context.PedidoDetalles.RemoveRange(context.PedidoDetalles);
        context.Pedidos.RemoveRange(context.Pedidos);
        context.MovimientosInventario.RemoveRange(context.MovimientosInventario);
        context.EventosOutbox.RemoveRange(context.EventosOutbox);
        context.Auditorias.RemoveRange(context.Auditorias);

        // Restablecer existencias oficiales del catálogo
        var p01 = await context.Productos.FirstOrDefaultAsync(p => p.Sku == "P01");
        if (p01 != null) { p01.Stock = 8; p01.Precio = 20.00m; }

        var p02 = await context.Productos.FirstOrDefaultAsync(p => p.Sku == "P02");
        if (p02 != null) { p02.Stock = 5; p02.Precio = 30.00m; }

        var p03 = await context.Productos.FirstOrDefaultAsync(p => p.Sku == "P03");
        if (p03 != null) { p03.Stock = 20; p03.Precio = 10.00m; }

        var p04 = await context.Productos.FirstOrDefaultAsync(p => p.Sku == "P04");
        if (p04 != null) { p04.Stock = 4; p04.Precio = 40.00m; }

        var p05 = await context.Productos.FirstOrDefaultAsync(p => p.Sku == "P05");
        if (p05 != null) { p05.Stock = 30; p05.Precio = 5.00m; }

        await context.SaveChangesAsync();
    }
}
