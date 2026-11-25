using LogiMarket.Application.Patterns.Facade;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcCobro : UserControl, IRecargableVista
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly ConfirmacionPedidoFacade _facade;
    private readonly ISesionContext _sesionContext;
    private readonly IFormularioMediator _mediator;

    private ComboBox _cmbPedidosValidados = null!;
    private Label _lblCliente = null!;
    private Label _lblEstado = null!;
    private Label _lblSubtotal = null!;
    private Label _lblDescuento = null!;
    private Label _lblEntrega = null!;
    private Label _lblTotal = null!;
    private ComboBox _cmbProveedor = null!;
    private Button _btnConfirmarCobro = null!;
    private Label _lblResultado = null!;
    private Label _lblBadgeRol = null!;

    private Pedido? _pedidoSeleccionado;

    public UcCobro(
        IPedidoRepository pedidoRepo,
        ConfirmacionPedidoFacade facade,
        ISesionContext sesionContext,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _facade = facade;
        _sesionContext = sesionContext;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcCobro", (evento, datos) =>
        {
            if (evento is "PedidoValidado" or "SesionIniciada")
            {
                if (IsHandleCreated)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(async () => await CargarPedidosParaCobroAsync()));
                    else
                        _ = CargarPedidosParaCobroAsync();
                }
            }
        });
    }

    public Task RecargarAsync() => CargarPedidosParaCobroAsync();

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Confirmación y cobro",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Autorización de rol (Proxy), validaciones y pasarela simulada (Adapter & Decorator)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlSelector = new Panel { Dock = DockStyle.Top, Height = 50 };
        var lblSel = new Label { Text = "Seleccionar pedido validado:", Location = new Point(0, 15), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbPedidosValidados = new ComboBox { Location = new Point(190, 12), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbPedidosValidados.SelectedIndexChanged += async (s, e) => await CargarDetallePedidoAsync();
        pnlSelector.Controls.AddRange(new Control[] { lblSel, _cmbPedidosValidados });

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 450
        };

        // Tarjeta Izquierda: Resumen del Pedido
        var cardPedido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblPT = new Label { Text = "Resumen del pedido", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        _lblCliente = new Label { Text = "Cliente: —", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblEstado = new Label { Text = "Estado: —", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblSubtotal = new Label { Text = "Subtotal: USD 0.00", Location = new Point(20, 110), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblDescuento = new Label { Text = "Descuento: USD 0.00", Location = new Point(20, 140), AutoSize = true, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(22, 101, 52) };
        _lblEntrega = new Label { Text = "Entrega: USD 0.00", Location = new Point(20, 170), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblTotal = new Label { Text = "TOTAL: USD 0.00", Location = new Point(20, 210), AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        cardPedido.Controls.AddRange(new Control[] { lblPT, _lblCliente, _lblEstado, _lblSubtotal, _lblDescuento, _lblEntrega, _lblTotal });
        split.Panel1.Controls.Add(cardPedido);

        // Tarjeta Derecha: Proceso de Cobro
        var cardPago = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblPP = new Label { Text = "Proceso de pago", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblProv = new Label { Text = "Proveedor simulado:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbProveedor = new ComboBox { Location = new Point(20, 75), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbProveedor.Items.AddRange(new object[] { "Pasarela Legacy (Cobro en centavos vía Adapter)", "Pasarela V2 (API Moderna)" });
        _cmbProveedor.SelectedIndex = 0;

        _lblBadgeRol = new Label
        {
            Text = "ROL AUTORIZADO: VENTAS / ADMIN",
            Location = new Point(20, 120),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            BackColor = Color.FromArgb(226, 232, 240),
            Padding = new Padding(6)
        };

        var lblBadgeIdem = new Label
        {
            Text = "COBRO ÚNICO (Idempotencia activa)",
            Location = new Point(20, 160),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(21, 128, 61),
            BackColor = Color.FromArgb(220, 252, 231),
            Padding = new Padding(6)
        };

        _btnConfirmarCobro = new Button
        {
            Text = "Confirmar cobro",
            Location = new Point(20, 210),
            Width = 300,
            Height = 42,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnConfirmarCobro.Click += async (s, e) => await EjecutarConfirmacionAsync();

        _lblResultado = new Label
        {
            Text = "Resultado / referencia: pendiente",
            Location = new Point(20, 270),
            Width = 350,
            Height = 80,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9)
        };

        cardPago.Controls.AddRange(new Control[] { lblPP, lblProv, _cmbProveedor, _lblBadgeRol, lblBadgeIdem, _btnConfirmarCobro, _lblResultado });
        split.Panel2.Controls.Add(cardPago);

        Controls.Add(split);
        Controls.Add(pnlSelector);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarPedidosParaCobroAsync();
    }

    public async Task CargarPedidosParaCobroAsync()
    {
        var rol = _sesionContext.RolActual.ToString().ToUpperInvariant();
        _lblBadgeRol.Text = $"ROL ACTUAL: {rol}";

        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        var validados = pedidos.Where(p => p.Estado == EstadoPedidoEnum.Validado).ToList();

        _cmbPedidosValidados.Items.Clear();
        foreach (var p in validados)
        {
            _cmbPedidosValidados.Items.Add($"{p.Codigo} - {p.Cliente?.Nombre} (USD {p.Total:F2})");
        }

        if (_cmbPedidosValidados.Items.Count > 0)
        {
            _cmbPedidosValidados.SelectedIndex = 0;
            _btnConfirmarCobro.Enabled = true;
        }
        else
        {
            _pedidoSeleccionado = null;
            LimpiarResumen();
            _btnConfirmarCobro.Enabled = false;
        }
    }

    private async Task CargarDetallePedidoAsync()
    {
        if (_cmbPedidosValidados.SelectedItem == null) return;
        var codigo = _cmbPedidosValidados.SelectedItem.ToString()!.Split(' ')[0];
        _pedidoSeleccionado = await _pedidoRepo.GetByCodigoAsync(codigo);

        if (_pedidoSeleccionado != null)
        {
            _lblCliente.Text = $"Cliente: {_pedidoSeleccionado.Cliente?.Nombre} ({_pedidoSeleccionado.Cliente?.Categoria})";
            _lblEstado.Text = $"Estado: {_pedidoSeleccionado.Estado}";
            _lblSubtotal.Text = $"Subtotal: USD {_pedidoSeleccionado.Subtotal:F2}";
            _lblDescuento.Text = $"Descuento ({_pedidoSeleccionado.EstrategiaDescuentoAplicada}): -USD {_pedidoSeleccionado.Descuento:F2}";
            _lblEntrega.Text = $"Entrega ({_pedidoSeleccionado.TipoEntrega}): USD {_pedidoSeleccionado.TarifaEnvio:F2}";
            _lblTotal.Text = $"TOTAL: USD {_pedidoSeleccionado.Total:F2}";
            _lblResultado.Text = "Listo para procesar cobro.";
        }
    }

    private void LimpiarResumen()
    {
        _lblCliente.Text = "Cliente: —";
        _lblEstado.Text = "Estado: —";
        _lblSubtotal.Text = "Subtotal: USD 0.00";
        _lblDescuento.Text = "Descuento: USD 0.00";
        _lblEntrega.Text = "Entrega: USD 0.00";
        _lblTotal.Text = "TOTAL: USD 0.00";
        _lblResultado.Text = "No hay pedidos pendientes de cobro.";
    }

    private async Task EjecutarConfirmacionAsync()
    {
        if (_pedidoSeleccionado == null) return;

        _btnConfirmarCobro.Enabled = false;
        _lblResultado.Text = "Procesando cobro mediante pasarela...";

        var rol = _sesionContext.RolActual.ToString();
        var prov = _cmbProveedor.SelectedIndex == 0 ? "Legacy" : "V2";

        var res = await _facade.ConfirmarPedidoAsync(
            _pedidoSeleccionado.Id,
            _pedidoSeleccionado.IdempotencyKey,
            rol,
            prov
        );

        if (res.Exitoso)
        {
            _lblResultado.Text = $"✅ {res.Mensaje}\nTransacción: {res.CodigoTransaccion}\nMonto cobrado: USD {res.TotalPagado:F2}";
            _lblResultado.ForeColor = Color.FromArgb(21, 128, 61);
            MessageBox.Show($"Cobro exitoso para {_pedidoSeleccionado.Codigo}.\nTransacción: {res.CodigoTransaccion}", "Cobro Confirmado");

            _mediator.Notificar("PedidoConfirmado", _pedidoSeleccionado.Codigo);
            await CargarPedidosParaCobroAsync();
        }
        else
        {
            _lblResultado.Text = $"❌ {res.Mensaje}";
            _lblResultado.ForeColor = Color.Crimson;
            MessageBox.Show(res.Mensaje, "Error en Cobro");
            _btnConfirmarCobro.Enabled = true;
        }
    }
}
