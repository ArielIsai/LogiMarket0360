using LogiMarket.Application.Patterns.AbstractFactory;
using LogiMarket.Application.Patterns.Command;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcDespacho : UserControl, IRecargableVista
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISesionContext _sesionContext;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvLogistica = null!;
    private TextBox _txtPedido = null!;
    private TextBox _txtGuia = null!;
    private Button _btnDespachar = null!;
    private Button _btnEntregar = null!;
    private Pedido? _pedidoSeleccionado;

    public UcDespacho(
        IPedidoRepository pedidoRepo,
        IAuditoriaRepository auditoriaRepo,
        IUnitOfWork unitOfWork,
        ISesionContext sesionContext,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
        _sesionContext = sesionContext;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcDespacho", (evento, datos) =>
        {
            if (evento is "PedidoConfirmado" or "PedidoDespachado")
            {
                if (IsHandleCreated)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(async () => await CargarLogisticaAsync()));
                    else
                        _ = CargarLogisticaAsync();
                }
            }
        });
    }

    public Task RecargarAsync() => CargarLogisticaAsync();

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Despacho y entrega",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Preparación, guía interna (Abstract Factory) y transición autorizada (BODEGA / ADMIN)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 600
        };

        _dgvLogistica = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvLogistica.Columns.Add("Codigo", "Pedido");
        _dgvLogistica.Columns.Add("Estado", "Estado");
        _dgvLogistica.Columns.Add("Entrega", "Entrega");
        _dgvLogistica.Columns.Add("Guia", "N° Guía");
        _dgvLogistica.SelectionChanged += (s, e) => SeleccionarPedido();

        split.Panel1.Controls.Add(_dgvLogistica);

        // Panel Derecho: Detalle Logístico
        var pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblT = new Label { Text = "Detalle logístico", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblP = new Label { Text = "Pedido:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtPedido = new TextBox { Location = new Point(20, 75), Width = 260, ReadOnly = true, Font = new Font("Segoe UI", 10) };

        var lblG = new Label { Text = "Guía simulada:", Location = new Point(20, 115), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtGuia = new TextBox { Location = new Point(20, 140), Width = 260, Font = new Font("Segoe UI", 10) };

        _btnDespachar = new Button
        {
            Text = "Marcar como despachado",
            Location = new Point(20, 190),
            Width = 260,
            Height = 38,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        _btnDespachar.Click += async (s, e) => await DespacharAsync();

        _btnEntregar = new Button
        {
            Text = "Registrar entrega final",
            Location = new Point(20, 240),
            Width = 260,
            Height = 38,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        _btnEntregar.Click += async (s, e) => await EntregarAsync();

        pnlCard.Controls.AddRange(new Control[] { lblT, lblP, _txtPedido, lblG, _txtGuia, _btnDespachar, _btnEntregar });
        split.Panel2.Controls.Add(pnlCard);

        Controls.Add(split);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarLogisticaAsync();
    }

    public async Task CargarLogisticaAsync()
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        var logisticos = pedidos.Where(p => p.Estado is EstadoPedidoEnum.Pagado or EstadoPedidoEnum.Despachado).ToList();

        _dgvLogistica.Rows.Clear();
        foreach (var p in logisticos)
        {
            _dgvLogistica.Rows.Add(p.Codigo, p.Estado.ToString(), p.TipoEntrega.ToString(), string.IsNullOrEmpty(p.GuiaDespacho) ? "Pendiente" : p.GuiaDespacho);
        }
    }

    private async void SeleccionarPedido()
    {
        if (_dgvLogistica.SelectedRows.Count == 0) return;
        var codigo = _dgvLogistica.SelectedRows[0].Cells["Codigo"].Value?.ToString();
        if (string.IsNullOrEmpty(codigo)) return;

        _pedidoSeleccionado = await _pedidoRepo.GetByCodigoAsync(codigo);
        if (_pedidoSeleccionado != null)
        {
            _txtPedido.Text = _pedidoSeleccionado.Codigo;

            if (string.IsNullOrEmpty(_pedidoSeleccionado.GuiaDespacho))
            {
                // Generar guía con Abstract Factory
                ILogisticaAbstractFactory factory = _pedidoSeleccionado.TipoEntrega == TipoEntregaEnum.Expres
                    ? new LogisticaExpresFactory()
                    : new LogisticaEstandarFactory();

                _txtGuia.Text = factory.CrearConstructorGuia().GenerarNumeroGuia(_pedidoSeleccionado.Codigo);
            }
            else
            {
                _txtGuia.Text = _pedidoSeleccionado.GuiaDespacho;
            }

            _btnDespachar.Enabled = _pedidoSeleccionado.Estado == EstadoPedidoEnum.Pagado;
            _btnEntregar.Enabled = _pedidoSeleccionado.Estado == EstadoPedidoEnum.Despachado;
        }
    }

    private async Task DespacharAsync()
    {
        if (_pedidoSeleccionado == null) return;
        if (string.IsNullOrWhiteSpace(_txtGuia.Text))
        {
            MessageBox.Show("Debe ingresar o generar un número de guía.", "Validación");
            return;
        }

        var cmd = new DespacharPedidoCommand(
            _pedidoRepo,
            _unitOfWork,
            _auditoriaRepo,
            _pedidoSeleccionado.Id,
            _txtGuia.Text.Trim(),
            _sesionContext.UsuarioActual?.Username ?? "demo",
            _sesionContext.RolActual
        );

        var res = await cmd.ExecuteAsync();
        if (res.Exitoso)
        {
            MessageBox.Show(res.Mensaje, "Despacho Confirmado");
            _mediator.Notificar("PedidoDespachado", _pedidoSeleccionado.Codigo);
            await CargarLogisticaAsync();
        }
        else
        {
            MessageBox.Show(res.Mensaje, "Error en Despacho");
        }
    }

    private async Task EntregarAsync()
    {
        if (_pedidoSeleccionado == null) return;
        try
        {
            _pedidoSeleccionado.Entregar();
            await _pedidoRepo.UpdateAsync(_pedidoSeleccionado);
            await _auditoriaRepo.AddAsync(new Auditoria(
                _sesionContext.UsuarioActual?.Username ?? "demo",
                "EntregarPedido",
                "Pedido",
                _pedidoSeleccionado.Id.ToString(),
                $"Pedido {_pedidoSeleccionado.Codigo} entregado al cliente."
            ));
            await _unitOfWork.CommitAsync();

            MessageBox.Show($"Pedido {_pedidoSeleccionado.Codigo} registrado como ENTREGADO.", "Entrega Realizada");
            _mediator.Notificar("PedidoEntregado", _pedidoSeleccionado.Codigo);
            await CargarLogisticaAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error en Entrega");
        }
    }
}
