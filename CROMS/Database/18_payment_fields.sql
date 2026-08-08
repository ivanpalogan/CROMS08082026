-- =====================================================================
-- 18_payment_fields.sql
-- Fees & Payments redesign: drop the Senior/PWD discount, add real
-- payment-processing fields (method, reference, status, tendered/change,
-- additional fee, remarks). Idempotent — safe to re-run.
-- (MySQL has no ADD/DROP COLUMN IF [NOT] EXISTS, so we guard via
--  information_schema in a throwaway stored procedure.)
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_migrate_payments`;
DELIMITER $$
CREATE PROCEDURE `_croms_migrate_payments`()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'payment_method') THEN
    ALTER TABLE `payments` ADD COLUMN `payment_method` VARCHAR(20) NOT NULL DEFAULT 'Cash' AFTER `or_number`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'reference_no') THEN
    ALTER TABLE `payments` ADD COLUMN `reference_no` VARCHAR(50) NULL AFTER `payment_method`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'status') THEN
    ALTER TABLE `payments` ADD COLUMN `status` VARCHAR(20) NOT NULL DEFAULT 'Paid' AFTER `reference_no`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'additional_fee') THEN
    ALTER TABLE `payments` ADD COLUMN `additional_fee` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `gross_amount`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'amount_tendered') THEN
    ALTER TABLE `payments` ADD COLUMN `amount_tendered` DECIMAL(10,2) NULL AFTER `net_amount`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'change_amount') THEN
    ALTER TABLE `payments` ADD COLUMN `change_amount` DECIMAL(10,2) NULL AFTER `amount_tendered`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'remarks') THEN
    ALTER TABLE `payments` ADD COLUMN `remarks` VARCHAR(255) NULL AFTER `change_amount`;
  END IF;

  -- Drop the discontinued discount fields (Senior/PWD discounting is done at the Treasury).
  IF EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'discount_type') THEN
    ALTER TABLE `payments` DROP COLUMN `discount_type`;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'discount_amount') THEN
    ALTER TABLE `payments` DROP COLUMN `discount_amount`;
  END IF;
END$$
DELIMITER ;

CALL `_croms_migrate_payments`();
DROP PROCEDURE `_croms_migrate_payments`;
