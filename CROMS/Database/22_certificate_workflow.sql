-- =====================================================================
-- 22_certificate_workflow.sql
-- Certificate Request new flow (2026-08-04):
--   Create Request -> Find/Print certificate -> Fees & Payment -> Release.
--   If the certificate is hard to find OR the client goes home, the request
--   is parked at "Waiting to Release" (no payment yet). The client returns,
--   goes to the kiosk, picks Release & Claim, and enters the ORIGINAL queue
--   ticket number they got last time (that number is now the pull key -> see
--   the continuous, never-reset queue numbering in the kiosk).
--
-- Adds two transaction statuses:
--   ForPrint          - request created, staff must locate + print the CTC.
--   WaitingToRelease  - parked (cert hard to find / client left) before payment.
--
-- Idempotent: the enum MODIFY is safe to re-run; the columns are added only
-- when missing (MySQL has no ADD COLUMN IF NOT EXISTS, so we guard by hand).
-- =====================================================================

ALTER TABLE `transactions`
  MODIFY COLUMN `status`
  ENUM('Queued','Processing','ForPrint','ForPayment','ForRelease',
       'WaitingToRelease','Released','Cancelled')
  NOT NULL DEFAULT 'Queued';

DROP PROCEDURE IF EXISTS `_croms_add_parked_cols`;
DELIMITER //
CREATE PROCEDURE `_croms_add_parked_cols`()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'transactions'
                   AND COLUMN_NAME = 'parked_at') THEN
    ALTER TABLE `transactions` ADD COLUMN `parked_at` DATETIME NULL AFTER `updated_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'transactions'
                   AND COLUMN_NAME = 'parked_reason') THEN
    ALTER TABLE `transactions` ADD COLUMN `parked_reason` VARCHAR(160) NULL AFTER `parked_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'transactions'
                   AND COLUMN_NAME = 'parked_ticket') THEN
    ALTER TABLE `transactions` ADD COLUMN `parked_ticket` VARCHAR(30) NULL AFTER `parked_reason`;
  END IF;
END //
DELIMITER ;
CALL `_croms_add_parked_cols`();
DROP PROCEDURE IF EXISTS `_croms_add_parked_cols`;
