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
        // Copia dalla cartella condivisa (UNC path)
        await Task.Run(() => File.Copy(sourcePath, destPath, true));
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
    /// Scarica tutti i file di una cartella slideshow
    /// </summary>
    public async Task CacheSlideshowFolderAsync(string remoteFolderPath)
    {
        try
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var videoExtensions = new[] { ".mp4", ".avi", ".wmv", ".webm", ".mkv", ".mov" };
            var allExtensions = imageExtensions.Concat(videoExtensions).ToArray();

            var remoteFiles = Directory.GetFiles(remoteFolderPath)
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();

            // Cache tutti i file dello slideshow
            foreach (var remoteFile in remoteFiles)
            {
                string filename = Path.GetFileName(remoteFile);
                await GetMediaPathAsync(filename);
            }

            Logger.Info($"Slideshow cache: {remoteFiles.Length} file");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Errore cache slideshow: {ex.Message}");
        }
    }
}
