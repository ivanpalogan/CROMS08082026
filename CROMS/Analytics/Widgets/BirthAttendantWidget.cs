using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Who attended the birth — the standard PSA tabulation (Physician, Nurse, Midwife,
    /// Hilot, Others), taken from Municipal Form 102 item 20. It is the closest thing the
    /// registry holds to a facility-versus-home delivery split, which is why PSA asks for it.
    ///
    /// On the office's current data this widget is EXPECTED to show its empty state:
    /// attendant_type is unfilled in almost every row. That is the useful output — it names
    /// the column and the count, so the office knows exactly what to start capturing.
    /// </summary>
    public class BirthAttendantWidget : AnalyticsWidget
    {
        // The five PSA categories, in the order the form prints them. Anything else a scan or
        // an operator produced is kept under "Others (as recorded)" rather than discarded —
        // a value nobody can see cannot be corrected.
        private static readonly string[] Standard =
        {
            "Physician", "Nurse", "Midwife", "Hilot", "Others"
        };

        // Attendants that make the delivery a professionally-attended one.
        private static readonly string[] Professional = { "Physician", "Nurse", "Midwife" };

        public BirthAttendantWidget()
            : base("Attendant at birth", "by date of birth (date_of_birth) · MF-102 item 20")
        {
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("births", "attendant_type", "date_of_birth", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No births with a date of birth in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Attendant at birth is Municipal Form 102 item 20 — capture it on registration to fill this chart.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT TRIM(attendant_type) AS attendant, COUNT(*) AS n " +
                "FROM births " +
                "WHERE date_of_birth IS NOT NULL AND date_of_birth >= @from AND date_of_birth < @to " +
                "  AND attendant_type IS NOT NULL AND TRIM(attendant_type) <> '' " +
                "GROUP BY attendant ORDER BY n DESC, attendant", range);

            // Fold each recorded value onto its PSA category; unmatched values stay visible.
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string s in Standard) counts[s] = 0;
            int professional = 0, total = 0;
            var nonStandard = new List<string>();

            foreach (DataRow r in dt.Rows)
            {
                string raw = AnalyticsData.Str(r, "attendant");
                int n = AnalyticsData.Int(r, "n");
                total += n;

                string bucket = Match(raw);
                if (bucket == null)
                {
                    bucket = "Others";
                    if (!nonStandard.Contains(raw)) nonStandard.Add(raw);
                }
                counts[bucket] += n;
                if (Array.IndexOf(Professional, bucket) >= 0) professional += n;
            }

            ResetChart();
            Series s2 = ChartStyle.Column("Births", 0);
            for (int i = 0; i < Standard.Length; i++)
            {
                s2.Points.AddXY(Standard[i], counts[Standard[i]]);
                // Non-professional attendants get their own tint so the facility/home split is
                // visible on the chart itself, not only in the caption.
                if (Array.IndexOf(Professional, Standard[i]) < 0)
                    ChartStyle.Highlight(s2, s2.Points.Count - 1, ChartStyle.Colour(1));
            }
            Chart.Series.Add(s2);

            ChartStyle.Axes(Chart, "Attendant", "Births");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(counts, professional, total, cov, nonStandard));
        }

        /// <summary>Maps a recorded value onto a PSA category, or null when it matches none.</summary>
        private static string Match(string raw)
        {
            foreach (string s in Standard)
                if (raw.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return s;

            // Common variants the office and the OCR both produce.
            if (raw.IndexOf("doctor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("md", StringComparison.OrdinalIgnoreCase) == 0) return "Physician";
            if (raw.IndexOf("traditional", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("tba", StringComparison.OrdinalIgnoreCase) >= 0) return "Hilot";
            return null;
        }

        /// <summary>Deterministic string.Format over the same counts the columns were built from.</summary>
        private string BuildCaption(Dictionary<string, int> counts, int professional, int total,
                                    AnalyticsData.Coverage cov, List<string> nonStandard)
        {
            string oddities = nonStandard.Count == 0
                ? ""
                : " Values not on the PSA list were counted under Others: " +
                  string.Join(", ", nonStandard.ToArray()) + ".";

            string coverage = cov.Partial ? " " + cov.PartialNote : "";

            if (total < 3)
            {
                // Under three records the percentages would be 33/67 arithmetic, not a finding.
                var parts = new List<string>();
                foreach (string s in Standard)
                    if (counts[s] > 0) parts.Add(counts[s] + " " + s.ToLowerInvariant());
                return string.Format("{0} birth{1} with an attendant recorded ({2}). Too few to state a split.{3}{4}",
                    total, total == 1 ? "" : "s",
                    parts.Count == 0 ? "none categorised" : string.Join(", ", parts.ToArray()),
                    coverage, oddities);
            }

            int home = total - professional;
            string lead = "";
            int best = 0;
            foreach (string s in Standard)
                if (counts[s] > best) { best = counts[s]; lead = s; }

            return string.Format(
                "{0} of {1} births ({2:0.#}%) were attended by a health professional; {3} ({4:0.#}%) were not. Most common attendant: {5} with {6}.{7}{8}",
                professional, total, AnalyticsData.Pct(professional, total),
                home, AnalyticsData.Pct(home, total),
                lead, best, coverage, oddities);
        }
    }
}
