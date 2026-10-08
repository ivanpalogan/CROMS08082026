# Prompt for the next window - finish the "text box = column width" work

Paste everything below the line into a new Claude Code window opened in
`C:\Users\ivan palogan\OneDrive\Documents\Capstone\CROMS`.

---

You are continuing work on CROMS (WinForms, .NET Framework 4.8, MySQL `croms`). Read `CLAUDE.md`
first (project rules, git workflow, and the 2026-10-08 progress entries at the bottom). Work on
`main`, commit only files that belong to the task, push to `origin/main`, never force push.

## Goal
No screen may accept more text than the database column behind it can store, so a save can never
fail with "Data too long". Passwords are limited to 16 characters in the UI only.

## Already done (commits fcd7977, 3691eac, 94e4d9b, c2c3775, 34eaf63, c3eb4b0, 61cf32e)
- Passwords: `MaxLength = 16` on the new-password boxes (Users & Access, Staff Biodata, forced
  first-login change). Login and admin re-verify boxes are deliberately NOT capped. The hash
  column `users.password_hash` stays VARCHAR(255) (it stores a 76-char PBKDF2 hash).
- Migration 83 tightened address columns (province 60, municipality/city 80, barangay 80,
  house/street 100). Migration 84 widened the joined address columns to 400. Both applied to the live DB.
- `CROMS/Modules/FieldLimit.cs`: `Cap(width, controls...)` and `FromDb(table, column, control, ...)`
  (reads the width from information_schema).
- Caps applied and tested: Birth, Death (incl. Medical & Permits tab), Marriage Form 90 and Form 97,
  Certificate Request, Fees & Payments, Release & Claim, Petitions, Users & Access, Staff Biodata,
  Office Assets, Master Files, the three Old Birth/Death/Marriage record screens, BREQS dialogs,
  Marriage Case, Delayed Birth, PSA transmittal, window dialog, reason prompts, and the kiosk
  (ticket names, CTC, BREQS). Results: Death 0/36, Birth 0/42, Form 97 0/60, Form 90 0/83 overflow;
  50/50 windows/dialogs; kiosk 31/31; address boxes 31/31.

## Method (reuse it)
1. Build the solution (MSBuild from VS2019, or VS2022 BuildTools; use `-p:Configuration=Debug`,
   and in Git Bash set `MSYS_NO_PATHCONV=1`).
2. PowerShell STA harness (`powershell.exe -STA -NoProfile`): load `CROMS\bin\Debug\CROMS.exe`,
   point `ConfigurationManager` at `CROMS.exe.config` (clear `s_initState`, `s_configSystem`,
   `ClientConfigPaths.s_current`), construct the real form by reflection, fill every editable box to
   its `MaxLength` (400 chars when it has none), read the values the form would save, and compare
   each with the column width from information_schema. For a form with a parameter list use
   `FieldParams()` + `Columns`/`ValuePlaceholders`; for a dictionary use `Values()`.
3. Fix with `FieldLimit.Cap` / `FromDb`, rebuild, rerun until 0 overflow.
4. Add a dated entry to `CLAUDE.md`, commit, push.

## Traps already hit - do not repeat
- Files with a UTF-8 BOM lose it if you rewrite them with `WriteAllText` default encoding. Check the
  first three bytes and write back with `new UTF8Encoding(bom)`. Many files use CRLF; match it.
- `FromDb` silently skips a null control: call it AFTER the form builds those boxes.
- A DropDownList cannot hold free text (skip it); an editable ComboBox does have `MaxLength`.
- PowerShell: do not name a function parameter `$args`.
- Migrations are applied by piping into mysql: keep them ASCII only and idempotent.
- `bin\Debug` is locked while CROMS.exe or a debugger runs; build to a temp `OutputPath` then.

## Still to do (next windows)
1. Windows not yet checked by the method: Transactions, Queue Management (search only), Incoming/
   Outgoing, Records Archive, Document Processing (OCR review grid values committed to
   births/deaths/marriages - check `OcrDigitizationForm` Commit/Draft/Auto-Fill paths), Settings
   pages, Template Designer / Form 3A-3C certification windows, Mobile Capture setup, Server setup.
2. Mobile apps (separate repos, not committed here): `ORCMobile_Application` and `claimapp` form
   inputs (client name, ID fields) and their save-API endpoints in `server/index.js`: add input
   length checks that match the column widths, with a readable error instead of a raw MySQL one.
3. Server-side guard: the desktop `Db`/service layer still lets an over-long value reach MySQL if a
   path was missed. Consider a shared catch for MySQL error 1406 ("Data too long") that names the
   column in a friendly message (see `ErrorLog.Friendly`).
4. Real typing test with a human on the live screens (the harness uses programmatic Text and
   WM_CHAR messages only).
5. Optional: add a small regression test project or script to the repo (the harness scripts live
   only in a temp scratchpad now) so this check can be rerun after any schema change.

## Reporting
Report plainly: what was checked, how many boxes/columns, what overflowed before, what is fixed,
what was NOT covered. Do not claim a screen works unless you ran it.
