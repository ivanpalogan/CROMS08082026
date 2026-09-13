-- 44_case_tracking_types.sql
-- Widens `petitions` — already a generic Filed -> ... -> PSA_Endorsement tracker — to cover the
-- four Phase 4 case types the office does NOT need CROMS to perform: Legitimation (RA 9858),
-- Supplemental Report, Legal Instruments (RA 9255 Affidavit of Acknowledgment / Admission of
-- Paternity / AUSF), and Court Order annotation (Rule 108 corrections, annulment/adoption decrees,
-- etc). These are TRACK-ONLY, per the office's own split recorded in the backlog: CROMS ASSISTS
-- some services (marriage licence, delayed birth registration — full requirements engine, posting
-- clock) and CROMS RECORDS AND TRACKS others. For these four, the LCRO performs the actual legal
-- procedure end to end; CROMS only keeps a status the office and PSA endorsement can be checked
-- against, so no requirements checklist or posting-period engine was built for them.
--
-- One table, not four, per the backlog's own stated preference ("14. Build order" — "one generic
-- case-tracking table ... IF the stages turn out similar"). Researched before deciding they do:
-- RA 9858 legitimation, PSA's own supplemental-report rule, RA 9255 legal-instrument registration,
-- and Rule 108 / final-decision court annotation all follow the same shape — filed, reviewed by
-- the LCR, registered/annotated on the record, then endorsed to PSA. RA 9048/10172 correction
-- petitions keep their own 'Posted' step (a real 15-day statutory public-posting requirement);
-- none of the four new types carry a posting period, so they get 'UnderReview' in its place —
-- Filed -> Under Review -> Decision -> PSA Endorsement.
--
-- Idempotent: MODIFY COLUMN on an already-widened enum is a no-op re-apply, not an error.

ALTER TABLE petitions MODIFY COLUMN petition_type
  ENUM('RA9048','RA10172','Legitimation','SupplementalReport','LegalInstrument','CourtOrder')
  NOT NULL;

ALTER TABLE petitions MODIFY COLUMN stage
  ENUM('Filed','Posted','UnderReview','Decision','PSA_Endorsement')
  NOT NULL DEFAULT 'Filed';
