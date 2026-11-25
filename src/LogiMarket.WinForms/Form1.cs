using LogiMarket.Application.Ports;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediator;
using LogiMarket.WinForms.Views;

namespace LogiMarket.WinForms;

public partial class Form1 : Form
{
    private readonly ISesionContext _sesionContext;
    private readonly IFormularioMediator _mediator;

    private readonly UcAcceso _ucAcceso;
    private readonly UcPanelGeneral _ucPanelGeneral;
    private readonly UcCatalogoKits _ucCatalogoKits;
    private readonly UcClientes _ucClientes;
    private readonly UcCarritoBorrador _ucCarritoBorrador;
    private readonly UcPedidosHistorial _ucPedidosHistorial;
    private readonly UcCobro _ucCobro;
    private readonly UcDespacho _ucDespacho;
    private readonly UcDevoluciones _ucDevoluciones;
    private readonly UcInventario _ucInventario;
    private readonly UcReportes _ucReportes;
    private readonly UcAuditoriaSimulacion _ucAuditoriaSimulacion;

    private Panel _pnlHeader = null!;
    private Panel _pnlSidebar = null!;
    private Panel _pnlContenido = null!;
    private Label _lblUserBadge = null!;
    private Button _btnCerrarSesion = null!;

    private readonly Dictionary<string, Button> _navButtons = new();

    public Form1(
        ISesionContext sesionContext,
        IFormularioMediator mediator,
        UcAcceso ucAcceso,
        UcPanelGeneral ucPanelGeneral,
        UcCatalogoKits ucCatalogoKits,
        UcClientes ucClientes,
        UcCarritoBorrador ucCarritoBorrador,
        UcPedidosHistorial ucPedidosHistorial,
        UcCobro ucCobro,
        UcDespacho ucDespacho,
        UcDevoluciones ucDevoluciones,
        UcInventario ucInventario,
        UcReportes ucReportes,
        UcAuditoriaSimulacion ucAuditoriaSimulacion)
    {
        _sesionContext = sesionContext;
        _mediator = mediator;

        _ucAcceso = ucAcceso;
        _ucPanelGeneral = ucPanelGeneral;
        _ucCatalogoKits = ucCatalogoKits;
        _ucClientes = ucClientes;
        _ucCarritoBorrador = ucCarritoBorrador;
        _ucPedidosHistorial = ucPedidosHistorial;
        _ucCobro = ucCobro;
        _ucDespacho = ucDespacho;
        _ucDevoluciones = ucDevoluciones;
        _ucInventario = ucInventario;
        _ucReportes = ucReportes;
        _ucAuditoriaSimulacion = ucAuditoriaSimulacion;

        InitializeComponent();
        InitializeCustomComponents();

        _mediator.RegistrarColega("Form1", (evento, datos) =>
        {
            if (evento == "SesionIniciada")
            {
                ActualizarEstadoSesion();
                CambiarVista(_ucPanelGeneral, "Panel general");
            }
        });
    }

