-- =====================================================================
-- CROMS — 11_kiosk_multiservice.sql
-- Multi-service kiosk requests: a client can pick several services in one
-- visit and receive a SINGLE queue number for the whole visit. Each chosen
-- service is stored as its own row in `queue_ticket_services`, all linked to
-- the one `queue_tickets` row (one queue number per transaction).
--
-- The existing single-service flow is unaffected: tickets with no rows in
-- this table behave exactly as before (their `type_label` is the service).
-- Run once.
-- =====================================================================
USE `croms`;

CREATE TABLE IF NOT EXISTS `queue_ticket_services` (
  `id`            INT NOT NULL AUTO_INCREMENT,
  `ticket_id`     INT NOT NULL,                        -- FK to queue_tickets.id
  `service_code`  VARCHAR(20)  NOT NULL,               -- e.g. CTC, NEWREG, DEATH
  `service_label` VARCHAR(80)  NOT NULL,               -- e.g. "Certified True Copy (CTC)"
  `status`        VARCHAR(20)  NOT NULL DEFAULT 'Pending',  -- Pending / Done (per-service progress)
  `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_qts_ticket` (`ticket_id`),
  CONSTRAINT `fk_qts_ticket`
    FOREIGN KEY (`ticket_id`) REFERENCES `queue_tickets` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
