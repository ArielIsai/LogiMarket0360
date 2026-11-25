using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcPedidosHistorial : UserControl
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvPedidos = null!;
    private TextBox _txtBuscar = null!;
    private Label _lblBreadcrumb = null!;

    public UcPedidosHistorial(IPedidoRepository pedidoRepo, IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcPedidosHistorial", (evento, datos) =>
        {
            if (evento is "PedidoValidado" or "PedidoConfirmado" or "PedidoDespachado" or "DevolucionProcesada")
            {
                Invoke(new Action(async () => await CargarPedidosAsync()));
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
            Text = "Pedidos e historial",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Consulta de estados, detalle y trazabilidad de eventos",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        _txtBuscar = new TextBox { Width = 220, Font = new Font("Segoe UI", 10), PlaceholderText = "Buscar por código..." };
        _txtBuscar.TextChanged += async (s, e) => await CargarPedidosAsync(_txtBuscar.Text);

        Button CreateFilterBtn(string texto, EstadoPedidoEnum? estado)
        {
            var btn = new Button
            {
                Text = texto,
                Height = 30,
                AutoSize = true,
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += async (s, e) => await CargarPedidosPorEstadoAsync(estado);
            return btn;
        }

        pnlToolbar.Controls.Add(_txtBuscar);
        pnlToolbar.Controls.Add(CreateFilterBtn("TODOS", null));
        pnlToolbar.Controls.Add(CreateFilterBtn("BORRADOR", EstadoPedidoEnum.Borrador));
        pnlToolbar.Controls.Add(CreateFilterBtn("VALIDADO", EstadoPedidoEnum.Validado));
        pnlToolbar.Controls.Add(CreateFilterBtn("PAGADO", EstadoPedidoEnum.Pagado));
        pnlToolbar.Controls.Add(CreateFilterBtn("DESPACHADO", EstadoPedidoEnum.Despachado));
        pnlToolbar.Controls.Add(CreateFilterBtn("ENTREGADO", EstadoPedidoEnum.Entregado));

        _dgvPedidos = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvPedidos.Columns.Add("Codigo", "Código");
        _dgvPedidos.Columns.Add("Cliente", "Cliente");
        _dgvPedidos.Columns.Add("Estado", "Estado");
        _dgvPedidos.Columns.Add("Total", "Total (USD)");
        _dgvPedidos.Columns.Add("Fecha", "Fecha");
        _dgvPedidos.SelectionChanged += (s, e) => ActualizarBreadcrumbs();

        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };
        _lblBreadcrumb = new Label
        {
            Text = "Historial del estado: Seleccione un pedido para ver su ciclo de vida.",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlBottom.Controls.Add(_lblBreadcrumb);

        Controls.Add(_dgvPedidos);
        Controls.Add(pnlBottom);
        Controls.Add(pnlToolbar);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarPedidosAsync();
    }

    public async Task CargarPedidosAsync(string? filtro = null)
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        _dgvPedidos.Rows.Clear();

        foreach (var p in pedidos)
        {
            if (string.IsNullOrWhiteSpace(filtro) || p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            {
                _dgvPedidos.Rows.Add(
                    p.Codigo,
                    p.Cliente?.Nombre ?? "N/A",
                    p.Estado.ToString(),
                    $"USD {p.Total:F2}",
                    p.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
                );
            }
        }
    }

    private async Task CargarPedidosPorEstadoAsync(EstadoPedidoEnum? estado)
    {
        if (estado == null)
        {
            await CargarPedidosAsync();
            return;
        }

        var pedidos = await _pedidoRepo.GetByEstadoAsync(estado.Value);
        _dgvPedidos.Rows.Clear();
        foreach (var p in pedidos)
        {
            _dgvPedidos.Rows.Add(
                p.Codigo,
                p.Cliente?.Nombre ?? "N/A",
                p.Estado.ToString(),
                $"USD {p.Total:F2}",
                p.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
            );
        }
    }

    private void ActualizarBreadcrumbs()
    {
        if (_dgvPedidos.SelectedRows.Count == 0) return;
        var estado = _dgvPedidos.SelectedRows[0].Cells["Estado"].Value?.ToString() ?? string.Empty;
        var codigo = _dgvPedidos.SelectedRows[0].Cells["Codigo"].Value?.ToString() ?? string.Empty;

        string Mark(string est, bool activo) => activo ? $"[ ● {est} ]" : $"[ ○ {est} ]";

        bool isBorrador = estado == "Borrador";
        bool isValidado = estado is "Validado" or "Pagado" or "Despachado" or "Entregado";
        bool isPagado = estado is "Pagado" or "Despachado" or "Entregado";
        bool isDespachado = estado is "Despachado" or "Entregado";
        bool isEntregado = estado == "Entregado";
        bool isCancelado = estado == "Cancelado";

        if (isCancelado)
        {
            _lblBreadcrumb.Text = $"Pedido {codigo}: ESTADO CANCELADO (Ciclo abortado).";
            _lblBreadcrumb.ForeColor = Color.Crimson;
            return;
        }

        _lblBreadcrumb.ForeColor = Color.FromArgb(15, 23, 42);
        _lblBreadcrumb.Text = $"Ciclo de {codigo}: " +
            $"{Mark("BORRADOR", true)} ➔ " +
            $"{Mark("VALIDADO", isValidado)} ➔ " +
            $"{Mark("PAGADO", isPagado)} ➔ " +
            $"{Mark("DESPACHADO", isDespachado)} ➔ " +
            $"{Mark("ENTREGADO", isEntregado)}";
    }
}
