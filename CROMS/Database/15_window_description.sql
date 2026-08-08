-- =====================================================================
-- CROMS — 15_window_description.sql
-- Adds an optional free-text description to service windows, edited from the
-- Settings → Window Management tab (e.g. "Birth & Marriage counter",
-- "Cashier / releasing"). Purely informational; does not affect routing.
-- Run once, after 14_window_assignment.sql.
-- =====================================================================
USE `croms`;

ALTER TABLE `windows`
  ADD COLUMN `description` VARCHAR(255) NULL AFTER `window_name`;
