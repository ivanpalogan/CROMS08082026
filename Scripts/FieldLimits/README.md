# Field-limit checks

Rule: **no screen may accept more text than the database column behind it can store**, so a
save can never fail with "Data too long".

These PowerShell scripts prove it against the *built* exes and the *live* database. They open
the real forms, fill every editable box to its limit (400 characters when the box has none),
read what the form would save, and compare each value with the column width taken from
`information_schema`.

| Script | Checks |
|---|---|
| `Census-MainApp.ps1` | every form in the main app: lists text boxes / editable combos with **no** limit |
| `Census-Kiosk.ps1` | same for the kiosk |
| `Test-DeathBoxes.ps1`, `Test-DeathTextBoxes.ps1` | Death Registration (incl. Medical & Permits tab) vs `deaths` |
| `Test-BirthMarriageBoxes.ps1` | Birth Registration vs `births`, Marriage Form 97 vs `marriages` |
| `Test-Form90Boxes.ps1` | Marriage licence (Form 90) vs `marriage_licenses` |
| `Test-AddressBoxes.ps1` | province / city / barangay / house boxes (incl. typing past the limit by window message) |
| `Test-DialogBoxes.ps1` | the smaller windows and dialogs (Petitions, Certificate Request, Release, Fees, Users, ...) |
| `Test-KioskBoxes.ps1` | kiosk ticket / CTC / BREQS boxes |
| `Test-OcrGridLimits.ps1` | Intelligent Document Processing: every grid field has a column width, and over-long scanned values are refused before Commit / Draft / Auto-Fill |
| `Test-DataTooLongGuard.ps1` | the server-side guard: an over-long write is refused by MySQL (1406) and turned into a sentence naming the column + limit |

## Run

Build first (these test what is on disk), then:

```
powershell.exe -STA -NoProfile -ExecutionPolicy Bypass -File Scripts\FieldLimits\Run-All.ps1
```

or one script: `powershell.exe -STA -NoProfile -File Scripts\FieldLimits\Test-DeathBoxes.ps1`.

`-STA` is required (WinForms). Set `CROMS_BIN` to test a build in another folder.
They only read the database. No password is stored here: the connection comes from the app's
own configuration (`_Init.ps1`).

## When to run

After any migration that changes a column width, after adding an input box, and before a release.
A new box shows up in `Census-*.ps1` as "Unlimited" - give it `FieldLimit.Cap(...)` or
`FieldLimit.FromDb(...)` (`CROMS\Modules\FieldLimit.cs`), then rerun.

## If a value still gets through

`ErrorLog.Friendly/Text` turns MySQL error 1406 into a sentence naming the column
("The "first name" entry is too long to be saved (the most it can hold is 50 characters)..."),
and `Application.ThreadException` catches anything unguarded.
