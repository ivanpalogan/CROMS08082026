using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CROMS.Data;

namespace CROMS.DocTest
{
    /// <summary>
    /// Measures the module against GROUND TRUTH transcribed from the certificates by hand,
    /// field by field.
    /// <para/>
    /// This exists because classification accuracy and Tesseract's mean confidence say
    /// nothing about whether the RECORD is right: the marriage sample classified at 99%
    /// while both spouse names were garbage, and the death sample recognises at 50% with
    /// every field correct. The only number worth reporting is how many field VALUES match
    /// the paper, so that is what this prints — correct, wrong and missing per document,
    /// with every wrong value shown next to what it should have been.
    /// <para/>
    /// Handwritten entries are deliberately absent from the expectations. Tesseract reads
    /// print; demanding handwriting would measure the wrong thing and hide real regressions
    /// behind a number that can never reach 100%.
    /// </summary>
    internal static class Truth
    {
        internal class Sample
        {
            public string File;
            public string What;
            public DocKind Kind;
            public string Layout;
            public Dictionary<string, string> Expect;
        }

        internal static List<Sample> Samples(string dir)
        {
            return new List<Sample>
            {
                new Sample
                {
                    File = Path.Combine(dir, "nice.jpg"),
                    What = "Birth — MF-102 (2007), PSA copy",
                    Kind = DocKind.Birth,
                    Layout = "MF-102 (2007)",
                    Expect = new Dictionary<string, string>
                    {
                        { "Province", "Cagayan" },
                        { "CityMunicipality", "Tuguegarao City" },
                        { "ChildFirst", "SHELLIAN CLEAR" },
                        { "ChildMiddle", "BALOSO" },
                        { "ChildLast", "TALOSIG" },
                        { "Sex", "Female" },
                        { "DateOfBirth", "2018-06-12" },
                        { "PlaceHospital", "Cagayan Valley Medical Center" },
                        { "PlaceMunicipality", "Tuguegarao City" },
                        { "PlaceProvince", "Cagayan" },
                        { "TypeOfBirth", "Single" },
                        { "BirthOrder", "Third" },
                        { "Weight", "2722" },
                        { "MotherFirst", "SHEILA" },
                        { "MotherMiddle", "ARTICULO" },
                        { "MotherLast", "BALOSO" },
                        { "MotherCitizenship", "Filipino" },
                        { "MotherReligion", "Roman Catholic" },
                        { "MotherOccupation", "Housewife" },
                        { "MotherAge", "36" },
                        { "FatherFirst", "GILBERT" },
                        { "FatherMiddle", "CATAGGATAN" },
                        { "FatherLast", "TALOSIG" },
                        { "FatherCitizenship", "Filipino" },
                        { "FatherReligion", "Roman Catholic" },
                        { "FatherOccupation", "Driver" },
                        { "FatherAge", "35" },
                        { "MarriageOfParentsDate", "2007-02-28" },
                        { "Attendant", "Physician" },
                        { "TimeOfBirth", "1:40 PM" },
                    }
                },
                new Sample
                {
                    File = Path.Combine(dir, "gilvan birth.jpg"),
                    What = "Birth — MF-102 (1993), NSO copy, faded",
                    Kind = DocKind.Birth,
                    Layout = "MF-102 (1993)",
                    Expect = new Dictionary<string, string>
                    {
                        { "Province", "Metro Manila" },
                        { "CityMunicipality", "Pasay City" },
                        { "ChildFirst", "Gilvan" },
                        { "ChildMiddle", "Baloso" },
                        { "ChildLast", "Talosig" },
                        { "Sex", "Male" },
                        { "DateOfBirth", "2005-06-18" },
                        { "TypeOfBirth", "Single" },
                        { "BirthOrder", "First" },
                        { "Weight", "2835" },
                        { "MotherFirst", "Sheila" },
                        { "MotherMiddle", "Articulo" },
                        { "MotherLast", "Baloso" },
                        { "MotherCitizenship", "Filipino" },
                        { "MotherOccupation", "Saleslady" },
                        { "MotherAge", "22" },
                        { "FatherFirst", "Gilbert" },
                        { "FatherMiddle", "Cataggatan" },
                        { "FatherLast", "Talosig" },
                        { "FatherCitizenship", "Filipino" },
                        { "FatherOccupation", "Bagger" },
                        { "FatherAge", "22" },
                        { "Attendant", "Others" },
                    }
                },
                new Sample
                {
                    File = Path.Combine(dir, "palogan_n.jpg"),
                    What = "Death — MF-103 (2016), PSA copy, 706 px",
                    Kind = DocKind.Death,
                    Layout = "MF-103 (2016)",
                    Expect = new Dictionary<string, string>
                    {
                        { "Province", "PAMPANGA" },
                        { "CityMunicipality", "ANGELES" },
                        { "RegistryNo", "1999-76251" },
                        { "DeceasedFirst", "GEORGE" },
                        { "DeceasedMiddle", "DE GUZMAN" },
                        { "DeceasedLast", "ABAD" },
                        { "Sex", "Male" },
                        { "DateOfDeath", "1999-08-29" },
                        { "DateOfBirth", "1953-04-23" },
                        { "Age", "46" },
                        { "PlaceOfDeath", "ANGELES, PAMPANGA" },
                        { "CivilStatus", "Married" },
                        { "Religion", "CATHOLIC" },
                        { "Citizenship", "FILIPINO" },
                        { "Residence", "MABALACAT PAMPANGA" },
                        { "Occupation", "CHECKER/ DISPATCHER" },
                        { "FatherName", "FELIPE ABAD" },
                        { "MotherMaidenName", "JUANITA ABAD" },
                        { "CauseOfDeath", "CARDIOPULMONARY ARREST" },
                        { "CorpseDisposal", "Burial" },
                    }
                },
                new Sample
                {
                    File = Path.Combine(dir, "marrage.jpg"),
                    What = "Marriage — MF-97 (1993), PSA copy",
                    Kind = DocKind.Marriage,
                    Layout = "MF-97 (1993)",
                    Expect = MarriageTruth()
                },
                new Sample
                {
                    File = Path.Combine(dir, "790238709_1053379053965181_1576431462900421972_n.jpg"),
                    What = "Marriage — same record, second photograph (layout/fit check)",
                    Kind = DocKind.Marriage,
                    Layout = "MF-97 (1993)",
                    Expect = MarriageTruth()
                },
            };
        }

