-- 56_server_beacon.sql
-- WHICH MACHINE IS SERVING THIS DATABASE, and when it last said so.
--
-- Every office PC has MySQL and a `croms` database installed, so "localhost
-- connects" is NOT proof that this PC holds the LIVE registry: a kiosk or
-- display on a client laptop happily opened its own empty local copy and
-- looked like it was running while syncing with nobody. That is the bug this
-- table exists to make impossible.
--
-- The main CROMS app writes one row here, on a timer, but ONLY when the
-- database it is using is hosted on the machine it is running on. Every app
-- (main / kiosk / display) then asks each candidate server "how many seconds
-- since your beacon was written?" and picks the FRESHEST. The age is computed
-- by the answering MySQL server itself (TIMESTAMPDIFF against its own NOW()),
-- so two laptops with different clocks still compare correctly.
--
-- Deliberately NOT seeded: a row inserted by the migration would carry a fresh
-- CURRENT_TIMESTAMP and make a brand-new client database look like the live
-- server for the next few minutes. No row at all = "nobody is serving this
-- copy", which is the honest answer and the one that loses the comparison.
-- A database that never received this migration answers with an error, which
-- readers treat the same way.

CREATE TABLE IF NOT EXISTS server_beacon (
  id           TINYINT      NOT NULL DEFAULT 1,
  machine_name VARCHAR(80)  NULL,
  ip_list      VARCHAR(255) NULL,
  app_version  VARCHAR(40)  NULL,
  updated_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                            ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- The LAN account must be able to read AND write it: the beacon is written by
-- whichever CROMS app is running on the serving machine, and that app may be
-- connected through croms_user rather than root.
