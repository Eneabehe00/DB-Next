using System.IO;

namespace DBNext.Shared;

/// <summary>
/// Gestisce il download e la cache automatica delle immagini dalla cartella condivisa
/// per le bilance slave.
/// </summary>
public class MediaCache
{
    private readonly string _cachePath;
    private readonly string _remotePath;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private readonly TimeSpan _minSyncInterval = TimeSpan.FromMinutes(5); // Sincronizza al massimo ogni 5 minuti

    public MediaCache(string remotePath)
    {
        _remotePath = remotePath;
        _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DBNext", "MediaCache"
        );
        Directory.CreateDirectory(_cachePath);

        Logger.Info($"MediaCache inizializzata: {_cachePath}");
    }

    /// <summary>
    /// Verifica se la cartella remota è accessibile
    /// </summary>
    public bool IsRemotePathAccessible()
    {
        try
        {
            return Directory.Exists(_remotePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restituisce il percorso locale del file, scaricandolo se necessario
    /// </summary>
    public async Task<string> GetMediaPathAsync(string filename)
    {
        var localPath = Path.Combine(_cachePath, filename);
        var remotePath = Path.Combine(_remotePath, filename);

        // Se il file locale esiste e non è vecchio, usalo
        if (File.Exists(localPath) && !IsFileOutdated(localPath, remotePath))
        {
            Logger.Info($"Cache HIT: {filename}");
            return localPath;
        }

        // Altrimenti scarica dalla cartella condivisa
        try
        {
            Logger.Info($"Cache MISS - Download: {filename}");
            await DownloadFileAsync(remotePath, localPath);
            return localPath;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Download fallito per {filename}, provo cache: {ex.Message}");

            // Se download fallisce ma abbiamo cache, usala
            if (File.Exists(localPath))
            {
                Logger.Warn($"Uso cache obsoleta per: {filename}");
                return localPath;
            }

            // Nessuna opzione disponibile
            throw new FileNotFoundException($"File {filename} non disponibile", ex);
        }
    }

    /// <summary>
    /// Controlla se il file locale è outdated rispetto a quello remoto
    /// </summary>
    private bool IsFileOutdated(string localPath, string remotePath)
    {
        try
        {
            var localInfo = new FileInfo(localPath);
            var remoteInfo = new FileInfo(remotePath);

            // Considera outdated se remoto è più recente di 30 secondi
            // (per gestire ritardi di rete/sincronizzazione)
            return remoteInfo.LastWriteTime > localInfo.LastWriteTime.AddSeconds(30);
        }
        catch
        {
            // Se non riusciamo a controllare, considera aggiornato
            return false;
        }
    }

    /// <summary>
    /// Scarica il file dalla cartella condivisa alla cache locale
    /// </summary>
    private async Task DownloadFileAsync(string sourcePath, string destPath)
    {
        try
        {
            // Verifica che il percorso remoto sia accessibile
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"File remoto non trovato: {sourcePath}");
            }

            // Copia dalla cartella condivisa (UNC path)
            await Task.Run(() => File.Copy(sourcePath, destPath, true));
            Logger.Info($"Download completato: {sourcePath} -> {destPath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new Exception($"Accesso negato al percorso remoto. Verifica permessi di rete: {sourcePath}", ex);
        }
        catch (IOException ex) when (ex.Message.Contains("network"))
        {
            throw new Exception($"Errore di rete nell'accesso al percorso remoto: {sourcePath}", ex);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Errore download {sourcePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Elimina file dalla cache più vecchi di X giorni
    /// </summary>
    public void CleanupOldFiles(int daysOld = 30)
    {
        try
        {
            var cacheDir = new DirectoryInfo(_cachePath);
            var files = cacheDir.GetFiles();

            foreach (var file in files)
            {
                if (file.LastAccessTime < DateTime.Now.AddDays(-daysOld))
                {
                    file.Delete();
                    Logger.Info($"Cache cleanup: eliminato {file.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Errore cleanup cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincronizza la cache locale con la cartella remota, eliminando file che non esistono più
    /// </summary>
    public void SyncWithRemote()
    {
        try
        {
            Logger.Info("SyncWithRemote: Inizio sincronizzazione - Path remoto: " + _remotePath);

            if (!Directory.Exists(_remotePath))
            {
                Logger.Warn($"SyncWithRemote: Cartella remota non accessibile: {_remotePath}");
                return;
            }

            // Prima ottieni TUTTI i file nella cartella remota
            var allRemoteFiles = Directory.GetFiles(_remotePath);
            Logger.Info($"SyncWithRemote: Tutti i file nella cartella remota ({allRemoteFiles.Length}):");
            foreach (var remoteFile in allRemoteFiles.OrderBy(f => Path.GetFileName(f)))
            {
                Logger.Info($"  REMOTO: {Path.GetFileName(remoteFile)}");
            }

            // Ottieni lista file remoti supportati
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var videoExtensions = new[] { ".mp4", ".avi", ".wmv", ".webm", ".mkv", ".mov" };
            var allExtensions = imageExtensions.Concat(videoExtensions).ToArray();

            var remoteFiles = allRemoteFiles
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => Path.GetFileName(f))
                .ToHashSet();

            Logger.Info($"SyncWithRemote: File remoti supportati ({remoteFiles.Count}): {string.Join(", ", remoteFiles.OrderBy(f => f))}");

            // Ottieni lista file locali nella cache
            if (!Directory.Exists(_cachePath))
            {
                Logger.Info("SyncWithRemote: Nessuna cache locale presente");
                return;
            }

            var cacheFiles = Directory.GetFiles(_cachePath)
                .Select(f => Path.GetFileName(f))
                .ToList();

            Logger.Info($"SyncWithRemote: File nella cache locale ({cacheFiles.Count}): {string.Join(", ", cacheFiles.OrderBy(f => f))}");

            // Elimina dalla cache i file che non esistono più nella cartella remota
            Logger.Info("SyncWithRemote: Controllo file da eliminare:");
            int deletedCount = 0;
            foreach (var cacheFile in cacheFiles.OrderBy(f => f))
            {
                if (!remoteFiles.Contains(cacheFile))
                {
                    Logger.Info($"  {cacheFile}: ELIMINA");
                    string fullPath = Path.Combine(_cachePath, cacheFile);
                    try
                    {
                        File.Delete(fullPath);
                        Logger.Info($"SyncWithRemote: Eliminato dalla cache {cacheFile} (non più presente nella cartella remota)");
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"SyncWithRemote: Errore eliminazione {cacheFile}: {ex.Message}");
                    }
                }
                else
                {
                    Logger.Info($"  {cacheFile}: MANTIENI");
                }
            }

            Logger.Info($"SyncWithRemote: Sincronizzazione completata - eliminati {deletedCount} file orfani");
        }
        catch (Exception ex)
        {
            Logger.Error($"SyncWithRemote: Errore durante sincronizzazione: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincronizza la cache solo se è passato abbastanza tempo dall'ultima sincronizzazione
    /// </summary>
    public void SyncIfNeeded()
    {
        if (DateTime.Now - _lastSyncTime < _minSyncInterval)
        {
            Logger.Debug("SyncIfNeeded: Saltato - troppo presto dall'ultima sincronizzazione");
            return;
        }

        Logger.Info("SyncIfNeeded: Eseguo sincronizzazione periodica");
        SyncWithRemote();
        _lastSyncTime = DateTime.Now;
    }

    /// <summary>
    /// Mostra lo stato attuale della cache per debug
    /// </summary>
    public void LogCacheStatus()
    {
        try
        {
            Logger.Info("=== STATO CACHE ===");
            Logger.Info($"Percorso remoto configurato: {_remotePath}");
            Logger.Info($"Percorso cache locale: {_cachePath}");
            Logger.Info($"Cartella remota accessibile: {IsRemotePathAccessible()}");

            if (Directory.Exists(_cachePath))
            {
                var cacheDir = new DirectoryInfo(_cachePath);
                var files = cacheDir.GetFiles();

                Logger.Info($"File presenti in cache: {files.Length}");

                if (files.Length > 0)
                {
                    Logger.Info("Ultimi 5 file in cache:");
                    foreach (var file in files.OrderByDescending(f => f.LastWriteTime).Take(5))
                    {
                        Logger.Info($"  {file.Name} ({file.Length} bytes, {file.LastWriteTime})");
                    }
                }
                else
                {
                    Logger.Info("Nessun file presente in cache");
                }
            }
            else
            {
                Logger.Info("Cartella cache locale non esiste ancora");
            }

            Logger.Info("===================");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Errore controllo stato cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Scarica tutti i file di una cartella slideshow
    /// </summary>
    public async Task CacheSlideshowFolderAsync(string remoteFolderPath)
    {
        try
        {
            Logger.Info($"CacheSlideshowFolderAsync: Inizio caching cartella {remoteFolderPath}");

            // Verifica che la cartella esista
            if (!Directory.Exists(remoteFolderPath))
            {
                Logger.Warn($"CacheSlideshowFolderAsync: Cartella remota non esiste: {remoteFolderPath}");
                return;
            }

            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var videoExtensions = new[] { ".mp4", ".avi", ".wmv", ".webm", ".mkv", ".mov" };
            var allExtensions = imageExtensions.Concat(videoExtensions).ToArray();

            // Prima elenca tutti i file nella cartella
            var allFiles = Directory.GetFiles(remoteFolderPath);
            Logger.Info($"CacheSlideshowFolderAsync: Trovati {allFiles.Length} file totali in {remoteFolderPath}");

            // Poi filtra per estensioni supportate
            var remoteFiles = allFiles
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();

            Logger.Info($"CacheSlideshowFolderAsync: {remoteFiles.Length} file media supportati trovati");

            if (remoteFiles.Length == 0)
            {
                Logger.Warn($"CacheSlideshowFolderAsync: Nessun file media trovato nella cartella {remoteFolderPath}");
                Logger.Warn($"CacheSlideshowFolderAsync: Estensioni supportate: {string.Join(", ", allExtensions)}");
                return;
            }

            // Cache tutti i file dello slideshow
            int successCount = 0;
            foreach (var remoteFile in remoteFiles)
            {
                string filename = Path.GetFileName(remoteFile);
                try
                {
                    Logger.Info($"CacheSlideshowFolderAsync: Caching {filename}...");
                    await GetMediaPathAsync(filename);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"CacheSlideshowFolderAsync: Errore caching {filename}: {ex.Message}");
                }
            }

            Logger.Info($"CacheSlideshowFolderAsync: Completato - {successCount}/{remoteFiles.Length} file cachati");
        }
        catch (Exception ex)
        {
            Logger.Error($"CacheSlideshowFolderAsync: Errore generale: {ex.Message}");
        }
    }
}
