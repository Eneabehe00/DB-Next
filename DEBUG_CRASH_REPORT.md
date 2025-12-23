# DB-Next - Crash Report & Debug Information

## 🚨 Problema Rilevato

**Sintomo**: L'applicazione `DB-Next.exe` crasha immediatamente all'avvio
**Causa Principale**: Errore "Parameter is not valid" in `Graphics.MeasureString()`

---

## 📋 Analisi dei Log

### Errore Esatto dai Log (Deployment/logs/app.log)

```
2025-12-23 11:33:55 [ERROR] UpdateLayout: ERRORE - Parameter is not valid.
2025-12-23 11:33:55 [ERROR] Stack trace:    
  at System.Drawing.Graphics.MeasureString(String text, Font font, SizeF layoutArea, StringFormat stringFormat)
  at System.Drawing.Graphics.MeasureString(String text, Font font)
  at DBNext.MainForm.UpdateNumberAndLabelLayout() in C:\Users\eneab\Desktop\WA\DB-Next\src\DBNext\MainForm.cs:line 482
  at DBNext.MainForm.UpdateLayout() in C:\Users\eneab\Desktop\WA\DB-Next\src\DBNext\MainForm.cs:line 419
```

### Sequenza Eventi che porta al Crash

1. ✅ Applicazione si avvia correttamente
2. ✅ Database si connette con successo
3. ✅ Settings vengono caricati
4. ✅ MainForm viene creato
5. ✅ LibVLC si inizializza
6. ✅ Finestra viene configurata
7. ❌ **CRASH**: `UpdateNumberAndLabelLayout()` fallisce quando chiama `CreateGraphics().MeasureString()`

---

## 🔍 Codice Problematico

### File: `src/DBNext/MainForm.cs`

#### Metodo Problematico: `UpdateNumberAndLabelLayout()` (linea ~456-482)

```csharp
private void UpdateNumberAndLabelLayout()
{
    var panelWidth = _numberPanel.Width;
    var panelHeight = _numberPanel.Height;
    
    if (panelWidth <= 0 || panelHeight <= 0) return;
    
    bool hasLabel = !string.IsNullOrEmpty(_settings.NumberLabelText);
    // ... codice ...
    
    if (hasLabel)
    {
        // ... setup font ...
        
        // 🚨 PROBLEMA QUI (linea ~456-483):
        using (var g = _numberPanel.CreateGraphics())  // ⚠️ CreateGraphics() può restituire null o invalido
        {
            var textSize = g.MeasureString(_settings.NumberLabelText, _textLabel.Font);  // ❌ CRASH!
            // ...
        }
    }
}
```

**Problema**: `_numberPanel.CreateGraphics()` viene chiamato quando il pannello non è ancora completamente renderizzato o visibile, quindi restituisce un oggetto `Graphics` non valido.

---

## 💡 Soluzioni Proposte

### Soluzione 1: Proteggere CreateGraphics con Try-Catch

```csharp
private void UpdateNumberAndLabelLayout()
{
    var panelWidth = _numberPanel.Width;
    var panelHeight = _numberPanel.Height;
    
    if (panelWidth <= 0 || panelHeight <= 0) return;
    
    // Verifica che il pannello sia pronto
    if (!_numberPanel.IsHandleCreated || !_numberPanel.Visible)
        return;
    
    bool hasLabel = !string.IsNullOrEmpty(_settings.NumberLabelText);
    // ...
    
    if (hasLabel)
    {
        try
        {
            // Misura il testo per assicurarsi che stia nel pannello
            using (var g = _numberPanel.CreateGraphics())
            {
                if (g == null) return;  // Safety check
                
                var textSize = g.MeasureString(_settings.NumberLabelText, _textLabel.Font);
                
                // Se il testo è più largo del pannello, riduci il font
                if (textSize.Width > panelWidth - 10)
                {
                    float scaleFactor = (panelWidth - 10) / textSize.Width;
                    float newFontSize = Math.Max(8, labelFontSize * scaleFactor);
                    try
                    {
                        var oldFont2 = _textLabel.Font;
                        _textLabel.Font = new Font("Segoe UI", newFontSize, FontStyle.Regular);
                        oldFont2?.Dispose();
                        labelFontSize = newFontSize;
                        labelHeight = (int)(newFontSize * 2.0);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore MeasureString: {ex.Message}");
            // Continua senza scalare il font
        }
        
        _textLabel.Height = labelHeight;
        _textLabel.Width = panelWidth;
        // ...
    }
}
```

