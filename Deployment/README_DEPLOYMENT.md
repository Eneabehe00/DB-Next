# 🚀 Deployment DB-Next sulla Bilancia Slave

Questa cartella contiene tutti i file necessari per installare DB-Next su una bilancia slave.

## 📁 Contenuto

- `DB-Next.exe` - Applicazione principale saltacoda (fullscreen)
- `DB-NextConfig.exe` - Configuratore grafico delle impostazioni
- `DB-NextCLI.exe` - Utility CLI per comandi batch
- `config.ini` - Configurazione connessione database (modificato per slave)
- `NEXT.bat` - Incrementa numero (99→0)
- `PREV.bat` - Decrementa numero (0→99)
- `SET.bat` - Imposta numero specifico
- `EDIT.bat` - Apre il configuratore
- `GET.bat` - Mostra numero corrente
- `RESET.bat` - Reset numero a 0
- `logs/` - Cartella per i file di log

## 🔧 Installazione

1. **Copia questa cartella** nella bilancia slave nella posizione desiderata
   (es: `C:\DB-Next\` o cartella di rete)

2. **Verifica config.ini**:
   ```ini
   server=192.168.X.X      # IP della bilancia master
   port=3306
   database=sys_datos      # Database esistente
   user=user              # Utente con accesso remoto
   password=dibal
   ```

3. **Test connessione**:
   ```bash
   DB-NextConfig.exe
   ```
   Se si apre senza errori, la connessione DB funziona.

4. **Avvio saltacoda**:
   ```bash
   DB-Next.exe
   ```

## 🎛️ Controlli Numero

Da riga di comando o script batch:

```bash
# Avanti
NEXT.bat

# Indietro
PREV.bat

# Imposta numero specifico
SET.bat 42

# Mostra numero corrente
GET.bat

# Reset a 0
RESET.bat
```

## ⚙️ Configurazione Display

Premi `EDIT.bat` per aprire il configuratore e modificare:
- Layout (75%/25% default)
- Modalità schermo (single/mirror/multi)
- Tipo finestra (fullscreen/borderless/windowed)
- Polling DB (1000ms default)

## 🔍 Troubleshooting

### "Impossibile connettersi al database"
- Verifica IP della master in `config.ini`
- Assicurati che MySQL sulla master ascolti su tutte le interfacce
- Verifica che l'utente abbia permessi remoti

### Il numero non si aggiorna
- Controlla intervallo polling nel configuratore
- Verifica log in `logs/app.log`
- Test connessione con `DB-NextConfig.exe`

### Finestra non si apre
- Verifica che .NET 8 sia installato
- Controlla configurazione monitor

## 📝 Log

I log vengono salvati in `logs/app.log`:
```
2024-01-15 10:30:00 [INFO] === DB-Next avviato ===
2024-01-15 10:30:01 [INFO] Database inizializzato
2024-01-15 10:30:15 [INFO] Numero cambiato: 05 -> 06 (next da batch)
```

---
*Generato automaticamente per deployment su bilancia slave*
