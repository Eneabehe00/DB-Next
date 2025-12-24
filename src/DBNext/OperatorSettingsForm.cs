using DBNext.Shared;

namespace DBNext;

/// <summary>
/// Finestra di impostazioni dedicata all'operatore con tastiera touch
/// </summary>
public class OperatorSettingsForm : Form
{
    // Controlli per la tab Turno
    private NumericUpDown _numCurrentNumber = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;
    private Button _btnSetNumber = null!;
    private TableLayoutPanel _keyboardPanel = null!;

    // Controlli per la tab Media
    private TextBox _txtMediaPath = null!;
    private ComboBox _cmbMediaType = null!;
    private ComboBox _cmbMediaFit = null!;
    private CheckBox _chkFolderMode = null!;
    private NumericUpDown _numSlideshowInterval = null!;

    // Controlli per lo scheduler media
    private CheckBox _chkSchedulerEnabled = null!;
    private DateTimePicker _dtpSchedulerStart = null!;
    private DateTimePicker _dtpSchedulerEnd = null!;
    private TextBox _txtSchedulerPath = null!;
    private ComboBox _cmbSchedulerType = null!;
    private ComboBox _cmbSchedulerFit = null!;
    private CheckBox _chkSchedulerFolderMode = null!;
    private NumericUpDown _numSchedulerInterval = null!;

    // Controlli comuni
    private Label _lblStatus = null!;
    private Button _btnSave = null!;
    private Button _btnReload = null!;

    private QueueSettings _settings = new();

