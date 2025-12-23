using System.Drawing.Drawing2D;
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
    
    // LibVLC
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    
    // Slideshow
    private string[] _slideshowFiles = Array.Empty<string>();
    private int _currentSlideIndex = 0;
    private bool _isPlayingVideoInSlideshow = false;
    
    public MainForm(Screen targetScreen)
    {
        _targetScreen = targetScreen;
        
        // Setup form - Fullscreen senza bordi
        this.Text = "DB-Next";
        this.BackColor = Color.Black;
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.KeyPreview = true;
        this.DoubleBuffered = true;
        
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
        
        // Eventi
        this.Load += MainForm_Load;
        this.KeyDown += MainForm_KeyDown;
        this.FormClosing += MainForm_FormClosing;
        this.Resize += MainForm_Resize;
    }
    
    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        // Quando il video finisce durante lo slideshow, passa alla slide successiva
        if (_isPlayingVideoInSlideshow && !_isClosing)
        {
            this.Invoke(() =>
            {
                Logger.Info("Video terminato, passo alla slide successiva");
                NextSlide();
            });
        }
    }
    
    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
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
            ApplyNumberStyle();

            this.Show();
            Application.DoEvents();

            UpdateLayout();
            LoadMedia();
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
            Logger.Info("LoadSettingsAsync: Chiamata Database.GetSettingsAsync...");
            _settings = await Database.GetSettingsAsync();
            Logger.Info("LoadSettingsAsync: Database.GetSettingsAsync completato");
            
            if (_settings.LayoutLeftPct <= 0 && _settings.LayoutRightPct <= 0)
            {
                _settings.LayoutLeftPct = 75;
                _settings.LayoutRightPct = 25;
            }
            
            if (string.IsNullOrEmpty(_settings.WindowMode))
                _settings.WindowMode = "borderless";
            
            _pollTimer.Interval = Math.Max(100, _settings.PollMs > 0 ? _settings.PollMs : 1000);
            _slideshowTimer.Interval = Math.Max(1000, _settings.SlideshowIntervalMs);
            
            Logger.Info($"Settings caricati: Layout {_settings.LayoutLeftPct}/{_settings.LayoutRightPct}, Window: {_settings.WindowMode}");
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
                    _settings = newSettings;
                    _pollTimer.Interval = Math.Max(100, _settings.PollMs);
                    _slideshowTimer.Interval = Math.Max(1000, _settings.SlideshowIntervalMs);
                    
                    this.Invoke(() =>
                    {
                        ApplyNumberStyle();
                        UpdateLayout();
                        LoadMedia();
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
            this.Bounds = _targetScreen.Bounds;
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
            int leftPct = _settings.LayoutLeftPct > 0 ? _settings.LayoutLeftPct : 75;
            leftPct = Math.Clamp(leftPct, 0, 100);

            int leftWidth = (int)(this.ClientSize.Width * leftPct / 100.0);
            Logger.Info($"UpdateLayout: Layout {leftPct}/{(100-leftPct)}, LeftWidth={leftWidth}");

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
    
    private void LoadMedia()
    {
        try
        {
            Logger.Info("LoadMedia: Inizio");
            _slideshowTimer.Stop();
            _videoView.Visible = false;
            _pictureBox.Visible = true;

            // Ferma il video se in riproduzione
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            Logger.Info($"LoadMedia: Caricamento media '{_settings.MediaPath}' tipo '{_settings.MediaType}'");
            _pictureBox.Image?.Dispose();
            _pictureBox.Image = null;
            
            if (string.IsNullOrEmpty(_settings.MediaPath))
            {
                _pictureBox.BackColor = Color.Black;
                return;
            }
            
            // Imposta modalità di visualizzazione
            _pictureBox.SizeMode = _settings.MediaFit?.ToLower() == "contain" 
                ? PictureBoxSizeMode.Zoom 
                : PictureBoxSizeMode.StretchImage;
            
            // Modalità cartella (slideshow)
            if (_settings.MediaFolderMode && Directory.Exists(_settings.MediaPath))
            {
                LoadSlideshow();
                return;
            }
            
            // File singolo
            if (!File.Exists(_settings.MediaPath))
            {
                _pictureBox.BackColor = Color.Black;
                return;
            }
            
            // File singolo = non è slideshow
            _isPlayingVideoInSlideshow = false;
            
            var ext = Path.GetExtension(_settings.MediaPath).ToLower();
            
            if (ext == ".gif" || _settings.MediaType == "gif")
            {
                _pictureBox.Image = Image.FromFile(_settings.MediaPath);
            }
            else if (IsVideoFile(ext) || _settings.MediaType == "video")
            {
                // Riproduci video con LibVLC (loop infinito per file singolo)
                LoadVideo(_settings.MediaPath);
            }
            else
            {
                _pictureBox.Image = Image.FromFile(_settings.MediaPath);
            }
            
            Logger.Info($"Media caricato: {_settings.MediaPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento media: {ex.Message}");
            _pictureBox.BackColor = Color.FromArgb(30, 30, 40);
        }
    }
    
    private void LoadSlideshow()
    {
        try
        {
            // Include sia immagini che video
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var videoExtensions = new[] { ".mp4", ".avi", ".wmv", ".webm", ".mkv", ".mov" };
            var allExtensions = imageExtensions.Concat(videoExtensions).ToArray();
            
            _slideshowFiles = Directory.GetFiles(_settings.MediaPath)
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToArray();
            
            if (_slideshowFiles.Length == 0)
            {
                Logger.Warn($"Nessun file media trovato in: {_settings.MediaPath}");
                _pictureBox.BackColor = Color.Black;
                return;
            }
            
            _currentSlideIndex = 0;
            ShowCurrentSlide();
            
            if (_slideshowFiles.Length > 1)
            {
                _slideshowTimer.Interval = Math.Max(1000, _settings.SlideshowIntervalMs);
                _slideshowTimer.Start();
            }
            
            Logger.Info($"Slideshow avviato: {_slideshowFiles.Length} file (immagini e video), intervallo {_settings.SlideshowIntervalMs}ms");
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
            
            // Ferma video precedente se in riproduzione
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            
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
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer?.Dispose();
            _slideshowTimer?.Dispose();
            _pictureBox?.Image?.Dispose();
            _numberLabel?.Font?.Dispose();
            _textLabel?.Font?.Dispose();
            
            // Pulisci LibVLC
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }
        base.Dispose(disposing);
    }
}
