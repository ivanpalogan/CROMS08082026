using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace CROMS.Data
{
    /// <summary>
    /// One recognised word and where it sits on the page. PSA forms are TABLES —
    /// on Municipal Form 97 the husband's and the wife's values share the same
    /// printed line, so flat text alone cannot say which spouse a value belongs to.
    /// The box is in the prepared image's pixel space; compare it against
    /// <see cref="OcrResult.PageWidth"/> rather than using absolute numbers.
    /// </summary>
    public class OcrWord
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Tesseract's confidence for THIS word, 0-100. The per-field score is
        /// the mean of the words that produced the value, so a field can be flagged even
        /// when the page as a whole read well.</summary>
        public int Confidence { get; set; }

        public int CenterX { get { return X + Width / 2; } }
        public int CenterY { get { return Y + Height / 2; } }
    }

    /// <summary>Result of an OCR pass: extracted text, mean confidence (0-100) and word layout.</summary>
    public class OcrResult
    {
        public string Text { get; set; }
        public int Confidence { get; set; }

        /// <summary>Every recognised word with its position, in reading order.</summary>
        public List<OcrWord> Words { get; set; }

        /// <summary>Size of the image the words were read from (the PREPARED image).</summary>
        public int PageWidth { get; set; }
        public int PageHeight { get; set; }

        public OcrResult() { Words = new List<OcrWord>(); }
    }

    /// <summary>
    /// Tesseract OCR wrapper. Based on the team's old system, with two changes that
    /// matter on real scans of PSA forms:
    /// <list type="bullet">
    /// <item>ADAPTIVE (local) thresholding instead of a single global cut-off. A
    /// global threshold fails on a document that is lit unevenly or printed on
    /// tinted security paper — it blacks out the darker side and washes out faint
    /// print. Measured on a real Certificate of Marriage, the global pipeline lost
    /// the entire middle of the form; the adaptive one reads it. (The mobile
    /// scanner hit the same wall and was fixed the same way.)</item>
    /// <item>No 2x upscale. Upscaling a downscaled scan cannot recover strokes it
    /// already threw away; recognition is better straight off the original pixels.</item>
    /// </list>
    /// Pixels are read through LockBits + Marshal.Copy rather than
    /// GetPixel/SetPixel, which also cut a full-page pass from ~22s to ~7s.
    /// Native libs (x86/x64) are copied next to the exe by the project; language
    /// data is "eng".
    /// </summary>
    public static class OcrService
    {
        /// <summary>
        /// GDI+ (System.Drawing) is not safe under concurrent access in this environment -
        /// measured directly: two DIFFERENT Bitmap instances, each touched by its own
        /// thread, still threw "Object is currently in use elsewhere" out of Image.Save,
        /// and the same happened on a plain Width property read. That rules out "only
        /// shared instances are unsafe" - every Graphics.DrawImage / Image.Save in this
        /// pipeline (Scale, Render, ReadRegion's crop, Recognise's encode) now goes
        /// through this ONE process-wide lock. The genuinely expensive, genuinely
        /// parallel-safe part - the native Tesseract engine.Process() call - stays
        /// outside every lock and is what field/page-level concurrency actually speeds up.
        /// </summary>
        internal static readonly object GdiLock = new object();

        /// <summary>True if the OCR engine's language data can be located.</summary>
        public static bool IsAvailable()
        {
            return File.Exists(Path.Combine(TessDataPath(), "eng.traineddata"));
        }

        internal static string TessData() { return TessDataPath(); }

        private static string TessDataPath()
        {
            const string installed = @"C:\Program Files\Tesseract-OCR\tessdata";
            if (File.Exists(Path.Combine(installed, "eng.traineddata")))
                return installed;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        /// <summary>OCR at the image's own resolution.</summary>
        public static OcrResult Run(Bitmap source)
        {
            return Run(source, Math.Max(source.Width, source.Height));
        }

        /// <summary>
        /// OCR with the page first scaled so its longest side is
        /// <paramref name="targetLongSide"/> pixels. Resolution changes what Tesseract
        /// can read and there is no single best value: measured on real scans, a
        /// Certificate of Live Birth reads best around 2400px, a Certificate of
        /// Marriage best at full size, and a small 755px Certificate of Death only
        /// once it is scaled UP. Callers try more than one and keep the better result.
        /// </summary>
        public static OcrResult Run(Bitmap source, int targetLongSide)
        {
            using (Bitmap scaled = Scale(source, targetLongSide))
            using (Bitmap prepared = Preprocess(scaled))
            using (var engine = new TesseractEngine(TessDataPath(), "eng", EngineMode.Default))
            {
                engine.SetVariable("user_defined_dpi", "300");
                using (var ms = new MemoryStream())
                {
                    prepared.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                    using (var page = engine.Process(pix, PageSegMode.Auto))
                    {
                        string text = page.GetText();
                        var result = new OcrResult
                        {
                            Text = string.IsNullOrEmpty(text) ? "" : text.Trim(),
                            Confidence = (int)Math.Round(page.GetMeanConfidence() * 100),
                            PageWidth = prepared.Width,
                            PageHeight = prepared.Height
                        };
                        ReadWords(page, result.Words);
                        return result;
                    }
                }
            }
        }

        /// <summary>Collect every recognised word and its bounding box.</summary>
        internal static void CollectWords(Page page, List<OcrWord> into) { ReadWords(page, into); }

        private static void ReadWords(Page page, List<OcrWord> into)
        {
            using (ResultIterator iter = page.GetIterator())
            {
                iter.Begin();
                do
                {
                    Rect box;
                    if (!iter.TryGetBoundingBox(PageIteratorLevel.Word, out box)) continue;

                    string word = iter.GetText(PageIteratorLevel.Word);
                    if (string.IsNullOrWhiteSpace(word)) continue;

                    into.Add(new OcrWord
                    {
                        Text = word.Trim(),
                        Confidence = (int)Math.Round(iter.GetConfidence(PageIteratorLevel.Word)),
                        X = box.X1,
                        Y = box.Y1,
                        Width = box.Width,
                        Height = box.Height
                    });
                }
                while (iter.Next(PageIteratorLevel.Word));
            }
        }

        /// <summary>
        /// Which way up the page is, as a rotation to APPLY (0/90/180/270). PSA scans
        /// arrive sideways often enough - a book laid on the glass, a phone photo - that a
        /// wrong orientation is the difference between a full read and nothing at all,
        /// because Tesseract's layout analysis assumes horizontal text lines.
        /// <para/>
        /// Two stages, each using the tool that is actually good at that half of the
        /// question. A recognition probe at all four right angles finds the AXIS: measured
        /// on a real death certificate turned on its side, the two horizontal angles scored
        /// 5292 and 5676 against 1960 upright, so the axis is unmistakable. It cannot tell
        /// the two apart, though - the difference between them is a 180 degree flip, and
        /// both read as horizontal lines. Tesseract's own orientation detector settles
        /// that, and it is reliable there because by then it is looking at a page that is
        /// already nearly upright; asked the same question about a sideways page it
        /// answered "180" with a confidence of 0.6, which is why it is not trusted first.
        /// <para/>
        /// A turn has to beat leaving the page alone by a clear margin. Upright is the
        /// common case, and turning a page the operator can already read is a worse failure
        /// than leaving a sideways one for them to rotate by hand.
        /// </summary>
        public static int DetectRotation(Bitmap source)
        {
            const int probe = 1600;
            const float trustOsdAt = 1.0f;

            // 1. Tesseract's own detector, when it is sure. Measured on the office's
            //    samples it was right every time it reported a confidence at or above 1.0
            //    (5.9 on an upright birth certificate, 2.2 on the same page turned 90
            //    degrees, 1.1 on an upside-down death certificate) and wrong the one time
            //    it reported less (0.6 on a sideways death certificate, where it said the
            //    page was upside down instead).
            float confidence;
            int reported = OsdOrientation(source, out confidence);
            if (reported >= 0 && confidence >= trustOsdAt)
                return (360 - reported) % 360;   // OSD reports how far the page IS turned

            // 2. Not sure, so fall back to recognition. Probing all four right angles finds
            //    the AXIS: on that sideways death certificate the two horizontal angles
            //    scored 5292 and 5676 against 1960 upright. It cannot tell those two apart,
            //    because they differ only by a 180 degree flip.
            //
            //    The four probes are fully independent - each rotates and reads its OWN
            //    copy of `source`, none reads what another produced - so they run across
            //    cores instead of one after another. Measured 2026-09-29: this step alone
            //    was 29.4s of a 54s total scan (over half) when OSD's confidence fell below
            //    the trust threshold, which real faded/skewed backlog pages do often. The
            //    SELECTION below still runs sequentially over the four scores in the same
            //    0/90/180/270 order the old loop used, so a tie breaks identically to
            //    before - only how the scores are COMPUTED changed, not which one wins.
            long[] scores = new long[4];
            System.Threading.Tasks.Parallel.For(0, 4, i => scores[i] = ProbeScore(source, i * 90, probe));
            long upright = scores[0];
            int axis = 0; long axisScore = upright;
            for (int i = 1; i < 4; i++)
            {
                if (scores[i] > axisScore) { axisScore = scores[i]; axis = i * 90; }
            }

            // A turn has to beat leaving the page alone by a clear margin: upright is the
            // common case, and turning a page the operator can already read is a worse
            // failure than leaving a sideways one for them to rotate by hand.
            if (axis == 0 || axisScore < Math.Max(1, upright) * 1.2) return 0;

            // 3. The candidate is now nearly upright, which is where OSD is strong, so ask
            //    it to settle the flip.
            using (Bitmap candidate = Rotate(source, axis))
            {
                float candidateConfidence;
                int candidateOrientation = OsdOrientation(candidate, out candidateConfidence);
                if (candidateOrientation == 180 && candidateConfidence >= trustOsdAt)
                    axis = (axis + 180) % 360;
            }
            return axis;
        }

        /// <summary>
        /// Tesseract's orientation-and-script detection: how far the page IS turned, in
        /// degrees. Returns -1 when osd.traineddata is not installed beside the language
        /// data, which is a supported configuration - the page is then left as it is.
        /// </summary>
        private static int OsdOrientation(Bitmap source, out float confidence)
        {
            confidence = 0f;
            if (!File.Exists(Path.Combine(TessDataPath(), "osd.traineddata"))) return -1;

            try
            {
                using (Bitmap small = Scale(source, 1600))
                using (Bitmap prepared = Preprocess(small))
                using (var engine = new TesseractEngine(TessDataPath(), "osd", EngineMode.TesseractOnly))
                using (var ms = new MemoryStream())
                {
                    prepared.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                    using (var page = engine.Process(pix, PageSegMode.OsdOnly))
                    {
                        int orientation; float conf;
                        page.DetectBestOrientation(out orientation, out conf);
                        confidence = conf;
                        return ((orientation % 360) + 360) % 360;
                    }
                }
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// What the orientation check saw, for diagnosing a page that came out the wrong
        /// way up: the angle OSD proposed with its confidence, and the recognition probe
        /// score at every right angle. Used by the document test harness.
        /// </summary>
        public static string DescribeOrientation(Bitmap source)
        {
            float conf;
            int osd = OsdOrientation(source, out conf);

            var parts = new List<string>();
            for (int deg = 0; deg < 360; deg += 90)
                parts.Add(deg + "deg=" + ProbeScore(source, deg, 1600));

            return "OSD says orientation " + osd + " (confidence " + conf.ToString("0.0") +
                   "), probe " + string.Join(" ", parts) + ", applied " + DetectRotation(source);
        }

        /// <summary>How well the page reads at one rotation: real words x mean confidence.</summary>
        private static long ProbeScore(Bitmap source, int degrees, int probeLongSide)
        {
            try
            {
                using (Bitmap turned = Rotate(source, degrees))
                using (Bitmap small = Scale(turned, probeLongSide))
                using (Bitmap prepared = Preprocess(small))
                using (var engine = new TesseractEngine(TessDataPath(), "eng", EngineMode.Default))
                {
                    engine.SetVariable("user_defined_dpi", "300");
                    using (var ms = new MemoryStream())
                    {
                        prepared.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                        using (var page = engine.Process(pix, PageSegMode.Auto))
                        {
                            string text = page.GetText() ?? "";
                            int words = text
                                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                                .Count(t => t.Count(char.IsLetter) >= 4);
                            int confidence = (int)(page.GetMeanConfidence() * 100);
                            return (long)words * confidence;
                        }
                    }
                }
            }
            catch
            {
                // A rotation that will not even process is not the best one.
                return 0;
            }
        }

        /// <summary>Rotate by a right angle. 0 returns a copy, so the caller always owns the result.</summary>
        public static Bitmap Rotate(Bitmap src, int degrees)
        {
            var copy = new Bitmap(src);
            switch (((degrees % 360) + 360) % 360)
            {
                case 90: copy.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                case 180: copy.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                case 270: copy.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
            }
            return copy;
        }

        /// <summary>Resize so the longest side is exactly the target (up or down).</summary>
        internal static Bitmap Scale(Bitmap src, int targetLongSide)
        {
            // Whole method under the GDI lock (see OcrService.GdiLock) - even the plain
            // Width/Height reads on `src` are not safe to run alongside another thread's
            // GDI+ work in this environment.
            lock (GdiLock)
            {
                int longest = Math.Max(src.Width, src.Height);
                if (longest == targetLongSide || targetLongSide <= 0) return new Bitmap(src);

                double s = (double)targetLongSide / longest;
                int w = Math.Max(1, (int)(src.Width * s));
                int h = Math.Max(1, (int)(src.Height * s));
                var outBmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(outBmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, w, h);
                }
                return outBmp;
            }
        }

        /// <summary>
        /// Grayscale, then Bradley/Wellner adaptive threshold over an integral image:
        /// every pixel is compared against the mean of its OWN neighbourhood, so text
        /// survives shadows, glare and colour casts that a single global cut-off
        /// destroys. Window is about 1/24 of the page width with a 15% bias — the
        /// values the mobile scanner settled on for these same forms.
        /// </summary>
        private static Bitmap Preprocess(Bitmap src)
        {
            return Render(src, true, 0.15, 24, false);
        }

        /// <summary>
        /// One rendering of a page or a crop: grayscale, optionally contrast-stretched,
        /// optionally adaptively thresholded with a chosen bias and window. A field crop
        /// is not the same problem as a whole page — a tighter window and a harder bias
        /// recover a faint typewriter row that the page settings smooth away — so the
        /// region reader varies these and votes on the results.
        /// </summary>
        internal static Bitmap Render(Bitmap src, bool binarize, double bias, int windowDiv, bool stretch)
        {
            // Split into a locked GDI+ phase (normalise + LockBits out, and later
            // UnlockBits + rewrap) and an UNLOCKED pure-managed-memory phase (the actual
            // pixel processing, on a plain byte[] this thread owns exclusively). Only the
            // first and third phases touch a Bitmap/Graphics object, which is what the
            // GdiLock exists to serialise; the per-pixel math in the middle is what field-
            // level parallelism is actually meant to speed up, so it runs unlocked.
            int w, h, stride;
            byte[] buffer;
            Bitmap rgb;
            lock (GdiLock)
            {
                w = src.Width; h = src.Height;
                rgb = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(rgb))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, w, h);
                }

                BitmapData bd = rgb.LockBits(new Rectangle(0, 0, w, h),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                stride = bd.Stride;
                buffer = new byte[stride * h];
                Marshal.Copy(bd.Scan0, buffer, 0, buffer.Length);
                rgb.UnlockBits(bd);
            }

            byte[] gray = ToGray(buffer, stride, w, h);
            if (stretch) StretchContrast(gray);
            if (binarize) AdaptiveThreshold(gray, w, h, bias, windowDiv);
            WriteGray(buffer, stride, w, h, gray);

            lock (GdiLock)
            {
                using (rgb)
                {
                    BitmapData bd = rgb.LockBits(new Rectangle(0, 0, w, h),
                        ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    Marshal.Copy(buffer, 0, bd.Scan0, buffer.Length);
                    rgb.UnlockBits(bd);

                    // Hand back a copy the caller owns.
                    return new Bitmap(rgb);
                }
            }
        }

        // NOTE: a despeckle pass (dropping black pixels with fewer than two black
        // neighbours) was tried here and REMOVED after measuring it: on the office's own
        // birth certificate it cut the fields read from 22 to 17, because PSA print is
        // thin at the resolution these pages are read at and the filter eats it. Noise is
        // already handled by the adaptive threshold, which judges each pixel against its
        // own neighbourhood.

        /// <summary>
        /// Stretch the histogram onto its 1st..99th percentile. Used for the grayscale
        /// variant of a field crop: on faint print, handing Tesseract grey levels it can
        /// still weigh sometimes beats handing it a hard black-and-white decision.
        /// </summary>
        private static void StretchContrast(byte[] gray)
        {
            var hist = new int[256];
            for (int i = 0; i < gray.Length; i++) hist[gray[i]]++;
            int total = gray.Length, lo = 0, hi = 255, acc = 0;
            for (int i = 0; i < 256; i++) { acc += hist[i]; if (acc > total * 0.01) { lo = i; break; } }
            acc = 0;
            for (int i = 255; i >= 0; i--) { acc += hist[i]; if (acc > total * 0.01) { hi = i; break; } }
            if (hi - lo <= 20) return;
            var lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                int v = (i - lo) * 255 / (hi - lo);
                lut[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
            }
            for (int i = 0; i < gray.Length; i++) gray[i] = lut[gray[i]];
        }

        /// <summary>Luminance grayscale (weighted, not a flat average — better on tinted paper).</summary>
        private static byte[] ToGray(byte[] buffer, int stride, int w, int h)
        {
            var gray = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                int outRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    int p = row + x * 3;
                    gray[outRow + x] = (byte)(buffer[p + 2] * 0.299 +   // R
                                              buffer[p + 1] * 0.587 +   // G
                                              buffer[p] * 0.114);       // B
                }
            }
            return gray;
        }

        private static void WriteGray(byte[] buffer, int stride, int w, int h, byte[] gray)
        {
            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                int inRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    byte v = gray[inRow + x];
                    int p = row + x * 3;
                    buffer[p] = buffer[p + 1] = buffer[p + 2] = v;
                }
            }
        }

        /// <summary>
        /// Bradley/Wellner adaptive threshold, in place. The integral image makes each
        /// window sum O(1); uint is exact here (255 * w * h stays under uint.MaxValue
        /// for any page we accept) and costs half what a double array would.
        /// </summary>
        private static void AdaptiveThreshold(byte[] gray, int w, int h, double bias, int windowDiv)
        {
            int iw = w + 1;
            var integral = new uint[iw * (h + 1)];
            for (int y = 0; y < h; y++)
            {
                uint rowSum = 0;
                for (int x = 0; x < w; x++)
                {
                    rowSum += gray[y * w + x];
                    integral[(y + 1) * iw + (x + 1)] = integral[y * iw + (x + 1)] + rowSum;
                }
            }

            int s = Math.Max(15, w / Math.Max(1, windowDiv));
            int half = s / 2;

            var result = new byte[gray.Length];
            for (int y = 0; y < h; y++)
            {
                int y1 = y - half < 0 ? 0 : y - half;
                int y2 = y + half >= h ? h - 1 : y + half;
                for (int x = 0; x < w; x++)
                {
                    int x1 = x - half < 0 ? 0 : x - half;
                    int x2 = x + half >= w ? w - 1 : x + half;

                    long count = (long)(x2 - x1 + 1) * (y2 - y1 + 1);
                    long sum = integral[(y2 + 1) * iw + (x2 + 1)]
                             - integral[y1 * iw + (x2 + 1)]
                             - integral[(y2 + 1) * iw + x1]
                             + integral[y1 * iw + x1];

                    result[y * w + x] = (byte)(gray[y * w + x] * count <= sum * (1 - bias) ? 0 : 255);
                }
            }
            Buffer.BlockCopy(result, 0, gray, 0, gray.Length);
        }
    }

    /// <summary>How a cropped field region should be recognised.</summary>
    public enum OcrRegionMode
    {
        Line,       // one printed line (PageSegMode.SingleLine)
        Block,      // a few lines inside one cell (PageSegMode.SingleBlock)
        Sparse      // scattered marks, e.g. a row of tick boxes (PageSegMode.SparseText)
    }

    /// <summary>One candidate reading of a region, kept so the caller can pick or merge.</summary>
    public class OcrRegionRead
    {
        public string Variant;            // which rendering produced it
        public string Text = "";
        public int Confidence;
        public List<OcrWord> Words = new List<OcrWord>();
        public int RegionWidth, RegionHeight;   // pixel size of the crop the words refer to
    }

    /// <summary>
    /// One analysis of one page: a single Tesseract engine, the prepared page renderings,
    /// and the ability to read either the whole page or an individual FIELD REGION.
    /// <para/>
    /// Region reading exists because of a measured failure, not a preference. Tesseract's
    /// page layout analysis silently drops entire typewriter rows inside these bordered
    /// PSA tables — on the Certificate of Marriage neither spouse's name nor either date of
    /// birth appears anywhere in the page text — while the same rows crop and read cleanly
    /// at PageSegMode.SingleLine. Whole-page text stays useful for identifying the form and
    /// for placing its template; it is not where values come from.
    /// <para/>
    /// The engine is built once per page on purpose: creating a <see cref="TesseractEngine"/>
    /// costs far more than a small recognition does, and a region-based read makes dozens of
    /// recognitions per page.
    /// </summary>
    public sealed class OcrSession : IDisposable
    {
        private readonly Bitmap _source;          // caller's bitmap, never disposed here
        // Captured ONCE, in the constructor (single-threaded), instead of read live off
        // `_source` from every parallel field thread - Denormalize used to call
        // _source.Width/_source.Height directly, and that crashed exactly like every
        // other unlocked GDI+ touch in this file ("Object is currently in use
        // elsewhere", thrown from Image.get_Width() during a real parallel run).
        private readonly int _sourceWidth, _sourceHeight;
        private readonly int _longSide;
        // Experiment knob (0 = off, use _longSide). Lets the two whole-page passes - which only
        // identify the form and place its template - run smaller than the page the per-field
        // reads see. Set from CROMS.DocTest --pagels; never set by the app.
        public static int PageLongSideOverride = 0;
        private int PageSide { get { return PageLongSideOverride > 0 ? PageLongSideOverride : _longSide; } }
        private Bitmap _pageBinary, _pageGray;
        private List<float> _rulings;
        // Concurrent: Page() and PageSparse() now run on separate threads (see PageIn),
        // so two different keys can be inserted at the same moment.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, OcrResult> _pageCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, OcrResult>(StringComparer.Ordinal);

        // ONE engine per THREAD, not one per session. RegionReader.Read fires dozens of
        // field recognitions per page and a TesseractEngine cannot run two Process() calls
        // at once, so a single shared engine forced every field to wait its turn even on an
        // idle multi-core PC. Building an engine costs real time (loading the traineddata),
        // which is exactly why it is still built ONCE per thread and reused, not once per
        // call - this trades a few engine-construction costs (bounded by core count) for
        // running the independent field reads across cores instead of stacked on one.
        // trackAllValues:true is what lets Dispose() reach every engine any thread created.
        private readonly ThreadLocal<TesseractEngine> _engineTls;

        public OcrSession(Bitmap source) : this(source, 2400) { }

        public OcrSession(Bitmap source, int targetLongSide)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
            _sourceWidth = source.Width;
            _sourceHeight = source.Height;
            _longSide = Math.Max(600, targetLongSide);
            _engineTls = new ThreadLocal<TesseractEngine>(() =>
            {
                var e = new TesseractEngine(OcrService.TessData(), "eng", EngineMode.Default);
                e.SetVariable("user_defined_dpi", "300");
                return e;
            }, trackAllValues: true);
        }

        /// <summary>The scan being analysed, at its original resolution.</summary>
        public Bitmap Source { get { return _source; } }

        // Page(), PageSparse() and the per-field reads now run concurrently (see below),
        // so the lazy build of these two page-level bitmaps needs its own lock - without
        // it, two threads racing the first "_pageBinary == null" check could each build
        // (and leak) a duplicate bitmap.
        private readonly object _pageBuildGate = new object();

        private Bitmap PageBinary
        {
            get
            {
                if (_pageBinary == null)
                    lock (_pageBuildGate)
                        if (_pageBinary == null)
                            using (Bitmap scaled = OcrService.Scale(_source, PageSide))
                                _pageBinary = OcrService.Render(scaled, true, 0.15, 24, false);
                return _pageBinary;
            }
        }

        private Bitmap PageGray
        {
            get
            {
                if (_pageGray == null)
                    lock (_pageBuildGate)
                        if (_pageGray == null)
                            using (Bitmap scaled = OcrService.Scale(_source, PageSide))
                                _pageGray = OcrService.Render(scaled, false, 0, 0, true);
                return _pageGray;
            }
        }

        /// <summary>Whole-page pass in the default layout mode. Cached.</summary>
        public OcrResult Page() { return PageIn(PageSegMode.Auto, "auto"); }

        /// <summary>
        /// Whole-page SPARSE pass. It finds printed text the layout analyser skips on a
        /// bordered table — on the death sample it is the only page pass that returns the
        /// registry number and both parents' names — but it returns them in no useful
        /// reading order, so it is used as EVIDENCE for identifying the form, never as a
        /// field value.
        /// </summary>
        public OcrResult PageSparse() { return PageIn(PageSegMode.SparseText, "sparse"); }

        private OcrResult PageIn(PageSegMode mode, string key)
        {
            OcrResult cached;
            if (_pageCache.TryGetValue(key, out cached)) return cached;

            // These two whole-page recognitions were the real cost this pipeline was
            // paying (measured: ~23s each on a real sample, dwarfing every per-field
            // read combined) - and they were run one after another for no reason, since
            // neither reads or writes anything the other touches. Each thread gets its
            // own Tesseract engine (OcrSession's ThreadLocal), so running them at once
            // is the same two recognitions, same two bitmaps, same comparison - just not
            // waited-for in sequence.
            var binaryTask = Task.Run(() => Recognise(PageBinary, mode));
            var grayTask = Task.Run(() => Recognise(PageGray, mode));
            Task.WaitAll(binaryTask, grayTask);
            OcrResult binary = binaryTask.Result, gray = grayTask.Result;

            OcrResult best = FormEvidence(gray) > FormEvidence(binary) ? gray : binary;
            _pageCache[key] = best;
            return best;
        }

        /// <summary>
        /// Score a page pass by how much of a CIVIL REGISTRY FORM it recovered, not by mean
        /// confidence: Tesseract reports a high mean for one confidently read speck on an
        /// otherwise blank page, so confidence alone picks the pass that read nothing.
        /// </summary>
        private static int FormEvidence(OcrResult r)
        {
            string text = (r.Text ?? "").ToLowerInvariant();
            if (text.Length < 40) return -1000 + text.Length;
            string[] anchors =
            {
                "certificate", "registrar", "birth", "marriage", "death", "registry", "name",
                "province", "municipality", "citizenship", "residence", "occupation", "religion"
            };
            int hits = 0;
            foreach (string a in anchors) if (text.Contains(a)) hits++;
            int body = Math.Min(30, text.Length / 120);
            return hits * 10 + body + r.Confidence / 4;
        }

        /// <summary>
        /// The y positions (0-1) of the form's own printed horizontal rules.
        /// <para/>
        /// These bordered tables carry a ruling between every row, and unlike the printed
        /// labels the rules are there and in the right place even when the text on them is
        /// unreadable. That makes them the one dependable way to tell that a photograph
        /// sits half a row higher than the reference scan. Detection runs at REDUCED
        /// resolution on purpose: at full size a slightly skewed page spreads one rule
        /// across several pixel rows, so no single row is dark enough to register.
        /// </summary>
        public List<float> Rulings()
        {
            if (_rulings != null) return _rulings;
            _rulings = new List<float>();

            using (Bitmap small = OcrService.Scale(_source, 1200))
            using (Bitmap page = OcrService.Render(small, true, 0.15, 24, false))
            {
                int w = page.Width, h = page.Height;
                var ink = new bool[w, h];
                BitmapData d = page.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                try
                {
                    var buf = new byte[Math.Abs(d.Stride) * h];
                    Marshal.Copy(d.Scan0, buf, 0, buf.Length);
                    for (int y = 0; y < h; y++)
                    {
                        int row = y * d.Stride;
                        for (int x = 0; x < w; x++) ink[x, y] = buf[row + x * 3] < 128;
                    }
                }
                finally { page.UnlockBits(d); }

                // The long vertical rules bound the table, so a row line only has to span
                // the TABLE to count — not the whole photograph.
                int left = w, right = 0;
                for (int x = 0; x < w; x++)
                {
                    int count = 0;
                    for (int y = 0; y < h; y++) if (ink[x, y]) count++;
                    if (count < h * 0.25) continue;
                    if (x < left) left = x;
                    if (x > right) right = x;
                }
                if (right - left < w * 0.2) { left = (int)(w * 0.1); right = (int)(w * 0.9); }
                int span = right - left;

                int runStart = -1;
                for (int y = 0; y <= h; y++)
                {
                    int count = 0;
                    if (y < h) for (int x = left; x <= right; x++) if (ink[x, y]) count++;
                    bool isRule = y < h && count >= span * 0.55;
                    if (isRule && runStart < 0) runStart = y;
                    else if (!isRule && runStart >= 0)
                    {
                        _rulings.Add((runStart + y - 1) / 2f / h);
                        runStart = -1;
                    }
                }
            }
            return _rulings;
        }

        /// <summary>
        /// Read one field region given as 0-1 coordinates of the page. The crop is taken
        /// from the ORIGINAL scan, so nothing is lost to the page-level downscale, then
        /// upscaled until its text is tall enough for the engine and recognised several
        /// ways. Every rendering is returned: which reading is right depends on what the
        /// FIELD is allowed to contain, and that is not something this class knows.
        /// </summary>
        public List<OcrRegionRead> ReadRegion(RectangleF norm, OcrRegionMode mode)
        {
            return ReadRegion(norm, mode, false);
        }

        /// <summary>
        /// As above, but <paramref name="deeper"/> adds two more renderings of the same
        /// crop. They are not in the default set because they cost time on every field of
        /// every page while only paying off on the difficult ones — so the caller runs the
        /// cheap set first and asks for these only when nothing acceptable came back. On
        /// the marriage certificate's date row, the wider-window renderings are the ones
        /// that read the day correctly.
        /// </summary>
        public List<OcrRegionRead> ReadRegion(RectangleF norm, OcrRegionMode mode, bool deeper)
        {
            var reads = new List<OcrRegionRead>();
            Rectangle box = Denormalize(norm);
            if (box.Width < 8 || box.Height < 6) return reads;

            // Crop creation touches _source directly via DrawImage, so it goes through
            // the same process-wide GDI lock every other GDI+ call in this pipeline
            // uses (see OcrService.GdiLock). box.Width/box.Height are plain ints off the
            // Rectangle struct already computed above - no further property reads on the
            // Bitmap are needed once the crop exists (crop.Width/Height would be the
            // same numbers as box.Width/Height, so box's own ints are used below
            // instead of touching the Bitmap again from outside the lock).
            Bitmap crop;
            lock (OcrService.GdiLock)
            {
                crop = new Bitmap(box.Width, box.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(crop))
                    g.DrawImage(_source, new Rectangle(0, 0, box.Width, box.Height), box, GraphicsUnit.Pixel);
            }
            using (crop)
            {
                // Field text is about 2% of page height; the engine wants roughly 30px of
                // cap height. Scale by the crop's own height, capped so a tall multi-line
                // cell does not blow up into a huge bitmap.
                int factor = Math.Max(2, Math.Min(8, (int)Math.Ceiling(140.0 / Math.Max(1, box.Height))));
                if (box.Width * factor > 4000) factor = Math.Max(2, 4000 / Math.Max(1, box.Width));

                using (Bitmap big = OcrService.Scale(crop, Math.Max(box.Width, box.Height) * factor))
                // THREE renderings, and that count is itself a measured choice. Dropping
                // the contrast stretch before thresholding was tried, because on the faded
                // 1993 birth certificate the stretched rendering turns "Gilvan" into
                // "beseud"; it gained a marriage field and lost one on each birth
                // certificate (73 correct to 72). Offering BOTH as a fourth rendering was
                // then tried and was worse still (73 to 70) — more candidates split the
                // vote, so a correct reading that used to win two votes out of three wins
                // one out of four and is discarded as having no consensus. Extra
                // renderings are not free accuracy; three is the measured optimum.
                using (Bitmap gray = OcrService.Render(big, false, 0, 0, true))
                using (Bitmap binA = OcrService.Render(big, true, 0.15, 8, true))
                using (Bitmap binB = OcrService.Render(big, true, 0.22, 4, true))
                {
                    PageSegMode psm = mode == OcrRegionMode.Line ? PageSegMode.SingleLine
                                    : mode == OcrRegionMode.Block ? PageSegMode.SingleBlock
                                    : PageSegMode.SparseText;
                    Add(reads, "gray", gray, psm, box);
                    Add(reads, "adaptive", binA, psm, box);
                    Add(reads, "adaptive-tight", binB, psm, box);

                    if (deeper)
                    {
                        using (Bitmap binC = OcrService.Render(big, true, 0.15, 16, true))
                        using (Bitmap binD = OcrService.Render(big, true, 0.10, 8, true))
                        {
                            Add(reads, "adaptive-wide", binC, psm, box);
                            Add(reads, "adaptive-soft", binD, psm, box);
                            PageSegMode other = psm == PageSegMode.SingleLine
                                ? PageSegMode.SingleBlock : PageSegMode.SingleLine;
                            Add(reads, "adaptive-wide-alt", binC, other, box);
                        }
                    }

                    // A single printed line that comes back empty is usually a line the
                    // layout analyser refused, not a blank cell: retry it as a block.
                    bool allEmpty = true;
                    foreach (OcrRegionRead r in reads) if (r.Text.Trim().Length > 0) allEmpty = false;
                    if (mode == OcrRegionMode.Line && allEmpty)
                    {
                        Add(reads, "gray-block", gray, PageSegMode.SingleBlock, box);
                        Add(reads, "adaptive-block", binA, PageSegMode.SingleBlock, box);
                    }
                }
            }
            return reads;
        }

        private void Add(List<OcrRegionRead> into, string variant, Bitmap image, PageSegMode psm, Rectangle box)
        {
            OcrResult r = Recognise(image, psm);
            // Report word boxes in the CROP's own pixel space, so a caller splitting a row
            // into cells is unaffected by the upscale factor. Width/height come from the
            // result Recognise already captured under the GDI lock, not a fresh read of
            // `image` here - image.Width/.Height outside any lock hit the same "Object is
            // currently in use elsewhere" fragility as Image.Save did.
            double sx = (double)box.Width / r.PageWidth, sy = (double)box.Height / r.PageHeight;
            var read = new OcrRegionRead
            {
                Variant = variant,
                Text = r.Text ?? "",
                Confidence = r.Confidence,
                RegionWidth = box.Width,
                RegionHeight = box.Height
            };
            foreach (OcrWord w in r.Words)
                read.Words.Add(new OcrWord
                {
                    Text = w.Text,
                    X = (int)Math.Round(w.X * sx),
                    Y = (int)Math.Round(w.Y * sy),
                    Width = (int)Math.Round(w.Width * sx),
                    Height = (int)Math.Round(w.Height * sy),
                    Confidence = w.Confidence
                });
            into.Add(read);
        }

        private Rectangle Denormalize(RectangleF norm)
        {
            int x = (int)Math.Round(norm.X * _sourceWidth);
            int y = (int)Math.Round(norm.Y * _sourceHeight);
            int w = (int)Math.Round(norm.Width * _sourceWidth);
            int h = (int)Math.Round(norm.Height * _sourceHeight);
            if (x < 0) { w += x; x = 0; }
            if (y < 0) { h += y; y = 0; }
            if (x + w > _sourceWidth) w = _sourceWidth - x;
            if (y + h > _sourceHeight) h = _sourceHeight - y;
            return new Rectangle(x, y, Math.Max(0, w), Math.Max(0, h));
        }

        // GDI+ (System.Drawing) is not safe to call from more than one thread at once -
        // NOT because two threads touch the SAME Bitmap, but because Image.Save shares
        // internal codec state process-wide. Measured directly: running Recognise() on
        // two different Bitmaps from two Tasks threw "Object is currently in use
        // elsewhere" out of Bitmap.Save. So the encode step (the only GDI+ call here)
        // stays behind the SAME process-wide lock every other GDI+ call in this file
        // uses (OcrService.GdiLock - must be the identical object, or two different
        // locks give zero mutual exclusion between them); everything after it -
        // Pix.LoadFromMemory (native Leptonica) and engine.Process (native Tesseract,
        // one engine per thread) - has no such restriction and is what actually runs
        // in parallel.
        private OcrResult Recognise(Bitmap image, PageSegMode psm)
        {
            byte[] png;
            int width, height;
            lock (OcrService.GdiLock)
            {
                width = image.Width;
                height = image.Height;
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    png = ms.ToArray();
                }
            }
            {
                using (var pix = Pix.LoadFromMemory(png))
                using (var page = _engineTls.Value.Process(pix, psm))
                {
                    string text = page.GetText();
                    var result = new OcrResult
                    {
                        Text = string.IsNullOrEmpty(text) ? "" : text.Trim(),
                        Confidence = (int)Math.Round(page.GetMeanConfidence() * 100),
                        PageWidth = width,
                        PageHeight = height
                    };
                    OcrService.CollectWords(page, result.Words);
                    return result;
                }
            }
        }

        public void Dispose()
        {
            if (_pageBinary != null) _pageBinary.Dispose();
            if (_pageGray != null) _pageGray.Dispose();
            foreach (var e in _engineTls.Values) e.Dispose();
            _engineTls.Dispose();
        }
    }
}
