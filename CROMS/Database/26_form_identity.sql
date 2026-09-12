-- 26_form_identity.sql
-- Form identification, office branding assets, and the flat report views that
-- Crystal Reports binds to.
--
-- WHY EACH PIECE EXISTS
--
--   1. FORM IDENTITY ON THE RECORD. Until now a row in `births` recorded WHAT was
--      registered but not WHICH FORM it came off. Registry number was already there;
--      form identity was not, and the office receives several revisions of the same
--      certificate (MF-102 Revised January 1993 and Revised January 2007 print the
--      same fields in different places). A report cannot reproduce the right layout
--      for a record whose form it does not know, so `form_code` + `form_name` are now
--      stored beside `registry_no` on all three registry tables. `form_code` is the
--      machine key ("MF-102-2007"); `form_name` is what staff and the report header
--      call it ("Certificate of Live Birth").
--
--   2. OFFICE ASSETS (`office_assets`). Logo and stamp are stored as SEPARATE rows of
--      the same table, keyed by `asset_kind`, so they are managed independently — the
--      office can replace a stamp without touching the logo, and a form revision can
--      carry its own stamp by setting `form_code`. A NULL `form_code` is the office-wide
--      default, which is what almost every form will use.
--
--   3. OFFICE PROFILE (`office_profile`). The printed forms carry the registering LGU
--      and the registrar's name in their header and signature blocks. Those were
--      hardcoded string literals in the printer ("Peñablanca", "Cagayan"), which means
--      a second LGU could not use the app and a change of registrar needed a rebuild.
--      One row, read by both the digital form and the report.
--
--   4. FLAT REPORT VIEWS (`v_birth_certificate`, `v_marriage_certificate`,
--      `v_death_certificate`). Crystal Reports wants ONE ROW with every field on it.
--      `marriages` and `deaths` store most of their values as lookup ids
--      (`husband_religion_id`, `cause_of_death_id`, …), so a report bound straight to
--      the table would print numbers. The views resolve every lookup to its name and
--      add the form identity and the office profile, so a .rpt binds to one view, drags
--      fields onto the canvas, and needs no joins of its own. They also keep the report
--      independent of column renames.
--
--      NOTE ON BLOBS: the views deliberately do NOT select `*_image` / `scan_image`.
--      A one-row certificate report does not need the 2 MB source scan, and pulling it
--      through the report datasource is what makes Crystal slow. The scan stays
--      reachable through the softcopy viewer, and logo/stamp reach the report as image
--      parameters instead.
--
--   5. `v_certificate_index` — one searchable list of every registered certificate
--      across the three tables, carrying form identity + registry number. This is what
--      a "pick a record, print its certificate" screen and a Crystal record-selection
--      report both need, and it is the only place the three tables are unioned.
--
--   6. `ocr_batch` gains `form_code` / `form_name` so a scan is traceable to the form
--      revision it was READ AS, not merely to the kind of certificate. When the layout
--      was refused (see the 2026-09-06 unknown-layout work) `form_code` is NULL while
--      `doc_kind` still says Birth — which is exactly the distinction an auditor needs.
--
-- Idempotent: column adds are guarded by information_schema (MySQL has no
-- ADD COLUMN IF NOT EXISTS), views use CREATE OR REPLACE, tables use IF NOT EXISTS.
-- Run once, after 25_document_intelligence.sql.

USE `croms`;

-- ---------------------------------------------------------------------------
-- 1. Form identity on the registry tables + the OCR batch log
-- ---------------------------------------------------------------------------

