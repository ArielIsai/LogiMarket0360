using LogiMarket.Application.Ports;
using LogiMarket.Application.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.WinForms.Views;

public class FormNuevoProducto : Form
{
    private readonly IProductoRepository _productoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CatalogoService _catalogoService;

    private TextBox _txtSku = null!;
    private TextBox _txtNombre = null!;
    private ComboBox _cmbCategoria = null!;
    private NumericUpDown _numPrecio = null!;
    private NumericUpDown _numStock = null!;
    private NumericUpDown _numPeso = null!;
    private Button _btnGuardar = null!;
    private Button _btnCancelar = null!;

    public FormNuevoProducto(
        IProductoRepository productoRepo,
        IUnitOfWork unitOfWork,
        CatalogoService catalogoService)
    {
        _productoRepo = productoRepo;
        _unitOfWork = unitOfWork;
        _catalogoService = catalogoService;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Registrar Nuevo Producto - LogiMarket 360";
        Width = 440;
        Height = 500;
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
            Height = 65,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 12, 20, 12)
        };
        var lblTitle = new Label
        {
            Text = "Nuevo Producto Simple",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        };
        var lblSub = new Label
        {
            Text = "Catálogo de artículos individuales con SKU, precio, stock y peso",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = true,
            Location = new Point(17, 34)
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Body Controls
        int startY = 80;
        int spacing = 52;
        int leftLabel = 24;
        int leftControl = 145;
        int controlWidth = 245;

        // 1. SKU
        var lblSku = new Label { Text = "SKU único:", Location = new Point(leftLabel, startY + 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _txtSku = new TextBox { Location = new Point(leftControl, startY), Width = controlWidth, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "Ej: P06, SSD-1TB" };

        // 2. Nombre
        var lblNombre = new Label { Text = "Nombre:", Location = new Point(leftLabel, startY + spacing + 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _txtNombre = new TextBox { Location = new Point(leftControl, startY + spacing), Width = controlWidth, PlaceholderText = "Ej: Disco Sólido SSD 1TB" };

        // 3. Categoría
        var lblCat = new Label { Text = "Categoría:", Location = new Point(leftLabel, startY + spacing * 2 + 4), AutoSize = true };
        _cmbCategoria = new ComboBox { Location = new Point(leftControl, startY + spacing * 2), Width = controlWidth, DropDownStyle = ComboBoxStyle.DropDown };
        _cmbCategoria.Items.AddRange(new object[] { "Periféricos", "Accesorios", "Almacenamiento", "Hardware", "Audio y Video", "Oficina" });
        _cmbCategoria.SelectedIndex = 0;

        // 4. Precio (USD)
        var lblPrecio = new Label { Text = "Precio (USD):", Location = new Point(leftLabel, startY + spacing * 3 + 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _numPrecio = new NumericUpDown { Location = new Point(leftControl, startY + spacing * 3), Width = 120, DecimalPlaces = 2, Minimum = 0.01m, Maximum = 99999.99m, Increment = 1.00m, Value = 25.00m };

        // 5. Stock inicial
        var lblStock = new Label { Text = "Stock inicial:", Location = new Point(leftLabel, startY + spacing * 4 + 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        _numStock = new NumericUpDown { Location = new Point(leftControl, startY + spacing * 4), Width = 120, DecimalPlaces = 0, Minimum = 0, Maximum = 99999, Increment = 1, Value = 10 };

        // 6. Peso (kg)
        var lblPeso = new Label { Text = "Peso (kg):", Location = new Point(leftLabel, startY + spacing * 5 + 4), AutoSize = true };
        _numPeso = new NumericUpDown { Location = new Point(leftControl, startY + spacing * 5), Width = 120, DecimalPlaces = 2, Minimum = 0.00m, Maximum = 999.99m, Increment = 0.05m, Value = 0.35m };

        // Buttons Panel
        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(20, 12, 24, 12)
        };

        _btnGuardar = new Button
        {
            Text = "Guardar producto",
            Width = 145,
            Height = 36,
            Location = new Point(135, 12),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnGuardar.FlatAppearance.BorderSize = 0;
        _btnGuardar.Click += async (s, e) => await GuardarProductoAsync();

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Width = 90,
            Height = 36,
            Location = new Point(290, 12),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlButtons.Controls.AddRange(new Control[] { _btnGuardar, _btnCancelar });

        Controls.AddRange(new Control[]
        {
            pnlHeader,
            lblSku, _txtSku,
            lblNombre, _txtNombre,
            lblCat, _cmbCategoria,
            lblPrecio, _numPrecio,
            lblStock, _numStock,
            lblPeso, _numPeso,
            pnlButtons
        });
    }

    private async Task GuardarProductoAsync()
    {
        var sku = _txtSku.Text.Trim().ToUpperInvariant();
        var nombre = _txtNombre.Text.Trim();
        var categoria = string.IsNullOrWhiteSpace(_cmbCategoria.Text) ? "General" : _cmbCategoria.Text.Trim();
        var precio = _numPrecio.Value;
        var stock = (int)_numStock.Value;
        var peso = _numPeso.Value;

        if (string.IsNullOrWhiteSpace(sku))
        {
            MessageBox.Show("El SKU del producto es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSku.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre del producto es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNombre.Focus();
            return;
        }

        // Validar unicidad de SKU en base de datos
        var existenteProd = await _productoRepo.GetBySkuAsync(sku);
        if (existenteProd != null)
        {
            MessageBox.Show($"Ya existe un producto con el SKU '{sku}' ({existenteProd.Nombre}).\nEl SKU debe ser único.", "SKU Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSku.Focus();
            return;
        }

        // Validar que no choque con SKU de un Kit registrado
        var kits = _catalogoService.ListarKits();
        if (kits.Any(k => k.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"El SKU '{sku}' ya está asignado a un Kit compuesto del catálogo.", "SKU Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSku.Focus();
            return;
        }

        try
        {
            // Validar invariantes del dominio mediante el constructor de Producto
            var nuevoProducto = new Producto(sku, nombre, categoria, precio, stock, peso);

            await _productoRepo.AddAsync(nuevoProducto);
            await _unitOfWork.CommitAsync();

            MessageBox.Show(
                $"Producto '{nuevoProducto.Sku} - {nuevoProducto.Nombre}' guardado exitosamente.\n\n" +
                $"Precio: USD {nuevoProducto.Precio:F2}\n" +
                $"Stock inicial: {nuevoProducto.Stock}\n" +
                $"Peso: {nuevoProducto.PesoKg:F2} kg",
                "Producto Registrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (DomainException dex)
        {
            MessageBox.Show(dex.Message, "Error de Invariante de Dominio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al registrar producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
