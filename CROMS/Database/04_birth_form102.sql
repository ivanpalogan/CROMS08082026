-- =====================================================================
-- CROMS — 04_birth_form102.sql
-- Expands `births` to the full PSA Municipal Form 102 (Certificate of Live
-- Birth) so the Birth Registration form can capture every section:
-- Child, Mother, Father, Marriage of Parents, Attendant, Informant,
-- Certification. Place of birth becomes a single text field, weight is in
-- grams, and registry/book/status are added for the workflow.
--
-- Run AFTER the earlier scripts, once. Safe on the existing 8 rows (new
-- columns are nullable; dropped columns were empty/unused).
-- =====================================================================
USE `croms`;

SET FOREIGN_KEY_CHECKS = 0;

-- Drop the old 3-dropdown place design (replaced by a single text field).
ALTER TABLE `births` DROP FOREIGN KEY `fk_births_hospital`;
ALTER TABLE `births` DROP FOREIGN KEY `fk_births_municipality`;
ALTER TABLE `births` DROP FOREIGN KEY `fk_births_province`;
ALTER TABLE `births`
  DROP COLUMN `hospital_id`,
  DROP COLUMN `municipality_id`,
  DROP COLUMN `province_id`,
  DROP COLUMN `if_multiple_birth`,
  DROP COLUMN `weight_at_birth`;

-- Child + workflow additions.
ALTER TABLE `births`
  ADD COLUMN `is_delayed`     TINYINT(1)   NOT NULL DEFAULT 0 AFTER `id`,
  ADD COLUMN `registry_no`    VARCHAR(30)  NULL AFTER `is_delayed`,
  ADD COLUMN `book_volume`    VARCHAR(30)  NULL AFTER `registry_no`,
  ADD COLUMN `status`         VARCHAR(30)  NOT NULL DEFAULT 'Draft' AFTER `book_volume`,
  ADD COLUMN `time_of_birth`  VARCHAR(20)  NULL AFTER `date_of_birth`,
  ADD COLUMN `place_of_birth` VARCHAR(255) NULL AFTER `time_of_birth`,
  ADD COLUMN `weight_grams`   INT          NULL AFTER `birth_order`;

-- Mother section.
ALTER TABLE `births`
  ADD COLUMN `mother_citizenship`          VARCHAR(50)  NULL,
  ADD COLUMN `mother_religion`             VARCHAR(50)  NULL,
  ADD COLUMN `mother_occupation`           VARCHAR(80)  NULL,
  ADD COLUMN `mother_age`                  INT          NULL,
  ADD COLUMN `mother_children_born_alive`  INT          NULL,
  ADD COLUMN `mother_children_living`      INT          NULL,
  ADD COLUMN `mother_children_dead`        INT          NULL,
  ADD COLUMN `mother_residence`            VARCHAR(200) NULL;

-- Father section.
ALTER TABLE `births`
  ADD COLUMN `father_citizenship` VARCHAR(50)  NULL,
  ADD COLUMN `father_religion`    VARCHAR(50)  NULL,
  ADD COLUMN `father_occupation`  VARCHAR(80)  NULL,
  ADD COLUMN `father_age`         INT          NULL,
  ADD COLUMN `father_residence`   VARCHAR(200) NULL;

-- Marriage of parents.
ALTER TABLE `births`
  ADD COLUMN `parents_marriage_date`  DATE         NULL,
  ADD COLUMN `parents_marriage_place` VARCHAR(200) NULL;

-- Attendant.
ALTER TABLE `births`
  ADD COLUMN `attendant_type`    VARCHAR(40)  NULL,
  ADD COLUMN `attendant_name`    VARCHAR(120) NULL,
  ADD COLUMN `attendant_title`   VARCHAR(80)  NULL,
  ADD COLUMN `attendant_address` VARCHAR(200) NULL;

-- Informant.
ALTER TABLE `births`
  ADD COLUMN `informant_name`         VARCHAR(120) NULL,
  ADD COLUMN `informant_relationship` VARCHAR(60)  NULL,
  ADD COLUMN `informant_address`      VARCHAR(200) NULL,
  ADD COLUMN `informant_date`         DATE         NULL;

-- Certification (received at the civil registry).
ALTER TABLE `births`
  ADD COLUMN `prepared_by` VARCHAR(120) NULL,
  ADD COLUMN `received_by` VARCHAR(120) NULL,
  ADD COLUMN `remarks`     VARCHAR(255) NULL;

SET FOREIGN_KEY_CHECKS = 1;

-- Existing migrated rows: mark them Registered (they are historical records).
UPDATE `births` SET `status` = 'Registered' WHERE `status` = 'Draft';
