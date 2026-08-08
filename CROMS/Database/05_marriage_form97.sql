-- =====================================================================
-- CROMS — 05_marriage_form97.sql
-- Supports the Marriage Registration & License screen:
--  * marriages gets registry/book/status/solemnizer (Form 97 workflow)
--  * new marriage_licenses table for the Form 90 posting-period tracker
-- Run once, after the earlier scripts. Safe on existing rows.
-- =====================================================================
USE `croms`;

ALTER TABLE `marriages`
  ADD COLUMN `registry_no` VARCHAR(30) NULL AFTER `id`,
  ADD COLUMN `book_volume` VARCHAR(30) NULL AFTER `registry_no`,
  ADD COLUMN `status`      VARCHAR(30) NOT NULL DEFAULT 'Draft' AFTER `book_volume`,
  ADD COLUMN `solemnizer`  VARCHAR(120) NULL AFTER `status`;

-- Existing migrated marriages are historical -> Registered.
UPDATE `marriages` SET `status` = 'Registered' WHERE `status` = 'Draft';

-- Form 90 marriage-license posting tracker.
CREATE TABLE IF NOT EXISTS `marriage_licenses` (
  `id`           INT NOT NULL AUTO_INCREMENT,
  `license_no`   VARCHAR(30)  NULL,
  `husband_name` VARCHAR(120) NULL,
  `wife_name`    VARCHAR(120) NULL,
  `filed_date`   DATE NULL,
  `posting_ends` DATE NULL,
  `status`       VARCHAR(20) NOT NULL DEFAULT 'Posting',  -- Posting / Issued
  `created_at`   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
