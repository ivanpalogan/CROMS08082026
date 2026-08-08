-- =====================================================================
-- CROMS — 13_windows.sql
-- Dynamic service windows. Replaces the hardcoded 3 windows: the Now Serving
-- board and window assignment now read the active rows of this table, so an
-- admin can add / rename / activate / deactivate / reorder windows with no
-- code change. `queue_tickets.window_no` references `windows.id`.
-- Seeded with ids 1-3 so existing tickets (window_no 1..3) stay valid.
-- Run once.
-- =====================================================================
USE `croms`;

CREATE TABLE IF NOT EXISTS `windows` (
  `id`            INT NOT NULL AUTO_INCREMENT,
  `window_name`   VARCHAR(50) NOT NULL,
  `status`        VARCHAR(10) NOT NULL DEFAULT 'Active',   -- Active / Inactive
  `display_order` INT NOT NULL DEFAULT 0,
  `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Seed the original three windows only when the table is empty (idempotent).
INSERT INTO `windows` (`id`, `window_name`, `status`, `display_order`)
SELECT 1, 'Window 1', 'Active', 1 WHERE NOT EXISTS (SELECT 1 FROM `windows`)
UNION ALL SELECT 2, 'Window 2', 'Active', 2 WHERE NOT EXISTS (SELECT 1 FROM `windows`)
UNION ALL SELECT 3, 'Window 3', 'Active', 3 WHERE NOT EXISTS (SELECT 1 FROM `windows`);
