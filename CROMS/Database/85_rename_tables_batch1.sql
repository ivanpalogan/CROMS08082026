-- =====================================================================
-- 85_rename_tables_batch1.sql
-- Rename eight tables whose names misled or looked like another table.
-- Names only: columns, data, indexes and foreign keys are untouched
-- (MySQL RENAME TABLE carries the foreign keys with the table).
--
--   marriage_requirements       -> document_requirements
--   marriage_requirement_types  -> document_requirement_types
--       (also used by delayed birth, petitions and licences)
--   window_transactions         -> window_service_assignments
--       (which services a window may serve, not transactions that happened)
--   ctc_requests                -> kiosk_ctc_intake
--       (what the client typed at the kiosk; not certificate_requests)
--   breqs_requests              -> psa_copy_requests
--   breqs_history               -> psa_copy_history
--   claim_requests              -> claimant_id_uploads
--       (ID / authorization-letter photos uploaded from the phone)
--   marriage_history            -> marriage_case_history
--
-- WHAT IS DELIBERATELY NOT CHANGED
--   * audit_log.table_name keeps the name each row was written under:
--     the audit trail records what was true at the time and is not rewritten.
--   * Constraint and index names keep their old spelling (cosmetic only).
--   * Older migration files still name the old tables; they run before this
--     one on a fresh install, so a fresh database ends with the new names.
--     Do NOT re-run an older migration on a database that already ran this
--     one - its CREATE TABLE IF NOT EXISTS would create an empty old table.
--
-- One stored link value is updated: payments.source_table holds the table a
-- payment was recorded from, and PaymentService compares it when it adopts
-- or refuses an Official Receipt.
-- Idempotent - safe to re-run (a table is renamed only when the old name
-- exists and the new name does not). ASCII only.
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_rename_batch1`;
DELIMITER $$
CREATE PROCEDURE `_croms_rename_batch1`()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirements')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_requirements') THEN
    RENAME TABLE `marriage_requirements` TO `document_requirements`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirement_types')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_requirement_types') THEN
    RENAME TABLE `marriage_requirement_types` TO `document_requirement_types`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'window_transactions')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'window_service_assignments') THEN
    RENAME TABLE `window_transactions` TO `window_service_assignments`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ctc_requests')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kiosk_ctc_intake') THEN
    RENAME TABLE `ctc_requests` TO `kiosk_ctc_intake`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'breqs_requests')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'psa_copy_requests') THEN
    RENAME TABLE `breqs_requests` TO `psa_copy_requests`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'breqs_history')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'psa_copy_history') THEN
    RENAME TABLE `breqs_history` TO `psa_copy_history`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'claim_requests')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'claimant_id_uploads') THEN
    RENAME TABLE `claim_requests` TO `claimant_id_uploads`;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_history')
     AND NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_case_history') THEN
    RENAME TABLE `marriage_history` TO `marriage_case_history`;
  END IF;

  -- the one stored link value (see header)
  IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments') THEN
    UPDATE `payments` SET `source_table` = 'psa_copy_requests' WHERE `source_table` = 'breqs_requests';
  END IF;
END$$
DELIMITER ;

CALL `_croms_rename_batch1`();
DROP PROCEDURE `_croms_rename_batch1`;
