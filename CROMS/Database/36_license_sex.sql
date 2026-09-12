-- 36_license_sex.sql
-- Sex of each applicant on the Application for Marriage License (Form 90).
--
-- Asked for by the LCRO at the 2026-09-11 pre-checkup: the paper form prints a sex for
-- each party and CROMS had nowhere to put it, so the field had to be written in by hand
-- on every printed application.
--
-- NOT derived from the husband / wife column. The column is what the form is ABOUT, not a
-- record of what was stated on it - and a sex entry can itself have been corrected under
-- RA 10172, so it is a fact the applicant supplies, not one the software concludes.
-- Existing rows stay NULL: the applications already on file did not record it, and writing
-- a value into them would put a fact in the register that nobody ever stated. The screen
-- shows the column's own value for those, editable, rather than a blank box.
--
-- Idempotent: MySQL has no ADD COLUMN IF NOT EXISTS, so this goes through a guarded
-- throwaway procedure, the same pattern as migrations 18/19/21/24/26.

DROP PROCEDURE IF EXISTS _croms_license_sex;
DELIMITER //
CREATE PROCEDURE _croms_license_sex()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'marriage_licenses'
                     AND COLUMN_NAME = 'husband_sex') THEN
        ALTER TABLE `marriage_licenses`
            ADD COLUMN `husband_sex` VARCHAR(10) NULL AFTER `husband_place_of_birth`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'marriage_licenses'
                     AND COLUMN_NAME = 'wife_sex') THEN
        ALTER TABLE `marriage_licenses`
            ADD COLUMN `wife_sex` VARCHAR(10) NULL AFTER `wife_place_of_birth`;
    END IF;
END //
DELIMITER ;

CALL _croms_license_sex();
DROP PROCEDURE IF EXISTS _croms_license_sex;
