# CROMS — UX Redesign Specification

Scope: a complete UX architecture and workflow redesign for the Civil Registry Operations
Management System, grounded in the system as it actually exists (see the CLAUDE.md progress
log and "CROMS — Project Truth & Research Facts.md" for the verified ground truth). This is
not a re-skin — it redesigns *interaction*, not just appearance, and it is honest about what
today's schema and screens already do well versus what has to be built.

## 0. Method and what this keeps vs. changes

CROMS already has real design-system fragments that this spec formalizes rather than
replaces: `UiTheme` tokens, a fixed spacing scale, `CardPanel`/`KpiCard`/`StatusPill`/
`SplitButton`/`ToggleSwitch`, a font-size snap (9pt label / 9.75pt input), print-preview-
before-print on every certificate, a state-machine rebuild of Release & Claim and BREQS
("one state, one action"), and a working split-screen OCR review grid (scan left, fields
right, per-field confidence). Those patterns are correct and this spec extends them
system-wide instead of reinventing them per screen.

What changes: every multi-field registration/application screen becomes a **guided
workflow** instead of one long form; the kiosk becomes a real citizen journey instead of a
sequence of data-entry steps; a formal **post-wedding marriage tracking stage model** is
added (does not exist today — `marriages` has no stage column); global search, a
notification center, and a meaningful dashboard are added; navigation is regrouped around
how staff actually think about their day, not around table names.

---

## PART 0 — Design System

### Typography
| Style | Size | Weight | Use |
|---|---|---|---|
| Page Title | 17pt | Semibold | Top of every screen, next to breadcrumb |
| Section Title | 12pt | Semibold | Card/tab headers |
| Body | 9.75pt | Regular | Field values, table cells (existing CROMS input snap) |
| Field Label | 9pt | Semibold, muted ink | Always visible above/beside its field — never a placeholder standing in for a label |
| Helper Text | 8.5pt | Regular, muted | Under a field: format hint, why a field is disabled |
| Table Header | 9pt | Semibold, muted | Column headers |
| Caption / Meta | 8pt | Regular, muted | Timestamps, "Last updated by…" |

### Spacing (extends the scale already used on Certificate Request/Queue Management)
Page margin 24 · Card padding 20/16/20/18 · Card gap 14 · Column gutter 20 · Field gutter 16 ·
Label-to-input 5 · Row height (tables) 34 · Wizard step rail width 240.

### Color — one palette, status colors reserved
| Token | Role |
|---|---|
| `Ink` / `Ink2` | Primary text / muted text — near-black on white, never pure black |
| `Page` | App background — very light neutral gray, not white-on-white (reduces glare during long shifts) |
| `Chrome` | Card face, white |
| `Primary` (navy/blue) | Primary actions, active nav item, links, selected states — **the only color used for interactive emphasis** |
| `Success` (green) | Completed / Verified / Paid / Active — status only, never decorative |
| `Warning` (amber) | Needs attention / expiring / pending review — status only |
| `Danger` (red) | Errors, destructive actions, rejected, expired — status only |
| `RowLine` | Table/card hairlines |

Rule: a color always means the same thing everywhere in the app. No two cards get different
accent colors "for variety." Icons are Primary or Ink, never rainbow.

### Buttons — one hierarchy, enforced everywhere
| Tier | Look | Example labels |
|---|---|---|
| Primary | Filled navy, one per screen/dialog | Continue · Submit Application · Issue License · Call Next · Complete Transaction |
| Secondary | Outlined navy | Save Draft · Add Another · Print Preview |
| Tertiary | Text-only, muted | Cancel · Back |
| Destructive | Filled red, always paired with a Cancel | Cancel Application · Delete · Void Receipt |
| Icon button | 32px square, icon **+ tooltip**, never icon-only for a primary action | Attach, Refresh, Expand row |

A screen has exactly **one** Primary button visible at a time. If two actions feel equally
important, one of them is wrong — demote it to Secondary or move it into a `⋯` menu.

### Status badges
Pill, colored per Part 0 → Color table, label in plain words (not a DB enum): `Draft`,
`Posting Period`, `Ready to Issue`, `Issued`, `Expiring Soon`, `Expired`, `Awaiting Marriage`,
`Certificate Received`, `Under Review`, `Registered`, `Endorsed to PSA`, `Completed`,
`Requirements Incomplete`, `Waiting for Payment`, `Paid`, `Released`, `Cancelled`. Each
domain gets its **own** status list (Part 6) — CROMS does not force one global enum onto
every module, per the brief.

