-- 17_scan_softcopy.sql
-- Softcopy of the source certificate: when a record is created via Document AI (or a
-- scan is attached), the ORIGINAL uploaded image is kept with the record so staff can
-- re-print the exact original document later. One LONGBLOB per registry record.

ALTER TABLE `births`    ADD COLUMN `scan_image` LONGBLOB NULL AFTER `remarks`;
ALTER TABLE `marriages` ADD COLUMN `scan_image` LONGBLOB NULL;
ALTER TABLE `deaths`    ADD COLUMN `scan_image` LONGBLOB NULL;
