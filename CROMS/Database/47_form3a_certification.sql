-- 47_form3a_certification.sql
-- Municipal Form 3A - CERTIFICATION (Marriage Available), a "TO WHOM IT MAY CONCERN" letter
-- certifying facts already in the Register of Marriage. Not a copy of the Certificate of
-- Marriage (MF-97) - a separate document. Two additions:
--
--   1. office_assets.asset_kind gains three header logo slots + a footer banner slot, on the
--      same table Logo/Stamp already live in (2026-09-06) - the office manages each
--      independently (replace one seal without touching the others), same reasoning as
--      Logo vs Stamp.
--   2. office_profile gains the SECOND signatory this form needs. registrar_name/title
--      already covers "Municipal Civil Registrar"; this form is also signed "VERIFIED BY:"
--      a Registration Officer, which office_profile had nowhere to hold.
--
-- Idempotent: MODIFY COLUMN is safe to re-run (same target type); ADD COLUMN is guarded.
USE `croms`;

ALTER TABLE `office_assets`
  MODIFY COLUMN `asset_kind`
  ENUM('Logo','Stamp','HeaderLogoLeft','HeaderLogoRight1','HeaderLogoRight2','FooterBanner')
  NOT NULL;

DROP PROCEDURE IF EXISTS _croms_f3a_profile;
DELIMITER //
CREATE PROCEDURE _croms_f3a_profile()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'office_profile'
          AND COLUMN_NAME = 'verifying_officer_name') THEN
        ALTER TABLE `office_profile` ADD COLUMN `verifying_officer_name` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'office_profile'
          AND COLUMN_NAME = 'verifying_officer_title') THEN
        ALTER TABLE `office_profile` ADD COLUMN `verifying_officer_title` VARCHAR(80) NULL
            DEFAULT 'Registration Officer II';
    END IF;
END //
DELIMITER ;
CALL _croms_f3a_profile();
DROP PROCEDURE IF EXISTS _croms_f3a_profile;
