# CROMS — Project Truth & Research Facts

> Single source of truth for the CURRENT system. Written from the actual codebase
> (files cited), not from the old manuscript. If Chapters 1–3 disagree with this
> file, THIS FILE WINS.
> Last verified against code: 2026-08-31.

---

## PROJECT TITLE
Design and Development of OCR-Based Record Management and Queuing System

## SYSTEM NAME
CROMS — Civil Registry Operations Management System

## TARGET OFFICE
Local Civil Registry Office (LCRO), LGU Peñablanca, Cagayan

## CURRENT STATUS
Research title stays unchanged. Adviser/professor approved keeping the existing title.

## IMPORTANT CHANGE
The queueing component was reworked, not merely reused. Current CROMS queueing is a
three-application system (staff app + client self-service kiosk + public Now Serving
display) with multi-service tickets, dynamic windows, priority lanes, forwarding, and
full workflow timestamps. Any queueing description in the old chapters is obsolete.

## IMPORTANT RULE
Do not assume statements in the old Chapters 1–3 describe the current system.
The running CROMS implementation is the source of truth for current functionality.
Anything not listed under "CURRENT SYSTEM FEATURES" below does not exist yet.

---

## OCR
Exact current implementation:

- Engine: **Tesseract** (Tesseract .NET wrapper, `CROMS/Tesseract/Tesseract.dll`), language data `eng`.
- Wrapper: `CROMS/Data/OcrService.cs`. `EngineMode.Default`, `PageSegMode.Auto`,
  `user_defined_dpi = 300`.
- Preprocessing before recognition: 2× bicubic upscale, grayscale, contrast stretch
  (factor 1.5 around 128), binarize at threshold 150. Tuned for faded registry-book pages.
- Output: extracted text + Tesseract **mean confidence 0–100**.
- Language data path: `C:\Program Files\Tesseract-OCR\tessdata` if installed, else the
  bundled `tessdata` next to the exe. Fully **offline** — no cloud OCR API.
- Field extraction after OCR is **rule/label-based regex matching**, not machine learning
  (`CROMS/Data/DocumentAI.cs` — see "KNOWN DISCREPANCIES").
- UI: `CROMS/Forms/OcrDigitizationForm.cs` — load a scanned page (png/jpg/tif/bmp), run OCR,
  review and correct extracted fields, then commit into the birth registry.
- Every OCR run is logged to the `ocr_batch` table (scan id, source book, doc class,
  confidence, status `For Review` / `Committed` / `Low Confidence`, raw text) —
  `CROMS/Database/08_ocr.sql`.
- The original scanned image can be stored with the record (`scan_image` LONGBLOB on
  `births` / `marriages` / `deaths`, `CROMS/Database/17_scan_softcopy.sql`) and re-viewed via
  `SoftcopyViewer`.
- Corrected values feed the **Smart Learning Library** (`reference_library` table), which
  supplies autocomplete to the registration forms (`CROMS/Data/LearningLibrary.cs`).

## RECORD MANAGEMENT
Exact current functionality (modules actually reachable in the sidebar):

- **Birth Registration** — PSA Municipal Form 102, full CRUD on `births`, draft/submit,
  edit, update, delete. Prints a visual replica of Form 102 via GDI+ (`BirthCertificatePrinter.cs`).
- **Marriage Registration** — Municipal Form 97, CRUD on `marriages`.
- **Death Registration** — Municipal Form 103, CRUD on `deaths`.
- **Petitions (RA 9048 / RA 10172)** — stages Filed → Posted → Decision → PSA Endorsement,
  `petitions` table.
- **Record Search** — one box across births/marriages/deaths; name LIKE match plus optional
  fuzzy match using **MySQL SOUNDEX**; results UNION into one grid; double-click jumps to
  the record's module.
- **Transactions** — master ledger; one transaction row per client visit, searchable by
  client/code, filterable by status. Statuses: `Queued, Processing, ForPrint, ForPayment,
  ForRelease, WaitingToRelease, Released, Cancelled`.
