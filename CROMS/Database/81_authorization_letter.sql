-- 81_authorization_letter.sql
-- Release & Claim: when a REPRESENTATIVE collects a document, the claimant photographs the
-- authorization letter with their phone (a second QR next to the ID-upload one). The photo
-- lands on the claim_requests row while the claimant is at the counter (staging), and is
-- copied onto the permanent releases row when the officer presses Release, so the letter
-- stays with the release record. NULL = no letter was taken. Nothing is backfilled.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_auth_letter;
DELIMITER //
CREATE PROCEDURE _croms_auth_letter()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'claim_requests' AND COLUMN_NAME = 'auth_letter') THEN
        ALTER TABLE `claim_requests`
            ADD COLUMN `auth_letter`    LONGBLOB NULL,
            ADD COLUMN `auth_letter_at` DATETIME NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'releases' AND COLUMN_NAME = 'authorization_letter') THEN
        ALTER TABLE `releases`
            ADD COLUMN `authorization_letter` LONGBLOB NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_auth_letter();
DROP PROCEDURE IF EXISTS _croms_auth_letter;
