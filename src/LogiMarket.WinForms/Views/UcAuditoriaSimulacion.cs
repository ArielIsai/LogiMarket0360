using LogiMarket.Application.Patterns.FactoryMethod;
using LogiMarket.Application.Ports;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.WinForms.Mediator;

namespace LogiMarket.WinForms.Views;

public class UcAuditoriaSimulacion : UserControl, IRecargableVista
{
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IEventoOutboxRepository _outboxRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LogiMarketDbContext _dbContext;
    private readonly IFormularioMediator _mediator;

    private DataGridView _dgvAuditoria = null!;
    private CheckBox _chkFalloOutbox = null!;
    private CheckBox _chkSimularRechazoCobro = null!;
    private Button _btnProcesarOutbox = null!;
    private Button _btnRestablecerBase = null!;
    private Label _lblStatusOutbox = null!;

    public UcAuditoriaSimulacion(
        IAuditoriaRepository auditoriaRepo,
        IEventoOutboxRepository outboxRepo,
        IUnitOfWork unitOfWork,
        LogiMarketDbContext dbContext,
        IFormularioMediator mediator)
    {
        _auditoriaRepo = auditoriaRepo;
        _outboxRepo = outboxRepo;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _mediator = mediator;

        InitializeUi();

        _mediator.RegistrarColega("UcAuditoriaSimulacion", (evento, datos) =>
        {
            if (IsHandleCreated)
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(async () => await CargarAuditoriaYOutboxAsync()));
                else
                    _ = CargarAuditoriaYOutboxAsync();
            }
        });
    }

    public Task RecargarAsync() => CargarAuditoriaYOutboxAsync();

    private void InitializeUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Padding = new Padding(24);

        var lblHeader = new Label
        {
            Text = "Auditoría y simulación",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Top,
            Height = 30
        };

        var lblSub = new Label
        {
            Text = "Eventos, registros de auditoría y escenarios controlados de prueba (Outbox Pattern)",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 25
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620
        };

        // Grid Auditoría (Izquierda)
        _dgvAuditoria = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _dgvAuditoria.Columns.Add("Hora", "Hora");
        _dgvAuditoria.Columns.Add("Usuario", "Usuario");
        _dgvAuditoria.Columns.Add("Accion", "Acción / Evento");
        _dgvAuditoria.Columns.Add("Detalle", "Detalle");

        split.Panel1.Controls.Add(_dgvAuditoria);

        // Panel de Control de Simulación (Derecha)
        var pnlSim = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16)
        };
        var lblST = new Label { Text = "Modo de demostración", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };

        _chkSimularRechazoCobro = new CheckBox
        {
            Text = "Inducir rechazo de cobro (Proxy/Pasarela)",
            Location = new Point(16, 50),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        _chkFalloOutbox = new CheckBox
        {
            Text = "Inducir fallo de comunicación en Outbox",
            Location = new Point(16, 85),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        _btnProcesarOutbox = new Button
        {
            Text = "🔄 Procesar eventos Outbox",
            Location = new Point(16, 130),
            Width = 260,
            Height = 38,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        _btnProcesarOutbox.Click += async (s, e) => await ProcesarOutboxAsync();

        _lblStatusOutbox = new Label
        {
            Text = "Outbox: comprobando...",
            Location = new Point(16, 180),
            Width = 260,
            Height = 50,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(71, 85, 105)
        };

        _btnRestablecerBase = new Button
        {
            Text = "⚠️ Restablecer base de datos (Seed Oficial)",
            Location = new Point(16, 250),
            Width = 260,
            Height = 38,
            BackColor = Color.FromArgb(254, 226, 226),
            ForeColor = Color.FromArgb(185, 28, 28),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        _btnRestablecerBase.Click += async (s, e) => await RestablecerBaseAsync();

        pnlSim.Controls.AddRange(new Control[] { lblST, _chkSimularRechazoCobro, _chkFalloOutbox, _btnProcesarOutbox, _lblStatusOutbox, _btnRestablecerBase });
        split.Panel2.Controls.Add(pnlSim);

        Controls.Add(split);
        Controls.Add(lblSub);
        Controls.Add(lblHeader);
    }

    public async Task CargarAuditoriaYOutboxAsync()
    {
        try
        {
            var logs = await _auditoriaRepo.GetUltimosAsync(50);
            _dgvAuditoria.Rows.Clear();

            foreach (var l in logs)
            {
                _dgvAuditoria.Rows.Add(l.Fecha.ToString("HH:mm:ss"), l.Usuario, l.Accion, l.Detalle);
            }

            var pendientes = await _outboxRepo.GetPendientesAsync();
            _lblStatusOutbox.Text = $"Eventos en tabla EventoOutbox:\n• Pendientes: {pendientes.Count}\n• Procesados con éxito: {(await _outboxRepo.GetAllAsync()).Count - pendientes.Count}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UcAuditoriaSimulacion] Error al cargar: {ex.Message}");
        }
    }

    private async Task ProcesarOutboxAsync()
    {
        _btnProcesarOutbox.Enabled = false;
        var procesador = new ProcesadorOutbox(_outboxRepo, _unitOfWork, new EmailNotificadorCreator());

        bool inducirFallo = _chkFalloOutbox.Checked;
        var procesados = await procesador.ProcesarPendientesAsync(simularFallo: inducirFallo);

        if (inducirFallo)
        {
            MessageBox.Show("Fallo inducido en Outbox: el evento quedó en estado de reintento con error registrado.", "Simulación Outbox");
        }
        else
        {
            MessageBox.Show($"Se procesaron {procesados} evento(s) durable(s) correctamente.", "Outbox Procesado");
        }

        await CargarAuditoriaYOutboxAsync();
        _btnProcesarOutbox.Enabled = true;
    }

    private async Task RestablecerBaseAsync()
    {
        var resp = MessageBox.Show(
            "¿Desea restablecer las existencias de productos al estado oficial (P01=8, P02=5, P03=20, P04=4, P05=30) y reiniciar pedidos de prueba?",
            "Restablecer Base de Datos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (resp == DialogResult.Yes)
        {
            await DbInitializer.RestablecerBasePruebasAsync(_dbContext);
            MessageBox.Show("Base de datos restablecida a los valores oficiales controlados.", "Éxito");
            _mediator.Notificar("InventarioActualizado");
            _mediator.Notificar("PedidoConfirmado");
            await CargarAuditoriaYOutboxAsync();
        }
    }
}