        // Both marriage images are photographs of the SAME certificate, so they share one
        // truth table — which is exactly what makes them a fit test: the template has to
        // land on two differently framed photographs of one form.
        private static Dictionary<string, string> MarriageTruth()
        {
            return new Dictionary<string, string>
            {
                { "Province", "Cagayan" },
                { "CityMunicipality", "Peñablanca" },
                { "HusbandFirst", "GILBERT" },
                { "HusbandMiddle", "CATAGGATAN" },
                { "HusbandLast", "TALOSIG" },
                { "WifeFirst", "SHEILA" },
                { "WifeMiddle", "ARTICULO" },
                { "WifeLast", "BALOSO" },
                { "HusbandDateOfBirth", "1983-04-30" },
                { "HusbandAge", "23" },
                { "WifeAge", "25" },
                { "HusbandPlaceOfBirth", "Bical, Peñablanca, Cag." },
                { "WifePlaceOfBirth", "Fabella Hospital, Manila" },
                { "HusbandSex", "Male" },
                { "WifeSex", "Female" },
                { "HusbandCitizenship", "Filipino" },
                { "WifeCitizenship", "Filipino" },
                { "HusbandResidence", "Bical, Peñablanca, Cagayan" },
                { "WifeResidence", "Bical, Peñablanca, Cagayan" },
                { "HusbandReligion", "Catholic" },
                { "WifeReligion", "Catholic" },
                { "HusbandCivilStatus", "Single" },
                { "WifeCivilStatus", "Single" },
                { "HusbandFatherName", "Alberto Buraga Talosig" },
                { "WifeFatherName", "Jesus Jr. Baloso" },
                { "HusbandMotherName", "Pepita Lenn Cataggatan" },
                { "WifeMotherName", "Diosity Articulo" },
                { "PlaceOfMarriage", "OFFICE OF THE MUNICIPAL MAYOR" },
                { "DateOfMarriage", "2007-02-28" },
            };
        }

