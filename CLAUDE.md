CROMS — Civil Registry Operations Management System

Full name: Civil Registry Operations Management System Client: LGU Peñablanca, Cagayan — Local Civil Registry Office (LCRO) Type: Desktop application (Windows Forms / C# .NET Framework 4.8) Purpose: A day-to-day operations system for the LCRO — not just a digitization tool, but the everyday workbench that staff use to register events, serve clients, issue certifications, track petitions, and submit reports to PSA.



What it does (in one paragraph)

CROMS replaces the paper-and-spreadsheet workflow of the LCRO with a single integrated desktop app. Clients get a queue number at the front desk, the receiving officer logs the transaction, the registrar processes the record (birth, marriage, death, or certification request), the cashier assesses fees against the Peñablanca Revenue Code, and the releasing officer captures the claimant's signature — all tied to one transaction ID with a full audit trail. In parallel, the office digitizes decades of old registry books through the OCR module, and generates PSA monthly submission reports automatically from the same database.



The 15 modules

🟦 Client Services (front-desk workflow)

Module	What it does

Dashboard	Live KPIs — queue length, today's registrations, collections, pending releases.

Queue Management	Issues numbered tickets (BIRTH-###, CERT-###, etc.), calls next client, tracks wait time.

Transactions	Master ledger of every client interaction — one TXN ID follows the client from queue to release.

Certificate Request	Intake for CTC (Certified True Copy) of Birth/Marriage/Death and Negative Certifications.

Release \& Claim	Captures claimant signature (or authorized representative + ID) on release; closes the transaction.

🟩 Record Management (the registry itself)

Module	What it does

Birth Registration	Municipal Form 102 — full data entry for live-birth registration.

Marriage Registration	Municipal Form 97 — marriage certificate registration.

Death Registration	Municipal Form 103 — death certificate + burial permit.

Registry Books	Digital equivalent of the physical registry books, organized by year/volume/page.

Petitions (RA 9048 / 10172)	Tracks correction-of-entry and change-of-first-name petitions through their legal stages (Filed → Posted → Decision → PSA endorsement).

Record Search	Cross-record search with fuzzy / SOUNDEX matching (so "Dela Cruz" finds "dela cruz" and "de la Cruz").

🟨 Document Workflow

Module	What it does

Incoming / Outgoing	Document routing log — what came in, where it went, who signed for it.

OCR Digitization	Scans old registry pages, runs OCR, shows confidence levels, lets staff verify and commit to the database. This is how the backlog gets digitized without replacing the daily workflow.

🟧 Operations

Module	What it does

Fees \& Payments	Auto-assesses fees per the Peñablanca Revenue Code, reconciles Official Receipt numbers with the Treasurer, applies Senior/PWD discounts (RA 11261).

Reports \& PSA	Generates the PSA monthly submission report (BReN, MReN, DReN counts) and internal management reports.

🟥 Administration

Module	What it does

Users \& Audit Trail	User accounts, role-based access (Registrar / Staff / Cashier / Releasing), and a tamper-evident audit log of every create/update/delete.

Why it's designed this way

Realistic, not ambitious. It solves what LCRO staff actually do every day — queue, register, assess, collect, release, report — instead of chasing AI features they don't need.

Deployable. WinForms + .NET 4.8 + MySQL runs on any office PC the LGU already owns. No cloud dependency, no subscription.

Survives past digitization. Even after every old book is scanned, the app is still the daily operations system — that's the whole point.

PSA-compliant. Uses the official Municipal Form numbers (102/97/103) and produces the monthly report PSA expects.

Auditable. Every transaction has one ID, one owner, one signature trail — important for a government office.



---

Progress Log

2026-08-31 — Added "CROMS — Project Truth & Research Facts.md" at the repo root: the verified-from-code source of truth for the manuscript (OCR engine, reworked queueing, database, framework, reporting, deployment, feature list, and an explicit list of features that do NOT exist). Note the discrepancies it records: projects target .NET Framework 4.7.2 (not 4.8); Document AI is declared removed but is still registered, still on the sidebar, and still called by OCR Digitization; Registry Books and Incoming/Outgoing forms exist in the source tree but are not registered in ModuleRegistry, so they are unreachable in the running app.

2026-09-02 — OCR Digitization: document routing + database mapping fixed. The screen no longer treats every scan as a birth certificate. It now routes by the classified DocKind: birth scans are committed here, marriage (MF-97) and death (MF-103) scans are handed to MarriageEntryForm / DeathRegistrationForm via PrimeFromExtraction + SetScanImage (the modules that own those column mappings), and an Unclassified scan cannot be committed at all. ocr_batch now logs the real doc_class plus doc_kind, record_table and record_id, so a scan is traceable to the row it produced. Birth insert mapping corrected: parent names go to mother_/father_first_middle_last columns instead of one joined string in *_last_name; time_of_birth, type_of_birth, weight_grams, occupations, religion, citizenship, informant_name and the scan image (birth_image) are no longer dropped; commit/draft writes an audit_log row. New migration Database/24_ocr_routing.sql (idempotent) makes births.sex NULL-able — the NOT NULL column made every commit fail with "Column 'sex' cannot be null" when OCR could not read the tick box — and adds the ocr_batch routing columns. Not yet applied to the live croms DB; run 24 before using the screen. Recognition accuracy itself is untouched, pending de-identified sample birth/marriage/death scans with expected values.

2026-09-02 (later) — Merged the fixes a Codex pass made on top of the routing work (its tree at Documents\Codex\CROMS is a copy of this one taken at 15:52, so its migration 24 is byte-identical to ours; only its later edits were new). Taken: (1) the birth insert now also writes births.scan_image, not just birth_image — 17_scan_softcopy.sql added scan_image and that is the column BirthRegistrationForm and the softcopy viewer read back, so the scan was previously stored where nothing looks for it; (2) death routing now calls PrimeFromExtraction BEFORE SetScanImage, because PrimeFromExtraction runs ClearForm() which nulls the pending scan — the old order silently dropped the image; (3) btnCommit widened 194->264px (with btnReocr/btnDraft shifted left) so the runtime caption "Open in Marriage Registration" fits; (4) LoadBatch catches MySQL error 1054 and shows "OCR BATCH — RUN DATABASE MIGRATION 24_ocr_routing.sql" with commit/draft disabled, since migration 24 is still not applied; (5) explicit MySqlDbType.LongBlob on the image parameters, and commit/draft disabled after an OCR exception. Also fixed the same prime-then-attach ordering bug in DocumentAiForm for BOTH the birth and death paths (BirthRegistrationForm.PrimeFromExtraction clears too) — Codex had fixed only the OCR screen. NOT taken: its DocumentAI.cs recognition changes (PlaceAfter, FindTimeOfBirth, SafeInformant, quote stripping in Clean) — plausible but unverified with no sample scans, still waiting on de-identified samples with expected values; and its in-place edits to 01_schema.sql / 08_ocr.sql, which restate post-migration state in the base schema files against this repo's additive-migration convention. Build clean.

2026-09-02 (later still) — Migration 24_ocr_routing.sql applied to the live croms database (exit 0, no leftover _croms_ocr_routing procedure). Note for anyone reading the earlier entry: the pre-check showed it had ALREADY been applied before this run — births.sex was nullable and ocr_batch already had doc_kind/record_table/record_id, most likely applied during the Codex pass. The script is idempotent so re-running was a no-op. Verified end state: births.sex enum NULL-able, births.birth_image + births.scan_image both longblob, ocr_batch.doc_kind varchar(20) / record_table varchar(30) / record_id int. The OCR Digitization screen is now usable against the live DB — no more "Column 'sex' cannot be null" on a scan whose sex tick box could not be read, and the batch grid Saved To column resolves.

2026-09-02 (marriage samples) — OCR recognition + Municipal Form 97 extraction, tuned against the office's real scans. The user supplied three de-identified samples (Downloads\Marriage Cert.jpg 2786x3902, Birth Certificate.jpeg 2792x4032, Death Cert.png 606x755) with the documents themselves as ground truth, so this pass is measured, not guessed. Baseline before the work: marriage classified as a BIRTH certificate, both spouses given the same garbage name ("pene" ) PANOUO(tm)"), date of marriage wrong (2023-08-08 vs 18 Aug 2023), place of marriage and solemnizing officer filled with the form's own printed label and the "THIS IS TO CERTIFY: THAT BEFORE ME..." paragraph.

ROOT CAUSE 1 - recognition. OcrService.Preprocess used a single GLOBAL threshold (contrast stretch then cut at 150) after a 2x upscale, on GetPixel/SetPixel. On the marriage scan's tinted, unevenly lit security paper that erased the entire middle of the form - fields 2a through 17 came back BLANK. Replaced with grayscale + Bradley/Wellner ADAPTIVE thresholding over an integral image (each pixel compared against its own neighbourhood), via LockBits + Marshal.Copy. Same fix the mobile scanner already needed on 2026-07-29; the desktop had never received it. Side effect: a full-page pass went from ~22s to ~7s, because GetPixel/SetPixel cost more than the recognition did (closes tech-debt item M3 from the 2026-07-14 audit).

ROOT CAUSE 2 - resolution. There is no single best page size, and this was measured across 8 scales per sample: the birth certificate reads best near 2400px (above it the date of birth is lost), the marriage certificate needs FULL size (11/11 ground-truth tokens vs 8/11 at 2400), and the 755px death certificate is unreadable until scaled UP. DocumentAI.Analyze now runs the page at 2400px and again at native size (when the source is meaningfully larger) and keeps whichever extracted more fields, tie-broken on OCR confidence. Two passes still finish faster than the single old pass. OcrService.Run gained a targetLongSide overload; DocumentAI.LoadImage's cap went 2200 -> 4200 so the native pass has something to work with.

ROOT CAUSE 3 - the marriage extractor read flat text. MF-97 is a TWO-COLUMN table: the husband's and the wife's value for the same numbered field sit side by side on ONE printed line ("1. Name of (First) RYAN (First) TOFIE FAE"), so "the line after the HUSBAND heading" lands on the row carrying BOTH names and gives the two spouses identical values - which is exactly what the screen showed. Flat text cannot fix this, so OcrService now also returns every word with its bounding box (OcrWord/OcrResult.Words + PageWidth, read from Tesseract's ResultIterator). ExtractMarriage was rewritten around that: BuildRows groups words into printed rows by their vertical middle; MeasureColumns derives the label/husband/wife boundaries from the page's OWN "HUSBAND" and "WIFE" headings (self-calibrating, so a crop or a phone photo at an angle still splits correctly) and bounds the wife column at the table edge rather than the page edge - unbounded, it was gluing margin speckle onto her surname. Within a cell only ALL-CAPS words are kept, which drops the "(First)/(Middle)/(Last)" hint without having to recognise it (OCR renders those as "Frsty", "Jownse)", "(Las)"). Place of Marriage now reads the label's own row to the right of the label (its printed hint sits on the line BELOW the value, so the old "line after the label" returned the hint). Solemnizing Officer reads only the name column above its signature line and rejects certification boilerplate, position and court - a sentence sitting in a name field is worse than a blank one, because it looks filled in and gets saved as somebody's name.

Also fixed, all found by these samples: the child's "1. NAME" and father's "14. NAME" anchors were brittle (OCR printed them as "4. NAME" and "14, NAME", and each miss silently emptied a whole name) - the numbered anchor is now tried first, then the row's own "(First) (Middle) (Last)" heading, matched on the bracket pattern rather than the mangled words; the father's occupation was re-deriving that same strict anchor and went missing with it; FindDmy now accepts an ordinal day ("18th August 2023"), which is what made the date of marriage wrong; SplitNameCells cleans each cell separately and drops lone stray characters (a scan of "SHEILA ARTICULO BALOSO a" was read as surname "a"); Clean strips quote marks (ruled lines read as quotes and stuck to "SHEILA"); the informant is rejected when it looks like a label (it was returning "Tite oF ADMIN STRATIVE ADEM!"); and the death age regex is anchored to a standalone AGE label - without the word boundary it matched "aged" inside the printed maternal-condition caption and reported 40 for a 46-year-old.

MEASURED RESULT on the three samples (values checked field by field against the documents). Marriage: was classified Birth with ~10 wrong values, now Marriage 99% with 8 fields and every one correct - RYAN / MACANANG, TOFIE FAE / CADAVA / QUILANG, 2023-08-18, Filipino. Birth: 21 of 24 fields, all correct, unchanged in count from baseline but no longer emitting a junk informant. Death: 5 fields, all correct, no longer inventing an age. Registry numbers stay blank on all three because they are handwritten, and PlaceOfMarriage/Solemnizer stay blank on this scan because the printing there is genuinely destroyed - blank is the correct answer, not a placeholder.

NOT FIXED, and worth knowing: the husband's middle name reads "PANU" instead of "PANLILIO" - that is the recognition itself, not the mapping, and no amount of parsing recovers it. Handwriting is still not read anywhere. The death certificate's deceased name is still blank (pre-existing; that sample is only 606px and its name row does not survive). Build clean, 0 warnings / 0 errors - but bin\Debug could NOT be updated because CROMS.exe is running and VS holds it, so REBUILD IN VS to pick this up.

2026-09-02 (spec) — Wrote "CROMS - OCR Digitization and Document AI Spec.md" at the repo root: one unified, implementation-ready specification merging the OCR Digitization screen and the Document AI Auto-Fill screen (Purpose / Inputs / Workflow / Outputs / Validation / Error Handling, plus Privacy, Auditability and Known Limits). Derived only from the code as it stands — OcrService, DocumentAI, OcrDigitizationForm, DocumentAiForm, 08_ocr.sql and 24_ocr_routing.sql — with no invented behaviour; open decisions are marked [TBD]. Two things it records as real gaps rather than prose: the Document AI file dialog still offers *.pdf although LoadImage throws NotSupportedException for PDF, and the Document AI screen writes no ocr_batch row, so an auto-filled record has no audit trail until the target form is saved.

2026-09-02 (merge) — Document AI folded into OCR Digitization; it is no longer its own module. The two screens already shared one engine (DocumentAI.Analyze), so what was duplicated was the review surface, not the recognition. OcrDigitizationForm now has a real mode toggle: "Backlog Digitization" (birth boxes, Commit / Save as Draft into `births`, marriage + death routed to their own modules) and "Document AI Auto-Fill" (the editable field grid with the OK / warning / empty flags, then PrimeFromExtraction + SetScanImage into Birth / Marriage / Death registration). Everything the Document AI screen did came across: the grid, the engine-ready label, the async analyze with a progress bar, the sub-90% review prompt, and LearnFromExtraction feeding LearningLibrary from the operator-CORRECTED values. Removed the third mode button ("Endorsement Encoding") — it only ever set an unused string, and leaving dead buttons next to a working toggle is worse than removing them.

Fixed while merging, both gaps the spec had recorded: (1) an OCR run is now logged to ocr_batch in BOTH modes, and Auto-Fill marks the row "Auto-Filled" with the target table and a NULL record_id (batch grid shows "births (pending)") — before, an auto-filled record left no trace until the target form was saved; (2) the file dialog no longer offers *.pdf, which DocumentAI.LoadImage always rejected anyway. Also: the grid is filled in BOTH modes, so a correction to a field the birth boxes have no room for (weight, occupations, informant, religion) now reaches the INSERT, and switching Auto-Fill back to Digitize carries grid edits into the boxes instead of losing them.

Unwired everywhere: ModuleRegistry "docai" entry, the sidebar btnDocAI button in MainForm.Designer, "docai" in the Registrar role list, and the two Compile items in CROMS.csproj. Forms/DocumentAiForm.cs and .Designer.cs are DELETED (they had uncommitted changes, so copies were kept in the session scratchpad before removal). Build: MSBuild exit 0, no CS errors or warnings — only MSB3061/MSB3026 file-lock warnings because CROMS.exe is running and VS 2019 holds bin\Debug, so REBUILD IN VS to pick this up. Spec file updated to match.

2026-09-02 (unified) — Dropped the mode toggle; OCR Digitization is now ONE screen that does both jobs. The two "modes" were never two workflows: same load, same OCR run, same classification, same batch log — only the review surface differed. So the editable field grid is now the single review surface and all three actions sit under it: Re-OCR | Save as Draft | Commit to Birth Registry | Auto-Fill <module>. What the document IS decides which action is live: birth enables all of them, marriage/death enable Auto-Fill only (Commit/Draft write into `births`, so they stay birth-only), unclassified enables none and locks the grid.

Removed from the Designer: btnMode1, btnMode2 (the toggle) and the 10 birth label/textbox pairs (RegistryNo, Year, First, Middle, Last, Sex, DateOfBirth, Place, Mother, Father). Those boxes duplicated grid rows and were the source of the sync code — SplitName / JoinName / FillBoxes / SyncBoxesFromGrid / ReviewBoxes / ReviewLabels / ApplyMode / ScreenMode are all gone with them (~160 lines). SaveBirth now reads the grid directly by canonical key, which also removes a real failure mode: the old path re-split a joined "First Middle Last" box back into three columns and could mangle a two-word surname, while the grid keeps the three cells the extractor produced. book_volume (the office's registry-book year) is now taken from the parsed date of birth instead of a separate Year box.

Also: the sub-90% review prompt now fires for every document (it used to be Auto-Fill only), and the OCR engine status label moved to the space the toggle vacated. Note for VS users: after the Document AI merge, VS 2019 re-added the deleted DocumentAiForm Compile items to CROMS.csproj from its in-memory copy and the build failed with two CS2001 "source file could not be found" errors — the csproj was cleaned again; if it happens once more, delete the two ghost entries in Solution Explorer so VS writes the project file itself. Build after this change: MSBuild exit 0, no CS errors or warnings, bin\Debug updated (app was closed).

2026-09-02 (Intelligent Document Processing) — OCR Digitization upgraded into one intelligent
document workflow and renamed "Intelligent Document Processing" in the sidebar, the module
registry and the screen title. Pipeline is now: straighten, read at two resolutions, classify
from content AND layout, extract, score every field, correct obvious misreadings, validate
against the certificate's own rules, hand to the operator, act only on their confirmation.

ORIENTATION (new). Getting this right took three attempts and the failures are worth keeping:
a word-count probe turned a perfectly upright birth certificate 180 degrees and destroyed the
read (21 fields to 0); a real-words-times-confidence probe still turned it 90 degrees (22 to
16); an OSD-first rule fixed it. Tesseract's own detector (osd.traineddata, already installed
beside eng) was RIGHT every time it reported confidence >= 1.0 (5.9 upright, 2.2 at 90 deg,
1.1 upside-down) and WRONG the one time it was below (0.6, calling a sideways page upside
down). So: trust OSD at >= 1.0; below that fall back to a four-angle recognition probe for the
axis (needing a 1.2x margin over leaving the page alone), then let OSD settle the 180 flip on
the now-nearly-upright candidate. Measured: 12 of 12 orientations across the three samples
corrected, each rotated run producing the SAME field count as upright.

DESPECKLE (removed after measuring). An isolated-pixel filter looked obviously right for
"noise" and cost the birth certificate 5 fields (22 -> 17): PSA print is thin at the
resolution these pages are read at and the filter eats it. Removed, with the measurement
recorded in the code so nobody adds it back.

NEW Data/DocIntelligence.cs. Per-field confidence = mean Tesseract confidence of the words
that produced THAT value (found by matching the value's tokens back onto the page), scaled by
how much of it could be traced; a value matching no word was derived, not read, and scores 50
rather than a flattering 100. Context correction never invents: it reshapes characters already
read (O/0, I/1, S/5 inside numeric fields and the reverse inside names, accepted only if the
result has the field's expected shape), snaps a mangled month to the nearest real one within 2
edits, and snaps places/occupations onto spellings the office already uses via the Learning
Library. An empty field is NEVER filled. Validation covers dates (parseable, not future, not
pre-1900), names (charset), sex, registry-number format, weight 300-8000g, age 0-130, places
that are actually the form's printed text, and relationships: child's surname matching neither
parent, mother's maiden surname equal to the father's, husband and wife reading as the same
name (the classic two-column failure), age contradicting date of death. Verdicts are Ok /
Uncertain / Missing / Invalid / Conflict. Manual review triggers on Unknown, overall confidence
below 65%, or an Invalid CORE field - a junk reading in a non-core field (the handwritten
informant line) is flagged red but does not hold up a sound document.

CLASSIFICATION now adds layout evidence to the marker scores: MF-97's HUSBAND and WIFE
headings side by side on ONE printed row (+4), MF-102's maiden-name and birth-weight bands,
MF-103's cause-of-death and burial blocks. Markers are also matched against punctuation-
stripped text, so "Municipal Form No, 1O2" still hits. Below a score of 3 the answer is
Unknown. Confidence now rewards the MARGIN over the runner-up. Verified with a control: a
plain office memo image reads at 95% recognition and is correctly called Unknown, not filed as
a birth certificate.

SCREEN. Grid is Field | Value (editable) | Conf% | Check, with the Check column naming the
rule that was broken and the original OCR text when a value was auto-corrected. Selecting a
row DRAWS THAT FIELD'S BOX ON THE SCAN (per-field regions come from the OCR word boxes).
Editing a value re-runs the rules immediately - a typed value is taken at face value, scored
100, and can lift the manual-review hold without another OCR pass. Commit and Auto-Fill both
require a confirmation dialog that names the document, its confidence and every still-flagged
field, defaulting to No. New "Send to Manual Review" button. Deskew button now actually rotates
by 90 instead of apologising.

AUDIT. New migration 25_document_intelligence.sql (APPLIED to the live croms DB): table
ocr_field_audit (scan_id, field_key, ocr_value, final_value, confidence, status, issue,
auto_corrected, edited_by_user, action, username, created_at) plus ocr_batch columns
overall_confidence, needs_review, review_reason, rotation_applied, username. With
ocr_batch.raw_text that answers all four audit questions: what the machine saw, what it was
changed to and by whom, how sure it was, who accepted it and when. Data/OcrAudit.cs writes it.

TEST HARNESS. New project CROMS.DocTest (console, NOT in the .sln - build it with
msbuild CROMS.DocTest\CROMS.DocTest.csproj). Outputs into CROMS\bin\Debug on purpose so it
loads the same Tesseract and tessdata as the app. Flags: --rotate <deg> to exercise
orientation, --diag to print OSD orientation/confidence and the probe scores at all four
angles. Measured on the office's samples: Birth 22/24 fields at 87% field confidence (class
99%), Marriage 8/11 at 65%, Death 5/11 at 90%, all three classified correctly. The poor phone
scan from the user's screenshot (gilvan birth.jpg, 48% recognition) is now HELD for manual
review with "Child First Name contains characters a name cannot have" instead of being
auto-fillable. Remaining blanks are handwritten source (registry numbers, informant, time of
birth) or destroyed print - not dropped fields.

NOT DELIVERED, and it cannot be with this engine: HANDWRITING. Tesseract reads print. The
handwritten entries on these forms come back blank and are flagged for typing in. Registration
modules (Birth, Marriage, Death) were not touched. Build: MSBuild exit 0, no CS errors or
warnings, on both projects.

2026-09-04 — OCR + Document AI: REGION-BASED extraction merged in, measured against ground
truth. Values are no longer read out of the whole-page text. Everything the Intelligent
Document Processing pass added is KEPT — OSD orientation correction, DocIntelligence scoring
and validation, the ocr_field_audit trail — this sits underneath them as the new source of
field values.

WHY. Classification was already at 99% while extraction failed: the marriage samples produced
garbage spouse names, the 1993 birth scan produced almost nothing, the death scan produced no
deceased name at all. Two causes, both measured:

(1) Tesseract's page layout analysis silently DROPS entire typewriter rows inside these
bordered PSA tables. On the Certificate of Marriage neither spouse name nor either date of
birth appears anywhere in the page text — the rows are simply absent, not misread. The same
rows crop and read cleanly at PageSegMode.SingleLine. So values now come from per-field crops
of the ORIGINAL scan; page text is used only to identify the form and place its template.

(2) One layout cannot serve two form revisions. "gilvan birth.jpg" is MF-102 Revised JANUARY
1993 and "nice.jpg" is Revised JANUARY 2007 — different item numbers, tick boxes instead of
written sex, no father-residence row in 1993. The 1993 sheet was the "nearly all fields
missing" sample.

NEW Data/DocLayouts.cs — four form profiles with explicit normalised field rectangles:
MF-102 (2007), MF-102 (1993), MF-97 (1993), MF-103 (2016). Each region is read in several
preprocessings, plus the page pass's own words where it managed that row, and confidence is
the AGREEMENT between those readings rather than Tesseract's mean. A three-cell name row is
also read as one strip and split by word position, which is what recovers a name clipped by
its own cell edge ("Gilvan" was reading as "ilvan"). Cleaning rejects the form's printed
labels and their fragments ("cipality" out of "City/Municipality"), leading item numbers, and
— by word-box HEIGHT against the median — the ruled lines that used to win the vote
("ARTICU LO Ninraetinresin"). Dates normalise only when a named month fixes the order.
Citizenship and religion snap onto canonical spellings within one or two edits ("Pilip ino"
to Filipino, "Catholie" to Catholic); anything further away is left exactly as read. Nothing
is invented: a region with no acceptable reading stays EMPTY and is flagged.

CALIBRATION onto the actual photograph: PageFit matches printed labels and fits scale+offset
by CONSENSUS (RANSAC-style), not by averaging — one mismatched label dragged the template a
percent of page height, which slid every field onto the row below and made marriage WORSE on
the first attempt. Where the anchors sit in the page header and agree while the TABLE has
moved, the form's own printed horizontal RULES lock the row grid (OcrSession.Rulings(),
detected at reduced resolution so page skew does not smear them).

OcrService gained OcrSession: one Tesseract engine per page (building one per region turned a
seconds-long page into minutes), whole-page Auto and SparseText passes, ReadRegion with
multiple renderings, and Rulings(). The existing adaptive threshold was PARAMETERISED rather
than duplicated — Render(binarize, bias, window, stretch) — so a field crop can use a tighter
window than a full page.

Also fixed a reading-order bug that changed a person's name: words were bucketed by
MidY/height, which split one printed line across two buckets and reversed it, so "DE GUZMAN"
came back as "GUZMAN DE".

DocIntelligence.Enrich now KEEPS a region read's own confidence instead of overwriting it with
a page-text match — scoring a region value by whether it appears in the page text would punish
it for exactly the failure region reading exists to work around. A page match still adds a
small bonus (a second, independent read agreeing), and a field the page could not locate falls
back to the region it was read from for the scan highlight.

MEASURED, field by field, against ground truth transcribed from the five documents
(CROMS.DocTest.exe --truth; new CROMS.DocTest/Truth.cs; report saved as
CROMS.DocTest/accuracy_report.txt):
  nice.jpg           Birth MF-102 (2007)   27/30 = 90%   (was: date/place/parents missing)
  gilvan birth.jpg   Birth MF-102 (1993)   10/23 = 43%   (was: nearly all fields missing)
  palogan_n.jpg      Death MF-103 (2016)   20/20 = 100%  (was: deceased name missing)
  marrage.jpg        Marriage MF-97        6/29  = 20%   (was: spouse names missing/garbage)
  790238709...n.jpg  Marriage, 2nd photo   2/29  = 6%
  TOTAL 65/131 = 49% exact field values, 57 wrong, 9 missing. 23-43 s per page.
Orientation re-verified after the merge: the death sample rotated 90 and 180 degrees is
corrected and still reads 21 of 21 fields.

SCREEN. The required-field guard no longer hard-codes child first/last name — it reads the
REQUIRED list off the recognised form, so it also covers a marriage or death scan being routed
out, and it now runs on Auto-Fill as well as Commit.

NOT FIXED, plainly: the marriage samples. Both are photographs of typewriter print on textured
security paper reading at 39-45%, and values come out one to three characters off ("OFLBENT"
for GILBERT, "SUTILA" for SHEILA) — that is the recognition itself, not the mapping. The
second marriage photograph is worse still because it is PERSPECTIVE-SKEWED: its printed rules
do not line up at ANY single offset, so no global scale-and-offset fit can place it. That
needs dewarping, which this pass does not do. Handwriting (registry numbers, informant lines,
signatures) is still not read anywhere and is flagged for typing in. Build: MSBuild exit 0,
0 warnings, 0 errors across CROMS, CROMS.Display and CROMS.Kiosk.

2026-09-04 (later) — Marriage extraction worked on; one fabrication bug found and fixed, one
technique built, measured and REJECTED.

MEASURED RESULT. Marriage MF-97 went 6/29 (20%) to 8/29 (27%); the second, perspective-skewed
photograph of the same certificate went 2/29 to 3/29. Totals across the five samples:
69/131 = 52% exact field values (was 65/131 = 49%), 54 wrong, 8 missing. Birth 2007 27/30,
birth 1993 11/23, death 20/20 unchanged. Orientation re-verified: a death certificate rotated
90 degrees still corrects and reads 21 of 21.

FABRICATION BUG in DocIntelligence.CorrectDate — pre-existing, and the most serious thing
found today. Its last-resort DateTime.TryParse FILLS IN whatever the string leaves out from
TODAY'S date, so a date field that read only "February" came back as 2026-02-01: a date that
appears nowhere on the certificate, written into a civil-registry record as if it had been
read off the paper. It only surfaced now because the marriage date field had always been
blank before and never reached that line. A parse there is now accepted only when the text
itself carries a four-digit year AND the parse agrees with it.

LABEL FILTER was eating real values. "OFFICE OF THE MUNICIPAL MAYOR" is a real place of
marriage, but "office" and "municipal" are also printed form words, so the label filter
stripped them and the field read "OF THE MAYOR". The filter cannot tell the two uses apart,
so FieldSpec.KeepingLabelWords() lets the field that knows say so. Place of marriage is now
read correctly.

OFFICE VOCABULARY (new Data/DocVocabulary.cs) — PLACES ONLY, and the limit is the finding.
It was built for names as well, on the sound-looking reasoning that a civil registry sees the
same families repeatedly and its own records are the authority on spelling. Measured before
it was trusted: on this marriage certificate it correctly repaired "HELLA" to SHEILA and
"ARTICTILO" to ARTICULO — and also turned "Albert", the husband's FATHER Alberto, into
"GILBERT", the husband, because those two names are two characters apart and only one of them
was in the registry. That is not a threshold to tune, it is the shape of the technique: the
more names an office accumulates the more near-neighbours every reading has, so name repair
gets LESS safe as the database grows. And a wrong name that looks right is worse than a
garbled one — nobody accepts "OFLEERT", everybody accepts "GILBERT". Person names are
therefore excluded. Barangays, municipalities, provinces and hospitals are a small closed set
the office controls, so places are repaired component by component ("Bical, Peflablanca,
Cagayan"), each repair capped at 72% confidence and shown to the operator beside the original
reading.

NOTED FOR THE OFFICE, not changed: the municipalities master file stores "Penablanca" without
the tilde, while the certificates print "Peñablanca". The place repair is faithfully applying
the office's own spelling, so this now propagates into scanned records — and it will already
be affecting search and matching elsewhere in the app. One UPDATE fixes it, but it is their
data to decide on.

NO-CONSENSUS READINGS are now refused. The marriage form's province and municipality lines are
destroyed print; the renderings disagreed completely and a short winner ("lees", "Freer") was
being reported as the province. A short place/text value that no two renderings agree on is
now returned blank with the disagreement quoted. This does not move the accuracy score — wrong
and missing both count as not-correct — but a blank asks to be filled while a wrong value has
to be noticed first.

ALSO: canonical vocabulary allowance widened from length/5 to length/4, which is what let
"Cathol" reach "Catholic" (at length/5 an eight-letter word was allowed a single edit). The
month snap now scales its allowance with length and refuses ties, so "repruery" reaches
February by three edits while nothing reaches two months at once. A failed region is retried
with two additional renderings before being reported blank. CROMS.DocTest gained an App.config
carrying the same connection string — without it the harness ran with no database, the office
vocabulary silently came back empty, and the measurement reported a weaker pipeline than the
one staff actually run.

TRIED AND REVERTED, with the measurement kept in the code so it is not retried blindly:
rejoining words whose boxes nearly touch, to mend "CATAGGATAN" arriving as "CAT AGGATAN". The
boxes were then measured. On this scan the REAL spaces in "OFFICE OF THE MUNICIPAL MAYOR" have
gaps of 0-3px and the false split inside CATAGGATAN has a gap of 0-1px — not separable by
geometry at any threshold — and the same boxes report negative gaps and zero heights. Every
threshold that mended the name also welded the heading into one word. Reverted.

STILL NOT FIXED, and honestly: most of what marriage still gets wrong is the recognition
itself, not the mapping. Both photographs are typewriter print on textured security paper
reading at 39-45%, and the values come out one to three characters short of correct
("OFLEERT" for GILBERT, "TALOST" for TALOSIG, "AOSD" for BALOSO) — the characters are simply
not on the image to be read. The husband's and wife's dates of birth and ages are the same
story. The second photograph is additionally perspective-skewed, so its printed rules do not
line up at any single offset and no scale-and-offset fit can place it; that needs dewarping.
Build: MSBuild exit 0, 0 warnings, 0 errors across CROMS, CROMS.Display and CROMS.Kiosk.

2026-09-04 (screenshots) — The review grid was putting a GREEN TICK on wrong values. Fixed,
plus the label filter was eating real place names. Totals: 71/131 = 54% exact field values
(was 69/131 = 52%); birth MF-102 (2007) 29/30 = 96%, death 20/20 = 100%, birth 1993 11/23,
marriage 8/29, marriage 2nd photo 3/29.

THE TICK BUG, and it is the important one. The screen showed "OFLEERT" (GILBERT), "TALOST"
(TALOSIG) and "CAT AGGATAN" at 75-78% with a green ✔ Ready — the UI actively telling the
operator that garbage was fine. Root cause was the confidence formula weighting AGREEMENT
between renderings at 0.6. Agreement looks like independent evidence and is not: every
rendering is derived from the same damaged pixels, so on a bad scan they agree on the same
mistake. Measured per region to confirm before changing anything: "OFLEERT" carried engine
confidence 48/38/40 while the blend reported 77, whereas readings that are CORRECT carry
93/60/92 ("SHELLIAN CLEAR") and 87/92/93 ("GEORGE"). So the engine's own confidence separates
good from bad cleanly and the formula was discarding it. Reported confidence is now capped at
the winning reading's engine confidence + 8 — agreement can raise trust WITHIN that ceiling,
never above it. Result: OFLEERT 77→55, TALOST 75→48, CAT AGGATAN 78→59 (all now flagged
"weak"), while SHELLIAN CLEAR and TALOSIG stay at 99% ok. The ceiling costs nothing when the
recognition was sound and removes the false reassurance when it was not.

LABEL FILTER, second instance of the same fault. "Tuguegarao City" was being reported as
"Tuguegarao" because "city" is also a printed form word, exactly as "OFFICE OF THE MUNICIPAL
MAYOR" had become "OF THE MAYOR". The region was never wrong — cropping it by hand reads
"Tuguegarao City" perfectly. Whole label words are now KEPT for place fields (a place
legitimately contains them); a region that caught only the printed label is rejected outright
instead, and clipped label fragments ("cipality" out of "City/Municipality") are still
stripped. Birth 2007 went 27/30 to 29/30 on this alone.

BUG I INTRODUCED AND CAUGHT BY MEASURING: the "reject a region that is only label text" rule
skipped tokens with no letters, so a value made entirely of digits had nothing to assess and
the rule concluded everything assessable was a label. That blanked the weight, both parents'
ages, the age at death, the registry number and the date of birth — 69/131 fell to 64/131.
A number is data, never a printed label; the rule now says so explicitly and returns false
the moment it sees a digit. Worth remembering as a shape of bug rather than a one-off: a
"do all items satisfy X" test over a list whose items were mostly skipped is vacuously true.

NOTE FOR THE NEXT BUILD: the code compiles clean, but bin\Debug\CROMS.exe could NOT be
updated because CROMS.exe was running and VS 2019 held it, so the accuracy above was measured
against a staged copy of the freshly compiled obj\Debug\CROMS.exe. CROMS.Display and
CROMS.Kiosk built normally. Close the running app and rebuild in VS to pick this up.

2026-09-04 (handwriting) — TrOCR offline handwriting recognition was requested, PROTOTYPED
AND MEASURED, and NOT shipped, because on these certificates it does not work. No CROMS code
changed. Recording it here so nobody spends the effort again without new evidence.

WHAT WAS BUILT AND RUN (throwaway Python, deleted afterwards): the real ONNX export of
microsoft/trocr-base-handwritten (onnx-community/trocr-base-handwritten-ONNX) — ViT encoder
plus autoregressive text decoder, greedy decode loop, RoBERTa byte-level BPE detokenising —
run over crops of the office's own scans. Deliberately measured BEFORE writing the C#
integration, because shipping it means a 390 MB (int8) to 1.5 GB (fp32) model beside an
application whose whole client bundle is currently 27 MB.

THE PIPELINE WAS VERIFIED FIRST, with printed-text controls, and that mattered: the first run
failed on EVERYTHING, controls included, which said the harness was broken rather than the
handwriting being hard. Cause was mine — the control crop was a 30:1 strip and TrOCR squashes
every input to 384x384, so the text was destroyed by the resize. At a sane aspect the same
model reads printed text correctly ("GEORGE" -> "george", "DE GUZMAN" -> "dequzman"), which is
what makes the handwriting result below trustworthy. Anything past about 8:1 fails even on
print.

RESULT ON HANDWRITING: nothing readable. Six attempts across the three handwritten registry
numbers (wide crop and tight digits-only crop each), plus the cursive informant signature:
  2005-4249  ->  "topping a"          (the clearest sample on the whole set)
  2018-4555  ->  "broken after"
  2007-72    ->  "transervatives"
  signature  ->  "excessive"
The crops were exported and eyeballed to confirm they really were on the handwriting, after
one of them turned out not to be. The failure mode is characteristic and explains itself:
trocr-base-handwritten is trained on IAM, which is English cursive PROSE, so digit strings are
out of domain, and because the decoder is a language model it answers with fluent English
words rather than admitting it cannot read. A confident wrong sentence is the worst possible
output for a civil-registry field.

WORTH KNOWING, and it narrows the problem a lot: on these PSA forms the handwriting is
almost entirely (a) the registry number, which is DIGITS, and (b) signatures. The informant
NAME on nice.jpg is typed, not handwritten — checked by cropping and looking. So the case
TrOCR is actually good at (cursive words) barely occurs here, and signatures are not
transcribed into the registry as data anyway.

CONCLUSION: not shipped. It would add a 390 MB - 1.5 GB dependency, 2-3 s per field, and read
none of the fields it was added for. If this is revisited, the target is a DIGIT recogniser
(SVHN/CRNN-style) for registry numbers, not a prose HTR model — but note the standing caution
that a misread registry number is worse than a blank one, because it is the record's key and
nobody will notice 2018-4655. The current behaviour (blank, flagged, typed in by staff) stays.

2026-09-04 (1993 birth form) — gilvan birth.jpg went 11/23 (47%) to 14/23 (60%). Totals across
the five samples: 74/131 = 56% exact field values (was 71/131 = 54%); birth 2007 29/30 = 96%,
death 20/20 = 100%, marriage 8/29, marriage 2nd photo 3/29. Orientation re-verified (rotated
death certificate still 21 of 21).

FAMILY NAMES ARE NOW RECONCILED FROM THE FORM'S OWN STRUCTURE. On Municipal Form 102 two pairs
of cells are not merely likely to match, they are the SAME NAME by law: the child's last name
IS the father's last name, and the child's middle name IS the mother's maiden surname. So when
the two cells come back one or two characters apart, that is one name the scanner read twice
with different luck, and the better read repairs the worse one. Measured: "Talosige" at 51%
became "Talosig" from the child's cell at 82%, and "Balogo" at 32% became "Baloso" from the
child's middle name at 70%.
  This is NOT the office-wide name gazetteer rejected earlier the same day for turning a
father into his son. Nothing is imported from other records — both readings come off the page
in front of us, the repair only runs when they already almost agree (2 edits), and it needs a
clear confidence winner. A child lawfully registered under the mother's surname is many edits
away and is left alone for the existing "surname matches neither parent" check to flag. Every
repair is shown to the operator with the original reading beside it.

TRAILING STRAY CHARACTER stripped from text values: the father's occupation read "Bagger 2",
where the "2" is the cell's ruled edge or a tick from the next column. Names are deliberately
excluded (an initial is a real part of a name), which is why the rule lives in the general text
branch and not in CleanNameCell.

TRIED AND REVERTED, measurements kept in OcrService so they are not retried blindly. The
region reader contrast-stretches before thresholding, and on this faded 1993 scan that is what
turns "Gilvan" into "beseud" — the same crop reads correctly unstretched. But dropping the
stretch everywhere gained a marriage field and lost one on EACH birth certificate (73 to 72),
and offering both as a fourth rendering was worse still (73 to 70): more candidates split the
vote, so a reading that used to win two votes out of three wins one out of four and is then
discarded as having no consensus. Extra renderings are not free accuracy. Three is the measured
optimum and the count is now commented as such.

STILL WRONG on this sample, and mostly not fixable by mapping: the weight reads 2035 for 2835
and the father's age 99 for 22 (single-digit misreads that pass every range check), "gheila"
for Sheila and "Catagrataa" for Cataggatan (recognition), and the province/municipality lines
("etro rgnits", "J ee") which are faint print at the top of a photocopied form. ChildFirst is
the interesting one: the region and the recognition are both FINE — the gray rendering returns
"Fref} | Gilvan" — but the printed "(First)" hint sits inside the same cell band because the
scan is skewed, and the pipeline merges it with the value instead of splitting on the printed
rule. Splitting a region read on the "|" ruling and scoring each side separately is the obvious
next move there; it was not attempted because it needs a discriminator better than "longest
segment" to be safe.

NOTE: bin\Debug\CROMS.exe still could NOT be updated (CROMS.exe running, VS 2019 holding it),
so this was measured against a staged copy of the freshly compiled obj\Debug\CROMS.exe. Close
the app and rebuild in VS.

2026-09-04 (child's given name) — "Frey ilvan" is now "ilvan": the intruding text is gone,
character accuracy on that field went 17% to 83%, and it is flagged at 52% so staff check it.
Totals unchanged at 74/131 = 56% (nothing else moved), orientation re-verified.

IT WAS NOT A "|" PROBLEM. Splitting the reading on the form's printed rule was tried first and
measured WORSE (74 correct to 71): the junk is present in every rendering, so the merged
reading is more STABLE than either side and short junk fragments won ties outright — it broke
the child's middle name, which then cost the mother's maiden surname its reconciliation
partner. Splitting on the rule using per-word confidence instead was neutral (74), and dumping
the words showed why: there is no "|" in this cell at all. The hint and the name are on two
separate LINES.

THE REAL SIGNAL WAS ENGINE CONFIDENCE, and it was never subtle. In that cell the intruding
"(First)" hint reads "vee"/"eeey"/"yeey" at confidence 9/0/5 while the name reads
"@ilvan"/"@iivan" at 58/79/78. MainText now drops words that far below the best word in the
same cell (needs a wide gap, so a uniformly difficult cell keeps everything).

AND THE FILTER WAS BEING DEFEATED BY A GUARD ABOVE IT. MainText bailed out to the raw text
whenever fewer than two words scored 20 or better — which is exactly what happens here, since
the hint scores 9 and is excluded, leaving one word. So the function handed back the raw text,
hint included, precisely in the case the filtering existed to fix. One good word is a good
answer; the guard now returns it.

STILL WRONG, and it is our own preprocessing: every rendering reads the leading G as "@"
("@ilvan"), while the same crop WITHOUT the contrast stretch reads "Gilvan". Removing that
stretch globally was measured earlier and costs more than it gains (73 to 72, and offering
both renderings 73 to 70). So the field stops one character short, flagged. A stretch-free
rendering offered only on a retry for still-doubtful fields is the next thing to try, and it
should be measured rather than assumed — the last two ideas here both looked obvious and both
made things worse.

2026-09-06 (unknown form layouts) — CROMS now REFUSES a form template that does not fit the
page in front of it, and reads the page by its printed labels instead. Totals: 71/131 = 54%
exact field values (was 74/131 = 56%), but WRONG values fell 49 to 28 — which is the number
that matters for a registry. The four samples whose template fits are byte-identical to
before (birth 2007 29/30, birth 1993 14/23, death 20/20, marriage 8/29); the only change is
the second marriage photograph.

WHY. `DocLayouts.Detect` picks a layout from the page's WORDS — "Certificate of Live Birth",
"Municipal Form No. 102". Those words identify the KIND of certificate but NOT which revision
of it, and the office receives several. Every revision prints the same words while putting
the rows in different places, so a 1993 sheet matches the 2007 layout's markers perfectly and
is then read with coordinates pointing at the wrong rows. The failure is silent and it is the
bad kind: a template that misses does NOT come back empty, it reads whatever is at those
coordinates and returns it as a value.

MEASURED, by ablation (temporarily dropping MF-102 (1993) from the library so the 1993 scan
is forced through the 2007 template — the exact "form is a different revision" case):
  before: 0/23 correct, 15 WRONG, 8 missing.
The wrong values were not noise an operator would catch — "OCRG USE ONLY" arrived as the
child's SURNAME at 74% confidence, "Fret Eddie" as the given name at 72%, and the printing
instruction "accurately and legibly. Use ink oF" as the province at 68%. Every one of them
passes the field's shape check, because they are the right shape; they are just from the
wrong row.

THE DETECTOR was already in the data and only needed reading. Anchors are printed labels the
template says should be at particular spots, so a layout that belongs to the page FINDS them
and LANDS on them. New PageFit.Agreeing()/SquareInliers counts how many matched anchors the
fitted placement actually lands on, and PageFit.Trustworthy is `AnchorsMatched >= 3 &&
(SquareInliers >= 2 || RulingsMatched >= 4)`. Across the samples:
  nice.jpg (correct template)        7 anchors, 7 agreeing
  gilvan birth.jpg (correct)         5 anchors, 5 agreeing
  palogan_n.jpg (correct)            4 anchors, 4 agreeing
  marrage.jpg (correct)              5 anchors, 3 agreeing + 11 rulings
  marriage photo 2 (skewed)          5 anchors, 0 agreeing   -> REFUSED
  gilvan through the 2007 template   1 anchor,  0 agreeing   -> REFUSED
Rulings can stand in for agreement, which is what keeps marrage.jpg on the region path: it
fits only 3 anchors but locks onto 11 printed rules, and that places the table just as firmly.

WHAT REFUSAL DOES: the layout is dropped and the scan falls through to the EXISTING
unknown-form path (Classify + the label-anchored ExtractBirth/Marriage/Death) — the code that
was already built for a form the library does not have. Result on the ablation: 1/23 correct,
3 wrong, 19 missing. Same score, entirely different failure — 15 fabricated values became 19
honest blanks. A blank asks the operator to type it; a wrong value has to be noticed first.

Also added DocumentAI.MergePageText: on a template that DID fit, fields its boxes could not
read are filled from the label pass, vetted against the field's own shape
(RegionReader.Vet/Declares, new public wrappers) and marked FromRegion=false so the grid
flags them. Honest note: on the five samples this recovers ONE field (a date of birth) —
its value is for revisions not in the sample set, and it cannot regress by construction
since it only fills blanks.

SCREEN + AUDIT: the doc-class line appends "unknown layout, read by labels"; a new dialog
explains in the operator's terms why a certificate they recognise was not read box by box and
that the blanks are deliberate; DocIntelligence.ReviewReason says it FIRST when it applies,
because it explains the blanks and is not something correcting a field will resolve. The fit
note (already written to ocr_batch.review_reason) now records the refusal and its counts.

COST, stated plainly: marriage photograph 2 went 3/29 to 0/29. It is refused because its
anchors agree with nothing — the perspective skew documented on 2026-09-04 — so its 3
accidentally-correct fields went with its 21 wrong ones. For a civil registry that is the
right trade, and the screen already held that scan for manual review either way.

TRIED AND REMOVED, measurement kept in DocLayouts.cs so nobody retries it blindly: a TILTED
(affine) fit, adding a shear term so a row's page position could depend on how far ACROSS the
form it sits. The reasoning looked strong — a photograph is never square to the camera, and on
marriage photo 2 the wife's residence read at 79% of its characters while the husband's, one
row group away in x, read at 8%. Built as a RANSAC over anchor triples with plausibility
guards, then measured: it NEVER engaged on any sample. SquareInliers shows why — where a
template belongs to the page the untilted fit already lands on every anchor (7/7, 5/5, 4/4),
leaving a tilt nothing to improve; where it does not, no three anchors agree well enough to
imply a trustworthy tilt either. Deleted; the inlier counting it was built on is what became
the trustworthiness test.

STILL NOT FIXED, and honestly: this makes an unknown layout SAFE, not READ. On a form whose
layout is not on file, values now come from the whole-page label pass, and Tesseract's layout
analysis drops entire typewriter rows inside these bordered tables — which is the whole reason
region reading was built on 2026-09-04. So a strange form yields blanks where a known one
yields values. The real fix is to locate each field by its OWN printed label's position and
crop THAT region — label-anchored region reading, which would generalise to any revision while
keeping the cropping that actually reads these tables. Not attempted here. Handwriting and the
perspective-skewed photograph are unchanged.

NOTE: compiles clean (0 errors, 0 warnings) but bin\Debug\CROMS.exe could NOT be updated —
CROMS.exe is running and VS 2019 holds it — so the accuracy above was measured against a
staged copy of the freshly compiled obj\Debug\CROMS.exe. Close the app and rebuild in VS.

2026-09-06 (registry numbers, read by hand) — Transcribed the registry number off every
sample by cropping the region from the source image and reading it at 6-12x. Recording them
because the harness deliberately has no expectation for this field (handwritten), so these
are the only record of what the paper actually says:
  nice.jpg          2018-4555   handwritten; the last three digits are the soft part, 4585
                                and 4575 are not fully excluded from the pixels alone
  gilvan birth.jpg  2005-4249   handwritten, unambiguous
  marrage.jpg       2007-72     TYPED, not handwritten (see below)
  photo 2           2007-72     same record, blurrier, confirms it
  palogan_n.jpg     1999-76251  printed; the pipeline already reads this one at 99%
The first three corroborate the numbers recorded in the 2026-09-04 TrOCR entry.

THE MARRIAGE NUMBER IS NOT A HANDWRITING PROBLEM, which is worth knowing before anyone
spends effort on handwriting again for this field. "2007-72" is typewritten; it fails because
the typist struck it high and slightly left, so the "72" overlaps the printed "Registry No."
label and reads as part of that label rather than as a value. That is a label/value collision,
which is a different and more tractable problem than reading cursive.

BUG FIXED, found while checking whether "2007-72" would even survive the pipeline: the SAME
field was validated by three different regexes, and they disagreed.
  DocLayouts.Normalise  (extraction)  \d{1,6} after the dash - accepts 2007-72
  DocLayouts.Score      (acceptance)  \d{1,6}                - accepts 2007-72
  DocIntelligence:482   (validation)  \d{3,5}                - REJECTS it as Invalid
  DocIntelligence:314   (correction)  \d{3,5}                - reverts a correct repair
So a CORRECTLY read 2007-72 would have been extracted, accepted, then flagged Invalid with
"expected YYYY-NNNNN" - and since RegistryNo is a core field, an Invalid core field holds the
whole document for manual review. The office's own certificate is the counter-example: a
small office numbers from 1 each year, so an early entry is genuinely two digits. Both
DocIntelligence patterns widened to \d{1,6} to match the two in DocLayouts that were always
right, with a comment on each pointing at the other so they stay in step. The "registry
numbers are handwritten, type it in" wording was dropped from the message too - on the death
certificate it is printed.
Measured after the change: 71/131, 28 wrong - unchanged, as expected (the marriage registry
field still reads blank, so the rule had nothing to fire on yet). Build clean, 0 warnings.

2026-09-06 (registry label collision — NOT FIXABLE, measured) — Tried to recover the marriage
certificate's registry number "2007-72", which the app reports as blank. It cannot be read,
and the sweep is recorded here so nobody spends the effort again.

WHAT THE COLLISION ACTUALLY IS. Not adjacency — glyph overlap. The typist struck the number
high and left, so "2007" lands across the printed word "Registry" and "72" sits INSIDE "No".
The two texts occupy the same pixels. Worth noting the same habit on the birth certificates:
the handwritten number is also written over the caption there, so this is a pattern on these
forms, not a one-off.

EVERYTHING TRIED, all negative on the cell the app actually crops (0.515,0.1280,0.150,0.0230
of MF-97, = 204x47 px on this 1355x2048 scan):
  - ~96 combinations of upscale (x4/6/8/10/14) x rendering (plain / autocontrast / unsharp /
    both) x page-seg (6,7,10,11,13) x digit whitelist on/off. ZERO produced "2007" or "72".
  - tessedit_char_whitelist=0123456789- specifically. It is the obvious lever for a field
    that is known to be digits, and it does not help: the engine is not choosing letters over
    digits, it is finding no coherent glyphs at all.
  - Separation by ink density. Measured the two bands: 5th-percentile grey is 41 for the
    printed caption and 40 for the typed value. Identical — no threshold splits them.
  - Reading it from somewhere else on the page. The whole-page text is 1024 characters and
    contains no "2007", no "72" and no "Registry" at all.
The ONLY readings that worked came from crops I placed BY HAND around the digits after
looking at the image (x10 + unsharp + whitelist reads "2007" cleanly). That is not a fix: the
app has the layout rectangle and no way to find the digits inside it. And even hand-placed,
the sequence is unreliable — the same crop reads "92" as often as "72", and a wrong registry
number is the worst possible output because it is the record's key and nobody notices
2007-92.

WHAT WOULD ACTUALLY FIX IT, neither attempted: (1) a higher-resolution scan — the cell is
204x47 px, and the strokes of the two texts would separate at 600 DPI where at this size they
merge; (2) FORM DROPOUT — register a blank MF-97 against the page and subtract the printed
layer before reading, which is how commercial form readers handle exactly this. That needs a
clean blank scan of each form revision the office uses, which is a real prerequisite to
confirm before building anything.

WHAT WAS CHANGED. Only the message. A blank Registry field said "Nothing usable read here —
the scan shows i", which reads as a scan-quality problem and invites the operator to rescan
at the same settings forever. New RegionReader.BlankIssue gives that shape its own wording:
"Type this in from the scan — the number is written over the printed 'Registry No.' caption,
which no amount of re-scanning separates." Every other field keeps the old message. Measured
after: 71/131, 28 wrong — unchanged, as expected for a text-only change. Build clean.

2026-09-06 (form identity + Crystal Reports) — Records now carry WHICH FORM they came off,
the digital form follows the paper form's own structure, logo and stamp are stored and placed
independently, and one reusable report path serves every certificate. Crystal Reports is
wired end to end; the only thing not delivered is the .rpt layout files themselves, and the
reason is recorded below.

CRYSTAL IS ACTUALLY INSTALLED — the risk this project's own log flagged on 2026-08-29
("prove one report renders end-to-end on the deployment PC BEFORE building UI around it") is
closed. SAP Crystal Reports for .NET Framework 13.0.4000.0 is in this machine's GAC (GAC_MSIL
+ GAC_64, plus the win32/win64 native folders). Compiled and RAN a smoke test on .NET 4.7.2 /
AnyCPU before writing anything: ReportDocument and CrystalReportViewer both load. Referenced
from CROMS.csproj by strong name with Private=False, so nothing is copied into bin\Debug or
into the client bundle.

WHAT CANNOT BE DONE, measured not assumed: .rpt files cannot be generated in code. Tried
in-process RAS (CrystalDecisions.ReportAppServer.ClientDoc IS present in the GAC):
ReportClientDocument.New() fails with "Failed to connect to server %MACHINENAME%" — the free
CR-for-VS runtime carries no Report Creation API, and no designer is installed. So a .rpt must
be authored in the Crystal designer. Everything around that is built, and CROMS prints every
certificate today without one.

NEW Data/FormCatalog.cs — the single form registry, and the answer to "make it reusable so
different forms use their own field mappings and report layouts without hardcoding". One
FormDefinition per certificate REVISION: form code / form name / municipal form no /
revision, the registry table, the report view, the .rpt name, the blank-form scan, the logo
and stamp rectangles, the printed sections, the extraction-key-to-column map, and the print
map. Four entries today (MF-102-2007, MF-102-1993, MF-97-1993, MF-103-2016). Adding a form is
one entry plus, optionally, a .rpt and a blank scan — no code downstream changes.
  Field labels, shapes and printed ORDER are NOT duplicated here: FormDefinition.Layout
resolves to the existing DocLayouts entry by code, so the coordinates that tell OCR where to
READ and the sections that tell the screen how to DISPLAY come from one place.
  The insight that made the print map free: DocLayouts already holds each field's position as
a normalised 0-1 page rectangle, so the same data can drive the printer. The 60 measured
point literals in BirthCertificatePrinter are now DATA in the catalog (divided by the
792x1224 pt page), so the MF-102 replica is the same output, just no longer hardcoded to one
form. BirthCertificatePrinter.cs is now unreferenced.

WHY THERE ARE TWO SECTION LISTS, since it looks like duplication and is not. FormSection is
keyed by EXTRACTION KEY and covers what a scan can produce — it drives the review grid.
ReportSection is keyed by REPORT VIEW COLUMN and covers what the RECORD holds, including
everything typed in later that no scan yields: the informant block, the attendant's
certification, prepared/received by. It drives the printed document and tells a .rpt author
what to lay out in what order.

NEW migration 26_form_identity.sql (APPLIED to the live croms DB, idempotent):
  - form_code + form_name on births / marriages / deaths / ocr_batch, indexed. The registry
    number was already stored; form identity was not — and `births` is shared by EVERY MF-102
    revision, so the revision is not recoverable from the row. Without it a certificate cannot
    be reprinted in the layout it came off. Existing rows backfilled to the revision the
    office issues today.
  - book_page on all three, book_volume on deaths — the paper form states book and page and
    two of the three tables had nowhere to put them.
  - registry_no indexed on all three; it is looked up constantly and had no index anywhere.
  - office_assets: logo and stamp as SEPARATE ROWS of one table, not two columns. The office
    replaces a stamp far more often than a seal, a form revision can carry its own stamp, and
    a report that stamps only issued copies must be able to ask for one without the other. A
    row with a form_code beats the office-wide default for that form. Removal DEACTIVATES
    rather than deletes — a certificate already issued carried that seal.
  - office_profile: one row. The registering LGU and the registrar were string literals in the
    printer, so a second LGU could not use the app and a change of registrar needed a rebuild.
  - THE CRYSTAL DATASOURCES: v_birth_certificate / v_marriage_certificate /
    v_death_certificate, each one flat row per record. This is what makes a .rpt trivial to
    author. `marriages` and `deaths` store most values as lookup IDs (husband_religion_id,
    cause_of_death_id, ...), so a report bound to the table would print NUMBERS; the views
    resolve every one, and add the form identity and office profile. They deliberately exclude
    the scan blobs — a one-row certificate does not need the 2 MB source scan, and pulling it
    through the datasource is what makes Crystal slow. Plus v_certificate_index, one
    searchable list across all three (16 rows).

NEW Data/CertificateReport.cs — the one way a certificate is printed, previewed or exported,
for every form. Resolves the form, pulls its one view row, then renders by the best means
available: Crystal when a .rpt exists AND the runtime is present; else an OVERLAY (the blank
form as the page background with each value at its measured coordinate — a true replica);
else STRUCTURED (the form's own sections, labels and printed order, with the office header,
logo and stamp). A broken or mismatched .rpt does not leave the operator with no certificate:
it says what happened and prints anyway.
  Crystal is isolated in Data/CrystalRunner.cs + Forms/CertificateViewerForm.cs — the ONLY
two types that mention Crystal. The CLR resolves an assembly when it JITs a method naming its
types, so keeping every reference inside those two means a PC WITHOUT the runtime runs CROMS
normally; CrystalAvailable probes by Assembly.Load and the fallback takes over. Verified
CrystalAvailable = True against the built exe.
  Logo and stamp reach a report as byte[] COLUMNS on the datasource (logo_image /
stamp_image), rendered as Blob fields. That is the only way with the report Engine alone —
setting a picture object's image at runtime needs the licensed creation API that was just
proven absent. Two columns, so a report can show or suppress either independently.
  Report parameters are filled only where the report declares them (FormName, RegistryNo,
RegistrarName, PrintedBy, ...), and any parameter it declares but we do not know is set to
empty — an unset Crystal parameter stops mid-print with a dialog a front-desk clerk cannot
answer. The viewer's own toolbar gives print, page setup, zoom and export (PDF/Excel/Word/
RTF/CSV) for free.

SCREEN CHANGES.
  Intelligent Document Processing: a FORM IDENTIFICATION strip above the grid showing form
name, Municipal Form No., revision, form code and the REGISTRY NUMBER (which follows the grid
as it is typed, since it is part of the record's identity). The review grid is now GROUPED
UNDER THE CERTIFICATE'S OWN SECTION HEADINGS in printed order ("1-5. Child", "6-12. Mother",
...) instead of extractor order — the operator is comparing the grid against the paper, so
the two must read the same way down the page. A field no section claims still appears, under
"Other Entries"; a value is never hidden because the catalog forgot it. New Print Certificate
button, live once the record exists. Commit/Draft write form_code + form_name, and the audit
line names the form. Auto-Fill carries FormCode/FormName across to the registration module,
which is what finally writes the row.
  ocr_batch records form_code only when the layout was TRUSTED, while doc_kind still says
Birth — which is exactly the distinction an auditor needs between "read box by box off this
revision" and "read by labels, filed under the revision we use".
  Birth / Marriage / Death registration: each stores and reloads its row's form identity, so
editing or reprinting keeps the revision it was registered on rather than silently migrating
it to today's; ClearForm resets to today's for a fresh record. Birth's print button and
Death's now go through CertificateReport. Death's BURIAL PERMIT stays a separate print — it
is a different document, not a rendering of the Certificate of Death — and is now only
offered when a permit was actually issued.
  Settings: new "Certificates & Forms" tab — behind the same admin re-verification as the
window CRUD — with the branding manager and a read-only list of every form, its datasource,
and whether its report has been authored yet. That last column is the only place the office
can see that a form is still printing on the built-in renderer.
  New Forms/OfficeAssetsForm.cs: logo and stamp managed independently (separate pickers,
previews, save and remove), office-wide or per-form, plus the office profile. It queries the
EXACT scope rather than going through OfficeAssets.Get, whose fall-back-to-default would make
an inherited office logo look like one saved against this form. Rejects a file over 3 MB and
one that does not decode as an image — a renamed non-image would otherwise sit in the database
and fail at print time.

NEW Data/SealDetector.cs — "if the scan contains a detected logo or stamp, preserve/map it
rather than treating it as ordinary OCR text". Tesseract has no idea what a seal is: it reads
one as nonsense words, and when a seal sits over a field's box that nonsense becomes the
field's VALUE. This project's log already records a signature read as "excessive" and an
informant line as "Tite oF ADMIN STRATIVE ADEM!". So seals are located first and any reading
from inside one is REFUSED — emptied, flagged, with the original kept in OcrValue for the
audit trail. One found can also be captured as the office logo or stamp.
  THE FIRST HEURISTIC WAS UNSAFE AND MEASURING CAUGHT IT. Density + squareness alone accepted
13 candidates across the five samples: 1 real seal and 12 dense TEXT BLOCKS — and one of those
blocks sat over the birth WEIGHT, so suppression would have blanked a correct value. Cropped
and LOOKED AT every candidate rather than trusting the numbers. The discriminator is not
darkness (print is just as dark) but the LEADING between text lines: measured per candidate,
the one genuine seal (the PSA emblem on nice.jpg) gives 15% near-empty scanlines in 1 run,
while every text block gives 27-67% in 4-10 runs — no overlap. Cut at 25% / 2 runs. After:
13 candidates -> 4, and both birth/marriage false positives are gone entirely.
  The 3 surviving extras are SIGNATURES, which is correct for the purpose — ink that OCR reads
as garbage and must not become a value. But saving a doctor's signature as the municipal seal
would be worse than useless, so the capture dialog now SHOWS each crop with Previous/Next
before the operator keeps one; a numbers-only prompt would have allowed exactly that mistake.
The faint seal on palogan_n.jpg is MISSED — deliberate direction: a miss costs nothing, a
false positive suppresses a real value.

BUG FOUND AND FIXED IN MY OWN WORK, caught only by rendering a real certificate: the office
municipality printed as garbage where the enye should be. Migration 26 is applied by piping
the file into the mysql client, which decodes it using the CONSOLE code page — the two UTF-8
bytes of the enye (C3 B1) were decoded as box-drawing characters and stored that way (HEX
confirmed 5065E2949CE2969261626C616E6361). The column default is now plain ASCII and the real
spelling is written through CONVERT(X'5065C3B161626C616E6361' USING utf8mb4), which states the
bytes explicitly and cannot be reinterpreted by any client charset. Re-applied and verified
(5065C3B161626C616E6361). WORTH REMEMBERING: never put a non-ASCII literal in a migration that
is applied by piping. Related and NOT changed, since it is the office's data to decide: the
municipalities master file still stores "Penablanca" without the tilde, as noted on 2026-09-04.

NEW CROMS/Reports/ + README.md, deployed beside CROMS.exe. The authoring contract for the one
thing I cannot produce: the file must be named <FormCode>.rpt, bind to the one view with no
extra tables and no record-selection formula (CROMS passes a pre-filtered row, so the report
must not query), place logo_image / stamp_image as Blob fields suppressed when null, and the
optional parameter names. Settings -> Certificates & Forms is where you confirm CROMS picked
the file up.

VERIFIED, by rendering real records rather than by compiling: migration 26 applied and its 4
views queried; the structured renderer drawn to a bitmap for death (42 view columns, 1 page),
marriage (45 columns, 2 pages) and birth (62 columns, 2 pages), each showing the form name,
Municipal Form No., revision, form code and registry number in its header; the MF-102 overlay
drawn with all 42 cells and 10 tick marks landing on their boxes; CrystalAvailable True and
all four catalog entries resolving their view. MSBuild clean, 0 errors 0 warnings, across
CROMS + CROMS.Display + CROMS.Kiosk. bin\Debug was NOT written (built to a temp OutputPath) —
REBUILD IN VS to pick this up.

NOT DELIVERED, plainly: the .rpt files (no designer, and programmatic creation proven
impossible with this runtime — the built-in renderers cover every form until they arrive);
and a blank-form scan for MF-102 (1993), MF-97 and MF-103, so those three print in structured
layout rather than as visual replicas — the replica needs a clean scan of each blank sheet,
which is a real prerequisite to get from the office and the same one already noted for form
dropout on 2026-09-06. OCR recognition accuracy is untouched by this pass.

2026-09-06 (print after auto-fill) — Print Certificate no longer dead-ends on an unsaved
record. Reported from the app: scan a birth certificate, Auto-Fill, land on Birth
Registration with every box filled from the scan — then Print Certificate answers "Click a
record in the list to print it first", and the record it is asking for does not exist yet.

THE GATE IS REAL, THE DEAD-END WAS THE BUG. A certificate is printed from the registry
ENTRY, not from the boxes on screen, and that is not procedure for its own sake: an unsaved
form has no registry number, no audit row, and no record anyone can look up, so a
certificate printed from it would be an official-looking document the registry cannot
account for. That is the same failure mode this project keeps guarding against — something
that looks filled in and correct while nothing stands behind it. So printing straight off
the textboxes was rejected.
  What was wrong is that the screen REPORTED the missing step instead of OFFERING it, and
told the operator to go find a row that cannot be there. Print Certificate now asks to save
the record and prints in the same click.

BIRTH: new SaveThenPrint(). Validates the child fields, then names what it is about to do
and saves with the status the operator ALREADY chose in the Status box — pressing Print must
not silently promote a draft into a registered birth. Auto-Fill deliberately leaves the
status on Draft (the operator has to review a scan before it becomes official), and a Draft
has no registry number by design, so the prompt says the certificate will print with that
line blank and points at the Status box. Non-draft saves get a registry number as usual.

DEATH: the same dead-end, fixed the same way. Its row-load was inline in
dgvDeaths_CellClick, so it was extracted into LoadDeath(id) to be reusable, and the insert
in btnSave_Click became Register(keepOpen).

Create(status, keepOpen) / Register(keepOpen) now RETURN the new id, and on keepOpen they
skip ClearForm and reload the row that was actually written instead. That matters twice: the
form stays on the record so _editingId is set for the print, and what gets printed is the
registry entry — including the registry number assigned during the save — rather than the
pre-save contents of the boxes. Both callers verify _editingId is set after the save rather
than assuming, so a failed write cannot fall through into printing nothing. The existing
Save Draft / Submit / Register buttons are unchanged (keepOpen defaults false).

NOT changed, deliberately: Intelligent Document Processing's own Print Certificate still
requires Commit first. It is not the same trap — the Commit button is right there and
enabled — and Commit carries the flagged-field confirmation that a print click should not be
able to bypass. Build clean, 0 errors 0 warnings.

2026-09-07 (Reports & Analytics, steps 1-2) - The Reports screen is now a SIX-TAB
Reports & Analytics module. Shell + Birth tab are built and measured against the live
database; Death / Marriage / Queuing / Certificates are deliberately empty and say so.
The PSA report is rehosted UNCHANGED. Read-only throughout: SELECT statements only, no
INSERT/UPDATE/DELETE/DDL anywhere under Analytics\, and no schema change.

WHY THE PSA TAB WAS DONE NOW rather than in its listed slot (step 5). The build order put
it last, but the module registry entry "reports" had to be repointed at the new form in step
1 - so shipping the shell without the rehost would have removed statutory reporting from the
running app for a whole review cycle. It is an embed, not a rewrite: ReportsPsaForm with
TopLevel=false, the same pattern MainForm already uses for every module, so its logic,
queries, CSV export and output are byte-identical to the screen the office checks against PSA.

NO NEW PACKAGES. Charting is System.Windows.Forms.DataVisualization, which ships with the
framework - added as an assembly Reference, not a NuGet package.

PARAMETERISATION, stated precisely because "parameterised only" cannot be met literally.
Every VALUE is a MySqlParameter. Table and column names CANNOT be parameters in SQL, so the
few helpers that take an identifier receive a compile-time literal from widget code and run
it through AnalyticsData.Ident(), which throws on anything that is not a bare lower-case
identifier. That makes injection through this path structurally impossible rather than merely
unlikely.

LAZY BY TAB. A tab queries nothing until it is selected, and RefreshData() refreshes only the
visible tab. Six domains x five aggregates on every navigation is exactly the load the office
LAN will not absorb.

THE EMPTY STATE IS THE FEATURE, and on this data it is most of what shows. Every widget
checks column coverage FIRST (AnalyticsData.GetCoverage) and renders a panel naming the
column and the count instead of a chart - "No data yet - mother_age is unfilled in 14 of 14
records." Under three data points, trend lines, percentages and comparison language are all
suppressed; the trend chart even switches from a LINE to COLUMNS below three months, because
a line through two points asserts a trend that two points cannot support. A card whose
previous period held nothing says so rather than dividing by zero, and the year-over-year card
reads "first year of data - no year-over-year yet" instead of inventing a comparison.
HasSufficientData is false in all these cases, which is what will let the Dashboard skip a
widget in step 6.

REGISTERED vs OCCURRED is carried on every widget, not once per tab: each states its basis
under the title AND on the X axis, so a screenshot cannot be misread. created_at = office
workload, date_of_birth = population.

CAPTIONS are string.Format over the SAME aggregate the chart was bound to - no service, model
or API is called, and the code says so. That also means a caption can never disagree with the
picture above it.

FOUND IN THE DATA, worth the office's attention: is_delayed is 0 on ALL 14 births, but
DATEDIFF says 12 of them were registered more than five years after the birth. The lag chart
computes its buckets from the dates rather than reading the flag, and the caption states the
disagreement outright - reading the flag would have hidden it.

TWO DEFECTS FOUND BY RENDERING THE REAL FORM, not by compiling. (1) Insight captions were
clipping mid-sentence at 46px; a caption cut off halfway is worse than none, because the
reader cannot tell whether the missing half changes the meaning - caption 46->64px, widget
396px. (2) On a chart whose largest value is 1, the auto Y interval lands on fractions and
formatting them as "0" produced an axis reading 1, 1, 0, 0, 0 - every label a lie.
ChartStyle.WholeNumberY now pins the interval to 1 when the range is small, summing stacked
series per category so a stacked column's axis reaches the top of the STACK.

SPEC ITEM THAT CANNOT BE BUILT AS WRITTEN, reported rather than substituted: Queuing chart 4
says "Transactions per window - bar chart from window_transactions joined to windows".
window_transactions is not a history table. It is the window-to-service CAPABILITY map written
by WindowAssignmentForm (3 rows today: window 2 handles MARRIAGE/NEWREG/PETITION), so joining
it to windows counts what a window is ALLOWED to do, not what it did. The actual per-window
history is queue_tickets.window_no / accepted_window. Not silently swapped - flagged for a
decision before the Queuing tab is built.

ALSO NOTED FOR LATER TABS, from the live data: queue_ticket_forwards is empty (0 rows), so
the "forwarded" series of the ticket-outcomes chart will be flat zero; causes_of_death holds 3
rows one of which is blank and one is "Pinagsasaksak", so leading-causes is unrankable and the
caption must say so; payments has no service-type column, so revenue by service type has to
route through transactions.type; and marriages holds ONE row with church_id NULL, so all five
Marriage widgets will show the empty state.

FILES: new Analytics\ (IAnalyticsWidget, DateRange, AnalyticsData, TimeBuckets, ChartStyle,
SummaryCard, EmptyStatePanel, AnalyticsWidget, DateRangeBar, AnalyticsTab), Analytics\Tabs(BirthAnalyticsTab, PendingAnalyticsTab), Analytics\Widgets\ (5 Birth widgets),
Forms\ReportsAnalyticsForm.cs. ModuleRegistry "reports" repointed and retitled; the sidebar
caption follows. ReportsPsaForm untouched.

VERIFIED, by running rather than by compiling: every query executed against the live croms
database; the real form rendered to a bitmap with the live connection string and inspected -
cards 14 / 2 / 0 / 14, all five charts drawing, the mother's-age empty state showing the exact
spec wording with HasSufficientData=False, and the attendant chart correctly reporting "Based
on 1 of 14 records". MSBuild exit 0, 0 warnings 0 errors, bin\Debug updated (app was closed).

NOT DELIVERED, per the build order: Death, Marriage, Queuing and Certificates tabs, and the
Dashboard widget placement with its 30s cache. Awaiting review of the Birth tab.

2026-09-07 (Reports & Analytics, steps 4-6 - MODULE COMPLETE) - Death, Marriage, Queuing and
Certificates tabs built, plus the Dashboard widget placement. All six tabs live; the PSA report
still rehosted unchanged. Still read-only (SELECT only), still no schema change, still no NuGet
package.

TWO CHARTS WERE SHARED RATHER THAN COPIED, which is what the "one implementation, two
placements" rule is actually for. Birth/Marriage/Death all want the same registered-vs-occurred
dual line, and Death/Marriage both want the same month-by-year heat map. Instead of five near
copies there are two parameterised widgets - RegistrationTrendWidget(table, eventColumn, ...)
and MonthYearHeatmapWidget(table, dateColumn, ...) - and BirthTrendWidget was DELETED and its
tab repointed at the shared one. A bug fixed in either now fixes it everywhere.

HEAT MAPS are DataGridViews with per-cell background colours, per the spec - the charting
library has no heat-map type, and on a registry where a month may hold two records the exact
count has to stay readable in the cell. New Analytics/HeatmapGrid.cs holds the shared styling
and the white-to-accent colour ramp; a zero cell gets its own tone so "none" is visibly
different from "the smallest count here", and zebra striping and the selection tint are
switched OFF because on a heat map colour IS the data.

DEPARTURE FROM THE SPEC, deliberate and flagged on screen. Queuing chart 4 was specified as
"transactions per window from window_transactions joined to windows". That table is not
history - it is the window-to-service CAPABILITY map the window-assignment screen writes (3
rows on this database: window 2 may accept MARRIAGE/NEWREG/PETITION). Charting it counts what
each window is PERMITTED to do and would report identical numbers on a day nobody came in. The
work actually done is on the ticket, so the chart reads queue_tickets.window_no. The tab
carries a permanent note saying exactly that, so an operator comparing the two cannot mistake
it for a bug. (The user was asked and chose window_no.)

JUDGEMENT CALLS WORTH KEEPING, each made because the naive version would have been wrong:
  - Marriage church-vs-civil. The schema has no ceremony-type column and the rule given was
    "church_id IS NULL means civil". True often, not always - a church wedding whose church was
    never picked from the lookup lands there too. So the solemnizer is cross-referenced, and a
    record with no church but a CLERGY officer (Rev/Fr/Pastor/Imam/...) is reported as UNCLEAR
    rather than silently counted as civil. An honest third category beats a clean two-way split
    that is quietly wrong.
  - Leading causes of death is NOT de-duplicated. immediate_cause is free text, so
    "cardiopulmonary arrest" and "CP arrest" are two causes to a GROUP BY. Guessing that two
    free-typed strings mean the same clinical cause is not something a registry should do behind
    the operator's back, so the chart shows what was recorded and the caption says the ranking
    is indicative only.
  - Service time by transaction. One queue number can carry several services but the system
    times the TICKET, so a bundled ticket contributes its whole duration to EVERY service on it.
    Splitting it would need per-service timestamps the schema does not have. Each bar therefore
    means "how long a visit including this service takes", and the caption says so whenever
    bundled tickets are present.
  - Ticket outcomes checks FORWARDED FIRST, because a forwarded ticket is usually completed
    later too and counting it twice would make the columns exceed the tickets issued. Tickets
    issued TODAY that are still open are excluded from "abandoned" - a client still waiting has
    not abandoned anything - and counted separately in the caption.
  - Certificate aging deliberately IGNORES the date-range control, and its basis line says so.
    A request filed eight months ago is exactly the one worth surfacing; filtering the backlog
    by when it started is how a backlog becomes invisible.
  - Marriage under-18 is flagged red, not buried in a percentage: RA 11596 makes such a marriage
    void, so a record showing it is either a data-entry error or something the registrar has to
    act on.
  - Outstanding certificates are counted by the ABSENCE of a releases row, not by
    status = 'Released'. The release row is the fact of the handover; the status is a label
    somebody set. Where they disagree the card says how many.

SCOPE EXTENSION, stated rather than slipped in: "most requested registry years" was specified
over births only. It unions births, marriages and deaths, because the office keeps a book per
registry per year and the question - which book to digitise first - is the same for all three.
It cannot hide a birth year, only add years that would otherwise be missing. On this data it
gives a real answer: 2005 is requested 14 times, and the top three years cover most of demand.

DASHBOARD (step 6). Three compact analytics widgets now sit under a new "Operations insights"
strip: pending/unclaimed aging, most requested registry years, and the average wait trend. They
are the SAME UserControls the tabs host, shrunk by SetCompact - no chart code is duplicated.
Deliberately NOT the queue and collection numbers the KPI cards above already show; these
answer what the cards cannot. A widget whose HasSufficientData is false hides itself, and the
heading hides with them, which is the whole point of that flag.
  CACHE: the widgets run several aggregates each, and RefreshData fires on every navigation to
the Dashboard AND every fourth status-timer tick (~12s). They are behind a 30-second TTL, so at
most one reload per 30s however often RefreshData is called. Verified by reflection: a repeat
call inside the window does not requery, a forced call does.
  LAYOUT: pnlWindows lost its Bottom anchor (it would have stretched straight over the new
strip). It already has AutoScroll, so a fixed height still reaches every window.

FIVE DEFECTS FOUND BY RENDERING THE REAL FORMS, none by compiling:
  1. Control.Visible returns EFFECTIVE visibility - false while any parent is unshown. The
     Dashboard read it back inside the constructor to decide whether to show the strip heading,
     got false for every widget, and left the heading permanently hidden above three live
     charts. Now tracked from HasSufficientData directly. Worth remembering as a shape: never
     read Visible back as a record of what you just set.
  2. A tab-level note set in Build() never appeared, because ReloadAll calls HideBanner on
     entry. HideBanner now restores a persistent note instead of clearing it.
  3. Heat-map label columns clipped to "M.." / "T..." - with 20+ value columns the Fill share
     starved column 0. Now fixed-width and frozen, so labels also survive a sideways scroll.
  4. Turnaround bar tints started at 4-7 days while the caption's threshold was "over a week".
     Now amber for 4-7 (getting slow) and red past a week, matching the sentence.
  5. Marriage year-over-year card compared this year's 0 against last year's 1 and reported a
     "-100%" collapse on a registry that has only ever seen ONE year. The test is now
     COUNT(DISTINCT YEAR(...)) >= 2, which is what "only one year of data exists" meant.

MEASURED against the live database, by rendering each tab with the real connection string:
Birth 14 registered / 2 with a 2026 date of birth; Death 3 records, average age 30, every 2026
chart correctly empty because all three deaths pre-date this year; Marriage 1 record, all five
widgets in their sparse or empty state; Queuing 90 tickets - peak Tue 2pm, average wait 2.2 min
over 10 days, 50 never called and said so, four windows ranked 15/8/3/1; Certificates 37
requests, 23 outstanding, 5 over 30 days, oldest 56 days, mean turnaround 5.4 days.

NOT DONE, and it is not a gap in this module: causes of death, disposal method, mother's age,
attendant, birth weight and spouse ages are unfilled or near-unfilled in the office's data, so
those widgets show the empty state naming the column and the count. That is the designed
output, not a failure - it tells the office exactly what to start capturing.

Build: MSBuild exit 0, 0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk;
bin\Debug updated (app was closed).

2026-09-07 (Dashboard UI rebuild) - DashboardForm rebuilt to spec: nested TableLayoutPanels
instead of absolute points, every colour off UiTheme, and the four Bootstrap literals that used
to sit at the top of the code-behind (Ink/Green/Red/Blue) deleted. Cosmetic and layout only -
no SQL string changed, no schema change, no NuGet package, no image asset. RefreshData, the
IRefreshable contract, statusTimer at 3000ms with its every-4th-tick insights refresh,
WaitingAlert = 10, WireCard/Card_Click Tag navigation, Shell()/Scalar()/ScalarDec() and the
LoadWindowStatus query are all intact.

WHY THE OLD LAYOUT HAD TO GO, stated as the measurable defect rather than as taste: the Designer
positioned everything at fixed points inside ClientSize 771x800, so on a 1920 monitor the whole
dashboard sat in the left 771px with dead grey beside it, and the section titles were free
Labels at absolute Y - any content that grew ran straight over the title beneath it.

NEW UiTheme TOKENS: SuccessTint / WarningTint / DangerTint / Chrome / RowLine, plus a public
Mix(a, b, t). Mix exists so a derived shade is stated as a RELATIONSHIP to the tokens instead of
becoming yet another literal - the pill border the spec gives as #CFE7DA is exactly
Mix(SuccessTint, Success, 0.13), so StatusPill derives its own border from its two colours and
no caller ever names it.

NEW Modules/CardPanel.cs (CardPanel + StatusPill) and Modules/KpiCard.cs. CardPanel paints a
rounded face, hairline border and a two-layer shadow; it is deliberately general, so the other
modules can adopt it as they are restyled. Two things about it are worth keeping:
  - Its own BackColor is the PAGE colour, not the card face. The face is painted inside the
    rounded path, so the corners show the page through instead of four white squares.
  - It sets SupportsTransparentBackColor and its body children use Color.Transparent. Without
    that a Dock=Fill child paints an opaque white rectangle over the rounded corners AND over
    the border, putting the square edge straight back - the exact thing this pass removes.

WHY THE KPI CARD IS PAINTED, NOT ASSEMBLED FROM LABELS. The four cards sit side by side and
their big numbers must share ONE baseline. Collections renders the peso sign and the centavos
at 12.75pt against 24pt digits, so with separate auto-sized Labels that card would sit a few
pixels off from the other three - which is why the old dashboard dropped it to 20F and still
looked misaligned. Painting lets the small glyphs be placed on the SAME baseline, computed from
each font's ascent. Verified in the render: the four values share a baseline.

BUGS FOUND BY RENDERING THE REAL FORM, none by compiling:
  1. THE SPARKLINE WAS INVISIBLE ON EVERY CARD - an off-by-one. The card is 172px, the caption
     ends at y+capH = 135 and the bottom-anchored sparkline starts at exactly 135, so a strict
     "spark.Top > y + capH" was false by one pixel and the whole feature silently drew nothing.
     Now >= (and the value block gives back 2px). This is the shape of bug to remember: a fit
     test between two blocks that are DESIGNED to land flush must not be strict.
  2. The KPI row came out 158px, not the specified 172. The root row was Absolute 172 but the
     card carries a 14px bottom margin inside it, so the card got 172-14. Row is now 186. That
     14px is also what was starving the sparkline in (1).
  3. Refresh and Assign windows drew a hard BLACK 1px box. FlatStyle.Flat draws a black border
     unless BorderSize is cleared - UiTheme.Polish does that, but only once MainForm hosts the
     form, so the buttons looked like boxed system controls until then. Cleared in the Designer.
  4. AT 1366x768 HALF THE SCREEN DISAPPEARED. The content row is the 100% row, so on a short
     screen it collapsed and the service-window board, the mobile card and the backlog list were
     not clipped - they were gone, unreachable. AutoScroll alone does NOT fix this: a Dock=Fill
     child SHRINKS and contributes nothing to the scroll extent. MinimumSize on the child was
     tried and MEASURED DOING NOTHING (VerticalScroll.Visible stayed False); the floor has to be
     stated on the form as AutoScrollMinSize (1100 x 940). Verified after: VScroll visible True,
     scrollable height 940 against a 704 client, and the reference 1400x1010 does NOT scroll.
  5. A zero day drew a 2px stub bar in the mini-bar sparkline, which at that size reads as "one
     registration". A day with nothing registered now draws nothing.

DELTA PILLS are new data, and the rule is that a missing comparison basis HIDES the pill - never
a fabricated zero percent. Waiting compares against the same count two hours ago, reconstructed
honestly from the ticket's own timestamps (issued before that moment, and neither called nor
completed by then) and hidden outright when the office has not been open two hours. Registered
is a percent against yesterday and is hidden when yesterday is zero, because that is no
denominator, not a 100% rise. Collections shows the receipt count behind the figure. Pending
shows how many have sat more than three days. A fall in registrations is drawn NEUTRAL, not red
- the office does not control how many births happen, so an alarm colour there would mislead.

SPARKLINE SERIES are real, never decorative: queue arrivals per hour for the last 8 hours,
registrations per day and collections per day for the last 8 days. The fourth is the one worth
flagging - how many transactions were PENDING on a past day is not recoverable, because the
table stores a status and not its history, so that card charts the AGE PROFILE of the current
backlog by the day each transaction last moved. It is stated as such rather than reconstructed.

TREND CARD is now stacked Birth / Marriage / Death with a legend, not one summed bar - the
office reads "which register is busy", which a single total cannot answer. Same three queries,
same SQL strings, just kept per table instead of summed. Horizontal gridlines only, whole-number
Y axis, and 46px columns at a 90px pitch that shrink together on a narrower monitor so seven
columns always fit rather than overlapping.

NEEDS ATTENTION reads the same tables the Analytics module aggregates. Births flagged delayed is
computed from DATEDIFF, NOT read off births.is_delayed - on this database the flag is 0 on every
row while the dates say otherwise, and reading the flag would hide that. A row whose count is 0
is REMOVED, not rendered as "0"; all three zero reads "Nothing outstanding".

DEVIATIONS FROM THE SPEC, each deliberate:
  - The service-window rows need the current ticket CODE, which the existing LoadWindowStatus
    query does not select. The spec says leave that query untouched, so the codes come from a
    separate small read-only SELECT rather than by editing it.
  - "Assign windows" opens the SETTINGS module, where the windows table is actually managed.
    Deliberately NOT WindowAssignmentForm, which is the login-flow dialog that claims a window
    and can end the session - a Dashboard button must not be able to log somebody out.
  - AnalyticsWidget gained a DrawFrame flag (default true, so the analytics tabs are unchanged),
    set false when the widget is hosted inside a rounded CardPanel. Without it the widget's own
    square hairline rectangle shows straight through the rounded corners. Not duplicated chart
    code - one flag on whether the control paints its own edge.

NOT SOLVED, and it is a real constraint rather than an oversight: at 1010px of content height
the right column cannot hold both a 150px QR card and three 46px backlog rows. Measured - three
rows plus their heading need about 200px and the mobile card cannot go below about 265px. The
mobile panel was tightened 330 -> 270 to get two rows visible; the third scrolls. Above about
1080px of content height all three show.

VERIFIED by rendering the real form against the live croms connection string and looking at it,
at 1400x1010, 1898x1016 and 1344x704 - not by compiling. Four KPI values on one baseline, no
card overlapping another, layout filling at 1920 and scrolling rather than vanishing at 1366.
grep of the two dashboard files for the colour-literal and square-border APIs returns 0 hits in
both. MSBuild Rebuild exit 0, 0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk;
bin\Debug updated (app was closed).

2026-09-07 (OCR preview on the real form) - Reviewed a Codex pass on OcrDigitizationForm
(Documents\Codex\CROMS, 02:25) against this tree. Its IDEA was good and is now in: let the
operator see the reviewed OCR values laid out on the certificate they came off, BEFORE anything
reaches the registry. Its implementation was not shippable and two of its three changes were
rejected outright. The diff was only 92 lines, so each change was judged on its own.

TAKEN, BUT REBUILT: preview the values on the form. Laying values out in their real boxes
catches a class of error the review grid structurally cannot show - a value read into the wrong
row, or a whole block the scan never filled - because the operator can hold the page against the
paper. That is worth having.

WHY THE CODEX VERSION COULD NOT SHIP. It marked the preview by writing a "preview_notice" column
into the DataTable... and NOTHING RENDERS THAT COLUMN. Checked rather than assumed: the built-in
printer draws by def.ReportSections (preview_notice is in no section) via DrawStructuredPage, or
by DrawOverlay, and neither reads it; the dialog title was the plain form name. A Crystal .rpt
would not have the field either. So the page it produced was VISUALLY IDENTICAL TO AN ISSUED
CERTIFICATE, from unsaved OCR data, sitting in a PrintPreviewDialog with a working Print button.
That is precisely the failure this project keeps guarding against - an official-looking document
the registry cannot account for (same reasoning as the 2026-09-06 print-after-auto-fill entry).

WHAT WAS BUILT INSTEAD. New CertificateReport.ShowOcrPreview + a real DrawWatermark, with three
constraints that make it safe to put in front of a front-desk clerk:
  - EVERY page is watermarked, drawn LAST so it lands on top of the content and nothing can hide
    it. An opaque red banner across the top plus a diagonal wash of the same words. The banner is
    opaque on purpose: a light diagonal wash can vanish through a photocopier, which is exactly
    how an unmarked page gets into a file.
  - The office STAMP is never applied to a preview.
  - It is drawn by CROMS's own printer EVEN IF a Crystal .rpt exists for the form. An authored
    .rpt has no watermark field, so routing a preview through Crystal would silently reproduce
    the Codex defect the moment someone authors one.
The preview dialog is also retitled "UNSAVED OCR PREVIEW ... (not a certificate)". Nothing here
reads or writes the registry.

REJECTED 1 - loosening the Auto-Fill guard. Codex changed `btnAutoFill.Enabled = have && !blocked`
to `have && !_result.RescanRecommended`, which is strictly weaker: NeedsManualReview is
`RescanRecommended || Unknown || nothing scored || confidence < 65 || an Invalid CORE field`, so
the change would route a document with an invalid core field into a registration form. Its stated
reason was that the operator needs to reach the form to fix weak fields - but that is already
false here: DgvFields_CellEndEdit calls DocIntelligence.Revalidate and ApplyResultToUi, so
correcting the flagged field LIFTS THE HOLD IN PLACE and Auto-Fill re-enables itself. The guard
removes nothing the operator needs and blocks something they should not do. Left as it was.

REJECTED 2 - forcing the preview into the Auto-Fill path. Codex called ShowOcrPreview
unconditionally inside btnAutoFill_Click, so every auto-fill grew a modal print-preview the
operator has to dismiss before routing. Its new message text also claimed "The Crystal Report
preview ... " when on this deployment there are no .rpt files at all and the built-in renderer is
what appears. The preview is a button the operator presses when they want it, not a toll on the
route they already chose.

SCREEN. btnReport is now live as soon as a form is identified, and SAYS which of its two jobs it
is about to do: "Preview on Form" before commit, "Print Certificate" after. Deliberately NOT
gated on the save - the preview is most useful precisely while the record is still wrong. After
commit it prints the saved registry entry exactly as before. Its no-form and no-scan cases give
distinct messages instead of the old single "commit first".

VERIFIED by rendering the real output, not by compiling. Reflected into the shipped exe, built
the preview row from a realistic reviewed-values dictionary, ran the actual DrawStructuredPage +
DrawWatermark onto a bitmap and looked at it: banner and diagonal wash present, values in their
right sections under the printed item numbers, blanks showing an em-dash so a missing block is
obvious, no stamp. Value mapping checked against all three forms with ZERO unmapped keys -
MF-102-2007 (child/mother/father name aliases land on child_first_name etc.),
MF-97-1993 (10/10 incl. PlaceOfMarriage -> church_name), MF-103-2016 (10/10 incl.
CauseOfDeath -> immediate_cause, MotherMaidenName -> mother_name). office_province=Cagayan and
office_municipality=Penablanca-with-tilde came through the office profile, so migration 26's
encoding fix still holds. NOTE for future harnesses: setting APP_CONFIG_FILE alone is not enough
- without also resetting ConfigurationManager's cached state the profile silently returns its
hard-coded defaults, which is what made Province/Municipality look blank on the first render.

MSBuild Rebuild exit 0, 0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk;
bin\Debug updated. No schema change, no new package. OCR recognition accuracy is untouched by
this pass - it changes what the operator can SEE before committing, not what the engine reads.

2026-09-07 (Death + Marriage printed on their own form) - Asked to "make a Crystal report on
death and marriage, exactly on the form, just like on birth". The premise needed correcting
first, and the correction is the whole shape of this entry.

THERE IS NO CRYSTAL REPORT FOR BIRTH - OR FOR ANYTHING. `find -iname *.rpt` returns nothing;
CROMS\Reports holds only README.md. Authoring one here is still impossible for the reason
measured on 2026-09-06 (in-process RAS returns "Failed to connect to server %MACHINENAME%" on
the free CR-for-VS runtime, and no designer is installed). What makes BIRTH print as an exact
replica is not Crystal at all - it is the OVERLAY renderer: MF-102 (2007) is the one form with
`BlankAsset = "Form102Blank.png"` plus a hand-measured print map, so CROMS draws the blank sheet
and lays the values into its boxes. Death and marriage had `BlankAsset = null` and NO print map,
so they fell to the structured listing. That gap, not Crystal, is what this pass closes.

PRINT MAPS DERIVED FROM THE OCR LAYOUTS. New `FormCatalog.PrintMapFromLayout` builds the print
map from `DocLayouts`, which already states every field as a 0-1 page rectangle - the same
coordinates that tell CROMS where to READ a value say where to PRINT it, so a form that can be
read box by box can be printed box by box with no second set of measurements. Applied to
MF-97 (1993) -> 31 cells, MF-103 (2016) -> 18 cells, and MF-102 (1993) -> mapped as well since
it had the same gap. Page sizes set from each layout's own reference aspect (MF-97 1355x2048 ->
612x925pt, MF-103 706x968 -> 612x839pt, MF-102-1993 1416x2048 -> 612x885pt), all landing on the
long bond these are printed on. Birth 2007 KEEPS its hand map: it was measured against
Form102Blank.png, a different coordinate space from the layout, so re-deriving it would move a
page that is already correct.
  A tick-box field prints its VALUE in the box rather than an X. The layout records where the
answer is READ from, not where each individual choice box sits, so writing "Male" on the sex
line is right and guessing a tick position would put a mark in the wrong box.

ONE SHARED KEY->COLUMN MAP. The extraction-key to report-view-column mapping added yesterday for
the OCR preview was duplicated by the print map, so it was moved into `FormCatalog.ViewColumn`
and the private copies in CertificateReport deleted. The preview and the printer now cannot
disagree about where a value belongs.

OVERLAY NO LONGER REQUIRES A BLANK SCAN. `HasOverlay` was `BlankAsset != null && Cells.Count > 0`
and is now just `Cells.Count > 0`, with a separate `HasBlankForm`. A form that knows its
positions but has no artwork prints the values alone, positioned - which is what printing onto
the office's official pre-printed stock actually needs. `DrawOverlay` already tolerated a null
background, so no change was needed there.

THE CHOICE IS ASKED, NOT ASSUMED. Values-only on plain paper is text floating with no labels, so
silently switching death and marriage to positioned output would have taken away the readable
listing they print today. `ChooseOverlay` asks once, at print time, whether the pre-printed form
is in the printer - defaulting to NO. A form with a blank scan (birth) is never asked.

ALIGNMENT, AND THE MEASUREMENT THAT JUSTIFIES IT. Rendered the death map over the office's own
palogan_n.jpg and the values were visibly displaced. Measured it rather than eyeballing:
  field            form_y  drawn_y   ratio
  Province            168      215    1.280
  RegistryNo          195      247    1.267
  Name                257      345    1.342
  CorpseDisposal      992     1310    1.321
  linear fit: drawn = 1.3289 * form - 8.3
One clean scale with almost no offset - the SAME transform for all eighteen fields. That is not
a broken map, it is a framing difference: the layout is calibrated against a reference scan that
includes the outer border and footer the printable area does not. Which means a single per-form
correction fixes the whole page instead of every field needing a nudge, and that is exactly what
was built. Solving the two-point fit (scaleY 0.7525, offsetY 3.12pt; scaleX 0.9432, offsetX
-33.3pt) and re-rendering put all 18 values on their correct rows - Province on the province
line, the three name cells on the NAME row, MALE on sex, 46 on age, BURIAL on corpse disposal.
Proven end to end, then the test calibration was CLEARED so nothing was left behind on this PC.

NEW Data/PrintCalibration.cs - per-form scale+offset in %APPDATA%\CROMS\print-align.cfg, beside
the server address. Per MACHINE on purpose: it describes this printer and this paper, not the
registry, and two printers rarely agree on where the top of the page is. Never throws; an
unreadable file just means every form falls back to identity, and a zero or negative scale is
rejected rather than collapsing the page onto a point.

NEW Forms/FormAlignForm.cs, reached from Settings -> Certificates & Forms behind the same admin
re-verification as branding (a bad alignment prints onto accountable forms). Offsets in points,
optional stretch, Save / Reset per form. Its point is the ALIGNMENT SHEET: a cross at every
position a value will occupy, each labelled with its field name, printed on the real form so the
drift is directly visible. A printed certificate cannot show this - a blank field leaves no mark,
so the fields most likely to be misplaced are the ones you cannot see. The forms list now also
says which of the three renderings each form uses, and whether it has been aligned on this PC.

MARRIAGE VERIFIED TOO: 31 cells, and the part that matters on MF-97 is right - it is a TWO-COLUMN
table and the husband values land in the husband column, the wife values in the wife column
(RYAN / PANLILIO / MACANANG left, TOFIE FAE / CADAVA / QUILANG right, with each parent, religion,
citizenship and civil status on its own side). Same framing drift, same one-transform fix.

WHAT IS STILL MISSING, and it is one file per form: a scan of the BLANK MF-97 and MF-103 sheets.
With those in CROMS\Assets and `BlankAsset` set, both print as true replicas exactly like birth -
CROMS draws the form itself, no pre-printed stock and no alignment step at all. That was already
flagged as outstanding on 2026-09-06 and remains the single thing blocking it. The filled sample
scans cannot be used: printing another person's certificate underneath is not an option.

VERIFIED by rendering the real output at every step, not by compiling - the derived maps, the
measured drift, the corrected render, and the marriage two-column split. MSBuild exit 0,
0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk (built to a temp OutputPath:
CROMS.exe was running and VS held bin\Debug, so REBUILD IN VS to pick this up). No schema change,
no new package. OCR recognition is untouched.

2026-09-07 (same day, regression caught on screen) - Giving MF-103 and MF-97 a print map set
`HasOverlay = true` on them, and the OCR "Preview on Form" routes through the same
`PrintBuiltIn`. So the death preview came back as the POSITIONED page: values floating on white
with no labels, no artwork, and nothing to check them against. Reported from the running app
with a screenshot, which is the only place it would have shown - it compiles, renders and
watermarks perfectly, it is simply the wrong rendering for that surface.

Two things were wrong at once, and the second is the one worth remembering. Position-only output
is meaningful on exactly ONE surface, the official pre-printed sheet; on screen it cannot be
compared to anything. And `ChooseOverlay` was asking "is the pre-printed form loaded in the
printer?" during a PREVIEW, where nothing is being printed and the question has no answer.

`PrintBuiltIn` and `ChooseOverlay` now take `allowPrePrinted`, and `ShowOcrPreview` passes false:
a form CROMS can actually draw the blank sheet for still previews as the replica, while a form
whose positions exist only for pre-printed paper previews as the labelled listing. No prompt is
raised on a preview at all.

The general shape, since this is the second time a rendering has been routed by a capability
flag rather than by what the OUTPUT IS FOR: `HasOverlay` says a form CAN be printed by position,
not that it SHOULD be for this caller. A print goes to paper the office chooses; a preview goes
to a screen where the operator is checking values against a scan. Same data, same renderer,
different question.

VERIFIED by asserting the routing decision itself rather than only looking at the picture -
`ChooseOverlay(MF-103-2016, allowPrePrinted: false)` returns False with `HasOverlay=True,
HasBlankForm=False` - and then rendering the preview: sectioned page, every value under its
printed item number, blanks as em-dashes, watermark and office header intact. MSBuild exit 0,
0 warnings 0 errors.

2026-09-07 (Print Certificate + View Softcopy folded into one split button) - Birth Registration
carried the two side by side. They are two things done with the SAME saved record, so as
equal-weight buttons they read as two unrelated choices and cost twice the width. Now one
control: printing on the main half, the softcopy under a chevron.

NEW Modules/SplitButton.cs, built as a reusable control rather than a one-off on this form -
the same consolidation applies wherever a record grows a second, rarer action. Owner-drawn to
the same rounded chip UiTheme gives every other button, and tagged "noskin" so UiTheme leaves
the painting alone: its painter has no concept of a surface split in two.

THE ONE THING THAT HAD TO BE RIGHT is that the two halves never both fire - an arrow click that
also printed a certificate would be the worst possible bug in this particular pair. Handled by
NOT delegating to the base on an arrow-zone MouseDown: ButtonBase then never enters its pressed
state and never raises Click, and OnMouseUp is guarded on the same flag so a stray up-event
cannot resurrect it. Asserted rather than assumed - driving the real handlers, a click on the
main half fires Click once and a click on the arrow half fires it zero times.

Details worth keeping: the arrow half tints only on hover, because a permanently darker strip
reads as a disabled section rather than as a second target; the divider and chevron are dropped
entirely when no menu is attached, so the control degrades to an ordinary button; and it clears
to the first OPAQUE ancestor rather than Parent.BackColor, avoiding the dark-halo-in-the-corners
trap that has now been fixed three times in this codebase (UiTheme, the kiosk, here).

"View Softcopy" is DISABLED as the menu opens when there is no scan to show - checked on
`Opening` against the in-memory scan and the loaded record - instead of accepting the click and
answering with a message box. Its handler and the print handler are untouched; only their
presentation changed.

Applied to Birth only. Death's second button is "Print COD + Burial Permit", a genuinely
different document rather than another view of the same one, so folding it under Print would
have hidden a separate legal output; Marriage's softcopy button lives in a modal dialog with no
print beside it. Neither is the same situation.

VERIFIED by rendering the real control in all four states (normal, arrow-hovered, disabled, and
menu-less) and by the click-routing assertion above. MSBuild exit 0, 0 warnings 0 errors (temp
OutputPath - CROMS.exe is running and VS holds bin\Debug, so REBUILD IN VS to see it).

2026-09-07 (blank MF-97 and MF-103 supplied - death and marriage now print as replicas) - The
office supplied the two blank forms as PDFs. That was the ONE missing thing flagged on
2026-09-06 and again earlier today, and with it both certificates now print exactly like birth:
CROMS draws the form itself and lays each value into its own box. The pre-printed-stock path and
its alignment step are no longer needed for either form.

WHAT THE FILES TURNED OUT TO BE, checked before using them: the marriage PDF is a VECTOR blank
MF-97 (Revised January 1993) whose page box is 792x1224pt - the same geometry birth's map was
measured in; the death PDF is a SCANNED blank MF-103 at 612x936pt, which is 8.5 x 13 in, the
long bond these are printed on. Page 2 of the marriage file is the Oath of Solemnizing Officer,
not part of the certificate, so only page 1 was taken. Exported at 150 DPI to
CROMS\Assets\Form97Blank.png (163 KB) and Form103Blank.png (531 KB), registered in the csproj
with CopyToOutputDirectory the same way Form102Blank.png already was.

MEASURED, NOT ESTIMATED - and the two forms had to be measured differently.
  MF-97 carries real text, so every coordinate came from the form's OWN printed labels: the
"(first) (middle initial) (last)" headings give each column's x, and the table's horizontal
rules give each row's band (160.6-211.0 names, 211.0-242.4 date of birth/age, 242.4-273.7 place
of birth, ... 488.0-515.8 mother). Values sit just inside the top of their band so they land in
the box rather than on the rule under it. Worth knowing: the husband/wife NAME row and the
parent rows do NOT share column x (204.7 vs 205/396), so each row uses its own measured columns.
  MF-103 is a scan with no text, so the numbered labels were located by running OCR over the
