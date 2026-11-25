using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Patterns.Builder;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Memento;
using LogiMarket.Domain.Patterns.Strategy;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcCarritoBorrador : UserControl, IRecargableVista
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly CatalogoService _catalogoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormularioMediator _mediator;

    private ComboBox _cmbCliente = null!;
    private ComboBox _cmbTipoEntrega = null!;
    private DataGridView _dgvLineas = null!;
    private Label _lblSubtotal = null!;
    private Label _lblDescuento = null!;
    private Label _lblEntrega = null!;
    private Label _lblTotal = null!;
    private Button _btnDeshacer = null!;
    private Button _btnRehacer = null!;
    private Button _btnValidar = null!;

    private Pedido _pedidoBorrador = null!;
    private readonly HistorialBorradorCareTaker _careTaker = new();
    private readonly CalculadoraDescuentoContext _calculadoraDescuento = new();

    public UcCarritoBorrador(
        IPedidoRepository pedidoRepo,
        IClienteRepository clienteRepo,
        IProductoRepository productoRepo,
        CatalogoService catalogoService,
        IUnitOfWork unitOfWork,
        IFormularioMediator mediator)
    {
        _pedidoRepo = pedidoRepo;
        _clienteRepo = clienteRepo;
        _productoRepo = productoRepo;
        _catalogoService = catalogoService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;

        InitializeUi();
        CrearNuevoBorrador();
    }

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Carrito y borrador",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Selección de cliente, líneas de pedido y control de cambios (Memento Deshacer/Rehacer)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        // Selector de Cliente y Botones
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblC = new Label { Text = "Cliente:", Location = new Point(0, 5), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbCliente = new ComboBox { Location = new Point(0, 25), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbCliente.SelectedIndexChanged += (s, e) => RecalcularBorrador();

        var btnAddProd = new Button
        {
            Text = "Añadir producto",
            Location = new Point(315, 23),
            Width = 130,
            Height = 30,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAddProd.Click += (s, e) => MostrarDialogoAgregar(esKit: false);

        var btnAddKit = new Button
        {
            Text = "Añadir kit",
            Location = new Point(455, 23),
            Width = 110,
            Height = 30,
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAddKit.Click += (s, e) => MostrarDialogoAgregar(esKit: true);

        pnlTop.Controls.AddRange(new Control[] { lblC, _cmbCliente, btnAddProd, btnAddKit });

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620
        };

        // Panel Izquierdo: DataGridView y botones Deshacer/Rehacer
        var pnlIzquierdo = new Panel { Dock = DockStyle.Fill };
        _dgvLineas = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvLineas.Columns.Add("Sku", "Artículo");
        _dgvLineas.Columns.Add("Cantidad", "Cantidad");
        _dgvLineas.Columns.Add("PrecioUnitario", "P. unitario");
        _dgvLineas.Columns.Add("Importe", "Importe (USD)");

        var pnlAcciones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(0, 8, 0, 0) };
        _btnDeshacer = new Button { Text = "↺ Deshacer", Width = 110, Height = 32, BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
        _btnDeshacer.Click += (s, e) => DeshacerCambio();

        _btnRehacer = new Button { Text = "↻ Rehacer", Width = 110, Height = 32, BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
        _btnRehacer.Click += (s, e) => RehacerCambio();

        var btnQuitar = new Button { Text = "✕ Quitar línea", Width = 120, Height = 32, BackColor = Color.FromArgb(254, 226, 226), ForeColor = Color.FromArgb(185, 28, 28), FlatStyle = FlatStyle.Flat };
        btnQuitar.Click += (s, e) => QuitarLineaSeleccionada();

        pnlAcciones.Controls.AddRange(new Control[] { _btnDeshacer, _btnRehacer, btnQuitar });
        pnlIzquierdo.Controls.Add(_dgvLineas);
        pnlIzquierdo.Controls.Add(pnlAcciones);
        split.Panel1.Controls.Add(pnlIzquierdo);

        // Panel Derecho: Tarjeta Resumen
        var pnlResumen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };
        var lblRT = new Label { Text = "Resumen del pedido", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblE = new Label { Text = "Forma de entrega:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _cmbTipoEntrega = new ComboBox { Location = new Point(20, 75), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbTipoEntrega.Items.Add("ESTÁNDAR (USD 5.00 / 3 días)");
        _cmbTipoEntrega.Items.Add("EXPRÉS (USD 12.00 / 1 día)");
        _cmbTipoEntrega.SelectedIndex = 0;
        _cmbTipoEntrega.SelectedIndexChanged += (s, e) => RecalcularBorrador();

        _lblSubtotal = new Label { Text = "Subtotal: USD 0.00", Location = new Point(20, 120), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblDescuento = new Label { Text = "Descuento: USD 0.00", Location = new Point(20, 150), AutoSize = true, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(22, 101, 52) };
        _lblEntrega = new Label { Text = "Entrega: USD 5.00", Location = new Point(20, 180), AutoSize = true, Font = new Font("Segoe UI", 10) };
        _lblTotal = new Label { Text = "TOTAL: USD 5.00", Location = new Point(20, 220), AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        _btnValidar = new Button
        {
            Text = "Validar y procesar pedido",
            Location = new Point(20, 270),
            Width = 230,
            Height = 42,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnValidar.Click += async (s, e) => await ValidarYCrearPedidoAsync();

        pnlResumen.Controls.AddRange(new Control[] { lblRT, lblE, _cmbTipoEntrega, _lblSubtotal, _lblDescuento, _lblEntrega, _lblTotal, _btnValidar });
        split.Panel2.Controls.Add(pnlResumen);

        Controls.Add(split);
        Controls.Add(pnlTop);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);
    }

    public Task RecargarAsync() => CargarClientesComboAsync();

    private async Task CargarClientesComboAsync()
    {
        try
        {
            var clientes = await _clienteRepo.GetAllAsync();
            _cmbCliente.Items.Clear();
            foreach (var c in clientes)
            {
                _cmbCliente.Items.Add($"{c.Codigo} - {c.Nombre} ({c.Categoria})");
            }
            if (_cmbCliente.Items.Count > 0) _cmbCliente.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UcCarritoBorrador] Error al cargar clientes: {ex.Message}");
        }
    }

    private void CrearNuevoBorrador()
    {
        var dummy = new Cliente("TEMP", "Temporal", "00", CategoriaCliente.Normal, "", "");
        _pedidoBorrador = new Pedido($"PED-{DateTime.UtcNow:mmss}", dummy, TipoEntregaEnum.Estandar, 5.00m);
        _careTaker.ReiniciarParaNuevoBorrador();
        ActualizarBotonesMemento();
    }

    private void GuardarSnapshot(string accion)
    {
        _careTaker.GuardarEstado(_pedidoBorrador, accion);
        ActualizarBotonesMemento();
    }

    private void ActualizarBotonesMemento()
    {
        _btnDeshacer.Enabled = _careTaker.PuedeDeshacer;
        _btnRehacer.Enabled = _careTaker.PuedeRehacer;
    }

    private void DeshacerCambio()
    {
        if (!_careTaker.PuedeDeshacer) return;
        var memento = _careTaker.Deshacer(_pedidoBorrador);
        RestaurarDesdeMemento(memento);
    }

    private void RehacerCambio()
    {
        if (!_careTaker.PuedeRehacer) return;
        var memento = _careTaker.Rehacer(_pedidoBorrador);
        RestaurarDesdeMemento(memento);
    }

    private void RestaurarDesdeMemento(PedidoBorradorMemento memento)
    {
        _pedidoBorrador.Lineas.Clear();
        foreach (var l in memento.LineasSnapshot)
        {
            _pedidoBorrador.Lineas.Add(new PedidoDetalle(l.Sku, l.Nombre, l.EsKit, l.Cantidad, l.PrecioUnitario));
        }
        RecalcularBorrador(guardarEnHistorial: false);
        ActualizarBotonesMemento();
    }

    private async void MostrarDialogoAgregar(bool esKit)
    {
        try
        {
            var form = new Form
            {
                Text = esKit ? "Seleccionar Kit" : "Seleccionar Producto",
                Size = new Size(380, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblItem = new Label { Text = "Elemento:", Location = new Point(20, 20), AutoSize = true };
            var cmb = new ComboBox { Location = new Point(20, 45), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };

            if (esKit)
            {
                foreach (var k in _catalogoService.ListarKits())
                    cmb.Items.Add($"{k.Sku} - {k.Nombre} (USD {k.ObtenerPrecio():F2})");
            }
            else
            {
                var prods = await _productoRepo.GetAllAsync();
                foreach (var p in prods)
                    cmb.Items.Add($"{p.Sku} - {p.Nombre} (USD {p.Precio:F2})");
            }

            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

            var lblQ = new Label { Text = "Cantidad:", Location = new Point(20, 80), AutoSize = true };
            var nud = new NumericUpDown { Location = new Point(20, 105), Width = 120, Minimum = 1, Maximum = 100, Value = 1 };

            var btnOk = new Button { Text = "Agregar", Location = new Point(160, 103), Width = 100, Height = 28, DialogResult = DialogResult.OK };
            form.Controls.AddRange(new Control[] { lblItem, cmb, lblQ, nud, btnOk });

            if (form.ShowDialog() == DialogResult.OK && cmb.SelectedItem != null)
            {
                var sku = cmb.SelectedItem.ToString()!.Split(' ')[0];
                var cantidad = (int)nud.Value;

                GuardarSnapshot($"Añadir {sku} x{cantidad}");

                var elemento = await _catalogoService.ObtenerElementoCatalogoAsync(sku);
                if (elemento != null)
                {
                    var linea = new PedidoDetalle(elemento.Sku, elemento.Nombre, elemento.EsKit, cantidad, elemento.ObtenerPrecio());
                    foreach (var comp in elemento.ObtenerDesgloseFisico(cantidad))
                    {
                        linea.ComponentesFisicos.Add(new PedidoDetalleComponente(
                            comp.Sku, comp.Nombre, comp.Cantidad, comp.PrecioUnitario, comp.PesoUnitario
                        ));
                    }
                    _pedidoBorrador.Lineas.Add(linea);
                    RecalcularBorrador();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al agregar elemento al carrito:\n\n{ex.Message}", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void QuitarLineaSeleccionada()
    {
        if (_dgvLineas.SelectedRows.Count == 0) return;
        var idx = _dgvLineas.SelectedRows[0].Index;
        if (idx >= 0 && idx < _pedidoBorrador.Lineas.Count)
        {
            GuardarSnapshot($"Quitar línea {_pedidoBorrador.Lineas[idx].Sku}");
            _pedidoBorrador.Lineas.RemoveAt(idx);
            RecalcularBorrador();
        }
    }

    private void RecalcularBorrador(bool guardarEnHistorial = false)
    {
        _dgvLineas.Rows.Clear();
        foreach (var l in _pedidoBorrador.Lineas)
        {
            _dgvLineas.Rows.Add(
                $"{l.Sku} • {l.Nombre}",
                l.Cantidad,
                $"USD {l.PrecioUnitario:F2}",
                $"USD {l.Subtotal:F2}"
            );
        }

        var isExpres = _cmbTipoEntrega.SelectedIndex == 1;
        _pedidoBorrador.TipoEntrega = isExpres ? TipoEntregaEnum.Expres : TipoEntregaEnum.Estandar;
        _pedidoBorrador.TarifaEnvio = isExpres ? 12.00m : 5.00m;

        var subtotal = _pedidoBorrador.Lineas.Sum(l => l.Subtotal);
        var unidadesTotales = _pedidoBorrador.ObtenerTotalUnidadesFisicas();

        var categoria = CategoriaCliente.Normal;
        if (_cmbCliente.SelectedItem != null && _cmbCliente.SelectedItem.ToString()!.Contains("Vip"))
            categoria = CategoriaCliente.Vip;

        var (estrategia, descuento) = _calculadoraDescuento.EvaluarDescuento(subtotal, unidadesTotales, categoria);
        _pedidoBorrador.RecalcularTotales(descuento, estrategia.Nombre);

        _lblSubtotal.Text = $"Subtotal: USD {_pedidoBorrador.Subtotal:F2}";
        _lblDescuento.Text = $"Descuento ({estrategia.Nombre}): -USD {_pedidoBorrador.Descuento:F2}";
        _lblEntrega.Text = $"Entrega: USD {_pedidoBorrador.TarifaEnvio:F2}";
        _lblTotal.Text = $"TOTAL: USD {_pedidoBorrador.Total:F2}";
    }

    private async Task ValidarYCrearPedidoAsync()
    {
        try
        {
            if (_pedidoBorrador.Lineas.Count == 0)
            {
                MessageBox.Show("El borrador no tiene líneas de productos o kits.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbCliente.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un cliente.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var codigoCliente = _cmbCliente.SelectedItem.ToString()!.Split(' ')[0];
            var cliente = await _clienteRepo.GetByCodigoAsync(codigoCliente);
            if (cliente == null)
            {
                MessageBox.Show("Cliente no encontrado en el sistema.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var nuevoCodigo = $"PED-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var pedido = new PedidoBuilder()
                .ConCodigo(nuevoCodigo)
                .ConCliente(cliente)
                .ConEntrega(_pedidoBorrador.TipoEntrega, _pedidoBorrador.TarifaEnvio);

            foreach (var linea in _pedidoBorrador.Lineas)
            {
                var elemento = await _catalogoService.ObtenerElementoCatalogoAsync(linea.Sku);
                if (elemento != null)
                    pedido.AgregarLinea(elemento, linea.Cantidad);
            }

            var pedidoConstruido = pedido.Build();
            pedidoConstruido.Validar(); // Transiciona a VALIDADO

            await _pedidoRepo.AddAsync(pedidoConstruido);
            await _unitOfWork.CommitAsync();

            _careTaker.BloquearYLimpiar();
            CrearNuevoBorrador();
            RecalcularBorrador();

            MessageBox.Show($"Pedido {pedidoConstruido.Codigo} registrado en estado VALIDADO por un total de USD {pedidoConstruido.Total:F2}.\nPuede proceder al cobro.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _mediator.Notificar("PedidoValidado", pedidoConstruido.Codigo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo validar y registrar el pedido:\n\n{ex.Message}", "Error en Pedido", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
