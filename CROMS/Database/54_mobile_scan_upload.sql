-- 54_mobile_scan_upload.sql
-- Lets a phone scan land directly in the Intelligent Document Processing
-- queue instead of being classified/extracted on the device. The phone's
-- own OCR engine (tesseract.js, in-browser) has no access to DocLayouts,
-- PageFit or the office vocabulary the desktop reads with — measured
-- 2026-07-28/09-09: desktop region-based extraction runs 54% exact field
-- values on the office's own samples, the phone's label-only path runs
-- 30% on the SAME samples. Sending the raw capture into this queue means
-- every mobile scan is read by the stronger engine, at the cost of a
-- staff member opening it here instead of the phone auto-filling a form.
--
--   source       — 'Desktop' (default, unchanged existing rows) or
--                  'Mobile'. Lets the batch grid and any future report
--                  tell a phone capture apart from a locally loaded scan.
--   source_image — the raw uploaded bytes, kept ONLY until the scan is
--                  opened on the desktop (OcrDigitizationForm re-inserts
--                  its own SCN- batch row via the normal Analyze/LogBatch
--                  path and clears this row rather than duplicating it —
--                  see OcrDigitizationForm.OpenMobileScan). A row that is
--                  still 'Pending Review' with bytes attached has not yet
--                  been looked at by anyone.
--
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).
USE `croms`;

DROP PROCEDURE IF EXISTS _croms_mobile_scan_upload;
DELIMITER //
CREATE PROCEDURE _croms_mobile_scan_upload()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'source'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN source VARCHAR(20) NOT NULL DEFAULT 'Desktop' AFTER username;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'source_image'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN source_image LONGBLOB NULL AFTER source;
    END IF;
END //
DELIMITER ;
CALL _croms_mobile_scan_upload();
DROP PROCEDURE IF EXISTS _croms_mobile_scan_upload;
