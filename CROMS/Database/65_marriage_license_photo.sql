-- 65_marriage_license_photo.sql
-- Kiosk "Marriage License" gate (MarriageLicenseCheckForm) no longer asks the client to TYPE
-- their licence number - it asks them to PHOTOGRAPH the physical licence with the kiosk's
-- webcam instead. A typed number is the client's own transcription of a document they are
-- holding; a photo is the document itself, and this project has repeatedly found a typed
-- registry/licence number to be the least trustworthy field on a form (2026-09-06 registry-
-- label-collision entry, 2026-09-10 registry-number bug). The photo is what gets stored.
--
-- `queue_tickets.marriage_license_image` is a HOLDING column - the kiosk has no marriage
-- record yet (that is only created when a registrar opens Marriage Registration), so the
-- photo rides on the ticket exactly like the existing `id_image`/`spouse_image` kiosk photos
-- until PrepareForQueueTicket (or the desk operator) reads it.
--
-- `marriages.license_image` is the PERMANENT copy, viewable alongside `scan_image` on the
-- record - same reasoning as every other "attach the document to the record" column in this
-- project. NOT the same column as scan_image (the certificate itself): the licence and the
-- certificate are two different physical documents.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_marriage_license_photo;
DELIMITER //
CREATE PROCEDURE _croms_marriage_license_photo()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'queue_tickets' AND COLUMN_NAME = 'marriage_license_image') THEN
        ALTER TABLE `queue_tickets`
            ADD COLUMN `marriage_license_image` LONGBLOB NULL AFTER `spouse_image`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'license_image') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `license_image` LONGBLOB NULL AFTER `license_place`;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_license_photo();
DROP PROCEDURE IF EXISTS _croms_marriage_license_photo;
