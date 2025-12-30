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
                    mirror_exclude_displays VARCHAR(50) DEFAULT '0',
                    mirror_info_bar_displays VARCHAR(50) DEFAULT '',
                    mirror_margin_tops VARCHAR(100) DEFAULT '0',
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
                    operator_window_enabled TINYINT(1) DEFAULT 0,
                    operator_window_x INT DEFAULT 50,
                    operator_window_y INT DEFAULT 50,
                    operator_window_width INT DEFAULT 200,
                    operator_window_height INT DEFAULT 80,
                    operator_bg_color VARCHAR(20) DEFAULT '#000000',
                    operator_text_color VARCHAR(20) DEFAULT '#FFFFFF',
                    operator_font_family VARCHAR(100) DEFAULT 'Arial Black',
                    operator_font_size INT DEFAULT 36,
                    operator_always_on_top TINYINT(1) DEFAULT 1,
                    operator_label_text VARCHAR(50) DEFAULT 'TURNO',

                    -- Barra Informativa
                    info_bar_enabled TINYINT(1) DEFAULT 0,
                    info_bar_bg_color VARCHAR(20) DEFAULT '#1a1a2e',
                    info_bar_height INT DEFAULT 40,
                    info_bar_font_family VARCHAR(100) DEFAULT 'Segoe UI',
                    info_bar_font_size INT DEFAULT 12,
                    info_bar_text_color VARCHAR(20) DEFAULT '#ffffff',
                    news_rss_update_interval_ms INT DEFAULT 3600000,
                    weather_api_key VARCHAR(200) DEFAULT '',
                    weather_city VARCHAR(100) DEFAULT 'Rome',
                    weather_units VARCHAR(20) DEFAULT 'metric',
                    weather_update_interval_ms INT DEFAULT 600000,

                    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Migrazione: aggiungi colonne per finestra operatore se non esistono
            try
            {
                await using (var cmd = new MySqlCommand(@"
                ALTER TABLE queue_settings
                ADD COLUMN IF NOT EXISTS operator_window_enabled TINYINT(1) DEFAULT 0,
                ADD COLUMN IF NOT EXISTS operator_window_x INT DEFAULT 50,
                ADD COLUMN IF NOT EXISTS operator_window_y INT DEFAULT 50,
                ADD COLUMN IF NOT EXISTS operator_window_width INT DEFAULT 200,
                ADD COLUMN IF NOT EXISTS operator_window_height INT DEFAULT 80,
                ADD COLUMN IF NOT EXISTS operator_monitor_index INT DEFAULT 0,
                ADD COLUMN IF NOT EXISTS operator_bg_color VARCHAR(20) DEFAULT '#000000',
                ADD COLUMN IF NOT EXISTS operator_text_color VARCHAR(20) DEFAULT '#FFFFFF',
                ADD COLUMN IF NOT EXISTS operator_font_family VARCHAR(100) DEFAULT 'Arial Black',
                ADD COLUMN IF NOT EXISTS operator_font_size INT DEFAULT 36,
                ADD COLUMN IF NOT EXISTS operator_always_on_top TINYINT(1) DEFAULT 1,
                ADD COLUMN IF NOT EXISTS operator_label_text VARCHAR(50) DEFAULT 'TURNO'", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                Logger.Info("Migrazione colonne finestra operatore completata");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Migrazione colonne finestra operatore: {ex.Message}");
            }

            // Migrazione: aggiungi colonne per barra informativa se non esistono
            try
            {
                await using (var cmd = new MySqlCommand(@"
                ALTER TABLE queue_settings
                ADD COLUMN IF NOT EXISTS info_bar_enabled TINYINT(1) DEFAULT 0,
                ADD COLUMN IF NOT EXISTS info_bar_bg_color VARCHAR(20) DEFAULT '#1a1a2e',
                ADD COLUMN IF NOT EXISTS info_bar_height INT DEFAULT 40,
                ADD COLUMN IF NOT EXISTS info_bar_font_family VARCHAR(100) DEFAULT 'Segoe UI',
                ADD COLUMN IF NOT EXISTS info_bar_font_size INT DEFAULT 12,
                ADD COLUMN IF NOT EXISTS info_bar_text_color VARCHAR(20) DEFAULT '#ffffff',
                ADD COLUMN IF NOT EXISTS news_rss_update_interval_ms INT DEFAULT 3600000,
                ADD COLUMN IF NOT EXISTS weather_api_key VARCHAR(200) DEFAULT '',
                ADD COLUMN IF NOT EXISTS weather_city VARCHAR(100) DEFAULT 'Rome',
                ADD COLUMN IF NOT EXISTS weather_units VARCHAR(20) DEFAULT 'metric',
                ADD COLUMN IF NOT EXISTS weather_update_interval_ms INT DEFAULT 600000", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                Logger.Info("Migrazione colonne barra informativa completata");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Migrazione colonne barra informativa: {ex.Message}");
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
                       target_display_index, multi_display_list, mirror_exclude_displays,
                       mirror_info_bar_displays, mirror_margin_tops, window_mode,
                       window_width, window_height, window_margin_top, number_font_family,
                       number_font_size, number_font_bold, number_color,
                       number_bg_color, number_label_text, number_label_color,
                       number_label_size, number_label_position, number_label_offset,
                       media_folder_mode, slideshow_interval_ms,
                       operator_window_enabled, operator_window_x, operator_window_y,
                       operator_window_width, operator_window_height, operator_monitor_index,
                       operator_bg_color, operator_text_color, operator_font_family, operator_font_size,
                       operator_always_on_top, operator_label_text,
                       media_scheduler_enabled, media_scheduler_start_date, media_scheduler_end_date,
                       media_scheduler_path, media_scheduler_type, media_scheduler_fit,
                       media_scheduler_folder_mode, media_scheduler_interval_ms,
                       info_bar_enabled, info_bar_bg_color, info_bar_height, info_bar_font_family,
                       info_bar_font_size, info_bar_text_color, news_rss_update_interval_ms,
                       weather_api_key, weather_city, weather_units,
                       weather_update_interval_ms, voice_enabled, voice_prefix, updated_at
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
                    MirrorExcludeDisplays = reader.IsDBNull(10) ? "0" : reader.GetString(10),
                    MirrorInfoBarDisplays = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    MirrorMarginTops = reader.IsDBNull(12) ? "0" : reader.GetString(12),
                    WindowMode = reader.IsDBNull(13) ? "borderless" : reader.GetString(13),
                    WindowWidth = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                    WindowHeight = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                    WindowMarginTop = reader.IsDBNull(16) ? 0 : reader.GetInt32(16),
                    NumberFontFamily = reader.IsDBNull(17) ? "Arial Black" : reader.GetString(17),
                    NumberFontSize = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                    NumberFontBold = reader.IsDBNull(19) ? true : reader.GetInt32(19) == 1,
                    NumberColor = reader.IsDBNull(20) ? "#FFC832" : reader.GetString(20),
                    NumberBgColor = reader.IsDBNull(21) ? "#14141E" : reader.GetString(21),
                    NumberLabelText = reader.IsDBNull(22) ? "" : reader.GetString(22),
                    NumberLabelColor = reader.IsDBNull(23) ? "#FFFFFF" : reader.GetString(23),
                    NumberLabelSize = reader.IsDBNull(24) ? 0 : reader.GetInt32(24),
                    NumberLabelPosition = reader.IsDBNull(25) ? "top" : reader.GetString(25),
                    NumberLabelOffset = reader.IsDBNull(26) ? 0 : reader.GetInt32(26),
                    MediaFolderMode = reader.IsDBNull(27) ? false : reader.GetInt32(27) == 1,
                    SlideshowIntervalMs = reader.IsDBNull(28) ? 5000 : reader.GetInt32(28),
                    OperatorWindowEnabled = reader.IsDBNull(29) ? false : reader.GetInt32(29) == 1,
                    OperatorWindowX = reader.IsDBNull(30) ? 50 : reader.GetInt32(30),
                    OperatorWindowY = reader.IsDBNull(31) ? 50 : reader.GetInt32(31),
                    OperatorWindowWidth = reader.IsDBNull(32) ? 200 : reader.GetInt32(32),
                    OperatorWindowHeight = reader.IsDBNull(33) ? 80 : reader.GetInt32(33),
                    OperatorMonitorIndex = reader.IsDBNull(34) ? 0 : reader.GetInt32(34),
                    OperatorBgColor = reader.IsDBNull(35) ? "#000000" : reader.GetString(35),
                    OperatorTextColor = reader.IsDBNull(36) ? "#FFFFFF" : reader.GetString(36),
                    OperatorFontFamily = reader.IsDBNull(37) ? "Arial Black" : reader.GetString(37),
                    OperatorFontSize = reader.IsDBNull(38) ? 36 : reader.GetInt32(38),
                    OperatorAlwaysOnTop = reader.IsDBNull(39) ? true : reader.GetInt32(39) == 1,
                    OperatorLabelText = reader.IsDBNull(40) ? "TURNO" : reader.GetString(40),
                    MediaSchedulerEnabled = reader.IsDBNull(41) ? false : reader.GetInt32(41) == 1,
                    MediaSchedulerStartDate = reader.IsDBNull(42) ? DateTime.Today : reader.GetDateTime(42),
                    MediaSchedulerEndDate = reader.IsDBNull(43) ? DateTime.Today.AddDays(1) : reader.GetDateTime(43),
                    MediaSchedulerPath = reader.IsDBNull(44) ? "" : reader.GetString(44),
                    MediaSchedulerType = reader.IsDBNull(45) ? "image" : reader.GetString(45),
                    MediaSchedulerFit = reader.IsDBNull(46) ? "cover" : reader.GetString(46),
                    MediaSchedulerFolderMode = reader.IsDBNull(47) ? true : reader.GetInt32(47) == 1,
                    MediaSchedulerIntervalMs = reader.IsDBNull(48) ? 5000 : reader.GetInt32(48),

                    // Barra Informativa
                    InfoBarEnabled = reader.IsDBNull(49) ? false : reader.GetInt32(49) == 1,
                    InfoBarBgColor = reader.IsDBNull(50) ? "#1a1a2e" : reader.GetString(50),
                    InfoBarHeight = reader.IsDBNull(51) ? 40 : reader.GetInt32(51),
                    InfoBarFontFamily = reader.IsDBNull(52) ? "Segoe UI" : reader.GetString(52),
                    InfoBarFontSize = reader.IsDBNull(53) ? 12 : reader.GetInt32(53),
                    InfoBarTextColor = reader.IsDBNull(54) ? "#ffffff" : reader.GetString(54),
                    NewsRssUpdateIntervalMs = reader.IsDBNull(55) ? 3600000 : reader.GetInt32(55),
                    WeatherApiKey = reader.IsDBNull(56) ? "" : reader.GetString(56),
                    WeatherCity = reader.IsDBNull(57) ? "Rome" : reader.GetString(57),
                    WeatherUnits = reader.IsDBNull(58) ? "metric" : reader.GetString(58),
                    WeatherUpdateIntervalMs = reader.IsDBNull(59) ? 600000 : reader.GetInt32(59),

                    // Sintesi Vocale
                    VoiceEnabled = reader.IsDBNull(60) ? false : reader.GetInt32(60) == 1,
                    VoicePrefix = reader.IsDBNull(61) ? "" : reader.GetString(61),

                    UpdatedAt = reader.IsDBNull(62) ? DateTime.Now : reader.GetDateTime(62)
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
                    mirror_exclude_displays = @mirror_exclude_displays,
                    mirror_info_bar_displays = @mirror_info_bar_displays,
                    mirror_margin_tops = @mirror_margin_tops,
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
                    number_label_offset = @number_label_offset,
                    operator_window_enabled = @operator_window_enabled,
                    operator_window_x = @operator_window_x,
                    operator_window_y = @operator_window_y,
                    operator_window_width = @operator_window_width,
                    operator_window_height = @operator_window_height,
                    operator_monitor_index = @operator_monitor_index,
                    operator_bg_color = @operator_bg_color,
                    operator_text_color = @operator_text_color,
                    operator_font_family = @operator_font_family,
                    operator_font_size = @operator_font_size,
                    operator_always_on_top = @operator_always_on_top,
                    operator_label_text = @operator_label_text,
                    media_scheduler_enabled = @media_scheduler_enabled,
                    media_scheduler_start_date = @media_scheduler_start_date,
                    media_scheduler_end_date = @media_scheduler_end_date,
                    media_scheduler_path = @media_scheduler_path,
                    media_scheduler_type = @media_scheduler_type,
                    media_scheduler_fit = @media_scheduler_fit,
                    media_scheduler_folder_mode = @media_scheduler_folder_mode,
                    media_scheduler_interval_ms = @media_scheduler_interval_ms,
                    info_bar_enabled = @info_bar_enabled,
                    info_bar_bg_color = @info_bar_bg_color,
                    info_bar_height = @info_bar_height,
                    info_bar_font_family = @info_bar_font_family,
                    info_bar_font_size = @info_bar_font_size,
                    info_bar_text_color = @info_bar_text_color,
                    news_rss_update_interval_ms = @news_rss_update_interval_ms,
                    weather_api_key = @weather_api_key,
                    weather_city = @weather_city,
                    weather_units = @weather_units,
                    weather_update_interval_ms = @weather_update_interval_ms,
                    voice_enabled = @voice_enabled,
                    voice_prefix = @voice_prefix
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
            cmd.Parameters.AddWithValue("@mirror_exclude_displays", settings.MirrorExcludeDisplays ?? "0");
            cmd.Parameters.AddWithValue("@mirror_info_bar_displays", settings.MirrorInfoBarDisplays ?? "");
            cmd.Parameters.AddWithValue("@mirror_margin_tops", settings.MirrorMarginTops ?? "0");
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
            cmd.Parameters.AddWithValue("@operator_window_enabled", settings.OperatorWindowEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@operator_window_x", settings.OperatorWindowX);
            cmd.Parameters.AddWithValue("@operator_window_y", settings.OperatorWindowY);
            cmd.Parameters.AddWithValue("@operator_window_width", settings.OperatorWindowWidth);
            cmd.Parameters.AddWithValue("@operator_window_height", settings.OperatorWindowHeight);
            cmd.Parameters.AddWithValue("@operator_monitor_index", settings.OperatorMonitorIndex);
            cmd.Parameters.AddWithValue("@operator_bg_color", settings.OperatorBgColor ?? "#000000");
            cmd.Parameters.AddWithValue("@operator_text_color", settings.OperatorTextColor ?? "#FFFFFF");
            cmd.Parameters.AddWithValue("@operator_font_family", settings.OperatorFontFamily ?? "Arial Black");
            cmd.Parameters.AddWithValue("@operator_font_size", settings.OperatorFontSize);
            cmd.Parameters.AddWithValue("@operator_always_on_top", settings.OperatorAlwaysOnTop ? 1 : 0);
            cmd.Parameters.AddWithValue("@operator_label_text", settings.OperatorLabelText ?? "TURNO");
            cmd.Parameters.AddWithValue("@media_scheduler_enabled", settings.MediaSchedulerEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@media_scheduler_start_date", settings.MediaSchedulerStartDate);
            cmd.Parameters.AddWithValue("@media_scheduler_end_date", settings.MediaSchedulerEndDate);
            cmd.Parameters.AddWithValue("@media_scheduler_path", settings.MediaSchedulerPath ?? "");
            cmd.Parameters.AddWithValue("@media_scheduler_type", settings.MediaSchedulerType ?? "image");
            cmd.Parameters.AddWithValue("@media_scheduler_fit", settings.MediaSchedulerFit ?? "cover");
            cmd.Parameters.AddWithValue("@media_scheduler_folder_mode", settings.MediaSchedulerFolderMode ? 1 : 0);
            cmd.Parameters.AddWithValue("@media_scheduler_interval_ms", settings.MediaSchedulerIntervalMs);

            // Parametri barra informativa
            cmd.Parameters.AddWithValue("@info_bar_enabled", settings.InfoBarEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@info_bar_bg_color", settings.InfoBarBgColor ?? "#1a1a2e");
            cmd.Parameters.AddWithValue("@info_bar_height", settings.InfoBarHeight);
            cmd.Parameters.AddWithValue("@info_bar_font_family", settings.InfoBarFontFamily ?? "Segoe UI");
            cmd.Parameters.AddWithValue("@info_bar_font_size", settings.InfoBarFontSize);
            cmd.Parameters.AddWithValue("@info_bar_text_color", settings.InfoBarTextColor ?? "#ffffff");
            cmd.Parameters.AddWithValue("@news_rss_update_interval_ms", settings.NewsRssUpdateIntervalMs);
            cmd.Parameters.AddWithValue("@weather_api_key", settings.WeatherApiKey ?? "");
            cmd.Parameters.AddWithValue("@weather_city", settings.WeatherCity ?? "Rome");
            cmd.Parameters.AddWithValue("@weather_units", settings.WeatherUnits ?? "metric");
            cmd.Parameters.AddWithValue("@weather_update_interval_ms", settings.WeatherUpdateIntervalMs);
            cmd.Parameters.AddWithValue("@voice_enabled", settings.VoiceEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@voice_prefix", settings.VoicePrefix ?? "");

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

