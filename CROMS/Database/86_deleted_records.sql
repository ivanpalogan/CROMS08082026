-- =====================================================================
-- 86_deleted_records.sql
-- A civil registry record is never lost by a Delete. Before a birth, death or
-- marriage is removed, a complete copy of the row (scanned images included) is
-- written here together with WHO removed it, WHEN and WHY, and the row is then
-- removed from its table in the SAME transaction - either both happen or
-- neither does. An administrator can restore it (Settings > Audit Trail >
-- Deleted Records).
--
-- WHY A SNAPSHOT TABLE AND NOT is_deleted COLUMNS (the unapplied migration 69
-- plan, now removed). A soft-delete flag only works if EVERY query that reads
-- births / deaths / marriages remembers to exclude flagged rows - about 150
-- statements, the certificate views, the PSA monthly counts, the analytics
-- widgets and the generic archive browser. One missed filter and a "deleted"
-- record is printed on a certificate or counted in the return sent to PSA. With a
-- snapshot, the record really is gone from every table the app reads, so there
-- is nothing to forget, and it is still recoverable.
--
-- row_json holds every column of the deleted row as [name, .NET type, value]
-- triples (binary values base64), so it does not depend on the table's column
-- list: a column added by a later migration neither breaks nor is lost by a
-- restore (columns that no longer exist are skipped, new ones take their default).
-- children_json holds the record's requirement rows (document_requirements)
-- that were removed with it.
--
-- registry_no is kept as its own column so RegistryNumber.Next can see it:
-- the number of a deleted record is NOT handed to the next registration.
--
-- Idempotent - safe to re-run. ASCII only.
-- =====================================================================

CREATE TABLE IF NOT EXISTS `deleted_records` (
  `id`              INT NOT NULL AUTO_INCREMENT,
  `source_table`    VARCHAR(40)  NOT NULL,
  `record_id`       INT          NOT NULL,
  `registry_no`     VARCHAR(30)  NULL,
  `record_label`    VARCHAR(200) NULL,
  `reason`          VARCHAR(255) NOT NULL,
  `deleted_by`      INT          NULL,
  `deleted_by_name` VARCHAR(120) NULL,
  `deleted_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `row_json`        LONGTEXT     NOT NULL,
  `children_json`   LONGTEXT     NULL,
  `restored_at`     DATETIME     NULL,
  `restored_by`     INT          NULL,
  PRIMARY KEY (`id`),
  KEY `ix_deleted_records_source` (`source_table`, `record_id`),
  KEY `ix_deleted_records_registry` (`source_table`, `registry_no`),
  KEY `ix_deleted_records_when` (`deleted_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
