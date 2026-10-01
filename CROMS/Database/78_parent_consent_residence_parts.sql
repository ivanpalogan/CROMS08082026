-- 78_parent_consent_residence_parts.sql
-- Form 90 (marriage licence application) and Form 97 (marriage registration): the Residence of
-- a party's FATHER, MOTHER and CONSENT / ADVICE PERSON (Form 97: the consent person only) was
-- ONE free-text box. Every other residence in the app is asked as
--     Province, City / Municipality, Barangay, House No. / Street (optional)
-- (the applicant's own residence: migrations 74 and 77). These get the same four cells.
--
-- Four SEPARATE columns per person, not one comma-joined value: a street such as
-- "Block 5, Lot 12, Avocado St." contains commas and could not be split back reliably.
--
-- The joined *_father_residence / *_mother_residence / *_consent_residence columns are KEPT and
-- the app still fills them ("House, Barangay, Municipality, Province") on every save, so the
-- printed Form 90, the consent form, the Form 97 certificate view and the print maps that read
-- one string keep working untouched. Nothing is backfilled: a record filed before this migration
-- shows its old residence in the House / Street box for the clerk to correct.
-- NULL = not stated. ASCII only (applied by piping through the mysql client). Idempotent.

DROP PROCEDURE IF EXISTS _croms_parent_res;
DELIMITER //
CREATE PROCEDURE _croms_parent_res()
BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE who VARCHAR(10);
    DECLARE person VARCHAR(10);
    DECLARE part VARCHAR(20);
    DECLARE col VARCHAR(60);
    DECLARE ddl VARCHAR(40);

    -- marriage_licenses: husband / wife x father / mother / consent x 4 parts = 24 columns
    SET i = 0;
    WHILE i < 24 DO
        SET who    = IF(i < 12, 'husband', 'wife');
        SET person = ELT((MOD(i, 12) DIV 4) + 1, 'father', 'mother', 'consent');
        SET part   = ELT(MOD(i, 4) + 1, 'province', 'municipality', 'barangay', 'house');
        SET ddl    = ELT(MOD(i, 4) + 1, 'VARCHAR(100) NULL', 'VARCHAR(120) NULL', 'VARCHAR(120) NULL', 'VARCHAR(150) NULL');
        SET col    = CONCAT(who, '_', person, '_res_', part);
        IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                       AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = col) THEN
            SET @sql = CONCAT('ALTER TABLE `marriage_licenses` ADD COLUMN `', col, '` ', ddl);
            PREPARE st FROM @sql; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;
        SET i = i + 1;
    END WHILE;

    -- marriages (Form 97 asks only for the consent person): husband / wife x 4 parts = 8 columns
    SET i = 0;
    WHILE i < 8 DO
        SET who  = IF(i < 4, 'husband', 'wife');
        SET part = ELT(MOD(i, 4) + 1, 'province', 'municipality', 'barangay', 'house');
        SET ddl  = ELT(MOD(i, 4) + 1, 'VARCHAR(100) NULL', 'VARCHAR(120) NULL', 'VARCHAR(120) NULL', 'VARCHAR(150) NULL');
        SET col  = CONCAT(who, '_consent_res_', part);
        IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                       AND TABLE_NAME = 'marriages' AND COLUMN_NAME = col) THEN
            SET @sql = CONCAT('ALTER TABLE `marriages` ADD COLUMN `', col, '` ', ddl);
            PREPARE st FROM @sql; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;
        SET i = i + 1;
    END WHILE;
END //
DELIMITER ;
CALL _croms_parent_res();
DROP PROCEDURE IF EXISTS _croms_parent_res;
