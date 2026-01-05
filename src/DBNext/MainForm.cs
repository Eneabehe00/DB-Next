using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using DBNext.Shared;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace DBNext;

/// <summary>
/// Form principale del saltacoda - mostra media a sinistra e numero a destra
/// </summary>
public class MainForm : Form
{
    private readonly Screen _targetScreen;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly System.Windows.Forms.Timer _slideshowTimer;
    private readonly Panel _mediaPanel;
    private readonly Panel _numberPanel;
    private readonly Label _numberLabel;
    private readonly Label _textLabel; // Scritta sopra/sotto il numero
    private readonly PictureBox _pictureBox;
    private readonly VideoView _videoView; // Per riproduzione video con LibVLCSharp
    
    private QueueSettings _settings = new();
    private int _lastNumber = -1;
    private DateTime _lastSettingsCheck = DateTime.MinValue;
    private bool _isClosing = false;
    private int _screenIndex = -1; // Indice del monitor (-1 se non specificato)

    // Finestra operatore
    private OperatorDisplayForm? _operatorForm;
    
    // LibVLC
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    // Sintesi Vocale
    private bool _speechInitialized = false;

    // Slideshow
    private string[] _slideshowFiles = Array.Empty<string>();
    private int _currentSlideIndex = 0;
    private bool _isPlayingVideoInSlideshow = false;

    // Barra Informativa
    private Panel _infoBarPanel;
    private Label _timeLabel;
    private Label _weatherLabel;
    private Label _newsLabel;
    private System.Windows.Forms.Timer _timeTimer;
    private System.Windows.Forms.Timer _newsTimer;
    private System.Windows.Forms.Timer _weatherTimer;
    private System.Windows.Forms.Timer _newsScrollTimer;
    private System.Windows.Forms.Timer _newsChangeTimer;
    private List<string> _newsHeadlines = new List<string>();
    private int _currentNewsIndex = 0;
    private string _currentWeather = "";
    private HttpClient _httpClient;

    // Cache automatica per slave
    private MediaCache? _mediaCache;
    private System.Timers.Timer? _cacheCleanupTimer;
    
