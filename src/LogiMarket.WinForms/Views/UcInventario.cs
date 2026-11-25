using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Patterns.Flyweight;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcInventario : UserControl
{
    private readonly IProductoRepository _productoRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvInventario = null!;
    private ComboBox _cmbSku = null!;
    private NumericUpDown _nudCantidadAjuste = null!;
    private TextBox _txtMotivo = null!;
    private ListBox _lstMovimientos = null!;

    public UcInventario(
        IProductoRepository productoRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IUnitOfWork unitOfWork,
        IFormularioMediator mediator)
    {
        _productoRepo = productoRepo;
        _movimientoRepo = movimientoRepo;
        _unitOfWork = unitOfWork;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcInventario", (evento, datos) =>
        {
            if (evento is "InventarioActualizado" or "PedidoConfirmado" or "DevolucionProcesada")
            {
                Invoke(new Action(async () => await CargarInventarioYMovimientosAsync()));
            }
        });
    }

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Inventario y movimientos",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Existencias en tiempo real, alertas de stock bajo y movimientos auditados",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var splitMedio = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620
        };

        // Grid Inventario
        _dgvInventario = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvInventario.Columns.Add("Sku", "SKU");
        _dgvInventario.Columns.Add("Producto", "Producto");
        _dgvInventario.Columns.Add("Disponible", "Disponible");
        _dgvInventario.Columns.Add("Alerta", "Alerta");

        splitMedio.Panel1.Controls.Add(_dgvInventario);

        // Panel Derecho: Movimiento Nuevo
        var cardMov = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16)
        };
        var lblMT = new Label { Text = "Movimiento nuevo", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblS = new Label { Text = "SKU del producto:", Location = new Point(16, 45), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbSku = new ComboBox { Location = new Point(16, 70), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };

        var lblC = new Label { Text = "Ajuste de cantidad (+ / -):", Location = new Point(16, 105), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _nudCantidadAjuste = new NumericUpDown { Location = new Point(16, 130), Width = 120, Minimum = -50, Maximum = 50, Value = 1, Font = new Font("Segoe UI", 10) };

        var lblM = new Label { Text = "Motivo del ajuste:", Location = new Point(16, 165), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtMotivo = new TextBox { Text = "Ajuste físico de inventario", Location = new Point(16, 190), Width = 220, Font = new Font("Segoe UI", 10) };

        var btnRegistrar = new Button
        {
            Text = "Registrar ajuste",
            Location = new Point(16, 230),
            Width = 220,
            Height = 36,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        btnRegistrar.Click += async (s, e) => await RegistrarAjusteAsync();

        var btnFlyweight = new Button
        {
            Text = "Demo Flyweight (1.000 filas)",
            Location = new Point(16, 275),
            Width = 220,
            Height = 32,
            BackColor = Color.FromArgb(241, 245, 249),
            Font = new Font("Segoe UI", 8),
            FlatStyle = FlatStyle.Flat
        };
        btnFlyweight.Click += (s, e) => EjecutarDemoFlyweight();

        cardMov.Controls.AddRange(new Control[] { lblMT, lblS, _cmbSku, lblC, _nudCantidadAjuste, lblM, _txtMotivo, btnRegistrar, btnFlyweight });
        splitMedio.Panel2.Controls.Add(cardMov);

        // Panel Inferior: Movimientos Recientes
        var grpMovs = new GroupBox
        {
            Text = "Movimientos recientes auditados",
            Dock = DockStyle.Bottom,
            Height = 140,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Padding = new Padding(8)
        };
        _lstMovimientos = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9)
        };
        grpMovs.Controls.Add(_lstMovimientos);

        Controls.Add(splitMedio);
        Controls.Add(grpMovs);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarInventarioYMovimientosAsync();
    }

    public async Task CargarInventarioYMovimientosAsync()
    {
        var productos = await _productoRepo.GetAllAsync();
        _dgvInventario.Rows.Clear();
        _cmbSku.Items.Clear();

        foreach (var p in productos)
        {
            var alerta = p.Stock < 5 ? "BAJO" : "OK";
            var rowIdx = _dgvInventario.Rows.Add(p.Sku, p.Nombre, p.Stock.ToString(), alerta);

            if (p.Stock < 5)
            {
                _dgvInventario.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.Crimson;
                _dgvInventario.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }

            _cmbSku.Items.Add($"{p.Sku} - {p.Nombre} (Stock: {p.Stock})");
        }

        if (_cmbSku.Items.Count > 0) _cmbSku.SelectedIndex = 0;

        // Movimientos recientes
        var movs = await _movimientoRepo.GetAllAsync();
        _lstMovimientos.Items.Clear();
        foreach (var m in movs.Take(8))
        {
            var signo = m.TipoMovimiento == TipoMovimientoInventario.Venta ? "-" : "+";
            _lstMovimientos.Items.Add($"[{m.Fecha:HH:mm:ss}] {m.Referencia,-15} | SKU: {m.ProductoSku} {signo}{m.Cantidad} (Stock: {m.StockPrevio} ➔ {m.StockNuevo}) | {m.Motivo}");
        }
    }

    private async Task RegistrarAjusteAsync()
    {
        if (_cmbSku.SelectedItem == null) return;
        var sku = _cmbSku.SelectedItem.ToString()!.Split(' ')[0];
        var delta = (int)_nudCantidadAjuste.Value;
        if (delta == 0) return;

        var prod = await _productoRepo.GetBySkuAsync(sku);
        if (prod == null) return;

        if (prod.Stock + delta < 0)
        {
            MessageBox.Show($"El ajuste dejaría el stock en negativo ({prod.Stock + delta}). Operación denegada.", "Validación");
            return;
        }

        var stockPrevio = prod.Stock;
        if (delta > 0) prod.ReintegrarStock(delta);
        else prod.DescontarStock(-delta);

        await _productoRepo.UpdateAsync(prod);

        var mov = new MovimientoInventario(
            prod.Sku,
            TipoMovimientoInventario.Ajuste,
            Math.Abs(delta),
            stockPrevio,
            prod.Stock,
            $"AJUSTE-{DateTime.UtcNow:mmss}",
            _txtMotivo.Text.Trim()
        );
        await _movimientoRepo.AddAsync(mov);
        await _unitOfWork.CommitAsync();

        MessageBox.Show($"Ajuste registrado para {prod.Sku}. Stock anterior: {stockPrevio}, nuevo: {prod.Stock}.", "Inventario Actualizado");
        _mediator.Notificar("InventarioActualizado");
        await CargarInventarioYMovimientosAsync();
    }

    private void EjecutarDemoFlyweight()
    {
        var factory = new CatalogoVisualFlyweightFactory();
        var filas = factory.GenerarRenglonesDemostracion(1000);

        var iguales = ReferenceEquals(filas[0].Metadatos, filas[5].Metadatos);
        MessageBox.Show(
            $"Demostración del Patrón Flyweight:\n" +
            $"• Renglones visuales generados: {filas.Count}\n" +
            $"• Instancias de metadatos únicas en memoria: {factory.TotalInstanciasCreadas}\n" +
            $"• ReferenceEquals entre fila 1 y fila 6 (mismo SKU P01): {iguales}\n" +
            $"Beneficio: Compartición de memoria idéntica para elementos inmutables.",
            "Patrón Flyweight Demostrado"
        );
    }
}