- **Certificate Request** — CTC / Negative Certification intake; opens a transaction and a
  `certificate_requests` row. Flow: Create Request → Find/Print certificate → Fees & Payment
  → Release. If the certificate is hard to find or the client leaves, the request is parked
  at `WaitingToRelease` (no fee charged yet) and pulled later by the ORIGINAL queue number.
- **Release & Claim** — lists `ForRelease` transactions; records claimant (owner or
  authorized representative + ID), captures an on-screen drawn signature, closes the
  transaction as `Released`.
- **Claim Form** — staff scan the client's claim QR (USB QR/barcode scanner types token +
  Enter), system loads the claim request, shows the ID photo the client uploaded from the
  mobile claimapp, staff verify and release.
- **Master Files** — one screen managing every lookup list feeding the registration
  dropdowns (fixed whitelist of category → table).
- **Users & Audit Trail** — accounts, roles, PBKDF2 password hashing (never plaintext),
  activate/deactivate, and an audit log of create/update/delete/login joined to the user.

## QUEUEING (NEW / CURRENT)
This is the reworked component. Three separate Windows applications share one database:

**1. CROMS.Kiosk — client self-service kiosk (touchscreen)**
- Welcome/attract screen → Step 1 service selection → Step 2 details + photo.
- Service catalogue (`KioskCore.Catalogue`): `NEWREG` New Registration, `CTC` Certified True
  Copy, `MARRIAGE`, `DEATH`, `PETITION` (Correction), `VERIFY` Verification/Others,
  `CLAIM` Release & Claim (Pick-up).
- **Multi-service in one visit**: client can pick several services and still gets ONE queue
  number; each service is a row in `queue_ticket_services` with its own Pending/Done status.
- Captures name, purpose, contact number, and a **live webcam photo** (AForge.Video.DirectShow).
- Priority lanes: Senior / PWD / Pregnant folded into one `priority` column
  (`Priority > PWD > Senior > Regular`).
- Only offers services that at least one ONLINE, active window is assigned to handle;
  checks office-online status by window heartbeat.
- **Prints a thermal ticket** (58 mm, `System.Drawing.Printing`) and shows an on-screen
  confirmation, including "clients ahead of you".
- Queue numbering is **continuous — it never resets daily**, because the ticket number is the
  pull key for a parked request when the client returns.
- For a pick-up (`CLAIM`), the ticket carries a **QR code** (QRCoder) deep-linking to the
  claimapp mobile page so the client uploads a valid ID from their own phone.
- Session is reset between clients (no leftover details/photo); idle timeout returns to the
  attract screen.

**2. CROMS (staff app) — Queue Management**
- Now Serving board + Live Queue built from `queue_tickets`.
- **Dynamic windows**: the `windows` table drives everything — an admin can add/rename/
  activate/deactivate/reorder windows with no code change. Nothing is hardcoded to 3 windows.
- **Window Assignment at login**: each operator claims ONE window and configures which
  transactions that window handles (`window_transactions`). Windows are not exclusive —
  several windows may handle the same service. Presence is tracked by heartbeat
  (online/offline, stale after N minutes).
- **Priority Window** flag (`windows.is_priority`) — handles all services, ticket stays until
  completion, never auto-forwarded unless an admin overrides.
- Two-stage call: **Call Next** merely ACCEPTS a ticket to a window (status `Accepted`, off
  the public board) so the records assistant can locate documents first; **Call Client** then
  flips it to `Serving`, stamps `started_at`, shows it on the public board and plays the
  voice callout.
- **Sequential same-ticket processing**: repeated clicks work through the ticket's services
  one at a time in the order chosen, keeping the SAME queue number; each service's status is
  stored so work resumes after a refresh; the ticket stays on the board until every service
  is done, then the ticket is Completed and the window freed.
