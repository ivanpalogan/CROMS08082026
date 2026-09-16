# CROMS — UI/UX Redesign Master Plan

Status: **Phase 1 of a multi-phase redesign.** Phase 1 delivers the design system, the full client journey (kiosk → queue → display), the staff shell/dashboard, the flagship Marriage module end to end, Intelligent Document Processing, and Release & Claim — as a real, clickable HTML prototype, not flat images. Phase 2 (specified below but not yet mocked up pixel-by-pixel) covers Birth, Death, Petitions, Payments, Records, Reports, and Administration.

**Working prototype:** [`Docs/UIUX-Mockups/index.html`](Docs/UIUX-Mockups/index.html) — open in a browser, click through in order. Built with plain HTML/CSS/JS against one shared stylesheet (`Docs/UIUX-Mockups/assets/design-system.css`) so every screen is pixel-consistent. To preview with working navigation, serve the folder (e.g. `python -m http.server 4500 --directory Docs/UIUX-Mockups`) rather than opening the file directly, so relative links resolve.

---

## 0. How this was built

Before designing anything, the current CROMS source was audited directly (not screenshots, which weren't available — the literal `UiTheme.cs` token file, `CardPanel`/`KpiCard`/`SplitButton`/`ToggleSwitch`, `MainForm`'s real sidebar and `ModuleRegistry`, `DashboardForm`, `QueueManagementForm`, the full Marriage module — `MarriageRegistrationForm`/`MarriageEntryForm`/`MarriageLicenseForm`/`MarriageCaseForm`/`MarriageRecordForm`/`PsaTransmittalForm`/`MarriageUi`, `MarriageRules`' actual stage vocabulary and statutory settings, the Kiosk's real state machine and 14-service catalogue, `OcrDigitizationForm`, `BreqsForm`, `ReleaseClaimForm`, `FormCatalog`, `LoginForm`, `PetitionsForm`, and `FeesPaymentsForm`). Every color, font size, spacing value, module key, status word, and field group used below is taken from that source, not invented. Where the redesign adds something the app doesn't do today (a notification center, a global search bar, a category step in the kiosk), it's called out explicitly as new.

**What this confirmed CROMS already does well**, kept as-is: the navy sidebar / accent-blue / Segoe UI identity; per-module role gating; a real audit trail; a marriage workflow already built around statutory rules (10-day posting, 120-day validity, 18–20 consent / 21–25 advice bands) rather than freeform status text; an OCR pipeline that never auto-commits without review.

**What this changed:** consolidated the sidebar into 8 clearer groups (no modules added or removed — see §2); introduced one system-wide status/color vocabulary (today each module rolls its own pill colors); added a persistent breadcrumb, global search, and a notification center (none exist today); broke the Marriage License application out of a flat form into a 5-step wizard with conditional consent/advice sections (the screen exists today as a single dialog); added a kiosk "category" step so the 14-service catalogue doesn't hit the client as one wall of buttons; and made every multi-column layout genuinely responsive down to a ~750px window, not just at one fixed resolution.

---

## 1. Design philosophy (see also the visual reference, `00-design-system.html`)

1. **One task per screen.** Show only what the user needs for the step they're on; everything else is a dialog, side panel, or a later wizard step.
2. **Status tells the story.** One color+label vocabulary, used identically everywhere (§3) — a color never means something different in two modules.
3. **Say the next step.** Every workspace states, in plain words, what happens if the primary button is pressed — never a bare "Submit". Every screen in this plan has a "next step" banner or equivalent plain-language cue.
4. **Never fake "done".** A disabled control, a blank field, and a greyed-out section always mean exactly what they look like. This mirrors CROMS's own long-standing engineering rule against a false green tick on unverified OCR output — extended here to the whole UI.

---

## 2. Navigation / information architecture

The real `ModuleRegistry` has 17 modules in 6 groups. Nothing is added or removed; grouping is clarified into 8 shorter groups so no single group runs past 4 items, and two module titles are reworded for a non-technical reader (technical capability is unchanged):

