using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Visitor;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcCatalogoKits : UserControl, IRecargableVista
{
    private readonly IProductoRepository _productoRepo;
    private readonly CatalogoService _catalogoService;
    private readonly IFormularioMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISesionContext _sesionContext;

    private DataGridView _dgvCatalogo = null!;
    private TextBox _txtBuscar = null!;
    private TreeView _tvEstructura = null!;
    private Label _lblDetalleKit = null!;
    private Button _btnNuevoProd = null!;
    private Button _btnNuevoKit = null!;
    private Button _btnInitKits = null!;
    private Button _btnComponerKit = null!;
    private Label _lblPermisos = null!;

    public UcCatalogoKits(
        IProductoRepository productoRepo,
        CatalogoService catalogoService,
        IFormularioMediator mediator,
        IUnitOfWork unitOfWork,
        ISesionContext sesionContext)
    {
        _productoRepo = productoRepo;
        _catalogoService = catalogoService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _sesionContext = sesionContext;

        InitializeUi();

        _mediator.RegistrarColega("UcCatalogoKits", (evento, datos) =>
        {
            if (evento is "InventarioActualizado" or "PedidoConfirmado" or "DevolucionProcesada")
            {
                if (IsHandleCreated)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(async () => await CargarCatalogoAsync()));
                    else
                        _ = CargarCatalogoAsync();
                }
            }
        });
    }

    public async Task RecargarAsync()
    {
        AplicarPermisosPorRol();
        await CargarCatalogoAsync();
    }

    private void AplicarPermisosPorRol()
    {
        var rol = _sesionContext.RolActual;
        // Según guía oficial: ADMIN, BODEGA y VENTAS pueden gestionar catálogo; CONSULTA es solo lectura
        bool puedeModificar = rol is RolUsuario.Admin or RolUsuario.Bodega or RolUsuario.Ventas;

        _btnNuevoProd.Enabled = puedeModificar;
        _btnNuevoKit.Enabled = puedeModificar;
        _btnComponerKit.Enabled = puedeModificar;

        if (!puedeModificar)
        {
            _lblPermisos.Text = $"Modo solo lectura activo (Rol actual: {rol})";
            _lblPermisos.Visible = true;
        }
        else
        {
            _lblPermisos.Visible = false;
        }
    }

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Catálogo y kits",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Productos individuales y kits compuestos sin ciclos (Composite)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        // Barra de búsqueda y botones (Fiel al Wireframe 03)
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 48 };
        _txtBuscar = new TextBox { Location = new Point(0, 8), Width = 280, Font = new Font("Segoe UI", 10), PlaceholderText = "Buscar por SKU, nombre o categoría..." };
        _txtBuscar.TextChanged += async (s, e) => await CargarCatalogoAsync(_txtBuscar.Text);

        _btnNuevoProd = new Button
        {
            Text = "Nuevo producto",
            Location = new Point(290, 6),
            Width = 130,
            Height = 34,
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        _btnNuevoProd.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnNuevoProd.Click += async (s, e) =>
        {
            using var dlg = new FormNuevoProducto(_productoRepo, _unitOfWork, _catalogoService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await CargarCatalogoAsync();
                _mediator.Notificar("InventarioActualizado", null);
            }
        };

        _btnNuevoKit = new Button
        {
            Text = "Nuevo kit",
            Location = new Point(428, 6),
            Width = 110,
            Height = 34,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnNuevoKit.FlatAppearance.BorderSize = 0;
        _btnNuevoKit.Click += async (s, e) =>
        {
            using var dlg = new FormNuevoKit(_productoRepo, _catalogoService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await CargarCatalogoAsync();
                _mediator.Notificar("InventarioActualizado", null);
            }
        };

        _btnInitKits = new Button
        {
            Text = "Kits oficiales",
            Location = new Point(546, 6),
            Width = 110,
            Height = 34,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        _btnInitKits.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnInitKits.Click += async (s, e) =>
        {
            await _catalogoService.InicializarKitsOficialesAsync();
            await CargarCatalogoAsync();
            MessageBox.Show("Kits oficiales (KIT_OFICINA y KIT_TELETRABAJO) cargados con éxito.", "Kits Compuestos");
        };

        _lblPermisos = new Label
        {
            Text = "Solo lectura",
            Location = new Point(665, 12),
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            Visible = false
        };

        pnlToolbar.Controls.AddRange(new Control[] { _txtBuscar, _btnNuevoProd, _btnNuevoKit, _btnInitKits, _lblPermisos });

        // Contenedor principal dividido: Izquierda tabla, Derecha árbol de kits
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 600
        };

        // Grid Catálogo
        _dgvCatalogo = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvCatalogo.Columns.Add("Sku", "SKU");
        _dgvCatalogo.Columns.Add("Producto", "Producto");
        _dgvCatalogo.Columns.Add("Precio", "Precio (USD)");
        _dgvCatalogo.Columns.Add("Stock", "Stock");
        _dgvCatalogo.Columns.Add("Peso", "Peso (kg)");
        _dgvCatalogo.SelectionChanged += (s, e) => MostrarEstructuraSeleccionada();

        split.Panel1.Controls.Add(_dgvCatalogo);

        // Panel lateral derecho: Estructura del Kit (Wireframe 03)
        var pnlDerecho = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
        var lblEstructura = new Label
        {
            Text = "Estructura del kit",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };

        _tvEstructura = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };

        var pnlKitAcciones = new Panel { Dock = DockStyle.Bottom, Height = 120 };

        _lblDetalleKit = new Label
        {
            Text = "Seleccione un kit para visualizar su composición y peso total.",
            Dock = DockStyle.Top,
            Height = 65,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9)
        };

        _btnComponerKit = new Button
        {
            Text = "Guardar composición",
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnComponerKit.FlatAppearance.BorderSize = 0;
        _btnComponerKit.Click += async (s, e) =>
        {
            using var dlg = new FormNuevoKit(_productoRepo, _catalogoService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await CargarCatalogoAsync();
                _mediator.Notificar("InventarioActualizado", null);
            }
        };

        pnlKitAcciones.Controls.Add(_lblDetalleKit);
        pnlKitAcciones.Controls.Add(_btnComponerKit);

        pnlDerecho.Controls.Add(_tvEstructura);
        pnlDerecho.Controls.Add(pnlKitAcciones);
        pnlDerecho.Controls.Add(lblEstructura);

        split.Panel2.Controls.Add(pnlDerecho);

        Controls.Add(split);
        Controls.Add(pnlToolbar);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);
    }

    public async Task CargarCatalogoAsync(string? filtro = null)
    {
        try
        {
            var productos = await _productoRepo.GetAllAsync();
            var kits = _catalogoService.ListarKits();

            _dgvCatalogo.Rows.Clear();

            // 1. Mostrar productos simples
            foreach (var p in productos)
            {
                if (string.IsNullOrWhiteSpace(filtro) ||
                    p.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    p.Categoria.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                {
                    _dgvCatalogo.Rows.Add(p.Sku, p.Nombre, $"USD {p.Precio:F2}", p.Stock.ToString(), $"{p.PesoKg:F2} kg");
                }
            }

            // 2. Mostrar kits compuestos con stock ensamblable calculado en tiempo real
            var stockDict = productos.ToDictionary(p => p.Sku, p => p.Stock);
            foreach (var k in kits)
            {
                if (string.IsNullOrWhiteSpace(filtro) ||
                    k.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    k.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                {
                    var desglose = k.ObtenerDesgloseFisico(1);
                    int stockCalculado = desglose.Count > 0
                        ? desglose.Min(c => stockDict.TryGetValue(c.Sku, out var st) ? st / c.Cantidad : 0)
                        : 0;

                    var rowIdx = _dgvCatalogo.Rows.Add(
                        k.Sku,
                        $"{k.Nombre} (KIT)",
                        $"USD {k.ObtenerPrecio():F2}",
                        $"{stockCalculado} kits",
                        $"{k.ObtenerPeso():F2} kg"
                    );

                    if (stockCalculado < 5)
                    {
                        _dgvCatalogo.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.DarkOrange;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UcCatalogoKits] Error al cargar: {ex.Message}");
        }
    }

    private void MostrarEstructuraSeleccionada()
    {
        if (_dgvCatalogo.SelectedRows.Count == 0) return;

        var sku = _dgvCatalogo.SelectedRows[0].Cells["Sku"].Value?.ToString() ?? string.Empty;
        var kit = _catalogoService.ListarKits().FirstOrDefault(k => k.Sku == sku);

        _tvEstructura.Nodes.Clear();

        if (kit == null)
        {
            _lblDetalleKit.Text = $"Producto simple seleccionado: {sku}.\nNo es un kit compuesto anidado.";
            return;
        }

        // Construir árbol recursivo del kit
        TreeNode ConstruirNodo(IElementoCatalogo elemento, int cant)
        {
            var texto = elemento.EsKit
                ? $"📁 {elemento.Sku} - {elemento.Nombre} (x{cant})"
                : $"📄 {elemento.Sku} - {elemento.Nombre} (x{cant})";

            var nodo = new TreeNode(texto);
            if (elemento is KitCompuesto kitHijo)
            {
                foreach (var comp in kitHijo.Componentes)
                {
                    nodo.Nodes.Add(ConstruirNodo(comp.Elemento, comp.Cantidad));
                }
            }
            return nodo;
        }

        var raiz = ConstruirNodo(kit, 1);
        _tvEstructura.Nodes.Add(raiz);
        _tvEstructura.ExpandAll();

        // Calcular peso logístico con Visitor
        var pesoVisitor = new PesoLogisticoVisitor();
        kit.Aceptar(pesoVisitor);

        var desglose = kit.ObtenerDesgloseFisico();
        var resumenDesglose = string.Join(", ", desglose.Select(d => $"{d.Sku} x{d.Cantidad}"));

        _lblDetalleKit.Text = $"Composición final física: {resumenDesglose}\nPrecio total: USD {kit.ObtenerPrecio():F2} | Peso logístico: {pesoVisitor.PesoTotalKg:F2} kg";
    }
}
