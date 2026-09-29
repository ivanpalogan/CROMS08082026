-- =====================================================================
-- 73_roles_admin_staff.sql
-- Two roles only: Admin (oversees everything) and Staff (the operational role).
-- Registrar / Cashier / Releasing were labels the code never treated differently in
-- daily work, so existing accounts of those roles become Staff. Idempotent, ASCII only.
-- NOTE: which staff member used to be a Registrar / Cashier / Releasing is not kept
-- after this runs (the enum no longer holds those values).
-- =====================================================================
UPDATE users SET role = 'Staff' WHERE role IN ('Registrar', 'Cashier', 'Releasing');
ALTER TABLE users MODIFY role ENUM('Admin', 'Staff') NOT NULL;
