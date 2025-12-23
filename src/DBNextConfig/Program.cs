using DBNext.Shared;

namespace DBNextConfig;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Inizializza logger e config
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        Logger.Initialize(basePath);
        Config.Load(basePath);
        
        Logger.Info("=== DB-NextConfig avviato ===");
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        
        // Test connessione (sincrono per mantenere STA thread)
        if (!Database.TestConnectionAsync().GetAwaiter().GetResult())
        {
            MessageBox.Show(
                "Impossibile connettersi al database MySQL.\n\n" +
                "Verifica che:\n" +
                "1. MySQL sia in esecuzione\n" +
                "2. Il file config.ini sia configurato correttamente\n" +
                "3. Il database esista",
                "DB-NextConfig - Errore Connessione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        
        Database.InitializeAsync().GetAwaiter().GetResult();
        
        Application.Run(new ConfigForm());
    }
}