DROP PROCEDURE IF EXISTS _croms_form_identity;
DELIMITER //
CREATE PROCEDURE _croms_form_identity()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE t VARCHAR(64);
    DECLARE cur CURSOR FOR
        SELECT 'births' UNION ALL SELECT 'marriages' UNION ALL
        SELECT 'deaths' UNION ALL SELECT 'ocr_batch';
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    OPEN cur;
    add_cols: LOOP
        FETCH cur INTO t;
        IF done = 1 THEN LEAVE add_cols; END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = t
              AND COLUMN_NAME = 'form_code') THEN
            SET @s = CONCAT('ALTER TABLE `', t, '` ADD COLUMN `form_code` VARCHAR(30) NULL');
            PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = t
              AND COLUMN_NAME = 'form_name') THEN
            SET @s = CONCAT('ALTER TABLE `', t, '` ADD COLUMN `form_name` VARCHAR(120) NULL');
            PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = t
              AND INDEX_NAME = CONCAT('ix_', t, '_form_code')) THEN
            SET @s = CONCAT('ALTER TABLE `', t, '` ADD INDEX `ix_', t,
                            '_form_code` (`form_code`)');
            PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;
    END LOOP;
    CLOSE cur;

    -- A registry number is looked up constantly (search, report selection, duplicate
    -- check) and had no index on any of the three tables.
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND INDEX_NAME = 'ix_births_registry_no') THEN
        ALTER TABLE `births` ADD INDEX `ix_births_registry_no` (`registry_no`);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages'
          AND INDEX_NAME = 'ix_marriages_registry_no') THEN
        ALTER TABLE `marriages` ADD INDEX `ix_marriages_registry_no` (`registry_no`);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths'
          AND INDEX_NAME = 'ix_deaths_registry_no') THEN
        ALTER TABLE `deaths` ADD INDEX `ix_deaths_registry_no` (`registry_no`);
    END IF;

    -- `marriages` and `deaths` have no book_volume/book_page pair, so a certificate
    -- printed from them cannot state its book and page the way the paper form does.
    -- `births` already has book_volume; give all three the same two columns.
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths'
          AND COLUMN_NAME = 'book_volume') THEN
        ALTER TABLE `deaths` ADD COLUMN `book_volume` VARCHAR(30) NULL AFTER `registry_no`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'book_page') THEN
        ALTER TABLE `births` ADD COLUMN `book_page` VARCHAR(20) NULL AFTER `book_volume`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriages'
          AND COLUMN_NAME = 'book_page') THEN
        ALTER TABLE `marriages` ADD COLUMN `book_page` VARCHAR(20) NULL AFTER `book_volume`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deaths'
          AND COLUMN_NAME = 'book_page') THEN
        ALTER TABLE `deaths` ADD COLUMN `book_page` VARCHAR(20) NULL AFTER `book_volume`;
    END IF;
END //
DELIMITER ;
CALL _croms_form_identity();
DROP PROCEDURE IF EXISTS _croms_form_identity;

-- Backfill: every existing row came off the revision the office uses today. Only
-- rows with no identity at all are touched, so re-running changes nothing.
UPDATE `births`    SET `form_code` = 'MF-102-2007', `form_name` = 'Certificate of Live Birth'
 WHERE `form_code` IS NULL;
UPDATE `marriages` SET `form_code` = 'MF-97-1993',  `form_name` = 'Certificate of Marriage'
 WHERE `form_code` IS NULL;
UPDATE `deaths`    SET `form_code` = 'MF-103-2016', `form_name` = 'Certificate of Death'
 WHERE `form_code` IS NULL;

