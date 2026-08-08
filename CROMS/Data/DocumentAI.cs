using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>The civil-registry document classes CROMS can recognise.</summary>
    public enum DocKind { Unknown, Birth, Marriage, Death }

    /// <summary>One extracted field: its canonical key, human label, value and whether it needs review.</summary>
    public class DocField
    {
        public string Key;
        public string Label;
        public string Value;
        public bool Uncertain;   // low OCR confidence or empty → highlight for the operator
        public DocField(string key, string label, string value, bool uncertain)
        { Key = key; Label = label; Value = value; Uncertain = uncertain; }
    }

    /// <summary>Result of analysing one document.</summary>
    public class DocAiResult
    {
        public DocKind Kind = DocKind.Unknown;
        public int ClassifyConfidence;   // 0-100, how sure of the document type
        public int OcrConfidence;        // 0-100, Tesseract mean confidence ("Recognition")
        public string RawText = "";
        public List<DocField> Fields = new List<DocField>();
        public string Error;

        public int ExtractedCount => Fields.Count(f => !string.IsNullOrWhiteSpace(f.Value));
        public int MissingCount => Fields.Count(f => string.IsNullOrWhiteSpace(f.Value));

        /// <summary>Extracted values keyed by canonical key, for auto-fill.</summary>
        public Dictionary<string, string> Map()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Fields)
                if (!string.IsNullOrWhiteSpace(f.Value)) d[f.Key] = f.Value.Trim();
            return d;
        }
    }

    /// <summary>
    /// Offline document-recognition engine: Tesseract OCR + rule/label-based
    /// classification and field extraction (no cloud service — civil-registry PII
    /// never leaves the machine). Classification identifies the document by the
    /// labels/headings it contains (not fixed coordinates), so it tolerates PSA form
    /// revisions and slight layout changes. Extraction reads a field's LABEL and pulls
    /// the value next to / below it. New document types are added by registering a
    /// <see cref="DocProfile"/> — the engine is deliberately modular so CENOMAR, court
    /// orders, etc. can be added later without touching the pipeline.
    /// </summary>
    public static class DocumentAI
    {
        public static bool IsAvailable() => OcrService.IsAvailable();

        // ---- classification profiles (extensible) --------------------------
        private class DocProfile
        {
            public DocKind Kind;
            public string[] StrongMarkers;   // near-definitive phrases
            public string[] WeakMarkers;     // supporting phrases
        }

        private static readonly List<DocProfile> Profiles = new List<DocProfile>
        {
            new DocProfile {
                Kind = DocKind.Birth,
                StrongMarkers = new[] { "certificate of live birth", "live birth", "municipal form no. 102", "form no. 102" },
                WeakMarkers   = new[] { "birth registration", "child", "date of birth", "place of birth", "maiden" }
            },
            new DocProfile {
                Kind = DocKind.Marriage,
                StrongMarkers = new[] { "certificate of marriage", "municipal form no. 97", "form no. 97" },
                WeakMarkers   = new[] { "marriage registration", "husband", "wife", "solemnizing", "date of marriage" }
            },
            new DocProfile {
                Kind = DocKind.Death,
                StrongMarkers = new[] { "certificate of death", "municipal form no. 103", "form no. 103" },
                WeakMarkers   = new[] { "death registration", "deceased", "cause of death", "date of death" }
            },
        };

        /// <summary>Load a supported image (JPG/JPEG/PNG). PDF is not yet supported.</summary>
        public static Bitmap LoadImage(string path)
        {
            string ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            if (ext == ".pdf")
                throw new NotSupportedException(
                    "PDF upload is not supported yet. Please upload an image (JPG, JPEG or PNG) " +
                    "of the document — e.g. a photo or scan saved as an image.");
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".tif" && ext != ".tiff")
                throw new NotSupportedException("Unsupported file type. Upload a JPG, JPEG or PNG image.");

            using (var tmp = new Bitmap(path))
            {
                // Cap very large scans so OCR preprocessing stays responsive.
                const int max = 2200;
                if (tmp.Width <= max && tmp.Height <= max) return new Bitmap(tmp);
                double s = Math.Min((double)max / tmp.Width, (double)max / tmp.Height);
                var outBmp = new Bitmap((int)(tmp.Width * s), (int)(tmp.Height * s));
                using (var g = Graphics.FromImage(outBmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(tmp, 0, 0, outBmp.Width, outBmp.Height);
                }
                return outBmp;
            }
        }

        /// <summary>OCR the image, classify it, and extract the fields for its type.</summary>
        public static DocAiResult Analyze(Bitmap image)
        {
            var result = new DocAiResult();
            if (!IsAvailable())
            {
                result.Error = "OCR engine (Tesseract 'eng' language data) not found.";
                return result;
            }

            OcrResult ocr = OcrService.Run(image);
            result.RawText = ocr.Text ?? "";
            result.OcrConfidence = ocr.Confidence;

            string lower = result.RawText.ToLowerInvariant();
            Classify(lower, result);

            if (result.Kind == DocKind.Birth)
                result.Fields = ExtractBirth(result.RawText, ocr.Confidence);
            else if (result.Kind == DocKind.Marriage)
                result.Fields = ExtractMarriage(result.RawText, ocr.Confidence);
            else if (result.Kind == DocKind.Death)
                result.Fields = ExtractDeath(result.RawText, ocr.Confidence);

            return result;
        }

        private static void Classify(string lower, DocAiResult result)
        {
            DocProfile best = null; int bestScore = 0;
            foreach (var p in Profiles)
            {
                int score = 0;
                foreach (var m in p.StrongMarkers) if (lower.Contains(m)) score += 5;
                foreach (var m in p.WeakMarkers) if (lower.Contains(m)) score += 1;
                if (score > bestScore) { bestScore = score; best = p; }
            }

            if (best == null || bestScore == 0)
            {
                result.Kind = DocKind.Unknown;
                result.ClassifyConfidence = 0;
                return;
            }

            result.Kind = best.Kind;
            // Map the marker score to a confidence %: a strong marker alone ≈ 85, more markers → higher.
            result.ClassifyConfidence = Math.Min(99, 55 + bestScore * 6);
        }

        // ---- Birth Certificate extraction (PSA Municipal Form 102) ---------
        // The COLB is a fixed table: each numbered field's VALUE sits on the line
        // below its label, prefixed by the section letter (C/H/I/L/D, M/O/T/H/E/R,
        // F/A/T/H/E/R) that OCR reads off the vertical band. Child / Mother / Father
        // names come as three cells (First | Middle | Last). We anchor on the numbered
        // labels and section keywords, strip the stray prefixes, and split names into
        // cells — far more accurate on the real form than a flat label search.
        private static List<DocField> ExtractBirth(string text, int ocrConf)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string[] lines = text.Replace("\r", "")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            bool low = ocrConf < 90;

            // --- names via numbered section anchors ---
            var (cf, cm, cl) = SplitNameCells(NameAfter(lines, FindIdx(lines, @"\b1\s*\.?\s*NAME")));
            var (mf, mm, ml) = SplitNameCells(NameAfter(lines, FindIdx(lines, @"MAIDEN")));
            var (ff, fm, fl) = SplitNameCells(NameAfter(lines, FindIdx(lines, @"\b14\s*\.?\s*NAME")));

            string sex = Regex.IsMatch(text, @"\bFemale\b", IC) ? "Female"
                       : Regex.IsMatch(text, @"\bMale\b", IC) ? "Male" : "";

            // Date of birth as "12 June 2018" (day month-name year).
            string dob = FindDmy(text);
            // Time of birth: "...born alive at 1:40 PM..."
            string tob = MatchRegex(text, @"at\s*(\d{1,2}[:.]\d{2}\s*[AaPp]\.?\s*[Mm])");

            string place = StripPrefix(LineAfter(lines, FindIdx(lines, @"P.?ACE\s+OF")));
            string weight = MatchRegex(text, @"(\d{3,5})\s*gram");

            // Type of birth: read the VALUE row (the one carrying the weight, e.g.
            // "Single _ Third 2722 grams") — NOT the whole text, whose label line
            // "(Single, Twin, Triplet, etc.)" lists every option and would misfire.
            string valueRow = lines.FirstOrDefault(l => Regex.IsMatch(l, @"\d{3,5}\s*gram", IC)) ?? "";
            string birthType = Regex.IsMatch(valueRow, @"\bTwin\b", IC) ? "Twin"
                             : Regex.IsMatch(valueRow, @"\bTriplet\b", IC) ? "Triplet"
                             : Regex.IsMatch(valueRow, @"\bQuadruplet\b", IC) ? "Quadruplet"
                             : Regex.IsMatch(valueRow, @"\bSingle\b", IC) ? "Single" : "";

            string nationality = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            // Split the "Hospital, Municipality, Province" place text into its three cells,
            // so each drops into the matching Place-of-Birth combo on the form.
            string[] pp = place.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            string placeHosp = pp.Length > 0 ? pp[0] : "";
            string placeMuni = pp.Length > 1 ? pp[1] : "";
            string placeProv = pp.Length > 2 ? pp[2] : "";

            // Religion (applied to both parents) + each parent's occupation (block scan).
            string religion = ReligionOf(text);
            int miMother = FindIdx(lines, @"MAIDEN");
            int miFather = FindIdx(lines, @"\b14\s*\.?\s*NAME");
            string motherOcc = OccupationIn(lines, miMother, miFather >= 0 ? miFather : lines.Length);
            string fatherOcc = OccupationIn(lines, miFather, lines.Length);

            // Registry no is usually handwritten → OCR often can't read it (leave blank).
            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            string informant = StripPrefix(LineAfter(lines, FindIdx(lines, @"Name in Print"))); // best effort

            var f = new List<DocField>
            {
                new DocField("RegistryNo",       "Registry Number",     regNo,          low || regNo == ""),
                new DocField("ChildFirst",       "Child First Name",    cf,             low || cf == ""),
                new DocField("ChildMiddle",       "Child Middle Name",  cm,             low || cm == ""),
                new DocField("ChildLast",        "Child Last Name",     cl,             low || cl == ""),
                new DocField("Sex",              "Sex",                 sex,            sex == ""),
                new DocField("DateOfBirth",      "Date of Birth",       dob,            low || dob == ""),
                new DocField("TimeOfBirth",      "Time of Birth",       tob,            low || tob == ""),
                new DocField("PlaceOfBirth",     "Place of Birth",      place,          low || place == ""),
                new DocField("PlaceHospital",    "Hospital / Facility", placeHosp,      low || placeHosp == ""),
                new DocField("PlaceMunicipality","Municipality",        placeMuni,      low || placeMuni == ""),
                new DocField("PlaceProvince",    "Province",            placeProv,      low || placeProv == ""),
                new DocField("TypeOfBirth",      "Type of Birth",       birthType,      birthType == ""),
                new DocField("Weight",           "Weight at Birth (g)", weight,         low || weight == ""),
                new DocField("MotherFirst",      "Mother First Name",   mf,             low || mf == ""),
                new DocField("MotherMiddle",     "Mother Middle Name",  mm,             low || mm == ""),
                new DocField("MotherLast",       "Mother Maiden Last",  ml,             low || ml == ""),
                new DocField("MotherOccupation", "Mother Occupation",   motherOcc,      low || motherOcc == ""),
                new DocField("FatherFirst",      "Father First Name",   ff,             low || ff == ""),
                new DocField("FatherMiddle",     "Father Middle Name",  fm,             low || fm == ""),
                new DocField("FatherLast",       "Father Last Name",    fl,             low || fl == ""),
                new DocField("FatherOccupation", "Father Occupation",   fatherOcc,      low || fatherOcc == ""),
                new DocField("Religion",         "Religion (parents)",  religion,       low || religion == ""),
                new DocField("Nationality",      "Nationality",         nationality,    low || nationality == ""),
                new DocField("Informant",        "Informant",           informant,      low || informant == ""),
            };
            return f;
        }

        // ---- Marriage Certificate extraction (PSA Municipal Form 97) -------
        // The COM lays HUSBAND and WIFE in two columns; each spouse's name is three
        // cells (First | Middle | Last) on the line below the section heading. We anchor
        // on the HUSBAND / WIFE headings, the date, place and solemnizing officer labels.
        private static List<DocField> ExtractMarriage(string text, int ocrConf)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string[] lines = text.Replace("\r", "")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            bool low = ocrConf < 90;

            var (hf, hm, hl) = SplitNameCells(NameAfter(lines, FindIdx(lines, @"\bHUSBAND\b")));
            var (wf, wm, wl) = SplitNameCells(NameAfter(lines, FindIdx(lines, @"\bWIFE\b")));

            string date = FindDmy(text);
            string place = StripPrefix(LineAfter(lines, FindIdx(lines, @"P.?ACE\s+OF\s+MARRIAGE")));
            string solemn = StripPrefix(LineAfter(lines, FindIdx(lines, @"[Ss]olemniz")));
            string nat = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            return new List<DocField>
            {
                new DocField("RegistryNo",      "Registry Number",      regNo,  low || regNo == ""),
                new DocField("HusbandFirst",    "Husband First Name",   hf,     low || hf == ""),
                new DocField("HusbandMiddle",   "Husband Middle Name",  hm,     low || hm == ""),
                new DocField("HusbandLast",     "Husband Last Name",    hl,     low || hl == ""),
                new DocField("WifeFirst",       "Wife First Name",      wf,     low || wf == ""),
                new DocField("WifeMiddle",      "Wife Middle Name",     wm,     low || wm == ""),
                new DocField("WifeLast",        "Wife Last Name",       wl,     low || wl == ""),
                new DocField("DateOfMarriage",  "Date of Marriage",     date,   low || date == ""),
                new DocField("PlaceOfMarriage", "Place of Marriage",    place,  low || place == ""),
                new DocField("Solemnizer",      "Solemnizing Officer",  solemn, low || solemn == ""),
                new DocField("Nationality",     "Citizenship",          nat,    low || nat == ""),
            };
        }

        // ---- Death Certificate extraction (PSA Municipal Form 103) ---------
        // Deceased name is three cells (First | Middle | Last) below the "NAME" heading.
        // Sex / civil status / age / date / place read off their labels and keywords.
        private static List<DocField> ExtractDeath(string text, int ocrConf)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string[] lines = text.Replace("\r", "")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            bool low = ocrConf < 90;

            int ni = FindIdx(lines, @"NAME\s+OF\s+DECEASED");
            if (ni < 0) ni = FindIdx(lines, @"\b1\s*\.?\s*NAME");
            var (df, dm, dl) = SplitNameCells(NameAfter(lines, ni));
            string fullName = Clean(string.Join(" ", new[] { df, dm, dl }.Where(s => s.Length > 0)));

            string sex = Regex.IsMatch(text, @"\bFemale\b", IC) ? "Female"
                       : Regex.IsMatch(text, @"\bMale\b", IC) ? "Male" : "";

            string civil = Regex.IsMatch(text, @"\bWidow", IC) ? "Widowed"
                         : Regex.IsMatch(text, @"\bMarried\b", IC) ? "Married"
                         : Regex.IsMatch(text, @"\bSeparated\b", IC) ? "Separated"
                         : Regex.IsMatch(text, @"\bDivorced\b", IC) ? "Divorced"
                         : Regex.IsMatch(text, @"\bAnnulled\b", IC) ? "Annulled"
                         : Regex.IsMatch(text, @"\bSingle\b", IC) ? "Single" : "";

            string age = MatchRegex(text, @"(\d{1,3})\s*(?:years|yrs?|year)");
            string dod = FindDmy(text);
            string place = StripPrefix(LineAfter(lines, FindIdx(lines, @"P.?ACE\s+OF\s+DEATH")));
            string nat = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            return new List<DocField>
            {
                new DocField("RegistryNo",     "Registry Number",      regNo,    low || regNo == ""),
                new DocField("DeceasedFirst",  "Deceased First Name",  df,       low || df == ""),
                new DocField("DeceasedMiddle", "Deceased Middle Name", dm,       low || dm == ""),
                new DocField("DeceasedLast",   "Deceased Last Name",   dl,       low || dl == ""),
                new DocField("FullName",       "Full Name",            fullName, low || fullName == ""),
                new DocField("Sex",            "Sex",                  sex,      sex == ""),
                new DocField("CivilStatus",    "Civil Status",         civil,    low || civil == ""),
                new DocField("Age",            "Age",                  age,      low || age == ""),
                new DocField("Citizenship",    "Citizenship",          nat,      low || nat == ""),
                new DocField("DateOfDeath",    "Date of Death",        dod,      low || dod == ""),
                new DocField("PlaceOfDeath",   "Place of Death",       place,    low || place == ""),
            };
        }

        /// <summary>Index of the first line matching the pattern (-1 if none).</summary>
        private static int FindIdx(string[] lines, string pattern)
        {
            for (int i = 0; i < lines.Length; i++)
                if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase)) return i;
            return -1;
        }

        private static string LineAfter(string[] lines, int idx) =>
            idx >= 0 && idx + 1 < lines.Length ? lines[idx + 1] : "";

        /// <summary>
        /// The name value below a name-label line: scans the next few lines for the
        /// first that looks like a name (≥2 alphabetic tokens after prefix stripping).
        /// </summary>
        private static string NameAfter(string[] lines, int idx)
        {
            if (idx < 0) return "";
            for (int j = idx + 1; j <= idx + 3 && j < lines.Length; j++)
            {
                string s = StripPrefix(lines[j]);
                int words = s.Split(' ').Count(t => t.Length >= 2 && t.All(char.IsLetter));
                if (words >= 2) return s;
            }
            return "";
        }

        /// <summary>Drop leading section letters / stray marks (e.g. "M ", "*_ ", "L ").</summary>
        private static string StripPrefix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string[] tok = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            while (start < tok.Length)
            {
                string t = tok[start];
                bool junk = t.Length <= 2 || !t.Any(char.IsLetter);
                if (!junk) break;
                start++;
            }
            return Clean(string.Join(" ", tok.Skip(start)));
        }

        /// <summary>Split a form-cell name ("First[ …] Middle Last") into its three cells.</summary>
        private static (string, string, string) SplitNameCells(string raw)
        {
            raw = Clean(raw);
            if (raw.Length == 0) return ("", "", "");
            string[] t = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length == 1) return (t[0], "", "");
            if (t.Length == 2) return (t[0], "", t[1]);
            // 3+ tokens: last = Last, second-to-last = Middle, the rest = First.
            string last = t[t.Length - 1];
            string mid = t[t.Length - 2];
            string first = string.Join(" ", t.Take(t.Length - 2));
            return (first, mid, last);
        }

        /// <summary>Parse a "12 June 2018" style date to yyyy-MM-dd (blank if none).</summary>
        private static string FindDmy(string text)
        {
            Match m = Regex.Match(text,
                @"\b(\d{1,2})\s+([A-Za-z]{3,9})\s+((?:19|20)\d{2})\b");
            if (!m.Success) return "";
            string cand = m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value;
            string[] fmts = { "d MMMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "dd MMM yyyy" };
            if (DateTime.TryParseExact(cand, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                return d.ToString("yyyy-MM-dd");
            return "";
        }

        // ---- extraction helpers --------------------------------------------

        /// <summary>Best-effort religion by keyword (normalised to a canonical spelling).</summary>
        private static string ReligionOf(string text)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            if (Regex.IsMatch(text, @"Roman\s+Catholic|R\.?\s*Catholic|\bCatholic\b", IC)) return "Roman Catholic";
            if (Regex.IsMatch(text, @"Iglesia\s+ni\s+Cristo|\bINC\b", IC)) return "Iglesia ni Cristo";
            if (Regex.IsMatch(text, @"Islam|Muslim", IC)) return "Islam";
            if (Regex.IsMatch(text, @"Born\s*Again", IC)) return "Born Again";
            if (Regex.IsMatch(text, @"Baptist", IC)) return "Baptist";
            if (Regex.IsMatch(text, @"Aglipay", IC)) return "Aglipayan";
            if (Regex.IsMatch(text, @"Methodist", IC)) return "Methodist";
            if (Regex.IsMatch(text, @"Adventist", IC)) return "Seventh-day Adventist";
            if (Regex.IsMatch(text, @"Protestant", IC)) return "Protestant";
            return "";
        }

        // Common occupations recognised on PSA forms (extend freely).
        private static readonly string[] Occupations =
        {
            "Housewife", "Housekeeper", "Farmer", "Fisherman", "Driver", "Teacher", "Nurse",
            "Physician", "Doctor", "Engineer", "Vendor", "Laborer", "Businessman", "Businesswoman",
            "Government Employee", "Employee", "Self-employed", "OFW", "Student", "Carpenter",
            "Merchant", "Clerk", "Accountant", "Police Officer", "Soldier", "Electrician",
            "Mechanic", "Cook", "Tailor", "Seamstress", "Security Guard", "Sales Lady",
            "Cashier", "Welder", "Plumber", "Painter", "Barber", "Midwife", "Pharmacist",
            "Lawyer", "Manager"
        };

        /// <summary>First recognised occupation within a line range (a parent's block).</summary>
        private static string OccupationIn(string[] lines, int from, int to)
        {
            if (from < 0) return "";
            from = Math.Max(0, from); to = Math.Min(lines.Length, to);
            for (int i = from; i < to; i++)
                foreach (string occ in Occupations)
                    if (Regex.IsMatch(lines[i], @"\b" + Regex.Escape(occ) + @"\b", RegexOptions.IgnoreCase))
                        return occ;
            return "";
        }

        /// <summary>
        /// Label-based lookup: find the first line containing any of the labels and
        /// return the text after the label on that line; if none, the next non-empty
        /// line. Reads meaning from the label, not a fixed position.
        /// </summary>
        private static string ValueAfter(string[] lines, params string[] labels)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string low = lines[i].ToLowerInvariant();
                foreach (string label in labels)
                {
                    int idx = low.IndexOf(label, StringComparison.Ordinal);
                    if (idx < 0) continue;
                    string after = lines[i].Substring(idx + label.Length)
                        .TrimStart(':', '.', ')', '-', ' ', '\t');
                    if (after.Length >= 2 && after.Any(char.IsLetterOrDigit)) return Clean(after);
                    if (i + 1 < lines.Length && lines[i + 1].Any(char.IsLetterOrDigit)) return Clean(lines[i + 1]);
                }
            }
            return "";
        }

        private static string MatchRegex(string text, string pattern)
        {
            Match m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        /// <summary>Split a full name into (first, middle, last); tolerant of "LAST, FIRST MIDDLE".</summary>
        private static (string, string, string) SplitName(string full)
        {
            full = Clean(full);
            if (full.Length == 0) return ("", "", "");

            if (full.Contains(","))
            {
                string[] parts = full.Split(',');
                string lastN = parts[0].Trim();
                string[] rest = parts[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string firstN = rest.Length > 0 ? rest[0] : "";
                string midN = rest.Length > 1 ? string.Join(" ", rest.Skip(1)) : "";
                return (firstN, midN, lastN);
            }

            string[] tok = full.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 1) return (tok[0], "", "");
            if (tok.Length == 2) return (tok[0], "", tok[1]);
            return (tok[0], string.Join(" ", tok.Skip(1).Take(tok.Length - 2)), tok[tok.Length - 1]);
        }

        /// <summary>Strip stray OCR punctuation/underscores from a captured value.</summary>
        private static string Clean(string s) =>
            Regex.Replace((s ?? "").Trim(' ', '_', '.', ':', '-', '|'), @"\s{2,}", " ").Trim();

        /// <summary>Best-effort date normalise to yyyy-MM-dd (blank if unparseable).</summary>
        private static string NormDate(string s)
        {
            s = Clean(s);
            if (s.Length == 0) return "";
            string[] fmts = { "MMMM d, yyyy", "MMMM dd, yyyy", "MMM d yyyy", "M/d/yyyy", "d/M/yyyy",
                              "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(s, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
                || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.ToString("yyyy-MM-dd");
            return s;   // keep raw text for the operator to correct
        }

        public static string KindName(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "Birth Certificate";
                case DocKind.Marriage: return "Marriage Certificate";
                case DocKind.Death: return "Death Certificate";
                default: return "Unknown";
            }
        }
    }
}
