using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>What a field is allowed to contain. Drives cleaning, scoring and validation.</summary>
    public enum FieldShape
    {
        Text,       // free text (occupation, religion, …)
        Name,       // person-name cell: letters, hyphen, apostrophe, Ñ
        Place,      // place text: letters, digits, commas, periods
        Number,     // an integer (weight, age, count)
        Date,       // a date in any of the printed orders
        Registry,   // "yyyy-nnnn" registry number
        Choice,     // one of a fixed option list, possibly ticked with an X
        Time        // clock time
    }

    /// <summary>One value region of a known PSA form.</summary>
    public class FieldSpec
    {
        public string Key;
        public string Label;
        public RectangleF Rect;        // 0-1 page coordinates in the layout's template space
        public OcrRegionMode Mode = OcrRegionMode.Line;
        public FieldShape Shape = FieldShape.Text;
        public string[] Choices;       // Shape == Choice
        public bool Required;          // a record cannot be saved without it
        public bool Core = true;       // part of the registry record (vs. a nice-to-have)

        /// <summary>
        /// Keep printed form words that would normally be stripped as labels. Needed where
        /// the VALUE legitimately contains them — "OFFICE OF THE MUNICIPAL MAYOR" is a real
        /// place of marriage, and stripping "office" and "municipal" as label text left it
        /// reading "OF THE MAYOR". The filter cannot tell the two uses apart, so the field
        /// that knows must say so.
        /// </summary>
        public bool KeepLabelWords;

        /// <summary>
        /// Fields sharing a row group are ALSO read as one strip and then split by word
        /// position. A three-cell name row reads better whole than as three crops: the
        /// engine gets the whole printed line as context, and a cell boundary that falls a
        /// couple of millimetres inside a word no longer clips it ("Gilvan" read as
        /// "ilvan"). The individual crops stay in the vote as well — whichever reading the
        /// evidence supports wins.
        /// </summary>
        public string RowGroup;

        public FieldSpec(string key, string label, float x, float y, float w, float h,
                         FieldShape shape = FieldShape.Text,
                         OcrRegionMode mode = OcrRegionMode.Line,
                         bool required = false, string[] choices = null, bool core = true)
        {
            Key = key; Label = label; Rect = new RectangleF(x, y, w, h);
            Shape = shape; Mode = mode; Required = required; Choices = choices; Core = core;
        }

        public FieldSpec InRow(string rowGroup) { RowGroup = rowGroup; return this; }

        public FieldSpec KeepingLabelWords() { KeepLabelWords = true; return this; }
    }

    /// <summary>A printed label whose position calibrates the template onto this particular photo.</summary>
    public class AnchorSpec
    {
        public string Pattern;         // regex matched against a single OCR word
        public PointF At;              // where that word sits in the template
        public AnchorSpec(string pattern, float x, float y) { Pattern = pattern; At = new PointF(x, y); }
    }

    /// <summary>
    /// One PSA form revision: how to recognise it, where its fields are, and which
    /// printed labels calibrate it. Revisions matter — the 1993 and 2007 Certificates of
    /// Live Birth number their fields differently and put the parents' blocks in
    /// different places, so one set of coordinates cannot serve both.
    /// </summary>
    public class FormLayout
    {
        public DocKind Kind;
        public string Code;                     // e.g. "MF-102 (2007)"
        public string[] StrongMarkers = new string[0];   // regex on the page text: near-definitive
        public string[] WeakMarkers = new string[0];
        public List<AnchorSpec> Anchors = new List<AnchorSpec>();
        public List<FieldSpec> Fields = new List<FieldSpec>();
        /// <summary>Printed words that must never be accepted AS a value on this form.</summary>
        public string[] LabelWords = new string[0];

        /// <summary>
        /// Where the form's printed horizontal rules fall in the template (0-1 of page
        /// height). Used to lock the row grid onto a photograph whose framing differs from
        /// the reference scan — the rules survive on a page whose text does not.
        /// </summary>
        public float[] Rulings = new float[0];
    }

    /// <summary>
    /// The form-layout library: explicit field regions for the PSA forms the office
    /// actually digitises. Coordinates were calibrated from the office's own scans and
    /// are stored in a template space normalised to the page (0-1); <see cref="PageFit"/>
    /// then shifts and scales that template onto the photo at hand, so a second photo of
    /// the same certificate — different distance, different crop — still reads.
    /// </summary>
    public static class DocLayouts
    {
        // Printed hint text that appears INSIDE value rows on these forms. If a region
        // read returns one of these, the row's handwritten/typed value was not found and
        // the label was picked up instead — which is worse than a blank, because it looks
        // filled in and gets saved as somebody's name.
        public static readonly string[] CommonLabelWords =
        {
            "first", "middle", "last", "maiden", "name", "initial", "day", "month", "year",
            "province", "city", "municipality", "barangay", "street", "house", "country",
            "hospital", "clinic", "institution", "registry", "signature", "print", "position",
            "title", "address", "date", "place", "relationship", "certification", "informant",
            "attendant", "physician", "nurse", "midwife", "hilot", "others", "specify",
            "single", "twin", "triplet", "quadruplet", "male", "female", "sex", "weight",
            "grams", "birth", "death", "marriage", "husband", "wife", "father", "mother",
            "child", "citizenship", "religion", "sect", "occupation", "age", "residence",
            "remarks", "annotation", "republic", "philippines", "office", "civil", "registrar",
            "general", "certificate", "live", "form", "revised", "municipal", "copy", "page",
            "accomplished", "quadruplicate", "completed", "years", "time", "order", "multiple",
            "total", "number", "children", "born", "alive", "still", "living", "including",
            "now", "dead", "deceased", "solemnizing", "officer", "designation", "witnesses",
            "settlement", "consent", "advice", "contracting", "parties", "reference",
            "cemetery", "crematory", "disposal", "corpse", "hereby", "certify", "supplied",
            "knowledge", "belief", "witnesses", "witness", "solemnizing", "license",
            "licence", "issued", "received", "prepared", "registered", "designation"
        };

        /// <summary>
        /// Spellings the office itself uses. These fields are closed lists on a Philippine
        /// civil-registry form, so a reading one or two characters off a canonical value
        /// ("Pilip ino", "Catholie") is that value misread — snapping it back is reading
        /// the form, not guessing at it. Anything further away is left exactly as read.
        /// </summary>
        public static readonly string[] Citizenships =
        {
            "Filipino", "American", "Chinese", "Japanese", "Korean", "Indian",
            "British", "Australian", "Canadian", "Spanish", "German", "Vietnamese"
        };

        public static readonly string[] Religions =
        {
            "Roman Catholic", "Catholic", "Iglesia ni Cristo", "Islam", "Baptist",
            "Protestant", "Born Again", "Aglipayan", "Methodist", "Seventh-day Adventist",
            "Jehovah's Witness", "Evangelical", "Pentecostal", "Church of Christ"
        };

        private static List<FormLayout> _all;

        public static List<FormLayout> All => _all ?? (_all = Build());

        /// <summary>
        /// Pick the layout that best explains this page. Returns null when nothing scores
        /// high enough — an unrecognised form must not be read with another form's
        /// coordinates, because every value would come from the wrong box.
        /// </summary>
        public static FormLayout Detect(string pageText, out int confidence)
        {
            confidence = 0;
            if (string.IsNullOrWhiteSpace(pageText)) return null;
            string text = pageText.ToLowerInvariant();
            string plain = Regex.Replace(text, @"[^a-z0-9\s]", " ");

            FormLayout best = null; int bestScore = 0, runnerUp = 0;
            foreach (FormLayout layout in All)
            {
                int score = 0;
                foreach (string m in layout.StrongMarkers)
                    if (Regex.IsMatch(text, m) || Regex.IsMatch(plain, m)) score += 5;
                foreach (string m in layout.WeakMarkers)
                    if (Regex.IsMatch(text, m) || Regex.IsMatch(plain, m)) score += 1;
                if (score > bestScore) { runnerUp = bestScore; bestScore = score; best = layout; }
                else if (score > runnerUp) runnerUp = score;
            }
            if (best == null || bestScore < 4) return null;
            // Reward the MARGIN over the runner-up: a page that matches two forms equally
            // well has not really been identified.
            confidence = Math.Min(99, 50 + bestScore * 4 + (bestScore - runnerUp) * 3);
            return best;
        }

        private static List<FormLayout> Build()
        {
            return new List<FormLayout> { Birth2007(), Birth1993(), Marriage1993(), Death2016() };
        }

        // ================= Certificate of Live Birth, MF-102 (Revised January 2007) =====
        // Calibrated on a 1528x2048 PSA-issued copy. Three-cell name rows, day/month/year
        // split across three columns, mother and father each with their own residence row.
        private static FormLayout Birth2007()
        {
            var f = new List<FieldSpec>
            {
                new FieldSpec("Province",          "Province",             0.330f, 0.1165f, 0.270f, 0.0135f, FieldShape.Place),
                new FieldSpec("CityMunicipality",  "City / Municipality",  0.330f, 0.1355f, 0.270f, 0.0145f, FieldShape.Place),
                new FieldSpec("RegistryNo",        "Registry Number",      0.600f, 0.1270f, 0.200f, 0.0230f, FieldShape.Registry, OcrRegionMode.Block),

                new FieldSpec("ChildFirst",        "Child First Name",     0.258f, 0.1635f, 0.212f, 0.0180f, FieldShape.Name, OcrRegionMode.Line, true).InRow("child2007"),
                new FieldSpec("ChildMiddle",       "Child Middle Name",    0.468f, 0.1635f, 0.150f, 0.0180f, FieldShape.Name).InRow("child2007"),
                new FieldSpec("ChildLast",         "Child Last Name",      0.608f, 0.1635f, 0.195f, 0.0180f, FieldShape.Name, OcrRegionMode.Line, true).InRow("child2007"),
                new FieldSpec("Sex",               "Sex",                  0.255f, 0.1855f, 0.125f, 0.0150f, FieldShape.Choice, OcrRegionMode.Line, true,
                              new[] { "Male", "Female" }),
                new FieldSpec("DobDay",            "Date of Birth — Day",  0.483f, 0.1855f, 0.100f, 0.0150f, FieldShape.Number),
                new FieldSpec("DobMonth",          "Date of Birth — Month",0.583f, 0.1855f, 0.112f, 0.0150f, FieldShape.Text),
                new FieldSpec("DobYear",           "Date of Birth — Year", 0.695f, 0.1855f, 0.112f, 0.0150f, FieldShape.Number),

                new FieldSpec("PlaceHospital",     "Hospital / Facility",  0.255f, 0.2145f, 0.235f, 0.0160f, FieldShape.Place),
                new FieldSpec("PlaceMunicipality", "Place — Municipality", 0.490f, 0.2145f, 0.170f, 0.0160f, FieldShape.Place),
                new FieldSpec("PlaceProvince",     "Place — Province",     0.660f, 0.2145f, 0.150f, 0.0160f, FieldShape.Place),

                new FieldSpec("TypeOfBirth",       "Type of Birth",        0.255f, 0.2475f, 0.125f, 0.0160f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Single", "Twin", "Triplet", "Quadruplet" }),
                new FieldSpec("BirthOrder",        "Birth Order",          0.545f, 0.2475f, 0.165f, 0.0160f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh" }),
                new FieldSpec("Weight",            "Weight at Birth (g)",  0.708f, 0.2475f, 0.078f, 0.0160f, FieldShape.Number),

                new FieldSpec("MotherFirst",       "Mother First Name",    0.258f, 0.2795f, 0.212f, 0.0180f, FieldShape.Name).InRow("mother2007"),
                new FieldSpec("MotherMiddle",      "Mother Middle Name",   0.468f, 0.2795f, 0.150f, 0.0180f, FieldShape.Name).InRow("mother2007"),
                new FieldSpec("MotherLast",        "Mother Maiden Surname",0.608f, 0.2795f, 0.195f, 0.0180f, FieldShape.Name).InRow("mother2007"),
                new FieldSpec("MotherCitizenship", "Mother Citizenship",   0.255f, 0.3025f, 0.250f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("MotherReligion",    "Mother Religion",      0.530f, 0.3025f, 0.280f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("ChildrenBornAlive", "Children Born Alive",  0.240f, 0.3355f, 0.075f, 0.0150f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ChildrenLiving",    "Children Still Living",0.315f, 0.3355f, 0.100f, 0.0150f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ChildrenDead",      "Children Now Dead",    0.420f, 0.3355f, 0.100f, 0.0150f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("MotherOccupation",  "Mother Occupation",    0.530f, 0.3355f, 0.180f, 0.0150f, FieldShape.Text),
                new FieldSpec("MotherAge",         "Mother Age at Birth",  0.708f, 0.3355f, 0.100f, 0.0150f, FieldShape.Number),
                new FieldSpec("MotherResidence",   "Mother Residence",     0.240f, 0.3565f, 0.240f, 0.0230f, FieldShape.Place, OcrRegionMode.Block),
                new FieldSpec("MotherResidenceCity","Mother Residence — City",0.475f,0.3565f,0.130f, 0.0230f, FieldShape.Place, OcrRegionMode.Block),
                new FieldSpec("MotherResidenceProvince","Mother Residence — Province",0.605f,0.3565f,0.100f,0.0230f, FieldShape.Place, OcrRegionMode.Block),

                new FieldSpec("FatherFirst",       "Father First Name",    0.258f, 0.3845f, 0.212f, 0.0210f, FieldShape.Name, OcrRegionMode.Block).InRow("father2007"),
                new FieldSpec("FatherMiddle",      "Father Middle Name",   0.468f, 0.3845f, 0.150f, 0.0210f, FieldShape.Name, OcrRegionMode.Block).InRow("father2007"),
                new FieldSpec("FatherLast",        "Father Last Name",     0.608f, 0.3845f, 0.195f, 0.0210f, FieldShape.Name, OcrRegionMode.Block).InRow("father2007"),
                new FieldSpec("FatherCitizenship", "Father Citizenship",   0.240f, 0.4235f, 0.140f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("FatherReligion",    "Father Religion",      0.380f, 0.4235f, 0.170f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("FatherOccupation",  "Father Occupation",    0.550f, 0.4235f, 0.160f, 0.0160f, FieldShape.Text),
                new FieldSpec("FatherAge",         "Father Age at Birth",  0.708f, 0.4235f, 0.100f, 0.0160f, FieldShape.Number),
                new FieldSpec("FatherResidence",   "Father Residence",     0.240f, 0.4470f, 0.240f, 0.0195f, FieldShape.Place, OcrRegionMode.Block),

                new FieldSpec("MarriageOfParentsDate", "Parents Married — Date", 0.255f, 0.4915f, 0.180f, 0.0165f, FieldShape.Date),
                new FieldSpec("MarriageOfParentsPlace","Parents Married — Place",0.455f, 0.4915f, 0.160f, 0.0165f, FieldShape.Place),
                new FieldSpec("Attendant",         "Attendant at Birth",   0.195f, 0.5265f, 0.620f, 0.0160f, FieldShape.Choice, OcrRegionMode.Sparse, false,
                              new[] { "Physician", "Nurse", "Midwife", "Hilot", "Others" }),
                new FieldSpec("TimeOfBirth",       "Time of Birth",        0.545f, 0.5445f, 0.135f, 0.0170f, FieldShape.Time, OcrRegionMode.Line, false, null, false),

                // ---- 21b. CERTIFICATION OF ATTENDANT AT BIRTH ----
                // Every row below carries its printed caption on the LEFT of the same band
                // as its value ("Name in Print ______ MARIE ..."), so the split here is
                // horizontal, not vertical: each region starts where its caption ends. The
                // caption words are in CommonLabelWords, so whatever the crop still catches
                // of them is stripped rather than saved as somebody's name.
                new FieldSpec("AttendantName",     "Attendant Name", 0.242f, 0.5850f, 0.280f, 0.0165f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("AttendantTitle",    "Attendant Title / Position", 0.312f, 0.5980f, 0.135f, 0.0155f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("AttendantAddress",  "Attendant Address",    0.548f, 0.5675f, 0.215f, 0.0290f, FieldShape.Place, OcrRegionMode.Block, false, null, false),
                new FieldSpec("AttendantDate",     "Attendant Date Signed",0.560f, 0.5935f, 0.220f, 0.0170f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 22. CERTIFICATION OF INFORMANT ----
                new FieldSpec("Informant",         "Informant Name", 0.240f, 0.6675f, 0.280f, 0.0175f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantRelationship","Informant Relationship", 0.315f, 0.6825f, 0.200f, 0.0165f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantAddress",  "Informant Address", 0.255f, 0.6980f, 0.260f, 0.0175f, FieldShape.Place, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantDate",     "Informant Date Signed", 0.255f, 0.7130f, 0.260f, 0.0165f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 23. PREPARED BY ----
                new FieldSpec("PreparedByName",    "Prepared By", 0.625f, 0.6600f, 0.145f, 0.0160f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByTitle",   "Prepared By Title", 0.618f, 0.6780f, 0.145f, 0.0155f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByDate",    "Prepared By Date", 0.655f, 0.6940f, 0.080f, 0.0150f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 24. RECEIVED BY ----
                new FieldSpec("ReceivedByName",    "Received By", 0.272f, 0.7455f, 0.180f, 0.0155f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByTitle",   "Received By Title", 0.292f, 0.7580f, 0.150f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByDate",    "Received By Date", 0.274f, 0.7800f, 0.100f, 0.0150f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 25. REGISTERED AT THE CIVIL REGISTRAR ----
                // This item exists only on the 2007 sheet; the 1993 revision folds the same
                // signature into its own item 22, "Received at the Office of the Civil
                // Registrar", so that revision carries no RegisteredBy* fields at all.
                new FieldSpec("RegisteredByName",  "Registered By", 0.580f, 0.7435f, 0.195f, 0.0160f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("RegisteredByTitle", "Registered By Title", 0.606f, 0.7540f, 0.145f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("RegisteredByDate",  "Registered By Date", 0.570f, 0.7755f, 0.095f, 0.0155f, FieldShape.Date, OcrRegionMode.Line, false, null, false),
            };

            return new FormLayout
            {
                Kind = DocKind.Birth,
                Code = "MF-102 (2007)",
                StrongMarkers = new[]
                {
                    @"certificate\s+of\s+live\s+birth", @"form\s+no\.?\s*1?[o0]2", @"revised\s+january\s+2007"
                },
                WeakMarkers = new[]
                {
                    @"maiden", @"weight\s+at\s+birth", @"birth\s+order", @"type\s+of\s+birth",
                    @"attendant", @"14\.?\s*name", @"7\.?\s*maiden", @"19\.?\s*residence",
                    @"marriage\s+of\s+parents", @"certification\s+of\s+informant", @"religion.{0,4}religious"
                },
                LabelWords = new[] { "colb", "psa" },
                Anchors = new List<AnchorSpec>
                {
                    new AnchorSpec(@"^CERTIFICATE",       0.4255f, 0.1019f),
                    new AnchorSpec(@"^City.?Municipa",    0.2485f, 0.1425f),
                    new AnchorSpec(@"^MAIDEN$",           0.2590f, 0.2725f),
                    new AnchorSpec(@"^RESIDENCE$",        0.2725f, 0.3542f),
                    new AnchorSpec(@"^OCCUPATION$",       0.5810f, 0.3206f),
                    new AnchorSpec(@"^PARENTS$",          0.3155f, 0.4700f),
                    new AnchorSpec(@"^ATTENDANT$",        0.2590f, 0.5152f),
                    new AnchorSpec(@"^INFORMANT$",        0.3495f, 0.6212f),
                },
                Fields = f
            };
        }

        // ================= Certificate of Live Birth, MF-102 (Revised January 1993) =====
        // Calibrated on a 1416x2048 NSO-issued copy. Different sheet entirely from the
        // 2007 revision: sex and type of birth are TICK BOXES, place of birth is one wide
        // row, the mother's block is items 6-12 and the father's 13-17, and the father has
        // no residence row of his own.
        private static FormLayout Birth1993()
        {
            var f = new List<FieldSpec>
            {
                new FieldSpec("Province",          "Province",             0.190f, 0.1385f, 0.290f, 0.0160f, FieldShape.Place, OcrRegionMode.Block),
                new FieldSpec("CityMunicipality",  "City / Municipality",  0.190f, 0.1525f, 0.290f, 0.0145f, FieldShape.Place),
                new FieldSpec("RegistryNo",        "Registry Number",      0.480f, 0.1395f, 0.190f, 0.0220f, FieldShape.Registry, OcrRegionMode.Block),

                new FieldSpec("ChildFirst",        "Child First Name",     0.238f, 0.1705f, 0.155f, 0.0220f, FieldShape.Name, OcrRegionMode.Block, true).InRow("child1993"),
                new FieldSpec("ChildMiddle",       "Child Middle Name",    0.393f, 0.1705f, 0.148f, 0.0220f, FieldShape.Name, OcrRegionMode.Block).InRow("child1993"),
                new FieldSpec("ChildLast",         "Child Last Name",      0.541f, 0.1705f, 0.140f, 0.0220f, FieldShape.Name, OcrRegionMode.Block, true).InRow("child1993"),
                new FieldSpec("Sex",               "Sex",                  0.200f, 0.2075f, 0.190f, 0.0155f, FieldShape.Choice, OcrRegionMode.Sparse, true,
                              new[] { "Male", "Female" }),
                new FieldSpec("DateOfBirth",       "Date of Birth",        0.500f, 0.2035f, 0.185f, 0.0195f, FieldShape.Date, OcrRegionMode.Block),
                new FieldSpec("PlaceOfBirth",      "Place of Birth",       0.170f, 0.2495f, 0.500f, 0.0165f, FieldShape.Place),
                new FieldSpec("TypeOfBirth",       "Type of Birth",        0.180f, 0.2790f, 0.210f, 0.0170f, FieldShape.Choice, OcrRegionMode.Sparse, false,
                              new[] { "Single", "Twin", "Triplet" }),
                new FieldSpec("BirthOrder",        "Birth Order",          0.185f, 0.3095f, 0.115f, 0.0160f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh" }),
                new FieldSpec("Weight",            "Weight at Birth (g)",  0.460f, 0.3095f, 0.140f, 0.0160f, FieldShape.Number),

                new FieldSpec("MotherFirst",       "Mother First Name",    0.248f, 0.3505f, 0.145f, 0.0165f, FieldShape.Name).InRow("mother1993"),
                new FieldSpec("MotherMiddle",      "Mother Middle Name",   0.385f, 0.3505f, 0.155f, 0.0165f, FieldShape.Name).InRow("mother1993"),
                new FieldSpec("MotherLast",        "Mother Maiden Surname",0.535f, 0.3505f, 0.145f, 0.0165f, FieldShape.Name).InRow("mother1993"),
                new FieldSpec("MotherCitizenship", "Mother Citizenship",   0.250f, 0.3730f, 0.190f, 0.0155f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("MotherReligion",    "Mother Religion",      0.525f, 0.3640f, 0.155f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("ChildrenBornAlive", "Children Born Alive",  0.215f, 0.4055f, 0.075f, 0.0155f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ChildrenLiving",    "Children Still Living",0.400f, 0.4000f, 0.075f, 0.0155f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ChildrenDead",      "Children Now Dead",    0.595f, 0.4090f, 0.075f, 0.0155f, FieldShape.Number, OcrRegionMode.Line, false, null, false),
                new FieldSpec("MotherOccupation",  "Mother Occupation",    0.250f, 0.4345f, 0.210f, 0.0165f, FieldShape.Text),
                new FieldSpec("MotherAge",         "Mother Age at Birth",  0.575f, 0.4345f, 0.075f, 0.0165f, FieldShape.Number),
                new FieldSpec("MotherResidence",   "Mother Residence",     0.160f, 0.4690f, 0.500f, 0.0170f, FieldShape.Place),

                new FieldSpec("FatherFirst",       "Father First Name",    0.248f, 0.4945f, 0.145f, 0.0165f, FieldShape.Name).InRow("father1993"),
                new FieldSpec("FatherMiddle",      "Father Middle Name",   0.393f, 0.4945f, 0.148f, 0.0165f, FieldShape.Name).InRow("father1993"),
                new FieldSpec("FatherLast",        "Father Last Name",     0.533f, 0.4945f, 0.152f, 0.0165f, FieldShape.Name).InRow("father1993"),
                new FieldSpec("FatherCitizenship", "Father Citizenship",   0.250f, 0.5165f, 0.190f, 0.0155f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("FatherReligion",    "Father Religion",      0.525f, 0.5165f, 0.155f, 0.0155f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("FatherOccupation",  "Father Occupation",    0.250f, 0.5420f, 0.210f, 0.0165f, FieldShape.Text),
                new FieldSpec("FatherAge",         "Father Age at Birth",  0.575f, 0.5420f, 0.075f, 0.0165f, FieldShape.Number),

                new FieldSpec("MarriageOfParents", "Parents Married — Date & Place", 0.240f, 0.5925f, 0.330f, 0.0165f, FieldShape.Place),
                new FieldSpec("Attendant",         "Attendant at Birth",   0.170f, 0.6185f, 0.510f, 0.0300f, FieldShape.Choice, OcrRegionMode.Sparse, false,
                              new[] { "Physician", "Nurse", "Midwife", "Hilot", "Others" }),
                new FieldSpec("TimeOfBirth",       "Time of Birth",        0.520f, 0.6455f, 0.110f, 0.0160f, FieldShape.Time, OcrRegionMode.Line, false, null, false),

                // ---- 19b. CERTIFICATION OF BIRTH, 20. INFORMANT, 21. PREPARED BY,
                //      22. RECEIVED AT THE OFFICE OF THE CIVIL REGISTRAR ----
                // This sheet lays the certification block out in TWO columns: name and
                // title on the left, address and date on the right.
                // MEASURED ON A DEGRADED PHOTOCOPY. The only 1993 sample the office has
                // reads its bottom third at single-digit word confidence, so these are the
                // weakest regions in the library. A row that cannot be read comes back
                // BLANK and flagged, which is the intended failure - re-measure against a
                // cleaner 1993 scan when the office supplies one.
                new FieldSpec("AttendantName",     "Attendant Name",       0.225f, 0.6900f, 0.240f, 0.0200f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("AttendantAddress",  "Attendant Address",    0.470f, 0.6900f, 0.240f, 0.0200f, FieldShape.Place, OcrRegionMode.Line, false, null, false),
                new FieldSpec("AttendantTitle",    "Attendant Title / Position", 0.225f, 0.7050f, 0.240f, 0.0190f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("AttendantDate",     "Attendant Date Signed",0.470f, 0.7040f, 0.240f, 0.0190f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                new FieldSpec("Informant",         "Informant Name",       0.225f, 0.7430f, 0.240f, 0.0200f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantAddress",  "Informant Address",    0.470f, 0.7420f, 0.240f, 0.0210f, FieldShape.Place, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantRelationship","Informant Relationship",0.300f, 0.7650f, 0.165f, 0.0190f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantDate",     "Informant Date Signed",0.470f, 0.7640f, 0.240f, 0.0200f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                new FieldSpec("PreparedByName",    "Prepared By",          0.220f, 0.8280f, 0.215f, 0.0190f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByTitle",   "Prepared By Title",    0.220f, 0.8420f, 0.215f, 0.0190f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByDate",    "Prepared By Date",     0.220f, 0.8560f, 0.215f, 0.0190f, FieldShape.Date, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByName",    "Received By",          0.450f, 0.8280f, 0.250f, 0.0190f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByTitle",   "Received By Title",    0.450f, 0.8420f, 0.250f, 0.0190f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByDate",    "Received By Date",     0.450f, 0.8560f, 0.250f, 0.0190f, FieldShape.Date, OcrRegionMode.Line, false, null, false),
            };

            return new FormLayout
            {
                Kind = DocKind.Birth,
                Code = "MF-102 (1993)",
                StrongMarkers = new[]
                {
                    @"certificate\s+of\s+live\s+birth", @"form\s+no\.?\s*1?[o0]2", @"revised\s+january\s+1993"
                },
                WeakMarkers = new[]
                {
                    @"maiden", @"weight\s+at\s+birth", @"birth\s+order", @"type\s+of\s+birth",
                    @"attendant", @"6\.?\s*maiden", @"13\.?\s*name", @"date\s+and\s+place\s+of\s+marriage",
                    @"place\s+an?\s*x\s+before", @"certification\s+of\s+birth", @"fill\s+out\s+completely"
                },
                Anchors = new List<AnchorSpec>
                {
                    new AnchorSpec(@"^CERTIFICATE",       0.3150f, 0.1010f),
                    new AnchorSpec(@"^NAME$",             0.1995f, 0.1758f),
                    new AnchorSpec(@"^OCCUPATION",        0.2210f, 0.4331f),
                    new AnchorSpec(@"^RESIDENCE",         0.2165f, 0.4625f),
                    new AnchorSpec(@"^CITIZENSHIP",       0.2200f, 0.5173f),
                    new AnchorSpec(@"^MARRIAGE$",         0.3440f, 0.5644f),
                    new AnchorSpec(@"^ATTENDANT$",        0.2190f, 0.6154f),
                },
                Fields = f
            };
        }

        // ================= Certificate of Marriage, MF-97 (Revised January 1993) =======
        // Calibrated on a 1355x2048 PSA-issued copy. This form is a TWO-COLUMN table: the
        // husband's and the wife's value for the same row sit side by side, which is why
        // reading "the line after the HUSBAND heading" out of flat page text gives both
        // spouses the same value. Each spouse's cell is its own region here.
        private static FormLayout Marriage1993()
        {
            var f = new List<FieldSpec>
            {
                new FieldSpec("Province",         "Province",              0.268f, 0.1275f, 0.250f, 0.0140f, FieldShape.Place),
                new FieldSpec("CityMunicipality", "City / Municipality",   0.268f, 0.1385f, 0.250f, 0.0145f, FieldShape.Place),
                new FieldSpec("RegistryNo",       "Registry Number",       0.515f, 0.1280f, 0.150f, 0.0230f, FieldShape.Registry, OcrRegionMode.Block),

                new FieldSpec("HusbandFirst",     "Husband First Name",    0.2535f, 0.1825f, 0.067f, 0.0165f, FieldShape.Name, OcrRegionMode.Line, true).InRow("husbandName"),
                new FieldSpec("HusbandMiddle",    "Husband Middle Name",   0.3140f, 0.1825f, 0.092f, 0.0165f, FieldShape.Name).InRow("husbandName"),
                new FieldSpec("HusbandLast",      "Husband Last Name",     0.3970f, 0.1825f, 0.066f, 0.0165f, FieldShape.Name, OcrRegionMode.Line, true).InRow("husbandName"),
                new FieldSpec("WifeFirst",        "Wife First Name",       0.4559f, 0.1800f, 0.065f, 0.0160f, FieldShape.Name, OcrRegionMode.Line, true).InRow("wifeName"),
                new FieldSpec("WifeMiddle",       "Wife Middle Name",      0.5110f, 0.1800f, 0.090f, 0.0160f, FieldShape.Name).InRow("wifeName"),
                new FieldSpec("WifeLast",         "Wife Last Name",        0.5920f, 0.1800f, 0.058f, 0.0160f, FieldShape.Name, OcrRegionMode.Line, true).InRow("wifeName"),

                new FieldSpec("HusbandDateOfBirth","Husband Date of Birth",0.2535f, 0.2055f, 0.155f, 0.0165f, FieldShape.Date),
                new FieldSpec("HusbandAge",       "Husband Age",           0.4050f, 0.2055f, 0.055f, 0.0165f, FieldShape.Number),
                new FieldSpec("WifeDateOfBirth",  "Wife Date of Birth",    0.4559f, 0.2040f, 0.155f, 0.0165f, FieldShape.Date),
                new FieldSpec("WifeAge",          "Wife Age",              0.6050f, 0.2040f, 0.048f, 0.0165f, FieldShape.Number),

                new FieldSpec("HusbandPlaceOfBirth","Husband Place of Birth",0.2535f,0.2250f, 0.205f, 0.0155f, FieldShape.Place),
                new FieldSpec("WifePlaceOfBirth", "Wife Place of Birth",   0.4559f, 0.2240f, 0.192f, 0.0155f, FieldShape.Place),
                new FieldSpec("HusbandSex",       "Husband Sex",           0.2535f, 0.2400f, 0.100f, 0.0150f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Male", "Female" }),
                new FieldSpec("WifeSex",          "Wife Sex",              0.4559f, 0.2400f, 0.100f, 0.0150f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Male", "Female" }),
                new FieldSpec("HusbandCitizenship","Husband Citizenship",  0.2535f, 0.2580f, 0.110f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("WifeCitizenship",  "Wife Citizenship",      0.4559f, 0.2580f, 0.110f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("HusbandResidence", "Husband Residence",     0.2535f, 0.2770f, 0.205f, 0.0155f, FieldShape.Place),
                new FieldSpec("WifeResidence",    "Wife Residence",        0.4559f, 0.2760f, 0.192f, 0.0155f, FieldShape.Place),
                new FieldSpec("HusbandReligion",  "Husband Religion",      0.2535f, 0.2930f, 0.110f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("WifeReligion",     "Wife Religion",         0.4559f, 0.2930f, 0.110f, 0.0150f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("HusbandCivilStatus","Husband Civil Status", 0.2535f, 0.3060f, 0.100f, 0.0150f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Single", "Widowed", "Divorced", "Annulled" }),
                new FieldSpec("WifeCivilStatus",  "Wife Civil Status",     0.4559f, 0.3060f, 0.100f, 0.0150f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Single", "Widowed", "Divorced", "Annulled" }),

                new FieldSpec("HusbandFatherName","Husband's Father",      0.2535f, 0.3315f, 0.205f, 0.0155f, FieldShape.Name),
                new FieldSpec("WifeFatherName",   "Wife's Father",         0.4559f, 0.3315f, 0.192f, 0.0155f, FieldShape.Name),
                new FieldSpec("HusbandMotherName","Husband's Mother",      0.2535f, 0.3705f, 0.205f, 0.0155f, FieldShape.Name),
                new FieldSpec("WifeMotherName",   "Wife's Mother",         0.4559f, 0.3705f, 0.192f, 0.0155f, FieldShape.Name),

                new FieldSpec("PlaceOfMarriage",  "Place of Marriage",     0.300f, 0.4795f, 0.300f, 0.0155f, FieldShape.Place).KeepingLabelWords(),
                new FieldSpec("PlaceOfMarriageAddress","Place of Marriage — Address",0.300f,0.4995f,0.300f,0.0155f, FieldShape.Place, OcrRegionMode.Line, false, null, false).KeepingLabelWords(),
                new FieldSpec("DateOfMarriage",   "Date of Marriage",      0.283f, 0.5135f, 0.242f, 0.0150f, FieldShape.Date),
                new FieldSpec("Solemnizer",       "Solemnizing Officer",   0.318f, 0.7335f, 0.205f, 0.0135f, FieldShape.Name),

                // ---- what makes the solemnisation lawful, and who witnessed it ----
                // The Family Code binds a marriage to the OFFICE the solemniser holds, not
                // merely to the name he signs, so the position is part of the record and
                // not a nicety. Both witnesses likewise: the certificate is not complete
                // without two.
                new FieldSpec("SolemnizerPosition","Solemnizer Position",   0.322f, 0.7530f, 0.165f, 0.0130f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("Witness1",         "Witness 1",             0.182f, 0.8035f, 0.160f, 0.0135f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("Witness2",         "Witness 2",             0.478f, 0.8035f, 0.155f, 0.0135f, FieldShape.Name, OcrRegionMode.Line, false, null, false),

                // ---- the marriage licence ----
                new FieldSpec("LicenseNo",        "Marriage Licence No.",  0.333f, 0.6810f, 0.090f, 0.0145f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("LicenseDate",      "Licence Issued On",     0.485f, 0.6810f, 0.160f, 0.0145f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- RECEIVED AT THE OFFICE OF THE CIVIL REGISTRAR ----
                // MF-97 has ONE receipt block, not the four MF-102 and MF-103 carry, so
                // there is nothing here for a prepared-by or a registered-by. A form that
                // does not have a row does not get a field.
                new FieldSpec("ReceivedByName",   "Received By",           0.663f, 0.6835f, 0.148f, 0.0145f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByTitle",  "Received By Title",     0.668f, 0.7080f, 0.100f, 0.0125f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByDate",   "Date Received",         0.656f, 0.7220f, 0.140f, 0.0135f, FieldShape.Date, OcrRegionMode.Line, false, null, false),
            };

            return new FormLayout
            {
                Kind = DocKind.Marriage,
                Code = "MF-97 (1993)",
                StrongMarkers = new[]
                {
                    @"certificate\s+of\s+marriage", @"form\s+no\.?\s*97", @"marriage\s+settlement"
                },
                WeakMarkers = new[]
                {
                    @"husband", @"wife", @"solemniz", @"contracting\s+parties", @"place\s+of\s+marriage",
                    @"witnesses", @"person\s+who\s+gave", @"consent\s+or\s+advice", @"municipal\s+mayor",
                    @"date\s+of\s+marriage", @"signature\s+of\s+wife", @"signature\s+of\s+husband"
                },
                Anchors = new List<AnchorSpec>
                {
                    new AnchorSpec(@"^CERTIFICATE",     0.3295f, 0.1079f),
                    new AnchorSpec(@"^M[A4]{1,2}RRIAGE",0.4805f, 0.1187f),
                    new AnchorSpec(@"^Father$",         0.2840f, 0.4437f),
                    new AnchorSpec(@"^MUNICIPAL",       0.4660f, 0.4852f),
                    new AnchorSpec(@"^CERTIFY$",        0.2750f, 0.6552f),
                    new AnchorSpec(@"REMARKS",          0.7395f, 0.0862f),
                    new AnchorSpec(@"^applicable",      0.5285f, 0.4437f),
                    new AnchorSpec(@"^REGISTRAR$",      0.7850f, 0.6552f),
                },
                Rulings = new[]
                {
                    0.2329f, 0.3092f, 0.3242f, 0.3500f, 0.3658f, 0.3921f,
                    0.4083f, 0.4342f, 0.4504f, 0.4788f, 0.6375f
                },
                Fields = f
            };
        }

        // ================= Certificate of Death, MF-103 (Revised August 2016) ==========
        // Calibrated on a 706x968 PSA-issued copy. Everything printed, one value row per
        // numbered item, deceased name in three cells with SEX in the same row.
        private static FormLayout Death2016()
        {
            var f = new List<FieldSpec>
            {
                new FieldSpec("Province",         "Province",              0.195f, 0.0915f, 0.390f, 0.0135f, FieldShape.Place),
                new FieldSpec("CityMunicipality", "City / Municipality",   0.255f, 0.1090f, 0.330f, 0.0140f, FieldShape.Place),
                new FieldSpec("RegistryNo",       "Registry Number",       0.620f, 0.1035f, 0.255f, 0.0230f, FieldShape.Registry, OcrRegionMode.Block),

                new FieldSpec("DeceasedFirst",    "Deceased First Name",   0.190f, 0.1475f, 0.175f, 0.0155f, FieldShape.Name, OcrRegionMode.Line, true).InRow("deceased"),
                new FieldSpec("DeceasedMiddle",   "Deceased Middle Name",  0.360f, 0.1475f, 0.165f, 0.0155f, FieldShape.Name).InRow("deceased"),
                new FieldSpec("DeceasedLast",     "Deceased Last Name",    0.520f, 0.1475f, 0.165f, 0.0155f, FieldShape.Name, OcrRegionMode.Line, true).InRow("deceased"),
                new FieldSpec("Sex",              "Sex",                   0.690f, 0.1475f, 0.180f, 0.0155f, FieldShape.Choice, OcrRegionMode.Line, true,
                              new[] { "Male", "Female" }),

                new FieldSpec("DateOfDeath",      "Date of Death",         0.130f, 0.1815f, 0.210f, 0.0160f, FieldShape.Date, OcrRegionMode.Line, true),
                new FieldSpec("DateOfBirth",      "Date of Birth",         0.340f, 0.1815f, 0.195f, 0.0160f, FieldShape.Date),
                new FieldSpec("Age",              "Age at Death",          0.570f, 0.1835f, 0.115f, 0.0135f, FieldShape.Number),
                new FieldSpec("PlaceOfDeath",     "Place of Death",        0.195f, 0.2115f, 0.460f, 0.0165f, FieldShape.Place),
                new FieldSpec("CivilStatus",      "Civil Status",          0.680f, 0.2115f, 0.195f, 0.0165f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Single", "Married", "Widowed", "Divorced", "Annulled", "Separated" }),

                new FieldSpec("Religion",         "Religion",              0.130f, 0.2425f, 0.205f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Religions),
                new FieldSpec("Citizenship",      "Citizenship",           0.340f, 0.2425f, 0.170f, 0.0160f, FieldShape.Text, OcrRegionMode.Line, false, DocLayouts.Citizenships),
                new FieldSpec("Residence",        "Residence",             0.515f, 0.2425f, 0.360f, 0.0160f, FieldShape.Place),
                new FieldSpec("Occupation",       "Occupation",            0.130f, 0.2570f, 0.170f, 0.0300f, FieldShape.Text, OcrRegionMode.Block),
                new FieldSpec("FatherName",       "Father's Name",         0.340f, 0.2715f, 0.235f, 0.0160f, FieldShape.Name),
                new FieldSpec("MotherMaidenName", "Mother's Maiden Name",  0.580f, 0.2715f, 0.295f, 0.0160f, FieldShape.Name),
                new FieldSpec("CauseOfDeath",     "Cause of Death (immediate)",0.325f,0.3225f, 0.375f, 0.0165f, FieldShape.Text),
                new FieldSpec("CorpseDisposal",   "Corpse Disposal",       0.150f, 0.5815f, 0.150f, 0.0155f, FieldShape.Choice, OcrRegionMode.Line, false,
                              new[] { "Burial", "Cremation" }, false),

                // ---- 25. NAME AND ADDRESS OF CEMETERY OR CREMATORY ----
                new FieldSpec("PlaceOfDisposal",  "Cemetery / Crematory",  0.450f, 0.5985f, 0.400f, 0.0160f, FieldShape.Place, OcrRegionMode.Line, false, null, false),

                // ---- 26. CERTIFICATION OF INFORMANT ----
                // Measured by cropping the block out of the reference scan and reading it
                // at 2.4x. This sheet is only 706x968, so a row is barely fourteen pixels
                // tall; the bands are given more height than the text needs because the
                // region reader loses a line it has no air around (the lesson from the
                // MF-102 pass on 2026-09-10).
                new FieldSpec("Informant",             "Informant Name",        0.245f, 0.6635f, 0.185f, 0.0145f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantRelationship", "Informant Relationship",0.300f, 0.6780f, 0.185f, 0.0140f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantAddress",      "Informant Address",     0.195f, 0.6910f, 0.285f, 0.0140f, FieldShape.Place, OcrRegionMode.Line, false, null, false),
                new FieldSpec("InformantDate",         "Informant Date Signed", 0.172f, 0.7060f, 0.215f, 0.0142f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 27. PREPARED BY ----
                new FieldSpec("PreparedByName",   "Prepared By",           0.638f, 0.6635f, 0.155f, 0.0140f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByTitle",  "Prepared By Title",     0.630f, 0.6780f, 0.175f, 0.0140f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("PreparedByDate",   "Prepared By Date",      0.640f, 0.6945f, 0.185f, 0.0145f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 28. RECEIVED BY ----
                new FieldSpec("ReceivedByName",   "Received By",           0.238f, 0.7465f, 0.205f, 0.0145f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByTitle",  "Received By Title",     0.240f, 0.7560f, 0.195f, 0.0140f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("ReceivedByDate",   "Received By Date",      0.228f, 0.7680f, 0.180f, 0.0140f, FieldShape.Date, OcrRegionMode.Line, false, null, false),

                // ---- 29. REGISTERED BY THE CIVIL REGISTRAR ----
                new FieldSpec("RegisteredByName", "Registered By",         0.618f, 0.7465f, 0.218f, 0.0140f, FieldShape.Name, OcrRegionMode.Line, false, null, false),
                new FieldSpec("RegisteredByTitle","Registered By Title",   0.630f, 0.7560f, 0.198f, 0.0145f, FieldShape.Text, OcrRegionMode.Line, false, null, false),
                new FieldSpec("RegisteredByDate", "Registered By Date",    0.632f, 0.7680f, 0.200f, 0.0140f, FieldShape.Date, OcrRegionMode.Line, false, null, false),
            };

            return new FormLayout
            {
                Kind = DocKind.Death,
                Code = "MF-103 (2016)",
                StrongMarkers = new[]
                {
                    @"certificate\s+of\s+death", @"form\s+no\.?\s*1?[o0]3", @"medical\s+certificate"
                },
                WeakMarkers = new[]
                {
                    @"deceased", @"cause\s+of\s+death", @"date\s+of\s+death", @"maternal\s+condition",
                    @"corpse\s+disposal", @"burial", @"autopsy", @"attendant", @"civil\s+status",
                    @"age\s+at\s+the\s+time\s+of\s+death", @"external\s+cause", @"cemetery"
                },
                Anchors = new List<AnchorSpec>
                {
                    new AnchorSpec(@"^CERTIFICATE",      0.4450f, 0.0818f),
                    new AnchorSpec(@"^C.?[hy]/Municipal",0.1915f, 0.1153f),
                    new AnchorSpec(@"^Registry$",        0.6665f, 0.0970f),
                    new AnchorSpec(@"^MEDICAL$",         0.4630f, 0.2929f),
                    new AnchorSpec(@"^Signature$",       0.1660f, 0.5110f),
                },
                Fields = f
            };
        }
    }

    /// <summary>
    /// Maps a layout's template coordinates onto the page in front of us.
    /// <para/>
    /// A photo of a certificate is never framed exactly like the reference scan: the page
    /// sits a bit higher, a bit smaller, a bit further left. Fixed coordinates therefore
    /// drift off the row and read the neighbouring one. This fits an independent
    /// scale+offset per axis from the printed labels the page actually shows, so the same
    /// template serves two different photographs of the same certificate.
    /// </summary>
    public class PageFit
    {
        public float ScaleX = 1f, OffsetX = 0f, ScaleY = 1f, OffsetY = 0f;

        public int AnchorsMatched;
        /// <summary>How many of the matched anchors the fitted placement actually lands on.</summary>
        public int SquareInliers;
        public int RulingsMatched;
        public int RulingsDetected;

        // TRIED AND REMOVED, 2026-09-06 — a TILTED (affine) fit, adding a shear term so a
        // row's page position could depend on how far ACROSS the form it sits. The
        // reasoning looked sound: a photograph is never square to the camera, and on the
        // second marriage photograph the wife's residence read at 79% of its characters
        // while the husband's, one row group away in x, read at 8% — which is what a
        // tilted row looks like. Built it as a RANSAC over anchor triples and measured:
        // it NEVER engaged on any of the five samples. The reason is visible in
        // SquareInliers — where a template belongs to the page the untilted fit already
        // lands on every anchor (7/7, 5/5, 4/4), leaving a tilt nothing to improve; and
        // where it does not (marriage photo 2: 5 anchors, 0 agreeing) no three of those
        // anchors agree well enough to imply a trustworthy tilt either. Don't re-add it
        // without a sample where the untilted fit lands on SOME anchors but not others.

        public RectangleF Map(RectangleF r)
        {
            float x = r.X * ScaleX + OffsetX;
            float y = r.Y * ScaleY + OffsetY;
            return new RectangleF(x, y, r.Width * ScaleX, r.Height * ScaleY);
        }

        /// <summary>
        /// Fit from the anchor labels found on the page. With fewer than three usable
        /// anchors only a translation is estimated, and with none the template is used as
        /// it stands — guessing a scale from one point would be worse than not scaling.
        /// </summary>
        public static PageFit From(FormLayout layout, OcrResult page)
        {
            var fit = new PageFit();
            if (page == null || page.Words == null || page.Words.Count == 0 ||
                page.PageWidth == 0 || page.PageHeight == 0) return fit;

            var pairs = new List<Tuple<PointF, PointF>>();   // template, page
            foreach (AnchorSpec a in layout.Anchors)
            {
                var rx = new Regex(a.Pattern, RegexOptions.IgnoreCase);
                OcrWord hit = null; double bestDist = double.MaxValue;
                foreach (OcrWord w in page.Words)
                {
                    if (w.Confidence < 40 || !rx.IsMatch(w.Text)) continue;
                    float px = (w.X + w.Width / 2f) / page.PageWidth, py = (w.Y + w.Height / 2f) / page.PageHeight;
                    double d = Math.Abs(px - a.At.X) * 0.5 + Math.Abs(py - a.At.Y);
                    if (d < bestDist) { bestDist = d; hit = w; }
                }
                // A label found half a page away from where the template puts it is a
                // different label that happens to read the same, not this anchor.
                if (hit == null || bestDist > 0.12) continue;
                pairs.Add(Tuple.Create(a.At,
                    new PointF((hit.X + hit.Width / 2f) / page.PageWidth, (hit.Y + hit.Height / 2f) / page.PageHeight)));
            }

            fit.AnchorsMatched = pairs.Count;
            if (pairs.Count == 0) return fit;

            Fit1D(pairs, p => p.X, ref fit.ScaleX, ref fit.OffsetX);
            Fit1D(pairs, p => p.Y, ref fit.ScaleY, ref fit.OffsetY);
            fit.SquareInliers = fit.Agreeing(pairs);
            return fit;
        }

        /// <summary>
        /// How many anchors the fitted placement actually lands on. The vertical
        /// tolerance is the same half-row <see cref="Fit1D"/> uses; the horizontal one is
        /// looser because these columns are wide and a small x error costs nothing.
        /// </summary>
        private int Agreeing(List<Tuple<PointF, PointF>> pairs)
        {
            return pairs.Count(p =>
                Math.Abs(p.Item2.Y - (p.Item1.Y * ScaleY + OffsetY)) <= 0.0035f &&
                Math.Abs(p.Item2.X - (p.Item1.X * ScaleX + OffsetX)) <= 0.006f);
        }

        /// <summary>
        /// Does this template actually belong to the page in front of us?
        /// <para/>
        /// A layout is picked from the page's WORDS — "Certificate of Live Birth",
        /// "Municipal Form No. 102" — which identifies the KIND of certificate but not
        /// which revision of it, and the office receives more than one. Every revision
        /// says the same words while putting the rows in different places, so a 1993
        /// sheet matches the 2007 layout's markers perfectly and is then read with
        /// coordinates that point at the wrong rows.
        /// <para/>
        /// The anchors settle it. They are printed labels the template says should be at
        /// particular spots, so a layout that genuinely belongs to this page FINDS them
        /// and lands on them: measured across the office's samples, a correct template
        /// matches 4-7 anchors and agrees with nearly all of them, while the 2007 layout
        /// on a 1993 sheet found ONE and agreed with none. Rulings can stand in for
        /// agreement — the marriage sample fits only 3 anchors but locks onto 11 printed
        /// rules, which places the table just as firmly.
        /// <para/>
        /// This matters more than it looks: a template that misses does NOT come back
        /// empty. It reads whatever is at those coordinates and returns it as a value, so
        /// "OCRG USE ONLY" arrives as a child's surname at 74% confidence. Refusing the
        /// template is what turns that into an honest blank.
        /// </summary>
        public bool Trustworthy
        {
            get { return AnchorsMatched >= 3 && (SquareInliers >= 2 || RulingsMatched >= 4); }
        }

        /// <summary>
        /// Nudge the vertical placement so the template's rules line up with the rules the
        /// page actually has. Label anchors live in the page header and can agree perfectly
        /// while the TABLE below them sits half a row out; the rules cannot.
        /// </summary>
        public void RefineToRulings(FormLayout layout, List<float> detected)
        {
            if (layout.Rulings == null || layout.Rulings.Length < 4) return;
            if (detected == null || detected.Count < 4) return;

            float bestOffset = 0f; int bestHits = 0; double bestError = double.MaxValue;
            // Search a range of about one row either way, finely enough to resolve less
            // than half a row.
            for (int step = -60; step <= 60; step++)
            {
                float candidate = OffsetY + step * 0.0004f;
                int hits = 0; double error = 0;
                foreach (float rule in layout.Rulings)
                {
                    float expected = rule * ScaleY + candidate;
                    float nearest = detected.OrderBy(y => Math.Abs(y - expected)).First();
                    double gap = Math.Abs(nearest - expected);
                    if (gap > 0.004) continue;
                    hits++; error += gap;
                }
                bool better = hits > bestHits || (hits == bestHits && error < bestError);
                if (!better) continue;
                bestHits = hits; bestError = error; bestOffset = candidate;
            }

            // Four rules landing together is the table; fewer is coincidence, and the
            // anchor fit (or the template as calibrated) stays. The bar is set against
            // however many rules this photograph actually shows, not against the full set
            // — a photo that only resolves seven of the eleven can still be placed.
            if (bestHits >= 4 && bestHits >= Math.Min(layout.Rulings.Length, detected.Count) / 2)
            {
                RulingsMatched = bestHits;
                OffsetY = bestOffset;
            }
        }

        /// <summary>
        /// Fit one axis by CONSENSUS rather than by averaging.
        /// <para/>
        /// One mis-matched anchor is normal — a label that reads the same appears twice on
        /// these forms — and an average or a median over four points is still dragged by
        /// it. Dragging the template by one percent of the page height is enough to slide
        /// every region onto the row below, which is exactly what happened to the marriage
        /// form: the spouse-name row was read off the date-of-birth row underneath it.
        /// <para/>
        /// So: propose a transform from the anchors, keep the one the most anchors AGREE
        /// with, and if too few agree, leave the template exactly as it was calibrated.
        /// An uncorrected template beats a confidently wrong correction.
        /// </summary>
        private static void Fit1D(List<Tuple<PointF, PointF>> pairs, Func<PointF, float> axis,
                                  ref float scale, ref float offset)
        {
            // Tight on purpose. The shift that has to be DETECTED here is itself under a
            // row height — the second marriage photograph sits about 0.6% of the page
            // higher than the first — so a tolerance of one row would call the shifted
            // page a match and leave every field reading the row above.
            const float tolerance = 0.0035f;

            var scales = new List<float> { 1f };
            for (int i = 0; i < pairs.Count; i++)
                for (int j = i + 1; j < pairs.Count; j++)
                {
                    float dt = axis(pairs[j].Item1) - axis(pairs[i].Item1);
                    if (Math.Abs(dt) < 0.20f) continue;      // too close to imply a scale
                    float s = (axis(pairs[j].Item2) - axis(pairs[i].Item2)) / dt;
                    if (s >= 0.96f && s <= 1.04f) scales.Add(s);
                }

            float bestScale = 1f, bestOffset = 0f;
            int bestInliers = 0; double bestSpread = double.MaxValue;

            foreach (float s in scales)
                foreach (var anchor in pairs)
                {
                    float o = axis(anchor.Item2) - axis(anchor.Item1) * s;
                    // These are photographs of a standard sheet framed the same way. A
                    // correction bigger than about one form row is not a correction, it is
                    // a mismatched label, and applying it moves EVERY field off its row.
                    if (Math.Abs(o) > 0.012f) continue;
                    var residuals = pairs
                        .Select(p => axis(p.Item2) - (axis(p.Item1) * s + o))
                        .ToList();
                    int inliers = residuals.Count(r => Math.Abs(r) <= tolerance);
                    double spread = residuals.Where(r => Math.Abs(r) <= tolerance)
                                             .Sum(r => Math.Abs(r));
                    bool better = inliers > bestInliers ||
                                  (inliers == bestInliers && spread < bestSpread);
                    if (!better) continue;
                    bestInliers = inliers; bestSpread = spread; bestScale = s; bestOffset = o;
                }

            // Three anchors agreeing is a correction; two is a coincidence often enough
            // to matter, and the cost of a wrong correction (every field read off the
            // neighbouring row) is far higher than the cost of not correcting at all.
            if (bestInliers >= 3) { scale = bestScale; offset = bestOffset; }
        }

    }

    /// <summary>What the reader made of one field.</summary>
    public class FieldRead
    {
        public string Key;
        public string Label;
        public string Value = "";
        public string OcrValue = "";      // what the engine returned before cleaning/normalising
        public int Confidence;            // 0-100, from agreement + engine confidence
        public string Issue;              // why it is flagged, null when clean
        /// <summary>A recorded office spelling replaced the reading (never creates a value).</summary>
        public bool Repaired;
        public bool Required;
        public bool Core = true;
        public RectangleF Region;         // where on the page it was read from
        public bool FromRegion = true;
    }

    /// <summary>
    /// Reads a page's fields from their own regions.
    /// <para/>
    /// Every region is recognised several ways and the readings are then judged against
    /// what the FIELD is allowed to contain: a date field keeps the reading that parses as
    /// a date, a name field keeps the one that looks like a name, a choice field keeps the
    /// one that matches an option. Confidence is the AGREEMENT between the variants, not
    /// Tesseract's mean — three preprocessings landing on the same string is evidence;
    /// one preprocessing being sure of itself is not.
    /// <para/>
    /// Nothing here invents a value. A region that yields no acceptable reading stays
    /// empty and is flagged for the operator to type in.
    /// </summary>
    public static class RegionReader
    {
        public static List<FieldRead> Read(OcrSession session, FormLayout layout, PageFit fit)
        {
            return Read(session, layout, fit, null);
        }

        /// <summary>
        /// Read every field of the layout. <paramref name="page"/>, when supplied, adds the
        /// whole-page pass as one more candidate per field: where the page pass DID manage
        /// to read a row, its words fall inside the field's own region and give a free,
        /// independent reading to vote with. Where it dropped the row — which is the normal
        /// case on these bordered tables — it simply contributes nothing.
        /// </summary>
        public static List<FieldRead> Read(OcrSession session, FormLayout layout, PageFit fit, OcrResult page)
        {
            var results = new List<FieldRead>();
            Dictionary<string, List<OcrRegionRead>> rows = ReadRowGroups(session, layout, fit);

            foreach (FieldSpec spec in layout.Fields)
            {
                RectangleF rect = fit.Map(spec.Rect);
                var read = new FieldRead
                {
                    Key = spec.Key, Label = spec.Label, Required = spec.Required,
                    Core = spec.Core, Region = rect
                };
                var reads = new List<OcrRegionRead>(session.ReadRegion(rect, spec.Mode));

                if (spec.RowGroup != null && rows.ContainsKey(spec.RowGroup))
                {
                    RectangleF strip = fit.Map(RowRect(layout, spec.RowGroup));
                    foreach (OcrRegionRead rowRead in rows[spec.RowGroup])
                    {
                        OcrRegionRead cell = SliceCell(rowRead, strip, rect);
                        if (cell != null) reads.Add(cell);
                    }
                }

                Judge(reads, spec, layout, read, PageWordsIn(page, rect));

                // Nothing acceptable from the cheap renderings: this is one of the hard
                // fields, so it is worth paying for the deeper ones before reporting a
                // blank the operator has to type in by hand.
                if (string.IsNullOrWhiteSpace(read.Value))
                {
                    reads.AddRange(session.ReadRegion(rect, spec.Mode, true));
                    Judge(reads, spec, layout, read, PageWordsIn(page, rect));
                }

                Repair(spec, read);
                results.Add(read);
            }
            return results;
        }

        /// <summary>
        /// Would this text be an acceptable value for that field?
        /// <para/>
        /// Used for values that did NOT come from the field's own region — the whole-page
        /// label pass — so a fallback can fill a blank without filling it with anything at
        /// all. Returns the cleaned value, or null when the text is not the shape the
        /// field is allowed to hold. A blank asks to be typed in; a wrong value has to be
        /// noticed first, so refusing is the safer failure.
        /// </summary>
        public static string Vet(FormLayout layout, string key, string raw, out int score)
        {
            score = 0;
            if (layout == null || string.IsNullOrWhiteSpace(raw)) return null;
            FieldSpec spec = layout.Fields.FirstOrDefault(
                f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
            if (spec == null) return null;

            string value = Normalise(Flatten(raw), spec, layout);
            if (value.Length == 0) return null;
            score = Score(value, spec);
            return score > 0 ? value : null;
        }

        /// <summary>True when this layout declares a field by that key.</summary>
        public static bool Declares(FormLayout layout, string key)
        {
            return layout != null && layout.Fields.Any(
                f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The bounding strip of every field in a row group, in template coordinates.</summary>
        private static RectangleF RowRect(FormLayout layout, string rowGroup)
        {
            List<FieldSpec> members = layout.Fields.Where(f => f.RowGroup == rowGroup).ToList();
            float left = members.Min(f => f.Rect.Left), right = members.Max(f => f.Rect.Right);
            float top = members.Min(f => f.Rect.Top), bottom = members.Max(f => f.Rect.Bottom);
            return new RectangleF(left, top, right - left, bottom - top);
        }

        private static Dictionary<string, List<OcrRegionRead>> ReadRowGroups(
            OcrSession session, FormLayout layout, PageFit fit)
        {
            var rows = new Dictionary<string, List<OcrRegionRead>>(StringComparer.Ordinal);
            foreach (string group in layout.Fields.Where(f => f.RowGroup != null)
                                                  .Select(f => f.RowGroup).Distinct())
            {
                RectangleF strip = fit.Map(RowRect(layout, group));
                OcrRegionMode mode = layout.Fields.First(f => f.RowGroup == group).Mode;
                rows[group] = session.ReadRegion(strip, mode);
            }
            return rows;
        }

        /// <summary>
        /// Take the part of a row reading that falls inside one cell, by where the words
        /// actually sit. A word is assigned to the cell its MIDDLE lands in, so a word
        /// straddling a printed column line goes to the cell that holds most of it rather
        /// than being cut in half.
        /// </summary>
        private static OcrRegionRead SliceCell(OcrRegionRead rowRead, RectangleF strip, RectangleF cell)
        {
            if (rowRead == null || rowRead.Words == null || rowRead.Words.Count == 0) return null;
            if (rowRead.RegionWidth <= 0 || strip.Width <= 0) return null;

            // The cell's x-range as a fraction of the strip, then as pixels of the crop.
            float from = (cell.Left - strip.Left) / strip.Width;
            float to = (cell.Right - strip.Left) / strip.Width;
            float x0 = from * rowRead.RegionWidth, x1 = to * rowRead.RegionWidth;

            List<OcrWord> inside = rowRead.Words
                .Where(w => (w.X + w.Width / 2f) >= x0 && (w.X + w.Width / 2f) <= x1)
                .OrderBy(w => w.X)
                .ToList();
            if (inside.Count == 0) return null;

            return new OcrRegionRead
            {
                Variant = "row/" + rowRead.Variant,
                Text = string.Join(" ", inside.Select(w => w.Text)),
                Confidence = (int)inside.Average(w => w.Confidence),
                Words = inside,
                RegionWidth = (int)Math.Max(1, x1 - x0),
                RegionHeight = rowRead.RegionHeight
            };
        }

        /// <summary>
        /// Replace a near-miss with the spelling this office already has on record.
        /// <para/>
        /// Only names and places, only when <see cref="DocVocabulary"/> finds exactly one
        /// recorded value a couple of characters away, and always reported: the operator
        /// sees what the scan actually said next to what it was changed to, because a name
        /// one letter from a name we know can still be a different person.
        /// </summary>
        private static void Repair(FieldSpec spec, FieldRead read)
        {
            if (string.IsNullOrWhiteSpace(read.Value)) return;
            // PLACES ONLY — see DocVocabulary for why person names are deliberately excluded.
            if (spec.Shape != FieldShape.Place) return;

            // A place field usually holds SEVERAL places ("Bical, Peñablanca, Cagayan"),
            // and the office knows each of them individually. Repairing the whole string
            // against the whole string almost never matches; repairing component by
            // component is what actually mends "Peflablanca" without touching the rest.
            string[] parts = read.Value.Split(',');
            var mended = new List<string>();
            bool changed = false;

            foreach (string part in parts)
            {
                string piece = part.Trim();
                if (piece.Length == 0) continue;
                int distance;
                string known = DocVocabulary.Snap(DocVocabulary.Places, piece, out distance);
                if (known == null) { mended.Add(piece); continue; }
                mended.Add(known.Trim());
                changed = true;
            }

            if (!changed || mended.Count == 0) return;

            string was = read.Value;
            read.Value = string.Join(", ", mended);
            read.Repaired = true;
            // A mended reading is not a clean one: it can never score as high as something
            // the scan actually said, and it is always shown to the operator as changed.
            read.Confidence = Math.Min(read.Confidence, 72);
            string note = "Read as \"" + Shorten(was) + "\" — spelling taken from a place the office already records";
            read.Issue = read.Issue == null ? note : read.Issue + "; " + note;
        }

        /// <summary>
        /// Why a box came back empty, in terms the operator can act on.
        /// <para/>
        /// The registry number gets its own wording because its blank is not a scan-quality
        /// problem the operator can fix by rescanning at the same settings, and telling
        /// them "the scan shows i" invites them to keep retrying something that cannot
        /// work. On these forms the number is habitually entered ON TOP of the printed
        /// "Registry No." caption — handwritten on the birth certificates, struck high by
        /// the typewriter on the marriage certificate, where "2007" lands across
        /// "Registry" and "72" sits inside "No". The two texts then share pixels, and they
        /// share ink density too (measured: 5th-percentile grey 41 for the caption against
        /// 40 for the value), so no threshold separates them. See the 2026-09-06 entry in
        /// CLAUDE.md for the full sweep that established this.
        /// </summary>
        private static string BlankIssue(FieldSpec spec, string ocrValue)
        {
            if (spec.Shape == FieldShape.Registry)
                return "Type this in from the scan — the number is written over the printed "
                     + "\"Registry No.\" caption, which no amount of re-scanning separates";

            return ocrValue.Length > 0
                ? "Nothing usable read here — the scan shows \"" + Shorten(ocrValue) + "\""
                : "Nothing read in this box";
        }

        /// <summary>The whole-page pass's words that sit inside a field region, in reading order.</summary>
        private static OcrRegionRead PageWordsIn(OcrResult page, RectangleF rect)
        {
            if (page == null || page.PageWidth == 0 || page.PageHeight == 0) return null;
            var inside = page.Words
                .Where(w =>
                {
                    float cx = (w.X + w.Width / 2f) / page.PageWidth, cy = (w.Y + w.Height / 2f) / page.PageHeight;
                    return cx >= rect.Left && cx <= rect.Right && cy >= rect.Top && cy <= rect.Bottom;
                })
                .OrderBy(w => w.X)
                .ToList();
            if (inside.Count == 0) return null;
            return new OcrRegionRead
            {
                Variant = "page",
                Text = string.Join(" ", inside.Select(w => w.Text)),
                Confidence = (int)inside.Average(w => w.Confidence),
                Words = inside
            };
        }

        private static void Judge(List<OcrRegionRead> reads, FieldSpec spec, FormLayout layout,
                                  FieldRead into, OcrRegionRead fromPage)
        {
            var all = new List<OcrRegionRead>(reads);
            if (fromPage != null) all.Add(fromPage);

            var candidates = new List<Tuple<string, int, int>>();   // value, score, engine confidence
            foreach (OcrRegionRead r in all)
            {
                string raw = Flatten(MainText(r));
                if (raw.Length == 0) continue;
                if (into.OcrValue.Length == 0) into.OcrValue = raw;

                string value = Normalise(raw, spec, layout);
                if (value.Length == 0) continue;
                int score = Score(value, spec);
                if (score <= 0) continue;
                candidates.Add(Tuple.Create(value, score, r.Confidence));
            }

            if (candidates.Count == 0)
            {
                into.Value = "";
                into.Issue = BlankIssue(spec, into.OcrValue);
                into.Confidence = 0;
                return;
            }

            // Group identical readings: the more preprocessings that agree, the more we
            // believe it. Ties break on the shape score, then on engine confidence.
            var groups = candidates
                .GroupBy(c => c.Item1, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Value = g.First().Item1,
                    Votes = g.Count(),
                    Shape = g.Max(c => c.Item2),
                    Engine = (int)g.Average(c => c.Item3)
                })
                .OrderByDescending(g => g.Shape * 2 + g.Votes * 15)
                .ThenByDescending(g => g.Engine)
                .ToList();

            var win = groups[0];

            // No two renderings agreed, the winner is short, and other renderings saw
            // something else entirely: that is noise off a damaged row, not a value. On the
            // marriage certificate's province and municipality lines — genuinely destroyed
            // print — this is the difference between reporting "lees" as the province and
            // reporting honestly that nothing could be read. A false value costs the
            // operator more than a blank: a blank asks to be filled, a wrong value has to
            // be noticed first.
            bool noConsensus = win.Votes == 1 && groups.Count >= 3 &&
                               win.Value.Length < 6 &&
                               (spec.Shape == FieldShape.Place || spec.Shape == FieldShape.Text);
            if (noConsensus)
            {
                into.Value = "";
                into.Confidence = 0;
                into.Issue = "Could not be read — the renderings disagreed completely (\"" +
                             Shorten(win.Value) + "\", \"" + Shorten(groups[1].Value) + "\")";
                return;
            }

            into.Value = win.Value;
            int agreement = Math.Min(100, 45 + win.Votes * 18);
            int blended = (int)Math.Round(agreement * 0.6 + win.Engine * 0.25 + win.Shape * 0.15);

            // CEILING: never report more confidence than the engine had in the characters
            // it actually read. Agreement between renderings looks like independent
            // evidence and is not — every rendering is derived from the SAME damaged
            // pixels, so on a bad scan they agree on the same mistake. Measured on the
            // office's marriage certificate: the husband's given name was read as
            // "OFLEERT" with engine confidence 48/38/40, and the blend reported 77% —
            // above the 70% mark, so the review grid showed the operator a green tick on a
            // name that is wrong. The same measurement on readings that ARE correct
            // ("SHELLIAN CLEAR" at 93/60/92, "GEORGE" at 87/92/93) leaves them untouched,
            // which is exactly what a ceiling should do: it costs nothing when the
            // recognition was sound and it removes the false reassurance when it was not.
            into.Confidence = Math.Min(blended, win.Engine + 8);
            if (into.Confidence > 99) into.Confidence = 99;
            if (into.Confidence < 1) into.Confidence = 1;
            if (win.Votes == 1 && groups.Count > 1)
                into.Issue = "Readings disagree (" + Shorten(groups[1].Value) + "?) — check against the scan";
        }

        /// <summary>
        /// The text of a region read, with the box's own furniture filtered out by word
        /// GEOMETRY rather than by spelling.
        /// <para/>
        /// A cell's real value is printed at one size. What comes in alongside it — the
        /// ruled underline read as letters, the clipped top of the row below, speckle in
        /// the margin — is recognised at a visibly different size and with poor
        /// confidence. Filtering on word height against the median therefore removes
        /// exactly the junk that used to win the vote ("ARTICU LO Ninraetinresin"),
        /// without a word list that would have to guess at Filipino names.
        /// <para/>
        /// Word SPACING is deliberately taken from the engine and not rebuilt from the
        /// boxes. Rejoining words whose boxes nearly touch was tried, to mend "CATAGGATAN"
        /// coming back as "CAT AGGATAN", and the boxes were then measured to check it:
        /// on this marriage certificate the REAL spaces in "OFFICE OF THE MUNICIPAL MAYOR"
        /// have gaps of 0-3px and the false split inside CATAGGATAN has a gap of 0-1px, so
        /// the two cases are not separable by geometry at all. The same boxes also report
        /// negative gaps and zero heights on this scan. Any threshold that mends the name
        /// therefore also welds the heading into one word, which is why this is not done.
        /// </summary>
        private static string MainText(OcrRegionRead r)
        {
            if (r.Words == null || r.Words.Count < 2) return r.Text;

            List<OcrWord> usable = r.Words.Where(w => w.Confidence >= 20 && w.Height > 0).ToList();
            // Falling back to the raw text when fewer than two words survive was a real
            // defect, not a safety net: in the 1993 child-name cell the intruding hint scores
            // 9 and the name 58, so the hint is excluded here, ONE word remains, and the old
            // guard then handed back the raw text — hint included — precisely in the case
            // the filtering existed to fix. One good word is a perfectly good answer.
            if (usable.Count == 0) return r.Text;
            if (usable.Count == 1) return usable[0].Text;

            var heights = usable.Select(w => w.Height).OrderBy(h => h).ToList();
            double median = heights[heights.Count / 2];
            if (median <= 0) return r.Text;

            List<OcrWord> kept = usable
                .Where(w => w.Height >= median * 0.6 && w.Height <= median * 1.7)
                .ToList();

            // Then drop what the engine itself barely read. A cell on a skewed scan often
            // catches the printed hint from the row above, and the two are not close: in the
            // 1993 child-name cell the intruding "(First)" hint came back as "vee"/"eeey"
            // with confidence 9/0/5 beside the name at 58/79/78. Anything that far below the
            // best word in the same cell is the neighbouring row bleeding in, not part of
            // this value. The gap has to be wide, so a cell that is merely difficult
            // everywhere keeps all of its words.
            if (kept.Count > 1)
            {
                double bestConf = kept.Max(w => (double)w.Confidence);
                List<OcrWord> strong = kept
                    .Where(w => w.Confidence >= bestConf - 40 || w.Confidence >= 30)
                    .ToList();
                if (strong.Count > 0) kept = strong;
            }
            // On a poor scan the engine's word heights are all over the place, and the
            // filter then throws away the value along with the noise. If it would drop
            // more than a third of the words it is not separating anything — leave the
            // reading alone and let the shape rules and the vote decide.
            if (kept.Count == 0 || kept.Count < usable.Count * 0.66) return r.Text;

            // Reading order: group words into printed lines first, THEN read each line
            // left to right. Bucketing on MidY/height instead split single lines across
            // two buckets and silently reversed them — "DE GUZMAN" came back as
            // "GUZMAN DE", a different person's name.
            var lines = new List<List<OcrWord>>();
            foreach (OcrWord w in kept.OrderBy(x => (x.Y + x.Height / 2f)))
            {
                List<OcrWord> line = lines.Count > 0 ? lines[lines.Count - 1] : null;
                if (line != null && Math.Abs((w.Y + w.Height / 2f) - line.Average(x => (x.Y + x.Height / 2f))) < median * 0.7)
                    line.Add(w);
                else
                    lines.Add(new List<OcrWord> { w });
            }
            return string.Join(" ", lines.SelectMany(line => line.OrderBy(w => w.X))
                                         .Select(w => w.Text));
        }

        /// <summary>Collapse a region's lines into one string and drop the form's ruling.</summary>
        private static string Flatten(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string s = text.Replace("\r", " ").Replace("\n", " ");
            s = Regex.Replace(s, @"[|_}{\[\]~^`\\]+", " ");        // ruled lines and box edges
            s = Regex.Replace(s, @"[‘’“”""']+", " ");
            s = Regex.Replace(s, @"\.{2,}", " ");                  // dotted leaders
            s = Regex.Replace(s, @"\s{2,}", " ");
            return s.Trim(' ', '.', ',', ';', ':', '-', '=', '*');
        }

        /// <summary>
        /// Turn a raw reading into the value the field should hold, or "" if it must not be
        /// accepted. Label text is rejected here: the printed hints sit inside these value
        /// rows, and a hint saved as a value is a false record.
        /// </summary>
        private static string Normalise(string raw, FieldSpec spec, FormLayout layout)
        {
            if (spec.Shape == FieldShape.Choice) return MatchChoice(raw, spec.Choices);

            string s = raw;
            // Drop tokens that are printed form text rather than data.
            var kept = new List<string>();
            foreach (string token in s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string bare = Regex.Replace(token, @"[^A-Za-z0-9ÑñÁÉÍÓÚáéíóú'\-/:.,]", "");
                if (bare.Length == 0) continue;
                string lower = Regex.Replace(bare, @"[^a-zA-Z]", "").ToLowerInvariant();
                // A PLACE legitimately contains the words the form also uses as labels —
                // "Tuguegarao City", "OFFICE OF THE MUNICIPAL MAYOR", "City of Manila" —
                // and stripping them turned real values into "Tuguegarao" and
                // "OF THE MAYOR". Whole label words are therefore kept for place fields;
                // a region that caught only the printed label is rejected wholesale below
                // instead, and clipped label FRAGMENTS ("cipality") are still removed.
                bool keepWholeLabelWords = spec.KeepLabelWords || spec.Shape == FieldShape.Place;
                if (!keepWholeLabelWords && lower.Length >= 3 && IsLabelWord(lower, layout)) continue;
                // A cell that clips its own printed label leaves a FRAGMENT of it behind
                // ("cipality" from "City/Municipality"): still label text, still not a value.
                if (!spec.KeepLabelWords && lower.Length >= 4 && IsLabelFragment(lower, layout)) continue;
                // The printed item number leading a value row ("11. CHECKER/DISPATCHER").
                if (kept.Count == 0 && spec.Shape != FieldShape.Name &&
                    Regex.IsMatch(bare, @"^[A-Za-z0-9]{1,3}[.,]$")) continue;
                kept.Add(bare);
            }
            s = string.Join(" ", kept).Trim();
            if (s.Length == 0) return "";

            // Kept every word, so make sure we did not just keep the printed label itself.
            if (IsOnlyLabelText(kept, layout)) return "";

            switch (spec.Shape)
            {
                case FieldShape.Name:
                    s = Regex.Replace(s, @"[^A-Za-zÑñÁÉÍÓÚáéíóú \-'\.]", " ");
                    return CleanNameCell(s);

                case FieldShape.Number:
                    Match num = Regex.Match(s, @"\d{1,5}");
                    return num.Success ? num.Value.TrimStart('0').PadLeft(1, '0') : "";

                case FieldShape.Registry:
                    Match reg = Regex.Match(s, @"((?:19|20)\d{2})\s*[-–—]\s*(\d{1,6})");
                    return reg.Success ? reg.Groups[1].Value + "-" + reg.Groups[2].Value : "";

                case FieldShape.Date:
                    return NormaliseDate(s);

                case FieldShape.Time:
                    Match clockMatch = Regex.Match(s, @"(\d{1,2})\s*[:.]\s*(\d{2})\s*([AaPp])?");
                    if (!clockMatch.Success) return "";
                    string clock = int.Parse(clockMatch.Groups[1].Value) + ":" + clockMatch.Groups[2].Value;
                    return clockMatch.Groups[3].Success
                        ? clock + " " + clockMatch.Groups[3].Value.ToUpperInvariant() + "M" : clock;

                case FieldShape.Place:
                    s = Regex.Replace(s, @"[^A-Za-zÑñÁÉÍÓÚáéíóú0-9 ,\.\-/#]", " ");
                    s = Regex.Replace(s, @"\s{2,}", " ").Trim(' ', ',', '.', '-');
                    return s.Count(char.IsLetterOrDigit) >= 3 ? s : "";

                default:
                    s = Regex.Replace(s, @"[^A-Za-zÑñÁÉÍÓÚáéíóú0-9 ,\.\-/]", " ");
                    s = Regex.Replace(s, @"\s{2,}", " ").Trim(' ', ',', '.', '-');
                    // A lone character trailing a text value is the cell's ruled edge or a
                    // tick from the next column read as a letter, not part of the word:
                    // the father's occupation came back as "Bagger 2". Names are excluded
                    // from this (an initial is a real part of a name), which is why it sits
                    // in the general text branch and not in CleanNameCell.
                    s = Regex.Replace(s, @"\s+[A-Za-z0-9]$", "").Trim();
                    if (s.Count(char.IsLetter) < 2) return "";
                    string canonical = SnapToVocabulary(s, spec.Choices);
                    return canonical ?? s;
            }
        }

        // Name particles that legitimately stand alone inside a surname cell. Everything
        // else that short is a fragment of a word the engine split, or noise.
        private static readonly string[] NameParticles =
        {
            "de", "del", "dela", "delos", "delas", "di", "da", "san", "sta", "sto",
            "y", "jr", "sr", "ii", "iii", "iv", "st", "mc", "na", "ng", "el", "la", "le"
        };

        /// <summary>
        /// Tidy one name cell.
        /// <para/>
        /// Two things happen that a plain trim cannot fix. First, a cell whose value is in
        /// capitals collects noise in mixed case (the ruled line, the row below); when the
        /// cell is clearly upper-case, mixed-case tokens are not part of the name. Second,
        /// the engine sometimes splits one printed word ("ARTICULO" as "ARTICU LO"), so a
        /// stray one- or two-letter fragment is glued back onto the token before it —
        /// unless it is a real name particle such as "DE" in "DE GUZMAN", which must stay
        /// a separate word.
        /// </summary>
        private static string CleanNameCell(string s)
        {
            var tokens = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Count(char.IsLetter) >= 1)
                .ToList();
            if (tokens.Count == 0) return "";

            int upper = tokens.Sum(t => t.Count(char.IsUpper));
            int lower = tokens.Sum(t => t.Count(char.IsLower));
            if (upper + lower >= 4 && upper >= (upper + lower) * 0.7)
                tokens = tokens.Where(t =>
                {
                    int u = t.Count(char.IsUpper), l = t.Count(char.IsLower);
                    return u + l == 0 || u >= (u + l) * 0.7;
                }).ToList();

            var joined = new List<string>();
            foreach (string token in tokens)
            {
                string bare = token.Trim('.', '-', '\'');
                if (bare.Length == 0) continue;
                bool particle = NameParticles.Contains(bare.ToLowerInvariant());
                if (bare.Count(char.IsLetter) <= 2 && !particle && joined.Count > 0)
                    joined[joined.Count - 1] += bare;      // a split word, put back together
                else
                    joined.Add(bare);
            }
            joined = joined.Where(t => t.Count(char.IsLetter) >= 2 ||
                                       NameParticles.Contains(t.ToLowerInvariant())).ToList();
            return string.Join(" ", joined).Trim();
        }

        /// <summary>
        /// True when every word of a reading is printed form text — i.e. the region caught
        /// the label and nothing else. "City" inside "Tuguegarao City" is a value; "City
        /// Municipality" on its own is the label above the value.
        /// </summary>
        private static bool IsOnlyLabelText(List<string> kept, FormLayout layout)
        {
            if (kept.Count == 0) return false;

            int judged = 0;
            foreach (string token in kept)
            {
                // A number is data, never a printed label: "2722" in "2722 grams" and the
                // digits of a registry number are the VALUE. Treating them as undecidable
                // and then concluding "everything decidable was a label" threw away every
                // numeric field on the page — the weight, both parents' ages, the age at
                // death and the registry number all came back blank.
                if (token.Any(char.IsDigit)) return false;

                string lower = Regex.Replace(token, @"[^a-zA-Z]", "").ToLowerInvariant();
                if (lower.Length < 3) continue;              // punctuation/initials decide nothing
                judged++;
                if (!IsLabelWord(lower, layout) && !IsLabelFragment(lower, layout)) return false;
            }
            // Nothing was actually assessed, so nothing was shown to be label text.
            return judged > 0;
        }

        private static bool IsLabelWord(string lower, FormLayout layout)
        {
            if (DocLayouts.CommonLabelWords.Contains(lower)) return true;
            return layout.LabelWords != null && layout.LabelWords.Contains(lower);
        }

        /// <summary>
        /// True when the token is a tail or head of one of the form's printed labels — what
        /// is left when a region clips the label rather than excluding it.
        /// </summary>
        private static bool IsLabelFragment(string lower, FormLayout layout)
        {
            foreach (string label in DocLayouts.CommonLabelWords)
            {
                if (label.Length <= lower.Length) continue;
                if (label.EndsWith(lower, StringComparison.Ordinal) ||
                    label.StartsWith(lower, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Normalise a date ONLY when the reading is unambiguous. A named month fixes the
        /// order, so "18 June 2005" and "June 18, 2005" are both safe; "03/04/1999" is
        /// not, and is handed to the operator as it was printed rather than guessed into
        /// the wrong month.
        /// </summary>
        private static string NormaliseDate(string s)
        {
            string text = Regex.Replace(s, @"(\d)(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s{2,}", " ").Trim();

            // Separator may be a space, a hyphen, a dot or a slash: the certification
            // blocks on these forms are typed rather than handwritten, and an office
            // typing "13-Jun-18" means exactly what one writing "13 June 2018" means.
            // The YEAR may be two digits for the same reason (see ExpandYear).
            const string sep = @"[\s\-\./]+";
            Match m = Regex.Match(text, @"\b(\d{1,2})" + sep + @"([A-Za-z]{3,12})\.?([\-\./]|,?\s+)(\d{2}|\d{4})\b");
            if (!m.Success)
            {
                Match m2 = Regex.Match(text, @"\b([A-Za-z]{3,12})\.?" + sep + @"(\d{1,2})([\-\./]|,?\s+)(\d{2}|\d{4})\b");
                if (m2.Success)
                {
                    string month2 = NearestMonth(m2.Groups[1].Value);
                    string year2 = ExpandYear(m2.Groups[4].Value, m2.Groups[3].Value);
                    if (month2 == null || year2 == null) return "";
                    return Compose(int.Parse(m2.Groups[2].Value), month2, year2);
                }
                // Numeric-only: keep it visible but do not decide which number is the month.
                Match m3 = Regex.Match(text, @"\b\d{1,2}[/\-\.]\d{1,2}[/\-\.](?:19|20)?\d{2}\b");
                return m3.Success ? m3.Value : "";
            }
            string month = NearestMonth(m.Groups[2].Value);
            string year = ExpandYear(m.Groups[4].Value, m.Groups[3].Value);
            if (month == null || year == null) return "";
            return Compose(int.Parse(m.Groups[1].Value), month, year);
        }

        /// <summary>
        /// Turn the year as printed into four digits. A four-digit year is taken as it is,
        /// and only 19xx/20xx are accepted — "13 June 3018" is a misread, not a date.
        /// <para/>
        /// A TWO-digit year is expanded into the hundred years ending this one, which is
        /// the same window the typist's own software used when it printed "13-Jun-18". It
        /// is a convention rather than a fact, so it is applied ONLY where a named month
        /// has already fixed the order — a fully numeric date is still handed to the
        /// operator exactly as printed rather than guessed at.
        /// </summary>
        private static string ExpandYear(string digits, string separator)
        {
            if (digits.Length == 4)
                return Regex.IsMatch(digits, @"^(?:19|20)\d{2}$") ? digits : null;

            // A TWO-digit year is only accepted in the compact hyphenated form an office
            // machine prints - "13-Jun-18". Written out, a certificate always states the
            // year in full ("February 28, 2007"), so a two-digit tail after a comma or a
            // space is not an abbreviation - it is the reading falling apart.
            //
            // MEASURED, which is why the rule exists: on the marriage sample the licence
            // date reads "February 25" plus debris, and the looser rule turned that into
            // 2000-02-25 at 76% WITH A GREEN TICK - a date that appears nowhere on the
            // certificate, presented as read off it. Same fabrication CorrectDate was
            // fixed for on 2026-09-04.
            if (!Regex.IsMatch(separator ?? "", @"^[\-\./]$")) return null;

            int yy = int.Parse(digits);
            int thisYear = DateTime.Today.Year;
            int century = thisYear / 100 * 100;
            int candidate = century + yy;
            if (candidate > thisYear) candidate -= 100;
            return candidate.ToString(CultureInfo.InvariantCulture);
        }

        private static string Compose(int day, string month, string year)
        {
            DateTime d;
            if (DateTime.TryParseExact(day + " " + month + " " + year, "d MMMM yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.ToString("yyyy-MM-dd");
            return "";
        }

        private static readonly string[] Months =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        /// <summary>
        /// Snap a mangled month onto a real one, but only within two edits — "Oevober" is
        /// October, "Segayan" is not a month at all and must not become September.
        /// </summary>
        private static string NearestMonth(string word)
        {
            string w = Regex.Replace(word, @"[^A-Za-z]", "");
            if (w.Length < 3) return null;
            string best = null; int bestD = int.MaxValue, secondD = int.MaxValue;
            foreach (string m in Months)
            {
                int d = Math.Min(
                    Distance(w.ToLowerInvariant(), m.ToLowerInvariant()),
                    Distance(w.ToLowerInvariant(), m.Substring(0, 3).ToLowerInvariant()));
                if (d < bestD) { secondD = bestD; bestD = d; best = m; }
                else if (d < secondD) secondD = d;
            }
            // A long month name can absorb more damage and still be unmistakable:
            // "repruery" is February by three edits and is nothing else by fewer. Short
            // readings stay strict, and a tie is refused outright — there is no safe way to
            // choose between two months on a certificate.
            int allowed = w.Length <= 4 ? 1 : w.Length <= 6 ? 2 : 3;
            if (best == null || bestD > allowed || secondD <= bestD) return null;
            return best;
        }

        /// <summary>
        /// Which option a choice row selected. Two ways are accepted: the option's own word
        /// is typed in the cell, or the option is TICKED — an x/X immediately left of the
        /// printed option (how the 1993 forms record sex, type of birth and attendant).
        /// </summary>
        private static string MatchChoice(string raw, string[] choices)
        {
            if (choices == null || choices.Length == 0) return "";
            string text = Regex.Replace(raw, @"[^A-Za-z0-9 ]", " ");
            text = Regex.Replace(text, @"\s{2,}", " ").Trim();
            if (text.Length == 0) return "";

            // A tick: "x 1 Male", "xx1 Physician", "X 5 Others".
            foreach (string choice in choices)
            {
                var tick = new Regex(@"\bx{1,2}\s*\d?\s*" + Regex.Escape(choice.Substring(0, Math.Min(4, choice.Length))),
                    RegexOptions.IgnoreCase);
                if (tick.IsMatch(text)) return choice;
            }

            // Typed in: the closest option to any token, within one or two edits.
            foreach (string token in text.Split(' '))
            {
                string t = Regex.Replace(token, @"[^A-Za-z]", "");
                if (t.Length < 4) continue;
                string best = null; int bestD = int.MaxValue;
                foreach (string choice in choices)
                {
                    int d = Distance(t.ToLowerInvariant(), choice.ToLowerInvariant());
                    if (d < bestD) { bestD = d; best = choice; }
                }
                if (best != null && bestD <= (best.Length <= 5 ? 1 : 2)) return best;
            }
            return "";
        }

        /// <summary>
        /// Snap a reading onto one of the field's canonical spellings, comparing the whole
        /// reading and each of its words. The allowance grows with the length of the
        /// canonical value but never past a fifth of it, so "Catholie" becomes "Catholic"
        /// while "cra" and "Vilagice" stay exactly as they were read.
        /// </summary>
        private static string SnapToVocabulary(string value, string[] vocabulary)
        {
            if (vocabulary == null || vocabulary.Length == 0) return null;
            string flat = Regex.Replace(value, @"\s+", " ").Trim();
            var probes = new List<string> { flat, Regex.Replace(flat, @"\s+", "") };
            probes.AddRange(flat.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            string best = null; int bestD = int.MaxValue;
            foreach (string candidate in vocabulary)
            {
                string target = candidate.ToLowerInvariant();
                // Roughly a quarter of the word, capped. At length/5 an eight-letter word
                // like "Catholic" was allowed a single edit, so a reading missing its last
                // two characters could never reach it.
                int allowance = Math.Max(1, Math.Min(3, candidate.Length / 4));
                foreach (string probe in probes)
                {
                    string p = Regex.Replace(probe.ToLowerInvariant(), @"[^a-z]", "");
                    if (p.Length < 3) continue;
                    int d = Distance(p, Regex.Replace(target, @"[^a-z]", ""));
                    if (d <= allowance && d < bestD) { bestD = d; best = candidate; }
                }
            }
            return best;
        }

        /// <summary>How well a value fits the shape its field expects (0 = reject).</summary>
        private static int Score(string value, FieldSpec spec)
        {
            if (value.Length == 0) return 0;
            int letters = value.Count(char.IsLetter);
            int digits = value.Count(char.IsDigit);
            int tokens = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

            switch (spec.Shape)
            {
                case FieldShape.Name:
                    if (letters < 2 || tokens > 5) return 0;
                    if (digits > 0) return 0;
                    // Concise wins. A name cell holds one to three words; every extra word
                    // is the row below or the ruled line, and rewarding length is what let
                    // that junk beat the real reading.
                    return 85 - Math.Max(0, tokens - 3) * 20 - JunkTokens(value) * 25;

                case FieldShape.Number:
                    if (digits == 0) return 0;
                    if (!ValidNumber(spec.Key, value)) return 0;
                    return 80;

                case FieldShape.Registry:
                    return Regex.IsMatch(value, @"^(?:19|20)\d{2}-\d{1,6}$") ? 90 : 0;

                case FieldShape.Date:
                    return Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}$") ? 95 : 45;

                case FieldShape.Time:
                    return 70;

                case FieldShape.Choice:
                    return 90;

                case FieldShape.Place:
                    if (value.Count(char.IsLetterOrDigit) < 3) return 0;
                    return 75 - Math.Max(0, tokens - 6) * 8 - JunkTokens(value) * 20;

                default:
                    if (letters < 2) return 0;
                    return 75 - Math.Max(0, tokens - 3) * 10 - JunkTokens(value) * 25;
            }
        }

        /// <summary>Range checks for the numeric fields these forms carry.</summary>
        private static bool ValidNumber(string key, string value)
        {
            int n;
            if (!int.TryParse(value, out n)) return false;
            if (key == "Weight") return n >= 300 && n <= 8000;
            if (key.EndsWith("Age")) return n >= 0 && n <= 130;
            if (key.StartsWith("Children")) return n >= 0 && n <= 30;
            if (key == "DobDay") return n >= 1 && n <= 31;
            if (key == "DobYear") return n >= 1900 && n <= DateTime.Today.Year;
            return true;
        }

        /// <summary>
        /// How many tokens of a reading cannot be words: no vowel at all, or a letter
        /// repeated three times running. These come from ruled lines and speckle, and a
        /// reading carrying them should lose to one that does not.
        /// </summary>
        private static int JunkTokens(string value)
        {
            int junk = 0;
            foreach (string token in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string letters = Regex.Replace(token, @"[^A-Za-z]", "");
                if (letters.Length >= 4 && !Regex.IsMatch(letters, @"[AEIOUYaeiouy]")) { junk++; continue; }
                if (Regex.IsMatch(letters, @"(.)")) junk++;
            }
            return junk;
        }

        private static string Shorten(string s)
        {
            s = (s ?? "").Trim();
            return s.Length <= 28 ? s : s.Substring(0, 27) + "…";
        }

        /// <summary>Levenshtein distance — used only to snap onto a KNOWN word, never to invent one.</summary>
        internal static int Distance(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                Array.Copy(cur, prev, cur.Length);
            }
            return prev[b.Length];
        }
    }
}
