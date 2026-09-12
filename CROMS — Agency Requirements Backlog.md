# CROMS — Agency Requirements Backlog

Source: pre-checkup / mini-interview with LGU Peñablanca LCRO, **2026-09-11**.
Recorded here on 2026-09-12 so the requirements survive the session that gathered them.

**This file is the scope anchor.** Anything not in it is not agreed work. Anything in it marked
PENDING must not be implemented from assumption — it is waiting on the office, and guessing it is
how a government record ends up holding a fact nobody stated.

Three labels are used throughout:

| Label | Meaning |
|---|---|
| **CONFIRMED** | The office said it, and it is consistent with the statute or with the code as it stands. Buildable. |
| **PENDING** | The office said a requirement exists but not its content. A document or an answer is owed before any field, status or rule is written. |
| **SUPPLEMENT** | Not from the interview. Added here from statute or from this repo's own history because it is load-bearing and was otherwise going to be forgotten. Still to be checked against the office's actual practice. |

---

## 0. The one finding that shapes everything else

The office does not want CROMS to become a second copy of every civil-registration system they
already run. Services split into two tiers, and the tier decides how much workflow gets built:

**Tier A — CROMS assists the work**
Marriage Application / Licence · Fee Collection · BREQS · Birth Registration (esp. delayed) ·
Petitions

**Tier B — CROMS records and tracks the work**
Marriage Registration · Death Registration · Legitimation · Supplemental Report · Legal
Instruments · Court Order

Tier B is not "do nothing". The LCRO still performs the statutory registration; CROMS's job there
is the internal record, the attached document, and visibility of where a case sits — not a
rebuilt legal procedure. **The exact CROMS responsibility for each Tier B service still needs
confirming one by one** (§9).

This matches the project's stated design rule (CLAUDE.md: "Realistic, not ambitious") and it is
the reason the roadmap in §10 does not treat every module as equal work.

---

## 1. Address and place of birth — applies system-wide

> **DONE 2026-09-12** (migration 37, applied). Country added and gating the cascade; the one
> genuine free textbox — place of birth on the Marriage Licence — is now Country / Province /
> City-Municipality. Detail in the CLAUDE.md entry of the same date. **Still open:** barangay on
> place of birth (§1 note below), and `v_birth_certificate` does not yet carry the country, so it
> is stored and shown but does not print.

**CONFIRMED.** Place of Birth must stop being one free textbox. It splits into:

- Country
- Province
- City / Municipality
- Barangay

**Country is required** — the office receives applicants born abroad and foreign nationals, and a
Philippines-only address shape cannot record them.

**Scope: every module that records a person's place of birth, not one form.**

> **Repo note.** The cascade already exists — `Data/GeoLookup.cs` plus migration
> `29_philippine_geography.sql` (87 provinces / 1,647 municipalities / 42,029 barangays, PSGC-coded).
> Birth, Death and Marriage registration already use it. What is missing is **Country**, which no
> table currently holds, and an audit of which remaining forms still use a single textbox.
> So this is an extension of existing work, not new machinery.

**SUPPLEMENT — the trap to avoid.** A foreign place of birth cannot cascade: there is no province
or barangay list for Japan. Country must gate the other three (PH → cascade; anything else → free
text for the foreign locality), or every non-PH birth becomes unrecordable. Decide this before
adding the field, not after.
*Resolved 2026-09-12:* `GeoLookup.CascadeCountry` empties the Philippine lists for a non-PH
country rather than disabling the cells, so the clerk types the foreign locality into the same
three boxes. Emptied, never disabled — a disabled cell would make a foreign birth unrecordable,
which is the exact failure this field exists to prevent.

### ⚠ Open question for the office — barangay on place of birth

The office listed **Barangay** as one of the four parts. It is NOT part of the work done on
2026-09-12, deliberately:

