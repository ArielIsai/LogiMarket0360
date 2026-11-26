using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Patterns.Composite;
using LogiMarket.Domain.Patterns.Visitor;

namespace LogiMarket.WinForms.Views;

public class FormNuevoKit : Form
{
    private readonly IProductoRepository _productoRepo;
    private readonly CatalogoService _catalogoService;

    private TextBox _txtSkuKit = null!;
    private TextBox _txtNombreKit = null!;
    private ComboBox _cmbElemento = null!;
    private NumericUpDown _numCantidad = null!;
    private Button _btnAgregar = null!;
    private Button _btnQuitar = null!;
    private DataGridView _dgvComponentes = null!;
    private Label _lblResumen = null!;
    private Button _btnGuardar = null!;
    private Button _btnCancelar = null!;

    private readonly List<ItemComponenteDto> _componentes = new();

    private sealed record ItemComponenteDto(
        string Sku,
        string Nombre,
        bool EsKit,
        int Cantidad,
        decimal PrecioUnitario,
        decimal PesoUnitario
    );

    public FormNuevoKit(
        IProductoRepository productoRepo,
        CatalogoService catalogoService)
    {
        _productoRepo = productoRepo;
        _catalogoService = catalogoService;

        InitializeComponent();
        _ = CargarElementosDisponiblesAsync();
    }

