namespace DBNext.Shared;

/// <summary>
/// Logger semplice su file
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    private static string _logPath = "";
    
    public static void Initialize(string basePath)
    {
        var logsDir = Path.Combine(basePath, "logs");
        Directory.CreateDirectory(logsDir);
        _logPath = Path.Combine(logsDir, "app.log");
    }
    
    public static void Log(string level, string message)
    {
        if (string.IsNullOrEmpty(_logPath)) return;
        
        try
        {
            lock (_lock)
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Ignora errori di logging
        }
    }
    
    public static void Info(string message) => Log("INFO", message);
    public static void Error(string message) => Log("ERROR", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Debug(string message) => Log("DEBUG", message);
}

