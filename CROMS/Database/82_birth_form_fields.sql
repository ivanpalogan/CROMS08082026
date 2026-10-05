-- 82_birth_form_fields.sql
-- Birth Registration (Municipal Form 102) recommendations 5, 6 and 10:
--   * the hospital / clinic's own address (house no. / street and barangay) - item 4 of the
--     paper form asks for it in the SAME box as the facility name, so it gets its own columns
--     (never appended to place_of_birth, which is a comma-joined "facility, province,
--     municipality" value that is split back on load - a fourth part would re-split every row);
--   * item 5b "if multiple birth, child was (1st, 2nd, 3rd ...)", which the form carries and
--     CROMS did not;
--   * birth order stored as 1st, 2nd, 3rd ... like the paper form, instead of First, Second ...
--     The master list is renamed and extended to 20th, and the words already stored on
--     births are converted one for one (First -> 1st): the same fact, written as the form
--     writes it. Nothing is guessed - a value that is not one of the ten old words is left
--     exactly as it is.
-- time_of_birth already exists (births.time_of_birth) and is already in the view.
-- Nothing is backfilled for the new columns: NULL = not stated.
-- v_birth_certificate is restated (copied from 43_birth_country_in_view.sql) plus the three
-- new columns.
--
-- Idempotent (guarded ADD COLUMN, ASCII only, applied by piping into mysql).

DROP PROCEDURE IF EXISTS _croms_birth_form_fields;
DELIMITER //
CREATE PROCEDURE _croms_birth_form_fields()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'births' AND COLUMN_NAME = 'place_of_birth_house') THEN
        ALTER TABLE `births`
            ADD COLUMN `place_of_birth_house`    VARCHAR(120) NULL,
            ADD COLUMN `place_of_birth_barangay` VARCHAR(100) NULL,
            ADD COLUMN `multiple_birth_order`    VARCHAR(20)  NULL;
    END IF;
END //
DELIMITER ;
CALL _croms_birth_form_fields();
DROP PROCEDURE IF EXISTS _croms_birth_form_fields;

-- Birth order master list: First..Tenth -> 1st..10th, then 11th..20th.
UPDATE `birth_orders` SET `name` = '1st' WHERE `name` = 'First';
UPDATE `birth_orders` SET `name` = '2nd' WHERE `name` = 'Second';
UPDATE `birth_orders` SET `name` = '3rd' WHERE `name` = 'Third';
UPDATE `birth_orders` SET `name` = '4th' WHERE `name` = 'Fourth';
UPDATE `birth_orders` SET `name` = '5th' WHERE `name` = 'Fifth';
UPDATE `birth_orders` SET `name` = '6th' WHERE `name` = 'Sixth';
UPDATE `birth_orders` SET `name` = '7th' WHERE `name` = 'Seventh';
UPDATE `birth_orders` SET `name` = '8th' WHERE `name` = 'Eighth';
UPDATE `birth_orders` SET `name` = '9th' WHERE `name` = 'Ninth';
UPDATE `birth_orders` SET `name` = '10th' WHERE `name` = 'Tenth';
INSERT INTO `birth_orders` (`name`) SELECT '1st' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '1st');
INSERT INTO `birth_orders` (`name`) SELECT '2nd' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '2nd');
INSERT INTO `birth_orders` (`name`) SELECT '3rd' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '3rd');
INSERT INTO `birth_orders` (`name`) SELECT '4th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '4th');
INSERT INTO `birth_orders` (`name`) SELECT '5th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '5th');
INSERT INTO `birth_orders` (`name`) SELECT '6th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '6th');
INSERT INTO `birth_orders` (`name`) SELECT '7th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '7th');
INSERT INTO `birth_orders` (`name`) SELECT '8th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '8th');
INSERT INTO `birth_orders` (`name`) SELECT '9th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '9th');
INSERT INTO `birth_orders` (`name`) SELECT '10th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '10th');
INSERT INTO `birth_orders` (`name`) SELECT '11th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '11th');
INSERT INTO `birth_orders` (`name`) SELECT '12th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '12th');
INSERT INTO `birth_orders` (`name`) SELECT '13th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '13th');
INSERT INTO `birth_orders` (`name`) SELECT '14th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '14th');
INSERT INTO `birth_orders` (`name`) SELECT '15th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '15th');
INSERT INTO `birth_orders` (`name`) SELECT '16th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '16th');
INSERT INTO `birth_orders` (`name`) SELECT '17th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '17th');
INSERT INTO `birth_orders` (`name`) SELECT '18th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '18th');
INSERT INTO `birth_orders` (`name`) SELECT '19th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '19th');
INSERT INTO `birth_orders` (`name`) SELECT '20th' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` WHERE `name` = '20th');

-- Stored birth orders: the ten old words become ordinals; anything else is untouched.
UPDATE `births` SET `birth_order` = CASE `birth_order` WHEN 'First' THEN '1st' WHEN 'Second' THEN '2nd' WHEN 'Third' THEN '3rd' WHEN 'Fourth' THEN '4th' WHEN 'Fifth' THEN '5th' WHEN 'Sixth' THEN '6th' WHEN 'Seventh' THEN '7th' WHEN 'Eighth' THEN '8th' WHEN 'Ninth' THEN '9th' WHEN 'Tenth' THEN '10th' ELSE `birth_order` END
WHERE `birth_order` IN ('First', 'Second', 'Third', 'Fourth', 'Fifth', 'Sixth', 'Seventh', 'Eighth', 'Ninth', 'Tenth');

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
    b.sex, b.date_of_birth, b.time_of_birth, b.place_of_birth, b.birth_country,
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
    b.place_of_birth_house, b.place_of_birth_barangay, b.multiple_birth_order,
    b.remarks,
    b.created_at, b.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `births` b
CROSS JOIN `office_profile` o;