    private void InitializeComponent()
    {
        Text = "Nuevo Kit Compuesto - Patrón GoF Composite";
        Width = 720;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 9.5f);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 12, 20, 12)
        };
        var lblTitle = new Label
        {
            Text = "Nuevo Kit Compuesto (GoF Composite)",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        };
        var lblSub = new Label
        {
            Text = "Agrupa productos u otros kits anidados sin ciclos. Cómputo recursivo de precio y peso logístico.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = true,
            Location = new Point(17, 36)
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Identificación del Kit
        var pnlId = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(20, 10, 20, 5) };
        var lblSku = new Label { Text = "SKU del Kit:", Location = new Point(20, 12), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _txtSkuKit = new TextBox { Location = new Point(20, 34), Width = 160, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "Ej: KIT_GAMER" };

        var lblNom = new Label { Text = "Nombre descriptivo del Kit:", Location = new Point(200, 12), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _txtNombreKit = new TextBox { Location = new Point(200, 34), Width = 480, PlaceholderText = "Ej: Kit Gamer Avanzado (Periféricos Pro)" };

        pnlId.Controls.AddRange(new Control[] { lblSku, _txtSkuKit, lblNom, _txtNombreKit });

        // Selector para agregar componente
        var pnlAgregar = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(20, 8, 20, 8), BackColor = Color.FromArgb(241, 245, 249) };
        var lblSelec = new Label { Text = "Componente a incluir (Producto o Sub-Kit):", Location = new Point(20, 10), AutoSize = true };
        _cmbElemento = new ComboBox { Location = new Point(20, 32), Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };

        var lblCant = new Label { Text = "Cantidad:", Location = new Point(415, 10), AutoSize = true };
        _numCantidad = new NumericUpDown { Location = new Point(415, 32), Width = 75, Minimum = 1, Maximum = 99, Value = 1 };

        _btnAgregar = new Button
        {
            Text = "+ Añadir al kit",
            Location = new Point(505, 30),
            Width = 115,
            Height = 30,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnAgregar.FlatAppearance.BorderSize = 0;
        _btnAgregar.Click += async (s, e) => await AgregarComponenteAlKitAsync();

        _btnQuitar = new Button
        {
            Text = "Quitar",
            Location = new Point(625, 30),
            Width = 65,
            Height = 30,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(220, 38, 38),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnQuitar.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
        _btnQuitar.Click += (s, e) => QuitarComponenteSeleccionado();

        pnlAgregar.Controls.AddRange(new Control[] { lblSelec, _cmbElemento, lblCant, _numCantidad, _btnAgregar, _btnQuitar });

        // Grilla de componentes del Kit
        _dgvComponentes = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9f)
        };
        _dgvComponentes.Columns.Add("Tipo", "Tipo");
        _dgvComponentes.Columns.Add("Sku", "SKU");
        _dgvComponentes.Columns.Add("Nombre", "Descripción");
        _dgvComponentes.Columns.Add("Cantidad", "Cant.");
        _dgvComponentes.Columns.Add("PrecioUnit", "P. Unitario");
        _dgvComponentes.Columns.Add("Subtotal", "Subtotal");
        _dgvComponentes.Columns.Add("PesoUnit", "Peso unit.");

        // Panel inferior con resumen y acciones
        var pnlInferior = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(20, 10, 20, 10) };
        _lblResumen = new Label
        {
            Text = "Componentes: 0 | Precio total: USD 0.00 | Peso estimado: 0.00 kg\nDesglose físico final: -",
            Dock = DockStyle.Top,
            Height = 45,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };

        var pnlBotones = new Panel { Dock = DockStyle.Bottom, Height = 45 };
        _btnGuardar = new Button
        {
            Text = "Guardar kit compuesto",
            Width = 175,
            Height = 36,
            Location = new Point(340, 5),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnGuardar.FlatAppearance.BorderSize = 0;
        _btnGuardar.Click += async (s, e) => await GuardarKitAsync();

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Width = 100,
            Height = 36,
            Location = new Point(525, 5),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBotones.Controls.AddRange(new Control[] { _btnGuardar, _btnCancelar });
        pnlInferior.Controls.AddRange(new Control[] { _lblResumen, pnlBotones });

        Controls.Add(_dgvComponentes);
        Controls.Add(pnlInferior);
        Controls.Add(pnlAgregar);
        Controls.Add(pnlId);
        Controls.Add(pnlHeader);
    }

    private async Task CargarElementosDisponiblesAsync()
    {
        _cmbElemento.Items.Clear();

        // 1. Productos simples
        var productos = await _productoRepo.GetAllAsync();
        foreach (var p in productos)
        {
            _cmbElemento.Items.Add(new ComboElementoItem(p.Sku, p.Nombre, p.Precio, p.PesoKg, false));
        }

        // 2. Kits compuestos existentes
        var kits = _catalogoService.ListarKits();
        foreach (var k in kits)
        {
            _cmbElemento.Items.Add(new ComboElementoItem(k.Sku, k.Nombre, k.ObtenerPrecio(), k.ObtenerPeso(), true));
        }

        if (_cmbElemento.Items.Count > 0)
            _cmbElemento.SelectedIndex = 0;
    }

    private async Task AgregarComponenteAlKitAsync()
    {
        if (_cmbElemento.SelectedItem is not ComboElementoItem selected)
        {
            MessageBox.Show("Seleccione un componente de la lista.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var skuKit = _txtSkuKit.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(skuKit))
        {
            MessageBox.Show("Ingrese primero el SKU del Kit antes de añadir componentes para validar referencias circulares.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSkuKit.Focus();
            return;
        }

        var cantidad = (int)_numCantidad.Value;

        // Comprobación anti-ciclos estricta del patrón Composite
        try
        {
            var kitPrueba = new KitCompuesto(skuKit, string.IsNullOrWhiteSpace(_txtNombreKit.Text) ? "Temp" : _txtNombreKit.Text.Trim());

            // Agregar componentes previamente incluidos
            foreach (var item in _componentes)
            {
                var elExistente = await _catalogoService.ObtenerElementoCatalogoAsync(item.Sku);
                if (elExistente != null)
                {
                    kitPrueba.AgregarComponente(elExistente, item.Cantidad);
                }
            }

            // Intentar agregar el nuevo elemento candidato
            var nuevoElemento = await _catalogoService.ObtenerElementoCatalogoAsync(selected.Sku);
            if (nuevoElemento == null)
            {
                MessageBox.Show($"No se pudo resolver el elemento {selected.Sku} desde el catálogo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Aquí salta DomainException si hay ciclo (A contiene B y B contiene A, o Kit contiene a sí mismo)
            kitPrueba.AgregarComponente(nuevoElemento, cantidad);

            // Si pasó la verificación anti-ciclos sin excepción:
            var idx = _componentes.FindIndex(c => c.Sku == selected.Sku);
            if (idx >= 0)
            {
                var actual = _componentes[idx];
                _componentes[idx] = actual with { Cantidad = actual.Cantidad + cantidad };
            }
            else
            {
                _componentes.Add(new ItemComponenteDto(
                    selected.Sku,
                    selected.Nombre,
                    selected.EsKit,
                    cantidad,
                    selected.Precio,
                    selected.Peso
                ));
            }

            ActualizarGrillaYResumen();
        }
        catch (DomainException dex)
        {
            MessageBox.Show(
                $"[RECHAZO COMPOSITE ANTI-CICLOS]\n\n{dex.Message}\n\n" +
                "La regla de negocio de la guía APE-04 impide que un kit contenga referencias circulares directas o indirectas.",
                "Referencia Circular Detectada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void QuitarComponenteSeleccionado()
    {
        if (_dgvComponentes.SelectedRows.Count == 0) return;

        var sku = _dgvComponentes.SelectedRows[0].Cells["Sku"].Value?.ToString();
        if (sku != null)
        {
            _componentes.RemoveAll(c => c.Sku == sku);
            ActualizarGrillaYResumen();
        }
    }

    private void ActualizarGrillaYResumen()
    {
        _dgvComponentes.Rows.Clear();

        decimal precioTotal = 0;
        decimal pesoTotal = 0;

        foreach (var c in _componentes)
        {
            var subtotal = c.PrecioUnitario * c.Cantidad;
            precioTotal += subtotal;
            pesoTotal += c.PesoUnitario * c.Cantidad;

            _dgvComponentes.Rows.Add(
                c.EsKit ? "KIT COMPUESTO" : "PRODUCTO SIMPLE",
                c.Sku,
                c.Nombre,
                c.Cantidad,
                $"USD {c.PrecioUnitario:F2}",
                $"USD {subtotal:F2}",
                $"{c.PesoUnitario:F2} kg"
            );
        }

        // Construir kit temporal para obtener el desglose físico expandido (hojas del Composite)
        var skuTemp = string.IsNullOrWhiteSpace(_txtSkuKit.Text) ? "KIT_TEMP" : _txtSkuKit.Text.Trim().ToUpperInvariant();
        var kitTemp = new KitCompuesto(skuTemp, "Kit Temporal");
        foreach (var c in _componentes)
        {
            var el = _catalogoService.ObtenerElementoCatalogoAsync(c.Sku).GetAwaiter().GetResult();
            if (el != null)
            {
                kitTemp.AgregarComponente(el, c.Cantidad);
            }
        }

        var desglose = kitTemp.ObtenerDesgloseFisico();
        var resumenDesglose = desglose.Count > 0
            ? string.Join(", ", desglose.Select(d => $"{d.Sku} x{d.Cantidad}"))
            : "-";

        _lblResumen.Text = $"Componentes: {_componentes.Count} | Precio total acumulado: USD {precioTotal:F2} | Peso total logístico: {pesoTotal:F2} kg\n" +
                           $"Desglose físico expandido: {resumenDesglose}";
    }

    private async Task GuardarKitAsync()
    {
        var sku = _txtSkuKit.Text.Trim().ToUpperInvariant();
        var nombre = _txtNombreKit.Text.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            MessageBox.Show("El SKU del kit es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSkuKit.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre del kit es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNombreKit.Focus();
            return;
        }

        if (_componentes.Count == 0)
        {
            MessageBox.Show("Debe incluir al menos un componente en el kit.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Verificar si el SKU colisiona con un producto de base de datos
        var productoExistente = await _productoRepo.GetBySkuAsync(sku);
        if (productoExistente != null)
        {
            MessageBox.Show($"El SKU '{sku}' ya pertenece al producto simple '{productoExistente.Nombre}'. Elija otro SKU.", "SKU Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSkuKit.Focus();
            return;
        }

        // Si ya existe como kit, confirmar sobrescritura o advertir
        var kitsExistentes = _catalogoService.ListarKits();
        var yaExisteKit = kitsExistentes.Any(k => k.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
        if (yaExisteKit)
        {
            var resp = MessageBox.Show($"El kit '{sku}' ya existe. ¿Desea actualizar su composición?", "Actualizar Kit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resp != DialogResult.Yes) return;
        }

        try
        {
            var nuevoKit = new KitCompuesto(sku, nombre);
            foreach (var item in _componentes)
            {
                var elemento = await _catalogoService.ObtenerElementoCatalogoAsync(item.Sku);
                if (elemento != null)
                {
                    nuevoKit.AgregarComponente(elemento, item.Cantidad);
                }
            }

            // Registrar en el catálogo central
            _catalogoService.RegistrarKit(nuevoKit);

            var pesoVisitor = new PesoLogisticoVisitor();
            nuevoKit.Aceptar(pesoVisitor);

            MessageBox.Show(
                $"Kit Compuesto '{nuevoKit.Sku} - {nuevoKit.Nombre}' guardado exitosamente.\n\n" +
                $"Precio computado: USD {nuevoKit.ObtenerPrecio():F2}\n" +
                $"Peso computado: {pesoVisitor.PesoTotalKg:F2} kg\n" +
                $"Componentes directos: {nuevoKit.Componentes.Count}\n" +
                $"Hojas físicas expandidas: {string.Join(", ", nuevoKit.ObtenerDesgloseFisico().Select(d => $"{d.Sku} x{d.Cantidad}"))}",
                "Kit Registrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (DomainException dex)
        {
            MessageBox.Show(dex.Message, "Error de Invariante Composite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar el kit: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed record ComboElementoItem(string Sku, string Nombre, decimal Precio, decimal Peso, bool EsKit)
    {
        public override string ToString() => $"[{(EsKit ? "KIT" : "PROD")}] {Sku} - {Nombre} (USD {Precio:F2} | {Peso:F2} kg)";
    }
}
