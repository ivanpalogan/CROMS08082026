-- =====================================================================
-- CROMS — 14_window_assignment.sql
-- Window Assignment / Online-Offline presence / per-window transaction routing.
--
-- Adds an OPERATOR PRESENCE layer on top of the existing `windows` table:
--   • current_operator / operator_name / last_heartbeat — who is signed into
--     the window right now. A window is "Online" when current_operator IS NOT
--     NULL and its heartbeat is recent; otherwise "Offline". This is SEPARATE
--     from `windows.status` (Active/Inactive), which is the admin's toggle for
--     whether a window exists on the board at all.
--   • window_transactions — which service types each window may handle. NO rows
--     for a window means it handles ALL transactions.
--   • queue_ticket_services.locked_by_window — per-service lock so a service
--     can be claimed by exactly one window (used by multi-window forwarding).
-- Run once, after 13_windows.sql.
-- =====================================================================
USE `croms`;

-- --- Presence columns on windows -------------------------------------
ALTER TABLE `windows`
  ADD COLUMN `current_operator` INT         NULL AFTER `status`,
  ADD COLUMN `operator_name`    VARCHAR(80) NULL AFTER `current_operator`,
  ADD COLUMN `last_heartbeat`   DATETIME    NULL AFTER `operator_name`;

-- --- Transaction types a window is allowed to serve ------------------
-- service_code matches queue_ticket_services.service_code
-- (NEWREG / CTC / MARRIAGE / DEATH / PETITION / VERIFY).
-- A window with no rows here handles ALL transaction types.
CREATE TABLE IF NOT EXISTS `window_transactions` (
  `id`           INT NOT NULL AUTO_INCREMENT,
  `window_id`    INT NOT NULL,
  `service_code` VARCHAR(20) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_window_service` (`window_id`, `service_code`),
  CONSTRAINT `fk_wt_window`
    FOREIGN KEY (`window_id`) REFERENCES `windows` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --- Per-service lock for multi-window forwarding -------------------
-- Which window currently owns (is processing) a given service on a ticket.
-- NULL = not yet claimed. A ticket keeps ONE queue number while its services
-- are forwarded window-to-window; each service is locked to one window at a
-- time so no two windows call the same service.
ALTER TABLE `queue_ticket_services`
  ADD COLUMN `locked_by_window` INT NULL AFTER `status`;
