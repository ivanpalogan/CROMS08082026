# Intelligent Document Processing — Specification

One engine, one screen (`OcrDigitizationForm`, sidebar name **Intelligent Document Processing**), one review grid. OCR Digitization and Document AI were always the same OCR run with two review surfaces; they are now one workflow: straighten → read → classify → extract → score → correct → validate → human review → act.

| Action | Applies to | Ends in |
|---|---|---|
| **Commit to Birth Registry** / **Save as Draft** | Birth (MF-102) only | A row in `births`, status `Registered` or `Draft` |
| **Auto-Fill …** | Any recognised class | Birth / Marriage / Death registration form, pre-filled, scan attached |

---

## 1. Purpose

Convert scanned PSA civil-registry documents into registry rows without re-typing, and without sending civil-registry PII to any cloud service. Recognition is offline (Tesseract, `eng`). Classification is by the labels/headings the document contains — not fixed coordinates — so PSA form revisions and slight layout shifts still classify. New document types plug in by registering a `DocProfile`; the pipeline is untouched.

Supported classes: `DocKind.Birth` (COLB, MF-102), `Marriage` (COM, MF-97), `Death` (COD, MF-103), `Unknown`.

## 2. Inputs

- **Files:** `.jpg .jpeg .png .bmp .tif .tiff`. **PDF is not offered and not accepted** — the file dialog lists images only, and `DocumentAI.LoadImage` throws `NotSupportedException` for a PDF reached any other way.
- **Size cap:** longest side 4200px on load. Kept high on purpose — PSA print is small, and shrinking a 4000px scan discards strokes OCR cannot recover.
- **Engine:** `eng.traineddata` from `C:\Program Files\Tesseract-OCR\tessdata`, else `<app>\tessdata`. `IsAvailable()` gates every entry point.
- **Retained:** the original file bytes are held as the record's softcopy and travel with the record; they are never re-encoded.
- **Prerequisite:** migration `24_ocr_routing.sql` applied (`births.sex` NULL-able; `ocr_batch.doc_kind` / `record_table` / `record_id`).

## 3. Workflow

**3.1 Ingestion.** Operator loads/uploads one image. A new page invalidates the previous classification: scan id, field map and kind are cleared before anything else runs.

**3.2 Orientation.** Two checks decide which way is up. Tesseract's own detector (`osd.traineddata`) is trusted when its confidence is ≥1.0 — measured on the office's samples it was right every time at or above that (5.9 upright, 2.2 at 90°, 1.1 upside-down) and wrong the one time below it (0.6). Under that threshold a recognition probe at all four right angles picks the axis (a sideways death certificate scored 5292/5676 horizontal against 1960 upright), requiring a 1.2× margin over leaving the page alone, and OSD then settles the 180° flip on the now-nearly-upright candidate. Measured result: **12 of 12 orientations across the three samples corrected, each producing the same field count as the upright scan.**

**3.3 Image cleanup** (`OcrService.Preprocess`, per pass):

1. Normalize to 24bpp (source may be indexed, CMYK or 32bpp).
2. Luminance grayscale (0.299R / 0.587G / 0.114B) — weighted, not flat average, which matters on tinted security paper.
3. **Bradley/Wellner adaptive threshold** over an integral image: window `max(15, w/24)`, bias 0.15. A single global cut-off blacks out the dark side of an unevenly lit page — measured, it erased the entire middle of a real Certificate of Marriage.
4. **No upscale.** Upscaling a downscaled scan cannot restore strokes it already threw away.
5. Pixels via `LockBits` + `Marshal.Copy`, never `GetPixel/SetPixel` (~22s to ~7s per page).
6. **No despeckle.** An isolated-pixel filter was tried and removed after measurement: on the office's birth certificate it cut fields read from 22 to 17, because PSA print is thin at the resolution these pages are read at. Noise is handled by the adaptive threshold instead.

**3.4 OCR.** `PageSegMode.Auto`, `user_defined_dpi = 300`. Returns text, mean confidence (0–100), and **every word with its bounding box** plus prepared-page dimensions — flat text alone cannot say which column a value came from.

