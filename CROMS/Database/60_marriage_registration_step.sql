-- 60_marriage_registration_step.sql
-- Marriage Registration (Form 97) desk redesign: adds a stored "Current Step" progress
-- indicator distinct from marriages.status, and links a marriage record back to the
-- transaction/queue-ticket it was created from (marriages had neither before this).
--
-- STATUS IS UNCHANGED. marriages.status stays exactly the 4 values MarriageService already
-- enforces (Draft / For Review / Returned / Registered) - that gate (registrar review,
-- delayed-case handling, the Register() transaction) is untouched. The screen displays a
-- simplified Pending/Registered label over those 4 values; nothing is collapsed in the
-- database. current_step is a SEPARATE, purely informational column - "Current Step is
-- only a progress indicator and must not replace the main status" (spec).
--
-- current_step is a fixed small vocabulary, advanced by MarriageService the same way
-- petitions.stage is advanced - never freely typed by a screen. New records start at
-- 'Form 97 Capture'.
--
-- transaction_id / queue_ticket_id are NEW - marriages never had a transaction/queue link
-- (unlike births/deaths/certificate_requests, which already ride the transactions spine).
-- Both NULL-able and NOT backfilled: an existing marriage was never tied to a transaction,
-- and inventing one would fabricate a queue history that never happened.
--
-- Idempotent (guarded ADD COLUMN/ADD CONSTRAINT, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_marriage_reg_step;
DELIMITER //
CREATE PROCEDURE _croms_marriage_reg_step()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'current_step') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `current_step` VARCHAR(40) NOT NULL DEFAULT 'Form 97 Capture';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'transaction_id') THEN
        ALTER TABLE `marriages` ADD COLUMN `transaction_id` INT NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'queue_ticket_id') THEN
        ALTER TABLE `marriages` ADD COLUMN `queue_ticket_id` INT NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND CONSTRAINT_NAME = 'fk_marriages_transaction') THEN
        -- SET NULL: closing/removing a transaction must not block deleting it, and the
        -- marriage record itself remains the authoritative registry entry either way.
        ALTER TABLE `marriages` ADD CONSTRAINT `fk_marriages_transaction`
            FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND CONSTRAINT_NAME = 'fk_marriages_queue_ticket') THEN
        ALTER TABLE `marriages` ADD CONSTRAINT `fk_marriages_queue_ticket`
            FOREIGN KEY (`queue_ticket_id`) REFERENCES `queue_tickets` (`id`) ON DELETE SET NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_reg_step();
DROP PROCEDURE IF EXISTS _croms_marriage_reg_step;
