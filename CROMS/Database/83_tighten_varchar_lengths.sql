-- 83_tighten_varchar_lengths.sql
-- Right-sizes the address-part VARCHAR columns that were declared far wider than any
-- Philippine place name needs:
--   *_province ................ 100/120 -> 60   (longest official name is ~21 chars)
--   *_municipality / event_city  120     -> 80
--   *_barangay ................ 100/120 -> 80
--   *_house (house no./street)  120/150 -> 100
-- SAFE: a column is only shrunk when it is wider than the target AND the longest value
-- stored in it fits the target, so no existing data is ever cut. A column holding a longer
-- value is left alone. NULL-ability, default, character set and collation are preserved.
-- Idempotent: a second run finds nothing wider than the target and does nothing.
-- Passwords are NOT touched here: users.password_hash stays VARCHAR(255) (it stores a
-- 76-char PBKDF2 hash, not the typed password). The 16-character password limit is enforced
-- in the screens (MaxLength on the password boxes).
-- ASCII only (applied by piping into the mysql client).

DROP PROCEDURE IF EXISTS _croms_tighten_varchar;
DELIMITER //
CREATE PROCEDURE _croms_tighten_varchar()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE t VARCHAR(64);
  DECLARE c VARCHAR(64);
  DECLARE target INT;
  DECLARE nullable VARCHAR(3);
  DECLARE dflt TEXT;
  DECLARE cs VARCHAR(64);
  DECLARE co VARCHAR(64);
  DECLARE mx INT;
  DECLARE cur CURSOR FOR
    SELECT col.TABLE_NAME, col.COLUMN_NAME,
           CASE WHEN col.COLUMN_NAME REGEXP 'province$'                          THEN 60
                WHEN col.COLUMN_NAME REGEXP 'municipality$' OR col.COLUMN_NAME = 'event_city' THEN 80
                WHEN col.COLUMN_NAME REGEXP 'barangay$'                          THEN 80
                WHEN col.COLUMN_NAME REGEXP '_house$'                            THEN 100
           END AS target,
           col.IS_NULLABLE, col.COLUMN_DEFAULT, col.CHARACTER_SET_NAME, col.COLLATION_NAME
    FROM information_schema.COLUMNS col
    JOIN information_schema.TABLES tb
      ON tb.TABLE_SCHEMA = col.TABLE_SCHEMA AND tb.TABLE_NAME = col.TABLE_NAME
    WHERE col.TABLE_SCHEMA = DATABASE()
      AND tb.TABLE_TYPE = 'BASE TABLE'
      AND col.DATA_TYPE = 'varchar'
      AND col.TABLE_NAME <> 'office_profile'
      AND (col.COLUMN_NAME REGEXP 'province$'
        OR col.COLUMN_NAME REGEXP 'municipality$'
        OR col.COLUMN_NAME = 'event_city'
        OR col.COLUMN_NAME REGEXP 'barangay$'
        OR col.COLUMN_NAME REGEXP '_house$')
      AND col.CHARACTER_MAXIMUM_LENGTH >
          CASE WHEN col.COLUMN_NAME REGEXP 'province$' THEN 60
               WHEN col.COLUMN_NAME REGEXP 'municipality$' OR col.COLUMN_NAME = 'event_city' THEN 80
               WHEN col.COLUMN_NAME REGEXP 'barangay$' THEN 80
               ELSE 100 END;
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN cur;
  loop1: LOOP
    FETCH cur INTO t, c, target, nullable, dflt, cs, co;
    IF done = 1 THEN LEAVE loop1; END IF;

    SET @q = CONCAT('SELECT COALESCE(MAX(CHAR_LENGTH(`', c, '`)),0) INTO @mx FROM `', t, '`');
    PREPARE s FROM @q; EXECUTE s; DEALLOCATE PREPARE s;
    SET mx = @mx;

    IF mx <= target THEN
      SET @ddl = CONCAT('ALTER TABLE `', t, '` MODIFY `', c, '` VARCHAR(', target, ')',
                        IF(cs IS NULL, '', CONCAT(' CHARACTER SET ', cs)),
                        IF(co IS NULL, '', CONCAT(' COLLATE ', co)),
                        IF(nullable = 'NO', ' NOT NULL', ' NULL'),
                        IF(dflt IS NULL, '', CONCAT(' DEFAULT ', QUOTE(dflt))));
      PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
    END IF;
  END LOOP;
  CLOSE cur;
END//
DELIMITER ;

CALL _croms_tighten_varchar();
DROP PROCEDURE IF EXISTS _croms_tighten_varchar;