- **MF-102** does print a barangay in the place-of-birth block ("Name of Hospital/Clinic/
  Institution · House No., St., Barangay · City/Municipality · Province"), so adding it there
  would be faithful to the form.
- **MF-90** (the licence application) asks place of birth as city/municipality and province only.
- Birth Registration's place-of-birth cell already holds **facility / province / municipality**,
  and the facility is not the same thing as a barangay.

So "add barangay" is a **new field on MF-102**, not the de-textboxing the office asked for, and
which forms should carry it differs by form. **Ask before adding it.**

---

## 2. Marriage Application — applicant fields

> **Sex: DONE 2026-09-12** (migration 36, applied).
> **Parents / guardian and the widow block: no longer PENDING as of 2026-09-13** — the blank
> MF-90 arrived and states them exactly. See §2a for the form's own field list. Only the
> another-province document (§4.1) is still unanswered.
> **Parents, consent/advice person and previously-married block: DONE 2026-09-13** (migration 38,
> applied). Names are three cells as MF-90 prints them; the old joined `*_father_name` /
> `*_mother_name` columns are retired (read only for older licences, never split). **They print on
> Form 90 as of 3B (2026-09-13)**; CROMS's own licence printout still shows only the joined names.

**CONFIRMED.**

- **Sex** must be added to applicant information on the marriage-licence application.
  *Done: a Male/Female pick per party, stored in `marriage_licenses.husband_sex` / `wife_sex`,
  shown on the printed application. It is pre-picked from the column (husband → Male) because
  that is the lawful answer the form's own two columns imply, but it stays visible and editable —
  a sex entry can itself have been corrected under RA 10172, so it is a fact the applicant states,
  not one the software concludes. Applications filed before the field existed hold NULL and are
  not backfilled.*
- **Both parents** (Father and Mother) for each applicant.
- **Guardian** where a parent is unavailable — **one guardian is enough**, not two.

Reason parents matter here: the parental consent and parental advice documents carry parent
information and signatures, so the licence application has to hold what those forms print.

~~**PENDING.** The exact parent/guardian fields and signature blocks must come from the office's
own Parental Consent and Parental Advice forms (§8), not be inferred.~~
**ANSWERED 2026-09-13 — see §2a.**

---

## 2a. What Municipal Form 90 actually asks — measured from the blank

**CONFIRMED.** The office supplied `Application-for-Marriage-License_Municipal-Form-90_BLANK.pdf`
— a **vector** blank, **Revised January 1993 (Form No. 2)**, page box **612 × 936 pt** (8.5 × 13 in
long bond), already carrying "Peñablanca" / "Cagayan" and the registrar's printed name.

It is a **two-column form**: one applicant per column (left ≈ x43–276, right ≈ x315–548) with the
field labels printed down the **centre gutter** — the same structure as MF-97. Every row below,
with its label's y-coordinate, was read out of the PDF's own content stream, not transcribed by
eye:

| y | Field | Cells |
|---|---|---|
| 672 | **Name of Applicant** | First · Middle · Last |
| 640 | **Date of Birth** + Age | Day · Month · Year · Age |
| 608 | **Place of Birth** | City/Municipality · Province |
| 578.6 | **Sex (Male/Female)** | one |
| 558.6 | Citizenship | one |
| 538.6 | Residence | one |
| 518.6 | Religion | one |
| 498.6 | Civil Status | one |
| 482–467 | **If previously married: how it was dissolved** | one |
| 454 | **Place where dissolved** | City/Municipality · Province |
| 430 | **Date when dissolved** | Day · Month · Year |
| 406–391 | **Degree of Relationship of Contracting Parties** | one |
| 378 | Name of Father | First · Middle · Last |
| 346.6 / 326.6 | Father's Citizenship / Residence | one each |
| 310 | Name of Mother | First · Middle · Last |
| 278.6 / 258.6 | Mother's Citizenship / Residence | one each |
| 242 | **Person who gave consent or advice** | First · Middle · Last |
| 212.6 / 192.6 / 172.6 | that person's Relationship / Citizenship / Residence | one each |
| 120 | Exempt from | one |
| 102 | Documentary (stamp) | one |
| 95–82 | Subscribed and sworn — day, month/year | printed boilerplate + blanks |
| 56–44 | Registrar signature over printed name | pre-printed |

**What this settles, item by item:**

- **Sex (§2)** — it IS on the form, at y578.6. Confirms the field built on 2026-09-12.
- **Both parents (§5)** — father and mother each get name + citizenship + residence.
- **Guardian (§5)** — there is **one** slot, "Person who gave consent or advice", with its own
  name / **relationship** / citizenship / residence. Matches what the office said: only one
  guardian is needed. Note it is one slot for **either** the consent-giver or the advice-giver,
  not two.
- **Widowed applicant (§4.2)** — **answered in full.** The form asks exactly three things: *how*
  the previous marriage was dissolved, the *place* (city/municipality + province), and the *date*
  (day/month/year). Nothing more. No PENDING left here.

**Two fields CROMS's licence screen does not have and the form does:**

1. **Degree of Relationship of Contracting Parties** — this is the consanguinity/affinity
   declaration behind Family Code Arts. 37–38 (incestuous and void-by-public-policy marriages).
   CROMS has no field and no check for it.
2. **Religion** — CROMS *does* have this one. Listed only to confirm the mapping is complete.

**SUPPLEMENT — buildable now.** The blank is vector, so it exports to PNG and gets a print map the
same way MF-97 and MF-103 did on 2026-09-07. That makes §18 (print the Marriage Application from
CROMS) real work rather than a blocked item. The page box already matches the long bond those two
print on.

---

## 3. Marriage Application — age-based requirements

**CONFIRMED by the office and by the Family Code.** Two different requirements, not one:

| Applicant age | Requirement | Statute |
|---|---|---|
| 18 – 20 | **Parental CONSENT** — from parent, surviving parent, guardian, or person having legal charge | Family Code Art. 14 |
| 21 – 25 | **Parental ADVICE** — applicant must *seek* it | Family Code Art. 15 |

**Effect of a failure differs, and the difference is the whole point:**

- **Consent missing (18–20):** the licence process does not proceed. Consent is a required
  licence document for that age band.
- **Advice unfavourable or not obtained (21–25):** the marriage licence **cannot be issued until
  three months after completion of the publication** of the application. It is a statutory delay,
  not a block.

**SUPPLEMENT — counselling.** Family Code Art. 16: where consent or advice is required (i.e. the
whole 18–25 band), a **marriage counselling certificate** is also required. The office did not
raise it in the interview. The repo already implements this (`MarriageRules.cs`, and counselling
is the only requirement CROMS lets be waived) — so CROMS is currently *stricter than the
interview*, correctly so. **Confirm it is the office's actual practice.**

**SUPPLEMENT — RA 6809.** Age of majority is 18. Someone will eventually reason "18 is an adult,
so drop the consent rule". The Family Code marriage provisions survive RA 6809 as a special rule
for marriage. Do not delete consent/advice logic on majority-age grounds.

**SUPPLEMENT — hard stop.** RA 11596 voids marriage below 18. That is a hard stop and is already
enforced on both Form 90 and Form 97.

### The two forms themselves — received 2026-09-13

**CONSENT — "CONSENT TO MARRIAGE OF A PERSON UNDER AGE", Municipal Form No. 06.**
One form, signed by one person. Its body reads:

> I, **[consenting parent/guardian]**, resident of **[address]**, *(Father / Mother) (GUARDIAN)*
> of **[applicant]**, a resident of **[address]**, single and **less than (twenty one) years of
> age**, being duly sworn, do hereby depose and say that I freely consent **[applicant]** marrying
> with **[other party]**, resident of **[address]**, and that I know of no legal impediment to such
> marriage.

Then: signature of *Father, Mother or Guardian*; **two witness lines** — annotated *"Not necessary
if this affidavit is subscribed before the Local Civil Registrar concerned"*; a subscribed-and-sworn
block (day, month/year, at Peñablanca, Cagayan, Philippines); and the Municipal Civil Registrar's
signature as the person administering the oath. The printed INSTRUCTIONS cite **Rep. Act 236 Art.
61** and define "person having legal charge".

**Fields CROMS would need:** consenting person's name + address + which role (Father / Mother /
Guardian), applicant's name + address, the other party's name + address, oath date. All of the
person fields already exist on MF-90's "Person who gave consent or advice" block (§2a), so this
form is largely a second rendering of data CROMS already captures — plus the **role marker** and
the **other party's residence**.

