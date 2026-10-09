# CROMS demo / test environment - design note

Purpose: run demonstrations, tests and staff training on realistic data **without ever touching the live
registry** (`croms`). Everything below was read from the code and the live schema on 2026-10-10, not assumed.

## 1. What an "environment" is here

| Piece | Live | Demo / test |
|---|---|---|
| MySQL schema | `croms` | `croms_demo` (any name starting `croms_demo` or `croms_test` is accepted by the scripts) |
| MySQL account the apps use | `root` (this PC) / `croms_user` (LAN PCs) | `croms_demo_app`@`localhost` - SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES, LOCK TABLES, SHOW VIEW, EXECUTE on `croms_demo`.* **only**; no rights at all on `croms` (verified: `SELECT command denied ... for table 'births'`) |
| Executables | `CROMS\bin\Debug`, `CROMS.Kiosk\bin\Debug`, `CROMS.Display\bin\Debug` | copied to `%LOCALAPPDATA%\CROMS\Demo\App` with their own generated `.exe.config` |
| Visible sign | none | orange **DEMO ENVIRONMENT - croms_demo** badge in the header bar, and the same words in the window title |
| Login | real office accounts | `demoadmin` (Admin, must change its password at first sign-in), `demostaff` (Staff) |

## 2. Every place a connection is made (traced)

All C# programs build their connection from `ServerConfig.EffectiveConnectionString`
(`CROMS\Data\ServerConfig.cs`, with copies in `CROMS.Kiosk\ServerConfig.cs` and `CROMS.Display\ServerConfig.cs`),
which is the App.config `Croms` connection string with the host/port overridden by `%APPDATA%\CROMS\server.cfg`
when that file exists. The **database name comes only from the App.config string** - so pointing a program at the
demo schema is a one-line change in *its own* `.exe.config`, which is why the demo uses a separate folder.

| Program | Where it connects | What decides the schema |
|---|---|---|
| `CROMS.exe` (admin app) | `Data\Db.cs` -> `ServerConfig.EffectiveConnectionString`; also `ServerBeacon`, `RecordRecycle`, `MarriageService`, `PaymentService` (transactions) | `CROMS.exe.config` -> `connectionStrings/Croms` |
| `CROMS.Kiosk.exe` | `CROMS.Kiosk\Db.cs` + `ServerConfig.cs` | `CROMS.Kiosk.exe.config` |
| `CROMS.Display.exe` | `CROMS.Display\Db.cs` + `ServerConfig.cs` | `CROMS.Display.exe.config` |
| phone save-API (`ORCMobile_Application\server`, also used by claimapp) | `db.js` (mysql2 pool) | env `DB_HOST/DB_PORT/DB_USER/DB_PASSWORD/DB_NAME`. The desktop starts it through `Data\ApiServerManager.cs`, which **passes the desktop's own effective connection as DB_\*** (`PassDatabaseSettings`) - so a demo desktop automatically starts a demo save-API; a hand-started one falls back to `server\.env` (live) |
| `CROMS.MarriageTest.exe`, `CROMS.DocTest.exe`, `CROMS.ReportGen.exe`, `CROMS.SampleData.exe` | `Db` of the main assembly | their own `.exe.config` (the repo ones name `croms`; the scripts write a demo one at run time) |

Ports: MySQL 3306 (unchanged). Save-API 3000 (HTTP) / 3443 (HTTPS), scanner dev server 4200, claimapp 4300 are
**not** started by the demo launcher (`IonicAutoStart=false` in the generated config) so a demo cannot collide with
the live ones.

```
                 live registry                                   demo environment
   CROMS.exe  Kiosk  Display  save-API                CROMS.exe  Kiosk  Display  MarriageTest  SampleData
        \       |      |       /                            \       |      |        |           /
         \      |      |      /   root / croms_user          \      |      |        |          /  croms_demo_app
          +-----+------+-----+                                +-----+------+--------+---------+  (generated config,
                     |                                                       |                    password from DPAPI)
              MySQL 3306  ---- schema croms                       MySQL 3306  ---- schema croms_demo
                     ^                                                       ^
                     |   read-only INSERT ... SELECT (reference data)        |
                     +-------------------------------------------------------+
```
Data only ever flows live -> demo, and only for the reference set in section 3.

## 3. Reference vs transactional tables

Derived from the schema (60 base tables) and from what the code needs at start-up.

