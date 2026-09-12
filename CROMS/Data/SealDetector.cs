using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace CROMS.Data
{
    /// <summary>
    /// Finds the INK MARKS on a scanned certificate that are not text — the PSA security
    /// emblem, a dry seal, the registrar's rubber stamp, and handwritten signatures — so
    /// they can be treated as images rather than as words.
    /// <para/>
    /// Measured on the office's five samples, it finds the PSA emblem on the 2007 birth
    /// certificate and three signatures across the birth and death certificates. Signatures
    /// coming out of the same detector is not an accident: for the purpose here they are the
    /// same problem — ink that OCR reads as garbage and that must never become a field value
    /// (this project's own log records a signature being read as "excessive" and an
    /// informant line as "Tite oF ADMIN STRATIVE ADEM!"). What differs is only what an
    /// operator may then DO with the crop, which is why saving one as the office logo or
    /// stamp shows the picture first.
    /// <para/>
    /// WHY THIS EXISTS. Tesseract has no idea what a seal is. It reads one as a handful of
    /// nonsense words, and if a seal happens to sit inside a field's region that nonsense
    /// becomes the field's VALUE — a garbled word saved as somebody's residence or as the
    /// solemnizing officer. That is the worst class of error on this project's record: it
    /// looks filled in. So the seals are located first, and anything read from inside one
    /// is refused.
    /// <para/>
    /// HOW. A seal is not distinguished from text by darkness — printed text is just as
    /// dark. It is distinguished by SHAPE and DENSITY: a line of text is wide and thin and
    /// mostly white between the letters, while a seal is roughly as tall as it is wide and
    /// its ink is spread across the whole area. So the page is reduced to a coarse ink-
    /// density grid, high-density cells are grouped, and a group is only accepted when its
    /// bounding box is close to square AND its interior is densely inked.
    /// <para/>
    /// This is deliberately conservative. A missed seal costs nothing — the field is read
    /// as it was before. A false positive would suppress a real value, so the thresholds
    /// are set to reject rather than guess.
    /// </summary>
    public static class SealDetector
    {
        /// <summary>A seal or stamp found on the page, in 0-1 page coordinates.</summary>
        public class Seal
        {
            public RectangleF Rect;
            /// <summary>Share of the box that is ink, 0-1. A dry seal is lighter than a
            /// rubber stamp, so this is reported rather than thresholded twice.</summary>
            public float InkRatio;
            /// <summary>Roughly how round the box is: 1 is square.</summary>
            public float Squareness;
        }

        // The grid the page is reduced to. Coarse on purpose: at this scale a line of body
        // text occupies one or two cell rows, so it cannot form a square group.
        private const int Cells = 64;
        /// <summary>A cell counts as inked above this share of dark pixels. Body text at
        /// this scale sits around 0.10-0.20; a seal's interior is well above.</summary>
        private const float CellInk = 0.28f;
        /// <summary>Reject anything not close to square — this is what excludes text rows,
        /// ruled lines and table borders.</summary>
        private const float MinSquareness = 0.62f;
        /// <summary>Ink share the whole box must reach. A hollow shape (a table cell
        /// outline) fails this even when its bounding box is square.</summary>
        private const float MinBoxInk = 0.30f;
        /// <summary>Size bounds as a share of the page's short side. Below this it is a
        /// blot or a full stop; above it, a shadow or a scanning artefact.</summary>
        private const float MinSide = 0.045f, MaxSide = 0.30f;

        /// <summary>
        /// The seals and stamps on this page, largest first. Never throws — a detection
        /// failure must not stop a document being read.
        /// </summary>
        public static List<Seal> Detect(Bitmap page)
        {
            var found = new List<Seal>();
            if (page == null || page.Width < 200 || page.Height < 200) return found;

            try
            {
                bool[,] inked = InkGrid(page, out int gw, out int gh);
                foreach (List<Point> group in Groups(inked, gw, gh))
                {
                    int minX = group.Min(p => p.X), maxX = group.Max(p => p.X);
                    int minY = group.Min(p => p.Y), maxY = group.Max(p => p.Y);
                    int boxW = maxX - minX + 1, boxH = maxY - minY + 1;

                    // Cells are square in grid space but the page is not, so squareness has
                    // to be measured in PAGE proportions, not in cell counts.
                    float pxW = boxW / (float)gw * page.Width;
                    float pxH = boxH / (float)gh * page.Height;
                    float square = Math.Min(pxW, pxH) / Math.Max(pxW, pxH);
                    if (square < MinSquareness) continue;

                    float shortSide = Math.Min(page.Width, page.Height);
                    float side = Math.Max(pxW, pxH) / shortSide;
                    if (side < MinSide || side > MaxSide) continue;

                    float boxInk = group.Count / (float)(boxW * boxH);
                    if (boxInk < MinBoxInk) continue;

                    var rect = new RectangleF(minX / (float)gw, minY / (float)gh,
                                              boxW / (float)gw, boxH / (float)gh);

                    // The density-and-shape test alone is NOT enough, and this was
                    // measured rather than assumed: on the office's own samples it
                    // accepted 12 dense TEXT BLOCKS alongside the 1 real seal, and one of
                    // those blocks sat over the birth weight — so suppressing readings
                    // inside it would have blanked a correct value. The banding test below
                    // separates them cleanly.
                    if (!IsSolidEmblem(page, rect)) continue;

                    found.Add(new Seal
                    {
                        Rect = rect,
                        InkRatio = boxInk,
                        Squareness = square
                    });
                }
            }
            catch { return new List<Seal>(); }

            return found.OrderByDescending(s => s.Rect.Width * s.Rect.Height).ToList();
        }

        /// <summary>
        /// Does this field's region sit inside a seal? Judged by how much of the FIELD is
        /// covered, not the other way round: a small field wholly inside a large seal is
        /// the case that matters, and comparing against the seal's area would miss it.
        /// </summary>
        public static bool OverlapsSeal(RectangleF fieldRect, IEnumerable<Seal> seals,
                                        float minCover = 0.55f)
        {
            if (seals == null || fieldRect.Width <= 0 || fieldRect.Height <= 0) return false;
            float area = fieldRect.Width * fieldRect.Height;

            foreach (Seal s in seals)
            {
                RectangleF hit = RectangleF.Intersect(fieldRect, s.Rect);
                if (hit.IsEmpty) continue;
                if (hit.Width * hit.Height / area >= minCover) return true;
            }
            return false;
        }

        /// <summary>Crop a detected seal out of the page at full resolution, for storing as
        /// the office's logo or stamp.</summary>
        public static Bitmap Crop(Bitmap page, Seal seal, float margin = 0.02f)
        {
            if (page == null || seal == null) return null;

            var r = seal.Rect;
            r.Inflate(margin, margin);
            int x = (int)Math.Max(0, r.X * page.Width);
            int y = (int)Math.Max(0, r.Y * page.Height);
            int w = (int)Math.Min(page.Width - x, r.Width * page.Width);
            int h = (int)Math.Min(page.Height - y, r.Height * page.Height);
            if (w <= 0 || h <= 0) return null;

            var crop = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(crop))
                g.DrawImage(page, new Rectangle(0, 0, w, h),
                            new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
            return crop;
        }

        /// <summary>Reject a candidate above this share of near-empty scanlines.</summary>
        private const float MaxGapLines = 0.25f;
        /// <summary>Reject a candidate with this many separate bands of near-empty
        /// scanlines. Two allows for a margin above and below the emblem.</summary>
        private const int MaxGapRuns = 2;

        /// <summary>
        /// Is this candidate a solid emblem rather than a block of text?
        /// <para/>
        /// Density and squareness cannot tell them apart: a tightly set block of small
        /// print is just as dark and, boxed, just as square as a seal. What separates them
        /// is the LEADING — the white line between one row of text and the next. So the
        /// candidate is read line by line at full resolution and its near-empty scanlines
        /// counted: text is banded, an emblem is continuous.
        /// <para/>
        /// MEASURED on the office's five samples (values in the code so the thresholds are
        /// not mistaken for guesses). The one genuine seal — the PSA emblem on nice.jpg —
        /// gives 15% gap lines in 1 run. The twelve text-block candidates give 27-67% in
        /// 4-10 runs. There is no overlap, so the cut sits at 25% and 2 runs with room on
        /// both sides.
        /// </summary>
        private static bool IsSolidEmblem(Bitmap page, RectangleF rect)
        {
            int x0 = (int)Math.Max(0, rect.X * page.Width);
            int y0 = (int)Math.Max(0, rect.Y * page.Height);
            int w = (int)Math.Min(page.Width - x0, rect.Width * page.Width);
            int h = (int)Math.Min(page.Height - y0, rect.Height * page.Height);
            if (w < 8 || h < 8) return false;

            BitmapData bd = page.LockBits(new Rectangle(x0, y0, w, h),
                                          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                var row = new byte[stride];

                // Threshold from this region's own mean, so a light dry seal on tinted
                // paper is judged on the same footing as a black rubber stamp.
                long sum = 0; long n = 0;
                for (int y = 0; y < h; y += 3)
                {
                    Marshal.Copy(bd.Scan0 + y * stride, row, 0, stride);
                    for (int x = 0; x < w; x += 3)
                    {
                        int i = x * 3;
                        sum += (row[i] * 29 + row[i + 1] * 150 + row[i + 2] * 77) >> 8;
                        n++;
                    }
                }
                int mean = n == 0 ? 128 : (int)(sum / n);
                int threshold = Math.Max(40, Math.Min(200, (int)(mean * 0.62)));

                int gapLines = 0, gapRuns = 0;
                bool inGap = false;

                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(bd.Scan0 + y * stride, row, 0, stride);
                    int ink = 0;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 3;
                        if (((row[i] * 29 + row[i + 1] * 150 + row[i + 2] * 77) >> 8) < threshold)
                            ink++;
                    }

                    if (ink / (float)w < 0.04f)
                    {
                        gapLines++;
                        if (!inGap) { gapRuns++; inGap = true; }
                    }
                    else inGap = false;
                }

                return gapLines / (float)h < MaxGapLines && gapRuns <= MaxGapRuns;
            }
            finally { page.UnlockBits(bd); }
        }

        // ---- the coarse ink grid -------------------------------------------------

        /// <summary>
        /// Reduce the page to a grid of "is this cell densely inked". Read through LockBits
        /// in one pass — the same lesson as OcrService's preprocessing, where GetPixel over
        /// a full page cost more than the recognition itself.
        /// </summary>
        private static bool[,] InkGrid(Bitmap page, out int gw, out int gh)
        {
            // Keep the cells roughly square in page proportions, so a shape's squareness is
            // measurable from the grid at all.
            if (page.Width >= page.Height)
            {
                gw = Cells;
                gh = Math.Max(8, (int)Math.Round(Cells * page.Height / (double)page.Width));
            }
            else
            {
                gh = Cells;
                gw = Math.Max(8, (int)Math.Round(Cells * page.Width / (double)page.Height));
            }

            var dark = new int[gw, gh];
            var total = new int[gw, gh];

            BitmapData bd = page.LockBits(new Rectangle(0, 0, page.Width, page.Height),
                                          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                var row = new byte[stride];

                // A cast tint (these scans are often yellowed) shifts absolute darkness, so
                // the threshold is taken from the page's own mean rather than fixed.
                long sum = 0; long n = 0;
                for (int y = 0; y < page.Height; y += 4)
                {
                    Marshal.Copy(bd.Scan0 + y * stride, row, 0, stride);
                    for (int x = 0; x < page.Width; x += 4)
                    {
                        int i = x * 3;
                        sum += (row[i] * 29 + row[i + 1] * 150 + row[i + 2] * 77) >> 8;
                        n++;
                    }
                }
                int mean = n == 0 ? 128 : (int)(sum / n);
                int threshold = Math.Max(40, Math.Min(200, (int)(mean * 0.62)));

                for (int y = 0; y < page.Height; y++)
                {
                    Marshal.Copy(bd.Scan0 + y * stride, row, 0, stride);
                    int cy = Math.Min(gh - 1, y * gh / page.Height);

                    for (int x = 0; x < page.Width; x++)
                    {
                        int i = x * 3;
                        int grey = (row[i] * 29 + row[i + 1] * 150 + row[i + 2] * 77) >> 8;
                        int cx = Math.Min(gw - 1, x * gw / page.Width);
                        total[cx, cy]++;
                        if (grey < threshold) dark[cx, cy]++;
                    }
                }
            }
            finally { page.UnlockBits(bd); }

            var inked = new bool[gw, gh];
            for (int x = 0; x < gw; x++)
                for (int y = 0; y < gh; y++)
                    inked[x, y] = total[x, y] > 0 && dark[x, y] / (float)total[x, y] >= CellInk;
            return inked;
        }

        /// <summary>Connected groups of inked cells (8-connected, flood filled iteratively
        /// so a large group cannot overflow the stack).</summary>
        private static List<List<Point>> Groups(bool[,] inked, int gw, int gh)
        {
            var groups = new List<List<Point>>();
            var seen = new bool[gw, gh];
            var stack = new Stack<Point>();

            for (int x = 0; x < gw; x++)
                for (int y = 0; y < gh; y++)
                {
                    if (!inked[x, y] || seen[x, y]) continue;

                    var group = new List<Point>();
                    stack.Push(new Point(x, y));
                    seen[x, y] = true;

                    while (stack.Count > 0)
                    {
                        Point p = stack.Pop();
                        group.Add(p);
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = p.X + dx, ny = p.Y + dy;
                                if (nx < 0 || ny < 0 || nx >= gw || ny >= gh) continue;
                                if (!inked[nx, ny] || seen[nx, ny]) continue;
                                seen[nx, ny] = true;
                                stack.Push(new Point(nx, ny));
                            }
                    }
                    groups.Add(group);
                }
            return groups;
        }
    }
}
