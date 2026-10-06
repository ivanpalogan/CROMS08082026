using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CROMS.OwnOcr
{
    /// <summary>What a repair did, kept so the benchmark (and an operator) can see it.</summary>
    public sealed class RepairResult
    {
        public string Text;          // the text after repair (equals the input when nothing was changed)
        public string Original;      // what the classifier read
        public bool Changed;
        public double Cost;          // classifier-weighted edit cost of the accepted entry (words: sum)
        public string Kind = "";     // which lexicon was consulted ("" = none, field has no public vocabulary)
    }

    /// <summary>
    /// A closed list of public words (provinces, municipalities, barangays, nationalities,
    /// religions, civil statuses) used to repair a reading that is a letter or two off.
    /// <para/>
    /// Deliberately NOT a list of persons' names. A civil registry sees the same families again
    /// and again, so a name list gains near-neighbours as it grows - the nearest name to a
    /// misread "Albert" is a different real person, "Gilbert" - and a wrong name that looks right
    /// is worse than a garbled one nobody accepts (the office measured exactly this on 2026-09-04).
    /// A province or a religion is a small fixed set; a near miss there is a misread, not a person.
    /// </summary>
    public sealed class Lexicon
    {
        private readonly string[] _canon;   // spelling to output
        private readonly string[] _fold;    // lower case, accents removed, letters/digits/spaces only
        public int Count { get { return _canon.Length; } }
        public IEnumerable<string> Entries() { return _canon; }

        public Lexicon(IEnumerable<string> entries)
        {
            // One entry per folded spelling: several provinces hold a "San Isidro", and the
            // repair must not call two spellings of the SAME word ambiguous.
            var seen = new Dictionary<string, string>();
            foreach (string e in entries)
            {
                string c = (e ?? "").Trim();
                if (c.Length == 0) continue;
                string f = Fold(c);
                if (f.Length == 0 || seen.ContainsKey(f)) continue;
                seen[f] = c;
            }
            _fold = seen.Keys.ToArray();
            _canon = _fold.Select(f => seen[f]).ToArray();
        }

        public static Lexicon FromFile(string path)
        {
            return new Lexicon(File.ReadAllLines(path, Encoding.UTF8));
        }

        /// <summary>The words of every entry (3+ letters), as a lexicon of single words.</summary>
        public Lexicon Words()
        {
            return new Lexicon(_canon.SelectMany(c => c.Split(new[] { ' ', '-', '.', '(', ')', ',', '/' }, StringSplitOptions.RemoveEmptyEntries))
                                     .Where(w => w.Length >= 3));
        }

        public static string Fold(string s)
        {
            string d = (s ?? "").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char ch in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                else if (char.IsWhiteSpace(ch) || ch == '-') { if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' '); }
            }
            return sb.ToString().Trim();
        }

        // ------------------------------------------------------------------ the reading, as tokens
        // A token is one thing the classifier saw: a character with its ranked candidates, or a
        // word space. Cost of calling a token a given letter is "how much worse than its own best
        // guess": 0 if it was the top candidate, up to 1 if the classifier never considered it.
        private sealed class Token
        {
            public bool Space;
            public char[] Fold = new char[0];       // folded candidate characters
            public double[] Score = new double[0];
            public double Best;
            public bool Punct;                      // top guess is punctuation: cheap to ignore
        }

        private static List<Token> Tokens(ReadResult rr)
        {
            var list = new List<Token>();
            for (int k = 0; k < rr.Candidates.Count; k++)
            {
                if (k < rr.SpaceBefore.Count && rr.SpaceBefore[k]) list.Add(new Token { Space = true });
                Prediction[] c = rr.Candidates[k];
                var t = new Token();
                var fc = new List<char>(); var fs = new List<double>();
                foreach (Prediction p in c)
                {
                    string f = Fold(p.Char.ToString());
                    if (f.Length != 1) continue;
                    int at = fc.IndexOf(f[0]);
                    if (at >= 0) fs[at] += p.Score; else { fc.Add(f[0]); fs.Add(p.Score); }
                }
                t.Fold = fc.ToArray(); t.Score = fs.ToArray();
                t.Best = fs.Count == 0 ? 0 : fs.Max();
                t.Punct = c.Length > 0 && !char.IsLetterOrDigit(c[0].Char);
                list.Add(t);
            }
            return list;
        }

        private static double Sub(Token t, char e)
        {
            if (e == ' ') return t.Space ? 0 : 1;
            if (t.Space) return 1;
            for (int i = 0; i < t.Fold.Length; i++)
                if (t.Fold[i] == e) return t.Best <= 0 ? 1 : Math.Min(1.0, 1.0 - t.Score[i] / t.Best);
            return 1;
        }

        // Cost of making the reading into this entry. Insert = the paper has a letter the engine
        // missed (two letters merged); delete = the engine produced a piece the paper does not have
        // (one letter cut in two, a rule remnant read as a mark).
        private static double Distance(List<Token> a, string e, double cutoff)
        {
            int n = a.Count, m = e.Length;
            var prev = new double[m + 1];
            var cur = new double[m + 1];
            for (int j = 1; j <= m; j++) prev[j] = prev[j - 1] + (e[j - 1] == ' ' ? 0.5 : 1.0);
            for (int i = 1; i <= n; i++)
            {
                Token t = a[i - 1];
                double del = t.Space ? 0.5 : (t.Punct ? 0.3 : 1.0);
                cur[0] = prev[0] + del;
                double rowMin = cur[0];
                for (int j = 1; j <= m; j++)
                {
                    double ins = e[j - 1] == ' ' ? 0.5 : 1.0;
                    double v = prev[j - 1] + Sub(t, e[j - 1]);
                    double d1 = prev[j] + del; if (d1 < v) v = d1;
                    double d2 = cur[j - 1] + ins; if (d2 < v) v = d2;
                    cur[j] = v;
                    if (v < rowMin) rowMin = v;
                }
                if (rowMin > cutoff) return double.MaxValue;     // cannot get back under the cutoff
                var sw = prev; prev = cur; cur = sw;
            }
            return prev[m];
        }

        // ---------------------------------------------------------------------------- repair
        /// <summary>How much total cost an entry of this length may carry and still count as "the same word misread".</summary>
        public static double Allowance(int length)
        {
            if (length < 4) return 0.0;          // too short to tell from another short word
            return 0.28 * length;
        }

        /// <summary>
        /// Snap the whole reading to the single closest entry - but only when it is close enough AND
        /// clearly closer than every other entry. Otherwise the reading is returned as it was.
        /// </summary>
        public RepairResult RepairPhrase(ReadResult rr, double margin = 0.5)
        {
            var res = new RepairResult { Text = rr.Text, Original = rr.Text };
            List<Token> tokens = Tokens(rr);
            if (tokens.Count == 0) return res;

            // Best and second best over EVERY entry. An entry only matters if it beats the current
            // second best, so that is also the pruning bound of the distance search.
            double best = double.MaxValue, second = double.MaxValue; int bestI = -1;
            for (int i = 0; i < _fold.Length; i++)
            {
                double d = Distance(tokens, _fold[i], second);
                if (d == double.MaxValue) continue;
                if (d < best) { second = best; best = d; bestI = i; }
                else if (d < second) second = d;
            }            if (bestI < 0) return res;
            double allowBest = Allowance(_fold[bestI].Length);
            if (best > allowBest) return res;                        // not close enough to count as a misread of it
            if (second - best < margin) return res;                  // another entry is nearly as good: ambiguous, leave it
            res.Cost = best;
            string chosen = Match(_canon[bestI], rr.Text);
            if (!string.Equals(chosen, rr.Text, StringComparison.Ordinal)) { res.Text = chosen; res.Changed = true; }
            return res;
        }

        /// <summary>Repair each word of the reading on its own (for a field holding several words, such as a place).</summary>
        public RepairResult RepairWords(ReadResult rr, int minWordLength = 4, double margin = 0.5)
        {
            var res = new RepairResult { Text = rr.Text, Original = rr.Text };
            if (rr.Candidates.Count == 0) return res;

            // group the characters into words at the recorded word spaces
            var groups = new List<List<Prediction[]>>(); var cur = new List<Prediction[]>();
            for (int k = 0; k < rr.Candidates.Count; k++)
            {
                if (k < rr.SpaceBefore.Count && rr.SpaceBefore[k] && cur.Count > 0) { groups.Add(cur); cur = new List<Prediction[]>(); }
                cur.Add(rr.Candidates[k]);
            }
            if (cur.Count > 0) groups.Add(cur);

            var outW = new string[groups.Count];
            bool changed = false; double cost = 0;
            for (int w = 0; w < groups.Count; w++)
            {
                var one = new ReadResult { Text = new string(groups[w].Select(c => c[0].Char).ToArray()) };
                foreach (Prediction[] c in groups[w]) { one.Candidates.Add(c); one.SpaceBefore.Add(false); }
                outW[w] = one.Text;
                if (groups[w].Count < minWordLength) continue;
                RepairResult r = RepairPhrase(one, margin);
                if (r.Changed) { outW[w] = r.Text; changed = true; cost += r.Cost; }
            }
            if (changed) { res.Text = string.Join(" ", outW); res.Changed = true; res.Cost = cost; }
            return res;
        }
        // Carry the reading's letter case onto the canonical spelling: SHEILA stays upper case.
        private static string Match(string canon, string read)
        {
            var letters = read.Where(char.IsLetter).ToArray();
            if (letters.Length >= 2 && letters.All(char.IsUpper)) return canon.ToUpperInvariant();
            if (letters.Length >= 2 && letters.All(char.IsLower)) return canon.ToLowerInvariant();
            return canon;
        }
    }

    /// <summary>The public vocabularies and which field uses which.</summary>
    public sealed class LexiconSet
    {
        private Lexicon _province, _municipality, _nationality, _religion, _civil, _placeWords;

        public static LexiconSet Load(string dir)
        {
            Func<string, Lexicon> load = n =>
            {
                string p = Path.Combine(dir, n + ".txt");
                return File.Exists(p) ? Lexicon.FromFile(p) : null;
            };
            var s = new LexiconSet
            {
                _province = load("provinces"), _municipality = load("municipalities"),
                _nationality = load("nationalities"), _religion = load("religions"), _civil = load("civil")
            };
            var barangays = load("barangays");
            var parts = new List<Lexicon>();
            foreach (var l in new[] { s._province, s._municipality, barangays }) if (l != null) parts.Add(l);
            // words of every place name; building it from three lists, each already deduplicated
            s._placeWords = parts.Count == 0 ? null
                : new Lexicon(parts.SelectMany(p => p.Words().Entries()));
            return s;
        }

        /// <summary>Repair a reading for this field. Fields with no public vocabulary (people's names, occupations) are returned untouched.</summary>
        public RepairResult Repair(string key, ReadResult rr)
        {
            string k = (key ?? "").ToLowerInvariant();
            RepairResult r = null; string kind = "";
            if (k.Contains("citizenship") || k.Contains("nationality")) { if (_nationality != null) { r = _nationality.RepairPhrase(rr); kind = "nationality"; } }
            else if (k.Contains("religion")) { if (_religion != null) { r = _religion.RepairPhrase(rr); kind = "religion"; } }
            else if (k.Contains("civilstatus")) { if (_civil != null) { r = _civil.RepairPhrase(rr); kind = "civil status"; } }
            else if (k == "province" || k.EndsWith("province")) { if (_province != null) { r = _province.RepairPhrase(rr); kind = "province"; } }
            else if (k.Contains("municipality")) { if (_municipality != null) { r = _municipality.RepairPhrase(rr); kind = "municipality"; } }
            else if (k.Contains("place") || k.Contains("residence") || k.Contains("hospital")) { if (_placeWords != null) { r = _placeWords.RepairWords(rr); kind = "place words"; } }
            if (r == null) return new RepairResult { Text = rr.Text, Original = rr.Text };
            r.Kind = kind;
            return r;
        }
    }
}
