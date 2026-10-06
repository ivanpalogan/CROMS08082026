using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.OwnOcr
{
    public sealed class RuleResult
    {
        /// <summary>1 where a printed table rule was found (already thickened by a pixel).</summary>
        public byte[] Mask;
        public int HorizontalRules;
        public int VerticalRules;
        /// <summary>Every candidate horizontal run group, kept or not, for diagnosis: left, top, right, bottom, kept(0/1).</summary>
        public System.Collections.Generic.List<int[]> HorizontalCandidates = new System.Collections.Generic.List<int[]>();
        /// <summary>The ink with the rules taken out.</summary>
        public BinaryImage Cleaned;
    }

    /// <summary>
    /// Finds and removes the ruled lines of a form.
    /// <para/>
    /// PSA certificates are bordered tables, so a field crop usually carries a rule along
    /// its top or bottom (and sometimes a cell edge down one side). Left in, a rule joins
    /// every letter above it into one giant connected component and the characters can
    /// never be separated.
    /// <para/>
    /// A rule is not just "a long horizontal run": a letter such as T or Z has a horizontal
    /// stroke too, and a photographed page is never perfectly level, so a long thin line
    /// reaches the camera as a row of short runs stepping up or down. So the work is done
    /// in two steps: first mark every run that is plausibly part of a rule, then group
    /// those runs into connected pieces and keep only the pieces that are long and thin as
    /// a WHOLE. A tilted line is one long thin piece; a letter's stroke is a short one.
    /// </summary>
    public static class LineRemover
    {
        public static RuleResult Remove(BinaryImage ink)
        {
            int w = ink.Width, h = ink.Height;
            var rules = new byte[w * h];
            var result = new RuleResult { Mask = rules };

            // ---- horizontal rules -------------------------------------------------------
            // A row segment counts as a candidate when it is longer than any letter stroke
            // could be (a stroke is at most about a character wide, and a character is not
            // wider than about half the crop's height).
            int hRun = Math.Max(20, (int)(h * 0.6));
            byte[] hMask = HorizontalRuns(ink, hRun, 1);
            Labeling hl = ConnectedComponents.Label(hMask, w, h);

            var keepH = new HashSet<int>();
            int minWidth = Math.Max(60, (int)(0.35 * w));
            // A thin or faint rule does not survive thresholding in one piece: on the birth
            // sample's City/Municipality cell it came out as three fragments with gaps of 3-4
            // pixels (x 0-173, 177-261, 264-473), each shorter than the length test, so the
            // left half of the rule stayed in the ink and formed an underscore under every
            // letter. So fragments that lie on the same line are joined into one candidate
            // BEFORE the length test is applied.
            var cands = hl.Blobs.Where(b => b.Width >= 20).OrderBy(b => b.Left).ToList();
            int joinGap = Math.Max(12, (int)(0.04 * w));
            var groups = new List<List<Blob>>();
            foreach (Blob b in cands)
            {
                List<Blob> into = null;
                foreach (List<Blob> g in groups)
                {
                    int gRight = g.Max(x => x.Right);
                    int gTop = g.Min(x => x.Top), gBottom = g.Max(x => x.Bottom);
                    bool sameLine = b.Top <= gBottom + 4 && b.Bottom >= gTop - 4;
                    if (sameLine && b.Left - gRight <= joinGap) { into = g; break; }
                }
                if (into == null) groups.Add(new List<Blob> { b }); else into.Add(b);
            }
            foreach (List<Blob> g in groups)
            {
                int gl = g.Min(x => x.Left), gr = g.Max(x => x.Right);
                int gt = g.Min(x => x.Top), gb = g.Max(x => x.Bottom);
                int gw = gr - gl + 1, gh = gb - gt + 1;
                // Long, and thin for its length (allowing for a little tilt).
                bool keep = gw >= minWidth && gh <= gw * 0.12 + 3;
                if (keep) foreach (Blob b in g) keepH.Add(b.Id);
                result.HorizontalCandidates.Add(new[] { gl, gt, gr, gb, keep ? 1 : 0 });
            }
            result.HorizontalRules = groups.Count(g =>
            {
                int gw = g.Max(x => x.Right) - g.Min(x => x.Left) + 1;
                int gh = g.Max(x => x.Bottom) - g.Min(x => x.Top) + 1;
                return gw >= minWidth && gh <= gw * 0.12 + 3;
            });

            // ---- vertical rules ---------------------------------------------------------
            // A cell edge runs the full height of the crop; a letter stem never does, so the
            // bar is set high (the stem of an I or l is about half the crop's height).
            int vRun = Math.Max(10, (int)(h * 0.35));
            byte[] vMask = VerticalRuns(ink, vRun, 1);
            Labeling vl = ConnectedComponents.Label(vMask, w, h);

            var keepV = new HashSet<int>();
            int minHeight = Math.Max(20, (int)(0.75 * h));
            foreach (Blob b in vl.Blobs)
            {
                if (b.Height >= minHeight && b.Width <= b.Height * 0.12 + 3)
                    keepV.Add(b.Id);
            }
            result.VerticalRules = keepV.Count;

            // ---- build the rule mask, a pixel thicker than the detected runs ------------
            // Anti-aliased edges leave a faint fringe either side of a rule; one pixel of
            // growth takes it with the line so it does not survive as a row of specks.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    int hl_id = hl.Labels[i];
                    if (hl_id != 0 && keepH.Contains(hl_id))
                    {
                        rules[i] = 1;
                        if (y > 0) rules[i - w] = 1;
                        if (y < h - 1) rules[i + w] = 1;
                    }
                    int vl_id = vl.Labels[i];
                    if (vl_id != 0 && keepV.Contains(vl_id))
                    {
                        rules[i] = 1;
                        if (x > 0) rules[i - 1] = 1;
                        if (x < w - 1) rules[i + 1] = 1;
                    }
                }
            }

            var cleaned = new byte[w * h];
            for (int i = 0; i < cleaned.Length; i++)
                cleaned[i] = (byte)((ink.Ink[i] != 0 && rules[i] == 0) ? 1 : 0);
            result.Cleaned = new BinaryImage(w, h, cleaned);
            return result;
        }

        /// <summary>
        /// Marks every horizontal run of ink at least <paramref name="minRun"/> long. A gap of
        /// up to <paramref name="gap"/> pixels is bridged, because a printed line is rarely
        /// perfectly unbroken after thresholding.
        /// </summary>
        private static byte[] HorizontalRuns(BinaryImage ink, int minRun, int gap)
        {
            int w = ink.Width, h = ink.Height;
            var mask = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int o = y * w;
                int start = -1, last = -1;
                for (int x = 0; x <= w; x++)
                {
                    bool on = x < w && ink.Ink[o + x] != 0;
                    if (on)
                    {
                        if (start < 0) start = x;
                        last = x;
                    }
                    else if (start >= 0 && (x == w || x - last > gap))
                    {
                        if (last - start + 1 >= minRun)
                            for (int k = start; k <= last; k++) mask[o + k] = 1;
                        start = -1;
                    }
                }
            }
            return mask;
        }

        private static byte[] VerticalRuns(BinaryImage ink, int minRun, int gap)
        {
            int w = ink.Width, h = ink.Height;
            var mask = new byte[w * h];
            for (int x = 0; x < w; x++)
            {
                int start = -1, last = -1;
                for (int y = 0; y <= h; y++)
                {
                    bool on = y < h && ink.Ink[y * w + x] != 0;
                    if (on)
                    {
                        if (start < 0) start = y;
                        last = y;
                    }
                    else if (start >= 0 && (y == h || y - last > gap))
                    {
                        if (last - start + 1 >= minRun)
                            for (int k = start; k <= last; k++) mask[k * w + x] = 1;
                        start = -1;
                    }
                }
            }
            return mask;
        }
    }
}
