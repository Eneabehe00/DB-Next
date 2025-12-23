using DBNext.Shared;

namespace DBNextConfig;

/// <summary>
/// Form di configurazione DB-Next
/// </summary>
public class ConfigForm : Form
{
    // Media
    private TextBox _txtMediaPath = null!;
    private ComboBox _cmbMediaType = null!;
    private ComboBox _cmbMediaFit = null!;
    private CheckBox _chkFolderMode = null!;
    private NumericUpDown _numSlideshowInterval = null!;
    
    // Layout
    private NumericUpDown _numLeftPct = null!;
    private NumericUpDown _numRightPct = null!;
    private TrackBar _trackLayout = null!;
    
    // Display
    private ComboBox _cmbScreenMode = null!;
    private ComboBox _cmbTargetDisplay = null!;
    private TextBox _txtMultiDisplayList = null!;
    private ComboBox _cmbWindowMode = null!;
    
    // Polling
    private NumericUpDown _numPollMs = null!;
    
    // Numero corrente
    private NumericUpDown _numCurrentNumber = null!;
    
    // Personalizzazione numero
    private ComboBox _cmbNumberFont = null!;
    private NumericUpDown _numNumberFontSize = null!;
    private CheckBox _chkNumberBold = null!;
    private Panel _pnlNumberColor = null!;
    private Panel _pnlNumberBgColor = null!;
    
    // Scritta sopra il numero
    private TextBox _txtNumberLabel = null!;
    private Panel _pnlLabelColor = null!;
    private NumericUpDown _numLabelSize = null!;
    private ComboBox _cmbLabelPosition = null!;
    private NumericUpDown _numLabelOffset = null!;
    
    // Status
    private Label _lblStatus = null!;
    
    private QueueSettings _settings = new();
    
    public ConfigForm()
    {
        InitializeComponent();
        this.Load += ConfigForm_Load;
    }
    
