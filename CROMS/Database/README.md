# CROMS Database

Clean `croms` schema built for the 15 CROMS modules. Based on your existing
`NEWOCR` data (Birth/Death/Marriage records + lookups), cleaned up, plus the
tables the CROMS spec needs (transactions, payments, audit log, etc).

## Safety first

- These scripts create a **new** database named `croms`.
- Your old `ocr_record_sytem` is **never modified** — the migration only reads
  from it. It stays as a frozen backup.
- To undo everything: `DROP DATABASE croms;` — the old DB is untouched.

## Files (run in this order)

| # | File | What it does |
|---|------|--------------|
| 1 | `01_schema.sql` | Creates `croms` + all tables. |
| 2 | `02_seed.sql` | Adds the admin user (placeholder) + fee schedule. |
| 3 | `03_migrate_from_ocr.sql` | Copies your existing records into `croms`. |

## How to run (MySQL Workbench)

1. Open Workbench, connect to `Local instance MySQL93`.
2. **File → Open SQL Script** → pick `01_schema.sql` → click the ⚡ (Execute) button.
3. Repeat for `02_seed.sql`, then `03_migrate_from_ocr.sql`.
4. After #3, the result grid shows an old-vs-new row-count check. The numbers
   should match. If they do, migration worked.

## After migration

1. Open `../App.config` and replace `YOUR_PASSWORD` in the `Croms`
   connection string with your MySQL password.
2. The app can then connect to `croms` (data layer is Phase 1 of the roadmap).

## Known follow-ups (flagged in the scripts)

- **Fee amounts** in `02_seed.sql` are `0.00` placeholders — fill in the real
  Peñablanca Revenue Code figures.
- **Admin password** is a placeholder — set a real BCrypt hash when the login
  module is built (Phase 2).
- **Old birth dates** were stored as text; any not in `YYYY-MM-DD` form migrate
  as `NULL`. Review `births.date_of_birth` after migrating.
- **Place of birth/death/marriage** is now 3 dropdowns (hospital/church +
  municipality + province). The old single place names can't be auto-split, so
  migrated records have NULL place fields — re-select them on the form. Old
  names stay visible in the frozen `NEWOCR` DB.
- **Old user accounts** (plaintext passwords) were intentionally not migrated.
- Old `parent` and `residents` stub tables were not migrated (redundant).

## What changed vs the old DB

- ` baranggay` (leading space) → `barangays`
- `hostpital` → `hospitals`, `simbahan` → `churches`
- `queuedocument.MerriageID` → `queue_tickets.marriage_id`
- text dates → real `DATE`; text weight/age → `DECIMAL`/`INT`
- single place name → 3 dropdown FKs: births/deaths use `hospital_id` +
  `municipality_id` + `province_id`; marriages use `church_id` +
  `place_municipality_id` + `place_province_id`
- one `users` table with proper roles (Admin/Registrar/Staff/Cashier/Releasing)
- new: `transactions`, `certificate_requests`, `releases`, `petitions`,
  `registry_books`, `fees`, `payments`, `document_routing`, `audit_log`