### Soluzione 2: Usare TextRenderer invece di Graphics.MeasureString

```csharp
private void UpdateNumberAndLabelLayout()
{
    // ... stesso codice ...
    
    if (hasLabel)
    {
        try
        {
            // Usa TextRenderer che è più affidabile per WinForms
            var textSize = TextRenderer.MeasureText(
                _settings.NumberLabelText, 
                _textLabel.Font,
                new Size(panelWidth, int.MaxValue),
                TextFormatFlags.NoPrefix
            );
            
            // Se il testo è più largo del pannello, riduci il font
            if (textSize.Width > panelWidth - 10)
            {
                float scaleFactor = (panelWidth - 10) / (float)textSize.Width;
                float newFontSize = Math.Max(8, labelFontSize * scaleFactor);
                try
                {
                    var oldFont = _textLabel.Font;
                    _textLabel.Font = new Font("Segoe UI", newFontSize, FontStyle.Regular);
                    oldFont?.Dispose();
                    labelFontSize = newFontSize;
                    labelHeight = (int)(newFontSize * 2.0);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore TextRenderer.MeasureText: {ex.Message}");
        }
    }
}
```

### Soluzione 3: Rinviare la Misurazione del Testo

```csharp
private void UpdateNumberAndLabelLayout()
{
    // ... codice esistente ...
    
    if (hasLabel)
    {
        try
        {
            var oldFont = _textLabel.Font;
            _textLabel.Font = new Font("Segoe UI", labelFontSize, FontStyle.Regular);
            oldFont?.Dispose();
        }
        catch { }
        
        // Invece di misurare subito, imposta AutoSize e MaximumSize
        _textLabel.AutoSize = false;
        _textLabel.MaximumSize = new Size(panelWidth - 10, labelHeight);
        _textLabel.Height = labelHeight;
        _textLabel.Width = panelWidth;
        _textLabel.AutoEllipsis = true;  // Tronca automaticamente se troppo lungo
        
        // NON chiamare CreateGraphics - lascia che WinForms gestisca il layout
    }
}
```

---

## 🏗️ Struttura del Progetto

```
DB-Next/
├── src/
│   ├── DBNext/                      # Applicazione principale (GUI)
│   │   ├── Program.cs              # Entry point
│   │   ├── MainForm.cs             # ⚠️ Form principale - QUI È IL PROBLEMA
│   │   └── DBNext.csproj
│   ├── DBNextConfig/               # Configuratore GUI
│   ├── DBNextCLI/                  # Tool command-line
│   └── Shared/                     # Libreria condivisa
│       ├── Database.cs             # Accesso MySQL
│       ├── Logger.cs               # Logging su file
│       ├── Config.cs               # Config.ini parser
│       └── Models.cs               # Modelli dati
├── Deployment/                     # Build compilato
│   ├── DB-Next.exe                 # ⚠️ Questo crasha
│   ├── config.ini
│   └── logs/
│       └── app.log                 # ⚠️ Log con errori
└── sql/
    └── install.sql                 # Schema database
```

---

## 🔧 Informazioni Tecniche

### Stack Tecnologico
- **Framework**: .NET 8.0 Windows
- **UI**: Windows Forms
- **Database**: MySQL (MySqlConnector)
- **Video Player**: LibVLCSharp
- **Target**: Windows 10/11

### Dipendenze (da .csproj)
```xml
<ItemGroup>
    <PackageReference Include="MySqlConnector" Version="2.x" />
    <PackageReference Include="LibVLCSharp" Version="3.x" />
    <PackageReference Include="LibVLCSharp.WinForms" Version="3.x" />
</ItemGroup>
```