    public MainForm(Screen targetScreen, QueueSettings? settings = null, int screenIndex = -1)
    {
        _targetScreen = targetScreen;
        _settings = settings ?? new QueueSettings();
        _screenIndex = screenIndex;
        
        // Setup form - Fullscreen senza bordi
        this.Text = "DB-Next";
        this.BackColor = Color.Black;
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.KeyPreview = true;
        this.DoubleBuffered = true;

        // Imposta l'icona dell'applicazione
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "DBNext.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Impossibile caricare l'icona: {ex.Message}");
        }
        
        // Imposta dimensioni e posizione esatte dello schermo
        this.Bounds = targetScreen.Bounds;
        
        // Panels
        _mediaPanel = new Panel
        {
            BackColor = Color.Black,
            Dock = DockStyle.Left
        };
        
        _numberPanel = new Panel
        {
            BackColor = Color.FromArgb(20, 20, 30),
            Dock = DockStyle.Fill
        };
        
        // PictureBox per media
        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };
        _mediaPanel.Controls.Add(_pictureBox);
        
        // Label per scritta (posizione dinamica sopra/sotto)
        _textLabel = new Label
        {
            Text = "",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Visible = false
        };
        
        // Label per numero
        _numberLabel = new Label
        {
            Text = "00",
            ForeColor = Color.FromArgb(255, 200, 50),
            BackColor = Color.FromArgb(20, 20, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Font = new Font("Arial Black", 100, FontStyle.Bold)
        };
        
        // Aggiungi le label al panel (ordine importante per Z-order)
        _numberPanel.Controls.Add(_numberLabel);
        _numberPanel.Controls.Add(_textLabel);
        
        // VideoView per riproduzione video con LibVLC
        _videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Color.Black
        };
        _mediaPanel.Controls.Add(_videoView);
        
        // Aggiungi panels al form
        this.Controls.Add(_numberPanel);
        this.Controls.Add(_mediaPanel);
        
        // Timer per polling DB
        _pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _pollTimer.Tick += async (s, e) => await PollDatabaseAsync();
        
        // Timer per slideshow
        _slideshowTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _slideshowTimer.Tick += (s, e) => NextSlide();

        // Barra Informativa
        _infoBarPanel = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(26, 26, 46),
            Height = 40,
            Visible = false
        };

        _timeLabel = new Label
        {
            Text = DateTime.Now.ToString("HH:mm:ss"),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(10, 0),
            Size = new Size(120, 40)
        };

        _weatherLabel = new Label
        {
            Text = "Caricamento meteo...",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(140, 0),
            Size = new Size(150, 40)
        };

        _newsLabel = new Label
        {
            Text = "Caricamento notizie...",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(300, 0),
            Size = new Size(this.Width - 300, 40)
        };

        _infoBarPanel.Controls.Add(_timeLabel);
        _infoBarPanel.Controls.Add(_weatherLabel);
        _infoBarPanel.Controls.Add(_newsLabel);

        // Timer per aggiornamenti barra informativa
        _timeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timeTimer.Tick += (s, e) => UpdateTime();

        _newsTimer = new System.Windows.Forms.Timer { Interval = 300000 }; // 5 minuti
        _newsTimer.Tick += async (s, e) => await UpdateNewsAsync();

        _weatherTimer = new System.Windows.Forms.Timer { Interval = 600000 }; // 10 minuti
        _weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();

        _newsScrollTimer = new System.Windows.Forms.Timer { Interval = 100 }; // Scrolling veloce (disabilitato)
        _newsScrollTimer.Tick += (s, e) => ScrollNews();

        _newsChangeTimer = new System.Windows.Forms.Timer { Interval = 8000 }; // Cambia notizia ogni 8 secondi
        _newsChangeTimer.Tick += (s, e) => ChangeNews();

        // HttpClient per API
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DB-Next/1.0");

        // Aggiungi la barra informativa al form (in cima)
        this.Controls.Add(_infoBarPanel);

        // Eventi
        this.Load += MainForm_Load;
        this.KeyDown += MainForm_KeyDown;
        this.FormClosing += MainForm_FormClosing;
        this.Resize += MainForm_Resize;

        // Inizializza cache automatica per slave
        InitializeMediaCache();
    }

    private void InitializeMediaCache()
    {
        try
        {
            // Inizializza cache solo se siamo slave
            if (IsSlave())
            {
                _mediaCache = new MediaCache(_settings.MediaPath ?? "");

                // Pulizia periodica ogni 24 ore
                _cacheCleanupTimer = new System.Timers.Timer(24 * 60 * 60 * 1000); // 24 ore
                _cacheCleanupTimer.Elapsed += (s, e) => _mediaCache.CleanupOldFiles();
                _cacheCleanupTimer.Start();

                Logger.Info("Cache automatica inizializzata per slave");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore inizializzazione cache: {ex.Message}");
        }
    }

    private bool IsSlave()
    {
        // Sei slave se il server configurato non è localhost
        return Config.Server != "localhost" && Config.Server != "127.0.0.1";
    }

    private string GetCachedSlideshowPath()
    {
        // Restituisce il percorso della cartella cache
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DBNext", "MediaCache"
        );
    }

    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        // Quando il video finisce durante lo slideshow, passa alla slide successiva
        if (_isPlayingVideoInSlideshow && !_isClosing)
        {
            this.Invoke(() =>
            {
                Logger.Info("Video terminato, passo alla slide successiva");
                // Usa BeginInvoke per evitare deadlock e race conditions
                this.BeginInvoke(() => NextSlide());
            });
        }
    }
    
    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            // Inizializza sintesi vocale
            try
            {
                _speechInitialized = true;
                Logger.Info("Sintesi vocale abilitata");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Sintesi vocale non disponibile: {ex.Message}");
                _speechInitialized = false;
            }

            // Inizializza LibVLC
            try
            {
                Core.Initialize();
                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC);
                _videoView.MediaPlayer = _mediaPlayer;
                
                // Event handler per quando il video finisce
                _mediaPlayer.EndReached += MediaPlayer_EndReached;
                
                Logger.Info("LibVLC inizializzato con successo");
            }
            catch (Exception ex)
            {
                Logger.Warn($"LibVLC non disponibile: {ex.Message}");
            }
            
            try
            {
            await LoadSettingsAsync();
            ApplyWindowBounds();
            ApplyNumberStyle();

            this.Show();
            Application.DoEvents();

            UpdateLayout();
            await LoadMedia();
            UpdateOperatorWindow();
            }
            catch (Exception ex)
            {
                Logger.Error($"Errore inizializzazione MainForm: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                throw; // Rilancia per essere catturato dal chiamante
            }
            
            var state = await Database.GetStateAsync();
            _lastNumber = state.CurrentNumber;
            UpdateNumber(state.CurrentNumber);

            Logger.Info($"Form caricato: Numero={state.CurrentNumber}, Media={_settings.MediaPath}");
            
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore inizializzazione form: {ex.Message}");
        }
    }
    
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _isClosing = true;
        _pollTimer.Stop();
        _slideshowTimer.Stop();
        
        if (Application.OpenForms.Count <= 1)
        {
            Logger.Info("=== DB-Next chiuso ===");
            Application.Exit();
        }
    }
    
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Escape:
                this.Close();
                break;
            case Keys.F11:
                ToggleFullscreen();
                break;
            case Keys.Right:
                if (_settings.MediaFolderMode) NextSlide();
                break;
            case Keys.Left:
                if (_settings.MediaFolderMode) PrevSlide();
                break;
        }
    }
    
    private void MainForm_Resize(object? sender, EventArgs e)
    {
        UpdateLayout();
        UpdateNumberAndLabelLayout();
    }
    
    private async Task LoadSettingsAsync()
    {
        try
        {
            // Se le impostazioni non sono state passate dal costruttore, caricale dal database
            if (_settings.Id == 1 && _settings.UpdatedAt == DateTime.MinValue)
            {
                Logger.Info("LoadSettingsAsync: Chiamata Database.GetSettingsAsync...");
                _settings = await Database.GetSettingsAsync();
                Logger.Info("LoadSettingsAsync: Database.GetSettingsAsync completato");
            }
            else
            {
                Logger.Info("LoadSettingsAsync: Impostazioni già caricate dal costruttore");
            }
            
            if (_settings.LayoutLeftPct <= 0 && _settings.LayoutRightPct <= 0)
            {
                _settings.LayoutLeftPct = 75;
                _settings.LayoutRightPct = 25;
            }
            
            if (string.IsNullOrEmpty(_settings.WindowMode))
                _settings.WindowMode = "borderless";
            
            _pollTimer.Interval = Math.Max(100, _settings.PollMs > 0 ? _settings.PollMs : 1000);
            _slideshowTimer.Interval = Math.Max(1000, _settings.SlideshowIntervalMs);

            Logger.Info($"Settings caricati: Layout {_settings.LayoutLeftPct}/{_settings.LayoutRightPct}, Window: {_settings.WindowMode}, MarginTop: {_settings.WindowMarginTop}");

            // Aggiorna barra informativa
            UpdateInfoBar();
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento settings: {ex.Message}");
            _settings = new QueueSettings
            {
                LayoutLeftPct = 75,
                LayoutRightPct = 25,
                WindowMode = "borderless",
                PollMs = 1000
            };
        }
    }
    
    private void ApplyNumberStyle()
    {
        try
        {
            Logger.Info("ApplyNumberStyle: Inizio");
            // Colore sfondo numero
            var bgColor = ColorFromHex(_settings.NumberBgColor);
            _numberPanel.BackColor = bgColor;
            _numberLabel.BackColor = bgColor;
            _textLabel.BackColor = bgColor;
        
        // Colore testo numero
        _numberLabel.ForeColor = ColorFromHex(_settings.NumberColor);
        
        // Scritta sopra/sotto il numero
        _textLabel.Text = _settings.NumberLabelText ?? "";
        _textLabel.ForeColor = ColorFromHex(_settings.NumberLabelColor);
        _textLabel.Visible = !string.IsNullOrEmpty(_settings.NumberLabelText);
        
            // Aggiorna layout
            UpdateNumberAndLabelLayout();
            Logger.Info("ApplyNumberStyle: Completato");
        }
        catch (Exception ex)
        {
            Logger.Error($"ApplyNumberStyle: ERRORE - {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private int GetMirrorMarginForScreen(int screenIndex, string mirrorMargins)
    {
        if (string.IsNullOrEmpty(mirrorMargins))
            return 0;

        var margins = mirrorMargins.Split(',');
        if (screenIndex < margins.Length)
        {
            if (int.TryParse(margins[screenIndex].Trim(), out int margin))
            {
                return Math.Max(0, margin); // Assicurati che non sia negativo
            }
        }

        return 0; // Default se non specificato
    }

    private void ApplyWindowBounds()
    {
        Logger.Info("ApplyWindowBounds: Inizio");

        try
        {
            // Sempre fullscreen senza bordi sul monitor target
            this.FormBorderStyle = FormBorderStyle.None;

            // Applica margine superiore se configurato (per banner/overlay)
            var bounds = _targetScreen.Bounds;

            // Determina il margine da applicare
            int marginTop = _settings.WindowMarginTop;

            // In modalità mirror, usa margini specifici per monitor se configurati
            if (_settings.ScreenMode == "mirror" && _screenIndex >= 0 && !string.IsNullOrEmpty(_settings.MirrorMarginTops))
            {
                marginTop = GetMirrorMarginForScreen(_screenIndex, _settings.MirrorMarginTops);
                Logger.Info($"Modalità mirror - Monitor {_screenIndex}: margine specifico = {marginTop}px");
            }

            if (marginTop > 0)
            {
                bounds = new Rectangle(
                    bounds.X,
                    bounds.Y + marginTop,
                    bounds.Width,
                    bounds.Height - marginTop
                );
                Logger.Info($"Applicato margine superiore di {marginTop}px");
            }

            this.Bounds = bounds;
            this.TopMost = false;

            Logger.Info($"Finestra impostata a: {this.Bounds.Width}x{this.Bounds.Height} at ({this.Bounds.X},{this.Bounds.Y})");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore ApplyWindowBounds: {ex.Message}");
        }

        Logger.Info("ApplyWindowBounds: Completato");
    }

    private async Task PollDatabaseAsync()
    {
        if (_isClosing) return;
        
        try
        {
            if ((DateTime.Now - _lastSettingsCheck).TotalSeconds > 5)
            {
                var newSettings = await Database.GetSettingsAsync();
                if (newSettings.UpdatedAt != _settings.UpdatedAt)
                {
                    Logger.Info($"Rilevate nuove impostazioni - UpdatedAt: {_settings.UpdatedAt} -> {newSettings.UpdatedAt}");

                    var oldMarginTop = _settings.WindowMarginTop;
                    var oldNewsInterval = _settings.NewsRssUpdateIntervalMs;
                    var oldInfoBarEnabled = _settings.InfoBarEnabled;

                    _settings = newSettings;
                    _pollTimer.Interval = Math.Max(100, _settings.PollMs);
                    _slideshowTimer.Interval = Math.Max(1000, _settings.SlideshowIntervalMs);

                    Logger.Info($"Nuovo intervallo RSS: {oldNewsInterval}ms -> {_settings.NewsRssUpdateIntervalMs}ms");

                    this.Invoke(async () =>
                    {
                        // Se il margine superiore è cambiato, ricalcola i bounds
                        if (oldMarginTop != _settings.WindowMarginTop)
                        {
                            ApplyWindowBounds();
                        }
                        ApplyNumberStyle();
                        UpdateLayout();
                        await LoadMedia();
                        UpdateOperatorWindow();

                        // Se è cambiato l'intervallo RSS o lo stato della barra informativa, aggiorna la barra
                        if (oldNewsInterval != _settings.NewsRssUpdateIntervalMs || oldInfoBarEnabled != _settings.InfoBarEnabled)
                        {
                            Logger.Info("Aggiornamento barra informativa per cambiamenti RSS");
                            UpdateInfoBar();
                        }
                    });
                }
                _lastSettingsCheck = DateTime.Now;
            }
            
            var state = await Database.GetStateAsync();
            
            if (state.CurrentNumber != _lastNumber)
            {
                _lastNumber = state.CurrentNumber;
                this.Invoke(() => UpdateNumber(state.CurrentNumber));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore polling: {ex.Message}");
        }
    }
    
    private void ApplyWindowMode()
    {
        Logger.Info("ApplyWindowMode: Inizio - Mode=" + _settings.WindowMode);

        try
        {
            // Semplificazione: sempre fullscreen senza bordi sul monitor target
            this.FormBorderStyle = FormBorderStyle.None;

            // Applica margine superiore se configurato (per banner/overlay)
            var bounds = _targetScreen.Bounds;
            if (_settings.WindowMarginTop > 0)
            {
                bounds = new Rectangle(
                    bounds.X,
                    bounds.Y + _settings.WindowMarginTop,
                    bounds.Width,
                    bounds.Height - _settings.WindowMarginTop
                );
                Logger.Info($"Applicato margine superiore di {_settings.WindowMarginTop}px");
            }

            this.Bounds = bounds;
            this.TopMost = false;

            Logger.Info($"Finestra impostata a: {this.Bounds.Width}x{this.Bounds.Height} at ({this.Bounds.X},{this.Bounds.Y})");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore ApplyWindowMode: {ex.Message}");
        }

        Logger.Info("ApplyWindowMode: Completato");
    }
    
    private void ToggleFullscreen()
    {
        // Disabilitato - sempre fullscreen
        Logger.Info("Toggle fullscreen disabilitato - finestra sempre in fullscreen");
    }
    
    private void UpdateLayout()
    {
        try
        {
            Logger.Info("UpdateLayout: Inizio");

            // Calcola altezza disponibile (considerando la barra informativa se attiva)
            var availableHeight = this.ClientSize.Height;
            if (_settings.InfoBarEnabled)
            {
                availableHeight -= _settings.InfoBarHeight;
            }

            // Imposta altezza dei pannelli
            _mediaPanel.Height = availableHeight;
            _numberPanel.Height = availableHeight;

            // Se c'è la barra informativa, riposiziona i pannelli sotto di essa
            if (_settings.InfoBarEnabled)
            {
                _mediaPanel.Top = _settings.InfoBarHeight;
                _numberPanel.Top = _settings.InfoBarHeight;
            }
            else
            {
                _mediaPanel.Top = 0;
                _numberPanel.Top = 0;
            }

            int leftPct = _settings.LayoutLeftPct > 0 ? _settings.LayoutLeftPct : 75;
            leftPct = Math.Clamp(leftPct, 0, 100);

            int leftWidth = (int)(this.ClientSize.Width * leftPct / 100.0);
            Logger.Info($"UpdateLayout: Layout {leftPct}/{(100-leftPct)}, LeftWidth={leftWidth}, InfoBar: {_settings.InfoBarEnabled}");

            _mediaPanel.Width = leftWidth;
            _mediaPanel.Visible = leftPct > 0;
            _numberPanel.Visible = leftPct < 100;

            UpdateNumberAndLabelLayout();

            this.Invalidate();
            this.Update();
            Logger.Info("UpdateLayout: Completato");
        }
        catch (Exception ex)
        {
            Logger.Error($"UpdateLayout: ERRORE - {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private void UpdateNumber(int number)
    {
        _numberLabel.Text = number.ToString("00");
        UpdateNumberAndLabelLayout();

        // Sintesi vocale se abilitata
        if (_settings.VoiceEnabled && _speechInitialized)
        {
            try
            {
                string textToSpeak = string.IsNullOrWhiteSpace(_settings.VoicePrefix)
                    ? $"SERVIAMO IL NUMERO {number}"
                    : $"{_settings.VoicePrefix} {number}";
                // Usa PowerShell per la sintesi vocale
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{textToSpeak}')\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                Logger.Info($"Sintesi vocale: {textToSpeak}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Errore sintesi vocale: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Aggiorna layout e font di numero e scritta in modo responsive
    /// La scritta è sempre centrata sull'asse X e posizionata con offset sull'asse Y
    /// </summary>
    private void UpdateNumberAndLabelLayout()
    {
        var panelWidth = _numberPanel.Width;
        var panelHeight = _numberPanel.Height;
        
        if (panelWidth <= 0 || panelHeight <= 0) return;
        
        bool hasLabel = !string.IsNullOrEmpty(_settings.NumberLabelText);
        bool labelOnTop = _settings.NumberLabelPosition?.ToLower() != "bottom";
        int offset = _settings.NumberLabelOffset; // Offset in pixel dalla posizione
        
        // Calcola dimensione font della scritta (responsive)
        float labelFontSize;
        if (_settings.NumberLabelSize > 0)
        {
            labelFontSize = _settings.NumberLabelSize;
        }
        else
        {
            // Auto: dimensione proporzionale al pannello, ma limitata per stare dentro
            labelFontSize = Math.Max(10, Math.Min(panelHeight * 0.06f, panelWidth * 0.045f));
        }
        
        // Altezza label proporzionale al font
        int labelHeight = hasLabel ? (int)(labelFontSize * 2.0) : 0;
        
        // Applica font alla scritta
        if (hasLabel)
        {
            try
            {
                var oldFont = _textLabel.Font;
                _textLabel.Font = new Font("Segoe UI", labelFontSize, FontStyle.Regular);
                oldFont?.Dispose();
            }
            catch { }
            
            // Imposta dimensioni e proprietà della label
            _textLabel.Height = labelHeight;
            _textLabel.Width = panelWidth; // Larghezza = pannello intero per centratura
            _textLabel.Left = 0; // Sempre a sinistra = 0, centrato con TextAlign
            _textLabel.Visible = true;
            _textLabel.AutoEllipsis = true; // Tronca automaticamente con ... se troppo lungo
            
            if (labelOnTop)
            {
                // Scritta sopra: offset positivo = più in basso dal top
                _textLabel.Top = Math.Max(0, offset);
                _textLabel.TextAlign = ContentAlignment.MiddleCenter;
            }
            else
            {
                // Scritta sotto: offset positivo = più in alto dal bottom
                int bottomY = panelHeight - labelHeight - Math.Max(0, offset);
                _textLabel.Top = Math.Max(0, bottomY);
                _textLabel.TextAlign = ContentAlignment.MiddleCenter;
            }
        }
        else
        {
            _textLabel.Visible = false;
        }
        
        // Calcola spazio disponibile per il numero
        int labelSpace = hasLabel ? labelHeight + Math.Abs(offset) + 5 : 0; // +5 padding
        int availableHeight = panelHeight - labelSpace;
        int numberTop = 0;
        
        if (hasLabel)
        {
            if (labelOnTop)
            {
                // Numero sotto la scritta
                numberTop = labelHeight + Math.Max(0, offset) + 5;
                availableHeight = panelHeight - numberTop;
            }
            else
            {
                // Numero sopra la scritta
                numberTop = 0;
                availableHeight = panelHeight - labelHeight - Math.Max(0, offset) - 5;
            }
        }
        
        // Posiziona la label del numero (centrata su X, con spazio per la scritta su Y)
        _numberLabel.Left = 0;
        _numberLabel.Top = numberTop;
        _numberLabel.Width = panelWidth;
        _numberLabel.Height = Math.Max(50, availableHeight);
        
        // Calcola dimensione font del numero (responsive)
        float fontSize;
        if (_settings.NumberFontSize > 0)
        {
            fontSize = _settings.NumberFontSize;
        }
        else
        {
            // Auto: adatta al pannello disponibile
            float fontSizeByHeight = _numberLabel.Height * 0.55f;
            float fontSizeByWidth = panelWidth * 0.35f;
            fontSize = Math.Min(fontSizeByHeight, fontSizeByWidth);
        }
        
        fontSize = Math.Max(24, Math.Min(fontSize, 500));
        
        var fontFamily = string.IsNullOrEmpty(_settings.NumberFontFamily) ? "Arial Black" : _settings.NumberFontFamily;
        var fontStyle = _settings.NumberFontBold ? FontStyle.Bold : FontStyle.Regular;
        
        try
        {
            var oldFont = _numberLabel.Font;
            _numberLabel.Font = new Font(fontFamily, fontSize, fontStyle);
            oldFont?.Dispose();
        }
        catch
        {
            try
            {
                _numberLabel.Font = new Font("Arial", Math.Max(24, fontSize * 0.8f), FontStyle.Bold);
            }
            catch { }
        }
    }
    
    private async Task LoadMedia()
    {
        try
        {
            Logger.Info("LoadMedia: Inizio");
            _slideshowTimer.Stop();
            _videoView.Visible = false;
            _pictureBox.Visible = true;

            // === LOGICA SCHEDULER MEDIA ===
            // Determina quali impostazioni media usare (normali o scheduler)
            string activeMediaPath;
            string activeMediaType;
            string activeMediaFit;
            bool activeMediaFolderMode;
            int activeSlideshowIntervalMs;

            DateTime oggi = DateTime.Today;
            bool schedulerAttivo = _settings.MediaSchedulerEnabled &&
                                   oggi >= _settings.MediaSchedulerStartDate &&
                                   oggi <= _settings.MediaSchedulerEndDate;

            if (schedulerAttivo)
            {
                // Usa impostazioni SCHEDULER
                activeMediaPath = _settings.MediaSchedulerPath ?? "";
                activeMediaType = _settings.MediaSchedulerType ?? "image";
                activeMediaFit = _settings.MediaSchedulerFit ?? "cover";
                activeMediaFolderMode = _settings.MediaSchedulerFolderMode;
                activeSlideshowIntervalMs = _settings.MediaSchedulerIntervalMs;

                Logger.Info($"LoadMedia: Scheduler ATTIVO - Data corrente {oggi:yyyy-MM-dd} è nel range [{_settings.MediaSchedulerStartDate:yyyy-MM-dd} - {_settings.MediaSchedulerEndDate:yyyy-MM-dd}]");
                Logger.Info($"LoadMedia: Uso impostazioni scheduler - Path: '{activeMediaPath}', Type: '{activeMediaType}'");
            }
            else
            {
                // Usa impostazioni NORMALI
                activeMediaPath = _settings.MediaPath ?? "";
                activeMediaType = _settings.MediaType ?? "image";
                activeMediaFit = _settings.MediaFit ?? "cover";
                activeMediaFolderMode = _settings.MediaFolderMode;
                activeSlideshowIntervalMs = _settings.SlideshowIntervalMs;

                if (_settings.MediaSchedulerEnabled)
                {
                    Logger.Info($"LoadMedia: Scheduler abilitato ma NON attivo - Data corrente {oggi:yyyy-MM-dd} NON è nel range [{_settings.MediaSchedulerStartDate:yyyy-MM-dd} - {_settings.MediaSchedulerEndDate:yyyy-MM-dd}]");
                }
                else
                {
                    Logger.Info("LoadMedia: Scheduler non abilitato - uso impostazioni normali");
                }
                Logger.Info($"LoadMedia: Uso impostazioni normali - Path: '{activeMediaPath}', Type: '{activeMediaType}'");
            }

            // === LOGICA CACHE AUTOMATICA ===
            string mediaPathToUse = activeMediaPath;
            if (IsSlave() && _mediaCache != null)
            {
                try
                {
                    // Per slideshow/cartelle
                    if (activeMediaFolderMode && Directory.Exists(activeMediaPath))
                    {
                        // Cache di tutta la cartella slideshow
                        await _mediaCache.CacheSlideshowFolderAsync(activeMediaPath);
                        mediaPathToUse = GetCachedSlideshowPath();
                    }
                    else
                    {
                        // Cache singolo file
                        string filename = Path.GetFileName(activeMediaPath);
                        mediaPathToUse = await _mediaCache.GetMediaPathAsync(filename);
                    }

                    Logger.Info($"Uso percorso cache: {mediaPathToUse}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Cache fallita, uso percorso remoto: {ex.Message}");
                    // Continua con mediaPathToUse = activeMediaPath (remoto)
                }
            }

            Logger.Info($"LoadMedia: Caricamento media '{mediaPathToUse}' tipo '{activeMediaType}'");
            _pictureBox.Image?.Dispose();
            _pictureBox.Image = null;

            if (string.IsNullOrEmpty(mediaPathToUse))
            {
                _pictureBox.BackColor = Color.Black;
                return;
            }

            // Imposta modalità di visualizzazione
            _pictureBox.SizeMode = activeMediaFit?.ToLower() == "contain"
                ? PictureBoxSizeMode.Zoom
                : PictureBoxSizeMode.StretchImage;

            // Modalità cartella (slideshow)
            if (activeMediaFolderMode && Directory.Exists(mediaPathToUse))
            {
                LoadSlideshow(mediaPathToUse, activeSlideshowIntervalMs);
                return;
            }

            // File singolo
            if (!File.Exists(mediaPathToUse))
            {
                _pictureBox.BackColor = Color.Black;
                return;
            }

            // File singolo = non è slideshow
            _isPlayingVideoInSlideshow = false;

            var ext = Path.GetExtension(mediaPathToUse).ToLower();

            if (ext == ".gif" || activeMediaType == "gif")
            {
                _pictureBox.Image = Image.FromFile(mediaPathToUse);
            }
            else if (IsVideoFile(ext) || activeMediaType == "video")
            {
                // Riproduci video con LibVLC (loop infinito per file singolo)
                LoadVideo(mediaPathToUse);
            }
            else
            {
                _pictureBox.Image = Image.FromFile(mediaPathToUse);
            }

            Logger.Info($"Media caricato: {activeMediaPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento media: {ex.Message}");
            _pictureBox.BackColor = Color.FromArgb(30, 30, 40);
        }
    }
    
    private void LoadSlideshow(string mediaPath, int slideshowIntervalMs)
    {
        try
        {
            // Include sia immagini che video
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var videoExtensions = new[] { ".mp4", ".avi", ".wmv", ".webm", ".mkv", ".mov" };
            var allExtensions = imageExtensions.Concat(videoExtensions).ToArray();

            _slideshowFiles = Directory.GetFiles(mediaPath)
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToArray();

            if (_slideshowFiles.Length == 0)
            {
                Logger.Warn($"Nessun file media trovato in: {mediaPath}");
                _pictureBox.BackColor = Color.Black;
                return;
            }
            
            _currentSlideIndex = 0;
            ShowCurrentSlide();
            
            if (_slideshowFiles.Length > 1)
            {
                _slideshowTimer.Interval = Math.Max(1000, slideshowIntervalMs);
                _slideshowTimer.Start();
            }

            Logger.Info($"Slideshow avviato: {_slideshowFiles.Length} file (immagini e video), intervallo {slideshowIntervalMs}ms");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento slideshow: {ex.Message}");
        }
    }
    
    private void ShowCurrentSlide()
    {
        if (_slideshowFiles.Length == 0) return;
        
        try
        {
            var path = _slideshowFiles[_currentSlideIndex];
            var ext = Path.GetExtension(path).ToLower();

            // Controlla se è un video
            if (IsVideoFile(ext))
            {
                // Mostra video
                _isPlayingVideoInSlideshow = true;
                _slideshowTimer.Stop(); // Ferma il timer, il video andrà fino alla fine
                LoadVideo(path);
                Logger.Info($"Avvio video nello slideshow: {Path.GetFileName(path)}");
            }
            else
            {
                // Mostra immagine
                _isPlayingVideoInSlideshow = false;
                _videoView.Visible = false;
                _pictureBox.Visible = true;
                
                _pictureBox.Image?.Dispose();
                
                // Carica immagine in memoria per evitare lock sul file
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                {
                    _pictureBox.Image = Image.FromStream(stream);
                }
                
                // Per le immagini, riavvia il timer se ci sono più file
                if (_slideshowFiles.Length > 1 && !_slideshowTimer.Enabled)
                {
                    _slideshowTimer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore visualizzazione slide: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
        Logger.Info("LoadMedia: Completato");
    }
    
    private void NextSlide()
    {
        if (_slideshowFiles.Length == 0) return;
        _currentSlideIndex = (_currentSlideIndex + 1) % _slideshowFiles.Length;
        ShowCurrentSlide();
    }
    
    private void PrevSlide()
    {
        if (_slideshowFiles.Length == 0) return;
        _currentSlideIndex = (_currentSlideIndex - 1 + _slideshowFiles.Length) % _slideshowFiles.Length;
        ShowCurrentSlide();
    }
    
    private bool IsVideoFile(string ext)
    {
        return ext == ".mp4" || ext == ".avi" || ext == ".wmv" || ext == ".webm" || ext == ".mkv" || ext == ".mov";
    }
    
    private void LoadVideo(string videoPath)
    {
        try
        {
            if (_mediaPlayer == null || _libVLC == null)
            {
                Logger.Warn("LibVLC non inizializzato, impossibile riprodurre video");
                ShowVideoPlaceholder(videoPath);
                return;
            }

            _pictureBox.Visible = false;
            _videoView.Visible = true;

            // Aspetta che il player sia pronto (non stia finendo un video)
            // per evitare race conditions durante la transizione
            if (_mediaPlayer.State == VLCState.Ended || _mediaPlayer.State == VLCState.Stopped)
            {
                // Piccola pausa per permettere al sistema di stabilizzarsi
                System.Threading.Thread.Sleep(100);
            }

            // Crea media da file
            using var media = new Media(_libVLC, new Uri(videoPath));

            // Loop solo se NON siamo in modalità slideshow
            if (!_isPlayingVideoInSlideshow)
            {
                media.AddOption(":input-repeat=65535"); // Loop infinito
            }
            // Se siamo in slideshow, il video va una sola volta e poi passa alla slide successiva

            // Imposta aspect ratio in base a MediaFit
            var aspectRatio = _settings.MediaFit?.ToLower() == "contain" ? "" : "16:9";

            _mediaPlayer.Media = media;
            _mediaPlayer.AspectRatio = aspectRatio;
            _mediaPlayer.Scale = 0; // 0 = fit to window

            // Avvia riproduzione
            _mediaPlayer.Play();

            Logger.Info($"Video caricato con LibVLC: {videoPath} (Loop: {!_isPlayingVideoInSlideshow})");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento video: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            ShowVideoPlaceholder(videoPath);
        }
    }
    
    private void ShowVideoPlaceholder(string videoPath)
    {
        _videoView.Visible = false;
        _pictureBox.Visible = true;
        _pictureBox.BackColor = Color.FromArgb(30, 30, 40);
        
        // Crea un placeholder con icona video
        try
        {
            var bmp = new Bitmap(_mediaPanel.Width > 0 ? _mediaPanel.Width : 800, 
                                 _mediaPanel.Height > 0 ? _mediaPanel.Height : 600);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(30, 30, 40));
                
                // Disegna icona play
                var centerX = bmp.Width / 2;
                var centerY = bmp.Height / 2;
                var size = Math.Min(bmp.Width, bmp.Height) / 4;
                
                var playIcon = new Point[]
                {
                    new Point(centerX - size/2, centerY - size/2),
                    new Point(centerX - size/2, centerY + size/2),
                    new Point(centerX + size/2, centerY)
                };
                
                using (var brush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillPolygon(brush, playIcon);
                }
                
                // Testo
                using (var font = new Font("Segoe UI", 14))
                using (var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                {
                    var text = "Video: " + Path.GetFileName(videoPath);
                    var textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, 
                        centerX - textSize.Width/2, 
                        centerY + size/2 + 20);
                    
                    var note = "Errore riproduzione video";
                    var noteSize = g.MeasureString(note, font);
                    g.DrawString(note, font, brush,
                        centerX - noteSize.Width/2,
                        centerY + size/2 + 50);
                }
            }
            
            _pictureBox.Image = bmp;
        }
        catch
        {
            _pictureBox.BackColor = Color.FromArgb(30, 30, 40);
        }
        
        Logger.Warn($"Video placeholder per: {videoPath}");
    }
    
    private static Color ColorFromHex(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.FromArgb(255, 200, 50);
        
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
        
        return Color.FromArgb(255, 200, 50);
    }
    
    private void UpdateOperatorWindow()
    {
        try
        {
            if (_settings.OperatorWindowEnabled)
            {
                if (_operatorForm == null)
                {
                    _operatorForm = new OperatorDisplayForm();
                }

                _operatorForm.UpdateSettings(_settings);

                if (!_operatorForm.Visible)
                {
                    _operatorForm.Show();
                }
            }
            else
            {
                if (_operatorForm != null)
                {
                    _operatorForm.Hide();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore gestione finestra operatore: {ex.Message}");
        }
    }

    // === METODI BARRA INFORMATIVA ===

    private void UpdateInfoBar()
    {
        try
        {
            // Controlla se la barra deve essere abilitata per questo monitor
            bool shouldShowInfoBar = _settings.InfoBarEnabled;

            // In modalità mirror, controlla se questo monitor specifico deve mostrare la barra
            if (_settings.ScreenMode == "mirror" && !string.IsNullOrEmpty(_settings.MirrorInfoBarDisplays))
            {
                var allowedDisplays = _settings.MirrorInfoBarDisplays.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                    .Where(n => n >= 0)
                    .ToArray();

                // Se la lista non è vuota, mostra la barra solo sui monitor specificati
                if (allowedDisplays.Length > 0)
                {
                    shouldShowInfoBar = allowedDisplays.Contains(_screenIndex);
                }
            }

            if (shouldShowInfoBar)
            {
                _infoBarPanel.BackColor = ColorFromHex(_settings.InfoBarBgColor);
                _infoBarPanel.Height = _settings.InfoBarHeight;
                _infoBarPanel.Visible = true;

                // Aggiorna font e colori
                var font = new Font(_settings.InfoBarFontFamily, _settings.InfoBarFontSize, FontStyle.Regular);
                var textColor = ColorFromHex(_settings.InfoBarTextColor);

                _timeLabel.Font = font;
                _timeLabel.ForeColor = textColor;
                _timeLabel.BackColor = Color.Transparent;
                _timeLabel.Height = _settings.InfoBarHeight;
                _timeLabel.TextAlign = ContentAlignment.MiddleCenter;

                _weatherLabel.Font = font;
                _weatherLabel.ForeColor = textColor;
                _weatherLabel.BackColor = Color.Transparent;
                _weatherLabel.Height = _settings.InfoBarHeight;
                _weatherLabel.TextAlign = ContentAlignment.MiddleLeft;

                _newsLabel.Font = font;
                _newsLabel.ForeColor = textColor;
                _newsLabel.BackColor = Color.Transparent;
                _newsLabel.Height = _settings.InfoBarHeight;
                _newsLabel.TextAlign = ContentAlignment.MiddleLeft;

                // Riposiziona controlli - ancora più spazio al meteo, tutto il resto alle notizie
                _timeLabel.Width = 120;
                _weatherLabel.Location = new Point(130, 0);
                _weatherLabel.Width = 350; // Aumentato ulteriormente a 350px per molto più spazio al meteo
                _newsLabel.Location = new Point(490, 0); // Aggiornato per riflettere il nuovo spazio del meteo
                _newsLabel.Width = Math.Max(200, this.Width - 490); // Assicurati che abbia almeno 200px

                // Ferma e riavvia i timer per prendere eventuali nuovi intervalli
                StopInfoBar();
                StartInfoBar();
            }
            else
            {
                _infoBarPanel.Visible = false;
                StopInfoBar();
            }

            // Aggiorna layout dei pannelli sottostanti
            UpdateLayout();
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore aggiornamento barra informativa: {ex.Message}");
        }
    }

    private void StartInfoBar()
    {
        try
        {
            _timeTimer.Start();

            // RSS Ansa.it - sempre disponibile se barra informativa abilitata
            if (_settings.InfoBarEnabled)
            {
                _newsTimer.Interval = _settings.NewsRssUpdateIntervalMs;
                _newsTimer.Start();
                _newsChangeTimer.Start();
                // Scarica notizie iniziali
                _ = UpdateNewsAsync();
            }
            else
            {
                _newsLabel.Text = "Barra informativa non abilitata";
            }

            if (!string.IsNullOrEmpty(_settings.WeatherApiKey))
            {
                _weatherTimer.Interval = _settings.WeatherUpdateIntervalMs;
                _weatherTimer.Start();
                // Scarica meteo iniziale
                _ = UpdateWeatherAsync();
            }
            else
            {
                _weatherLabel.Text = "API Meteo non configurata";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore avvio barra informativa: {ex.Message}");
        }
    }

    private void StopInfoBar()
    {
        try
        {
            _timeTimer.Stop();
            _newsTimer.Stop();
            _weatherTimer.Stop();
            _newsScrollTimer.Stop();
            _newsChangeTimer.Stop();
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore arresto barra informativa: {ex.Message}");
        }
    }

    private void UpdateTime()
    {
        try
        {
            if (_timeLabel.IsHandleCreated)
            {
                _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore aggiornamento orario: {ex.Message}");
        }
    }

    private async Task UpdateNewsAsync()
    {
        try
        {
            Logger.Info("Inizio caricamento RSS Ansa.it");

            // Definizione categorie RSS Ansa.it con URL (filtrate per quelle abilitate)
            var allRssFeeds = new[]
            {
                new { Category = "Ultima Ora", Url = "https://www.ansa.it/sito/ansait/rss.xml", Enabled = DBNext.Shared.Config.RssUltimaOraEnabled },
                new { Category = "Cronaca", Url = "https://www.ansa.it/sito/notizie/cronaca/cronaca_rss.xml", Enabled = DBNext.Shared.Config.RssCronacaEnabled },
                new { Category = "Politica", Url = "https://www.ansa.it/sito/notizie/politica/politica_rss.xml", Enabled = DBNext.Shared.Config.RssPoliticaEnabled },
                new { Category = "Mondo", Url = "https://www.ansa.it/sito/notizie/mondo/mondo_rss.xml", Enabled = DBNext.Shared.Config.RssMondoEnabled },
                new { Category = "Economia", Url = "https://www.ansa.it/sito/notizie/economia/economia_rss.xml", Enabled = DBNext.Shared.Config.RssEconomiaEnabled },
                new { Category = "Sport", Url = "https://www.ansa.it/sito/notizie/sport/sport_rss.xml", Enabled = DBNext.Shared.Config.RssSportEnabled }
            };

            // Filtra solo i feed abilitati
            var rssFeeds = allRssFeeds.Where(f => f.Enabled).ToArray();

            var allNews = new List<(string Category, string Title, string Description, DateTime PubDate)>();

            // Scarica da tutti i feed RSS
            foreach (var feed in rssFeeds)
            {
                try
                {
                    Logger.Info($"Caricamento RSS {feed.Category}: {feed.Url}");

                    var response = await _httpClient.GetAsync(feed.Url);
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"RSS {feed.Category} non disponibile: {response.StatusCode}");
                        continue;
                    }

                    var xmlContent = await response.Content.ReadAsStringAsync();
                    var rssDoc = XDocument.Parse(xmlContent);

                    // Estrai i primi N items (notizie più recenti) da ogni categoria
                    var items = rssDoc.Descendants("item").Take(DBNext.Shared.Config.RssNewsPerCategory);
                    foreach (var item in items)
                    {
                        var title = item.Element("title")?.Value ?? "";
                        var description = item.Element("description")?.Value ?? "";
                        var pubDateStr = item.Element("pubDate")?.Value ?? "";

                        // Parse data pubblicazione
                        DateTime pubDate = DateTime.MinValue;
                        if (!string.IsNullOrEmpty(pubDateStr))
                        {
                            try
                            {
                                // Formato RSS: "Wed, 01 Jan 2020 12:00:00 +0000"
                                pubDate = DateTime.Parse(pubDateStr);
                            }
                            catch
                            {
                                // Fallback: usa data corrente se parsing fallisce
                                pubDate = DateTime.Now;
                            }
                        }

                        // Pulisci e limita testo
                        title = System.Web.HttpUtility.HtmlDecode(title.Trim());
                        description = System.Web.HttpUtility.HtmlDecode(description.Trim());

                        // Rimuovi tag HTML dalla descrizione
                        description = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]+>", "");

                        // Limita lunghezze
                        if (title.Length > 100) title = title.Substring(0, 97) + "...";
                        if (description.Length > 150) description = description.Substring(0, 147) + "...";

                        if (!string.IsNullOrEmpty(title))
                        {
                            allNews.Add((feed.Category, title, description, pubDate));
                            Logger.Info($"RSS {feed.Category}: '{title}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Errore caricamento RSS {feed.Category}: {ex.Message}");
                }
            }

            // Ordina per data pubblicazione (più recenti prima)
            allNews = allNews.OrderByDescending(n => n.PubDate).ToList();

            _newsHeadlines.Clear();

            // Crea headlines nel formato "CATEGORIA: TITOLO - DESCRIZIONE"
            foreach (var news in allNews)
            {
                var headline = $"{news.Category}: {news.Title}";
                if (!string.IsNullOrEmpty(news.Description))
                {
                    headline += $" - {news.Description}";
                }
                _newsHeadlines.Add(headline);
            }

            if (_newsHeadlines.Count == 0)
            {
                _newsHeadlines.Add("Nessuna notizia disponibile da Ansa.it");
                Logger.Info("RSS Ansa.it: nessuna notizia caricata");
            }
            else
            {
                Logger.Info($"RSS Ansa.it: caricate {_newsHeadlines.Count} notizie da {rssFeeds.Length} categorie (max {DBNext.Shared.Config.RssNewsPerCategory} per categoria)");
            }

            _currentNewsIndex = 0;

            // Mostra immediatamente la prima notizia
            if (_newsHeadlines.Count > 0 && _newsLabel.IsHandleCreated)
            {
                _newsLabel.Text = _newsHeadlines[0];
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento RSS Ansa.it: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            _newsHeadlines.Clear();
            _newsHeadlines.Add("Errore caricamento notizie");
        }
    }

    private async Task UpdateWeatherAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.WeatherApiKey))
                return;

            var encodedCity = Uri.EscapeDataString(_settings.WeatherCity);
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={encodedCity}&units={_settings.WeatherUnits}&appid={_settings.WeatherApiKey}&lang=it";
            Logger.Info($"Richiesta OpenWeatherMap: città={_settings.WeatherCity} (encoded: {encodedCity}), unità={_settings.WeatherUnits}");

            var response = await _httpClient.GetAsync(url);
            Logger.Info($"OpenWeatherMap response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.Error($"OpenWeatherMap error: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _weatherLabel.Text = "API Key non valida";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _weatherLabel.Text = "Limite richieste superato";
                }
                else
                {
                    _weatherLabel.Text = $"Errore API: {(int)response.StatusCode}";
                }
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            Logger.Info($"OpenWeatherMap response JSON length: {json.Length}");

            var doc = JsonDocument.Parse(json);

            // Controlla se c'è un codice di errore nella risposta
            if (doc.RootElement.TryGetProperty("cod", out var cod))
            {
                var codValue = cod.GetInt32();
                if (codValue != 200)
                {
                    var message = doc.RootElement.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : "Errore sconosciuto";
                    Logger.Error($"OpenWeatherMap error code: {codValue} - {message}");
                    _weatherLabel.Text = $"Errore: {message}";
                    return;
                }
            }

            // Estrai i dati del meteo
            if (doc.RootElement.TryGetProperty("main", out var main) &&
                main.TryGetProperty("temp", out var tempProp))
            {
                var temp = tempProp.GetDouble();

                string description = "N/A";
                if (doc.RootElement.TryGetProperty("weather", out var weather) &&
                    weather.GetArrayLength() > 0)
                {
                    var weatherItem = weather[0];
                    if (weatherItem.TryGetProperty("description", out var descProp))
                    {
                        description = descProp.GetString() ?? "N/A";
                    }
                }

                _currentWeather = $"{temp:F1}°C - {description}";
                _weatherLabel.Text = _currentWeather;

                Logger.Info($"OpenWeatherMap: {_currentWeather}");
            }
            else
            {
                Logger.Error("OpenWeatherMap: struttura JSON non valida, mancanti proprietà 'main' o 'temp'");
                _weatherLabel.Text = "Dati meteo non disponibili";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento meteo: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            _weatherLabel.Text = "Errore meteo";
        }
    }

    private void ChangeNews()
    {
        try
        {
            if (_newsHeadlines.Count == 0)
                return;

            // Passa alla notizia successiva
            _currentNewsIndex = (_currentNewsIndex + 1) % _newsHeadlines.Count;

            if (_newsLabel.IsHandleCreated)
            {
                _newsLabel.Text = _newsHeadlines[_currentNewsIndex];
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore cambio notizia: {ex.Message}");
        }
    }


    private void ScrollNews()
    {
        // Metodo disabilitato - ora le notizie cambiano invece di scorrere
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer?.Dispose();
            _slideshowTimer?.Dispose();
            _timeTimer?.Dispose();
            _newsTimer?.Dispose();
            _weatherTimer?.Dispose();
            _newsScrollTimer?.Dispose();
            _newsChangeTimer?.Dispose();
            _httpClient?.Dispose();
            _pictureBox?.Image?.Dispose();
            _numberLabel?.Font?.Dispose();
            _textLabel?.Font?.Dispose();

            // Chiudi finestra operatore
            _operatorForm?.Hide();
            _operatorForm?.Dispose();

            // Sintesi vocale pulita automaticamente

            // Pulisci LibVLC
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }
        base.Dispose(disposing);
    }
}