**Reference (copied live -> demo by New/Reset/Refresh; non-personal)**
`app_settings`, `barangays`, `birth_orders`, `causes_of_death`, `certificate_template_images`, `certificate_templates`,
`churches`, `civil_statuses`, `countries`, `document_requirement_types`, `fees`, `hospitals`, `municipalities`,
`nationalities`, `occupations`, `office_assets`, `office_profile`, `provinces`, `relationships`, `religions`,
`residences`, `type_of_births`, `window_service_assignments`, `windows` (24 tables; windows are copied *signed out*).
Test entries typed into the live master files (SQL fragments, markup, an emoji, `aaaa...`) are filtered out of the
demo copy only (`Scripts\Environment\_Junk.ps1`); the live master files are not edited.

**Kept by Reset:** `users` (the two demo logins) and `_env_migrations` (the demo's migration ledger).

**Transactional (emptied by Reset; never copied)** - everything else:
`audit_log`, `births`, `certificate_requests`, `claimant_id_uploads`, `client_service_slips`, `deaths`, `deleted_records`,
`document_requirements`, `document_routing`, `form97_capture_images`, `form97_capture_tokens`, `kiosk_ctc_intake`,
`marriage_case_history`, `marriage_copies`, `marriage_final_documents`, `marriage_licenses`, `marriages`, `ocr_batch`,
`ocr_field_audit`, `payment_items`, `payments`, `petitions`, `psa_copy_history`, `psa_copy_requests`,
`psa_transmittal_batches`, `psa_transmittal_items`, `queue_ticket_forwards`, `queue_ticket_services`, `queue_tickets`,
`reference_library` (the learning library is built from real names, so it starts empty), `registry_books`, `releases`,
`server_beacon`, `staff_biodata`, `transactions` (35 tables).

## 4. Safety rules built into the scripts

1. The live schema is only ever **read** (`INSERT ... SELECT FROM croms.x`, `mysqldump`). `Assert-DisposableSchema`
   throws for `croms`, for an empty name, and for anything not starting `croms_demo` / `croms_test`.
2. Every destructive script (`New`, `Reset`, `Remove`, `Refresh-FromLive`, `Apply-Migrations`) prints the schema and
   host it is about to change and refuses without `-Confirm`. Nothing defaults to `croms`.
3. No password is stored in any file in the repo. The MySQL administrator login is read from `CROMS\App.config`
   at run time into a temporary `--defaults-extra-file` that is deleted straight after. The demo account's password is
   generated at creation, shown once, and kept only DPAPI-protected (current Windows user) in
   `%APPDATA%\CROMS\demo-env.cfg`. The generated `.exe.config` files live under `%LOCALAPPDATA%` (not the repo, not
   OneDrive) with an ACL for the current user only.
4. Structure is loaded from a structure-only dump of live, with every `` `croms`. `` qualifier rewritten, so the
   four certificate views read the **demo** tables (checked: none references `croms`).
5. Migrations: the demo keeps a ledger (`_env_migrations`). The 88 files up to `86` are marked as the baseline (the
   dump already contains them); `Apply-Migrations.ps1` applies only newer files, once. Re-running an old migration
   after `85` would re-create the old table names as empty tables - the ledger prevents that. A pending file that
   names the live schema is refused.

## 5. Personal data

The demo contains **no** real citizen data. `Refresh-FromLive.ps1` is therefore deliberately limited to the reference
set. A copy of the registry tables with masking was *not* built: births/deaths/marriages/licences/tickets carry
hundreds of free-text and binary columns (names, addresses, ID numbers, contact numbers, photographs, signatures,
scans), and a mask that misses one column puts a real person in a demo. The people in the demo come from the
sample-data seeder (`Scripts\SampleData`), which generates fictional Cagayan-Valley names.

## 6. Indicator

`ServerConfig.DatabaseName` / `IsDemoEnvironment` (a name that is non-empty and not `croms`, case-insensitive).
`MainForm.BuildUserBar` adds an orange label to the header bar and appends `- DEMO ENVIRONMENT (<schema>)` to the
window title. Covered by `CROMS.MarriageTest.exe --envbadge` (13 checks: seven pure-rule cases incl. `croms`, `CROMS`,
`" croms "`, empty, null; connected-schema = configured-schema; badge and title present for a demo schema and absent
for the live name). Kiosk and Display are not changed (they have no header).

## 7. Known limits

- The demo database lives on **this PC** (`localhost`). `%APPDATA%\CROMS\server.cfg` is shared with the live install
  (the shell's per-user folder cannot be redirected per process); `Start-DemoEnvironment.ps1` refuses to start if it
  names a remote host.
- Scans, photographs, signatures, phone pairing (`form97_capture_*`), uploaded IDs and the learning library are not copied.
- The demo sign-in screen, Mobile Capture setup (`%APPDATA%\CROMS\mobile.cfg`) and print alignment (`print-align.cfg`)
  still read the shared per-user files; the demo launcher turns the dev-server auto-start off.
- A kiosk/display on **another PC** cannot use the demo account (it is `@localhost` only).