BLANK sheet and each value placed inside the band its label opens. Less exact by nature, which
is why it was verified by drawing the map back over the blank rather than trusted from numbers.

32 cells for marriage, 20 for death. Both replaced the coordinates derived earlier today from
the OCR layouts - those were a reasonable stand-in while no blank existed, but a map measured
against the actual sheet is strictly better and needs no per-printer correction.

TWO DEFECTS THE RENDER CAUGHT, neither visible from the code:
  1. Death printed BLANK for Residence and Occupation. Not a positioning problem - `deaths`
     stores those as lookup IDS, so they are not in the form's key-to-column map at all and
     ViewColumn returned null. The view already resolves both to names, so they were added to
     the view-only list beside Father/Mother/Cause. Both now print.
  2. A long surname touched the column rule on MF-97 - the surname cells are the narrowest on
     that form. Both surnames start slightly earlier and set one point smaller.

DATA CORRECTION: the blank sheet prints "Revised August 2016"; the catalog said "Revised January
2016". The revision is shown on the certificate and stored with the record, so it now follows
the paper. DocLayouts' comment corrected to match.

A HARNESS TRAP WORTH REMEMBERING, because it looked exactly like a broken map. Rendering the
overlay to a bitmap, every value came out ~33% too far down and right. DrawOverlay sets
`PageUnit = Point`, and GDI+ then applies the 96/72 DPI ratio ON TOP of PageScale - so a harness
asking for 1.7 px/pt actually drew at 2.267. The fix belongs in the harness (PageScale must be
scale * 72/96), NOT in the coordinates; "correcting" the map to compensate would have baked a
33% error into the real printer output, where PageUnit=Point maps correctly to the physical
page. Two earlier renders in this session were misread this way before it was diagnosed.
  Also harness-only: the app finds the blank via AppDomain.BaseDirectory, which in a PowerShell
