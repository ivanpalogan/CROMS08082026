-- 57_marriage_death_missing_fields.sql
-- The printed Certificate of Marriage (MF-97) and Certificate of Death (MF-103) carry entries the
-- registry had no column for, so they printed blank however carefully staff worked (found
-- 2026-09-20 while re-checking both print maps against the office's blank sheets).
--
-- MARRIAGE: the sex of each party, each parent's citizenship (four rows the sheet prints), the
-- "persons who gave consent or advice" block per party (name, relationship, residence - ONE person per
-- party, as on the sheet and on Form 90), and whether the couple entered a marriage settlement.
-- DEATH: the medical-certificate items on the front of MF-103 - interval between onset and death for
-- each cause, other significant conditions, maternal condition (19c), external causes (19d), autopsy
-- (20), attendant (21a) and the attendance duration (21b), whether the certifier attended (22) with
-- title and address, the burial/cremation permit (24a) and transfer permit (24b) numbers and dates,
-- and the health officer who reviewed it.
-- NOT ADDED: items 14-19a (the infant-death block for ages 0-7 days) - they are on the BACK of the
-- form and no scan of the back is on file; adding columns for a page nobody has seen would be a guess.
--
-- Nothing is backfilled: a sex, citizenship or permit number written into a registry entry that never
-- stated one is a fabricated fact. Every column is NULL-able and NULL means "not stated".
-- ASCII only (applied by piping through the mysql client). Idempotent. Requires migration 46 (the
-- marriage view below restates 46's definition and reads its place-of-birth columns).

DROP PROCEDURE IF EXISTS _croms_marriage_death_fields;
DELIMITER //
CREATE PROCEDURE _croms_marriage_death_fields()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_sex') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_sex` ENUM('Male','Female') NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_sex') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_sex` ENUM('Male','Female') NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_father_citizenship') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_father_citizenship` VARCHAR(50) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_mother_citizenship') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_mother_citizenship` VARCHAR(50) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_father_citizenship') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_father_citizenship` VARCHAR(50) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_mother_citizenship') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_mother_citizenship` VARCHAR(50) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_consent_name') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_consent_name` VARCHAR(150) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_consent_relationship') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_consent_relationship` VARCHAR(60) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_consent_residence') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_consent_residence` VARCHAR(200) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_consent_name') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_consent_name` VARCHAR(150) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_consent_relationship') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_consent_relationship` VARCHAR(60) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_consent_residence') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_consent_residence` VARCHAR(200) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'marriage_settlement') THEN
        ALTER TABLE `marriages` ADD COLUMN `marriage_settlement` VARCHAR(20) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'interval_immediate') THEN
        ALTER TABLE `deaths` ADD COLUMN `interval_immediate` VARCHAR(40) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'interval_antecedent') THEN
        ALTER TABLE `deaths` ADD COLUMN `interval_antecedent` VARCHAR(40) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'interval_underlying') THEN
        ALTER TABLE `deaths` ADD COLUMN `interval_underlying` VARCHAR(40) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'other_conditions') THEN
        ALTER TABLE `deaths` ADD COLUMN `other_conditions` VARCHAR(255) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'maternal_condition') THEN
        ALTER TABLE `deaths` ADD COLUMN `maternal_condition` VARCHAR(60) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'external_manner') THEN
        ALTER TABLE `deaths` ADD COLUMN `external_manner` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'external_place') THEN
        ALTER TABLE `deaths` ADD COLUMN `external_place` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'autopsy') THEN
        ALTER TABLE `deaths` ADD COLUMN `autopsy` ENUM('Yes','No') NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'attendant_type') THEN
        ALTER TABLE `deaths` ADD COLUMN `attendant_type` VARCHAR(80) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'attendance_from') THEN
        ALTER TABLE `deaths` ADD COLUMN `attendance_from` DATE NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'attendance_to') THEN
        ALTER TABLE `deaths` ADD COLUMN `attendance_to` DATE NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'certifier_attended') THEN
        ALTER TABLE `deaths` ADD COLUMN `certifier_attended` TINYINT(1) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'certifier_title') THEN
        ALTER TABLE `deaths` ADD COLUMN `certifier_title` VARCHAR(80) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'certifier_address') THEN
        ALTER TABLE `deaths` ADD COLUMN `certifier_address` VARCHAR(200) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'burial_permit_no') THEN
        ALTER TABLE `deaths` ADD COLUMN `burial_permit_no` VARCHAR(40) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'burial_permit_date') THEN
        ALTER TABLE `deaths` ADD COLUMN `burial_permit_date` DATE NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'transfer_permit_no') THEN
        ALTER TABLE `deaths` ADD COLUMN `transfer_permit_no` VARCHAR(40) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'transfer_permit_date') THEN
        ALTER TABLE `deaths` ADD COLUMN `transfer_permit_date` DATE NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'reviewed_by') THEN
        ALTER TABLE `deaths` ADD COLUMN `reviewed_by` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'reviewed_by_date') THEN
        ALTER TABLE `deaths` ADD COLUMN `reviewed_by_date` DATE NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_death_fields();
