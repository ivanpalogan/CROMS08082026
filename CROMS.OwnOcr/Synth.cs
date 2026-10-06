using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CROMS.OwnOcr
{
    public sealed class FontSpec
    {
        public string Name;
        public FontFamily Family;
        public FontStyle Style;
        public double Weight;    // how often it is chosen
    }

    public sealed class SynthStats
    {
        public int Lines, Kept, Dropped;
        public int[] PerClass = new int[Charset.Count];
        public TimeSpan Elapsed;
    }

    /// <summary>
    /// Makes labelled training data without anyone typing a label.
    /// <para/>
    /// The office has five scans; a classifier needs thousands of examples of each character.
    /// So lines of text are RENDERED from fonts and then made to look like the scans:
    /// small, soft, noisy, unevenly lit, slightly tilted, sometimes sitting on a table rule.
    /// Each line goes through the same engine as a real crop (so the pictures it produces
    /// are normalised exactly the way real ones will be), and when the engine finds as many
    /// characters as the line has, the characters are labelled in order.
    /// <para/>
    /// Lines where letters touched and the count does not match are dropped. That is
    /// deliberate and visible in the stats: this data teaches the classifier what clean
    /// single characters look like; the touching ones are handled by the splitter.
    /// </summary>
    public static class Synth
    {
        private static PrivateFontCollection _private;   // must outlive the FontFamily objects
        private static readonly object RenderLock = new object();

        // Names only: generic Filipino-style given names, surnames and places, written for
        // this purpose. The classifier works character by character, so a vocabulary this
        // small cannot teach it a name - it only gives the letters realistic neighbours.
        private static readonly string[] Words =
        {
            "Maria","Jose","Juan","Ana","Pedro","Carlos","Elena","Rosa","Miguel","Luz","Antonio",
            "Teresa","Ramon","Gloria","Eduardo","Cecilia","Roberto","Josefina","Fernando","Lourdes",
            "Santos","Reyes","Cruz","Garcia","Mendoza","Ramos","Aquino","Bautista","Villanueva",
            "Castillo","Navarro","Domingo","Pascual","Salazar","Valdez","Soriano","Delos","Dela",
            "Manila","Cebu","Davao","Baguio","Tuguegarao","Aparri","Tarlac","Laoag","Vigan","Naga",
            "Bical","Centro","Poblacion","Barangay","Hospital","Medical","Center","City","Province",
            "Roman","Catholic","Islam","Baptist","Filipino","Single","Married","Widowed","Farmer",
            "Teacher","Driver","Housewife","Fisherman","Nurse","Engineer","Carpenter","Vendor",
            "Quezon","Zamora","Xavier","Jaime","Quirino","Javier","Zacarias","Vicente","Yolanda",
            "Kristine","Wilfredo","Xenia","Oscar","Ignacio","Ulysses","Benito","Felipe","Hermina"
        };

        // ===================================================================== fonts
        public static List<FontSpec> LoadFonts(string fontDir)
        {
            var fonts = new List<FontSpec>();
            _private = new PrivateFontCollection();
            foreach (string f in new[] { "CourierPrime-Regular.ttf", "CourierPrime-Bold.ttf" })
            {
                string p = Path.Combine(fontDir, f);
                if (File.Exists(p)) _private.AddFontFile(p);
            }
            if (_private.Families.Length > 0)
            {
                FontFamily cp = _private.Families[0];
                // The office's marriage and death forms are typewriter print: weight it highest.
                if (cp.IsStyleAvailable(FontStyle.Regular)) fonts.Add(new FontSpec { Name = "Courier Prime", Family = cp, Style = FontStyle.Regular, Weight = 3 });
                if (cp.IsStyleAvailable(FontStyle.Bold)) fonts.Add(new FontSpec { Name = "Courier Prime Bold", Family = cp, Style = FontStyle.Bold, Weight = 3 });
            }

            // Installed Windows fonts: the birth form is computer-printed in a bold serif.
            Add(fonts, "Courier New", FontStyle.Regular, 1.5);
            Add(fonts, "Courier New", FontStyle.Bold, 1.5);
            Add(fonts, "Times New Roman", FontStyle.Regular, 1.5);
            Add(fonts, "Times New Roman", FontStyle.Bold, 2.5);
            Add(fonts, "Georgia", FontStyle.Bold, 1.5);
            Add(fonts, "Cambria", FontStyle.Bold, 1.0);
            Add(fonts, "Arial", FontStyle.Regular, 0.7);
            Add(fonts, "Arial", FontStyle.Bold, 0.7);
            Add(fonts, "Consolas", FontStyle.Regular, 0.6);
            Add(fonts, "Lucida Console", FontStyle.Regular, 0.5);
            return fonts;
        }

        private static void Add(List<FontSpec> fonts, string name, FontStyle style, double weight)
        {
            try
            {
                var fam = new FontFamily(name);
                if (fam.IsStyleAvailable(style))
                    fonts.Add(new FontSpec { Name = name + (style == FontStyle.Bold ? " Bold" : ""), Family = fam, Style = style, Weight = weight });
            }
            catch (ArgumentException) { /* not installed on this PC */ }
        }

        // ===================================================================== text
        public static string RandomText(Random rng)
        {
            int mode = rng.Next(100);
            if (mode < 45) return Words_(rng);
            if (mode < 62) return Digits(rng);
            if (mode < 92) return RandomChars(rng);
            return Punctuated(rng);
        }

        private static string Words_(Random rng)
        {
            int n = rng.Next(1, 4);
            var parts = new List<string>();
            for (int i = 0; i < n; i++) parts.Add(Words[rng.Next(Words.Length)]);
            string s = string.Join(" ", parts);
            int style = rng.Next(100);
            if (style < 50) return s.ToUpperInvariant();
            if (style < 85) return s;
            return s.ToLowerInvariant();
        }

        private static string Digits(Random rng)
        {
            int kind = rng.Next(3);
            if (kind == 0) return rng.Next(1900, 2030) + "-" + rng.Next(1, 99999);
            if (kind == 1) return rng.Next(1, 99999999).ToString();
            return rng.Next(1, 31) + " " + rng.Next(1, 13) + " " + rng.Next(1930, 2026);
        }

        // Uniform over the whole charset so the rare characters (Q, X, Z, J, the enye, the
        // punctuation) get as many examples as the common ones.
        private static string RandomChars(Random rng)
        {
            int len = rng.Next(4, 11);
            bool upper = rng.Next(2) == 0;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < len; i++)
            {
                char c = Charset.At(rng.Next(Charset.Count));
                if (char.IsLetter(c)) c = upper ? char.ToUpperInvariant(c) : c;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Punctuated(Random rng)
        {
            string[] t = { "Bical, Peñablanca", "A. B. Cruz", "O'Brien-Reyes", "No. 12, St.", "Brgy. (Centro)", "San Jose/Sta. Ana", "Jr., Sr. & Co.", "Block 5; Lot #7" };
            string s = t[rng.Next(t.Length)];
            return rng.Next(2) == 0 ? s : s.ToUpperInvariant();
        }

        // ===================================================================== rendering
        private static FontSpec PickFont(List<FontSpec> fonts, Random rng)
        {
            double total = fonts.Sum(f => f.Weight), r = rng.NextDouble() * total;
            foreach (FontSpec f in fonts) { r -= f.Weight; if (r <= 0) return f; }
            return fonts[fonts.Count - 1];
        }

        /// <summary>Draws one line of text and degrades it to look like a photographed form.</summary>
        public static GrayImage RenderLine(string text, FontSpec fs, Random rng)
        {
            int em = rng.Next(12, 30);   // pixels; cap height is about 0.65 of this - the scans are 9-17
            double extraScale = em / 20.0;

            lock (RenderLock)
            {
                using (Font font = new Font(fs.Family, em, fs.Style, GraphicsUnit.Pixel))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic))
                {
                    fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;

                    // Advance of each character, then jitter. A few pairs are pulled together so
                    // some letters touch, as typewriter ink does.
                    var adv = new float[text.Length];
                    using (var tmp = new Bitmap(1, 1))
                    using (Graphics gm = Graphics.FromImage(tmp))
                    {
                        gm.TextRenderingHint = TextRenderingHint.AntiAlias;
                        for (int i = 0; i < text.Length; i++)
                        {
                            adv[i] = gm.MeasureString(text[i].ToString(), font, 10000, fmt).Width;
                            double extra = rng.NextDouble() * 2.4 - 0.8;
                            if (rng.NextDouble() < 0.12) extra = -(0.04 + rng.NextDouble() * 0.08) * em;
                            adv[i] += (float)(extra * extraScale * 0.6);
                        }
                    }

                    int pad = em / 2 + 6;
                    int width = (int)Math.Ceiling(adv.Sum()) + pad * 2;
                    int height = (int)(em * 1.9) + pad * 2;
                    GrayImage rendered;
                    using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                    {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        int paper = rng.Next(170, 241), ink = rng.Next(20, 96);
                        g.Clear(Color.FromArgb(paper, paper, paper));
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = rng.Next(2) == 0 ? TextRenderingHint.AntiAlias : TextRenderingHint.AntiAliasGridFit;

                        // Slight tilt and shear: no photograph is square to the page.
                        var m = new Matrix();
                        m.Translate(width / 2f, height / 2f);
                        m.Rotate((float)(rng.NextDouble() * 2.4 - 1.2));
                        m.Shear((float)(rng.NextDouble() * 0.16 - 0.08), 0f);
                        m.Translate(-width / 2f, -height / 2f);
                        g.Transform = m;

                        float ascentPx = fs.Family.GetCellAscent(fs.Style) * (float)em / fs.Family.GetEmHeight(fs.Style);
                        float top = pad;
                        float baseline = top + ascentPx;

                        using (var brush = new SolidBrush(Color.FromArgb(ink, ink, ink)))
                        {
                            bool bloom = rng.NextDouble() < 0.35;   // ink spreading on paper
                            float x = pad;
                            for (int i = 0; i < text.Length; i++)
                            {
                                if (text[i] != ' ')
                                {
                                    g.DrawString(text[i].ToString(), font, brush, x, top, fmt);
                                    if (bloom) g.DrawString(text[i].ToString(), font, brush, x + 0.6f, top, fmt);
                                }
                                x += adv[i];
                            }

                            // A table rule just under the baseline on half the lines, so rule
                            // removal is exercised the way it is on the real crops.
                            if (rng.NextDouble() < 0.5)
                            {
                                float ry = baseline + (float)(rng.NextDouble() * 0.25 + 0.02) * em;
                                using (var pen = new Pen(Color.FromArgb(ink + 20, ink + 20, ink + 20), 1f + (float)rng.NextDouble() * em / 12f))
                                    g.DrawLine(pen, 0, ry, width, ry + (float)(rng.NextDouble() * 4 - 2));
                            }
                        }
                    }
                    // Read the pixels only after the Graphics is released.
                    rendered = GrayImage.FromBitmap(bmp);
                    }
                    return Degrade(rendered, rng);
                }
            }
        }

        // Blur, uneven lighting and sensor noise.
        private static GrayImage Degrade(GrayImage src, Random rng)
        {
            int w = src.Width, h = src.Height;
            var px = new float[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = src.Pixels[i];

            int passes = rng.NextDouble() < 0.65 ? (rng.NextDouble() < 0.4 ? 2 : 1) : 0;
            for (int p = 0; p < passes; p++) px = Box3(px, w, h);

            double gx = rng.NextDouble() * 0.3 - 0.15, gy = rng.NextDouble() * 0.3 - 0.15;
            double sigma = 2 + rng.NextDouble() * 10;
            var dst = new GrayImage(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double light = 1.0 + gx * (x / (double)w - 0.5) + gy * (y / (double)h - 0.5);
                    double v = px[y * w + x] * light + Gaussian(rng) * sigma;
                    dst.Pixels[y * w + x] = (byte)(v < 0 ? 0 : v > 255 ? 255 : (int)v);
                }
            return dst;
        }

        private static float[] Box3(float[] s, int w, int h)
        {
            var d = new float[s.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float acc = 0; int n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                            acc += s[yy * w + xx]; n++;
                        }
                    d[y * w + x] = acc / n;
                }
            return d;
        }

        private static double Gaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        // ===================================================================== data set
        public static List<Sample> Generate(int lines, int seed, string fontDir, SynthStats stats, Action<int> progress)
        {
            List<FontSpec> fonts = LoadFonts(fontDir);
            if (fonts.Count == 0) throw new InvalidOperationException("No fonts could be loaded.");

            var all = new List<Sample>[lines];
            var kept = 0; var dropped = 0; var done = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, lines, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
            {
                var rng = new Random(unchecked(seed * 7919 + i));
                string text = RandomText(rng);
                // Only characters the classifier knows can be labelled.
                string chars = new string(text.Where(c => c != ' ' && Charset.IndexOf(c) >= 0).ToArray());
                string drawn = new string(text.Where(c => c == ' ' || Charset.IndexOf(c) >= 0).ToArray());
                if (chars.Length == 0) return;

                FontSpec fs = PickFont(fonts, rng);
                GrayImage img = RenderLine(drawn, fs, rng);
                Analysis a = OwnOcrEngine.Analyze(img);

                if (a.Cells.Count != chars.Length)
                {
                    System.Threading.Interlocked.Increment(ref dropped);
                }
                else
                {
                    var list = new List<Sample>(chars.Length);
                    for (int k = 0; k < chars.Length; k++)
                        list.Add(Sample.FromCell(a.Cells[k], Charset.IndexOf(chars[k])));
                    all[i] = list;
                    System.Threading.Interlocked.Increment(ref kept);
                }
                int d = System.Threading.Interlocked.Increment(ref done);
                if (progress != null && d % 250 == 0) progress(d);
            });

            var result = new List<Sample>();
            foreach (var l in all) if (l != null) result.AddRange(l);
            if (stats != null)
            {
                stats.Lines = lines; stats.Kept = kept; stats.Dropped = dropped; stats.Elapsed = sw.Elapsed;
                foreach (Sample s in result) stats.PerClass[s.Label]++;
            }
            return result;
        }
    }
}