| Group | Modules | Notes |
|---|---|---|
| **Client services** | Dashboard · Queue management · Transactions | unchanged |
| **Civil registry** | Birth registration · Marriage registration · Death registration · Registry books | unchanged |
| **Applications & requests** | Certificate request · PSA copies (BREQS) · Release & claim | was split across two groups before |
| **Petitions & legal** | Petitions & case tracking | renamed from "Petitions" — this module now tracks 6 case types (RA 9048, RA 10172, Legitimation, Supplemental Report, Legal Instrument, Court Order), so the label says so |
| **Document processing** | Intelligent document processing | unchanged |
| **Records** | Record search · Records archive | was mixed into "Petitions & Search" before |
| **Operations** | Fees & payments · Reports & analytics | unchanged |
| **Administration** | Master files · Users & audit trail · Settings | unchanged, admin-only |

Sidebar behavior (new, built in `assets/shell.js`): collapses to icon-only at any window width, or manually via the footer toggle; the active module gets a left accent bar; a numeric badge (e.g. queue waiting count) can sit on any nav item.

**New in the header, not in the app today:** a breadcrumb (`Marriage / Marriage desk / New application`) so the user always knows where they are without remembering how they got there; a global search field (`Ctrl K`) for records/registry numbers/clients/transactions, collapsing to an icon under ~920px; a notification bell with an unread badge. These three are the most-requested-but-missing pieces per the brief's own "Global record header / breadcrumbs / system-wide search" sections — everything else in the shell (logout, biodata, role display, update button) is the app's real header, kept.

---

## 3. Status system (one vocabulary, everywhere)

| State | Color | Used for |
|---|---|---|
| Draft / neutral | Grey chip | Not yet submitted |
| Posting / Processing / In progress | Blue | Marriage posting, OCR review-in-progress, delayed-birth posting |
| Needs attention / Expiring soon | Amber | Missing requirement, license expiring within 7 days, OCR field below confidence threshold |
| Ready / Completed / Verified | Green | Ready to issue, released, requirement verified |
| Expired / Rejected / Missing | Red | Expired license, rejected application, missing required field |

This exact ramp is reused by Marriage licenses, BREQS, Petitions, Birth/Death registration status, OCR field confidence, and Queue tickets — see `00-design-system.html` for every pill in context.

## 4. Button hierarchy

One primary (solid accent blue) button per screen. Secondary actions are the light-grey "chrome" style. Cancel/dismiss is a borderless ghost button. Anything destructive or hard to reverse (reject, delete, release) is red or requires the confirmation-dialog pattern in §7. See the design system page for the full set.

---

## 5. Screens — in the order a user actually encounters them

Each entry: Purpose / User / Entry point / Layout / Main action / Secondary actions / Modals / Validation / Empty state / Next screen. Built screens link to the working file; planned screens (Phase 2) are specified the same way but not yet pixel-built.

### 5.1 System foundation

