-- 34_kiosk_id_upload.sql
-- Adds a valid-ID TYPE to the kiosk intake (Step 2 "Personal Info & Photo"): which kind of ID
-- (Philippine National ID, Driver's License, etc.) the client says they will present. The actual
-- ID PHOTO is not stored via a local kiosk file browse (a public kiosk PC has no client ID
-- images in its own filesystem) -- it is captured the same way Release & Claim already does it:
-- the client scans a QR with their own phone and uploads it through claimapp into
-- claim_requests.id_image (see 21_workflow_enhancements.sql / 23_claim_queue_link.sql). That QR
-- box is now shown on EVERY kiosk visit's Step 2, not only Release & Claim pickups, so no new
-- image column is needed here. Idempotent (guarded ADD COLUMN via information_schema, since
-- MySQL has no ADD COLUMN IF NOT EXISTS).

DELIMITER $$
CREATE PROCEDURE _croms_34_add_col(IN tbl VARCHAR(64), IN col VARCHAR(64), IN ddl VARCHAR(255))
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
  ) THEN
    SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN ', ddl);
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$
DELIMITER ;

CALL _croms_34_add_col('queue_tickets', 'valid_id_type', '`valid_id_type` VARCHAR(60) NULL AFTER `id_image`');

DROP PROCEDURE _croms_34_add_col;
