-- 59_marriage_submitted_by.sql
-- Marriage Registration (Form 97) intake now records WHO is filing the registration:
-- Solemnizing Officer / Husband / Wife / Authorized Representative. When the filer is a
-- representative, their name and office/organization are recorded too.
--
-- Nothing backfilled: existing rows get NULL (unknown), not a guessed default.
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).
USE `croms`;

DROP PROCEDURE IF EXISTS _croms_marriage_submitted_by;
DELIMITER //
CREATE PROCEDURE _croms_marriage_submitted_by()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages'
          AND COLUMN_NAME = 'submitted_by'
    ) THEN
        ALTER TABLE marriages ADD COLUMN submitted_by VARCHAR(20) NULL AFTER remarks;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages'
          AND COLUMN_NAME = 'submitted_by_rep_name'
    ) THEN
        ALTER TABLE marriages ADD COLUMN submitted_by_rep_name VARCHAR(150) NULL AFTER submitted_by;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages'
          AND COLUMN_NAME = 'submitted_by_rep_org'
    ) THEN
        ALTER TABLE marriages ADD COLUMN submitted_by_rep_org VARCHAR(150) NULL AFTER submitted_by_rep_name;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_submitted_by();
DROP PROCEDURE IF EXISTS _croms_marriage_submitted_by;
