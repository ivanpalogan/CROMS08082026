-- 42_delayed_birth_registration.sql
-- Backlog section 10: birth registration cannot be one process. Registered within 30 days is
-- timely; past that is DELAYED REGISTRATION, with the office's own checklist (PSA MC 2024-17)
-- and a posting/evaluation workflow - not a longer checklist bolted onto the same screen.
--
-- REUSED, NOT REBUILT: the marriage licence already has a 10-day posting clock, a requirements
-- table keyed by owner_type/owner_id with conditional rule keys, and a registrar finding. This
-- migration extends the SAME two tables (marriage_requirement_types / marriage_requirements)
-- with applies_to='Birth' rows rather than building a second requirements engine.
--
-- THE ONE NEW SHAPE: item (c) on the office's card is "ANY TWO of the following eight evidences
-- of birth" - a group with a satisfy-count, which a flat per-row "blocking" flag cannot express.
-- group_code/group_min are added so a row can say "I am one of several that together need N
-- verified", while every existing row (group_code NULL, group_min 1) is completely unaffected -
-- it still means exactly what it always meant.
--
-- births gains only what the workflow itself produces: when the 10-day posting ran, and the
-- registrar's evaluation. NOT added: a redundant "is delayed" flag - births.is_delayed already
-- exists and is computed honestly from the dates (2026-09-08), and this workflow reads that flag
-- rather than duplicating it. NOT backfilled: every new column is NULL on existing rows, since a
-- posting/evaluation that never happened must not be invented for a record already on file.
--
-- ASCII only (piped into the mysql client). Idempotent - safe to run twice.

DROP PROCEDURE IF EXISTS _croms_42;
DELIMITER //
CREATE PROCEDURE _croms_42()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births' AND COLUMN_NAME = 'delayed_posting_start') THEN
        ALTER TABLE `births`
            ADD COLUMN `delayed_posting_start`      DATE     NULL AFTER `is_delayed`,
            ADD COLUMN `delayed_posting_end`         DATE     NULL AFTER `delayed_posting_start`,
            ADD COLUMN `delayed_registrant_deceased` TINYINT(1) NULL AFTER `delayed_posting_end`,
            ADD COLUMN `delayed_mother_unavailable`  TINYINT(1) NULL AFTER `delayed_registrant_deceased`,
            ADD COLUMN `delayed_parent_deceased`     TINYINT(1) NULL AFTER `delayed_mother_unavailable`,
            ADD COLUMN `delayed_evaluation`          VARCHAR(500) NULL AFTER `delayed_parent_deceased`,
            ADD COLUMN `delayed_evaluation_by`        INT      NULL AFTER `delayed_evaluation`,
            ADD COLUMN `delayed_evaluation_at`        DATETIME NULL AFTER `delayed_evaluation_by`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_requirement_types' AND COLUMN_NAME = 'group_code') THEN
        ALTER TABLE `marriage_requirement_types`
            ADD COLUMN `group_code` VARCHAR(30) NULL AFTER `blocking`,
            ADD COLUMN `group_min`  INT NOT NULL DEFAULT 1 AFTER `group_code`;
    END IF;
END //
DELIMITER ;
CALL _croms_42();
DROP PROCEDURE IF EXISTS _croms_42;

-- ---- the posting period, as a setting - not a constant, the same reasoning BREQS/marriage use.
-- CONFIRM WITH LCRO: the office's card names PSA MC 2024-17 (which specifies 10 days), but
-- whether THIS office actually runs the posting step for a delayed birth (vs just collecting the
-- checklist) is still an open question (backlog section 14).
INSERT IGNORE INTO `app_settings` (`setting_key`, `setting_value`, `description`) VALUES
  ('BIRTH_DELAYED_POSTING_DAYS', '10', 'Delayed birth registration posting period (PSA MC 2024-17). CONFIRM WITH LCRO whether this office runs the posting step.');

