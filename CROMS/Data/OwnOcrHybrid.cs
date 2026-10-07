using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CROMS.OwnOcr;

namespace CROMS.Data
{
    /// <summary>
    /// Plugs the capstone's own OCR engine (CROMS.OwnOcr) in beside Tesseract as a SECOND OPINION.
    /// <para/>
    /// Tesseract stays the engine that reads every page. After it has finished, the fields whose
    /// answer comes from a small closed list (province, municipality, citizenship, religion,
    /// civil status) are re-read by the own engine from the same crop of the same page, in
    /// parallel, and the own reading replaces Tesseract's only under the strict rule in
    /// <see cref="OwnSecondOpinion.ShouldReplace"/>: it must be an exact list entry while the
    /// current value is not one. A value Tesseract already got onto the list is never overruled,
    /// and personal names are never touched. Every replaced value is flagged for the operator
    /// (confidence below the "uncertain" line, shown as auto-corrected, original kept).
    /// <para/>
    /// Fail-safe by construction: if the data files are missing, the engine is still loading, a
    /// crop cannot be read or the time budget runs out, the field simply keeps Tesseract's value
    /// and nothing else changes. Measured results: CLAUDE.md, 2026-10-07.
    /// </summary>
    public static class OwnOcrHybrid
    {
        /// <summary>Master switch. Off = the pipeline behaves exactly as before this feature existed.</summary>
        public static bool Enabled = true;

        /// <summary>
        /// Wall-clock allowance for the whole second-opinion step, in milliseconds. The page is
        /// already read by the time this runs; this only bounds how long we wait for extra crops,
        /// so the 20-second scan target is never put at risk by the own engine.
        /// </summary>
        public static int BudgetMs = 4000;

        private static OwnSecondOpinion _engine;
        private static string _error = "";
        private static int _loadStarted;
        private static readonly ManualResetEventSlim _loaded = new ManualResetEventSlim(false);

        /// <summary>The folder next to CROMS.exe that holds refs.bin and lexicon\*.txt.</summary>
        public static string DataDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OwnOcr"); }
        }

        public static bool Ready { get { return _engine != null; } }

        /// <summary>Why the engine is not available (empty while loading or when it loaded fine).</summary>
        public static string LoadError { get { return _error; } }

        /// <summary>
        /// Start loading the reference glyphs on a background thread (a few seconds, ~40 MB). Safe
        /// to call more than once. Scans started before it finishes simply skip the second opinion.
        /// </summary>
        public static void StartLoading()
        {
            if (Interlocked.Exchange(ref _loadStarted, 1) == 1) return;
            var t = new Thread(() =>
            {
                try { _engine = OwnSecondOpinion.Load(DataDir); }
                catch (Exception ex) { _error = ex.GetType().Name + ": " + ex.Message; _engine = null; }
                finally { _loaded.Set(); }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.BelowNormal;
            t.Name = "OwnOcr loader";
            t.Start();
        }

        /// <summary>Block until loading has finished (test harnesses only; the app never waits).</summary>
        public static bool WaitReady(int timeoutMs)
        {
            StartLoading();
            return _loaded.Wait(timeoutMs) && _engine != null;
        }

        /// <summary>
        /// Run the second opinion over <paramref name="r"/> using the upright page the fields'
        /// regions were measured on. Returns how many values were replaced; the caller
        /// re-validates the result when that is above zero. Never throws.
        /// </summary>
        public static int Apply(Bitmap page, DocAiResult r, Action<string> diag)
        {
            if (!Enabled || page == null || r == null || r.Fields == null) return 0;
            OwnSecondOpinion eng = _engine;
            if (eng == null)
            {
                if (diag != null) { try { diag("own second opinion SKIPPED: " + (_error.Length > 0 ? _error : "engine still loading")); } catch { } }
                return 0;
            }

            var sw = Stopwatch.StartNew();
            var jobs = new List<Job>();
            try
            {
                // Crops are taken here, on the calling thread, one after another: a GDI+ Bitmap
                // is not safe to read from several threads at once (the BREQS suite caught that
                // on 2026-10-05). Only the reading itself runs in parallel, each on its own crop.
                foreach (DocField f in r.Fields)
                {
                    if (!f.FromRegion || f.RegionNorm.IsEmpty) continue;
                    if (string.IsNullOrWhiteSpace(f.Value)) continue;
                    if (!eng.IsClosedListField(f.Key)) continue;
                    Rectangle px = Pad(f.RegionNorm, page.Width, page.Height);
                    if (px.Width < 8 || px.Height < 8) continue;
                    jobs.Add(new Job { Field = f, Crop = Crop(page, px) });
                }
                if (jobs.Count == 0) return 0;

                using (var cts = new CancellationTokenSource(BudgetMs))
                {
                    var opts = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 1),
                        CancellationToken = cts.Token
                    };
                    try
                    {
                        Parallel.ForEach(jobs, opts, j =>
                        {
                            try { j.Opinion = eng.Read(j.Crop, j.Field.Key); }
                            catch { j.Opinion = null; }
                        });
                    }
                    catch (OperationCanceledException) { /* budget spent: keep whatever finished */ }
                }

                int replaced = 0;
                foreach (Job j in jobs)
                {
                    if (j.Opinion == null) continue;
                    DocField f = j.Field;
                    if (!eng.ShouldReplace(f.Key, f.Value, j.Opinion)) continue;

                    string before = f.Value;
                    f.Value = j.Opinion.Text;
                    f.Corrected = true;
                    // A second engine agreeing with a list entry is a repair, not a clean read of
                    // the box: keep it under the "uncertain" line so the operator confirms it.
                    f.Confidence = Math.Min(f.Confidence, DocIntelligence.UncertainBelow - 1);
                    replaced++;
                    if (diag != null) { try { diag("own second opinion REPLACED " + f.Key + ": '" + before + "' -> '" + f.Value + "'"); } catch { } }
                }

                if (diag != null)
                {
                    try
                    {
                        int done = jobs.Count(j => j.Opinion != null);
                        diag("[" + sw.Elapsed.TotalSeconds.ToString("0.0") + "s] own second opinion: " + done + "/" + jobs.Count
                            + " closed-list fields read, " + replaced + " replaced");
                    }
                    catch { }
                }
                return replaced;
            }
            catch (Exception ex)
            {
                if (diag != null) { try { diag("own second opinion FAILED: " + ex.GetType().Name + ": " + ex.Message); } catch { } }
                return 0;
            }
            finally
            {
                foreach (Job j in jobs) { try { j.Crop.Dispose(); } catch { } }
            }
        }

        private sealed class Job
        {
            public DocField Field;
            public Bitmap Crop;
            public OwnSecondOpinion.Opinion Opinion;
        }

        // The same padding the benchmark crops were cut with (CROMS.DocTest CropDump): a little air
        // around the box, because a line needs vertical room to be read at all.
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

        private static Bitmap Crop(Bitmap src, Rectangle rect)
        {
            var dst = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(src, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }
            return dst;
        }
    }
}
