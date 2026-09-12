-- 25_document_intelligence.sql
-- Intelligent Document Processing: per-field audit trail + document-level review state.
--
--   1. `ocr_field_audit` — one row per FIELD per disposition. This is what makes a
--      correction traceable: it stores what OCR originally read, what the operator
--      confirmed, the engine's confidence, the validation verdict, whether the value was
--      auto-corrected, whether the operator edited it, who did it and when.
--      `ocr_batch.raw_text` already keeps the page's whole original OCR text, so the two
--      together answer "what did the machine see, and what did the human change".
--
--   2. `ocr_batch` gains the document-level verdict: overall field confidence, whether it
--      was sent to manual review and why, the rotation that had to be applied, and the
--      user who ran it.
--
-- Idempotent: guarded by information_schema (MySQL has no ADD COLUMN IF NOT EXISTS).
-- Run once, after 24_ocr_routing.sql.

USE `croms`;

CREATE TABLE IF NOT EXISTS `ocr_field_audit` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `scan_id`        VARCHAR(30)  NOT NULL,          -- ocr_batch.scan_id
  `field_key`      VARCHAR(40)  NOT NULL,          -- canonical key, e.g. ChildFirst
  `field_label`    VARCHAR(80)  NULL,
  `ocr_value`      TEXT         NULL,              -- what OCR read, before any correction
  `final_value`    TEXT         NULL,              -- what was actually used
  `confidence`     INT          NULL,              -- 0-100, this field alone
  `status`         VARCHAR(20)  NULL,              -- Ok / Uncertain / Missing / Invalid / Conflict
  `issue`          VARCHAR(255) NULL,              -- the rule it broke, if any
  `auto_corrected` TINYINT(1)   NOT NULL DEFAULT 0,-- the engine repaired the reading
  `edited_by_user` TINYINT(1)   NOT NULL DEFAULT 0,-- the operator changed it in the grid
  `action`         VARCHAR(20)  NULL,              -- Committed / Draft / Auto-Filled / Manual Review
  `username`       VARCHAR(60)  NULL,
  `created_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_ocr_field_audit_scan` (`scan_id`),
  KEY `ix_ocr_field_audit_action` (`action`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DROP PROCEDURE IF EXISTS _croms_doc_intelligence;
DELIMITER //
CREATE PROCEDURE _croms_doc_intelligence()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'overall_confidence') THEN
        ALTER TABLE ocr_batch ADD COLUMN overall_confidence INT NULL AFTER confidence;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'needs_review') THEN
        ALTER TABLE ocr_batch ADD COLUMN needs_review TINYINT(1) NOT NULL DEFAULT 0 AFTER overall_confidence;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'review_reason') THEN
        ALTER TABLE ocr_batch ADD COLUMN review_reason VARCHAR(255) NULL AFTER needs_review;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'rotation_applied') THEN
        ALTER TABLE ocr_batch ADD COLUMN rotation_applied INT NULL AFTER review_reason;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ocr_batch'
          AND COLUMN_NAME = 'username') THEN
        ALTER TABLE ocr_batch ADD COLUMN username VARCHAR(60) NULL AFTER rotation_applied;
    END IF;
END //
DELIMITER ;
CALL _croms_doc_intelligence();
DROP PROCEDURE IF EXISTS _croms_doc_intelligence;
