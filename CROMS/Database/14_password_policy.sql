-- =====================================================================
-- 14_password_policy.sql — force-change-on-first-login support
-- ---------------------------------------------------------------------
-- Adds users.must_change_password. New accounts (and the seeded admin)
-- default to 1, so the operator is forced to set their own password the
-- first time they sign in — this is what neutralises the default
-- admin/admin123 credential. Cleared to 0 once the user changes it.
-- ADD COLUMN sets every existing row to the default (1) automatically,
-- so all current accounts are forced to rotate on next login.
-- Idempotent-ish: re-running errors only with "Duplicate column"; safe
-- to ignore if already applied.
-- =====================================================================
ALTER TABLE `users`
  ADD COLUMN `must_change_password` TINYINT(1) NOT NULL DEFAULT 1 AFTER `is_active`;
