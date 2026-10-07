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
            if (args.Length >= 3 && args[0] == "gen")
                return Gen(int.Parse(args[1]), args[2], args.Length > 3 ? int.Parse(args[3]) : 1);
            if (args.Length >= 3 && args[0] == "realset") return RealSet(args[1], args[2]);
            if (args.Length >= 5 && args[0] == "sheet") return Sheet(args[1], args[2], args[3], args[4]);
            if (args.Length >= 3 && args[0] == "eval") return Eval(args[1], args[2], args.Length > 3 && args[3] == "--tune");
            if (args.Length >= 3 && args[0] == "dist") return Dist(args[1], args[2]);
            if (args.Length >= 3 && args[0] == "read") return ReadBench(args[1], args[2], args.Skip(3).ToArray());
            if (args.Length >= 4 && args[0] == "compare") return Compare(args[1], args[2], args[3]);

            Console.WriteLine("Usage: CROMS.OwnOcr.Bench.exe debug <imageOrFolder> <outFolder>");
            Console.WriteLine("       CROMS.OwnOcr.Bench.exe segment <cropsFolder> [outFolder]");
            Console.WriteLine("       CROMS.OwnOcr.Bench.exe gen <lines> <out.bin> [seed]      synthetic training set");
            Console.WriteLine("       CROMS.OwnOcr.Bench.exe realset <cropsFolder> <out.bin>   labelled glyphs from real crops");
            Console.WriteLine("       CROMS.OwnOcr.Bench.exe sheet <synth.bin> <real.bin> <out.png> <chars>");
            return 2;
        }

        // The fonts sit beside the CROMS.OwnOcr project; the bench runs from its own bin folder.
        private static string FontDir()
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.OwnOcr"));
        }

        // ------------------------------------- the whole engine against Tesseract, per field
        // Same crops, same truth, same rows as the segmentation benchmark. The Tesseract column
        // is NOT raw Tesseract: it is what CROMS ended up with after region reading, several
        // renderings, voting and context repair - the production result - so beating it is hard
        // and matching it would already say something.
        //   read <crops> <synth.bin> [--nodp] [--penalty 0.25] [--debug <outDir>]
        //     --nodp     use the plain thinnest-column cut (the Day-2 splitter) for comparison
        //     --penalty  price per character piece in the splitter search
        //     --debug    write the stage-by-stage picture of every field the engine got wrong
        private static int ReadBench(string dir, string synthFile, string[] opts)
        {
            bool noDp = opts.Contains("--nodp");
            bool useGlobal = opts.Contains("--global");
            double penalty = 0.25;
            string debugDir = null, lexDir = null; float realWeight = 0;
            for (int i = 0; i < opts.Length; i++)
            {
                if (opts[i] == "--penalty" && i + 1 < opts.Length)
                    penalty = double.Parse(opts[++i], System.Globalization.CultureInfo.InvariantCulture);
                if (opts[i] == "--debug" && i + 1 < opts.Length) debugDir = opts[++i];
                if (opts[i] == "--lexicon" && i + 1 < opts.Length) lexDir = opts[++i];
                if (opts[i] == "--realtrain" && i + 1 < opts.Length)
                    realWeight = float.Parse(opts[++i], System.Globalization.CultureInfo.InvariantCulture);
            }

            var all = Dataset.Load(synthFile);
            Console.WriteLine("training k-NN on {0} synthetic glyphs ...  splitter: {1}", all.Count,
                noDp ? "thinnest column (Day 2)" : "classifier-scored search, penalty " + penalty);
            if (debugDir != null) Directory.CreateDirectory(debugDir);
            // Every field in this benchmark is text (names, places, nationality, occupation), so
            // field knowledge may be switched on: no digits, one letter case per word.
            Func<KnnClassifier, OwnOcrReader> makeReader = k =>
            {
                var rd = new OwnOcrReader(k, noDp ? null : new KnnClassifier(all, 150, 5));
                rd.SplitPenalty = penalty; rd.GlobalMerge = useGlobal;
                rd.LettersOnly = opts.Contains("--letters"); rd.CaseConsistency = opts.Contains("--case");
                rd.TrimEdgeMarks = opts.Contains("--trim");
                return rd;
            };
            OwnOcrReader plainReader = realWeight > 0 ? null : makeReader(new KnnClassifier(all, 600, 3));

            // Leave-one-document-out: the reader used for a document has seen real glyphs from the
            // OTHER documents only, so the score is not the engine remembering its own test.
            var readerFor = new Dictionary<string, OwnOcrReader>();
            if (realWeight > 0)
            {
                var realBy = new Dictionary<string, List<Sample>>();
                foreach (Row r0 in ReadManifest(dir).Where(x => !NotPrintedAsStored.Any(s => x.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    string tr0 = new string(r0.Truth.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    if (tr0.Length == 0 || tr0.Any(c => Charset.IndexOf(c) < 0)) continue;
                    using (Bitmap b0 = new Bitmap(Path.Combine(dir, r0.File)))
                    {
                        Analysis a0 = OwnOcrEngine.Analyze(b0);
                        if (a0.Cells.Count != tr0.Length) continue;
                        List<Sample> l;
                        if (!realBy.TryGetValue(r0.Sample, out l)) realBy[r0.Sample] = l = new List<Sample>();
                        for (int i0 = 0; i0 < tr0.Length; i0++) l.Add(Sample.FromCell(a0.Cells[i0], Charset.IndexOf(tr0[i0])));
                    }
                }
                foreach (string doc in realBy.Keys.ToList())
                {
                    var others = realBy.Where(kv => kv.Key != doc).SelectMany(kv => kv.Value).ToList();
                    readerFor[doc] = makeReader(new KnnClassifier(all, others, realWeight, 600, 3));
                    Console.WriteLine("  reader without '{0}': +{1} real glyphs from the other documents (vote x{2})", doc, others.Count, realWeight);
                }
            }
            LexiconSet lex = lexDir == null ? null : LexiconSet.Load(lexDir);
            int repExact = 0, repExactCI = 0, repChanged = 0, repBetter = 0, repWorse = 0; double repSim = 0;
            var repLog = new List<string>();

            List<Row> rows = ReadManifest(dir)
                .Where(r => !NotPrintedAsStored.Any(s => r.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            int n = 0, oursExact = 0, tessExact = 0, oursExactCI = 0, tessExactCI = 0;
            double oursSim = 0, tessSim = 0;
            var examples = new List<string>();
            var bySample = new Dictionary<string, double[]>();   // sample -> {n, oursExact, tessExact, oursSim, tessSim}

            foreach (Row r in rows)
            {
                string path = Path.Combine(dir, r.File);
                if (!File.Exists(path)) continue;
                Console.Error.WriteLine("[{0:HH:mm:ss}] {1}", DateTime.Now, r.File);
                string ours;
                using (Bitmap bmp = new Bitmap(path))
                {
                    OwnOcrReader reader = plainReader;
                    if (reader == null && !readerFor.TryGetValue(r.Sample, out reader)) reader = readerFor.Values.First();
                    ReadResult rr = reader.Read(bmp);
                    ours = rr.Text;
                    if (lex != null)
                    {
                        RepairResult rep = lex.Repair(r.Key, rr);
                        string before = Norm(ours), after = Norm(rep.Text), tr = Norm(r.Truth);
                        double sb = Similarity(before, tr), sa = Similarity(after, tr);
                        if (rep.Changed) { repChanged++; if (sa > sb) repBetter++; else if (sa < sb) repWorse++;
                            repLog.Add(string.Format("  {0,-22} {1,-8} truth [{2}]  read [{3}] -> [{4}]  ({5:0}%->{6:0}%)", Trunc(r.Key, 22), rep.Kind, tr, before, after, sb * 100, sa * 100)); }
                        if (after == tr) repExact++;
                        if (string.Equals(after, tr, StringComparison.OrdinalIgnoreCase)) repExactCI++;
                        repSim += sa;
                    }
                    if (debugDir != null && Norm(ours) != Norm(r.Truth))
                        using (Bitmap pic = DebugRender.Render(bmp, rr.Analysis, r.File + "   truth '" + r.Truth + "'   ours '" + ours + "'"))
                            pic.Save(Path.Combine(debugDir, Path.GetFileNameWithoutExtension(r.File) + "_debug.png"), ImageFormat.Png);
                }

                string truth = Norm(r.Truth), o = Norm(ours), t = Norm(r.Tesseract);
                n++;
                bool oe = o == truth, te = t == truth;
                bool oci = string.Equals(o, truth, StringComparison.OrdinalIgnoreCase);
                bool tci = string.Equals(t, truth, StringComparison.OrdinalIgnoreCase);
                double os = Similarity(o, truth), ts = Similarity(t, truth);
                if (oe) oursExact++; if (te) tessExact++; if (oci) oursExactCI++; if (tci) tessExactCI++;
                oursSim += os; tessSim += ts;

                double[] s;
                if (!bySample.TryGetValue(r.Sample, out s)) bySample[r.Sample] = s = new double[5];
                s[0]++; if (oe) s[1]++; if (te) s[2]++; s[3] += os; s[4] += ts;

                examples.Add(string.Format("  {0,-26} truth '{1}'   ours '{2}' ({3:0}%)   tesseract '{4}' ({5:0}%)",
                    Trunc(r.Key, 26), truth, o, os * 100, t, ts * 100));
            }

            Console.WriteLine();
            Console.WriteLine("{0} printed-text fields (names, places, citizenship, ...)", n);
            Console.WriteLine("                         exact   exact(any case)   mean character similarity");
            Console.WriteLine("  OWN ENGINE        {0,6:0.0}%   {1,10:0.0}%        {2,10:0.0}%", oursExact * 100.0 / n, oursExactCI * 100.0 / n, oursSim * 100 / n);
            Console.WriteLine("  CROMS (Tesseract) {0,6:0.0}%   {1,10:0.0}%        {2,10:0.0}%", tessExact * 100.0 / n, tessExactCI * 100.0 / n, tessSim * 100 / n);
            if (lex != null)
            {
                Console.WriteLine("  OWN + REPAIR      {0,6:0.0}%   {1,10:0.0}%        {2,10:0.0}%   (repaired {3} fields: {4} better, {5} worse)",
                    repExact * 100.0 / n, repExactCI * 100.0 / n, repSim * 100 / n, repChanged, repBetter, repWorse);
                foreach (string l in repLog) Console.WriteLine(l);
            }
            Console.WriteLine();
            foreach (var kv in bySample)
                Console.WriteLine("  {0,-16} fields {1,2}   exact ours {2,2} vs tess {3,2}   similarity ours {4,3:0}% vs tess {5,3:0}%",
                    kv.Key, kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3] * 100 / kv.Value[0], kv.Value[4] * 100 / kv.Value[0]);
            Console.WriteLine();
            foreach (string e in examples.Take(40)) Console.WriteLine(e);
            return 0;
        }

        private static string Norm(string s)
        {
            return string.Join(" ", (s ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        // 1 - edit distance / longer length: 100% = identical, 0% = nothing in common.
        private static double Similarity(string a, string b)
        {
            int max = Math.Max(a.Length, b.Length);
            if (max == 0) return 1.0;
            return 1.0 - (double)Levenshtein(a, b) / max;
        }

        private static int Levenshtein(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                                       d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }

        // ------------------------- how far is the nearest example, for right and wrong readings?
        // The splitter search scores a candidate character by this distance, so what matters is
        // whether it separates a glyph the classifier got RIGHT from one it got WRONG (a clump
        // of two letters, half a letter). If the two overlap, the distance is a poor judge.
        private static int Dist(string synthFile, string realFile)
        {
            List<Sample> all = Dataset.Load(synthFile), real = Dataset.Load(realFile);
            var train = new List<Sample>(); var hold = new List<Sample>();
            for (int i = 0; i < all.Count; i++) (i % 10 == 0 ? hold : train).Add(all[i]);
            var knn = new KnnClassifier(train, 150, 5);

            Action<string, List<Sample>> report = (name, set) =>
            {
                var good = new List<double>(); var bad = new List<double>();
                var goodS = new List<double>(); var badS = new List<double>();
                foreach (Sample s in set)
                {
                    float d; Prediction[] r = knn.Rank(s.Features(), 1, out d);
                    bool ok = r.Length > 0 && r[0].Label == s.Label;
                    (ok ? good : bad).Add(d);
                    (ok ? goodS : badS).Add(r.Length > 0 ? r[0].Score : 0);
                }
                Func<List<double>, string> pct = l =>
                {
                    if (l.Count == 0) return "n/a";
                    var o = l.OrderBy(x => x).ToList();
                    return string.Format("p10 {0:0.00}  median {1:0.00}  p90 {2:0.00}", o[o.Count / 10], o[o.Count / 2], o[o.Count * 9 / 10]);
                };
                Console.WriteLine("{0}: {1} right, {2} wrong", name, good.Count, bad.Count);
                Console.WriteLine("   nearest distance, RIGHT: {0}", pct(good));
                Console.WriteLine("   nearest distance, WRONG: {0}", pct(bad));
                Console.WriteLine("   vote share,       RIGHT: mean {0:0.00}   WRONG: mean {1:0.00}",
                    goodS.Count == 0 ? 0 : goodS.Average(), badS.Count == 0 ? 0 : badS.Average());
            };
            report("synthetic hold-out", hold.Take(2000).ToList());
            report("real glyphs", real);
            return 0;
        }

        // ----------------------------------------------------------- classifier accuracy
        // Two scores, because they answer different questions:
        //   synthetic hold-out - every 10th synthetic glyph is kept out of training. It says
        //     whether the classifier works AT ALL; it is an upper bound, since the held-out
        //     glyphs come from the same generator as the training ones.
        //   real - the glyphs cut from the office's own scans, which the classifier never saw
        //     in any form. THIS is the number that predicts how the engine will read a form.
        private static int Eval(string synthFile, string realFile, bool tune)
        {
            List<Sample> all = Dataset.Load(synthFile), real = Dataset.Load(realFile);
            var train = new List<Sample>(); var hold = new List<Sample>();
            for (int i = 0; i < all.Count; i++) (i % 10 == 0 ? hold : train).Add(all[i]);
            Console.WriteLine("synthetic: {0} train / {1} hold-out    real test glyphs: {2}", train.Count, hold.Count, real.Count);

            var settings = tune
                ? new[] { new[] { 1f, 1f, 1f }, new[] { 1f, 1.5f, 1f }, new[] { 1f, 0.6f, 1f }, new[] { 1f, 1f, 0.4f },
                          new[] { 1f, 1f, 2f }, new[] { 1f, 1.5f, 2f }, new[] { 0.6f, 1.5f, 1f }, new[] { 1f, 2f, 1f } }
                : new[] { new[] { Features.ZoneWeight, Features.HogWeight, Features.GeomWeight } };

            foreach (float[] w in settings)
            {
                Features.ZoneWeight = w[0]; Features.HogWeight = w[1]; Features.GeomWeight = w[2];
                var sw = System.Diagnostics.Stopwatch.StartNew();
                // Tuning compares many settings, so it uses a smaller reference set and fewer
                // hold-out queries; the full run (no --tune) uses everything.
                var knn = new KnnClassifier(train, tune ? 500 : 1200, 3);
                double synthAcc = Accuracy(knn, hold.Take(tune ? 800 : 3000).ToList(), null);
                var confusions = new Dictionary<string, int>();
                double realAcc = Accuracy(knn, real, confusions);
                double realCI = CaseInsensitive(knn, real);
                Console.WriteLine("weights zone {0} hog {1} geom {2}:  synthetic hold-out {3:0.0}%   REAL {4:0.0}%   (ignoring upper/lower case {5:0.0}%)   [{6} refs, {7:0.0}s]",
                    w[0], w[1], w[2], synthAcc, realAcc, realCI, knn.Count, sw.Elapsed.TotalSeconds);

                if (!tune)
                {
                    Console.WriteLine();
                    Console.WriteLine("Most common mistakes on the real glyphs (true -> read):");
                    foreach (var kv in confusions.OrderByDescending(c => c.Value).Take(15))
                        Console.WriteLine("  {0}   x{1}", kv.Key, kv.Value);
                }
            }
            return 0;
        }

        private static double Accuracy(KnnClassifier knn, List<Sample> set, Dictionary<string, int> confusions)
        {
            if (set.Count == 0) return 0;
            int ok = 0;
            foreach (Sample s in set)
            {
                Prediction p = knn.Best(s.Features());
                if (p != null && p.Label == s.Label) ok++;
                else if (confusions != null && p != null)
                {
                    string key = "'" + Charset.At(s.Label) + "' -> '" + p.Char + "'";
                    int n; confusions.TryGetValue(key, out n); confusions[key] = n + 1;
                }
            }
            return ok * 100.0 / set.Count;
        }

        // Upper/lower case of the same letter (o/O, s/S, c/C, x/X ...) are the same shape at
        // different sizes and are the cheapest mistake to repair later from context.
        private static double CaseInsensitive(KnnClassifier knn, List<Sample> set)
        {
            if (set.Count == 0) return 0;
            int ok = 0;
            foreach (Sample s in set)
            {
                Prediction p = knn.Best(s.Features());
                if (p != null && char.ToLowerInvariant(p.Char) == char.ToLowerInvariant(Charset.At(s.Label))) ok++;
            }
            return ok * 100.0 / set.Count;
        }

        // ------------------------------------------------------------ synthetic training set
        private static int Gen(int lines, string outFile, int seed)
        {
            var stats = new SynthStats();
            Console.WriteLine("Rendering {0} lines from {1}", lines, FontDir());
            foreach (FontSpec f in Synth.LoadFonts(FontDir())) Console.WriteLine("  font: {0}", f.Name);

            List<Sample> set = Synth.Generate(lines, seed, FontDir(), stats,
                done => Console.WriteLine("  {0}/{1} lines", done, lines));
            Dataset.Save(outFile, set);

            Console.WriteLine();
            Console.WriteLine("lines {0}   kept {1} ({2}%)   dropped {3} (letters touched or speckle - count did not match)",
                stats.Lines, stats.Kept, stats.Lines == 0 ? 0 : stats.Kept * 100 / stats.Lines, stats.Dropped);
            Console.WriteLine("glyphs {0}   in {1:0.0}s   -> {2}", set.Count, stats.Elapsed.TotalSeconds, outFile);

            int min = int.MaxValue, max = 0; string minC = "", maxC = "";
            var thin = new List<string>();
            for (int i = 0; i < Charset.Count; i++)
            {
                int n = stats.PerClass[i];
                if (n < min) { min = n; minC = Charset.At(i).ToString(); }
                if (n > max) { max = n; maxC = Charset.At(i).ToString(); }
                if (n < 100) thin.Add(Charset.At(i) + "=" + n);
            }
            Console.WriteLine("per class: min {0} ('{1}')   max {2} ('{3}')   reject (not one character): {4}",
                min, minC, max, maxC, stats.PerClass[Charset.Reject]);
            if (thin.Count > 0) Console.WriteLine("classes under 100 examples: " + string.Join(" ", thin));
            return 0;
        }

        // ------------------------------------------- labelled glyphs from the real scans
        // Only crops where the engine found exactly as many characters as the paper has can
        // be labelled by position, so only those are used. They are a TEST set: the classifier
        // is trained on synthetic data alone, so scoring on these is an honest measure.
        private static int RealSet(string dir, string outFile)
        {
            var set = new List<Sample>();
            int crops = 0, used = 0;
            foreach (Row r in ReadManifest(dir)
                .Where(x => !NotPrintedAsStored.Any(s => x.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                crops++;
                string truth = new string(r.Truth.Where(c => !char.IsWhiteSpace(c)).ToArray());
                if (truth.Length == 0 || truth.Any(c => Charset.IndexOf(c) < 0)) continue;
                using (Bitmap bmp = new Bitmap(Path.Combine(dir, r.File)))
                {
                    Analysis a = OwnOcrEngine.Analyze(bmp);
                    if (a.Cells.Count != truth.Length) continue;
                    for (int i = 0; i < truth.Length; i++)
                        set.Add(Sample.FromCell(a.Cells[i], Charset.IndexOf(truth[i])));
                    used++;
                }
            }
            Dataset.Save(outFile, set);
            Console.WriteLine("{0} of {1} printed-text crops had a matching character count -> {2} labelled real glyphs -> {3}",
                used, crops, set.Count, outFile);
            return 0;
        }

        // ------------------------------------- side-by-side: synthetic against real, per character
        private static int Sheet(string synthFile, string realFile, string outFile, string chars)
        {
            List<Sample> synth = Dataset.Load(synthFile), real = Dataset.Load(realFile);
            const int px = 3, cell = GlyphCell.Size * px, gap = 4, per = 12;
            int rows = chars.Length;
            using (var bmp = new Bitmap(40 + (per * 2 + 1) * (cell + gap) + 20, rows * (cell + gap) + 30))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var font = new Font("Segoe UI", 12f, FontStyle.Bold))
            {
                g.Clear(Color.White);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawString("synthetic", font, Brushes.DimGray, 40, 4);
                g.DrawString("real (from the scans)", font, Brushes.DimGray, 40 + (per + 1) * (cell + gap), 4);

                for (int r = 0; r < rows; r++)
                {
                    int label = Charset.IndexOf(chars[r]);
                    int y = 26 + r * (cell + gap);
                    g.DrawString(chars[r].ToString(), font, Brushes.Black, 8, y + 16);
                    Draw(g, synth.Where(s => s.Label == label).Take(per).ToList(), 40, y, px, gap);
                    Draw(g, real.Where(s => s.Label == label).Take(per).ToList(), 40 + (per + 1) * (cell + gap), y, px, gap);
                }
                bmp.Save(outFile, ImageFormat.Png);
            }
            Console.WriteLine("wrote " + outFile);
            return 0;
        }

        private static void Draw(Graphics g, List<Sample> samples, int x0, int y, int px, int gap)
        {
            int n = GlyphCell.Size;
            for (int i = 0; i < samples.Count; i++)
            {
                using (var b = new Bitmap(n, n))
                {
                    for (int yy = 0; yy < n; yy++)
                        for (int xx = 0; xx < n; xx++)
                        {
                            int v = 255 - samples[i].Image[yy * n + xx];
                            b.SetPixel(xx, yy, Color.FromArgb(v, v, v));
                        }
                    g.DrawImage(b, new Rectangle(x0 + i * (n * px + gap), y, n * px, n * px));
                }
            }
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
            public string File, Sample, Key, Truth, Tesseract; public int TessConf;
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
                int tc = 0; if (c.Length > 5) int.TryParse(c[5], out tc);
                rows.Add(new Row { File = c[0], Sample = c[1], Key = c[2], Truth = c[3], Tesseract = c[4], TessConf = tc });
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

        // ------------------------------------------------ head to head + hybrid policies
        // compare <crops> <synth.bin> <lexiconDir>
        // Same crops and truth as `read`. For every field it keeps what each engine said and
        // how long the own engine took, then asks: if the two were combined by a simple rule,
        // would the result beat Tesseract alone? The whole threshold sweep is printed so the
        // choice can be judged, not trusted.
        private static int Compare(string dir, string synthFile, string lexDir)
        {
            var all = Dataset.Load(synthFile);
            var reader = new OwnOcrReader(new KnnClassifier(all, 600, 3));
            reader.LettersOnly = true; reader.CaseConsistency = true; reader.TrimEdgeMarks = true;
            LexiconSet lex = LexiconSet.Load(lexDir);
            List<Row> rows = ReadManifest(dir)
                .Where(r => !NotPrintedAsStored.Any(s => r.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                .Where(r => File.Exists(Path.Combine(dir, r.File))).ToList();

            var recs = new List<Rec>();
            var ms = new List<double>();
            foreach (Row r in rows)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                string raw, rep; bool changed; string kind;
                using (Bitmap bmp = new Bitmap(Path.Combine(dir, r.File)))
                {
                    ReadResult rr = reader.Read(bmp);
                    RepairResult rp = lex.Repair(r.Key, rr);
                    raw = rr.Text; rep = rp.Text; changed = rp.Changed; kind = rp.Kind;
                }
                sw.Stop(); ms.Add(sw.Elapsed.TotalMilliseconds);
                recs.Add(new Rec { R = r, Own = Norm(raw), OwnRep = Norm(rep), Changed = changed, HasLex = kind != "",
                                   Tess = Norm(r.Tesseract), Truth = Norm(r.Truth), Ms = sw.Elapsed.TotalMilliseconds });
            }
            int n = recs.Count;
            Func<Func<Rec, string>, string> line = pick =>
            {
                int ex = 0, ci = 0; double sim = 0;
                foreach (Rec x in recs)
                {
                    string t = pick(x);
                    if (t == x.Truth) ex++;
                    if (string.Equals(t, x.Truth, StringComparison.OrdinalIgnoreCase)) ci++;
                    sim += Similarity(t, x.Truth);
                }
                return string.Format("{0,3}/{1}  exact {2,5:0.0}%   any case {3,5:0.0}%   similarity {4,5:0.0}%",
                    ex, n, ex * 100.0 / n, ci * 100.0 / n, sim * 100 / n);
            };

            Console.WriteLine("{0} printed-text fields over every document that has crops", n);
            Console.WriteLine();
            Console.WriteLine("SINGLE ENGINES");
            Console.WriteLine("  Tesseract (production)     " + line(x => x.Tess));
            Console.WriteLine("  Own engine, raw            " + line(x => x.Own));
            Console.WriteLine("  Own engine + lexicon       " + line(x => x.OwnRep));
            Console.WriteLine("  Oracle (best of the two)   " + line(x => x.Tess == x.Truth ? x.Tess : x.OwnRep));
            Console.WriteLine();
            Console.WriteLine("OVERLAP  both right {0}   only Tesseract {1}   only own {2}   neither {3}",
                recs.Count(x => x.Tess == x.Truth && x.OwnRep == x.Truth),
                recs.Count(x => x.Tess == x.Truth && x.OwnRep != x.Truth),
                recs.Count(x => x.Tess != x.Truth && x.OwnRep == x.Truth),
                recs.Count(x => x.Tess != x.Truth && x.OwnRep != x.Truth));
            Console.WriteLine();

            Console.WriteLine("HYBRID RULE A: own+lexicon when Tesseract confidence < T, else Tesseract");
            for (int t = 40; t <= 100; t += 10)
            {
                int T = t;
                Console.WriteLine("  T={0,3}  own used on {1,2} fields   {2}", T, recs.Count(x => x.R.TessConf < T), line(x => x.R.TessConf < T ? x.OwnRep : x.Tess));
            }
            Console.WriteLine();
            Console.WriteLine("HYBRID RULE B: own+lexicon only when the lexicon REPAIRED it (closed-list fields) and Tesseract conf < T");
            for (int t = 60; t <= 100; t += 10)
            {
                int T = t;
                Console.WriteLine("  T={0,3}  own used on {1,2} fields   {2}", T, recs.Count(x => x.Changed && x.R.TessConf < T), line(x => x.Changed && x.R.TessConf < T ? x.OwnRep : x.Tess));
            }
            Console.WriteLine();
            Console.WriteLine("HYBRID RULE C: agree (any case) -> keep; disagree and Tesseract conf < T -> own");
            for (int t = 50; t <= 90; t += 20)
            {
                int T = t;
                Console.WriteLine("  T={0,3}  {1}", T, line(x => string.Equals(x.Tess, x.OwnRep, StringComparison.OrdinalIgnoreCase) ? x.Tess : (x.R.TessConf < T ? x.OwnRep : x.Tess)));
            }

            Console.WriteLine();
            Console.WriteLine("BY DOCUMENT, exact fields (Tesseract / own+lexicon / rule A T=70)");
            foreach (var g in recs.GroupBy(x => x.R.Sample))
                Console.WriteLine("  {0,-16} {1,2} fields   {2,2} / {3,2} / {4,2}", g.Key, g.Count(),
                    g.Count(x => x.Tess == x.Truth), g.Count(x => x.OwnRep == x.Truth),
                    g.Count(x => (x.R.TessConf < 70 ? x.OwnRep : x.Tess) == x.Truth));

            ms.Sort();
            Console.WriteLine();
            Console.WriteLine("OWN ENGINE TIME per field: mean {0:0} ms   median {1:0} ms   95th {2:0} ms   max {3:0} ms   total {4:0.0} s for {5} fields",
                ms.Average(), ms[ms.Count / 2], ms[(int)(ms.Count * 0.95)], ms[ms.Count - 1], ms.Sum() / 1000, n);
            Console.WriteLine();
            Console.WriteLine("EVERY FIELD");
            foreach (Rec x in recs)
                Console.WriteLine("  {0,-9}{1,-22} truth '{2}' | tess '{3}' ({4}%) {5} | own '{6}' {7}",
                    Trunc(x.R.Sample, 9), Trunc(x.R.Key, 22), x.Truth, x.Tess, x.R.TessConf, x.Tess == x.Truth ? "OK" : "--",
                    x.OwnRep, x.OwnRep == x.Truth ? "OK" : "--");
            return 0;
        }

        private sealed class Rec
        {
            public Row R; public string Own, OwnRep, Tess, Truth; public bool Changed, HasLex; public double Ms;
        }

        private static string Trunc(string s, int n) { return s.Length <= n ? s : s.Substring(0, n); }
    }
}
