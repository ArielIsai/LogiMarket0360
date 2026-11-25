using LogiMarket.Application.Patterns.Interpreter;
using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Patterns.Bridge;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcReportes : UserControl
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IDevolucionRepository _devolucionRepo;
    private readonly IFormularioMediator _mediator;

    private TextBox _txtConsultaInterpreter = null!;
    private Label _lblPedidosConf = null!;
    private Label _lblVentasNetas = null!;
    private Label _lblDevoluciones = null!;
    private RichTextBox _rtbReporte = null!;
    private ComboBox _cmbTipoDocumento = null!;
    private ComboBox _cmbPedidoSeleccionado = null!;

    public UcReportes(
        IPedidoRepository pedidoRepo,
        IDevolucionRepository devolucionRepo,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _devolucionRepo = devolucionRepo;
        _mediator = mediator;

        InitializeUi();
    }

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Reportes y comprobantes",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Motor de consulta (Interpreter) y generación de comprobantes desacoplados (Bridge)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        // Barra Interpreter
        var pnlInterpreter = new Panel { Dock = DockStyle.Top, Height = 50 };
        var lblInterp = new Label { Text = "Filtro gramatical (Interpreter):", Location = new Point(0, 12), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtConsultaInterpreter = new TextBox
        {
            Text = "ESTADO = PAGADO AND TOTAL >= 50",
            Location = new Point(190, 9),
            Width = 320,
            Font = new Font("Consolas", 10)
        };
        var btnEjecutar = new Button
        {
            Text = "Consultar",
            Location = new Point(520, 8),
            Width = 100,
            Height = 30,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnEjecutar.Click += async (s, e) => await EjecutarConsultaInterpreterAsync();

        pnlInterpreter.Controls.AddRange(new Control[] { lblInterp, _txtConsultaInterpreter, btnEjecutar });

        // KPIs
        var pnlKpis = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 90 };
        Panel CreateKpi(string t, out Label l)
        {
            var p = new Panel { Width = 200, Height = 75, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 16, 0) };
            var lt = new Label { Text = t, Location = new Point(10, 8), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            var lv = new Label { Text = "0", Location = new Point(10, 30), AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
            p.Controls.AddRange(new Control[] { lt, lv });
            l = lv;
            return p;
        }
        pnlKpis.Controls.Add(CreateKpi("Pedidos confirmados", out _lblPedidosConf));
        pnlKpis.Controls.Add(CreateKpi("Ventas netas (USD)", out _lblVentasNetas));
        pnlKpis.Controls.Add(CreateKpi("Devoluciones", out _lblDevoluciones));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 550
        };

        // Vista previa (Izquierda)
        var pnlIzq = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0) };
        var lblVPT = new Label { Text = "Vista previa del reporte o comprobante", Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
        _rtbReporte = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Font = new Font("Consolas", 9),
            ReadOnly = true
        };
        pnlIzq.Controls.Add(_rtbReporte);
        pnlIzq.Controls.Add(lblVPT);
        split.Panel1.Controls.Add(pnlIzq);

        // Panel Generador de Comprobantes (Bridge) a la derecha
        var pnlDer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16)
        };
        var lblBT = new Label { Text = "Salida / Comprobante (Bridge)", Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblSelPed = new Label { Text = "Seleccionar pedido:", Location = new Point(16, 45), AutoSize = true };
        _cmbPedidoSeleccionado = new ComboBox { Location = new Point(16, 70), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };

        var lblTD = new Label { Text = "Tipo de documento:", Location = new Point(16, 110), AutoSize = true };
        _cmbTipoDocumento = new ComboBox { Location = new Point(16, 135), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbTipoDocumento.Items.AddRange(new object[] { "Comprobante de Venta", "Comprobante de Devolución" });
        _cmbTipoDocumento.SelectedIndex = 0;

        var btnVistaPrevia = new Button
        {
            Text = "Vista previa (Pantalla)",
            Location = new Point(16, 185),
            Width = 260,
            Height = 36,
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnVistaPrevia.Click += async (s, e) => await GenerarComprobanteBridgeAsync(usarArchivoTxt: false);

        var btnExportarTxt = new Button
        {
            Text = "Exportar a Archivo TXT",
            Location = new Point(16, 230),
            Width = 260,
            Height = 36,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnExportarTxt.Click += async (s, e) => await GenerarComprobanteBridgeAsync(usarArchivoTxt: true);

        pnlDer.Controls.AddRange(new Control[] { lblBT, lblSelPed, _cmbPedidoSeleccionado, lblTD, _cmbTipoDocumento, btnVistaPrevia, btnExportarTxt });
        split.Panel2.Controls.Add(pnlDer);

        Controls.Add(split);
        Controls.Add(pnlKpis);
        Controls.Add(pnlInterpreter);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) =>
        {
            await CargarCombosReporteAsync();
            await EjecutarConsultaInterpreterAsync();
        };
    }

    private async Task CargarCombosReporteAsync()
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        _cmbPedidoSeleccionado.Items.Clear();
        foreach (var p in pedidos)
        {
            _cmbPedidoSeleccionado.Items.Add($"{p.Codigo} ({p.Estado}) - USD {p.Total:F2}");
        }
        if (_cmbPedidoSeleccionado.Items.Count > 0) _cmbPedidoSeleccionado.SelectedIndex = 0;
    }

    private async Task EjecutarConsultaInterpreterAsync()
    {
        var pedidos = await _pedidoRepo.GetAllWithDetailsAsync();
        var devoluciones = await _devolucionRepo.GetAllAsync();

        try
        {
            var parser = new ParserFiltroPedidos(_txtConsultaInterpreter.Text);
            var expresion = parser.Parsear();

            var filtrados = pedidos.Where(p => expresion.Interpretar(p)).ToList();

            var confirmados = filtrados.Count(p => p.Estado is EstadoPedidoEnum.Pagado or EstadoPedidoEnum.Despachado or EstadoPedidoEnum.Entregado);
            var ventasNetas = filtrados.Where(p => p.Estado is EstadoPedidoEnum.Pagado or EstadoPedidoEnum.Despachado or EstadoPedidoEnum.Entregado).Sum(p => p.Total);

            _lblPedidosConf.Text = confirmados.ToString();
            _lblVentasNetas.Text = $"USD {ventasNetas:F2}";
            _lblDevoluciones.Text = devoluciones.Count.ToString();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== RESULTADO DE LA CONSULTA (INTERPRETER PATTERN) ===");
            sb.AppendLine($"Expresión evaluada: {_txtConsultaInterpreter.Text}");
            sb.AppendLine($"Pedidos coincidentes: {filtrados.Count}");
            sb.AppendLine(new string('-', 60));

            foreach (var p in filtrados)
            {
                sb.AppendLine($"[{p.Codigo}] Cliente: {p.Cliente?.Nombre,-15} | Estado: {p.Estado,-10} | Total: USD {p.Total,7:F2} | Fecha: {p.FechaCreacion:yyyy-MM-dd}");
            }

            _rtbReporte.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            _rtbReporte.Text = $"ERROR SINTÁCTICO EN EL FILTRO:\n{ex.Message}\n\nEjemplos válidos:\n• ESTADO = PAGADO\n• TOTAL >= 50\n• CLIENTE = Ana\n• (ESTADO = PAGADO OR ESTADO = VALIDADO) AND TOTAL > 40";
        }
    }

    private async Task GenerarComprobanteBridgeAsync(bool usarArchivoTxt)
    {
        if (_cmbPedidoSeleccionado.SelectedItem == null)
        {
            MessageBox.Show("Seleccione un pedido para emitir su comprobante.", "Comprobantes");
            return;
        }

        var codigo = _cmbPedidoSeleccionado.SelectedItem.ToString()!.Split(' ')[0];
        var pedido = await _pedidoRepo.GetByCodigoAsync(codigo);
        if (pedido == null) return;

        ISalidaDocumento salida = usarArchivoTxt ? new SalidaArchivoTxt() : new SalidaVistaPrevia();
        bool esVenta = _cmbTipoDocumento.SelectedIndex == 0;

        ComprobanteDocumento comprobante;
        if (esVenta)
        {
            comprobante = new ComprobanteVenta(pedido, salida);
        }
        else
        {
            var dev = await _devolucionRepo.GetByPedidoIdAsync(pedido.Id);
            if (dev == null)
            {
                MessageBox.Show($"El pedido {pedido.Codigo} no tiene devoluciones registradas.", "Aviso");
                return;
            }
            comprobante = new ComprobanteDevolucion(dev, pedido, salida);
        }

        var resultado = comprobante.Generar();
        _rtbReporte.Text = resultado;

        if (usarArchivoTxt)
        {
            MessageBox.Show(resultado, "Bridge: Archivo TXT Generado");
        }
    }
}
