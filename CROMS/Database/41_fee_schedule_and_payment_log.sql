-- 41_fee_schedule_and_payment_log.sql
-- The office's fee card, and a payment log that holds every collection.
--
-- SOURCE: the office's own card, headed "PLS. PAY AT TREASURY OFFICE" (backlog section 8), and the
-- office's answer of 2026-09-13 that the "+ 30" on certified copy / certification is PART OF THE
-- FEE (one amount, not a separate line).
--
-- FEES
--   * The seeded placeholders of 2026-07-14 are corrected ONLY where they still hold the seed value
--     (155 / 210 / 0). A fee the office has since edited is left alone, so re-running this cannot
--     overwrite their own correction.
--   * The single PET-9048 code could not carry two fees: the card charges CCE under RA 9048 1,000
--     and CFN under RA 9048 3,000. It is split, and the combined code is deactivated (kept, not
--     deleted, for any history that names it).
--   * amount becomes NULL-able. NULL means the office has not stated an amount - the card leaves
--     burial permit and transfer of cadaver blank. A 0.00 there would print "free" on a receipt,
--     which nobody said. The cashier types the amount for a NULL fee.
--   * REG-BIRTH / REG-MARRIAGE / REG-DEATH are not on the card; their 0.00 was a seed guess. They are
--     deactivated rather than kept at a "free" amount nobody stated.
--
-- PAYMENTS
--   * transaction_id becomes NULL-able: the office collects payments that start in no CROMS module
--     (walk-in, miscellaneous). The foreign key stays.
--   * payer_name, purpose (the "purpose of transaction" the office asked for), source
--     ('Transaction' / 'Walk-in' / 'BREQS' / 'Marriage License') and source_table / source_id for a
--     payment recorded from another module.
--   * payment_items: which fees one Official Receipt covered, with quantity and amount. Payments
--     recorded before this migration are NOT itemised after the fact - what fees they covered was
--     never recorded, and guessing would put a fabricated line in the collection report. They are
--     reported as "not itemised".
--
-- ASCII only (applied by piping into the mysql client). Idempotent.

DROP PROCEDURE IF EXISTS _croms_41;
DELIMITER //
CREATE PROCEDURE _croms_41()
BEGIN
    -- ---- fees columns
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'fees' AND COLUMN_NAME = 'category') THEN
        ALTER TABLE `fees`
            MODIFY COLUMN `amount` DECIMAL(10,2) NULL DEFAULT NULL,
            ADD COLUMN `category`   VARCHAR(30)  NULL AFTER `description`,
            ADD COLUMN `card_note`  VARCHAR(120) NULL AFTER `amount`,
            ADD COLUMN `sort_order` INT NOT NULL DEFAULT 100 AFTER `card_note`;
    END IF;

    -- ---- payments columns
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'payer_name') THEN
        ALTER TABLE `payments`
            MODIFY COLUMN `transaction_id` INT NULL,
            ADD COLUMN `payer_name`   VARCHAR(150) NULL AFTER `transaction_id`,
            ADD COLUMN `purpose`      VARCHAR(150) NULL AFTER `payer_name`,
            ADD COLUMN `source`       VARCHAR(20)  NOT NULL DEFAULT 'Transaction' AFTER `purpose`,
            ADD COLUMN `source_table` VARCHAR(30)  NULL AFTER `source`,
            ADD COLUMN `source_id`    INT          NULL AFTER `source_table`,
            ADD KEY `ix_payments_paid_at` (`paid_at`),
            ADD KEY `ix_payments_or` (`or_number`),
            ADD KEY `ix_payments_source` (`source_table`, `source_id`);
    END IF;
END //
DELIMITER ;
CALL _croms_41();
DROP PROCEDURE IF EXISTS _croms_41;

CREATE TABLE IF NOT EXISTS `payment_items` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `payment_id`  INT NOT NULL,
  `fee_id`      INT NULL,
  `fee_code`    VARCHAR(30)  NULL,
  `description` VARCHAR(150) NOT NULL,
  `quantity`    INT NOT NULL DEFAULT 1,
  `unit_amount` DECIMAL(10,2) NOT NULL,
  `line_amount` DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_payment_items_payment` (`payment_id`),
  KEY `ix_payment_items_fee` (`fee_code`),
  CONSTRAINT `fk_payment_items_payment` FOREIGN KEY (`payment_id`) REFERENCES `payments` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_payment_items_fee` FOREIGN KEY (`fee_id`) REFERENCES `fees` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---- corrections to the 2026-07-14 seeds, only where the seed value is still there
