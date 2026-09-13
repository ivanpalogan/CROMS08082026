-- 43_birth_country_in_view.sql
-- v_birth_certificate never carried births.birth_country (added by migration 37), so a
-- certificate print - and anything else reading the view, including the "1-5. Child"
-- structured section in Data/FormCatalog.cs - could not show a foreign birth's country even
-- though it is stored and shown on the registration screen. No schema change: the column
-- already exists (migration 37, applied). This only restates the view to expose it, and adds
-- no other change - copied verbatim from 31_parents_married.sql's definition plus one column.

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
    b.remarks,
    b.created_at, b.updated_at,

    o.office_name, o.municipality AS office_municipality, o.province AS office_province,
    o.registrar_name, o.registrar_title
FROM `births` b
CROSS JOIN `office_profile` o;
