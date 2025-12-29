using DBNext.Shared;

namespace DBNext;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Inizializza logger e config
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        Logger.Initialize(basePath);
        Config.Load(basePath);
        
        Logger.Info("=== DB-Next avviato ===");
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        
        // Test connessione e inizializza DB (sincrono per mantenere STA thread)
        if (!Database.TestConnectionAsync().GetAwaiter().GetResult())
        {
            MessageBox.Show(
                "Impossibile connettersi al database MySQL.\n\n" +
                "Verifica che:\n" +
                "1. MySQL sia in esecuzione\n" +
                "2. Il file config.ini sia configurato correttamente\n" +
                "3. Il database esista",
                "DB-Next - Errore Connessione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        
        Database.InitializeAsync().GetAwaiter().GetResult();

        // Carica settings
        Logger.Info("Caricamento settings...");
        QueueSettings settings;
        try
        {
            settings = Database.GetSettingsAsync().GetAwaiter().GetResult();
            Logger.Info($"Settings caricati: ScreenMode={settings.ScreenMode}, WindowMode={settings.WindowMode}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento settings: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            MessageBox.Show($"Errore caricamento impostazioni: {ex.Message}",
                "DB-Next - Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MainForm? mainForm = null;
        
        // Avvia finestre in base alla modalità
        switch (settings.ScreenMode.ToLower())
        {
            case "mirror":
                // Una finestra per ogni monitor esclusi quelli nella lista di esclusione
                var excludedDisplays = ParseDisplayList(settings.MirrorExcludeDisplays);
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    var screen = Screen.AllScreens[i];
                    // Salta i monitor nella lista di esclusione
                    if (excludedDisplays.Contains(i))
                        continue;

                    var form = new MainForm(screen, settings, i);
                    if (mainForm == null) mainForm = form;
                    form.Show();
                }
                break;
                
            case "multi":
                // Finestre solo sui monitor specificati
                var indices = ParseDisplayList(settings.MultiDisplayList);
                foreach (var idx in indices)
                {
                    if (idx >= 0 && idx < Screen.AllScreens.Length)
                    {
                        var form = new MainForm(Screen.AllScreens[idx], settings, idx);
                        if (mainForm == null) mainForm = form;
                        form.Show();
                    }
                }
                break;
                
            case "single":
            default:
                // Una finestra sul monitor selezionato
                Logger.Info("Modalità single screen");
                var targetIdx = settings.TargetDisplayIndex;
                if (targetIdx < 0 || targetIdx >= Screen.AllScreens.Length)
                {
                    Logger.Warn($"Monitor {targetIdx} non valido, uso monitor 0");
                    targetIdx = 0;
                }
                var selectedScreen = Screen.AllScreens[targetIdx];
                Logger.Info($"Creazione MainForm su schermo {targetIdx}: {selectedScreen.Bounds.Width}x{selectedScreen.Bounds.Height} at ({selectedScreen.Bounds.X},{selectedScreen.Bounds.Y})");
                try
                {
                    mainForm = new MainForm(selectedScreen, settings, targetIdx);
                    Logger.Info("MainForm creato");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Errore creazione MainForm: {ex.Message}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                    MessageBox.Show($"Errore creazione finestra principale: {ex.Message}",
                        "DB-Next - Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                break;
        }
        
        if (mainForm != null)
        {
            Logger.Info("Avvio Application.Run...");
            try
            {
                Application.Run(mainForm);
                Logger.Info("Application.Run completato");
            }
            catch (Exception ex)
            {
                Logger.Error($"Errore in Application.Run: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Errore esecuzione applicazione: {ex.Message}",
                    "DB-Next - Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            Logger.Error("mainForm è null!");
            MessageBox.Show("Errore: finestra principale non creata",
                "DB-Next - Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    static int[] ParseDisplayList(string list)
    {
        return list.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
            .Where(n => n >= 0)
            .ToArray();
    }
}

