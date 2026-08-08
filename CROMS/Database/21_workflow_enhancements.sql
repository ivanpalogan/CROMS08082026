-- =====================================================================
-- CROMS — 21_workflow_enhancements.sql
-- Queue / Window / Claim workflow enhancement spec (8 sections). Adds:
--   • windows.is_priority            — Priority Window (handles all services,
--                                       ticket stays until completion, never
--                                       auto-forwarded unless admin overrides).
--   • queue_tickets workflow/timing   — accepted / called / started / completed
--                                       timestamps, recall_count, final_status,
--                                       priority flag, and the window that
--                                       accepted a not-yet-called ticket.
--   • queue_ticket_forwards           — forward history (window→window), keeping
--                                       the same queue number.
--   • claim_requests                  — Claim Request service: QR token, claim
--                                       ticket, requester + ID (uploaded from the
--                                       claimapp mobile app), status, release info.
-- Idempotent — safe to re-run (MySQL has no ADD COLUMN IF NOT EXISTS, so column
-- adds are guarded via information_schema in a throwaway procedure).
-- Run once, after 20_claimant_photo.sql.
-- =====================================================================
USE `croms`;

-- ---------------------------------------------------------------------
-- Column additions (guarded)
-- ---------------------------------------------------------------------
DROP PROCEDURE IF EXISTS `_croms_migrate_workflow`;
DELIMITER $$
CREATE PROCEDURE `_croms_migrate_workflow`()
BEGIN
  -- Priority Window flag on the dynamic windows table.
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'windows' AND COLUMN_NAME = 'is_priority') THEN
    ALTER TABLE `windows` ADD COLUMN `is_priority` TINYINT(1) NOT NULL DEFAULT 0 AFTER `status`;
  END IF;

  -- Workflow / processing-time tracking on queue_tickets.
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'accepted_at') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `accepted_at` DATETIME NULL AFTER `created_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'accepted_window') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `accepted_window` INT NULL AFTER `accepted_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'called_at') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `called_at` DATETIME NULL AFTER `accepted_window`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'started_at') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `started_at` DATETIME NULL AFTER `called_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'completed_at') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `completed_at` DATETIME NULL AFTER `started_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'recall_count') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `recall_count` INT NOT NULL DEFAULT 0 AFTER `completed_at`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'is_priority_ticket') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `is_priority_ticket` TINYINT(1) NOT NULL DEFAULT 0 AFTER `recall_count`;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'final_status') THEN
    ALTER TABLE `queue_tickets` ADD COLUMN `final_status` VARCHAR(30) NULL AFTER `is_priority_ticket`;
  END IF;
END$$
DELIMITER ;
CALL `_croms_migrate_workflow`();
DROP PROCEDURE `_croms_migrate_workflow`;

-- ---------------------------------------------------------------------
-- Forward history: one row per window→window hand-off, same queue number.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `queue_ticket_forwards` (
  `id`            INT NOT NULL AUTO_INCREMENT,
  `ticket_id`     INT NOT NULL,                       -- FK queue_tickets.id
  `from_window`   INT NULL,                           -- windows.id (source)
  `to_window`     INT NULL,                           -- windows.id (destination)
  `service_code`  VARCHAR(20) NULL,                   -- service being forwarded
  `service_label` VARCHAR(80) NULL,
  `forwarded_by`  INT NULL,                           -- users.id who forwarded
  `note`          VARCHAR(255) NULL,
  `forwarded_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_qtf_ticket` (`ticket_id`),
  CONSTRAINT `fk_qtf_ticket`
    FOREIGN KEY (`ticket_id`) REFERENCES `queue_tickets` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- Claim Request: a client requests a document, gets a QR-coded claim
-- ticket. The claimapp mobile app scans the QR and uploads the client's
-- valid ID (image + OCR-extracted name). Staff scan the same QR in the
-- desktop Claim Form to verify identity and release.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `claim_requests` (
  `id`               INT NOT NULL AUTO_INCREMENT,
  `qr_token`         VARCHAR(64)  NOT NULL,            -- unique token encoded in the QR
  `claim_ticket_no`  VARCHAR(30)  NULL,               -- human claim ticket e.g. CLM-2026-0001
  `transaction_id`   INT NULL,                        -- links to transactions (optional)
  `first_name`       VARCHAR(80)  NULL,               -- requester (from the request)
  `middle_name`      VARCHAR(80)  NULL,
  `last_name`        VARCHAR(80)  NULL,
  `request_details`  VARCHAR(255) NULL,               -- what is being claimed
  `status`           VARCHAR(20)  NOT NULL DEFAULT 'Pending', -- Pending / IDUploaded / Verified / Released
  `id_image`         LONGBLOB     NULL,               -- uploaded valid ID photo
  `id_first_name`    VARCHAR(80)  NULL,               -- OCR-extracted from the ID
  `id_middle_name`   VARCHAR(80)  NULL,
  `id_last_name`     VARCHAR(80)  NULL,
  `id_uploaded_at`   DATETIME     NULL,
  `release_info`     VARCHAR(255) NULL,               -- notes captured at release
  `released_by`      INT NULL,
  `released_at`      DATETIME     NULL,
  `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_claim_qr` (`qr_token`),
  KEY `idx_claim_txn` (`transaction_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
