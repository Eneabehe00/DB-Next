using DBNext.Shared;

namespace DBNextCLI;

/// <summary>
/// Utility CLI per gestire il numero della coda via batch
/// Uso: DB-NextCLI.exe [next|prev|set N|get]
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        Logger.Initialize(basePath);
        Config.Load(basePath);
        
        if (args.Length == 0)
        {
            ShowHelp();
            return 1;
        }
        
        var command = args[0].ToLower();
        
        try
        {
            // Test connessione
            if (!await Database.TestConnectionAsync())
            {
                Console.Error.WriteLine("ERRORE: Impossibile connettersi al database");
                return 2;
            }
            
            await Database.InitializeAsync();
            
            switch (command)
            {
                case "next":
                    var nextNum = await Database.NextNumberAsync("batch");
                    Console.WriteLine(nextNum.ToString("00"));
                    return 0;
                    
                case "prev":
                    var prevNum = await Database.PrevNumberAsync("batch");
                    Console.WriteLine(prevNum.ToString("00"));
                    return 0;
                    
                case "set":
                    if (args.Length < 2 || !int.TryParse(args[1], out var setNum))
                    {
                        Console.Error.WriteLine("ERRORE: Specificare un numero valido (0-99)");
                        return 1;
                    }
                    setNum = Math.Clamp(setNum, 0, 99);
                    await Database.SetNumberAsync(setNum, "batch", "set");
                    Console.WriteLine(setNum.ToString("00"));
                    return 0;
                    
                case "get":
                    var state = await Database.GetStateAsync();
                    Console.WriteLine(state.CurrentNumber.ToString("00"));
                    return 0;
                    
                case "reset":
                    await Database.SetNumberAsync(0, "batch", "reset");
                    Console.WriteLine("00");
                    return 0;
                    
                default:
                    Console.Error.WriteLine($"ERRORE: Comando sconosciuto '{command}'");
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERRORE: {ex.Message}");
            Logger.Error($"CLI error: {ex.Message}");
            return 3;
        }
    }
    
    static void ShowHelp()
    {
        Console.WriteLine(@"
DB-Next CLI - Gestione numero coda

Uso: DB-NextCLI.exe <comando> [parametri]

Comandi:
  next        Incrementa il numero (99 -> 0)
  prev        Decrementa il numero (0 -> 99)
  set <N>     Imposta il numero a N (0-99)
  get         Mostra il numero corrente
  reset       Reimposta a 0

Esempi:
  DB-NextCLI.exe next
  DB-NextCLI.exe set 42
  DB-NextCLI.exe get
");
    }
}

