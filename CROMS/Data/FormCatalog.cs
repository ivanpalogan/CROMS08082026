using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// Where one value is drawn on the printed form, and which part of which column
    /// supplies it.
    /// <para/>
    /// <see cref="Column"/> names a column of the form's REPORT VIEW, not of the base
    /// table. That is deliberate: the view already resolves lookup ids to names and adds
    /// the office profile, so the overlay printer and a Crystal .rpt bound to the same
    /// view draw the same value from the same column. Rename a base column and only the
    /// view changes.
    /// <para/>
    /// <see cref="Part"/> splits a combined column across the several boxes the paper form
    /// gives it: place of birth is one column but three printed boxes (hospital, city,
    /// province), so the three cells carry Part 0, 1 and 2 of the comma-separated value.
    /// -1 means the whole value.
    /// </summary>
    public class PrintCell
    {
        public string Column;
        public PointF At;            // 0-1 of the page
        public float FontSize = 7.5f;
        public int Part = -1;        // index into the comma-split, -1 = whole value
        public bool IsDate;          // render as "dd MMMM yyyy"

        public PrintCell(string column, float x, float y, float size = 7.5f,
                         int part = -1, bool isDate = false)
        { Column = column; At = new PointF(x, y); FontSize = size; Part = part; IsDate = isDate; }
    }

    /// <summary>
    /// A tick box: when <see cref="Column"/> reads <see cref="WhenValue"/>, an X is drawn
    /// at <see cref="At"/>. <see cref="WhenValue"/> of "*" is the catch-all for a value
    /// that matched none of the listed options ("Others, specify").
    /// </summary>
    public class PrintMark
    {
        public string Column;
        public string WhenValue;
        public PointF At;

        public PrintMark(string column, string whenValue, float x, float y)
        { Column = column; WhenValue = whenValue; At = new PointF(x, y); }
    }

    /// <summary>
    /// One numbered section of the paper certificate, in the order it is printed, with
    /// the fields it contains. This is what keeps the DIGITAL form in the shape of the
    /// original: the review grid and the structured print both walk the sections in this
    /// order rather than inventing their own grouping.
    /// </summary>
    public class FormSection
    {
        public string Title;
        public string[] Keys;        // canonical extraction keys, in printed order

        public FormSection(string title, params string[] keys) { Title = title; Keys = keys; }
    }

    /// <summary>One printed entry of the report: a column of the form's report view and
    /// the label the paper form prints beside it.</summary>
    public class ReportField
    {
        public string Column;
        public string Label;
        public bool IsDate;

        public ReportField(string column, string label, bool isDate = false)
        { Column = column; Label = label; IsDate = isDate; }
    }

    /// <summary>
    /// A section of the printed certificate expressed in REPORT VIEW columns — the same
    /// sections as <see cref="FormSection"/>, but named the way the report datasource
    /// names them.
    /// <para/>
    /// The two exist because they answer two different questions. <see cref="FormSection"/>
    /// groups the fields OCR extracted, so it is keyed by extraction key and only covers
    /// what a scan can produce. This groups what the RECORD holds, so it is keyed by view
    /// column and also covers everything typed in later — the informant block, the
    /// attendant's certification, prepared/received by. A Crystal .rpt bound to the view
    /// should follow these sections and labels; so does the built-in structured printer,
    /// which is what keeps the two renderings consistent.
    /// </summary>
    public class ReportSection
    {
        public string Title;
        public List<ReportField> Fields = new List<ReportField>();

        public ReportSection(string title, params ReportField[] fields)
        { Title = title; Fields.AddRange(fields); }
    }

    /// <summary>
    /// Everything CROMS knows about one certificate form: how it identifies itself, which
    /// registry table and report view hold it, the order and grouping of its sections, how
    /// each extracted field maps to a stored column, and where every value is printed.
    /// <para/>
    /// Adding a form is one entry here. Nothing downstream is hardcoded to a particular
    /// certificate: the OCR review grid, the record write, the logo/stamp placement, the
    /// Crystal report lookup and the fallback printer all read this object.
    /// </summary>
    public class FormDefinition
    {
        // ---- identity, as printed on the paper and stored on the record ----
        public string FormCode;          // machine key, e.g. "MF-102-2007"
        public string FormName;          // "Certificate of Live Birth"
        public DocKind FormType;         // Birth / Marriage / Death
        public string MunicipalFormNo;   // "102"
        public string Revision;          // "Revised January 2007"

        /// <summary>The <see cref="FormLayout.Code"/> this definition reads its field
        /// regions, labels and printed order from — no field list is duplicated here.</summary>
        public string LayoutCode;

        // ---- where the data lives ----
        public string RecordTable;       // "births"
        public string ReportView;        // "v_birth_certificate"
        public string RegistryColumn = "registry_no";

        // ---- how it is rendered ----
        /// <summary>Crystal report file, resolved under the app's Reports folder.</summary>
        public string RptFile;
        /// <summary>Scan of the BLANK form, resolved under Assets. Null when the office
        /// has not supplied one — the form then prints in structured layout instead.</summary>
        public string BlankAsset;
        /// <summary>Page size in points the print map was measured against.</summary>
        public SizeF PrintPage = new SizeF(792f, 1224f);   // 11 x 17 in

        /// <summary>Logo position, 0-1 of the page. Empty when the form prints its own.</summary>
        public RectangleF LogoRect = RectangleF.Empty;
        /// <summary>Stamp position, 0-1 of the page. Kept separate from the logo so the
        /// two are managed and placed independently.</summary>
        public RectangleF StampRect = RectangleF.Empty;

        public List<FormSection> Sections = new List<FormSection>();
        /// <summary>The printed structure in report-view columns. Drives the structured
        /// renderer and tells a .rpt author what to lay out, in what order.</summary>
        public List<ReportSection> ReportSections = new List<ReportSection>();
        public List<PrintCell> Cells = new List<PrintCell>();
        public List<PrintMark> Marks = new List<PrintMark>();

        /// <summary>Extraction key to stored column, for the fields the record keeps.</summary>
        public Dictionary<string, string> Columns =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// True when this form knows WHERE each value sits on the page, so it can be
        /// printed in position rather than as a listing. That is what makes a replica
        /// possible; whether the replica also draws the form ARTWORK depends on
        /// <see cref="HasBlankForm"/>. Without a blank scan the values still land in their
        /// real boxes, which is what printing onto official pre-printed stock needs.
        /// </summary>
        public bool HasOverlay => Cells.Count > 0;

        /// <summary>
        /// True when a scan of the BLANK sheet is on file, so the printed page can carry
        /// the form's own artwork instead of expecting pre-printed paper.
        /// </summary>
        public bool HasBlankForm => !string.IsNullOrEmpty(BlankAsset);

        /// <summary>The OCR layout this form reads by, or null if it is not on file.</summary>
        public FormLayout Layout =>
            DocLayouts.All.FirstOrDefault(l => l.Code == LayoutCode);

        public FieldSpec Spec(string key)
        {
            FormLayout l = Layout;
            return l?.Fields.FirstOrDefault(f =>
                string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The label the paper form prints for this field.</summary>
        public string LabelFor(string key)
        {
            FieldSpec s = Spec(key);
            if (s != null) return s.Label;
            // Derived fields (DateOfBirth from three cells, FullName from three) have no
            // region of their own but are still shown and stored.
            switch (key)
            {
                case "DateOfBirth": return "Date of Birth";
                case "PlaceOfBirth": return "Place of Birth";
                case "FullName": return "Full Name";
                case "Religion": return "Religion";
                case "Nationality": return "Citizenship";
                case "Informant": return "Informant";
                default: return key;
            }
        }

        /// <summary>
        /// Every field of this form in the order the paper prints it, grouped by section.
        /// Keys named in a section but absent from the layout are dropped, so one section
        /// list can be shared by two revisions that differ by a row.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string[]>> OrderedSections(
            IEnumerable<string> availableKeys)
        {
            var have = new HashSet<string>(availableKeys ?? Enumerable.Empty<string>(),
                                           StringComparer.OrdinalIgnoreCase);
            foreach (FormSection s in Sections)
            {
                string[] keys = s.Keys.Where(have.Contains).ToArray();
                if (keys.Length > 0)
                    yield return new KeyValuePair<string, string[]>(s.Title, keys);
            }
            // Anything the layout produced that no section claims still has to be shown —
            // a field is never silently hidden from the operator.
            var claimed = new HashSet<string>(Sections.SelectMany(s => s.Keys),
                                              StringComparer.OrdinalIgnoreCase);
            string[] rest = have.Where(k => !claimed.Contains(k)).ToArray();
            if (rest.Length > 0)
                yield return new KeyValuePair<string, string[]>("Other Entries", rest);
        }

        public override string ToString() => FormName + " (Municipal Form No. " +
                                             MunicipalFormNo + ", " + Revision + ")";
    }

    /// <summary>
    /// The form library. One entry per certificate revision the office handles; every
    /// screen and every report resolves its behaviour from here.
    /// <para/>
    /// The intended flow is
    /// <c>scan → OCR → identify form → extract fields → digital form → database →
    /// report → print/export</c>, and the "identify form" step is a lookup in this
    /// catalog: <see cref="ByLayoutCode"/> turns what the recogniser matched into the
    /// stored <see cref="FormDefinition.FormCode"/>, and <see cref="ByCode"/> turns a
    /// stored code back into everything needed to render the record.
    /// </summary>
    public static class FormCatalog
    {
        private static List<FormDefinition> _all;

        public static List<FormDefinition> All => _all ?? (_all = Build());

        /// <summary>
        /// The report-view COLUMN an extraction key prints into, or null when the form does
        /// not print that key. One map, used by both the OCR preview and the print map, so
        /// the two can never disagree about where a value belongs.
        /// <para/>
        /// Two steps, because two different things are being reconciled. First the keys the
        /// record does not store as a plain column (they resolve to a lookup id or a joined
        /// table on save) but the VIEW still exposes. Then the form's own key-to-column map,
        /// aliased where the view has to disambiguate the three certificates that all store
        /// a "first_name".
        /// </summary>
        public static string ViewColumn(FormDefinition def, string key)
        {
            if (def == null || string.IsNullOrEmpty(key)) return null;

            string direct = ViewOnlyColumn(def.FormType, key);
            if (direct != null) return direct;

            string stored;
            return def.Columns.TryGetValue(key, out stored)
                ? ViewAlias(def.FormType, stored) : null;
        }

        private static string ViewOnlyColumn(DocKind kind, string key)
        {
            if (string.Equals(key, "RegistryNo", StringComparison.OrdinalIgnoreCase))
                return "registry_no";
            if (string.Equals(key, "Province", StringComparison.OrdinalIgnoreCase))
                return "office_province";
            if (string.Equals(key, "CityMunicipality", StringComparison.OrdinalIgnoreCase))
                return "office_municipality";

            if (kind == DocKind.Marriage)
            {
                switch (key)
                {
                    case "HusbandPlaceOfBirth":    return "husband_place_of_birth";
                    case "HusbandCitizenship":     return "husband_citizenship";
                    case "HusbandResidence":       return "husband_residence";
                    case "HusbandReligion":        return "husband_religion";
                    case "HusbandFatherName":      return "husband_father_name";
                    case "HusbandMotherName":      return "husband_mother_name";
                    case "WifePlaceOfBirth":       return "wife_place_of_birth";
                    case "WifeCitizenship":        return "wife_citizenship";
                    case "WifeResidence":          return "wife_residence";
                    case "WifeReligion":           return "wife_religion";
                    case "WifeFatherName":         return "wife_father_name";
                    case "WifeMotherName":         return "wife_mother_name";
                    case "PlaceOfMarriage":        return "church_name";
                    case "PlaceOfMarriageAddress": return "place_municipality";
                    case "TimeOfMarriage":         return "time_of_marriage";
                }
            }
            if (kind == DocKind.Death)
            {
                switch (key)
                {
                    case "FatherName":       return "father_name";
                    case "MotherMaidenName": return "mother_name";
                    case "CauseOfDeath":     return "immediate_cause";
                    case "CorpseDisposal":   return "disposal_method";
                    case "Residence":        return "residence";
                    case "Occupation":       return "occupation";
                }
            }
            return null;
        }

        private static string ViewAlias(DocKind kind, string stored)
        {
            if (kind == DocKind.Birth)
            {
                if (stored == "first_name") return "child_first_name";
                if (stored == "middle_name") return "child_middle_name";
                if (stored == "last_name") return "child_last_name";
            }
            if (kind == DocKind.Death)
            {
                if (stored == "first_name") return "deceased_first_name";
                if (stored == "middle_name") return "deceased_middle_name";
                if (stored == "last_name") return "deceased_last_name";
                if (stored == "religion_name") return "religion";
            }
            return stored;
        }

        /// <summary>
        /// Builds the print map from the form's OCR LAYOUT, which already states where every
        /// field sits as a 0-1 page rectangle. The coordinates that tell CROMS where to READ
        /// a value are the same coordinates that say where to PRINT it, so a form that can be
        /// read box by box can be printed box by box without a second set of measurements.
        /// <para/>
        /// Used for the forms whose positions were never hand-measured against a blank scan
        /// (MF-97, MF-103, and the 1993 MF-102). Birth 2007 keeps its hand map: it was
        /// measured against Form102Blank.png, a different coordinate space from the layout.
        /// <para/>
        /// A tick-box field prints its VALUE inside the box rather than an X, because the
        /// layout records where the answer is read from, not where each individual choice
        /// box sits. Writing "Male" on the sex line is right; guessing at a tick position
        /// would put a mark in the wrong box.
        /// </summary>
        private static void PrintMapFromLayout(FormDefinition d)
        {
            FormLayout layout = d.Layout;
            if (layout == null) return;

            foreach (FieldSpec spec in layout.Fields)
            {
                if (spec.Rect.Width <= 0 || spec.Rect.Height <= 0) continue;

                string column = ViewColumn(d, spec.Key);
                if (column == null) continue;

                // The band the value was read from is also the band it prints in. Nudged
                // down slightly so the glyphs sit inside the ruled line rather than on it.
                float x = spec.Rect.X;
                float y = spec.Rect.Y + spec.Rect.Height * 0.12f;

                // Size the type to the row it has to fit, clamped to what stays legible.
                float pt = spec.Rect.Height * d.PrintPage.Height * 0.62f;
                if (pt < 6f) pt = 6f;
                if (pt > 9.5f) pt = 9.5f;

                d.Cells.Add(new PrintCell(column, x, y, pt, -1,
                                          spec.Shape == FieldShape.Date));
            }
        }

        public static FormDefinition ByCode(string formCode)
        {
            if (string.IsNullOrWhiteSpace(formCode)) return null;
            return All.FirstOrDefault(f =>
                string.Equals(f.FormCode, formCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Resolve what the recogniser matched (a <see cref="FormLayout.Code"/>)
        /// to the stored form definition.</summary>
        public static FormDefinition ByLayoutCode(string layoutCode)
        {
            if (string.IsNullOrWhiteSpace(layoutCode)) return null;
            return All.FirstOrDefault(f =>
                string.Equals(f.LayoutCode, layoutCode, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<FormDefinition> ForKind(DocKind kind) =>
            All.Where(f => f.FormType == kind);

        /// <summary>
        /// The revision the office issues TODAY for this kind of certificate. Used when a
        /// record is created by hand (no scan to identify), and as the fallback when a
        /// scan's own layout was refused — the record still gets a form identity, because
        /// a certificate with no form name cannot be reprinted correctly later.
        /// </summary>
        public static FormDefinition Current(DocKind kind)
        {
            switch (kind)
            {
                case DocKind.Birth: return ByCode("MF-102-2007");
                case DocKind.Marriage: return ByCode("MF-97-1993");
                case DocKind.Death: return ByCode("MF-103-2016");
                default: return null;
            }
        }

        /// <summary>
        /// Identify the form from what the recogniser produced: the exact layout when it
        /// was trusted, otherwise the current revision of that KIND. The distinction is
        /// preserved by the caller — a record read through a refused layout is stored with
        /// the kind's current form name but is flagged in the batch log, so an auditor can
        /// tell "read box by box off this revision" from "read by labels, filed under the
        /// revision we use".
        /// </summary>
        public static FormDefinition Identify(DocAiResult r)
        {
            if (r == null) return null;
            FormDefinition exact = r.LayoutRejected == null ? ByLayoutCode(r.LayoutCode) : null;
            return exact ?? Current(r.Kind);
        }

        public static string ViewFor(DocKind kind) => Current(kind)?.ReportView;

        // ===================================================================
        // The library
        // ===================================================================

        private static List<FormDefinition> Build()
        {
            return new List<FormDefinition> { Birth2007(), Birth1993(), Marriage1993(), Death2016() };
        }

        // ---- shared section shapes -------------------------------------------------
        // Two revisions of the same certificate print the same sections in the same
        // order; they differ in which rows exist and where they sit. Section lists are
        // therefore shared and filtered per revision by OrderedSections().

        private static List<FormSection> BirthSections() => new List<FormSection>
        {
            new FormSection("Form Identification",
                "RegistryNo", "Province", "CityMunicipality"),
            new FormSection("1-5. Child",
                "ChildFirst", "ChildMiddle", "ChildLast", "Sex",
                "DateOfBirth", "DobDay", "DobMonth", "DobYear", "TimeOfBirth",
                "PlaceOfBirth", "PlaceHospital", "PlaceMunicipality", "PlaceProvince",
                "TypeOfBirth", "BirthOrder", "Weight"),
            new FormSection("6-12. Mother",
                "MotherFirst", "MotherMiddle", "MotherLast",
                "MotherCitizenship", "MotherReligion",
                "ChildrenBornAlive", "ChildrenLiving", "ChildrenDead",
                "MotherOccupation", "MotherAge",
                "MotherResidence", "MotherResidenceCity", "MotherResidenceProvince"),
            new FormSection("13-17. Father",
                "FatherFirst", "FatherMiddle", "FatherLast",
                "FatherCitizenship", "FatherReligion",
                "FatherOccupation", "FatherAge", "FatherResidence"),
            new FormSection("18. Marriage of Parents",
                "MarriageOfParents", "MarriageOfParentsDate", "MarriageOfParentsPlace"),
            new FormSection("19/21a. Attendant at Birth",
                "Attendant"),
            new FormSection("19b/21b. Certification of Attendant",
                "AttendantName", "AttendantTitle", "AttendantAddress", "AttendantDate"),
            new FormSection("20/22. Certification of Informant",
                "Informant", "InformantRelationship", "InformantAddress", "InformantDate"),
            new FormSection("21/23. Prepared By",
                "PreparedByName", "PreparedByTitle", "PreparedByDate"),
            new FormSection("22/24. Received at the Office of the Civil Registrar",
                "ReceivedByName", "ReceivedByTitle", "ReceivedByDate"),
            new FormSection("25. Registered at the Civil Registrar",
                "RegisteredByName", "RegisteredByTitle", "RegisteredByDate"),
            new FormSection("Read from the whole page",
                "Religion", "Nationality"),
        };

        private static List<FormSection> MarriageSections() => new List<FormSection>
        {
            new FormSection("Form Identification",
                "RegistryNo", "Province", "CityMunicipality"),
            new FormSection("1-8. Husband",
                "HusbandFirst", "HusbandMiddle", "HusbandLast",
                "HusbandDateOfBirth", "HusbandAge", "HusbandPlaceOfBirth", "HusbandSex",
                "HusbandCitizenship", "HusbandResidence", "HusbandReligion",
                "HusbandCivilStatus"),
            new FormSection("1-8. Wife",
                "WifeFirst", "WifeMiddle", "WifeLast",
                "WifeDateOfBirth", "WifeAge", "WifePlaceOfBirth", "WifeSex",
                "WifeCitizenship", "WifeResidence", "WifeReligion", "WifeCivilStatus"),
            new FormSection("9-12. Parents of the Contracting Parties",
                "HusbandFatherName", "HusbandMotherName",
                "WifeFatherName", "WifeMotherName"),
            new FormSection("13. Marriage Licence",
                "LicenseNo", "LicenseDate"),
            new FormSection("14-16. Place and Date of Marriage",
                "PlaceOfMarriage", "PlaceOfMarriageAddress", "DateOfMarriage"),
            new FormSection("17. Solemnizing Officer",
                "Solemnizer", "SolemnizerPosition", "Nationality"),
            new FormSection("18. Witnesses",
                "Witness1", "Witness2"),
            new FormSection("Received at the Office of the Civil Registrar",
                "ReceivedByName", "ReceivedByTitle", "ReceivedByDate"),
        };

        // ---- the printed structure, in report-view columns ------------------------
        // Order and labels follow the paper certificate item by item. A Crystal .rpt
        // bound to the matching view should reproduce this; the built-in structured
        // printer already does.

        private static List<ReportSection> BirthReport() => new List<ReportSection>
        {
            new ReportSection("Registry",
                new ReportField("registry_no",   "Registry No."),
                new ReportField("book_volume",   "Book / Volume"),
                new ReportField("book_page",     "Page No."),
                new ReportField("office_province",     "Province"),
                new ReportField("office_municipality", "City / Municipality")),
            new ReportSection("1-5. Child",
                new ReportField("child_first_name",  "1. First Name"),
                new ReportField("child_middle_name", "    Middle Name"),
                new ReportField("child_last_name",   "    Last Name"),
                new ReportField("sex",               "2. Sex"),
                new ReportField("date_of_birth",     "3. Date of Birth", true),
                new ReportField("time_of_birth",     "    Time of Birth"),
                new ReportField("place_of_birth",    "4. Place of Birth"),
                new ReportField("birth_country",     "    Country of Birth"),
                new ReportField("type_of_birth",     "5a. Type of Birth"),
                new ReportField("birth_order",       "5c. Birth Order"),
                new ReportField("weight_grams",      "5d. Weight at Birth (grams)")),
            new ReportSection("6-12. Mother",
                new ReportField("mother_first_name",  "6. First Name"),
                new ReportField("mother_middle_name", "    Middle Name"),
                new ReportField("mother_last_name",   "    Maiden Surname"),
                new ReportField("mother_citizenship", "7. Citizenship"),
                new ReportField("mother_religion",    "8. Religion"),
                new ReportField("mother_children_born_alive", "9a. Children Born Alive"),
                new ReportField("mother_children_living",     "9b. Children Still Living"),
                new ReportField("mother_children_dead",       "9c. Children Now Dead"),
                new ReportField("mother_occupation",  "10. Occupation"),
                new ReportField("mother_age",         "11. Age at Time of Birth"),
                new ReportField("mother_residence",   "12. Residence")),
            new ReportSection("13-17. Father",
                new ReportField("father_first_name",  "13. First Name"),
                new ReportField("father_middle_name", "     Middle Name"),
                new ReportField("father_last_name",   "     Last Name"),
                new ReportField("father_citizenship", "14. Citizenship"),
                new ReportField("father_religion",    "15. Religion"),
                new ReportField("father_occupation",  "16. Occupation"),
                new ReportField("father_age",         "17. Age at Time of Birth"),
                new ReportField("father_residence",   "     Residence")),
            new ReportSection("18. Marriage of Parents",
                new ReportField("parents_marriage_date",  "Date of Marriage", true),
                new ReportField("parents_marriage_place", "Place of Marriage")),
            new ReportSection("19. Attendant at Birth / Certification",
                new ReportField("attendant_type",    "19a. Attendant"),
                new ReportField("attendant_name",    "19b. Name in Print"),
                new ReportField("attendant_title",   "      Title or Position"),
                new ReportField("attendant_address", "      Address"),
                new ReportField("attendant_date",    "      Date Signed", true)),
            new ReportSection("20. Certification of Informant",
                new ReportField("informant_name",         "Name in Print"),
                new ReportField("informant_relationship", "Relationship to the Child"),
                new ReportField("informant_address",      "Address"),
                new ReportField("informant_date",         "Date", true)),
            new ReportSection("21. Prepared By",
                new ReportField("prepared_by",       "Name in Print"),
                new ReportField("prepared_by_title", "Title or Position"),
                new ReportField("prepared_by_date",  "Date", true)),
            new ReportSection("22. Received at the Office of the Civil Registrar",
                new ReportField("received_by",       "Name in Print"),
                new ReportField("received_by_title", "Title or Position"),
                new ReportField("received_by_date",  "Date", true)),
            // Item 25 exists on the 2007 sheet only. A 1993 record leaves these blank, and
            // the structured renderer already prints a blank entry as an em-dash, so the
            // section is shared rather than split per revision.
            new ReportSection("25. Registered at the Civil Registrar",
                new ReportField("registered_by",       "Name in Print"),
                new ReportField("registered_by_title", "Title or Position"),
                new ReportField("registered_by_date",  "Date", true)),
            new ReportSection("Remarks",
                new ReportField("remarks",     "Remarks / Annotations")),
        };

        private static List<ReportSection> MarriageReport() => new List<ReportSection>
        {
            new ReportSection("Registry",
                new ReportField("registry_no", "Registry No."),
                new ReportField("book_volume", "Book / Volume"),
                new ReportField("book_page",   "Page No."),
                new ReportField("office_province",     "Province"),
                new ReportField("office_municipality", "City / Municipality")),
            new ReportSection("1-8. Husband",
                new ReportField("husband_first_name",   "1. First Name"),
                new ReportField("husband_middle_name",  "    Middle Name"),
                new ReportField("husband_last_name",    "    Last Name"),
                new ReportField("husband_date_of_birth","2. Date of Birth", true),
                new ReportField("husband_age",          "    Age"),
                new ReportField("husband_place_of_birth","3. Place of Birth"),
                new ReportField("husband_citizenship",  "5. Citizenship"),
                new ReportField("husband_residence",    "6. Residence"),
                new ReportField("husband_religion",     "7. Religion / Sect"),
                new ReportField("husband_civil_status", "8. Civil Status")),
            new ReportSection("1-8. Wife",
                new ReportField("wife_first_name",    "1. First Name"),
                new ReportField("wife_middle_name",   "    Middle Name"),
                new ReportField("wife_last_name",     "    Maiden Surname"),
                new ReportField("wife_date_of_birth", "2. Date of Birth", true),
                new ReportField("wife_age",           "    Age"),
                new ReportField("wife_place_of_birth","3. Place of Birth"),
                new ReportField("wife_citizenship",   "5. Citizenship"),
                new ReportField("wife_residence",     "6. Residence"),
                new ReportField("wife_religion",      "7. Religion / Sect"),
                new ReportField("wife_civil_status",  "8. Civil Status")),
            new ReportSection("14-17. Place and Date of Marriage",
                new ReportField("church_name",        "Church / Office"),
                new ReportField("place_municipality", "City / Municipality"),
                new ReportField("place_province",     "Province"),
                new ReportField("date_of_marriage",   "Date of Marriage", true),
                new ReportField("time_of_marriage",   "Time of Marriage"),
                new ReportField("solemnizer",         "Solemnizing Officer")),
        };

        private static List<ReportSection> DeathReport() => new List<ReportSection>
        {
            new ReportSection("Registry",
                new ReportField("registry_no", "Registry No."),
                new ReportField("book_volume", "Book / Volume"),
                new ReportField("book_page",   "Page No."),
                new ReportField("office_province",     "Province"),
                new ReportField("office_municipality", "City / Municipality")),
            new ReportSection("1-13. Deceased",
                new ReportField("deceased_first_name",  "1. First Name"),
                new ReportField("deceased_middle_name", "    Middle Name"),
                new ReportField("deceased_last_name",   "    Last Name"),
                new ReportField("sex",            "2. Sex"),
                new ReportField("date_of_death",  "3. Date of Death", true),
                new ReportField("time_of_death",  "    Time of Death"),
                new ReportField("date_of_birth",  "4. Date of Birth", true),
                new ReportField("age",            "5. Age at Time of Death"),
                new ReportField("place_of_death", "6. Place of Death"),
                new ReportField("civil_status",   "7. Civil Status"),
                new ReportField("religion",       "8. Religion / Sect"),
                new ReportField("citizenship",    "9. Citizenship"),
                new ReportField("residence",      "10. Residence"),
                new ReportField("occupation",     "11. Occupation"),
                new ReportField("father_name",    "12. Name of Father"),
                new ReportField("mother_name",    "13. Mother's Maiden Name")),
            new ReportSection("Medical Certificate",
                new ReportField("immediate_cause",      "Immediate Cause"),
                new ReportField("antecedent_cause",     "Antecedent Cause"),
                new ReportField("underlying_cause",     "Underlying Cause"),
                new ReportField("medical_certifier",    "Certifier"),
                new ReportField("certifier_license_no", "License No.")),
            new ReportSection("Corpse Disposal / Permit",
                new ReportField("disposal_method",   "Method of Disposal"),
                new ReportField("place_of_disposal", "Place of Disposal"),
                new ReportField("date_of_disposal",  "Date of Disposal", true),
                new ReportField("permit_type",       "Permit Type")),
        };

        private static List<FormSection> DeathSections() => new List<FormSection>
        {
            new FormSection("Form Identification",
                "RegistryNo", "Province", "CityMunicipality"),
            new FormSection("1-8. Deceased",
                "DeceasedFirst", "DeceasedMiddle", "DeceasedLast", "FullName", "Sex",
                "DateOfDeath", "DateOfBirth", "Age", "PlaceOfDeath", "CivilStatus",
                "Religion", "Citizenship", "Residence", "Occupation"),
            new FormSection("9-10. Parents",
                "FatherName", "MotherMaidenName"),
            new FormSection("Medical Certificate",
                "CauseOfDeath"),
            new FormSection("24-25. Corpse Disposal",
                "CorpseDisposal", "PlaceOfDisposal"),
            new FormSection("26. Certification of Informant",
                "Informant", "InformantRelationship", "InformantAddress", "InformantDate"),
            new FormSection("27. Prepared By",
                "PreparedByName", "PreparedByTitle", "PreparedByDate"),
            new FormSection("28. Received By",
                "ReceivedByName", "ReceivedByTitle", "ReceivedByDate"),
            new FormSection("29. Registered by the Civil Registrar",
                "RegisteredByName", "RegisteredByTitle", "RegisteredByDate"),
        };

        // ---- Certificate of Live Birth, MF-102 (Revised January 2007) --------------

        private static FormDefinition Birth2007()
        {
            var d = new FormDefinition
            {
                FormCode = "MF-102-2007",
                FormName = "Certificate of Live Birth",
                FormType = DocKind.Birth,
                MunicipalFormNo = "102",
                Revision = "Revised January 2007",
                LayoutCode = "MF-102 (2007)",
                RecordTable = "births",
                ReportView = "v_birth_certificate",
                RptFile = "MF-102-2007.rpt",
                BlankAsset = "Form102Blank.png",
                // The form prints the Republic seal itself, so an overlay must not draw a
                // logo over it. The structured renderer, which draws its own header, does.
                LogoRect = RectangleF.Empty,
                // Over the "received at the office of the civil registrar" block.
                StampRect = new RectangleF(0.700f, 0.855f, 0.180f, 0.075f),
                Sections = BirthSections(),
                ReportSections = BirthReport(),
            };

            BirthColumns(d);
            Birth2007PrintMap(d);
            return d;
        }

        // ---- Certificate of Live Birth, MF-102 (Revised January 1993) --------------
        // Same registry table and report view; different rows and different positions.
        // No blank scan of the 1993 sheet is on file, so it prints in structured layout
        // until the office supplies one — a replica drawn on the WRONG blank form would
        // put every value in the wrong box, which is worse than a clean listing.

        private static FormDefinition Birth1993()
        {
            var d = new FormDefinition
            {
                FormCode = "MF-102-1993",
                FormName = "Certificate of Live Birth",
                FormType = DocKind.Birth,
                MunicipalFormNo = "102",
                Revision = "Revised January 1993",
                LayoutCode = "MF-102 (1993)",
                RecordTable = "births",
                ReportView = "v_birth_certificate",
                RptFile = "MF-102-1993.rpt",
                BlankAsset = null,
                LogoRect = new RectangleF(0.070f, 0.030f, 0.100f, 0.065f),
                StampRect = new RectangleF(0.700f, 0.855f, 0.180f, 0.075f),
                Sections = BirthSections(),
                ReportSections = BirthReport(),
            };
            BirthColumns(d);
            // 1416x2048 reference scan -> 8.5 x 12.3 in.
            d.PrintPage = new SizeF(612f, 885f);
            PrintMapFromLayout(d);
            return d;
        }

        private static FormDefinition Marriage1993()
        {
            var d = new FormDefinition
            {
                FormCode = "MF-97-1993",
                FormName = "Certificate of Marriage",
                FormType = DocKind.Marriage,
                MunicipalFormNo = "97",
                Revision = "Revised January 1993",
                LayoutCode = "MF-97 (1993)",
                RecordTable = "marriages",
                ReportView = "v_marriage_certificate",
                RptFile = "MF-97-1993.rpt",
                // The office supplied the blank MF-97 as a vector PDF, so the print map below
                // is measured against the form's OWN printed labels rather than estimated.
                BlankAsset = "Form97Blank.png",
                // The form prints its own Republic heading, so an overlay must not draw a logo
                // over it - same call as MF-102.
                LogoRect = RectangleF.Empty,
                StampRect = new RectangleF(0.700f, 0.855f, 0.180f, 0.075f),
                Sections = MarriageSections(),
                ReportSections = MarriageReport(),
            };

            var c = d.Columns;
            c["RegistryNo"] = "registry_no";
            c["HusbandFirst"] = "husband_first_name";
            c["HusbandMiddle"] = "husband_middle_name";
            c["HusbandLast"] = "husband_last_name";
            c["HusbandAge"] = "husband_age";
            c["HusbandDateOfBirth"] = "husband_date_of_birth";
            c["HusbandCivilStatus"] = "husband_civil_status";
            c["WifeFirst"] = "wife_first_name";
            c["WifeMiddle"] = "wife_middle_name";
            c["WifeLast"] = "wife_last_name";
            c["WifeAge"] = "wife_age";
            c["WifeDateOfBirth"] = "wife_date_of_birth";
            c["WifeCivilStatus"] = "wife_civil_status";
            c["DateOfMarriage"] = "date_of_marriage";
            c["Solemnizer"] = "solemnizer";
            c["SolemnizerPosition"] = "solemnizer_position";
            c["Witness1"] = "witness1_name";
            c["Witness2"] = "witness2_name";
            c["LicenseNo"] = "license_no";
            c["LicenseDate"] = "license_date";
            c["ReceivedByName"] = "received_by";
            c["ReceivedByTitle"] = "received_by_title";
            c["ReceivedByDate"] = "received_by_date";
            // The scanner has been reading the four parents since 2026-09-02; until
            // migration 30 there was nowhere on the record to put them.
            c["HusbandFatherName"] = "husband_father_name";
            c["HusbandMotherName"] = "husband_mother_name";
            c["WifeFatherName"] = "wife_father_name";
            c["WifeMotherName"] = "wife_mother_name";
            d.PrintPage = new SizeF(792f, 1224f);   // the blank form's own page box
            Marriage1993PrintMap(d);
            return d;
        }

        /// <summary>
        /// MF-97 print map, measured off the blank form the office supplied.
        /// <para/>
        /// That PDF carries real text, so every position here was taken from the form's OWN
        /// printed labels and table rules rather than estimated: the "(first) (middle initial)
        /// (last)" headings give the column x, and the horizontal rules give each row's band.
        /// A value is placed just inside the top of its band so it sits in the box rather than
        /// on the line under it.
        /// <para/>
        /// Row bands, from the form's rules: 160.6-211.0 names, 211.0-242.4 date of birth/age,
        /// 242.4-273.7 place of birth, 273.7-307.0 sex, 307.0-338.3 citizenship,
        /// 338.3-369.7 residence, 369.7-401.0 religion, 401.0-432.4 civil status,
        /// 432.4-456.7 father, 488.0-515.8 mother.
        /// </summary>
        private static void Marriage1993PrintMap(FormDefinition d)
        {
            const float W = 792f, H = 1224f;
            Action<string, float, float, float, bool> cell =
                (col, x, y, size, isDate) =>
                    d.Cells.Add(new PrintCell(col, x / W, y / H, size, -1, isDate));

            // Header block.
            cell("office_province", 160f, 110f, 8f, false);
            cell("office_municipality", 194f, 123f, 8f, false);
            cell("registry_no", 440f, 124f, 8f, false);

            // Name of contracting parties. The husband and wife name rows carry their own
            // "(first) (middle initial) (last)" headings and they do NOT share x with the
            // parent rows lower down, so each row uses its own measured columns.
            cell("husband_first_name", 205f, 180f, 8.5f, false);
            cell("husband_middle_name", 252f, 180f, 8.5f, false);
            cell("husband_last_name", 321f, 180f, 8f, false);
            cell("wife_first_name", 371f, 180f, 8.5f, false);
            cell("wife_middle_name", 423f, 180f, 8.5f, false);
            cell("wife_last_name", 507f, 180f, 8f, false);

            // Date of birth / age. The (day)(month)(year) sub-headings are a guide for a
            // handwritten entry; a printed date reads across them, which is what the office
            // does on a typed form.
            cell("husband_date_of_birth", 205f, 228f, 7.5f, true);
            cell("husband_age", 330f, 228f, 7.5f, false);
            cell("wife_date_of_birth", 371f, 228f, 7.5f, true);
            cell("wife_age", 517f, 228f, 7.5f, false);

            cell("husband_place_of_birth", 195f, 256f, 7.5f, false);
            cell("wife_place_of_birth", 363f, 256f, 7.5f, false);

            cell("husband_citizenship", 195f, 320f, 7.5f, false);
            cell("wife_citizenship", 363f, 320f, 7.5f, false);

            cell("husband_residence", 195f, 351f, 7.5f, false);
            cell("wife_residence", 363f, 351f, 7.5f, false);

            cell("husband_religion", 195f, 383f, 7.5f, false);
            cell("wife_religion", 363f, 383f, 7.5f, false);

            cell("husband_civil_status", 195f, 414f, 7.5f, false);
            cell("wife_civil_status", 363f, 414f, 7.5f, false);

            // The parents' rows are on the form and OCR reads them, but v_marriage_certificate
            // has no column for them yet, so they print blank until the registry stores them.
            // Kept mapped so adding the columns is the only change needed.
            cell("husband_father_name", 205f, 445f, 7.5f, false);
            cell("wife_father_name", 396f, 445f, 7.5f, false);
            cell("husband_mother_name", 205f, 501f, 7.5f, false);
            cell("wife_mother_name", 396f, 501f, 7.5f, false);

            // Place of marriage, its address line, then the date and time.
            cell("church_name", 226f, 657f, 8f, false);
            cell("place_municipality", 228f, 692f, 8f, false);
            cell("date_of_marriage", 226f, 715f, 8f, true);
            cell("time_of_marriage", 434f, 715f, 8f, false);

            // Above the "(Signature of Solemnizing Officer)" caption at y 1054.
            cell("solemnizer", 300f, 1042f, 8f, false);
        }

        private static FormDefinition Death2016()
        {
            var d = new FormDefinition
            {
                FormCode = "MF-103-2016",
                FormName = "Certificate of Death",
                FormType = DocKind.Death,
                MunicipalFormNo = "103",
                // The blank sheet the office supplied prints "Revised August 2016". The
                // revision is shown on the certificate and stored with the record, so it
                // follows the paper rather than the earlier guess.
                Revision = "Revised August 2016",
                LayoutCode = "MF-103 (2016)",
                RecordTable = "deaths",
                ReportView = "v_death_certificate",
                RptFile = "MF-103-2016.rpt",
                // Blank MF-103 supplied by the office as a scan of the printed sheet.
                BlankAsset = "Form103Blank.png",
                LogoRect = RectangleF.Empty,
                StampRect = new RectangleF(0.700f, 0.855f, 0.180f, 0.075f),
                Sections = DeathSections(),
                ReportSections = DeathReport(),
            };

            var c = d.Columns;
            c["RegistryNo"] = "registry_no";
            c["DeceasedFirst"] = "first_name";
            c["DeceasedMiddle"] = "middle_name";
            c["DeceasedLast"] = "last_name";
            c["FullName"] = "full_name";
            c["Sex"] = "sex";
            c["DateOfDeath"] = "date_of_death";
            c["DateOfBirth"] = "date_of_birth";
            c["Age"] = "age";
            c["PlaceOfDeath"] = "place_of_death";
            c["CivilStatus"] = "civil_status";
            c["Religion"] = "religion_name";
            c["Citizenship"] = "citizenship";
            c["CauseOfDeath"] = "immediate_cause";
            c["CorpseDisposal"] = "disposal_method";
            c["PlaceOfDisposal"] = "place_of_disposal";
            c["Informant"] = "informant_name";
            c["InformantRelationship"] = "informant_relationship";
            c["InformantAddress"] = "informant_address";
            c["InformantDate"] = "informant_date";
            c["PreparedByName"] = "prepared_by";
            c["PreparedByTitle"] = "prepared_by_title";
            c["PreparedByDate"] = "prepared_by_date";
            c["ReceivedByName"] = "received_by";
            c["ReceivedByTitle"] = "received_by_title";
            c["ReceivedByDate"] = "received_by_date";
            c["RegisteredByName"] = "registered_by";
            c["RegisteredByTitle"] = "registered_by_title";
            c["RegisteredByDate"] = "registered_by_date";
            d.PrintPage = new SizeF(612f, 936f);   // the blank form's own page box, 8.5 x 13 in
            Death2016PrintMap(d);
            return d;
        }

        /// <summary>
        /// MF-103 print map, measured off the blank form the office supplied.
        /// <para/>
        /// That sheet is a SCAN, not vector text, so the numbered labels were located by
        /// running OCR over the blank and each value placed inside the band its label opens.
        /// Less exact than the marriage form by nature, and verified by drawing the map back
        /// over the blank rather than trusted from the numbers alone.
        /// </summary>
        private static void Death2016PrintMap(FormDefinition d)
        {
            const float W = 612f, H = 936f;
            Action<string, float, float, float, bool> cell =
                (col, x, y, size, isDate) =>
                    d.Cells.Add(new PrintCell(col, x / W, y / H, size, -1, isDate));

            cell("office_province", 120f, 62f, 8f, false);
            cell("office_municipality", 140f, 86f, 8f, false);
            cell("registry_no", 410f, 78f, 8.5f, false);

            // 1. Name, in three cells, with 2. Sex on the same row.
            cell("deceased_first_name", 150f, 122f, 8.5f, false);
            cell("deceased_middle_name", 250f, 122f, 8.5f, false);
            cell("deceased_last_name", 340f, 122f, 8.5f, false);
            cell("sex", 470f, 122f, 8.5f, false);

            // 3/4/5. Dates and age.
            cell("date_of_death", 78f, 163f, 7.5f, true);
            cell("date_of_birth", 214f, 163f, 7.5f, true);
            cell("age", 372f, 166f, 7.5f, false);

            // 6/7. Place of death and civil status.
            cell("place_of_death", 80f, 198f, 7.5f, false);
            cell("civil_status", 460f, 200f, 7.5f, false);

            // 8/9/10. Religion, citizenship, residence.
            cell("religion", 78f, 233f, 7.5f, false);
            cell("citizenship", 215f, 233f, 7.5f, false);
            cell("residence", 344f, 233f, 7.5f, false);

            // 11/12/13. Occupation and the parents.
            cell("occupation", 80f, 268f, 7.5f, false);
            cell("father_name", 200f, 268f, 7.5f, false);
            cell("mother_name", 395f, 268f, 7.5f, false);

            // 19b I. Immediate cause, on the "a." rule.
            cell("immediate_cause", 200f, 320f, 7.5f, false);

            // 23. Corpse disposal.
            cell("disposal_method", 82f, 610f, 7.5f, false);
        }

        /// <summary>
        /// Extraction key to `births` column. Shared by both MF-102 revisions — the
        /// registry table is the same whichever revision the paper was, which is exactly
        /// why form identity has to be stored alongside rather than inferred from the row.
        /// </summary>
        private static void BirthColumns(FormDefinition d)
        {
            var c = d.Columns;
            c["RegistryNo"] = "registry_no";
            c["ChildFirst"] = "first_name";
            c["ChildMiddle"] = "middle_name";
            c["ChildLast"] = "last_name";
            c["Sex"] = "sex";
            c["DateOfBirth"] = "date_of_birth";
            c["TimeOfBirth"] = "time_of_birth";
            c["PlaceOfBirth"] = "place_of_birth";
            c["TypeOfBirth"] = "type_of_birth";
            c["BirthOrder"] = "birth_order";
            c["Weight"] = "weight_grams";
            c["MotherFirst"] = "mother_first_name";
            c["MotherMiddle"] = "mother_middle_name";
            c["MotherLast"] = "mother_last_name";
            c["MotherCitizenship"] = "mother_citizenship";
            c["MotherReligion"] = "mother_religion";
            c["MotherOccupation"] = "mother_occupation";
            c["MotherAge"] = "mother_age";
            c["MotherResidence"] = "mother_residence";
            c["ChildrenBornAlive"] = "mother_children_born_alive";
            c["ChildrenLiving"] = "mother_children_living";
            c["ChildrenDead"] = "mother_children_dead";
            c["FatherFirst"] = "father_first_name";
            c["FatherMiddle"] = "father_middle_name";
            c["FatherLast"] = "father_last_name";
            c["FatherCitizenship"] = "father_citizenship";
            c["FatherReligion"] = "father_religion";
            c["FatherOccupation"] = "father_occupation";
            c["FatherAge"] = "father_age";
            c["FatherResidence"] = "father_residence";
            c["MarriageOfParentsDate"] = "parents_marriage_date";
            c["MarriageOfParentsPlace"] = "parents_marriage_place";
            c["Attendant"] = "attendant_type";
            c["AttendantName"] = "attendant_name";
            c["AttendantTitle"] = "attendant_title";
            c["AttendantAddress"] = "attendant_address";
            c["AttendantDate"] = "attendant_date";
            c["Informant"] = "informant_name";
            c["InformantRelationship"] = "informant_relationship";
            c["InformantAddress"] = "informant_address";
            c["InformantDate"] = "informant_date";
            c["PreparedByName"] = "prepared_by";
            c["PreparedByTitle"] = "prepared_by_title";
            c["PreparedByDate"] = "prepared_by_date";
            c["ReceivedByName"] = "received_by";
            c["ReceivedByTitle"] = "received_by_title";
            c["ReceivedByDate"] = "received_by_date";
            c["RegisteredByName"] = "registered_by";
            c["RegisteredByTitle"] = "registered_by_title";
            c["RegisteredByDate"] = "registered_by_date";
        }

        /// <summary>
        /// Where each value sits on the blank MF-102 (2007). These are the coordinates
        /// that were measured against the real PSA form and used to live as literals in
        /// BirthCertificatePrinter — the same numbers, divided by the 792 x 1224 pt page
        /// so they are resolution-independent and can be held as DATA rather than code.
        /// Columns are the report view's, so a Crystal .rpt bound to the same view and
        /// this overlay draw the same value in the same place.
        /// </summary>
        private static void Birth2007PrintMap(FormDefinition d)
        {
            const float W = 792f, H = 1224f;
            Action<string, float, float, float, int, bool> cell =
                (col, x, y, size, part, isDate) =>
                    d.Cells.Add(new PrintCell(col, x / W, y / H, size, part, isDate));
            Action<string, string, float, float> mark =
                (col, when, x, y) => d.Marks.Add(new PrintMark(col, when, x / W, y / H));

            // Header: registering LGU + registry number. These came from the office
            // profile rather than the record, so a second LGU needs no code change.
            cell("office_province", 150, 168, 7.5f, -1, false);
            cell("registry_no", 490, 168, 7.5f, -1, false);
            cell("office_municipality", 180, 180, 7.5f, -1, false);

            // 1. Child's name
            cell("child_first_name", 250, 234, 9f, -1, false);
            cell("child_middle_name", 328, 234, 9f, -1, false);
            cell("child_last_name", 447, 234, 9f, -1, false);

            // 2. Sex
            mark("sex", "Male", 155, 261);
            mark("sex", "Female", 222, 261);

            // 3. Date of birth
            cell("date_of_birth", 419, 276, 7.5f, -1, true);

            // 4. Place of birth — one column, three printed boxes
            cell("place_of_birth", 225, 328, 7.5f, 0, false);
            cell("place_of_birth", 376, 328, 7.5f, 1, false);
            cell("place_of_birth", 470, 328, 7.5f, 2, false);

            // 5a. Type of birth
            mark("type_of_birth", "Single", 136, 355);
            mark("type_of_birth", "Twin", 210, 355);
            mark("type_of_birth", "*", 170, 365);       // Triplet, Quadruplet, …

            // 5c/d. Birth order + weight
            cell("birth_order", 196, 419, 7.5f, -1, false);
            cell("weight_grams", 408, 419, 7.5f, -1, false);

            // 6. Mother's maiden name
            cell("mother_first_name", 253, 469, 7.5f, -1, false);
            cell("mother_middle_name", 332, 469, 7.5f, -1, false);
            cell("mother_last_name", 445, 469, 7.5f, -1, false);

            // 7/8. Mother citizenship / religion
            cell("mother_citizenship", 152, 500, 7.5f, -1, false);
            cell("mother_religion", 407, 500, 7.5f, -1, false);

            // 9a/b/c. Mother's children
            cell("mother_children_born_alive", 180, 545, 7.5f, -1, false);
            cell("mother_children_living", 335, 545, 7.5f, -1, false);
            cell("mother_children_dead", 503, 545, 7.5f, -1, false);

            // 10/11. Mother occupation / age
            cell("mother_occupation", 152, 595, 7.5f, -1, false);
            cell("mother_age", 492, 578, 7.5f, -1, false);

            // 12. Mother residence
            cell("mother_residence", 235, 634, 7.5f, -1, false);

            // 13. Father's name
            cell("father_first_name", 250, 668, 9f, -1, false);
            cell("father_middle_name", 328, 668, 9f, -1, false);
            cell("father_last_name", 450, 668, 9f, -1, false);

            // 14/15. Father citizenship / religion
            cell("father_citizenship", 155, 712, 7.5f, -1, false);
            cell("father_religion", 432, 712, 7.5f, -1, false);

            // 16/17. Father occupation / age
            cell("father_occupation", 158, 762, 7.5f, -1, false);
            cell("father_age", 492, 752, 7.5f, -1, false);

            // 18. Date and place of marriage of parents. The paper gives this one line;
            // the printer joins the two columns onto it.
            cell("parents_marriage_date", 95, 802, 7.5f, -1, true);
            cell("parents_marriage_place", 185, 802, 7.5f, -1, false);

            // 19a. Attendant
            mark("attendant_type", "Physician", 160, 828);
            mark("attendant_type", "Nurse", 318, 828);
            mark("attendant_type", "Midwife", 478, 828);
            mark("attendant_type", "Hilot", 160, 838);
            mark("attendant_type", "*", 318, 838);

            // 19b. Certification of birth
            cell("time_of_birth", 456, 878, 7.5f, -1, false);
            cell("attendant_address", 404, 908, 7.5f, -1, false);
            cell("attendant_name", 188, 924, 7.5f, -1, false);
            cell("attendant_title", 195, 948, 7.5f, -1, false);

            cell("attendant_date", 404, 948, 7.5f, -1, true);

            // 20. Informant
            cell("informant_address", 404, 1004, 7.5f, -1, false);
            cell("informant_name", 189, 1020, 7.5f, -1, false);
            cell("informant_relationship", 228, 1044, 7.5f, -1, false);
            cell("informant_date", 390, 1044, 7.5f, -1, true);

            // 21/22. Prepared by / Received at the office. Both blocks print a name, then a
            // title or position, then a date; the two columns share the same row bands, so
            // the y values are paired.
            cell("prepared_by", 188, 1118, 7.5f, -1, false);
            cell("received_by", 425, 1118, 7.5f, -1, false);
            cell("prepared_by_title", 188, 1145, 7.5f, -1, false);
            cell("received_by_title", 425, 1145, 7.5f, -1, false);
            cell("prepared_by_date", 188, 1158, 7.5f, -1, true);
            cell("received_by_date", 425, 1158, 7.5f, -1, true);
        }
    }
}
