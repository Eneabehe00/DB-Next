# DB-Next 🎫

Sistema **Saltacoda** ultra-leggero per PC industriali (bilance).

## 📋 Caratteristiche

- **Display Saltacoda**: Mostra media (immagine/GIF/video) a sinistra e numero (0-99) a destra
- **Slideshow Avanzato**: Supporto slideshow con immagini, GIF e video misti
- **Layout Adattivo**: Proporzioni configurabili (default 75%/25%)
- **Multi-Monitor**: Supporto single, mirror, multi-display
- **Database MySQL**: Sincronizzazione tra bilancia master e slave
- **Controllo via Batch**: File .bat per avanzare/indietreggiare numero
- **Ultra-leggero**: WinForms .NET 8 con LibVLC per video

## 🏗️ Architettura Master/Slave

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Bilancia Master │     │  Bilancia Slave │     │  Bilancia Slave │
│  ┌───────────┐  │     │  ┌───────────┐  │     │  ┌───────────┐  │
│  │  MySQL DB │◄─┼─────┼──│ DB-Next   │  │     │  │ DB-Next   │  │
│  │  DB-Next  │  │     │  │ config.ini│  │     │  │ config.ini│  │
│  │  .bat     │  │     │  │  .bat     │  │     │  │  .bat     │  │
│  └───────────┘  │     │  └───────────┘  │     │  └───────────┘  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## 📦 Componenti

| File | Descrizione |
|------|-------------|
| `DB-Next.exe` | Applicazione principale saltacoda (fullscreen) |
| `DB-NextConfig.exe` | Configuratore grafico |
| `DB-NextCLI.exe` | Utility CLI per batch |
| `NEXT.bat` | Incrementa numero (99→0) |
| `PREV.bat` | Decrementa numero (0→99) |
| `SET.bat` | Imposta numero manualmente |
| `EDIT.bat` | Apre configuratore |
| `config.ini` | Configurazione connessione DB |

## 🚀 Installazione

### 1. Requisiti

- Windows 10/11 (x64)
- MySQL 5.x o superiore
- .NET 8 Runtime ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- VLC Media Player (per supporto video) o LibVLC libraries incluse

### 2. Setup Database (solo su Master)

```sql
-- Esegui su MySQL della bilancia master
SOURCE sql/install.sql;
```

Questo script unico gestisce sia l'installazione iniziale che l'aggiornamento di database esistenti.

Oppure crea manualmente il database:

```sql
CREATE DATABASE db_next;
USE db_next;
SOURCE sql/install.sql;
```

### 3. Configurazione

Copia `config.ini.example` in `config.ini` e modifica:

**Bilancia Master:**
```ini
server=localhost
port=3306
database=db_next
user=root
password=tua_password
```

**Bilance Slave:**
```ini
server=192.168.1.100    # IP della bilancia master
port=3306
database=db_next
user=db_next_user       # Utente con accesso remoto
password=password
```

### 4. Build

```bash
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -o ./publish
```

### 5. Deployment

Copia nella cartella di destinazione:
- `DB-Next.exe`
- `DB-NextConfig.exe`
- `DB-NextCLI.exe`
- `config.ini`
- Tutti i file `.bat`
- Cartella `logs/`

## 🖥️ Utilizzo

### Avvio Saltacoda

```bash
DB-Next.exe
```

Shortcut da tastiera:
- `ESC` - Chiudi applicazione
- `F11` - Toggle fullscreen/windowed

### Configurazione

```bash
DB-NextConfig.exe
# oppure
EDIT.bat
```

### Controllo Numero

```bash
# Da batch (per bilance)
NEXT.bat          # Prossimo cliente
PREV.bat          # Numero precedente
SET.bat 42        # Imposta a 42
SET.bat           # Chiede input

# Da CLI
DB-NextCLI.exe next
DB-NextCLI.exe prev
DB-NextCLI.exe set 42
DB-NextCLI.exe get
DB-NextCLI.exe reset
```