-- ---- the office's own checklist, headed "REQUIREMENTS FOR DELAYED REGISTRATION (PSA MC No.
-- 2024-17)". Items (f)/(g)/(h) are conditional - encoded as their own rows with a rule key,
-- the same shape the marriage licence already uses for ConsentAge/AdviceAge/PreviouslyMarried -
-- rather than one row with a text caveat, per the backlog's own instruction to follow that
-- precedent.
INSERT IGNORE INTO `marriage_requirement_types`
 (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order, group_code, group_min) VALUES
 ('NEG_CERT_PSA',              'Negative Certification of Birth from PSA',                              'Birth','Always',            0,1,'PSA MC 2024-17 item (a)',                       1, 10, NULL, 1),
 ('TWO_WITNESS_AFFIDAVIT',     'Affidavit of Two Disinterested Persons',                                'Birth','Always',            0,1,'PSA MC 2024-17 item (b)',                       1, 20, NULL, 1),
 ('EVID_BAPTISMAL',            'Baptismal Record',                                                      'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 30, 'BIRTH_EVIDENCE', 2),
 ('EVID_SCHOOL',               'School Record',                                                         'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 31, 'BIRTH_EVIDENCE', 2),
 ('EVID_VOTER',                'Voter''s Record',                                                       'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 32, 'BIRTH_EVIDENCE', 2),
 ('EVID_MEDICAL',              'Medical Records',                                                       'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 33, 'BIRTH_EVIDENCE', 2),
 ('EVID_MARRIAGE_CERT',        'Marriage Certificate',                                                  'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 34, 'BIRTH_EVIDENCE', 2),
 ('EVID_BARANGAY_CERT',        'Barangay Certification',                                                'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 35, 'BIRTH_EVIDENCE', 2),
 ('EVID_GOV_ID',                'Any Government-issued ID',                                              'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 36, 'BIRTH_EVIDENCE', 2),
 ('EVID_POLICE_NBI',           'Police/NBI Clearance',                                                  'Birth','Always',            0,0,'PSA MC 2024-17 item (c) - any two required',    1, 37, 'BIRTH_EVIDENCE', 2),
 ('RESIDENCY_CERT',            'Certificate of Residency',                                              'Birth','Always',            0,1,'PSA MC 2024-17 item (d)',                       1, 40, NULL, 1),
 ('NATIONAL_ID',               'National ID',                                                           'Birth','Always',            0,1,'PSA MC 2024-17 item (e)',                       1, 50, NULL, 1),
 ('PHOTO_2X2',                 'Latest 2x2 ID picture',                                                 'Birth','Always',            0,1,'PSA MC 2024-17 item (f)',                       1, 60, NULL, 1),
 ('DEATH_CERT_REGISTRANT',     'Death Certificate of the registrant',                                   'Birth','RegistrantDeceased', 0,1,'PSA MC 2024-17 item (f) - if deceased',         1, 61, NULL, 1),
 ('PARENTS_MARRIAGE_CERT',     'Marriage Certificate of Parents',                                       'Birth','ParentsMarried',     0,1,'PSA MC 2024-17 item (g)',                       1, 70, NULL, 1),
 ('PARENTS_AFFIDAVIT_RA9255',  'Affidavit of Parents (not married) - RA 9255',                          'Birth','ParentsUnmarried',   0,1,'PSA MC 2024-17 item (g) - if not married',      1, 71, NULL, 1),
 ('MOTHER_WHEREABOUTS_AFFIDAVIT','Affidavit / Sworn Statement on the mother''s whereabouts',            'Birth','MotherUnavailable',  0,1,'PSA MC 2024-17 item (g) - if mother unavailable',1, 72, NULL, 1),
 ('PARENT_ID_DOCS',            'Documentary evidence of parents'' identity (valid IDs / birth certificate)','Birth','Always',        0,1,'PSA MC 2024-17 item (h)',                       1, 80, NULL, 1),
 ('PARENT_DEATH_CERT',         'Death Certificate of the deceased parent',                              'Birth','ParentDeceased',    0,1,'PSA MC 2024-17 item (h) - if a parent deceased',1, 81, NULL, 1),
 ('REGISTRANT_AFFIDAVIT',      'Registrant''s Affidavit (that all documents submitted are authentic)', 'Birth','Always',            0,1,'PSA MC 2024-17 item (i)',                       1, 90, NULL, 1),
 ('PERSONAL_APPEARANCE',       'Personal appearance of the registrant (or parents if a minor)',        'Birth','Always',            0,1,'PSA MC 2024-17 item (j)',                       1, 100, NULL, 1);
