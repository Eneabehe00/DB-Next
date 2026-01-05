using DBNext.Shared;

class Program
{
    static void Main()
    {
        // Test 1: Configurazione come master (localhost)
        Config.SetConnectionParameters("localhost", 3306, "db_next", "root", "");
        Console.WriteLine("=== TEST MASTER (localhost) ===");
        Console.WriteLine($"IsSlave: {IsSlave()}");
        Console.WriteLine($"Master Address: {GetMasterAddress()}");
        Console.WriteLine($"Transformed Path: {TransformMediaPathForSlave(@"\\CS1200-1\Pubblicità")}");

        // Test 2: Configurazione come slave (IP address)
        Config.SetConnectionParameters("192.168.1.100", 3306, "db_next", "root", "");
        Console.WriteLine("\n=== TEST SLAVE (192.168.1.100) ===");
        Console.WriteLine($"IsSlave: {IsSlave()}");
        Console.WriteLine($"Master Address: {GetMasterAddress()}");
        Console.WriteLine($"Transformed Path: {TransformMediaPathForSlave(@"\\CS1200-1\Pubblicità")}");

        // Test 3: Path locale (non UNC)
        Console.WriteLine("\n=== TEST PATH LOCALE ===");
        Console.WriteLine($"Transformed Path: {TransformMediaPathForSlave(@"C:\Media\Pubblicità")}");
    }

    static bool IsSlave()
    {
        return Config.Server != "localhost" && Config.Server != "127.0.0.1";
    }

    static string GetMasterAddress()
    {
        return Config.Server;
    }

    static string TransformMediaPathForSlave(string mediaPath)
    {
        if (!IsSlave() || string.IsNullOrEmpty(mediaPath))
            return mediaPath;

        try
        {
            // Controlla se è un percorso UNC (inizia con \\)
            if (mediaPath.StartsWith("\\\\"))
            {
                // Estrai il nome del server dal percorso UNC (prima parte dopo \\)
                var firstBackslash = mediaPath.IndexOf('\\', 2);
                if (firstBackslash > 2)
                {
                    var oldServerName = mediaPath.Substring(2, firstBackslash - 2);
                    var remainingPath = mediaPath.Substring(firstBackslash);

                    // Sostituisci con l'indirizzo del master
                    var masterAddress = GetMasterAddress();
                    var transformedPath = $"\\\\{masterAddress}{remainingPath}";

                    Console.WriteLine($"Percorso media trasformato per slave: {mediaPath} -> {transformedPath}");
                    return transformedPath;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore trasformazione percorso media: {ex.Message}");
        }

        // Se non è un percorso UNC o errore, restituisci il percorso originale
        return mediaPath;
    }
}
