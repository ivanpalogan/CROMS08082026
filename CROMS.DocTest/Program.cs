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
            float wordsFrom = 0f;
            string truthDir = null;
            var files = new System.Collections.Generic.List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--rotate" && i + 1 < args.Length) { int.TryParse(args[++i], out rotate); continue; }
                if (args[i] == "--diag") { diag = true; continue; }
                if (args[i] == "--words")
                {
                    words = true;
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], out float from)) { i++; wordsFrom = from; }
                    continue;
                }
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

            // Ground-truth mode: the only run that says whether the RECORD is right.
            if (truthDir != null) return Truth.Run(truthDir);

            // Calibration mode: every printed word with its NORMALISED box, which is the
            // template space DocLayouts field rectangles are stated in. This is how a new
            // field region is measured off the reference scan rather than estimated.
            if (words)
            {
                foreach (string path in files) DumpWords(path, rotate, wordsFrom);
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
                Console.WriteLine("  elapsed       : " + (int)(DateTime.Now - started).TotalMilliseconds + " ms");
                Console.WriteLine();

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
                                          "  c" + x.Confidence.ToString().PadLeft(3) + "  " + x.Text);
                }
            }
            Console.WriteLine();
        }
    }
}
