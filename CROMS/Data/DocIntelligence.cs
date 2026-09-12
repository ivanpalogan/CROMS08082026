using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>What the operator has to know about one extracted value.</summary>
    public enum FieldStatus
    {
        /// <summary>Read cleanly and passed its rules.</summary>
        Ok,
        /// <summary>Read, but the recognition was weak — check it against the scan.</summary>
        Uncertain,
        /// <summary>Nothing was read. Blank is honest; it is never filled with a guess.</summary>
        Missing,
        /// <summary>A value that cannot be right (a future date of birth, a name full of digits).</summary>
        Invalid,
        /// <summary>Valid on its own but disagrees with another field on the same document.</summary>
        Conflict
    }

    /// <summary>
    /// The layer between raw extraction and the operator: it scores every field from the
    /// confidence of the words that actually produced it, applies conservative
    /// context corrections, and runs the certificate's own rules over the result.
    /// <para/>
    /// Two rules govern everything here:
    /// <list type="bullet">
    /// <item><b>Never invent.</b> A correction may only reshape characters that are
    /// already there (a zero read as an "O" inside a name), pick between spellings the
    /// office has already used, or normalise a format. An empty field stays empty — a
    /// plausible guess in a civil-registry record is worse than a blank, because a blank
    /// gets typed in and a guess gets signed.</item>
    /// <item><b>Say how sure you are.</b> Every field carries its own confidence, so a
    /// page that read at 85% overall cannot hide one field that read at 20%.</item>
    /// </list>
    /// </summary>
    public static class DocIntelligence
    {
        /// <summary>Below this a field is flagged for the operator's eye.</summary>
        public const int UncertainBelow = 70;

        /// <summary>Below this the whole document is sent to manual review.</summary>
        public const int ReviewBelow = 65;

        // ---- entry point ---------------------------------------------------

        /// <summary>
        /// Score, correct and validate every field of a finished extraction. Safe to call
        /// on an Unknown document (it just marks the result for manual review).
        /// </summary>
        public static void Enrich(DocAiResult r, OcrResult ocr)
        {
            if (r == null) return;

            foreach (DocField f in r.Fields)
            {
                if (string.IsNullOrEmpty(f.OcrValue)) f.OcrValue = f.Value ?? "";

                Rectangle region;
                int pageScore = ScoreValue(f.Value ?? "", ocr, out region);
                if (region != Rectangle.Empty) f.Region = region;

                // A value read from its own REGION keeps the confidence that read earned
                // (agreement between renderings). Scoring it by matching it back onto the
                // whole-page text would punish it for the very failure region reading was
                // added to work around: on these bordered tables the page pass never
                // produced most of these rows at all. A page match is still worth a little
                // — it is a second, independent read agreeing.
                if (f.RegionConfidence > 0)
                    f.Confidence = Math.Min(99, f.RegionConfidence + (pageScore > 0 ? 5 : 0));
                else
                    f.Confidence = pageScore;

                string fixedUp = Correct(f.Key, f.OcrValue, r.Kind);
                if (!string.Equals(fixedUp, f.OcrValue, StringComparison.Ordinal))
                {
                    f.Value = fixedUp;
                    f.Corrected = true;
                    // A corrected value is a repaired reading, not a fresh one: it never
                    // scores as high as a clean read of the same field.
                    f.Confidence = Math.Max(0, f.Confidence - 10);
                }
            }

            // Any field the page text could not locate still has a box: the region it was
            // read from. Without this the review grid could not draw the highlight for
            // exactly the fields that most need checking.
            foreach (DocField f in r.Fields)
            {
                if (f.Region != Rectangle.Empty || f.RegionNorm.IsEmpty) continue;
                if (r.PageWidth <= 0 || r.PageHeight <= 0) continue;
                f.Region = new Rectangle(
                    (int)Math.Round(f.RegionNorm.X * r.PageWidth),
                    (int)Math.Round(f.RegionNorm.Y * r.PageHeight),
                    (int)Math.Round(f.RegionNorm.Width * r.PageWidth),
                    (int)Math.Round(f.RegionNorm.Height * r.PageHeight));
            }

            Validate(r);

            foreach (DocField f in r.Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Value))
                    f.Status = FieldStatus.Missing;
                else if (f.Status != FieldStatus.Invalid && f.Status != FieldStatus.Conflict)
                    f.Status = f.Confidence < UncertainBelow ? FieldStatus.Uncertain : FieldStatus.Ok;

                // The old single flag stays truthful for anything still reading it.
                f.Uncertain = f.Status != FieldStatus.Ok;
            }

            var scored = r.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList();
            r.OverallConfidence = scored.Count == 0 ? 0 : (int)Math.Round(scored.Average(f => f.Confidence));

            r.NeedsManualReview =
                r.RescanRecommended ||
                r.Kind == DocKind.Unknown ||
                scored.Count == 0 ||
                r.OverallConfidence < ReviewBelow ||
                r.Fields.Any(f => f.Status == FieldStatus.Invalid && IsCore(f.Key, r.Kind));

            r.ReviewReason = ReviewReason(r);
        }

        private static string ReviewReason(DocAiResult r)
        {
            if (!r.NeedsManualReview) return "";
            if (r.RescanRecommended)
                return "scan quality " + r.ScanQualityScore + "% is too low for safe filing";
            if (r.Kind == DocKind.Unknown) return "document type could not be identified";
            // Worth saying FIRST when it applies: it explains the blanks the operator is
            // about to see, and it is not something correcting a field will resolve.
            if (r.LayoutRejected != null)
                return "this form's layout is not one CROMS has on file, so it was read by "
                     + "its printed labels and anything they did not reach is blank";
            var broken = r.Fields
                .Where(f => f.Status == FieldStatus.Invalid && IsCore(f.Key, r.Kind)).ToList();
            if (broken.Count > 0)
                return "a key field failed its validation rule: " +
                       string.Join("; ", broken.Select(f => f.Label + " - " + f.Issue).Take(3));
            return "overall field confidence " + r.OverallConfidence + "% is below " + ReviewBelow + "%";
        }

        /// <summary>
        /// Re-run the rules after the operator has edited the grid. Their typed value is
        /// taken at face value - it is a person reading the paper in front of them, which
        /// outranks any recognition score - so an edited field is scored 100 and only the
        /// validation rules decide whether it is still flagged. Everything else keeps the
        /// confidence it was read with.
        /// </summary>
        public static void Revalidate(DocAiResult r)
        {
            if (r == null) return;

            foreach (DocField f in r.Fields)
            {
                f.Issue = "";
                f.Status = FieldStatus.Ok;
                if (f.EditedByUser && !string.IsNullOrWhiteSpace(f.Value)) f.Confidence = 100;
            }

            Validate(r);

            foreach (DocField f in r.Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Value))
                    f.Status = FieldStatus.Missing;
                else if (f.Status != FieldStatus.Invalid && f.Status != FieldStatus.Conflict)
                    f.Status = f.Confidence < UncertainBelow ? FieldStatus.Uncertain : FieldStatus.Ok;
                f.Uncertain = f.Status != FieldStatus.Ok;
            }

            var scored = r.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList();
            r.OverallConfidence = scored.Count == 0 ? 0 : (int)Math.Round(scored.Average(f => f.Confidence));
            r.NeedsManualReview =
                r.RescanRecommended ||
                r.Kind == DocKind.Unknown ||
                scored.Count == 0 ||
                r.OverallConfidence < ReviewBelow ||
                r.Fields.Any(f => f.Status == FieldStatus.Invalid && IsCore(f.Key, r.Kind));
            r.ReviewReason = ReviewReason(r);
        }

        /// <summary>
        /// The fields that decide whether a document can go forward at all. A junk reading
        /// in one of these holds the whole scan for review; a junk reading anywhere else is
        /// flagged red in the grid for the operator to clear or correct, but does not block
        /// a document that is otherwise sound. The informant line on a real birth
        /// certificate is the case in point: it is handwritten, it frequently comes back as
        /// a piece of the form's own printed text, and it is not worth stopping a
        /// well-read record over.
        /// </summary>
        private static bool IsCore(string key, DocKind kind)
        {
            switch (kind)
            {
                case DocKind.Birth:
                    return key == "ChildFirst" || key == "ChildLast" || key == "Sex"
                        || key == "DateOfBirth" || key == "MotherFirst" || key == "MotherLast"
                        || key == "FatherFirst" || key == "FatherLast";
                case DocKind.Marriage:
                    return key == "HusbandFirst" || key == "HusbandLast"
                        || key == "WifeFirst" || key == "WifeLast" || key == "DateOfMarriage";
                case DocKind.Death:
                    return key == "DeceasedFirst" || key == "DeceasedLast" || key == "FullName"
                        || key == "DateOfDeath";
                default:
                    return false;
            }
        }

        // ---- per-field confidence -------------------------------------------

        /// <summary>
        /// Confidence for one value: the mean of the OCR words that produced it, found by
        /// matching the value's own tokens back onto the page. A value that cannot be
        /// traced to any word on the page was DERIVED rather than read (the sex tick box,
        /// "Filipino" from a keyword), so it gets a deliberately middling score instead of
        /// a flattering one.
        /// </summary>
        private static int ScoreValue(string value, OcrResult ocr, out Rectangle region)
        {
            region = Rectangle.Empty;
            if (string.IsNullOrWhiteSpace(value)) return 0;
            if (ocr == null || ocr.Words == null || ocr.Words.Count == 0) return 50;

            string[] tokens = Tokens(value);
            if (tokens.Length == 0) return 50;

            var used = new List<OcrWord>();
            var scores = new List<int>();

            foreach (string token in tokens)
            {
                OcrWord hit = ocr.Words
                    .Where(w => !used.Contains(w))
                    .FirstOrDefault(w => string.Equals(Norm(w.Text), token, StringComparison.OrdinalIgnoreCase));
                if (hit == null) continue;
                used.Add(hit);
                scores.Add(hit.Confidence);
            }

            if (scores.Count == 0) return 50;

            if (used.Count > 0)
            {
                int x1 = used.Min(w => w.X), y1 = used.Min(w => w.Y);
                int x2 = used.Max(w => w.X + w.Width), y2 = used.Max(w => w.Y + w.Height);
                region = Rectangle.FromLTRB(x1, y1, x2, y2);
            }

            int mean = (int)Math.Round(scores.Average());

            // Tokens we could not find on the page are tokens we cannot vouch for.
            double traced = (double)scores.Count / tokens.Length;
            return (int)Math.Round(mean * (0.6 + 0.4 * traced));
        }

        private static string[] Tokens(string s)
        {
            return (s ?? "").Split(new[] { ' ', ',', '/', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Norm).Where(t => t.Length > 0).ToArray();
        }

        private static string Norm(string s)
        {
            return Regex.Replace(s ?? "", @"[^A-Za-z0-9]", "");
        }

        // ---- context correction ---------------------------------------------

        private static readonly string[] Months =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        /// <summary>
        /// Repair an obvious misreading using what the field IS. Only ever reshapes
        /// characters that were read; returns the input unchanged when it has no
        /// confident repair, and never turns a blank into a value.
        /// </summary>
        public static string Correct(string key, string value, DocKind kind)
        {
            if (string.IsNullOrWhiteSpace(value)) return value ?? "";
            string v = Regex.Replace(value.Trim(), @"\s{2,}", " ");

            if (IsDateKey(key)) return CorrectDate(v);

            if (key == "Sex")
            {
                if (Regex.IsMatch(v, @"^F[a-z]{0,3}m[a-z]{0,3}l[ae]$", RegexOptions.IgnoreCase)) return "Female";
                if (Regex.IsMatch(v, @"^[MNH]a[il1]e$", RegexOptions.IgnoreCase)) return "Male";
                return v;
            }

            if (IsNumericKey(key))
            {
                var repaired = v.Select(c =>
                {
                    switch (c)
                    {
                        case 'O': case 'o': case 'Q': return '0';
                        case 'I': case 'l': case '|': return '1';
                        case 'S': return '5';
                        case 'B': return '8';
                        case 'Z': return '2';
                        case 'G': return '6';
                        default: return c;
                    }
                }).ToArray();
                string digits = new string(repaired);
                // Only accept the repair if it actually produced the shape of the field.
                // 1-6 after the dash, not 3-5 — see the validation rule below for why.
                if (key == "RegistryNo" && Regex.IsMatch(digits, @"^\d{4}\s*[-–—]\s*\d{1,6}$"))
                    return Regex.Replace(digits, @"\s*[-–—]\s*", "-");
                if (key != "RegistryNo" && Regex.IsMatch(digits, @"^\d+$")) return digits;
                return v;
            }

            if (IsNameKey(key))
            {
                var repaired = v.Select(c =>
                {
                    switch (c)
                    {
                        case '0': return 'O';
                        case '1': return 'I';
                        case '5': return 'S';
                        case '8': return 'B';
                        default: return c;
                    }
                }).ToArray();
                string name = new string(repaired).Trim(' ', '.', ',', '-', '\'');
                // A name is letters; if the repair still leaves digits, leave the original
                // alone and let validation flag it rather than half-fixing it.
                return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z \-'\.]*$") ? name : v;
            }

            if (IsLookupKey(key, out string category))
            {
                string snapped = SnapToKnown(category, v);
                if (snapped != null) return snapped;
            }

            return v;
        }

        /// <summary>Normalise a date, repairing a mangled month name on the way.</summary>
        private static string CorrectDate(string v)
        {
            if (DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime iso))
                return iso.ToString("yyyy-MM-dd");

            Match m = Regex.Match(v, @"\b(\d{1,2})(?:st|nd|rd|th)?\s+([A-Za-z]{3,12})\s+((?:19|20)\d{2})\b",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string month = NearestMonth(m.Groups[2].Value);
                if (month != null &&
                    DateTime.TryParseExact(m.Groups[1].Value + " " + month + " " + m.Groups[3].Value,
                        new[] { "d MMMM yyyy", "dd MMMM yyyy" }, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime d))
                    return d.ToString("yyyy-MM-dd");
            }

            // Last resort, and deliberately fenced. DateTime.TryParse FILLS IN whatever the
            // string leaves out from TODAY'S date, so a field that read only "February"
            // came back as the 1st of February of the current year — a date that appears
            // nowhere on the certificate, written into a civil-registry record as if it
            // had been read off the paper. A parse is only accepted here when the text
            // itself carries a four-digit year and the parse agrees with it.
            Match year = Regex.Match(v, @"(19|20)\d{2}");
            if (year.Success &&
                DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime any) &&
                any.Year.ToString(CultureInfo.InvariantCulture) == year.Value)
                return any.ToString("yyyy-MM-dd");

            return v;   // unparseable: keep it visible and let validation flag it
        }

        /// <summary>The month a mangled word was meant to be, or null when it is too far off.</summary>
        private static string NearestMonth(string word)
        {
            string best = null; int bestDist = int.MaxValue;
            foreach (string m in Months)
            {
                int d = Distance(word.ToLowerInvariant(), m.ToLowerInvariant());
                if (d < bestDist) { bestDist = d; best = m; }
            }
            // Two edits on a month name is a misreading; three is a different word.
            return bestDist <= 2 ? best : null;
        }

        /// <summary>
        /// Snap a value onto a spelling the office has already used, when it is one or two
        /// characters away. This is how a corrected place name spreads: the operator fixes
        /// "Pefiablanca" once, the Learning Library keeps "Peñablanca", and the next scan
        /// of the same barangay lands on it. A larger distance is a different place, not a
        /// misreading, and is left alone.
        /// </summary>
        private static string SnapToKnown(string category, string value)
        {
            List<string> known;
            try { known = LearningLibrary.Suggest(category, "", 500); }
            catch { return null; }
            if (known == null || known.Count == 0) return null;
            if (value.Length < 5) return null;   // short values are too easy to snap wrongly

            string best = null; int bestDist = int.MaxValue;
            foreach (string k in known)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                if (Math.Abs(k.Length - value.Length) > 2) continue;
                int d = Distance(value.ToLowerInvariant(), k.Trim().ToLowerInvariant());
                if (d < bestDist) { bestDist = d; best = k.Trim(); }
            }
            return bestDist > 0 && bestDist <= 2 ? best : null;
        }

        /// <summary>Levenshtein distance (two rows, no matrix — these strings are short).</summary>
        private static int Distance(string a, string b)
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

        // ---- validation ------------------------------------------------------

        /// <summary>
        /// Certificate-specific rules. Field-level rules catch a value that cannot be
        /// right on its own; the cross-field rules at the end catch values that are each
        /// plausible but disagree with one another.
        /// </summary>
        private static void Validate(DocAiResult r)
        {
            foreach (DocField f in r.Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Value)) continue;
                string v = f.Value.Trim();

                if (IsDateKey(f.Key))
                {
                    if (!DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                        Flag(f, FieldStatus.Invalid, "not a readable date");
                    else if (d.Date > DateTime.Today)
                        Flag(f, FieldStatus.Invalid, "dated in the future");
                    else if (d.Year < 1900)
                        Flag(f, FieldStatus.Invalid, "year " + d.Year + " is before civil registration");
                }
                else if (IsNameKey(f.Key))
                {
                    if (!Regex.IsMatch(v, @"^[A-Za-zñÑ][A-Za-zñÑ \-'\.]*$"))
                        Flag(f, FieldStatus.Invalid, "contains characters a name cannot have");
                    else if (v.Replace(" ", "").Length < 2)
                        Flag(f, FieldStatus.Invalid, "too short to be a name");
                }
                else if (f.Key == "Sex")
                {
                    if (v != "Male" && v != "Female")
                        Flag(f, FieldStatus.Invalid, "must be Male or Female");
                }
                else if (f.Key == "RegistryNo")
                {
                    // The sequence is NOT fixed-width. A small office numbers from 1 each
                    // year, so an early entry is genuinely short: the office's own 2007
                    // Certificate of Marriage is registry number 2007-72. Demanding three
                    // digits flagged a correctly-read number as Invalid, and because this
                    // is a core field that held the whole document for review. Keep this
                    // in step with FieldShape.Registry in DocLayouts (Normalise + Score),
                    // which have always accepted 1-6.
                    if (!Regex.IsMatch(v, @"^\d{4}-\d{1,6}$"))
                        Flag(f, FieldStatus.Invalid, "expected YYYY-NNN — check it against the scan");
                }
                else if (f.Key == "Weight")
                {
                    if (!int.TryParse(v, out int g) || g < 300 || g > 8000)
                        Flag(f, FieldStatus.Invalid, "birth weight outside 300–8000 g");
                }
                else if (f.Key == "Age")
                {
                    if (!int.TryParse(v, out int a) || a < 0 || a > 130)
                        Flag(f, FieldStatus.Invalid, "age outside 0–130");
                }
                else if (IsPlaceKey(f.Key))
                {
                    if (v.Count(char.IsLetter) < 3)
                        Flag(f, FieldStatus.Invalid, "too short to be a place");
                    else if (Regex.IsMatch(v, @"CERTIF|THIS IS TO|SIGNATURE|^\(", RegexOptions.IgnoreCase))
                        Flag(f, FieldStatus.Invalid, "reads like the form's own printed text, not a place");
                }
            }

            switch (r.Kind)
            {
                case DocKind.Birth: ValidateBirth(r); break;
                case DocKind.Marriage: ValidateMarriage(r); break;
                case DocKind.Death: ValidateDeath(r); break;
            }
        }

        private static void ValidateBirth(DocAiResult r)
        {
            string childLast = Val(r, "ChildLast");
            string fatherLast = Val(r, "FatherLast");
            string motherLast = Val(r, "MotherLast");

            // A legitimate mismatch exists (illegitimate child carrying the mother's
            // surname), so this is a conflict to look at, never an error to block on.
            if (childLast != "" && fatherLast != "" && motherLast != "" &&
                !Same(childLast, fatherLast) && !Same(childLast, motherLast))
                Flag(Field(r, "ChildLast"), FieldStatus.Conflict,
                    "surname matches neither parent — check the scan");

            if (motherLast != "" && fatherLast != "" && Same(motherLast, fatherLast))
                Flag(Field(r, "MotherLast"), FieldStatus.Conflict,
                    "mother's MAIDEN surname is the same as the father's — check the scan");

            string dobText = Val(r, "DateOfBirth");
            if (dobText != "" && DateTime.TryParse(dobText, out DateTime dob))
            {
                if (dob > DateTime.Today)
                    Flag(Field(r, "DateOfBirth"), FieldStatus.Invalid, "date of birth is in the future");
            }
        }

        private static void ValidateMarriage(DocAiResult r)
        {
            string h = (Val(r, "HusbandFirst") + " " + Val(r, "HusbandLast")).Trim();
            string w = (Val(r, "WifeFirst") + " " + Val(r, "WifeLast")).Trim();

            // The two-column layout fails by giving both spouses the SAME value, which is
            // exactly what identical names mean here.
            if (h != "" && Same(h, w))
            {
                Flag(Field(r, "HusbandFirst"), FieldStatus.Conflict, "husband and wife read as the same name");
                Flag(Field(r, "WifeFirst"), FieldStatus.Conflict, "husband and wife read as the same name");
            }

            string date = Val(r, "DateOfMarriage");
            if (date != "" && DateTime.TryParse(date, out DateTime m) && m > DateTime.Today)
                Flag(Field(r, "DateOfMarriage"), FieldStatus.Invalid, "date of marriage is in the future");
        }

        private static void ValidateDeath(DocAiResult r)
        {
            string dodText = Val(r, "DateOfDeath");
            string ageText = Val(r, "Age");

            if (dodText != "" && ageText != "" &&
                DateTime.TryParse(dodText, out DateTime dod) && int.TryParse(ageText, out int age))
            {
                int impliedBirthYear = dod.Year - age;
                if (impliedBirthYear < 1880 || impliedBirthYear > dod.Year)
                    Flag(Field(r, "Age"), FieldStatus.Conflict,
                        "age and date of death imply a birth year of " + impliedBirthYear);
            }
        }

        // ---- small helpers ---------------------------------------------------

        private static void Flag(DocField f, FieldStatus status, string issue)
        {
            if (f == null) return;
            // An invalid value outranks a conflict: fix what cannot be right first.
            if (f.Status == FieldStatus.Invalid && status != FieldStatus.Invalid) return;
            f.Status = status;
            f.Issue = issue;
            f.Uncertain = true;
        }

        private static DocField Field(DocAiResult r, string key)
        {
            return r.Fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static string Val(DocAiResult r, string key)
        {
            DocField f = Field(r, key);
            return f == null || f.Value == null ? "" : f.Value.Trim();
        }

        private static bool Same(string a, string b)
        {
            return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDateKey(string key)
        {
            return key == "DateOfBirth" || key == "DateOfMarriage" || key == "DateOfDeath";
        }

        private static bool IsNumericKey(string key)
        {
            return key == "RegistryNo" || key == "Weight" || key == "Age";
        }

        private static bool IsNameKey(string key)
        {
            return key.EndsWith("First", StringComparison.Ordinal)
                || key.EndsWith("Middle", StringComparison.Ordinal)
                || key.EndsWith("Last", StringComparison.Ordinal)
                || key == "FullName" || key == "Informant" || key == "Solemnizer";
        }

        private static bool IsPlaceKey(string key)
        {
            return key.StartsWith("Place", StringComparison.Ordinal)
                || key == "PlaceMunicipality" || key == "PlaceProvince" || key == "PlaceHospital";
        }

        /// <summary>Fields whose value can be snapped onto a spelling the office already uses.</summary>
        private static bool IsLookupKey(string key, out string category)
        {
            switch (key)
            {
                case "PlaceOfBirth": category = LearningLibrary.PlaceOfBirth; return true;
                case "PlaceOfMarriage": category = LearningLibrary.PlaceOfMarriage; return true;
                case "PlaceOfDeath": category = LearningLibrary.PlaceOfDeath; return true;
                case "PlaceHospital": category = LearningLibrary.Hospital; return true;
                case "PlaceMunicipality": category = LearningLibrary.Municipality; return true;
                case "PlaceProvince": category = LearningLibrary.Province; return true;
                case "MotherOccupation":
                case "FatherOccupation": category = LearningLibrary.Occupation; return true;
                case "Nationality":
                case "Citizenship": category = LearningLibrary.Nationality; return true;
                default: category = null; return false;
            }
        }
    }
}