### Core components
- **Card** — rounded, hairline border, soft shadow (already `CardPanel`). Transparent
  children only — the one bug class this codebase has hit three separate times.
- **Table** — sortable headers, zebra rows off (status color already carries meaning),
  row click opens the record, `⋯` menu for secondary row actions, never more than 2 buttons
  inline per row.
- **Modal sizes** — Small (≈380×220, confirmations/simple edits), Medium (≈600×520, a short
  form or a single-record edit), Large/Full workflow (a dedicated window — used for anything
  multi-step, never a modal).
- **Drawer** — right-anchored, 420px, for contextual detail (activity history, document
  preview) that shouldn't replace the main screen.
- **Wizard/Stepper** — left rail listing every step with state (done / current / upcoming /
  blocked), Back + Continue pinned bottom-right, autosave on every step transition, a Review
  step before final submit. (CROMS already has this shape in the Form 90 stepper — this spec
  generalizes it.)
- **Attachment component** — one implementation everywhere a document is needed: Upload /
  Scan / Preview / Replace / Remove / Download, type + upload date + uploaded-by, and a
  required-vs-optional marker (✓ verified / ! required, missing / ○ optional).
- **Breadcrumb** — `Module > Record` under the page title, always present below the top bar.
- **Notification** — toast for in-session events, persistent bell-menu for anything the user
  should still see after navigating away (see Part 6 → Notifications).
- **Empty state** — one sentence saying *why* it's empty + the Primary action that fixes it.
- **Error banner** — plain-language sentence + what happens to the user's work + a Retry,
  never a stack trace on screen (technical detail collapses under "Show details" for Admin).
- **Loading state** — inline spinner + label ("Processing scan…"), never a frozen screen with
  no feedback.

---

## PART 1 — UX Architecture

### Primary users
| User | Where | Primary need |
|---|---|---|
| Citizen/client | Kiosk | Get a queue number for the right transaction with minimum typing, understand what happens next |
| Front-desk / Receiving staff | Queue Management, intake | Call clients, open their pre-filled request, route them |
| Registrar / Processing staff | Birth/Marriage/Death, Marriage License, OCR, Petitions | Process the legal record correctly, know what's still missing |
| Cashier | Fees & Payments | Assess correctly, log an O.R., never let an unpaid transaction slip through |
| Releasing staff | Release & Claim, BREQS | Verify the right person is receiving the right document |
| Administrator | Users, Master Files, Templates, Audit | Configure the office once, see everything for oversight |

### Navigation architecture (redesigned)
The current sidebar mixes tables ("Registry Books") with workflows ("Release & Claim"). The
redesign groups by **what staff are trying to do**, collapsible, max ~7 top items:

```
Dashboard
Queue & Front Desk            (Queue Management)
Transactions                  (Record Requests · Release & Claim)
Civil Registry                (Birth · Marriage · Death · Registry Books · Record Search)
Marriage License               ← promoted to its own top-level item (it is its own lifecycle,
                                   not a sub-page of Marriage, per Part 6)
Legal & Annotation Transactions (Petitions · Legitimation · Supplemental Report ·
                                  RA 9255/9858 · Court Orders — one screen, filterable by type)
Documents                     (OCR / Intelligent Document Processing · PSA Copies (BREQS))
Payments
Reports & Analytics
Administration                (Users & Roles · Master Files · Document Templates ·
                                  Records Archive · Audit Trail · Settings)
```

Sidebar is collapsible to icon-only; a role sees only groups relevant to it (Part 5, "role
scoping" — CROMS's own semi-admin decision of 2026-09-14 means most operational groups are
visible to every non-Admin role, only Administration is gated).

### Module relationships (how the modules feed each other)
```
Kiosk / Front Desk ──creates──▶ Queue Ticket ──links──▶ Transaction (Certificate Request,
    BREQS, or a routed registration case)
Transaction ──may require──▶ Payment (Fees & Payments)
Transaction ──may require──▶ Requirements/Attachments (shared component, every module)
Scanned document ──runs through──▶ OCR / Intelligent Document Processing
    ──commits into──▶ Birth / Marriage / Death record  OR  ──attaches to──▶ a Requirement row
Marriage License Application ──on issuance──▶ becomes the licence a Marriage Registration
    (Form 97) links to ──▶ post-wedding tracking ──▶ PSA endorsement
Any registered civil-registry record ──may spawn──▶ Petition / Legitimation / Supplemental
    Report / Legal Instrument / Court Order (annotation tracking against that record)
Certificate Request / BREQS ──ends at──▶ Release & Claim
Everything ──writes to──▶ Audit Trail; everything scanned is ──searchable from──▶
    Records Archive and Global Search
```

