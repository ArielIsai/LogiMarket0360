using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Visitor;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcCatalogoKits : UserControl, IRecargableVista
{
    private readonly IProductoRepository _productoRepo;
    private readonly CatalogoService _catalogoService;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvCatalogo = null!;
    private TextBox _txtBuscar = null!;
    private TreeView _tvEstructura = null!;
    private Label _lblDetalleKit = null!;

    public UcCatalogoKits(
        IProductoRepository productoRepo,
        CatalogoService catalogoService,
        IFormularioMediator mediator)
    {
        _productoRepo = productoRepo;
        _catalogoService = catalogoService;
        _mediator = mediator;

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

    public Task RecargarAsync() => CargarCatalogoAsync();

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

        // Barra de búsqueda y botones
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45 };
        _txtBuscar = new TextBox { Location = new Point(0, 8), Width = 300, Font = new Font("Segoe UI", 10), PlaceholderText = "Buscar por SKU, nombre o categoría..." };
        _txtBuscar.TextChanged += async (s, e) => await CargarCatalogoAsync(_txtBuscar.Text);

        var btnNuevoProd = new Button
        {
            Text = "Nuevo producto",
            Location = new Point(315, 6),
            Width = 130,
            Height = 32,
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat
        };
        btnNuevoProd.Click += (s, e) => MessageBox.Show("Función de creación rápida disponible.", "Catálogo");

        var btnInitKits = new Button
        {
            Text = "Cargar kits oficiales",
            Location = new Point(455, 6),
            Width = 150,
            Height = 32,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnInitKits.Click += async (s, e) =>
        {
            await _catalogoService.InicializarKitsOficialesAsync();
            await CargarCatalogoAsync();
            MessageBox.Show("Kits oficiales (KIT_OFICINA y KIT_TELETRABAJO) cargados con éxito.", "Kits Compuestos");
        };

        pnlToolbar.Controls.AddRange(new Control[] { _txtBuscar, btnNuevoProd, btnInitKits });

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

        // Panel lateral derecho: Estructura de Kits
        var pnlDerecho = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
        var lblEstructura = new Label
        {
            Text = "Estructura del kit",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };

        _tvEstructura = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

        _lblDetalleKit = new Label
        {
            Text = "Seleccione un kit para visualizar su composición y peso total.",
            Dock = DockStyle.Bottom,
            Height = 70,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9)
        };

        pnlDerecho.Controls.Add(_tvEstructura);
        pnlDerecho.Controls.Add(_lblDetalleKit);
        pnlDerecho.Controls.Add(lblEstructura);

        split.Panel2.Controls.Add(pnlDerecho);

        Controls.Add(split);
        Controls.Add(pnlToolbar);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) =>
        {
            await _catalogoService.InicializarKitsOficialesAsync();
            await CargarCatalogoAsync();
        };
    }

    public async Task CargarCatalogoAsync(string? filtro = null)
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

        // 2. Mostrar kits compuestos
        foreach (var k in kits)
        {
            if (string.IsNullOrWhiteSpace(filtro) ||
                k.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                k.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            {
                _dgvCatalogo.Rows.Add(k.Sku, $"{k.Nombre} (KIT)", $"USD {k.ObtenerPrecio():F2}", "—", $"{k.ObtenerPeso():F2} kg");
            }
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