        internal static int Run(string dir)
        {
            int totalCorrect = 0, totalWrong = 0, totalMissing = 0;

            foreach (Sample s in Samples(dir))
            {
                Console.WriteLine();
                Console.WriteLine(new string('=', 78));
                Console.WriteLine(Path.GetFileName(s.File) + "   " + s.What);
                Console.WriteLine(new string('=', 78));
                if (!File.Exists(s.File)) { Console.WriteLine("  MISSING FILE: " + s.File); continue; }

                DocAiResult r;
                var sw = Stopwatch.StartNew();
                using (Bitmap image = DocumentAI.LoadImage(s.File)) r = DocumentAI.Analyze(image);
                sw.Stop();

                if (!string.IsNullOrEmpty(r.Error)) { Console.WriteLine("  ERROR: " + r.Error); continue; }

                bool kindOk = r.Kind == s.Kind;
                bool layoutOk = string.Equals(r.LayoutCode, s.Layout, StringComparison.Ordinal);
                Console.WriteLine("  classified   : {0} {1}   layout {2} {3}   class {4}%",
                    r.Kind, kindOk ? "OK" : "WRONG (expected " + s.Kind + ")",
                    r.LayoutCode ?? "-", layoutOk ? "OK" : "WRONG (expected " + s.Layout + ")",
                    r.ClassifyConfidence);
                Console.WriteLine("  recognition  : {0}%   field confidence {1}%   rotation {2}°   {3:0.0}s",
                    r.OcrConfidence, r.OverallConfidence, r.RotationApplied, sw.Elapsed.TotalSeconds);
                Console.WriteLine("  template fit : " + (r.FitNote ?? "-"));
                if (r.NeedsManualReview) Console.WriteLine("  HELD         : " + r.ReviewReason);

                var got = new Dictionary<string, DocField>(StringComparer.OrdinalIgnoreCase);
                foreach (DocField f in r.Fields) got[f.Key] = f;

                int correct = 0, wrong = 0, missing = 0;
                foreach (var kv in s.Expect)
                {
                    DocField f;
                    string actual = got.TryGetValue(kv.Key, out f) ? (f.Value ?? "") : "";
                    int conf = f == null ? 0 : f.Confidence;

                    if (Matches(actual, kv.Value))
                    {
                        correct++;
                        Console.WriteLine("  OK      {0,-24} {1,3}%  {2}", kv.Key, conf, actual);
                    }
                    else if (actual.Trim().Length == 0)
                    {
                        missing++;
                        Console.WriteLine("  MISSING {0,-24}       expected: {1}", kv.Key, kv.Value);
                    }
                    else
                    {
                        wrong++;
                        Console.WriteLine("  WRONG   {0,-24} {1,3}%  got: {2}   expected: {3}   ({4}% of chars)",
                            kv.Key, conf, actual, kv.Value, CharSimilarity(actual, kv.Value));
                    }
                }

                // Values produced for fields with no expectation are never silently
                // ignored: a confidently wrong extra value is a false record too.
                var extra = r.Fields
                    .Where(f => !string.IsNullOrWhiteSpace(f.Value) && !s.Expect.ContainsKey(f.Key))
                    .Select(f => f.Key + "=" + f.Value)
                    .ToList();

                int n = s.Expect.Count;
                Console.WriteLine("  " + new string('-', 60));
                Console.WriteLine("  ACCURACY     : {0}/{1} correct ({2}%),  {3} wrong,  {4} missing",
                    correct, n, n == 0 ? 0 : correct * 100 / n, wrong, missing);
                if (extra.Count > 0)
                    Console.WriteLine("  also read    : " + string.Join(" | ", extra));

                totalCorrect += correct; totalWrong += wrong; totalMissing += missing;
            }

            int all = totalCorrect + totalWrong + totalMissing;
            Console.WriteLine();
            Console.WriteLine(new string('=', 78));
            Console.WriteLine("TOTAL: {0}/{1} field values correct ({2}%),  {3} wrong,  {4} missing",
                totalCorrect, all, all == 0 ? 0 : totalCorrect * 100 / all, totalWrong, totalMissing);
            Console.WriteLine(new string('=', 78));
            return 0;
        }

        /// <summary>
        /// Field-value comparison. Case and the spacing around punctuation are not part of
        /// the record's meaning, so they are normalised away; anything else is a different
        /// value and counts as wrong.
        /// </summary>
        private static bool Matches(string actual, string expected)
        {
            return string.Equals(Norm(actual), Norm(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static string Norm(string s)
        {
            s = (s ?? "").Trim();
            s = Regex.Replace(s, @"\s*([,/])\s*", "$1");
            s = Regex.Replace(s, @"\s{2,}", " ");
            return s.Trim(' ', '.', ',');
        }

        /// <summary>How much of the expected string the reading got right, as a percentage.</summary>
        private static int CharSimilarity(string actual, string expected)
        {
            string a = Norm(actual).ToLowerInvariant(), b = Norm(expected).ToLowerInvariant();
            if (b.Length == 0) return 0;
            int d = Distance(a, b);
            int pct = (int)Math.Round(100.0 * (b.Length - Math.Min(d, b.Length)) / b.Length);
            return pct < 0 ? 0 : pct;
        }

        private static int Distance(string a, string b)
        {
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
