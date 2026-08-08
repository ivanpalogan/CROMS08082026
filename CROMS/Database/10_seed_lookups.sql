-- =====================================================================
-- CROMS — 10_seed_lookups.sql
-- Seeds the generic / national-standard lookup lists so the registration
-- form dropdowns are usable. These are country-wide reference values
-- (nationalities, religions, occupations, civil statuses, type of births),
-- NOT Penablanca-specific data. Local geo (barangays, hospitals, churches,
-- municipalities) is official LGU data and is seeded separately from the
-- list the LCRO provides — it is intentionally NOT guessed here.
--
-- Idempotent: each value is inserted only if a row of the same name does
-- not already exist, so re-running is safe and it never duplicates the
-- existing (including legacy) rows. Values can be edited later in the
-- Master Files module.
-- Run any time after 01_schema.sql / 02_seed.sql.
-- =====================================================================
USE `croms`;

-- ---- nationalities / citizenship ----
INSERT INTO `nationalities` (`name`)
SELECT v FROM (
  SELECT 'Filipino' v UNION ALL SELECT 'American' UNION ALL SELECT 'Chinese' UNION ALL
  SELECT 'Japanese' UNION ALL SELECT 'Korean' UNION ALL SELECT 'British' UNION ALL
  SELECT 'Australian' UNION ALL SELECT 'Canadian' UNION ALL SELECT 'Indian' UNION ALL
  SELECT 'Spanish' UNION ALL SELECT 'Others'
) d WHERE NOT EXISTS (SELECT 1 FROM `nationalities` x WHERE x.`name` = d.v);

-- ---- religions ----
INSERT INTO `religions` (`name`)
SELECT v FROM (
  SELECT 'Roman Catholic' v UNION ALL SELECT 'Islam' UNION ALL
  SELECT 'Iglesia ni Cristo' UNION ALL SELECT 'Born Again Christian' UNION ALL
  SELECT 'Seventh-day Adventist' UNION ALL SELECT 'Jehovah''s Witness' UNION ALL
  SELECT 'Protestant' UNION ALL SELECT 'Aglipayan (Philippine Independent Church)' UNION ALL
  SELECT 'Baptist' UNION ALL SELECT 'Methodist' UNION ALL SELECT 'Others'
) d WHERE NOT EXISTS (SELECT 1 FROM `religions` x WHERE x.`name` = d.v);

-- ---- occupations ----
INSERT INTO `occupations` (`name`)
SELECT v FROM (
  SELECT 'Farmer' v UNION ALL SELECT 'Teacher' UNION ALL SELECT 'Student' UNION ALL
  SELECT 'Housekeeper' UNION ALL SELECT 'Driver' UNION ALL SELECT 'Government Employee' UNION ALL
  SELECT 'Self-employed' UNION ALL SELECT 'Business Owner' UNION ALL SELECT 'Laborer' UNION ALL
  SELECT 'Vendor' UNION ALL SELECT 'Overseas Filipino Worker (OFW)' UNION ALL
  SELECT 'Fisherman' UNION ALL SELECT 'Nurse' UNION ALL SELECT 'Engineer' UNION ALL
  SELECT 'Unemployed' UNION ALL SELECT 'Others'
) d WHERE NOT EXISTS (SELECT 1 FROM `occupations` x WHERE x.`name` = d.v);

-- ---- civil statuses ----
INSERT INTO `civil_statuses` (`name`)
SELECT v FROM (
  SELECT 'Single' v UNION ALL SELECT 'Married' UNION ALL SELECT 'Widowed' UNION ALL
  SELECT 'Separated' UNION ALL SELECT 'Divorced' UNION ALL SELECT 'Annulled'
) d WHERE NOT EXISTS (SELECT 1 FROM `civil_statuses` x WHERE x.`name` = d.v);

-- ---- type of births ----
INSERT INTO `type_of_births` (`name`)
SELECT v FROM (
  SELECT 'Single' v UNION ALL SELECT 'Twin' UNION ALL SELECT 'Triplet' UNION ALL
  SELECT 'Quadruplet'
) d WHERE NOT EXISTS (SELECT 1 FROM `type_of_births` x WHERE x.`name` = d.v);

-- ---- birth orders ----
INSERT INTO `birth_orders` (`name`)
SELECT v FROM (
  SELECT 'First' v UNION ALL SELECT 'Second' UNION ALL SELECT 'Third' UNION ALL
  SELECT 'Fourth' UNION ALL SELECT 'Fifth' UNION ALL SELECT 'Sixth' UNION ALL
  SELECT 'Seventh' UNION ALL SELECT 'Eighth' UNION ALL SELECT 'Ninth' UNION ALL SELECT 'Tenth'
) d WHERE NOT EXISTS (SELECT 1 FROM `birth_orders` x WHERE x.`name` = d.v);

-- ---- relationships (informant relationship to registrant) ----
INSERT INTO `relationships` (`name`)
SELECT v FROM (
  SELECT 'Mother' v UNION ALL SELECT 'Father' UNION ALL SELECT 'Grandparent' UNION ALL
  SELECT 'Guardian' UNION ALL SELECT 'Sibling' UNION ALL SELECT 'Spouse' UNION ALL
  SELECT 'Relative' UNION ALL SELECT 'Attending Physician' UNION ALL
  SELECT 'Administrator' UNION ALL SELECT 'Self' UNION ALL SELECT 'Others'
) d WHERE NOT EXISTS (SELECT 1 FROM `relationships` x WHERE x.`name` = d.v);