DROP PROCEDURE IF EXISTS _croms_marriage_death_fields;

-- Views: 46's marriage definition and 30's death definition, plus the new columns.
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
    m.husband_age, m.husband_date_of_birth, m.husband_civil_status, m.husband_sex,
    COALESCE(m.husband_place_of_birth, hbp.name) AS husband_place_of_birth,
    m.husband_birth_country,
    hc.name  AS husband_citizenship,
    hr.name  AS husband_religion,
    hres.name AS husband_residence,
    m.husband_father_name, m.husband_father_citizenship,
    m.husband_mother_name, m.husband_mother_citizenship,
    m.husband_consent_name, m.husband_consent_relationship, m.husband_consent_residence,

    m.wife_first_name, m.wife_middle_name, m.wife_last_name,
    TRIM(CONCAT_WS(' ', m.wife_first_name, m.wife_middle_name, m.wife_last_name))
                                AS wife_full_name,
    m.wife_age, m.wife_date_of_birth, m.wife_civil_status, m.wife_sex,
    COALESCE(m.wife_place_of_birth, wbp.name) AS wife_place_of_birth,
    m.wife_birth_country,
    wc.name  AS wife_citizenship,
    wr.name  AS wife_religion,
    wres.name AS wife_residence,
    m.wife_father_name, m.wife_father_citizenship,
    m.wife_mother_name, m.wife_mother_citizenship,
    m.wife_consent_name, m.wife_consent_relationship, m.wife_consent_residence,

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
    m.license_basis, m.exemption_basis, m.marriage_settlement,
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

CREATE OR REPLACE VIEW `v_death_certificate` AS
SELECT
    d.id                        AS record_id,
    'deaths'                    AS record_table,
    COALESCE(d.form_code, 'MF-103-2016')          AS form_code,
    COALESCE(d.form_name, 'Certificate of Death') AS form_name,
    '103'                       AS municipal_form_no,
    d.registry_no, d.book_volume, d.book_page, d.status,

    d.first_name  AS deceased_first_name,
    d.middle_name AS deceased_middle_name,
    d.last_name   AS deceased_last_name,
    COALESCE(NULLIF(TRIM(CONCAT_WS(' ', d.first_name, d.middle_name, d.last_name)), ''),
             d.full_name)       AS deceased_full_name,
    d.sex, d.date_of_death, d.time_of_death, d.date_of_birth, d.age,
    d.civil_status, d.citizenship,
    COALESCE(d.place_of_death,
             TRIM(CONCAT_WS(', ', h.name, mu.name, pv.name))) AS place_of_death,
    COALESCE(d.religion_name, rel.name) AS religion,
    res.name  AS residence,
    occ.name  AS occupation,
    d.father_name, d.mother_name,
    COALESCE(d.immediate_cause, cod.name) AS immediate_cause,
    d.antecedent_cause, d.underlying_cause,
    d.interval_immediate, d.interval_antecedent, d.interval_underlying, d.other_conditions,
    d.maternal_condition, d.external_manner, d.external_place, d.autopsy,
    d.attendant_type, d.attendance_from, d.attendance_to, d.certifier_attended,
    d.medical_certifier, d.certifier_license_no, d.certifier_title, d.certifier_address,
    d.disposal_method, d.place_of_disposal, d.date_of_disposal, d.permit_type,
    d.burial_permit_no, d.burial_permit_date, d.transfer_permit_no, d.transfer_permit_date,
    d.informant_name, d.informant_relationship, d.informant_address, d.informant_date,
    d.prepared_by, d.prepared_by_title, d.prepared_by_date,
    d.received_by, d.received_by_title, d.received_by_date,
    d.registered_by, d.registered_by_title, d.registered_by_date,
    d.reviewed_by, d.reviewed_by_date,
    d.remarks,
    d.created_at, d.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `deaths` d
CROSS JOIN `office_profile` o
LEFT JOIN `hospitals`       h   ON h.id   = d.hospital_id
LEFT JOIN `municipalities`  mu  ON mu.id  = d.municipality_id
LEFT JOIN `provinces`       pv  ON pv.id  = d.province_id
LEFT JOIN `religions`       rel ON rel.id = d.religion_id
LEFT JOIN `residences`      res ON res.id = d.residence_id
LEFT JOIN `occupations`     occ ON occ.id = d.occupation_id
LEFT JOIN `causes_of_death` cod ON cod.id = d.cause_of_death_id;
