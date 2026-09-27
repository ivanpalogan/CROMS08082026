-- =====================================================================
-- 69_soft_delete_reason.sql
-- Records-integrity pass: a Delete must never actually remove a civil
-- registry record any more - it is marked deleted (soft delete), with a
-- mandatory reason and who/when, so a mistaken or malicious removal is
-- always recoverable and always attributable. Applies to the record
-- tables whose Delete used to run a real DELETE statement:
--   births, deaths, marriages, windows, certificate_templates,
--   certificate_template_images, marriage_requirements.
-- Petitions/Master Files lookups are deliberately NOT included here -
-- Petitions already audits every edit/delete and does not need a reason
-- prompt (per the office's own instruction); Master Files lookup rows
-- keep a real hard delete (they are catalogue entries, not client
-- records, and this project has already done real deletes on them after
-- checking for FK references - see 2026-09-13 relationships cleanup).
-- Idempotent - safe to re-run (MySQL has no ADD COLUMN IF NOT EXISTS,
-- so each column is guarded via information_schema).
-- =====================================================================

DROP PROCEDURE IF EXISTS `_croms_migrate_soft_delete`;
DELIMITER $$
CREATE PROCEDURE `_croms_migrate_soft_delete`()
BEGIN
  -- births
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `births` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `births` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `births` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `births` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- deaths
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `deaths` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `deaths` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `deaths` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `deaths` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- marriages
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `marriages` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `marriages` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `marriages` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `marriages` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- windows
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'windows' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `windows` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'windows' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `windows` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'windows' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `windows` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'windows' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `windows` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- certificate_templates
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_templates' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `certificate_templates` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_templates' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `certificate_templates` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_templates' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `certificate_templates` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_templates' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `certificate_templates` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- certificate_template_images
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_template_images' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `certificate_template_images` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_template_images' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `certificate_template_images` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_template_images' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `certificate_template_images` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'certificate_template_images' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `certificate_template_images` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;

  -- marriage_requirements
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirements' AND COLUMN_NAME = 'is_deleted') THEN
    ALTER TABLE `marriage_requirements` ADD COLUMN `is_deleted` TINYINT(1) NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirements' AND COLUMN_NAME = 'delete_reason') THEN
    ALTER TABLE `marriage_requirements` ADD COLUMN `delete_reason` VARCHAR(255) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirements' AND COLUMN_NAME = 'deleted_by') THEN
    ALTER TABLE `marriage_requirements` ADD COLUMN `deleted_by` INT NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirements' AND COLUMN_NAME = 'deleted_at') THEN
    ALTER TABLE `marriage_requirements` ADD COLUMN `deleted_at` DATETIME NULL;
  END IF;
END$$
DELIMITER ;

CALL `_croms_migrate_soft_delete`();
DROP PROCEDURE `_croms_migrate_soft_delete`;
