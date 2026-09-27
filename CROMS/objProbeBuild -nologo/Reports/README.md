# CROMS — Crystal Reports layouts

Drop a Crystal Reports `.rpt` file in this folder and CROMS will use it to print that
certificate. Until a form has one, CROMS prints the certificate itself and nothing
breaks — so this folder can stay empty, and can be filled in one form at a time.

This folder is copied next to `CROMS.exe` on build, so at runtime the app looks in
`<folder containing CROMS.exe>\Reports\`.

---

## 1. File name

The file name **must** be the form's Form Code plus `.rpt`:

| Form | File name |
|---|---|
| Certificate of Live Birth, Municipal Form 102 (Revised January 2007) | `MF-102-2007.rpt` |
| Certificate of Live Birth, Municipal Form 102 (Revised January 1993) | `MF-102-1993.rpt` |
| Certificate of Marriage, Municipal Form 97 (Revised January 1993) | `MF-97-1993.rpt` |
| Certificate of Death, Municipal Form 103 (Revised January 2016) | `MF-103-2016.rpt` |

The authoritative list is in **Settings → Certificates & Forms**, which shows every
form's Form Code, its datasource, and whether its report has been found. Check there
after adding a file — that screen is how you confirm CROMS picked it up.

Nothing else needs to be edited to add a report. The name is the wiring.

---

## 2. Datasource — bind to the VIEW, not the table

Each form has one database view that returns **exactly one row** with every field on
it. In the Crystal designer: *Database Expert → Create New Connection → OLE DB or
ODBC → MySQL → the `croms` database → Views*, and pick the one view for your form:

| Form Code | View |
|---|---|
| `MF-102-2007`, `MF-102-1993` | `v_birth_certificate` |
| `MF-97-1993` | `v_marriage_certificate` |
| `MF-103-2016` | `v_death_certificate` |

**Add only that one view. Do not add a second table and do not create links.**

Why the view and not the `births` / `marriages` / `deaths` table:

- `marriages` and `deaths` store most values as lookup **ids** (`husband_religion_id`,
  `cause_of_death_id`, …). A report bound to the table would print numbers. The view
  resolves every one of them to its name.
- The view adds the **form identity** (`form_name`, `form_code`, `municipal_form_no`)
  and the **office profile** (`office_name`, `office_municipality`,
  `office_province`, `registrar_name`, `registrar_title`), so the header and the
  signature block need no hardcoded text.
- The view keeps the report working when a base column is renamed.
- The view deliberately excludes the scanned-image blobs. A one-row certificate does
  not need the 2 MB source scan, and pulling it through the datasource is what makes
  Crystal slow.

**At design time the report will show every row in the view. That is expected.** At
run time CROMS does not let the report query the database at all: it passes in a
single pre-filtered row. So do **not** add a record-selection formula, and do not
save any database credentials into the report.

---

## 3. Logo and stamp

They arrive as two extra **byte[] columns on the datasource**, not as picture files:

| Column | What it is |
|---|---|
| `logo_image` | the office seal, for the header area |
| `stamp_image` | the stamp, for the position the form reserves for it |

In the designer these appear in Field Explorer as **Blob** fields. Drag each one onto
the canvas and size it; Crystal renders it as a picture.

They are two separate columns on purpose, so a report can show one without the other,
and each can be suppressed on its own (right-click → Format Graphic → Suppress, with
a formula such as `IsNull({v_birth_certificate.stamp_image})`).

These columns exist only at run time and will be **empty in the designer** unless the
office has already uploaded a logo or stamp. Upload them first, in
**Settings → Certificates & Forms → Manage Logo, Stamp and Office Details**, and they
will show up while you design.

A logo or stamp can also be captured straight off a scan in Intelligent Document
Processing, using the *Seals & Signatures* button.

---

## 4. Optional parameters

If a report declares a parameter with one of these names, CROMS fills it in. A report
that declares none works fine — this is a convenience, not a requirement. Every one of
them is also available as a datasource field, so use whichever suits the layout.

`FormName`, `FormCode`, `FormType`, `MunicipalFormNo`, `Revision`,
`RegistryNo`, `BookVolume`, `BookPage`,
`OfficeName`, `Municipality`, `Province`, `RegistrarName`, `RegistrarTitle`,
`PrintedBy`, `PrintedAt`

All are **String** parameters. Any other parameter the report declares is set to an
empty string rather than left unset — an unset parameter makes Crystal stop and prompt
the operator mid-print, which is not something a front-desk clerk can answer.

---

## 5. What the layout should contain

The report is meant to reproduce the paper certificate. The section order, the item
numbers and the labels CROMS already uses are defined per form in
`CROMS/Data/FormCatalog.cs` (`BirthReport()`, `MarriageReport()`, `DeathReport()`),
and the built-in printer follows them — so if you match those sections your report and
the fallback print stay consistent with each other.

Required by the office, in every form's report:

- **Form Name / Form Type** and the **Municipal Form No.** and revision
- **Registry Number** (plus Book / Volume and Page where the form prints them)
- every applicable certificate field
- the registering office and the registrar's signature block
- the **logo**, in the header area
- the **stamp**, where the form reserves for it, suppressed when absent

For a certificate meant to look exactly like the printed sheet, set the report's paper
size to the real form (Municipal Form 102 is 11 × 17 in) and place the fields on the
boxes.

---

## 6. Prerequisite on each PC

The **SAP Crystal Reports runtime for .NET Framework** must be installed on any PC
that will render a `.rpt`. CROMS targets 13.0.4000.0. A PC without it still prints
every certificate — CROMS falls back to its own renderer and says so in
Settings → Certificates & Forms.

The runtime is only needed to *render*. Authoring a `.rpt` needs the Crystal Reports
designer (the SAP Crystal Reports for Visual Studio extension, or Crystal Reports
2011+), which is a separate install and is **not** required on the office PCs.

---

## 7. Checklist before handing a report over

- [ ] File name is exactly `<FormCode>.rpt`
- [ ] Bound to the one correct view, no extra tables, no links
- [ ] No record-selection formula, no saved database credentials
- [ ] `logo_image` and `stamp_image` placed, each suppressed when null
- [ ] Form name, Municipal Form No., revision and Registry No. all appear
- [ ] Paper size matches the real form
- [ ] Opened once from CROMS on a PC with the runtime, and printed

---

## 8. `MF-90-1993.rpt` — Application for Marriage License (GENERATED, not hand-authored)

This one is different from the certificate reports above, and the difference matters
before anyone edits it:

- **It is generated by code.** `CROMS.ReportGen` opens the blank `CrystalReport.rpt`
  that Crystal Reports for Visual Studio ships as its item template, adds the datasource,
  the 8.5 × 13 in page, the blank Form 90 as an embedded picture and one field per
  printed box, and saves it here. Regenerate with:

  ```
  msbuild CROMS.ReportGen\CROMS.ReportGen.csproj
  CROMS\bin\Debug\CROMS.ReportGen.exe
  ```

- **The positions live in `CROMS/Data/Mf90Form.cs` (`Cells`)**, measured from the office's
  vector blank. The same list is what CROMS draws on a PC without the Crystal runtime, so
  the Crystal print and the fallback print are the same page.
  **Change a position there and regenerate** — an edit made only in the Crystal designer
  is overwritten the next time the generator runs, and the fallback would no longer match.

- **Datasource: an ADO.NET table `mf90_application`, not a database view.** CROMS builds the
  one row itself, already split into the form's boxes (Day / Month / Year, city / province,
  first / middle / last), so the report only places text. There is nothing to log into.

- **No logo, stamp or parameters.** An application form is filled in and signed by the
  applicants; it is not an issued certificate.

- **Registry No. is left blank** on purpose: it is the office's register number for the
  application, and CROMS's own application number is not necessarily that.

The window: Marriage License Application → **Print application (MF-90)**. Zoom with
**Ctrl + mouse wheel** (or Ctrl + / Ctrl − / Ctrl 0); Print and Export are on the toolbar.
