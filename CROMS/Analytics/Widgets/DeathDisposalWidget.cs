using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How the remains were disposed of — burial, cremation and the rest — as a 100% stacked
    /// column per year. It is the figure a burial-permit workload is built from, and the one
    /// the sanitation office asks for.
    ///
    /// On the office's current data this widget is EXPECTED to show its empty state:
    /// disposal_method is unfilled in every death record. That is the useful output — it names
    /// the column and the count, so the office knows what to start capturing.
    /// </summary>
    public class DeathDisposalWidget : AnalyticsWidget
    {
        // The methods Municipal Form 103 prints, in its order. Anything else recorded is kept
        // under "Other (as recorded)" rather than dropped — a value nobody can see cannot be
        // corrected.
        private static readonly string[] Standard = { "Burial", "Cremation", "Entombment" };

        public DeathDisposalWidget()
            : base("Disposal method", "by date of death (date_of_death) · MF-103 corpse disposal")
        {
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("deaths", "disposal_method", "date_of_death", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No deaths with a date of death in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Corpse disposal is captured on Municipal Form 103 with the burial permit.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT YEAR(date_of_death) AS y, TRIM(disposal_method) AS method, COUNT(*) AS n " +
                "FROM deaths " +
                "WHERE date_of_death IS NOT NULL AND date_of_death >= @from AND date_of_death < @to " +
                "  AND disposal_method IS NOT NULL AND TRIM(disposal_method) <> '' " +
                "GROUP BY y, method ORDER BY y", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No deaths with a disposal method recorded in this period.", null);
                return;
            }

            // Pivot year -> method counts, folding recorded values onto the printed categories.
            var years = new List<int>();
            var cells = new Dictionary<int, Dictionary<string, int>>();
            var totals = new Dictionary<string, int>();
            var categories = new List<string>(Standard);
            var nonStandard = new List<string>();
            int total = 0;

            foreach (string s in Standard) totals[s] = 0;

            foreach (DataRow r in dt.Rows)
            {
                int y = AnalyticsData.Int(r, "y");
                int n = AnalyticsData.Int(r, "n");
                string raw = AnalyticsData.Str(r, "method");
                string bucket = Match(raw);
                if (bucket == null)
                {
                    bucket = "Other (as recorded)";
                    if (!categories.Contains(bucket)) categories.Add(bucket);
                    if (!totals.ContainsKey(bucket)) totals[bucket] = 0;
                    if (!nonStandard.Contains(raw)) nonStandard.Add(raw);
                }

                if (!cells.ContainsKey(y)) { cells[y] = new Dictionary<string, int>(); years.Add(y); }
                if (!cells[y].ContainsKey(bucket)) cells[y][bucket] = 0;
                cells[y][bucket] += n;
                totals[bucket] += n;
                total += n;
            }
            years.Sort();

            // Drop printed categories nobody used, so the legend does not list three empty series.
            var shown = new List<string>();
            foreach (string c in categories) if (totals[c] > 0) shown.Add(c);

            ResetChart();
            for (int i = 0; i < shown.Count; i++)
            {
                Series s = ChartStyle.StackedColumn(shown[i], i, true);
                foreach (int y in years)
                {
                    int n;
                    if (!cells[y].TryGetValue(shown[i], out n)) n = 0;
                    s.Points.AddXY(y.ToString(), n);
                }
                Chart.Series.Add(s);
            }

            ChartStyle.Axes(Chart, "Year of death", "Share of deaths");
            ChartStyle.PercentY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(shown, totals, total, cov, nonStandard));
        }

        /// <summary>Maps a recorded value onto a printed category, or null when it matches none.</summary>
        private static string Match(string raw)
        {
            foreach (string s in Standard)
                if (raw.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return s;
            if (raw.IndexOf("crema", StringComparison.OrdinalIgnoreCase) >= 0) return "Cremation";
            if (raw.IndexOf("buri", StringComparison.OrdinalIgnoreCase) >= 0) return "Burial";
            if (raw.IndexOf("tomb", StringComparison.OrdinalIgnoreCase) >= 0) return "Entombment";
            return null;
        }

        /// <summary>Deterministic string.Format over the same totals the columns were built from.</summary>
        private string BuildCaption(List<string> shown, Dictionary<string, int> totals,
                                    int total, AnalyticsData.Coverage cov, List<string> nonStandard)
        {
            string coverage = cov.Partial ? " " + cov.PartialNote : "";
            string oddities = nonStandard.Count == 0
                ? ""
                : " Values not on the form's list were counted under Other: " +
                  string.Join(", ", nonStandard.ToArray()) + ".";

            if (total < 3)
            {
                var parts = new List<string>();
                foreach (string c in shown) parts.Add(totals[c] + " " + c.ToLowerInvariant());
                return string.Format("{0} death{1} with a disposal method recorded ({2}). Too few to state a split.{3}{4}",
                    total, total == 1 ? "" : "s", string.Join(", ", parts.ToArray()), coverage, oddities);
            }

            string lead = "";
            int best = 0;
            foreach (string c in shown) if (totals[c] > best) { best = totals[c]; lead = c; }

            return string.Format("{0} is the usual disposal — {1} of {2} ({3:0.#}%).{4}{5}",
                lead, best, total, AnalyticsData.Pct(best, total), coverage, oddities);
        }
    }
}
