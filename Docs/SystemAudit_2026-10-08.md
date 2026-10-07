# CROMS system audit - 2026-10-08

Scope: every sidebar module and Settings page of CROMS, CROMS.Kiosk, CROMS.Display, the phone side
(save-API, claimapp, ORCMobile), the kiosk-to-release flow, and Admin / Staff gating.
Method: ran the existing suites, wrote a new whole-system harness (`CROMS.MarriageTest --audit <dir>`),
drove the real form classes against the live `croms` DB with ZZ-tagged data, rendered every module at two
sizes and LOOKED at the PNGs, tested the live save-API over HTTP and HTTPS. Nothing was printed (print dialogs
are only ever cancelled).

## Totals

| Suite | Result |
|---|---|
| Marriage workflow (default) | 77 / 77 |
| `--form90` / `--parentres` / `--mf90` / `--consentadvice` / `--delayedbirth` | 16 / 18 / 15 / 8 / 15, all pass |
| `--fees` / `--breqs` / `--birthtest` | 57 / 44 / 50, all pass |
| `--flows` (13 kiosk services -> queue -> payment -> release) | 116 / 116 |
| OCR ground truth (`DocTest --truth`, 7 scans) | 113 / 184 fields (61%), 36 wrong - unchanged baseline |
| NEW `--audit` (modules, layout x2, roles, operations, kiosk, display) | 274 pass, 8 fail (7 cosmetic layout, 1 policy gap - see below) |
| Phone save-API, live (`apitest.py`, 39 checks) | 39 / 39 |
| `ng build` claimapp / ORCMobile | clean / clean |
| MSBuild CROMS.sln | 0 errors, 0 CS warnings |

Row counts of queue_tickets, transactions, payments, releases, claim_requests, births, marriages, deaths,
audit_log (count and id sum), certificate_requests, petitions, breqs_requests, marriage_licenses, ocr_batch,
windows, users are **identical before and after** (verified after the final run).

## Module status

| Module | Status | Evidence |
|---|---|---|
| Dashboard | PASS | opens, refresh clean, waiting-now figure agrees with DB, layout @1920/1366 |
| Queue Management | PASS (1 fixed) | flows 116/116; Claim chip fell off the row at 1366 - fixed |
| Transaction History | PASS | list = DB count, search, status filter agree with DB |
| Certificate Request | PASS | flows + empty submit writes nothing |
| PSA Copies (BREQS) | PASS | 44/44 incl. real OCR on a sample copy |
| Release & Claim | PASS | flows (rep without ID refused, verify dialog, release, finish) |
| Fees & Payments | PASS | 57/57 |
| Birth Registration | PASS | 50/50, all 8 steps rendered |
| Marriage (desk, Form 90, Form 97, delayed) | PASS (2 fixed) | 77+16+18+15+8+15; header buttons wrapped over KPI row at 1366 and `&` lost in tab - fixed |
| Death Registration | PASS (1 fixed) | register / refuse / reload / update; two-word surname was mangled on reload - fixed; 5 steps rendered |
| Petitions & Case Tracking | PASS | 20 checks: 6 types saved, stage sequences, slips, case documents (1 / 5), fee-vs-acknowledgment, edit, delete |
| Records Archive | PASS | 16 categories open, search (type filter, SOUNDEX), View Certificate preview |
| Document Processing (OCR) | PASS (1 fixed) | real scan -> Birth 65 fields -> Commit -> record + batch + field audit -> 2nd Commit refused -> Preview on Form |
| Reports & Analytics | PASS | all 7 tabs load |
| Settings | PASS (1 fixed) | 8 pages open; General could not scroll at 1366 - fixed; admin pages ask for verification |
| Users & Access / sign-in | PASS | 18 checks: validation, hashing, duplicate, last-admin protection, lock-out after 5, deactivated, forced password change |
| Master Files | PASS (1 fixed) | 15 categories list; add/rename/delete; in-use value protected; duplicates were allowed - fixed |
| Certificate previews | PASS | Birth / Marriage / Death via Crystal, Form 3A/3B/3C, nothing printed |
| Templates | PASS (read) | 12 templates load with elements |
| Kiosk (13 services, 11 refusals, lanes) | PASS | flows; 9 step screens rendered with an online window |
| Display board | PASS | renders, lists active windows |
| Save-API | PASS | births/deaths/marriages/scans/claims/letter/form97 capture incl. expired token, malformed JSON |
| claimapp / ORCMobile | PASS (build) | compile only; camera NOT TESTED |
| Roles | PASS | Admin full; Staff / unknown get operational set only; door refuses Settings/Users/Master Files/Templates; Staff bypass needs reason + audited with role; unknown role refused |

## Defects found

