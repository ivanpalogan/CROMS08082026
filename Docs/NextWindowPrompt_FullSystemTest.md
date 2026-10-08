# Prompt for the next windows - test EVERY function and EVERY text box, find and fix bugs

How to use: open each window in `C:\Users\ivan palogan\OneDrive\Documents\Capstone\CROMS`, paste the
**common block** below, then add ONE line: `You are Window A` (or B, C, D, E). Windows do not share
memory, so each one tests only its own area. Recommended order: A, B, C, D, E (A and B find the most).

---

## COMMON BLOCK (paste into every window)

You are testing CROMS (WinForms .NET Framework 4.8 + MySQL `croms`, plus the separate kiosk, public
display, and two phone apps). Read `CLAUDE.md` first (rules, git workflow, the 2026-10-08 entries at the
bottom, and every "traps hit" note). Work on `main`; commit only files that belong to a fix; push to
`origin/main`; never force push; never run destructive git commands.

### Goal
Find and fix real bugs by RUNNING things, not by reading code. For your area, exercise **every function
(every button, menu item, tab, step, dialog) and every text box / combo box / date picker / number box**,
with the inputs below, and write down what happened. A feature counts as working only if you ran it.

### Input battery for EVERY text box (do all of these, per box)
1. Normal value; empty (required and optional); only spaces; leading/trailing spaces.
2. Exactly the column width, and one character over (the box must stop it; the save must never show "Data too long").
3. Special characters: apostrophe `O'Brien`, double quote, backslash, `%`, `_`, `[`, `;`, `--`, `<script>`, `'; DROP TABLE x;--` (nothing may break or run), n-tilde `Peñablanca`, accents `José`, emoji, Tagalog text, a pasted multi-line value with a newline.
4. Names: hyphen, `Dela Cruz`, `De La Cruz Jr.`, one-letter middle name, ALL CAPS, all lower case.
5. Numbers (ages, weights, amounts, copies, registry numbers): letters, negative, zero, decimals, `1e5`, huge (`99999999999`), thousands separator.
6. Dates and times: today, tomorrow (future), 1900-01-01, year 0001/9999, 29 Feb on a leap and a non-leap year, a death before the birth, marriage date before 18th birthday, 24:00.
7. Combo boxes: pick each kind, type text not in the list, clear it, change a parent (province -> city -> barangay) and confirm children reset.
8. Search boxes: empty, one letter, a very long string, special characters, SOUNDEX/sound-alike spelling, 10,000-row result.
9. Then the full round trip: Save -> close -> reopen -> every value identical (no trimming, no swapped fields, dates not shifted, accents intact) -> Update -> Delete/Cancel paths.
10. Keyboard: Tab order follows reading order, Enter does not fire the wrong button, Esc closes dialogs safely, double-click and rapid repeat clicks do not create two records.

### Also check, in your area
- Required-field rules and the messages shown (plain sentence, no raw exception text, no stack trace).
- Every status transition and who may do it (Admin vs Staff: only these two roles exist now).
- Layout at 1920x1080 AND 1366x768: nothing overlapping, clipped, unreachable, or scrolling sideways.
- What happens with the database unreachable, and with a second PC editing the same record.
- Anything that looks filled-in but has nothing behind it (a green tick on a wrong value, a printed field that is blank, a preview that looks like an issued document).

### Method (reuse - do not rebuild)
- Build: MSBuild from VS2019 (`C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe`). In Git Bash set `MSYS_NO_PATHCONV=1` and use `-p:` not `/p:`. If `CROMS.exe` or Visual Studio holds `bin\Debug`, build to a temp `-p:OutputPath=...` and test that copy.
- Drive the REAL forms from a PowerShell STA harness: `Scripts\FieldLimits\_Init.ps1` (loads the built exe, points config at it, gives `$asm` and `Sql "<query>"`; no password in scripts). Look at `Scripts\FieldLimits\Test-DialogBoxes.ps1` and `Test-BirthMarriageBoxes.ps1` for how to construct a form by reflection, fill every box, and read what it would save. `CROMS.MarriageTest` (`--flows`, `--birthtest`, `--fees`, `--breqs`, `--audit`, etc.) drives the service layer; run the suites that touch your area before and after any fix. `CROMS.DocTest` drives OCR.
- Interactive screenshots: computer-use cannot attach to CROMS (not a Start-menu app) - render forms with `DrawToBitmap` and read the PNG, and check geometry with a sibling-overlap sweep.
- Run `Scripts\FieldLimits\Run-All.ps1` at the end: it must still exit 0.
- Run long suites in the background (a 10-minute tool timeout kills them before cleanup).

