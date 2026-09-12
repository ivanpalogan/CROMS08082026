-- 23_claim_queue_link.sql
-- Link a claim_requests row directly to the kiosk queue ticket created in the same
-- visit, so the Claim Form can show that ticket's face photo (queue_tickets.id_image)
-- even when neither row is tied to a transaction yet (a fresh pickup with no parked txn).
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).

DROP PROCEDURE IF EXISTS _croms_add_claim_qt;
DELIMITER //
CREATE PROCEDURE _croms_add_claim_qt()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'claim_requests'
          AND COLUMN_NAME = 'queue_ticket_id'
    ) THEN
        ALTER TABLE claim_requests ADD COLUMN queue_ticket_id INT NULL AFTER transaction_id;
    END IF;
END //
DELIMITER ;
CALL _croms_add_claim_qt();
DROP PROCEDURE IF EXISTS _croms_add_claim_qt;
