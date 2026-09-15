-- 48_office_email.sql
-- office_profile gains `email` - Form 3A/3B print "Tel. No. ... | Email: ..." on one line
-- under the office name (the office's own certification letterhead), and there was nowhere
-- to hold the email half of that line. Idempotent (guarded ADD COLUMN).
USE `croms`;

DROP PROCEDURE IF EXISTS _croms_office_email;
DELIMITER //
CREATE PROCEDURE _croms_office_email()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'office_profile'
          AND COLUMN_NAME = 'email') THEN
        ALTER TABLE `office_profile` ADD COLUMN `email` VARCHAR(120) NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_office_email();
DROP PROCEDURE IF EXISTS _croms_office_email;

-- Fill only where blank - never overwrite a value the office already set.
UPDATE `office_profile` SET `contact` = '(078) 304-8105' WHERE id = 1 AND (`contact` IS NULL OR `contact` = '');
UPDATE `office_profile` SET `email` = 'lcropenablanca@gmail.com' WHERE id = 1 AND (`email` IS NULL OR `email` = '');
