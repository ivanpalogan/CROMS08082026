-- 35_kiosk_spouse.sql
-- Marriage Application / Marriage Registration are the two kiosk services about TWO people, not
-- one -- husband and wife. Step 2 ("Personal Info & Photo") now collects a second name and a
-- second photo for these two services only. Adds `queue_tickets.spouse_full_name` VARCHAR(150)
-- and `spouse_image` LONGBLOB, both NULL for every other service. Idempotent (guarded ADD
-- COLUMN via information_schema, since MySQL has no ADD COLUMN IF NOT EXISTS).

DELIMITER $$
CREATE PROCEDURE _croms_35_add_col(IN tbl VARCHAR(64), IN col VARCHAR(64), IN ddl VARCHAR(255))
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

CALL _croms_35_add_col('queue_tickets', 'spouse_full_name', '`spouse_full_name` VARCHAR(150) NULL AFTER `full_name`');
CALL _croms_35_add_col('queue_tickets', 'spouse_image',     '`spouse_image` LONGBLOB NULL AFTER `id_image`');

DROP PROCEDURE _croms_35_add_col;