**3.5 Dual-resolution pass.** Run at 2400px (`PreferredLongSide`); if the source's longest side exceeds 2400 × 1.15, run again at native size and keep the better result. Score = `ExtractedCount × 1000 + OcrConfidence` (fields first, confidence breaks ties). There is no single best scale — measured: birth reads best near 2400px, marriage needs full size, and a 606–755px death certificate only reads once scaled **up**.

**3.6 Classification** — from content and layout, never from the file name or a symbol. Three kinds of evidence are summed:

| Evidence | Weight | Examples |
|---|---|---|
| Strong markers | +5 | "certificate of live birth", "municipal form no. 97" — also matched against the text with punctuation stripped, so "Municipal Form No, 1O2" still hits |
| Weak markers | +1 | field labels belonging to that certificate ("husband", "cause of death", "maiden") |
| Layout | +2 per band, max +4 | MF-97's HUSBAND and WIFE headings **side by side on one printed row** (+4); MF-102's maiden-name / birth-weight bands; MF-103's cause-of-death and burial blocks |

Highest score wins; below 3 the answer is `Unknown`, which routes to manual review rather than into a registry. Confidence rewards the **margin** over the runner-up: `45 + score×4 + min(20, margin×4)`, clamped to 50–99. A page that looks a little like all three is not a confident anything.

**3.7 Field extraction** — per class, label-anchored, never coordinate-fixed:

- **Birth (MF-102):** anchor on numbered labels, falling back to the row's own `(First) (Middle) (Last)` bracket heading when OCR mangles a digit ("1. NAME" read as "4. NAME", "14. NAME" as "14, NAME"). Mother anchored on "MAIDEN". Names split into three cells; place split into hospital / municipality / province. Type of birth read from the **value row** (the one carrying the weight), not the whole page, whose printed caption lists every option.
- **Marriage (MF-97):** a **two-column table** — both spouses' values for one numbered field share a single printed line. Words are grouped into rows (`BuildRows`), and a `ColumnRuler` derives the label / husband / wife boundaries from the page's **own** HUSBAND and WIFE headings, so a crop or an angled photo still splits correctly. The wife column is bounded at the table edge, not the page edge (unbounded, margin speckle glued onto her surname). Within a cell only ALL-CAPS words survive, which drops the "(First)/(Middle)/(Last)" hints without having to recognise them. Date is read near the "Date of Marriage" label, not the first date on the page — that one is the marriage licence's issue date. Solemnizing officer is read only in the name column above its signature line and rejects certification boilerplate: a sentence in a name field is worse than a blank one, because it looks filled in and gets saved as somebody's name.
- **Death (MF-103):** name cells below the NAME / DECEASED heading; sex, civil status and place by keyword. Age anchored to a standalone `\bAGE\b` label — unanchored, it matched "aged" inside the printed maternal-condition caption and reported the wrong age.

**3.8 Confidence, correction and validation** (`DocIntelligence`). Every field carries its own score and verdict:

- **Per-field confidence** — the mean Tesseract confidence of the words that produced *that* value, found by matching the value's tokens back onto the page, scaled by how much of it could be traced (`mean × (0.6 + 0.4 × traced)`). A value that matches no word was derived rather than read (sex from a tick box, "Filipino" from a keyword) and scores a deliberate 50 instead of a flattering 100. A page that read 87% overall cannot hide a field that read 8%.
- **Context correction, never invention.** Only reshapes characters already read: digit/letter confusions inside numeric fields (`O→0`, `I→1`, `S→5`) and inside names (`0→O`, `1→I`), a repair accepted only if it produces the field's expected shape; a mangled month name snapped to the nearest real month within 2 edits; sex spellings ("Femaie"); and a place or occupation snapped onto a spelling the office already uses, within 2 edits and 5+ characters (this is how a corrected place name spreads through the Learning Library). **An empty field is never filled**, and a corrected value loses 10 confidence points.
- **Validation.** Dates must parse, not be in the future, and not predate 1900. Names must be letters, spaces, hyphens and apostrophes. Sex must be Male or Female. Registry numbers must be `YYYY-NNNNN`. Birth weight 300–8000 g, age 0–130. Places must not be the form's own printed text. Relationships: child's surname matching neither parent, mother's maiden surname equal to the father's, husband and wife reading as the *same* name (the classic two-column failure), age contradicting the date of death.
- **Verdicts:** `Ok` / `Uncertain` (<70%) / `Missing` / `Invalid` (broke a rule) / `Conflict` (disagrees with another field).
- **Manual review** is triggered by: `Unknown` type, overall confidence <65%, or an **Invalid core field** (the names, sex and dates that define the record). A junk reading in a non-core field — the handwritten informant line, say — is flagged red but does not hold up an otherwise sound document.

