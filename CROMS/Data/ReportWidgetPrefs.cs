using System;
using System.Collections.Generic;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Which of a domain tab's on-screen charts also print as extra pages on its Assessment
    /// Report — a per-machine DISPLAY preference, not registry data, so it lives beside
    /// <see cref="PrintCalibration"/> in %APPDATA% rather than in the database.
    /// <para/>
    /// Default is ALL ON: every chart the tab already shows on screen prints unless the
    /// operator explicitly turns it off from "Customize Report" — nothing has to be
    /// re-enabled after this feature ships, and the operator's own choice is what changes
    /// the shipped default, never the other way round.
    /// </summary>
    public static class ReportWidgetPrefs
    {
        private static readonly object Gate = new object();

        // formCode -> set of widget titles explicitly turned OFF for that report.
        private static Dictionary<string, HashSet<string>> _disabled;

        private static string FilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CROMS", "report-widgets.cfg");

        public static bool IsEnabled(string formCode, string title)
        {
            if (string.IsNullOrEmpty(formCode) || string.IsNullOrEmpty(title)) return true;
            lock (Gate)
            {
                Load();
                HashSet<string> off;
                return !(_disabled.TryGetValue(formCode, out off) && off.Contains(title));
            }
        }

        public static void SetEnabled(string formCode, string title, bool enabled)
        {
            if (string.IsNullOrEmpty(formCode) || string.IsNullOrEmpty(title)) return;
            lock (Gate)
            {
                Load();
                HashSet<string> off;
                if (!_disabled.TryGetValue(formCode, out off))
                    _disabled[formCode] = off = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (enabled) off.Remove(title);
                else off.Add(title);

                if (off.Count == 0) _disabled.Remove(formCode);
                Write();
            }
        }

        // ---- storage: one line per disabled chart, deliberately hand-editable ----------

        private static void Load()
        {
            if (_disabled != null) return;
            _disabled = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;

                    string[] p = t.Split('|');
                    if (p.Length != 2) continue;

                    string formCode = p[0].Trim();
                    string title = p[1].Trim();
                    if (formCode.Length == 0 || title.Length == 0) continue;

                    HashSet<string> off;
                    if (!_disabled.TryGetValue(formCode, out off))
                        _disabled[formCode] = off = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    off.Add(title);
                }
            }
            catch { /* unreadable settings: every chart stays enabled */ }
        }

        private static void Write()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>
                {
                    "# CROMS Reports & Analytics -- charts turned OFF the printed Assessment Report.",
                    "# formCode | widget title   (one line per DISABLED chart; absence = enabled)",
                };
                foreach (var kv in _disabled)
                    foreach (string title in kv.Value)
                        lines.Add(kv.Key + " | " + title);

                File.WriteAllLines(FilePath, lines);
            }
            catch { /* read-only profile: the choice just does not persist */ }
        }
    }
}
