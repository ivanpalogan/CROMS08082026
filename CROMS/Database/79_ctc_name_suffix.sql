-- 79_ctc_name_suffix.sql
-- Kiosk Certified True Copy request: the client can now give a name SUFFIX (Jr., Sr., III) for
-- the record owner (the husband, on a marriage) and for the wife. It is its own column rather
-- than text glued onto the last name, so "Dela Cruz Jr." can still be searched as "Dela Cruz".
-- Nothing is backfilled; NULL = none given. The kiosk falls back to appending the suffix to the
-- last name if this migration has not been applied, so nothing is lost either way.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_ctc_suffix;
DELIMITER //
CREATE PROCEDURE _croms_ctc_suffix()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'ctc_requests' AND COLUMN_NAME = 'owner_suffix') THEN
        ALTER TABLE `ctc_requests`
            ADD COLUMN `owner_suffix`  VARCHAR(20) NULL AFTER `owner_last`,
            ADD COLUMN `spouse_suffix` VARCHAR(20) NULL AFTER `spouse_last`;
    END IF;
END //
DELIMITER ;
CALL _croms_ctc_suffix();
DROP PROCEDURE IF EXISTS _croms_ctc_suffix;
