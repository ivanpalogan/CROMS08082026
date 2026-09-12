-- =====================================================================
-- CROMS - 33_marriage_workflow.sql
--
-- The marriage module as it actually runs in an LCRO:
--
--   Form 90 application -> requirements -> 10-day posting (FC Art. 17)
--     -> licence issued -> 120-day validity (FC Art. 20)
--     -> wedding -> Form 97 received -> registered -> copies -> PSA/OCRG
--
-- Before this migration `marriage_licenses` had seven columns (two free-text
-- names, two dates, a status) and `marriages.license_no` was free text, so a
-- certificate could quote a licence that did not exist, had lapsed, or had
-- already been spent on another couple, and nothing would object.
--
-- EVERYTHING HERE IS ADDITIVE AND NULL-ABLE. Nothing is backfilled: the one
-- legacy marriage keeps NULL licence / registration-type / review columns,
-- because inventing a consent, an issue date or a registration date nobody
-- recorded would put a fabricated fact in a government record.
--
-- Statute-derived NUMBERS (posting days, validity days, consent / advice age
-- bands, reporting periods) are rows in `app_settings`, not constants in code:
-- the Family Code wording ("between the ages of eighteen and twenty-one") is
-- read differently by different offices, and Penablanca's reading has to be
-- confirmed by the registrar, not guessed by the software.
--
-- ASCII ONLY: this file is applied by piping into the mysql client, which
-- decodes with the console code page (see the 2026-09-06 enye incident).
-- Idempotent: every ALTER is guarded; seeds use INSERT IGNORE.
-- =====================================================================