### Transaction lifecycle — the meta-pattern, not one global enum
Every tracked thing in CROMS moves through the same **shape**, even though the actual status
words differ per domain (Part 6 gives each domain's real list):

```
Opened → Requirements/Information gathered → (Payment, if required) → Processing/Review
    → Decision point → Completed/Issued/Registered → (Endorsed/Released, if applicable)
```

exits available at most points: **Cancelled**, **On Hold**, **Expired** (time-based domains
only: marriage license, BREQS turnaround).

---

## PART 2 — Complete User Flow (master diagram)

```
CITIZEN
  │
  ▼
KIOSK — Welcome → What do you need today? → transaction-specific guided steps →
        ID/document capture if required → Review → Confirm → Queue Ticket printed
  │
  ▼
QUEUE (staff screen) — new ticket appears in Waiting → staff clicks Call Next →
        ticket status → Called → shown on the display monitor / kiosk screen
  │
  ▼
CLIENT walks to the assigned counter
  │
  ▼
STAFF clicks Start Transaction on the called ticket
  → the linked transaction record OPENS AUTOMATICALLY (no re-search — the ticket already
    carries the reference)
  │
  ▼
TRANSACTION — staff verifies information & documents
  ├─▶ if requirements incomplete → status "Requirements Incomplete", client informed,
  │      ticket may be parked (see Scenario 2)
  ├─▶ if a document needs OCR → Documents tab → Scan/Upload → OCR Review → Save to Record
  ▼
PAYMENT (if the transaction has a fee) — assess → O.R. logged → status Paid
  │
  ▼
PROCESSING / DECISION — e.g., licence issued, certificate printed, record registered
  │
  ▼
COMPLETION
  ├─▶ document ready now → RELEASE (claimant verified, signed off) → Released
  └─▶ client must return later → system creates a CLAIM REFERENCE the client can present at
         Release & Claim (only THAT flow ever asks for a prior reference — see Kiosk rule)
  │
  ▼
TRACKING (where applicable) — marriage post-wedding stages, PSA endorsement, audit trail
```

**Alternate paths** (lettered, cross-referenced to the Scenario Matrix at the end of this
document): A — client has no prior ticket and picks a first-time transaction (never prompted
for an old reference); B — incomplete requirements park the transaction without losing intake
data; C — OCR confidence is low and the record cannot be committed until a human verifies it;
D — payment is required and completion is blocked until Paid or an authorized waiver;
E — posting/validity windows gate an action automatically, no manual date math; F — a
transaction is transferred between windows without losing its queue position or history.

---

## PART 3 — Complete Screen Inventory

Legend: **U** = primary user(s). Screen IDs are referenced in Parts 4–7.

### Front Desk / Queue
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-DASH | Dashboard | All staff | KPI tiles, service-window board, insight widgets (posting ending soon, licences expiring, OCR needing review, PSA endorsement pending) | — | click any card → filtered list |
| S-QUEUE | Queue Management | Front desk | Now serving, waiting/priority lane, filters (type/window/status/priority/date) | **Call Next** | opens the called ticket's linked transaction |
| S-DISPLAY | Client Display (public monitor) | Citizen | Now serving + counter, ambient | — | — |

### Transactions
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-CERTLIST | Certificate Requests (list) | Front desk/Registrar | search, filters (status, type, date) | **New Request** | S-CERTNEW or S-CERTDETAIL |
| S-CERTNEW | New Certificate Request | Front desk | intake fields for CTC/Negative Cert | **Submit** | S-CERTDETAIL (status ForPrint) |
| S-CERTDETAIL | Certificate Request detail | Registrar/Cashier/Releasing | status, requester, requested doc, history | status-driven (Find & Print / Proceed to Payment / Release) | next stage screen |
| S-RELEASE | Release & Claim | Releasing | rail (which request) + workspace (state-driven: summary / payment redirect / claimant+verify+checklist / receipt) | **Verify & Release** (only when ForRelease) | receipt view, back to worklist |

### Civil Registry
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-BIRTHLIST / S-MARRLIST / S-DEATHLIST | Registration lists (embedded in each module) | Registrar | recent registrations, search | **New Registration** | detail/entry form |
| S-BIRTH / S-MARR / S-DEATH | Birth / Marriage / Death Registration | Registrar | tabbed entry (child/parents/certification etc.) | **Register / Save Draft** | printed certificate, or Delayed-Birth case (Birth only) |
| S-BOOKS | Registry Books | Registrar | books on file by type+volume, page usage, records in a book | row click | jumps to the record's registration screen |
| S-SEARCH | Record Search | All | fuzzy/SOUNDEX name search across all record types | row click | opens the found record |

### Marriage License (own top-level lifecycle — see Part 6)
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-MLLIST | Marriage License Applications (list) | Registrar | applicants, application no., posting status, license status, expiry, filters | **New Application** | S-MLWIZ |
| S-MLWIZ | New Application — guided wizard (10 steps, per brief) | Registrar | one step at a time | **Continue** | S-MLREVIEW → S-MLDETAIL |
| S-MLDETAIL | Application detail | Registrar | tabs: Overview/Applicants/Requirements/Documents/Posting/License/Marriage Tracking/Payments/History | status-driven Primary (Start Posting / Issue License / …) | S-MLISSUE, S-MLTRACK |
| S-MLISSUE | Issue License (confirmation + print) | Registrar | final review, validity calc | **Issue License** | print preview, S-MLDETAIL updated |
| S-MLTRACK | Post-wedding tracking (new — see Part 6) | Registrar | stage timeline | stage-driven action | PSA endorsement batch |

### Legal & Annotation Transactions
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-CASELIST | Legal & Annotation Transactions (list, filterable by type incl. Petitions/Legitimation/Supplemental/RA9255/RA9858/Court Order) | Registrar | case no., type, linked record, stage | **New Case** | S-CASEDETAIL |
| S-CASEDETAIL | Case detail | Registrar | stage timeline, linked record, requirements, remarks | stage-driven Primary | history, PSA endorsement |

### Documents
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-OCR | Intelligent Document Processing | Registrar | record-type select → scan/upload → split review (scan left, fields right) | **Commit to Record** / **Auto-Fill** | target registration form pre-filled, or record saved |
| S-BREQSLIST | PSA Copies (BREQS) | Front desk/Releasing | tiles (to pay/to submit/at PSA/ready), list | status-driven | S-BREQSDETAIL |
| S-BREQSDETAIL | BREQS request detail | Releasing | status, document match flag | **Receive & Scan** / **Release** | S-RELEASE pattern |

### Payments · Reports · Administration
| ID | Screen | U | Shows | Primary action | Leads to |
|---|---|---|---|---|---|
| S-PAYAWAIT | Awaiting Payment | Cashier | transactions needing payment | **Assess & Charge** | S-PAYRECEIPT |
| S-PAYWALK | Walk-in / Other Payment | Cashier | payer, purpose, fee lines | **Record Payment** | S-PAYRECEIPT |
| S-PAYLOG | Payment Log | Cashier/Admin | date range, search, export | — | — |
| S-PAYMONTH | Monthly Collection | Admin | by fee/source/method | — | export |
| S-REPORTS | Reports & Analytics (6 tabs: Birth/Death/Marriage/Queuing/Certificates + PSA) | Registrar/Admin | charts, PSA monthly submission | **Export** | CSV/PDF |
| S-USERS | Users & Roles | Admin | accounts, role | **Add User** | edit user |
| S-MASTER | Master Files | Admin | lookups (province/municipality/barangay/relationships/etc.) | **Add** | edit row |
| S-TEMPLATES | Document Templates & Forms | Admin | which forms have a Crystal report vs. built-in renderer, alignment | **Align Form** | S-ALIGN |
| S-ARCHIVE | Records Archive | Admin | every scan/photo/form ever saved, by category | row click | S-ARCHDETAIL (full record + view scan) |
| S-AUDIT | Audit Trail | Admin | who/what/when | filter | — |

---

## PART 4 — Kiosk Redesign

The kiosk is redesigned as one citizen journey, connected end-to-end to the staff queue —
not a form. Large text (≥16pt effective), large tap targets, icon + plain-language label
together (never icon-only), obvious Back/Continue on every screen.

| ID | Screen | Content |
|---|---|---|
| K-01 Welcome | "Welcome to the Peñablanca Civil Registry Office" + language toggle if needed + big **Start** |
| K-02 What do you need today? | Large icon tiles in plain language: *Request a Birth/Marriage/Death Certificate* · *PSA Copy (BREQS)* · *Marriage License Application* · *Claim a Document I Already Requested* · *Other/Ask Staff*. Each tile has a one-line explanation, not jargon. |
| K-03 Transaction-specific steps | Only the fields that transaction needs (see rule below), 1–2 questions per screen, big inputs, dependent Province→Municipality→Barangay pickers with search |
| K-04 ID / document capture | Camera step, live framing guide (already built — reused), "Take Photo" / "Choose from Gallery" fallback |
| K-05 Review | Everything just entered, in plain sentences, with an **Edit** link back to the exact step (not "start over") |
| K-06 Confirm | Big **Confirm & Get My Number** |
| K-07 Queue Ticket | Printed ticket: queue number in large type, transaction name, estimated wait, "Please have a seat, we'll call your number" |
| K-08 Waiting / display sync | (On the public monitor, S-DISPLAY) shows number + assigned counter when called |

**Kiosk queue rule (per brief):** K-02's *Claim a Document I Already Requested* is the **only**
tile that asks for a prior reference number (or allows lookup by name if it was lost — see
Scenario 6). Every first-time transaction skips that question entirely; today's kiosk already
gets this mostly right for most services — the rule here is to make sure no future service is
added without checking it against this table first.

**Marriage License at the kiosk** — deliberately routes to *"Start my application in person"*
rather than a full self-service wizard: parental consent/advice, prior-marriage evidence and
the posting-period explanation genuinely need a staff conversation on a first pass. The kiosk's
job here is only to issue a queue number and pre-collect the two applicants' names so the
counter opens directly into Step 1 of S-MLWIZ instead of the receiving staff re-asking "who are
you here for."

---

## PART 5 — Staff System (sequential walkthrough)

1. **Login** — username/password (or badge, if the office adds one later); on success →
   Dashboard scoped to the signed-in role and claimed window.
2. **Dashboard (S-DASH)** — KPI row (Waiting, Serving, Collections Today, Pending Release),
   service-window board, and an "Operations insights" strip whose cards are *only* the ones
   with something to act on (per brief: no decorative cards) — Posting Ending Soon, Licenses
   Expiring Soon, OCR Needing Review, PSA Endorsement Pending, Documents for Release. Every
   card is clickable straight into the matching filtered list.
3. **Queue (S-QUEUE)** — daily operations start here for front desk; **Call Next** is the one
   loud button. Calling opens the client's pre-filled transaction directly (no search).
4. **Transaction** — whichever module the ticket routed to; follows Part 6's per-module
   pattern (List → Detail → Process → Review → Confirm → Result).
5. **Documents/Verification** — OCR review inline where the transaction needs a scan.
6. **Payment** — only shown when the transaction carries a fee; blocks completion until Paid
   or an authorized override (see Payments, Part 6).
7. **Completion/Tracking** — status updates, printed output, or a stage handed to tracking
   (marriage post-wedding, PSA endorsement).
8. **Release** — Release & Claim, state-driven screen (already rebuilt this way in CROMS).
9. **Reports/History** — Reports & Analytics, Records Archive, Audit Trail — always available,
   never in the golden path.

---

## PART 6 — Detailed Module Flows

Each module: **List → Detail → Create/Process → Review → Confirm → Result**, with its own
status model (per brief: not one global enum) and its real button behavior (Part 7 has the
button-by-button spec for the flagship ones).

### 6.1 Marriage License — the flagship, and it is genuinely three separate lifecycles

The brief is right that CROMS should not mix these. Redesign makes them three tracked phases
on one application record, visible as three tabs on S-MLDETAIL, each with its own status:

**Phase A — Application** (statuses: `Draft` → `Posting Period` → `Ready to Issue` → `Issued`;
side exits `On Hold`, `Cancelled`). New Application opens the wizard:

```
Step 1  Applicant 1 — name, sex, DOB (age auto-computed), civil status, citizenship
Step 2  Applicant 1 — birth place (Country/Province/Municipality — structured, never one box)
                       and residence
Step 3  Applicant 2 — same two steps, mirrored
Step 4  Previous marriage / widowed information — shown ONLY if civil status is
        Widowed/Annulled/Divorced for either applicant (conditional UI, per brief)
Step 5  Parents & guardian — one consent/advice person slot per applicant
Step 6  Parental Consent (18–20) or Advice (21–25) — shown ONLY for an applicant whose age on
        filing falls in that band; an applicant 26+ or married-track-not-applicable sees
        neither section
Step 7  Requirements checklist — CENOMAR, valid IDs, etc., each Verified/Missing
Step 8  Supporting documents — attach/scan
Step 9  Review — every answer shown in one readable summary, Edit-in-place per section
Step 10 Submit — creates the application, computes the 10-day posting window automatically
```
Each step: progress rail with step names + done/current/upcoming state, autosave on Continue,
inline validation with plain-language errors, Back never discards data.

**Posting**, shown on S-MLDETAIL as a strip, not a field the staff computes:
```
Application Submitted ✓ → Requirements Complete ✓ → Posting Period (Day 4 of 10, ends
Sep 20, 2026) → Eligible for License Issuance
```
While posting is active, **Issue License is disabled** with the exact copy from the brief:
*"Marriage license can be issued after completion of the required posting period."* The moment
the window closes, a notification fires and the button enables — no staff math, ever.

**Phase B — Issuance.** S-MLISSUE: validates posting completion + required info, final review,
**Issue License** → generates/prints (via the existing MF-90 Crystal path or the print-preview
fallback), stamps issuance date, computes `expiry = issuance + 120 days` and displays
`Active` / `Expiring Soon` (≤14 days, amber) / `Expired` (red) — all derived, never stored as a
choice. Success screen matches the brief's example exactly: license number, issued/expiry
dates, **Print License / View Record / Return to Applications**.

**Phase C — Post-wedding tracking (new).** This does not exist as a stage model in CROMS today
— `marriages` has no stage column, only the fields Form 97 itself asks for. Redesign adds it as
its own tab, S-MLTRACK:
```
License Issued → Awaiting Marriage → Certificate Received → Under Review → Registered
    → Endorsed to PSA → Completed
```
*Certificate Received* is what turns "open Form 97 and type everything in" into a real intake
step: staff scans the signed Form 97, OCR pre-fills the registration (reusing 6.2's engine),
and Under Review is exactly today's registrar-verification work, now given a name and a place
on a timeline instead of being implicit. *Endorsed to PSA* hooks into the same transmittal-
batch mechanism BREQS/petitions already use. **This phase requires a schema migration and is
flagged, not silently assumed built.**

### 6.2 OCR / Intelligent Document Processing — already close to the brief; formalized
```
Select Record Type → Scan/Upload → Image Preview → OCR Processing (progress) →
Split Review (scan LEFT / fields RIGHT, selecting a row highlights that field's box on the
scan — already built) → field-level badges (Verified / Needs Review / Missing / Low
Confidence, mapped from the existing Ok/Uncertain/Missing/Invalid/Conflict verdicts) →
staff corrects → Commit to Record (birth) or Auto-Fill (routes to Marriage/Death/Marriage
License with the module pre-filled, never auto-saved without a human in the loop — already
enforced today and kept)
```
Redesign changes: promote the confirmation dialog CROMS already requires on Commit/Auto-Fill
into the brief's clearer format (document name, confidence, every still-flagged field, named
individually, default answer No); keep the existing rule that a document with an Invalid core
field cannot Auto-Fill until corrected.

### 6.3 Queue Management
Statuses: `Waiting` → `Called` → `Serving` → `Completed`; side exits `Skipped`, `No Show`,
`Cancelled`, `Forwarded`. Staff actions, one Primary at a time: **Call Next** (loudest, always
visible) · Recall · Start Transaction · Transfer · Skip · Complete Transaction — presented as
one Primary + a `⋯` menu for the rest, not six equal buttons (existing Queue Management rebuild
already established this discipline; formalized here).

### 6.4 Certificate Request → Release & Claim
Statuses: `For Print` → `For Payment` → `For Release` → `Released` (existing state machine,
kept). New Request intake stays a single short form (5–6 fields) — this is intentionally
**not** wizardized, per the brief's own instruction not to force every module into the same
shape; a short form doesn't need a stepper. Release & Claim stays the existing state-driven
single-workspace screen.

### 6.5 Birth / Marriage / Death Registration
These are **intake-and-process**, not application wizards — a registrar is transcribing a
completed civil event, usually from a scan, not walking a citizen through a decision tree. Per
the brief's own instruction ("some modules may primarily require intake… rather than a
complicated processing workflow"), these stay tabbed single-screen forms (as already rebuilt
on CROMS's shared layout system), with one refinement: **collapse tabs the OCR pass couldn't
populate into a "needs attention" indicator** on the tab itself, so a registrar reviewing an
OCR-sourced record sees exactly where to look first instead of clicking through every tab.

### 6.6 Legal & Annotation Transactions (Petitions, Legitimation, Supplemental Report, RA
9255/9858, Court Orders)
Two status tracks, already correct in the current schema and kept: RA 9048/10172 correction
petitions use `Filed → Posted → Decision → PSA Endorsement` (their statutory posting period
applies); the other four use `Filed → Under Review → Decision → PSA Endorsement` (no posting
step — matches the researched statutory shape for each). One list screen, filterable by type,
one detail screen — intake-and-track, matching the brief's guidance that this tier doesn't need
a complicated processing workflow, only intake/log/status/endorse.

