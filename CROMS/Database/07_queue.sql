-- =====================================================================
-- CROMS — 07_queue.sql
-- Adds the fields Queue Management needs on `queue_tickets`:
-- transaction label, priority lane, serving window, and who served.
-- Run once.
-- =====================================================================
USE `croms`;

ALTER TABLE `queue_tickets`
  ADD COLUMN `type_label` VARCHAR(50)  NULL AFTER `document_type`,
  ADD COLUMN `priority`   VARCHAR(20)  NOT NULL DEFAULT 'Regular' AFTER `type_label`,
  ADD COLUMN `window_no`  INT          NULL AFTER `priority`,
  ADD COLUMN `served_by`  VARCHAR(80)  NULL AFTER `window_no`;