### Configurazione Screen
Dal log emerge:
- Sistema multi-monitor (3 schermi rilevati)
- Screen 0: 1920x1080 at (0,0)
- Screen 1: 3200x1800 at (2400,0)
- Screen 2: 2400x1350 at (-2400,0)
- Target: Screen 1 (configurazione attuale)

---

## 📊 Flow di Inizializzazione

```
Program.Main()
  ├── Logger.Initialize()
  ├── Config.Load()
  ├── Database.TestConnectionAsync() ✅
  ├── Database.InitializeAsync() ✅
  ├── Database.GetSettingsAsync() ✅
  └── new MainForm(targetScreen)
      └── MainForm Constructor
          ├── this.Bounds = targetScreen.Bounds  ✅
          ├── Setup Panels ✅
          ├── Setup Controls ✅
          └── Eventi:
              └── Load += MainForm_Load
                  ├── LibVLC.Initialize() ✅
                  ├── LoadSettingsAsync() ✅
                  ├── ApplyNumberStyle()
                  │   └── UpdateNumberAndLabelLayout() ⚠️
                  ├── this.Show()
                  ├── UpdateLayout()
                  │   └── UpdateNumberAndLabelLayout() ❌ CRASH QUI
                  └── LoadMedia()
```

---

## 🔎 Settings Caricati (dal log)

```ini
ScreenMode = single
WindowMode = borderless
LayoutLeftPct = 64
LayoutRightPct = 36
NumberLabelText = (probabilmente NON vuoto, causa del problema)
NumberLabelPosition = top
NumberLabelOffset = 0
MediaPath = C:\Users\eneab\Desktop\Negozi\Palermo\db-next\0417(1).mp4
MediaType = video
```

---

## 🐛 Perché Crasha

1. Il metodo `UpdateLayout()` viene chiamato **prima che la finestra sia completamente visualizzata**
2. Viene chiamato `UpdateNumberAndLabelLayout()`
3. C'è una label con testo (`NumberLabelText` non vuoto)
4. Il codice cerca di misurare il testo con `CreateGraphics().MeasureString()`
5. **Il controllo `_numberPanel` non ha ancora un handle Windows valido** perché:
   - La finestra non è ancora stata mostrata (`this.Show()` viene chiamato DOPO)
   - O il pannello ha dimensioni 0 o non è ancora renderizzato
6. `CreateGraphics()` restituisce un oggetto Graphics non valido
7. `MeasureString()` lancia `ArgumentException: Parameter is not valid`

---

## ✅ Fix Raccomandato (più semplice e sicuro)

### Modifica a `src/DBNext/MainForm.cs`

**Opzione A**: Rimuovi completamente la misurazione dinamica del testo (più semplice)

```csharp
private void UpdateNumberAndLabelLayout()
{
    var panelWidth = _numberPanel.Width;
    var panelHeight = _numberPanel.Height;
    
    if (panelWidth <= 0 || panelHeight <= 0) return;
    
    bool hasLabel = !string.IsNullOrEmpty(_settings.NumberLabelText);
    bool labelOnTop = _settings.NumberLabelPosition?.ToLower() != "bottom";
    int offset = _settings.NumberLabelOffset;
    
    // Calcola dimensione font della scritta (responsive)
    float labelFontSize;
    if (_settings.NumberLabelSize > 0)
    {
        labelFontSize = _settings.NumberLabelSize;
    }
    else
    {
        // Auto: dimensione proporzionale al pannello
        labelFontSize = Math.Max(10, Math.Min(panelHeight * 0.06f, panelWidth * 0.045f));
    }
    
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
        
        _textLabel.Height = labelHeight;
        _textLabel.Width = panelWidth;
        _textLabel.Left = 0;
        _textLabel.Visible = true;
        _textLabel.AutoEllipsis = true; // Tronca automaticamente
        
        if (labelOnTop)
        {
            _textLabel.Top = Math.Max(0, offset);
            _textLabel.TextAlign = ContentAlignment.MiddleCenter;
        }
        else
        {
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
    int labelSpace = hasLabel ? labelHeight + Math.Abs(offset) + 5 : 0;
    int availableHeight = panelHeight - labelSpace;
    int numberTop = 0;
    
    if (hasLabel)
    {
        if (labelOnTop)
        {
            numberTop = labelHeight + Math.Max(0, offset) + 5;
            availableHeight = panelHeight - numberTop;
        }
        else
        {
            numberTop = 0;
            availableHeight = panelHeight - labelHeight - Math.Max(0, offset) - 5;
        }
    }
    
    // Posiziona la label del numero
    _numberLabel.Left = 0;
    _numberLabel.Top = numberTop;
    _numberLabel.Width = panelWidth;
    _numberLabel.Height = Math.Max(50, availableHeight);
    
    // Calcola dimensione font del numero
    float fontSize;
    if (_settings.NumberFontSize > 0)
    {
        fontSize = _settings.NumberFontSize;
    }
    else
    {
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
```

