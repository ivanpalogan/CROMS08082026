-- =====================================================================
-- CROMS — 08_ocr.sql
-- Batch log for the OCR Digitization module. Each scanned page that is
-- run through OCR gets a row here (scan id, source book, class, confidence,
-- status), and committing it writes the record into the real registry.
-- Run once.
-- =====================================================================
USE `croms`;

CREATE TABLE IF NOT EXISTS `ocr_batch` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `scan_id`     VARCHAR(30)  NULL,
  `source_book` VARCHAR(120) NULL,
  `doc_class`   VARCHAR(60)  NULL,
  `confidence`  INT          NULL,
  `status`      VARCHAR(30)  NOT NULL DEFAULT 'For Review',  -- For Review / Committed / Low Confidence
  `raw_text`    MEDIUMTEXT   NULL,
  `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
