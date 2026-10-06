using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CROMS.OwnOcr
{
    public sealed class ReadResult
    {
        public string Text = "";
        /// <summary>Mean vote share of the characters actually chosen (0..1); low means the neighbours disagreed.</summary>
        public double Confidence;
        public Analysis Analysis;
        /// <summary>For each character in <see cref="Text"/> (spaces excluded), its ranked candidates.</summary>
        public List<Prediction[]> Candidates = new List<Prediction[]>();
        /// <summary>Parallel to <see cref="Candidates"/>: true when a word space precedes that character.</summary>
        public List<bool> SpaceBefore = new List<bool>();
    }

    /// <summary>
    /// Image in, text out: the whole engine in one place.
    /// <para/>
    /// It runs the front half (threshold, rule removal, line choice, character split), asks the
    /// classifier what each character is, puts spaces back where the gaps between characters are
    /// word-sized, and then applies what is known about the FIELD: a name or a place has no
    /// digits, and a word keeps one letter case.
    /// </summary>
    public sealed class OwnOcrReader
    {
        private readonly KnnClassifier _knn;
        private readonly KnnClassifier _scorer;

        /// <summary>Fixed price per character piece in the splitter search; higher = fewer, larger pieces.</summary>
        public double SplitPenalty = 0.25;

        /// <summary>Search the whole line at once (can also merge broken letters) instead of each clump alone.</summary>
        public bool GlobalMerge { set { SplitSearch.GlobalMerge = value; } }

        /// <summary>
        /// The field holds text, not numbers (names, places, nationality, occupation): digits are
        /// not offered as an answer. Without this, I read as 1 and O as 0 whenever the shapes agree.
        /// </summary>
        public bool LettersOnly;

        /// <summary>A word is all capitals, Capitalised or all lower case - not a mixture. Decided by vote.</summary>
        public bool CaseConsistency;

        // Which labels may be chosen. The reject class is never printable.
        private static readonly bool[] AnyCharacter = Charset.Mask(c => true);
        // A name or place is letters plus the few marks they really contain; # & : ; / ( ) are
        // speckle or a ruled line read as a mark, never part of the value.
        private const string NonTextMarks = "#&:;/()";
        private static readonly bool[] NoDigits = Charset.Mask(c => !char.IsDigit(c) && NonTextMarks.IndexOf(c) < 0);
        private static readonly bool[] UpperOnly = Charset.Mask(c => !char.IsDigit(c) && !char.IsLower(c) && NonTextMarks.IndexOf(c) < 0);
        private static readonly bool[] LowerOnly = Charset.Mask(c => !char.IsDigit(c) && !char.IsUpper(c) && NonTextMarks.IndexOf(c) < 0);

        /// <summary>
        /// Drop a mark that cannot start or end a word: a value never begins with punctuation, and
        /// only a full stop (an initial, "B.") may end one. These come from the edge of the crop.
        /// </summary>
        public bool TrimEdgeMarks;

        /// <param name="knn">Names each character. The larger the reference set the better.</param>
        /// <param name="scorer">
        /// Judges candidate cuts. It is queried many times per clump, so it is a much smaller
        /// reference set than <paramref name="knn"/>. When null the plain thinnest-column cut is used.
        /// </param>
        public OwnOcrReader(KnnClassifier knn, KnnClassifier scorer = null)
        {
            _knn = knn; _scorer = scorer;
        }

        public ReadResult Read(System.Drawing.Bitmap bmp)
        {
            return Read(OwnOcrEngine.Analyze(bmp));
        }

        public ReadResult Read(Analysis a)
        {
            var result = new ReadResult { Analysis = a };
            if (a.Cells.Count == 0 || a.Line == null) return result;

            // Re-carve the clumps by what the classifier makes of the pieces, replacing the
            // plain thinnest-column cut. The result replaces a.Cells so the debug picture
            // shows what was actually read.
            if (_scorer != null)
            {
                List<GlyphCell> better = SplitSearch.Run(a, _scorer, SplitPenalty);
                if (better.Count > 0) a.Cells = better;
            }

            int n = a.Cells.Count;

            // ---- words: where the gap between characters is word-sized -----------------------
            var gaps = new List<double>();
            for (int i = 0; i + 1 < n; i++)
                gaps.Add(Math.Max(0, a.Cells[i + 1].Left - a.Cells[i].Right - 1));
            double medianGap = gaps.Count == 0 ? 0 : gaps.OrderBy(g => g).ToList()[gaps.Count / 2];
            double spaceGap = Math.Max(0.30 * a.Line.CapHeight, 2.5 * medianGap);

            var wordOf = new int[n];
            var spaceBefore = new bool[n];
            int word = 0;
            for (int i = 1; i < n; i++)
            {
                if (a.Cells[i].Left - a.Cells[i - 1].Right - 1 > spaceGap) { word++; spaceBefore[i] = true; }
                wordOf[i] = word;
            }

            // ---- first reading of every character ----------------------------------------------
            bool[] allowed = LettersOnly ? NoDigits : AnyCharacter;
            var feats = new float[n][];
            var ranked = new Prediction[n][];
            for (int i = 0; i < n; i++)
            {
                feats[i] = Features.FromCell(a.Cells[i]);
                float d;
                ranked[i] = _knn.Rank(feats[i], 5, out d, allowed);
            }

            // ---- one letter case per word -----------------------------------------------------
            if (CaseConsistency)
            {
                for (int w = 0; w <= word; w++)
                {
                    List<int> idx = Enumerable.Range(0, n).Where(i => wordOf[i] == w).ToList();
                    int upper = 0, lower = 0;
                    foreach (int i in idx)
                    {
                        if (ranked[i].Length == 0) continue;
                        char c = ranked[i][0].Char;
                        if (char.IsUpper(c)) upper++; else if (char.IsLower(c)) lower++;
                    }
                    if (upper + lower < 2) continue;   // an initial or a single letter: nothing to be consistent with

                    bool[] mask; bool titleFirst = false;
                    if (upper >= 0.6 * (upper + lower)) mask = UpperOnly;                       // SHEILA
                    else if (ranked[idx[0]].Length > 0 && char.IsUpper(ranked[idx[0]][0].Char) && upper <= 2)
                    { mask = LowerOnly; titleFirst = true; }                                      // Sheila
                    else mask = LowerOnly;                                                       // sheila

                    for (int k = 0; k < idx.Count; k++)
                    {
                        int i = idx[k];
                        if (titleFirst && k == 0) continue;   // the capital stays as read
                        float d;
                        Prediction[] again = _knn.Rank(feats[i], 5, out d, Intersect(mask, allowed));
                        if (again.Length > 0) ranked[i] = again;
                    }
                }
            }

            // ---- write the text -----------------------------------------------------------------
            var sb = new StringBuilder();
            double scoreSum = 0;
            for (int i = 0; i < n; i++)
            {
                if (ranked[i].Length == 0) continue;
                if (spaceBefore[i]) sb.Append(' ');
                sb.Append(ranked[i][0].Char);
                scoreSum += ranked[i][0].Score;
                result.Candidates.Add(ranked[i]);
                result.SpaceBefore.Add(spaceBefore[i] && sb.Length > 1);
            }
            result.Text = TrimEdgeMarks ? TrimMarks(sb.ToString(), result) : sb.ToString();
            result.Confidence = result.Candidates.Count == 0 ? 0 : scoreSum / result.Candidates.Count;
            return result;
        }

        // Removes leading / trailing marks from the text AND the parallel candidate lists, so the
        // lexicon repair (which reads Candidates) still lines up with the text.
        private static string TrimMarks(string text, ReadResult r)
        {
            var drop = new List<int>();         // candidate indices to remove
            string[] words = text.Split(' ');
            int ci = 0;
            var outWords = new List<string>();
            foreach (string w in words)
            {
                int start = 0, end = w.Length;
                while (start < end && !char.IsLetterOrDigit(w[start])) { drop.Add(ci + start); start++; }
                while (end > start && !char.IsLetterOrDigit(w[end - 1]) && w[end - 1] != '.') { drop.Add(ci + end - 1); end--; }
                if (end > start) outWords.Add(w.Substring(start, end - start));
                ci += w.Length;
            }
            var dropSet = new HashSet<int>(drop);
            var cands = new List<Prediction[]>(); var spaces = new List<bool>();
            bool pending = false;                       // a word space seen on a dropped character
            for (int i = 0; i < r.Candidates.Count; i++)
            {
                bool sp = i < r.SpaceBefore.Count && r.SpaceBefore[i];
                if (dropSet.Contains(i)) { pending |= sp; continue; }
                cands.Add(r.Candidates[i]);
                spaces.Add((sp || pending) && cands.Count > 1);
                pending = false;
            }
            r.Candidates = cands; r.SpaceBefore = spaces;
            return string.Join(" ", outWords);
        }

        private static bool[] Intersect(bool[] a, bool[] b)
        {
            var r = new bool[a.Length];
            for (int i = 0; i < r.Length; i++) r[i] = a[i] && b[i];
            return r;
        }
    }
}
