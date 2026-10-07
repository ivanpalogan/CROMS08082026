using System;
using System.Drawing;
using System.IO;
using System.Linq;
using CROMS.Data;

namespace CROMS.DocTest
{
    /// <summary>
    /// Harness for Intelligent Document Processing: runs the real pipeline
    /// (<see cref="DocumentAI.Analyze"/>, which straightens, reads at two resolutions,
    /// classifies, extracts, scores, corrects and validates) over sample certificates and
    /// prints what the operator would see in the review grid.
    /// <para/>
    /// It runs from CROMS\bin\Debug so it loads the same Tesseract engine and the same
    /// tessdata the application does — a harness that loads a different engine is not
    /// testing the application. Optional argument: a rotation to apply to every sample
    /// first, which is how the orientation handling is exercised (<c>--rotate 90</c>).
    /// <para/>
    /// Usage: CROMS.DocTest.exe [--rotate 90] file1 [file2 …]
    ///        CROMS.DocTest.exe --truth [sampleDir]   — measure extracted FIELD VALUES
    ///        against ground truth transcribed from the certificates (see Truth.cs).
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int rotate = 0;
            bool diag = false;
            bool words = false;
            bool rawText = false;
            string annotDir = null;
            bool noOwn = false, ownDiag = false;
            string selfTestImage = null, selfTestKey = null, selfTestBad = null;
            float wordsFrom = 0f;
            string truthDir = null;
            string dumpDir = null, dumpSamples = null;
            var files = new System.Collections.Generic.List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                // Data set for the own OCR engine (CROMS.OwnOcr): every field region as a PNG
                // plus truth and the Tesseract reading. See CropDump.cs.
                if (args[i] == "--dump-crops" && i + 1 < args.Length)
                {
                    dumpDir = args[++i];
                    dumpSamples = i + 1 < args.Length && !args[i + 1].StartsWith("--")
                        ? args[++i]
                        : Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.UserProfile), "Downloads");
                    continue;
                }
                if (args[i] == "--rotate" && i + 1 < args.Length) { int.TryParse(args[++i], out rotate); continue; }
                if (args[i] == "--diag") { diag = true; continue; }
                // Task-A experiment (2026-09-29, OFF by default in the app): also try the
                // native-resolution pass on a RESOLVED-but-low-confidence layout, not only an
                // unresolved one. Kept as a harness flag so a before/after --truth run is a
                // one-flag comparison, never a code edit.
                if (args[i] == "--pagels" && i + 1 < args.Length) { int pls; if (int.TryParse(args[++i], out pls)) OcrSession.PageLongSideOverride = pls; continue; }
                if (args[i] == "--labelpath") { DocumentAI.ForceLabelPath = true; continue; }
                if (args[i] == "--retry") { DocumentAI.EnableLowConfidenceNativeRetry = true; continue; }
                // Second opinion from the own OCR engine (CROMS.OwnOcr): --noown measures the pipeline
                // without it, --owndiag prints what it replaced. Default = as the app runs.
                if (args[i] == "--noown") { noOwn = true; continue; }
                // --ownselftest <image> <fieldKey> <garbledValue>: read the page, overwrite that field with a
                // garbled value, run the second opinion and report whether it repaired it. Proves the
                // replace path, which the real samples never exercise (Tesseract + vocabulary already
                // get every closed-list field right or too garbled to repair).
                if (args[i] == "--ownselftest" && i + 3 < args.Length) { selfTestImage = args[++i]; selfTestKey = args[++i]; selfTestBad = args[++i]; continue; }
                if (args[i] == "--owndiag") { ownDiag = true; continue; }
                if (args[i] == "--words")
                {
                    words = true;
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], out float from)) { i++; wordsFrom = from; }
                    continue;
                }
                if (args[i] == "--rawtext") { rawText = true; continue; }
                if (args[i] == "--annot" && i + 1 < args.Length) { annotDir = args[++i]; continue; }
                if (args[i] == "--truth")
                {
                    truthDir = i + 1 < args.Length && !args[i + 1].StartsWith("--")
                        ? args[++i]
                        : Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.UserProfile), "Downloads");
                    continue;
                }
                files.Add(args[i]);
            }

            if (!DocumentAI.IsAvailable())
            {
                Console.WriteLine("FAIL: Tesseract 'eng' language data not found.");
                return 1;
            }

            if (noOwn) OwnOcrHybrid.Enabled = false;
            else if (!OwnOcrHybrid.WaitReady(120000))
                Console.WriteLine("note: own OCR engine unavailable (" + OwnOcrHybrid.LoadError + ") - running without it.");
            if (ownDiag)
            {
                var prev = DocumentAI.Diag;
                DocumentAI.Diag = m => { if (m != null && m.IndexOf("own second opinion", StringComparison.Ordinal) >= 0) Console.WriteLine("  " + m); if (prev != null) prev(m); };
            }

            if (selfTestImage != null) return OwnSelfTest(selfTestImage, selfTestKey, selfTestBad);

            // Ground-truth mode: the only run that says whether the RECORD is right.
            if (truthDir != null) return Truth.Run(truthDir);

            if (dumpDir != null) return CropDump.Run(dumpDir, dumpSamples);

            // Calibration mode: every printed word with its NORMALISED box, which is the
            // template space DocLayouts field rectangles are stated in. This is how a new
            // field region is measured off the reference scan rather than estimated.
            if (words)
            {
                foreach (string path in files) DumpWords(path, rotate, wordsFrom);
                return 0;
            }

            if (rawText)
            {
                foreach (string path in files) DumpRawText(path, rotate);
                return 0;
            }

            if (files.Count == 0)
            {
                Console.WriteLine("Usage: CROMS.DocTest.exe [--rotate 90] <image> [<image> ...]");
                Console.WriteLine("       CROMS.DocTest.exe --truth [sampleDir]   measure against ground truth");
                return 2;
            }

            foreach (string path in files)
            {
                Console.WriteLine(new string('=', 78));
                Console.WriteLine(Path.GetFileName(path) + (rotate != 0 ? "   [rotated " + rotate + "° first]" : ""));
                Console.WriteLine(new string('=', 78));

                if (!File.Exists(path)) { Console.WriteLine("  missing file\n"); continue; }

                if (diag)
                {
                    using (Bitmap raw = DocumentAI.LoadImage(path))
                    using (Bitmap image = rotate == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, rotate))
                        Console.WriteLine("  " + OcrService.DescribeOrientation(image));

                    DocumentAI.Diag = line => Console.WriteLine("  [phase] " + line);
                }

                var started = DateTime.Now;
                DocAiResult r;
                try
                {
                    using (Bitmap raw = DocumentAI.LoadImage(path))
                    using (Bitmap image = rotate == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, rotate))
                        r = DocumentAI.Analyze(image);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  ERROR: " + ex.Message + "\n");
                    continue;
                }

                if (!string.IsNullOrEmpty(r.Error)) { Console.WriteLine("  ERROR: " + r.Error + "\n"); continue; }

                Console.WriteLine("  detected      : " + DocumentAI.KindName(r.Kind) +
                                  "  (class " + r.ClassifyConfidence + "%)");
                Console.WriteLine("  rotation      : " + r.RotationApplied + "°" +
                                  (rotate != 0 ? (r.RotationApplied == (360 - rotate) % 360 ? "  [corrected]" : "  [NOT corrected]") : ""));
                Console.WriteLine("  recognition   : " + r.OcrConfidence + "%   fields " + r.OverallConfidence + "%");
                Console.WriteLine("  fields        : " + r.ExtractedCount + " read, " + r.MissingCount + " blank");
                Console.WriteLine("  manual review : " + (r.NeedsManualReview ? "YES — " + r.ReviewReason : "no"));
                Console.WriteLine("  layout        : " + (r.LayoutCode ?? "(none — label path)") +
                                  (string.IsNullOrEmpty(r.FitNote) ? "" : "   " + r.FitNote));
                Console.WriteLine("  elapsed       : " + (int)(DateTime.Now - started).TotalMilliseconds + " ms");
                Console.WriteLine();

                if (annotDir != null) Annotate(path, rotate, r, annotDir);

                foreach (DocField f in r.Fields)
                {
                    string flag;
                    switch (f.Status)
                    {
                        case FieldStatus.Invalid: flag = "INVALID "; break;
                        case FieldStatus.Conflict: flag = "CONFLICT"; break;
                        case FieldStatus.Uncertain: flag = "weak    "; break;
                        case FieldStatus.Missing: flag = "blank   "; break;
                        default: flag = "ok      "; break;
                    }
                    Console.WriteLine("    " + f.Label.PadRight(22) +
                        (f.Value ?? "").PadRight(34).Substring(0, Math.Max(34, (f.Value ?? "").Length)) +
                        " " + (f.Confidence + "%").PadLeft(5) + "  " + flag +
                        (!f.Region.IsEmpty ? "  {px " + f.Region.X + "," + f.Region.Y + " " + f.Region.Width + "x" + f.Region.Height + "}" : "") + (f.RegionNorm.IsEmpty ? "  [no region]" : "  [" + f.RegionNorm.X.ToString("0.000") + "," + f.RegionNorm.Y.ToString("0.000") + " " + f.RegionNorm.Width.ToString("0.000") + "x" + f.RegionNorm.Height.ToString("0.000") + (f.FromRegion ? "" : " label") + "]") +
                        (f.Corrected ? "  (was \"" + f.OcrValue + "\")" : "") +
                        (string.IsNullOrEmpty(f.Issue) ? "" : "  " + f.Issue));
                }
                Console.WriteLine();
            }
            return 0;
        }

        /// <summary>
        /// Print every recognised word with its box as a fraction of the page. Words are
        /// grouped into printed rows so a form's rows can be read off directly. Only the
        /// part of the page from <paramref name="from"/> downwards is printed.
        /// </summary>
        private static int OwnSelfTest(string path, string key, string bad)
        {
            using (Bitmap raw = DocumentAI.LoadImage(path))
            {
                DocAiResult r = DocumentAI.Analyze(raw);
                Bitmap page = r.RotationApplied == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, r.RotationApplied);
                using (page)
                {
                    DocField f = r.Fields.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                    if (f == null) { Console.WriteLine("FAIL: no field " + key); return 1; }
                    Console.WriteLine("before: " + key + " = '" + f.Value + "'  conf " + f.Confidence + "  region " + !f.RegionNorm.IsEmpty);
                    f.Value = bad; f.Corrected = false;
                    f.Confidence = 95;
                    int n = OwnOcrHybrid.Apply(page, r, Console.WriteLine);
                    DocIntelligence.Revalidate(r);
                    Console.WriteLine("after : " + key + " = '" + f.Value + "'  conf " + f.Confidence + "  corrected " + f.Corrected + "  status " + f.Status + "  replaced " + n);
                    return n > 0 ? 0 : 2;
                }
            }
        }

        /// <summary>Draws every field's highlight box onto the page and saves it, so the box can be checked by eye.</summary>
        private static void Annotate(string path, int rotate, DocAiResult r, string dir)
        {
            Directory.CreateDirectory(dir);
            using (Bitmap raw = DocumentAI.LoadImage(path))
            using (Bitmap start = rotate == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, rotate))
            using (Bitmap page = r.RotationApplied == 0 ? new Bitmap(start) : OcrService.Rotate(start, r.RotationApplied))
            using (Bitmap canvas = new Bitmap(page.Width, page.Height))
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.DrawImage(page, 0, 0, page.Width, page.Height);
                using (var font = new Font("Arial", Math.Max(9, page.Width / 110f), FontStyle.Bold))
                    foreach (DocField f in r.Fields)
                    {
                        if (f.RegionNorm.IsEmpty && f.Region.IsEmpty) continue;
                        RectangleF b = !f.RegionNorm.IsEmpty
                            ? new RectangleF(f.RegionNorm.X * page.Width, f.RegionNorm.Y * page.Height,
                                f.RegionNorm.Width * page.Width, f.RegionNorm.Height * page.Height)
                            : new RectangleF(f.Region.X, f.Region.Y, f.Region.Width, f.Region.Height);
                        Color c = string.IsNullOrWhiteSpace(f.Value) ? Color.Gray
                            : f.Status == FieldStatus.Ok ? Color.LimeGreen : Color.OrangeRed;
                        using (var pen = new Pen(c, 3)) g.DrawRectangle(pen, b.X, b.Y, b.Width, b.Height);
                        g.DrawString(f.Key, font, new SolidBrush(c), b.X, Math.Max(0, b.Y - font.Height));
                    }
                string name = Path.GetFileNameWithoutExtension(path) + (rotate != 0 ? "_rot" + rotate : "") + "_annot.png";
                canvas.Save(Path.Combine(dir, name), System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("  annotated page saved: " + Path.Combine(dir, name));
            }
        }

        private static void DumpRawText(string path, int rotate)
        {
            Console.WriteLine(new string('=', 78));
            Console.WriteLine(Path.GetFileName(path) + "   raw text");
            Console.WriteLine(new string('=', 78));
            if (!File.Exists(path)) { Console.WriteLine("  missing file"); return; }
            using (Bitmap raw = DocumentAI.LoadImage(path))
            using (Bitmap image = rotate == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, rotate))
            using (var session = new OcrSession(image))
            {
                OcrResult page = session.Page();
                string[] lines = (page.Text ?? "").Replace("\r", "")
                    .Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    Console.WriteLine("  [" + i.ToString().PadLeft(3) + "] " + lines[i]);
            }
            Console.WriteLine();
        }

        private static void DumpWords(string path, int rotate, float from)
        {
            Console.WriteLine(new string('=', 78));
            Console.WriteLine(Path.GetFileName(path) + "   words from y=" + from.ToString("0.000"));
            Console.WriteLine(new string('=', 78));
            if (!File.Exists(path)) { Console.WriteLine("  missing file"); return; }

            using (Bitmap raw = DocumentAI.LoadImage(path))
            using (Bitmap image = rotate == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, rotate))
            using (var session = new OcrSession(image))
            {
                OcrResult page = session.Page();
                float w = page.PageWidth, h = page.PageHeight;
                var rows = page.Words
                    .Where(x => !string.IsNullOrWhiteSpace(x.Text) && (x.Y + x.Height / 2f) / h >= from)
                    .GroupBy(x => (int)Math.Round((x.Y + x.Height / 2f) / h * 400f))
                    .OrderBy(gr => gr.Key);

                foreach (var row in rows)
                {
                    var line = row.OrderBy(x => x.X).ToList();
                    float y0 = line.Min(x => x.Y) / h, y1 = line.Max(x => x.Y + x.Height) / h;
                    Console.WriteLine("  y " + y0.ToString("0.0000") + "-" + y1.ToString("0.0000"));
                    foreach (OcrWord x in line)
                        Console.WriteLine("      x " + (x.X / w).ToString("0.0000") +
                                          "-" + ((x.X + x.Width) / w).ToString("0.0000") +
                                          "  y " + (x.Y / h).ToString("0.0000") +
                                          "-" + ((x.Y + x.Height) / h).ToString("0.0000") +
                                          "  c" + x.Confidence.ToString().PadLeft(3) + "  " + x.Text);
                }
            }
            Console.WriteLine();
        }
    }
}