-- ---------------------------------------------------------------------------
-- 2. Logo and stamp, managed independently
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `office_assets` (
  `id`         INT NOT NULL AUTO_INCREMENT,
  `asset_kind` ENUM('Logo','Stamp') NOT NULL,
  `name`       VARCHAR(80)  NOT NULL,        -- what staff call it, e.g. "LGU Seal"
  `form_code`  VARCHAR(30)  NULL,            -- NULL = office-wide default
  `image`      LONGBLOB     NOT NULL,
  `mime_type`  VARCHAR(40)  NULL,
  `is_active`  TINYINT(1)   NOT NULL DEFAULT 1,
  `source`     VARCHAR(20)  NOT NULL DEFAULT 'Uploaded',  -- Uploaded / Scan
  `scan_id`    VARCHAR(30)  NULL,            -- ocr_batch.scan_id when source = 'Scan'
  `uploaded_by` VARCHAR(60) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_office_assets_kind` (`asset_kind`, `is_active`),
  KEY `ix_office_assets_form` (`form_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------------
-- 3. Office profile — one row, read by the digital form and the report header
-- ---------------------------------------------------------------------------

-- NOTE ON THE ENYE. "Peñablanca" is deliberately NOT written as a literal in this
-- file. This script is applied by piping it into the mysql client, and the client
-- decodes the file using the console's active code page — on this office's Windows
-- machines that is a DOS code page, so the two UTF-8 bytes of "ñ" (C3 B1) were
-- decoded as two box-drawing characters and STORED that way. It reached the printed
-- certificate as "Pe├▒ablanca". So the column default is plain ASCII and the real
-- spelling is written below through CONVERT(X'…' USING utf8mb4), which states the
-- bytes explicitly and cannot be re-interpreted by any client charset.
CREATE TABLE IF NOT EXISTS `office_profile` (
  `id`              INT NOT NULL DEFAULT 1,
  `office_name`     VARCHAR(120) NOT NULL DEFAULT 'Office of the Local Civil Registrar',
  `municipality`    VARCHAR(80)  NOT NULL DEFAULT 'Penablanca',
  `province`        VARCHAR(80)  NOT NULL DEFAULT 'Cagayan',
  `region`          VARCHAR(80)  NULL,
  `registrar_name`  VARCHAR(120) NULL,
  `registrar_title` VARCHAR(80)  NULL DEFAULT 'Municipal Civil Registrar',
  `address`         VARCHAR(200) NULL,
  `contact`         VARCHAR(120) NULL,
  `updated_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  CONSTRAINT `ck_office_profile_single_row` CHECK (`id` = 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO `office_profile` (`id`) VALUES (1)
ON DUPLICATE KEY UPDATE `id` = `id`;

-- Write the municipality with its bytes stated explicitly: "Peñablanca" as UTF-8.
-- Only replaces the ASCII placeholder or an already-mangled value, so an office that
-- has edited its own details in Settings is never overwritten.
UPDATE `office_profile`
   SET `municipality` = CONVERT(X'5065C3B161626C616E6361' USING utf8mb4)
 WHERE `id` = 1
   AND (`municipality` = 'Penablanca'
        OR HEX(`municipality`) = '5065E2949CE2969261626C616E6361');

-- ---------------------------------------------------------------------------
-- 4. Flat report views — the Crystal Reports datasources
-- ---------------------------------------------------------------------------

CREATE OR REPLACE VIEW `v_birth_certificate` AS
SELECT
    b.id                        AS record_id,
    'births'                    AS record_table,
    COALESCE(b.form_code, 'MF-102-2007')             AS form_code,
    COALESCE(b.form_name, 'Certificate of Live Birth') AS form_name,
    '102'                       AS municipal_form_no,
    b.registry_no, b.book_volume, b.book_page, b.status, b.is_delayed,

    b.first_name  AS child_first_name,
    b.middle_name AS child_middle_name,
    b.last_name   AS child_last_name,
    TRIM(CONCAT_WS(' ', b.first_name, b.middle_name, b.last_name)) AS child_full_name,
    b.sex, b.date_of_birth, b.time_of_birth, b.place_of_birth,
    b.type_of_birth, b.birth_order, b.weight_grams,

    b.mother_first_name, b.mother_middle_name, b.mother_last_name,
    TRIM(CONCAT_WS(' ', b.mother_first_name, b.mother_middle_name, b.mother_last_name))
                                AS mother_maiden_name,
    b.mother_citizenship, b.mother_religion, b.mother_occupation, b.mother_age,
    b.mother_children_born_alive, b.mother_children_living, b.mother_children_dead,
    b.mother_residence,

    b.father_first_name, b.father_middle_name, b.father_last_name,
    TRIM(CONCAT_WS(' ', b.father_first_name, b.father_middle_name, b.father_last_name))
                                AS father_full_name,
    b.father_citizenship, b.father_religion, b.father_occupation, b.father_age,
    b.father_residence,

    b.parents_marriage_date, b.parents_marriage_place,
    b.attendant_type, b.attendant_name, b.attendant_title, b.attendant_address,
    b.informant_name, b.informant_relationship, b.informant_address, b.informant_date,
    b.prepared_by, b.received_by, b.remarks,
    b.created_at, b.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `births` b
CROSS JOIN `office_profile` o;

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

    m.wife_first_name, m.wife_middle_name, m.wife_last_name,
    TRIM(CONCAT_WS(' ', m.wife_first_name, m.wife_middle_name, m.wife_last_name))
                                AS wife_full_name,
    m.wife_age, m.wife_date_of_birth, m.wife_civil_status,
    wbp.name AS wife_place_of_birth,
    wc.name  AS wife_citizenship,
    wr.name  AS wife_religion,
    wres.name AS wife_residence,

    ch.name  AS church_name,
    pm.name  AS place_municipality,
    pp.name  AS place_province,
    TRIM(CONCAT_WS(', ', ch.name, pm.name, pp.name)) AS place_of_marriage,
    m.date_of_marriage, m.time_of_marriage, m.solemnizer,
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

-- ---------------------------------------------------------------------------
-- 5. One list of every certificate, for record selection and index reports
-- ---------------------------------------------------------------------------

CREATE OR REPLACE VIEW `v_certificate_index` AS
SELECT 'births' AS record_table, record_id, form_code, form_name, municipal_form_no,
       registry_no, book_volume, book_page, status,
       child_full_name AS subject_name, date_of_birth AS event_date, created_at
  FROM `v_birth_certificate`
UNION ALL
SELECT 'marriages', record_id, form_code, form_name, municipal_form_no,
       registry_no, book_volume, book_page, status,
       TRIM(CONCAT_WS(' & ', NULLIF(husband_full_name, ''), NULLIF(wife_full_name, ''))),
       date_of_marriage, created_at
  FROM `v_marriage_certificate`
UNION ALL
SELECT 'deaths', record_id, form_code, form_name, municipal_form_no,
       registry_no, book_volume, book_page, status,
       deceased_full_name, date_of_death, created_at
  FROM `v_death_certificate`;
