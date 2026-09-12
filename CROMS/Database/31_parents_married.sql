-- 31_parents_married.sql
-- "The parents are not married" as a RECORDED FACT, not an inference from two blank boxes.
--
-- Item 18 of Municipal Form 102 asks for the date and place of the parents' marriage. Up to
-- now the registry could only hold those two values, so a record with both blank meant one
-- of two completely different things:
--
--     the parents are NOT MARRIED - which on a birth certificate is a legal statement
--     about the child (it is what makes the birth illegitimate under the Family Code,
--     and what an RA 9255 acknowledgement later attaches to); or
--
--     the parents ARE married and nobody filled the boxes in.
--
-- A report cannot tell those apart, and neither can the next clerk to open the record. This
-- column says which, so an empty pair of boxes stops being ambiguous.
--
--     1     the parents are married (the form's own default - most registrations)
--     0     the parents are NOT married; items 18a/18b do not apply and the screen
--           greys them out rather than inviting an entry
--     NULL  not stated - every row that predates this column
--
-- Existing rows are deliberately left NULL and NOT backfilled to either value. Reading a
-- blank date as "unmarried" would stamp illegitimacy onto records that simply were not
-- filled in; reading it as "married" would assert a marriage nobody recorded. NULL is the
-- truth about what the database knows, and the screen treats it as the default (married,
-- fields enabled) so nothing already entered changes appearance.

DROP PROCEDURE IF EXISTS _croms_parents_married;
DELIMITER //
CREATE PROCEDURE _croms_parents_married()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'parents_married') THEN
        ALTER TABLE `births` ADD COLUMN `parents_married` TINYINT(1) NULL
            COMMENT '1 married, 0 not married, NULL not stated (pre-dates the column)'
            AFTER `parents_marriage_place`;
    END IF;
END //
DELIMITER ;

CALL _croms_parents_married();
DROP PROCEDURE IF EXISTS _croms_parents_married;

-- The certificate datasource has to carry it, or a printed certificate cannot say that
-- item 18 was answered "not married" rather than left blank. Restated in full so the
-- view definition stays in one place.

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

    b.parents_marriage_date, b.parents_marriage_place, b.parents_married,
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
