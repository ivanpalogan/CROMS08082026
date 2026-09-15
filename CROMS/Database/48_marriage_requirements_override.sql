-- 48_marriage_requirements_override.sql
-- Admin power to issue a marriage licence despite missing/unverified requirement attachments
-- (a client cannot supply every document, but the office still needs to issue). Scoped
-- narrowly: it bypasses only requirement rows (Code "REQ_*" in MarriageRules - missing or
-- unverified attachments), never posting completion, an unresolved impediment, missing
-- payment, or a hard legal stop (under-18). Those stay enforced regardless - see
-- MarriageRules.ApplyOverride.
--
-- Restricted to the Admin role only (MarriageService.OverrideRequirements), not Registrar -
-- deliberately narrower than the existing RequireRegistrar gate used everywhere else on this
-- table, because this specific power lets a licence issue with incomplete paperwork and needs
-- the highest authority in the office plus a written reason, always audited.

DROP PROCEDURE IF EXISTS _croms_marriage_req_override;
DELIMITER //
CREATE PROCEDURE _croms_marriage_req_override()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = 'requirements_override_by') THEN
        ALTER TABLE `marriage_licenses`
            ADD COLUMN `requirements_override_by` INT NULL,
            ADD COLUMN `requirements_override_at` DATETIME NULL,
            ADD COLUMN `requirements_override_reason` VARCHAR(255) NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_req_override();
DROP PROCEDURE IF EXISTS _croms_marriage_req_override;
