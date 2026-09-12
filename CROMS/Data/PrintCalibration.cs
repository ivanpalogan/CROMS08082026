using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Where a form's print map actually lands on THIS office's paper and printer.
    /// <para/>
    /// A form that has no scan of its blank sheet is printed onto the official pre-printed
    /// stock, and its positions come from the OCR layout — which is calibrated against a
    /// reference scan, not against the sheet in the tray. Those two framings differ: the
    /// reference scan includes the outer border and footer that the printable area does not,
    /// so every value is displaced by the SAME scale and offset. Measured on the MF-103
    /// sample that displacement was a clean 1.33x with almost no offset — one transform for
    /// all eighteen fields, which is precisely why a single per-form correction fixes the
    /// whole page rather than needing every field nudged.
    /// <para/>
    /// Printers add their own share of it: two models rarely agree on where the top of the
    /// page is. So this is stored per FORM and per MACHINE, alongside the server address,
    /// and never in the database — it describes the hardware in this room, not the registry.
    /// </summary>
    public class PrintAlign
    {
        public float ScaleX = 1f, ScaleY = 1f;   // multiplies the 0-1 position
        public float OffsetX = 0f, OffsetY = 0f; // then shifts it, in points

        public bool IsIdentity =>
            ScaleX == 1f && ScaleY == 1f && OffsetX == 0f && OffsetY == 0f;

        public PrintAlign Clone() => new PrintAlign
        { ScaleX = ScaleX, ScaleY = ScaleY, OffsetX = OffsetX, OffsetY = OffsetY };

        /// <summary>Maps a print-map position (0-1 of the page) to a point on the sheet.</summary>
        public PointF Apply(PointF at, float pageW, float pageH)
        {
            return new PointF(at.X * ScaleX * pageW + OffsetX,
                              at.Y * ScaleY * pageH + OffsetY);
        }
    }

    public static class PrintCalibration
    {
        private static readonly object Gate = new object();
        private static Dictionary<string, PrintAlign> _cache;

        private static string FilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CROMS", "print-align.cfg");

        /// <summary>
        /// This form's correction, or an identity one when it has never been calibrated.
        /// Never returns null and never throws: an unreadable settings file must not stop
        /// somebody printing a certificate.
        /// </summary>
        public static PrintAlign For(string formCode)
        {
            if (string.IsNullOrEmpty(formCode)) return new PrintAlign();
            lock (Gate)
            {
                Load();
                PrintAlign a;
                return _cache.TryGetValue(formCode, out a) ? a.Clone() : new PrintAlign();
            }
        }

        /// <summary>True once the office has actually aligned this form on this machine.</summary>
        public static bool IsCalibrated(string formCode)
        {
            if (string.IsNullOrEmpty(formCode)) return false;
            lock (Gate)
            {
                Load();
                return _cache.ContainsKey(formCode);
            }
        }

        public static void Save(string formCode, PrintAlign align)
        {
            if (string.IsNullOrEmpty(formCode) || align == null) return;
            lock (Gate)
            {
                Load();
                _cache[formCode] = align.Clone();
                Write();
            }
        }

        public static void Clear(string formCode)
        {
            if (string.IsNullOrEmpty(formCode)) return;
            lock (Gate)
            {
                Load();
                if (_cache.Remove(formCode)) Write();
            }
        }

        // ---- storage: one line per form, deliberately hand-editable ----------

        private static void Load()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, PrintAlign>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;

                    string[] p = t.Split('|');
                    if (p.Length != 5) continue;

                    var a = new PrintAlign();
                    if (!Num(p[1], out a.ScaleX)) continue;
                    if (!Num(p[2], out a.ScaleY)) continue;
                    if (!Num(p[3], out a.OffsetX)) continue;
                    if (!Num(p[4], out a.OffsetY)) continue;

                    // A zero or negative scale would collapse the whole page onto one point.
                    if (a.ScaleX <= 0f || a.ScaleY <= 0f) continue;
                    _cache[p[0].Trim()] = a;
                }
            }
            catch { /* unreadable settings: every form falls back to identity */ }
        }

        private static bool Num(string s, out float v) =>
            float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        private static void Write()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>
                {
                    "# CROMS print alignment for pre-printed forms.",
                    "# formCode | scaleX | scaleY | offsetX(pt) | offsetY(pt)",
                    "# Per machine: it describes this printer and this paper, not the registry.",
                };
                foreach (var kv in _cache)
                    lines.Add(string.Join(" | ", kv.Key,
                        kv.Value.ScaleX.ToString("0.####", CultureInfo.InvariantCulture),
                        kv.Value.ScaleY.ToString("0.####", CultureInfo.InvariantCulture),
                        kv.Value.OffsetX.ToString("0.##", CultureInfo.InvariantCulture),
                        kv.Value.OffsetY.ToString("0.##", CultureInfo.InvariantCulture)));

                File.WriteAllLines(FilePath, lines);
            }
            catch { /* read-only profile: the correction just does not persist */ }
        }
    }
}
