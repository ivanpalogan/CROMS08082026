-- 74_marriage_residence_parts.sql
-- Form 97: a party's residence was ONE pick-list id (marriages.husband_residence_id / wife_residence_id
-- into the `residences` master file), so a clerk could not record a real address: no barangay, no
-- house number or street, and anything outside the short master list was impossible.
-- The screen now asks for it the way Form 90 and the birth certificate print it:
--     Province, City / Municipality, Barangay, House No. / Street
-- Four SEPARATE columns per party, not one comma-joined value: a street such as "Block 5, Lot 12,
-- Avocado St." contains commas, so a joined string could not be split back into its cells reliably
-- (the M2 debt recorded 2026-07-14 for place_of_birth - a new field does not repeat it).
--
-- The old *_residence_id columns are LEFT IN PLACE and nothing is backfilled: they hold what older
-- records stated, and the restated view falls back to the master-file name when a record has no
-- structured address, so no printed certificate goes blank. NULL = not stated.
-- ASCII only (applied by piping through the mysql client). Idempotent. Requires migration 57.

DROP PROCEDURE IF EXISTS _croms_marriage_residence;
DELIMITER //
CREATE PROCEDURE _croms_marriage_residence()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_res_province') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_res_province` VARCHAR(100) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_res_municipality') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_res_municipality` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_res_barangay') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_res_barangay` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_res_house') THEN
        ALTER TABLE `marriages` ADD COLUMN `husband_res_house` VARCHAR(150) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_res_province') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_res_province` VARCHAR(100) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_res_municipality') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_res_municipality` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_res_barangay') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_res_barangay` VARCHAR(120) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'wife_res_house') THEN
        ALTER TABLE `marriages` ADD COLUMN `wife_res_house` VARCHAR(150) NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_marriage_residence();
DROP PROCEDURE IF EXISTS _croms_marriage_residence;

-- View: migration 57's definition, with the residence built from the four parts
-- ("House, Barangay, Municipality, Province") and the master-file name only as the fallback.
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
    COALESCE(NULLIF(TRIM(CONCAT_WS(', ', NULLIF(m.husband_res_house, ''), NULLIF(m.husband_res_barangay, ''),
                                         NULLIF(m.husband_res_municipality, ''), NULLIF(m.husband_res_province, ''))), ''),
             hres.name) AS husband_residence,
    m.husband_res_province, m.husband_res_municipality, m.husband_res_barangay, m.husband_res_house,
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
    COALESCE(NULLIF(TRIM(CONCAT_WS(', ', NULLIF(m.wife_res_house, ''), NULLIF(m.wife_res_barangay, ''),
                                         NULLIF(m.wife_res_municipality, ''), NULLIF(m.wife_res_province, ''))), ''),
             wres.name) AS wife_residence,
    m.wife_res_province, m.wife_res_municipality, m.wife_res_barangay, m.wife_res_house,
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
