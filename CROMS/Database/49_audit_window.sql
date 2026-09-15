-- 49_audit_window.sql
-- Stamps every audit_log row with the service window the acting user was
-- signed in to at the time (Session.WindowId), so Activity Monitoring can
-- answer "which window did this" for ANY action (registration save, payment,
-- petition, login, ...), not just payments -- and, joined through
-- table_name='payments' + record_id, tells you which window handled a given
-- payment. Idempotent (guarded ADD COLUMN via information_schema, since
-- MySQL has no ADD COLUMN IF NOT EXISTS).
--
-- Old rows stay NULL (window unknown) -- there is no way to reconstruct
-- which window wrote a historical row, so nothing is guessed.

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'audit_log' AND COLUMN_NAME = 'window_id'
);

SET @sql := IF(@col_exists = 0,
  'ALTER TABLE audit_log ADD COLUMN window_id INT NULL AFTER user_id, ADD KEY fk_audit_window (window_id)',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- FK added separately (guarded) so a re-run never tries to add it twice.
SET @fk_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'audit_log' AND CONSTRAINT_NAME = 'fk_audit_window'
);

SET @sql := IF(@fk_exists = 0,
  'ALTER TABLE audit_log ADD CONSTRAINT fk_audit_window FOREIGN KEY (window_id) REFERENCES windows (id) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Done.
