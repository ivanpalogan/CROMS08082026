-- 77_license_residence_parts.sql
-- Form 90 (marriage licence application): an applicant's Residence was ONE free-text box
-- (marriage_licenses.husband_residence / wife_residence), so no barangay, no listed province or
-- municipality, and nothing to keep the spelling consistent with the rest of the registry.
-- The screen now asks for it the way Form 97 and the birth certificate do:
--     Province, City / Municipality, Barangay, House No. / Street (optional)
-- Four SEPARATE columns per applicant, not one comma-joined value: a street such as
-- "Block 5, Lot 12, Avocado St." contains commas and could not be split back reliably
-- (the M2 debt recorded 2026-07-14 - a new field does not repeat it).
--
-- The joined husband_residence / wife_residence columns are KEPT and the app still fills them
-- ("House, Barangay, Municipality, Province") on every save, so the printed Form 90, the consent
-- form and the Form 97 copy that read one string keep working. Nothing is backfilled: a licence
-- filed before this migration shows its old residence in the House / Street box for the clerk.
-- NULL = not stated. ASCII only (applied by piping through the mysql client). Idempotent.

DROP PROCEDURE IF EXISTS _croms_license_res;
DELIMITER //
CREATE PROCEDURE _croms_license_res()
BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE who VARCHAR(10);
    DECLARE part VARCHAR(20);
    DECLARE col VARCHAR(40);
    DECLARE ddl VARCHAR(40);
    WHILE i < 8 DO
        SET who  = IF(i < 4, 'husband', 'wife');
        SET part = ELT(MOD(i, 4) + 1, 'province', 'municipality', 'barangay', 'house');
        SET ddl  = ELT(MOD(i, 4) + 1, 'VARCHAR(100) NULL', 'VARCHAR(120) NULL', 'VARCHAR(120) NULL', 'VARCHAR(150) NULL');
        SET col  = CONCAT(who, '_res_', part);
        IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                       AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = col) THEN
            SET @sql = CONCAT('ALTER TABLE `marriage_licenses` ADD COLUMN `', col, '` ', ddl);
            PREPARE st FROM @sql; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;
        SET i = i + 1;
    END WHILE;
END //
DELIMITER ;
CALL _croms_license_res();
DROP PROCEDURE IF EXISTS _croms_license_res;
