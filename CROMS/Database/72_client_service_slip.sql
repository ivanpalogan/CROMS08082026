-- =====================================================================
-- 72_client_service_slip.sql
-- Client Service Slip: the paper slip handed to a client for a tracking-only transaction
-- (petition / case tracking, and the Transactions ledger). One row per source record, so
-- saving the same petition again re-prints the SAME control number instead of issuing a new
-- one. control_no is "YYYY-N" (N counts up within the year) and is UNIQUE.
-- Also gives petitions the requester it never captured (who is filing, and their
-- relationship to the document owner). ASCII only, idempotent.
-- =====================================================================
CREATE TABLE IF NOT EXISTS client_service_slips (
  id               INT NOT NULL AUTO_INCREMENT,
  control_no       VARCHAR(20) NOT NULL,
  slip_year        SMALLINT NOT NULL,
  slip_seq         INT NOT NULL,
  source_table     VARCHAR(30) NOT NULL,
  source_id        INT NOT NULL,
  requester_name   VARCHAR(150) NULL,
  relationship     VARCHAR(80) NULL,
  document_owner   VARCHAR(200) NULL,
  doc_type         VARCHAR(20) NULL,
  doc_type_other   VARCHAR(80) NULL,
  txn_date         DATE NULL,
  attending_staff  VARCHAR(120) NULL,
  remarks          TEXT NULL,
  created_by       INT NULL,
  created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_slip_control (control_no),
  UNIQUE KEY uq_slip_year_seq (slip_year, slip_seq),
  UNIQUE KEY uq_slip_source (source_table, source_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DROP PROCEDURE IF EXISTS _croms_slip72;
DELIMITER //
CREATE PROCEDURE _croms_slip72()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'petitions' AND column_name = 'requester_name') THEN
    ALTER TABLE petitions ADD COLUMN requester_name VARCHAR(150) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'petitions' AND column_name = 'requester_relationship') THEN
    ALTER TABLE petitions ADD COLUMN requester_relationship VARCHAR(80) NULL;
  END IF;
END//
DELIMITER ;
CALL _croms_slip72();
DROP PROCEDURE IF EXISTS _croms_slip72;
