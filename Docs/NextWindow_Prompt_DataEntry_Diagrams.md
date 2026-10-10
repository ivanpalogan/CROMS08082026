# Prompt for the next window - data-entry table diagrams (draw.io) + screenshots into the Word doc

Paste everything below the line into a new Claude Code window opened on the CROMS repo.

---

You are working in the CROMS repo (C:\Users\ivan palogan\OneDrive\Documents\Capstone\CROMS). Read CLAUDE.md first
(Git workflow, the "Progress Log", and the notes on how screens were rendered/tested). Do the task yourself end to end;
do not hand back a plan. Reply in short caveman style if the caveman plugin is active.

## Goal

1. Build a **draw.io file** that shows every *data-entry table* of CROMS as a **hub-and-spoke "bubble" diagram**
   (style reference: a big ringed circle in the middle with small coloured circles around it).
   - **Big centre circle = the table name** (bold, large). Navy/slate thick ring, white fill.
   - **Small circles around it = ONE circle per column (field) of that table** - every column, none skipped,
     written exactly as the database column name.
   - Small circles: thick coloured ring (rotate orange / purple / blue / green / teal / red / yellow), soft light-grey
     inner fill, centred dark text, a short grey line from each small circle to the centre ring.
   - One **page (tab) per table** inside the same .drawio file, page name = table name.
2. Take **one screenshot of every data-entry screen** (the screen where that table is typed in), using the
   **demo environment**, and put the diagram image + the screenshot, per table, into the Word document that
   already exists in Downloads.

## Inputs you must use (do not guess these)

- Table and column names: read them from `information_schema` of schema **croms_demo** (identical structure to live).
  Connection: use `Scripts\Environment\_Common.ps1` helpers (`Invoke-MySql`, `Get-AdminConnection`) - they read the
  login from CROMS\App.config at run time. **Never write a password into any file, script, prompt or document.**
  Do not touch the live schema `croms`.
- Which tables count as data entry (a person types into them through a screen). Start from this map and verify it by
  grepping the code (`INSERT INTO <table>`) - add any table that a screen writes and I forgot, drop any that no screen writes:

  | Table(s) | Screen (sidebar path) |
  |---|---|
  | births | Civil Registration > Birth Registration (Form 102); also Intelligent Document Processing commit |
  | deaths | Civil Registration > Death Registration (Form 103) |
  | marriage_licenses | Civil Registration > Marriage Registration > Applications & Licenses (Form 90) |
  | marriages | Civil Registration > Marriage Registration > Form 97 entry |
  | petitions | Petitions & Cases > Case Tracking |
  | certificate_requests, transactions | Transactions > Certificate Request |
  | psa_copy_requests | Transactions > PSA Copies (BREQS) |
  | payments, payment_items | Transactions > Fees & Payments (walk-in payment) |
  | releases | Transactions > Release & Claim |
  | queue_tickets, queue_ticket_services, kiosk_ctc_intake | CROMS.Kiosk (client touch screen) and Queue Management |
  | ocr_batch | Records & Documents > Document Processing |
  | users, windows, fees, office_profile | System > Settings (Users & Access, Window Management, fee schedule, branding) |
  | provinces, municipalities, barangays, hospitals, churches, occupations, religions, nationalities, relationships, causes_of_death, countries, birth_orders, type_of_births, civil_statuses, residences | System > Settings > Master Files |

  Tables nobody types into (audit_log, *_history, document_requirements generated rows, deleted_records, server_beacon,
  _env_migrations, form97_capture_*, ocr_field_audit ...) are **not** diagrammed - list them at the end of your report
  as "system-written, not data entry".

## Step 1 - the .drawio

- Write a Python generator (keep it in `Docs\DiagramTools\gen_table_bubbles.py`, beside the existing
  `gen_integrated_diagram.py` - read that file first and reuse its XML helpers/ID scheme). Output:
  `Docs\CROMS_Data_Entry_Table_Diagrams.drawio` (plain uncompressed XML, one `<diagram name="births">` per table).
- Layout rules:
  - Centre circle diameter ~260 px for short names, bigger for long ones; font bold 26-34 pt.
  - Place column circles on a ring around the centre, evenly spaced by angle. **Up to ~16 columns per ring**; more
    columns go on a **second and third concentric ring** (each ring further out, circles on the outer ring offset by half
    a step so lines do not cross). `births` and `marriages` have 100+ columns - that is expected, make the page larger
    (up to ~4000 x 4000) instead of dropping fields or shrinking text below 9 pt.
  - Column circle diameter ~110-130 px; long names (`husband_father_citizenship`) wrap on `_` or at 12 characters;
    never let text overflow its circle (measure, shrink font to a floor of 9 pt, then enlarge the circle).
  - Mark the primary key column (`id`) with a filled ring; foreign keys (names ending `_id`) with a dashed ring.
    Add a small legend box in a corner of every page (solid = normal, filled = primary key, dashed = foreign key).
  - Groups of related columns (names / dates / address / signature block ...) may share a ring colour, but every
    column still gets its own circle.
