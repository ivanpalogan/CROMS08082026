-- =====================================================================
-- CROMS — 03_migrate_from_ocr.sql
-- Copies your existing data from the `NEWOCR` schema INTO `croms`.
--
-- SAFETY: this only READS from NEWOCR (INSERT ... SELECT). It never
-- writes to or alters the old database. If anything looks wrong you can
-- TRUNCATE the croms tables and re-run — the source is never at risk.
--
-- Run AFTER 01_schema.sql and 02_seed.sql.
--
-- CAVEATS (flagged, not errors):
--   * Old birth dates were stored as TEXT. They convert only if in
--     YYYY-MM-DD form; anything else lands as NULL for manual review.
--   * Old "place of birth/death/marriage" was a single free-text name.
--     The new schema splits place into hospital/church + municipality +
--     province dropdowns, so those old single names CANNOT be auto-split.
--     Migrated records get NULL place fields — re-select them on the form.
--     (The old names remain visible in the frozen NEWOCR database.)
--   * Old plaintext user accounts are NOT migrated (insecure).
--   * `parent` and `residents` (old stub tables) are not migrated.
-- =====================================================================
USE `croms`;

SET SESSION sql_mode = '';
SET FOREIGN_KEY_CHECKS = 0;

-- ---------------------------------------------------------------------
-- LOOKUPS  (ids preserved so surviving foreign keys still line up)
-- ---------------------------------------------------------------------
INSERT INTO `barangays` (`id`,`name`)
  SELECT `PkBaranggayID`, `BaranggayName` FROM `NEWOCR`.` baranggay`;

INSERT INTO `municipalities` (`id`,`name`)
  SELECT `PkMunicipalityID`, `MunicipalityName` FROM `NEWOCR`.`municipality`;

INSERT INTO `provinces` (`id`,`name`)
  SELECT `PKProvinceID`, `NameProvince` FROM `NEWOCR`.`province`;

INSERT INTO `hospitals` (`id`,`name`)
  SELECT `PkHostpitalID`, `HostpitalName` FROM `NEWOCR`.`hostpital`;

INSERT INTO `churches` (`id`,`name`)
  SELECT `PkSimbahanID`, `SimbahanName` FROM `NEWOCR`.`simbahan`;

INSERT INTO `causes_of_death` (`id`,`name`)
  SELECT `CauseOfDeathID`, `CauseOfDeathName` FROM `NEWOCR`.`causeofdeath`;

INSERT INTO `religions` (`id`,`name`)
  SELECT `ReligionID`, `ReligionName` FROM `NEWOCR`.`tblreligion`;

INSERT INTO `nationalities` (`id`,`name`)
  SELECT `NationalityID`, `NationalityName` FROM `NEWOCR`.`tblnationality`;

INSERT INTO `occupations` (`id`,`name`)
  SELECT `OccupationID`, `OccupationName` FROM `NEWOCR`.`tbloccupation`;

INSERT INTO `civil_statuses` (`id`,`name`)
  SELECT `Civil_StatusID`, `Civil_StatusName` FROM `NEWOCR`.`tblcivil_status`;

INSERT INTO `birth_orders` (`id`,`name`)
  SELECT `BirthOrderID`, `BirthOrderName` FROM `NEWOCR`.`tblbirthorder`;

INSERT INTO `type_of_births` (`id`,`name`)
  SELECT `TypeOfBirthID`, `TypeOfBirthName` FROM `NEWOCR`.`tbltypeofbirth`;

INSERT INTO `relationships` (`id`,`name`)
  SELECT `RelationshipID`, `RelationshipName` FROM `NEWOCR`.`tblrelationship`;

INSERT INTO `residences` (`id`,`name`)
  SELECT `ResidenceID`, `ResidenceName` FROM `NEWOCR`.`tblresidence`;

-- ---------------------------------------------------------------------
-- BIRTHS  (text date -> DATE; text weight -> DECIMAL; sex -> ENUM)
-- place (hospital/municipality/province) left NULL — see caveats.
-- ---------------------------------------------------------------------
INSERT INTO `births`
  (`id`,`first_name`,`middle_name`,`last_name`,`sex`,`date_of_birth`,
   `type_of_birth`,`birth_order`,`if_multiple_birth`,`weight_at_birth`,
   `mother_first_name`,`mother_middle_name`,`mother_last_name`,
   `father_first_name`,`father_middle_name`,`father_last_name`,`birth_image`)
  SELECT
    `IDBirth`, `FirstName`, `MiddleName`, `LastName`,
    CASE WHEN UPPER(LEFT(`Sex`,1)) = 'F' THEN 'Female' ELSE 'Male' END,
    STR_TO_DATE(`DateOfBirth`, '%Y-%m-%d'),
    `TypeOfBirth`, `BirthOrder`, `IfMultipleBirth`,
    CASE WHEN `WeightAtBirth` REGEXP '^[0-9]+\\.?[0-9]*$'
         THEN CAST(`WeightAtBirth` AS DECIMAL(5,2)) ELSE NULL END,
    `MotherFirstName`, `MotherMiddleName`, `MotherLastName`,
    `FatherFirstName`, `FatherMiddleName`, `FatherLastName`,
    `BirthImage`
  FROM `NEWOCR`.`tblbirth`;

