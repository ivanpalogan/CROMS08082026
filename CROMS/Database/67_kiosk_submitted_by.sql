-- 67_kiosk_submitted_by.sql
-- Marriage Registration: the kiosk now asks WHO is submitting the Certificate of Marriage -
-- the Solemnizing Officer, the Husband, the Wife, or an Authorized Representative - the same
-- four answers Form 97's own "Submitted by" field (migration 59) records. The person at the
-- kiosk types their own name on the next screen, so the ticket's full_name IS the submitter;
-- only the role and, for a representative, their office/organization need new columns.
--
-- `queue_tickets` is a HOLDING place (the marriage record does not exist until staff open
-- Form 97); MarriageEntryForm.PrepareForQueueTicket copies these into marriages.submitted_by /
-- submitted_by_rep_name / submitted_by_rep_org. NULL for every other service and for tickets
-- issued before this migration.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_kiosk_submitted_by;
DELIMITER //
CREATE PROCEDURE _croms_kiosk_submitted_by()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'submitted_by') THEN
        ALTER TABLE `queue_tickets` ADD COLUMN `submitted_by` VARCHAR(20) NULL AFTER `marriage_license_image`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'submitted_by_org') THEN
        ALTER TABLE `queue_tickets` ADD COLUMN `submitted_by_org` VARCHAR(150) NULL AFTER `submitted_by`;
    END IF;
END //
DELIMITER ;
CALL _croms_kiosk_submitted_by();
DROP PROCEDURE IF EXISTS _croms_kiosk_submitted_by;