**Cosa è stato rimosso**:
- Tutto il blocco `using (var g = _numberPanel.CreateGraphics())` 
- La misurazione dinamica del testo che causava il crash
- Affidamento a `AutoEllipsis = true` per troncare testo troppo lungo

---

## 🧪 Test Suggeriti

### 1. Test Base
```bash
cd Deployment
DB-Next.exe
```

### 2. Test con Log Verboso
Aggiungi più logging nel metodo problematico per vedere esattamente dove fallisce:

```csharp
private void UpdateNumberAndLabelLayout()
{
    Logger.Info($"UpdateNumberAndLabelLayout: Start - Panel {_numberPanel.Width}x{_numberPanel.Height}");
    
    var panelWidth = _numberPanel.Width;
    var panelHeight = _numberPanel.Height;
    
    if (panelWidth <= 0 || panelHeight <= 0)
    {
        Logger.Info("UpdateNumberAndLabelLayout: Skip - invalid dimensions");
        return;
    }
    
    Logger.Info($"UpdateNumberAndLabelLayout: HasLabel={!string.IsNullOrEmpty(_settings.NumberLabelText)}");
    
    // ... resto del codice ...
}
```

### 3. Test Fallback
Se il problema persiste, prova a disabilitare temporaneamente la label:

```sql
UPDATE queue_settings SET number_label_text = '' WHERE id = 1;
```

---

## 📝 Checklist Debug

- [x] Problema identificato: `CreateGraphics().MeasureString()` su pannello non inizializzato
- [x] Location nel codice: `MainForm.cs:482`
- [x] Log analizzati: `Deployment/logs/app.log`
- [ ] Fix implementato
- [ ] Build testato
- [ ] Crash risolto

---

## 🎯 Conclusione

**Il crash è causato da una chiamata prematura a `CreateGraphics()` su un controllo Windows Forms non ancora completamente inizializzato.**

**Soluzione più semplice**: Rimuovere la misurazione dinamica del testo e affidarsi alle proprietà built-in di WinForms (`AutoEllipsis`, `MaximumSize`).

**Soluzione alternativa**: Aggiungere controlli `IsHandleCreated` e try-catch, ma è più fragile.

---

## 📎 File da Modificare

1. **`src/DBNext/MainForm.cs`** - Metodo `UpdateNumberAndLabelLayout()` (linee 418-562 circa)

---

## 🆘 Informazioni Aggiuntive per ChatGPT

Se chiedi aiuto a ChatGPT, fornisci:

1. Questo file completo
2. Il contenuto completo di `src/DBNext/MainForm.cs`
3. Gli ultimi 50 righe di `Deployment/logs/app.log`
4. Sistema operativo: Windows 10/11
5. .NET Version: 8.0
6. Numero monitor: 3 (multi-monitor setup)

**Domanda da fare**: "Ho un'app Windows Forms che crasha con 'Parameter is not valid' quando chiama `Graphics.MeasureString()` su un pannello durante l'inizializzazione. Come posso risolvere in modo sicuro? Ecco i dettagli..."