    private void InitializeCustomComponents()
    {
        Text = "LogiMarket 360 - Plataforma Empresarial Clean Architecture & GoF";
        Size = new Size(1280, 800);
        MinimumSize = new Size(1024, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9);

        // Header Superior
        _pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20, 0, 20, 0)
        };

        var lblLogo = new Label
        {
            Text = "LOGIMARKET 360",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(20, 14),
            AutoSize = true
        };

        var lblSubheader = new Label
        {
            Text = "Control comercial • Escritorio",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(200, 18),
            AutoSize = true
        };

        _lblUserBadge = new Label
        {
            Text = "No autenticado",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(900, 18),
            AutoSize = true
        };

        _btnCerrarSesion = new Button
        {
            Text = "Cerrar sesión",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1140, 12),
            Width = 100,
            Height = 30,
            BackColor = Color.FromArgb(241, 245, 249),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCerrarSesion.Click += (s, e) => CerrarSesion();

        _pnlHeader.Controls.AddRange(new Control[] { lblLogo, lblSubheader, _lblUserBadge, _btnCerrarSesion });

        // Sidebar Izquierdo
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true
        };

        // Contenedor Central
        _pnlContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252)
        };

        Controls.Add(_pnlContenido);
        Controls.Add(_pnlSidebar);
        Controls.Add(_pnlHeader);

        ConstruirBotonesSidebar();
        ActualizarEstadoSesion();
        CambiarVista(_ucAcceso, "Acceso");
    }

    private void ConstruirBotonesSidebar()
    {
        _pnlSidebar.Controls.Clear();
        _navButtons.Clear();

        var opciones = new (string Clave, string Texto, UserControl Vista)[]
        {
            ("Panel general", "Panel general", _ucPanelGeneral),
            ("Catálogo y kits", "Catálogo y kits", _ucCatalogoKits),
            ("Clientes", "Clientes", _ucClientes),
            ("Carrito / borrador", "Carrito / borrador", _ucCarritoBorrador),
            ("Pedidos e historial", "Pedidos e historial", _ucPedidosHistorial),
            ("Cobro", "Cobro", _ucCobro),
            ("Despacho", "Despacho", _ucDespacho),
            ("Devoluciones", "Devoluciones", _ucDevoluciones),
            ("Inventario", "Inventario", _ucInventario),
            ("Reportes", "Reportes", _ucReportes),
            ("Auditoría / pruebas", "Auditoría / pruebas", _ucAuditoriaSimulacion),
            ("Acceso", "Acceso / Login", _ucAcceso)
        };

        int top = 10;
        foreach (var (clave, texto, vista) in opciones)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(8, top),
                Width = 180,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand
            };

            btn.Click += (s, e) => CambiarVista(vista, clave);
            _pnlSidebar.Controls.Add(btn);
            _navButtons[clave] = btn;
            top += 42;
        }
    }

    private void CambiarVista(UserControl vista, string clave)
    {
        _pnlContenido.Controls.Clear();
        vista.Dock = DockStyle.Fill;
        _pnlContenido.Controls.Add(vista);

        foreach (var (k, btn) in _navButtons)
        {
            if (k == clave)
            {
                btn.BackColor = Color.FromArgb(241, 245, 249);
                btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                btn.ForeColor = Color.FromArgb(15, 23, 42);
            }
            else
            {
                btn.BackColor = Color.White;
                btn.Font = new Font("Segoe UI", 9);
                btn.ForeColor = Color.FromArgb(51, 65, 85);
            }
        }
    }

    private void ActualizarEstadoSesion()
    {
        var user = _sesionContext.UsuarioActual;
        if (user != null)
        {
            _lblUserBadge.Text = $"{user.Rol.ToString().ToUpperInvariant()} • Sesión: {user.NombreCompleto}";
            _lblUserBadge.ForeColor = Color.FromArgb(30, 41, 59);
            _btnCerrarSesion.Visible = true;

            AplicarPermisosVisuales(user.Rol);
        }
        else
        {
            _lblUserBadge.Text = "No autenticado • Inicie sesión";
            _lblUserBadge.ForeColor = Color.FromArgb(148, 163, 184);
            _btnCerrarSesion.Visible = false;
        }
    }

    private void AplicarPermisosVisuales(RolUsuario rol)
    {
        if (_navButtons.TryGetValue("Cobro", out var btnCobro))
        {
            btnCobro.Enabled = rol is RolUsuario.Admin or RolUsuario.Ventas or RolUsuario.Consulta;
        }

        if (_navButtons.TryGetValue("Despacho", out var btnDesp))
        {
            btnDesp.Enabled = rol is RolUsuario.Admin or RolUsuario.Bodega;
        }
    }

    private void CerrarSesion()
    {
        _sesionContext.CerrarSesion();
        ActualizarEstadoSesion();
        CambiarVista(_ucAcceso, "Acceso");
    }
}
