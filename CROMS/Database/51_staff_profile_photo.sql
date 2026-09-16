-- 51_staff_profile_photo.sql
-- Optional profile photo for the signed-in staff member's My Profile card.
-- Expand-only and idempotent: existing staff_biodata rows and older app builds remain valid.
-- Rollback, only if no deployed app still reads these columns:
--   ALTER TABLE staff_biodata DROP COLUMN profile_photo, DROP COLUMN profile_photo_name;

DROP PROCEDURE IF EXISTS _croms_51_staff_profile_photo;
DELIMITER //
CREATE PROCEDURE _croms_51_staff_profile_photo()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'staff_biodata'
          AND COLUMN_NAME = 'profile_photo'
    ) THEN
        ALTER TABLE `staff_biodata`
            ADD COLUMN `profile_photo` LONGBLOB NULL AFTER `emergency_contact_no`,
            ADD COLUMN `profile_photo_name` VARCHAR(255) NULL AFTER `profile_photo`;
    END IF;
END //
DELIMITER ;

CALL _croms_51_staff_profile_photo();
DROP PROCEDURE IF EXISTS _croms_51_staff_profile_photo;
