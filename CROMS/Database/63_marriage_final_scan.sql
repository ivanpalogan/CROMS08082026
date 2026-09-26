-- 63_marriage_final_scan.sql
-- STEP 10 - Require Final Registered Form 97. Once Registration Information is saved
-- (Step 9), the desk requires a picture of the FINAL, physically signed/stamped Form 97 -
-- a different image from `marriages.scan_image`, which is the ORIGINAL certificate
-- captured/OCR'd BEFORE registration (Steps 7-8). Keeping them as two columns is
-- deliberate: overwriting scan_image would destroy the pre-registration evidence trail
-- and conflate "what OCR read" with "what the finished, signed record looks like".
--
-- `form97_capture_tokens.purpose` lets the SAME mobile-capture token/QR mechanism built
-- for the pre-OCR capture (migration 61) serve this second, distinct purpose too - the
-- phone page and the save-API read this column to decide what to show/label, instead of
-- a second parallel table. Existing rows default to 'INCOMING_FORM_97' (today's only use),
-- so nothing already captured is reinterpreted.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_marriage_final_scan;
DELIMITER //
CREATE PROCEDURE _croms_marriage_final_scan()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'final_scan_image') THEN
        ALTER TABLE `marriages` ADD COLUMN `final_scan_image` LONGBLOB NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'form97_capture_tokens' AND COLUMN_NAME = 'purpose') THEN
        ALTER TABLE `form97_capture_tokens`
            ADD COLUMN `purpose` VARCHAR(40) NOT NULL DEFAULT 'INCOMING_FORM_97' AFTER `txn_code`;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_final_scan();
DROP PROCEDURE IF EXISTS _croms_marriage_final_scan;