-- ---------------------------------------------------------------------
-- DEATHS  (dates already DATE; age text -> INT; place left NULL)
-- ---------------------------------------------------------------------
INSERT INTO `deaths`
  (`id`,`first_name`,`middle_name`,`last_name`,`sex`,`date_of_death`,`date_of_birth`,
   `age`,`civil_status`,`religion_id`,`residence_id`,`occupation_id`,
   `father_name`,`mother_name`,`cause_of_death_id`,`death_image`)
  SELECT
    `IDDeath`, `FirstName`, `MiddleName`, `LastName`,
    CASE WHEN UPPER(LEFT(`Sex`,1)) = 'F' THEN 'Female' ELSE 'Male' END,
    `DateOfDeath`, `DateOfBirth`,
    CASE WHEN `Age` REGEXP '^[0-9]+$' THEN CAST(`Age` AS UNSIGNED) ELSE NULL END,
    `CivilStatus`, `ReligionID`, `ResidenceID`, `OccupationID`,
    `FatherName`, `MotherName`, `CauseOfDeathID`, `DeathImage`
  FROM `NEWOCR`.`tbldeath`;

-- ---------------------------------------------------------------------
-- MARRIAGES  (ages text -> INT; place + birthplaces left NULL)
-- ---------------------------------------------------------------------
INSERT INTO `marriages`
  (`id`,`husband_first_name`,`husband_middle_name`,`husband_last_name`,
   `wife_first_name`,`wife_middle_name`,`wife_last_name`,
   `husband_age`,`husband_date_of_birth`,`wife_age`,`wife_date_of_birth`,
   `husband_citizenship_id`,`wife_citizenship_id`,
   `husband_religion_id`,`wife_religion_id`,`husband_civil_status`,`wife_civil_status`,
   `husband_residence_id`,`wife_residence_id`,`date_of_marriage`,
   `time_of_marriage`,`marriage_image`)
  SELECT
    `IDMarriage`, `HusbandFirstName`, `HusbandMiddleName`, `HusbandLastName`,
    `WifeFirstName`, `WifeMiddleName`, `WifeLastName`,
    CASE WHEN `HusbandAge` REGEXP '^[0-9]+$' THEN CAST(`HusbandAge` AS UNSIGNED) ELSE NULL END,
    `HusbandDateOfBirth`,
    CASE WHEN `WifeAge` REGEXP '^[0-9]+$' THEN CAST(`WifeAge` AS UNSIGNED) ELSE NULL END,
    `WifeDateOfBirth`,
    `HusbandCitizenshipID`, `WifeCitizenshipID`,
    `HusbandReligionID`, `WifeReligionID`, `HusbandCivilStatus`, `WifeCivilStatus`,
    `HusbandResidenceID`, `WifeResidenceID`, `DateOfMarriage`,
    `TimeOfMarriage`, `MarriageImage`
  FROM `NEWOCR`.`tblmarriage`;

-- ---------------------------------------------------------------------
-- QUEUE TICKETS  (MerriageID typo -> marriage_id)
-- ---------------------------------------------------------------------
INSERT INTO `queue_tickets`
  (`id`,`full_name`,`number_queue`,`date`,`time`,`status`,`name_reviewer`,
   `document_type`,`birth_id`,`death_id`,`marriage_id`)
  SELECT
    `QueueID`, `FullName`, `NumberQueue`, `Date`, `Time`, `Status`, `NameReviewer`,
    `DocumentType`, `BirthID`, `DeathID`, `MerriageID`
  FROM `NEWOCR`.`queuedocument`;

SET FOREIGN_KEY_CHECKS = 1;

-- Verify counts: old vs new should match.
SELECT 'births'   AS tbl, (SELECT COUNT(*) FROM `NEWOCR`.`tblbirth`)    AS old_rows, (SELECT COUNT(*) FROM `births`)    AS new_rows
UNION ALL SELECT 'deaths',    (SELECT COUNT(*) FROM `NEWOCR`.`tbldeath`),     (SELECT COUNT(*) FROM `deaths`)
UNION ALL SELECT 'marriages', (SELECT COUNT(*) FROM `NEWOCR`.`tblmarriage`),  (SELECT COUNT(*) FROM `marriages`)
UNION ALL SELECT 'queue',     (SELECT COUNT(*) FROM `NEWOCR`.`queuedocument`),(SELECT COUNT(*) FROM `queue_tickets`);

-- Done. Old database `NEWOCR` is unchanged.