**Login** — [`01-login.html`](Docs/UIUX-Mockups/01-login.html)
Purpose: authenticate staff. User: any role. Entry: app launch. Layout: split panel — left is office branding/context (navy, matches kiosk/sidebar), right is the credential form. Main action: Sign in. Secondary: Exit. Modal: none (forced password change is a separate existing screen, unchanged). Validation: inline "Incorrect username or password", Caps Lock warning shown live. Empty state: n/a. Error: 5-attempt lockout message with a countdown, per the existing `LoginForm` behavior. Next: Dashboard (or the sidebar's last-remembered module).

**Main shell & Dashboard** — [`02-shell-dashboard.html`](Docs/UIUX-Mockups/02-shell-dashboard.html)
Purpose: orient staff on arrival; surface only actionable items, not decorative stats. User: all roles (role-gated nav, §2). Entry: after login. Layout: sidebar + header (§2) wrapping a content area with 4 KPI cards (waiting now / registered today / collections today / pending releases — all clickable, all real `DashboardForm` metrics), a "Tasks requiring attention" list (new — surfaces marriage licenses ready to issue, licenses expiring, missing requirements, unclaimed releases, pending PSA transmittal, delayed-birth posting — each item routes straight to the record, satisfying "clicking a notification should open the correct record"), recent transactions, service windows board, and a 7-day registration trend. Main action: none singular — this is a dispatch screen; each task card's own button is the action. Modals: none. Empty state: "Tasks requiring attention" shows "Nothing outstanding — you're caught up" rather than a blank list, matching the existing Dashboard's "needs attention" rule of removing a zero row rather than rendering it. Next: any module via sidebar or task click.

### 5.2 Client journey — kiosk → queue → display

**Kiosk — Welcome** — [`10-kiosk-welcome.html`](Docs/UIUX-Mockups/10-kiosk-welcome.html)
Purpose: attract/idle screen. User: walk-in client, touch-only. Entry: kiosk idle. Layout: full-bleed navy, single "Touch to begin" CTA, language toggle. Validation: none. Empty/error: existing app already polls office-online status and shows "Currently closed" — preserved, not shown here since it's a state variant of this same screen. Next: Category selection.

**Kiosk — Choose a category** — [`11-kiosk-category.html`](Docs/UIUX-Mockups/11-kiosk-category.html)
Purpose: **new screen** — progressive disclosure over the real 14-service catalogue so the client isn't shown a 14-button wall. Layout: 6 large touch cards (Birth / Marriage & family / Death / Certifications / Petitions & legal instruments / Other services), each grouping several of the real `KioskCore.Catalogue` services, plus a visually distinct dashed-border band below for **Claim / Release** — deliberately separated per the brief's explicit instruction not to conflate a new request with a pickup. Main action: tap a category. Secondary: Start over. Next: Service selection (new request) or Claim lookup (claim/release) — two different next screens from one decision point.

**Kiosk — Choose a service** — [`12-kiosk-service.html`](Docs/UIUX-Mockups/12-kiosk-service.html)
Purpose: pick the specific transaction within a category (shown for Marriage & family: License application / Registration / PSA copy). Layout: step dots (2 of 3), 3 cards, Back/Continue. Next: Details.

**Kiosk — Your details** — [`13-kiosk-details.html`](Docs/UIUX-Mockups/13-kiosk-details.html)
Purpose: collect only what's needed to place the client in queue and speed up verification at the counter — not the full application (that's completed with staff, per the brief's own philosophy). Layout: name field(s) — adapts to 1 or 2 name fields depending on whether the service is a marriage transaction (matches the real `DetailsPhotoForm`'s husband/wife column behavior), contact number, valid-ID type, priority-lane chips (Senior/PWD/Pregnant, mutually exclusive where the real business rule requires it), optional photo capture. Validation: required fields inline; a priority-lane claim doesn't require proof at the kiosk (verified by staff later). Next: Ticket.

**Kiosk — Queue ticket** — [`14-kiosk-ticket.html`](Docs/UIUX-Mockups/14-kiosk-ticket.html)
Purpose: confirm the transaction is queued and set expectations. Layout: ticket-style card — queue number in large monospace, window assignment, applicants, people ahead, estimated wait. Next: returns to Welcome (kiosk resets for the next client) — matches real kiosk behavior.

**Kiosk — Claim / release** — [`15-kiosk-claim.html`](Docs/UIUX-Mockups/15-kiosk-claim.html)
Purpose: **the differentiated returning-client path** the brief calls out explicitly. No personal-info form — reference/claim-stub number or name lookup only, then a read-only preview of what will be released before a queue number is issued. Validation: "We couldn't find that reference — check the number or ask staff for help" (never a bare "not found"). Next: Ticket (same ticket screen, tagged as a claim rather than a new request so staff see the distinction immediately — see §5.3 and Release & Claim §5.5).

