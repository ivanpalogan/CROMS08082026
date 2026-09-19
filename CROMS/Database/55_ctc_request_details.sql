-- 55_ctc_request_details.sql
--
-- Two things, both about what the QUEUE STAFF actually receive when a ticket reaches them.
--
-- 1. `ctc_requests` — the kiosk's Certified True Copy intake, normalised.
--    Until now a CTC ticket carried the document type plus ONE free-text blob
--    ("Record details (name, registry number, year, or other helpful information)")
--    which was written into `queue_tickets.purpose`. That is not a record the office
--    can act on: it cannot be searched, it cannot be compared against the registry,
--    and what it contains depends entirely on what the client thought to type. Half
--    the time it is a name with no year; sometimes it is empty.
--    This table asks for the SAME things the office already asks for on a PSA copy
--    (see `breqs_requests`, migration 40) — whose record, when and where the event
--    happened, the parents for a birth, the spouse for a marriage — plus the local
--    registry number, which `breqs_requests` has no use for but which finds a
--    LOCAL record immediately when the client happens to know it.
--    It is intake, NOT the certificate request itself: `certificate_requests` is
--    created by staff at the counter once they have found the actual registry row
--    (it carries `record_id`). This table is what the client DESCRIBED, before any
--    record has been located — exactly the relationship `breqs_requests` has to the
--    BREQS desk.
--
-- 2. Abandon columns on `queue_ticket_services`. A client who leaves, or who asks for
--    something the office cannot do today, currently has no representation: the task
--    sits Pending forever and the visit can never be completed (CompleteVisit blocks
--    on any service that is not Completed). `status` is already VARCHAR, so the state
--    itself needs no migration — but WHY it was abandoned and WHEN does, otherwise the
--    queue statistics silently count an abandoned task as an unfinished one.
--
-- Idempotent. Safe to re-run.

CREATE TABLE IF NOT EXISTS `ctc_requests` (
  `id`                  INT NOT NULL AUTO_INCREMENT,
  `source`              VARCHAR(10)  NOT NULL DEFAULT 'Kiosk',   -- Kiosk / Counter
  `queue_ticket_id`     INT          NULL,
  `transaction_id`      INT          NULL,

  -- What is being asked for.
  `doc_type`            VARCHAR(10)  NOT NULL,                   -- Birth / Marriage / Death
  `copies`              INT          NOT NULL DEFAULT 1,
  `purpose`             VARCHAR(80)  NULL,
  `relationship`        VARCHAR(40)  NULL,                       -- requester's relationship to the owner

  -- Whose record. `registry_no` is the office's own number: when the client knows it
  -- the record is found in one lookup, which is why it is asked first.
  `registry_no`         VARCHAR(40)  NULL,
  `owner_first`         VARCHAR(60)  NULL,
  `owner_middle`        VARCHAR(60)  NULL,
  `owner_last`          VARCHAR(60)  NULL,
  `spouse_first`        VARCHAR(60)  NULL,                       -- marriage only
  `spouse_middle`       VARCHAR(60)  NULL,
  `spouse_last`         VARCHAR(60)  NULL,
  `event_date`          DATE         NULL,
  `event_city`          VARCHAR(120) NULL,
  `event_province`      VARCHAR(120) NULL,
  `father_name`         VARCHAR(150) NULL,                       -- birth only
  `mother_maiden_name`  VARCHAR(150) NULL,                       -- birth only

  -- Anything the structured fields have no room for. Kept deliberately: the old free
  -- text was the ONLY field, and it is still the right place for "the name might be
  -- spelled Baloso or Balozo". It is now an addition, never the whole request.
  `remarks`             VARCHAR(255) NULL,

  `status`              VARCHAR(30)  NOT NULL DEFAULT 'Requested',
  `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_ctcreq_ticket` (`queue_ticket_id`),
  KEY `ix_ctcreq_txn` (`transaction_id`),
  KEY `ix_ctcreq_owner` (`owner_last`, `owner_first`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DROP PROCEDURE IF EXISTS `_croms_ctc_details`;
DELIMITER $$
CREATE PROCEDURE `_croms_ctc_details`()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_ticket_services'
        AND COLUMN_NAME = 'abandoned_at') THEN
    ALTER TABLE `queue_ticket_services` ADD COLUMN `abandoned_at` DATETIME NULL;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_ticket_services'
        AND COLUMN_NAME = 'abandon_reason') THEN
    ALTER TABLE `queue_ticket_services` ADD COLUMN `abandon_reason` VARCHAR(255) NULL;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'queue_ticket_services'
        AND COLUMN_NAME = 'abandoned_by') THEN
    ALTER TABLE `queue_ticket_services` ADD COLUMN `abandoned_by` INT NULL;
  END IF;
END$$
DELIMITER ;
CALL `_croms_ctc_details`();
DROP PROCEDURE `_croms_ctc_details`;