### 6.7 Payments
```
Transaction → Applicable Fees (from the fee schedule) → Payment → Official Receipt logged
    → Payment Status
```
Current model: `Unpaid → Paid`, plus `Waived` (Admin/Registrar override, audited). **Partially
Paid** and **Refunded** are in the brief but not in the current schema/workflow — flagged as an
open design question for the office rather than silently added (does the office ever accept
partial payment on a civil-registry fee? today's answer observed in the code is no). Hard rule
kept and reinforced in the UI: a transaction requiring payment cannot reach Completed while
Unpaid, without an explicit, audited authorization step.

### 6.8 PSA Copies (BREQS)
Statuses: `Requested → Paid → Submitted to PSA → Received from PSA → Released`; exits
`No Record at PSA`, `Cancelled`; derived `Overdue at PSA`, `Unclaimed`. Already built as a
state machine with OCR-assisted receiving (compares the scanned PSA copy's name against the
request) — kept as-is, formalized into this spec's screen inventory.

---

## PART 7 — Button Behavior (flagship screens)

### Marriage License Applications (S-MLLIST / wizard / S-MLDETAIL)
| Button | Behavior |
|---|---|
| **+ New Application** | Opens the 10-step wizard in a dedicated window (not a modal) |
| **Continue** (each step) | Validates the current step only; on pass, advances and autosaves; on fail, shows plain-language inline errors and does not advance |
| **Back** | Returns to the previous step without discarding anything already entered |
| **Save Draft** | Saves current progress, closes the wizard, returns to S-MLLIST with the application listed as `Draft` |
| **Cancel** (wizard) | Asks "Discard this application, or keep it as a Draft?" — never silently discards |
| **Submit Application** (Step 10) | Creates the record, starts the 10-day posting clock, opens S-MLDETAIL |
| **Start Posting** (S-MLDETAIL, if not auto-started) | Confirms today's date as day 1, shows the computed end date |
| **Issue License** | *Disabled with an explanatory tooltip* while Posting is active; once eligible, opens a confirmation modal naming the license number about to be assigned, issue date, and computed 120-day expiry, then prints |
| **Print License** | Opens print preview (Print / Save PDF / choose printer) — never prints directly |
| **Cancel Application** (destructive) | Confirms consequence in plain words, requires a reason, logged to history |

### Queue Management (S-QUEUE)
| Button | Behavior |
|---|---|
| **Call Next** | Pulls the next ticket by priority-then-FIFO, marks it `Called`, pushes to the display monitor |
| **Recall** | Re-announces the same ticket without changing its queue position |
| **Start Transaction** | Opens the ticket's linked record directly — no search step |
| **Transfer** | Picks a target window/service, keeps the ticket's original issue time and history (Scenario 8) |
| **Skip** | Marks `Skipped`, keeps it available for Recall later that day |
| **Complete Transaction** | Marks `Completed`, clears the window |

### OCR Review (S-OCR)
| Button | Behavior |
|---|---|
| **Re-OCR** | Re-runs recognition on the current scan |
| **Save as Draft** | Stores the reviewed values without committing to the registry |
| **Commit to Record** | Blocked while any core field is `Invalid`; otherwise opens a confirmation naming every still-flagged field, default No |
| **Auto-Fill \<module\>** | Same confirmation, then opens the target registration screen pre-filled — never saves on its own |
| **Send to Manual Review** | Flags the batch, removes it from the "ready" queue without deleting anything |

### Release & Claim (S-RELEASE)
| Button | Behavior |
|---|---|
| **Send to Payment** | Only live when status is `For Payment`; routes to Fees & Payments |
| **Verify & Release…** | Only live when status is `For Release`; opens claimant/ID/checklist confirmation |
| **Scan Claim QR** | Identifies which request — explicitly does not claim to verify the person, per CROMS's own standing rule |

---

## Scenario Matrix — every situation named in the brief, not just the happy path

| # | Scenario | UX response |
|---|---|---|
| 1 | New walk-in wants a record | Kiosk K-02 → guided steps → queue ticket; or front desk creates S-CERTNEW directly |
| 2 | Incomplete requirements | Transaction status → `Requirements Incomplete`; intake data is kept, not discarded; client given a plain list of what's missing and told they may return |
| 3 | Invalid document submitted | Attachment component shows it as rejected with a reason; requirement stays "Missing" until replaced |
| 4 | Client needs to return later | System issues a claim reference (printed) tied to the transaction; that reference is what Release & Claim looks up |
| 5 | Client returns to claim | Release & Claim: search by reference/name/queue number, verify, release |
| 6 | Client lost their ticket/reference | Release & Claim search also works by name + approximate date (SOUNDEX-backed, matching Record Search) — never a dead end |
| 7 | Wrong transaction selected at kiosk | Any kiosk step has Back; K-05 Review has per-section Edit; nothing commits until K-06 Confirm |
| 8 | Transfer to another counter | Queue **Transfer** — keeps history and position (Part 7) |
| 9 | Staff opens the wrong record | Every detail screen's breadcrumb + header identifies the record clearly before any edit; destructive/committing actions always confirm with the record's own name in the dialog text |
| 10 | Duplicate client/record detected | Record Search/SOUNDEX surfaces likely duplicates at intake; flagged, never auto-merged |
| 11 | OCR doesn't match the document | Field-level Low Confidence/Invalid badges block Commit until a human corrects or confirms it |
| 12 | Posting period not yet complete | Issue License disabled, plain-language reason shown inline (brief's own copy) |
| 13 | Posting period completed | Notification fires, button enables automatically — no manual date check |
| 14 | License expires after 120 days | Status auto-flips to `Expired`, red badge, a new application is required — nothing lets an expired license be used |
| 15 | Form 97 returned after wedding | S-MLTRACK: `Certificate Received` step, OCR-assisted intake into the registration record |
| 16 | Needs Petition/Legitimation/Supplemental/Court Order tracking | Legal & Annotation Transactions: New Case, linked to the originating record, tracked on its own stage list |
| 17 | Payment required | Completion blocked until `Paid` or an audited override; cashier screen shows exactly what's owed |
| 18 | Reprint a receipt/document | Every printable record has a **Reprint** in its `⋯` menu, logged as a reprint (not a new issuance) in Activity History |
| 19 | Printer/scanner unavailable | Print preview still renders on screen (Save as PDF always available); scan step offers "Upload instead" as a fallback everywhere a scan is required |
| 20 | Unauthorized action attempted | Button is visible but the tooltip explains the required role/permission rather than a bare "Access Denied" (per brief) |

---

## Open items requiring an office decision (flagged, not assumed)
1. **Post-wedding marriage tracking (6.1 Phase C)** needs a schema migration (a stage column
   and a PSA-endorsement link on `marriages`) before the tab in S-MLDETAIL can be real.
2. **Payment states** — confirm whether `Partially Paid` / `Refunded` are ever used by this
   office before adding them to the payment status model; today's system only has
   Unpaid/Paid/Waived.
3. **Kiosk self-service for Marriage License** — this spec deliberately routes it to an
   in-person start; confirm the office agrees before any self-service version is attempted.
4. **Notification delivery** — this spec assumes an in-app bell/toast center; confirm whether
   anything should also alert off-app (e.g., a printed daily "expiring soon" slip for the
   registrar) given CROMS has no external notification channel today.

---

## Next steps
This document covers architecture, flow, screen inventory, and the flagship modules in full
step-by-step and button-by-button detail. Birth/Death/Petitions/Payments/Reports/
Administration are specified at list→detail→result depth and reuse the same documented system
— any one of them can be expanded to the same wizard-level detail as Marriage License on
request. Visual mockups for the flagship flows (Kiosk journey, Marriage License wizard,
Queue, OCR review, Dashboard) accompany this document in the session; ask for any additional
screen to be mocked up.
