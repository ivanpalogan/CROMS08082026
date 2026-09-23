-- 58_ocr_upload_metadata.sql
-- Mobile OCR upload metadata + a stable link from the upload to the record it produces.
--
--   ocr_batch.client_name     — the name the client (or the office) confirmed at upload
--                                time (mobile lightweight OCR is a guess; the person
--                                confirms/corrects it on the phone before it is sent).
--   ocr_batch.requested_type  — the document type (Birth/Marriage/Death) the client
--                                picked on the phone before upload. This is the CLIENT'S
--                                claim, kept separate from doc_kind (the engine's own
--                                classification, set only once the scan is opened and
--                                fully read on the desktop) — the two are never conflated.
--   ocr_batch.final_registry_no — the registry number of the record this scan produced,
--                                filled in only once a birth/marriage/death module actually
--                                saves that record. Blank stays blank; a registry number is
--                                never guessed from the upload id or invented at this stage.
--
--   births.ocr_scan_id / deaths.ocr_scan_id — mirrors marriages.ocr_scan_id (migration 33):
--                                the record's own record of which scan produced it, so the
--                                registration module can write back to ocr_batch (mark it
--                                Processed, set final_registry_no) at the moment it saves.
--
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).
USE `croms`;

DROP PROCEDURE IF EXISTS _croms_ocr_upload_metadata;
DELIMITER //
CREATE PROCEDURE _croms_ocr_upload_metadata()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'client_name'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN client_name VARCHAR(150) NULL AFTER source_image;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'requested_type'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN requested_type VARCHAR(20) NULL AFTER client_name;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'final_registry_no'
    ) THEN
        ALTER TABLE ocr_batch ADD COLUMN final_registry_no VARCHAR(30) NULL AFTER requested_type;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'ocr_scan_id'
    ) THEN
        ALTER TABLE births ADD COLUMN ocr_scan_id VARCHAR(30) NULL AFTER scan_image;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths'
          AND COLUMN_NAME = 'ocr_scan_id'
    ) THEN
        ALTER TABLE deaths ADD COLUMN ocr_scan_id VARCHAR(30) NULL AFTER scan_image;
    END IF;
END //
DELIMITER ;
CALL _croms_ocr_upload_metadata();
DROP PROCEDURE IF EXISTS _croms_ocr_upload_metadata;