- **Forwarding** window → window with the same queue number, logged in `queue_ticket_forwards`.
- Workflow toolbar gates the buttons to the valid next action and states the next step in words.
- Full timing/audit columns on `queue_tickets`: `accepted_at`, `accepted_window`, `called_at`,
  `started_at`, `completed_at`, `recall_count`, `is_priority_ticket`, `final_status`,
  `served_by`, `window_no`.
- Queue tickets link to claim requests (`claim_requests.queue_ticket_id`) so the Claim Form can
  show the kiosk face photo even with no parked transaction.

**3. CROMS.Display — public Now Serving board**
- Separate full-screen, view-only application for the waiting area, self-refreshing every 2 s.
- One card per ACTIVE window, read from the same `windows` table; up to 4 cards per row, wraps.
- **Voice announcement** is spoken here (and only here) using `System.Speech.Synthesis`,
  reading e.g. "Q-016" as "Q 0 1 6"; recalls are announced once.
- Auto-fitting fonts so codes stay readable at any screen size. Esc or double-click to close.

## DATABASE
- **MySQL** (InnoDB, `utf8mb4` / `utf8mb4_0900_ai_ci`), schema name `croms`.
- Connector: **MySql.Data 9.7.0** (NuGet), accessed through a single helper
  `CROMS/Data/Db.cs` (`Push` / `Pull` / `GetCount` / `IsConnected`).
- Schema is versioned as numbered SQL migration files in `CROMS/Database/`
  (`01_schema.sql` … `23_claim_queue_link.sql`), most written idempotently.
- Main tables: `users`, `audit_log`, `transactions`, `queue_tickets`,
  `queue_ticket_services`, `queue_ticket_forwards`, `windows`, `window_transactions`,
  `claim_requests`, `births`, `marriages`, `deaths`, `petitions`, `certificate_requests`,
  `payments`, `fees`, `ocr_batch`, `reference_library`, plus lookup/master tables.
- The database is migrated from the team's earlier OCR system (`03_migrate_from_ocr.sql`).

## PROGRAMMING LANGUAGE
**C#** — no other application language in the desktop system.
(The companion mobile apps are TypeScript/Angular; the small save-API is Node.js.)

