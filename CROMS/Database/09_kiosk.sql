-- =====================================================================
-- CROMS — 09_kiosk.sql
-- Adds the fields the client self-service kiosk (CROMS.Kiosk) captures:
-- purpose of the visit, contact number, and a photo of the client's ID.
-- These ride along on the same queue_tickets row the staff app already uses.
-- Run once.
-- =====================================================================
USE `croms`;

ALTER TABLE `queue_tickets`
  ADD COLUMN `purpose`    VARCHAR(255) NULL AFTER `full_name`,
  ADD COLUMN `contact_no` VARCHAR(30)  NULL AFTER `purpose`,
  ADD COLUMN `id_image`   LONGBLOB     NULL AFTER `contact_no`;
