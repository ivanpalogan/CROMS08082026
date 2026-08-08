-- =====================================================================
-- CROMS — Civil Registry Operations Management System
-- 01_schema.sql : creates the clean `croms` database and all tables.
--
-- SAFE: this script creates a NEW database named `croms`. It does NOT
-- touch your existing `ocr_record_sytem` database. Run this first, then
-- 02_seed.sql, then (optionally) 03_migrate_from_ocr.sql.
--
-- Conventions (fixing the old DB's problems):
--   * snake_case table + column names (no spaces, no typos)
--   * every table has an INT AUTO_INCREMENT `id` primary key
--   * foreign keys named <entity>_id
--   * real DATE / DATETIME / DECIMAL types (not varchar dates)
--   * passwords stored as a hash, never plaintext
--   * created_at / updated_at audit stamps on operational tables
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `croms`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_0900_ai_ci;
USE `croms`;

SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================================
-- LOOKUP TABLES  (cleaned versions of your existing reference tables)
-- =====================================================================

-- was ` baranggay` (leading space removed)
CREATE TABLE IF NOT EXISTS `barangays` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_barangays_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `municipalities` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_municipalities_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `provinces` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_provinces_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- was `hostpital` (typo fixed)
CREATE TABLE IF NOT EXISTS `hospitals` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(150) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_hospitals_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- was `simbahan` (church)
CREATE TABLE IF NOT EXISTS `churches` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(150) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- NOTE: "place of birth/death/marriage" is captured as 3 dropdowns on each
-- record (venue + municipality + province), so the old single-name
-- places_of_* lookup tables are intentionally NOT created. Venue comes from
-- `hospitals` (birth/death) or `churches` (marriage); municipality + province
-- come from the `municipalities` / `provinces` lookups.

CREATE TABLE IF NOT EXISTS `causes_of_death` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(150) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `religions` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `nationalities` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `occupations` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `civil_statuses` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `birth_orders` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `type_of_births` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `relationships` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `residences` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(150) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- USERS & ROLES  (replaces conflicting tblaccount + tblusers)
-- Roles match the CROMS spec, not the old 'Student' enum.
-- =====================================================================
CREATE TABLE IF NOT EXISTS `users` (
  `id`            INT NOT NULL AUTO_INCREMENT,
  `username`      VARCHAR(50)  NOT NULL,
  `password_hash` VARCHAR(255) NOT NULL,               -- BCrypt hash, never plaintext
  `full_name`     VARCHAR(120) NOT NULL,
  `role`          ENUM('Admin','Registrar','Staff','Cashier','Releasing') NOT NULL,
  `is_active`     TINYINT(1) NOT NULL DEFAULT 1,
  `must_change_password` TINYINT(1) NOT NULL DEFAULT 1,   -- force reset on first login (see 14_password_policy.sql)
  `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_users_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- CORE RECORDS  (cleaned versions of tblbirth / tbldeath / tblmarriage)
-- Dates are real DATE; images kept as LONGBLOB for the OCR module.
-- =====================================================================
CREATE TABLE IF NOT EXISTS `births` (
  `id`                INT NOT NULL AUTO_INCREMENT,
  `first_name`        VARCHAR(50)  NOT NULL,
  `middle_name`       VARCHAR(50)  NULL,
  `last_name`         VARCHAR(50)  NOT NULL,
  `sex`               ENUM('Male','Female') NOT NULL,
  `date_of_birth`     DATE NULL,
  -- place of birth = 3 dropdowns
  `hospital_id`       INT NULL,
  `municipality_id`   INT NULL,
  `province_id`       INT NULL,
  `type_of_birth`     VARCHAR(30) NULL,
  `birth_order`       VARCHAR(30) NULL,
  `if_multiple_birth` VARCHAR(30) NULL,
  `weight_at_birth`   DECIMAL(5,2) NULL,                -- kilograms
  `mother_first_name` VARCHAR(50) NULL,
  `mother_middle_name` VARCHAR(50) NULL,
  `mother_last_name`  VARCHAR(50) NULL,
  `father_first_name` VARCHAR(50) NULL,
  `father_middle_name` VARCHAR(50) NULL,
  `father_last_name`  VARCHAR(50) NULL,
  `birth_image`       LONGBLOB NULL,                    -- scanned page (OCR module)
  `created_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_births_hospital`     (`hospital_id`),
  KEY `fk_births_municipality` (`municipality_id`),
  KEY `fk_births_province`     (`province_id`),
  CONSTRAINT `fk_births_hospital`     FOREIGN KEY (`hospital_id`)     REFERENCES `hospitals` (`id`),
  CONSTRAINT `fk_births_municipality` FOREIGN KEY (`municipality_id`) REFERENCES `municipalities` (`id`),
  CONSTRAINT `fk_births_province`     FOREIGN KEY (`province_id`)     REFERENCES `provinces` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `deaths` (
  `id`                INT NOT NULL AUTO_INCREMENT,
  `first_name`        VARCHAR(50) NOT NULL,
  `middle_name`       VARCHAR(50) NULL,
  `last_name`         VARCHAR(50) NOT NULL,
  `sex`               ENUM('Male','Female') NOT NULL,
  `date_of_death`     DATE NULL,
  `date_of_birth`     DATE NULL,
  `age`               INT NULL,
  -- place of death = 3 dropdowns
  `hospital_id`       INT NULL,
  `municipality_id`   INT NULL,
  `province_id`       INT NULL,
  `civil_status`      VARCHAR(30) NULL,
  `religion_id`       INT NULL,
  `residence_id`      INT NULL,
  `occupation_id`     INT NULL,
  `father_name`       VARCHAR(80) NULL,
  `mother_name`       VARCHAR(80) NULL,
  `cause_of_death_id` INT NULL,
  `death_image`       LONGBLOB NULL,
  `created_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_deaths_hospital`     (`hospital_id`),
  KEY `fk_deaths_municipality` (`municipality_id`),
  KEY `fk_deaths_province`     (`province_id`),
  KEY `fk_deaths_cause`      (`cause_of_death_id`),
  KEY `fk_deaths_religion`   (`religion_id`),
  KEY `fk_deaths_residence`  (`residence_id`),
  KEY `fk_deaths_occupation` (`occupation_id`),
  CONSTRAINT `fk_deaths_hospital`     FOREIGN KEY (`hospital_id`)     REFERENCES `hospitals` (`id`),
  CONSTRAINT `fk_deaths_municipality` FOREIGN KEY (`municipality_id`) REFERENCES `municipalities` (`id`),
  CONSTRAINT `fk_deaths_province`     FOREIGN KEY (`province_id`)     REFERENCES `provinces` (`id`),
  CONSTRAINT `fk_deaths_cause`      FOREIGN KEY (`cause_of_death_id`) REFERENCES `causes_of_death` (`id`),
  CONSTRAINT `fk_deaths_religion`   FOREIGN KEY (`religion_id`)       REFERENCES `religions` (`id`),
  CONSTRAINT `fk_deaths_residence`  FOREIGN KEY (`residence_id`)      REFERENCES `residences` (`id`),
  CONSTRAINT `fk_deaths_occupation` FOREIGN KEY (`occupation_id`)     REFERENCES `occupations` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `marriages` (
  `id`                  INT NOT NULL AUTO_INCREMENT,
  `husband_first_name`  VARCHAR(50) NOT NULL,
  `husband_middle_name` VARCHAR(50) NULL,
  `husband_last_name`   VARCHAR(50) NOT NULL,
  `wife_first_name`     VARCHAR(50) NOT NULL,
  `wife_middle_name`    VARCHAR(50) NULL,
  `wife_last_name`      VARCHAR(50) NOT NULL,
  `husband_age`         INT NULL,
  `husband_date_of_birth` DATE NULL,
  `wife_age`            INT NULL,
  `wife_date_of_birth`  DATE NULL,
  `husband_birth_place_id` INT NULL,
  `wife_birth_place_id` INT NULL,
  `husband_citizenship_id` INT NULL,
  `wife_citizenship_id` INT NULL,
  `husband_religion_id` INT NULL,
  `wife_religion_id`    INT NULL,
  `husband_civil_status` VARCHAR(30) NULL,
  `wife_civil_status`   VARCHAR(30) NULL,
  `husband_residence_id` INT NULL,
  `wife_residence_id`   INT NULL,
  -- place of marriage = 3 dropdowns (church + municipality + province)
  `church_id`             INT NULL,
  `place_municipality_id` INT NULL,
  `place_province_id`     INT NULL,
  `date_of_marriage`    DATE NULL,
  `time_of_marriage`    VARCHAR(30) NULL,
  `marriage_image`      LONGBLOB NULL,
  `created_at`          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_marr_h_birthplace` (`husband_birth_place_id`),
  KEY `fk_marr_w_birthplace` (`wife_birth_place_id`),
  KEY `fk_marr_h_citizen`    (`husband_citizenship_id`),
  KEY `fk_marr_w_citizen`    (`wife_citizenship_id`),
  KEY `fk_marr_h_religion`   (`husband_religion_id`),
  KEY `fk_marr_w_religion`   (`wife_religion_id`),
  KEY `fk_marr_h_residence`  (`husband_residence_id`),
  KEY `fk_marr_w_residence`  (`wife_residence_id`),
  KEY `fk_marr_church`       (`church_id`),
  KEY `fk_marr_place_muni`   (`place_municipality_id`),
  KEY `fk_marr_place_prov`   (`place_province_id`),
  CONSTRAINT `fk_marr_h_birthplace` FOREIGN KEY (`husband_birth_place_id`) REFERENCES `hospitals` (`id`),
  CONSTRAINT `fk_marr_w_birthplace` FOREIGN KEY (`wife_birth_place_id`)    REFERENCES `hospitals` (`id`),
  CONSTRAINT `fk_marr_h_citizen`    FOREIGN KEY (`husband_citizenship_id`) REFERENCES `nationalities` (`id`),
  CONSTRAINT `fk_marr_w_citizen`    FOREIGN KEY (`wife_citizenship_id`)    REFERENCES `nationalities` (`id`),
  CONSTRAINT `fk_marr_h_religion`   FOREIGN KEY (`husband_religion_id`)    REFERENCES `religions` (`id`),
  CONSTRAINT `fk_marr_w_religion`   FOREIGN KEY (`wife_religion_id`)       REFERENCES `religions` (`id`),
  CONSTRAINT `fk_marr_h_residence`  FOREIGN KEY (`husband_residence_id`)   REFERENCES `residences` (`id`),
  CONSTRAINT `fk_marr_w_residence`  FOREIGN KEY (`wife_residence_id`)      REFERENCES `residences` (`id`),
  CONSTRAINT `fk_marr_church`     FOREIGN KEY (`church_id`)             REFERENCES `churches` (`id`),
  CONSTRAINT `fk_marr_place_muni` FOREIGN KEY (`place_municipality_id`) REFERENCES `municipalities` (`id`),
  CONSTRAINT `fk_marr_place_prov` FOREIGN KEY (`place_province_id`)     REFERENCES `provinces` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- CLIENT-SERVICES WORKFLOW  (new — from the CROMS spec)
-- The transaction is the spine: one txn_code follows a client from
-- queue -> processing -> payment -> release.
-- =====================================================================
CREATE TABLE IF NOT EXISTS `transactions` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `txn_code`    VARCHAR(30) NOT NULL,                   -- e.g. TXN-2026-000123
  `client_name` VARCHAR(120) NOT NULL,
  `type`        ENUM('Birth','Marriage','Death','Certification','Petition','Other') NOT NULL,
  `status`      ENUM('Queued','Processing','ForPayment','ForRelease','Released','Cancelled') NOT NULL DEFAULT 'Queued',
  `created_by`  INT NULL,
  `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_transactions_code` (`txn_code`),
  KEY `fk_transactions_user` (`created_by`),
  CONSTRAINT `fk_transactions_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- was `queuedocument` (fixed FK typo MerriageID -> marriage_id)
CREATE TABLE IF NOT EXISTS `queue_tickets` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `ticket_code`    VARCHAR(30) NULL,                    -- e.g. BIRTH-045
  `full_name`      VARCHAR(120) NULL,
  `number_queue`   INT NULL,
  `date`           DATE NULL,
  `time`           VARCHAR(30) NULL,
  `status`         VARCHAR(30) NULL,
  `name_reviewer`  VARCHAR(80) NULL,
  `document_type`  VARCHAR(50) NULL,
  `transaction_id` INT NULL,
  `birth_id`       INT NULL,
  `death_id`       INT NULL,
  `marriage_id`    INT NULL,
  `created_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_queue_txn`      (`transaction_id`),
  KEY `fk_queue_birth`    (`birth_id`),
  KEY `fk_queue_death`    (`death_id`),
  KEY `fk_queue_marriage` (`marriage_id`),
  CONSTRAINT `fk_queue_txn`      FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`),
  CONSTRAINT `fk_queue_birth`    FOREIGN KEY (`birth_id`)       REFERENCES `births` (`id`),
  CONSTRAINT `fk_queue_death`    FOREIGN KEY (`death_id`)       REFERENCES `deaths` (`id`),
  CONSTRAINT `fk_queue_marriage` FOREIGN KEY (`marriage_id`)    REFERENCES `marriages` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `certificate_requests` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `transaction_id` INT NULL,
  `record_type`    ENUM('Birth','Marriage','Death') NULL,
  `record_id`      INT NULL,                            -- points at births/deaths/marriages
  `cert_type`      ENUM('CTC','Negative') NOT NULL DEFAULT 'CTC',
  `copies`         INT NOT NULL DEFAULT 1,
  `purpose`        VARCHAR(150) NULL,
  `status`         ENUM('Requested','Processing','Ready','Released') NOT NULL DEFAULT 'Requested',
  `created_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_certreq_txn` (`transaction_id`),
  CONSTRAINT `fk_certreq_txn` FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `releases` (
  `id`                    INT NOT NULL AUTO_INCREMENT,
  `transaction_id`        INT NOT NULL,
  `claimant_name`         VARCHAR(120) NOT NULL,
  `is_representative`     TINYINT(1) NOT NULL DEFAULT 0,
  `representative_id_type`   VARCHAR(50) NULL,
  `representative_id_number` VARCHAR(50) NULL,
  `signature_image`       LONGBLOB NULL,          -- legacy (mouse signature); unused since 2026-07-23
  `claimant_photo`        LONGBLOB NULL,          -- webcam photo of the claimant at release
  `released_by`           INT NULL,
  `released_at`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_release_txn`  (`transaction_id`),
  KEY `fk_release_user` (`released_by`),
  CONSTRAINT `fk_release_txn`  FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`),
  CONSTRAINT `fk_release_user` FOREIGN KEY (`released_by`)    REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- RECORD MANAGEMENT  (new — Petitions + Registry Books)
-- =====================================================================
CREATE TABLE IF NOT EXISTS `petitions` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `transaction_id` INT NULL,
  `petition_type`  ENUM('RA9048','RA10172') NOT NULL,
  `record_type`    ENUM('Birth','Marriage','Death') NULL,
  `record_id`      INT NULL,
  `stage`          ENUM('Filed','Posted','Decision','PSA_Endorsement') NOT NULL DEFAULT 'Filed',
  `filed_date`     DATE NULL,
  `remarks`        VARCHAR(255) NULL,
  `created_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_petition_txn` (`transaction_id`),
  CONSTRAINT `fk_petition_txn` FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `registry_books` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `book_type`   ENUM('Birth','Marriage','Death') NOT NULL,
  `year`        INT NOT NULL,
  `volume`      VARCHAR(20) NULL,
  `page`        VARCHAR(20) NULL,
  `record_type` ENUM('Birth','Marriage','Death') NULL,
  `record_id`   INT NULL,
  `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_registry_lookup` (`book_type`,`year`,`volume`,`page`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- OPERATIONS  (new — Fees + Payments)
-- =====================================================================
CREATE TABLE IF NOT EXISTS `fees` (
  `id`          INT NOT NULL AUTO_INCREMENT,
  `code`        VARCHAR(30) NOT NULL,
  `description` VARCHAR(150) NOT NULL,
  `amount`      DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `is_active`   TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_fees_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `payments` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `transaction_id` INT NOT NULL,
  `or_number`      VARCHAR(50) NULL,                    -- Official Receipt no.
  `payment_method` VARCHAR(20) NOT NULL DEFAULT 'Cash', -- Cash / GCash / Bank Transfer
  `reference_no`   VARCHAR(50) NULL,                    -- digital-payment reference
  `gross_amount`   DECIMAL(10,2) NOT NULL DEFAULT 0.00, -- document fee
  `additional_fee` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `net_amount`     DECIMAL(10,2) NOT NULL DEFAULT 0.00, -- total (gross + additional)
  `amount_tendered` DECIMAL(10,2) NULL,
  `change_amount`  DECIMAL(10,2) NULL,
  `remarks`        VARCHAR(255) NULL,
  `cashier_id`     INT NULL,
  `paid_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_payment_txn`     (`transaction_id`),
  KEY `fk_payment_cashier` (`cashier_id`),
  CONSTRAINT `fk_payment_txn`     FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`),
  CONSTRAINT `fk_payment_cashier` FOREIGN KEY (`cashier_id`)     REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- DOCUMENT WORKFLOW  (new — Incoming/Outgoing routing)
-- =====================================================================
CREATE TABLE IF NOT EXISTS `document_routing` (
  `id`             INT NOT NULL AUTO_INCREMENT,
  `direction`      ENUM('Incoming','Outgoing') NOT NULL,
  `title`          VARCHAR(150) NOT NULL,
  `from_office`    VARCHAR(120) NULL,
  `to_office`      VARCHAR(120) NULL,
  `received_by`    VARCHAR(120) NULL,
  `transaction_id` INT NULL,
  `routed_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_routing_txn` (`transaction_id`),
  CONSTRAINT `fk_routing_txn` FOREIGN KEY (`transaction_id`) REFERENCES `transactions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- =====================================================================
-- ADMINISTRATION  (new — tamper-evident audit log)
-- Every create/update/delete in the app writes one row here.
-- =====================================================================
CREATE TABLE IF NOT EXISTS `audit_log` (
  `id`         BIGINT NOT NULL AUTO_INCREMENT,
  `user_id`    INT NULL,
  `action`     ENUM('Create','Update','Delete','Login','Logout') NOT NULL,
  `table_name` VARCHAR(60) NULL,
  `record_id`  VARCHAR(60) NULL,
  `details`    TEXT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_audit_user` (`user_id`),
  CONSTRAINT `fk_audit_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Done. Next: run 02_seed.sql (lookups + admin user + fee schedule).