    public OperatorSettingsForm()
    {
        InitializeComponent();
        this.Load += OperatorSettingsForm_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "DB-Next - Impostazioni Operatore";
        this.Size = new Size(1000, 950);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.MinimumSize = new Size(900, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 40);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 9);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White
        };

        // === Tab Turno ===
        var tabTurno = new TabPage("🎯 Turno")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var turnoLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, AutoSize = true };

        // === Sezione Numero Corrente ===
        var grpNumber = CreateGroupBox("Numero Corrente");
        var numberLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };

        _numCurrentNumber = new NumericUpDown
        {
            Minimum = 0, Maximum = 99, Value = 0, Width = 120,
            Font = new Font("Arial Black", 20, FontStyle.Bold)
        };

        _btnPrev = new Button { Text = "◀ PREV", Width = 100, Height = 50 };
        _btnPrev.Click += async (s, e) => await ChangeNumber(-1);

        _btnNext = new Button { Text = "NEXT ▶", Width = 100, Height = 50 };
        _btnNext.Click += async (s, e) => await ChangeNumber(1);

        _btnSetNumber = new Button { Text = "Imposta", Width = 100, Height = 50 };
        _btnSetNumber.Click += async (s, e) => await SetNumber();

        numberLayout.Controls.Add(_numCurrentNumber);
        numberLayout.Controls.Add(_btnPrev);
        numberLayout.Controls.Add(_btnNext);
        numberLayout.Controls.Add(_btnSetNumber);
        grpNumber.Controls.Add(numberLayout);
        turnoLayout.Controls.Add(grpNumber);

        // === Sezione Tastiera ===
        var grpKeyboard = CreateGroupBox("Tastiera Touch");
        _keyboardPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            AutoSize = true,
            Padding = new Padding(10)
        };

        CreateKeyboard();
        grpKeyboard.Controls.Add(_keyboardPanel);
        turnoLayout.Controls.Add(grpKeyboard);

        tabTurno.Controls.Add(turnoLayout);
        tabControl.TabPages.Add(tabTurno);

        // === Tab Media ===
        var tabMedia = new TabPage("📁 Media")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var mediaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, AutoSize = true };

        // === Sezione Impostazioni Media ===
        var grpMediaSettings = CreateGroupBox("Impostazioni Media");
        var mediaSettingsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5, AutoSize = true };

        // Riga 0: Modalità
        _chkFolderMode = new CheckBox { Text = "Modalità Cartella (slideshow)", AutoSize = true, ForeColor = Color.White };
        _chkFolderMode.CheckedChanged += (s, e) => UpdateMediaControls();
        mediaSettingsLayout.SetColumnSpan(_chkFolderMode, 3);
        mediaSettingsLayout.Controls.Add(_chkFolderMode, 0, 0);

        // Riga 1: Percorso
        mediaSettingsLayout.Controls.Add(new Label { Text = "Percorso:", AutoSize = true }, 0, 1);
        _txtMediaPath = new TextBox { Width = 500 };
        mediaSettingsLayout.Controls.Add(_txtMediaPath, 1, 1);

        var btnBrowsePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var btnBrowseFile = new Button { Text = "📄", Width = 35, Height = 25 };
        btnBrowseFile.Click += BtnBrowseFile_Click;
        var btnBrowseFolder = new Button { Text = "📁", Width = 35, Height = 25 };
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        btnBrowsePanel.Controls.Add(btnBrowseFile);
        btnBrowsePanel.Controls.Add(btnBrowseFolder);
        mediaSettingsLayout.Controls.Add(btnBrowsePanel, 2, 1);

        // Riga 2: Tipo e Adattamento
        mediaSettingsLayout.Controls.Add(new Label { Text = "Tipo:", AutoSize = true }, 0, 2);
        _cmbMediaType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbMediaType.Items.AddRange(new[] { "image", "gif", "video" });
        mediaSettingsLayout.Controls.Add(_cmbMediaType, 1, 2);

        // Riga 3: Adattamento
        mediaSettingsLayout.Controls.Add(new Label { Text = "Adattamento:", AutoSize = true }, 0, 3);
        _cmbMediaFit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbMediaFit.Items.AddRange(new[] { "cover", "contain" });
        mediaSettingsLayout.Controls.Add(_cmbMediaFit, 1, 3);

        // Riga 4: Intervallo slideshow
        mediaSettingsLayout.Controls.Add(new Label { Text = "Intervallo slide (sec):", AutoSize = true }, 0, 4);
        _numSlideshowInterval = new NumericUpDown { Minimum = 1, Maximum = 60, Value = 5, Width = 70 };
        mediaSettingsLayout.Controls.Add(_numSlideshowInterval, 1, 4);

        grpMediaSettings.Controls.Add(mediaSettingsLayout);
        mediaLayout.Controls.Add(grpMediaSettings);

        // === Sezione Scheduler Media ===
        var grpMediaScheduler = CreateGroupBox("Scheduler Media");
        var schedulerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7, AutoSize = true };

        // Riga 0: Abilitato
        _chkSchedulerEnabled = new CheckBox { Text = "Abilita scheduler", AutoSize = true, ForeColor = Color.White };
        _chkSchedulerEnabled.CheckedChanged += (s, e) => UpdateSchedulerControls();
        schedulerLayout.SetColumnSpan(_chkSchedulerEnabled, 3);
        schedulerLayout.Controls.Add(_chkSchedulerEnabled, 0, 0);

        // Riga 1: Date inizio-fine
        schedulerLayout.Controls.Add(new Label { Text = "Da:", AutoSize = true }, 0, 1);
        _dtpSchedulerStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        schedulerLayout.Controls.Add(_dtpSchedulerStart, 1, 1);

        schedulerLayout.Controls.Add(new Label { Text = "A:", AutoSize = true }, 0, 2);
        _dtpSchedulerEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        schedulerLayout.Controls.Add(_dtpSchedulerEnd, 1, 2);

        // Riga 3: Percorso scheduler
        schedulerLayout.Controls.Add(new Label { Text = "Cartella media:", AutoSize = true }, 0, 3);
        _txtSchedulerPath = new TextBox { Width = 400 };
        schedulerLayout.Controls.Add(_txtSchedulerPath, 1, 3);

        var btnBrowseScheduler = new Button { Text = "📁", Width = 35, Height = 25 };
        btnBrowseScheduler.Click += BtnBrowseSchedulerFolder_Click;
        schedulerLayout.Controls.Add(btnBrowseScheduler, 2, 3);

        // Riga 4: Tipo scheduler
        schedulerLayout.Controls.Add(new Label { Text = "Tipo:", AutoSize = true }, 0, 4);
        _cmbSchedulerType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbSchedulerType.Items.AddRange(new[] { "image", "gif", "video" });
        schedulerLayout.Controls.Add(_cmbSchedulerType, 1, 4);

        // Riga 5: Adattamento scheduler
        schedulerLayout.Controls.Add(new Label { Text = "Adattamento:", AutoSize = true }, 0, 5);
        _cmbSchedulerFit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbSchedulerFit.Items.AddRange(new[] { "cover", "contain" });
        schedulerLayout.Controls.Add(_cmbSchedulerFit, 1, 5);

        // Riga 6: Modalità cartella e intervallo scheduler
        _chkSchedulerFolderMode = new CheckBox { Text = "Modalità cartella", AutoSize = true, ForeColor = Color.White };
        schedulerLayout.Controls.Add(_chkSchedulerFolderMode, 0, 6);

        schedulerLayout.Controls.Add(new Label { Text = "Intervallo (sec):", AutoSize = true }, 1, 6);
        _numSchedulerInterval = new NumericUpDown { Minimum = 1, Maximum = 60, Value = 5, Width = 70 };
        schedulerLayout.Controls.Add(_numSchedulerInterval, 2, 6);

        grpMediaScheduler.Controls.Add(schedulerLayout);
        mediaLayout.Controls.Add(grpMediaScheduler);

        tabMedia.Controls.Add(mediaLayout);
        tabControl.TabPages.Add(tabMedia);

        // === Pannello principale con TabControl e controlli inferiori ===
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };

        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Crea un pannello scorrevole per il TabControl
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 30, 40),
            Padding = new Padding(5)
        };

        // TabControl nel pannello scorrevole
        scrollPanel.Controls.Add(tabControl);
        mainContainer.Controls.Add(scrollPanel, 0, 0);

        // Pannello inferiore con bottoni e status
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(10)
        };

        // === Bottoni ===
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        _btnSave = new Button
        {
            Text = "💾 SALVA", Width = 150, Height = 55,
            BackColor = Color.FromArgb(50, 150, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        _btnSave.Click += BtnSave_Click;
        btnPanel.Controls.Add(_btnSave);

        _btnReload = new Button
        {
            Text = "🔄 Ricarica", Width = 120, Height = 55,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnReload.Click += async (s, e) => await LoadSettingsAsync();
        btnPanel.Controls.Add(_btnReload);

        bottomPanel.Controls.Add(btnPanel, 0, 0);

        // === Status ===
        _lblStatus = new Label
        {
            Text = "Pronto",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 25
        };
        bottomPanel.Controls.Add(_lblStatus, 0, 1);

        mainContainer.Controls.Add(bottomPanel, 0, 1);
        this.Controls.Add(mainContainer);
    }

    private GroupBox CreateGroupBox(string title)
    {
        return new GroupBox
        {
            Text = title,
            ForeColor = Color.FromArgb(255, 200, 50),
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 15)
        };
    }

    private void CreateKeyboard()
    {
        // Prima riga: 1 2 3
        for (int i = 1; i <= 3; i++)
        {
            var btn = new Button
            {
                Text = i.ToString(),
                Width = 80,
                Height = 80,
                Font = new Font("Arial Black", 20, FontStyle.Bold),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += KeyboardButton_Click;
            _keyboardPanel.Controls.Add(btn, i - 1, 0);
        }

        // Seconda riga: 4 5 6
        for (int i = 4; i <= 6; i++)
        {
            var btn = new Button
            {
                Text = i.ToString(),
                Width = 80,
                Height = 80,
                Font = new Font("Arial Black", 20, FontStyle.Bold),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += KeyboardButton_Click;
            _keyboardPanel.Controls.Add(btn, i - 4, 1);
        }

        // Terza riga: 7 8 9
        for (int i = 7; i <= 9; i++)
        {
            var btn = new Button
            {
                Text = i.ToString(),
                Width = 80,
                Height = 80,
                Font = new Font("Arial Black", 20, FontStyle.Bold),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += KeyboardButton_Click;
            _keyboardPanel.Controls.Add(btn, i - 7, 2);
        }

        // Quarta riga: CANC 0 OK
        var btnCancel = new Button
        {
            Text = "⌫",
            Width = 80,
            Height = 80,
            Font = new Font("Arial Black", 16, FontStyle.Bold),
            BackColor = Color.FromArgb(150, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => {
            if (_numCurrentNumber.Value > 0)
                _numCurrentNumber.Value = (int)(_numCurrentNumber.Value / 10);
        };
        _keyboardPanel.Controls.Add(btnCancel, 0, 3);

        var btnZero = new Button
        {
            Text = "0",
            Width = 80,
            Height = 80,
            Font = new Font("Arial Black", 20, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 60, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnZero.Click += KeyboardButton_Click;
        _keyboardPanel.Controls.Add(btnZero, 1, 3);

        var btnOk = new Button
        {
            Text = "✓",
            Width = 80,
            Height = 80,
            Font = new Font("Arial Black", 16, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 150, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.Click += async (s, e) => await SetNumber();
        _keyboardPanel.Controls.Add(btnOk, 2, 3);
    }

    // Metodi per gestire gli eventi - da implementare
    private async void OperatorSettingsForm_Load(object? sender, EventArgs e)
    {
        await LoadSettingsAsync();
        await LoadCurrentNumberAsync();
    }

    private void KeyboardButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Text, out int digit))
        {
            var currentValue = (int)_numCurrentNumber.Value;
            var newValue = currentValue * 10 + digit;
            if (newValue <= 99)
                _numCurrentNumber.Value = newValue;
        }
    }

    private void UpdateMediaControls()
    {
        _numSlideshowInterval.Enabled = _chkFolderMode.Checked;
    }

    private void UpdateSchedulerControls()
    {
        var enabled = _chkSchedulerEnabled.Checked;
        _dtpSchedulerStart.Enabled = enabled;
        _dtpSchedulerEnd.Enabled = enabled;
        _txtSchedulerPath.Enabled = enabled;
        _cmbSchedulerType.Enabled = enabled;
        _cmbSchedulerFit.Enabled = enabled;
        _chkSchedulerFolderMode.Enabled = enabled;
        _numSchedulerInterval.Enabled = enabled && _chkSchedulerFolderMode.Checked;
    }

    private async Task ChangeNumber(int delta)
    {
        // Implementazione simile a ConfigForm
        try
        {
            if (delta > 0)
                await Database.NextNumberAsync("operator");
            else
                await Database.PrevNumberAsync("operator");

            await LoadCurrentNumberAsync();
            SetStatus($"Numero aggiornato", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Errore: {ex.Message}", Color.Red);
        }
    }

    private async Task SetNumber()
    {
        // Implementazione simile a ConfigForm
        try
        {
            var num = (int)_numCurrentNumber.Value;
            await Database.SetNumberAsync(num, "operator", "set");
            SetStatus($"Numero impostato a {num:00}", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Errore: {ex.Message}", Color.Red);
        }
    }

    private async Task LoadSettingsAsync()
    {
        // Implementazione da completare
        try
        {
            _settings = await Database.GetSettingsAsync();

            // Carica impostazioni media
            _txtMediaPath.Text = _settings.MediaPath;
            _cmbMediaType.SelectedItem = _settings.MediaType;
            _cmbMediaFit.SelectedItem = _settings.MediaFit;
            _chkFolderMode.Checked = _settings.MediaFolderMode;
            _numSlideshowInterval.Value = Math.Max(1, _settings.SlideshowIntervalMs / 1000);

            // Carica impostazioni scheduler
            _chkSchedulerEnabled.Checked = _settings.MediaSchedulerEnabled;
            _dtpSchedulerStart.Value = _settings.MediaSchedulerStartDate;
            _dtpSchedulerEnd.Value = _settings.MediaSchedulerEndDate;
            _txtSchedulerPath.Text = _settings.MediaSchedulerPath;
            _cmbSchedulerType.SelectedItem = _settings.MediaSchedulerType;
            _cmbSchedulerFit.SelectedItem = _settings.MediaSchedulerFit;
            _chkSchedulerFolderMode.Checked = _settings.MediaSchedulerFolderMode;
            _numSchedulerInterval.Value = Math.Max(1, _settings.MediaSchedulerIntervalMs / 1000);

            UpdateMediaControls();
            UpdateSchedulerControls();
            SetStatus("Impostazioni caricate", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Errore: {ex.Message}", Color.Red);
        }
    }

    private async Task LoadCurrentNumberAsync()
    {
        try
        {
            var state = await Database.GetStateAsync();
            _numCurrentNumber.Value = state.CurrentNumber;
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore lettura numero: {ex.Message}");
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Salva impostazioni media
            _settings.MediaPath = _txtMediaPath.Text;
            _settings.MediaType = _cmbMediaType.SelectedItem?.ToString() ?? "image";
            _settings.MediaFit = _cmbMediaFit.SelectedItem?.ToString() ?? "cover";
            _settings.MediaFolderMode = _chkFolderMode.Checked;
            _settings.SlideshowIntervalMs = (int)_numSlideshowInterval.Value * 1000;

            // Salva impostazioni scheduler
            _settings.MediaSchedulerEnabled = _chkSchedulerEnabled.Checked;
            _settings.MediaSchedulerStartDate = _dtpSchedulerStart.Value.Date;
            _settings.MediaSchedulerEndDate = _dtpSchedulerEnd.Value.Date;
            _settings.MediaSchedulerPath = _txtSchedulerPath.Text;
            _settings.MediaSchedulerType = _cmbSchedulerType.SelectedItem?.ToString() ?? "image";
            _settings.MediaSchedulerFit = _cmbSchedulerFit.SelectedItem?.ToString() ?? "cover";
            _settings.MediaSchedulerFolderMode = _chkSchedulerFolderMode.Checked;
            _settings.MediaSchedulerIntervalMs = (int)_numSchedulerInterval.Value * 1000;

            await Database.SaveSettingsAsync(_settings);
            SetStatus("✅ Impostazioni salvate!", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Errore: {ex.Message}", Color.Red);
            Logger.Error($"Errore salvataggio: {ex.Message}");
        }
    }

    private void BtnBrowseFile_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Seleziona immagine o video",
            Filter = "Tutti i media|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.avi;*.wmv;*.webm|" +
                     "Immagini|*.jpg;*.jpeg;*.png;*.bmp|" +
                     "GIF|*.gif|" +
                     "Video|*.mp4;*.avi;*.wmv;*.webm|" +
                     "Tutti i file|*.*"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _txtMediaPath.Text = dlg.FileName;
            _chkFolderMode.Checked = false;
            AutoDetectMediaType(_txtMediaPath.Text);
        }
    }

    private void BtnBrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Seleziona cartella con immagini/video per slideshow"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _txtMediaPath.Text = dlg.SelectedPath;
            _chkFolderMode.Checked = true;
            _cmbMediaType.SelectedItem = "image";
        }
    }

    private void BtnBrowseSchedulerFolder_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Seleziona cartella per scheduler media"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _txtSchedulerPath.Text = dlg.SelectedPath;
        }
    }

    private void AutoDetectMediaType(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        if (ext == ".gif")
            _cmbMediaType.SelectedItem = "gif";
        else if (ext == ".mp4" || ext == ".avi" || ext == ".wmv" || ext == ".webm")
            _cmbMediaType.SelectedItem = "video";
        else
            _cmbMediaType.SelectedItem = "image";
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color;
    }
}