**Public queue display** — [`16-queue-display.html`](Docs/UIUX-Mockups/16-queue-display.html)
Purpose: the "Now Serving" monitor in the waiting area. User: n/a (passive display), read from Queue Management's live state. Layout: 4 large "now serving" cards (one per window, the currently-flashing one highlighted) + a waiting list beneath, same navy identity as the kiosk. No interaction.

### 5.3 Staff queue workflow

**Queue management** — [`20-queue-management.html`](Docs/UIUX-Mockups/20-queue-management.html)
Purpose: the front-desk workspace — matches the real `QueueManagementForm`'s structure exactly (audited, not guessed): 4 KPI tiles, a "my window" serving card, a workflow toolbar with the plain-language next-step line, a filterable/searchable queue table, and an always-visible priority lane table beside it (not a filter — the real app made this an explicit, measured decision on 2026-09-10 so the priority lane is never one click away from being hidden). Main action: **Call next**. Secondary: Call again, Transfer, Skip (danger-toned, visually distinct per the brief's "dangerous actions must not look identical to normal actions" rule), Complete (success-toned). A claim-flagged ticket (from kiosk Claim/Release, §5.2) shows a distinct badge in this table so staff instantly see it's a pickup, not a new transaction — satisfying "clearly differentiate NEW REQUEST vs CLAIM/RELEASE… in staff interfaces" too. Empty state: "No one is waiting" replaces the table, not a blank grid. Next: opens the relevant module (Birth/Marriage/Death/Release/Certificate Request) with the transaction preloaded — "the staff interface should support the conversation rather than force staff to navigate multiple unrelated screens while the client waits."

### 5.4 Marriage module (flagship — built in full)

**Marriage desk (Window 1)** — [`30-marriage-desk.html`](Docs/UIUX-Mockups/30-marriage-desk.html)
Purpose: exactly the brief's own example — the main screen shows applications/applicants/dates/posting status/license status/actions, **never** the full application form. Layout: 6 clickable KPI cards mirroring the real `MarriageRegistrationForm`'s own KPIs (in posting / ready to issue / valid licenses / expiring soon / awaiting Form 97 / pending PSA transmittal — each is a real, distinct business state, not decoration), a 3-tab list (License applications / Registered marriages / Case tracking for delayed-or-exempt cases — mirrors the real desk's tabs plus the real Petitions-style case tracking), filter chips matching the status vocabulary in §3, and a quick-preview panel with a plain-language next-step line ("Posting is not yet complete. The license cannot be issued until September 16, 2026."). Main action: **+ New application** (→ wizard). Row action: Open (→ the relevant window — application detail, or **Issue license** directly from the row when status is Ready to issue, so a same-day-obvious action doesn't require a full navigation). Next: New application wizard, or the license-issuance confirmation dialog (§7), or the registered-marriage record (Phase 2, §6).

**New marriage license application wizard** — [`31-marriage-wizard.html`](Docs/UIUX-Mockups/31-marriage-wizard.html)
Purpose: the brief's central example, fully built and interactive. 5 steps (the brief's 9 conceptual stages collapse correctly to 5 data-entry steps plus 2 tracked-afterward states — Posting and License Issuance are **not** wizard steps, because they're waiting periods and actions, not data entry; they live on the Marriage Desk instead, which is the more honest workflow model):
1. **Applicant information** — groom/bride side by side, name/sex/DOB/age(auto-computed)/citizenship/civil status. The age is computed live and silently drives step 4's conditional logic — no manual "does this person need consent" toggle exists, matching the real `MarriageRules` age-band evaluation.
2. **Address & family** — place of birth (Country → Province → City/Municipality cascade, per the real `GeoLookup` cascade), residence, both parents' name/citizenship/residence for each party. The "previously married" block is conditionally shown only for Widowed/Annulled/Divorced (an explicit non-blocking info banner explains why it's hidden for Single applicants, rather than leaving unexplained blank space).
3. **Requirements** — a checklist (CENOMAR, valid ID, no-marriage-record certificate, plus the consent/advice rows that appear automatically per applicant) with Verified/Pending/Attach actions, mirroring the real `RequirementsGrid` pattern already used for the licence workflow.
4. **Consent / advice** — **the brief's headline conditional-UI requirement, built and verified interactively**: a party aged 18–20 gets an amber "Parental consent required" card (Family Code Art. 14); a party aged 21–25 gets a blue "Parental advice required" card (Art. 15) with the 3-month-deferral note; a party outside both bands gets no card at all — confirmed in the browser that the layout genuinely omits the section rather than rendering it empty.
5. **Review & submit** — read-only summary grouped by section, each group's heading links back to its step, ending in a next-step banner naming the exact consequence: submitting starts the 10-day posting period, and the license cannot be issued before it completes.
Main action per step: **Continue →** (becomes **Submit application →**, success-toned, on step 5). Secondary: Previous, Save draft (persists partial progress — "for long applications, support saving progress," shown via a live "Saved as draft" indicator in the header), Cancel. Validation: required-field errors inline, kept on screen (never cleared) on failed continue. Next: back to Marriage desk, where the new application now shows "Posting — Day 0 of 10."

