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
    }

    /// <summary>
    /// Image in, text out: the whole engine in one place.
    /// <para/>
    /// It runs the front half (threshold, rule removal, line choice, character split), asks the
    /// classifier what each character is, and puts spaces back where the gaps between
    /// characters are word-sized.
    /// </summary>
    public sealed class OwnOcrReader
    {
        private readonly KnnClassifier _knn;
        private readonly KnnClassifier _scorer;

        /// <summary>Fixed price per character piece in the splitter search; higher = fewer, larger pieces.</summary>
        public double SplitPenalty = 0.25;

        /// <summary>Search the whole line at once (can also merge broken letters) instead of each clump alone.</summary>
        public bool GlobalMerge { set { SplitSearch.GlobalMerge = value; } }

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

            var sb = new StringBuilder();
            double scoreSum = 0;
            var gaps = new List<double>();
            for (int i = 0; i + 1 < a.Cells.Count; i++)
                gaps.Add(Math.Max(0, a.Cells[i + 1].Left - a.Cells[i].Right - 1));
            double medianGap = gaps.Count == 0 ? 0 : gaps.OrderBy(g => g).ToList()[gaps.Count / 2];
            double spaceGap = Math.Max(0.30 * a.Line.CapHeight, 2.5 * medianGap);

            for (int i = 0; i < a.Cells.Count; i++)
            {
                Prediction[] ranked = _knn.Rank(Features.FromCell(a.Cells[i]), 5);
                if (ranked.Length == 0) continue;

                if (i > 0 && a.Cells[i].Left - a.Cells[i - 1].Right - 1 > spaceGap) sb.Append(' ');

                // The best NAMEABLE character: "not a character" is a verdict about the box, not
                // something to print. If every candidate is a reject, say so with a question mark
                // rather than inventing a letter.
                Prediction pick = ranked.FirstOrDefault(p => p.Label != Charset.Reject);
                if (pick == null) { sb.Append('?'); }
                else { sb.Append(pick.Char); scoreSum += pick.Score; }
                result.Candidates.Add(ranked);
            }
            result.Text = sb.ToString();
            result.Confidence = result.Candidates.Count == 0 ? 0 : scoreSum / result.Candidates.Count;
            return result;
        }
    }
}
