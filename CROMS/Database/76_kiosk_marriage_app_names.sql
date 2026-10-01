-- 76_kiosk_marriage_app_names.sql
-- Marriage Application (Form 90): the kiosk asks for BOTH applicants' First / Middle / Last name,
-- but queue_tickets only kept one joined string per party (full_name / spouse_full_name). Staff
-- opening the Form 90 window would have to re-type both names, and splitting a joined string back
-- into cells mangles a two-word surname (measured on BREQS, 2026-08-04). So the three cells are
-- kept as typed, in a holding place - MarriageLicenseForm.PrepareForQueueTicket copies them into
-- the application. NULL for every other service and for tickets issued before this migration.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_kiosk_app_names;
DELIMITER //
CREATE PROCEDURE _croms_kiosk_app_names()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'app_h_first') THEN
        ALTER TABLE `queue_tickets`
            ADD COLUMN `app_h_first`  VARCHAR(80) NULL,
            ADD COLUMN `app_h_middle` VARCHAR(80) NULL,
            ADD COLUMN `app_h_last`   VARCHAR(80) NULL,
            ADD COLUMN `app_w_first`  VARCHAR(80) NULL,
            ADD COLUMN `app_w_middle` VARCHAR(80) NULL,
            ADD COLUMN `app_w_last`   VARCHAR(80) NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_kiosk_app_names();
DROP PROCEDURE IF EXISTS _croms_kiosk_app_names;
