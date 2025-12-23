-- ============================================
-- DB-Next - Script di Installazione/Aggiornamento
-- Sistema Saltacoda per Bilance Industriali
-- ============================================
-- Esegui questo script per installazione iniziale o aggiornamento
-- mysql -u user -pdibal sys_datos < install.sql
--
-- Questo script:
-- 1. Crea tutte le tabelle se non esistono
-- 2. Aggiunge colonne mancanti (aggiornamento sicuro)
-- 3. Inserisce dati di default
-- 4. Crea views e stored procedures
-- ============================================

-- Verifica di essere nel database corretto
-- USE sys_datos;

-- ============================================
-- 1. CREAZIONE TABELLE (se non esistono)
-- ============================================

-- Tabella: queue_state
-- Stato corrente della coda (numero visualizzato)
-- Contiene sempre una sola riga con id=1
CREATE TABLE IF NOT EXISTS queue_state (
    id INT NOT NULL DEFAULT 1,
    current_number INT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
    -- NOTA: CHECK CONSTRAINT non supportati in MySQL 5.x, validazione fatta nell'app
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Tabella: queue_settings
-- Configurazione display e comportamento
-- Contiene sempre una sola riga con id=1
CREATE TABLE IF NOT EXISTS queue_settings (
    id INT NOT NULL DEFAULT 1,

    -- Media
    media_path VARCHAR(500) DEFAULT '' COMMENT 'Percorso file immagine/video',
    media_type VARCHAR(20) DEFAULT 'image' COMMENT 'Tipo: image, gif, video',
    media_fit VARCHAR(20) DEFAULT 'cover' COMMENT 'Adattamento: cover, contain',

    -- Polling
    poll_ms INT DEFAULT 1000 COMMENT 'Intervallo polling DB in millisecondi',

    -- Layout
    layout_left_pct INT DEFAULT 75 COMMENT 'Percentuale larghezza sinistra (media)',
    layout_right_pct INT DEFAULT 25 COMMENT 'Percentuale larghezza destra (numero)',

    -- Display
    screen_mode VARCHAR(20) DEFAULT 'single' COMMENT 'Modalità: single, mirror, multi',
    target_display_index INT DEFAULT 0 COMMENT 'Indice monitor per modalità single',
    multi_display_list VARCHAR(50) DEFAULT '0' COMMENT 'Lista monitor per modalità multi (es. 0,2)',
    window_mode VARCHAR(20) DEFAULT 'borderless' COMMENT 'Finestra: fullscreen, borderless, windowed',
    window_width INT DEFAULT 0 COMMENT 'Larghezza finestra personalizzata in pixel (0=auto)',
    window_height INT DEFAULT 0 COMMENT 'Altezza finestra personalizzata in pixel (0=auto)',
    window_margin_top INT DEFAULT 0 COMMENT 'Margine superiore in pixel (per banner/overlay)',

    -- Personalizzazione Numero
    number_font_family VARCHAR(100) DEFAULT 'Arial Black' COMMENT 'Font del numero',
    number_font_size INT DEFAULT 0 COMMENT 'Dimensione font (0=auto)',
    number_font_bold TINYINT(1) DEFAULT 1 COMMENT 'Grassetto',
    number_color VARCHAR(20) DEFAULT '#FFC832' COMMENT 'Colore numero',
    number_bg_color VARCHAR(20) DEFAULT '#14141E' COMMENT 'Colore sfondo',

    -- Scritta sopra/sotto il numero
    number_label_text VARCHAR(200) DEFAULT '' COMMENT 'Testo etichetta (es. Ora serviamo il numero)',
    number_label_color VARCHAR(20) DEFAULT '#FFFFFF' COMMENT 'Colore etichetta',
    number_label_size INT DEFAULT 0 COMMENT 'Dimensione font etichetta (0=auto responsive)',
    number_label_position VARCHAR(20) DEFAULT 'top' COMMENT 'Posizione: top, bottom',
    number_label_offset INT DEFAULT 0 COMMENT 'Offset in pixel dalla posizione',

    -- Slideshow
    media_folder_mode TINYINT(1) DEFAULT 0 COMMENT 'Modalità cartella (slideshow)',
    slideshow_interval_ms INT DEFAULT 5000 COMMENT 'Intervallo slideshow in ms',

    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (id)
    -- NOTA: CHECK CONSTRAINT non supportati in MySQL 5.x, validazione fatta nell'app
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Tabella: queue_events
-- Log di tutte le modifiche al numero
-- Utile per audit e debugging
CREATE TABLE IF NOT EXISTS queue_events (
    id INT AUTO_INCREMENT PRIMARY KEY,
    action VARCHAR(20) NOT NULL COMMENT 'Azione: next, prev, set, reset',
    old_number INT NOT NULL COMMENT 'Numero precedente',
    new_number INT NOT NULL COMMENT 'Nuovo numero',
    source VARCHAR(50) NOT NULL COMMENT 'Origine: batch, config, manual',
    ts TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp evento',

    INDEX idx_timestamp (ts),
    INDEX idx_action (action)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ============================================
-- 2. INSERIMENTO DATI DI DEFAULT
-- ============================================

-- Inserisci riga di default per queue_state
INSERT IGNORE INTO queue_state (id, current_number) VALUES (1, 0);

-- Inserisci riga di default per queue_settings
INSERT IGNORE INTO queue_settings (id) VALUES (1);

-- ============================================
-- 3. AGGIORNAMENTO SCHEMA (aggiunge colonne mancanti)
-- ============================================

DELIMITER //

DROP PROCEDURE IF EXISTS sp_update_schema//
CREATE PROCEDURE sp_update_schema()
BEGIN
    -- Colonne per personalizzazione numero
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_font_family'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_font_family VARCHAR(100) DEFAULT 'Arial Black';
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_font_size'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_font_size INT DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_font_bold'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_font_bold TINYINT(1) DEFAULT 1;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_color'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_color VARCHAR(20) DEFAULT '#FFC832';
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_bg_color'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_bg_color VARCHAR(20) DEFAULT '#14141E';
    END IF;

    -- Colonne per slideshow
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'media_folder_mode'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN media_folder_mode TINYINT(1) DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'slideshow_interval_ms'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN slideshow_interval_ms INT DEFAULT 5000;
    END IF;

    -- Colonne per scritta sopra/sotto il numero
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_label_text'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_label_text VARCHAR(200) DEFAULT '';
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_label_color'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_label_color VARCHAR(20) DEFAULT '#FFFFFF';
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_label_size'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_label_size INT DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_label_position'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_label_position VARCHAR(20) DEFAULT 'top';
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'number_label_offset'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN number_label_offset INT DEFAULT 0;
    END IF;

    -- Colonne per dimensioni finestra personalizzate
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'window_width'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN window_width INT DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'window_height'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN window_height INT DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'queue_settings'
          AND COLUMN_NAME = 'window_margin_top'
    ) THEN
        ALTER TABLE queue_settings ADD COLUMN window_margin_top INT DEFAULT 0;
    END IF;

    SELECT 'Schema aggiornato con successo' AS status;
END//

DELIMITER ;

-- Esegui la procedura di aggiornamento schema
CALL sp_update_schema();

-- Rimuovi la procedura temporanea
DROP PROCEDURE IF EXISTS sp_update_schema;

-- ============================================
-- 4. VIEWS UTILI
-- ============================================

-- Vista stato corrente con ultima modifica
-- NOTA: MySQL 5.x non supporta CREATE OR REPLACE, usiamo DROP se esiste
DROP VIEW IF EXISTS v_queue_current;
CREATE VIEW v_queue_current AS
SELECT
    s.current_number,
    s.updated_at,
    (SELECT COUNT(*) FROM queue_events WHERE DATE(ts) = CURDATE()) as changes_today
FROM queue_state s
WHERE s.id = 1;

-- Vista ultimi 10 eventi
DROP VIEW IF EXISTS v_queue_recent_events;
CREATE VIEW v_queue_recent_events AS
SELECT
    action,
    old_number,
    new_number,
    source,
    ts
FROM queue_events
ORDER BY ts DESC
LIMIT 10;

-- ============================================
-- 5. STORED PROCEDURES PER OPERAZIONI ATOMICHE
-- ============================================

DELIMITER //

-- Incrementa numero con wrap-around
DROP PROCEDURE IF EXISTS sp_next_number//
CREATE PROCEDURE sp_next_number(IN p_source VARCHAR(50))
BEGIN
    DECLARE v_old INT;
    DECLARE v_new INT;

    SELECT current_number INTO v_old FROM queue_state WHERE id = 1 FOR UPDATE;
    SET v_new = (v_old + 1) % 100;

    UPDATE queue_state SET current_number = v_new WHERE id = 1;
    INSERT INTO queue_events (action, old_number, new_number, source)
        VALUES ('next', v_old, v_new, p_source);

    SELECT v_new AS new_number;
END//

-- Decrementa numero con wrap-around
DROP PROCEDURE IF EXISTS sp_prev_number//
CREATE PROCEDURE sp_prev_number(IN p_source VARCHAR(50))
BEGIN
    DECLARE v_old INT;
    DECLARE v_new INT;

    SELECT current_number INTO v_old FROM queue_state WHERE id = 1 FOR UPDATE;
    SET v_new = IF(v_old = 0, 99, v_old - 1);

    UPDATE queue_state SET current_number = v_new WHERE id = 1;
    INSERT INTO queue_events (action, old_number, new_number, source)
        VALUES ('prev', v_old, v_new, p_source);

    SELECT v_new AS new_number;
END//

-- Imposta numero specifico
DROP PROCEDURE IF EXISTS sp_set_number//
CREATE PROCEDURE sp_set_number(IN p_number INT, IN p_source VARCHAR(50))
BEGIN
    DECLARE v_old INT;
    DECLARE v_new INT;

    SET v_new = GREATEST(0, LEAST(99, p_number));

    SELECT current_number INTO v_old FROM queue_state WHERE id = 1 FOR UPDATE;

    UPDATE queue_state SET current_number = v_new WHERE id = 1;
    INSERT INTO queue_events (action, old_number, new_number, source)
        VALUES ('set', v_old, v_new, p_source);

    SELECT v_new AS new_number;
END//

DELIMITER ;

-- ============================================
-- 6. VERIFICA FINALE
-- ============================================

-- Mostra le tabelle create
SELECT TABLE_NAME, TABLE_COMMENT
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME LIKE 'queue_%'
ORDER BY TABLE_NAME;

-- Mostra le colonne della tabella settings
SELECT COLUMN_NAME, DATA_TYPE, COLUMN_DEFAULT, COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'queue_settings'
ORDER BY ORDINAL_POSITION;

-- Mostra stato corrente
SELECT * FROM v_queue_current;

-- ============================================
-- INSTALLAZIONE COMPLETATA!
-- ============================================
-- Il database è pronto per l'uso con DB-Next
--
-- Per pulizia log vecchi (opzionale):
-- DELETE FROM queue_events WHERE ts < DATE_SUB(NOW(), INTERVAL 30 DAY);
--
-- ============================================
