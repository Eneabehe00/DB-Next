using MySqlConnector;

namespace DBNext.Shared;

/// <summary>
/// Helper per operazioni database MySQL
/// </summary>
public static class Database
{
    /// <summary>
    /// Testa la connessione al database
    /// </summary>
    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore connessione DB: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Inizializza le tabelle se non esistono
    /// </summary>
    public static async Task InitializeAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            
            // Crea tabella queue_state (sintassi compatibile MySQL 5.x)
            await using (var cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS queue_state (
                    id INT PRIMARY KEY DEFAULT 1,
                    current_number INT NOT NULL DEFAULT 0,
                    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            // Crea tabella queue_settings (sintassi compatibile MySQL 5.x)
            await using (var cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS queue_settings (
                    id INT PRIMARY KEY DEFAULT 1,
                    media_path VARCHAR(500) DEFAULT '',
                    media_type VARCHAR(20) DEFAULT 'image',
                    media_fit VARCHAR(20) DEFAULT 'cover',
                    poll_ms INT DEFAULT 1000,
                    layout_left_pct INT DEFAULT 75,
                    layout_right_pct INT DEFAULT 25,
                    screen_mode VARCHAR(20) DEFAULT 'single',
                    target_display_index INT DEFAULT 0,
                    multi_display_list VARCHAR(50) DEFAULT '0',
                    window_mode VARCHAR(20) DEFAULT 'borderless',
                    window_width INT DEFAULT 0,
                    window_height INT DEFAULT 0,
                    window_margin_top INT DEFAULT 0,
                    number_font_family VARCHAR(100) DEFAULT 'Arial Black',
                    number_font_size INT DEFAULT 0,
                    number_font_bold TINYINT(1) DEFAULT 1,
                    number_color VARCHAR(20) DEFAULT '#FFC832',
                    number_bg_color VARCHAR(20) DEFAULT '#14141E',
                    media_folder_mode TINYINT(1) DEFAULT 0,
                    slideshow_interval_ms INT DEFAULT 5000,
                    number_label_text VARCHAR(200) DEFAULT '',
                    number_label_color VARCHAR(20) DEFAULT '#FFFFFF',
                    number_label_size INT DEFAULT 0,
                    number_label_position VARCHAR(20) DEFAULT 'top',
                    number_label_offset INT DEFAULT 0,
                    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            // Crea tabella queue_events (sintassi compatibile MySQL 5.x)
            await using (var cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS queue_events (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    action VARCHAR(20) NOT NULL,
                    old_number INT NOT NULL,
                    new_number INT NOT NULL,
                    source VARCHAR(50) NOT NULL,
                    ts TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_ts (ts)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            // Inserisci record di default se non esistono
            await using (var cmd = new MySqlCommand(
                "INSERT IGNORE INTO queue_state (id, current_number) VALUES (1, 0)", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            await using (var cmd = new MySqlCommand(
                "INSERT IGNORE INTO queue_settings (id) VALUES (1)", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            Logger.Info("Database inizializzato");
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore inizializzazione DB: {ex.Message}");
            throw;
        }
    }
    
    #region Queue State
    
    /// <summary>
    /// Legge il numero corrente della coda
    /// </summary>
    public static async Task<QueueState> GetStateAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            
            await using var cmd = new MySqlCommand(
                "SELECT id, current_number, updated_at FROM queue_state WHERE id = 1", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new QueueState
                {
                    Id = reader.GetInt32(0),
                    CurrentNumber = reader.GetInt32(1),
                    UpdatedAt = reader.GetDateTime(2)
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore lettura stato: {ex.Message}");
        }
        
        return new QueueState();
    }
    
    /// <summary>
    /// Imposta il numero corrente
    /// </summary>
    public static async Task<bool> SetNumberAsync(int number, string source, string action)
    {
        number = Math.Clamp(number, 0, 99);
        
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            
            // Leggi numero precedente
            int oldNumber = 0;
            await using (var cmd = new MySqlCommand(
                "SELECT current_number FROM queue_state WHERE id = 1", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null) oldNumber = Convert.ToInt32(result);
            }
            
            // Aggiorna numero
            await using (var cmd = new MySqlCommand(
                "UPDATE queue_state SET current_number = @num WHERE id = 1", conn))
            {
                cmd.Parameters.AddWithValue("@num", number);
                await cmd.ExecuteNonQueryAsync();
            }
            
            // Log evento
            await using (var cmd = new MySqlCommand(@"
                INSERT INTO queue_events (action, old_number, new_number, source) 
                VALUES (@action, @old, @new, @source)", conn))
            {
                cmd.Parameters.AddWithValue("@action", action);
                cmd.Parameters.AddWithValue("@old", oldNumber);
                cmd.Parameters.AddWithValue("@new", number);
                cmd.Parameters.AddWithValue("@source", source);
                await cmd.ExecuteNonQueryAsync();
            }
            
            Logger.Info($"Numero cambiato: {oldNumber} -> {number} ({action} da {source})");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore impostazione numero: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Incrementa numero (wrap 99->0)
    /// </summary>
    public static async Task<int> NextNumberAsync(string source)
    {
        var state = await GetStateAsync();
        var newNum = (state.CurrentNumber + 1) % 100;
        await SetNumberAsync(newNum, source, "next");
        return newNum;
    }
    
    /// <summary>
    /// Decrementa numero (wrap 0->99)
    /// </summary>
    public static async Task<int> PrevNumberAsync(string source)
    {
        var state = await GetStateAsync();
        var newNum = state.CurrentNumber == 0 ? 99 : state.CurrentNumber - 1;
        await SetNumberAsync(newNum, source, "prev");
        return newNum;
    }
    
    #endregion
    
    #region Queue Settings
    
    /// <summary>
    /// Legge le impostazioni correnti
    /// </summary>
    public static async Task<QueueSettings> GetSettingsAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            
            await using var cmd = new MySqlCommand(@"
                SELECT id, media_path, media_type, media_fit, poll_ms,
                       layout_left_pct, layout_right_pct, screen_mode,
                       target_display_index, multi_display_list, window_mode,
                       window_width, window_height, window_margin_top, number_font_family,
                       number_font_size, number_font_bold, number_color,
                       number_bg_color, number_label_text, number_label_color,
                       number_label_size, number_label_position, number_label_offset,
                       media_folder_mode, slideshow_interval_ms, updated_at
                FROM queue_settings WHERE id = 1", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new QueueSettings
                {
                    Id = reader.GetInt32(0),
                    MediaPath = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    MediaType = reader.IsDBNull(2) ? "image" : reader.GetString(2),
                    MediaFit = reader.IsDBNull(3) ? "cover" : reader.GetString(3),
                    PollMs = reader.IsDBNull(4) ? 1000 : reader.GetInt32(4),
                    LayoutLeftPct = reader.IsDBNull(5) ? 75 : reader.GetInt32(5),
                    LayoutRightPct = reader.IsDBNull(6) ? 25 : reader.GetInt32(6),
                    ScreenMode = reader.IsDBNull(7) ? "single" : reader.GetString(7),
                    TargetDisplayIndex = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    MultiDisplayList = reader.IsDBNull(9) ? "0" : reader.GetString(9),
                    WindowMode = reader.IsDBNull(10) ? "borderless" : reader.GetString(10),
                    WindowWidth = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                    WindowHeight = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                    WindowMarginTop = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                    NumberFontFamily = reader.IsDBNull(14) ? "Arial Black" : reader.GetString(14),
                    NumberFontSize = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                    NumberFontBold = reader.IsDBNull(16) ? true : reader.GetInt32(16) == 1,
                    NumberColor = reader.IsDBNull(17) ? "#FFC832" : reader.GetString(17),
                    NumberBgColor = reader.IsDBNull(18) ? "#14141E" : reader.GetString(18),
                    NumberLabelText = reader.IsDBNull(19) ? "" : reader.GetString(19),
                    NumberLabelColor = reader.IsDBNull(20) ? "#FFFFFF" : reader.GetString(20),
                    NumberLabelSize = reader.IsDBNull(21) ? 0 : reader.GetInt32(21),
                    NumberLabelPosition = reader.IsDBNull(22) ? "top" : reader.GetString(22),
                    NumberLabelOffset = reader.IsDBNull(23) ? 0 : reader.GetInt32(23),
                    MediaFolderMode = reader.IsDBNull(24) ? false : reader.GetInt32(24) == 1,
                    SlideshowIntervalMs = reader.IsDBNull(25) ? 5000 : reader.GetInt32(25),
                    UpdatedAt = reader.IsDBNull(26) ? DateTime.Now : reader.GetDateTime(26)
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore lettura settings: {ex.Message}");
        }
        
        return new QueueSettings();
    }
    
    /// <summary>
    /// Salva le impostazioni
    /// </summary>
    public static async Task<bool> SaveSettingsAsync(QueueSettings settings)
    {
        try
        {
            await using var conn = new MySqlConnection(Config.ConnectionString);
            await conn.OpenAsync();
            
            await using var cmd = new MySqlCommand(@"
                UPDATE queue_settings SET
                    media_path = @media_path,
                    media_type = @media_type,
                    media_fit = @media_fit,
                    poll_ms = @poll_ms,
                    layout_left_pct = @layout_left_pct,
                    layout_right_pct = @layout_right_pct,
                    screen_mode = @screen_mode,
                    target_display_index = @target_display_index,
                    multi_display_list = @multi_display_list,
                    window_mode = @window_mode,
                    window_width = @window_width,
                    window_height = @window_height,
                    window_margin_top = @window_margin_top,
                    number_font_family = @number_font_family,
                    number_font_size = @number_font_size,
                    number_font_bold = @number_font_bold,
                    number_color = @number_color,
                    number_bg_color = @number_bg_color,
                    media_folder_mode = @media_folder_mode,
                    slideshow_interval_ms = @slideshow_interval_ms,
                    number_label_text = @number_label_text,
                    number_label_color = @number_label_color,
                    number_label_size = @number_label_size,
                    number_label_position = @number_label_position,
                    number_label_offset = @number_label_offset
                WHERE id = 1", conn);
            
            cmd.Parameters.AddWithValue("@media_path", settings.MediaPath ?? "");
            cmd.Parameters.AddWithValue("@media_type", settings.MediaType ?? "image");
            cmd.Parameters.AddWithValue("@media_fit", settings.MediaFit ?? "cover");
            cmd.Parameters.AddWithValue("@poll_ms", settings.PollMs);
            cmd.Parameters.AddWithValue("@layout_left_pct", settings.LayoutLeftPct);
            cmd.Parameters.AddWithValue("@layout_right_pct", settings.LayoutRightPct);
            cmd.Parameters.AddWithValue("@screen_mode", settings.ScreenMode ?? "single");
            cmd.Parameters.AddWithValue("@target_display_index", settings.TargetDisplayIndex);
            cmd.Parameters.AddWithValue("@multi_display_list", settings.MultiDisplayList ?? "0");
            cmd.Parameters.AddWithValue("@window_mode", settings.WindowMode ?? "borderless");
            cmd.Parameters.AddWithValue("@window_width", settings.WindowWidth);
            cmd.Parameters.AddWithValue("@window_height", settings.WindowHeight);
            cmd.Parameters.AddWithValue("@window_margin_top", settings.WindowMarginTop);
            cmd.Parameters.AddWithValue("@number_font_family", settings.NumberFontFamily ?? "Arial Black");
            cmd.Parameters.AddWithValue("@number_font_size", settings.NumberFontSize);
            cmd.Parameters.AddWithValue("@number_font_bold", settings.NumberFontBold ? 1 : 0);
            cmd.Parameters.AddWithValue("@number_color", settings.NumberColor ?? "#FFC832");
            cmd.Parameters.AddWithValue("@number_bg_color", settings.NumberBgColor ?? "#14141E");
            cmd.Parameters.AddWithValue("@media_folder_mode", settings.MediaFolderMode ? 1 : 0);
            cmd.Parameters.AddWithValue("@slideshow_interval_ms", settings.SlideshowIntervalMs);
            cmd.Parameters.AddWithValue("@number_label_text", settings.NumberLabelText ?? "");
            cmd.Parameters.AddWithValue("@number_label_color", settings.NumberLabelColor ?? "#FFFFFF");
            cmd.Parameters.AddWithValue("@number_label_size", settings.NumberLabelSize);
            cmd.Parameters.AddWithValue("@number_label_position", settings.NumberLabelPosition ?? "top");
            cmd.Parameters.AddWithValue("@number_label_offset", settings.NumberLabelOffset);
            
            await cmd.ExecuteNonQueryAsync();
            Logger.Info("Impostazioni salvate");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore salvataggio settings: {ex.Message}");
            return false;
        }
    }
    
    #endregion
}

