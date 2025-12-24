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
    
    public static string ConnectionString => 
        $"Server={Server};Port={Port};Database={Database};User={User};Password={Password};";
    
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