| # | Severity | Where | Defect | Status |
|---|---|---|---|---|
| 1 | Medium | `OcrDigitizationForm.cs` Analyze | OCR read failed with "Object is currently in use elsewhere": engine read the same Bitmap the picture box paints | FIXED fafa8c0 |
| 2 | Medium | `DeathRegistrationForm.cs` | Saved only full_name; reload split by word count so "Dela Cruz" went into Middle Name | FIXED ab56dea |
| 3 | Medium | `Database/52_petition_documents.sql` | Aborted: legal_basis longer than varchar(120) (same as 45 earlier) | FIXED a2b1301, applied |
| 4 | Medium | live DB | Migrations 48 (licence requirement override), 48 (office e-mail), 52 (petition documents) were never applied: licence override UPDATE failed with 1054, Case Documents empty | APPLIED (idempotent, run twice) |
| 5 | Medium | `SettingsForm.cs` General page | No scrollbar: Mobile Capture buttons unreachable at 1366x768 | FIXED |
| 6 | Low-Med | `MasterFilesForm.cs` | Same value could be added twice to a pick-list; DB errors shown raw | FIXED 3c9d608 |
| 7 | Low | `MainForm.Designer.cs`, `ReportsAnalyticsForm.cs`, `MarriageRegistrationForm*` | `&` eaten as mnemonic: header "Fees  Payments", tab "Fees _Collections", "APPLICATIONS _LICENSES" | FIXED 009d906, 70bcbe5 |
| 8 | Low | `MarriageRegistrationForm.Designer.cs` | 50/50 header split wrapped PSA Transmittal over the KPI row at 1366 | FIXED 70bcbe5 |
| 9 | Low | `QueueManagementForm.Designer.cs` | Claim filter chip off the row at 1366 | FIXED |
| 10 | Low | `RecordsArchiveForm.Designer.cs` | Result caption ran under Refresh / off the window | FIXED |

### Not fixed (reported)

| # | Severity | Finding | Why not fixed |
|---|---|---|---|
| A | Medium | **Soft delete is half-built.** Migration 69 (is_deleted/delete_reason) is not applied and `ReasonPrompt` is used nowhere. Births (`BirthRegistrationForm.cs:1795`), digitized Birth/Marriage/Death records, windows, petitions and template images still HARD-DELETE with no reason. | Needs a decision + query changes in every list (filter is_deleted); a feature, not a bug fix |
| B | Medium | Document Processing Commit accepts a digitized record with **no registry number** (NULL; the unique index allows many NULLs), so the same certificate can be digitized twice undetected. Registry number is not in the required list (handwritten on scans). | Office policy decision |
| C | Low | 41 places show `ex.Message` raw in 29 forms (e.g. Users, Login "Cannot reach the database: ..."). Birth, Death, Master Files, Kiosk use the friendly `ErrorLog.Report`. | Mechanical, wide; do as one pass |
| D | Low | OCR screen is an absolute layout: at 1366 the buttons overlap the subtitle and captions truncate ("Zo...", "Commit to Regis..."). Functions work. | Needs the layout rebuild the other modules got |
| E | Low | Reports > Birth cards row overflows its panel at 1366 (Customize tile cut). Records Archive tree node captions clip ("Certification & PSA Copie"). Fees caption labels overlap their boxes by 6 px; Petitions requester TextBox/label overlap flagged by the sweep (looked fine). | Cosmetic |
| F | Info | Kiosk forms ignore window size (full-screen), so the 1366x768 kiosk was rendered at this PC's resolution only. | Needs a real 1366 screen |

## Migrations

All migration files 01-82 compared with the live DB (tables, columns, views): all applied **except 69** (left unapplied on purpose, finding A).
48 (x2) and 52 were unapplied at the start of this audit and are now applied. 54 and 58 (listed pending in older notes) are applied.

## Not testable here (needs hardware / another machine)

Printing on paper and MF-102/97/103 alignment (TODO already on file); phone camera, QR scan and claimapp/ORCMobile on a real phone
(only the API and builds tested); real touchscreen kiosk; second PC over the LAN (publish/update share, AutoConnect); DuckDNS certificate issuance
(needs Internet; link logic and existing config checked); Crystal viewer print button; Windows Settings > App Updates publish (changes shares - not run);
admin-verified Settings pages beyond opening (Window Management add/edit dialog, Audit Trail export).

## Ranked fix list

1. Decide soft-delete (A): apply 69 and wire `ReasonPrompt` to every record delete, or remove the dead pieces.
2. Decide registry-number rule for digitized commits (B).
3. Replace raw `ex.Message` dialogs with `ErrorLog.Report` (C).
4. Rebuild OCR screen layout like the other modules (D).
5. Cosmetic 1366 items (E); real 1366 kiosk check (F).

## Reproduce

`CROMS.MarriageTest.exe --audit <dir>` (about 10 minutes; `AUDIT_ONLY=<text>` runs only matching operation checks and skips the sweeps;
run it in the background - a 10-minute tool timeout kills it before cleanup). Cleanup deletes only ZZA-tagged rows and audit rows above the baseline id.
PNG renders land in `<dir>`; `audit_log.txt` has the full pass/fail list.
