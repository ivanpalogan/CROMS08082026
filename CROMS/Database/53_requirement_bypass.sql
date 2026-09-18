-- 53_requirement_bypass.sql
-- Per-requirement bypass, replacing the single whole-checklist "Admin Override" button that
-- used to sit beside the grid (marriage licence issue screen, delayed-birth case screen).
-- That button bypassed EVERY row on the case in one click with no way to say which document
-- was actually missing versus which the office chose to accept without checking. This adds a
-- bypass to the ROW itself, in marriage_requirements - the one table already shared by the
-- marriage licence, Form 97 registration, delayed birth registration, and petition/case
-- documents (MarriageService.Requirements/SaveRequirement/SyncRequirements are already generic
-- on owner type), so every one of those screens gets the per-row control for free through
-- RequirementsGrid.
--
-- Deliberately kept SEPARATE from `status`: a bypassed row's status still says what is
-- actually on file (Missing/Submitted/...) - bypassing does not pretend the document was
-- checked, it records that an Admin chose to let the case proceed without it being checked.
-- That is a materially different fact for an audit than "Verified" would be.
--
-- Admin-only (MarriageService.BypassRequirement/ClearBypass), narrower than the Registrar-or-
-- Admin gate on the rest of this table - same reasoning as marriage_licenses'
-- requirements_override_by column this replaces in the UI (migration 48).

DROP PROCEDURE IF EXISTS _croms_req_bypass;
DELIMITER //
CREATE PROCEDURE _croms_req_bypass()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_requirements' AND COLUMN_NAME = 'bypassed_by') THEN
        ALTER TABLE `marriage_requirements`
            ADD COLUMN `bypassed_by`     INT NULL,
            ADD COLUMN `bypassed_at`     DATETIME NULL,
            ADD COLUMN `bypass_reason`   VARCHAR(255) NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_req_bypass();
DROP PROCEDURE IF EXISTS _croms_req_bypass;
