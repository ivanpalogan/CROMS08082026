-- =====================================================================
-- 20_claimant_photo.sql
-- Release & Claim: the mouse-drawn signature is replaced by a webcam
-- photo of the claimant taken at release (proof of who claimed the
-- document). Add a dedicated LONGBLOB column; `signature_image` is left
-- in place (unused going forward). Idempotent (guarded).
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_add_claimant_photo`;
DELIMITER $$
CREATE PROCEDURE `_croms_add_claimant_photo`()
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'releases' AND COLUMN_NAME = 'claimant_photo') THEN
    ALTER TABLE `releases` ADD COLUMN `claimant_photo` LONGBLOB NULL AFTER `signature_image`;
  END IF;
END$$
DELIMITER ;

CALL `_croms_add_claimant_photo`();
DROP PROCEDURE `_croms_add_claimant_photo`;
