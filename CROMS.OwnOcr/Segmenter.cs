using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// One candidate character: a tight box round its ink and a normalised picture of it,
    /// ready for the classifier.
    /// </summary>
    public sealed class GlyphCell
    {
        /// <summary>Side of the square picture handed to the classifier.</summary>
        public const int Size = 20;

        public int Left, Top, Right, Bottom;   // tight box round the ink, in analysis pixels
        public bool FromSplit;                 // cut out of a wider piece, not a whole component
        public float[] Image;                  // Size*Size, 0 = paper, 1 = ink

        // Where the box sat against its line, in units of the line's cap height. The picture
        // already shows this inside a fixed frame, but only coarsely (20 px for 1.6 cap
        // heights); these keep the exact figures for telling . from , from -.
        public float RelW;        // width / cap height
        public float RelTop;      // (top - cap line) / cap height     (0 = at the cap line)
        public float RelBottom;   // (bottom - baseline) / cap height  (0 = on the baseline)
        public float Aspect;      // width / height of the box

        public int Width { get { return Right - Left + 1; } }
        public int Height { get { return Bottom - Top + 1; } }
    }

    /// <summary>Where the chosen line of text sits, in analysis pixels.</summary>
    public sealed class LineInfo
    {
        public double CapTop;       // top of the tall letters
        public double Baseline;     // bottom of letters that sit on the line
        public double CapHeight { get { return Baseline - CapTop; } }
        public double TypicalWidth; // width of an ordinary single character
        public List<Blob> Blobs = new List<Blob>();
    }

    /// <summary>
    /// Picks the line of text that is the VALUE, cuts it into character-sized pieces, and
    /// normalises each piece into a fixed-size picture.
    /// <para/>
    /// A field crop is padded so a value is never clipped, which drags in the printed hint
    /// above it, the label beside it and the top of the next row. Reading all of that would
    /// be wrong, so the first job is deciding which line is the answer: the one with the
    /// most ink. (The value is typed, dark and large; a printed hint is small and thin.)
    /// </summary>
    public static class Segmenter
    {
        public static void Segment(Analysis a)
        {
            if (a.Glyphs.Count == 0 || a.MedianHeight <= 0) return;
            double medH = a.MedianHeight;

            // ---- 1. group the tall-enough pieces into lines by vertical position ----------
            var main = a.Glyphs.Where(b => b.Height >= 0.5 * medH).OrderBy(b => b.CenterY).ToList();
            if (main.Count == 0) return;

            var clusters = new List<List<Blob>>();
            var means = new List<double>();
            foreach (Blob b in main)
            {
                int hit = -1;
                for (int i = 0; i < clusters.Count; i++)
                    if (Math.Abs(means[i] - b.CenterY) <= 0.6 * medH) { hit = i; break; }
                if (hit < 0) { clusters.Add(new List<Blob> { b }); means.Add(b.CenterY); }
                else
                {
                    clusters[hit].Add(b);
                    means[hit] = clusters[hit].Average(x => (double)x.CenterY);
                }
            }

            // ---- 2. the value is the tall line nearest the middle of the crop --------------
            // First attempt picked the line with the MOST INK and was wrong exactly when it
            // mattered: the row below the cell (cut off at the crop edge, but a whole row of
            // small letters) outweighs a seven-letter name, and was chosen over it. What
            // separates the value from its neighbours is that it is TALLER than the cropped
            // pieces of the rows above and below, and it sits near the middle of the crop
            // (the crop is the field's own rectangle plus a little padding).
            double cropMid = a.Cleaned.Height / 2.0;
            double sigma = 0.3 * a.Cleaned.Height;
            List<Blob> best = clusters.OrderByDescending(c =>
            {
                var hs = c.Select(b => (double)b.Height).OrderBy(v => v).ToList();
                double dy = Math.Abs(c.Average(b => (double)b.CenterY) - cropMid);
                return hs[hs.Count / 2] * Math.Exp(-(dy * dy) / (sigma * sigma)) * (c.Count >= 2 ? 1.0 : 0.5);
            }).First();

            // ---- 3. its cap height and baseline, robust to descenders and lower case ------
            var tops = best.Select(b => (double)b.Top).OrderBy(v => v).ToList();
            var bottoms = best.Select(b => (double)b.Bottom).OrderBy(v => v).ToList();
            double baseline = Median(bottoms);
            baseline = Median(bottoms.Where(v => Math.Abs(v - baseline) <= 0.25 * medH).ToList(), baseline);
            double capTop = tops[Math.Max(0, (int)(tops.Count * 0.1))];
            if (baseline - capTop < 4) capTop = baseline - medH;   // degenerate: fall back to the median
            var line = new LineInfo { CapTop = capTop, Baseline = baseline };
            double capH = line.CapHeight;

            // ---- 4. everything belonging to that line, including dots and commas ----------
            double lo = capTop - 0.35 * capH, hi = baseline + 0.5 * capH;
            foreach (Blob b in a.Glyphs)
                if (b.CenterY >= lo && b.CenterY <= hi) line.Blobs.Add(b);
            line.Blobs = line.Blobs.OrderBy(b => b.Left).ToList();

            // ---- 5. join the parts of one character (dot + stem of an i, the two dots of :)
            var pieces = MergeStacked(line.Blobs, capH);

            // ---- 5b. throw away what the rule removal left behind --------------------------
            // Taking a rule out never leaves a perfectly clean edge: slivers of it survive just
            // under the baseline as short flat dashes and specks, and each one would be read
            // as a character. Measured on the first run: 28 "characters" found for a 13
            // character name, most of them these.
            pieces = pieces.Where(p => !IsRemnant(p, line)).ToList();

            // ---- 5c. keep only the contiguous run of text that is the value ----------------
            // A crop also catches the tail of the label in the cell beside it ("...ME" of
            // NAME). A value is one run of characters whose gaps are at most a word space; a
            // gap of more than a character and a half means a different piece of text, and
            // the run with the most ink is the value.
            pieces = LargestRun(pieces, 1.5 * capH);
            line.Blobs = pieces.SelectMany(p => p.Blobs).ToList();

            // ---- 6. typical single-character width, then split what is much wider --------
            // Upper quartile, not median: narrow letters (I, L, J, punctuation) outnumber wide
            // ones in a name, and a median pulled down by them made every W, M and N look
            // like two characters.
            var singles = pieces.Where(p => p.Height >= 0.6 * capH && p.Width <= 1.25 * capH)
                                .Select(p => (double)p.Width).OrderBy(v => v).ToList();
            line.TypicalWidth = singles.Count > 0
                ? Math.Max(singles[(int)(singles.Count * 0.75)], 0.55 * capH)
                : 0.6 * capH;

            a.Line = line;
            a.Pieces = pieces;
            foreach (Piece p in pieces)
            {
                int n = (int)Math.Round(p.Width / line.TypicalWidth);
                if (n >= 2 && p.Width > 1.25 * capH && n <= 6)
                {
                    foreach (int[] range in Cut(a, p, n, line.TypicalWidth))
                    {
                        GlyphCell cell = MakeCell(a, p, range[0], range[1], true);
                        if (cell != null) a.Cells.Add(cell);
                    }
                }
                else
                {
                    GlyphCell cell = MakeCell(a, p, p.Left, p.Right, false);
                    if (cell != null) a.Cells.Add(cell);
                }
            }
            a.Cells = a.Cells.OrderBy(c => c.Left).ToList();
        }

        /// <summary>
        /// A speck, or a flat sliver that is not where a hyphen would be. A hyphen is flat too,
        /// so flat pieces are kept only when they sit in the middle of the letters' height;
        /// a flat piece on or under the baseline is a leftover of the rule (an underscore is
        /// not part of any value on these forms).
        /// </summary>
        private static bool IsRemnant(Piece p, LineInfo line)
        {
            double capH = line.CapHeight;
            double area = p.Blobs.Sum(b => (double)b.Area);
            if (area < 0.015 * capH * capH) return true;

            // No letter lies entirely under the baseline, and none entirely above the capital
            // line: what does is a stray stroke or the next row's top edge.
            if (p.Top > line.Baseline - 0.05 * capH) return true;
            if (p.Bottom < line.CapTop) return true;

            bool flat = p.Height < 0.18 * capH && p.Width > 1.5 * p.Height;
            if (flat)
            {
                double cy = (p.Top + p.Bottom) / 2.0;
                bool hyphenBand = cy >= line.CapTop + 0.3 * capH && cy <= line.Baseline - 0.2 * capH
                                  && p.Width >= 0.2 * capH && p.Width <= 1.0 * capH;
                return !hyphenBand;
            }
            return p.Height < 0.10 * capH;
        }

        /// <summary>Splits the left-to-right pieces wherever the gap exceeds maxGap; keeps the run with the most ink.</summary>
        private static List<Piece> LargestRun(List<Piece> pieces, double maxGap)
        {
            if (pieces.Count < 2) return pieces;
            var ordered = pieces.OrderBy(p => p.Left).ToList();
            var runs = new List<List<Piece>> { new List<Piece> { ordered[0] } };
            int right = ordered[0].Right;
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Left - right > maxGap) runs.Add(new List<Piece>());
                runs[runs.Count - 1].Add(ordered[i]);
                if (ordered[i].Right > right) right = ordered[i].Right;
            }
            return runs.OrderByDescending(r => r.Sum(p => p.Blobs.Sum(b => (long)b.Area))).First();
        }

        // ===== one or more components that make up a single character =====================
        internal sealed class Piece
        {
            public List<Blob> Blobs = new List<Blob>();
            public int Left = int.MaxValue, Right = -1, Top = int.MaxValue, Bottom = -1;
            public int Width { get { return Right - Left + 1; } }
            public int Height { get { return Bottom - Top + 1; } }
            public void Add(Blob b)
            {
                Blobs.Add(b);
                if (b.Left < Left) Left = b.Left;
                if (b.Right > Right) Right = b.Right;
                if (b.Top < Top) Top = b.Top;
                if (b.Bottom > Bottom) Bottom = b.Bottom;
            }
        }

        /// <summary>
        /// Components that overlap horizontally and where at least one is small are parts of
        /// one character: the dot over an i or j, the two dots of a colon, a semicolon, an
        /// accent. Side-by-side letters do not overlap, and two big overlapping components
        /// are left alone, so this cannot weld neighbouring letters together.
        /// </summary>
        private static List<Piece> MergeStacked(List<Blob> blobs, double capH)
        {
            var pieces = new List<Piece>();
            foreach (Blob b in blobs)
            {
                Piece into = null;
                bool small = b.Height < 0.45 * capH;
                foreach (Piece p in pieces)
                {
                    int overlap = Math.Min(p.Right, b.Right) - Math.Max(p.Left, b.Left) + 1;
                    if (overlap <= 0) continue;
                    bool pSmall = p.Height < 0.45 * capH;
                    if (!small && !pSmall) continue;
                    int narrower = Math.Min(p.Width, b.Width);
                    if (overlap < 0.4 * narrower) continue;
                    int gap = Math.Max(p.Top, b.Top) - Math.Min(p.Bottom, b.Bottom);   // > 0 = apart
                    if (gap > 0.5 * capH) continue;
                    into = p; break;
                }
                if (into == null) { into = new Piece(); pieces.Add(into); }
                into.Add(b);
            }
            return pieces.OrderBy(p => p.Left).ToList();
        }

        // ===== split a wide piece into n parts at the thinnest columns near equal spacing =====
        // Day-2 placeholder: cuts where the ink is thinnest. Day 5 replaces the choice with a
        // search that scores each candidate split with the classifier.
        private static List<int[]> Cut(Analysis a, Piece p, int n, double typW)
        {
            int w = a.Cleaned.Width;
            var colInk = new int[p.Width];
            foreach (Blob b in p.Blobs)
                for (int y = b.Top; y <= b.Bottom; y++)
                    for (int x = b.Left; x <= b.Right; x++)
                        if (a.Blobs.Labels[y * w + x] == b.Id) colInk[x - p.Left]++;

            var cuts = new List<int>();
            for (int k = 1; k < n; k++)
            {
                double expected = k * (double)p.Width / n;
                int from = Math.Max(1, (int)(expected - 0.3 * typW));
                int to = Math.Min(p.Width - 1, (int)(expected + 0.3 * typW));
                int bestX = (int)Math.Round(expected), bestInk = int.MaxValue;
                for (int x = from; x <= to; x++)
                {
                    if (colInk[x] < bestInk || (colInk[x] == bestInk &&
                        Math.Abs(x - expected) < Math.Abs(bestX - expected)))
                    { bestInk = colInk[x]; bestX = x; }
                }
                cuts.Add(bestX);
            }

            var ranges = new List<int[]>();
            int start = 0;
            foreach (int c in cuts) { ranges.Add(new[] { p.Left + start, p.Left + c - 1 }); start = c; }
            ranges.Add(new[] { p.Left + start, p.Right });
            return ranges;
        }

        // ===== tight box + normalised picture of one character =============================
        internal static GlyphCell MakeCell(Analysis a, Piece p, int x0, int x1, bool split)
        {
            int w = a.Cleaned.Width, h = a.Cleaned.Height;
            var ids = new HashSet<int>(p.Blobs.Select(b => b.Id));

            // Tight box over the ink that belongs to this character and lies in [x0, x1].
            int l = int.MaxValue, r = -1, t = int.MaxValue, bt = -1, count = 0;
            foreach (Blob b in p.Blobs)
            {
                int bx0 = Math.Max(b.Left, x0), bx1 = Math.Min(b.Right, x1);
                for (int y = b.Top; y <= b.Bottom; y++)
                    for (int x = bx0; x <= bx1; x++)
                        if (a.Blobs.Labels[y * w + x] == b.Id)
                        {
                            if (x < l) l = x; if (x > r) r = x;
                            if (y < t) t = y; if (y > bt) bt = y;
                            count++;
                        }
            }
            if (count < 4) return null;   // a sliver left by a cut is not a character

            var cell = new GlyphCell { Left = l, Right = r, Top = t, Bottom = bt, FromSplit = split };
            double capHeight = a.Line.CapHeight;
            cell.RelW = (float)(cell.Width / capHeight);
            cell.RelTop = (float)((t - a.Line.CapTop) / capHeight);
            cell.RelBottom = (float)((bt - a.Line.Baseline) / capHeight);
            cell.Aspect = (float)cell.Width / Math.Max(1, cell.Height);

            // The picture is taken in a frame fixed to the LINE, not to the character: the
            // frame runs from a little above the cap line to below the baseline, so a full
            // stop stays tiny and sits low, a comma hangs under the line, and a capital
            // reaches the top. Scaling every character to fill the square would throw away
            // exactly the size and position that tell . from O and , from '.
            double capH = a.Line.CapHeight;
            double refTop = a.Line.CapTop - 0.20 * capH;
            double refH = 1.6 * capH;
            double step = refH / GlyphCell.Size;
            double left = (l + r) / 2.0 - refH / 2.0;   // centred on the character's box

            cell.Image = new float[GlyphCell.Size * GlyphCell.Size];
            for (int ty = 0; ty < GlyphCell.Size; ty++)
            {
                int sy0 = (int)Math.Floor(refTop + ty * step);
                int sy1 = Math.Max(sy0 + 1, (int)Math.Ceiling(refTop + (ty + 1) * step));
                for (int tx = 0; tx < GlyphCell.Size; tx++)
                {
                    int sx0 = (int)Math.Floor(left + tx * step);
                    int sx1 = Math.Max(sx0 + 1, (int)Math.Ceiling(left + (tx + 1) * step));
                    int ink = 0, total = 0;
                    for (int y = sy0; y < sy1; y++)
                    {
                        for (int x = sx0; x < sx1; x++)
                        {
                            total++;
                            if (x < x0 || x > x1 || x < 0 || y < 0 || x >= w || y >= h) continue;
                            if (ids.Contains(a.Blobs.Labels[y * w + x])) ink++;
                        }
                    }
                    cell.Image[ty * GlyphCell.Size + tx] = total == 0 ? 0f : (float)ink / total;
                }
            }
            return cell;
        }

        private static double Median(List<double> v, double fallback = 0)
        {
            if (v.Count == 0) return fallback;
            v = v.OrderBy(x => x).ToList();
            return v[v.Count / 2];
        }
    }
}