## ⚙️ Configurazione Display

| Parametro | Valori | Descrizione |
|-----------|--------|-------------|
| `screen_mode` | single, mirror, multi | Modalità multi-monitor |
| `target_display_index` | 0, 1, 2... | Monitor per modalità single |
| `multi_display_list` | "0,2" | Monitor per modalità multi |
| `window_mode` | borderless, fullscreen, windowed | Tipo finestra |
| `layout_left_pct` | 0-100 | % larghezza media |
| `layout_right_pct` | 0-100 | % larghezza numero |
| `media_fit` | cover, contain | Adattamento media |
| `media_folder_mode` | 0, 1 | Modalità slideshow (0=singolo file, 1=cartella) |
| `slideshow_interval_ms` | 1000-30000 | Intervallo slideshow (ms) |
| `poll_ms` | 100-10000 | Intervallo polling DB |

### Personalizzazione Numero

| Parametro | Valori | Descrizione |
|-----------|--------|-------------|
| `number_font_family` | "Arial Black", ... | Font del numero |
| `number_font_size` | 0-500 | Dimensione font (0=auto) |
| `number_font_bold` | 0, 1 | Grassetto |
| `number_color` | "#FFC832" | Colore numero (hex) |
| `number_bg_color` | "#14141E" | Colore sfondo (hex) |

### Scritta Sopra/Sotto il Numero

| Parametro | Valori | Descrizione |
|-----------|--------|-------------|
| `number_label_text` | "Ora serviamo..." | Testo da mostrare |
| `number_label_color` | "#FFFFFF" | Colore testo (hex) |
| `number_label_size` | 0-100 | Dimensione font (0=auto responsive) |
| `number_label_position` | top, bottom | Posizione: sopra o sotto il numero |
| `number_label_offset` | -100 a 100 | Offset in pixel dalla posizione |

## 🎞️ Slideshow e Supporto Video

DB-Next supporta slideshow avanzati con immagini, GIF e video misti utilizzando **LibVLC**.

### Modalità Slideshow

Per abilitare lo slideshow di una cartella:

```sql
UPDATE queue_settings SET
    media_path = 'C:\percorso\alla\cartella',
    media_folder_mode = 1,
    slideshow_interval_ms = 5000
WHERE id = 1;
```

Il sistema:
- Carica automaticamente tutti i file immagine/video dalla cartella
- Mostra ciascun file per l'intervallo specificato
- Supporta video con loop singolo o playback normale
- Passa automaticamente alla slide successiva quando un video termina

### Supporto Video

- **Formati supportati**: MP4, WMV, WebM, AVI, MKV, MOV
- **Playback**: Automatico con LibVLC (VLC libraries incluse)
- **Loop**: I video possono essere riprodotti in loop o una sola volta
- **Fallback**: Se VLC non disponibile, mostra placeholder

### Configurazione Slideshow

```sql
-- Esempio configurazione slideshow
UPDATE queue_settings SET
    media_path = 'C:\immagini\promo',
    media_folder_mode = 1,        -- Abilita modalità cartella
    slideshow_interval_ms = 8000,  -- 8 secondi per slide
    media_fit = 'contain'          -- Adattamento immagini
WHERE id = 1;
```

**Esempio:** Per mostrare "Ora serviamo il numero" sopra con offset di 20px:
```sql
UPDATE queue_settings SET 
    number_label_text = 'Ora serviamo il numero',
    number_label_position = 'top',
    number_label_offset = 20
WHERE id = 1;
```

### Modalità Schermo

- **single**: Una finestra sul monitor selezionato
- **mirror**: Una finestra su ogni monitor disponibile
- **multi**: Finestre solo sui monitor specificati in `multi_display_list`

## 📊 Database Schema

