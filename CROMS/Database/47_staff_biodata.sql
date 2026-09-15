-- 47_staff_biodata.sql
-- Staff biodata + permanent-personnel identification (Users & Audit Trail).
-- One row per user account, written by the STAFF MEMBER THEMSELVES (Forms/StaffBiodataForm.cs),
-- not by an admin — the admin side only VIEWS the list (Users & Audit -> Staff Biodata tab) to
-- see who is Permanent vs Casual/Job Order/Contractual/Probationary.
-- Idempotent (CREATE TABLE IF NOT EXISTS); safe to re-run.

CREATE TABLE IF NOT EXISTS `staff_biodata` (
  `user_id`                 INT NOT NULL,
  `employee_no`              VARCHAR(30)  NULL,
  `employment_status`        ENUM('Permanent','Casual','Job Order','Contractual','Probationary')
                              NOT NULL DEFAULT 'Casual',
  `position`                 VARCHAR(100) NULL,
  `date_hired`                DATE NULL,
  `birthdate`                 DATE NULL,
  `sex`                       ENUM('Male','Female') NULL,
  `civil_status`              VARCHAR(30)  NULL,
  `address`                   VARCHAR(255) NULL,
  `contact_no`                VARCHAR(30)  NULL,
  `emergency_contact_name`    VARCHAR(120) NULL,
  `emergency_contact_no`      VARCHAR(30)  NULL,
  `updated_at`                DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `fk_staff_biodata_user` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
