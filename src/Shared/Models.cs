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
    public string MirrorExcludeDisplays { get; set; } = "0"; // Monitor da escludere in modalità mirror, es. "0" (touch screen)
    public string WindowMode { get; set; } = "borderless"; // fullscreen, borderless, windowed
    public int WindowWidth { get; set; } = 0; // 0 = auto
    public int WindowHeight { get; set; } = 0; // 0 = auto
    public int WindowMarginTop { get; set; } = 0; // Margine superiore in pixel (per banner/overlay)
    
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

    // Finestra Operatore
    public bool OperatorWindowEnabled { get; set; } = false; // Abilita/disabilita finestra operatore
    public int OperatorWindowX { get; set; } = 50; // Posizione X della finestra operatore
    public int OperatorWindowY { get; set; } = 50; // Posizione Y della finestra operatore
    public int OperatorWindowWidth { get; set; } = 200; // Larghezza finestra operatore
    public int OperatorWindowHeight { get; set; } = 80; // Altezza finestra operatore
    public int OperatorMonitorIndex { get; set; } = 0; // Indice monitor per finestra operatore
    public string OperatorBgColor { get; set; } = "#000000"; // Sfondo finestra operatore
    public string OperatorTextColor { get; set; } = "#FFFFFF"; // Colore testo finestra operatore
    public string OperatorFontFamily { get; set; } = "Arial Black"; // Font finestra operatore
    public int OperatorFontSize { get; set; } = 36; // Dimensione font finestra operatore
    public bool OperatorAlwaysOnTop { get; set; } = true; // Finestra sempre in primo piano
    public string OperatorLabelText { get; set; } = "TURNO"; // Testo etichetta operatore

    // Scheduler Media per Operatore
    public bool MediaSchedulerEnabled { get; set; } = false; // Abilita/disabilita scheduler media
    public DateTime MediaSchedulerStartDate { get; set; } = DateTime.Today; // Data inizio schedule
    public DateTime MediaSchedulerEndDate { get; set; } = DateTime.Today.AddDays(1); // Data fine schedule
    public string MediaSchedulerPath { get; set; } = ""; // Percorso cartella per schedule
    public string MediaSchedulerType { get; set; } = "image"; // Tipo media per schedule
    public string MediaSchedulerFit { get; set; } = "cover"; // Adattamento per schedule
    public bool MediaSchedulerFolderMode { get; set; } = true; // Modalità cartella per schedule
    public int MediaSchedulerIntervalMs { get; set; } = 5000; // Intervallo slideshow per schedule

    // Barra Informativa
    public bool InfoBarEnabled { get; set; } = false; // Abilita/disabilita barra informativa
    public string MirrorInfoBarDisplays { get; set; } = ""; // Monitor che mostrano la barra in modalità mirror, es. "1,2" (vuoto = tutti)
    public string MirrorMarginTops { get; set; } = "0"; // Margini superiori per monitor in modalità mirror, es. "0,50,0,0" (monitor 0,1,2,3)
    public string InfoBarBgColor { get; set; } = "#1a1a2e"; // Colore sfondo barra
    public int InfoBarHeight { get; set; } = 40; // Altezza barra in pixel
    public string InfoBarFontFamily { get; set; } = "Segoe UI"; // Font barra informativa
    public int InfoBarFontSize { get; set; } = 12; // Dimensione font barra
    public string InfoBarTextColor { get; set; } = "#ffffff"; // Colore testo barra

    // API News (NewsAPI)
    public string NewsApiKey { get; set; } = ""; // Chiave API NewsAPI
    public string NewsCountry { get; set; } = "it"; // Paese per le notizie (codice ISO)
    public int NewsUpdateIntervalMs { get; set; } = 300000; // Intervallo aggiornamento notizie (5 minuti)

    // API Meteo (OpenWeatherMap)
    public string WeatherApiKey { get; set; } = ""; // Chiave API OpenWeatherMap
    public string WeatherCity { get; set; } = "Rome"; // Città per il meteo
    public string WeatherUnits { get; set; } = "metric"; // Unità di misura (metric, imperial)
    public int WeatherUpdateIntervalMs { get; set; } = 600000; // Intervallo aggiornamento meteo (10 minuti)

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

