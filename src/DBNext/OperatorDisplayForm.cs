using DBNext.Shared;
using System.Runtime.InteropServices;

namespace DBNext;

/// <summary>
/// P/Invoke declarations for Windows API
/// </summary>
internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_SHOWWINDOW = 0x0040;
}

/// <summary>
/// Finestra piccola per l'operatore che mostra il numero corrente del turno
/// </summary>
public class OperatorDisplayForm : Form
{
    private readonly Label _numberLabel;
    private readonly Label _labelText;
    private readonly FlowLayoutPanel _mainLayout;
    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly System.Windows.Forms.Timer _longPressTimer;
    private readonly System.Windows.Forms.Timer _alwaysOnTopTimer;
    private QueueSettings _settings = new();
    private bool _isDragging = false;
    private bool _canDrag = false;
    private Point _dragStartPoint;
    private Point _mouseDownPoint;

    public OperatorDisplayForm()
    {
        // Setup form - Piccola finestra sempre in primo piano
        this.Text = "DB-Next - Operatore";
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.BackColor = Color.Black;
        this.ForeColor = Color.White;
        this.ShowInTaskbar = false;
        // Nota: TopMost tradizionale sostituito con SetWindowPos per maggiore affidabilità

        // Layout principale - orizzontale forzato (scritta + numero sulla stessa riga)
        _mainLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        // Etichetta "TURNO" - a sinistra
        _labelText = new Label
        {
            Text = "TURNO:",
            ForeColor = Color.White,
            BackColor = Color.Black,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Font = new Font("Arial Black", 24, FontStyle.Bold),
            Visible = true,
            Padding = new Padding(10, 0, 0, 0), // Solo padding sinistro
            Margin = new Padding(0, 0, 0, 0)
        };

        // Label per il numero - subito dopo la scritta
        _numberLabel = new Label
        {
            Text = "00",
            ForeColor = Color.White,
            BackColor = Color.Black,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Font = new Font("Arial Black", 24, FontStyle.Bold),
            Padding = new Padding(0, 0, 10, 0), // Solo padding destro
            Margin = new Padding(0, 0, 0, 0)
        };

        _mainLayout.Controls.Add(_labelText);
        _mainLayout.Controls.Add(_numberLabel);

        this.Controls.Add(_mainLayout);

        // Timer per aggiornare il numero
        _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _updateTimer.Tick += async (s, e) => await UpdateNumberAsync();

        // Timer per riconoscimento tocco prolungato (800ms)
        _longPressTimer = new System.Windows.Forms.Timer { Interval = 800 };
        _longPressTimer.Tick += LongPressTimer_Tick;

        // Timer per mantenere sempre la finestra sopra (ogni 1 secondo)
        _alwaysOnTopTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _alwaysOnTopTimer.Tick += AlwaysOnTopTimer_Tick;

        // Gestione chiusura
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                this.Hide();
        };

        // Gestione trascinamento e tocco prolungato
        this.MouseDown += OperatorDisplayForm_MouseDown;
        this.MouseMove += OperatorDisplayForm_MouseMove;
        this.MouseUp += OperatorDisplayForm_MouseUp;

        // Assicurati che anche i controlli interni gestiscano gli eventi mouse
        _mainLayout.MouseDown += OperatorDisplayForm_MouseDown;
        _mainLayout.MouseMove += OperatorDisplayForm_MouseMove;
        _mainLayout.MouseUp += OperatorDisplayForm_MouseUp;
        _labelText.MouseDown += OperatorDisplayForm_MouseDown;
        _labelText.MouseMove += OperatorDisplayForm_MouseMove;
        _labelText.MouseUp += OperatorDisplayForm_MouseUp;
        _numberLabel.MouseDown += OperatorDisplayForm_MouseDown;
        _numberLabel.MouseMove += OperatorDisplayForm_MouseMove;
        _numberLabel.MouseUp += OperatorDisplayForm_MouseUp;

