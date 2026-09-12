-- 39_confirm_marriage_age_bands.sql
-- The two Family Code age bands, CONFIRMED by the Penablanca LCRO on 2026-09-13.
--
-- Migration 33 seeded both bands with "CONFIRM WITH LCRO", reading "between twenty-one and
-- twenty-five" (Art. 15) as 21-24. The office answered: parental ADVICE applies to 21-25, so a
-- 25-year-old applicant needs it. The CONSENT band stays 18-20: the office's own consent form
-- (Municipal Form No. 06) describes the applicant as "less than (twenty-one) years of age".
--
-- Only these two rows are touched. ASCII only (applied by piping into the mysql client).
-- Idempotent: re-running sets the same values.

UPDATE `app_settings`
   SET `setting_value` = '25',
       `description`   = 'FC Art. 15 parental advice band, upper bound (inclusive). 21-25 CONFIRMED by the LCRO 2026-09-13.'
 WHERE `setting_key` = 'MARRIAGE_ADVICE_AGE_TO';

UPDATE `app_settings`
   SET `description`   = 'FC Art. 14 parental consent band, upper bound (inclusive). 18-20 CONFIRMED by the office consent form ("less than twenty-one").'
 WHERE `setting_key` = 'MARRIAGE_CONSENT_AGE_TO';