*(Phase 2, specified but not built as pixel mockups — Marriage record detail, PSA transmittal desk, Expiring/Expired license list views, and the license-issuance confirmation dialog: see §6 and §7.)*

### 5.5 Document processing

**Intelligent document processing (OCR verification)** — [`40-ocr-verification.html`](Docs/UIUX-Mockups/40-ocr-verification.html)
Purpose: exactly the brief's specified pattern, matching the real `OcrDigitizationForm`'s actual side-by-side layout (confirmed from source, not assumed). Layout: left panel is the scanned document with zoom/deskew/replace controls and a field highlight box that would move to match the selected field on the right (shown as two states — a green "Ok" highlight and an amber "needs review" highlight); right panel is the extracted-fields grid, each row showing the field name, editable value, a numeric confidence score color-coded per §3 (never a bare percentage with no color cue), and a status pill (Ok / Needs review / Missing). Main action: **Commit to registry** (birth-only, per the real form's rule that Commit/Draft stay birth-specific) or **Auto-fill registration form** (routes marriage/death scans to their own module). Secondary: Re-OCR, Save as draft, Preview on form (watermarked, non-official preview — matches the real app's explicit safeguard against an unsaved OCR read looking like an issued certificate). Validation: a "needs review" banner names the exact count of flagged fields and explains that editing a value re-checks it immediately, no re-scan needed. Never auto-saves to the official record without this review screen. Next: Birth/Marriage/Death registration record, or the OCR batch log (shown at the bottom of this same screen).

### 5.6 Release

