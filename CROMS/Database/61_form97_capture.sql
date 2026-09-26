-- 61_form97_capture.sql
-- Mobile Capture for Marriage Registration (Form 97): a temporary QR-linked token that
-- lets a phone photograph the paper certificate and upload it straight onto the
-- Marriage Registration screen that is open on the desk, BEFORE OCR runs.
--
-- Denormalized on purpose (husband_name/wife_name/txn_code are a snapshot, not a live
-- join): the marriage record the operator is filling in may not be saved yet (a brand
-- new "Register Marriage" dialog has no marriages.id), so the token cannot always FK to
-- one. marriage_id/transaction_id are filled in once the record actually exists.
--
-- One-time and expiring: status starts 'Pending', becomes 'Uploaded' on the first image,
-- 'Completed' once the phone taps Done (or the desktop closes the dialog) - a completed
-- or expired token is refused by the save-API even if the QR is scanned again.
--
-- CREATE TABLE IF NOT EXISTS is inherently idempotent - no ALTER-guard procedure needed
-- for a brand-new table. ASCII only, safe to apply by piping into mysql.

CREATE TABLE IF NOT EXISTS `form97_capture_tokens` (
    `id`             INT NOT NULL AUTO_INCREMENT,
    `token`          CHAR(32) NOT NULL,
    `marriage_id`    INT NULL,
    `transaction_id` INT NULL,
    `husband_name`   VARCHAR(150) NULL,
    `wife_name`      VARCHAR(150) NULL,
    `txn_code`       VARCHAR(40) NULL,
    `status`         ENUM('Pending','Uploaded','Completed','Expired') NOT NULL DEFAULT 'Pending',
    `expires_at`     DATETIME NOT NULL,
    `completed_at`   DATETIME NULL,
    `created_by`     INT NULL,
    `created_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_form97_capture_token` (`token`),
    KEY `ix_form97_capture_marriage` (`marriage_id`),
    KEY `ix_form97_capture_txn` (`transaction_id`),
    CONSTRAINT `fk_form97_capture_marriage`
        FOREIGN KEY (`marriage_id`) REFERENCES `marriages` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_form97_capture_txn`
        FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `form97_capture_images` (
    `id`           INT NOT NULL AUTO_INCREMENT,
    `token_id`     INT NOT NULL,
    `page_no`      INT NOT NULL DEFAULT 1,
    `image`        LONGBLOB NOT NULL,
    -- Every page arrives tagged with this fixed label - "the original image, before OCR" -
    -- so the audit trail and the capture dialog can both name what they are looking at
    -- without guessing from a filename (there is no filename; the bytes never touch disk).
    `image_label`  VARCHAR(40) NOT NULL DEFAULT 'INCOMING_FORM_97',
    `uploaded_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_form97_capture_images_token` (`token_id`),
    CONSTRAINT `fk_form97_capture_images_token`
        FOREIGN KEY (`token_id`) REFERENCES `form97_capture_tokens` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