## FRAMEWORK / UI
- **Windows Forms (WinForms)** on **.NET Framework 4.7.2** (`TargetFrameworkVersion v4.7.2`
  in all three csproj files — note: CLAUDE.md's "4.8" is inaccurate).
- Three WinExe projects in one solution: `CROMS` (staff), `CROMS.Kiosk`, `CROMS.Display`.
- Shell pattern: modules are real Forms hosted inside MainForm's content panel with
  `TopLevel = false`, built from `CROMS/Modules/ModuleRegistry.cs`.
- Custom UI layer written by hand: `UiTheme`, owner-drawn cards/buttons, `HoverFade`,
  Phosphor icon font, per-DPI scaling via `app.manifest`. No third-party UI control suite.
- Extra libraries: AForge 2.2.5 (webcam), QRCoder 1.4.3 (QR), Tesseract (OCR),
  System.Speech (voice callout).

## REPORTING
- **No report engine** (no Crystal Reports, no RDLC, no SSRS).
- **Reports & PSA module** (`ReportsPsaForm.cs`): pick month + year → counts of births,
  marriages and deaths REGISTERED that month (by `created_at`), the timely-vs-delayed birth
  split (RA 3753 30-day reglementary period, from `is_delayed`), collections total for the
  month, and a per-event-type detail roster. Read-only.
- Export is **CSV** written directly by the app (SaveFileDialog).
- Printed documents are drawn with **GDI+ / `System.Drawing.Printing`**:
  Municipal Form 102 replica (overlaid on a scanned blank form, 792×1224 pt = 11×17 in),
  the 58 mm cashier payment slip, and the 58 mm kiosk queue ticket.

## DEPLOYMENT
- On-premise LAN only. Windows PCs the LGU already owns.
- One PC acts as **server** (MySQL + release share); other PCs run as clients.
- Per-PC server address stored in `%APPDATA%\CROMS\server.cfg` (`ServerConfig.cs`), which
  overrides the host/port of the App.config `Croms` connection string. A built-in
  **Connect-to-Server** screen loops at startup until the database is reachable or the
  operator exits — no hand-editing of config files.
- **Launcher** at startup chooses which app(s) to run on this PC (staff / Kiosk / Display /
  combinations).
- **In-app LAN updater** (`AppUpdater.cs`): the server shares `\\<server-ip>\CROMSRelease`
  with `Main` / `Kiosk` / `Display` sub-folders; a client detects newer exes, pulls them for
  every app installed on that PC, and restarts. Config files are never overwritten. Fully
  offline.
- Client install layout: sibling folders under one parent
  (`...\CROMS\Main\CROMS.exe`, `...\CROMS\Kiosk\CROMS.Kiosk.exe`, `...\CROMS\Display\CROMS.Display.exe`).
- Distribution: zipped bundles in the repo root (`CROMS_AllApps_Bundle.zip`, etc.) plus
  `UPDATE-CROMS.bat`.

## OFFLINE / NETWORK
- **No internet required.** No cloud service, no external API, no subscription.
- The apps DO require the **local network**: every app talks to the one MySQL server over
  LAN/Wi-Fi/mobile-hotspot. There is **no local offline cache and no store-and-forward sync** —
  if the DB server is unreachable, the app shows the Connect-to-Server screen instead of
  working offline.
- MySQL connection uses `SslMode=Disabled` on the LAN.
- Companion mobile apps are served from the staff PC itself: CROMS auto-starts two Ionic/Angular
  dev servers on launch (`IonicServerManager.cs`) — the certificate scanner on port 4200 (http)
  and the **claimapp** ID-upload app on port 4300 — plus a Node save-API. Phones reach them over
  the same Wi-Fi/hotspot; the desktop shows the URL and a QR. Process trees are killed on exit.

## REMOVED FEATURES
- **Document AI feature has been removed.** (⚠ see KNOWN DISCREPANCIES — the code has not
  been cut yet; the module is still present in the build.)
- **Senior / PWD discount (RA 11261) in Fees & Payments** — dropped in
  `18_payment_fields.sql`, replaced by real payment-processing fields.
- **Payment status values pending / cancelled / refunded** — dropped in
  `19_drop_payment_status.sql`; every recorded payment is `Paid`.
- **Hardcoded 3 service windows** — replaced by the dynamic `windows` table.
- **Old single-form kiosk** (`KioskForm.cs`) — replaced by the Welcome → ServiceSelect →
  DetailsPhoto flow.
- **Login-time full-office routing wizard** — replaced by single-window assignment.

## CURRENT SYSTEM FEATURES
Modules actually wired into the staff app sidebar (`ModuleRegistry.cs`), in order:

1. Dashboard — live KPIs (Waiting Now, Registered Today, Collections Today, Pending
   Releases), clickable cards, 7-day registration bar trend, live Service Windows
   online/offline board, timer refresh.
2. Queue Management — as described under QUEUEING.
3. Transactions — master ledger.
4. Certificate Request — CTC / Negative Certification + Find/Print + park to Waiting-to-Release.
5. Release & Claim — claimant capture + on-screen signature + close transaction.
6. Birth Registration — Form 102 CRUD + printed replica.
7. Marriage Registration — Form 97 CRUD.
8. Death Registration — Form 103 CRUD.
9. Petitions — RA 9048 / RA 10172 stage tracking.
10. Record Search — LIKE + SOUNDEX fuzzy across all three registries.
11. OCR Digitization — Tesseract scan → review → commit + `ocr_batch` log.
12. Document AI — ⚠ still present in the build (see KNOWN DISCREPANCIES).
13. Fees & Payments — cashier window: auto-assess fee from `fees`, additional charge,
    payment method, tendered/change, OR / reference number, advance to `ForRelease`,
    print 58 mm slip.
14. Reports & PSA — monthly registered-events report + CSV export.
15. Master Files — all lookup lists in one screen.
16. Settings — Window Management tab + built-in searchable User Manual.
17. Users & Audit Trail — accounts, roles, PBKDF2, audit log.

System-wide features:
- Role-based menu access: **Admin** (all), **Registrar**, **Staff**, **Cashier**, **Releasing**
  (`MainForm.AllowedKeys`).
- Login with PBKDF2-hashed passwords, password policy, admin verification dialog,
  session resume, window heartbeat.
- Audit trail of create/update/delete/login.
- Smart Learning Library autocomplete shared by every module.
- Softcopy viewer for the original scanned certificate stored with a record.
- Claim Form + mobile claimapp ID upload + QR claim tokens.
- Kiosk ticket printing, Display voice callout, LAN auto-update, Connect-to-Server screen,
  desktop shortcut creation, launcher for the three apps.

## FEATURES THAT ARE NOT PRESENT
Never claim these exist:

- ❌ Handwriting recognition / ICR — Tesseract is printed-text OCR only.
- ❌ Machine learning, neural networks, LLM, or any trained AI model. Field extraction is
  regex/label rules.
- ❌ Cloud OCR (Google Vision, Azure, AWS Textract) or any internet API call.
- ❌ Web portal / online public access / online appointment booking.
- ❌ SMS or email notification of queue status.
- ❌ Biometrics (fingerprint, face recognition). The kiosk photo is a stored image only —
  it is never matched.
- ❌ Digital signature / PKI / certificate encryption. The release signature is a drawn
  bitmap, not a cryptographic signature.
- ❌ Direct electronic submission to PSA. The system produces the report + CSV; submission
  is manual.
- ❌ Online payment / e-payment gateway / GCash integration. Payment is recorded, not processed.
- ❌ Offline mode with local cache and later sync — the DB must be reachable.
- ❌ Automatic database backup/restore scheduler inside the app.
- ❌ Registry Books module and Incoming/Outgoing Document Routing module — the forms exist
  in the source tree but are NOT in `ModuleRegistry` and have no sidebar button, so they are
  unreachable in the running app.
- ❌ Senior/PWD fee discount, refunds, or pending payments (removed).
- ❌ Multi-branch / multi-LGU / cloud sync.
- ❌ Mobile app for staff. The two Ionic apps are client-facing (certificate scanner and
  claimapp ID upload) and are served from the staff PC over LAN, not published to any store.
- ❌ .NET 8 / .NET Core / WPF / web front-end. It is WinForms on .NET Framework 4.7.2.
- ❌ Report engine (Crystal/RDLC), PDF export, or Excel export. CSV + direct printing only.

---

## KNOWN DISCREPANCIES (fix before citing this document as final)
1. **Document AI is declared removed but is still in the code.** It remains registered as
   module `docai` (`CROMS/Modules/ModuleRegistry.cs:49`), has a sidebar button
   (`CROMS/MainForm.Designer.cs:385`), is in the Registrar role allowlist
   (`CROMS/MainForm.cs:224`), and `DocumentAiForm.cs` + `Data/DocumentAI.cs` are compiled.
   Further, **OCR Digitization depends on it**: `OcrDigitizationForm.cs:60,74,85` calls
   `DocumentAI.IsAvailable()` / `Analyze()` / `KindName()`. Removing Document AI requires
   either folding that extraction code into `OcrService` or keeping it as an internal helper.
2. **Framework version**: CLAUDE.md says .NET 4.8; the projects target **4.7.2**.
3. **CLAUDE.md still lists 15 modules including Registry Books and Incoming/Outgoing**;
   the running app exposes 17 sidebar modules and neither of those two.