**ADVICE — "ADVICE UPON INTENDED MARRIAGE", Municipal Form No. 68 (Form No. 6).**
**Two separate sheets, one (MALE) and one (FEMALE)** — not one form covering both parties. Body:

> To **[applicant]** — Our/my advice upon the intended marriage with **[other party]** having been
> asked by you and knowing no legal impediment to this marriage will hereby advice you to marry
> him/her **LEGALLY**.

**Three signature slots, not two:** *(Signature of Father)*, *(Signature of Mother)*, and
*(Signature of Legal Guardian of Head of Institution)*. Then subscribed and sworn (day, month/year,
at Peñablanca, Cagayan) over the administering officer.

**Two things worth noting from the sample:**

1. A **deceased parent is annotated on the form** — one sample shows the father's name printed with
   **"(DECEASED)"** under it and only the mother signing. So CROMS needs a way to record *why* a
   parent did not sign, not just that they did not.
2. The printed wording is a **favourable** advice only ("will hereby advice you to marry
   him/her LEGALLY"). There is no unfavourable variant on the sheet. In practice an unfavourable
   or withheld advice means **this form is simply not produced** — which is what triggers the
   three-month deferral. That matches how CROMS already models it (the licence is deferred, not
   blocked), and it means *absence of the document* is the signal, not a field on it.

### ⚠ The band boundary — a question for the office, NOT a bug to fix

The interview says **21–25**. `app_settings` in migration `33_marriage_workflow.sql` says
**21–24**:

```
('MARRIAGE_CONSENT_AGE_TO', '20', 'FC Art. 14 "between eighteen and twenty-one" read as 18-20. CONFIRM WITH LCRO.'),
('MARRIAGE_ADVICE_AGE_TO',  '24', 'FC Art. 15 "between twenty-one and twenty-five" read as 21-24. CONFIRM WITH LCRO.'),
```

**This is not an off-by-one.** It is a deliberate and internally consistent reading of the
statute: "between eighteen and twenty-one" excludes 21, so consent is 18–20; by the same logic
"between twenty-one and twenty-five" excludes 25, so advice is 21–24. The code already flags both
for confirmation.

The interview's "21–25" is how the requirement is normally *spoken*, and many LCRO checklists are
written that way too. **The single real question is: does a 25-year-old applicant need parental
advice at this office?**

- If **yes** → change `MARRIAGE_ADVICE_AGE_TO` to 25.
- If **no** → leave it; the reading is correct and the interview note is loose phrasing.

**Do not "fix" this from the interview note alone.** Silently widening the band makes CROMS demand
a document the office does not demand, and an applicant is turned away for it.

**ANSWERED 2026-09-13 (user, from the office): advice is 21–25.** A 25-year-old needs parental advice.
Applied: `MARRIAGE_ADVICE_AGE_TO` = **25** (migration 39), code default and a boundary test (25 needs
advice, 26 does not).

**HALF-ANSWERED 2026-09-13 by the office's own consent form.** Municipal Form No. 06 describes the
applicant as *"single and **less than (twenty one) years of age**"*. "Less than twenty-one" excludes
21, so the **consent** band is **18–20** — exactly what `MARRIAGE_CONSENT_AGE_TO = 20` already says.
That line can be treated as confirmed.

The **advice** band is still open: the advice form (Municipal Form No. 68) carries no age wording at
all, so nothing on paper settles whether a 25-year-old needs it. **Still one question to ask.**

---

## 4. Marriage Application — other cases

### 4.1 Applicant from another province
**CONFIRMED:** an extra supporting document / attachment must be possible.
**PENDING:** *what* document, *when* it is required, and whether the trigger is residence, place
of birth, previous registration, or something else. Do not assume — record the attachment slot,
not the rule.

### 4.2 Widowed applicant — **ANSWERED 2026-09-13**
**CONFIRMED:** previous-marriage information must be capturable.
~~**PENDING:** exactly which fields.~~ The blank MF-90 states them, and there are only three
(§2a): **how the previous marriage was dissolved**, the **place where dissolved** (city/municipality
+ province), and the **date when dissolved** (day/month/year). Note the form says *"If previously
married"*, not *"if widowed"* — so it covers annulled and dissolved marriages too, which is
consistent with CROMS's existing `CivilStatuses` list.

*Done 2026-09-13:* the block is live for a party whose civil status is Widowed, Annulled or Divorced
and **greyed and cleared** otherwise (the server writes NULL too). "Married" / "Separated" do not
open it — those marriages have not been dissolved; they go to the registrar's Art. 18 finding.
"How it was dissolved" is an editable suggestion list (Death of spouse / Annulment / Declaration of
nullity / Divorce), not a closed rule. Place dissolved is two columns (province, city), never one
joined string.

> **SUPPLEMENT.** Migration 33 already stores `license_basis` and civil status, and already
> declines to hard-block "Widowed"/"Separated" — it blocks until the registrar *records a
> finding*, per Art. 18. The widow's prior-marriage detail is the missing data, not the rule.

---

## 5. Marriage Application vs Marriage Registration — keep them separate

**CONFIRMED, and it is an explicit correction from the office.**

- **Marriage Application / Licence** — the existing CROMS process was considered *sufficient*,
  subject to the corrections in §2–§4.
- **Marriage Registration** — the office does **not** want the whole application re-entered. After
  solemnisation the staff receive the issued licence and the accomplished Certificate of Marriage,
  add registration details, and record the marriage.

**PENDING and important:** exactly what the staff add. The interviewee could not fully recall.
Candidates mentioned: dates, registry information, signatures. Needs confirming.

**PENDING:** the CROMS ↔ **PhilCRIS** boundary. The office already uses PhilCRIS for the PSA
side. CROMS should not duplicate what PhilCRIS already does.

**SUPPLEMENT — do not under-build this on the interview alone.** "The office just stores the
document" is too strong a reading. PSA guidance: the solemnising officer reports the marriage to
the City/Municipal Civil Registrar **within 15 days** (licensed marriages); the Civil Registrar is
responsible for the correct form being used, properly and completely accomplished, with the
required attachments. Act No. 3753 requires entry in the marriage register. The honest reading is:
*the LCRO does perform official registration; CROMS may not need to duplicate the processing if
PhilCRIS already carries it.* Where the line falls is the open question.

---

## 6. Death Registration

**CONFIRMED (as reported):** the hospital / health personnel prepare most of the certificate. The
office said CROMS does not need a complicated death-certificate workflow — mostly it needs the
record stored.

**PENDING:** whether the office still enters a date, signature, registry number or similar
registration detail — the interview was uncertain, and this is precisely the content of the
module.

**SUPPLEMENT — again, do not under-build.** PSA: for a death in a hospital or clinic, the
physician or hospital administrator prepares the certificate and certifies cause of death; it goes
to the health officer, who examines it, signs, and **orders its registration with the Civil
Registrar**. Registration is at the LCRO of the city/municipality of death, **within 30 days**.
Act No. 3753 requires it recorded at the LCRO. So the LCRO performs the registration — CROMS's
share of it is what is undecided.

> **Repo note.** Migration `30_marriage_death_certification.sql` already added the full items
> 26–29 certification block to `deaths` (informant, prepared-by, received-by, registered-by, each
> with title and date). The Death Registration screen got those controls on 2026-09-10. So the
> storage exists; the question is which of it the office actually fills.

---

## 7. New tracking-only case types

**CONFIRMED.** Four case types need Petition-style tracking — a list, a current stage, and
movement through stages. The office asked for a **database and tracking system**, explicitly *not*
for CROMS to perform the legal process or generate the official documents.

1. **Legitimation** — RA 9858
2. **Supplemental Report**
3. **Legal Instruments** — RA 9255 (father's surname) and RA 9858
4. **Court Order**

**PENDING — the stage names.** "Filed → Endorsed to PSA → Released to Client" was offered as an
*example*, not a confirmed list. **Do not commit these to an enum or a status column until the
office describes how these documents actually move through their office.** A wrong status list is
worse than none: it looks authoritative and everyone fills it in.

**SUPPLEMENT — these are real PSA categories, so the vocabulary is not invented.** PSA annotation
services recognise Supplemental Reports, Court Decrees, Legal Instruments, Legitimation, and RA
9255 acknowledgment. RA 9255 instruments (Affidavit of Admission of Paternity, Affidavit to Use
the Surname of the Father) are recorded in the Register of Legal Instruments.

**SUPPLEMENT — build one, not four.** If the stages turn out similar across the four, one generic
case-tracking table + screen with a case-type discriminator beats four near-copies. Decide after
the stages arrive, not before. The Petition module is the pattern to follow either way.

---

## 8. Fee collection and payments

**CONFIRMED.** The office needs a fuller fee-collection / payment-transaction system. Specifically:

- Payments must be recordable **even when they do not originate from a CROMS module** —
  miscellaneous and walk-in collections, not only module-linked fees.
- Every payment needs a **purpose of transaction** — enough to say *why* the payment happened.
- A **payment log** showing all transactions.
- Feeds the **monthly fees-collected report**.

**RECEIVED 2026-09-13 — the office's own fee card, headed "PLS. PAY AT TREASURY OFFICE":**

| Fee | Amount |
|---|---|
| BREQS fee | 50 |
| Certified copy | 50 + 30 |
| Certification fee | 100 + 30 |
| Annotation fee | 100 |
| Marriage application fee | 1,000 |
| Marriage solemnization fee | 1,000 |
| Marriage license fee | 200 |
| Electronic endorsement fee | 500 |
| Out of town registration | 500 |
| Filing fee CCE — RA 9048 | 1,000 |
| Filing fee CCE — RA 10172 | 3,000 |
| Filing fee CFN — RA 9048 | 3,000 |
| Migrant petition fee | 1,000 |
| Burial permit fee | *(no amount printed)* |
| Transfer of cadaver | *(no amount printed)* |

The card's own heading **confirms the 2026-07-23 decision**: collection belongs to the Treasury and
CROMS records the O.R., it is not a cash drawer.

### 🔴 The seeded fee amounts are wrong, and the cashier screen is using them

`fees` still holds the placeholders seeded on 2026-07-14 and flagged then as "realistic but
unverified". Against the office's actual card they are wrong, and the Fees & Payments screen
assesses from this table today:

| Code | Seeded now | Office's card |
|---|---|---|
| `CTC-BIRTH` / `CTC-MARRIAGE` / `CTC-DEATH` | **155.00** | **50 + 30 = 80** |
| `NEG-CERT` | **210.00** | certification fee **100 + 30 = 130** |
| `PET-9048` | **0.00** | 1,000 (CCE) / 3,000 (CFN) |
| `PET-10172` | **0.00** | 3,000 |
| `BURIAL` | 0.00 | not printed on the card |

Ten more fee types on the card have no row in `fees` at all.

> **ANSWERED 2026-09-13 by the user, from the office:** the "+ 30" is **part of the fee** — record
> ONE amount (certified copy **80**, certification **130**), not a separate line. And the card itself
> answers the petition question: the fee follows the **petition type** — CCE under RA 9048 **1,000**,
> CCE under RA 10172 **3,000**, CFN under RA 9048 **3,000**, migrant petition **1,000**. So `PET-9048`
> must split into CCE and CFN codes. Burial permit and transfer of cadaver still have **no amount on
> the card** — those two stay unset until the office states them. *Scheduled after BREQS (§15).*

**Two things to settle before changing a single amount** *(both answered above)*:

1. **What is the "+ 30"?** Almost certainly the documentary stamp tax charged on top — but
   "almost certainly" is not a fact about a government fee. Does CROMS record 80 as one amount, or
   50 and 30 as two lines? The `payments` table already has `additional_fee`, which would hold a
   separate 30 cleanly.
2. **`PET-9048` covers two different fees on the card** — CCE (correction of clerical error, 1,000)
   and CFN (change of first name, 3,000). One code cannot carry both; the petition type has to pick
   the fee.

Until both are answered the amounts stay as they are, wrong but unchanged — quietly rewriting a fee
schedule from a phone photo is not a call to make alone.

> **Repo note.** 2026-07-23 established collection stays with the Treasury and CROMS records the
> Treasury O.R. — a fee module must not turn into a cash drawer. Reports & Analytics already has
> the reporting structure to hang the monthly report on.

---

## 9. BREQS — PSA Batch Request System

> **DONE 2026-09-13** (migration 40, applied). Kiosk step + staff desk "PSA Copies (BREQS)", the
> status design below, received PSA copies scanned through the OCR engine and name-checked against
> the request, history + audit on every move. Detail in the CLAUDE.md entry of the same date.

**CONFIRMED.** Two sides, both needed:

- **Kiosk side** — client requests a PSA copy of a Birth / Marriage / Death document. Client
  presents a **valid ID**. Not released same-day; the client returns later to claim.
- **Staff side** — a BREQS window inside CROMS so staff can view and manage submitted requests,
  and eventually release the PSA document.

**SUPPLEMENT — what BREQS actually is.** PSA's **Batch Request System**: an authorised partner
(LGU/LCRO) receives requests from the public; the actual processing is done by the PSA Serbilis
Outlet assigned to that partner. PSA supplies the partner with software, templates, updates and
procedures. Coverage includes birth, marriage and death documents, certain annotated or endorsed
documents previously issued by PSA, and **CENOMAR**. The interview's description is correct.

**PENDING — and one correction.** The **"about one week" turnaround must not be hard-coded.** It
was a local estimate; actual turnaround depends on the PSA/partner arrangement and backlog. Make
it a setting. Also pending: accepted valid IDs, local fees, the internal status list, how the
office records submission to PSA, and how it records receipt and release.

**ANSWERED 2026-09-13 (user, from the office).** BREQS works like a CTC request, except the document
comes from **PSA**, not from the LCRO's own register:

1. The client logs a request: **name, valid ID type, ID number**, the **certificate wanted**
   (birth / marriage / death) and the **details of that document**.
2. CROMS stores it with a **status** so staff can see what is still pending.
3. Staff collect the documents from the PSA office **in person**.
4. On arrival, the PSA copy is **scanned through CROMS's OCR**, so the system records that this is
   the document that person asked for, and **keeps a copy**.
5. Intake: **kiosk AND staff window** (user's decision, 2026-09-13). Fee: BREQS fee **50** (card).

**Status design** — the user asked CROMS to decide these (SUPPLEMENT; built on how PSA's own
request tracking reads, adjust if the office uses other words). Stored statuses:

| Status | Means | Set when |
|---|---|---|
| Requested | logged, not yet paid | kiosk or staff intake |
| Paid | Treasury O.R. recorded | staff records the O.R. |
| Submitted to PSA | request sent through BREQS | staff enters the BREQS reference and date |
| Received from PSA | PSA copy collected, scanned and attached — ready to release | scan attached |
| Released | handed to the client (or authorised representative) | release recorded |
| No Record at PSA | PSA returned no record / a negative result | staff records it |
| Cancelled | withdrawn | staff, with a reason |

**Derived, never stored** (so they cannot go stale): *Overdue* = submitted and past the expected
date (submitted + `BREQS_TURNAROUND_DAYS`, a setting, default 7); *Unclaimed* = received and not
released after `BREQS_UNCLAIMED_DAYS` (setting, default 30).

**SUPPLEMENT — who may request.** PSA restricts civil registry documents to the document owner or
someone with a right to it (parent, spouse, child, guardian, or a representative with an
authorisation letter and IDs). CROMS therefore records the requester's **relationship to the
document owner**. Whether the office also demands an authorisation letter on file is **not** assumed.

---

## 10. Timely vs delayed birth registration

**CONFIRMED.** Birth registration cannot be one process. Registered within 30 days → normal.
Beyond 30 days → **delayed registration**, with extra requirements and procedures.

**RECEIVED 2026-09-13 — the office's own checklist, headed "REQUIREMENTS FOR DELAYED REGISTRATION
(PSA MC No. 2024-17 dated JUNE 4, 2024)":**

- **a.** Negative Certification of Birth from PSA
- **b.** Affidavit of Two Disinterested Persons
- **c.** **Any TWO** of the following evidences of birth — Baptismal Record · School Record ·
  Voter's Record · Medical Records · Marriage Certificate · Barangay Certification · Any
  Government-issued ID · Police/NBI Clearance
- **d.** Certificate of Residency
- **e.** National ID
- **f.** Latest 2×2 ID picture *(Death Certificate if deceased)*
- **g.** Marriage Certificate of Parents *(Affidavit of Parents if not married, thru RA 9255;
  Affidavit / Sworn Statement stating the whereabouts of the mother if she is not available)*
- **h.** Documentary evidence showing identity of parents — Valid IDs · Birth Certificate ·
  Death Certificate (if deceased)
- **i.** Registrant's Affidavit (that all documents submitted are authentic)
- **j.** Personal appearance of the registrant, or of the parents if a minor

**Two things about this list that shape the build, not just the data:**

1. **Item (c) is "any two of eight"** — so the requirements model cannot be a flat checklist of
   required documents. It needs a *group* with a satisfy-count. The existing
   `marriage_requirements` table is one row per named requirement with a status, which does not
   express "any two of these eight". Either that table gains a group + threshold, or delayed
   registration gets its own shape. **Decide before building.**
2. **Items (f), (g) and (h) are conditional** — on the registrant being deceased, on the parents
   being unmarried, on the mother being unavailable, on a parent being deceased. Same conditional
   shape the marriage licence already handles through rule keys, so that part has a precedent to
   follow.

**SUPPLEMENT — the checklist is not the whole workflow.** PSA MC 2024-17 also carries the **10-day
posting** of the pending application and the registrar's evaluation, neither of which appears on
this card because the card is what the *client* is handed. The workflow still needs the posting
clock (§10 note above), not just these ten items.

**SUPPLEMENT — delayed registration is a workflow, not a longer checklist.** PSA rules provide
for: **posting a notice of the pending application for at least 10 days**; evaluation of documents
by the Civil Registrar; additional documentary requirements; and further investigation if an
opposition is filed.

> **Repo note — reuse, do not rebuild.** The marriage licence already implements a 10-day posting
> clock with a derived earliest-issue date, deferral reasons, a requirements table with
> attachments, and a registrar finding. Delayed birth registration is the same shape. Use the same
> mechanism rather than writing a second posting engine.
>
> Also: `births.is_delayed` exists but is **0 on every row** while `DATEDIFF` says 12 of 14 were
> registered over five years late (recorded 2026-09-07). The flag is not trustworthy and the
> analytics deliberately compute from dates instead. Whatever the delayed workflow does, it must
> set this flag honestly going forward.

---

## 11. Printable forms

**CONFIRMED.** CROMS must generate and print the office's actual **Marriage Application form** —
data entered in CROMS populating the printed form, so staff do not re-type it. A photo/copy of the
form is available. The same applies later to **Parental Consent** and **Parental Advice** once
those documents are in hand.

The office named **Crystal Reports** as the intended output.

> **Repo note — state this plainly to the office.** There are **no `.rpt` files in this project and
> none can be authored on this machine**: no Crystal designer is installed, and programmatic
> creation was measured impossible on the free CR-for-VS runtime (in-process RAS returns "Failed to
> connect to server %MACHINENAME%", 2026-09-06). What CROMS does instead already produces the
> result the office is asking for — the built-in **overlay renderer** draws the blank form and lays
> each value into its own measured box, which is what all three certificates print as today.
> `FormCatalog` + `CertificateReport` + `PrintCalibration` are the path; a `.rpt` dropped into
> `CROMS\Reports` later takes over automatically with no other change.
>
> **So a blank scan is NOT Crystal input.** It is the background bitmap the overlay renderer draws
> before laying values on top. Print preview already exists and already works — it is
> `CertificateReport.ShowOcrPreview` plus the built-in renderer, not a `CrystalReportViewer`. The
> Crystal plumbing (`CrystalRunner`, `CertificateViewerForm`) is wired and isolated so a PC without
> the runtime still runs CROMS, but **nothing in the app uses it today**, because there is no
> report to run.
>
> So for the three new forms the deliverable is: a **clean scan of each blank form**, a print map,
> and a `FormCatalog` entry. **The blank scan is the prerequisite** — the same one already
> outstanding for MF-102 (1993).

---

## 12. Three-stage staff workflow / flexible windows

**CONFIRMED.** Different staff handle different parts of one transaction:

1. **Receiving / Front desk** — accepts and receives the client's request
2. **Processor** — does the actual work
3. **Releasing** — hands the finished document to the client

The office wants this flexible, rather than CROMS assuming one employee receives, processes and
releases the same transaction. **General office requirement — affects multiple services.**

**PENDING:** exact staff permissions, whether a transaction can be transferred, resulting queue
behaviour, and whether one employee may hold several roles.

> **Repo note — this largely exists.** The transaction already carries one TXN ID through
> `ForPrint` / `ForPayment` / `ForRelease` / `Released` states; Release & Claim was rebuilt on
> 2026-09-09 as a state machine that re-reads status on action (another window may have moved the
> request); Queue Management has window claiming, forwarding and per-window routing. The work is
> **extending the existing status machine and role gates**, not inventing a workflow framework.

---

## 13. Documents and references owed by the office

These exist or were promised. **Each one unblocks specific work above — chase them as a batch.**

| Document | Unblocks | Status |
|---|---|---|
| Marriage Application form — blank MF-90 | §2 fields, §11 print map | ✅ **received 2026-09-13**, vector PDF, mapped in §2a |
| Parental Consent form — MF No. 06 | §2 §3 fields, §11 print map | ✅ **received 2026-09-13** (§3) |
| Parental Advice form — MF No. 68 | §2 §3 fields, §11 print map | ✅ **received 2026-09-13** (§3) |
| Late/delayed birth registration requirements | §10 workflow | ✅ **received 2026-09-13** (§10) |
| Complete list of office fees / payment transactions | §8 categories and amounts | ✅ **received 2026-09-13** (§8) |
| Blank MF-102 (2007) sheet | fixes the wrong blank noted below | ⚠️ **draft in hand 2026-09-13** — see below |

**All six now have at least a draft.**

### The MF-102 (2007) blank — usable, with two caveats

Supplied 2026-09-13 as a third-party (studocu) PDF, saved to `Docs/AgencyForms/`. Page 1 is a
**clean, unwatermarked, unfilled** MF-102 at **1275 × 2100 px** (150 DPI on 8.5 × 14 legal), and
its numbering confirms the revision: **22 Certification of Informant · 23 Prepared By · 24 Received
By · 25 Registered by the Civil Registrar** — the 2007 sequence, against the 20/21/22 of the sheet
currently in `Assets`.

**Bonus: page 2 is the BACK of the form**, which the office never mentioned and which matters
twice over — it carries the **Affidavit of Acknowledgment / Admission of Paternity** (the RA 9255
instrument, §7) and the **Affidavit for Delayed Registration of Birth** (§10). The delayed
affidavit has seven numbered clauses with real fields: whose birth (tick box, self or another),
who attended the birth and where they reside, citizenship, whether the parents were married (tick
box, with the father's name if acknowledged), **the reason for the delay**, and the applicant's
relationship to the registrant. That is the part of delayed registration the client-facing
checklist in §10 does not cover.

**Caveat 1 — it is NOT a drop-in replacement.** The existing hand-measured print map was measured
against the current asset in a **792 × 1224 pt** page (aspect 0.647); this sheet is **8.5 × 14
legal** (aspect 0.607). Different aspect, so every value would be displaced. Swapping the asset
means **re-measuring the print map**, the same job MF-97 and MF-103 got on 2026-09-07 — not a file
copy. Until that is done the asset stays as it is.

**Caveat 2 — it is not the office's own stock.** This is a scan found online, not a sheet from
Peñablanca's drawer. It is almost certainly identical (it is a national PSA form), but the whole
point of the replica is fidelity, so **the office should confirm it matches what they actually
issue** before it becomes the printed background on real certificates.

> **Repo note.** `CROMS\Assets\Form102Blank.png` is a **1993-numbered** sheet registered as the
> **2007** blank (found 2026-09-10). The overlay still lands correctly, but a 2007 certificate is
> being drawn on a 1993 form. Add it to the same request.

---

### Consent and Advice blanks — received 2026-09-13 as Word documents

`Blank_Consent_to_Marriage_of_a_Person_Underage.docx` and `Blank_Advice_Upon_Intended_Marriage.docx`
(Downloads). **Re-typed Word documents on Letter paper (8.5 × 11), not scans of the office's stock.**
The advice file holds both sheets, **(MALE)** and **(FEMALE)**, and carries "Municipal Form No. 68
(Form No. 6)"; the consent file does **not** print "Municipal Form No. 06" and leaves Municipality /
Province blank where the photographed original had Peñablanca, Cagayan pre-printed. Usable as the
background for the printouts; worth one look from the office that the re-typing matches their sheets.

---

## 14. Open questions to put to the office

Grouped as they should be asked. **Nothing here is to be implemented from a guess.**

*(Struck-through items were answered by the documents received 2026-09-13.)*

**Marriage**
- Exact supporting document for an applicant from another province — and what triggers it
- ~~Exact previous-marriage information for a widowed applicant~~ — answered, §4.2
- ~~Exact contents and signature blocks of the Parental Consent form~~ — answered, §3
- ~~Exact contents and signature blocks of the Parental Advice form~~ — answered, §3
- How the office records a marriage registration after receiving the Certificate of Marriage
- Which registry number, dates, signatures or details the staff add at registration
- The CROMS ↔ PhilCRIS division of responsibility
- **Does a 25-year-old need parental advice here?** (§3 — settles 21–24 vs 21–25). The consent
  side is now settled by the form's own "less than twenty-one" wording; only advice is open.
- Is a counselling certificate required for the whole 18–25 band in practice? Is CENOMAR required?
- Is any RA 10354 §15 family-planning certificate enforced locally?
- **New, from MF-90:** the form asks for **Degree of Relationship of Contracting Parties** (Arts.
  37–38). CROMS has no such field. Does the office want it captured, and does anything act on it?
- **New, from the advice form:** a deceased parent is annotated **"(DECEASED)"** in the signature
  block. Should CROMS record *why* a parent did not sign (deceased / absent / refused), or is the
  annotation written by hand?

**Death registration**
- Exactly what staff enter when accepting/registering a Death Certificate
- Whether CROMS needs more than document storage plus basic registration data
- Relationship between CROMS and the registration system the office already uses

**Case tracking** — the real internal stages for: Legitimation · Supplemental Report · Legal
Instruments · Court Order. *(The Filed → Endorsed to PSA → Released to Client example is not a
workflow yet.)*

**BREQS** — local turnaround · accepted valid IDs · local fees · internal statuses · how
submission to PSA is recorded · how receipt and release are recorded

**Fee collection** — ~~complete list of transaction purposes · fees~~ (received, §8) · but still:
**what is the "+ 30"** on certified copy and certification (documentary stamp? recorded as one
amount or two?) · **which fee does a petition take** — CCE-9048 is 1,000 while CFN-9048 is 3,000 and
CROMS has one `PET-9048` code · the burial permit and transfer-of-cadaver amounts, which the card
leaves blank · exemptions and special cases (Senior/PWD under RA 11261?) · exactly what the monthly
collection report must contain

**Delayed birth registration** — ~~review the office's own requirement copies~~ (received, §10) ·
but still: is item (c) really **"any two of the eight"** as the card reads, or are some of the eight
preferred/required? · is the **10-day posting** run by this office for delayed birth registration
the way it is for a marriage licence?

**Workflow** — staff permissions · can a transaction be transferred mid-flight · queue behaviour ·
may one employee hold several roles

---

## 15. Build order

Sequenced by: what already has foundations → what is unblocked → what is waiting on §13.

**Phase 1 — DONE 2026-09-12**
1. ~~Marriage Application: add **Sex** to applicant info~~ ✔
2. ~~**Place of birth**: audit every module for the single-textbox form; add **Country** and
   decide the non-PH behaviour (§1)~~ ✔

*(The 21–24 vs 21–25 band is a question for the office, not Phase 1 work — see §3.)*

**Phase 2 — chase documents — ✔ 5 of 6 DONE 2026-09-13**
4. ~~Collect everything in §13~~ — only the blank MF-102 (2007) is still owed. The blank MF-90 is
   preserved in the repo at `Docs/AgencyForms/`; the other three arrived as photographs and are
   transcribed into §3, §8 and §10 of this file.
5. Put §14 to the office as one list — **still to do**, and now shorter: the documents answered
   four of its questions and raised two new ones.

**Phase 3 — marriage fill-ins — 3A DONE 2026-09-13 (items 6-7); item 8 blocked; 9 next**
6. ~~Parent (father + mother, each with citizenship + residence) and the single
   "person who gave consent or advice" block (name + relationship + citizenship + residence)~~ ✔
   migration 38; three name cells per person
7. ~~Widowed / previously-married block: **how dissolved · place dissolved · date dissolved**~~ ✔
   greyed + cleared unless Widowed / Annulled / Divorced
   *Not done in 3A, flagged:* the consent/advice slot is NOT age-gated (it is open for a 30-year-old
   too) — the office said only that one slot is needed, not when it must be blank; and the printed
   licence (`LicensePrinter`) still shows only the joined father/mother names, not the new blocks.
8. Another-province attachment — reuse the `marriage_requirements` attachment path — **still
   blocked**, the document and its trigger are the one unanswered §14 marriage item
9. ~~Printed Marriage Application (MF-90)~~ ✔ **3B DONE 2026-09-13** — a real **Crystal report**
   (`CROMS/Reports/MF-90-1993.rpt`, generated from measured cells), previewed in the Crystal
   viewer with **Ctrl + mouse wheel** zoom, printable and exportable; identical fallback page on a
   PC without the Crystal runtime. Parental Consent (MF 06) and Advice (MF 68, **two sheets**) are
   next — but they arrived as PHOTOGRAPHS, so a clean blank scan of each is needed first
10. *(new)* Degree of Relationship of Contracting Parties — **ask first** (§14)

**Phase 4 — tracking case types**
10. Decide one generic case-tracking table vs four — *after* stages arrive
11. Legitimation · Supplemental Report · Legal Instruments · Court Order

**Phase 5 — fee collection — NOW MOSTLY UNBLOCKED**
12. **Correct the fee schedule first** (§8) — the seeded amounts are wrong and the cashier screen
    assesses from them today. Needs the "+ 30" and the CCE-vs-CFN answers before touching them.
13. Add the ten fee types the card lists and `fees` does not have
14. Standalone payment entry + purpose of transaction + payment log
15. Monthly collection report into Reports & Analytics

**Phase 6 — BREQS — DONE 2026-09-13**
14. ~~Kiosk request flow (document type, valid ID)~~ ✔ new "PSA Document" kiosk step
15. ~~Staff window (manage, submit to PSA, receive, release)~~ ✔ "PSA Copies (BREQS)" desk
16. ~~Turnaround and fee as **settings**~~ ✔ `BREQS_TURNAROUND_DAYS`, `BREQS_UNCLAIMED_DAYS`,
    `BREQS_FEE_PER_COPY` in app_settings. *Not done:* a claim date is not promised to the client
    (the turnaround is the office's estimate, not PSA's commitment), and CENOMAR is not offered
    (the office named birth, marriage and death only).

**Phase 7 — birth registration split — NOW UNBLOCKED**
19. Timely vs delayed branch; reuse the licence posting mechanism; set `is_delayed` honestly
20. The ten-item checklist from PSA MC 2024-17 (§10). **Design decision first:** item (c) is
    "any two of eight", which the current one-row-per-requirement model cannot express — it needs a
    group with a satisfy-count, or its own shape

**Phase 8 — three-stage workflow**
18. Extend the existing status machine and role gates — after permissions are confirmed (§12)

**Phase 9 — confirm-only, no code**
19. Everything remaining in §14

---

## 16. Standing rules for this backlog

- A **PENDING** item is not a licence to design something provisional and "fix it later". A status
  list, a fee, or a required document invented here reaches a government record and is then
  indistinguishable from a fact the office stated.
- Prefer **extending** what this codebase already proved — GeoLookup, the posting clock, the
  requirements table, the status machine, FormCatalog/overlay printing — over new parallel
  machinery. Nearly every item above has an existing analogue.
- When the office's practice and the statute disagree, **record both and ask**. Do not silently
  implement either.
- Update this file when an item is answered or delivered, and note it in the CLAUDE.md progress log.