UPDATE `fees` SET `amount` = 80.00, `description` = 'Certified copy - Birth certificate', `category` = 'Certification',
       `card_note` = 'Certified copy (50 + 30)', `sort_order` = 20 WHERE `code` = 'CTC-BIRTH' AND `amount` = 155.00;
UPDATE `fees` SET `amount` = 80.00, `description` = 'Certified copy - Marriage certificate', `category` = 'Certification',
       `card_note` = 'Certified copy (50 + 30)', `sort_order` = 21 WHERE `code` = 'CTC-MARRIAGE' AND `amount` = 155.00;
UPDATE `fees` SET `amount` = 80.00, `description` = 'Certified copy - Death certificate', `category` = 'Certification',
       `card_note` = 'Certified copy (50 + 30)', `sort_order` = 22 WHERE `code` = 'CTC-DEATH' AND `amount` = 155.00;
UPDATE `fees` SET `amount` = 130.00, `description` = 'Certification (incl. negative certification)', `category` = 'Certification',
       `card_note` = 'Certification fee (100 + 30)', `sort_order` = 30 WHERE `code` = 'NEG-CERT' AND `amount` = 210.00;
UPDATE `fees` SET `amount` = 3000.00, `description` = 'Filing fee - CCE (RA 10172)', `category` = 'Petition',
       `card_note` = 'Filing fee CCE-RA 10172 (3,000)', `sort_order` = 110 WHERE `code` = 'PET-10172' AND `amount` = 0.00;
UPDATE `fees` SET `amount` = NULL, `description` = 'Burial permit fee', `category` = 'Permit',
       `card_note` = 'Burial permit fee (no amount on the card)', `sort_order` = 140 WHERE `code` = 'BURIAL' AND `amount` = 0.00;
UPDATE `fees` SET `is_active` = 0, `category` = 'Petition',
       `card_note` = 'Replaced by PET-9048-CCE and PET-9048-CFN (the card charges them differently)' WHERE `code` = 'PET-9048' AND `amount` = 0.00;
UPDATE `fees` SET `is_active` = 0, `category` = 'Registration', `card_note` = 'Not on the office fee card'
 WHERE `code` IN ('REG-BIRTH', 'REG-MARRIAGE', 'REG-DEATH') AND `amount` = 0.00;

-- ---- fees on the card that had no row
INSERT IGNORE INTO `fees` (`code`, `description`, `category`, `amount`, `card_note`, `sort_order`, `is_active`) VALUES
  ('BREQS',              'BREQS fee (PSA copy), per copy',   'BREQS',         50.00,   'BREQS fee (50)',                           10, 1),
  ('ANNOTATION',         'Annotation fee',                   'Certification', 100.00,  'Annotation fee (100)',                     40, 1),
  ('MAR-APPLICATION',    'Marriage application fee',         'Marriage',      1000.00, 'Marriage application fee (1,000)',         50, 1),
  ('MAR-SOLEMNIZATION',  'Marriage solemnization fee',       'Marriage',      1000.00, 'Marriage solemnization fee (1,000)',       60, 1),
  ('MAR-LICENSE',        'Marriage license fee',             'Marriage',      200.00,  'Marriage license fee (200)',               70, 1),
  ('ENDORSE-ELECTRONIC', 'Electronic endorsement fee',       'Endorsement',   500.00,  'Electronic endorsement fee (500)',         80, 1),
  ('REG-OUT-OF-TOWN',    'Out of town registration',         'Registration',  500.00,  'Out of town registration (500)',           90, 1),
  ('PET-9048-CCE',       'Filing fee - CCE (RA 9048)',       'Petition',      1000.00, 'Filing fee CCE-RA 9048 (1,000)',          100, 1),
  ('PET-9048-CFN',       'Filing fee - CFN (RA 9048)',       'Petition',      3000.00, 'Filing fee CFN-RA 9048 (3,000)',          120, 1),
  ('PET-MIGRANT',        'Migrant petition fee',             'Petition',      1000.00, 'Migrant petition fee (1,000)',            130, 1),
  ('TRANSFER-CADAVER',   'Transfer of cadaver',              'Permit',        NULL,    'Transfer of cadaver (no amount on the card)', 150, 1);