harness is PowerShell's own folder, so the background silently did not draw at all until it was
loaded explicitly. Confirmed separately that both PNGs DO deploy to the build output.

VERIFIED by drawing each map onto its real blank and reading every field off the page. Marriage:
province, city, registry no, both three-cell names, both ages, both places of birth, citizenship,
residence, religion, civil status, both parents' names, place of marriage, its address line, the
date and time, and the solemnizer above his signature line - all in their own boxes. Death: all
20, including the two that were blank before. MSBuild exit 0, 0 warnings 0 errors across CROMS,
CROMS.Display and CROMS.Kiosk (temp OutputPath - CROMS.exe is running and VS holds bin\Debug, so
REBUILD IN VS). No schema change, no new package.

STILL NOT A CRYSTAL .rpt, and it still cannot be one here (no designer; programmatic creation
proven impossible with this runtime on 2026-09-06). What the office asked for - the certificate
printed exactly on its own form - is what a .rpt would have been authored to produce, and that
is now what all three certificates do. If a .rpt is ever authored it drops into CROMS\Reports
and takes over automatically; nothing else changes.
  Remaining form without a blank: MF-102 (1993). It keeps the derived map and the pre-printed
alignment path, and is now the only entry listed on the alignment screen.

2026-09-08 (Place of Birth split into its three cells) - Reported from the screen: on a birth
scan whose layout is not on file, Place of Birth and Hospital/Facility both showed the SAME
"HUYON HUYON TIGAON CA..." while Municipality and Province read "not found on the page".

ROOT CAUSE, and it explains all three symptoms at once. `ExtractBirth` split the place row on
COMMAS only:
    string[] pp = place.Split(',') ...
    placeHosp = pp[0]; placeMuni = pp[1]; placeProv = pp[2];
The three cells of a Place of Birth are separated on the paper by RULED LINES, not punctuation,
so OCR returns the row as one run of words with no comma anywhere in it. Everything therefore
landed in pp[0]: the facility got the whole string, municipality and province got nothing, and
because the composite Place of Birth is REBUILT from those parts it came back identical to the
facility. Not a recognition failure - the engine had read the place correctly.

FIXED with `DocVocabulary.SplitPlace`, which matches the province from the END of the run,
longest first (so "Davao del Norte" wins over "Norte"), then looks for a municipality in what
is left, and returns the remainder as the facility. Nothing is guessed: a run it cannot
recognise comes back whole in the facility cell, exactly as before.

WHY A BUILT-IN PROVINCE LIST, given this project rejected a name gazetteer on 2026-09-04. The
two are not the same call. That gazetteer was refused because names grow denser as the registry
grows, so every reading gains near-neighbours and repair gets LESS safe over time - it turned a
father into his son. The provinces of the Philippines are a FIXED, public, closed list of 82,
and it is used only to SPLIT text the scan already contains, never to add a province the page
did not say. The office's own `provinces` table holds exactly one row (Cagayan) because that is
where it sits, so without the national list a birth registered anywhere else could not have its
province recognised at all - which is precisely the reported case, a Camarines Sur birth.
Municipalities are NOT built in (there are ~1,600); they come from the office's table, plus a
trailing "... CITY" which is part of a Philippine city's official name rather than an inference.

MEASURED on eight cases through the shipped binary:
  HUYON HUYON TIGAON CAMARINES SUR      -> HUYON HUYON TIGAON | (blank) | Camarines Sur
  CAGAYAN VALLEY MEDICAL CENTER TUGUEGARAO CITY CAGAYAN
                                        -> CAGAYAN VALLEY MEDICAL CENTER | Tuguegarao City | Cagayan
  BICAL PENABLANCA CAGAYAN              -> BICAL | Penablanca | Cagayan
  SAN JOSE DAVAO DEL NORTE              -> SAN JOSE | (blank) | Davao del Norte
  FABELLA HOSPITAL MANILA               -> unchanged, no province (Manila is a city)
  SOME UNKNOWN BARANGAY PLACE           -> unchanged
  HUYON HUYON, TIGAON, CAMARINES SUR    -> HUYON HUYON | TIGAON | CAMARINES SUR
  TIGAON                                -> unchanged
