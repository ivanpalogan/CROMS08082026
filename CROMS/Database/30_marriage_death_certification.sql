-- 30_marriage_death_certification.sql
-- The foot of Municipal Form 97 and Municipal Form 103 - the same gap 28 closed for
-- the Certificate of Live Birth, now for the other two registrable events.
--
-- WHAT THE PAPER SAYS AND THE REGISTRY COULD NOT HOLD
--
--   MF-103 (Certificate of Death) ends with four signed blocks, all typed:
--     26. CERTIFICATION OF INFORMANT - name in print, relationship to the deceased,
--         address, date
--     27. PREPARED BY                - name in print, title or position, date
--     28. RECEIVED BY                - name in print, title or position, date
--     29. REGISTERED BY THE CIVIL REGISTRAR - name in print, title or position, date
--   `deaths` had NONE of those columns. On the office's own sample every one of them is
--   filled in ink on the paper and blank in the database.
--
--   MF-97 (Certificate of Marriage) carries, below the two-column table:
--     the marriage LICENCE number, the date and place it was issued;
--     the solemnizing officer's POSITION or designation (the name was already stored,
--     the office he holds - "Municipal Mayor" - was not, and that is what makes the
--     solemnisation lawful under the Family Code);
--     the two WITNESSES;
--     RECEIVED AT THE OFFICE OF THE CIVIL REGISTRAR - name, title, date received;
--     and the four PARENTS of the contracting parties, which the scanner has been
--     extracting since 2026-09-02 with nowhere to put them.
--
-- Every column is NULL-able and nothing is backfilled, for the same reason as 28: a
-- value here is a fact off one sheet of paper, and inventing one for the rows that
-- predate this migration would put an unsigned signature in a government record.
--
-- No column is added for a block a form does not have. MF-97 (1993) has no separate
-- "Prepared by" and no separate "Registered by" - it has one received-at-the-office
-- block - so `marriages` gets one, not four. A form that does not have a row does not
-- get a column.

DROP PROCEDURE IF EXISTS _croms_cert_blocks;
DELIMITER //
CREATE PROCEDURE _croms_cert_blocks()
BEGIN
    DECLARE done INT DEFAULT 0;

    -- ---- deaths: items 26-29 -------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'informant_name') THEN
        ALTER TABLE `deaths`
            ADD COLUMN `informant_name`         VARCHAR(120) NULL,
            ADD COLUMN `informant_relationship` VARCHAR(60)  NULL,
            ADD COLUMN `informant_address`      VARCHAR(200) NULL,
            ADD COLUMN `informant_date`         DATE         NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'prepared_by') THEN
        ALTER TABLE `deaths`
            ADD COLUMN `prepared_by`         VARCHAR(120) NULL,
            ADD COLUMN `prepared_by_title`   VARCHAR(80)  NULL,
            ADD COLUMN `prepared_by_date`    DATE         NULL,
            ADD COLUMN `received_by`         VARCHAR(120) NULL,
            ADD COLUMN `received_by_title`   VARCHAR(80)  NULL,
            ADD COLUMN `received_by_date`    DATE         NULL,
            ADD COLUMN `registered_by`       VARCHAR(120) NULL,
            ADD COLUMN `registered_by_title` VARCHAR(80)  NULL,
            ADD COLUMN `registered_by_date`  DATE         NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'deaths' AND COLUMN_NAME = 'remarks') THEN
        ALTER TABLE `deaths` ADD COLUMN `remarks` VARCHAR(255) NULL;
    END IF;

    -- ---- marriages: parents, licence, officer, witnesses, receipt -------------
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'husband_father_name') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `husband_father_name` VARCHAR(120) NULL,
            ADD COLUMN `husband_mother_name` VARCHAR(120) NULL,
            ADD COLUMN `wife_father_name`    VARCHAR(120) NULL,
            ADD COLUMN `wife_mother_name`    VARCHAR(120) NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'solemnizer_position') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `solemnizer_position` VARCHAR(80)  NULL,
            ADD COLUMN `witness1_name`       VARCHAR(120) NULL,
            ADD COLUMN `witness2_name`       VARCHAR(120) NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'license_no') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `license_no`    VARCHAR(40)  NULL,
            ADD COLUMN `license_date`  DATE         NULL,
            ADD COLUMN `license_place` VARCHAR(150) NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'received_by') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `received_by`       VARCHAR(120) NULL,
            ADD COLUMN `received_by_title` VARCHAR(80)  NULL,
            ADD COLUMN `received_by_date`  DATE         NULL,
            ADD COLUMN `remarks`           VARCHAR(255) NULL;
    END IF;
END //
DELIMITER ;

CALL _croms_cert_blocks();
DROP PROCEDURE IF EXISTS _croms_cert_blocks;

-- ---------------------------------------------------------------------------
-- The two certificate datasources, restated so a printed certificate can show what
-- the paper one says. Both keep every column they already had.

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
    hbp.name AS husband_place_of_birth,
    hc.name  AS husband_citizenship,
    hr.name  AS husband_religion,
    hres.name AS husband_residence,
    m.husband_father_name, m.husband_mother_name,

    m.wife_first_name, m.wife_middle_name, m.wife_last_name,
    TRIM(CONCAT_WS(' ', m.wife_first_name, m.wife_middle_name, m.wife_last_name))
                                AS wife_full_name,
    m.wife_age, m.wife_date_of_birth, m.wife_civil_status,
    wbp.name AS wife_place_of_birth,
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
    m.license_no, m.license_date, m.license_place,
    m.received_by, m.received_by_title, m.received_by_date,
    m.remarks,
    m.created_at, m.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `marriages` m
CROSS JOIN `office_profile` o
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
    d.medical_certifier, d.certifier_license_no,
    d.disposal_method, d.place_of_disposal, d.date_of_disposal, d.permit_type,
    d.informant_name, d.informant_relationship, d.informant_address, d.informant_date,
    d.prepared_by, d.prepared_by_title, d.prepared_by_date,
    d.received_by, d.received_by_title, d.received_by_date,
    d.registered_by, d.registered_by_title, d.registered_by_date,
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
