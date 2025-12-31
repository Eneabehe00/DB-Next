namespace DBNext.Shared;

/// <summary>
/// Configurazione connessione database (da file config.ini)
/// </summary>
public static class Config
{
    public static string Server { get; private set; } = "localhost";
    public static int Port { get; private set; } = 3306;
    public static string Database { get; private set; } = "db_next";
    public static string User { get; private set; } = "root";
    public static string Password { get; private set; } = "";

    // RSS Settings
    public static int RssNewsPerCategory { get; private set; } = 1; // Numero di notizie da prendere per categoria
    public static bool RssUltimaOraEnabled { get; private set; } = true; // Abilita/disabilita categoria Ultima Ora
    public static bool RssCronacaEnabled { get; private set; } = true; // Abilita/disabilita categoria Cronaca
    public static bool RssPoliticaEnabled { get; private set; } = true; // Abilita/disabilita categoria Politica
    public static bool RssMondoEnabled { get; private set; } = true; // Abilita/disabilita categoria Mondo
    public static bool RssEconomiaEnabled { get; private set; } = true; // Abilita/disabilita categoria Economia
    public static bool RssSportEnabled { get; private set; } = true; // Abilita/disabilita categoria Sport

    public static string ConnectionString =>
        $"Server={Server};Port={Port};Database={Database};User={User};Password={Password}";
    
    /// <summary>
    /// Carica configurazione da file config.ini nella stessa cartella dell'exe
    /// </summary>
    public static void Load(string basePath)
    {
        var configPath = Path.Combine(basePath, "config.ini");
        
        if (!File.Exists(configPath))
        {
            // Crea file config di default
            CreateDefault(configPath);
            return;
        }
        
        try
        {
            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;
                
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2) continue;
                
                var key = parts[0].Trim().ToLower();
                var value = parts[1].Trim();
                
                switch (key)
                {
                    case "server": Server = value; break;
                    case "port": Port = int.TryParse(value, out var p) ? p : 3306; break;
                    case "database": Database = value; break;
                    case "user": User = value; break;
                    case "password": Password = value; break;

                    // RSS Settings
                    case "rss_news_per_category": RssNewsPerCategory = int.TryParse(value, out var n) ? Math.Max(1, n) : 1; break;
                    case "rss_ultima_ora_enabled": RssUltimaOraEnabled = bool.TryParse(value, out var b1) ? b1 : true; break;
                    case "rss_cronaca_enabled": RssCronacaEnabled = bool.TryParse(value, out var b2) ? b2 : true; break;
                    case "rss_politica_enabled": RssPoliticaEnabled = bool.TryParse(value, out var b3) ? b3 : true; break;
                    case "rss_mondo_enabled": RssMondoEnabled = bool.TryParse(value, out var b4) ? b4 : true; break;
                    case "rss_economia_enabled": RssEconomiaEnabled = bool.TryParse(value, out var b5) ? b5 : true; break;
                    case "rss_sport_enabled": RssSportEnabled = bool.TryParse(value, out var b6) ? b6 : true; break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore caricamento config.ini: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Imposta temporaneamente i parametri di connessione (per test)
    /// </summary>
    public static void SetConnectionParameters(string server, int port, string database, string user, string password)
    {
        Server = server;
        Port = port;
        Database = database;
        User = user;
        Password = password;
    }

    /// <summary>
    /// Imposta temporaneamente le impostazioni RSS
    /// </summary>
    public static void SetRssSettings(int newsPerCategory, bool ultimaOra, bool cronaca, bool politica, bool mondo, bool economia, bool sport)
    {
        RssNewsPerCategory = newsPerCategory;
        RssUltimaOraEnabled = ultimaOra;
        RssCronacaEnabled = cronaca;
        RssPoliticaEnabled = politica;
        RssMondoEnabled = mondo;
        RssEconomiaEnabled = economia;
        RssSportEnabled = sport;
    }

    /// <summary>
    /// Salva configurazione su file config.ini
    /// </summary>
    public static bool SaveToFile(string basePath)
    {
        try
        {
            var configPath = Path.Combine(basePath, "config.ini");
            var content = $@"# DB-Next Configuration
# Configurazione connessione MySQL

server={Server}
port={Port}
database={Database}
user={User}
password={Password}

# RSS News Settings
# Numero di notizie da prendere per ogni categoria RSS (default: 1)
rss_news_per_category={RssNewsPerCategory}

# Abilita/disabilita categorie RSS (true/false)
rss_ultima_ora_enabled={RssUltimaOraEnabled}
rss_cronaca_enabled={RssCronacaEnabled}
rss_politica_enabled={RssPoliticaEnabled}
rss_mondo_enabled={RssMondoEnabled}
rss_economia_enabled={RssEconomiaEnabled}
rss_sport_enabled={RssSportEnabled}
";
            File.WriteAllText(configPath, content);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore salvataggio config.ini: {ex.Message}");
            return false;
        }
    }

    private static void CreateDefault(string path)
    {
        var content = @"# DB-Next Configuration
# Configurazione connessione MySQL

server=localhost
port=3306
database=db_next
user=root
password=

# RSS News Settings
# Numero di notizie da prendere per ogni categoria RSS (default: 1)
rss_news_per_category=1

# Abilita/disabilita categorie RSS (true/false)
rss_ultima_ora_enabled=true
rss_cronaca_enabled=true
rss_politica_enabled=true
rss_mondo_enabled=true
rss_economia_enabled=true
rss_sport_enabled=true
";
        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore creazione config.ini: {ex.Message}");
        }
    }
}

