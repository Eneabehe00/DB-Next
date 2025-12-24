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
    private NumericUpDown _numWindowMarginTop = null!;
    
    // Polling
    private NumericUpDown _numPollMs = null!;
    
    // Numero corrente
    private NumericUpDown _numCurrentNumber = null!;

    // Database Connection
    private TextBox _txtDbServer = null!;
    private NumericUpDown _numDbPort = null!;
    private TextBox _txtDbName = null!;
    private TextBox _txtDbUser = null!;
    private TextBox _txtDbPassword = null!;
    private Button _btnTestConnection = null!;

    // Finestra Operatore
    private CheckBox _chkOperatorEnabled = null!;
    private NumericUpDown _numOperatorX = null!;
    private NumericUpDown _numOperatorY = null!;
    private NumericUpDown _numOperatorWidth = null!;
    private NumericUpDown _numOperatorHeight = null!;
    private ComboBox _cmbOperatorMonitor = null!;
    private Panel _pnlOperatorBgColor = null!;
    private Panel _pnlOperatorTextColor = null!;
    private ComboBox _cmbOperatorFont = null!;
    private NumericUpDown _numOperatorFontSize = null!;
    private CheckBox _chkOperatorAlwaysOnTop = null!;
    private TextBox _txtOperatorLabelText = null!;
    
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
        this.Size = new Size(800, 700);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
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
        
        // === Tab Media ===
        var tabMedia = new TabPage("📁 Media")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

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

        tabMedia.Controls.Add(mediaLayout);
        tabControl.TabPages.Add(tabMedia);
        
        // === Tab Aspetto ===
        var tabAspetto = new TabPage("🎨 Aspetto")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var aspettoLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, AutoSize = true };

        // === Sezione Personalizzazione Numero ===
        var grpNumberStyle = CreateGroupBox("Stile Numero");
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
        aspettoLayout.Controls.Add(grpNumberStyle);

        // === Sezione Scritta sopra Numero ===
        var grpLabel = CreateGroupBox("Scritta sul Numero");
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
        aspettoLayout.Controls.Add(grpLabel);

        tabAspetto.Controls.Add(aspettoLayout);
        tabControl.TabPages.Add(tabAspetto);
        
        // === Tab Layout ===
        var tabLayout = new TabPage("📐 Layout")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var layoutTabLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, AutoSize = true };

        // === Sezione Layout ===
        var grpLayout = CreateGroupBox("Layout (Sinistra/Destra)");
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
        layoutTabLayout.Controls.Add(grpLayout);

        // === Sezione Display ===
        var grpDisplay = CreateGroupBox("Display");
        var displayLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, AutoSize = true };

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

        displayLayout.Controls.Add(new Label { Text = "Margine superiore (px):", AutoSize = true }, 0, 4);
        _numWindowMarginTop = new NumericUpDown { Minimum = 0, Maximum = 500, Value = 0, Width = 100 };
        displayLayout.Controls.Add(_numWindowMarginTop, 1, 4);

        grpDisplay.Controls.Add(displayLayout);
        layoutTabLayout.Controls.Add(grpDisplay);

        tabLayout.Controls.Add(layoutTabLayout);
        tabControl.TabPages.Add(tabLayout);
        
        // === Tab Sistema ===
        var tabSistema = new TabPage("⚙️ Sistema")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var sistemaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, AutoSize = true };

        // === Sezione Polling ===
        var grpPolling = CreateGroupBox("Polling Database");
        var pollingLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        pollingLayout.Controls.Add(new Label { Text = "Intervallo (ms):", AutoSize = true });
        _numPollMs = new NumericUpDown { Minimum = 100, Maximum = 10000, Value = 1000, Width = 100, Increment = 100 };
        pollingLayout.Controls.Add(_numPollMs);
        grpPolling.Controls.Add(pollingLayout);
        sistemaLayout.Controls.Add(grpPolling);

        // === Sezione Numero Corrente ===
        var grpNumber = CreateGroupBox("Numero Corrente");
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
        sistemaLayout.Controls.Add(grpNumber);

        // === Sezione Connessione Database ===
        var grpDatabase = CreateGroupBox("Connessione Database");
        var databaseLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6, AutoSize = true };

        // Riga 0: Server
        databaseLayout.Controls.Add(new Label { Text = "Server:", AutoSize = true }, 0, 0);
        _txtDbServer = new TextBox { Width = 150, Text = "localhost" };
        databaseLayout.Controls.Add(_txtDbServer, 1, 0);

        databaseLayout.Controls.Add(new Label { Text = "Porta:", AutoSize = true }, 2, 0);
        _numDbPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 3306, Width = 70 };
        databaseLayout.Controls.Add(_numDbPort, 3, 0);

        // Riga 1: Database
        databaseLayout.Controls.Add(new Label { Text = "Database:", AutoSize = true }, 0, 1);
        _txtDbName = new TextBox { Width = 150, Text = "db_next" };
        databaseLayout.SetColumnSpan(_txtDbName, 3);
        databaseLayout.Controls.Add(_txtDbName, 1, 1);

        // Riga 2: User
        databaseLayout.Controls.Add(new Label { Text = "Utente:", AutoSize = true }, 0, 2);
        _txtDbUser = new TextBox { Width = 150, Text = "root" };
        databaseLayout.SetColumnSpan(_txtDbUser, 3);
        databaseLayout.Controls.Add(_txtDbUser, 1, 2);

        // Riga 3: Password
        databaseLayout.Controls.Add(new Label { Text = "Password:", AutoSize = true }, 0, 3);
        _txtDbPassword = new TextBox { Width = 150, PasswordChar = '•' };
        databaseLayout.SetColumnSpan(_txtDbPassword, 3);
        databaseLayout.Controls.Add(_txtDbPassword, 1, 3);

        // Riga 4: Pulsante test connessione
        _btnTestConnection = new Button
        {
            Text = "🔗 Test Connessione",
            Width = 150,
            Height = 35,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnTestConnection.Click += BtnTestConnection_Click;
        databaseLayout.SetColumnSpan(_btnTestConnection, 4);
        databaseLayout.Controls.Add(_btnTestConnection, 0, 4);

        grpDatabase.Controls.Add(databaseLayout);
        sistemaLayout.Controls.Add(grpDatabase);

        tabSistema.Controls.Add(sistemaLayout);
        tabControl.TabPages.Add(tabSistema);

        // === Tab Operatore ===
        var tabOperatore = new TabPage("👤 Operatore")
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var operatoreLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, AutoSize = true };

        // === Sezione Abilitazione ===
        var grpOperatorEnable = CreateGroupBox("Finestra Operatore");
        var enableLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _chkOperatorEnabled = new CheckBox
        {
            Text = "Abilita finestra operatore (piccola finestra con numero corrente)",
            AutoSize = true,
            ForeColor = Color.White,
            Checked = false
        };
        enableLayout.Controls.Add(_chkOperatorEnabled);
        grpOperatorEnable.Controls.Add(enableLayout);
        operatoreLayout.Controls.Add(grpOperatorEnable);

        // === Sezione Posizione e Dimensioni ===
        var grpOperatorPosition = CreateGroupBox("Posizione e Dimensioni");
        var positionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, AutoSize = true };

        // Riga 0: Posizione X Y
        positionLayout.Controls.Add(new Label { Text = "Posizione X:", AutoSize = true }, 0, 0);
        _numOperatorX = new NumericUpDown { Minimum = 0, Maximum = 4000, Value = 50, Width = 70 };
        positionLayout.Controls.Add(_numOperatorX, 1, 0);

        positionLayout.Controls.Add(new Label { Text = "Y:", AutoSize = true }, 2, 0);
        _numOperatorY = new NumericUpDown { Minimum = 0, Maximum = 4000, Value = 50, Width = 70 };
        positionLayout.Controls.Add(_numOperatorY, 3, 0);

        // Riga 1: Dimensioni
        positionLayout.Controls.Add(new Label { Text = "Larghezza:", AutoSize = true }, 0, 1);
        _numOperatorWidth = new NumericUpDown { Minimum = 100, Maximum = 1000, Value = 200, Width = 70 };
        positionLayout.Controls.Add(_numOperatorWidth, 1, 1);

        positionLayout.Controls.Add(new Label { Text = "Altezza:", AutoSize = true }, 2, 1);
        _numOperatorHeight = new NumericUpDown { Minimum = 50, Maximum = 500, Value = 80, Width = 70 };
        positionLayout.Controls.Add(_numOperatorHeight, 3, 1);

        // Riga 2: Monitor e sempre in primo piano
        positionLayout.Controls.Add(new Label { Text = "Monitor:", AutoSize = true }, 0, 2);
        _cmbOperatorMonitor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        PopulateOperatorMonitors();
        positionLayout.Controls.Add(_cmbOperatorMonitor, 1, 2);

        _chkOperatorAlwaysOnTop = new CheckBox { Text = "Sempre in primo piano", AutoSize = true, ForeColor = Color.White, Checked = true };
        positionLayout.Controls.Add(_chkOperatorAlwaysOnTop, 2, 2);
        positionLayout.SetColumnSpan(_chkOperatorAlwaysOnTop, 2);

        grpOperatorPosition.Controls.Add(positionLayout);
        operatoreLayout.Controls.Add(grpOperatorPosition);

        // === Sezione Aspetto ===
        var grpOperatorStyle = CreateGroupBox("Aspetto");
        var styleLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, AutoSize = true };

        // Riga 0: Colori
        styleLayout.Controls.Add(new Label { Text = "Sfondo:", AutoSize = true }, 0, 0);
        _pnlOperatorBgColor = new Panel
        {
            Width = 60, Height = 25,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        _pnlOperatorBgColor.Click += (s, e) => PickColor(_pnlOperatorBgColor);
        styleLayout.Controls.Add(_pnlOperatorBgColor, 1, 0);

        styleLayout.Controls.Add(new Label { Text = "Testo:", AutoSize = true }, 2, 0);
        _pnlOperatorTextColor = new Panel
        {
            Width = 60, Height = 25,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        _pnlOperatorTextColor.Click += (s, e) => PickColor(_pnlOperatorTextColor);
        styleLayout.Controls.Add(_pnlOperatorTextColor, 3, 0);

        // Riga 1: Font
        styleLayout.Controls.Add(new Label { Text = "Font:", AutoSize = true }, 0, 1);
        _cmbOperatorFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        _cmbOperatorFont.Items.AddRange(new[] { "Arial Black", "Arial", "Impact", "Verdana", "Tahoma",
            "Segoe UI", "Consolas", "Courier New", "Times New Roman", "Georgia" });
        styleLayout.Controls.Add(_cmbOperatorFont, 1, 1);

        styleLayout.Controls.Add(new Label { Text = "Dimensione:", AutoSize = true }, 2, 1);
        _numOperatorFontSize = new NumericUpDown { Minimum = 12, Maximum = 100, Value = 36, Width = 70 };
        styleLayout.Controls.Add(_numOperatorFontSize, 3, 1);

        // Riga 2: Etichetta
        styleLayout.Controls.Add(new Label { Text = "Testo etichetta:", AutoSize = true }, 0, 2);
        _txtOperatorLabelText = new TextBox { Width = 100, Text = "TURNO" };
        styleLayout.Controls.Add(_txtOperatorLabelText, 1, 2);

        grpOperatorStyle.Controls.Add(styleLayout);
        operatoreLayout.Controls.Add(grpOperatorStyle);

        tabOperatore.Controls.Add(operatoreLayout);
        tabControl.TabPages.Add(tabOperatore);

        // === Pannello principale con TabControl e controlli inferiori ===
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };

        // Imposta che la prima riga (TabControl) prenda tutto lo spazio disponibile
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // TabControl nella prima riga
        mainContainer.Controls.Add(tabControl, 0, 0);

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

    private void PopulateOperatorMonitors()
    {
        _cmbOperatorMonitor.Items.Clear();
        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            var screen = Screen.AllScreens[i];
            var primary = screen.Primary ? " (Primario)" : "";
            _cmbOperatorMonitor.Items.Add($"{i}: {screen.Bounds.Width}x{screen.Bounds.Height}{primary}");
        }
        if (_cmbOperatorMonitor.Items.Count > 0)
            _cmbOperatorMonitor.SelectedIndex = 0;
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
            // Carica impostazioni database
            _txtDbServer.Text = Config.Server;
            _numDbPort.Value = Config.Port;
            _txtDbName.Text = Config.Database;
            _txtDbUser.Text = Config.User;
            _txtDbPassword.Text = Config.Password;

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
            _numWindowMarginTop.Value = Math.Max(0, Math.Min(500, _settings.WindowMarginTop));
            
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

            // Finestra Operatore
            _chkOperatorEnabled.Checked = _settings.OperatorWindowEnabled;
            _numOperatorX.Value = Math.Max(0, Math.Min(4000, _settings.OperatorWindowX));
            _numOperatorY.Value = Math.Max(0, Math.Min(4000, _settings.OperatorWindowY));
            _numOperatorWidth.Value = Math.Max(100, Math.Min(1000, _settings.OperatorWindowWidth));
            _numOperatorHeight.Value = Math.Max(50, Math.Min(500, _settings.OperatorWindowHeight));
            if (_settings.OperatorMonitorIndex < _cmbOperatorMonitor.Items.Count)
                _cmbOperatorMonitor.SelectedIndex = _settings.OperatorMonitorIndex;
            _pnlOperatorBgColor.BackColor = ColorFromHex(_settings.OperatorBgColor);
            _pnlOperatorTextColor.BackColor = ColorFromHex(_settings.OperatorTextColor);
            _cmbOperatorFont.SelectedItem = _settings.OperatorFontFamily;
            if (_cmbOperatorFont.SelectedIndex < 0) _cmbOperatorFont.SelectedIndex = 0;
            _numOperatorFontSize.Value = Math.Max(12, Math.Min(100, _settings.OperatorFontSize));
            _chkOperatorAlwaysOnTop.Checked = _settings.OperatorAlwaysOnTop;
            _txtOperatorLabelText.Text = _settings.OperatorLabelText ?? "TURNO";
            
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
            _settings.WindowMarginTop = (int)_numWindowMarginTop.Value;
            
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

            // Finestra Operatore
            _settings.OperatorWindowEnabled = _chkOperatorEnabled.Checked;
            _settings.OperatorWindowX = (int)_numOperatorX.Value;
            _settings.OperatorWindowY = (int)_numOperatorY.Value;
            _settings.OperatorWindowWidth = (int)_numOperatorWidth.Value;
            _settings.OperatorWindowHeight = (int)_numOperatorHeight.Value;
            _settings.OperatorMonitorIndex = _cmbOperatorMonitor.SelectedIndex;
            _settings.OperatorBgColor = ColorToHex(_pnlOperatorBgColor.BackColor);
            _settings.OperatorTextColor = ColorToHex(_pnlOperatorTextColor.BackColor);
            _settings.OperatorFontFamily = _cmbOperatorFont.SelectedItem?.ToString() ?? "Arial Black";
            _settings.OperatorFontSize = (int)_numOperatorFontSize.Value;
            _settings.OperatorAlwaysOnTop = _chkOperatorAlwaysOnTop.Checked;
            _settings.OperatorLabelText = _txtOperatorLabelText.Text;
            
            await Database.SaveSettingsAsync(_settings);

            // Salva configurazione database
            Config.SetConnectionParameters(_txtDbServer.Text, (int)_numDbPort.Value, _txtDbName.Text, _txtDbUser.Text, _txtDbPassword.Text);
            var exePath = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            if (Config.SaveToFile(exePath))
            {
                SetStatus("✅ Impostazioni salvate!", Color.LightGreen);
            }
            else
            {
                SetStatus("✅ Impostazioni DB salvate, errore config file!", Color.Orange);
            }
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

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        try
        {
            SetStatus("Test connessione in corso...", Color.Yellow);

            // Salva temporaneamente i valori correnti
            var oldServer = Config.Server;
            var oldPort = Config.Port;
            var oldDatabase = Config.Database;
            var oldUser = Config.User;
            var oldPassword = Config.Password;

            // Imposta i valori dalla UI
            Config.SetConnectionParameters(_txtDbServer.Text, (int)_numDbPort.Value, _txtDbName.Text, _txtDbUser.Text, _txtDbPassword.Text);

            // Testa la connessione
            var success = await Database.TestConnectionAsync();

            // Ripristina i valori originali
            Config.SetConnectionParameters(oldServer, oldPort, oldDatabase, oldUser, oldPassword);

            if (success)
            {
                SetStatus("✅ Connessione riuscita!", Color.LightGreen);
            }
            else
            {
                SetStatus("❌ Connessione fallita!", Color.Red);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Errore test connessione: {ex.Message}", Color.Red);
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
