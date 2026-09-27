-- =====================================================================
-- 70_digitization_metadata.sql
-- Step 10 (Legacy Digitization): every record committed to births/deaths
-- from Intelligent Document Processing (or hand-added on the "Old Birth/
-- Death Records (OCR)" screens) must carry, on the record itself:
--   digitized_by      - who committed it (full name / username)
--   date_digitized     - when it was committed
--   encoding_method    - 'OCR + Manual Verification' (came through the
--                        OCR wizard) or 'Manual' (typed in directly, no
--                        scan at all)
--   source_reference   - the original file name / capture source
--                        ("nice.jpg", "Mobile Capture", "Phone scan"),
--                        so the record states where its image came from
--                        even after ocr_batch's own row ages out
-- The scanned image itself is NOT duplicated here - births/deaths already
-- keep it in birth_image/scan_image (2026-09-02) and the full read/audit
-- trail already lives in ocr_batch + ocr_field_audit (record_table/
-- record_id links back, 2026-09-02/09-04), so this migration only adds
-- the four fields nothing already carries directly on the record.
-- "Year" is deliberately NOT a separate column - book_volume already IS
-- the registry-book year (retired as its own field on 2026-09-02; see
-- Registry Books grouping by book_volume, 2026-09-15) and duplicating it
-- here would be the same fact kept in two places with nothing to stop
-- them disagreeing, which this project has refused every other time it
-- came up (registry-number regexes 2026-09-06, Others-box category/detail
-- split 2026-09-10).
-- record_source already distinguishes a backlog record from a live
-- registration ('OCR-Backlog' vs 'Registration', migration 68) - not
-- renamed to "Legacy Digitization" here to avoid touching every existing
-- query/string literal built on that value for no functional gain.
-- Idempotent (guarded ADD COLUMN, ASCII only).
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_digitization_metadata`;
DELIMITER $$
CREATE PROCEDURE `_croms_digitization_metadata`()
BEGIN
  -- births
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'digitized_by') THEN
    ALTER TABLE `births` ADD COLUMN `digitized_by` VARCHAR(80) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'date_digitized') THEN
    ALTER TABLE `births` ADD COLUMN `date_digitized` DATETIME NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'encoding_method') THEN
    ALTER TABLE `births` ADD COLUMN `encoding_method` VARCHAR(40) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'source_reference') THEN
    ALTER TABLE `births` ADD COLUMN `source_reference` VARCHAR(255) NULL;
  END IF;

  -- deaths
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'digitized_by') THEN
    ALTER TABLE `deaths` ADD COLUMN `digitized_by` VARCHAR(80) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'date_digitized') THEN
    ALTER TABLE `deaths` ADD COLUMN `date_digitized` DATETIME NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'encoding_method') THEN
    ALTER TABLE `deaths` ADD COLUMN `encoding_method` VARCHAR(40) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'source_reference') THEN
    ALTER TABLE `deaths` ADD COLUMN `source_reference` VARCHAR(255) NULL;
  END IF;
END$$
DELIMITER ;

CALL `_croms_digitization_metadata`();
DROP PROCEDURE `_croms_digitization_metadata`;
