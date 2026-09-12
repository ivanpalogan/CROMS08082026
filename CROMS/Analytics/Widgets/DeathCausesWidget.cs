using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CROMS.Modules;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// The ten most-recorded causes of death, as a horizontal bar chart, with a toggle that
    /// restricts the same chart to children under five (the standard under-5 mortality cut).
    ///
    /// THE RANKING IS NOT RELIABLE AND THE CAPTION SAYS SO. `deaths.immediate_cause` is a free
    /// text field, so "cardiopulmonary arrest", "Cardiopulmonary Arrest" and "CP arrest" are
    /// three different causes to a GROUP BY; the lookup table behind `cause_of_death_id` is no
    /// better on this data (three rows, one of them blank). Nothing here silently merges them —
    /// guessing that two free-typed strings mean the same clinical cause is not something a
    /// registry system should do behind the operator's back. The chart shows what was recorded
    /// and the caption states the limitation.
    /// </summary>
    public class DeathCausesWidget : AnalyticsWidget
    {
        private const int TopN = 10;
        private const int UnderFive = 5;

        private readonly CheckBox _underFive;

        public DeathCausesWidget()
            : base("Leading causes of death", "by date of death (date_of_death) · top 10 recorded")
        {
            _underFive = new CheckBox
            {
                Text = "Under 5 only",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = UiTheme.Muted,
                BackColor = UiTheme.Surface
            };
            // Measure so the header can reserve the right width before the control is shown.
            _underFive.Size = new Size(96, 18);
            // Range is only meaningful once the host has loaded the widget at least once;
            // a toggle before that would query the year 0001.
            _underFive.CheckedChanged += (s, e) => { if (Range.To > DateTime.MinValue) Load(Range); };
            SetHeaderControl(_underFive);
        }

        protected override void Render(DateRange range)
        {
            bool childrenOnly = _underFive.Checked;

            // The age filter is applied as a VALUE parameter, switched by a second parameter,
            // so the same statement serves both cuts and nothing is concatenated in.
            string ageFilter = childrenOnly ? " AND d.age IS NOT NULL AND d.age < @childAge " : "";

            DataTable dt = AnalyticsData.Query(
                "SELECT COALESCE(NULLIF(TRIM(d.immediate_cause), ''), " +
                "                NULLIF(TRIM(c.name), '')) AS cause, COUNT(*) AS n " +
                "FROM deaths d " +
                "LEFT JOIN causes_of_death c ON c.id = d.cause_of_death_id " +
                "WHERE d.date_of_death IS NOT NULL AND d.date_of_death >= @from AND d.date_of_death < @to " +
                ageFilter +
                "  AND COALESCE(NULLIF(TRIM(d.immediate_cause), ''), NULLIF(TRIM(c.name), '')) IS NOT NULL " +
                "GROUP BY cause ORDER BY n DESC, cause LIMIT " + TopN, range,
                new MySql.Data.MySqlClient.MySqlParameter("@childAge", UnderFive));

            // How many records in the same cut carry no cause at all — the number that decides
            // whether the ranking above it means anything.
            int noCause = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM deaths d " +
                "LEFT JOIN causes_of_death c ON c.id = d.cause_of_death_id " +
                "WHERE d.date_of_death IS NOT NULL AND d.date_of_death >= @from AND d.date_of_death < @to " +
                ageFilter +
                "  AND COALESCE(NULLIF(TRIM(d.immediate_cause), ''), NULLIF(TRIM(c.name), '')) IS NULL", range,
                new MySql.Data.MySqlClient.MySqlParameter("@childAge", UnderFive));

            if (dt.Rows.Count == 0)
            {
                string who = childrenOnly ? "deaths of children under 5" : "deaths";
                if (noCause > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — immediate_cause is unfilled in " + noCause + " of " + noCause + " records.",
                        "Neither the free-text cause nor the cause lookup is filled on these records.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows, "No " + who + " in this period.", null);
                }
                return;
            }

            var names = new List<string>();
            var counts = new List<int>();
            int recorded = 0;
            foreach (DataRow r in dt.Rows)
            {
                names.Add(AnalyticsData.Str(r, "cause"));
                int n = AnalyticsData.Int(r, "n");
                counts.Add(n);
                recorded += n;
            }

            ResetChart();
            Series s2 = ChartStyle.Bar("Deaths", childrenOnly ? 4 : 0);

            // A Bar chart draws the FIRST point at the bottom, so the list is added in reverse
            // to put the leading cause at the top where it is read first.
            for (int i = names.Count - 1; i >= 0; i--)
                s2.Points.AddXY(Shorten(names[i]), counts[i]);

            Chart.Series.Add(s2);
            ChartStyle.Axes(Chart, "Cause as recorded", "Deaths");
            ChartArea a = Chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "0";
            if (counts[0] <= 6) { a.AxisY.Interval = 1; a.AxisY.Maximum = Math.Max(1, counts[0]); }
            AutoLegend();

            SetCaption(BuildCaption(names, counts, recorded, noCause, childrenOnly));
        }

        /// <summary>Keeps a long free-typed cause readable on the axis; the full text stays in the data.</summary>
        private static string Shorten(string s)
        {
            if (s.Length <= 34) return s;
            return s.Substring(0, 32).TrimEnd() + "…";
        }

        /// <summary>Deterministic string.Format over the same counts the bars were built from.</summary>
        private string BuildCaption(List<string> names, List<int> counts,
                                    int recorded, int noCause, bool childrenOnly)
        {
            string scope = childrenOnly ? "Under-5 deaths: " : "";
            int total = recorded + noCause;

            string unrecorded = noCause == 0
                ? ""
                : string.Format(" {0} of {1} death{2} in this period record no cause at all.",
                    noCause, total, total == 1 ? "" : "s");

            // The honesty line. It applies at every data volume, so it is never dropped.
            const string caveat =
                " Causes are free-typed, so spellings are not merged and this ranking is indicative only.";

            if (recorded < 3)
            {
                return scope + string.Format("{0} death{1} with a cause recorded, led by \"{2}\".{3}{4}",
                    recorded, recorded == 1 ? "" : "s", names[0], unrecorded, caveat);
            }

            return scope + string.Format(
                "\"{0}\" is the most recorded cause with {1} of {2} ({3:0.#}%); {4} distinct cause{5} appear in the top {6}.{7}{8}",
                names[0], counts[0], recorded, AnalyticsData.Pct(counts[0], recorded),
                names.Count, names.Count == 1 ? "" : "s", TopN, unrecorded, caveat);
        }
    }
}
