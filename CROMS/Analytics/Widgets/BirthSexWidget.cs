using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Share of male and female births per year, as a 100% stacked column.
    ///
    /// Counted by DATE OF BIRTH, not registration: this is a statement about the population
    /// born in a year, and a delayed registration would otherwise move a 2019 birth into the
    /// 2026 bar. The sex ratio at birth (males per 100 females) is stated in the caption
    /// because that is the figure PSA tabulations quote, and it is not readable off a
    /// percentage column.
    /// </summary>
    public class BirthSexWidget : AnalyticsWidget
    {
        public BirthSexWidget()
            : base("Sex distribution by year", "by date of birth (date_of_birth)")
        {
        }

        protected override void Render(DateRange range)
        {
            // Coverage first: a chart drawn over a column nobody fills is worse than no chart.
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("births", "sex", "date_of_birth", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No births with a date of birth in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Sex is captured on Municipal Form 102 item 3.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT YEAR(date_of_birth) AS y, sex, COUNT(*) AS n " +
                "FROM births " +
                "WHERE date_of_birth IS NOT NULL AND date_of_birth >= @from AND date_of_birth < @to " +
                "  AND sex IS NOT NULL AND TRIM(sex) <> '' " +
                "GROUP BY y, sex ORDER BY y", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No births with a recorded sex in this period.", null);
                return;
            }

            // Pivot year -> (male, female). Any value that is neither is counted separately
            // rather than dropped, so the columns always add up to the record count.
            var years = new List<int>();
            var male = new Dictionary<int, int>();
            var female = new Dictionary<int, int>();
            var other = new Dictionary<int, int>();
            int totalM = 0, totalF = 0, totalO = 0;

            foreach (DataRow r in dt.Rows)
            {
                int y = AnalyticsData.Int(r, "y");
                int n = AnalyticsData.Int(r, "n");
                string sex = AnalyticsData.Str(r, "sex");

                if (!years.Contains(y)) { years.Add(y); male[y] = 0; female[y] = 0; other[y] = 0; }

                if (string.Equals(sex, "Male", StringComparison.OrdinalIgnoreCase))
                { male[y] += n; totalM += n; }
                else if (string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase))
                { female[y] += n; totalF += n; }
                else
                { other[y] += n; totalO += n; }
            }
            years.Sort();

            ResetChart();
            Series sM = ChartStyle.StackedColumn("Male", 0, true);
            Series sF = ChartStyle.StackedColumn("Female", 1, true);
            Series sO = ChartStyle.StackedColumn("Other / not standard", 6, true);

            foreach (int y in years)
            {
                string label = y.ToString();
                sM.Points.AddXY(label, male[y]);
                sF.Points.AddXY(label, female[y]);
                sO.Points.AddXY(label, other[y]);
            }

            Chart.Series.Add(sM);
            Chart.Series.Add(sF);
            if (totalO > 0) Chart.Series.Add(sO);      // only shown when it actually occurs

            ChartStyle.Axes(Chart, "Year of birth", "Share of births");
            ChartStyle.PercentY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(totalM, totalF, totalO, years.Count, cov));
        }

        /// <summary>Deterministic string.Format over the same totals the columns were built from.</summary>
        private string BuildCaption(int males, int females, int other, int yearCount,
                                    AnalyticsData.Coverage cov)
        {
            int counted = males + females + other;

            if (counted < 3)
            {
                // Under three records a ratio is arithmetic, not a finding.
                return string.Format("{0} birth{1} with a recorded sex — {2} male, {3} female. Too few to state a ratio.",
                    counted, counted == 1 ? "" : "s", males, females);
            }

            string ratio;
            if (females == 0)
                ratio = "every recorded birth is male, so no sex ratio can be stated";
            else if (males == 0)
                ratio = "every recorded birth is female, so no sex ratio can be stated";
            else
                ratio = string.Format("a sex ratio of {0:0} males per 100 females",
                    (double)males / females * 100.0);

            string span = yearCount == 1
                ? "in a single year of data"
                : string.Format("across {0} years", yearCount);

            string note = cov.Partial ? " " + cov.PartialNote : "";

            return string.Format("{0} male ({1:0.#}%) and {2} female ({3:0.#}%) {4} — {5}.{6}",
                males, AnalyticsData.Pct(males, counted),
                females, AnalyticsData.Pct(females, counted),
                span, ratio, note);
        }
    }
}