**3.9 Human review.** Mandatory before any write.

- The **field grid** is the single review surface: Field | Value (editable) | Conf % | Check. The Check column says what is wrong in words the operator can act on, coloured by verdict, and names the original OCR text whenever a value was auto-corrected.
- **Selecting a row draws that field's box on the scan**, so comparing the extracted value against the page is one click rather than a hunt.
- **Edits re-run the rules immediately.** A typed value is taken at face value (scored 100 — a person reading the paper outranks any recognition score) and re-validated, so correcting a flagged date can lift the manual-review hold and re-enable Commit without another OCR pass.
- Names stay as separate first / middle / last cells, so nothing is re-split out of a joined string.
- **Nothing leaves the screen unconfirmed.** Commit and Auto-Fill both open a confirmation naming the document, the overall confidence, and every field still flagged, with its value and reason. The default button is No.
- Only a birth scan is committed here — Commit and Draft write into `births`. Marriage and death go through Auto-Fill to the module that owns their column mapping; a scan is never written into `births` just because it arrived on the OCR screen.
- **Ordering rule:** `PrimeFromExtraction` calls `ClearForm()`, which nulls a pending scan. Always prime **then** `SetScanImage`. Reversing the order silently drops the image.

**3.10 Learning.** On Auto-Fill, the *operator-corrected* values are fed to `LearningLibrary` (places, hospitals, given names, surnames, occupations, nationality, church, officer), so corrected spellings win on later scans and become available office-wide.

## 4. Outputs

