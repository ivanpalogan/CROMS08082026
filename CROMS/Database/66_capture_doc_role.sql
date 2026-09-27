-- 66_capture_doc_role.sql
-- Mobile Capture now photographs TWO different documents in one session: the Certificate of
-- Marriage (Form 97, the one OCR reads) and, optionally, the Marriage License (kept as a
-- picture only). Before this every uploaded page was the same kind of thing, so CROMS took
-- the NEWEST page as the certificate - which meant photographing the licence second turned
-- the licence into the "certificate", OCR read the wrong document and nothing was filled.
--
-- `doc_role` says which document a page IS: 'CERTIFICATE' or 'LICENSE'. It is NULL for
-- pages uploaded before this migration and for the final-registered-copy capture (Step 10),
-- which only ever carries one kind of document; CROMS reads NULL as CERTIFICATE.
--
-- Separate from `image_label`, which records the capture PURPOSE (pre-registration vs final
-- signed copy) - a different question.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_capture_doc_role;
DELIMITER //
CREATE PROCEDURE _croms_capture_doc_role()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'form97_capture_images' AND COLUMN_NAME = 'doc_role') THEN
        ALTER TABLE `form97_capture_images`
            ADD COLUMN `doc_role` VARCHAR(20) NULL AFTER `image_label`;
    END IF;
END //
DELIMITER ;
CALL _croms_capture_doc_role();
DROP PROCEDURE IF EXISTS _croms_capture_doc_role;
