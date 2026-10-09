# Sample data for CROMS demos

Fills a CROMS database with **fictional** Cagayan-Valley records so every screen, report, certificate and the
dashboard has something to show. Nobody in it is real; names are generated combinations (none are taken from the scanned
sample documents). Everything is written through the application's own services where one exists
(`RegistryNumber`/`GeoLookup` rules, `MarriageService`, `BreqsService`, `PaymentService`, `DelayedBirthService`,
`PetitionDocumentService`, `LookupStore`); births, deaths, petitions, certificate requests, releases and queue tickets
use the same column lists as their screens.

## Run

```powershell
# 1. build (VS 2019 MSBuild)
MSBuild CROMS.sln -p:Configuration=Debug
MSBuild CROMS.SampleData\CROMS.SampleData.csproj -p:Configuration=Debug

# 2. demo schema first (recommended)
Scripts\SampleData\Run-SampleData.ps1 -Target Demo -Action Seed
Scripts\SampleData\Run-SampleData.ps1 -Target Demo -Action Counts
Scripts\SampleData\Run-SampleData.ps1 -Target Demo -Action Cleanup      # removes only the tagged rows

# 3. the real database (backs it up first, asks for -ConfirmLive)
Scripts\SampleData\Run-SampleData.ps1 -Target Live -Action Seed -ConfirmLive
Scripts\SampleData\Run-SampleData.ps1 -Target Live -Action Cleanup -ConfirmLive
```
`-Only births,deaths,marriages,petitions,certs,breqs,queue` runs part of it. The demo needs
`Scripts\Environment\New-DemoEnvironment.ps1` first (see `Docs\Environment_README.md`).

## Safety

- Insert only: no schema change, no UPDATE/DELETE of existing rows, `audit_log` and `deleted_records` are never touched.
- `--database <schema>` must equal the schema the connection really reaches, or the program refuses.
- `-Target Live` takes a `mysqldump` backup to `C:\Users\<you>\CROMS_Backups\<date>_pre-sample-data\croms.sql` first.
- No password in any file: the connection string is generated at run time from the demo account's DPAPI secret (Demo) or
  the application's own login (Live), written under `%LOCALAPPDATA%\CROMS\SampleRun_<target>`, and deleted afterwards.
- **Idempotent.** Each block checks for its own tagged rows and says `already seeded - skipped`; a second run adds nothing.
- **Cleanup removes only tagged rows.** Every row carries `SAMPLE DATA 2026` in a text column of its own table
  (`remarks`, or `purpose` for certificate requests and queue tickets); child rows are found through their tagged
  parents, never by an id range. The audit rows the services wrote stay (the trail is append-only). Lookup values the
  seeder added (a hospital, a church, an occupation...) stay as ordinary master-file entries.
- Does not use any `ZZ...` name or tag - the test suites delete those.
- No queue ticket is left Waiting or Serving, so Call Next can never offer a sample client to a real window.

## What it creates (counts are for 2026-10-10; the queue/date blocks follow the day it is run)

| Block | Rows | Notes |
|---|---|---|
| Births | 60 | `YYYY-B-####` per registration year (2024-2026); 50 Registered, 4 Pending Approval, 6 Draft; 6 delayed registrations (affidavit checklist, posting on 4, registrar evaluation on 2); 2 twin sets and 1 triplet set (1st/2nd/3rd); ~18% parents not married; home, rural-health-unit and hospital births; both parents' details, attendant, informant, staff signature block |
| Deaths | 30 | `YYYY-D-####`; ages computed from date of birth and date of death; 27 Registered, 3 Pending Verification; one infant; causes of death with intervals, certifier, informant, permits |
| Marriage licences (Form 90) | 22 | 2 Draft, 3 Posting, 1 On Hold, 1 Cancelled, 13 Issued (3 expiring within 22 days, 8 linked to a marriage), 2 Expired; consent (18-20) and advice (21-25) cases, a widowed applicant, a foreign (Japanese) applicant; requirement rows, payments in the payment log |
| Marriages (Form 97) | 20 | 8 licence-linked, stopped at Capture / For Review / Verify (2) / Register (2) / Final Scan / Returned; 12 **Registered** digitized registry-book entries (see below); 2 PSA transmittal batches (one sent and acknowledged, one draft) |
| Petitions / cases | 15 | all six case types, every stage; requester and relationship; document checklist rows; 6 filing-fee payments |
| Certificate requests | 30 | 18 with an Official Receipt (fees from the `fees` table x copies, Cash and GCash), 13 released (some to a representative with ID type and number), plus ForPrint 3, ForPayment 4, ForRelease 5, WaitingToRelease 3, Cancelled 2 |
| PSA copy (BREQS) | 10 | Requested 2, Paid 3, Submitted 3 (one past its expected date), No Record 1, Cancelled 1 |
| Queue | ~110 tickets, ~150 service rows, ~24 CTC intake rows | 14 days (weekdays only), continuous Q-numbers, Senior/PWD/Priority lanes, 1-3 services per ticket, accept -> call -> serve -> complete times for the Queuing analytics, ~8% abandoned with a reason, ~2% never called |

## Things that are deliberately NOT there

- **Registered modern marriages.** `MarriageService.Register` needs a confirmed *final registered Form 97* (a scanned
  image) and this seeder attaches no scans. So the 8 licence-linked marriages stay at the step the application leaves
  them in, and the Registered ones are the **digitized registry-book entries** the Old Marriage Records screen writes
  (`record_source = OCR-Backlog`, `encoding_method = Manual`). Both appear in the PSA transmittal queue.
- **PSA copies received or released** (`ReceiveFromPsa` needs the scanned copy).
- Scan images, photographs, signatures and handwritten-only fields (left blank like the real forms); contact numbers.
- Backdated *history* timestamps: `marriage_case_history`, `psa_copy_history` and `audit_log` rows carry the time the
  seeder ran (the services stamp `NOW()`); the records' own dates (created, filed, issued, registered, paid, released)
  are spread over 2024-2026.
