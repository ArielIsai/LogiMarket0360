using LogiMarket.Application.Patterns.Command;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcDevoluciones : UserControl, IRecargableVista
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IDevolucionRepository _devolucionRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISesionContext _sesionContext;
    private readonly IFormularioMediator _mediator;

    private ComboBox _cmbPedidosEntregados = null!;
    private ComboBox _cmbMotivo = null!;
    private NumericUpDown _nudDias = null!;
    private CheckBox _chkSinUso = null!;
    private Label _lblReembolsoMonto = null!;
    private Button _btnConfirmarDev = null!;
    private Pedido? _pedidoSeleccionado;

    public UcDevoluciones(
        IPedidoRepository pedidoRepo,
        IDevolucionRepository devolucionRepo,
        IProductoRepository productoRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IAuditoriaRepository auditoriaRepo,
        IUnitOfWork unitOfWork,
        ISesionContext sesionContext,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _devolucionRepo = devolucionRepo;
        _productoRepo = productoRepo;
        _movimientoRepo = movimientoRepo;
        _auditoriaRepo = auditoriaRepo;
        _unitOfWork = unitOfWork;
        _sesionContext = sesionContext;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcDevoluciones", (evento, datos) =>
        {
            if (evento is "PedidoEntregado" or "DevolucionProcesada")
            {
                if (IsHandleCreated)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(async () => await CargarPedidosEntregadosAsync()));
                    else
                        _ = CargarPedidosEntregadosAsync();
                }
            }
        });
    }

    public Task RecargarAsync() => CargarPedidosEntregadosAsync();

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Devoluciones y reintegros",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Solicitud total, política de motivo (Template Method) y reembolso único sin flete",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlFormTop = new Panel { Dock = DockStyle.Top, Height = 75 };
        var lblPed = new Label { Text = "Pedido entregado:", Location = new Point(0, 10), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbPedidosEntregados = new ComboBox { Location = new Point(0, 35), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbPedidosEntregados.SelectedIndexChanged += async (s, e) => await CargarPedidoSeleccionadoAsync();

        var lblMot = new Label { Text = "Motivo de devolución:", Location = new Point(340, 10), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbMotivo = new ComboBox { Location = new Point(340, 35), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbMotivo.Items.AddRange(new object[] { "DEFECTO (Hasta 30 días)", "ARREPENTIMIENTO (Hasta 7 días sin uso)" });
        _cmbMotivo.SelectedIndex = 0;
        _cmbMotivo.SelectedIndexChanged += (s, e) => ActualizarCalculoReembolso();

        pnlFormTop.Controls.AddRange(new Control[] { lblPed, _cmbPedidosEntregados, lblMot, _cmbMotivo });

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 550
        };

        // Panel Izquierdo: Validación de política
        var cardPolitica = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblPolT = new Label { Text = "Validación de política", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblD = new Label { Text = "Días transcurridos desde entrega:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _nudDias = new NumericUpDown { Location = new Point(20, 75), Width = 120, Minimum = 0, Maximum = 120, Value = 5, Font = new Font("Segoe UI", 10) };
        _nudDias.ValueChanged += (s, e) => ActualizarCalculoReembolso();

        _chkSinUso = new CheckBox { Text = "Producto completamente nuevo y sin uso", Location = new Point(20, 115), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9) };
        _chkSinUso.CheckedChanged += (s, e) => ActualizarCalculoReembolso();

        var lblReglas = new Label
        {
            Text = "Reglas del negocio aplicadas:\n" +
                   "• DEFECTO: Plazo de hasta 30 días tras la entrega.\n" +
                   "• ARREPENTIMIENTO: Plazo de hasta 7 días; exige producto sin uso.\n" +
                   "• Reintegro de existencias en inventario por SKU físico.\n" +
                   "• Reembolso único por pedido: no se admite devolución duplicada.",
            Location = new Point(20, 160),
            Width = 480,
            Height = 110,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(71, 85, 105)
        };

        cardPolitica.Controls.AddRange(new Control[] { lblPolT, lblD, _nudDias, _chkSinUso, lblReglas });
        split.Panel1.Controls.Add(cardPolitica);

        // Panel Derecho: Resumen del Reembolso
        var cardResumen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblRT = new Label { Text = "Resumen del reembolso", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblAvisoFlete = new Label
        {
            Text = "Nota oficial: En LogiMarket 360 el reembolso corresponde al importe de los productos tras el descuento, SIN devolver la tarifa de envío.",
            Location = new Point(20, 50),
            Width = 320,
            Height = 55,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        var lblRTit = new Label { Text = "Total del reembolso:", Location = new Point(20, 115), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblReembolsoMonto = new Label { Text = "USD 0.00", Location = new Point(20, 140), AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        _btnConfirmarDev = new Button
        {
            Text = "Confirmar devolución",
            Location = new Point(20, 200),
            Width = 260,
            Height = 42,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnConfirmarDev.Click += async (s, e) => await ProcesarDevolucionAsync();

        cardResumen.Controls.AddRange(new Control[] { lblRT, lblAvisoFlete, lblRTit, _lblReembolsoMonto, _btnConfirmarDev });
        split.Panel2.Controls.Add(cardResumen);

        Controls.Add(split);
        Controls.Add(pnlFormTop);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarPedidosEntregadosAsync();
    }

    public async Task CargarPedidosEntregadosAsync()
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        var entregados = pedidos.Where(p => p.Estado == EstadoPedidoEnum.Entregado).ToList();

        _cmbPedidosEntregados.Items.Clear();
        foreach (var p in entregados)
        {
            var yaDevuelto = await _devolucionRepo.GetByPedidoIdAsync(p.Id) != null;
            var sufijo = yaDevuelto ? " (YA DEVUELTO)" : string.Empty;
            _cmbPedidosEntregados.Items.Add($"{p.Codigo} - {p.Cliente?.Nombre}{sufijo}");
        }

        if (_cmbPedidosEntregados.Items.Count > 0)
        {
            _cmbPedidosEntregados.SelectedIndex = 0;
        }
        else
        {
            _pedidoSeleccionado = null;
            _lblReembolsoMonto.Text = "USD 0.00";
            _btnConfirmarDev.Enabled = false;
        }
    }

    private async Task CargarPedidoSeleccionadoAsync()
    {
        if (_cmbPedidosEntregados.SelectedItem == null) return;
        var codigo = _cmbPedidosEntregados.SelectedItem.ToString()!.Split(' ')[0];
        _pedidoSeleccionado = await _pedidoRepo.GetByCodigoAsync(codigo);

        ActualizarCalculoReembolso();
    }

    private void ActualizarCalculoReembolso()
    {
        if (_pedidoSeleccionado == null)
        {
            _lblReembolsoMonto.Text = "USD 0.00";
            _btnConfirmarDev.Enabled = false;
            return;
        }

        // Subtotal productos - Descuento (sin incluir TarifaEnvio)
        var reembolso = Math.Round(_pedidoSeleccionado.Subtotal - _pedidoSeleccionado.Descuento, 2, MidpointRounding.AwayFromZero);
        _lblReembolsoMonto.Text = $"USD {reembolso:F2}";
        _btnConfirmarDev.Enabled = true;
    }

    private async Task ProcesarDevolucionAsync()
    {
        if (_pedidoSeleccionado == null) return;

        var motivo = _cmbMotivo.SelectedIndex == 0 ? MotivoDevolucionEnum.Defecto : MotivoDevolucionEnum.Arrepentimiento;
        var dias = (int)_nudDias.Value;
        var sinUso = _chkSinUso.Checked;

        var cmd = new ProcesarDevolucionCommand(
            _pedidoRepo,
            _devolucionRepo,
            _productoRepo,
            _movimientoRepo,
            _auditoriaRepo,
            _unitOfWork,
            _pedidoSeleccionado.Id,
            motivo,
            dias,
            sinUso,
            _sesionContext.UsuarioActual?.Username ?? "demo"
        );

        var res = await cmd.ExecuteAsync();
        if (res.Exitoso)
        {
            MessageBox.Show(res.Mensaje, "Devolución Exitosa");
            _mediator.Notificar("DevolucionProcesada", _pedidoSeleccionado.Codigo);
            _mediator.Notificar("InventarioActualizado");
            await CargarPedidosEntregadosAsync();
        }
        else
        {
            MessageBox.Show(res.Mensaje, "Rechazo de Devolución");
        }
    }
}
