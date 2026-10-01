-- 80_ctc_relationship_width.sql
-- Kiosk CTC request: "Relationship to Record Owner" now has an Other choice with a specify box, which
-- is stored as "Other - <what the client typed>". The old VARCHAR(40) is too short for that, so it
-- becomes VARCHAR(80), the same width as `purpose`, which already stores "Others - <detail>".
-- Widening only: no existing value is changed. Idempotent (MODIFY to the same type is a no-op).

ALTER TABLE `ctc_requests` MODIFY COLUMN `relationship` VARCHAR(80) NULL;