The seventh found a flaw in the first cut: called directly on punctuated text it left the commas
in the facility. That input never reaches it from ExtractBirth (which only calls it when the
comma split found nothing), but a public helper should be right regardless of its caller, so it
now honours punctuation when the text carries it.

STILL BLANK, deliberately: the municipality on the reported scan. "Tigaon" is a Camarines Sur
municipality and this office has two municipalities on file, neither of them that one. Guessing
"the word before the province is the municipality" would be right often and wrong silently, and
a wrong municipality on a civil-registry record is the class of error this project keeps
refusing. It stays blank, flagged, for the operator to type - one word, against a value they can
now actually read.

READABILITY, the other half of what was asked. The Value column was FillWeight 32 against
Check's 27, so a place name was truncated to "HUYON HUYON TIGAON CA..." while the Check column
had room to repeat the same sentence on every row. Value 32 -> 40, Check 27 -> 20, Conf 10 -> 9,
and every value cell now carries its full text as a tooltip - a value the operator cannot finish
reading cannot be compared against the scan at all.

VERIFIED against the built exe by driving SplitPlace over the eight cases above; the office
lookup contents were checked first against the live database (provinces 1, municipalities 2,
hospitals 3, barangays 2), which is what showed the national list was needed. MSBuild exit 0,
0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk, bin\\Debug updated. No schema
change. OCR recognition itself is untouched - this changes how a correctly-read place is
apportioned into the three cells the form has for it.

2026-09-08 (the post-scan pop-up removed) - Reported from the screen: every scan ended with a
modal "Review needed - This document needs a closer look: overall field confidence 53% is below
65%. Correct the flagged fields in the grid ... or send the scan to manual review."

It was not one dialog but FOUR, in an else-if chain that fired after every single OCR run:
"Rescan required", "Unclassified", "Review needed", and "Possible new form layout". On the poor
scans this office actually digitises, one of them fired every time.

WHY THEY WERE WRONG, which is not that the information was wrong - it is that it was already on
the screen behind them. The status line under the grid ends with "MANUAL REVIEW: <reason>" in
amber, the confidence badge is amber and states the number, every flagged row is highlighted
with its own reason in the Check column, and Commit and Auto-Fill are already disabled. The
modal repeated all of it and added one obligation: it had to be DISMISSED before the grid it was
telling the operator to read became reachable. A dialog that must be cleared to reach the thing
it is pointing at is pure interruption.

The one thing genuinely NOT on screen was the long explanation of why a form CROMS recognises
was not read box by box - the doc-class line only carries the short "POSSIBLE NEW FORM/REVISION"
tag. That text was added deliberately on 2026-09-06 and is kept.

FIXED by moving it from push to pull. `ReviewNote(r)` composes the same text the four dialogs
used to show, and the status line now ends with "click for details" and takes a hand cursor
whenever there is something to explain - clicking it shows the full note. Nothing was deleted,
nothing now arrives uninvited, and the suffix only appears when it leads somewhere.

TWO DEFECTS IN MY OWN EDIT, both caught by the compiler and by re-reading rather than by
trusting the patch script:
  1. Cutting the chain by string index left an ORPHANED fourth branch - I had only found three
     dialogs by grepping for the reported title, and `else if (r.LayoutRejected != null)` sat
     past my end marker. It failed to compile as a dangling `else`, which is the good outcome;
     had it been a statement rather than a clause it would have compiled and misbehaved.
  2. The note was assigned AFTER `ApplyResultToUi()`, so on the first render the status line
     would have been built while `_reviewNote` was still empty and the "click for details"
     suffix would never have appeared on the scan that just finished. Moved above it.

VERIFIED: grep confirms none of the four dialog titles remain, and that ReviewNote, the click
handler and the suffix are all present. Compile exit 0, 0 warnings 0 errors.
  NOTE: bin\Debug was NOT updated - a running CROMS.exe (a fresh instance, PID 7244) and VS hold
it, so this was compiled to a temp OutputPath. Close the app and rebuild in VS to see it.

2026-09-08 (unknown-layout path: the other blank fields) - Following the Place of Birth fix,
asked to fix the rest of the fields that came back "not found on the page" on the reported scan.

FIRST, WHICH PATH WAS ACTUALLY BROKEN. The accuracy harness says those fields are FINE: on all
five samples DateOfBirth, TypeOfBirth and Weight read correctly. But the reported scan showed
"POSSIBLE NEW FORM/REVISION", which means its layout was refused and it fell through to the
LABEL path - flat text, no rectangles - and every sample that exercises that path is a marriage.
So the birth label path had effectively never been measured. Captured the real OCR text of two
birth scans and drove `ExtractBirth` directly on it, which turns a 90-second image run into an
instant one and shows exactly what an unknown layout produces:
    nice.jpg, before: 9 of 14 watched fields BLANK
        PlaceOfBirth, Hospital, Municipality, Province, TypeOfBirth, Weight,
        MotherFirst, FatherFirst, RegistryNo
The same scan scores 29/30 through the region path. The gap was entirely the label path.

FOUR CAUSES, all visible in the OCR text once it was in front of me:
  1. TYPE OF BIRTH WAS TIED TO THE WEIGHT. It was read only off the row carrying the birth
     weight, found by `(\\d{3,5})\\s*gram`. This scan reads "2722 grenis", not "2722 grams", so
     the row was never found and Type of Birth came back blank on a form that plainly prints
     "Single". Two fields failing because one word was misread is a coupling to remove, not a
     threshold to tune. Now read from the rows under the "TYPE OF BIRTH" heading, skipping the
     heading's own "(Single, Twin, Triplet, etc.)" hint - which lists every option and would
     otherwise always match the first one.
  2. THE WEIGHT UNIT WAS MATCHED BY SPELLING. Same "grenis" problem. Now matched as a number
     followed by a g-word; the existing 300-8000 g range check is what keeps a stray number out.
  3. TWO ANCHORS DIED ON A SINGLE WRONG LETTER. "MAIDEN" came back as "ADEN" and "PLACE OF" as
     "PLAGE OF", and `@"MAIDEN"` / `@"P.?ACE\\s+OF"` match neither - taking the mother, the
     father and the whole place block down with them. Both now tolerate a lost or swapped
     letter. This is the same failure the 2026-09-02 pass fixed for the numbered anchors
     ("1. NAME" read as "4. NAME"); the word anchors had the same weakness and were missed.
  4. THE PRINTED HINT WAS BEING RETURNED AS A VALUE. On the 1993 scan the place fields held
     "Moves Mo., Gireet, Barangay)" - that is the form's own "(House No., St., Barangay)" hint,
     misread. New guard: these hints are parenthesised on the paper and a written value is not,
     so an UNBALANCED BRACKET gives it away even after OCR has mauled the words. Rejected to
     blank. A blank asks the operator to type it; a plausible-looking wrong value has to be
     noticed first.

MEASURED, before and after, on the same captured text:
    nice.jpg   label path   9 of 14 blank  ->  1 of 14 blank
                            place/hospital/municipality/province all correct,
                            TypeOfBirth Single, Weight 2722, Mother SHEILA, Father GILBERT.
                            The one remaining blank is the handwritten registry number.
    gilvan.jpg label path   7 blank -> 11 blank, and that is the intended direction: the four
                            extra blanks are the place fields that used to hold the misread
                            hint. Nothing real was lost - every one of those values was junk.
NO REGRESSION on the region path, which is what the five samples use: TOTAL 71/131 (54%),
28 wrong, 32 missing - byte-identical to the run taken before the change.

NOT FIXED, and stated plainly: the registry number is handwritten (unchanged, and the
2026-09-06 sweep established it cannot be read); and on the 1993 scan the child's name still
comes back as "@ilvan Yaloso Talosig epnietion" through the label path. That is the recognition
itself on a faint photocopy, not the mapping, and the region path already reads that form
correctly - the label path only runs there when the layout is refused.

VERIFIED by driving ExtractBirth on real captured OCR text before and after, and by re-running
the full ground-truth harness for the regression check. Compile exit 0, 0 warnings 0 errors.
  NOTE: bin\Debug NOT updated - CROMS.exe was running again (PID 26304), so this was compiled
and measured against a temp OutputPath. Close the app and rebuild in VS to pick it up.

2026-09-08 (UI overhaul, module 1 of N: Birth Registration) - The registration screen is
rebuilt on nested TableLayoutPanels with UiTheme tokens and CardPanel, the same system the
Dashboard was rebuilt onto on 2026-09-07. Layout and styling only: not one SQL string, save
path, validation rule or event handler changed, and no schema change or new package.

WHY THIS MODULE FIRST, stated as the measurable defect rather than as taste. Every module
except the Dashboard is still absolutely positioned - measured across the tree:
    QueueManagement    1449x837   anchors=5  docks=0  autoscroll=0
    CertificateRequest 1449x837   anchors=1  docks=0  autoscroll=0
    Transactions       1449x837   anchors=1  docks=0  autoscroll=0
    ReleaseClaim       1449x837   anchors=3  docks=1  autoscroll=0
    FeesPayments       1200x760   anchors=0  docks=3  autoscroll=0
    BirthRegistration  1690x966   anchors=7  docks=0  autoscroll=0
Birth is the worst of them and the most demoed. Its designer asked for 1690x966 - WIDER AND
TALLER THAN A 1366x768 OFFICE PC, which is exactly the machine this app is sold as running on.
The tab block sat at a fixed y=115 with a fixed 460 height and the grid began at y=612, so on a
short host the grid was squeezed toward nothing while everything above it stayed put. Same
shape as Dashboard defect #4: content does not clip, it goes out of reach.

THE HIDDEN-TEXTBOX OVERLAY WAS THE REAL WORK, not the panels. 13 of the form's ~37 textboxes
were never fields at all - they are PLACEHOLDERS whose only job was to supply a pixel rectangle.
BuildLookups laid pick-only comboboxes ON TOP of them (`Location = tb.Location`,
`tb.Left + n * (w + gap)`, caption labels at `tb.Bottom + 2`) and hid the textbox underneath.
That mechanism only works while every field sits at a fixed point, so it pinned the whole form
to one window size - the layout could not be fixed without replacing it.
  New LookupCells() puts the comboboxes IN THE CELL the placeholder occupied: read the cell
position and column span BEFORE removing the control (both are lost with it), build a
TableLayoutPanel of N equal-percent columns plus a caption row, and drop it back at the same
cell with the same span, anchor and margin. Lookup/LookupTriple/LookupQuad are now three
one-line calls into it and TripleCombo is gone; all the `(tb.Width - 2 * gap) / 3` arithmetic
went with them. Measured at three widths, the groups now track their column: Place of Birth
308/308/316 at a 1240 content width and 267/267/274 at 1133.
  The placeholder is KEPT as a hidden child of the host rather than disposed - it still carries
the field's identity for anything referencing it by name, and staying parented means nothing
holding a reference sees a null Parent.
  A placeholder anchored Left ONLY is read as a deliberately narrow field (an age, a birth
order) and keeps its width; one anchored to both sides fills its column. That distinction is
what stops "Age at time of birth" becoming a 380px box.

CENTERING KEPT, ARITHMETIC DROPPED. The old CenterContent() moved and resized six controls by
hand on every Resize and right-aligned four buttons by subtraction. The root is now three
columns - elastic gutter, content, elastic gutter - and the only thing code sets is that
middle column's width. The record action buttons sit in a RightToLeft FlowLayoutPanel, so
btnCertificate (built in code) still lands at the left of the group without anyone computing
`btnNew.Left - 6 - btnCertificate.Width`.

SPARE HEIGHT GOES TO THE LIST, NOT THE CARD - and that was a defect the render caught. With the
entry card on the Percent row it grew to 566px to hold 284px of fields, painting ~250px of
white under the last box while the records grid stayed at its minimum. The tabs hold a fixed
set of boxes; the list holds however many registrations exist. Card is now Absolute 384 (its
tallest tab plus chrome) and the list takes the rest: visible rows went 7 to 14 at 1400x1010.

ALSO FOUND BY RENDERING: the subtitle read "MUNICIPAL FORM 102 - NEW  DELAYED REGISTRATION".
A Label eats "&" as a mnemonic prefix and underlines the following character. Pre-existing, and
invisible in the code - `UseMnemonic = false` on lblSubtitle.

MEASURED at three host sizes, hosted exactly as MainForm does it (TopLevel=false, Dock=Fill in
a content panel, UiTheme.PolishButtons applied), with a sibling-overlap check over every
container and a render inspected per tab:
    1898x1016  content centered at X=329 W=1240, no scrollbars
    1400x1010  no scrollbars, four record actions in order, 14 grid rows
    1150x620   VScroll TRUE and the records card intact at Y=496 H=286 - reachable by
               scrolling instead of vanishing
  0 overlapping siblings at every size; 14 of 14 lookup groups in their cells with their
placeholders hidden. AutoScrollMinSize is set on the FORM (900x800), not on a child: a
Dock=Fill child shrinks and contributes nothing to the scroll extent, the same thing that had
to be established for the Dashboard.

NOT FIXED, flagged rather than silently changed: WireLearningAutocomplete attaches the Learning
Library to txtPlace, which is one of the hidden placeholders - it is replaced by the three
Place of Birth comboboxes, so that attach can neither suggest nor learn anything. Pre-existing
and dead in the old overlay code too. The real fix is to attach to the hospital combobox, which
is a behaviour change and not part of a layout pass. The other six attaches (child and parent
name boxes) are on real visible fields and work.

Build: MSBuild exit 0, 0 warnings 0 errors, bin\Debug updated (app was closed).
Next in the sequence: Death and Marriage registration, then the front-desk trio (Queue,
Certificate Request, Release & Claim), then Fees & Payments and Transactions.

2026-09-09 (UI overhaul, module 2: Certificate Request) - Rebuilt on nested
TableLayoutPanels with UiTheme tokens and CardPanel, following the mockup the office
approved. Layout and presentation only: not one SQL string, save path or validation RULE
changed, no schema change, no new package. Two shipped defects fixed along the way, both
found by RENDERING the form rather than by compiling.

DEFECT 1 - GREY BLOCKS OVER THE CARDS. A CardPanel paints its own face inside a rounded
path, so its BackColor is deliberately the PAGE colour, not the card. Every child label
inherits that and paints an opaque grey rectangle straight over the white face - which is
exactly what the summary column looked like on screen. The class doc already said children
must be Color.Transparent; the first cut of this screen simply did not set it. Every label
and every nested table inside a card is now Transparent. Worth keeping as a shape: on a
CardPanel, an unset child BackColor is a visible bug, not a default.

DEFECT 2 - THE SCREEN ONLY FILLED THE LEFT HALF. Cards sat at fixed x while only the grid
anchored Left|Right, so on the office's 1920 monitor the form hugged the left ~1150px and
left a dead band beside it. The root is now a TableLayoutPanel (header / body / banner /
actions / list) with the body split 64/36, so every card grows with the window and the
column gutter holds at any width. Measured at 1700x980 and 1449x900.

BUG I INTRODUCED AND CAUGHT BY MEASURING, worth remembering: a TableLayoutPanel row's height
INCLUDES the child's margin, so sizing a row to the card it holds clips that card by exactly
the margin - every card came up 14px short and cut its bottom control in half (the name
boxes, the record combo, the whole Next-step callout). Fixed by reading each inner table's
PreferredSize off the running form and setting the row to content + margin: client 154+14,
certificate 210+14, purpose 178+0, summary 409+14. Re-measured after: every card now equals
its content exactly. Estimating these by hand is what produced the wrong numbers twice.

ONE SPACING SCALE, stated in a comment at the top of the Designer so the next screen can
reuse it rather than re-guess: page 24, card padding 20/16/20/18, column gutter 20, card gap
14, field gutter 16, new-field-row 16, label-to-input 5. Everything on the screen is one of
those numbers.

SCREEN. Each section is a card headed by a filled accent badge (StatusPill, reused - no new
control) with the title on the SAME line and a one-line description under it. The Request
summary is a live panel: six caption/value rows mirroring the fields as they are typed, an
entry that is still blank drawn in the faint tone so filled and empty rows are told apart at
a glance, and a NEXT STEP callout with an accent bar whose text changes on whether the
required fields are actually filled - a hint that reads the same in every state is
decoration. Values ellipsize by AutoEllipsis (pixel width), not by a character count, which
had been trimming a name that still fitted. The submit-time "first and last name required"
MessageBox is now an inline banner in an AutoSize row, so it collapses to nothing while
hidden instead of holding a permanent gap. Refresh moved onto the list card as a drawn
circular arrow, tagged "noskin" - UiTheme owner-draws captions with TextRenderer, which
mangles a glyph like the one it replaced. The queue badge and the kiosk photo are designer
controls now (the photo in its own card under the summary) instead of being built in code at
fixed coordinates; EnsurePhotoBox/EnsureQueueRefLabel are gone with them.

VERIFIED by rendering the REAL form off the built exe against the live croms database at
1700x980 (the office's actual content size), 1700x1000 and 1449x900, in the empty, filled,
validation-banner and queue-linked states, and by reading back every control's geometry - no
overlap, no clipping, no scrollbar at the real size. AutoScrollMinSize 1150x950 is what keeps
the list from being crushed on a short screen; at 1040 it forced a scrollbar on a 1080p
monitor, at 880 the list collapsed to 22px. MSBuild exit 0, 0 warnings 0 errors.

KNOWN, not fixed: with the summary at its natural 409 the photo card gets ~130px, so the
kiosk photo shows small - it is an at-a-glance aid here and Release & Claim shows it full
size. CertificatePrintForm (the Find/Print dialog this screen opens) is untouched and still
carries the old Bootstrap literals.

2026-09-09 (UI overhaul, module 3: Release & Claim) - Same treatment as Certificate
Request. This screen was ALREADY on docked TableLayoutPanels (2026-08-04), so the work was
not a structural rebuild: it was putting the screen back on the app's palette, giving the
three columns a readable order, and fixing three real defects that rendering exposed. No SQL,
no save path, no release logic changed; no schema change, no new package.

DEFECT 1 - THE RELEASE BUTTON SHARED PIXELS WITH THE FIELDS PANEL ABOVE IT. In the claim
card, `fields` (Dock=Fill) was added AFTER btnRelease (Dock=Bottom). A docked child added
LATER is laid out FIRST, so Fill claimed the entire content rect (y 6-466) and the button's
strip (y 410-466) sat underneath it - the button stayed clickable only because it is at
z-index 0. The comment on the line even said "fill (add last)", which is backwards; the LEFT
card in the same file gets it right and says "add first". Confirmed by dumping the parent's
children with their bounds, not by reading the code. The rule is now written down at the fix.

DEFECT 2 - "Release  Claim" AND "Claim  Verification". The `&` mnemonic trap, for the third
time in this project (ForceChangePasswordForm 2026-07-19, the kiosk StepIndicator 2026-08-29).
A Label eats `&` as a mnemonic prefix. Fixed with UseMnemonic=false on the page title and on
the Card title. NOT fixed app-wide in UiTheme: its button owner-draw has the same weakness,
but every button caption in the app already escapes it as `&&` (the sidebar, ServerSetupForm,
Marriage), so adding NoPrefix there would render those as literal "&&". This screen's button
captions therefore follow the existing `&&` convention.

DEFECT 3 - THE PRIMARY ACTION WAS BELOW THE FOLD. The right column's two photo boxes were
230px each, stacked, inside an AutoScroll stack, which pushed the content past the card and
put a scrollbar between the officer and the only committing action on the screen. Meanwhile
the loudest control was "Verify by QR".

WHAT CHANGED.
  - Palette: the eight private colour constants now resolve to the UiTheme tokens instead of
    a private Bootstrap set - which is how this screen had drifted off the retinted shell.
    Every remaining literal in the file went with them except the shadow's alpha black.
  - The three columns are numbered 1 Select release / 2 Claimant details / 3 Verify & release,
    with the badge and title on one line (Card gained an optional step; it draws the same
    StatusPill the other screens use - no new control).
  - THE RELEASE BUTTON MOVED to the bottom of step 3, outside the scrolling stack, which is
    both what the mockup shows and what makes the numbering honest: the evidence the officer
    checks and the action that commits it are now in the same column, and the button cannot be
    scrolled away from.
  - The two faces are SIDE BY SIDE, because comparing the uploaded ID against the kiosk photo
    is the officer's actual task and stacked they could not be read together. The pair grows
    into spare height but is capped at 260px - uncapped it filled the column and became two
    large empty frames when nothing was loaded (measured: 450px each).
  - A small line under each photo states "On file" / "No kiosk photo" / "Not yet uploaded",
    and the ID's line carries the name the ID was read as. Present is inked, absent is faint.
  - New "BEFORE RELEASING" list: release selected / claimant name entered / kiosk photo on
    file / uploaded ID on file. These are FACTS the screen already knows, never a verdict -
    CROMS does not match a face to an ID, so nothing on it claims the claimant was verified.
  - Release is now DISABLED until a release is picked. It was live with nothing selected, so
    the click could only ever answer with a message box.
  - The centre column's split went 60/40 to 42/58: the claimant fields are a fixed short set
    while the releases list grows with the day, so the spare height went to the list (8 rows
    visible to 14).

VERIFIED by rendering the REAL form off the built exe against the live croms database at
1700x980, and at 1366x820, in four states - nothing selected, a row selected, the camera
switched on, and after the fix to each defect. With the camera on, the face pair shrinks
220->154 and the Release button stays at its place; the "Verify && Release" caption renders
with its ampersand. Control geometry was read back each time rather than eyeballed. MSBuild
Rebuild exit 0, 0 warnings 0 errors.

NOTE on the harness, worth keeping: DrawToBitmap paints sibling controls in Controls order,
which is front-to-back, so a control that is IN FRONT can be overpainted in the bitmap by one
behind it. That is how the old Release button came out invisible in the first render even
though it was z-index 0 and would have been visible on screen. The overlap it revealed was
real; the invisibility was the harness. Check bounds, not just pixels.

2026-09-09 (Release & Claim rebuilt as a state machine) - The screen now shows ONE state with
ONE action. Built from the mockup the office approved. No SQL string, release rule or audit
write changed; no schema change, no new package.

THE ORGANISING IDEA. The screen already knew the transaction's status (it queried it on every
row click) and simply never used it to decide what to display. Every redundancy on the screen
came from showing all states at once. Root is now a rail (400px: which request) plus a
workspace (what to do about it), and one new method - ApplyState(status) - owns visibility and
the single action button:
  nothing picked      empty state, button disabled
  WaitingToRelease /
  ForPrint            request summary only, amber "Send to Payment"
  ForPayment          summary only, "Open in Fees && Payments"
  ForRelease          claimant, faces, camera, checklist, green "Verify && Release..."
  Released            read-only receipt, "View release details"

REDUNDANCIES REMOVED.
  - _btnResume DELETED. "Resume - Send to Payment" (bottom left, Waiting tab only) and the
    morphing "Send to Payment" (bottom right) were two controls, two captions, two places,
    and both called ResumeSelected(). In the Waiting state the primary button IS that action.
  - ClaimFormForm RETIRED (390 lines, one caller, checked). "Verify by QR" opened a second
    release window with its OWN claimant fields and its own Release button - two code paths to
    the same handover, so two places an audit row could be written differently. The QR entry
    now feeds LoadFromKioskTicket, which already loaded a scanned claim into this screen from
    Queue Management. Unwired from CROMS.csproj; the two files were copied to the session
    scratchpad before removal (they are also in git).
  - Renamed "Verify by QR" to "Scan claim QR" and demoted it from a 48px accent block to a
    sibling of the search box. Scanning identifies a CLAIM; it does not verify a person, and
    this screen must not say it does - CROMS never matches a face to an ID.
  - Three summary tiles became two tab counts plus a footer line. Recent Releases moved out of
    the middle of the screen into that footer, freeing 58% of the old centre column.
  - Rep ID Type / ID Number are hidden until the representative box is ticked. Live for every
    release they read as two more blanks the officer failed to fill in.

NOTHING WAS SACRIFICED. All 25 features survive: tiles (now tab counts), both tabs, search +
clear + queue-number matching, worklist grid, resume-to-payment (now the primary button),
selected header, claimant name, rep checkbox, PH government ID combo (still editable),
ID number, inline validation, release history + double-click details, QR entry, claim status
line, kiosk photo + state line, uploaded ID + the OCR'd name + state line, camera toggle, the
whole camera panel, the 4-item checklist, release button, ReleaseVerifyDialog, PreselectTransaction,
LoadFromKioskTicket, ReleasePickupClaim, RefreshData.

THE BUTTON NOW DOES WHAT ITS CAPTION SAYS. btnRelease_Click re-reads the status (another window
may have moved the request since it was selected) and branches: Released opens the history
instead of releasing twice; ForPayment just follows the request to Fees & Payments rather than
re-stamping a status it already has; parked/awaiting-print sends it to the cashier; only
ForRelease reaches the verify dialog and the INSERT.

FOUR DEFECTS FOUND BY RENDERING THE REAL FORM, none by compiling.
  1. STACKOVERFLOW ON SHOW. A Dock=Fill child inside an AutoScroll panel that ALSO holds
     Dock=Top children oscillates - the scrollbar appears, the display rect shrinks, the Fill
     child resizes, the scrollbar goes away - and WinForms resolves that by recursing until the
     process dies. Nothing in the body is Dock=Fill now. The same trap applies to an AutoSize
     TableLayoutPanel holding Dock=Fill children (its height depends on them, theirs on it), so
     the claim block states its own height from its row styles instead - SizeClaimBlock().
  2. THE CLAIM BLOCK STAYED VISIBLE WITH NOTHING SELECTED. ApplyState hid it only inside the
     "picked" branch and returned early before reaching it, so live claimant fields, both face
     panes and the checklist sat on screen for a release nobody had chosen - the exact
     "looks ready, nothing behind it" state this rebuild exists to remove.
  3. THE EXPLANATORY NOTE RENDERED UNDER THE CLAIM BLOCK, 600px below the state it described.
     Dock=Top order follows z-order, but a control that was HIDDEN when the panel last laid out
     does not reclaim its place on its own. OrderBody() re-asserts the order after every
     visibility change.
  4. THE PAGE SUBTITLE WAS CLIPPED and the two title labels overlapped - a 19pt title renders
     ~40px tall while the subtitle sat at y=36. Caught by a sibling-overlap sweep over every
     container, not by eye.
  Also: the worklist could not be read. Four columns shared 294px, so the header "Txn Code"
  wrapped onto two lines and every code rendered "TXN-2026-...". Rail widened to 400, Type
  dropped (the workspace header carries it the moment a row is clicked), remaining columns
  weighted. Full codes and full dates now fit.

A HARNESS TRAP WORTH KEEPING. The render harness died with a StackOverflowException that looked
exactly like a layout loop, and two rounds of layout "fixes" were made against it before it was
bisected properly. It was the HARNESS: poking CROMS.Data.Session by reflection from PowerShell
overflows in that host. Bisect (skip-hooks in the ctor, one builder at a time) rather than
reason about WinForms layout from symptoms - the first two diagnoses were both wrong.

VERIFIED by rendering the real form off the built exe against the live croms database at
1400x1010 and reading geometry back, not by compiling: empty / ForRelease / representative /
ForPrint / history. Confirmed per state that the claim block, note, summary and empty block
show only where they should; that ticking the representative box opens a 102px ID panel and
unticking closes it; that the button reads "Verify && Release..." green, "Send to Payment"
amber, and disabled when nothing is picked; that history hides the tabs and the search row and
flips the footer to "Back to the worklist" over 14 released rows. ZERO overlapping sibling
pairs at that size. Close/dispose path exercised separately and is clean.

STILL OPEN, unchanged by this pass and both needing a migration rather than layout: `releases`
records who claimed and their representative ID but NOT how identity was verified, so the
receipt cannot state it; and the class doc still claims a signature is captured on release when
no signature control and no signature column exist.

Build: MSBuild compile clean, 0 errors 0 warnings. bin\Debug was NOT written - CROMS.exe is
running (PID 4196) and VS 2019 holds it - so the verification above was measured against a
freshly compiled temp OutputPath. CLOSE THE APP AND REBUILD IN VS to pick this up.

2026-09-09 (Publish now LINKS the share instead of copying it) - "Publish New Release" creates
JUNCTIONS from C:\CROMSRelease\{Main,Kiosk,Display} to the three build outputs, so the share
always serves whatever Visual Studio last built. Publish once; every later rebuild reaches the
clients on its own.

WHY THE FIRST JUNCTION ATTEMPT FAILED, since the code carried a comment asserting junctions are
impossible here ("a junction into OneDrive/profile is blocked -> access denied") and that
comment is what kept the copy in place. A junction is a REPARSE POINT: SMB resolves it on the
server and then access-checks the TARGET. The old code granted Everyone read on
C:\CROMSRelease, which for a junction grants nothing at all - the folder actually being opened
is ...\OneDrive\...\bin\Debug, and MEASURED, that folder's ACL is SYSTEM / Administrators /
the owner and nobody else. So the share account could not open it and Windows answered
"access denied", which read as "junctions don't work" rather than "the target has no ACL".
  The fix is one line per app: grant read on the SOURCE as well. Intermediate profile
directories do NOT need grants - Everyone holds SeChangeNotifyPrivilege (bypass traverse
checking) by default, so only the final folder's ACL is evaluated. That is why granting three
bin\Debug folders is enough and the user's whole profile is not exposed.

PROVEN BEFORE SHIPPING, not reasoned about - the previous attempt was reverted on a wrong
diagnosis, so this one was measured end to end:
  - built the exact command sequence LinkLines emits and ran it (junctions and icacls on
    folders you own need no elevation; only `net share` does, and it already exists);
  - all three entries came back as real reparse points, and the copy fallback never fired;
  - read every app's exe over SMB AS cromsshare, from this same machine via the OTHER local
    IP (192.168.137.1) so a second credential session was allowed alongside my own - a clean
    way to test as the share account without a second PC. Main, Kiosk and Display all read.
  - AUTO-REFRESH proven directly: rebuilt CROMS.Display to bin\Debug (local exe 4:39:45 pm,
    was 3:59 pm) and the share served 04:39 pm over SMB with NO re-publish.

THE TRADE-OFF, stated rather than buried: a junction exposes the LIVE build folder, so a client
updating in the middle of a rebuild can pull a half-written exe. That is the price of never
re-publishing. The batch keeps a COPY FALLBACK - if mklink fails for any reason it xcopies
instead, so a failure degrades to the old behaviour rather than leaving an empty share, and the
success dialog says which of the two actually landed.

ALSO FIXED IN THE SAME PASS: Publish reported success on `Directory.Exists(Main)`, which a
FAILED mklink satisfies - it leaves an empty directory. It now checks for the EXE, so an empty
share can no longer be reported as a published one. Everyone is written as the SID *S-1-1-0
because the literal name is localised and would silently fail to match on a non-English
Windows. The Settings help text no longer says "do this after every rebuild".

THE ORIGINAL COMPLAINT ("what I publish cannot be found on the other laptop") IS A DIFFERENT
BUG AND IS NOT FIXED BY THIS. Publish was working - the share resolved locally with all three
exes, share ACL Everyone Read, NTFS Everyone Read, SMB2 up, port 445 listening, firewall
allowing SMB inbound on all three profiles. The client cannot AUTHENTICATE: reading a Windows
share needs a session, `Everyone` excludes anonymous, and the field note of 2026-08-27 records
laptop B still running the 2026-08-03 build - while EnsureShareConnection, the silent `net use`,
was added 2026-08-04, one day later. So that laptop has no auto-connect and only ever worked
because someone ran `net use` by hand; the account confirms it, cromsshare LastLogon = 27 Aug
2026 and nothing since, while this PC's Wi-Fi IP has changed. It is a chicken-and-egg: the
client needs the update to get the auto-connect code and cannot reach the share to fetch it.
One manual `net use` on that laptop breaks the loop, after which it is self-healing.
  NOTED AND NOT CHANGED, because it is an account decision: cromsshare's password expires
11 Sep 2026. When it does, every client's silent auto-connect fails and they all report
"share not found" again.

Build: MSBuild compile clean, 0 errors 0 warnings. bin\Debug for the main app was NOT written -
CROMS.exe is running and VS holds it - so this is compiled to a temp OutputPath. CROMS.Display
built normally. REBUILD IN VS, then click Publish ONCE to convert the share to links.

2026-09-09 (cromsshare password set to never expire) - The share account's password was due to
expire 11 Sep 2026 (set 31 Jul, so a 42-day maximum-password-age policy). On that date every
client's silent `net use` would have failed and all of them would have reported "Update share
not found" with nothing obviously changed - the worst kind of failure to diagnose later.

Set PasswordNeverExpires on the account, and account expiry to Never while there. Needed
elevation, so it ran through one UAC prompt.
  CORRECTION to the command given earlier in this session: `net user <u> /expires:never` sets
ACCOUNT expiry, which is a different flag from password expiry and would NOT have fixed this.
The password lever is Set-LocalUser -PasswordNeverExpires (or WMI PasswordExpires=false).

VERIFIED two independent ways rather than trusting the exit code - Get-LocalUser reports
PasswordExpires and AccountExpires both empty, and `net user cromsshare` reports "Password
expires: Never" / "Account expires: Never". Then re-authenticated to the share as cromsshare
over SMB and read CROMS.exe through the junction, to confirm nothing about the login broke;
the test session was removed afterwards.
  "Password required" is still Yes, which matters: a blank-password local account is refused
network logon by Windows default policy, so clearing the password instead of its expiry would
have broken the share permanently.

STANDING CAUTION, unchanged: Croms#2026 is a default that appears in App.config and in this
log, and it now never rotates on its own. Change it before real deployment - and remember it
lives in TWO places, the account itself and ReleaseSharePassword in each client's App.config.

2026-09-09 (the two Ionic apps read like the desktop now) - ORCMobile_Application (the
certificate scanner) and claimapp (the ID-upload app) were still running the OCR pipeline of
2026-07-28: a port of DocumentAI.cs as it stood BEFORE every fix since. Everything the desktop
learned from the office's real scans - orientation correction, word boxes, the two-column
marriage reader, the anchor-tolerance fixes, per-field confidence, validation, the review hold
- was missing from both. This pass brings them up to date. Both are separate repos
(C:\Users\ivan palogan\ORCMobile_Application and C:\Users\ivan palogan\claimapp); no CROMS
desktop code, no schema and no save-API endpoint changed, and no new npm package was added.

MEASURED, against the office's own five scans and the SAME ground truth the desktop harness
uses (CROMS.DocTest/Truth.cs), on the keys the phone actually produces:
  before  13/64 = 20% exact field values, 13 wrong, 38 missing   (app as it was)
  after   19/64 = 30% exact field values,  8 wrong, 37 missing
  per document (after): birth 2007 14/21, birth 1993 1/14, death 4/11, marriage 0/9, second
  marriage photo 0/9; all five classified correctly (3 of 5 before the preprocessing fix).
  WRONG values falling 13 to 8 is the number that matters for a registry.
The harness runs tesseract.js in Node over copies of the samples preprocessed exactly as the
phone preprocesses them (a numpy port of the same Bradley threshold), because the app's own
preprocessing needs a browser canvas. It lives in the session scratchpad, not in either repo.

FIVE THINGS CHANGED THE NUMBERS, each measured before it was kept.
  1. RESOLUTION. The phone capped large pages and only upscaled very small ones, so a 2048px
     capture was read at 2048 while the desktop reads the same page at 2400. Matching the
     desktop's Scale() - to the target long side in BOTH directions - was worth more than any
     parsing change in this pass.
  2. A SECOND PAGE-SEG PASS, and this is the finding worth keeping. Page-seg mode 3 (fully
     automatic) SILENTLY DROPS whole typewriter rows inside these bordered PSA tables: on
     nice.jpg the date of birth, the place of birth and the entire father block were not
     misread, they were ABSENT from its text. Mode 4 (one column of variable-size text) reads
     them, at slightly higher confidence (65 vs 64). So a second pass at mode 4 runs whenever
     a field that MATTERS came back empty, and its values fill the blanks of the better-scoring
     pass. This is the phone's answer to the desktop's per-field region reading, which is far
     too slow to run in WASM.
  3. THE ANCHOR-TOLERANCE FIXES from 2026-09-02 and 2026-09-08, which the phone had never
     received: "MAIDEN" read as "ADEN", "14. NAME" as "14, NAME", the numbered anchor falling
     back to the row's own "(First) (Middle) (Last)" bracket pattern, Type of Birth decoupled
     from the weight row, the weight unit matched as "a number followed by a g-word", the
     parenthesised printed hint rejected by its unbalanced bracket, and the informant rejected
     when it reads like a label. On nice.jpg these alone recovered the father's three name
     cells and his occupation.
  4. THE TWO-COLUMN MARRIAGE READER. Flat text cannot say which spouse a value belongs to -
     the husband's and the wife's values for one numbered field sit side by side on ONE printed
     line - so the phone now returns every word WITH ITS BOX (tesseract.js blocks/words, ML Kit
     element boundingBox) and the marriage extractor cuts each row into label / husband / wife
     columns using the page's own HUSBAND and WIFE headings as the ruler.
  5. PLACE OF BIRTH split into its three cells by the closed, public list of Philippine
     provinces (new doc-vocabulary.ts), the same fix the desktop got on 2026-09-08. The
     office's own name gazetteer is deliberately NOT ported: it needs MySQL, which a phone
     cannot reach, and the desktop already measured that name repair gets less safe as the
     registry grows.

