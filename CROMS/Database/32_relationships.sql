-- 32_relationships.sql   (BR-16)
-- The informant's "Relationship to the ..." list, corrected against the forms.
--
-- ONE TABLE, TWO QUESTIONS. `relationships` is shared: Municipal Form 102 item 22 asks
-- "Relationship to the CHILD" and Municipal Form 103 item 26 asks "Relationship to the
-- DECEASED". Sharing the list meant a newborn's informant could be recorded as "Wife" and
-- a decedent's as "Attending Midwife" - each list carried the other's answers. Rather than
-- split the table (which would orphan the text already stored on records), each entry now
-- says which form it belongs on:
--
--     'Birth'  only on the Certificate of Live Birth
--     'Death'  only on the Certificate of Death
--     'Both'   valid on either
--
-- The screens filter on it. Nothing is hidden retroactively: the combos add back any value
-- a loaded record holds even when it is not in the filtered list, so an existing record
-- always shows what it actually says.
--
-- JUNK REMOVED. 'anthon' and 'athin' were typed into the Master File by accident. Checked
-- before deleting: `relationships` has no foreign key pointing at it - births and deaths
-- store the relationship as TEXT - and neither string appears in any record.
--
-- 'Self' is KEPT even though it cannot be true of either form (nobody informs on their own
-- birth or their own death). One existing birth record stores it, and quietly deleting the
-- option would not change that record - it would only make the value it holds look like a
-- typo. It is scoped to neither form, so it stops being offered while the record that has
-- it still displays it.

DROP PROCEDURE IF EXISTS _croms_relationships;
DELIMITER //
CREATE PROCEDURE _croms_relationships()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'relationships'
          AND COLUMN_NAME = 'applies_to') THEN
        ALTER TABLE `relationships`
            ADD COLUMN `applies_to` VARCHAR(10) NOT NULL DEFAULT 'Both'
            COMMENT 'Birth | Death | Both | None';
    END IF;
END //
DELIMITER ;

CALL _croms_relationships();
DROP PROCEDURE IF EXISTS _croms_relationships;

DELETE FROM `relationships` WHERE `name` IN ('anthon', 'athin');

-- The standard answers. Inserted only when absent, so a re-run adds nothing twice and the
-- office's own additions are never disturbed.
INSERT INTO `relationships` (`name`, `applies_to`)
SELECT * FROM (
    SELECT 'Mother' AS n, 'Both' AS a UNION ALL
    SELECT 'Father', 'Both' UNION ALL
    SELECT 'Grandmother', 'Both' UNION ALL
    SELECT 'Grandfather', 'Both' UNION ALL
    SELECT 'Grandparent', 'Both' UNION ALL
    SELECT 'Aunt', 'Both' UNION ALL
    SELECT 'Uncle', 'Both' UNION ALL
    SELECT 'Brother', 'Both' UNION ALL
    SELECT 'Sister', 'Both' UNION ALL
    SELECT 'Sibling', 'Both' UNION ALL
    SELECT 'Cousin', 'Both' UNION ALL
    SELECT 'Guardian', 'Both' UNION ALL
    SELECT 'Relative', 'Both' UNION ALL
    SELECT 'Attending Physician', 'Both' UNION ALL
    SELECT 'Attending Midwife', 'Birth' UNION ALL
    SELECT 'Attending Nurse', 'Birth' UNION ALL
    SELECT 'Hospital Administrator', 'Both' UNION ALL
    SELECT 'Clinic Administrator', 'Birth' UNION ALL
    SELECT 'Spouse', 'Death' UNION ALL
    SELECT 'Husband', 'Death' UNION ALL
    SELECT 'Wife', 'Death' UNION ALL
    SELECT 'Son', 'Death' UNION ALL
    SELECT 'Daughter', 'Death' UNION ALL
    SELECT 'Son-in-law', 'Death' UNION ALL
    SELECT 'Daughter-in-law', 'Death' UNION ALL
    SELECT 'Brother-in-law', 'Death' UNION ALL
    SELECT 'Sister-in-law', 'Death' UNION ALL
    SELECT 'Grandchild', 'Death' UNION ALL
    SELECT 'Nephew', 'Death' UNION ALL
    SELECT 'Niece', 'Death' UNION ALL
    SELECT 'Funeral Director', 'Death' UNION ALL
    SELECT 'Others', 'Both'
) AS want
WHERE NOT EXISTS (
    SELECT 1 FROM `relationships` r WHERE r.`name` = want.n
);

-- Scope the entries that were already there.
UPDATE `relationships` SET `applies_to` = 'Death'
 WHERE `name` IN ('Wife', 'Husband', 'Spouse', 'Son', 'Daughter', 'Grandchild',
                  'Nephew', 'Niece', 'Funeral Director');
UPDATE `relationships` SET `applies_to` = 'Birth'
 WHERE `name` IN ('Attending Midwife', 'Attending Nurse', 'Clinic Administrator');
UPDATE `relationships` SET `applies_to` = 'Both'
 WHERE `name` IN ('Mother', 'Father', 'Grandparent', 'Grandmother', 'Grandfather',
                  'Aunt', 'Uncle', 'Brother', 'Sister', 'Sibling', 'Cousin',
                  'Guardian', 'Relative', 'Attending Physician',
                  'Hospital Administrator', 'Administrator', 'Others');
-- Cannot be true of either certificate; see the note above on why the row survives.
UPDATE `relationships` SET `applies_to` = 'None' WHERE `name` = 'Self';
