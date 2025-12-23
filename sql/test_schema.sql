-- ============================================
-- Test Schema DB-Next
-- Verifica che le tabelle siano state create correttamente
-- ============================================

-- Usa il database
USE sys_datos;

-- ============================================
-- 1. Verifica Tabelle
-- ============================================
SHOW TABLES LIKE 'queue_%';
-- Dovrebbe mostrare: queue_state, queue_settings, queue_events

-- ============================================
-- 2. Verifica Struttura Tabelle
-- ============================================
DESCRIBE queue_state;
DESCRIBE queue_settings;
DESCRIBE queue_events;

-- ============================================
-- 3. Verifica Dati Default
-- ============================================
SELECT * FROM queue_state;
-- Dovrebbe mostrare: id=1, current_number=0

SELECT * FROM queue_settings;
-- Dovrebbe mostrare: id=1 con configurazione default

-- ============================================
-- 4. Verifica Views
-- ============================================
SHOW FULL TABLES IN sys_datos WHERE TABLE_TYPE LIKE 'VIEW';
-- Dovrebbe mostrare: v_queue_current, v_queue_recent_events

SELECT * FROM v_queue_current;
-- Dovrebbe mostrare: current_number=0, updated_at, changes_today=0

-- ============================================
-- 5. Verifica Stored Procedures
-- ============================================
SHOW PROCEDURE STATUS WHERE Db = 'sys_datos' AND Name LIKE 'sp_%';
-- Dovrebbe mostrare: sp_next_number, sp_prev_number, sp_set_number

-- ============================================
-- 6. Test Funzionamento Base
-- ============================================

-- Test NEXT (0 -> 1)
CALL sp_next_number('test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 1

-- Test NEXT (1 -> 2)
CALL sp_next_number('test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 2

-- Test SET (2 -> 50)
CALL sp_set_number(50, 'test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 50

-- Test PREV (50 -> 49)
CALL sp_prev_number('test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 49

-- Test wrap-around NEXT (99 -> 0)
CALL sp_set_number(99, 'test');
CALL sp_next_number('test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 0

-- Test wrap-around PREV (0 -> 99)
CALL sp_prev_number('test');
SELECT current_number FROM queue_state WHERE id = 1;
-- Dovrebbe essere 99

-- ============================================
-- 7. Verifica Log Eventi
-- ============================================
SELECT * FROM queue_events ORDER BY ts DESC LIMIT 10;
-- Dovrebbe mostrare tutti i test effettuati

SELECT * FROM v_queue_recent_events;
-- Vista degli ultimi 10 eventi

-- ============================================
-- 8. Reset per ripulire i test
-- ============================================
-- Riporta a 0 e cancella i log di test
UPDATE queue_state SET current_number = 0 WHERE id = 1;
DELETE FROM queue_events WHERE source = 'test';

-- Verifica finale
SELECT 'Test completato!' AS status,
       (SELECT current_number FROM queue_state WHERE id = 1) AS numero_corrente,
       (SELECT COUNT(*) FROM queue_events) AS eventi_rimanenti;

