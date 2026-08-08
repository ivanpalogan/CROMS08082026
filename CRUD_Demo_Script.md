# CROMS — CRUD Demo Script
**For:** Capstone defense demo — Create / Update / Delete on the three registry forms
**Forms covered:** Birth Registration (Form 102), Death Registration (Form 103), Marriage Registration (Form 97)

---

## Before you start (setup)

1. In Visual Studio, **rebuild** the solution (`Ctrl+Shift+B`), then **run** (`F5`).
2. At the Sign In screen, log in with your admin account.
3. **Maximize the window.** The forms are wide — maximizing avoids the side controls being cut off. (Note: forms now scroll if the window is small, so nothing is unreachable.)
4. Have this sample data ready so you don't pause to think during the demo.

**One-line opening you can say:**
> "CROMS keeps a single registry for each vital event. I'll show the full record lifecycle — creating a record, updating it, and deleting it — on Birth, then Death, then Marriage."

---

## 1) BIRTH REGISTRATION (Municipal Form 102)

Open **Birth Registration** from the left sidebar. The entry tabs are on top (Child, Mother, Father, …); the **Recent Birth Registrations** list is at the bottom.

### CREATE
*Say: "To register a live birth, I fill the child's core details and submit it for approval."*
1. On the **Child** tab, enter:
   - First Name: `Juan`
   - Last Name: `Dela Cruz`
   - Sex: `Male`
   - (Optional, to look thorough) Date of Birth, Type of Birth, Weight.
2. Click **Submit for Approval**.
3. A confirmation appears ("Submitted for approval"), the form clears, and the new record shows at the **top of the list** with an auto-generated **Registry No** (format `YYYY-B-####`) and status **Pending Approval**.

*Point out: "The system assigns the official registry number automatically — staff never type it, so there are no duplicates."*

> Tip: **Save Draft** instead of Submit if you want to show that a draft is saved **without** a registry number (drafts aren't official yet).

### UPDATE
*Say: "If there's a correction, staff open the record and edit it."*
1. In the **Recent Birth Registrations** list, **click the row** you just created — its details load back into the tabs.
2. Change something visible, e.g. Middle Name → `Santos`, or Weight → `3200`.
3. Click **Update**.
4. Confirmation appears; the list reflects the change.

### DELETE
*Say: "And an erroneous record can be removed, with a confirmation guard."*
1. **Click the row** in the list to select/load it.
2. Click **Delete**.
3. Confirm **Yes** on the prompt. The row disappears from the list.

> Reset for a clean screen anytime with the **New** button.

---

## 2) DEATH REGISTRATION (Municipal Form 103)

Open **Death Registration**. Left side = **Deceased Information**; right side = **Cause of Death / Disposal**; bottom = **Recent Death Registrations**.

### CREATE
1. In **Full Name**, enter: `Pedro Reyes`
   - (Optional) Sex, Age at Death, Date of Death, Immediate Cause: `Cardiac arrest`.
2. Click **Register Death**.
3. Confirmation appears; the record shows in the list with an auto **Registry No** (`YYYY-D-####`) and status **Registered**.

### UPDATE
1. **Click the row** for `Pedro Reyes` in the list — it loads into the form.
2. Change a field, e.g. Age → `72`, or Disposal Method → `Burial`.
3. Click **Update**. The list updates.

### DELETE
1. **Click the row** to select it.
2. Click **Delete** → confirm **Yes**. The row is removed.

---

## 3) MARRIAGE REGISTRATION (Municipal Form 97)

Open **Marriage Registration**. This is an overview screen (KPI cards + a **Marriage Records** list). Data entry happens in a pop-up dialog.

### CREATE
*Say: "Marriage uses a dedicated Form 97 entry dialog for the couple."*
1. Click **+ Register Marriage** (top of the screen). A dialog opens.
2. Fill the **Husband** group: First `Jose`, Last `Ramos`.
3. Fill the **Wife** group: First `Maria`, Last `Santos`.
4. (Optional) Solemnizer, Place of Marriage (Church / Municipality / Province), dates.
5. Click **Create** (or **Save**) in the dialog.
6. The dialog closes; the couple appears in the **Marriage Records** list with an auto **Registry No** (`YYYY-M-####`).

### UPDATE
1. **Click the couple's row** in the Marriage Records list — the dialog reopens **pre-loaded** with their data.
2. Change a field, e.g. Solemnizer → `Rev. Fr. Cruz`, or Wife civil status.
3. Click **Update** in the dialog. The list refreshes.

### DELETE
1. **Click the row** to reopen the dialog.
2. Click **Delete** → confirm. The record is removed and the list refreshes.

---

## Closing line
> "Every create, update, and delete is written to the **audit trail** under the signed-in user, and each record carries one official registry number — so the office has a complete, accountable history of every birth, death, and marriage."

---

## Quick reference — buttons per form

| Form | Create | Update | Delete | Clear |
|------|--------|--------|--------|-------|
| Birth | Submit for Approval / Save Draft | Update (after clicking a row) | Delete (after selecting a row) | New |
| Death | Register Death | Update (after clicking a row) | Delete (after selecting a row) | New |
| Marriage | + Register Marriage → Create | Click row → Update (in dialog) | Click row → Delete (in dialog) | Cancel/close dialog |

## If a button seems unresponsive during the demo
- Make sure the window is **maximized** (wide forms can push side buttons off-screen on a small window; scrolling also reveals them).
- For Create on Birth, the child's **First name, Last name, and Sex are required** — if missing, a "Missing data" prompt appears and jumps you to the Child tab.