**Release & claim** — [`50-release-claim.html`](Docs/UIUX-Mockups/50-release-claim.html)
Purpose: matches the real `ReleaseClaimForm`'s state-driven rebuild. Layout: left rail is a searchable, tabbed list (For release / Waiting to release); right workspace shows the selected request's claimant fields, side-by-side ID photo vs. kiosk photo for visual comparison (never claims to auto-verify identity — CROMS states facts, staff make the call, per the real app's own documented principle), and a "Before releasing" checklist. Main action: **Verify & release…**, which opens a confirmation dialog naming the claimant and the exact consequence rather than a bare "Are you sure?" (built and verified — see §7). Next: back to the release list, request now shows Released.

---

## 6. Phase 2 — specified, not yet mocked up pixel-by-pixel

The following are fully scoped here (purpose / layout / status vocabulary / next screen) so building them is a direct continuation of this plan, not a fresh design pass. All reuse the design system, shell, status pills, and interaction patterns already built in Phase 1.

**Birth module.** Records list (master workspace — search/filter/status, not a form) → New record (routes through Intelligent Document Processing when scanning a physical certificate, or a direct entry form when there's no scan) → Record detail (master-detail: Overview / Documents / Timeline / Related records / Print, per §6 layout below) → Delayed-registration case tracking (reuses the Petitions-style tracker, since the real `DelayedBirthCaseForm` already models it that way).

**Death module.** Same shape as Birth. Per the brief's own instruction not to invent unconfirmed process, any step the office's actual workflow hasn't confirmed is marked **"Workflow confirmation required"** directly on the screen rather than guessed at — this applies specifically to whether death registration is fully processed in CROMS or primarily tracked (the audited code registers full Form 103 data, so this is likely "processed in CROMS," but is flagged for office confirmation rather than asserted here).

**Petitions & legal / case tracking.** One reusable tracking workflow (already built server-side as `PetitionsForm`'s two stage tracks) with a horizontal status tracker component: RA petitions show Filed → Posted → Decision → PSA endorsement (keeps the statutory 15-day posting step); the other 4 case types (Legitimation, Supplemental Report, Legal Instrument, Court Order) show Filed → Under review → Decision → PSA endorsement. Each stage change writes to the timeline (§ "Audit trail" pattern below).

**Payments.** One centralized workspace (the real `FeesPaymentsForm` already has 5 tabs: Awaiting payment / Walk-in payment / Payment log / Monthly collection / Fee schedule) — redesigned as a master-detail: transaction list on the left with a payment-status pill per §3, itemized fee breakdown + Record payment on the right, with the confirmation dialog pattern (§7) before marking anything paid. Every module that references a payment (Certificate Request, Marriage, BREQS) shows the same status pill rather than re-describing payment state in its own words.

**Release list & PSA copies (BREQS).** Same list-then-detail shape as Release & Claim, using the real 6-state BREQS vocabulary (Requested → Paid → Submitted to PSA → Received from PSA → Released, plus No record at PSA / Cancelled) mapped onto the shared status ramp.

**Records — universal search & record profile.** One search field, an **advanced filters** button that only expands when pressed (never all filters shown at once, per the brief's explicit instruction), and results as a scannable table. Clicking a result opens a Record profile using the master-detail layout: Overview / Personal information / Documents / Transactions / Payments / Timeline / Related records / Audit log / Print — one screen instead of forcing staff to open several windows for related information, per the brief's explicit instruction.

**Reports & analytics.** Report category → date range → filters → Preview → Generate/Print/Export, in that order, so a non-technical user never sees a query builder. The real app's Crystal Reports limitation (no report designer on this deployment — documented at length in the engineering log) means any custom template still has to be authored outside CROMS; the UI's job is to make choosing and running an existing report simple, which this flow does without exposing that constraint to staff.

**Administration.** Users & roles (role = which sidebar groups are visible, per §2 — not a separate permissions language staff have to learn), Master files, Queue/window configuration, Fee schedule editor, Document type / form catalog viewer, Audit log as a readable timeline (not a technical table — see the Audit trail pattern below), System settings.

---

## 7. Cross-cutting patterns (used consistently across every module above)

**Confirmation dialogs** — built and verified in Release & Claim (§5.6); the same component is reused for: issuing a marriage license, rejecting an application, deleting a record, completing a registration, recording a payment. Each names the record and states the exact consequence in one sentence, defaults focus to Cancel, and is reserved for genuinely significant actions — not shown for routine saves.

**Audit trail / timeline** — a vertical dot-and-line timeline (`.timeline` in the design system) showing "what happened, when, by whom" in plain sentences ("Payment recorded — September 16, 9:32 AM — Cashier 02"), never a raw database log. Used on every record-detail screen in Phase 2.

**Empty states** — every list in this plan has a designed empty state (e.g. "No one is waiting," "Nothing outstanding — you're caught up," "No applications yet" with the create action inline) rather than blank space, so the user can always tell "no data" apart from "still loading" or "filtered out."

**Validation** — every error names the specific problem and what to do about it ("Complete the applicant's parental consent information before continuing," not "Invalid input"), and never clears what the user already typed.

**Responsiveness** — every multi-column layout in the built screens collapses to a single column under ~1180px and the sidebar auto-collapses to icons under ~900px (verified in-browser at 750px, 1180px, and 1440px widths); no textbox stretches edge-to-edge on a wide monitor — content is capped to a sensible reading width instead.

---

## 8. Scenario walkthroughs

**1 — New birth-related service.** Kiosk Welcome (§5.2) → Category "Birth" → Service → Details (name/contact/photo) → Ticket → Queue Management (§5.3), staff calls the number, opens the transaction with context preloaded → Intelligent Document Processing if a physical certificate needs scanning (§5.5) → Birth record (Phase 2) → Payments (Phase 2) → status becomes Ready for release → Release & Claim (§5.6).

**2 — Marriage license application.** Kiosk or staff-initiated → Marriage wizard (§5.4) 5 steps, consent/advice conditionally shown → Submit → Marriage desk shows "Posting — Day 0 of 10" → each day advances automatically → Day 10 the KPI card flips to "Ready to issue" and a Dashboard task appears (§5.1) → Issue license (confirmation dialog, §7) → license now tracked with a 120-day expiration on the desk's Valid/Expiring/Expired filter chips.

**3 — Post-wedding marriage record.** Marriage desk's "Awaiting Form 97" tab (already counts real records per the audited `MarriageRegistrationForm`) → staff receives the physical Certificate of Marriage → Marriage record detail (Phase 2) logs date received, solemnizing officer, attaches the scan → Registration tracking status advances → PSA transmittal desk (Phase 2, mirrors the real `PsaTransmittalForm`'s batch workflow) → marked Sent → later Acknowledged → Completed. This whole chain is explicitly labeled **"Tracked/recorded in CROMS"**, not "processed in CROMS," per the office's own confirmed finding that PSA transmission itself happens through PhilCRIS outside this system (§0's audit, `PsaTransmittalForm`).

**4 — Returning client claiming a document.** Kiosk Category → Claim/Release band (§5.2, visually separated from new requests) → reference lookup → read-only preview of what will be released → Ticket (tagged Claim) → Queue Management shows the claim badge distinctly → staff opens Release & Claim (§5.6) with the request preloaded → verify claimant + ID photo comparison → confirmation dialog → Released.

**5 — OCR-assisted encoding.** Scan uploaded in Intelligent Document Processing (§5.5) → fields extracted with per-field confidence → fields below threshold flagged amber and outlined on the scan → staff corrects a value inline, which re-scores instantly → Commit (birth) or Auto-fill (marriage/death, routes to that module's form) → OCR batch log at the bottom of the same screen records the action for audit.

**6 — Special petition / legal instrument.** Petitions & case tracking (Phase 2, §6) → Filed → requirements checked → Under review (or Posted, for RA petitions) → Decision recorded → PSA endorsement → Completed, using the same horizontal status tracker component across all 6 case types.

**7 — Incomplete application discovered.** Record search (Phase 2, §6) → open record → a validation banner on the record itself states the specific missing requirement ("CENOMAR not yet attached") exactly as shown on the Dashboard's Tasks list (§5.1) and the Marriage wizard's own requirements step (§5.4) — the same wording, not re-derived per screen → staff adds the requirement inline → workflow continues from wherever it left off.

**8 — Marriage license expires.** No manual step — the license's status is computed from its issue date against the 120-day validity setting (already how the real `MarriageRules` works), so it moves Valid → Expiring soon (7-day amber warning, appears as a Dashboard task) → Expired (red) automatically. Staff opens the Expired licenses view (Phase 2, §6) from the Marriage desk's filter chip; the record view shows the only available action per actual business rules (re-application, since an expired license itself cannot be reinstated) rather than offering a disabled "Issue" button with no explanation.

---

*Every field name, status word, and business rule cited above (posting = 10 days, validity = 120 days, consent 18–20, advice 21–25, the 6 petition case types, the 14 kiosk services, the 5 Fees & Payments tabs, the BREQS 6-state pipeline) is taken directly from the current CROMS source as of 2026-09-16, not assumed. Anything not yet confirmed by the office is flagged inline rather than guessed at, per the same standard the CROMS engineering log itself has held to throughout this project.*
