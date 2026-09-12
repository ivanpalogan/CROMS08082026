-- 24_ocr_routing.sql
-- OCR Digitization routing + mapping fixes.
--   1. `births`.`sex` becomes NULL-able. OCR cannot always read the sex tick box,
--      and the NOT NULL column made every such commit fail with
--      "Column 'sex' cannot be null". `deaths`.`sex` was already relaxed in
--      06_death_form103.sql; this brings births in line.
--   2. `ocr_batch` records WHERE a scan was routed: its detected kind and the
--      registry table + row id it was committed to, so a scan can be traced to
--      the record it produced (and a scan is never assumed to be a birth).
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).

DROP PROCEDURE IF EXISTS _croms_ocr_routing;
DELIMITER //
CREATE PROCEDURE _croms_ocr_routing()
BEGIN
    -- 1. births.sex nullable
    IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'sex' AND IS_NULLABLE = 'NO'
    ) THEN
        ALTER TABLE births MODIFY COLUMN sex ENUM('Male','Female') NULL;
    END IF;

    -- 2. ocr_batch routing columns
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'doc_kind'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN doc_kind VARCHAR(20) NULL AFTER doc_class;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'record_table'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN record_table VARCHAR(30) NULL AFTER doc_kind;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'record_id'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN record_id INT NULL AFTER record_table;
    END IF;
END //
DELIMITER ;
CALL _croms_ocr_routing();
DROP PROCEDURE IF EXISTS _croms_ocr_routing;
