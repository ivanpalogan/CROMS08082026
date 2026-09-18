-- 52_petition_documents.sql
-- Petitions & Case Tracking (RA 9048/10172 plus the four track-only types from migration 44)
-- had NO document upload/attachment at all - checked before building this: `petitions` has no
-- scan_image column and PetitionsForm never touched marriage_requirements. Asked directly about
-- Court Order specifically ("monitor court orders and upload related documents, like petitions
-- do") - answer was petitions don't have this either today, so this migration gives the WHOLE
-- Petitions module the same generic requirements-checklist-with-attachment engine the marriage
-- licence, delayed birth registration and out-of-province licence flag already use, rather than
-- inventing a second document-tracking mechanism for one case type.
--
-- REUSED, NOT REBUILT: `marriage_requirement_types` / `marriage_requirements` are already
-- generic on owner_type/owner_id (owner_type='License'/'Marriage'/'Birth' so far) and
-- MarriageService.Requirements/SaveRequirement/SyncRequirements take the owner type as a plain
-- string parameter - no code change needed there. This migration only adds applies_to='Petition'
-- catalog rows; owner_type='Petition' with owner_id=petitions.id is a new value of an existing
-- column, not a new table.
--
-- TWO RULE_KEY SHAPES, same convention as the marriage licence's ConsentAge/PreviouslyMarried
-- and the delayed-birth RegistrantDeceased/ParentsMarried: 'Always' applies to every petition
-- type filed through this tracker (a single generic "supporting document" slot, since RA 9048/
-- RA 10172/Legitimation/Supplemental Report/Legal Instrument all lack an office-confirmed
-- checklist of their own - see the standing backlog PENDING items), and a rule_key equal to the
-- petition_type code (here, 'CourtOrder') applies only to that one type. Adding a real checklist
-- for another type later is one more INSERT with its own rule_key, not a schema change.
--
-- COURT ORDER DOCUMENTS are Rule 108 (Cancellation or Correction of Entries in the Civil
-- Registry) plus this project's own 2026-09-13 research note on annotation practice: the
-- winning party files the court's decision, its Certificate of Finality and its Entry of
-- Judgment at the LCRO, which then annotates the civil registry record and endorses to PSA. The
-- certified copy and the Certificate of Finality are BLOCKING (the LCRO cannot verify or act on
-- an order it cannot confirm is final); Entry of Judgment and the requesting party's letter are
-- kept as informational slots (their absence should not itself hold up an otherwise-verified
-- court order, and this project does not invent a stricter local rule the office never stated).
--
-- ASCII only (piped into the mysql client - the "never put a non-ASCII literal in a migration
-- applied by piping" rule from 2026-09-06 applies here too). Idempotent - safe to run twice.

INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'CASE_SUPPORTING_DOC',
       'Supporting document for this case',
       'Petition', 'Always', 0, 0,
       'General attachment slot for any petition/case type tracked here - no office-specific checklist has been confirmed for RA 9048, RA 10172, Legitimation, Supplemental Report or Legal Instrument yet (see the standing backlog PENDING items).',
       1, 10
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'CASE_SUPPORTING_DOC' AND applies_to = 'Petition'
);

INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'COURT_ORDER_CERTIFIED_COPY',
       'Certified true copy of the court decision/order',
       'Petition', 'CourtOrder', 0, 1,
       'Rule 108 annotation practice - the LCRO annotates only against a certified copy of the actual decision.',
       1, 20
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'COURT_ORDER_CERTIFIED_COPY' AND applies_to = 'Petition'
);

INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'COURT_ORDER_FINALITY',
       'Certificate of Finality',
       'Petition', 'CourtOrder', 0, 1,
       'The decision must be FINAL before the LCRO annotates or endorses to PSA - the certificate is how that is confirmed.',
       1, 21
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'COURT_ORDER_FINALITY' AND applies_to = 'Petition'
);

INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'COURT_ORDER_ENTRY_JUDGMENT',
       'Entry of Judgment',
       'Petition', 'CourtOrder', 0, 0,
       'Standard supporting document in Rule 108 annotation packets; kept informational since not every court issues one separately from the Certificate of Finality.',
       1, 22
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'COURT_ORDER_ENTRY_JUDGMENT' AND applies_to = 'Petition'
);

INSERT INTO `marriage_requirement_types`
    (code, label, applies_to, rule_key, per_party, blocking, legal_basis, is_active, sort_order)
SELECT 'COURT_ORDER_REQUEST_LETTER',
       'Requesting party''s letter / endorsement to the LCRO',
       'Petition', 'CourtOrder', 0, 0,
       'Identifies who is asking the LCRO to annotate and why; kept informational, not a statutory requirement of the order itself.',
       1, 23
WHERE NOT EXISTS (
    SELECT 1 FROM `marriage_requirement_types` WHERE code = 'COURT_ORDER_REQUEST_LETTER' AND applies_to = 'Petition'
);
