# DB-Next - Info Rapide per Debug con ChatGPT

## 🎯 Problema e Soluzione

### Crash Rilevato
**Errore**: `ArgumentException: Parameter is not valid`  
**Dove**: `Graphics.MeasureString()` in `MainForm.cs:482`  
**Quando**: All'avvio dell'app, durante inizializzazione finestra

### Soluzione Applicata
Rimosso blocco `CreateGraphics().MeasureString()` che crashava perché chiamato prima che il pannello fosse completamente inizializzato.

**Fix**: Usa `AutoEllipsis = true` invece di misurazione manuale → Windows Forms tronca automaticamente il testo.

---

## 📁 Struttura Progetto

```
DB-Next/
├── src/
│   ├── DBNext/             # App principale (WinForms)
│   │   ├── Program.cs      # Entry point
│   │   └── MainForm.cs     # ⚠️ Form - QUI ERA IL BUG (RISOLTO)
│   ├── DBNextConfig/       # Configuratore GUI
│   ├── DBNextCLI/          # CLI tools
│   └── Shared/             # Libreria comune
│       ├── Database.cs     # MySQL access
│       ├── Logger.cs       # File logging
│       ├── Config.cs       # config.ini parser
│       └── Models.cs       # Data models
├── Deployment/             # Build pronto all'uso
│   ├── DB-Next.exe         # ✅ FIXATO
│   ├── config.ini
│   └── logs/app.log        # ⚠️ Controlla qui per errori
└── sql/install.sql         # DB schema
```

---

## 🔧 Stack Tecnologico

- **.NET**: 8.0 Windows
- **UI**: Windows Forms
- **DB**: MySQL (MySqlConnector)
- **Video**: LibVLCSharp + LibVLC
- **OS**: Windows 10/11

---

## 📝 File Importanti

### 1. `src/DBNext/MainForm.cs` (MODIFICATO - RISOLTO)
Form principale dell'applicazione con display split:
- **Sinistra**: Media (immagini/video/slideshow)
- **Destra**: Numero grande + label opzionale

**Fix applicato** (linea ~428-450):
```csharp
// PRIMA (crashava):
using (var g = _numberPanel.CreateGraphics()) { 
    var textSize = g.MeasureString(...); // ❌ CRASH
}

// DOPO (OK):
_textLabel.AutoEllipsis = true;  // ✅ Windows tronca auto
```

### 2. `Deployment/logs/app.log`
Log completo. Cerca errori tipo:
```
[ERROR] UpdateLayout: ERRORE - Parameter is not valid.
```

### 3. `config.ini`
Configurazione MySQL:
```ini
server=localhost
port=3306
database=db_next
user=root
password=
```

---

## 🐛 Come Debuggare Ulteriormente

### Se crasha ancora:

1. **Controlla log**:
   ```bash
   type Deployment\logs\app.log
   ```

2. **Verifica DB**:
   ```sql
   SELECT * FROM queue_settings WHERE id=1;
   ```

3. **Disabilita label temporaneamente**:
   ```sql
   UPDATE queue_settings SET number_label_text = '' WHERE id = 1;
   ```

4. **Testa in modalità Debug**:
   ```bash
   dotnet run --project src/DBNext/DBNext.csproj
   ```

---

## 🎨 Requisiti Utente

1. ✅ **Finestra fullscreen senza bordi** - IMPLEMENTATO
2. ✅ **No crash all'avvio** - RISOLTO
3. ✅ **Display su monitor specifico** - OK (configura `target_display_index` in DB)

---

## 📊 Database Schema (Importante)

### Tabella `queue_settings`
```sql
CREATE TABLE queue_settings (
    id INT PRIMARY KEY,
    
    -- Display
    window_mode VARCHAR(20),        -- "borderless", "fullscreen", "windowed"
    screen_mode VARCHAR(20),        -- "single", "mirror", "multi"
    target_display_index INT,       -- 0, 1, 2... (indice monitor)
    
    -- Layout
    layout_left_pct INT,            -- % larghezza media (es. 70)
    layout_right_pct INT,           -- % larghezza numero (es. 30)
    
    -- Numero
    number_font_size INT,           -- 0 = auto
    number_color VARCHAR(10),       -- es. "#FFC832"
    number_bg_color VARCHAR(10),    -- es. "#14141E"
    
    -- Label (⚠️ causa del crash se CreateGraphics chiamato troppo presto)
    number_label_text VARCHAR(255), -- Testo sopra/sotto numero
    number_label_color VARCHAR(10),
    number_label_size INT,          -- 0 = auto
    number_label_position VARCHAR(10), -- "top" o "bottom"
    
    -- Media
    media_path VARCHAR(500),
    media_type VARCHAR(20),         -- "image", "video", "gif"
    media_fit VARCHAR(20),          -- "cover", "contain"
    media_folder_mode BOOLEAN,      -- TRUE = slideshow cartella
    
    -- Performance
    poll_ms INT,                    -- Polling DB (default 1000)
    slideshow_interval_ms INT,      -- Intervallo slideshow (default 5000)
    
    updated_at DATETIME
);
```

---

## 🚀 Quick Commands

```bash
# Build
dotnet build -c Release

# Publish
dotnet publish src\DBNext\DBNext.csproj -c Release -o publish

# Deploy
xcopy /Y publish\*.exe Deployment\
xcopy /Y publish\*.dll Deployment\

# Run
cd Deployment
DB-Next.exe

# View logs
type Deployment\logs\app.log
```

---

## ⚠️ Problemi Comuni

### 1. Crash con "Parameter is not valid"
**Causa**: `CreateGraphics()` su controllo non inizializzato  
**Fix**: ✅ RISOLTO - rimosso il blocco problematico

### 2. Finestra non fullscreen
**Check**: `queue_settings.window_mode` deve essere `"borderless"` o `"fullscreen"`

### 3. Display su monitor sbagliato
**Check**: `queue_settings.target_display_index` (0=primo, 1=secondo, ecc.)

### 4. Errore connessione DB
**Check**: `config.ini` - verifica server, user, password

---

## 📞 Info per ChatGPT

**Quando chiedi aiuto, fornisci**:
1. Questo file completo
2. Ultime 50 righe di `Deployment/logs/app.log`
3. Eventuali messaggi di errore
4. Query: `SELECT * FROM queue_settings WHERE id=1;` (output)

**Domanda tipo**:
> "Ho un'app .NET 8 Windows Forms che [DESCRIVI PROBLEMA]. Sistema: Windows 11, multi-monitor (3 schermi). Ecco il log: [PASTE LOG]. Come risolvo?"

---

## ✅ Status Corrente

- [x] **Crash risolto** (CreateGraphics fix)
- [x] **Finestra fullscreen** implementata
- [x] **Compilazione OK** (0 errori)
- [x] **Deploy completato** in `Deployment/`
- [x] **Documentazione** creata

**Versione**: 1.0  
**Data**: 23 Dicembre 2025  
**Status**: ✅ **FUNZIONANTE**

---

## 🔗 File Correlati

- `DEBUG_CRASH_REPORT.md` - Analisi dettagliata del crash (per debug avanzato)
- `FIX_CRASH_FULLSCREEN.md` - Documentazione completa della soluzione
- `Deployment/logs/app.log` - Log runtime
- `src/DBNext/MainForm.cs` - Codice sorgente (fix applicato)