- Validate: the file parses as XML, every `id` is unique, no two circles overlap (check rectangles in code), every
  column of every table appears exactly once (compare counts against `information_schema.columns`).
- Look at it: draw.io is not installed, so render each page to PNG yourself (PIL, as `gen_integrated_diagram.py` does)
  into `Docs\DiagramTools\out\<table>.png`, open at least `births`, `marriage_licenses`, `queue_tickets` and one
  small lookup table with the Read tool, and fix whatever looks wrong (overlaps, clipped text, crossed lines).
  Tell me honestly that it was not opened in real draw.io.

## Step 2 - screenshots of every data-entry screen

- Use the **demo** only (it has fictional sample data and an orange DEMO ENVIRONMENT badge, so no real citizen is shown).
  Start it with `Scripts\Environment\Start-DemoEnvironment.ps1` (build first with MSBuild from VS 2019 if
  `CROMS\bin\Debug` is stale; close CROMS.exe/Visual Studio locks first). If the demo does not exist,
  `Scripts\Environment\New-DemoEnvironment.ps1 -Confirm` then `Scripts\SampleData\Run-SampleData.ps1 -Target Demo`.
  The demo logins are `demoadmin` / `demostaff`; their passwords were shown once at creation and are not stored
  readable - if you need to sign in and do not have one, ask me, or avoid signing in by using the render harness below.
- Two ways to get a picture, use the first when it covers the screen:
  1. **Render harness (no sign-in, no desktop):** `CROMS.MarriageTest.exe --audit <dir>`, `--birthrender <dir>`,
     `--render <dir>` (marriage windows), `--form90`, `--breqs`, `--fees` render the real forms to PNG against the
     demo schema (set the connection to croms_demo exactly as `Scripts\Environment\Test-DemoEnvironment.ps1` does).
     Build it with `msbuild CROMS.MarriageTest\CROMS.MarriageTest.csproj`. Read how each mode is used in
     `CROMS.MarriageTest\Program.cs` before running it; run long modes in the background (a 10-minute tool timeout
     kills them before cleanup).
  2. **Real desktop capture:** computer-use cannot attach to CROMS (it is not a Start-menu app). Drive the window with
     PowerShell (`mouse_event`, `SendKeys`, `Graphics.CopyFromScreen`), as done in earlier sessions. Crop to the app window.
- For each table in the map above capture **the data-entry screen showing the fields** - not only the list. For
  multi-step screens (Birth, Death, Marriage Form 90/97) capture **every step/tab** and name files
  `<table>_<step>.png`. The kiosk screens come from `CROMS.Kiosk` (render the real forms like the earlier kiosk work).
- Screens must show the sample data, not an empty form, where the screen allows (open an existing sample record).
- Save PNGs to `Docs\DiagramTools\shots\` (this folder must be git-ignored if it holds anything real - it should not,
  it is demo data only). Look at every PNG you keep; retake blank, clipped or wrong ones.

## Step 3 - put it in the Word doc

- Target: `C:\Users\ivan palogan\Downloads\CROMS_Doc1_Data_Entry_Report.docx` (the Data Entry report made earlier).
  **Back it up first** (`...\CROMS_Doc1_Data_Entry_Report_backup.docx`) and do not delete or rewrite the sections
  that are already there - add a new section "Data-entry tables and screens" after them.
- For each table, in this order: heading (table name) -> one line saying which screen writes it -> the bubble diagram
  image (PNG you rendered; for tables with 3 rings use a full-page landscape section) -> the screenshot(s) of the
  screen -> a two-column list "column name | type" is **not** needed (the diagram already lists the columns).
- Build it with the docx skill / docx-js (`NODE_PATH=$(npm root -g)`), keep the style of the existing doc (Times New
  Roman, US Letter), add alt text to images, then export through Word to PDF and look at the pages
  (as done before) to confirm no image is cut off.
- Do not put passwords, connection strings or real citizen data in the doc.

## Finish

- Add one Progress Log entry to CLAUDE.md (what was built, counts of tables/columns, how it was verified, what was
  NOT verified: not opened in real draw.io, screens not clicked by hand if that is true).
- Commit only your own files (generator, .drawio, shots if small, CLAUDE.md) with a clear message and push to
  origin/main. Never force-push. The Word doc stays in Downloads (not in git).
- Final reply: file paths (draw.io, doc), number of tables diagrammed, number of screens captured, and anything skipped with the reason.
