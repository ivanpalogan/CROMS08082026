using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Decides where one character ends and the next begins, by asking the classifier.
    /// <para/>
    /// Thresholding makes two opposite mistakes: it GLUES letters that touch (typewriter ink,
    /// blur) and it BREAKS letters whose thin strokes fade (the H of "SHELLIAN" came out as
    /// three pieces: two stems and a crossbar). A splitter that only cuts glued clumps cannot
    /// repair the second kind, and the first splitter (thinnest column) made the first kind
    /// worse, because where two letters touch the join is usually thicker than the gap inside
    /// an H or an A.
    /// <para/>
    /// So nothing about the pieces is trusted. Every boundary that could be a character
    /// boundary becomes a NODE: the edges of each piece, plus the thin points inside wide
    /// pieces. A path through the nodes is a reading of the line, and its cost is the sum, over
    /// its characters, of how far each is from the nearest character the classifier knows,
    /// plus a small fixed price per character (otherwise cutting into slivers would always look
    /// cheap). The cheapest path is found by dynamic programming. Because a character may span
    /// several pieces OR part of one piece, the same search merges fragments and cuts clumps.
    /// </summary>
    internal static class SplitSearch
    {
        /// <summary>A candidate wider than this is not one character (M and W are the widest).</summary>
        private const double MaxWidthInCapHeights = 1.35;
        private const double MinWidthInCapHeights = 0.10;
        private const int MaxValleysPerPiece = 12;

        /// <summary>
        /// Evenly spaced extra candidates inside clumps. Measured 2026-10-06: no gain (42.0% vs
        /// 42.3% character similarity) and 19 minutes for one benchmark run instead of 90 seconds,
        /// so it is off. Kept as a switch so the negative result can be reproduced.
        /// </summary>
        public static bool DenseCandidates = false;

        /// <summary>
        /// false = search inside each clump only; pieces are never merged. true = one search over
        /// the whole line, which can also join a letter that thresholding broke apart - and, as
        /// measured, also joins letters that should stay apart.
        /// </summary>
        public static bool GlobalMerge = false;

        public static List<GlyphCell> Run(Analysis a, KnnClassifier scorer, double lambda)
        {
            var result = new List<GlyphCell>();
            if (a.Line == null || a.Pieces.Count == 0) return result;
            if (GlobalMerge) return Search(a, a.Pieces, scorer, lambda);

            double capH = a.Line.CapHeight;
            foreach (Segmenter.Piece p in a.Pieces.OrderBy(p => p.Left))
            {
                // One character's width or less: no search, exactly as the plain splitter treats it.
                if (p.Width <= 1.0 * capH)
                {
                    GlyphCell one = Segmenter.MakeCell(a, p, p.Left, p.Right, false);
                    if (one != null) result.Add(one);
                    continue;
                }
                List<GlyphCell> best = Search(a, new List<Segmenter.Piece> { p }, scorer, lambda);
                if (best.Count > 0) result.AddRange(best);
                else
                    foreach (GlyphCell c in a.Cells.Where(c => c.Left >= p.Left && c.Right <= p.Right))
                        result.Add(c);   // no path: keep what the plain splitter made
            }
            return result.OrderBy(c => c.Left).ToList();
        }

        private static List<GlyphCell> Search(Analysis a, List<Segmenter.Piece> pieceList, KnnClassifier scorer, double lambda)
        {
            var empty = new List<GlyphCell>();
            double capH = a.Line.CapHeight;
            var pieces = pieceList.OrderBy(p => p.Left).ToList();

            // ---- nodes ----------------------------------------------------------------------
            var set = new SortedSet<int>();
            foreach (Segmenter.Piece p in pieces)
            {
                set.Add(p.Left);
                set.Add(p.Right + 1);
                if (p.Width > 0.45 * capH)
                    foreach (int x in Valleys(a, p, capH)) set.Add(x);

                // A clump wider than one character may have NO thin point at the true boundary:
                // where typewriter ink or blur has fused two letters, the join is as thick as the
                // letters (seen on "van" in the 1993 photocopy). So a clump also gets evenly
                // spaced candidates and the classifier decides, as it has to anyway.
                if (DenseCandidates && p.Width > 0.9 * capH)
                {
                    int step = Math.Max(2, (int)(0.08 * capH));
                    for (int x = p.Left + step; x < p.Right; x += step) set.Add(x);
                }
            }
            List<int> nodes = set.ToList();
            int m = nodes.Count - 1;
            if (m < 1) return empty;

            // Ink per column across the whole line, to tell an ink-free gap from a character.
            int origin = nodes[0];
            var colInk = new int[nodes[m] - origin + 1];
            int w = a.Cleaned.Width;
            foreach (Segmenter.Piece p in pieces)
                foreach (Blob b in p.Blobs)
                    for (int y = b.Top; y <= b.Bottom; y++)
                        for (int x = b.Left; x <= b.Right; x++)
                            if (a.Blobs.Labels[y * w + x] == b.Id) colInk[x - origin]++;
            var prefix = new int[colInk.Length + 1];
            for (int i = 0; i < colInk.Length; i++) prefix[i + 1] = prefix[i] + colInk[i];
            Func<int, int, int> inkIn = (x0, x1) => prefix[Math.Min(colInk.Length, x1 - origin)] - prefix[Math.Max(0, x0 - origin)];

            int minW = Math.Max(3, (int)(MinWidthInCapHeights * capH));
            int maxW = (int)(MaxWidthInCapHeights * capH);

            var cellCache = new Dictionary<int, GlyphCell>();
            var costCache = new Dictionary<int, float>();
            Func<int, int, float> edge = (i, j) =>
            {
                int key = i * 4096 + j;
                float c;
                if (costCache.TryGetValue(key, out c)) return c;
                GlyphCell cell = CellFor(a, pieces, nodes[i], nodes[j] - 1, !(i == 0 && j == m));
                if (cell == null) c = 5f;
                else
                {
                    // How badly does this look like a character? MEASURED on the glyph data:
                    // the distance to the nearest example barely separates a glyph the classifier
                    // reads right from one it reads wrong (median 0.57 against 0.69 on real
                    // glyphs, with the middle halves overlapping), so on its own it let two
                    // letters merge into one "character". The share of the neighbours' vote does
                    // separate them (0.86 right against 0.57 wrong): a real letter is surrounded
                    // by its own kind, a clump or a half-letter is not. So the vote leads, and
                    // the distance only adds a mild penalty once it is clearly beyond the usual.
                    float nearest;
                    Prediction[] r = scorer.Rank(Features.FromCell(cell), 1, out nearest);
                    float share = r.Length > 0 ? r[0].Score : 0f;
                    // A picture the classifier takes for a non-character costs more than any letter.
                    c = (r.Length > 0 && r[0].Label == Charset.Reject)
                        ? 1.2f + share
                        : (1f - share) + 0.5f * Math.Max(0f, nearest - 0.45f);
                    cellCache[key] = cell;
                }
                costCache[key] = c;
                return c;
            };

            // ---- dynamic programming ----------------------------------------------------------
            var cost = new float[m + 1];
            var prev = new int[m + 1];
            for (int j = 1; j <= m; j++) { cost[j] = float.MaxValue; prev[j] = -1; }

            for (int j = 1; j <= m; j++)
            {
                for (int i = 0; i < j; i++)
                {
                    if (cost[i] == float.MaxValue) continue;
                    int width = nodes[j] - nodes[i];
                    float total;
                    if (inkIn(nodes[i], nodes[j]) == 0)
                        total = cost[i];                       // a gap between characters or words: free
                    else
                    {
                        if (width < minW || width > maxW) continue;
                        total = cost[i] + edge(i, j) + (float)lambda;
                    }
                    if (total < cost[j]) { cost[j] = total; prev[j] = i; }
                }
            }
            if (cost[m] == float.MaxValue) return empty;

            var path = new List<int>();
            for (int j = m; j > 0; j = prev[j]) path.Add(j);
            path.Reverse();

            var cells = new List<GlyphCell>();
            int from = 0;
            foreach (int to in path)
            {
                if (inkIn(nodes[from], nodes[to]) > 0)
                {
                    GlyphCell c;
                    if (!cellCache.TryGetValue(from * 4096 + to, out c))
                        c = CellFor(a, pieces, nodes[from], nodes[to] - 1, !(from == 0 && to == m));
                    if (c != null) cells.Add(c);
                }
                from = to;
            }
            return cells;
        }

        // The character that occupies columns [x0, x1]: every piece that reaches into that
        // range contributes the ink it has inside it.
        internal static GlyphCell CellFor(Analysis a, List<Segmenter.Piece> pieces, int x0, int x1, bool split)
        {
            var sp = new Segmenter.Piece();
            foreach (Segmenter.Piece p in pieces)
                if (p.Right >= x0 && p.Left <= x1)
                    foreach (Blob b in p.Blobs) sp.Add(b);
            if (sp.Blobs.Count == 0) return null;
            return Segmenter.MakeCell(a, sp, x0, x1, split);
        }

        /// <summary>
        /// Thin points inside a wide piece: local minima of its column ink profile (smoothed),
        /// the thinnest few that are not crowded together. They are only CANDIDATES - an O has a
        /// thin point in its middle - and the search decides.
        /// </summary>
        private static List<int> Valleys(Analysis a, Segmenter.Piece p, double capH)
        {
            int w = a.Cleaned.Width;
            var col = new float[p.Width];
            foreach (Blob b in p.Blobs)
                for (int y = b.Top; y <= b.Bottom; y++)
                    for (int x = Math.Max(b.Left, p.Left); x <= Math.Min(b.Right, p.Right); x++)
                        if (a.Blobs.Labels[y * w + x] == b.Id) col[x - p.Left]++;

            var sm = new float[col.Length];
            for (int i = 0; i < col.Length; i++)
            {
                float l = col[Math.Max(0, i - 1)], r = col[Math.Min(col.Length - 1, i + 1)];
                sm[i] = (l + 2 * col[i] + r) / 4f;
            }

            var minima = new List<int>();
            for (int i = 1; i < sm.Length - 1; i++)
                if (sm[i] <= sm[i - 1] && sm[i] <= sm[i + 1]) minima.Add(i);

            int spacing = Math.Max(2, (int)(0.10 * capH));
            int limit = Math.Min(MaxValleysPerPiece, 2 + (int)(6.0 * p.Width / capH));
            var chosen = new List<int>();
            foreach (int x in minima.OrderBy(x => sm[x]))
            {
                if (chosen.Any(c => Math.Abs(c - x) < spacing)) continue;
                chosen.Add(x);
                if (chosen.Count >= limit) break;
            }
            return chosen.Select(x => p.Left + x).ToList();
        }
    }
}