```sql
-- Stato corrente
queue_state (
    id INT PRIMARY KEY,      -- Sempre 1
    current_number INT,      -- 0-99
    updated_at DATETIME
)

-- Configurazione
queue_settings (
    id INT PRIMARY KEY,      -- Sempre 1

    -- Media
    media_path VARCHAR(500), -- Percorso file immagine/video
    media_type VARCHAR(20),  -- image, gif, video
    media_fit VARCHAR(20),   -- cover, contain
    media_folder_mode TINYINT(1), -- Modalità cartella (slideshow)
    slideshow_interval_ms INT, -- Intervallo slideshow

    -- Layout e Display
    poll_ms INT,             -- Intervallo polling DB
    layout_left_pct INT,     -- % larghezza sinistra (media)
    layout_right_pct INT,    -- % larghezza destra (numero)
    screen_mode VARCHAR(20), -- single, mirror, multi
    target_display_index INT,-- Monitor per modalità single
    multi_display_list VARCHAR(50), -- Monitor per modalità multi
    window_mode VARCHAR(20), -- fullscreen, borderless, windowed

    -- Personalizzazione Numero
    number_font_family VARCHAR(100), -- Font del numero
    number_font_size INT,   -- Dimensione font (0=auto)
    number_font_bold TINYINT(1), -- Grassetto
    number_color VARCHAR(20), -- Colore numero (hex)
    number_bg_color VARCHAR(20), -- Colore sfondo (hex)

    -- Scritta sopra/sotto il numero
    number_label_text VARCHAR(200), -- Testo etichetta
    number_label_color VARCHAR(20), -- Colore etichetta (hex)
    number_label_size INT,  -- Dimensione font etichetta (0=auto)
    number_label_position VARCHAR(20), -- top, bottom
    number_label_offset INT,-- Offset in pixel

    updated_at DATETIME
)

-- Log eventi
queue_events (
    id INT AUTO_INCREMENT,
    action VARCHAR(20),      -- next, prev, set, reset
    old_number INT,
    new_number INT,
    source VARCHAR(50),      -- batch, config, manual
    ts DATETIME
)
```

## 🔒 Setup MySQL per Slave

Sulla bilancia master, crea un utente per accesso remoto:

```sql
CREATE USER 'db_next_user'@'%' IDENTIFIED BY 'password_sicura';
GRANT SELECT, INSERT, UPDATE ON db_next.* TO 'db_next_user'@'%';
FLUSH PRIVILEGES;
```

Assicurati che MySQL ascolti su tutte le interfacce:
```ini
# my.ini o my.cnf
bind-address = 0.0.0.0
```

## 📝 Logging

I log sono salvati in `logs/app.log`:

```
2024-01-15 10:30:00 [INFO] === DB-Next avviato ===
2024-01-15 10:30:01 [INFO] Database inizializzato
2024-01-15 10:30:15 [INFO] Numero cambiato: 05 -> 06 (next da batch)
```

## 🐛 Troubleshooting

### "Impossibile connettersi al database"

1. Verifica che MySQL sia in esecuzione
2. Controlla `config.ini` (server, porta, credenziali)
3. Per slave: verifica connettività di rete verso master

### Il numero non si aggiorna

1. Verifica `poll_ms` nel configuratore
2. Controlla i log in `logs/app.log`
3. Verifica che il DB sia raggiungibile

### Video non funziona

Il supporto video utilizza **LibVLC** (VLC Media Player libraries).
- Supporta automaticamente MP4, WMV, WebM, AVI, MKV, MOV
- Le librerie VLC sono incluse nel deployment
- Se VLC non è disponibile, viene mostrato un placeholder

### Aggiornamento da versioni precedenti

Per aggiornare un database esistente alle nuove funzionalità:

```sql
-- Connetti al database esistente
mysql -u user -pdibal sys_datos

-- Esegui lo script di installazione (aggiorna automaticamente)
SOURCE sql/install.sql;
```

Lo script rileva automaticamente le colonne mancanti e le aggiunge senza perdere dati esistenti.

## 📄 Licenza

MIT License

## 🤝 Supporto

Per problemi o richieste, aprire una issue su GitHub.

