-- =====================================================================
-- CROMS — 12_widen_type_label.sql
-- Multi-service kiosk tickets store the comma-joined service labels in
-- `queue_tickets.type_label`. Several services overflow the old VARCHAR(50)
-- ("Data too long for column 'type_label'"). Widen it to 255.
-- `document_type` stays VARCHAR(50) — it only holds the single primary service.
-- Run once.
-- =====================================================================
USE `croms`;

ALTER TABLE `queue_tickets`
  MODIFY COLUMN `type_label` VARCHAR(255) NULL;
