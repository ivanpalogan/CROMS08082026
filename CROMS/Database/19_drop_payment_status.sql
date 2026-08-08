-- =====================================================================
-- 19_drop_payment_status.sql
-- Every payment recorded here is Paid — this office does not process
-- pending / cancelled / refunded at the cashier window. Drop the
-- unused `payments.status` column. Idempotent (guarded).
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_drop_payment_status`;
DELIMITER $$
CREATE PROCEDURE `_croms_drop_payment_status`()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'status') THEN
    ALTER TABLE `payments` DROP COLUMN `status`;
  END IF;
END$$
DELIMITER ;

CALL `_croms_drop_payment_status`();
DROP PROCEDURE `_croms_drop_payment_status`;
