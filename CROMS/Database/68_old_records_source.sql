-- 68_old_records_source.sql
-- Digitizing an old, already-registered paper record (Intelligent Document Processing / OCR)
-- must never pass through the LIVE Birth/Death Registration screens -- those are for today's
-- walk-in registrations, not decades-old backlog. `record_source` marks which path a row came
-- from: 'Registration' (the default, for every row entered through the live screen) or
-- 'OCR-Backlog' (committed straight from the OCR screen). A new pair of dedicated screens,
-- "Old Birth Records (OCR)" / "Old Death Records (OCR)", show and edit ONLY the 'OCR-Backlog'
-- rows -- full Add / Edit / View / Delete -- so an operator never has to open the live
-- registration form to correct a digitized record, and a digitized record never clutters the
-- live registration screen's own list.
--
-- Marriage is unchanged: its OCR path already writes straight into the live Marriage
-- Registration record (Form 97), which is what was asked for, so no column is added there.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_old_records_source;
DELIMITER //
CREATE PROCEDURE _croms_old_records_source()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'births' AND COLUMN_NAME = 'record_source') THEN
        ALTER TABLE `births` ADD COLUMN `record_source` VARCHAR(20) NOT NULL DEFAULT 'Registration' AFTER `status`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'record_source') THEN
        ALTER TABLE `deaths` ADD COLUMN `record_source` VARCHAR(20) NOT NULL DEFAULT 'Registration' AFTER `status`;
    END IF;
END //
DELIMITER ;
CALL _croms_old_records_source();
DROP PROCEDURE IF EXISTS _croms_old_records_source;