        // Click destro per nascondere
        this.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
                this.Hide();
        };

        this.Load += OperatorDisplayForm_Load;
        this.VisibleChanged += OperatorDisplayForm_VisibleChanged;
        this.FormClosing += (s, e) =>
        {
            e.Cancel = true; // Non chiudere, solo nascondere
            this.Hide();
        };
    }

    private void OperatorDisplayForm_Load(object? sender, EventArgs e)
    {
        LoadSettings();
        _updateTimer.Start();
        _alwaysOnTopTimer.Start();
    }

    private void OperatorDisplayForm_VisibleChanged(object? sender, EventArgs e)
    {
        if (this.Visible)
        {
            // Quando la finestra diventa visibile, assicurati che stia sempre sopra
            MakeWindowAlwaysOnTop();
        }
    }

    private void LoadSettings()
    {
        try
        {
            // Carica impostazioni dal database
            var task = Task.Run(async () => await Database.GetSettingsAsync());
            _settings = task.Result;

            // Posiziona sul monitor selezionato
            var targetScreen = GetTargetScreen();
            var bounds = targetScreen.Bounds;

            // Applica impostazioni
            var x = Math.Min(bounds.Right - _settings.OperatorWindowWidth, Math.Max(bounds.Left, _settings.OperatorWindowX));
            var y = Math.Min(bounds.Bottom - _settings.OperatorWindowHeight, Math.Max(bounds.Top, _settings.OperatorWindowY));
            this.Location = new Point(x, y);
            this.Size = new Size(_settings.OperatorWindowWidth, _settings.OperatorWindowHeight);
            this.BackColor = ColorFromHex(_settings.OperatorBgColor);
            _numberLabel.BackColor = this.BackColor;
            _numberLabel.ForeColor = ColorFromHex(_settings.OperatorTextColor);
            _labelText.BackColor = this.BackColor;
            _labelText.ForeColor = ColorFromHex(_settings.OperatorTextColor);

            // Assicurati che la finestra stia sempre sopra qualsiasi altra applicazione
            MakeWindowAlwaysOnTop();

            // Etichetta (sempre visibile sulla stessa riga)
            _labelText.Text = (_settings.OperatorLabelText ?? "TURNO").TrimEnd(':') + ":";

            // Font unificato per scritta e numero
            try
            {
                var font = new Font(
                    _settings.OperatorFontFamily,
                    _settings.OperatorFontSize,
                    FontStyle.Bold
                );
                _labelText.Font = font;
                _numberLabel.Font = font;
            }
            catch
            {
                var font = new Font("Arial Black", _settings.OperatorFontSize, FontStyle.Bold);
                _labelText.Font = font;
                _numberLabel.Font = font;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento impostazioni operatore: {ex.Message}");
        }
    }

    private async Task UpdateNumberAsync()
    {
        try
        {
            var state = await Database.GetStateAsync();
            _numberLabel.Text = state.CurrentNumber.ToString("00");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore aggiornamento numero operatore: {ex.Message}");
            _numberLabel.Text = "--";
        }
    }

    /// <summary>
    /// Aggiorna le impostazioni e la posizione della finestra
    /// </summary>
    public void UpdateSettings(QueueSettings settings)
    {
        _settings = settings;
        LoadSettings();
    }

    private void OperatorDisplayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _mouseDownPoint = new Point(e.X, e.Y);
            _canDrag = false;
            _isDragging = false;

            // Cattura tutti gli eventi mouse per assicurarsi che vengano ricevuti
            this.Capture = true;

            // Avvia timer per tocco prolungato
            _longPressTimer.Start();
        }
    }

    private void OperatorDisplayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _canDrag)
        {
            // Trascinamento attivo dopo tocco prolungato
            Point newPoint = this.PointToScreen(new Point(e.X, e.Y));
            newPoint.Offset(-_dragStartPoint.X, -_dragStartPoint.Y);
            this.Location = newPoint;
        }
        else if (_canDrag && !_isDragging)
        {
            // Dopo tocco prolungato confermato, inizia il trascinamento al primo movimento
            var distance = Math.Sqrt(Math.Pow(e.X - _mouseDownPoint.X, 2) + Math.Pow(e.Y - _mouseDownPoint.Y, 2));
            if (distance > 5) // Soglia minima per iniziare il drag
            {
                _isDragging = true;
                _dragStartPoint = new Point(e.X, e.Y); // Usa la posizione corrente come punto di inizio drag
            }
        }
        else if (_longPressTimer.Enabled && !_canDrag)
        {
            // Durante il timer del tocco prolungato, se ci si muove troppo annulla
            var distance = Math.Sqrt(Math.Pow(e.X - _mouseDownPoint.X, 2) + Math.Pow(e.Y - _mouseDownPoint.Y, 2));
            if (distance > 15) // 15 pixel di tolleranza aumentata
            {
                _longPressTimer.Stop();
            }
        }
    }

    private void OperatorDisplayForm_MouseUp(object? sender, MouseEventArgs e)
    {
        _longPressTimer.Stop();

        // Rilascia la cattura del mouse
        this.Capture = false;

        if (_isDragging && _canDrag)
        {
            // Salva la nuova posizione quando finisce il trascinamento
            _settings.OperatorWindowX = this.Location.X;
            _settings.OperatorWindowY = this.Location.Y;

            // Salva nel database in background
            Task.Run(async () =>
            {
                try
                {
                    await Database.SaveSettingsAsync(_settings);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Errore salvataggio posizione finestra operatore: {ex.Message}");
                }
            });

            // Ripristina colori dopo il salvataggio
            LoadSettings();
        }

        _isDragging = false;
        _canDrag = false;
    }

    private void LongPressTimer_Tick(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        _canDrag = true;
        _isDragging = false; // Non iniziare immediatamente il drag, aspetta MouseMove
        _dragStartPoint = _mouseDownPoint;

        // Feedback visivo (cambio colore temporaneo) - indica che ora si può trascinare
        this.BackColor = Color.FromArgb(100, 100, 150); // Blu scuro per indicare modalità drag abilitata
        _labelText.BackColor = this.BackColor;
        _numberLabel.BackColor = this.BackColor;

        // Ripristina colore dopo un breve delay
        Task.Delay(300).ContinueWith(_ =>
        {
            this.Invoke(() =>
            {
                if (!_isDragging) // Solo se non stiamo ancora trascinando
                {
                    LoadSettings(); // Riapplica i colori originali
                }
            });
        });
    }

    private void AlwaysOnTopTimer_Tick(object? sender, EventArgs e)
    {
        // Mantieni sempre la finestra sopra qualsiasi altra applicazione
        MakeWindowAlwaysOnTop();
    }

    private void MakeWindowAlwaysOnTop()
    {
        try
        {
            // Usa SetWindowPos per portare la finestra sempre in primo piano
            NativeMethods.SetWindowPos(
                this.Handle,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW
            );
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore impostazione finestra sempre sopra: {ex.Message}");
        }
    }

    private Screen GetTargetScreen()
    {
        var screens = Screen.AllScreens;
        var targetIndex = Math.Min(_settings.OperatorMonitorIndex, screens.Length - 1);
        targetIndex = Math.Max(0, targetIndex);
        return screens[targetIndex];
    }

    private static Color ColorFromHex(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                return Color.FromArgb(
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
            }
        }
        catch { }

        return hex == "#000000" ? Color.Black : Color.White;
    }
}
