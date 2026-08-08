-- =====================================================================
-- CROMS — 02_seed.sql : baseline rows the app needs to run.
-- Run AFTER 01_schema.sql.
--
-- Lookup tables (religions, places, etc.) are NOT seeded here — they are
-- copied from your real data in 03_migrate_from_ocr.sql. This file only
-- adds things the old DB did not have: a fee schedule and an admin user.
-- =====================================================================
USE `croms`;

-- ---------------------------------------------------------------------
-- Admin user.
-- NOTE: password_hash is a placeholder. Auth (BCrypt) is not built yet
-- (Phase 2 of the roadmap). When the login module is added, this hash
-- will be replaced with a real BCrypt hash and a known password.
-- Until then this row just reserves the admin account.
-- ---------------------------------------------------------------------
INSERT INTO `users` (`username`, `password_hash`, `full_name`, `role`, `is_active`)
VALUES ('admin', 'PLACEHOLDER_SET_ON_FIRST_RUN', 'System Administrator', 'Admin', 1)
ON DUPLICATE KEY UPDATE `full_name` = VALUES(`full_name`);

-- ---------------------------------------------------------------------
-- Fee schedule — Peñablanca Revenue Code.
-- AMOUNTS BELOW ARE PLACEHOLDERS. Replace each `amount` with the real
-- figure from the Peñablanca Revenue Code before go-live.
-- ---------------------------------------------------------------------
INSERT INTO `fees` (`code`, `description`, `amount`, `is_active`) VALUES
  ('CTC-BIRTH',    'Certified True Copy - Birth Certificate',     0.00, 1),
  ('CTC-MARRIAGE', 'Certified True Copy - Marriage Certificate',  0.00, 1),
  ('CTC-DEATH',    'Certified True Copy - Death Certificate',     0.00, 1),
  ('NEG-CERT',     'Negative Certification',                      0.00, 1),
  ('REG-BIRTH',    'Registration - Birth (Municipal Form 102)',   0.00, 1),
  ('REG-MARRIAGE', 'Registration - Marriage (Municipal Form 97)', 0.00, 1),
  ('REG-DEATH',    'Registration - Death (Municipal Form 103)',   0.00, 1),
  ('BURIAL',       'Burial Permit',                               0.00, 1),
  ('PET-9048',     'Petition - RA 9048 (Clerical Error / Change of First Name)', 0.00, 1),
  ('PET-10172',    'Petition - RA 10172 (Day/Month/Sex Correction)',            0.00, 1)
ON DUPLICATE KEY UPDATE `description` = VALUES(`description`);

-- Done. Next: run 03_migrate_from_ocr.sql to copy your existing records.