REGRESSION I CAUSED AND CAUGHT BY MEASURING: filling blanks from the second pass imported the
marriage form's certification paragraph into the spouse name cells - "CZEATIFY", "CERTIFY
TRAT", "FURTHER AT" - and wrong values went 7 to 10. MF-97 prints that paragraph in the same
capitals as the names, so an ALL-CAPS filter alone cannot tell them apart. Name cells now
refuse the form's printed sentences and its function words, and a place value now refuses the
form identifying ITSELF (its Municipal Form number and printing instruction, the certificate's
own title, and its printed field captions). Wrong values fell back to 8.

NEW, ported from the desktop: Data/DocIntelligence.cs becomes doc-intelligence.service.ts -
per-field confidence from the words that actually produced the value (a value traceable to no
word was DERIVED, not read, and scores 50 rather than a flattering 100), the conservative
character corrections, the date correction WITH the 2026-09-04 fabrication guard (a loose parse
fills what the string leaves out from TODAY'S date, so a field reading only "February" came
back as this year's 1 February - a date that appears nowhere on the certificate), and every
validation rule including the cross-field ones: child's surname matching neither parent,
mother's maiden surname equal to the father's, husband and wife reading as the same name, age
contradicting date of death. Registry numbers accept 1-6 digits after the dash, matching the
desktop's 2026-09-06 correction. Verdicts are Ok / Uncertain / Missing / Invalid / Conflict,
and the document is HELD for manual review on the same rules the desktop uses.

ORIENTATION. Tesseract's own OSD detector now runs on the phone too (tesseract.js exposes it,
but only with the legacy core loaded, so it gets its own worker and a device that cannot load
it simply reads the page as given). Trusted at confidence >= 1.0, exactly as measured on the
desktop. DEVIATION, stated: the desktop's four-angle recognition probe is NOT run before every
scan here - it costs four extra passes in WASM - it runs only after a page has already read
badly, which is the case it exists for.

SCREEN. The review page now shows each field's own confidence and status, the rule it broke,
and "auto-corrected from ..." when a reading was repaired; editing a value re-runs the rules
immediately, so correcting a flagged field can lift the review hold without another OCR pass.
A page held for review says why, once, in one banner - and still goes to the review screen,
because that is where the flag gets cleared. Only an unreadable IMAGE stops the flow, since no
amount of correcting fixes a photo that has to be retaken. Saving with fields still flagged
now asks first and names them.

CLAIMAPP. It shares the same upgraded ocr.service.ts (byte-identical file), and its ID name
reader was rewritten as id-extract.service.ts. The old one took "the longest line of letters"
when it found no name label - and on a Philippine ID the longest line of letters is "REPUBLIC
OF THE PHILIPPINES" or the issuing agency, so the claimant's name arrived pre-filled with the
government's name and looked like a successful read. It now reads the card's own name captions
in BOTH languages (the PhilID prints "Apelyido/Last Name"), falls back to the biggest printed
text on the card by word HEIGHT, refuses the card's own words, and otherwise returns blank
saying so.
  TWO BUGS IN MY OWN WORK, both caught by the test and not by the compiler: stripping the
caption only up to the first match left the words "Last Name" as the surname on a bilingual
card; and the same /g/ regex used for both replace() and test() carries a lastIndex between
calls, which would have skipped matches on every other call - an intermittently wrong answer,
which is worse than a consistently wrong one. Measured: 3/3 cases correct, against the old
code returning "REPUBLIC OF THE PHILIPPINES" and "NON-PROFESSIONAL DRIVERS LICENSE" as names.

NOT PORTED, and it cannot sensibly be: the desktop's REGION reading (DocLayouts, PageFit, the
printed-rule grid). It crops each field's own rectangle and reads it several ways - dozens of
recognition passes per page, seconds each in WASM. The phone uses the label path, which is the
same code the desktop itself falls back to when a form's layout is not on file, so a phone scan
yields blanks where the desktop yields values. Also not ported: the office vocabulary and the
Learning Library (no database from a phone), and the ocr_field_audit trail (the save-API has no
column for it). Handwriting is still not read anywhere - registry numbers and informant lines
come back blank and flagged for typing in.

STILL WRONG, plainly: both marriage photographs. They read at 39-48% - typewriter print on
textured security paper, the second one perspective-skewed - and the values come out several
characters off ("OILEEWT" for GILBERT). That is the recognition itself, not the mapping, and
the desktop records the same limit on the same two files. Both are held for manual review.

Build: ng build clean on both apps (the two NG8113 warnings in ORCMobile are pre-existing
unused Ionic imports in the scan template, untouched here).

2026-09-09 (live camera guide put back on the scan screen) - The green/red document
guide was still fully BUILT and completely unreachable: scan.page.ts had the whole
machinery - getUserMedia with continuous focus, the 150 ms analysis loop, the
perspective-aware outline drawn on the paper's four real corners, the status pill, the
torch, the camera picker, and auto-capture after ~1.2 s of steady green - but the
TEMPLATE had been reduced to two file-input buttons, so none of it could run. This
restores the interface. No analysis, capture or OCR logic changed.

WHY IT HAD BEEN REMOVED, and why the fallback stays. getUserMedia is refused outright
on an insecure origin, and this app is opened from the QR at http://192.168.x.x:4200 -
so on the office's phones the live camera cannot start at all. The native camera
file-input needs no secure context, which is why it was put there. Both are now
offered: "Open live camera" first, "Take Photo of Document" and "Choose from gallery"
under it, and a new `liveCameraAvailable` getter states the http:// limitation ON THE
SCREEN instead of letting the operator find it as a permission error after tapping.
Either path feeds the identical detect -> de-warp -> enhance -> OCR pipeline.

The camera is opened ON DEMAND rather than when the page loads: a screen that grabs
the camera on entry asks for permission before the operator has said they want it.
New closeCamera() puts it down without leaving the page.

VERIFIED IN THE RUNNING APP, not by compiling. The Browser pane has no camera, so
navigator.mediaDevices.getUserMedia was replaced IN THE PAGE with a canvas
captureStream drawing a synthetic document - the app's own loop then ran on those
frames with no app code changed:
  square-on, filling the frame  -> stage class "stage ok", pill rgb(22,163,74),
                                   "Perfect! Document ready to scan.", green outline
                                   with green corner handles; and on the first run it
                                   AUTO-CAPTURED, de-warped and moved to the captured
                                   view on its own.
  tilted page                    -> stage class "stage bad", pill rgb(220,38,38), red
                                   outline tracking the four tilted corners (373,065
                                   overlay pixels drawn), no auto-capture.
  small/dark page                -> red, "Too dark - improve lighting.", no outline
                                   (no document found is the correct answer, not a
                                   guessed rectangle).
Worth knowing for anyone testing with synthetic frames: the analyser checks brightness
and glare BEFORE angle, so a too-white paper reports "Reduce glare or shadows" rather
than the angle message. That is its precedence, not a defect - it was reproduced twice
while building the test frames.

Build: ng build clean, 0 errors 0 warnings (the two NG8113 "IonSelect is not used"
warnings are gone as well - the camera picker uses them again).

2026-09-09 (the scanner is served over HTTPS, so the live camera can actually run) -
Reported from the phone: "Camera blocked: open the app over HTTPS (https://...:4200)".
That message was correct and the app was behaving as designed - browsers refuse
getUserMedia on an insecure origin, and the scanner was served at
http://192.168.x.x:4200, so the green/red framing guide restored earlier today could
never start on a real phone. Fixed by serving the mobile app over TLS.

WHAT WAS ALREADY THERE. IonicServerManager was written for this: it takes the URL
scheme from App.config per instance (`MobileScheme`, `ClaimAppScheme`) and its own
comment says "Mobile=https for its --ssl camera server". The defaults were both http
and no certificate existed, so the intent had never been wired up.

CHANGED, four small things:
  * ORCMobile_Application/ssl/ - a self-signed development certificate (RSA 2048, ten
    years) whose Subject Alternative Names cover localhost, 127.0.0.1 and both of this
    PC's LAN addresses, plus `make-cert.sh` to regenerate it (it auto-detects every
    IPv4 the machine has, so the usual case takes no arguments) and a README stating
    plainly what it is and what the phone will show.
  * angular.json `serve.options`: ssl + sslCert + sslKey, so `ng serve` for this app
    is HTTPS however it is launched - by CROMS, by a developer, or by the QR. The
    desktop's serve COMMAND needs no new flags, which keeps the two configs from
    disagreeing.
  * App.config `MobileScheme` -> https, with the reasoning and the way back written
    beside it.
  * DashboardForm now appends "The phone will warn once - tap Advanced, then Proceed."
    to the connection hint whenever the scheme is https. A self-signed certificate
    means one browser warning per phone, and a staff member who has not been told
    that concludes the app is broken rather than tapping through it.

THE TRAP THIS WOULD HAVE WALKED INTO, found by reading the config path rather than by
running it. The QR carries `?sid=&host=<LanIp>&api=3000`, and ConfigService turned
`host` into an ABSOLUTE `http://<ip>:3000` API base. From an https:// page that is
mixed content: the browser blocks it outright and every save from the phone fails.
The browser never needed it - the dev server proxies /api to the save-API, which is
what the service's own comments say it relies on - so the host/api pair is now applied
only when running as the INSTALLED app, which has no proxy to fall back on. The
WebSocket helper already chose wss:// on an https page, so nothing else was exposed.

VERIFIED by running it, not by reading it: the dev server comes up announcing
https://192.168.1.234:4200 and https://192.168.137.1:4200; `openssl s_client` shows
the served certificate carrying both of those addresses; over TLS the index and the
/scan route both return 200, plain http on the same port is refused, and - the one
that matters for saving - `https://127.0.0.1:4200/api/health` returns 200, so the
proxy still reaches the save-API with no mixed content.
  The in-app Browser pane cannot be used to view it: it refuses an untrusted
certificate outright with no way to accept it. That is a limitation of that pane, not
of the phone, which offers Advanced -> Proceed.

CLAIMAPP IS DELIBERATELY LEFT ON HTTP, and this is a decision worth stating rather
than a thing forgotten. It also calls getUserMedia twice (the QR scan on the home page
and the ID capture), so those two paths stay unavailable there - but claimapp is
opened by CITIZENS, not staff, and teaching the public to tap through a security
warning on a government ID-upload page is a worse outcome than losing the in-page
camera. Both of its camera paths already have working fallbacks: the claim token can
be typed, and the ID photo uses the phone's own camera app through a file input.
Flipping it needs one App.config key (`ClaimAppScheme`) and a certificate generated
the same way.

NO WARNING AT ALL, if that is wanted: install the app (`npx cap sync android`), whose
native WebView is a secure context, or for one test phone on USB run
`adb reverse tcp:4200 tcp:4200` and open http://localhost:4200 - browsers treat
localhost as secure. Both are in ssl/README.md.

Build: MSBuild exit 0 on CROMS (built to a temp OutputPath - CROMS.exe was running -
so REBUILD IN VS to pick it up), `ng build` clean on the mobile app. No schema change,
no new package. Note: starting the HTTPS server required killing the ionic serve the
running CROMS.exe had started on 4200; CROMS starts its own again, and it will now be
HTTPS.

2026-09-10 (the bottom of the birth certificate is now scanned) — Reported: Intelligent
Document Processing read the top two thirds of Municipal Form 102 and stopped. The
certification blocks the office actually signs — 19b/21b CERTIFICATION OF ATTENDANT AT BIRTH,
20/22 CERTIFICATION OF INFORMANT, 21/23 PREPARED BY, 22/24 RECEIVED AT THE OFFICE OF THE CIVIL
REGISTRAR, and 25 REGISTERED AT THE CIVIL REGISTRAR on the 2007 sheet — were not fields at all.
Of that whole block the layout defined exactly ONE region (the attendant tick row) and the
registry could hold nine of the values; nothing read them.

WHAT WAS ADDED. 17 new regions on MF-102 (2007) and 14 on MF-102 (1993), all Core=false so a
junk reading in a signature block cannot hold up a sound certificate. Keys: AttendantName /
Title / Address / Date, Informant / Relationship / Address / Date, PreparedByName / Title /
Date, ReceivedByName / Title / Date, and (2007 only) RegisteredByName / Title / Date. The 1993
revision folds item 25 into its own item 22, so it carries no RegisteredBy* fields — a form
that does not have a row does not get one.

MEASURED, NOT ESTIMATED, and the first cut was wrong in an instructive way. New DocTest mode
--words <y> dumps every recognised word with its NORMALISED box, which is the same template
space DocLayouts states its rectangles in (nice.jpg IS the 1528x2048 reference scan, so page
coordinates and template coordinates are the same numbers). The first pass placed each region
beside its printed caption. That is not where the values are: the typist strikes each value on
the ruled line ABOVE its caption, so "EVANGELINE L. BARSABAL" sits on the Signature line and
"ADMINISTRATIVE AIDE III" on the Name-in-Print line. Reading it back off the whole-page dump
was ambiguous; cropping each block out of the scan and LOOKING at it at 2x settled every row in
one pass. Result on the reference scan: Received By went "ADMIN STRATIVE" (the title, read into
the name) to "BARSABAL"; Registered By went "don" to "CARULYNU MALLILUIN"; Attendant Title went
"Medical" 50% to "Medical III" 94%.

A SECOND MEASUREMENT REVERSED PART OF THE FIRST, which is worth keeping. Tightening the bands
onto the measured text (h 0.011) made the TITLES better and the NAMES worse — the informant name
collapsed from "SHEILA SALOSIG" to "SH". The region reader needs vertical air around a line, not
a tight crop, so the final coordinates keep the measured centres with about 0.016 of height. The
four informant regions are back at their wider first-cut values because they measurably read
better there; everything else keeps the re-measured position.

TWO DATE DEFECTS FIXED, one of them a fabrication risk.
  (1) NormaliseDate required a SPACE between day and month and a four-digit year, so
"13-Jun-18" — which is how an office types a date, and what these blocks actually carry — was
refused outright and the field came back blank while the scan plainly showed a date. Separator
now accepts space/hyphen/dot/slash, and a two-digit year is expanded through the same
hundred-year window the typist's own software used to print it. Both certification dates on the
reference scan now read 2018-06-13.
  (2) The expansion is ONLY safe where a named month has already fixed the order, so the commit
path now accepts yyyy-MM-dd and nothing else. The counter-example is on the same certificate:
the "JUN 2 6 2018" stamp reads back as "2-6-2018", and a loose DateTime.TryParse turns that into
6 February — a date the certificate never carried, written into a civil-registry record. Same
shape as the CorrectDate fabrication fixed on 2026-09-04. An ambiguous reading now stays NULL in
the column and stays VISIBLE in the grid for the operator to type.

DATABASE. Migration 28_certification_block.sql (applied to the live croms DB, idempotent):
`births` gains attendant_date, prepared_by_title/date, received_by_title/date, and
registered_by/title/date. `births` already held the attendant name/title/address and the whole
informant block since 04_birth_form102.sql; what it had nowhere to put was the DATE each
certification was signed and the TITLE of the two office signatories — which is most of what
those blocks say, and what an audit of a delayed registration turns on. Nothing is backfilled:
a signature date invented for the rows that predate this migration would be a fabricated fact in
a government record. v_birth_certificate restated to carry them (71 columns), keeping
is_delayed and adding date_registered, which migration 27 created but never exposed.

EVERYTHING DOWNSTREAM FOLLOWS THE CATALOG, so this is one form entry rather than edits in five
places: FormCatalog's Columns map routes each new key to its column; the review grid groups them
under the certificate's own headings in printed order (verified off the built exe — six new
sections, correct order, and an empty section is skipped so a 1993 scan simply has no item-25
group); ReportSections print them; and the MF-102 print map gained six cells.

SCREEN + RECORD. Birth Registration gained the eight controls the new columns need — attendant
Date Signed on the Attendant tab, and prepared/received title+date plus the whole registered-by
row on the Certification tab. Every new date picker uses ShowCheckBox: a plain DateTimePicker
can only ever say "some date", which on a certification block would invent a signing date for
every record that never carried one, so an unticked box means the sheet states none and the
column stays NULL. Auto-Fill maps all of the new keys across, including the attendant and
informant address triples and the informant relationship through the same Master-File
persistence path the other lookups use.

MEASURED RESULT, and stated honestly. On the 2007 reference scan the certification block now
reads: attendant date 2018-06-13 (92%), attendant title Medical III (94%), attendant name MARIE
DELA CRUZMD (70%), informant SHEILA SALOSIG (49%), informant address Penablanca, Cagayan (62%),
informant date 2018-06-13 (81%), received by BARSABAL (58%), received title AIDE U (69%),
registered by CARULYNU MALLILUIN (39%), prepared title Assistant (47%). Most of those are ONE TO
THREE CHARACTERS off, and that is the recognition on a 68% scan, not the mapping — the region
is right and the pixels are not there. What matters for a registry is that none of them shows a
false green tick: every doubtful value is flagged weak with its own reading quoted, which is the
2026-09-04 confidence-ceiling rule doing its job. The 1993 sample reads its bottom third at 48%
and produces mostly garbage there, all flagged; those coordinates are commented in the code as
the weakest in the library, to be re-measured when the office supplies a cleaner 1993 scan.

NO REGRESSION: the ground-truth harness is byte-identical before and after — 71/131 (54%), 28
wrong, 32 missing; birth 2007 29/30, birth 1993 14/23, death 20/20, marriage 8/29, marriage
photo 2 0/29. The certification fields have no ground-truth expectations (Truth.cs predates
them), so they neither help nor flatter that score.

FOUND WHILE VERIFYING, not fixed, and the office should know: CROMS\Assets\Form102Blank.png is
a 1993-NUMBERED sheet (20 INFORMANT / 21 PREPARED BY / 22 RECEIVED AT THE OFFICE OF THE CIVIL
REGISTRAR) but is registered as the BlankAsset of MF-102-2007, whose scans number those blocks
22/23/24/25 and add item 25. The overlay still lands correctly — the six new print cells were
checked by drawing them onto the blank and looking, and they sit on the Name-in-Print, Title and
Date rules — but the printed replica of a 2007 certificate is being drawn on a 1993 sheet. A
blank scan of the 2007 revision is the fix, and it is the same kind of prerequisite already
outstanding for MF-102 (1993).

ALSO NOT DONE, plainly: on a form whose LAYOUT is not on file the scan falls through to the
label path, and ExtractBirth reads only the informant name from this whole block — so an
unrecognised revision yields blanks here, exactly as it does for most other fields. That is the
known limit recorded on 2026-09-06, not a new one.

TRAP HIT AGAIN, worth repeating: Visual Studio regenerated BirthRegistrationForm.Designer.cs
from its in-memory copy mid-session and silently DELETED all 16 new control declarations, while
also moving btnCertificate/certificateMenu/mnuViewSoftcopy into the Designer and leaving the
old copies in the .cs — which is what the duplicate-definition build errors were. Same class of
loss the csproj suffered on 2026-09-02. Close the form's designer window before editing a
Designer file by hand, and check the edits survived before trusting a build.

VERIFIED by running rather than by compiling: migration 28 applied and the view queried; the OCR
commit INSERT and the Birth Registration insert/update column lists both executed against live
croms inside transactions and read back through v_birth_certificate, then rolled back; the
review-grid grouping enumerated off the built exe; the six new print cells drawn onto the real
blank form and inspected; the ground-truth harness re-run for the regression check. MSBuild exit
0, 0 warnings 0 errors across CROMS, CROMS.Display and CROMS.Kiosk, bin\Debug updated.

2026-09-10 (later — marriage and death get the same treatment, and the country gets loaded)
Two asks: read the certification blocks at the foot of MF-97 and MF-103 the way MF-102 now is,
and make Province -> Municipality -> Barangay a real cascade over the whole of the Philippines.

FIRST, TWO THINGS THAT WERE BROKEN AND HAD TO BE FIXED BEFORE ANYTHING ELSE.

  (1) BIRTH REGISTRATION COULD NOT SAVE AT ALL. Its column lists still named
  `time_of_birth` / `@tob`, the form no longer has a time-of-birth control, and FieldParams
  supplies no such parameter — so every insert and every update threw "Parameter '@tob' must be
  defined". The column is now dropped from both lists rather than sent as NULL: a form that
  cannot SHOW a value must not overwrite it, and a NULL on update would erase the time from the
  records that have one.

  (2) THE CASCADE THE FORM ALREADY HAD COULD NEVER HAVE WORKED. It joined
  `municipalities.province_id` and `barangays.municipality_id`; NEITHER COLUMN EXISTED. Every
  query threw, every catch swallowed it, and choosing a province produced an empty municipality
  list. The screen looked wired up and did nothing.

  Also: Visual Studio had regenerated BirthRegistrationForm.Designer.cs again and deleted the
  16 certification controls added earlier the same day (the trap recorded in the previous
  entry). Re-applied. Close the form's designer window before hand-editing a Designer file.

GEOGRAPHY — migration 29_philippine_geography.sql (3.1 MB, APPLIED to the live croms DB).
87 provinces, 1,647 cities and municipalities, 42,029 barangays from the PSGC, each row
carrying its parent and its `psgc_code` so a later PSGC revision can be re-applied over this one.

  IT IS A SCHEMA CHANGE, not just data, and UNIQUE(name) is why. 109 of 1,443 distinct
  city/municipality names repeat across provinces, and 3,942 of 27,068 distinct barangay names
  repeat. Loading the country under the old UNIQUE(name) would have silently collapsed 42,029
  barangays into 27,068 and filed them under whichever municipality was inserted first. Both
  indexes are replaced by the PSGC code, which is the real key; `province_id` /
  `municipality_id` are added NULL-able so LookupStore.Ensure can still write a bare name when a
  scan turns up somewhere unlisted.

  EXISTING ROWS ARE NOT REPLACED, because `deaths` and `marriages` hold FOREIGN KEYS into these
  tables. Each legacy row is ATTACHED to its PSGC twin accent-insensitively — which is how
  "Penablanca" finds the enye spelling — and only where the country holds EXACTLY ONE place of
  that name, so a legacy "San Isidro" is never bound to an arbitrary one of the several that
  exist. Where a twin already existed the foreign keys are repointed at the survivor FIRST and
  the duplicate only then removed. The office's own spelling is deliberately LEFT ALONE: its
  records store place names as text, so rewriting "Penablanca" to the PSGC spelling would make
  the master file disagree with every record already quoting it. Their data, their call.

  " (Capital)" is stripped from municipality names — it is a PSGC annotation, not part of the
  name, and left in, the picker offers "Tuguegarao City (Capital)" and a certificate prints it.
  That strip is also what let the office's own "Tuguegarao City" row be recognised as the same
  place instead of sitting beside it as a duplicate. Barangay names keep their "(Pob.)": there
  it distinguishes the poblacion barangay from its namesakes in the same municipality, so it IS
  part of the name.

  EVERY NAME IS EMITTED AS A HEX LITERAL, `CONVERT(X'..' USING utf8mb4)`. 467 of them carry an
  enye or an accent, and the migration is applied by piping into the mysql client, which decodes
  the file with the CONSOLE code page — exactly how the enye in "Penablanca" was stored as two
  box-drawing characters on 2026-09-06. Stating the bytes is the only form no client charset can
  reinterpret.

  VERIFIED against the live database: 87 / 1,647 / 42,029 rows, ZERO municipalities without a
  province, ZERO barangays without a municipality, zero rows left carrying "(Capital)", no
  orphaned foreign key in `deaths`, and Penablanca's own 24 barangays cascading correctly
  (Bical among them).

NEW Data/GeoLookup.cs — one implementation of the pickers for all three registration screens.
They each had their own copy of "fill a combo from a lookup table", which is how they drifted:
Birth cascaded (or would have), Death listed every municipality in the country regardless of
province, Marriage bound ids while the others bound text. Nothing is ever loaded whole — a
barangay list is only ever the barangays of one municipality, which is what keeps a 42,000-row
table usable in a dropdown. Changing the province clears the BARANGAY as well as the
municipality, because a barangay of the old municipality is not a place in the new province and
leaving it on screen would let it be saved as one.

  Birth now uses it, and its record-loading paths go through GeoLookup.SetAddress rather than
  splitting on commas and setting four boxes independently — the part a plain split gets wrong
  is that the cells cascade, so a barangay set before its municipality selects into a list that
  is still empty and the dropdown behind the value belongs to nowhere.

  DEATH's place-of-death boxes are reordered on screen to facility / PROVINCE / MUNICIPALITY,
  because a list cannot be narrowed by a choice that has not been made yet. The STORED string
  keeps its original "facility, municipality, province" order — reordering the boxes is a screen
  change, reordering the storage would silently re-read every row already written — and
  JoinPod/SetPod hold that mapping.

  MARRIAGE cascades by ID, since `marriages` stores place_municipality_id / place_province_id
  rather than text. Loading a record now selects the province FIRST and the municipality after:
  the province rebuilds the municipality list, so a municipality set beforehand would be thrown
  away by the rebuild.

CERTIFICATION BLOCKS — migration 30_marriage_death_certification.sql (APPLIED).
  `deaths` gains items 26-29 in full: informant name / relationship to the deceased / address /
  date, and prepared-by, received-by and registered-by each with name, title or position and
  date, plus remarks. On the office's own sample EVERY one of those is filled in ink on the
  paper and was blank in the database.
  `marriages` gains the marriage LICENCE (number, date, place), the solemnising officer's
  POSITION — the Family Code binds a marriage to the office he holds, not merely to the name he
  signs — the two WITNESSES, the received-at-the-office block, and the FOUR PARENTS of the
  contracting parties, which the scanner has been extracting since 2026-09-02 with nowhere to
  put them.
  No column is added for a block a form does not have: MF-97 has ONE receipt block, not the four
  MF-102 and MF-103 carry, so `marriages` gets one. Both certificate views restated to expose
  the new columns (v_death_certificate 56 columns, v_marriage_certificate 59).

REGIONS, measured by cropping each block out of the office's own scans and reading it at 2.4x —
17 new on MF-103, 8 new on MF-97, plus a re-measured solemnising-officer row.

  MF-103 reads its certification block WELL, which is the useful result here: informant
  relationship SISTER-IN-LAW exact, address MABALACAT, PAMPANGA at 98%, informant date
  1999-09-01 at 99%, prepared-by title MEDICAL RECORDS CLERK at 98%, registered-by
  EDLINA MASONGSONG at 88% and its date at 98%. TERESITA ABAD comes back "TERESWIA ABAD" and
  JASON M. SAN DIEGO "JASONI BAN CIEGO" — one or two characters, on a 706x968 sheet, both
  flagged. Two dates stay blank because the office SEAL is stamped across them; that is the
  correct answer, not a failure.

  MF-97 is the documented limit again. The regions now land on the right rows — the licence
  number comes back "OVS965" for 0025563 and the receipt name "ELISA AROGAT" for ELISA C.
  ARUGAY — but the scan reads at 45%, typewriter print on textured security paper with the
  solemniser's name struck through, so the values are several characters off and every one is
  flagged weak. Nothing here reads as confident and wrong.

A FABRICATION CAUGHT BY MEASURING, and this is the important one. The two-digit-year rule added
earlier today (so "13-Jun-18" would read at all) turned the marriage licence date — which the
scan renders as "February 25" plus debris — into 2000-02-25 AT 76% WITH A GREEN TICK. A date
that appears nowhere on the certificate, presented as read off it. Same shape as the CorrectDate
fabrication fixed on 2026-09-04, and it took nine minutes for the convenience to produce one.
  The rule now accepts a two-digit year ONLY in the compact hyphenated form an office machine
prints — "13-Jun-18". Written out, a certificate always states the year in full
("February 28, 2007"), so a two-digit tail after a comma or a space is not an abbreviation, it
is the reading falling apart. Verified both ways: the birth certificate's two "13-Jun-18"
signature dates still read 2018-06-13 at 92% and 81%; the marriage licence date is now blank and
says what it saw.

NO REGRESSION: ground-truth harness unchanged at 71/131 (54%), 28 wrong — birth 2007 29/30,
birth 1993 14/23, death 20/20, marriage 8/29, marriage photo 2 0/29. The new fields carry no
ground-truth expectations (Truth.cs predates them), so they neither help nor flatter that score.

VERIFIED by running, not by compiling: both migrations applied to the live croms DB and their
views queried; the new marriage and death columns written and read back through
v_marriage_certificate / v_death_certificate inside a transaction, then rolled back; the review
grid enumerated off the built exe for both forms (41 regions on MF-97 in nine sections, 34 on
MF-103 in eight, printed order, empty sections skipped); the geography counts and parent links
checked directly.

NOT DONE, and it is the honest remainder. The Death and Marriage REGISTRATION SCREENS still have
no controls for the columns migration 30 added, so a scanned certification block is read,
scored, shown in the review grid and audited — but on Auto-Fill it lands on a form with nowhere
to put it. Birth got its controls in this pass; the other two need the same tab work, and until
then those values reach the record only through a direct write. That is deliberately NOT bridged
with hidden fields: writing a value the operator cannot see or correct is the trap this project
keeps refusing.
  Also outstanding: MF-97 and MF-103 have no print cells for the new values (the built-in
overlay draws what the print map lists), and `Assets\Form102Blank.png` is still the 1993-numbered
sheet registered as the 2007 blank.

2026-09-10 (item 18 becomes a question with an answer) - "Parents Married?" is now a toggle at
the top of the Marriage of Parents tab, defaulting to ON, and switching it off GREYS OUT the
date and place rather than leaving them blank.

WHY IT IS A STORED COLUMN AND NOT JUST A SCREEN STATE. Until now a record with items 18a/18b
blank meant one of two completely different things: the parents are NOT married - which on a
birth certificate is a legal statement about the CHILD, since it is what makes the birth
illegitimate under the Family Code and what an RA 9255 acknowledgement later attaches to - or
the parents are married and nobody filled the boxes in. No report and no clerk could tell those
apart. Migration 31_parents_married.sql (APPLIED to the live croms DB) adds
`births.parents_married`: 1 married, 0 not married, NULL not stated. v_birth_certificate
restated to carry it, so a printed certificate can say item 18 was ANSWERED rather than skipped.
  Existing rows are left NULL and deliberately NOT backfilled. Reading a blank date as
"unmarried" would stamp illegitimacy onto records that simply were not filled in; reading it as
"married" would assert a marriage nobody recorded. NULL is what the database actually knows, and
the screen treats NULL as the default - toggle on, fields live - so nothing already entered
changes appearance.

DISABLED, NOT MERELY EMPTY, which is the point of the request. `ApplyParentsMarried` disables
dtpMarrDate, both captions and all three place cells (and the small captions under them, which
are siblings in the inner grid and would otherwise stay black under greyed boxes). A blank box
says "nobody filled this in"; a greyed box says "this question does not apply to this child".
  It also CLEARS the two fields on the way down. A disabled control still holds its text and is
still read by FieldParams, so a date left behind would be written onto a record that states
there was no marriage. Belt and braces: the parameters themselves force NULL when the toggle is
off, so neither route can leak a value.

THE THREE PLACES THAT HAD TO FOLLOW IT.
  * Loading a record restores the toggle BEFORE re-applying, and a stored 0 then clears and
    greys the block.
  * New / Clear resets to the default ON.
  * "Add Another Birth Form" copies the answer across with the rest of the parents' details -
    siblings share their parents - and restores it BEFORE the date and place, because applying
    it afterwards would clear what had just been copied in.
  * An auto-filled SCAN deliberately does NOT set it. A scan that produced neither a date nor a
    place has not told us the parents are unmarried, it has told us it could not read item 18;
    guessing "not married" from a failed read would put illegitimacy on the record. The toggle
    stays on its default and the operator answers it.

VERIFIED by driving the real form off the built exe rather than by compiling: default state is
toggle ON with the date picker, the place label and all three place cells enabled and the state
line reading "Married - state the date and place below"; after seeding a date and place and
switching the toggle off, all five controls report Enabled=False, the place cells come back
empty, the date's own tick is cleared, the line reads "Not married - items 18a and 18b do not
apply", and FieldParams emits @pmdate NULL, @pmplace NULL, @pmarried 0; switching back on
re-enables everything and emits @pmarried 1. Separately the unmarried state was written through
the form's own INSERT column list against the live database and read back through
v_birth_certificate as 0 / NULL / NULL, then rolled back. MSBuild exit 0, 0 warnings 0 errors.

2026-09-10 (still later — the death and marriage screens catch up, plus BR-16, BR-18, BR-20)
Five things: give Death and Marriage the certification fields migration 30 created, put a
specify box behind every "Others", correct the informant relationship list, one type
convention across the app, and strip the scanner's noise before it reaches a field.

DEATH REGISTRATION - a new "Certification (Items 26-29)" block. Thirteen fields: informant
name / relationship to the deceased / address / date, then prepared-by, received-by and
registered-by each with name, title or position and date. Every date picker shows its check
box, so "this sheet carries no date there" stays sayable instead of every record claiming
today. The form grew from 837 to 1200 and the records list moved below it; the shell's
AutoScroll carries it on a short screen. Auto-Fill from a scan now fills all thirteen.

MARRIAGE ENTRY - a new "LICENCE, OFFICER, WITNESSES AND RECEIPT" block: licence number, date
and place of issue; the solemnising officer's POSITION; both witnesses; and the office's own
receipt (name, title, date). Each spouse group gained Father's Name and Mother's Maiden Name -
items 9-12, which the scanner has been reading since 2026-09-02 with nowhere to put them. The
dialog grew to 780 and each spouse group to 570.
  With these two screens the loop the previous entry left open is closed: a certification block
is now read, scored, shown, corrected, routed and STORED.

"OTHERS, SPECIFY ____" - new Modules/OthersBox.cs, one implementation for every dropdown that
keeps an Others choice. A list that offers "Others" and nothing else is a dead end: the encoder
picks it, the record says "Others", and what the paper says is lost - which is why the forms
print the word "specify" beside the box.
  WHAT IS STORED IS "Others - Traditional Birth Attendant", not the typed text alone. The
CATEGORY survives, so a PSA count of "attended by others" is still possible; the DETAIL
survives, so the certificate can be reprinted with what the paper says. Storing only the typed
text loses the category, storing only "Others" loses the answer.
  The box is HIDDEN, not merely blank, when the choice is anything else - same reasoning as the
greyed marriage fields earlier today: an empty box that does not apply reads as one somebody
forgot. It also CLEARS on the way down, so a detail typed under Others cannot be carried onto a
record that no longer says Others. Bound on the birth attendant, the birth informant
relationship and the death informant relationship; "Other", "Others (specify)" and "Other,
specify" are all recognised, since the master file and the forms do not agree on the spelling.

BR-16, the relationship list - migration 32_relationships.sql (APPLIED). ONE TABLE, TWO
QUESTIONS: Form 102 item 22 asks "Relationship to the CHILD" and Form 103 item 26 asks
"Relationship to the DECEASED", and sharing the list meant each form offered the other's
answers - a newborn's informant could be recorded as "Wife", a decedent's as "Attending
Midwife". Every entry now says which form it belongs on (Birth / Death / Both) and each screen
filters on it. The combos still add back whatever a loaded record holds, so nothing already
stored is hidden.
  Junk removed: 'anthon' and 'athin', typed into the Master File by accident. Checked before
deleting - `relationships` has no foreign key pointing at it, and neither string appears on any
record. The standard answers were added (grandparents, aunt/uncle, siblings, attending
midwife/nurse, hospital and clinic administrator on the birth side; spouse, children, in-laws,
grandchild, funeral director on the death side).
  'Self' is KEPT although it can be true of neither form. One birth record stores it, and
deleting the option would not change that record - it would only make the value it holds look
like a typo. It is scoped to neither form, so it stops being offered while the record that has
it still displays it.

BR-20, OCR noise - new DocumentAI.Sanitize, applied at the single point where the region path
and the label path have converged, so neither can leak into a form field, a stored value or a
printed certificate. What survives is narrow and deliberate: letters (including the enye and
the accented vowels these names carry), digits, and the punctuation a registry value actually
uses - comma, full stop, hyphen, slash, apostrophe, brackets, colon for a time, hash and
ampersand for an address. Everything else is a ruled line, a tick box, a fold or speckle read
as a glyph. Runs collapse (the dotted fill-in line reads as "____" or "......" and no value is
written that way), a token with no letter and no digit is dropped, and a value with nothing
left comes back EMPTY - which asks the operator to type it, where "|" or "-" looks like
something was read.
  MEASURED against the built exe: "____ SHEILA B. TALOSIG ~~" -> "SHEILA B. TALOSIG",
"##Tuguegarao City##" -> "Tuguegarao City", box-drawing + "Others" -> "Others",
"CVMC,,, Carig ... Tuguegarao City" -> "CVMC, Carig Tuguegarao City", "| - |" -> empty; while
"MARIE ELIZABETH P. DELA CRUZ, MD", "2018-06-13", "3:40 PM", "2007-72", "Bagger 2" and the
enye in "Penablanca, Cagayan" pass through untouched. Ground-truth harness unchanged at
71/131 (54%), 28 wrong, per sample identical - the strip costs nothing.

