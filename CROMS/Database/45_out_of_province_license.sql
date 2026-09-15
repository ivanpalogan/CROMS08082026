-- 45_out_of_province_license.sql
-- Backlog Phase 3, item 8 (the one remaining marriage §14 item with a trigger but no build
-- until now): "an applicant from another province needs an extra supporting document /
-- attachment." The trigger itself was confirmed by the office 2026-09-13, backlog §4.1: it is
-- NOT about the applicant's residence or birthplace - it is when the marriage LICENCE was
-- obtained in ANOTHER province but the wedding is solemnized HERE (Penablanca).
--
-- WHICH DOCUMENT: answered by the office 2026-09-13 (backlog Sec.4.1, follow-up). No specific
-- proof document is required - it is lawful to apply for the licence in one province and marry
-- in another; the licence itself (number/date, already typed into license_no/license_date) is
-- what the record carries. The only thing that actually matters is the licence's own 120-day
-- validity window, which MarriageRules already checks (Expired/Expiring) regardless of which
-- office issued it. So this requirement row is OPTIONAL (blocking=0, non-blocking) - it flags
-- the fact on the checklist for the registrar's awareness and gives an attachment SLOT if the
-- applicant happens to bring a copy, but nothing about it holds up registration.
--
-- WHY A NEW COLUMN AND NOT A THIRD license_basis VALUE. `marriages.license_basis` already
-- means Licensed vs Exempt (Family Code licensing vs Art. 27-34 exemption) - a marriage
-- licensed elsewhere is still LICENSED, just not by this office. Overloading a third value
-- onto that column would conflate "was there a licence" with "which office issued it," two
-- different questions. `license_out_of_province` is a separate boolean fact, independent of
-- Basis, exactly the way `PreviouslyMarried` already sits alongside Basis rather than folded
-- into it. `license_no` / `license_date` / `license_place` (added 2026-09-07, migration 30)
-- already hold exactly what an out-of-town licence states when there is no local
-- `marriage_licenses` row to link - so no new fields are needed for the licence's own data,
-- only the flag that says "this one came from elsewhere" and the requirement it triggers.

DROP PROCEDURE IF EXISTS _croms_oop_license;
DELIMITER //
CREATE PROCEDURE _croms_oop_license()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'license_out_of_province') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `license_out_of_province` TINYINT(1) NOT NULL DEFAULT 0;
    END IF;
END //
DELIMITER ;
CALL _croms_oop_license();
DROP PROCEDURE IF EXISTS _croms_oop_license;

-- The conditional requirement, same shape as PreviouslyMarried/ConsentAge/AdviceAge on this
-- same table (33_marriage_workflow.sql) and RegistrantDeceased/ParentsMarried on the delayed-
-- birth checklist (42_delayed_birth_registration.sql): a catalog row with a rule_key the
-- engine (MarriageRules.Needs) evaluates, not a hardcoded document name.
INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'OUT_OF_PROVINCE_LICENSE',
       'Licence obtained in another province (informational - no proof document required; attach a copy only if the applicant has one)',
       'Marriage', 'OutOfProvinceLicense', 0, 0,
       'Office confirmed 2026-09-13: it is lawful to license in one province and marry in another; nothing beyond the licence number/date is required. Non-blocking.',
       1, 50
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'OUT_OF_PROVINCE_LICENSE' AND applies_to = 'Marriage'
);
