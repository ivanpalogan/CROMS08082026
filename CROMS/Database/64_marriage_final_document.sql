-- 64_marriage_final_document.sql
-- STEP 11 - Final Document Handling. Stores the FINAL_REGISTERED_FORM_97 pages permanently,
-- and records when staff confirm the final document.
--
-- A dedicated table, not `marriages.final_scan_image` (a single-column idea from Step 10 that
-- was never applied - see the note in 63_marriage_final_scan.sql): Step 11 needs View / Replace
-- / Add Page, i.e. more than one page, which one LONGBLOB column cannot hold. Also not the
-- form97_capture_images/tokens tables - those are an EXPIRING capture session (one token, one
-- QR, one sitting at the desk); this is the PERMANENT record of the finished document, kept
-- regardless of how many capture sessions it took to assemble.
--
-- final_confirmed is a plain flag, separate from marriages.status - per spec, Status stays
-- Pending until staff confirm the final document, and confirming it does NOT itself register
-- the marriage (that is still the existing Register() action). "Pending" here is this project's
-- own established show-only mapping: every status except Registered already reads as Pending.
--
-- Idempotent (CREATE TABLE IF NOT EXISTS + guarded ADD COLUMN, ASCII only, applied by piping
-- into mysql).

CREATE TABLE IF NOT EXISTS `marriage_final_documents` (
    `id`           INT NOT NULL AUTO_INCREMENT,
    `marriage_id`  INT NOT NULL,
    `page_no`      INT NOT NULL DEFAULT 1,
    `image`        LONGBLOB NOT NULL,
    `uploaded_by`  INT NULL,
    `uploaded_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_marriage_final_documents_marriage` (`marriage_id`),
    CONSTRAINT `fk_marriage_final_documents_marriage`
        FOREIGN KEY (`marriage_id`) REFERENCES `marriages` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP PROCEDURE IF EXISTS _croms_marriage_final_document;
DELIMITER //
CREATE PROCEDURE _croms_marriage_final_document()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'final_confirmed') THEN
        ALTER TABLE `marriages` ADD COLUMN `final_confirmed` TINYINT(1) NOT NULL DEFAULT 0;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'final_confirmed_by') THEN
        ALTER TABLE `marriages` ADD COLUMN `final_confirmed_by` INT NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'final_confirmed_at') THEN
        ALTER TABLE `marriages` ADD COLUMN `final_confirmed_at` DATETIME NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_final_document();
DROP PROCEDURE IF EXISTS _croms_marriage_final_document;