BR-18, type - UiTheme now snaps LABEL text to 9pt and INPUT text to 9.75pt alongside the family
it already unified. The forms were built at different times and drifted: death types at 9.75,
the marriage dialog at 10, birth at the WinForms default 8.25, captions at 8.5 / 9 / 9.75
between them - side by side that reads as three applications. The snap is deliberately NARROW,
only a control already within a point of the target moves, so a 12pt heading, a 30pt queue
number and a deliberately small hint stay as designed. Keeping the step under a point also
keeps text metrics close enough that nothing in a fixed-width box starts clipping.
  NOT DONE, and it is a decision rather than an omission: the LETTER CASE of entered VALUES.
PSA forms are filled in capitals, so upper-casing names on save is arguable - but it rewrites
data, and it would change how every existing record searches, sorts and prints. That is the
office's call, not a styling pass. Labels and headings are left in the sentence case they are
written in; auto-transforming them would turn "PSA" into "Psa" and mangle "Municipal Form No.".

NOT IN THIS PASS, because the message did not carry them: BR-13, BR-17 and BR-19 were
referenced but not included - BR-13 in particular is named as the change that decides WHICH
dropdowns keep an "Others" choice, so the specify box is currently bound to the three that have
one today and will need re-checking once BR-13 lands. The same goes for the first item under
"Layout & presentation" ("tell the encoder where they are and what comes next") and the first
two under "OCR".

VERIFIED by running rather than by compiling: migration 32 applied and the scoped lists read
back; the Others box driven through the real control - blank hides it, "Others" shows box and
caption and composes "Others - Traditional Birth Attendant", switching to "Physician" hides AND
clears it and composes "Physician", and a stored "Others - Barangay Captain" splits back into
choice and detail; the death and marriage certification columns written and read back through
their views inside a transaction, then rolled back; the sanitiser exercised over twelve real
noise cases; the ground-truth harness re-run. MSBuild exit 0, 0 warnings 0 errors across CROMS,
CROMS.Display and CROMS.Kiosk.
  NOTE on the Others test: the first run reported every box as hidden, because Control.Visible
is EFFECTIVE visibility and the harness had hidden the parent form - the same trap as Dashboard
defect #1 on 2026-09-07. Never read Visible back through an unshown parent.

2026-09-10 (later still) — Dashboard Service Windows board: fixed the flicker/sluggishness
reported from the running app. `LoadWindowStatus` ran on the existing 3s `statusTimer` and, on
EVERY tick, disposed and recreated every row from scratch (`ClearRows` + rebuild) even though
the window set almost never changes tick to tick — only presence/ticket/status does. Each
`ListRow` owns 5 `Font`s + a `ToolTip`, so a 3-second cycle of dispose-then-recreate-N-of-those
is both the visible flicker and needless allocation churn.

Applied the same rebuild-only-on-signature-change pattern `QueueManagementForm.RefreshServing`
already uses for its Now Serving cards: a `Dictionary<int, ListRow>` keyed by window id plus a
cached signature string (`id:name|` per active window). `RebuildWindowRows` (clear+recreate) now
runs only when the active-window SET changes (added/removed/renamed); an ordinary tick calls
`UpdateWindowRows`, which just updates each existing row's Title/Subtitle/Ticket/status in place
and `Invalidate()`s it — no dispose, no new controls, no full-panel relayout.

VERIFIED: MSBuild clean, 0 errors (temp OutputPath — CROMS.exe was running, so REBUILD IN VS to
pick this up). No schema/behaviour change — same query, same displayed fields.

2026-09-10 (Queue Management UI rebuilt) — The screen is rebuilt on nested TableLayoutPanels with
UiTheme tokens, CardPanel and KpiCard — the same system the Dashboard, Birth Registration and
Certificate Request were rebuilt onto. Layout and presentation only: not one SQL statement that
writes, no save path, no schema change, no new package, and every existing handler
(Call Next / Call Client / Recall / Forward / window-card click / double-click / Export / Pause)
is untouched.

NO THIRD-PARTY UI LIBRARY, and the question was asked directly ("can you use Guna"). Guna.UI2
was rejected for three reasons, none of them taste: (1) it owner-draws its own controls, and
UiTheme ALREADY owner-draws every Button and DataGridView in this app — the two painters would
fight, and a Guna control ignores UiTheme entirely, so the app would end up on two visual systems
again, which is the exact drift this project keeps undoing; (2) it is another DLL in a client
bundle that ships over a Wi-Fi share to office PCs, and this project has held a no-new-package
line since the analytics module; (3) everything the mockup needed already exists in-house —
CardPanel (rounded face + hairline + shadow), StatusPill, KpiCard, ToggleSwitch, SplitButton,
HoverFade. What was used here IS a UI library; it is ours.

THE TABLE IS DOABLE, and it is a plain DataGridView. The mockup's row treatment comes from
CellFormatting, not a control swap: the wait time is coloured by severity (under 5 min muted,
5-15 amber, over 15 red and bold — matching the legend), the queue number renders in Consolas so
the codes line up, and a PRIORITY-LANE ticket tints its whole row amber with the lane named in
its own column. That is the honest version of the mockup's separate priority lane: one grid, one
ordering, the lane carried by the row itself. Two physically separate grids were considered and
not built — it would double the double-click/export/selection wiring for a visual grouping the
tint already conveys.

WHAT IS ON THE SCREEN NOW, top to bottom: title + LIVE CLOCK (seconds, plus the full date, on a
1s timer separate from the 3s sync tick) + the sync indicator + Client Display; four KPI tiles;
the window board; the workflow toolbar with the next step in words; the queue heading with
service filters and search; the list; a legend and the two secondary actions.

MY WINDOW, not everyone's. When the operator is signed in to a window the board already showed
only theirs (2026-07-31), but the heading still said "NOW SERVING" and the single card stretched
across the full width holding one queue number. The heading now names the window and states the
scope ("MY WINDOW — Window 1 · Only tickets routed to your window appear here"), and a lone card
is laid out gutter/card/gutter so it sits centred at about half width. An admin with no window
claimed still sees every active window, and the heading says so.

FILTERS + SEARCH work on the ALREADY-LOADED table through a DataView, so typing never touches the
database — which matters on a screen that re-queries every 3 seconds. Chips are All / Priority /
New Reg. / CTC / Marriage / Death / Petition / Claim, each showing its own count so the filter
says how much is behind it before you click. Export now writes WHAT IS ON SCREEN (filter and
search included) rather than the whole day — the operator asked for that list.

KPI TILES: Waiting now (+ how many are in the priority lane), Average wait (issued to called,
today), Served today, Longest wait (turns red past 15 minutes and NAMES the ticket to call next).
An unknown value stays a dash with a caption saying why ("no one called yet today"), never a
zero — a fabricated average on a queue board reads as measured.

THREE DEFECTS FOUND BY RENDERING THE REAL FORM against the live croms database, none by compiling:
  1. A TableLayoutPanel with NO RowStyle gives its child the default 100px row. Four inner tables
     had none, so their children were 100px tall inside 46px bands — "TODAY'S QUEUE" rendered
     below the chips and was clipped by the card under it. Every inner table now states its row.
  2. The section hint was Anchor=Top|Right and landed at x=2109 on a 1409-wide panel — off-screen.
     Same trap recorded on 2026-08-29 for the kiosk's step label: a Right-anchored control added
     to a container that has not yet reached its real width locks in a huge negative margin. It is
     Dock=Right now (added AFTER the Fill child, which is the ordering this codebase already had
     to establish in Release & Claim).
  3. The 17pt AutoSize title measured taller than it looks and ran into the subtitle beneath it;
     both are explicitly sized now.
Also caught: the clock's date was clipping at "September 10, 2" in a 150px panel — widened to 210.

CARD FACE vs BACKCOLOR, worth repeating because it is the second module to hit it: a CardPanel's
BackColor is the PAGE behind it, not the card — the face is painted inside the rounded path. The
offline-dim state therefore sets CardColor, not BackColor; setting BackColor would repaint the
page and leave the card white.

VERIFIED by rendering the real form off the freshly built exe against the live croms connection at
1449x900 (admin with no window: four window cards, one online) and again as the Window 1 operator
(single centred card, Call Client/Recall/Forward correctly disabled and visibly grey, "Next: press
Call Next…"), and by a sibling-overlap sweep over every container — ZERO overlapping pairs at that
size. At 1280x700 the form scrolls (AutoScrollMinSize 1120x800) instead of crushing the list, which
is the floor stated on the FORM because a Dock=Fill child contributes nothing to the scroll extent.
Live data came through correctly: waiting 2, average 52 min, served 2, longest 592 min flagged red,
the Senior ticket tinted, chip counts All 5 / Priority 1 / New Reg. 2.

MSBuild exit 0, 0 warnings 0 errors. bin\Debug was NOT written — CROMS.exe is running (PID 24256)
and holds it — so this was built and measured against a temp OutputPath. CLOSE THE APP AND REBUILD
IN VS to see it.

NOT DONE, deliberately: the mockup's client-name column is not shown (the queue list is keyed by
ticket number, and putting names on a screen that faces the counter is a Data Privacy Act question
for the office, not a layout decision); and the public-facing "Now Serving" monitor (ClientDisplayForm
/ CROMS.Display) is untouched by this pass.

2026-09-10 (same day, reported from the running app) — Two fixes on the rebuilt Queue Management
screen: the KPI numbers were clipped, and the priority lane is now its own always-visible table.

THE CLIPPED NUMBERS WERE MY OWN ROW HEIGHT. A KpiCard draws chip(32) + gap + label + the value's
ASCENT + caption inside its own 14/13 inset, which needs about 148px; I gave the row 116. So the
big number ran into the card's bottom edge and the caption never drew at all — the four cards
showed a label and a half-cut figure. Row is 148 now, and every caption is back
("none waiting in the priority lane", "issued to called, today", "Q-047 — call them next").
Height was taken back from the window card (172 -> 152) and the workflow bar (56 -> 52) so the
list did not lose space, and AutoScrollMinSize went 800 -> 880 so a short screen scrolls rather
than crushing the two tables.

