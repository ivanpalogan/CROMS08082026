-- 40_breqs.sql
-- BREQS: requests for a PSA-issued copy of a birth, marriage or death certificate.
--
-- As the office described it (2026-09-13): the client logs a request - name, valid ID type and
-- number, which certificate, and the details of that document. CROMS keeps it with a status so
-- staff can see what is still pending. Staff collect the PSA copies from the PSA office in person;
-- when a copy arrives it is scanned through CROMS's OCR so the system records that THIS is the
-- document that person asked for, and keeps a copy. Intake happens at the kiosk AND the counter.
--
-- STATUSES STORED (the user asked CROMS to choose them):
--   Requested  Paid  Submitted to PSA  Received from PSA  Released  No Record at PSA  Cancelled
-- "Overdue" and "Unclaimed" are DERIVED from dates and settings and never stored, so they cannot
-- go stale - the same rule the marriage licence's "Expiring" follows.
--
-- ONE ROW PER REQUEST, the document details in generic columns whose meaning follows doc_type:
--   owner_*   Birth: the registrant   Marriage: the husband   Death: the deceased
--   spouse_*  Marriage only: the wife
--   event_*   date and place of the birth / marriage / death
--   father_name, mother_maiden_name   Birth only
-- Lists never select scan_image; the scan is only read when a request is opened.
--
-- No backfill: nothing existed before. ASCII only (applied by piping into the mysql client).
-- Idempotent: CREATE TABLE IF NOT EXISTS, INSERT IGNORE for settings.

CREATE TABLE IF NOT EXISTS `breqs_requests` (
  `id`                  INT NOT NULL AUTO_INCREMENT,
  `request_no`          VARCHAR(30)  NULL,
  `source`              VARCHAR(10)  NOT NULL DEFAULT 'Counter',
  `queue_ticket_id`     INT          NULL,

  `requester_first`     VARCHAR(60)  NULL,
  `requester_middle`    VARCHAR(60)  NULL,
  `requester_last`      VARCHAR(60)  NULL,
  `contact_no`          VARCHAR(30)  NULL,
  `relationship`        VARCHAR(40)  NULL,
  `valid_id_type`       VARCHAR(60)  NULL,
  `valid_id_no`         VARCHAR(60)  NULL,

  `doc_type`            VARCHAR(10)  NOT NULL,
  `copies`              INT          NOT NULL DEFAULT 1,
  `purpose`             VARCHAR(80)  NULL,
  `owner_first`         VARCHAR(60)  NULL,
  `owner_middle`        VARCHAR(60)  NULL,
  `owner_last`          VARCHAR(60)  NULL,
  `spouse_first`        VARCHAR(60)  NULL,
  `spouse_middle`       VARCHAR(60)  NULL,
  `spouse_last`         VARCHAR(60)  NULL,
  `event_date`          DATE         NULL,
  `event_city`          VARCHAR(120) NULL,
  `event_province`      VARCHAR(120) NULL,
  `father_name`         VARCHAR(150) NULL,
  `mother_maiden_name`  VARCHAR(150) NULL,

  `status`              VARCHAR(30)  NOT NULL DEFAULT 'Requested',
  `fee_amount`          DECIMAL(10,2) NULL,
  `or_no`               VARCHAR(40)  NULL,
  `or_date`             DATE         NULL,
  `psa_reference_no`    VARCHAR(60)  NULL,
  `submitted_at`        DATETIME     NULL,
  `expected_date`       DATE         NULL,

  `received_at`         DATETIME     NULL,
  `received_by`         INT          NULL,
  `scan_image`          LONGBLOB     NULL,
  `scan_id`             VARCHAR(30)  NULL,
  `ocr_doc_kind`        VARCHAR(20)  NULL,
  `ocr_confidence`      INT          NULL,
  `ocr_name`            VARCHAR(180) NULL,
  `ocr_match`           VARCHAR(20)  NULL,
  `psa_security_no`     VARCHAR(40)  NULL,

  `released_at`         DATETIME     NULL,
  `released_by`         INT          NULL,
  `claimant_name`       VARCHAR(180) NULL,
  `claimant_id_type`    VARCHAR(60)  NULL,
  `claimant_id_no`      VARCHAR(60)  NULL,
  `claimant_is_rep`     TINYINT(1)   NOT NULL DEFAULT 0,

  `outcome_reason`      VARCHAR(255) NULL,
  `remarks`             VARCHAR(255) NULL,
  `created_by`          INT          NULL,
  `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_breqs_request_no` (`request_no`),
  KEY `ix_breqs_status` (`status`),
  KEY `ix_breqs_ticket` (`queue_ticket_id`),
  KEY `ix_breqs_owner` (`owner_last`, `owner_first`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `breqs_history` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `request_id`  INT NOT NULL,
  `action`      VARCHAR(60)  NOT NULL,
  `from_status` VARCHAR(30)  NULL,
  `to_status`   VARCHAR(30)  NULL,
  `note`        VARCHAR(255) NULL,
  `user_id`     INT          NULL,
  `created_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_breqs_history_request` (`request_id`),
  CONSTRAINT `fk_breqs_history_request` FOREIGN KEY (`request_id`) REFERENCES `breqs_requests` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Office-adjustable numbers. The turnaround is the office's local estimate ("about one week"),
-- not a PSA commitment, which is why it is a setting and not a constant.
INSERT IGNORE INTO `app_settings` (`setting_key`, `setting_value`, `description`) VALUES
  ('BREQS_TURNAROUND_DAYS', '7',  'BREQS: days after submission to PSA before a request shows as Overdue. Local estimate - adjust.'),
  ('BREQS_UNCLAIMED_DAYS',  '30', 'BREQS: days after a PSA copy is received before it shows as Unclaimed.'),
  ('BREQS_FEE_PER_COPY',    '50', 'BREQS fee per copy, from the office fee card (PLS. PAY AT TREASURY OFFICE).');

-- The kiosk and window routing carried a placeholder service code 'BREKS' (a misspelling).
-- It becomes 'BREQS'; no rows used it on the live database, this covers any other copy.
UPDATE `window_transactions`   SET `service_code` = 'BREQS' WHERE `service_code` = 'BREKS';
UPDATE `queue_ticket_services` SET `service_code` = 'BREQS', `service_label` = 'PSA Copy (BREQS)' WHERE `service_code` = 'BREKS';
