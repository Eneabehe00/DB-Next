namespace DBNext.Shared;

/// <summary>
/// Stato corrente della coda (numero visualizzato)
/// </summary>
public class QueueState
{
    public int Id { get; set; } = 1;
    public int CurrentNumber { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Impostazioni di configurazione del display
/// </summary>
public class QueueSettings
{
    public int Id { get; set; } = 1;
    
    // Media
    public string MediaPath { get; set; } = "";
    public string MediaType { get; set; } = "image"; // image, gif, video
    public string MediaFit { get; set; } = "cover"; // cover, contain
    public bool MediaFolderMode { get; set; } = false; // true = cartella, false = file singolo
    public int SlideshowIntervalMs { get; set; } = 5000; // Intervallo slideshow
    
    // Polling
    public int PollMs { get; set; } = 1000;
    
    // Layout (percentuali, somma = 100)
    public int LayoutLeftPct { get; set; } = 75;
    public int LayoutRightPct { get; set; } = 25;
    
    // Display
    public string ScreenMode { get; set; } = "single"; // single, mirror, multi
    public int TargetDisplayIndex { get; set; } = 0;
    public string MultiDisplayList { get; set; } = "0"; // es. "0,2"
    public string WindowMode { get; set; } = "borderless"; // fullscreen, borderless, windowed
    public int WindowWidth { get; set; } = 0; // 0 = auto
    public int WindowHeight { get; set; } = 0; // 0 = auto
    
    // Personalizzazione Numero
    public string NumberFontFamily { get; set; } = "Arial Black";
    public int NumberFontSize { get; set; } = 0; // 0 = auto
    public bool NumberFontBold { get; set; } = true;
    public string NumberColor { get; set; } = "#FFC832"; // Giallo dorato
    public string NumberBgColor { get; set; } = "#14141E"; // Scuro
    
    // Scritta sopra/sotto il numero
    public string NumberLabelText { get; set; } = ""; // es. "Ora serviamo il numero"
    public string NumberLabelColor { get; set; } = "#FFFFFF";
    public int NumberLabelSize { get; set; } = 0; // 0 = auto (responsive)
    public string NumberLabelPosition { get; set; } = "top"; // top, bottom
    public int NumberLabelOffset { get; set; } = 0; // Offset in pixel dalla posizione (positivo = verso centro)
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Log eventi modifiche numero
/// </summary>
public class QueueEvent
{
    public int Id { get; set; }
    public string Action { get; set; } = ""; // next, prev, set
    public int OldNumber { get; set; }
    public int NewNumber { get; set; }
    public string Source { get; set; } = ""; // batch, config, manual
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