PRIORITY LANE IS NOW A SECOND TABLE, permanently on screen under the regular queue (the user
offered "side or below"; below keeps all nine columns readable — side-by-side would halve the
width of a table that carries Queue No / Type / Issued / Wait / Priority / Recalls / Proc. Time /
Status). It is a fixed 168px band with its own amber-bordered card and a heading that states the
count and the reason ("PRIORITY LANE · 1 ticket today — serve ahead of the regular queue
(RA 11261)"). An empty lane still says so in the heading, because a grid showing only its header
row reads as broken.
  Both tables bind DataViews over the SAME loaded table — regular is
`Priority IS NULL OR Priority = 'Regular'`, the lane is everything else — so the service filter
and the search apply to both and the split costs no extra query. Both grids share the one
CellDoubleClick and the one CellFormatting handler, which now take their grid from `sender`
rather than the field, so a lane row opens its services exactly like a regular row.

THE "PRIORITY" FILTER CHIP WAS REMOVED, not overlooked: with the lane always visible, filtering
to priority-only would just take the regular queue off screen and show what is already there.

EXPORT still writes BOTH lanes (the base filter without the lane split) — the priority lane is
part of the same day's queue and is only shown apart for reading.

WORDING FIXED where the two disagreed on screen: the KPI counts tickets that are WAITING, while
the lane lists every priority ticket today, so a card reading "no one in the priority lane" sat
directly above a table showing one. The card now says "none waiting in the priority lane" and the
lane heading says "1 ticket today".

VERIFIED by rendering the real form against the live croms database at 1680x1010 (Window 1
operator): captions present on all four tiles, no clipping, the lane card at y=286 with its
Senior ticket, zero overlapping sibling pairs; and at 1366x720 the form scrolls (VScroll True)
instead of crushing either table. MSBuild exit 0, 0 warnings 0 errors — temp OutputPath again,
CROMS.exe is still running, so REBUILD IN VS.

2026-09-10 (Master Files froze the app) - Reported: opening Master Files on the admin side
froze CROMS (mouse and Alt+Tab still worked, only Stop Debugging got out). Root cause, measured:
the screen opens on its first category, Barangays, and migration 29 put the whole country in
that table - 42,029 rows. LoadItems bound every one of them to a DataGridView whose Designer set
AutoSizeRowsMode = AllCells, which MEASURES EVERY CELL OF EVERY ROW on the UI thread. That is
the hang: not the query (ORDER BY name over 42k rows is milliseconds), the per-cell row sizing.
The screen was fine at 2 barangays and nobody reopened it after the geography load.

FIX (MasterFilesForm only). AutoSizeRowsMode AllCells removed, so rows take UiTheme's fixed
34px. The list now shows at most 500 rows with a new Search box (debounced 300ms, one query when
typing stops, cleared on category change) and a count line that says when rows are held back
("Showing 500 of 42,029 - type in Search to narrow the list"). Barangays show Municipality +
Province, municipalities show Province: 3,942 barangay names repeat across the country (searching
"bical" returns six different Bicals), so a bare name could not tell the rows apart and an
operator could edit or delete the wrong one. A failed load now says so in the count line instead
of throwing out of the constructor.

MEASURED by opening the real form off the built exe against live croms: open + render 667ms
(was a freeze), 500 rows with ID/Name/Municipality/Province, search 100ms, switching to
Municipalities 44ms ("Showing 500 of 1,647"), Religions shows all 15. Compile exit 0 (temp
OutputPath - REBUILD IN VS).

NOT CHANGED, same shape, worth doing next: LookupStore.Ensure (Data/LearningLibrary.cs) dedups
by pulling the ENTIRE lookup table and normalising every row in C# on each call. Harmless at
2 barangays, but on barangays/municipalities it is now a 42k/1.6k-row pull per auto-filled field.
Also, Add in Master Files still inserts a barangay or municipality with no parent
(municipality_id / province_id NULL), so it will not appear in the cascading pickers.

2026-09-11 (Marriage module rebuilt end to end: Form 90 -> licence -> Form 97 -> PSA) - Built
from the marriageui.html proposal after checking every rule against the Family Code (Arts. 5,
12-18, 20, 21, 23, 27-34), RA 11596, RA 10354 s.15 and AO 1 s.1993. What existed: a 7-column
`marriage_licenses` (two free-text names, status Posting/Issued), a scrolling 45-field Form 97
dialog, and `marriages.license_no` as FREE TEXT - so a certificate could quote a licence that
did not exist, had lapsed or was already spent, and nothing objected.

MIGRATION 33_marriage_workflow.sql (APPLIED, run twice to prove idempotent, ASCII only).
marriage_licenses 7 -> 49 columns (both applicants in full, posting_start, earliest_issue_date,
deferral_reason, issue_date, expiry_date, issued_by, payment O.R., impediment finding, hold,
cancel); marriages +24 (license_id with UNIQUE + FK ON DELETE RESTRICT = one licence, one
marriage; license_basis Licensed/Exempt; exemption_basis; registration_type Timely/Delayed;
case posting; registrar review; date_registered/registered_by; OCR scan/confidence/weak/review;
PSA availability ref). New: app_settings (statutory numbers), marriage_requirement_types
(catalogue, CENOMAR switchable), marriage_requirements (one table for licence + marriage docs,
with attachment), marriage_history, marriage_copies, psa_transmittal_batches/_items (generic
record_table so births/deaths can join later). v_marriage_certificate restated to print the
LINKED licence's number/date. Nothing backfilled - the one legacy marriage stays NULL/"legacy".

ASSESSMENT - what was changed from the proposal, and why:
  - Only Draft/Posting/On Hold/Issued/Expired/Used/Cancelled are STORED. "Ready to Issue",
    "Valid", "Expiring" are DERIVED from dates so they can never go stale; Expired is also
    persisted by a sweep so it is queryable and in the history.
  - Posting does NOT require every document first (the proposal's own example posts with
    counselling outstanding) - it requires complete applicant data and no hard stop.
  - "Married"/"Separated" civil status is NOT a hard stop: Art. 18 has the registrar note an
    impediment and still issue unless a court orders otherwise. It blocks until the registrar
    RECORDS a finding. Only under-18 (RA 11596) is a hard stop, on both Form 90 and Form 97.
  - Unfavourable/absent parental advice (Art. 15) and a WAIVED counselling certificate
    (Art. 16) do not block - the law defers issue three months after posting; CROMS moves the
    earliest issue date and says why. Only counselling may be waived.
  - Payment = the Treasury O.R. recorded (collection stays with the Treasury, per 2026-07-23).
  - PSA "sent" and "available at PSA" are separate; availability needs an authoritative ref.
  - Legacy registrations report PSA status "Unknown (legacy record)", never "pending".
  - "Certified Copy Workflow" routes to the existing Certificate Request module, not a new one.

RULE NUMBERS (app_settings, all flagged CONFIRM WITH LCRO where the reading is an office's):
posting 10 days (day 1 = start, issue from day 11); validity 120 days, last valid day = issue +
120 (first day excluded, last included); consent band 18-20, advice 21-24; reporting 15 days
(30 for exempt); delayed-registration notice 10 days; PSA due day 10.

CODE. Data/MarriageRules.cs (PURE rule engine - every finding is a sentence plus where to fix
it); Data/MarriageService.cs (every write, server-side re-validation, transactions for
issue/register/batch, MAX+1 numbers retried on the unique index). Windows: MarriageRegistrationForm
(Desk: 6 KPIs, lifecycle board drawing both legal clocks to one scale, licences/marriages tabs,
search), MarriageLicenseForm (Form 90 stepper + rail) + IssueLicenseForm + LicensePrinter
(preview watermarked), MarriageEntryForm (Form 97: 5 tabs, licence PICKER, exempt path, OCR
source panel with weak-field highlight + Compare With Scan + review hold), MarriageCaseForm
(delayed/exempt), MarriageRecordForm (copies, PSA, history), PsaTransmittalForm (batch, print
transmittal, mark sent with method/office/ref, acknowledge, return). Forms/MarriageUi.cs holds
the shared pieces (tones, Banner, IssueList with "Open <tab>" links, StepStrip, LifecycleBoard,
RequirementsGrid). OcrDigitizationForm now passes its OCR run to Form 97 (SetOcrContext).

TESTS. New CROMS.MarriageTest console (not in the .sln, like DocTest): 74 checks driving
MarriageService against the LIVE db, then deleting everything it made - 74/74 PASS, leftovers 0.
Covers every scenario in the brief (under 18, consent, advice + deferral, counselling,
widowed/foreign, impediment, posting day 1/6, issue before posting / with missing doc / without
payment, role gate, 120-day maths, expiring/expired/sweep, valid/expired/before-issue licence,
name mismatch, duplicate licence use, exempt, timely/delayed, OCR weak/strong, registry number,
copies, batch/sent/ack/return/re-batch). `--render <dir>` seeds a desk and saves every window
as PNG - 13 windows rendered and inspected.

DEFECTS FOUND BY RENDERING, not compiling: PSA window CRASHED on open (SplitterDistance set
before the container had a size); Banner body drawn over its title (read _body.Visible -
EFFECTIVE visibility, false while the step page is hidden - the Dashboard trap again);
requirements grid height read back from a Dock=Fill child and measured while hidden; desk grid
showed the hidden id column (hidden before columns existed); "ISSUE & PRINT" rendered "ISSUE
_PRINT" and a KPI caption ate its '&' (TextRenderer mnemonic trap, third time); clipped section
subtitles. All fixed and re-rendered.

Build: MSBuild exit 0, 0 errors. bin\Debug NOT written - CROMS.exe was running - so REBUILD IN
VS. Still to confirm with the Penablanca LCRO: the age-band reading, whether CENOMAR and
counselling-for-all are office policy, the licence print layout (CROMS prints its own rendering,
not the official MF-90 stock), which duplicate/triplicate copy goes to PSA, the PSA submission
method and due day, and the delayed-registration posting period.

2026-09-12 (agency interview recorded - scope anchor, no code) - The LCRO pre-checkup /
mini-interview of 2026-09-11 is written down as "CROMS - Agency Requirements Backlog.md" at the
repo root, before the session that gathered it was lost. Nothing was built in this pass; this is
the scope document the next several passes answer to.

WHY A SEPARATE FILE rather than a progress entry: the progress log records what was DONE, and
every item in the backlog is either not yet agreed or not yet answered. Mixing the two makes a
pending requirement read as a delivered one. Each item carries one of three labels - CONFIRMED
(the office said it and it is buildable), PENDING (the office says a requirement exists but not
its content - a document or an answer is owed), or SUPPLEMENT (not from the interview; added from
statute or from this repo's own history because it is load-bearing and was going to be forgotten).

THE FINDING THAT SHAPES THE REST. The office does not want a second copy of systems they already
run. Services split in two: CROMS ASSISTS the work (marriage licence, fee collection, BREQS,
birth registration especially delayed, petitions) or CROMS RECORDS AND TRACKS it (marriage
registration, death registration, legitimation, supplemental report, legal instruments, court
order). Tier two is not "do nothing" - the LCRO still performs the statutory registration - but
CROMS's share of it is internal record, attached document and case visibility, not a rebuilt legal
procedure. Which is the design rule this project already states.

CONFIRMED AND BUILDABLE: place of birth splits into Country / Province / City-Municipality /
Barangay across EVERY module (Country is new - the office receives applicants born abroad, and no
table holds it); Sex added to the marriage applicant; both parents plus a single guardian where a
parent is unavailable; age bands 18-20 parental CONSENT (a licence document) and 21-25 parental
ADVICE (not a block - unfavourable or absent advice defers issue three months after posting
completes, FC Art. 15); widowed applicants need a previous-marriage block; an applicant from
another province needs an attachment slot; marriage APPLICATION and marriage REGISTRATION stay
separate and registration must not re-collect the application; four new Petition-style tracking
case types (legitimation RA 9858, supplemental report, legal instruments RA 9255/9858, court
order); a fuller fee-collection module that can record a payment NOT originating from any CROMS
module, each with a purpose of transaction and a log feeding the monthly report; BREQS on both the
kiosk and a staff window; timely vs delayed birth registration as distinct workflows; and a
three-stage receiving -> processor -> releasing flow.

WHAT THE BACKLOG SAYS TO REUSE RATHER THAN BUILD, because nearly every item has an analogue here
already: GeoLookup + migration 29 for the address cascade (only Country is missing); the marriage
licence's 10-day posting clock, requirements-with-attachment table and registrar finding for
DELAYED BIRTH REGISTRATION, which is the same shape; the TXN status machine and window routing
already in Release & Claim and Queue Management for the three-stage workflow; FormCatalog +
CertificateReport + the overlay renderer for the printed forms; and the Petition module as the
pattern for the four tracking types - with one generic case-tracking table preferred over four
near-copies IF the stages turn out similar, decided after the office states them, not before.

THE CRYSTAL REPORTS ANSWER HAS TO BE GIVEN TO THE OFFICE. They asked for the Marriage Application,
Parental Consent and Parental Advice forms to print from CROMS via Crystal Reports. There are no
.rpt files in this project and none can be authored on this machine (no designer; programmatic
creation measured impossible on the free CR-for-VS runtime, 2026-09-06). The overlay renderer
already produces exactly what they are asking for - the blank form drawn with each value in its
own measured box, which is what all three certificates print as today. So the deliverable per form
is a CLEAN SCAN OF THE BLANK FORM plus a print map plus a FormCatalog entry, and the blank scan is
the prerequisite. Same outstanding request as MF-102 (1993).

ONE THING CHECKED RATHER THAN ASSUMED, and it changed what the file says. The interview states the
advice band as 21-25 while app_settings says 21-24, which looked like an off-by-one to be fixed.
It is not: the migration reads "between twenty-one and twenty-five" as excluding 25, exactly as it
reads "between eighteen and twenty-one" as excluding 21, and both lines already carry CONFIRM WITH
LCRO. The reading is internally consistent and defensible; "21-25" is how the requirement is
normally spoken. So the backlog records the single real question - does a 25-year-old need advice
at THIS office - instead of an instruction to widen the band. Silently widening it would make
CROMS demand a document the office does not demand, and an applicant is turned away for it.

DOCUMENTS OWED BY THE OFFICE, chased as one batch because each unblocks named work: the Marriage
Application form, the Parental Consent form, the Parental Advice form, the delayed-birth-
registration requirements, the complete fee/transaction list, and a blank MF-102 (2007) sheet -
which also fixes a defect found on 2026-09-10, that Assets\Form102Blank.png is a 1993-NUMBERED
sheet registered as the 2007 blank.

EXPLICITLY NOT DECIDED, and the file marks them so nobody implements them from a guess: the
tracking stages for all four new case types (Filed -> Endorsed to PSA -> Released to Client was
offered as an EXAMPLE, not a workflow); the BREQS turnaround, accepted IDs, fees and statuses (the
"about one week" is a local estimate and must be a setting, not a constant); the exact fields staff
add at marriage registration and at death registration; the CROMS/PhilCRIS boundary; the another-
province document and its trigger; the widow's previous-marriage fields; and the staff permission
model for the three-stage workflow. A status list or a fee invented here reaches a government
record and is then indistinguishable from a fact the office stated.

2026-09-12 (backlog Phase 1: applicant sex, and country on every place of birth) - The two
unblocked items from "CROMS - Agency Requirements Backlog.md" are built, applied and measured.
Migrations 36 and 37 are APPLIED to the live croms database. Everything else in the backlog
waits on documents the office still owes.

APPLICANT SEX (migration 36). The office said Form 90 prints a sex for each party and CROMS had
nowhere to put it, so it was written in by hand on every printed application. marriage_licenses
gains husband_sex / wife_sex; MarriageRules.Party gains Sex; the licence screen gains a
Male/Female pick per party and the printed application prints it.
  IT IS PRE-PICKED, NOT DERIVED, and the distinction is the whole decision. The column is
HUSBAND / WIFE, so under the Family Code the lawful answer is Male / Female and leaving the box
blank would be friction with only one correct resolution. But a sex entry can itself have been
corrected under RA 10172, so the value stays visible and editable rather than being concluded
from the column - MarriageRules.SexForRole is only ever the INITIAL pick. No validation rule was
added: the office asked for a field, and a rule that could refuse a licence is not something to
invent on their behalf. Licences filed before the column existed hold NULL and are NOT
backfilled; the screen shows the column's own value for those so the clerk sees a filled box
rather than a blank one to notice.

COUNTRY OF BIRTH (migration 37). New `countries` lookup (204 rows, seeded, whitelisted in Master
Files and LookupStore so the office can edit it), plus births.birth_country and
marriage_licenses.husband_birth_country / wife_birth_country.
  COUNTRY IS ITS OWN COLUMN AND MUST STAY THAT WAY. births.place_of_birth stores
"Hospital, Province, Municipality" as ONE comma-joined value and is split back on the comma when
a record loads. Appending a country to that string would re-split every row already written into
the wrong three cells - the same reasoning that kept place-of-death's stored order fixed when its
boxes were reordered on 2026-09-10. So Birth's place cell went from 3 combos to 4, but only the
last three are joined; the country sits beside them and is written to its own column.
  THE GATING IS THE PART THAT MATTERS. A foreign birth cannot cascade - there is no province list
for Japan and no barangay list for anywhere outside the PSGC. GeoLookup.CascadeCountry EMPTIES
the Philippine lists for a non-home country instead of disabling the cells, so the clerk types
the foreign locality into the same boxes. Emptied, never disabled: a disabled cell would make a
foreign birth unrecordable, which is precisely the case the office asked for this field to
handle. Verified by driving the real form - country=Japan takes the province list 88 -> 1 (the
blank) and back to 88 on Philippines.
  Existing rows stay NULL rather than being backfilled to Philippines. Almost all of them ARE
Philippine births, but "almost all" is not a fact about any particular record, and a country
written into a registry entry that never stated one is a fabricated entry. A NEW record defaults
to Philippines; a loaded record shows what it actually holds.

THE MARRIAGE LICENCE WAS THE ONLY REAL SINGLE TEXTBOX. Audited every module first rather than
assuming: Birth and Death already had structured place cells through GeoLookup, and Marriage
Form 97 has no birthplace control at all. The one bare TextBox the office was describing was
place of birth on Form 90, which is now Country of birth / Province of birth / City or
municipality of birth. Legacy free-text values are split on the comma with the LAST part taken
as the province and EVERYTHING BEFORE IT kept as the municipality, so a three-part legacy value
("Bical, Penablanca, Cagayan") is shown in full for the clerk to correct instead of being
quietly truncated - measured on five shapes including a bare "Manila" and a blank.

ASCII COUNTRY NAMES ARE DELIBERATE. These migrations are applied by piping the file into the
mysql client, which decodes it with the console code page - that is how the enye in
"Penablanca" was stored as two box-drawing characters on 2026-09-06. Every seeded name is ASCII
("Cote d'Ivoire", "Turkiye", "Sao Tome and Principe"), so no client charset can reinterpret it,
and unlike the office's own municipality these are picklist values they can correct in Master
Files rather than text printed on their forms.

THREE DEFECTS FOUND BY RENDERING THE REAL FORMS, none by compiling - the build was clean before
all three.
  1. NULL REFERENCE ON OPENING THE LICENCE SCREEN, and it was mine. Selecting the default
     country at the end of PartyColumn fired TextChanged -> Touched -> RefreshAges -> Current(),
     which reads BOTH party boxes - and the wife's boxes do not exist yet while the husband's
     column is being built. The geography wiring and the default pick now run BEFORE the Touched
     handlers are attached, which also means defaulting the country no longer marks a blank
     application dirty. Shape worth keeping: a "set a sensible default" line is a real event
     source, and in a constructor it can fire into half-built state.
  2. "AGE ON FILING - COMPUTED" ran straight into the SEX caption once that row went to three
     columns, rendering as "AGE ON FILING - COMPUSEX". Shortened to "Age on filing"; the value
     beside it already says it is derived.
  3. "CITY / MUNICIPALITY OF BIRTH" is the longest caption on the card and clipped at a third of
     the card's width, so place of birth is two rows (country + province, then city) rather than
     three columns - which also matches the order the pickers cascade in. That extra row then
     cut the parents' names in half at the card's bottom edge, so the applicants block went
     390 -> 450px (seven 56px rows + a 32px header + 16px padding). Each of these only showed by
     looking at the rendered page.

VERIFIED BY RUNNING, not by compiling: migrations 36 and 37 applied and their columns read back;
the births INSERT executed against live croms with the column list read OFF THE BUILT ASSEMBLY
(not retyped) and the country read back as 'Japan', inside a transaction and rolled back; a
licence saved through MarriageService.SaveLicense itself and read back as Male/Philippines/
"Tuguegarao City, Cagayan" and Female/Japan/Osaka, then deleted; the place-split helpers driven
over five shapes; both screens rendered and inspected. Leak check afterwards: births still 19,
zero stray licence or history rows. MSBuild exit 0, 0 warnings 0 errors across CROMS,
CROMS.Display and CROMS.Kiosk, bin\Debug updated (app was closed).
  NOTE on the harness: setting APP_CONFIG_FILE alone still is not enough - ConfigurationManager
caches its resolved paths and init state on first touch, so s_current / s_initState /
s_configSystem have to be cleared too or MarriageService silently connects as the Windows user
and fails. Same trap recorded on 2026-09-07.

NOT DONE, and stated so nobody assumes otherwise. (1) v_birth_certificate is NOT restated to
carry birth_country - that view is the datasource a printed certificate binds to and extending
it means restating ~71 columns, which belongs with the print-map work; until then the country is
stored and shown but does not print. (2) BARANGAY on place of birth, which the office also
listed, is deliberately not added: MF-102 does print one, MF-90 does not, and Birth's existing
third cell is the FACILITY, not a barangay - so it is a new field whose scope differs per form
and needs the office's answer first. (3) Migrations 34 and 35 (kiosk valid-ID type, kiosk spouse
columns) are still NOT applied to the live database; they are someone else's pending work, they
are unrelated to this pass, and the kiosk INSERT will fail until they are run.

2026-09-13 (five of the six owed documents arrived; recorded, not yet built) - The office supplied
the blank Municipal Form 90, the Parental Consent form (MF No. 06), the Parental Advice form
(MF No. 68), the delayed-registration requirement card (PSA MC 2024-17) and the fee card. All five
are transcribed into "CROMS - Agency Requirements Backlog.md" and four PENDING items are now
CONFIRMED. NO CODE CHANGED in this pass - this is the reading, so that the building is done
against what the paper says rather than against what we assumed.

MF-90 WAS MEASURED, NOT EYEBALLED. The file is a VECTOR blank (ReportLab-generated, 612x936pt =
8.5 x 13 in long bond, Revised January 1993 / Form No. 2) already carrying Penablanca, Cagayan and
the registrar's printed name. Its content stream is ASCII85 + Flate, so every label and every
ruled line was read out of the PDF itself: 115 text items and their coordinates, plus the rules a
value is written on. The form is TWO-COLUMN - one applicant per column, labels printed down the
centre gutter - exactly the MF-97 structure. Copied to Docs\AgencyForms\ so it does not live only
in Downloads.

WHAT THE FORM SETTLES, and it is more than it was asked for.
  - SEX is on it, at y578.6. That confirms the field built yesterday rather than leaving it as an
    office preference.
  - THE WIDOW BLOCK IS ANSWERED IN FULL, which was a PENDING item: the form asks exactly three
    things - HOW the previous marriage was dissolved, the PLACE (city/municipality + province) and
    the DATE (day/month/year). Nothing else. And it is headed "If previously married", not "if
    widowed", so it covers annulled and dissolved marriages too.
  - PARENTS AND GUARDIAN ARE ANSWERED: father and mother each get name + citizenship + residence,
    and there is exactly ONE further slot - "Person who gave consent or advice" - with its own
    name, RELATIONSHIP, citizenship and residence. That matches what the office said about only
    one guardian being needed, and it is one slot for EITHER the consent-giver or the
    advice-giver, not two.
  - TWO FIELDS THE FORM HAS AND CROMS DOES NOT: "Degree of Relationship of Contracting Parties"
    (the consanguinity / affinity declaration behind Family Code Arts. 37-38) and the
    "Exempt from" / "Documentary" stamp boxes. The degree-of-relationship one is flagged as a
    question rather than built - CROMS has no field AND no check, and inventing a check that could
    refuse a licence is not ours to decide.

THE CONSENT FORM HALF-ANSWERS THE AGE BAND I flagged yesterday as a question rather than a bug.
Municipal Form No. 06 describes the applicant as "single and less than (twenty one) years of age".
"Less than twenty-one" excludes 21, so the CONSENT band is 18-20 - exactly what
MARRIAGE_CONSENT_AGE_TO = 20 already says, so that line is now confirmed by the office's own
paper. The ADVICE band is still open: MF No. 68 carries no age wording at all, so nothing on
paper settles whether a 25-year-old needs it. One question left where there were two.

THE ADVICE FORM IS TWO SHEETS, NOT ONE - a (MALE) and a (FEMALE), one per contracting party - and
it has THREE signature slots: Father, Mother, and Legal Guardian / Head of Institution. Two things
in the sample matter for the build: a deceased parent is annotated "(DECEASED)" in the signature
block with only the surviving parent signing, so the office records WHY a parent did not sign; and
the printed wording is FAVOURABLE only ("will hereby advice you to marry him/her LEGALLY") with no
unfavourable variant, which means an unfavourable or withheld advice shows up as the DOCUMENT NOT
EXISTING. That is what triggers the three-month deferral, and it is already how CROMS models it -
absence of the document is the signal, not a field on it.

THE FEE CARD CONTRADICTS THE SEEDED AMOUNTS, and the cashier screen is using the seeded ones. The
office's card ("PLS. PAY AT TREASURY OFFICE" - which incidentally confirms the 2026-07-23 decision
that collection is the Treasury's and CROMS only records the O.R.) lists 15 fees. Against `fees`
as seeded on 2026-07-14 and flagged then as "realistic but unverified":
    CTC-BIRTH / MARRIAGE / DEATH   seeded 155.00   card says certified copy 50 + 30 = 80
    NEG-CERT                       seeded 210.00   card says certification 100 + 30 = 130
    PET-9048                       seeded   0.00   card says CCE 1,000 and CFN 3,000
    PET-10172                      seeded   0.00   card says 3,000
Ten more fee types on the card have no row at all. NOT CHANGED, deliberately, because two things
have to be answered first: what the "+ 30" is (almost certainly documentary stamp tax, but
"almost certainly" is not a fact about a government fee, and `payments.additional_fee` already
exists if it is a separate line), and which fee a petition takes - one PET-9048 code cannot carry
both the 1,000 CCE and the 3,000 CFN. Rewriting a fee schedule from a phone photo is not a call to
make alone.

THE DELAYED-REGISTRATION CARD CHANGES THE REQUIREMENTS MODEL, which is the part worth catching
before any code. Item (c) is "ANY TWO of the following evidences of birth" over eight options.
`marriage_requirements` is one row per named requirement with a status, which cannot express "any
two of these eight" - it needs a group with a satisfy-count, or delayed registration needs its own
shape. Items (f), (g) and (h) are conditional (registrant deceased, parents unmarried, mother
unavailable, parent deceased), which the licence rule-keys already have a precedent for. Also
noted: the card is what the CLIENT is handed, so it carries the ten documents but NOT the 10-day
posting or the registrar's evaluation that PSA MC 2024-17 also requires - the workflow still needs
the posting clock, not just the checklist.

STILL OWED: the blank MF-102 (2007). It is the last of the six, and it is also the fix for the
defect found on 2026-09-10 - Assets\Form102Blank.png is a 1993-NUMBERED sheet registered as the
2007 blank, so a 2007 certificate currently prints its values onto a 1993 form.

STILL UNANSWERED and explicitly not guessed: the another-province supporting document and what
triggers it; how the office records a marriage registration after receiving the Certificate of
Marriage, and which details staff add; the CROMS / PhilCRIS boundary; whether a 25-year-old needs
parental advice here; whether counselling and CENOMAR are office policy; the tracking stages for
the four new case types; every BREQS detail; and now two new ones raised by the documents
themselves - whether the office wants Degree of Relationship captured, and whether CROMS should
record WHY a parent did not sign the advice form.

2026-09-13 (later - the MF-102 (2007) blank arrived, and it brought the back of the form with it)
The last of the six owed documents is in, as a third-party (studocu) PDF rather than the office's
own stock. Saved to Docs\AgencyForms\ with both pages extracted. No code changed.

IT IS THE RIGHT REVISION, confirmed by its own numbering rather than by its cover text: 22
Certification of Informant / 23 Prepared By / 24 Received By / 25 Registered by the Civil
Registrar. The sheet currently sitting in Assets as the 2007 blank numbers those 20/21/22, which
is the 1993 sequence - the defect found on 2026-09-10. Page 1 is clean, unfilled and
unwatermarked (the studocu branding is on separate wrapper images and an A4 cover page, not on
the form), 1275x2100 px = 150 DPI on 8.5 x 14 legal.

PAGE 2 IS THE BACK OF THE FORM, which nobody asked for and which answers two open items. It
carries the AFFIDAVIT OF ACKNOWLEDGMENT / ADMISSION OF PATERNITY - the RA 9255 instrument behind
the Legal Instruments tracking in the backlog's section 7 - and the AFFIDAVIT FOR DELAYED
REGISTRATION OF BIRTH, which is the part of delayed registration the client-facing requirement
card does NOT cover. The delayed affidavit has seven numbered clauses with real fields: whose
birth (tick box - the affiant's own, or another person's), who attended the birth and where they
reside, citizenship, whether the parents were married (tick box, with the father's name where the
child was acknowledged), THE REASON FOR THE DELAY, and the affiant's relationship to the
registrant. That is concrete input for Phase 7 instead of a generic checklist.

NOT A DROP-IN REPLACEMENT, and this is the thing to remember before anyone copies the file into
Assets. The existing MF-102 print map is 60 hand-measured point literals expressed against a
792x1224 pt page (aspect 0.647); this sheet is 8.5 x 14 legal (aspect 0.607). Different aspect
ratio, so dropping it in would displace every value on the page. Swapping the asset means
RE-MEASURING the print map - the same job MF-97 and MF-103 got on 2026-09-07 - so the asset is
deliberately left alone until that work is done.

ALSO NOT THE OFFICE'S OWN STOCK. It is a scan found online. Almost certainly identical, since
MF-102 is a national PSA form, but the entire point of the overlay replica is fidelity, so the
office should confirm it matches the sheets they actually issue before it becomes the printed
background on real certificates.

CRYSTAL REPORTS, asked directly and worth restating because the premise keeps recurring: a blank
scan is NOT Crystal input. There are still no .rpt files in this project and none can be authored
on this machine (no designer; in-process RAS creation proven impossible on the free CR-for-VS
runtime, 2026-09-06). The blank is the BACKGROUND BITMAP the built-in overlay renderer draws
before laying each value into its measured box. Print preview already exists and already works -
CertificateReport.ShowOcrPreview plus that renderer, watermarked and stamp-free, not a
CrystalReportViewer. The Crystal plumbing is wired and isolated so a PC without the runtime still
runs CROMS, and a .rpt dropped into CROMS\Reports would take over automatically, but nothing in
the app uses it today because there is no report to run.

2026-09-13 (backlog Phase 3A: MF-90 parent, consent/advice and previously-married blocks) - The
Form 90 applicant fields the blank asks for and the licence screen did not have are built, applied
and measured. Migration 38_form90_applicant_blocks.sql is APPLIED to the live croms database (run
twice to prove idempotent; 40 columns; no leftover procedure; 0 non-ASCII bytes).

SCHEMA DECISION, stated before any code: THREE NAME CELLS, and the joined columns retired.
marriage_licenses held husband_/wife_father_name and _mother_name as one VARCHAR(120). MF-90 prints
First / Middle / Last as three cells and the extractor already produces three. Keeping the joined
string and splitting it at print time is the 2026-09-02 OCR failure again: "Jose Santos Dela Cruz"
cannot be told apart from "Jose Santos Dela / Cruz", so a two-word surname is mangled. So each
party now has father / mother first-middle-last + citizenship + residence, one consent-or-advice
person (first-middle-last + relationship + citizenship + residence - ONE slot, as on the form), and
the previously-married block (how dissolved, province, city/municipality, date). The joined
columns are NOT dropped - they are the only record of what older applications stated - and nothing
writes them any more. A pre-38 licence shows its joined name WHOLE in the Last cell for the clerk
to correct (same precedent as the legacy husband_name), never split on spaces. Verified on the one
real licence on file (#59, father "Ronova"). Party.Father / .Mother survive as the JOINED value
derived from the cells, because Form 97 prints one cell and copies it from the licence.
Place dissolved is two columns, not "City, Province" in one - place_of_birth's comma-joined
storage is the M2 debt from 2026-07-14 and a new field does not repeat it. Nothing backfilled.

PREVIOUSLY MARRIED follows Birth's parents-married pattern (2026-09-10): live only for Widowed /
Annulled / Divorced (MarriageRules.IsPreviouslyMarried), otherwise GREYED AND CLEARED, with a note
line saying why ("Does not apply - civil status is Single."). Married / Separated do not open it:
those marriages were not dissolved and go to the Art. 18 registrar finding. Cleared because a
disabled box still holds text that ReadParty would carry onto the record, and SaveLicense forces
the four columns NULL for any other civil status as well - so a stale value cannot reach the table
by either route. Values load AFTER civil status, since choosing the status is what clears them.
Suggestion lists are sourced, not invented, and editable: relationship = Father / Mother / Guardian
/ Legal Guardian / Person having legal charge / Head of Institution (MF 06, MF 68, FC Art. 14);
dissolution = Death of spouse / Annulment / Declaration of nullity / Divorce.
Citizenship reuses the nationalities lookup, now loaded once per window instead of per combo (six
boxes per party read it). Place dissolved reuses GeoLookup.CascadePlace. Parent and consent
RESIDENCE are single textboxes, matching the applicant's own Residence field and the single cell
MF-90 prints - a GeoLookup trio there would need joining into one column, and the join is the
thing being avoided.

THE PARTYCOLUMN TRAP was respected: the geography wiring, the default country and the initial
grey-out all run before any TextChanged / SelectedIndexChanged handler is attached.

THREE DEFECTS FOUND BY RENDERING, none by compiling - the build was clean before each.
  1. THE WINDOW OPENED HALFWAY DOWN THE PAGE with the consent-person's Residence text selected.
     Stack() adds controls last-to-first so Dock=Top lays them out top-down - which also made TAB
     order run BOTTOM-TO-TOP, so the first focusable box was at the foot of the card (the greyed
     previously-married rows were skipped). Pre-existing, but invisible while the card was 450px;
     at 1042px the page auto-scrolled to it. Stack now sets TabIndex in visual order; the window
     opens on Date filed at scroll 0 (asserted).
  2. EVERY DATE ON THE MARRIAGE WINDOWS RAN PAST ITS CELL. Measured: each DateTimePicker stayed at
     its default 200px - Date of birth in a 130px cell (clipped, which is why no render ever showed
     its dropdown arrow) and Date dissolved in 196px, touching the card border. Reproduced in
     isolation: MUi.Field's TableLayoutPanel had NO ColumnStyle, so its one column AutoSized to the
     widest child. TextBox and ComboBox prefer less than the cell, so only dates showed it. One
     ColumnStyle(Percent 100) in the shared helper fixes Form 90, Form 97 and the issue window
     (Date of birth now 120 in 130, Date dissolved 186 in 196 - both asserted).
  3. The card height could not stay a hand count (450 for seven rows went to ~1000). It is now
     measured from the stacked blocks at build time (1042) and asserted equal to the drawn container.

VERIFIED BY RUNNING. New CROMS.MarriageTest mode `--form90 <dir>`: saves a licence through
MarriageService.SaveLicense itself and reads the row back RAW from the table (not through
LoadLicense, which could mask a column never written) - all 39 populated/expected columns plus the
4 retired ones NULL, the date, and the Single husband's previously-married block written as NULL
despite values being supplied; then LoadLicense round trip (joined father keeps "Dela Cruz"), the
legacy fallback on licence #59, an update to Single clearing the block while keeping the rest, and
the screen driven: husband's rows greyed, wife's live and loaded, three father cells, switching
the wife to Single on screen greys and clears the block, opens at scroll 0, dates inside their
cells, cards drawn at full height and LOOKED AT. 17/17 pass, 0 strays. The existing 74-check
workflow suite still passes 74/74 and all 13 marriage windows re-render (the MUi.Field change is
shared), 0 leftovers. MSBuild clean, 0 warnings 0 errors, bin\Debug updated (app was closed).
Also registered migrations 37 (it had been left out of CROMS.csproj) and 38 as None items.

NOT DONE, stated so it is not assumed: (1) the printed licence (LicensePrinter) still shows only
the joined father / mother lines - the new blocks print with the MF-90 overlay, backlog item 9;
(2) the consent/advice slot is not age-gated - the office said one slot is needed, not when it must
be blank, so it stays open for every applicant; (3) Degree of Relationship is not added (ask
first, §14); (4) CROMS.Display and CROMS.Kiosk were not rebuilt - nothing they compile changed.

2026-09-13 (backlog Phase 3B: Form 90 printed through a real Crystal report, Ctrl+wheel zoom) -
"Print application (MF-90)" on the Marriage License window previews Municipal Form 90 filled in
from the saved application, in the Crystal viewer, printable and exportable from its toolbar, and
zoomable with Ctrl + mouse wheel. It is a genuine .rpt (CROMS/Reports/MF-90-1993.rpt), not the
built-in overlay dressed up.

CORRECTION TO 2026-09-06, and the most useful finding of the pass. That entry recorded .rpt
authoring as impossible on this machine and "no designer installed". Both were wrong, and I
re-checked instead of repeating them:
  - The designer IS installed: VS 2019 Community carries Extensions\SAP\CRVsPackage, and
    CrystalDecisions.VSDesigner / CrystalReports.Design are in the GAC.
  - 2026-09-06 only tried ReportClientDocument.New(), which needs a report server. Loading an
    EXISTING .rpt and editing it through its in-process ReportClientDocument works - and VS ships
    a blank one as its item template (ItemTemplates\CSharp\Reporting\1033\CrystalReport\
    CrystalReport.rpt). Proven with a throwaway probe before building anything: load the seed,
    add an ADO.NET table, a text object, a data field and a picture, SaveAs, reload, bind a row,
    export PDF - the value came out as real text in the PDF.
So the certificates' .rpt files (MF-102 / 97 / 103) are now buildable the same way. Not done in
this pass; noted for whoever next touches Reports.

HOW THE REPORT IS BUILT. New CROMS.ReportGen console (not in the .sln, like DocTest/MarriageTest)
generates the .rpt: seed -> datasource table -> user paper 8.5 x 13 in, zero margins -> every
section but Detail suppressed -> the blank Form 90 embedded as a full-page picture -> one field
per printed box -> SaveAs. The POSITIONS are not in the generator: they are Data/Mf90Form.Cells,
the single list the generator, the no-runtime fallback renderer and the test harness all read, so
the three cannot drift. Coordinates were MEASURED from the office's vector blank with
Docs/AgencyForms/extract_mf90.py: each row is the band between two printed rules, each
First/Middle/Last and Day/Month/Year cell is centred on the form's own printed hint, and the wife's
column is the husband's shifted 305 pt (her hints sit exactly 305 pt right). The .rpt opens in the
VS Crystal designer, but a position changed only there is overwritten on regeneration - Reports
README section 8 says to change Mf90Form.Cells and regenerate.
The datasource is an ADO.NET table (mf90_application), not a SQL view: Mf90Form.BuildTable builds
the one row already split into the form's boxes, so the report only places text. Values:
  - "May I apply ... with ___" carries the OTHER party's name in each column.
  - Date of birth as Day / Month / Year, age on the FILING date (the figure the screen shows).
  - A birth abroad has no province, so the country rides in the province box ("Japan") rather
    than being dropped - the form has no country box.
  - The previously-married block prints only for Widowed / Annulled / Divorced - the third place
    that rule is enforced (screen, SaveLicense, now print).
  - Date Receipt = filed date; Date of Issuance = issue date.
  - REGISTRY NO. IS LEFT BLANK: it is the office's register number for the application, and
    CROMS's own application number is not necessarily that. Printing it there would assert it is.
  - Degree of Relationship is not printed - not captured (backlog 14).
An over-long value cannot grow on paper, so both renderers would clip it silently. Mf90Form.
Overflows measures each value against its box and the preview shows a warning banner naming it
before anything is printed.
The window saves the application first: the paper the applicants sign must be the record CROMS
holds, not unsaved boxes.

ZOOM. New Modules/CtrlWheelZoom: an application message filter, because the wheel never reaches
the host - the Crystal viewer and PrintPreviewControl hand it to inner child windows that scroll
and swallow it. Takes WM_MOUSEWHEEL only with Ctrl held and only over the target (lParam screen
point), ~15% per notch snapped to 5%, 10-400%; plain wheel still scrolls. CertificateViewerForm
gained a zoom bar (-, %, +, Fit page, Fit width, 100%) and Ctrl + / Ctrl - / Ctrl 0, opens at fit
page computed from the report's own page size, and HIDES the viewer's zoom dropdown - the viewer
cannot report its zoom back, so two controls changing it would leave the % shown wrong. Every
certificate shown through Crystal gets this, not only MF-90. The filter is removed on close.
Fallback: Forms/ZoomPrintPreviewForm, the same bar and gestures around a PrintPreviewControl, with
Print; used when the .rpt is missing, the Crystal runtime is not on the PC (it is per machine and
a client PC may not have it), or the report throws - the operator is told and still gets the form,
drawn from the same cells.

THREE DEFECTS FOUND BY RENDERING, none by compiling:
  1. A BLANK PAGE. The first saved .rpt exported empty. Diagnosed by reading the saved report's
     structure: the picture made the Detail section 19521 twips tall on a Letter page holding
     15500, so the section fit no page. Fixed with the 8.5 x 13 user paper size and zero margins.
  2. A BLANK SECOND PAGE, twice. Setting Detail 1 pt shorter than the page did not fix it -
     measured, the saved section was 18721, not 18700: importing the picture at its native height
     grows the section AFTER its height was set, and shrinking the picture does not shrink it
     back. The height is now set LAST. Verified 1 page, 612 x 936 pt.
  3. A 10 MB report. Crystal stores an embedded picture as an uncompressed bitmap; the blank is
     black line art, so it is exported as 8-bit grayscale: 10.0 MB -> 2.5 MB, no visible change.
Also: single-line values rode ~2 pt high against the gutter labels - nudged, re-rendered.

VERIFIED BY RUNNING. CROMS.MarriageTest --mf90 <dir>: saves a complete licence through
SaveLicense, builds the row (77 columns = 77 boxes; apply-with swapped; DOB split, age 30; foreign
birth; previously-married only for the wife; registry blank), renders it BY CRYSTAL to PDF with
the shipped .rpt and by the fallback to PNG, checks the overflow warning, then opens the real
viewer and drives zoom with a REAL Ctrl key state (keybd_event) and a real WM_MOUSEWHEEL: opens at
65% fit, Ctrl+wheel up 65 -> 100%, down 100 -> 85%, plain wheel leaves it at 85%, Fit page returns
to 65%. 15/15, 0 strays. The PDF and both on-screen viewer captures were rasterised and LOOKED AT:
every value in its own box, centred under the form's hints, both columns, header dates on their
rules, 1 page, and Crystal's own status bar reading the same zoom factor. Regression: --form90
17/17, workflow suite 74/74, 13 marriage windows re-render, 0 leftovers. MSBuild clean, bin\Debug
updated.
Harness trap, third appearance of this family: a Graphics takes a bitmap's DPI when it is CREATED,
so SetResolution must come before Graphics.FromImage or the page draws at 96 DPI.

NOT DONE, plainly: (1) Parental Consent (MF 06) and Advice (MF 68, two sheets) - they arrived as
photographs, so each needs a clean blank scan first; (2) CROMS's own licence printout
(LicensePrinter) is unchanged and still shows only joined parent names; (3) MF-90 is not listed in
Settings -> Certificates & Forms, which reads FormCatalog (registry certificates only); (4) office
PCs need the Crystal runtime for the Crystal path - without it they get the identical fallback
page, by design, but worth knowing before a demo.

2026-09-13 (office answers recorded; kiosk fixed; advice 21-25; BREQS built end to end) - The user
supplied the fee card again, the Consent and Advice blanks as Word files, the advice-band answer
(21-25) and the BREQS specification, and chose: commit first, urgent fixes, then BREQS on BOTH the
kiosk and the staff side, and the "+ 30" is part of the fee.

GIT. The repository had ONE commit; 147 changed files and every source file added since the initial
import (MarriageService, CertificateReport, DocIntelligence, migrations 23-38, the kiosk rework, the
Analytics module...) had never been committed. Committed as 8e30fe8 (223 files). Build products are
now ignored instead of committed (CROMS_Bundle/, the bundle and mockup zips, output/, tmp/).
Caught while committing: .gitattributes is `* text=auto` with core.autocrlf=true, and a VECTOR PDF
is mostly ASCII, so git classed the MF-90 blank as TEXT - a fresh checkout would rewrite its line
endings and corrupt it. The stored bytes were verified identical; *.pdf/*.rpt/*.docx/*.xlsx are now
marked binary (a0760a7).

URGENT FIX 1 - THE KIOSK COULD NOT ISSUE TICKETS. Migrations 34/35 had never been applied, but the
kiosk INSERT already wrote valid_id_type / spouse_full_name / spouse_image, so every ticket submission
failed. Applied both (ASCII-checked, no leftover procedures) and ran the kiosk's own INSERT inside a
transaction: it succeeds; rolled back.

URGENT FIX 2 - ADVICE BAND 21-25, CONFIRMED BY THE OFFICE. Migration 39 sets MARRIAGE_ADVICE_AGE_TO
= 25 and marks consent 18-20 confirmed (the office's consent form says "less than twenty-one"); code
default follows; new boundary test (25 needs advice, 26 does not). Suite 75/75.

FEES, recorded not yet built: "+ 30" is part of the fee (certified copy 80, certification 130), and
the fee card itself answers the petition question - the fee follows the petition type (CCE-9048
1,000; CCE-10172 3,000; CFN-9048 3,000; migrant 1,000), so PET-9048 must split. Burial permit and
transfer of cadaver still have no amount on the card. Scheduled after BREQS.

BREQS - PSA-issued copies of birth, marriage and death certificates. Migration 40 (APPLIED, run
twice): breqs_requests (52 columns), breqs_history (cascade), settings BREQS_TURNAROUND_DAYS 7,
BREQS_UNCLAIMED_DAYS 30, BREQS_FEE_PER_COPY 50 (fee card). The document details live in generic
columns whose meaning follows doc_type (owner = registrant / husband / deceased; spouse = wife;
event_* = date and place; father/mother = birth only) - one table, no per-type copies.
  STATUSES (the user asked CROMS to choose): Requested -> Paid -> Submitted to PSA -> Received from
PSA -> Released, with exits No Record at PSA and Cancelled. "Overdue at PSA" (past submitted +
turnaround) and "Unclaimed" (received, not released after 30 days) are DERIVED, never stored, so
they cannot go stale - the marriage licence's "Expiring" rule.
  Data/BreqsService.cs owns every write. One Move() refuses any jump the workflow does not allow
("a request that is requested cannot be marked released"), writes history + audit_log, and updates
with `WHERE status = @from` so a double-click or a second PC acting on the same request cannot record
the move twice. Details are editable while Requested/Paid and LOCKED once sent to PSA - what PSA was
asked for must stay what the record says was asked for. Spouse is only kept on a marriage request,
parents only on a birth request. Numbers BREQS-YYYY-#### with retry on the unique index.
  RECEIVING A PSA COPY - the part the office specifically asked for. "Receive & scan PSA copy" loads
the scan, runs it through the SAME DocumentAI engine as Intelligent Document Processing (background
task, progress shown), reads the name the copy is FOR (child / husband / deceased by type) and
compares it to the request on normalised names: Match / Partial / Mismatch / Unread, plus "Wrong
document" when OCR reads a different certificate type. It is a flag, never a gate: the copy can be
attached after a warning, and Release asks the staff member to confirm with their own eyes when the
flag is not Match. The scan is stored on the request (kept copy) and the run is logged to ocr_batch
with record_table='breqs_requests', so it sits in the same OCR audit trail as every other scan.
  STAFF: new module "PSA Copies (BREQS)" under Certification (Registrar, Staff, Releasing, Admin):
four tiles (to be paid / to submit / at PSA with overdue count / ready with unclaimed count), the
list, and a detail panel with one next-step sentence, only the actions the status allows, every
recorded fact and the history. Dialogs for new/edit (fields change with the certificate), Treasury
O.R., submission (shows the expected date), receive-and-scan, release (claimant or representative +
ID). New Data/GovIds.cs - the government-ID list Release & Claim had inline, now shared.
  KIOSK: the placeholder service "BREKS"/"Breks" (a misspelling, never used by any stored row) is now
"PSA Copy (BREQS)" in the kiosk, window routing and queue. Choosing it adds a "PSA Document" step
(the step indicator becomes 3 steps) between Select Services and Personal Info: certificate as three
cards (one at a time), copies, purpose, relationship, valid ID type + number, and the document
details, which follow the certificate (wife only for marriage, parents only for birth; the gap
closes). KioskCore.Submit validates the request BEFORE the ticket is inserted - a half-filled
request never leaves a ticket behind - then saves the request linked to the ticket (source Kiosk).
Serving that ticket in Queue Management opens the BREQS desk on that exact request; a ticket with no
request opens a new one with the ticket's contact and ID prefilled and its JOINED name shown as a
hint rather than split into boxes (the two-word-surname failure again).

DEFECTS FOUND BY RENDERING, none by compiling:
  1. The kiosk step's red * markers sat on top of caption text ("Las*", "ID num*er") - placed from
     an estimated character width. Now placed from each caption's measured width, after scaling.
  2. At 1366x768 the whole step collided: fit-to-screen shrinks positions (Control.Scale) but not
     fonts under AutoScaleMode.None, so full-size text ran into shrunken boxes. Fonts now scale with
     the box when it shrinks (explicit fonts only - inherited ones would be scaled twice). Verified
     at 1920x1040 and 1366x728. NOTE: the existing Personal Info & Photo step has the same weakness
     (recorded 2026-08-29) and was NOT changed in this pass.
  3. Captions with "(optional)" ran into the next field and off the card - shortened; one hint line
     "Only * is required" instead.
  4. The desk printed a place as "Tuguegarao City Cagayan" (no comma), and the Copies header clipped.
Harness lesson: a kiosk step must be SIZED BEFORE Show() - its fit-to-screen runs once on Shown, so
resizing afterwards rendered a 62%-scaled box that no real maximized kiosk would show.

VERIFIED BY RUNNING. CROMS.MarriageTest --breqs <dir>, against the live database: the workflow rules
(allowed/refused moves, overdue/unclaimed boundaries, name matching incl. "Dela Cruz" vs "DELACRUZ",
validation messages); a counter request through every status with a fee of 50 x 2 copies, details
editable then locked, expected date = submitted + 7; the office's REAL birth scan (Downloads\Birth
Certificate.jpeg) through the real OCR engine: Birth 99% class / 75% recognition, name read
"SHELLIAN CLEAR TALOSIG" -> Match, logged to ocr_batch; the same scan on a DEATH request -> "Wrong
document"; receive-twice refused, release requires the claimant's ID, 6 history rows, audit written;
no-record and cancel exits; the kiosk's own validation and save path loaded from CROMS.Kiosk.exe,
linked to a test ticket and found again by ticket; desk, dialogs and the kiosk step rendered and
LOOKED AT. 44/44, 0 strays. Marriage suite 75/75. MSBuild clean for CROMS, CROMS.Display,
CROMS.Kiosk; bin\Debug updated.

NOT DONE, plainly: (1) the PSA copy is not attached to the request from the Intelligent Document
Processing screen - receiving happens in the BREQS desk, which uses the same engine; (2) no printed
claim stub with the BREQS number for the client (the queue ticket still prints); (3) CENOMAR is not
offered - the office named birth, marriage and death; (4) the Select Services step still shows a
2-step indicator even when BREQS is picked (the later steps show 3); (5) the kiosk certificate cards
clip their small "TAP TO SELECT" line at 1366x768; (6) the fee schedule changes, Consent/Advice
printouts and the Personal Info scaling fix are the next items.

2026-09-13 (fee schedule + one payment log + monthly collection) - Backlog section 8 built. The
schedule now matches the office's fee card, every payment from every source goes into one itemised
log, and the month's collections report from it. Migration 41 was applied during the previous
session; re-run this pass to prove idempotent (21 fees unchanged, no leftover procedure, ASCII only).

FEE SCHEDULE (migration 41). Seeded placeholders corrected ONLY where the seed value was still
there, so re-running never overwrites an amount the office edited: certified copy 155 -> 80,
certification 210 -> 130 (the "+ 30" is part of the fee, per the office), PET-10172 -> 3,000. The
single PET-9048 code could not carry both fees the card lists, so it is retired and replaced by
PET-9048-CCE 1,000 and PET-9048-CFN 3,000. Ten fees on the card that had no row were added. The
REG-BIRTH/MARRIAGE/DEATH rows (0.00, not on the card) are deactivated rather than kept at a "free"
nobody stated. fees.amount became NULL-able: burial permit and transfer of cadaver have NO amount on
the card, and 0.00 would print "free" on a slip - NULL means "not stated, the cashier types it".

ONE PAYMENT LOG. payments.transaction_id is now NULL-able and gained payer_name, purpose (the
"purpose of transaction" the office asked for), source and source_table/source_id; new
payment_items holds which fees one O.R. covered. Payments recorded before this are NOT itemised
after the fact - what they covered was never recorded - and report as "not itemised".
New Data/PaymentService.cs owns every write:
  - An Official Receipt is an accountable form issued once, so an O.R. already in the log is
    refused. One O.R. covering several fees is ONE payment with several lines.
  - A module recording its O.R. (BREQS, marriage licence) ADOPTS a matching unlinked walk-in row
    instead of counting the collection twice; an O.R. linked to anything else is refused.
  - EnsureOrFree runs BEFORE BREQS or the licence changes its own status, so a refused receipt
    cannot leave a Paid request with no log row.
  - BreqsService.RecordPayment now logs a BREQS line (copies x per-copy fee).
    MarriageService.RecordPayment logs ONE UNITEMISED line: the licence O.R. may cover application,
    licence and solemnization together and which of them it covered is not captured, so splitting
    it would invent the breakdown. No amount means nothing is logged.
  - BREQS's per-copy fee now reads the fee schedule first; app_settings BREQS_FEE_PER_COPY is only
    the fallback. Otherwise editing BREQS on the schedule would silently not change what BREQS charges.
  - Fee changes (Admin/Registrar) are audited with old and new amount; an unchanged save writes nothing.

SCREEN. Fees & Payments is now five tabs: Awaiting payment (the original transaction flow, now
charging copies x fee - it charged one copy whatever was requested), Walk-in / other payment
(payer, purpose, several fee lines, a scheduled amount is locked while an unpriced one is typed),
Payment log (date range + search, CSV), Monthly collection (NEW: by fee, by source, by method - each
table adds up to the month's total, with "additional charges" and "not itemised" as their own fee
rows for exactly that reason), and Fee schedule. The printed slip lists every fee line and still
says "not an Official Receipt".
  Monthly by-fee groups by FEE CODE, named from the schedule. The first cut grouped by code AND
description and split BREQS into two rows because two modules worded the line differently - a fee
reworded next year would have split the month the same way.
  Two pre-existing defects on the Awaiting tab, found by rendering: "Total Amount" ran under the bold
total beside it ("Total Amou"), and "Date & Time Paid" lost its '&' to the Label mnemonic trap -
the fourth time that trap has appeared in this codebase.

TESTS. New CROMS.MarriageTest --fees (57 checks, live DB): every card amount, assessment x copies,
all validation rules, itemised walk-in, duplicate O.R. refused, walk-in adopted by a module, BREQS
logging and refusal leaving the request Requested, a January-2099 month whose three breakdowns
reconcile to PHP 535, an audited fee change restored, and all five tabs rendered and looked at.
Marriage suite gained two checks (licence payment in the log; same O.R. on another licence
refused). Results: fees 57/57, BREQS 44/44, marriage 77/77, Form 90 17/17, MF-90 15/15. MSBuild
clean, 0 warnings 0 errors.

MISTAKE MADE AND CAUGHT, and it touched real data. The test cleanups deleted audit_log rows "by
payment id". Test payments reused ids 22-46, and eight OLDER audit rows still named those ids
(their payments had been deleted before today - the rows were orphaned trail, not live payments).
Those eight rows were deleted at 15:12-15:20. Found because one assertion counted two audit rows for
one payment. Confirmed from the MySQL binary log (MSI-bin.000524, read with mysqlbinlog
--read-from-remote-server) that ONLY those eight audit rows were real - all 32 deleted payment rows
were ZZT/ZZB/ZZF test rows. The exact row images were recovered into
tmp/restore_audit_rows_2026-09-13.sql (INSERT IGNORE, same ids/users/timestamps); applying it was
blocked by the session's permission rules, so IT STILL HAS TO BE RUN BY HAND. All three cleanups now
delete a payment audit row only when its details name that test payment's O.R., and a before/after
check across all suites showed pre-existing audit rows unchanged (1138 rows, same id sum).
  Shape worth keeping: an id is not an identity once rows are deleted. Deleting "everything that
references id N" in a table that outlives the rows it references deletes history.

NOT DONE. Burial permit and transfer of cadaver amounts (office to state). Petitions still charge
nothing - which filing fee a petition takes is now expressible but not wired. The Awaiting-payment
path (transaction -> ForRelease) is exercised only by rendering, since it raises MessageBoxes.
Consent/Advice printouts are next.

2026-09-13 (audit rows restored) - tmp/restore_audit_rows_2026-09-13.sql was run by the user.
Verified from this side: all 8 rows (441, 551, 719, 755, 811, 851, 993, 995) are back in audit_log
with their original ids, users, actions (Create) and timestamps. The loss recorded in the fee-schedule
entry above is fully repaired.

2026-09-13 (backlog Phase 3C: Consent MF-06 and Advice MF-68 printed) - The two forms named in
§11/§13 as still needing printouts are done. Neither has a scanned blank of the office's own
stock - both arrived as re-typed Word documents - so an image overlay was never going to be a
true replica anyway. Instead every printed line was measured (static text and field position, in
points, top-down) and CROMS draws the whole page directly: same fidelity as an overlay, no PNG
to source, no Crystal .rpt to author.

New Data/ConsentForm.cs, Data/AdviceForm.cs. Consent (MF-06) prints ONCE PER PARTY - whichever is
18-20 on the filing date (FC Art. 14) - naming the other as the intended spouse; a couple where
both are underage gets both forms shown in turn, not merged onto one, matching what the paper
itself is (one affidavit per underage applicant). Advice (MF-68) prints as ONE page carrying both
the MALE and FEMALE halves, because that is what the office's actual document is - not two
separate sheets, as an earlier entry (2026-09-13, morning) had assumed before the real form was
measured. Offered whenever either party is 21-25 (FC Art. 15).

NOTHING IS EVER WRITTEN INTO A SIGNATURE LINE. Every signature/oath-administering field draws a
blank horizontal rule, never text - CROMS does not capture a signature image for these forms, and
a filled-looking signature line would be exactly the "looks complete, nothing behind it" failure
this project keeps refusing. The oath-administering officer's title is likewise left blank: whoever
administers the oath is decided at signing, not something CROMS has on file in advance.

A FABRICATION-SHAPED BUG CAUGHT BY RENDERING THE REAL PAGE, not by reading the code. Both forms
print ", 20__" as STATIC text - the "20" is part of the paper, only the last two digits are a
blank - and the first cut wrote the FULL 4-digit year into that blank, producing "202026" on the
rendered page. Same family as the CorrectDate fabrication fixed 2026-09-04 and the marriage-licence
date fabrication fixed 2026-09-10: a date-shaped field silently grew digits nobody asked for. Fixed
by writing only `year % 100`, comment left at both call sites pointing at the static text so the
convention isn't rediscovered as a bug next time either form is touched.

VERIFIED against the real blank PDFs, not against my own transcription. The office's blank MF-06
and MF-68 were converted to page images (a rendering pass unrelated to this one, found already on
disk) and compared line for line against the built ConsentForm/AdviceForm output: every static
sentence, every blank's position relative to its label, the WITNESSES parenthetical, the two
signature blocks on Advice (Father/Mother side-by-side, Guardian centered below), and both forms'
footer form numbers all match.

New CROMS.MarriageTest --consentadvice (8 checks, live DB): a licence saved with a 19-year-old
husband (consent band) and 23-year-old wife (advice band) in one couple, so one save exercises
both forms; band membership asserted both ways (qualifying party true, non-qualifying false);
ConsentForm.BuildTable maps applicant/spouse/consent-person correctly in each direction;
AdviceForm.BuildTable's male_/female_ halves match husband/wife; every signature/oath column
confirmed blank; Overflows() confirmed clean (after widening one field - see below); both pages
drawn to PNG and inspected; a 30/30 couple confirmed to need neither form. A harness-only DPI trap
was hit and fixed while building the test (recorded before, on 2026-09-07, for a different
renderer): a Graphics takes its bitmap's 96 DPI at creation, and PageUnit=Point then scales
everything by 96/72 - SetResolution(72,72) must run before Graphics.FromImage, not after.

One field widened after measuring the actual overflow: date_signed_year on the Consent form was
18.91pt, enough for the two digits it now holds many times over but originally sized for content
before the year-format fix — left at the wider 30pt since there was unused slack before the next
label and no reason to re-narrow it.

Regression: full suite re-run after these changes - marriage 77/77, Form 90 17/17, MF-90 15/15,
BREQS 44/44, fees 57/57, consent/advice 8/8 - 218/218, no leftovers. MSBuild clean, 0 warnings 0
errors.

NOT DONE, stated plainly: neither form is wired into Settings -> Certificates & Forms (that list
reads FormCatalog, which is registry certificates only - Consent/Advice are licence-workflow
documents, a different catalog entirely, matching how MF-90 already sits outside that list); no
Crystal .rpt was authored for either (a real one COULD be built the way MF-90's was, but there is
no scanned artwork to embed and the direct-draw approach already delivers the same fidelity, so it
was not worth the extra Crystal machinery for these two); the consent/advice-PERSON slot is still
not age-gated to a particular relationship (the office said one slot is needed, not who may fill
it - unchanged from Phase 3A); Degree of Relationship is still not captured (ask first, per §14).

2026-09-13 (backlog Phase 7: delayed birth registration - PSA MC 2024-17) - Birth registration
is now two workflows, not one, for the part that matters legally: a delayed registration carries
the office's own ten-item checklist and a 10-day posting period, tracked on the record itself.

REUSED THE MARRIAGE LICENCE'S ENGINE RATHER THAN BUILDING A SECOND ONE, per the backlog's own
instruction. MarriageService.Requirements(ownerType, ownerId) / SaveRequirement(ReqRow) /
SyncRequirements(ownerType, ownerId, needs) / Catalog() were already generic on owner type -
nothing in them names "License" or "Marriage" as a literal - so migration 42 adds
applies_to='Birth' rows to the SAME marriage_requirement_types / marriage_requirements tables
rather than a parallel birth_requirements schema. The licence's own RequirementsGrid screen
control binds to (ownerType, ownerId, needs) already, so it is reused UNMODIFIED for Birth.
SyncRequirements was private; widened to public so Birth's own service class could call it -
the only visibility change needed, no logic touched.

THE ONE GENUINELY NEW SHAPE. Item (c) on the office's card is "ANY TWO of the following eight
evidences of birth" - a group with a satisfy-count, which a flat per-row required/not-required
flag cannot express (demanding all eight, or accepting any single one, are both wrong readings).
ReqType gains GroupCode/GroupMin (both optional; every pre-existing row is GroupCode=null,
GroupMin=1, meaning exactly what it always meant - Catalog() reads the two new columns
defensively via DataTable.Columns.Contains, so a database still on migration 41 keeps working
unmodified). New DelayedBirthRules.EvidenceGroupSatisfied counts Verified rows in the group
against GroupMin - never against "all rows in the group" and never against "any one row".

CONDITIONAL ITEMS, SAME PRECEDENT THE LICENCE ALREADY SET. Items (f)/(g)/(h) on the card are
conditional - on the registrant being deceased, on the parents being married or not, on the
mother being unavailable, on a parent being deceased. Encoded as their own catalog rows with a
rule key (RegistrantDeceased / ParentsMarried / ParentsUnmarried / MotherUnavailable /
ParentDeceased), exactly the shape ConsentAge/AdviceAge/PreviouslyMarried already use on the
licence - not one row with a text caveat bolted on. births.parents_married (already on the table
since 2026-09-10) is READ to decide ParentsMarried/ParentsUnmarried; nothing new is added for it.

births.is_delayed IS NOT DUPLICATED. It already exists and is computed honestly from the dates
(fixed 2026-09-08); this workflow reads that flag to decide whether the case even applies, and
adds only what the workflow itself produces - delayed_posting_start/end, three conditional
booleans (registrant/mother/parent), and the registrar's own evaluation text + who + when.
Nothing is backfilled on existing rows: a posting that never happened must not be invented for
a record already on file.

THE POSTING PERIOD IS A SETTING, NOT A CONSTANT - app_settings.BIRTH_DELAYED_POSTING_DAYS,
default 10 per PSA MC 2024-17, explicitly flagged CONFIRM WITH LCRO (whether this office
actually runs the posting step for a delayed birth, versus just collecting the checklist, is
still an open backlog §14 question - the code does not assume an answer, it exposes a number
the office can change without a rebuild, same reasoning as the marriage licence and BREQS
settings). StartPosting refuses a future start date (the posting is the notice going up TODAY,
not a date not yet reached) - the exact rule the marriage licence's own StartPosting enforces.

THE REGISTRAR'S EVALUATION IS ALWAYS THEIR OWN WORDS, NEVER A COMPUTED VERDICT.
DelayedBirthRules.AllSatisfied tells the SCREEN whether the checklist looks complete (every
blocking item Verified, the evidence group satisfied) - it is shown to the registrar as
information, never written to the record and never used to gate anything. What gets recorded
is the free-text finding they type and Save, stamped with who and when. A "looks complete"
computation standing in for a registrar's actual sign-off is exactly the kind of fabricated
certainty this project keeps refusing.

NEW Forms/DelayedBirthCaseForm.cs - a modal opened from a new "Delayed Registration..." button
on Birth Registration, enabled only for a SAVED record whose is_delayed is actually set (a blank
form or a timely one has nothing to open). Built in CODE, not the Designer - this project's own
history records Visual Studio silently deleting hand-added Designer controls on regeneration
more than once, and a button added purely in code cannot be lost that way. Reuses MUi/UiTheme/
Banner/RequirementsGrid/ToggleSwitch from the existing marriage-workflow toolkit wholesale.

TWO DEFECTS FOUND BY RENDERING THE REAL DIALOG, neither visible from the code:
  1. THE WINDOW OPENED SCROLLED PAST THE TOP. The Dock=Top children were added in REVERSE array
     order (the established rule in this codebase: last Controls.Add = topmost in a Dock=Top
     stack), which as a side effect also reversed the DEFAULT TAB ORDER - so WinForms focused a
     control near the visual BOTTOM on Show, and the AutoScroll panel followed it there,
     opening the case facts/posting/evidence line/most of the grid off-screen above the fold.
     Same trap already recorded for the MF-90 screen on 2026-09-13 (root cause identical: two
     independent behaviours - stacking order and tab order - both driven by one Controls.Add
     sequence, and getting one right can silently break the other). Fixed by stating TabIndex
     explicitly in visual reading order instead of leaving it to fall out of the Add order,
     plus a defensive AutoScrollPosition reset to (0,0) after load.
  2. DISABLED BUTTONS RENDERED AS BLANK GREY BOXES WITH NO CAPTION VISIBLE. Missing
     UiTheme.Polish(this) - this is a stand-alone modal dialog, never passed through
     MainForm.ShowModule's own polish pass, and every other dialog in Forms/ (MarriageLicenseForm,
     BreqsForm) explicitly self-polishes for exactly this reason. One line fixed it; the grid
     header, zebra striping and button styling all came in immediately after.

VERIFIED BY RUNNING, not by compiling. New CROMS.MarriageTest --delayedbirth (15 checks, live
croms): a case with parents-unmarried and mother-unavailable both true, registrant/parent-
deceased both false - proves the two applicable conditional rows appear and the three
inapplicable ones (marriage certificate, registrant's own death certificate, parent's death
certificate) do not, out of exactly 18 of the 21 catalog rows; the evidence group is proven
to need genuinely TWO verified rows (one verified is asserted insufficient, then two is
asserted sufficient - not "any one" and not "all eight"); posting refuses a future date and
computes the correct end date from the live setting; the evaluation is confirmed recorded with
its author and timestamp. The real dialog was rendered to PNG and inspected - correct toggle
states, correct banner wording and colour, the grid's own "Why it applies" column matching each
row's actual reason, evidence-group status line reading "2 of 2 required verified - satisfied"
in green. Full regression re-run across every existing suite: marriage 77/77, Form 90 17/17,
MF-90 15/15, BREQS 44/44, fees 57/57, consent/advice 8/8, delayed birth 15/15 - 233/233, zero
leftovers. MSBuild clean, 0 warnings 0 errors.

NOT DONE, stated plainly per this backlog item's own "explicitly not decided" list: whether
THIS office actually runs the 10-day posting step for a delayed birth, or only collects the
checklist, is still open (§14) - the posting UI is built and works, but using it is the
registrar's choice per the setting, not assumed. The delayed-affidavit's own specific fields
(the seven numbered clauses on the back of the MF-102 sheet, found 2026-09-13 earlier the same
day) are not yet captured as their own data - the affidavit is covered by the generic
REGISTRANT_AFFIDAVIT requirement row today, not by dedicated fields for whose birth, who
attended, the reason for the delay, etc. Attaching a scanned document to a requirement row
(the "Attach" button already in RequirementsGrid) works exactly as it does for the licence -
untested against a real delayed-birth scan specifically, since none was on hand.

2026-09-13 (later still - three §14 answers recorded, no code) - User answered part of §3, §4.1
and §5 of the backlog from the office. No code changed; the backlog file and this log are the
only edits.

§4.1 (another-province attachment) got a real answer: the trigger is not the applicant's
residence or birthplace, it is a marriage licence obtained in ANOTHER province being used for a
wedding solemnized HERE. Recorded as CONFIRMED trigger in the backlog. Which document proves it
is still not stated (probably the licence itself or a certified copy from the issuing LCRO, but
"probably" is not a fact about a government requirement) - stays PENDING.

§3 (counselling / CENOMAR / RA 10354 family-planning certificate) did NOT get a real answer. The
reply restated the consent (18-20) / advice (21-25, deferred three months if unfavourable or
unobtained) mechanic that is already CONFIRMED in this file - it does not say whether counselling
is demanded across the band, whether CENOMAR is required, or whether RA 10354 §15 is enforced
locally. Recorded as an attempted-but-inconclusive answer so nobody later reads it as "asked and
answered" when it only re-described a rule already settled.

§5 (Marriage Application vs Registration) similarly got a conceptual restatement - "application is
applying, registration is the wedding being done" - which confirms the split this section already
states but names no field, no registry-number rule and no PhilCRIS boundary. Both PENDING items
(what staff actually add at registration; the CROMS/PhilCRIS division) stand unchanged.

Backlog's own standing rule (§16): update the file when an item is answered, note it here. Done.

2026-09-13 (uncommitted work found + logged, not authored this session) — Five small fixes were
sitting uncommitted with no log entry: (1) kiosk `FontScaler`/card-geometry rework so the BREQS
step and the camera/details step scale their captions WITH the box instead of clipping/overlapping
on a shrunk screen (the same font-doesn't-scale-with-Control.Scale gap recorded for Personal
Info & Photo on 2026-09-13 earlier); (2) new `LearningLibrary.Attach(ComboBox, category)` overload
— the existing autocomplete/learn-on-Leave attach only worked on a TextBox, so an editable lookup
ComboBox (the Place of Birth hospital cell) had no autocomplete path; (3) Birth Registration's
`WireLearningAutocomplete` was attaching to `txtPlace`, the HIDDEN placeholder `CreateLookupCells`
leaves behind once the 3 Place-of-Birth comboboxes replace it — so the attach could neither
suggest nor learn anything; now attaches to `_pob[0]`, the real hospital combo; (4) the printed
marriage licence (`LicensePrinter`) showed only the joined father/mother name line — migration 38
(2026-09-13) added structured first/middle/last + citizenship + residence per parent and the print
path never picked it up; now prints both structured names (falling back to the pre-38 joined value
when a legacy licence has no separate cells) plus citizenship/residence for each parent; (5) new
migration `43_birth_country_in_view.sql` restates `v_birth_certificate` to expose
`births.birth_country` — the column has existed since migration 37 (2026-09-12) but the view was
never restated, so a foreign birth's country was stored and shown on screen yet invisible to
anything reading the view (a printed certificate, FormCatalog's structured section). No schema
change, copied verbatim from 31_parents_married.sql's definition plus the one column.
MSBuild clean, 0 errors (temp OutputPath).

2026-09-13 (Phase 4: Legitimation / Supplemental Report / Legal Instrument / Court Order —
tracking-only, unblocked by research instead of waiting on the office) — Backlog Phase 4 was
blocked on "stages arrive" from the office. Asked instead to research the statutory stage shape
for each and build tracking directly, the same tier as the existing Petitions module (CROMS
RECORDS AND TRACKS; it does not run the legal procedure) — matching the backlog's own build-order
note that a single generic case-tracking table beats four near-copies "IF the stages turn out
similar," which the research confirmed.

RESEARCHED (web): RA 9858 legitimation (parents marry after the child's birth; Affidavit of
Legitimation registered at the LCRO of the place of birth; LCRO annotates the record and registry
book; forwarded to PSA); PSA's own Supplemental Report rule (up to TWO missing entries per report;
more than two must go to the Office of the Civil Registrar General, not be forced through this
one); RA 9255 legal instruments (Affidavit of Admission of Paternity / Affidavit of Acknowledgment
/ private handwritten instrument / AUSF, registered within 20 days of execution, LCR examines
authenticity then annotates); and Rule 108 / final-decision court-order annotation (winning party
files the decision + Certificate of Finality + Entry of Judgment at the LCRO, which annotates then
endorses to PSA). All four share one shape: Filed -> reviewed by the LCR -> registered/annotated
-> endorsed to PSA — the same shape Petitions already tracks, minus RA 9048/10172's statutory
15-day public-POSTING step, which none of the four new types carry.

`petitions` (already the generic tracker) widened rather than duplicated. Migration
`44_case_tracking_types.sql` (applied to the live croms database, idempotent — re-run confirmed
a no-op) widens `petition_type` to add Legitimation/SupplementalReport/LegalInstrument/CourtOrder,
and `stage` to add `UnderReview` — used by the four new types in place of `Posted`, since none of
them has a posting period. RA9048/RA10172 keep Filed -> Posted -> Decision -> PSA_Endorsement
unchanged; the four new types run Filed -> Under Review -> Decision -> PSA_Endorsement.
`01_schema.sql` updated to match for fresh installs.

`PetitionsForm` (renamed on screen "Petitions & Case Tracking", `petitions` table unchanged)
now carries two stage sequences instead of one fixed array (`StageCodesFor`/`StageLabelsFor`,
keyed on whether the chosen type is RA9048/RA10172). Choosing a case type repopulates the Stage
dropdown with the sequence that actually applies to it (`cboType_SelectedIndexChanged` ->
`RepopulateStage`), so a Legitimation case is never offered "Posted" and a correction petition
is never offered "Under Review". `AdvanceStage`/`Save`/`LoadPetition`/`ClearForm` all read the
sequence for the CURRENT type rather than a single hardcoded array. Grid query gained the four
new type labels and an explicit stage-label CASE (the old bare `REPLACE(stage,'_',' ')` would
have printed "UnderReview" with no space, since there is no underscore to replace).

DELIBERATELY NOT BUILT, matching the tracking-only tier: no requirements checklist, no posting-
clock engine, no per-type extra fields (legal basis, due-by date) — those belong to Tier-1
"CROMS ASSISTS" work like the marriage licence or delayed-birth registration, and this backlog
item was explicitly the other tier. A case's own paperwork/basis goes in the existing free-text
Remarks field. The Supplemental Report "max two missing entries, else escalate to OCRG" rule
and the legal-instrument 20-day registration window are NOT enforced in code — this module
tracks stage, it does not adjudicate the office's compliance with either rule.

VERIFIED against the live database: migration applied and re-applied idempotently; a Legitimation
case inserted with stage UnderReview, read back, rolled back (0 leftover). MSBuild clean, 0
warnings 0 errors (temp OutputPath — CROMS.exe running elsewhere holds bin\Debug on this machine
at times; REBUILD IN VS to pick this up if so).

NOT DONE: Legal Instruments/Court Order/Legitimation/Supplemental Report all still route to the
SAME `petitions` table and record/type picker Petitions already had — no per-type extra screen.
The backlog's Phase 4 items 10-11 (decide one table vs four) is now answered: one table, done.

2026-09-13 (Phase 3, item 8 — the another-province licence attachment, the last unbuilt marriage
§14 item with a known trigger) - Backlog build order flagged this item as blocked pending which
DOCUMENT proves the trigger. The TRIGGER itself was already confirmed by the office (backlog
Sec.4.1, 2026-09-13 earlier the same day): not the applicant's residence or birthplace, but a
marriage LICENCE obtained in ANOTHER province, solemnized HERE. So the trigger and the generic
"an attachment must be possible" requirement were buildable now; only the document's IDENTITY
stays open, and nothing here guesses at that - the new requirement's caption says so explicitly
and the attachment slot is generic, matching the build order's own instruction ("reuse the
marriage_requirements attachment path").

Migration 45_out_of_province_license.sql (applied against the built assembly's schema
expectations, registered in CROMS.csproj) adds `marriages.license_out_of_province` (a boolean
fact kept SEPARATE from `license_basis`, since Licensed vs Exempt and "which office issued the
licence" are two different questions - the same reasoning `PreviouslyMarried` already sits beside
Basis rather than folding into it) and a conditional `OUT_OF_PROVINCE_LICENSE` requirement row in
`marriage_requirement_types` (applies_to='Marriage', rule_key='OutOfProvinceLicense', blocking=1),
the same shape ConsentAge/AdviceAge/PreviouslyMarried/Delayed already use on this table.

WHY A REAL GAP, NOT JUST A MISSING CHECKBOX. Before this, `MarriageEntryForm`'s licence tab only
let a Licensed marriage link a LOCAL `marriage_licenses` row, and `MarriageRules.ValidateMarriage`
hard-blocked ("NO_LICENSE") on nothing being linked. A marriage solemnized here under a licence
issued by another LCRO has no local licence row and could never pass that check - so the ONLY way
to register one in CROMS was to mis-mark it Exempt (wrong: it IS licensed) or fabricate a fake
local licence record (worse). The checkbox and its own validation branch open a real path: when
checked, the licence number and issue date are typed in directly (they're the licence's own
stated facts, already have columns - `license_no`/`license_date`, added 2026-09-07 migration 30 -
and print onto the certificate the same way a linked licence's would via
v_marriage_certificate's existing COALESCE), and NO_LICENSE is skipped in favor of requiring
those two fields plus a marriage-date-not-before-issue check.

`Data/MarriageRules.cs`: MarriageFacts gains OutOfProvinceLicense/ExternalLicenseNo/
ExternalLicenseDate; Needs() gains an outOfProvinceLicense parameter and an
"OutOfProvinceLicense" rule-key case; ValidateMarriage's licence-link block branches on the new
flag before falling into the existing local-licence checks. `Data/MarriageService.cs`:
MarriageColumns whitelist gains license_out_of_province; LoadMarriageFacts reads the flag
(Columns.Contains-guarded, so a database still on migration 44 doesn't throw) plus the two
external fields from the existing license_no/license_date columns; SyncMarriageRequirements
threads the flag through to Needs() so the new requirement appears in the RequirementsGrid
exactly like every other marriage-level document. `Forms/MarriageEntryForm.cs`: a checkbox
swaps the local-licence search panel for a typed number/date panel plus a warning banner
("Attach proof of the out-of-province licence below... which document is still being
confirmed"); the rail, the register-confirmation dialog, the OCR-preview field map, and the
load/save round-trip all follow the same branch.

VERIFIED by compiling only (no live DB touched this pass - the change is schema-additive and
was reasoned from the existing, already-verified marriage workflow patterns rather than run
against croms). MSBuild (VS2019, CROMS.csproj) exit 0, 0 warnings 0 errors, built to a temp
OutputPath. Migration 45 NOT yet applied to the live croms database - run it before using the
new checkbox on a real record. bin\Debug not updated - rebuild in VS.

NOT DONE, stated plainly: the printed Marriage Application (MF-90) does not reflect
out-of-province status (MF-90 is the LICENCE application, filed at the ISSUING office - this
marriage's licence was never applied for here, so MF-90 for it was never CROMS's to print in
the first place; only the Certificate of Marriage, MF-97, is affected, and that already prints
correctly via the existing license_no/date/place columns). Which specific document to require is
still open - see item 5's consolidated list, item A1.

2026-09-13 (Phase 3 item 8, follow-up — scan or type the out-of-province licence, image and data
both saved) - The out-of-province licence panel built earlier today only took typed number/date.
Added a "Scan / attach license image..." button beside those two fields: it opens an image,
runs it through the plain `OcrService.Run` pass (no DocLayouts template applies - the issuing
LCRO's own form is unknown to CROMS, unlike Birth/Marriage/Death), and offers a licence number
and date it can find in the text via two narrow regexes (a token after "Lic.../No." or the
office's own YYYY-#### numbering shape; a named-month or numeric date). A suggestion only fills
a field that is still BLANK - it never overwrites what the operator already typed - and a
confirmation dialog states plainly what was read and that it needs checking against the picture,
or that nothing was read and both fields need typing. Manual entry alone, with no scan at all,
still works exactly as before.

BOTH THE IMAGE AND THE DATA ARE SAVED, per the ask, but not at the same moment - and the delay is
structural, not a shortcut. The image belongs to the `marriage_requirements` row for
OUT_OF_PROVINCE_LICENSE (the generic attachment slot item 8 already wired up), and that row does
not exist until the marriage record it belongs to has been saved once - `SyncMarriageRequirements`
creates it from inside `MarriageService.SaveMarriage`. So `ScanOopLicense` holds the scanned bytes
in memory only; `Save()` now looks the row up right after `SaveMarriage` returns and attaches the
held image to it in the same click the operator already used to save the record - one action,
both facts land. The licence number and date, being ordinary marriage columns, save immediately
with everything else on Save regardless of whether they were typed or read off a scan.

NOT A NEW OCR PROFILE. This deliberately does not join Birth/Marriage/Death's classified,
per-field extraction pipeline (DocLayouts/DocIntelligence) - that machinery is built and measured
against the office's OWN forms; an out-of-province licence is a different LCRO's own stock, of
unknown layout, and pretending otherwise would produce a confident-looking field that is really a
guess. Two narrow regexes over the whole-page text is the honest version of "try to help, never
claim more certainty than that" this project has kept everywhere else this kind of scan appears.

VERIFIED by compiling only - no live database or OCR engine exercised this pass (no sample
out-of-province licence image on hand to test extraction against). MSBuild (VS2019,
CROMS.csproj) exit 0, 0 warnings 0 errors, built to a temp OutputPath. Depends on migration 45
(not yet applied to the live croms database, per the earlier entry) - the requirement row this
attaches to does not exist until that migration runs.

2026-09-13 (Phase 2, item 5 — the §14 questions sent to the office as one list, no code) - New
"CROMS — Questions for LCRO Peñablanca.md" at the repo root: 24 items (A-H) pulled straight from
backlog §14, everything still genuinely open after the six documents arrived, plus two items that
were not questions before - confirming the MF-102 (2007) blank and the re-typed Consent/Advice
Word docs actually match the office's own stock (§13 flagged both as unverified). Struck-through
(already-answered) §14 items are not repeated. Backlog §15 Phase 2 marked DONE and its item 5
checked off, pointing at this file. Nothing here is a guess - it is the standing PENDING items
restated as one list to hand over, per backlog rule §16.

2026-09-13 (Records Archive — admin-only browser over everything ever saved) - New module,
Administration group, key "archive". One screen: a category tree on the left (Civil Registry
Records / Marriage Licensing / Petitions & Legal Instruments / Certification & PSA Copies /
Claims & Releases / Front Desk), a grid on the right, and a "View Full Record" button that opens
every column of the selected row plus a button for each stored scan/photo (routed through the
existing SoftcopyViewer, same viewer Birth/Marriage/Death already use for their softcopies - no
new image-viewing code).

Fourteen categories, one per table this system actually writes records/images/forms into: Birth
/ Marriage / Death registration (scan_image, birth_image), Marriage License applications (Form
90), the six petition_type values as SIX SEPARATE categories - Correction of Entry (RA9048),
Change of First Name (RA10172), Legitimation, Supplemental Report, Legal Instrument, and Court
Order (petitions is one table but the office thinks of these as different case types, so each
gets its own node rather than one "Petitions" bucket with a filter dropdown) - Certificate
Requests, PSA Copies/BREQS (scan_image), Claim Requests (uploaded valid ID), Releases (claimant
webcam photo), and Queue Tickets (kiosk face photo + spouse photo for marriage tickets).

Read-only by design - no INSERT/UPDATE/DELETE anywhere in the file, matching the Analytics
module's own rule. Admin-only the same way every other admin-only screen in this app already is:
the key "archive" is not listed in Registrar/Staff/Cashier/Releasing's AllowedKeys in MainForm,
and AllowedKeys returns null (full access) only for roles it doesn't recognise - which today
means only Admin.

Petition record-name resolution (which birth/death/marriage a petition is about) reuses the
exact CASE expression PetitionsForm's own grid already runs - one query pattern, not a second
one that could drift from it. Detail view is a generic "SELECT * FROM <table> WHERE id=@id" read
into a label:value list, so a column added to any of these tables later shows up here with no
code change; blob columns are excluded from that list and offered as their own "View <label>"
button instead, matching the pattern already used for scan_image with a shown/committed record.

Not built: a cross-category text search box (deferred - column-header click-to-sort already
works since every grid binds a DataTable) and marriage_licenses' own attachment scans, which
live on marriage_requirements keyed by owner type rather than on the license row itself.

MSBuild exit 0, 0 warnings 0 errors (temp OutputPath). bin\Debug not touched by this build -
REBUILD IN VS to pick this up if CROMS.exe is running.

### 2026-09-14 — Backlog §12/§14 "Workflow" answered: semi-admin staff roles + Phase 8 confirmed built

Two office answers closed the last open workflow item and let Phase 8 be marked done without new
plumbing — the pipeline it asked for already existed.

**Semi-admin staff permissions (§12).** Office: any staff member may cover any stage (receiving /
processing / releasing) on a given day, decided internally — not one fixed person per stage, and
one employee may hold several roles. `MainForm.AllowedKeys` (MainForm.cs) changed from four
different per-role module sets to ONE shared `OperationalKeys` set returned for Registrar, Staff,
Cashier and Releasing alike — every non-Admin role now sees the identical broad operational menu
(queue/transactions/certrequest/release/breqs/birth/marriage/death/petitions/search/ocr/fees/
reports). True admin config (Master Files, Settings, Users & Audit Trail, Records Archive) stays
Admin-only — the distinction that matters is operational vs administrative, not which of the four
staff titles someone holds.

**Phase 8 three-stage workflow — already built, no new code.** Office described the flow in their
own words: Window 1 only receives the request; Window 2 finds/prints the certificate and collects
payment; Window 3 only hands over the printed certificate; each window passes the transaction to
the next. That is exactly the Certificate Request pipeline built 2026-08-04: Create (**ForPrint**)
→ `CertificatePrintForm` find+print → "Proceed to Payment" (**ForPayment**) → Fees & Payments →
**ForRelease** → Release & Claim (**Released**) — one transaction, handed off by STATUS, not by a
hardcoded window identity, so any window/staff can pick it up at whatever stage it's at. That is
exactly what makes it compatible with the semi-admin answer above. Confirmed in
`CROMS — Agency Requirements Backlog.md` §15 Phase 8, no schema/code change needed. (BREQS already
has its own analogous 5-status pipeline, 2026-09-13; Birth/Marriage/Death registration and
Petitions are record CREATION, not certificate retrieval, so the find-and-print hand-off doesn't
apply to them the same way.)

**Marriage §14 items answered, backlog updated:** (1) how the office records a marriage
registration after receiving the Certificate of Marriage — nothing extra; save the certificate as
a scanned image on the record, which CROMS already has (`marriages.scan_image` + the shared
SoftcopyViewer every registration module uses) — matches the file's own Tier B framing (§0),
"records and tracks," not a rebuilt procedure. (2) CROMS ↔ PhilCRIS boundary — PhilCRIS is a
future API CONSUMER of CROMS data (pulls a client's info via National ID when they need it), not
something CROMS integrates into; no endpoint exists or was requested. (3) Advice form's
"(DECEASED)" annotation — confirmed no reason field needed, the handwritten note on paper is
enough. (4) Out-of-province licence proof document — confirmed NO document is required at all:
lawful to license in one province and marry in another, and the licence's existing 120-day
validity check (already enforced regardless of issuing office) is all that matters. Migration 45
(`CROMS/Database/45_out_of_province_license.sql`, not yet applied to the live DB) changed the
`OUT_OF_PROVINCE_LICENSE` requirement row from `blocking=1` to `blocking=0` (informational/
optional — attach a copy only if the applicant has one) and reworded its caption/legal_basis;
`MarriageEntryForm.cs`'s on-screen banner reworded from a Warning ("attach proof... which document
is still being confirmed") to an Info note stating no document is required. (5) Degree of
Relationship (MF-90, Family Code Arts. 37-38) — explained what "capture it" meant (a field for
the applicants' declared relationship, e.g. first cousins, used to catch legally prohibited
marriages) but stays PENDING — office has not yet said whether they want it recorded and whether
anything should act on a close-relationship answer.

Build: `MSBuild CROMS.csproj` clean, 0 errors (temp OutputPath via VS2022 BuildTools — this
machine has no VS2019 msbuild.exe on PATH, used 2022's instead; same compiler target, no issue).
`MainForm.cs`, `CROMS/Database/45_out_of_province_license.sql`, `CROMS/Forms/MarriageEntryForm.cs`
changed; migration 45 still not applied to the live croms DB (same as before this pass) — apply it
before relying on the reworded non-blocking requirement row showing up on a real record. REBUILD
IN VS to update bin\Debug if CROMS.exe is running elsewhere.

### 2026-09-14 (later) — Correction: fuller Marriage Registration interview finding recovered; no code change

Earlier the same day this file recorded the marriage-registration answer too thin ("nothing
extra, just save the scanned image"). User supplied the fuller interview finding (item 10 of the
original interview, previously only a PENDING placeholder in the backlog's §5): after
solemnization, staff receive the already-issued marriage licence and the accomplished/approved
Certificate of Marriage, and mainly do REGISTRATION-related work — add or verify dates, registry
information, signatures and similar registration details, then record the marriage. The office
also uses **PhilCRIS** as the existing system for PSA transmission/coordination, and the actual
civil-registration data for PSA still needs to be placed there separately (by staff, outside
CROMS) — CROMS's role is recording the marriage, scanning/attaching the relevant documents,
keeping the important marriage information, and maintaining an internal record; it is not
responsible for PSA transmission.

**No code change** — `MarriageEntryForm` (Form 97) already has every field this describes
(registry number, book/volume, status, date/time, solemnizer, the licence link, and the
certification-block fields from migrations 30/38) plus `marriages.scan_image` +
`SoftcopyViewer` for the attached documents. The fuller finding confirms the existing screen is
already the right scope — it corrects the earlier progress-log entry's *description* of the
answer (which undersold it as "just an image"), not the conclusion that nothing needs building.

Backlog updated: §5 ("Marriage Application vs Marriage Registration") marked CONFIRMED with the
full finding quoted and both its PENDING items resolved; §14's Marriage bullets for
"how registration is recorded," "which details staff add," and "CROMS ↔ PhilCRIS" reworded to
match and cross-reference §5, rather than repeating the thinner version. The separate,
unrelated same-day mention of PhilCRIS possibly calling a future CROMS API to pull
kiosk-collected client data by National ID is kept as a distinct, not-yet-requested integration
point — not conflated with the PSA-transmission answer here.

### 2026-09-14 (later still) — Marriage Registration (Form 97): place of birth standardized to Country/Province/Municipality

User supplied a fuller, formally-written version of the original interview notes for cross-check.
Auditing it against the running app confirmed the earlier answers hold, and surfaced one real gap
against item 1 ("place of birth split applies wherever it's recorded, not limited to one module"):
`marriages.husband_birth_place_id` / `wife_birth_place_id` were still a single bare FK id with no
country at all — the only place-of-birth fields in the app not already on the Country/Province/
Municipality trio. Birth Registration and the Marriage Licence (Form 90) already had it
(migrations 36/37, 2026-09-12). Built on request.

A SECOND, PRE-EXISTING DEFECT surfaced while fixing the first, unrelated to this session's own
work: the FK constraints on those two columns (`01_schema.sql`, 2026-07 build) reference
`hospitals(id)`, but every query touching them (`MarriageEntryForm`'s `ReloadMunis`, and
`v_marriage_certificate`'s own `LEFT JOIN municipalities hbp ON hbp.id = m.husband_birth_place_id`)
has always treated the value as a MUNICIPALITY id. A place of birth is a municipality, not a
hospital — the queries were right and the constraint was wrong from the start. Not touched
directly (risk not worth it for a column now being retired from the write path); documented in
the new migration so nobody re-derives the same confusion from the schema file alone.

**Storage changed to TEXT, matching the rest of the app, not to a second FK.** Every other
place-of-birth field CROMS has (`births.place_of_birth`, `marriage_licenses.husband_place_of_birth`)
is a joined "Municipality, Province" VARCHAR plus its own country column — because an id-based FK
cannot hold a birthplace with no PSGC row (there is no `municipalities` entry for Osaka). Migration
46 (`CROMS/Database/46_marriage_registration_birthplace.sql`, NOT yet applied to the live DB) adds
`marriages.husband_place_of_birth` / `wife_place_of_birth` (VARCHAR(150)) and
`husband_birth_country` / `wife_birth_country` (VARCHAR(80), matching migration 37's shape on
`marriage_licenses` exactly) and restates `v_marriage_certificate` to read the new columns first,
falling back to the old FK-joined name only for a row saved before this migration — nothing
already on file goes blank. No row is backfilled. The deprecated `husband_birth_place_id` /
`wife_birth_place_id` columns are left in the table (harmless, unreferenced) rather than dropped.

**`GeoLookup.JoinPlace`/`ProvinceOf`/`MunicipalityOf` promoted out of `MarriageLicenseForm.cs`
into `GeoLookup.cs`** as public statics, so Form 90 and Form 97 share one join/split
implementation instead of `MarriageEntryForm` growing a second private copy of the identical
logic — `MarriageLicenseForm.cs`'s three private methods now just forward to `GeoLookup`.

`MarriageEntryForm.cs`: `SP.BirthCountry` added; `PartyInner` swaps the old ad-hoc
`Bind`/`ReloadMunis` province-only wiring for `GeoLookup.LoadCountries` +
`GeoLookup.CascadeCountry` (editable combos, so a foreign locality can be typed — matches Form 90
and Birth exactly); the place-of-birth block goes from one 2-column row to two rows (Country +
Province, then Municipality alone) — same layout call Form 90 already made for the same reason
("City/municipality of birth" is the longest caption on the card). Load reads the new columns via
`GeoLookup.SetCountryPlace`, falling back to `GeoLookup.HomeCountry` when a legacy row has neither
column set. Save writes `GeoLookup.JoinPlace(municipality, province)` +
`N(BirthCountry.Text)` instead of the old `FkVal(BirthMuni)`. `MarriageColumns` in
`MarriageService.cs` updated to match (old `*_birth_place_id` keys removed from the whitelist, so
nothing can write them again by mistake).

**Layout bug caught before it shipped, not after:** the husband/wife card row is a FIXED-height
`TwoColumns(344, ...)` and the inner content panel has no `AutoScroll` — adding the second
place-of-birth row (5 rows -> 6 at 56px each) would have silently clipped the Civil
status/Residence row off the bottom of the card with no scrollbar to reach it. Row height raised
344 -> 400 before ever running it, reasoning from the same math Form 90's own layout already
had to solve.

Build: `MSBuild CROMS.csproj` clean, 0 errors (temp OutputPath, VS2022 BuildTools — this machine
has no VS2019 msbuild.exe on PATH). Not run against the live database — migration 46 still needs
applying, and the change was verified by compile + reading the exact query/save paths, not by
driving the real form (no interactive desktop / live croms connection in this session). REBUILD IN
VS to update bin\Debug if CROMS.exe is running elsewhere; apply migration 46 before saving a
marriage registration record, or the INSERT/UPDATE will fail on the two new columns.

NOT DONE, stated plainly: OCR extraction (`DocumentAI.ExtractMarriage`) still does not fill
Form 97's place-of-birth fields at all — `MarriageEntryForm.PrimeFromExtraction` has no
place-of-birth handling today (checked; pre-existing gap, not introduced or worsened here). Barangay
was deliberately not added — no PSA marriage form in this codebase asks for one at the place-of-birth
level (matching Birth Registration's own choice not to add it there either).

### 2026-09-15 — Registry Books module built; Book Page added to Birth and Death Registration

User asked for the book/page pair to be enterable on all three registration screens and for a
real Registry Books screen showing the year (volume) of a book and its pages — the module was
still the 2026-07-09 title-only stub, dropped from the sidebar that same day and never rebuilt.

WHAT WAS ALREADY THERE, checked before writing anything. `births.book_volume` has existed since
the first Form-102 build, and Marriage's `MarriageEntryForm` already had BOTH `_book` and
`_page` wired into its save dict — migration 26 (2026-09-06) added `book_page` to all three
tables and `book_volume` to marriages/deaths specifically so a certificate could print book/page
for any of the three, but only Marriage's screen ever got the matching input boxes. Birth had
Book/Volume but no Page box; Death had NEITHER box — two live database columns with no way for
staff to fill them on the form the office actually uses.

BIRTH REGISTRATION: added `txtBookPage` as a new row (row 8) in the Certification tab's
`tblCert` grid — the existing 4-column layout's remaining cells at the Book/Volume row and the
Remarks row are already occupied (Remarks' textbox spans all 3 value columns). `tblCert` grew
from 8 to 9 rows / 404 to 448px. Wired into `Columns`/`ValuePlaceholders`/`SetClause`/
`FieldParams`/`LoadBirth`; `ClearForm` needed no extra line — it clears every input control
generically via `EnumerateInputs`/`ClearControl`, which already covers a plain TextBox. Added
Page to the Recent Registrations grid beside the existing Book column.

DEATH REGISTRATION: added BOTH `txtBookVol` and `txtBookPage`, since neither existed. Placed as
a new label+textbox pair at the bottom of the "Deceased Information" groupbox (there was ~60px
of unused space below the Religion field). `grpDeceased` grew 407->452px, so the button row and
`grpCert`/the records grid were nudged down 45px to stay clear (480->525, 492->537, 855->900,
875->920); `ClientSize` grew 1200->1245 — the form is embedded with `AutoScroll=true` by
`MainForm.ShowModule` regardless, so this only tidies the design-time canvas. Wired into
`Columns`/`ValuePlaceholders`/`SetClause`/`FieldParams`/`LoadDeath`; Death's `ClearForm` is NOT
generic (clears each control by name), so `txtBookVol.Clear()`/`txtBookPage.Clear()` were added
explicitly. Added Book/Page to the Recent Death Registrations grid.

MARRIAGE REGISTRATION: unchanged — `MarriageEntryForm` already reads/writes/loads both fields
(confirmed before touching anything).

REGISTRY BOOKS, built as a real read-only screen. Two grids: BOOKS ON FILE (one row per Type +
Volume — the office's staff-typed "book/year" — with a live COUNT of records and a COUNT
DISTINCT of the pages actually used in that volume, via three independent `GROUP BY
book_volume` queries UNION ALL'd together), and clicking a book fills RECORDS IN THE SELECTED
BOOK below it (Registry No / Page / Name / Event Date / Status for just that volume, ordered by
page). Double-clicking a record jumps to its registration module, reusing the exact
`Shell()`/`GoToModule` pattern `RecordSearchForm` already established for the same "found it
here, go edit it there" hand-off. A record with no book/volume recorded groups under "(no
volume recorded)" rather than being silently dropped. The table name in the per-type query is
chosen by a `switch` over the fixed literal Type value ("Birth"/"Marriage"/"Death"), never from
user input, so building it as a string is not an injection vector.

Registered as module key `"books"` in `ModuleRegistry` (Petitions & Search group), added to
`MainForm.OperationalKeys` (every non-Admin role sees it — it's a lookup screen, not an admin
function), and given a sidebar nav button (`btnBooks`, Tag="books") in `MainForm.Designer.cs`.

VERIFIED by compiling only — no live database touched this pass (schema unchanged; every column
already existed). MSBuild (VS2019) `CROMS.csproj` clean, 0 errors, 0 warnings, temp OutputPath.
GUI not clicked (no interactive desktop) — rebuild in VS to see the new Book Page boxes on
Birth/Death and the Registry Books screen on the sidebar.

NOT DONE: no way to type book_volume/book_page from the Registry Books screen itself (values
are entered on Birth/Marriage/Death Registration, where the record's other facts are typed, not
on a separate catalogue screen); Birth's Book/Volume box is still free text exactly as
Marriage's and Death's now are (nothing validates it against an actual physical book count or
enforces one page per record).
