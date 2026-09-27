-- =====================================================================
-- 71_old_marriage_records_source.sql
-- Marriage gets the same "Marriage Record" workbench Birth and Death already have
-- (migration 68/70) -- a full Add/Edit/View/Delete screen over old, already-registered
-- marriages hand-transcribed or digitized from the paper registry books, separate from
-- the live Marriage Registration (Form 97) screen used for today's solemnizations.
--
-- Migration 68's own note said marriage did not need `record_source` because its OCR
-- path wrote straight into the live Form 97 record -- that decision is UNCHANGED (a
-- scanned marriage certificate still goes through the live screen). This migration is
-- for the SEPARATE, later request: a dedicated backlog workbench, exactly parallel to
-- "Old Birth/Death Records (OCR)", for a marriage that only ever existed as a paper
-- registry-book entry and is being typed in directly (Manual encoding_method, same as
-- the Old Birth/Death screens' own "no scan behind it" rows).
--
-- `place_of_marriage` is a plain free-text column, deliberately separate from the live
-- screen's FK trio (church_id / place_municipality_id / place_province_id) -- a decades-
-- old paper entry naming a church or barangay that closed or was never entered into
-- those lookup tables should not force a lossy best-fit match onto a live-record FK the
-- same way births.place_of_birth is a single free-text field rather than three FKs.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_old_marriage_records_source`;
DELIMITER $$
CREATE PROCEDURE `_croms_old_marriage_records_source`()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'record_source') THEN
    ALTER TABLE `marriages` ADD COLUMN `record_source` VARCHAR(20) NOT NULL DEFAULT 'Registration' AFTER `status`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'place_of_marriage') THEN
    ALTER TABLE `marriages` ADD COLUMN `place_of_marriage` VARCHAR(200) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'remarks') THEN
    ALTER TABLE `marriages` ADD COLUMN `remarks` VARCHAR(255) NULL;
  END IF;

  -- Step 10 digitization metadata, mirroring 70_digitization_metadata.sql for births/deaths.
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'digitized_by') THEN
    ALTER TABLE `marriages` ADD COLUMN `digitized_by` VARCHAR(80) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'date_digitized') THEN
    ALTER TABLE `marriages` ADD COLUMN `date_digitized` DATETIME NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'encoding_method') THEN
    ALTER TABLE `marriages` ADD COLUMN `encoding_method` VARCHAR(40) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'source_reference') THEN
    ALTER TABLE `marriages` ADD COLUMN `source_reference` VARCHAR(255) NULL;
  END IF;
END$$
DELIMITER ;

CALL `_croms_old_marriage_records_source`();
DROP PROCEDURE `_croms_old_marriage_records_source`;