-- ---------------------------------------------------------------- settings
CREATE TABLE IF NOT EXISTS `app_settings` (
  `setting_key`   VARCHAR(60)  NOT NULL,
  `setting_value` VARCHAR(255) NULL,
  `description`   VARCHAR(255) NULL,
  `updated_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`setting_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO `app_settings` (setting_key, setting_value, description) VALUES
 ('MARRIAGE_POSTING_DAYS',          '10',  'FC Art. 17: notice posted for ten consecutive days. Day 1 = posting start; licence may issue the day after the last day.'),
 ('MARRIAGE_LICENSE_VALIDITY_DAYS', '120', 'FC Art. 20: valid 120 days from the date of issue. Last valid day = issue date + 120 (first day excluded, last included - Admin Code s.31). CONFIRM WITH LCRO.'),
 ('MARRIAGE_EXPIRING_SOON_DAYS',    '30',  'Warn when an issued licence has this many days or fewer left.'),
 ('MARRIAGE_CONSENT_AGE_FROM',      '18',  'FC Art. 14 parental consent band, lower bound (inclusive).'),
 ('MARRIAGE_CONSENT_AGE_TO',        '20',  'FC Art. 14 "between eighteen and twenty-one" read as 18-20. CONFIRM WITH LCRO.'),
 ('MARRIAGE_ADVICE_AGE_FROM',       '21',  'FC Art. 15 parental advice band, lower bound (inclusive).'),
 ('MARRIAGE_ADVICE_AGE_TO',         '24',  'FC Art. 15 "between twenty-one and twenty-five" read as 21-24. CONFIRM WITH LCRO.'),
 ('MARRIAGE_DEFERRAL_MONTHS',       '3',   'FC Arts. 15-16: licence not issued until three months after posting completes when advice is unfavourable/not obtained, or required counselling is not attached.'),
 ('MARRIAGE_REPORT_DAYS_LICENSED',  '15',  'FC Art. 23: solemnizing officer sends the certificate within 15 days after the marriage.'),
 ('MARRIAGE_REPORT_DAYS_EXEMPT',    '30',  'FC Art. 30: licence-exempt marriages - affidavit and certificate sent within 30 days.'),
 ('MARRIAGE_DELAYED_POSTING_DAYS',  '10',  'Posting of the notice of delayed registration (AO 1 s.1993; LCRO charters use 10 days). CONFIRM WITH LCRO.'),
 ('MARRIAGE_LICENSE_PAYMENT_REQUIRED','1', '1 = the Treasury official receipt number must be recorded before a licence can issue.'),
 ('PSA_TRANSMITTAL_DUE_DAY',        '10',  'Registered documents transmitted to the PSA provincial office on or before this day of the following month. CONFIRM WITH LCRO / PSO.');

-- ------------------------------------------------- requirement catalogue
-- Which supporting documents apply, and WHY. `rule_key` is interpreted by
-- CROMS.Data.MarriageRules; `is_active` lets the office switch an LCRO-policy
-- requirement (CENOMAR) off without a code change. CROMS never issues any of
-- these documents - it records that they were presented and checked.
CREATE TABLE IF NOT EXISTS `marriage_requirement_types` (
  `code`        VARCHAR(30)  NOT NULL,
  `label`       VARCHAR(120) NOT NULL,
  `applies_to`  VARCHAR(10)  NOT NULL DEFAULT 'License',   -- License / Marriage
  `rule_key`    VARCHAR(30)  NOT NULL DEFAULT 'Always',
  `per_party`   TINYINT(1)   NOT NULL DEFAULT 1,           -- 1 = one row per applicant, 0 = one for the couple
  `blocking`    TINYINT(1)   NOT NULL DEFAULT 1,
  `legal_basis` VARCHAR(120) NULL,
  `is_active`   TINYINT(1)   NOT NULL DEFAULT 1,
  `sort_order`  INT          NOT NULL DEFAULT 100,
  PRIMARY KEY (`code`, `applies_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO `marriage_requirement_types`
 (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order) VALUES
 ('BIRTH_CERT',       'Birth certificate (or baptismal certificate)',                    'License','Always',            1,1,'Family Code Art. 12',                 1, 10),
 ('VALID_ID',         'Valid government-issued ID',                                      'License','Always',            1,1,'LCRO requirement',                    1, 20),
 ('CENOMAR',          'CENOMAR - Certificate of No Marriage (PSA-issued)',               'License','Always',            1,1,'LCRO requirement (PSA-issued; CROMS does not issue it)', 1, 30),
 ('RPFP_CERT',        'Certificate of Compliance - responsible parenthood / family planning', 'License','Always',       0,1,'RA 10354 s.15',                       1, 40),
 ('PARENTAL_CONSENT', 'Written parental consent',                                        'License','ConsentAge',        1,1,'Family Code Art. 14',                 1, 50),
 ('PARENTAL_ADVICE',  'Parental advice',                                                 'License','AdviceAge',         1,1,'Family Code Art. 15',                 1, 60),
 ('COUNSELING',       'Marriage counselling certificate',                                'License','CounselingAge',     0,1,'Family Code Art. 16',                 1, 70),
 ('PREV_MARRIAGE',    'Proof the previous marriage ended (death certificate / decree)',  'License','PreviouslyMarried', 1,1,'Family Code Art. 13',                 1, 80),
 ('LEGAL_CAPACITY',   'Certificate of legal capacity to contract marriage (embassy)',    'License','Foreigner',         1,1,'Family Code Art. 21',                 1, 90),
 ('EXEMPT_AFFIDAVIT', 'Affidavit supporting the licence exemption',                      'Marriage','Exempt',           0,1,'Family Code Arts. 29, 34',            1, 10),
 ('DELAYED_AFFIDAVIT','Affidavit of delayed registration',                               'Marriage','Delayed',          0,1,'AO 1 s.1993 (delayed registration of marriage)', 1, 20),
 ('DELAYED_SUPPORT',  'Supporting evidence of the marriage (e.g. solemnizer / church certification)', 'Marriage','Delayed', 0,1,'AO 1 s.1993',                    1, 30),
 ('PREV_MARRIAGE',    'Proof the previous marriage ended (death certificate / decree)',  'Marriage','PreviouslyMarried',1,1,'Family Code Art. 13',                 1, 40);
-- PREV_MARRIAGE appears twice on purpose: a licensed marriage satisfies it from the
-- licence file, a licence-exempt one has to carry it on the marriage record itself.

-- --------------------------------------- requirement / document records
-- One table for every supporting document on either a licence application or a
-- marriage record. `owner_type` says which.
CREATE TABLE IF NOT EXISTS `marriage_requirements` (
  `id`              INT NOT NULL AUTO_INCREMENT,
  `owner_type`      VARCHAR(10)  NOT NULL,                 -- License / Marriage
  `owner_id`        INT          NOT NULL,
  `party`           VARCHAR(10)  NOT NULL DEFAULT 'Both',  -- Husband / Wife / Both
  `req_code`        VARCHAR(30)  NOT NULL,
  `req_label`       VARCHAR(120) NULL,
  `status`          VARCHAR(20)  NOT NULL DEFAULT 'Missing', -- Missing/Submitted/Verified/Rejected/Waived
  `outcome`         VARCHAR(20)  NULL,                     -- advice: Favorable/Unfavorable/Not obtained
  `given_by`        VARCHAR(120) NULL,                     -- consent/advice: who gave it
  `reference_no`    VARCHAR(80)  NULL,
  `doc_date`        DATE         NULL,
  `attachment`      LONGBLOB     NULL,
  `attachment_name` VARCHAR(160) NULL,
  `verified_by`     INT          NULL,
  `verified_at`     DATETIME     NULL,
  `notes`           VARCHAR(255) NULL,
  `created_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_mreq_owner_code_party` (`owner_type`, `owner_id`, `req_code`, `party`),
  KEY `ix_mreq_owner` (`owner_type`, `owner_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ------------------------------------------------------ lifecycle history
CREATE TABLE IF NOT EXISTS `marriage_history` (
  `id`          BIGINT NOT NULL AUTO_INCREMENT,
  `entity`      VARCHAR(20)  NOT NULL,          -- License / Marriage / Batch
  `entity_id`   INT          NOT NULL,
  `event`       VARCHAR(60)  NOT NULL,
  `from_status` VARCHAR(40)  NULL,
  `to_status`   VARCHAR(40)  NULL,
  `details`     TEXT         NULL,
  `user_id`     INT          NULL,
  `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_mhist_entity` (`entity`, `entity_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ------------------------------------------------------- copy distribution
-- FC Art. 23: the solemnizing officer furnishes the ORIGINAL to a contracting
-- party, sends the DUPLICATE and TRIPLICATE to the LCR, and keeps the
-- QUADRUPLICATE. CROMS tracks each copy's disposition; it records only what
-- staff confirm (the LCRO file copy is the one fact CROMS itself produces).
CREATE TABLE IF NOT EXISTS `marriage_copies` (
  `id`               INT NOT NULL AUTO_INCREMENT,
  `marriage_id`      INT NOT NULL,
  `copy_type`        VARCHAR(20)  NOT NULL,       -- Original/Duplicate/Triplicate/Quadruplicate
  `intended_for`     VARCHAR(120) NULL,
  `status`           VARCHAR(20)  NOT NULL DEFAULT 'Pending', -- Pending/Prepared/Released/Sent/Filed/Acknowledged
  `recipient`        VARCHAR(120) NULL,
  `disposition_date` DATE         NULL,
  `reference_no`     VARCHAR(80)  NULL,
  `remarks`          VARCHAR(255) NULL,
  `staff_id`         INT          NULL,
  `updated_at`       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_mcopy_marriage_type` (`marriage_id`, `copy_type`),
  CONSTRAINT `fk_mcopy_marriage` FOREIGN KEY (`marriage_id`) REFERENCES `marriages` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ------------------------------------------------------- PSA transmittal
-- Generic on purpose (record_table + record_id): births and deaths go to the
-- same PSA provincial office in the same monthly packet, so they can join these
-- tables later without a second design. Only marriages use it today.
CREATE TABLE IF NOT EXISTS `psa_transmittal_batches` (
  `id`               INT NOT NULL AUTO_INCREMENT,
  `batch_no`         VARCHAR(40)  NOT NULL,
  `record_kind`      VARCHAR(20)  NOT NULL DEFAULT 'Marriage',
  `period_year`      INT          NULL,
  `period_month`     INT          NULL,
  `date_prepared`    DATE         NULL,
  `date_sent`        DATE         NULL,
  `method`           VARCHAR(40)  NULL,          -- Physical Batch / Electronic Submission / Other Official Method
  `receiving_office` VARCHAR(150) NULL,
  `reference_no`     VARCHAR(80)  NULL,
  `ack_date`         DATE         NULL,
  `ack_reference`    VARCHAR(80)  NULL,
  `status`           VARCHAR(20)  NOT NULL DEFAULT 'Draft', -- Draft / Sent / Acknowledged
  `remarks`          VARCHAR(255) NULL,
  `prepared_by`      INT          NULL,
  `sent_by`          INT          NULL,
  `created_at`       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_psa_batch_no` (`batch_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `psa_transmittal_items` (
  `id`            INT NOT NULL AUTO_INCREMENT,
  `batch_id`      INT          NOT NULL,
  `record_table`  VARCHAR(20)  NOT NULL,
  `record_id`     INT          NOT NULL,
  `status`        VARCHAR(20)  NOT NULL DEFAULT 'Included', -- Included / Sent / Acknowledged / Returned
  `return_reason` VARCHAR(255) NULL,
  `returned_at`   DATETIME     NULL,
  `created_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_psa_item` (`batch_id`, `record_table`, `record_id`),
  KEY `ix_psa_item_record` (`record_table`, `record_id`),
  CONSTRAINT `fk_psa_item_batch` FOREIGN KEY (`batch_id`) REFERENCES `psa_transmittal_batches` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------- licence + marriage columns
DROP PROCEDURE IF EXISTS _croms_marriage_workflow;
DELIMITER //
CREATE PROCEDURE _croms_marriage_workflow()
BEGIN
    -- ---- marriage_licenses: a real Form 90 application ----------------------
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = 'application_no') THEN
        ALTER TABLE `marriage_licenses`
            MODIFY COLUMN `status` VARCHAR(30) NOT NULL DEFAULT 'Draft',
            ADD COLUMN `application_no`           VARCHAR(30)  NULL AFTER `id`,
            ADD COLUMN `husband_first_name`       VARCHAR(60)  NULL,
            ADD COLUMN `husband_middle_name`      VARCHAR(60)  NULL,
            ADD COLUMN `husband_last_name`        VARCHAR(60)  NULL,
            ADD COLUMN `husband_date_of_birth`    DATE         NULL,
            ADD COLUMN `husband_place_of_birth`   VARCHAR(150) NULL,
            ADD COLUMN `husband_citizenship`      VARCHAR(60)  NULL,
            ADD COLUMN `husband_civil_status`     VARCHAR(30)  NULL,
            ADD COLUMN `husband_religion`         VARCHAR(60)  NULL,
            ADD COLUMN `husband_residence`        VARCHAR(200) NULL,
            ADD COLUMN `husband_father_name`      VARCHAR(120) NULL,
            ADD COLUMN `husband_mother_name`      VARCHAR(120) NULL,
            ADD COLUMN `wife_first_name`          VARCHAR(60)  NULL,
            ADD COLUMN `wife_middle_name`         VARCHAR(60)  NULL,
            ADD COLUMN `wife_last_name`           VARCHAR(60)  NULL,
            ADD COLUMN `wife_date_of_birth`       DATE         NULL,
            ADD COLUMN `wife_place_of_birth`      VARCHAR(150) NULL,
            ADD COLUMN `wife_citizenship`         VARCHAR(60)  NULL,
            ADD COLUMN `wife_civil_status`        VARCHAR(30)  NULL,
            ADD COLUMN `wife_religion`            VARCHAR(60)  NULL,
            ADD COLUMN `wife_residence`           VARCHAR(200) NULL,
            ADD COLUMN `wife_father_name`         VARCHAR(120) NULL,
            ADD COLUMN `wife_mother_name`         VARCHAR(120) NULL,
            ADD COLUMN `posting_start`            DATE         NULL,
            ADD COLUMN `earliest_issue_date`      DATE         NULL,
            ADD COLUMN `deferral_reason`          VARCHAR(255) NULL,
            ADD COLUMN `issue_date`               DATE         NULL,
            ADD COLUMN `expiry_date`              DATE         NULL,
            ADD COLUMN `issued_by`                INT          NULL,
            ADD COLUMN `issued_at`                DATETIME     NULL,
            ADD COLUMN `payment_or_no`            VARCHAR(40)  NULL,
            ADD COLUMN `payment_amount`           DECIMAL(10,2) NULL,
            ADD COLUMN `payment_date`             DATE         NULL,
            ADD COLUMN `impediment_note`          VARCHAR(255) NULL,
            ADD COLUMN `registrar_override_by`    INT          NULL,
            ADD COLUMN `registrar_override_at`    DATETIME     NULL,
            ADD COLUMN `hold_reason`              VARCHAR(255) NULL,
            ADD COLUMN `cancel_reason`            VARCHAR(255) NULL,
            ADD COLUMN `remarks`                  VARCHAR(255) NULL,
            ADD COLUMN `created_by`               INT          NULL,
            ADD COLUMN `updated_at`               DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND INDEX_NAME = 'ux_mlic_application_no') THEN
        ALTER TABLE `marriage_licenses` ADD UNIQUE KEY `ux_mlic_application_no` (`application_no`);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND INDEX_NAME = 'ux_mlic_license_no') THEN
        UPDATE `marriage_licenses` SET license_no = NULL WHERE license_no = '';
        ALTER TABLE `marriage_licenses` ADD UNIQUE KEY `ux_mlic_license_no` (`license_no`);
    END IF;

    -- ---- marriages: link, basis, registration, review, OCR ------------------
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND COLUMN_NAME = 'license_id') THEN
        ALTER TABLE `marriages`
            ADD COLUMN `license_id`             INT          NULL,
            ADD COLUMN `license_basis`          VARCHAR(12)  NULL,   -- Licensed / Exempt ; NULL = legacy
            ADD COLUMN `exemption_basis`        VARCHAR(20)  NULL,   -- ART27 ART28 ART31 ART32 ART33 ART34
            ADD COLUMN `exemption_notes`        VARCHAR(255) NULL,
            ADD COLUMN `registration_type`      VARCHAR(10)  NULL,   -- Timely / Delayed ; NULL = legacy
            ADD COLUMN `delay_reason`           VARCHAR(255) NULL,
            ADD COLUMN `case_posting_start`     DATE         NULL,
            ADD COLUMN `registrar_review_status` VARCHAR(20) NULL,   -- Pending / Approved / Returned
            ADD COLUMN `registrar_review_by`    INT          NULL,
            ADD COLUMN `registrar_review_at`    DATETIME     NULL,
            ADD COLUMN `registrar_review_notes` VARCHAR(255) NULL,
            ADD COLUMN `return_reason`          VARCHAR(255) NULL,
            ADD COLUMN `date_registered`        DATE         NULL,
            ADD COLUMN `registered_by`          INT          NULL,
            ADD COLUMN `registered_at`          DATETIME     NULL,
            ADD COLUMN `ocr_scan_id`            VARCHAR(30)  NULL,
            ADD COLUMN `ocr_confidence`         INT          NULL,
            ADD COLUMN `ocr_weak_fields`        INT          NULL,
            ADD COLUMN `ocr_review_status`      VARCHAR(20)  NULL,   -- Required / Completed ; NULL = no scan
            ADD COLUMN `ocr_reviewed_by`        INT          NULL,
            ADD COLUMN `ocr_reviewed_at`        DATETIME     NULL,
            ADD COLUMN `psa_available_date`     DATE         NULL,
            ADD COLUMN `psa_available_reference` VARCHAR(80) NULL,
            ADD COLUMN `created_by`             INT          NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND INDEX_NAME = 'ux_marriages_license_id') THEN
        -- One issued licence produces at most one registered marriage. NULLs (exempt
        -- and legacy marriages) are not constrained by a UNIQUE index.
        ALTER TABLE `marriages` ADD UNIQUE KEY `ux_marriages_license_id` (`license_id`);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriages' AND CONSTRAINT_NAME = 'fk_marriages_license') THEN
        -- RESTRICT: a licence that a marriage stands on cannot be deleted.
        ALTER TABLE `marriages` ADD CONSTRAINT `fk_marriages_license`
            FOREIGN KEY (`license_id`) REFERENCES `marriage_licenses` (`id`) ON DELETE RESTRICT;
    END IF;
END //
DELIMITER ;

CALL _croms_marriage_workflow();
DROP PROCEDURE IF EXISTS _croms_marriage_workflow;

-- -------------------------------------------------- certificate datasource
-- Restated so a printed certificate quotes the LINKED licence's number and issue
-- date when there is one, falling back to the typed text for legacy rows. Every
-- column the view already had keeps its name, so the print map is unaffected.
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
