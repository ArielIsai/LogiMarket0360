using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcClientes : UserControl
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvClientes = null!;
    private TextBox _txtBuscar = null!;
    private TextBox _txtNombre = null!;
    private ComboBox _cmbCategoria = null!;
    private TextBox _txtContacto = null!;
    private TextBox _txtIdentificacion = null!;
    private Button _btnGuardar = null!;
    private Cliente? _clienteSeleccionado;

    public UcClientes(IClienteRepository clienteRepo, IUnitOfWork unitOfWork, IFormularioMediator mediator)
    {
        _clienteRepo = clienteRepo;
        _unitOfWork = unitOfWork;
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
            Text = "Gestión de clientes",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Registro, búsqueda y categorización comercial (Normal / VIP)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45 };
        _txtBuscar = new TextBox { Location = new Point(0, 8), Width = 300, Font = new Font("Segoe UI", 10), PlaceholderText = "Nombre o identificación..." };
        _txtBuscar.TextChanged += async (s, e) => await CargarClientesAsync(_txtBuscar.Text);

        var btnNuevo = new Button
        {
            Text = "Nuevo cliente",
            Location = new Point(315, 6),
            Width = 120,
            Height = 32,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnNuevo.Click += (s, e) => LimpiarFicha();

        pnlToolbar.Controls.AddRange(new Control[] { _txtBuscar, btnNuevo });

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 600
        };

        _dgvClientes = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvClientes.Columns.Add("Codigo", "Código");
        _dgvClientes.Columns.Add("Nombre", "Nombre");
        _dgvClientes.Columns.Add("Categoria", "Categoría");
        _dgvClientes.Columns.Add("Contacto", "Contacto");
        _dgvClientes.SelectionChanged += (s, e) => CargarFichaCliente();

        split.Panel1.Controls.Add(_dgvClientes);

        // Ficha del Cliente (Panel derecho)
        var pnlFicha = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0) };
        var lblFichaT = new Label { Text = "Ficha del cliente", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        var lblN = new Label { Text = "Nombre completo:", Location = new Point(16, 45), AutoSize = true };
        _txtNombre = new TextBox { Location = new Point(16, 70), Width = 280, Font = new Font("Segoe UI", 10) };

        var lblId = new Label { Text = "Identificación:", Location = new Point(16, 105), AutoSize = true };
        _txtIdentificacion = new TextBox { Location = new Point(16, 130), Width = 280, Font = new Font("Segoe UI", 10) };

        var lblCat = new Label { Text = "Categoría comercial:", Location = new Point(16, 165), AutoSize = true };
        _cmbCategoria = new ComboBox { Location = new Point(16, 190), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
        _cmbCategoria.Items.AddRange(new object[] { CategoriaCliente.Normal, CategoriaCliente.Vip });

        var lblCont = new Label { Text = "Contacto (Email / Celular):", Location = new Point(16, 225), AutoSize = true };
        _txtContacto = new TextBox { Location = new Point(16, 250), Width = 280, Font = new Font("Segoe UI", 10) };

        _btnGuardar = new Button
        {
            Text = "Guardar cambios",
            Location = new Point(16, 300),
            Width = 140,
            Height = 36,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnGuardar.Click += async (s, e) => await GuardarClienteAsync();

        pnlFicha.Controls.AddRange(new Control[] { lblFichaT, lblN, _txtNombre, lblId, _txtIdentificacion, lblCat, _cmbCategoria, lblCont, _txtContacto, _btnGuardar });
        split.Panel2.Controls.Add(pnlFicha);

        Controls.Add(split);
        Controls.Add(pnlToolbar);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);

        Load += async (s, e) => await CargarClientesAsync();
    }

    public async Task CargarClientesAsync(string? filtro = null)
    {
        var clientes = await _clienteRepo.GetAllAsync();
        _dgvClientes.Rows.Clear();

        foreach (var c in clientes)
        {
            if (string.IsNullOrWhiteSpace(filtro) ||
                c.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                c.Identificacion.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                c.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            {
                _dgvClientes.Rows.Add(c.Codigo, c.Nombre, c.Categoria.ToString(), c.Contacto);
            }
        }
    }

    private async void CargarFichaCliente()
    {
        if (_dgvClientes.SelectedRows.Count == 0) return;
        var codigo = _dgvClientes.SelectedRows[0].Cells["Codigo"].Value?.ToString();
        if (string.IsNullOrEmpty(codigo)) return;

        _clienteSeleccionado = await _clienteRepo.GetByCodigoAsync(codigo);
        if (_clienteSeleccionado != null)
        {
            _txtNombre.Text = _clienteSeleccionado.Nombre;
            _txtIdentificacion.Text = _clienteSeleccionado.Identificacion;
            _cmbCategoria.SelectedItem = _clienteSeleccionado.Categoria;
            _txtContacto.Text = _clienteSeleccionado.Contacto;
        }
    }

    private void LimpiarFicha()
    {
        _clienteSeleccionado = null;
        _txtNombre.Text = string.Empty;
        _txtIdentificacion.Text = string.Empty;
        _cmbCategoria.SelectedItem = CategoriaCliente.Normal;
        _txtContacto.Text = string.Empty;
        _txtNombre.Focus();
    }

    private async Task GuardarClienteAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtNombre.Text))
        {
            MessageBox.Show("El nombre del cliente es obligatorio.", "Validación");
            return;
        }

        if (_clienteSeleccionado == null)
        {
            // Nuevo
            var count = (await _clienteRepo.GetAllAsync()).Count + 1;
            var codigo = $"C{count:D3}";
            var nuevo = new Cliente(
                codigo,
                _txtNombre.Text,
                _txtIdentificacion.Text,
                (CategoriaCliente)(_cmbCategoria.SelectedItem ?? CategoriaCliente.Normal),
                _txtContacto.Text,
                _txtContacto.Text
            );
            await _clienteRepo.AddAsync(nuevo);
        }
        else
        {
            // Actualizar
            _clienteSeleccionado.Nombre = _txtNombre.Text;
            _clienteSeleccionado.Identificacion = _txtIdentificacion.Text;
            _clienteSeleccionado.Categoria = (CategoriaCliente)(_cmbCategoria.SelectedItem ?? CategoriaCliente.Normal);
            _clienteSeleccionado.Contacto = _txtContacto.Text;
            await _clienteRepo.UpdateAsync(_clienteSeleccionado);
        }

        await _unitOfWork.CommitAsync();
        await CargarClientesAsync();
        MessageBox.Show("Cliente guardado correctamente.", "Éxito");
        _mediator.Notificar("ClientesActualizados");
    }
}
