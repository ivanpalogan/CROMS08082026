using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CROMS.OwnOcr;

namespace CROMS.OwnOcr.Bench
{
    /// <summary>
    /// Harness for the own OCR engine.
    /// <para/>
    /// Usage:
    ///   CROMS.OwnOcr.Bench.exe debug   &lt;imageOrFolder&gt; &lt;outFolder&gt;
    ///       stage-by-stage picture per image (threshold, rules, line, characters).
    ///   CROMS.OwnOcr.Bench.exe segment &lt;cropsFolder&gt; [outFolder]
    ///       how often the number of characters found equals the number the paper has
    ///       (manifest.tsv from CROMS.DocTest --dump-crops). Optional folder gets debug pictures
    ///       of the worst cases.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "debug") return Debug(args[1], args[2]);
            if (args.Length >= 2 && args[0] == "segment") return Segment(args[1], args.Length > 2 ? args[2] : null);

            Console.WriteLine("Usage: CROMS.OwnOcr.Bench.exe debug <imageOrFolder> <outFolder>");
            Console.WriteLine("       CROMS.OwnOcr.Bench.exe segment <cropsFolder> [outFolder]");
            return 2;
        }

        // ---------------------------------------------------------------- debug pictures
        private static int Debug(string input, string outDir)
        {
            Directory.CreateDirectory(outDir);
            string[] files = Directory.Exists(input)
                ? Directory.GetFiles(input, "*.png").OrderBy(f => f).ToArray()
                : new[] { input };

            Console.WriteLine("{0,-40} {1,9} {2,5} {3,5} {4,5} {5,7} {6,5} {7,6}",
                "image", "size", "x", "hRule", "vRule", "pieces", "cells", "medH");
            foreach (string f in files)
            {
                using (Bitmap bmp = new Bitmap(f))
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    Analysis a = OwnOcrEngine.Analyze(bmp);
                    sw.Stop();

                    string name = Path.GetFileNameWithoutExtension(f);
                    using (Bitmap pic = DebugRender.Render(bmp, a, name))
                        pic.Save(Path.Combine(outDir, name + "_debug.png"), ImageFormat.Png);

                    Console.WriteLine("{0,-40} {1,9} {2,5:0.0} {3,5} {4,5} {5,7} {6,5} {7,6:0}   {8} ms",
                        Trunc(name, 40), bmp.Width + "x" + bmp.Height, a.Scale,
                        a.Rules.HorizontalRules, a.Rules.VerticalRules,
                        a.Glyphs.Count, a.Cells.Count, a.MedianHeight, sw.ElapsedMilliseconds);
                    if (files.Length == 1)
                        foreach (int[] c in a.Rules.HorizontalCandidates)
                            Console.WriteLine("    rule candidate x {0}-{1} y {2}-{3} {4}", c[0], c[2], c[1], c[3], c[4] == 1 ? "KEPT" : "rejected");
                }
            }
            return 0;
        }

        // ------------------------------------------------------- segmentation accuracy
        private sealed class Row
        {
            public string File, Sample, Key, Truth, Tesseract;
        }

        // Keys whose stored truth is NOT the text as printed (normalised dates, an enum the
        // form shows as a tick box, a number with its unit dropped) cannot be compared with
        // a count of printed characters, so they are left out of this measurement.
        private static readonly string[] NotPrintedAsStored =
            { "Date", "Sex", "Weight", "Age", "Registry", "BirthOrder", "TimeOf",
              // tick-box rows: the crop shows every printed option, the truth is the one ticked
              "Attendant", "TypeOfBirth" };

        private static List<Row> ReadManifest(string dir)
        {
            string path = Path.Combine(dir, "manifest.tsv");
            var rows = new List<Row>();
            foreach (string line in File.ReadAllLines(path).Skip(1))
            {
                string[] c = line.Split('\t');
                if (c.Length < 5) continue;
                rows.Add(new Row { File = c[0], Sample = c[1], Key = c[2], Truth = c[3], Tesseract = c[4] });
            }
            return rows;
        }

        private static int Segment(string dir, string outDir)
        {
            List<Row> rows = ReadManifest(dir)
                .Where(r => !NotPrintedAsStored.Any(s => r.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            int total = 0, exact = 0, within1 = 0, noLine = 0;
            var bySample = new Dictionary<string, int[]>();   // sample -> {total, exact, within1}
            var bad = new List<Tuple<int, Row, int, int>>();

            foreach (Row r in rows)
            {
                string path = Path.Combine(dir, r.File);
                if (!File.Exists(path)) continue;
                using (Bitmap bmp = new Bitmap(path))
                {
                    Analysis a = OwnOcrEngine.Analyze(bmp);
                    int want = r.Truth.Count(ch => !char.IsWhiteSpace(ch));
                    int got = a.Cells.Count;
                    int diff = Math.Abs(got - want);

                    total++;
                    if (a.Line == null) noLine++;
                    if (diff == 0) exact++;
                    if (diff <= 1) within1++;

                    int[] s;
                    if (!bySample.TryGetValue(r.Sample, out s)) bySample[r.Sample] = s = new int[3];
                    s[0]++; if (diff == 0) s[1]++; if (diff <= 1) s[2]++;

                    if (diff >= 2) bad.Add(Tuple.Create(diff, r, want, got));
                    if (outDir != null && diff >= 2)
                    {
                        Directory.CreateDirectory(outDir);
                        using (Bitmap pic = DebugRender.Render(bmp, a, r.File + "   truth='" + r.Truth + "'   want " + want + " got " + got))
                            pic.Save(Path.Combine(outDir, Path.GetFileNameWithoutExtension(r.File) + "_debug.png"), ImageFormat.Png);
                    }
                }
            }

            Console.WriteLine("Character-count segmentation over {0} printed-text crops", total);
            Console.WriteLine("  exact count   : {0,3} / {1}  ({2}%)", exact, total, total == 0 ? 0 : exact * 100 / total);
            Console.WriteLine("  within +/- 1  : {0,3} / {1}  ({2}%)", within1, total, total == 0 ? 0 : within1 * 100 / total);
            Console.WriteLine("  no line found : {0}", noLine);
            Console.WriteLine();
            foreach (var kv in bySample)
                Console.WriteLine("  {0,-16} exact {1,2}/{2,-2}   within1 {3,2}/{2}", kv.Key, kv.Value[1], kv.Value[0], kv.Value[2]);

            Console.WriteLine();
            Console.WriteLine("Worst (|got - want| >= 2):");
            foreach (var t in bad.OrderByDescending(x => x.Item1).Take(20))
                Console.WriteLine("  {0,-34} want {1,2}  got {2,2}   '{3}'", Trunc(t.Item2.File, 34), t.Item3, t.Item4, t.Item2.Truth);
            return 0;
        }

        private static string Trunc(string s, int n) { return s.Length <= n ? s : s.Substring(0, n); }
    }
}