    private void InitializeComponent()
    {
        this.Text = "DB-Next Configurazione";
        this.Size = new Size(650, 900);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(620, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 40);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 9);
        
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        
        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Padding = new Padding(20),
            RowCount = 10,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        
        // === Sezione Media ===
        var grpMedia = CreateGroupBox("📁 Media / Slideshow");
        var mediaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5, AutoSize = true };
        
        // Riga 0: Modalità
        _chkFolderMode = new CheckBox { Text = "Modalità Cartella (slideshow)", AutoSize = true, ForeColor = Color.White };
        _chkFolderMode.CheckedChanged += (s, e) => UpdateMediaControls();
        mediaLayout.SetColumnSpan(_chkFolderMode, 3);
        mediaLayout.Controls.Add(_chkFolderMode, 0, 0);
        
        // Riga 1: Percorso
        mediaLayout.Controls.Add(new Label { Text = "Percorso:", AutoSize = true }, 0, 1);
        _txtMediaPath = new TextBox { Width = 350 };
        mediaLayout.Controls.Add(_txtMediaPath, 1, 1);
        
        var btnBrowsePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var btnBrowseFile = new Button { Text = "📄", Width = 35, Height = 25 };
        btnBrowseFile.Click += BtnBrowseFile_Click;
        var btnBrowseFolder = new Button { Text = "📁", Width = 35, Height = 25 };
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        btnBrowsePanel.Controls.Add(btnBrowseFile);
        btnBrowsePanel.Controls.Add(btnBrowseFolder);
        mediaLayout.Controls.Add(btnBrowsePanel, 2, 1);
        
        // Riga 2: Tipo e Adattamento
        mediaLayout.Controls.Add(new Label { Text = "Tipo:", AutoSize = true }, 0, 2);
        _cmbMediaType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbMediaType.Items.AddRange(new[] { "image", "gif", "video" });
        mediaLayout.Controls.Add(_cmbMediaType, 1, 2);
        
        // Riga 3: Adattamento
        mediaLayout.Controls.Add(new Label { Text = "Adattamento:", AutoSize = true }, 0, 3);
        _cmbMediaFit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbMediaFit.Items.AddRange(new[] { "cover", "contain" });
        mediaLayout.Controls.Add(_cmbMediaFit, 1, 3);
        
        // Riga 4: Intervallo slideshow
        mediaLayout.Controls.Add(new Label { Text = "Intervallo slide (sec):", AutoSize = true }, 0, 4);
        _numSlideshowInterval = new NumericUpDown { Minimum = 1, Maximum = 60, Value = 5, Width = 70 };
        mediaLayout.Controls.Add(_numSlideshowInterval, 1, 4);
        
        grpMedia.Controls.Add(mediaLayout);
        mainPanel.Controls.Add(grpMedia);
        
        // === Sezione Personalizzazione Numero ===
        var grpNumberStyle = CreateGroupBox("🎨 Stile Numero");
        var numberStyleLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, AutoSize = true };
        
        // Riga 0: Font
        numberStyleLayout.Controls.Add(new Label { Text = "Font:", AutoSize = true }, 0, 0);
        _cmbNumberFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        _cmbNumberFont.Items.AddRange(new[] { "Arial Black", "Arial", "Impact", "Verdana", "Tahoma", 
            "Segoe UI", "Consolas", "Courier New", "Times New Roman", "Georgia" });
        numberStyleLayout.Controls.Add(_cmbNumberFont, 1, 0);
        
        numberStyleLayout.Controls.Add(new Label { Text = "Size (0=auto):", AutoSize = true }, 2, 0);
        _numNumberFontSize = new NumericUpDown { Minimum = 0, Maximum = 500, Value = 0, Width = 70 };
        numberStyleLayout.Controls.Add(_numNumberFontSize, 3, 0);
        
        // Riga 1: Bold e Colori
        _chkNumberBold = new CheckBox { Text = "Grassetto", AutoSize = true, ForeColor = Color.White, Checked = true };
        numberStyleLayout.Controls.Add(_chkNumberBold, 0, 1);
        
        numberStyleLayout.Controls.Add(new Label { Text = "Colore Testo:", AutoSize = true }, 1, 1);
        _pnlNumberColor = new Panel 
        { 
            Width = 60, Height = 25, 
            BackColor = Color.FromArgb(255, 200, 50),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        _pnlNumberColor.Click += (s, e) => PickColor(_pnlNumberColor);
        numberStyleLayout.Controls.Add(_pnlNumberColor, 2, 1);
        
        // Riga 2: Sfondo
        numberStyleLayout.Controls.Add(new Label { Text = "Colore Sfondo:", AutoSize = true }, 1, 2);
        _pnlNumberBgColor = new Panel 
        { 
            Width = 60, Height = 25, 
            BackColor = Color.FromArgb(20, 20, 30),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        _pnlNumberBgColor.Click += (s, e) => PickColor(_pnlNumberBgColor);
        numberStyleLayout.Controls.Add(_pnlNumberBgColor, 2, 2);
        
        grpNumberStyle.Controls.Add(numberStyleLayout);
        mainPanel.Controls.Add(grpNumberStyle);
        
        // === Sezione Scritta sopra Numero ===
        var grpLabel = CreateGroupBox("📝 Scritta sul Numero");
        var labelLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 3, AutoSize = true };
        
        // Riga 0: Testo
        labelLayout.Controls.Add(new Label { Text = "Testo:", AutoSize = true }, 0, 0);
        _txtNumberLabel = new TextBox { Width = 400 };
        labelLayout.SetColumnSpan(_txtNumberLabel, 5);
        labelLayout.Controls.Add(_txtNumberLabel, 1, 0);
        
        // Riga 1: Colore e Dimensione
        labelLayout.Controls.Add(new Label { Text = "Colore:", AutoSize = true }, 0, 1);
        _pnlLabelColor = new Panel 
        { 
            Width = 60, Height = 25, 
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        _pnlLabelColor.Click += (s, e) => PickColor(_pnlLabelColor);
        labelLayout.Controls.Add(_pnlLabelColor, 1, 1);
        
        labelLayout.Controls.Add(new Label { Text = "Dimensione (0=auto):", AutoSize = true }, 2, 1);
        _numLabelSize = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 0, Width = 70 };
        labelLayout.Controls.Add(_numLabelSize, 3, 1);
        
        // Riga 2: Posizione e Offset
        labelLayout.Controls.Add(new Label { Text = "Posizione:", AutoSize = true }, 0, 2);
        _cmbLabelPosition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _cmbLabelPosition.Items.AddRange(new[] { "top", "bottom" });
        _cmbLabelPosition.SelectedIndex = 0;
        labelLayout.Controls.Add(_cmbLabelPosition, 1, 2);
        
        labelLayout.Controls.Add(new Label { Text = "Offset Y (px):", AutoSize = true }, 2, 2);
        _numLabelOffset = new NumericUpDown { Minimum = 0, Maximum = 500, Value = 20, Width = 70 };
        labelLayout.Controls.Add(_numLabelOffset, 3, 2);
        
        labelLayout.Controls.Add(new Label { Text = "(distanza dal bordo)", AutoSize = true, ForeColor = Color.Gray }, 4, 2);
        
        grpLabel.Controls.Add(labelLayout);
        mainPanel.Controls.Add(grpLabel);
        
        // === Sezione Layout ===
        var grpLayout = CreateGroupBox("📐 Layout (Sinistra/Destra)");
        var layoutPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, AutoSize = true };
        
        layoutPanel.Controls.Add(new Label { Text = "Sinistra %:", AutoSize = true }, 0, 0);
        _numLeftPct = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 75, Width = 70 };
        _numLeftPct.ValueChanged += NumPct_ValueChanged;
        layoutPanel.Controls.Add(_numLeftPct, 1, 0);
        
        layoutPanel.Controls.Add(new Label { Text = "Destra %:", AutoSize = true }, 2, 0);
        _numRightPct = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 25, Width = 70 };
        _numRightPct.ValueChanged += NumPct_ValueChanged;
        layoutPanel.Controls.Add(_numRightPct, 3, 0);
        
        _trackLayout = new TrackBar { Minimum = 0, Maximum = 100, Value = 75, Width = 400, TickFrequency = 10 };
        _trackLayout.ValueChanged += TrackLayout_ValueChanged;
        layoutPanel.SetColumnSpan(_trackLayout, 4);
        layoutPanel.Controls.Add(_trackLayout, 0, 1);
        
        grpLayout.Controls.Add(layoutPanel);
        mainPanel.Controls.Add(grpLayout);
        
        // === Sezione Display ===
        var grpDisplay = CreateGroupBox("🖥️ Display");
        var displayLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, AutoSize = true };
        
        displayLayout.Controls.Add(new Label { Text = "Modalità schermo:", AutoSize = true }, 0, 0);
        _cmbScreenMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        _cmbScreenMode.Items.AddRange(new[] { "single", "mirror", "multi" });
        _cmbScreenMode.SelectedIndexChanged += CmbScreenMode_Changed;
        displayLayout.Controls.Add(_cmbScreenMode, 1, 0);
        
        displayLayout.Controls.Add(new Label { Text = "Monitor target:", AutoSize = true }, 0, 1);
        _cmbTargetDisplay = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        PopulateMonitors();
        displayLayout.Controls.Add(_cmbTargetDisplay, 1, 1);
        
        displayLayout.Controls.Add(new Label { Text = "Lista multi (es. 0,2):", AutoSize = true }, 0, 2);
        _txtMultiDisplayList = new TextBox { Width = 200 };
        displayLayout.Controls.Add(_txtMultiDisplayList, 1, 2);
        
        displayLayout.Controls.Add(new Label { Text = "Modalità finestra:", AutoSize = true }, 0, 3);
        _cmbWindowMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        _cmbWindowMode.Items.AddRange(new[] { "borderless", "fullscreen", "windowed" });
        displayLayout.Controls.Add(_cmbWindowMode, 1, 3);
        
        grpDisplay.Controls.Add(displayLayout);
        mainPanel.Controls.Add(grpDisplay);
        
        // === Sezione Polling ===
        var grpPolling = CreateGroupBox("⏱️ Polling Database");
        var pollingLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        pollingLayout.Controls.Add(new Label { Text = "Intervallo (ms):", AutoSize = true });
        _numPollMs = new NumericUpDown { Minimum = 100, Maximum = 10000, Value = 1000, Width = 100, Increment = 100 };
        pollingLayout.Controls.Add(_numPollMs);
        grpPolling.Controls.Add(pollingLayout);
        mainPanel.Controls.Add(grpPolling);
        
        // === Sezione Numero Corrente ===
        var grpNumber = CreateGroupBox("🔢 Numero Corrente");
        var numberLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        
        _numCurrentNumber = new NumericUpDown 
        { 
            Minimum = 0, Maximum = 99, Value = 0, Width = 80,
            Font = new Font("Arial Black", 16, FontStyle.Bold)
        };
        numberLayout.Controls.Add(_numCurrentNumber);
        
        var btnPrev = new Button { Text = "◀ PREV", Width = 80, Height = 35 };
        btnPrev.Click += async (s, e) => await ChangeNumber(-1);
        numberLayout.Controls.Add(btnPrev);
        
        var btnNext = new Button { Text = "NEXT ▶", Width = 80, Height = 35 };
        btnNext.Click += async (s, e) => await ChangeNumber(1);
        numberLayout.Controls.Add(btnNext);
        
        var btnSetNumber = new Button { Text = "Imposta", Width = 80, Height = 35 };
        btnSetNumber.Click += async (s, e) => await SetNumber();
        numberLayout.Controls.Add(btnSetNumber);
        
        grpNumber.Controls.Add(numberLayout);
        mainPanel.Controls.Add(grpNumber);
        
        // === Bottoni ===
        var btnPanel = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 15, 0, 0)
        };
        
        var btnSave = new Button 
        { 
            Text = "💾 SALVA", Width = 130, Height = 45,
            BackColor = Color.FromArgb(50, 150, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnSave.Click += BtnSave_Click;
        btnPanel.Controls.Add(btnSave);
        
        var btnPreview = new Button 
        { 
            Text = "👁️ Preview", Width = 100, Height = 45,
            BackColor = Color.FromArgb(60, 100, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnPreview.Click += BtnPreview_Click;
        btnPanel.Controls.Add(btnPreview);
        
        var btnReload = new Button 
        { 
            Text = "🔄 Ricarica", Width = 100, Height = 45,
            FlatStyle = FlatStyle.Flat
        };
        btnReload.Click += async (s, e) => await LoadSettingsAsync();
        btnPanel.Controls.Add(btnReload);
        
        mainPanel.Controls.Add(btnPanel);
        
        // === Status ===
        _lblStatus = new Label 
        { 
            Text = "Pronto", 
            Dock = DockStyle.Fill, 
            ForeColor = Color.Gray,
            Padding = new Padding(0, 10, 0, 0)
        };
        mainPanel.Controls.Add(_lblStatus);
        
        scrollPanel.Controls.Add(mainPanel);
        this.Controls.Add(scrollPanel);
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
            Margin = new Padding(0, 0, 0, 10)
        };
    }
    
    private void PopulateMonitors()
    {
        _cmbTargetDisplay.Items.Clear();
        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            var screen = Screen.AllScreens[i];
            var primary = screen.Primary ? " (Primario)" : "";
            _cmbTargetDisplay.Items.Add($"{i}: {screen.Bounds.Width}x{screen.Bounds.Height}{primary}");
        }
        if (_cmbTargetDisplay.Items.Count > 0)
            _cmbTargetDisplay.SelectedIndex = 0;
    }
    
    private void UpdateMediaControls()
    {
        _numSlideshowInterval.Enabled = _chkFolderMode.Checked;
    }
    
    private void PickColor(Panel panel)
    {
        using var dlg = new ColorDialog { Color = panel.BackColor };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            panel.BackColor = dlg.Color;
        }
    }
    
    private async void ConfigForm_Load(object? sender, EventArgs e)
    {
        await LoadSettingsAsync();
        await LoadCurrentNumberAsync();
    }
    
    private async Task LoadSettingsAsync()
    {
        try
        {
            _settings = await Database.GetSettingsAsync();
            
            // Media
            _txtMediaPath.Text = _settings.MediaPath;
            _cmbMediaType.SelectedItem = _settings.MediaType;
            _cmbMediaFit.SelectedItem = _settings.MediaFit;
            _chkFolderMode.Checked = _settings.MediaFolderMode;
            _numSlideshowInterval.Value = Math.Max(1, _settings.SlideshowIntervalMs / 1000);
            
            // Layout
            _numLeftPct.Value = _settings.LayoutLeftPct;
            _numRightPct.Value = _settings.LayoutRightPct;
            _trackLayout.Value = _settings.LayoutLeftPct;
            
            // Display
            _cmbScreenMode.SelectedItem = _settings.ScreenMode;
            if (_settings.TargetDisplayIndex < _cmbTargetDisplay.Items.Count)
                _cmbTargetDisplay.SelectedIndex = _settings.TargetDisplayIndex;
            _txtMultiDisplayList.Text = _settings.MultiDisplayList;
            _cmbWindowMode.SelectedItem = _settings.WindowMode;
            
            // Polling
            _numPollMs.Value = _settings.PollMs;
            
            // Stile Numero
            _cmbNumberFont.SelectedItem = _settings.NumberFontFamily;
            if (_cmbNumberFont.SelectedIndex < 0) _cmbNumberFont.SelectedIndex = 0;
            _numNumberFontSize.Value = _settings.NumberFontSize;
            _chkNumberBold.Checked = _settings.NumberFontBold;
            _pnlNumberColor.BackColor = ColorFromHex(_settings.NumberColor);
            _pnlNumberBgColor.BackColor = ColorFromHex(_settings.NumberBgColor);
            
            // Scritta
            _txtNumberLabel.Text = _settings.NumberLabelText ?? "";
            _pnlLabelColor.BackColor = ColorFromHex(_settings.NumberLabelColor);
            _numLabelSize.Value = Math.Max(0, Math.Min(100, _settings.NumberLabelSize));
            _cmbLabelPosition.SelectedItem = _settings.NumberLabelPosition ?? "top";
            if (_cmbLabelPosition.SelectedIndex < 0) _cmbLabelPosition.SelectedIndex = 0;
            _numLabelOffset.Value = Math.Max(0, Math.Min(500, _settings.NumberLabelOffset));
            
            UpdateDisplayControls();
            UpdateMediaControls();
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
            AutoDetectMediaType(dlg.FileName);
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
            _cmbMediaType.SelectedItem = "image"; // Default per slideshow
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
    
    private void NumPct_ValueChanged(object? sender, EventArgs e)
    {
        if (sender == _numLeftPct)
        {
            _numRightPct.Value = 100 - _numLeftPct.Value;
            _trackLayout.Value = (int)_numLeftPct.Value;
        }
        else if (sender == _numRightPct)
        {
            _numLeftPct.Value = 100 - _numRightPct.Value;
            _trackLayout.Value = (int)_numLeftPct.Value;
        }
    }
    
    private void TrackLayout_ValueChanged(object? sender, EventArgs e)
    {
        _numLeftPct.ValueChanged -= NumPct_ValueChanged;
        _numRightPct.ValueChanged -= NumPct_ValueChanged;
        
        _numLeftPct.Value = _trackLayout.Value;
        _numRightPct.Value = 100 - _trackLayout.Value;
        
        _numLeftPct.ValueChanged += NumPct_ValueChanged;
        _numRightPct.ValueChanged += NumPct_ValueChanged;
    }
    
    private void CmbScreenMode_Changed(object? sender, EventArgs e)
    {
        UpdateDisplayControls();
    }
    
    private void UpdateDisplayControls()
    {
        var mode = _cmbScreenMode.SelectedItem?.ToString() ?? "single";
        _cmbTargetDisplay.Enabled = mode == "single";
        _txtMultiDisplayList.Enabled = mode == "multi";
    }
    
    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Media
            _settings.MediaPath = _txtMediaPath.Text;
            _settings.MediaType = _cmbMediaType.SelectedItem?.ToString() ?? "image";
            _settings.MediaFit = _cmbMediaFit.SelectedItem?.ToString() ?? "cover";
            _settings.MediaFolderMode = _chkFolderMode.Checked;
            _settings.SlideshowIntervalMs = (int)_numSlideshowInterval.Value * 1000;
            
            // Layout
            _settings.LayoutLeftPct = (int)_numLeftPct.Value;
            _settings.LayoutRightPct = (int)_numRightPct.Value;
            
            // Display
            _settings.ScreenMode = _cmbScreenMode.SelectedItem?.ToString() ?? "single";
            _settings.TargetDisplayIndex = _cmbTargetDisplay.SelectedIndex;
            _settings.MultiDisplayList = _txtMultiDisplayList.Text;
            _settings.WindowMode = _cmbWindowMode.SelectedItem?.ToString() ?? "borderless";
            
            // Polling
            _settings.PollMs = (int)_numPollMs.Value;
            
            // Stile Numero
            _settings.NumberFontFamily = _cmbNumberFont.SelectedItem?.ToString() ?? "Arial Black";
            _settings.NumberFontSize = (int)_numNumberFontSize.Value;
            _settings.NumberFontBold = _chkNumberBold.Checked;
            _settings.NumberColor = ColorToHex(_pnlNumberColor.BackColor);
            _settings.NumberBgColor = ColorToHex(_pnlNumberBgColor.BackColor);
            
            // Scritta
            _settings.NumberLabelText = _txtNumberLabel.Text;
            _settings.NumberLabelColor = ColorToHex(_pnlLabelColor.BackColor);
            _settings.NumberLabelSize = (int)_numLabelSize.Value;
            _settings.NumberLabelPosition = _cmbLabelPosition.SelectedItem?.ToString() ?? "top";
            _settings.NumberLabelOffset = (int)_numLabelOffset.Value;
            
            await Database.SaveSettingsAsync(_settings);
            SetStatus("✅ Impostazioni salvate!", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Errore: {ex.Message}", Color.Red);
            Logger.Error($"Errore salvataggio: {ex.Message}");
        }
    }
    
    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        try
        {
            var mode = _cmbScreenMode.SelectedItem?.ToString() ?? "single";
            var previewForms = new List<Form>();
            
            switch (mode)
            {
                case "mirror":
                    foreach (var screen in Screen.AllScreens)
                        previewForms.Add(CreatePreviewForm(screen));
                    break;
                    
                case "multi":
                    var indices = _txtMultiDisplayList.Text.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                        .Where(n => n >= 0 && n < Screen.AllScreens.Length);
                    foreach (var idx in indices)
                        previewForms.Add(CreatePreviewForm(Screen.AllScreens[idx]));
                    break;
                    
                default:
                    var targetIdx = _cmbTargetDisplay.SelectedIndex;
                    if (targetIdx >= 0 && targetIdx < Screen.AllScreens.Length)
                        previewForms.Add(CreatePreviewForm(Screen.AllScreens[targetIdx]));
                    break;
            }
            
            foreach (var form in previewForms)
                form.Show();
            
            var timer = new System.Windows.Forms.Timer { Interval = 3000 };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                foreach (var form in previewForms)
                {
                    form.Close();
                    form.Dispose();
                }
            };
            timer.Start();
            
            SetStatus("Preview attiva per 3 secondi...", Color.Yellow);
        }
        catch (Exception ex)
        {
            SetStatus($"Errore preview: {ex.Message}", Color.Red);
        }
    }
    
    private Form CreatePreviewForm(Screen screen)
    {
        var form = new Form
        {
            Text = "DB-Next Preview",
            FormBorderStyle = FormBorderStyle.None,
            BackColor = _pnlNumberBgColor.BackColor,
            TopMost = true
        };
        
        var windowMode = _cmbWindowMode.SelectedItem?.ToString() ?? "borderless";
        if (windowMode == "windowed")
        {
            int height = (int)(screen.Bounds.Height * 0.6);
            int width = (int)(height * 4.0 / 3.0);
            form.Size = new Size(width, height);
            form.Location = new Point(
                screen.Bounds.X + (screen.Bounds.Width - width) / 2,
                screen.Bounds.Y + (screen.Bounds.Height - height) / 2);
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
        }
        else
        {
            form.Bounds = screen.Bounds;
        }
        
        var leftPct = (int)_numLeftPct.Value;
        var leftWidth = (int)(form.ClientSize.Width * leftPct / 100.0);
        
        var leftPanel = new Panel
        {
            BackColor = Color.FromArgb(40, 40, 50),
            Location = new Point(0, 0),
            Size = new Size(leftWidth, form.ClientSize.Height)
        };
        
        var rightPanel = new Panel
        {
            BackColor = _pnlNumberBgColor.BackColor,
            Location = new Point(leftWidth, 0),
            Size = new Size(form.ClientSize.Width - leftWidth, form.ClientSize.Height)
        };
        
        var mediaLabel = new Label
        {
            Text = "📺 MEDIA\n" + (string.IsNullOrEmpty(_txtMediaPath.Text) ? "(nessun file)" : 
                   (_chkFolderMode.Checked ? "[CARTELLA]" : Path.GetFileName(_txtMediaPath.Text))),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        leftPanel.Controls.Add(mediaLabel);
        
        var fontSize = _numNumberFontSize.Value > 0 ? (float)_numNumberFontSize.Value : rightPanel.Height * 0.4f;
        var fontStyle = _chkNumberBold.Checked ? FontStyle.Bold : FontStyle.Regular;
        var fontFamily = _cmbNumberFont.SelectedItem?.ToString() ?? "Arial Black";
        
        var numberLabel = new Label
        {
            Text = _numCurrentNumber.Value.ToString("00"),
            ForeColor = _pnlNumberColor.BackColor,
            BackColor = _pnlNumberBgColor.BackColor,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        
        try { numberLabel.Font = new Font(fontFamily, fontSize, fontStyle); }
        catch { numberLabel.Font = new Font("Arial", fontSize, fontStyle); }
        
        rightPanel.Controls.Add(numberLabel);
        
        form.Controls.Add(leftPanel);
        form.Controls.Add(rightPanel);
        
        form.KeyPreview = true;
        form.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
        
        return form;
    }
    
    private async Task ChangeNumber(int delta)
    {
        try
        {
            if (delta > 0)
                await Database.NextNumberAsync("config");
            else
                await Database.PrevNumberAsync("config");
            
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
        try
        {
            var num = (int)_numCurrentNumber.Value;
            await Database.SetNumberAsync(num, "config", "set");
            SetStatus($"Numero impostato a {num:00}", Color.LightGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Errore: {ex.Message}", Color.Red);
        }
    }
    
    private void SetStatus(string message, Color color)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color;
    }
    
    private static Color ColorFromHex(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }
        catch
        {
            return Color.FromArgb(255, 200, 50);
        }
    }
    
    private static string ColorToHex(Color c)
    {
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