### Safety rules (this is a live database)
- Tag every test row `ZZT` (names, registry numbers, O.R. numbers) and DELETE them afterwards by that tag plus id range. NEVER delete audit_log rows "by record id" (ids are reused; that destroyed real history once - see CLAUDE.md 2026-09-13). Count rows of births, marriages, deaths, payments, transactions, queue_tickets, audit_log BEFORE and AFTER and report both.
- Never print, log, or commit a password. Do not touch real clients' rows. No real printing: cancel print dialogs.
- Traps already hit - do not repeat: `Control.Visible` is false while any parent is unshown; a `Label` eats `&` (UseMnemonic=false); a Dock=Fill child inside AutoScroll can StackOverflow; bulk text edits drop a UTF-8 BOM and mix CRLF/LF - check bytes before committing; the Bash tool unescapes `\n` inside heredocs (write patch scripts to a file or use the Edit tool); `ConfigurationManager` caches its config (clear it in harnesses); never read a harness result off a stale `bin\Debug`.

### How to report (plain, no hedging)
For each function / box: PASS / FAIL / NOT TESTED, the exact input, what happened, and the file:line of the cause. Then: bugs fixed (with the regression check you added), bugs found but NOT fixed (and why - decision needed), and what you could not test. Add a dated entry to `CLAUDE.md`, commit, push. Do not claim a screen works unless you ran it.

---

## WINDOW A - civil registration (the registry itself)
Birth Registration (all 8 steps incl. attendant, informant, certification, fee/O.R., parents-married toggle, birth order, time of birth, hospital address, country of birth, delayed-registration case and checklist, Print/View Softcopy, Draft vs Submit vs autosave), Death Registration (5 steps, DOB/age computation, Medical & Permits tab, place cascade, informant "Others"), Marriage Registration (Form 97 start wizard, licence picker, exempt path, out-of-province licence, submitted-by, Mobile Capture, OCR review, final-scan confirm, Register gate, Registry Number), Marriage Desk (Form 90 licence, issue, consent/advice/MF-90 printing, requirements grid + bypass, posting clock, PSA transmittal, copies, history), Petitions & Case Tracking (all 6 case types, stage flow, case documents, service slip).

## WINDOW B - front desk and money
Queue Management (Call Next FIFO + priority lane, Call Client, Recall, Forward, Pause, Abandon, filters/search/export, window cards, Client Tasks panel), Certificate Request (kiosk intake card, Find Record -> Records Archive pick mode, create, Print/View certificate, claim), Release & Claim (states, ID-upload QR, letter QR, verification dialog, camera on/off, release history), Fees & Payments (assess, tender/change, O.R. rules, walk-in payment, itemised log, monthly collection, fee schedule edit, slip print), PSA Copies (BREQS: every status move, receive & scan, release, overdue/unclaimed), Transactions ledger and service slip, Dashboard KPIs and trend.

## WINDOW C - admin, records, documents, reports
Login + forced password change + lockout, Window Assignment (pick window, services, Skip, Log out), Settings (every page: General, Users & Access, Master Files incl. 42,029 barangays, Forms & Templates, Window Management, Audit Trail, App Updates/Publish, User Manual, Mobile Capture setup), Records Archive (Search Records incl. SOUNDEX, View Record Details, View Certificate, all categories, legacy Birth/Marriage/Death Record workbenches + "+ Digitize Old Record"), Intelligent Document Processing (every document kind, review grid edits, wizard steps, Commit/Draft/Auto-Fill guards, duplicate registry number, Mobile Capture, scan highlight/zoom, Print preview watermark), Certificate Templates designer + Form 3A/3B/3C (Add Text/Field/Image/Line/Rectangle, Apply Header/Footer, Edit Layout from preview), Reports & Analytics (all 6 tabs, date ranges, Customize Report, PSA/Statutory, collections), Role access (Admin vs Staff menus; Staff must not reach Settings/templates).

## WINDOW D - kiosk and public display
CROMS.Kiosk: Welcome screen, Step 1 service cards (every service, availability when no window is online, Back, idle reset), Step 2 Personal Info (single and marriage couple, Submitted-by + Others, priority lane Senior XOR Pregnant, valid-ID type mask per ID type, webcam capture/retake, no-camera case), CTC details (Birth/Marriage/Death, PSGC province -> city search, relationship Other), BREQS details, Marriage License gate (Yes/No), review/ticket print, offline overlay, 1366x768 scaling. CROMS.Display: board with 1..8 windows, Online/Offline, long ticket codes, resize. Include the kiosk -> staff hand-off (`CROMS.MarriageTest --flows`).

## WINDOW E - phone apps and save-API
ORCMobile_Application (scan page camera/gallery, green/red guide, auto-capture, de-warp, client name + document type confirm, upload, review page if reachable, connect page, pairing/heartbeat, Form 97 capture page `form97-capture.html` incl. certificate + licence steps), claimapp (QR/ticket entry, ID capture, name boxes, letter mode, upload), save-API `server/index.js` every endpoint with valid, empty, huge, malformed and hostile input (SQL injection strings, wrong token, expired token, double submit, concurrent uploads), HTTPS/trusted-host status, `npm test`. These are separate folders: commit ORCMobile in its own repo (no remote), claimapp is not a git repo.
