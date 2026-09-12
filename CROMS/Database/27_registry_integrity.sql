-- 27_registry_integrity.sql
-- Two integrity fixes on the registry tables: the registry number can no longer
-- be duplicated, and a delayed registration is derived from dates instead of
-- being remembered by hand.
--
--   1. UNIQUE REGISTRY NUMBER. `registry_no` is the record's legal key — it is what
--      a certificate is reprinted by, what PSA is given, and what a client quotes at
--      the counter. It carried a plain (non-unique) index from 26_form_identity, so
--      two rows could hold the same number and nothing would object. That is not
--      hypothetical: all three forms generate the next number with
--      MAX(...) + 1, read in one statement and written in another, so two
--      registrars saving in the same moment both read the same MAX and both write
--      the same number. A UNIQUE index makes the second write fail instead of
--      succeed, and the forms now catch that failure and take the next number
--      (see the 1062 retry in BirthRegistrationForm / DeathRegistrationForm /
--      MarriageEntryForm). Constraint plus retry closes the race; either alone
--      does not.
--
--      Drafts are unnumbered by design and store NULL, and MySQL allows any number
--      of NULLs in a UNIQUE index, so this constrains only real registry numbers.
--      Verified before applying: no duplicates exist in the live database, and
--      every blank is NULL rather than an empty string (an empty string WOULD
--      collide, since '' is a value).
--
--   2. `births.date_registered` — THE DATE THE EVENT WAS REGISTERED, which the
--      database could not previously state. RA 3753 gives 30 days to register a
--      birth; past that it is a delayed registration needing extra affidavits, and
--      the monthly PSA report has to split the two. That split was read off
--      `births.is_delayed`, a checkbox the registrar had to remember to tick — and
--      on this database it is 0 on every row while the dates say 16 of 18 are more
--      than 30 days late. The PSA report was therefore claiming every registration
--      was timely.
--
--      The obvious repair — derive it from `created_at` — is wrong here, and the
--      reason is worth stating because it will come up again. `created_at` is when
--      the ROW was made, which for a record typed in by a registrar is the
--      registration date, but for the OCR backlog is the DIGITIZATION date. Scanning
--      a 2018 certificate in 2026 is not a late registration; that birth was
--      registered in 2018, on paper, and CROMS is only copying it in. Deriving from
--      `created_at` would mark the entire digitized backlog "delayed" and inflate
--      the delayed count in every PSA report from here on.
--
--      So the registration date gets its own column, written only when a
--      registration actually happens. Existing rows are deliberately left NULL: we
--      cannot tell retroactively which were true registrations and which were
--      migrated or digitized, and guessing either way would put a fabricated
--      statutory claim in a government report. NULL reads as "registered before
--      CROMS, or digitized from the books", and the PSA report counts those
--      separately instead of calling them timely.

-- ---------------------------------------------------------------------------

DROP PROCEDURE IF EXISTS _croms_registry_integrity;
DELIMITER //
CREATE PROCEDURE _croms_registry_integrity()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE t VARCHAR(64);
    DECLARE dupes INT DEFAULT 0;
    DECLARE cur CURSOR FOR
        SELECT 'births' UNION ALL SELECT 'marriages' UNION ALL SELECT 'deaths';
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    -- 1. registry_no: plain index -> UNIQUE, on all three registry tables.
    OPEN cur;
    uniq: LOOP
        FETCH cur INTO t;
        IF done = 1 THEN LEAVE uniq; END IF;

        -- An empty string is not a registry number, but UNIQUE treats it as a value,
        -- so two blanks stored as '' would collide where two NULLs do not. Normalise
        -- them to NULL first — that is what an unnumbered draft already stores.
        -- (LENGTH(...) > 0 rather than <> '' so no quote has to survive CONCAT.)
        SET @s = CONCAT('UPDATE `', t, '` SET registry_no = NULL ',
                        'WHERE registry_no IS NOT NULL AND LENGTH(TRIM(registry_no)) = 0');
        PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;

        -- Refuse rather than fail half-way if this database somehow already holds a
        -- duplicate: ALTER would abort with 1062 and the migration would stop with
        -- no explanation of which row is at fault.
        SET @q = CONCAT(
            'SELECT COUNT(*) INTO @dupe_count FROM (SELECT registry_no FROM `', t,
            '` WHERE registry_no IS NOT NULL',
            ' GROUP BY registry_no HAVING COUNT(*) > 1) d');
        PREPARE st FROM @q; EXECUTE st; DEALLOCATE PREPARE st;
        SET dupes = IFNULL(@dupe_count, 0);

        IF dupes > 0 THEN
            SET @msg = CONCAT('Cannot make ', t, '.registry_no UNIQUE: ', dupes,
                              ' duplicate number(s) already exist. Resolve them first ',
                              '(SELECT registry_no, COUNT(*) FROM ', t,
                              ' GROUP BY registry_no HAVING COUNT(*) > 1).');
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = @msg;
        END IF;

        -- The old non-unique index is redundant once a UNIQUE one exists on the same
        -- column — the UNIQUE index serves every lookup the plain one did.
        IF EXISTS (SELECT 1 FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = t
              AND INDEX_NAME = CONCAT('ix_', t, '_registry_no')) THEN
            SET @s = CONCAT('ALTER TABLE `', t, '` DROP INDEX `ix_', t, '_registry_no`');
            PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = t
              AND INDEX_NAME = CONCAT('ux_', t, '_registry_no')) THEN
            SET @s = CONCAT('ALTER TABLE `', t, '` ADD UNIQUE INDEX `ux_', t,
                            '_registry_no` (`registry_no`)');
            PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
        END IF;
    END LOOP;
    CLOSE cur;

    -- 2. The registration date, so "delayed" can be derived instead of remembered.
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND COLUMN_NAME = 'date_registered') THEN
        ALTER TABLE `births`
            ADD COLUMN `date_registered` DATE NULL COMMENT
            'Date the birth was registered at the LCRO. NULL = registered before CROMS or digitized from the registry books; never inferred from created_at, which for a scan is the digitization date.'
            AFTER `is_delayed`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'births'
          AND INDEX_NAME = 'ix_births_date_registered') THEN
        ALTER TABLE `births` ADD INDEX `ix_births_date_registered` (`date_registered`);
    END IF;
END //
DELIMITER ;

CALL _croms_registry_integrity();
DROP PROCEDURE IF EXISTS _croms_registry_integrity;
