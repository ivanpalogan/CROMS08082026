-- =====================================================================
-- CROMS — 06_death_form103.sql
-- Expands `deaths` for the PSA Municipal Form 103 death-registration screen
-- (deceased info + cause of death + disposal/permit). Run once.
-- =====================================================================
USE `croms`;

-- The prototype captures the deceased as one "Full Name" field, so relax the
-- old NOT NULL structured-name columns (they stay for the migrated rows).
ALTER TABLE `deaths`
  MODIFY COLUMN `first_name` VARCHAR(50) NULL,
  MODIFY COLUMN `middle_name` VARCHAR(50) NULL,
  MODIFY COLUMN `last_name` VARCHAR(50) NULL,
  MODIFY COLUMN `sex` ENUM('Male','Female') NULL;

ALTER TABLE `deaths`
  ADD COLUMN `registry_no`          VARCHAR(30)  NULL AFTER `id`,
  ADD COLUMN `status`               VARCHAR(30)  NOT NULL DEFAULT 'Draft' AFTER `registry_no`,
  ADD COLUMN `full_name`            VARCHAR(150) NULL AFTER `status`,
  ADD COLUMN `citizenship`          VARCHAR(50)  NULL,
  ADD COLUMN `time_of_death`        VARCHAR(20)  NULL,
  ADD COLUMN `place_of_death`       VARCHAR(255) NULL,
  ADD COLUMN `religion_name`        VARCHAR(80)  NULL,
  ADD COLUMN `immediate_cause`      VARCHAR(200) NULL,
  ADD COLUMN `antecedent_cause`     VARCHAR(200) NULL,
  ADD COLUMN `underlying_cause`     VARCHAR(200) NULL,
  ADD COLUMN `medical_certifier`    VARCHAR(120) NULL,
  ADD COLUMN `certifier_license_no` VARCHAR(50)  NULL,
  ADD COLUMN `disposal_method`      VARCHAR(30)  NULL,
  ADD COLUMN `place_of_disposal`    VARCHAR(200) NULL,
  ADD COLUMN `date_of_disposal`     DATE         NULL,
  ADD COLUMN `permit_type`          VARCHAR(30)  NULL;

-- Existing migrated deaths are historical -> Registered, and give them a
-- display full name from their structured columns.
UPDATE `deaths`
   SET `status` = 'Registered'
 WHERE `status` = 'Draft';
UPDATE `deaths`
   SET `full_name` = TRIM(CONCAT(COALESCE(`last_name`,''), ', ',
                                 COALESCE(`first_name`,''), ' ',
                                 COALESCE(`middle_name`,'')))
 WHERE `full_name` IS NULL;
