using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using CROMS.Data;

namespace CROMS.DocTest
{
    /// <summary>
    /// Exports every field's region from the real pipeline as a small PNG plus a manifest of
    /// what the paper says (ground truth from Truth.cs) and what Tesseract read.
    /// <para/>
    /// This is the data set for the OWN OCR engine (CROMS.OwnOcr): that engine takes a
    /// crop and returns text and has no dependency on Tesseract or on CROMS, so the crops
    /// are produced once, here, and the engine and its benchmark just read a folder. The
    /// Tesseract reading is stored beside the truth so "ours vs Tesseract" needs no second
    /// Tesseract run at benchmark time.
    /// <para/>
    /// Usage: CROMS.DocTest.exe --dump-crops outDir [sampleDir]
    /// </summary>
    internal static class CropDump
    {
        internal static int Run(string outDir, string sampleDir)
        {
            Directory.CreateDirectory(outDir);
            var manifest = new StringBuilder();
            manifest.AppendLine("file\tsample\tkey\ttruth\ttesseract\ttesseract_conf");
            int saved = 0;

            foreach (Truth.Sample s in Truth.Samples(sampleDir))
            {
                if (!File.Exists(s.File)) { Console.WriteLine("MISSING " + s.File); continue; }
                Console.WriteLine("Analysing " + Path.GetFileName(s.File) + " ...");

                DocAiResult r;
                Bitmap page;
                using (Bitmap raw = DocumentAI.LoadImage(s.File))
                {
                    r = DocumentAI.Analyze(raw);
                    // Region coordinates are in the page AFTER the pipeline turned it upright.
                    page = r.RotationApplied == 0 ? new Bitmap(raw) : OcrService.Rotate(raw, r.RotationApplied);
                }

                string tag = Sanitize(Path.GetFileNameWithoutExtension(s.File));
                var byKey = new Dictionary<string, DocField>(StringComparer.OrdinalIgnoreCase);
                foreach (DocField f in r.Fields) byKey[f.Key] = f;

                using (page)
                {
                    foreach (var kv in s.Expect)
                    {
                        DocField f;
                        if (!byKey.TryGetValue(kv.Key, out f)) continue;
                        RectangleF n = f.RegionNorm;
                        if (n.IsEmpty || n.Width <= 0 || n.Height <= 0) continue;

                        Rectangle px = Pad(n, page.Width, page.Height);
                        if (px.Width < 8 || px.Height < 8) continue;

                        string name = tag + "__" + kv.Key + ".png";
                        using (Bitmap crop = Crop(page, px))
                            crop.Save(Path.Combine(outDir, name), System.Drawing.Imaging.ImageFormat.Png);

                        manifest.AppendLine(string.Join("\t", new[]
                        {
                            name, tag, kv.Key, Clean(kv.Value), Clean(f.OcrValue ?? f.Value ?? ""),
                            f.Confidence.ToString()
                        }));
                        saved++;
                    }
                }
            }

            File.WriteAllText(Path.Combine(outDir, "manifest.tsv"), manifest.ToString(), new UTF8Encoding(false));
            Console.WriteLine("Saved {0} crops + manifest.tsv to {1}", saved, outDir);
            return saved > 0 ? 0 : 1;
        }

        // A little air around the field: the region reader needs vertical room around a
        // line (measured 2026-09-10) and so will any engine reading the same crop.
        private static Rectangle Pad(RectangleF n, int w, int h)
        {
            float padX = Math.Max(3f, n.Width * w * 0.01f);
            float padY = Math.Max(3f, n.Height * h * 0.12f);
            float x0 = n.X * w - padX, y0 = n.Y * h - padY;
            float x1 = (n.X + n.Width) * w + padX, y1 = (n.Y + n.Height) * h + padY;
            int ix0 = (int)Math.Max(0, Math.Floor(x0)), iy0 = (int)Math.Max(0, Math.Floor(y0));
            int ix1 = (int)Math.Min(w, Math.Ceiling(x1)), iy1 = (int)Math.Min(h, Math.Ceiling(y1));
            return new Rectangle(ix0, iy0, Math.Max(0, ix1 - ix0), Math.Max(0, iy1 - iy0));
        }

        private static Bitmap Crop(Bitmap src, Rectangle r)
        {
            var dst = new Bitmap(r.Width, r.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(src, new Rectangle(0, 0, r.Width, r.Height), r, GraphicsUnit.Pixel);
            }
            return dst;
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            string t = sb.ToString();
            return t.Length > 14 ? t.Substring(0, 14) : t;
        }

        private static string Clean(string s)
        {
            return (s ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}