- **`DocAiResult`** — `Kind`, `ClassifyConfidence`, `OcrConfidence`, `RawText`, `Fields`, `ExtractedCount`, `MissingCount`, `Error`, and `Map()` (canonical key to value).
- **Registry row.** OCR commit/draft inserts into `births` with each value in the column it belongs to: parent names into `*_first/middle/last_name` (never one joined string), plus `time_of_birth`, `type_of_birth`, `weight_grams`, occupations, religion, citizenship, `informant_name`. Status `Registered` or `Draft`.
- **Softcopy.** Scan bytes written to **both** `births.birth_image` (the OCR module's own copy) and `births.scan_image` (what Birth Registration and the softcopy viewer read back), as `MySqlDbType.LongBlob`.
- **`ocr_batch` row** per run: `scan_id` (`SCN-yyMMdd-NNN`), `source_book`, `doc_class`, `doc_kind`, `confidence` (page), `overall_confidence` (fields), `needs_review`, `review_reason`, `rotation_applied`, `username`, `status`, `raw_text`, and after disposal `record_table` + `record_id`.
- **`ocr_field_audit` rows** — one per field per disposition: the original OCR value beside the final value, the confidence, the verdict and issue, whether the engine auto-corrected it, whether the operator edited it, the action, the user and the time.
- **Auto-filled form** — Birth or Death in place, Marriage in the modal MF-97 dialog.
- **Today's batch grid** — Scan ID, Source Book, Class, Conf, Status, Saved To.

## 5. Validation

| Rule | Effect |
|---|---|
| Auto-Fill pressed with `Kind == Unknown` | refused — the operator is told to run OCR on a Birth, Marriage or Death certificate |
| `OcrConfidence < 90`, or field empty | field flagged `Uncertain` ⚠, review prompt shown |
| `OcrConfidence < 70` | OCR screen banner turns amber; batch status `Low Confidence` |
| `Kind == Unknown` | commit blocked, auto-fill disabled, operator told to check scan quality or encode manually |
| `Kind != Birth` | Commit and Draft disabled; Auto-Fill is the live action, labelled with the target module |
| `ChildFirst` or `ChildLast` blank in the grid | commit and draft refused, operator told to correct the grid |
| Sex not read | stored NULL — migration 24 made `births.sex` nullable; the NOT NULL column failed every such commit |
| Date or weight unparseable | stored NULL, never guessed |

Blank is a valid answer. Registry numbers are handwritten and normally come back empty; a placeholder there would be worse than nothing.

## 6. Error Handling

| Condition | Behaviour |
|---|---|
| `eng.traineddata` missing | `IsAvailable()` false — engine label red, run refused with install instructions |
| PDF or unsupported extension | `NotSupportedException` naming the accepted formats |
| Image will not open | "Could not open the image: \<reason\>", no state change |
| Exception during OCR | message shown **and Commit + Draft disabled** — a failed run must not be committable |
| MySQL 1054 on the batch grid | grid cleared, header reads "RUN DATABASE MIGRATION 24_ocr_routing.sql", Commit/Draft disabled |
| Unknown classification | explicit warning naming scan quality or manual encoding |
| OCR screen opened outside the shell | routing refused: "Open this screen from the CROMS main window" |
| Deskew requested | not implemented — operator told to straighten the page on the scanner |

## 7. Privacy

Fully offline. Tesseract runs in-process; no document, image or extracted value leaves the PC. All storage is the local/LAN `croms` MySQL database. `[TBD]` scan retention period, blob encryption at rest, and whether OCR / Document AI should be restricted by role — both screens are currently open to any signed-in user.

## 8. Auditability

Four questions have to be answerable about any machine-produced record, and each has a home:

| Question | Where |
|---|---|
| What did the machine see? | `ocr_batch.raw_text` (whole page) + `ocr_field_audit.ocr_value` (per field) |
| What was it changed to, and by whom? | `ocr_field_audit.final_value` with `auto_corrected` / `edited_by_user` |
| How sure was it? | `ocr_field_audit.confidence` + `status` + `issue`; `ocr_batch.overall_confidence` |
| Who accepted it, and when? | `ocr_field_audit.username` + `created_at`; `ocr_batch.username` |

- Every run writes an `ocr_batch` row *before* review; the row is then updated with the disposition and the exact `record_table` + `record_id`, so a scan traces to the row it produced.
- Commit and Draft also write an `audit_log` entry via `Audit.Write`.
- Batch statuses: `For Review`, `Needs Review`, `Unclassified`, `Committed`, `Draft`, `Auto-Filled`, `Manual Review`.
- Schema: `Database/25_document_intelligence.sql` (applied).

## 9. Measured results

Run with `CROMS.DocTest.exe` (harness in `CROMS.DocTest`, built into `CROMS\bin\Debug` so it loads the same engine as the app) over the office's three de-identified samples:

| Sample | Detected | Class | Fields read | Field confidence | Verdict |
|---|---|---|---|---|---|
| Birth Certificate.jpeg | Birth | 99% | 22 of 24 | 87% | flagged (junk informant, non-core) |
| Marriage Cert.jpg | Marriage | 99% | 8 of 11 | 65% | passes |
| Death Cert.png | Death | 99% | 5 of 11 | 90% | passes |
| *(control)* memo image | **Unknown** | — | 0 | — | manual review, not misfiled as a certificate |
| *(control)* poor phone scan | Birth | 99% | 9 of 24 | 50% | **held** — "Child First Name contains characters a name cannot have" |

Orientation: **12 of 12** (each sample at 0°/90°/180°/270°) corrected, each rotated run producing the same field count as upright. Blank fields are blank because the source is handwritten (registry numbers, informant, time of birth) or the print is destroyed (place of marriage, solemnizing officer, the 606px death certificate's name row) — not because they were dropped.

## 10. Known Limits

**Handwriting is not read.** Tesseract recognises print; the handwritten entries on these forms (registry numbers, informant lines, most signatures) come back blank and are flagged for typing in. This is a limit of the offline engine, not of the mapping, and no amount of parsing changes it.

Recognition still loses characters on damaged print — a sample reads "PANLILIO" as "PANU". Multi-page and PDF are not supported. `osd.traineddata` must sit beside `eng.traineddata` for orientation detection; without it the recognition probe alone decides, which is weaker. Accuracy beyond these samples is `[TBD]`, pending more de-identified scans with expected values.
