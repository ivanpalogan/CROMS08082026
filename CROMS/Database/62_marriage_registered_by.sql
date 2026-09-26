-- 62_marriage_registered_by.sql
-- STEP 9 - Registration Information. The Form 97 desk records who at the LCRO registered the
-- marriage, exactly as it appears on the PHYSICAL certificate (already signed/stamped outside
-- CROMS - no signature is generated or recreated here). `marriages.registered_by` ALREADY
-- exists (migration 33_marriage_workflow.sql) but it is an INT user id, auto-stamped by the
-- live Register() transaction - it is not a place to type a name read off a paper form, and
-- reusing it would collide with that column's real meaning. This adds a SEPARATE, plain
-- free-text column for exactly that: the name written/stamped on the physical Form 97.
--
-- registry_no, date_registered and remarks already exist (migrations 27/33/30) and are reused
-- as-is - no other new columns.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_marriage_registered_by;
DELIMITER //
CREATE PROCEDURE _croms_marriage_registered_by()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'registered_by_name') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `registered_by_name` VARCHAR(120) NULL AFTER `registered_at`;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_registered_by();
DROP PROCEDURE IF EXISTS _croms_marriage_registered_by;
