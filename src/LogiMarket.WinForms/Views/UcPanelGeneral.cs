using LogiMarket.Application.Patterns.Singleton;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcPanelGeneral : UserControl
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly IEventoOutboxRepository _outboxRepo;
    private readonly IDevolucionRepository _devolucionRepo;
    private readonly MetricasSesion _metricas;
    private readonly IFormularioMediator _mediator;

    private Label _lblPedidosActivos = null!;
    private Label _lblPorDespachar = null!;
    private Label _lblStockBajo = null!;
    private Label _lblVentas = null!;
    private DataGridView _dgvRecientes = null!;
    private ListBox _lstAlertas = null!;

    public UcPanelGeneral(
        IPedidoRepository pedidoRepo,
        IProductoRepository productoRepo,
        IEventoOutboxRepository outboxRepo,
        IDevolucionRepository devolucionRepo,
        MetricasSesion metricas,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _productoRepo = productoRepo;
        _outboxRepo = outboxRepo;
        _devolucionRepo = devolucionRepo;
        _metricas = metricas;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcPanelGeneral", (evento, datos) =>
        {
            if (evento is "PedidoConfirmado" or "PedidoDespachado" or "DevolucionProcesada" or "InventarioActualizado")
            {
                Invoke(new Action(async () => await CargarDatosAsync()));
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
            Text = "Panel general",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Resumen de operaciones, pedidos y movimientos recientes",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 30
        };

        // KPI Cards Container
        var pnlKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            WrapContents = false,
            AutoScroll = true
        };

        Panel CreateCard(string titulo, out Label valLabel)
        {
            var pnl = new Panel
            {
                Width = 200,
                Height = 90,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 16, 0)
            };

            var lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(12, 12),
                AutoSize = true
            };

            var lblV = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(12, 38),
                AutoSize = true
            };

            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            valLabel = lblV;
            return pnl;
        }

        pnlKpis.Controls.Add(CreateCard("Pedidos activos", out _lblPedidosActivos));
        pnlKpis.Controls.Add(CreateCard("Por despachar", out _lblPorDespachar));
        pnlKpis.Controls.Add(CreateCard("Stock bajo (< 5)", out _lblStockBajo));
        pnlKpis.Controls.Add(CreateCard("Ventas (USD)", out _lblVentas));

        // Contenedor medio: Pedidos recientes a la izquierda, Alertas a la derecha
        var pnlMedio = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 16, 0, 0)
        };
        pnlMedio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        pnlMedio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        // Pedidos Recientes
        var grpRecientes = new GroupBox
        {
            Text = "Pedidos recientes",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _dgvRecientes = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvRecientes.Columns.Add("Codigo", "Pedido");
        _dgvRecientes.Columns.Add("Cliente", "Cliente");
        _dgvRecientes.Columns.Add("Estado", "Estado");
        _dgvRecientes.Columns.Add("Total", "Total (USD)");
        grpRecientes.Controls.Add(_dgvRecientes);

        // Alertas
        var grpAlertas = new GroupBox
        {
            Text = "Alertas del sistema",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _lstAlertas = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            BorderStyle = BorderStyle.None
        };
        grpAlertas.Controls.Add(_lstAlertas);

        pnlMedio.Controls.Add(grpRecientes, 0, 0);
        pnlMedio.Controls.Add(grpAlertas, 1, 0);

        Controls.Add(pnlMedio);
        Controls.Add(pnlKpis);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarDatosAsync();
    }

    public async Task CargarDatosAsync()
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        var productos = await _productoRepo.GetAllAsync();
        var outboxPendientes = await _outboxRepo.GetPendientesAsync();
        var devoluciones = await _devolucionRepo.GetAllAsync();

        var activos = pedidos.Count(p => p.Estado != EstadoPedidoEnum.Entregado && p.Estado != EstadoPedidoEnum.Cancelado);
        var porDespachar = pedidos.Count(p => p.Estado == EstadoPedidoEnum.Pagado);
        var stockBajo = productos.Count(p => p.Stock < 5);
        var ventasTotal = pedidos.Where(p => p.Estado is EstadoPedidoEnum.Pagado or EstadoPedidoEnum.Despachado or EstadoPedidoEnum.Entregado)
                                .Sum(p => p.Total);

        _lblPedidosActivos.Text = activos.ToString();
        _lblPorDespachar.Text = porDespachar.ToString();
        _lblStockBajo.Text = stockBajo.ToString();
        _lblVentas.Text = $"USD {ventasTotal:F2}";

        _dgvRecientes.Rows.Clear();
        foreach (var p in pedidos.Take(10))
        {
            _dgvRecientes.Rows.Add(p.Codigo, p.Cliente?.Nombre ?? "N/A", p.Estado.ToString(), $"USD {p.Total:F2}");
        }

        _lstAlertas.Items.Clear();
        _lstAlertas.Items.Add($"• {stockBajo} producto(s) con stock bajo");
        _lstAlertas.Items.Add($"• {outboxPendientes.Count} evento(s) Outbox pendientes");
        _lstAlertas.Items.Add($"• {devoluciones.Count} devolución(es) registrada(s)");
        _lstAlertas.Items.Add($"• Sesión actual: {_metricas.PedidosConfirmados} ventas confirmadas");
    }
}
