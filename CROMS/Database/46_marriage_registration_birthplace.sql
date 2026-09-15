-- 46_marriage_registration_birthplace.sql
-- Place of birth, standardized on Marriage REGISTRATION (Form 97 / `marriages`) the same way
-- it already is on Birth Registration and the Marriage LICENCE (Form 90, migration 37): Country
-- / Province / City-Municipality, not a single field. Found missing 2026-09-14 while auditing
-- the office's own item 1 ("this should not be limited to one module") against the running app
-- - `marriages.husband_birth_place_id` / `wife_birth_place_id` had no country at all, and were
-- the only place-of-birth fields in the whole app stored as a bare FK id instead of the
-- Country/Province/Municipality picker trio every other screen uses.
--
-- A SECOND, PRE-EXISTING DEFECT FOUND WHILE FIXING THE FIRST: the FK constraints on those two
-- columns (`fk_marr_h_birthplace` / `fk_marr_w_birthplace`, in 01_schema.sql) reference
-- `hospitals(id)`, but MarriageEntryForm has always treated the value as a MUNICIPALITY id
-- (ReloadMunis binds it against `municipalities`, and the certificate view already joins it to
-- `municipalities` too - `LEFT JOIN municipalities hbp ON hbp.id = m.husband_birth_place_id`).
-- A place of birth is a municipality, not a hospital, so the view was right and the FK
-- constraint was simply wrong from the original 2026-07 build. Since this migration retires the
-- column from the write path entirely (below), the stale constraint is left in place rather
-- than touched - nothing writes to a column bound for deprecation, so the wrong reference can no
-- longer cause a failed save. Noted here so nobody re-derives the same confusion from the schema
-- file alone.
--
-- WHY TEXT, NOT AN ID. Every other place-of-birth field in CROMS (births.place_of_birth,
-- marriage_licenses.husband_place_of_birth) is a joined "Municipality, Province" TEXT value,
-- because that is what lets a foreign birth be recorded at all - there is no municipalities row
-- for Osaka. An id-based FK cannot hold that. So `husband_birth_place_id`/`wife_birth_place_id`
-- are DEPRECATED (kept, for the one legacy row that may hold a value, never written again) and
-- replaced by the same joined-text shape as the licence: `husband_place_of_birth` /
-- `wife_place_of_birth` (VARCHAR(150)) plus its own `husband_birth_country` /
-- `wife_birth_country` (VARCHAR(80), matching migration 37's `marriage_licenses` columns
-- exactly). GeoLookup.JoinPlace/ProvinceOf/MunicipalityOf (promoted out of MarriageLicenseForm
-- into GeoLookup itself in this same pass, so Form 90 and Form 97 share one join/split
-- implementation instead of a second private copy) do the join and the split.
--
-- Idempotent: guarded ADD COLUMN through a throwaway procedure (MySQL has no ADD COLUMN IF NOT
-- EXISTS). No existing row is backfilled - a country/province/municipality invented for a record
-- that never stated one is a fabricated fact in a government record, the same rule migration 37
-- already applied to births.

DROP PROCEDURE IF EXISTS _croms_marriage_reg_birthplace;
DELIMITER //
CREATE PROCEDURE _croms_marriage_reg_birthplace()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_place_of_birth') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `husband_place_of_birth` VARCHAR(150) NULL AFTER `husband_birth_place_id`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_birth_country') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `husband_birth_country` VARCHAR(80) NULL AFTER `husband_place_of_birth`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_place_of_birth') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `wife_place_of_birth` VARCHAR(150) NULL AFTER `wife_birth_place_id`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_birth_country') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `wife_birth_country` VARCHAR(80) NULL AFTER `wife_place_of_birth`;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_reg_birthplace();
DROP PROCEDURE IF EXISTS _croms_marriage_reg_birthplace;

-- Restated: the printed/reported place of birth now reads the new text column first (what a
-- record saved after this migration actually holds), falling back to the old FK-derived name
-- only for a row saved before it existed - so nothing already on file goes blank. Everything
-- else in the view is unchanged from 33_marriage_workflow.sql's restatement.
CREATE OR REPLACE VIEW `v_marriage_certificate` AS
SELECT
    m.id                        AS record_id,
    'marriages'                 AS record_table,
    COALESCE(m.form_code, 'MF-97-1993')            AS form_code,
    COALESCE(m.form_name, 'Certificate of Marriage') AS form_name,
    '97'                        AS municipal_form_no,
    m.registry_no, m.book_volume, m.book_page, m.status,

    m.husband_first_name, m.husband_middle_name, m.husband_last_name,
    TRIM(CONCAT_WS(' ', m.husband_first_name, m.husband_middle_name, m.husband_last_name))
                                AS husband_full_name,
    m.husband_age, m.husband_date_of_birth, m.husband_civil_status,
    COALESCE(m.husband_place_of_birth, hbp.name) AS husband_place_of_birth,
    m.husband_birth_country,
    hc.name  AS husband_citizenship,
    hr.name  AS husband_religion,
    hres.name AS husband_residence,
    m.husband_father_name, m.husband_mother_name,

    m.wife_first_name, m.wife_middle_name, m.wife_last_name,
    TRIM(CONCAT_WS(' ', m.wife_first_name, m.wife_middle_name, m.wife_last_name))
                                AS wife_full_name,
    m.wife_age, m.wife_date_of_birth, m.wife_civil_status,
    COALESCE(m.wife_place_of_birth, wbp.name) AS wife_place_of_birth,
    m.wife_birth_country,
    wc.name  AS wife_citizenship,
    wr.name  AS wife_religion,
    wres.name AS wife_residence,
    m.wife_father_name, m.wife_mother_name,

    ch.name  AS church_name,
    pm.name  AS place_municipality,
    pp.name  AS place_province,
    TRIM(CONCAT_WS(', ', ch.name, pm.name, pp.name)) AS place_of_marriage,
    m.date_of_marriage, m.time_of_marriage,
    m.solemnizer, m.solemnizer_position,
    m.witness1_name, m.witness2_name,
    COALESCE(ml.license_no, m.license_no)   AS license_no,
    COALESCE(ml.issue_date, m.license_date) AS license_date,
    m.license_place,
    m.license_basis, m.exemption_basis,
    m.received_by, m.received_by_title, m.received_by_date,
    m.registration_type, m.date_registered,
    m.remarks,
    m.created_at, m.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `marriages` m
CROSS JOIN `office_profile` o
LEFT JOIN `marriage_licenses` ml ON ml.id = m.license_id
LEFT JOIN `municipalities` hbp  ON hbp.id  = m.husband_birth_place_id
LEFT JOIN `municipalities` wbp  ON wbp.id  = m.wife_birth_place_id
LEFT JOIN `nationalities`  hc   ON hc.id   = m.husband_citizenship_id
LEFT JOIN `nationalities`  wc   ON wc.id   = m.wife_citizenship_id
LEFT JOIN `religions`      hr   ON hr.id   = m.husband_religion_id
LEFT JOIN `religions`      wr   ON wr.id   = m.wife_religion_id
LEFT JOIN `residences`     hres ON hres.id = m.husband_residence_id
LEFT JOIN `residences`     wres ON wres.id = m.wife_residence_id
LEFT JOIN `churches`       ch   ON ch.id   = m.church_id
LEFT JOIN `municipalities` pm   ON pm.id   = m.place_municipality_id
LEFT JOIN `provinces`      pp   ON pp.id   = m.place_province_id;
