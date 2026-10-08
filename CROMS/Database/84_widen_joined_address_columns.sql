-- 84_widen_joined_address_columns.sql
-- A few address columns do not hold one typed value, they hold the cells of an address joined
-- into one string ("house, barangay, municipality, province"; parents' marriage "church,
-- municipality, province"). The cells are limited to 100 / 80 / 80 / 60 (migration 83), so the
-- joined text can reach ~324 characters - more than the 200 these columns were declared with, and
-- the save would fail with "Data too long". Widened to 400. Widening never cuts data.
-- Scope: VARCHAR(200) columns in births / marriages / marriage_licenses whose name ends in
-- _residence or _address, plus births.parents_marriage_place. Death keeps its single-box
-- addresses at 200 (the screen caps them at 200).
-- NULL-ability, default, charset and collation preserved. Idempotent. ASCII only.

DROP PROCEDURE IF EXISTS _croms_widen_joined_addr;
DELIMITER //
CREATE PROCEDURE _croms_widen_joined_addr()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE t VARCHAR(64);
  DECLARE c VARCHAR(64);
  DECLARE nullable VARCHAR(3);
  DECLARE dflt TEXT;
  DECLARE cs VARCHAR(64);
  DECLARE co VARCHAR(64);
  DECLARE cur CURSOR FOR
    SELECT col.TABLE_NAME, col.COLUMN_NAME, col.IS_NULLABLE, col.COLUMN_DEFAULT,
           col.CHARACTER_SET_NAME, col.COLLATION_NAME
    FROM information_schema.COLUMNS col
    JOIN information_schema.TABLES tb
      ON tb.TABLE_SCHEMA = col.TABLE_SCHEMA AND tb.TABLE_NAME = col.TABLE_NAME
    WHERE col.TABLE_SCHEMA = DATABASE()
      AND tb.TABLE_TYPE = 'BASE TABLE'
      AND col.TABLE_NAME IN ('births', 'marriages', 'marriage_licenses')
      AND col.DATA_TYPE = 'varchar'
      AND col.CHARACTER_MAXIMUM_LENGTH = 200
      AND (col.COLUMN_NAME REGEXP '_residence$' OR col.COLUMN_NAME REGEXP '_address$'
           OR col.COLUMN_NAME = 'parents_marriage_place');
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN cur;
  loop1: LOOP
    FETCH cur INTO t, c, nullable, dflt, cs, co;
    IF done = 1 THEN LEAVE loop1; END IF;
    SET @ddl = CONCAT('ALTER TABLE `', t, '` MODIFY `', c, '` VARCHAR(400)',
                      IF(cs IS NULL, '', CONCAT(' CHARACTER SET ', cs)),
                      IF(co IS NULL, '', CONCAT(' COLLATE ', co)),
                      IF(nullable = 'NO', ' NOT NULL', ' NULL'),
                      IF(dflt IS NULL, '', CONCAT(' DEFAULT ', QUOTE(dflt))));
    PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
  END LOOP;
  CLOSE cur;
END//
DELIMITER ;

CALL _croms_widen_joined_addr();
DROP PROCEDURE IF EXISTS _croms_widen_joined_addr;
