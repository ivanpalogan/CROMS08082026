-- 38_form90_applicant_blocks.sql
-- The Municipal Form 90 applicant blocks the licence screen did not have.
--
-- Measured off the office's own blank MF-90 (Docs/AgencyForms, Revised January 1993), which is
-- a two-column form - one contracting party per column - and asks, for EACH party:
--   Name of Father          First / Middle / Last, Citizenship, Residence
--   Name of Mother          First / Middle / Last, Citizenship, Residence
--   Person who gave consent or advice
--                           First / Middle / Last, Relationship, Citizenship, Residence
--   If previously married   how the marriage was dissolved, place (city/municipality + province),
--                           date
-- The consent/advice block is ONE slot per party, not two: the form has one, and the office
-- confirmed only one guardian is needed. It covers either the consent-giver or the advice-giver.
--
-- THREE NAME CELLS, NOT ONE JOINED NAME. husband_father_name / husband_mother_name (and the
-- wife's) from migration 33 hold one joined string. MF-90 prints three separate cells, and
-- turning a joined name back into cells is exactly the failure recorded on 2026-09-02 for OCR
-- names: "JUAN DELA CRUZ" cannot be told apart from "JUAN DELA / CRUZ" or "JUAN / DELA CRUZ",
-- so a two-word surname gets mangled on print. The cells are therefore stored as entered.
--   The joined columns are RETIRED, not dropped: CROMS stops writing them, and reads one only
--   for a licence filed before this migration that has no cells. Dropping them would erase the
--   only record of what those older applications stated.
--
-- PLACE DISSOLVED IS TWO COLUMNS, not "City, Province" in one. place_of_birth is already stored
-- joined and split back on the comma, which is the debt noted on 2026-07-14 (M2); a new field
-- does not repeat it.
--
-- NO BACKFILL. A licence filed before these columns existed did not state these facts; writing
-- anything into them would put a fabricated entry in a government record.
--
-- ASCII ONLY. Applied by piping into the mysql client, which decodes with the console code page
-- (how the enye in Penablanca was corrupted on 2026-09-06). Nothing here is non-ASCII.
--
-- Idempotent: MySQL has no ADD COLUMN IF NOT EXISTS, so each column goes through a guarded
-- throwaway procedure, the same pattern as 36_license_sex.sql. Guarded per column rather than
-- per block, so a run interrupted half-way is completed by the next run instead of skipped.

DROP PROCEDURE IF EXISTS _croms_f90_add;
DELIMITER //
CREATE PROCEDURE _croms_f90_add(IN col VARCHAR(64), IN ddl VARCHAR(100))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'marriage_licenses'
                     AND COLUMN_NAME = col) THEN
        SET @croms_f90_sql = CONCAT('ALTER TABLE `marriage_licenses` ADD COLUMN `', col, '` ', ddl);
        PREPARE croms_f90_stmt FROM @croms_f90_sql;
        EXECUTE croms_f90_stmt;
        DEALLOCATE PREPARE croms_f90_stmt;
    END IF;
END //
DELIMITER ;

-- ---- husband / party 1 ------------------------------------------------------------
CALL _croms_f90_add('husband_father_first_name',          'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_father_middle_name',         'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_father_last_name',           'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_father_citizenship',         'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_father_residence',           'VARCHAR(200) NULL');
CALL _croms_f90_add('husband_mother_first_name',          'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_mother_middle_name',         'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_mother_last_name',           'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_mother_citizenship',         'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_mother_residence',           'VARCHAR(200) NULL');
CALL _croms_f90_add('husband_consent_first_name',         'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_consent_middle_name',        'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_consent_last_name',          'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_consent_relationship',       'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_consent_citizenship',        'VARCHAR(60)  NULL');
CALL _croms_f90_add('husband_consent_residence',          'VARCHAR(200) NULL');
CALL _croms_f90_add('husband_prev_dissolution',           'VARCHAR(120) NULL');
CALL _croms_f90_add('husband_prev_dissolved_municipality','VARCHAR(120) NULL');
CALL _croms_f90_add('husband_prev_dissolved_province',    'VARCHAR(120) NULL');
CALL _croms_f90_add('husband_prev_dissolved_date',        'DATE         NULL');

-- ---- wife / party 2 ---------------------------------------------------------------
CALL _croms_f90_add('wife_father_first_name',             'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_father_middle_name',            'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_father_last_name',              'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_father_citizenship',            'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_father_residence',              'VARCHAR(200) NULL');
CALL _croms_f90_add('wife_mother_first_name',             'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_mother_middle_name',            'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_mother_last_name',              'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_mother_citizenship',            'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_mother_residence',              'VARCHAR(200) NULL');
CALL _croms_f90_add('wife_consent_first_name',            'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_consent_middle_name',           'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_consent_last_name',             'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_consent_relationship',          'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_consent_citizenship',           'VARCHAR(60)  NULL');
CALL _croms_f90_add('wife_consent_residence',             'VARCHAR(200) NULL');
CALL _croms_f90_add('wife_prev_dissolution',              'VARCHAR(120) NULL');
CALL _croms_f90_add('wife_prev_dissolved_municipality',   'VARCHAR(120) NULL');
CALL _croms_f90_add('wife_prev_dissolved_province',       'VARCHAR(120) NULL');
CALL _croms_f90_add('wife_prev_dissolved_date',           'DATE         NULL');

DROP PROCEDURE IF EXISTS _croms_f90_add;

-- NOT DONE HERE, on purpose:
--   * The retired husband_/wife_father_name and _mother_name columns are left in place (see
--     above). Nothing new writes them.
--   * "Degree of Relationship of Contracting Parties" (MF-90 y406) is NOT added. It is the
--     Family Code Arts. 37-38 declaration, and adding a field invites a check that could refuse
--     a licence - that is the office's call, raised in the backlog, not ours to invent.
--   * marriages (Form 97) keeps its joined father/mother columns: MF-97 prints one cell there.
