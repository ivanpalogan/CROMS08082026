-- 28_certification_block.sql
-- The bottom third of Municipal Form 102 — the part every certificate carries and
-- the registry could not hold.
--
-- Both revisions of the Certificate of Live Birth end with four signed blocks:
--
--     21a/19a  Attendant at birth (physician / nurse / midwife / hilot / other)
--     21b/19b  CERTIFICATION OF ATTENDANT AT BIRTH — signature, name in print,
--              title or position, address, and the DATE it was signed
--     22 /20   CERTIFICATION OF INFORMANT — signature, name in print,
--              relationship to the child, address, date
--     23 /21   PREPARED BY — signature, name in print, title or position, date
--     24 /22   RECEIVED BY / RECEIVED AT THE OFFICE OF THE CIVIL REGISTRAR
--     25       REGISTERED AT THE CIVIL REGISTRAR (2007 sheet only; the 1993
--              revision folds this into its own item 22)
--
-- `births` already held attendant_name/title/address, informant_name/relationship/
-- address/date, prepared_by and received_by (04_birth_form102.sql). What it had
-- nowhere to put was the DATE each certification was signed and the TITLE of the
-- two office signatories — which is most of what those blocks say. A certificate
-- reprinted without them is not the document that was signed: the date a birth was
-- certified and the date it was received at the LCRO are separate legal facts from
-- the date of birth, and they are what an audit of a delayed registration turns on.
--
-- Every column is NULL-able and nothing is backfilled. A value here is a fact off
-- one particular sheet of paper; inventing one for the rows that predate this
-- migration would put a signature date in a government record that nobody signed.

DROP PROCEDURE IF EXISTS _croms_certification_block;
DELIMITER //
CREATE PROCEDURE _croms_certification_block()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'attendant_date') THEN
        ALTER TABLE `births` ADD COLUMN `attendant_date` DATE NULL AFTER `attendant_address`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'prepared_by_title') THEN
        ALTER TABLE `births` ADD COLUMN `prepared_by_title` VARCHAR(80) NULL AFTER `prepared_by`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'prepared_by_date') THEN
        ALTER TABLE `births` ADD COLUMN `prepared_by_date` DATE NULL AFTER `prepared_by_title`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'received_by_title') THEN
        ALTER TABLE `births` ADD COLUMN `received_by_title` VARCHAR(80) NULL AFTER `received_by`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'received_by_date') THEN
        ALTER TABLE `births` ADD COLUMN `received_by_date` DATE NULL AFTER `received_by_title`;
    END IF;

    -- Item 25 on the 2007 sheet. Kept separate from received_by on purpose: on that
    -- form they are two different people signing two different things — the clerk who
    -- received the document, and the registrar who registered it.
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'registered_by') THEN
        ALTER TABLE `births` ADD COLUMN `registered_by` VARCHAR(120) NULL AFTER `received_by_date`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'registered_by_title') THEN
        ALTER TABLE `births` ADD COLUMN `registered_by_title` VARCHAR(80) NULL AFTER `registered_by`;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'registered_by_date') THEN
        ALTER TABLE `births` ADD COLUMN `registered_by_date` DATE NULL AFTER `registered_by_title`;
    END IF;
END //
DELIMITER ;

CALL _croms_certification_block();
DROP PROCEDURE IF EXISTS _croms_certification_block;

-- ---------------------------------------------------------------------------
-- The certificate datasource has to carry them too, or a printed certificate
-- still cannot show what the paper one says. Restated in full (CREATE OR REPLACE)
-- rather than patched, so the view definition lives in one place.

CREATE OR REPLACE VIEW `v_birth_certificate` AS
SELECT
    b.id                        AS record_id,
    'births'                    AS record_table,
    COALESCE(b.form_code, 'MF-102-2007')              AS form_code,
    COALESCE(b.form_name, 'Certificate of Live Birth') AS form_name,
    '102'                       AS municipal_form_no,
    b.registry_no, b.book_volume, b.book_page, b.status, b.is_delayed,
    b.date_registered,

    b.first_name                AS child_first_name,
    b.middle_name               AS child_middle_name,
    b.last_name                 AS child_last_name,
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
    b.attendant_date,
    b.informant_name, b.informant_relationship, b.informant_address, b.informant_date,
    b.prepared_by, b.prepared_by_title, b.prepared_by_date,
    b.received_by, b.received_by_title, b.received_by_date,
    b.registered_by, b.registered_by_title, b.registered_by_date,
    b.remarks,
    b.created_at, b.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `births` b
CROSS JOIN `office_profile` o;
