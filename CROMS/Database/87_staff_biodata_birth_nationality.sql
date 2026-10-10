-- 87_staff_biodata_birth_nationality.sql
-- Staff biodata (Settings > Users & Access > Staff Biodata): place of birth and nationality.
-- Age is NOT stored - it is computed from `birthdate` wherever it is shown, because a stored
-- age is wrong the day after it is saved.
-- Nothing is backfilled: each person's own birth details are entered by them / the admin.
-- Expand-only and idempotent: existing rows and older app builds remain valid.
-- Rollback, only if no deployed app still reads these columns:
--   ALTER TABLE staff_biodata DROP COLUMN birth_place, DROP COLUMN nationality;

DROP PROCEDURE IF EXISTS _croms_87_staff_biodata_birth_nationality;
DELIMITER //
CREATE PROCEDURE _croms_87_staff_biodata_birth_nationality()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'staff_biodata' AND COLUMN_NAME = 'birth_place'
    ) THEN
        ALTER TABLE `staff_biodata` ADD COLUMN `birth_place` VARCHAR(150) NULL AFTER `birthdate`;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'staff_biodata' AND COLUMN_NAME = 'nationality'
    ) THEN
        ALTER TABLE `staff_biodata` ADD COLUMN `nationality` VARCHAR(60) NULL AFTER `birth_place`;
    END IF;
END //
DELIMITER ;

CALL _croms_87_staff_biodata_birth_nationality();
DROP PROCEDURE IF EXISTS _croms_87_staff_biodata_birth_nationality;
