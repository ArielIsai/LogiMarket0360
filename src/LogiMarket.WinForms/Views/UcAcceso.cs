using LogiMarket.Application.Services;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcAcceso : UserControl
{
    private readonly AuthService _authService;
    private readonly IFormularioMediator _mediator;

    private TextBox _txtUser = null!;
    private TextBox _txtPass = null!;
    private Label _lblError = null!;
    private Button _btnLogin = null!;

    public UcAcceso(AuthService authService, IFormularioMediator mediator)
    {
        _authService = authService;
        _mediator = mediator;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(243, 244, 246);

        var card = new Panel
        {
            Size = new Size(460, 480),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Location = new Point((Width - card.Width) / 2, Math.Max(40, (Height - card.Height) / 2));
        Resize += (s, e) => card.Location = new Point((Width - card.Width) / 2, Math.Max(40, (Height - card.Height) / 2));

        var lblTitle = new Label
        {
            Text = "ACCESO AL SISTEMA",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 45
        };

        var lblSub = new Label
        {
            Text = "Credenciales de demostración y perfil de acceso",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(107, 114, 128),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlForm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30, 20, 30, 20) };

        var lblUser = new Label { Text = "Usuario", Location = new Point(30, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtUser = new TextBox { Text = "admin.demo", Location = new Point(30, 45), Width = 380, Font = new Font("Segoe UI", 11) };

        var lblPass = new Label { Text = "Contraseña", Location = new Point(30, 85), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _txtPass = new TextBox { Text = "admin123", Location = new Point(30, 110), Width = 380, PasswordChar = '•', Font = new Font("Segoe UI", 11) };

        _lblError = new Label { Text = "", ForeColor = Color.Crimson, Location = new Point(30, 145), Width = 380, AutoSize = true };

        _btnLogin = new Button
        {
            Text = "Ingresar",
            Location = new Point(30, 175),
            Width = 380,
            Height = 42,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLogin.Click += async (s, e) => await EjecutarLoginAsync();

        var lblDemo = new Label
        {
            Text = "Roles de prueba: ADMIN • VENTAS • BODEGA • CONSULTA\nContraseñas: admin123, ventas123, bodega123, consulta123",
            Location = new Point(30, 235),
            Width = 380,
            Height = 45,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(107, 114, 128),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var pnlQuick = new FlowLayoutPanel
        {
            Location = new Point(30, 285),
            Width = 380,
            Height = 60,
            FlowDirection = FlowDirection.LeftToRight
        };

        void AddQuickBtn(string u, string p, string r)
        {
            var btn = new Button
            {
                Text = r,
                Width = 85,
                Height = 28,
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(229, 231, 235),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => { _txtUser.Text = u; _txtPass.Text = p; };
            pnlQuick.Controls.Add(btn);
        }

        AddQuickBtn("admin.demo", "admin123", "ADMIN");
        AddQuickBtn("ventas.demo", "ventas123", "VENTAS");
        AddQuickBtn("bodega.demo", "bodega123", "BODEGA");
        AddQuickBtn("consulta.demo", "consulta123", "CONSULTA");

        pnlForm.Controls.AddRange(new Control[] { lblUser, _txtUser, lblPass, _txtPass, _lblError, _btnLogin, lblDemo, pnlQuick });

        card.Controls.Add(pnlForm);
        card.Controls.Add(lblSub);
        card.Controls.Add(lblTitle);

        Controls.Add(card);
    }

    private async Task EjecutarLoginAsync()
    {
        try
        {
            _lblError.Text = string.Empty;
            _btnLogin.Enabled = false;

            var res = await _authService.IniciarSesionAsync(_txtUser.Text, _txtPass.Text);
            if (res.Exitoso)
            {
                _mediator.Notificar("SesionIniciada", res.Datos);
            }
            else
            {
                _lblError.Text = res.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }
}
