using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How long after the birth the registration actually happened, bucketed 0–30 days /
    /// 31 days–1 year / 1–5 years / 5 years+, stacked by the year the office registered it.
    ///
    /// The 30-day boundary is the reglementary period under RA 3753, which is what
    /// <c>births.is_delayed</c> is meant to record. The buckets here are computed from
    /// DATEDIFF(created_at, date_of_birth) rather than read off that flag, and the caption
    /// says so — if the two disagree, the flag is the thing that needs looking at, and
    /// hiding the disagreement behind the flag would keep it invisible.
    /// </summary>
    public class BirthLagWidget : AnalyticsWidget
    {
        private static readonly string[] BucketNames =
        {
            "0–30 days", "31 days–1 year", "1–5 years", "5 years+"
        };

        public BirthLagWidget()
            : base("Delayed registration lag",
                   "stacked by year registered (created_at); lag = created_at − date_of_birth")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT YEAR(created_at) AS y, " +
                "       CASE WHEN DATEDIFF(created_at, date_of_birth) <= 30   THEN 0 " +
                "            WHEN DATEDIFF(created_at, date_of_birth) <= 365  THEN 1 " +
                "            WHEN DATEDIFF(created_at, date_of_birth) <= 1825 THEN 2 " +
                "            ELSE 3 END AS bucket, " +
                "       COUNT(*) AS n, " +
                "       SUM(CASE WHEN is_delayed = 1 THEN 1 ELSE 0 END) AS flagged, " +
                "       SUM(CASE WHEN DATEDIFF(created_at, date_of_birth) < 0 THEN 1 ELSE 0 END) AS impossible " +
                "FROM births " +
                "WHERE date_of_birth IS NOT NULL " +
                "  AND created_at >= @from AND created_at < @to " +
                "GROUP BY y, bucket ORDER BY y, bucket", range);

            if (dt.Rows.Count == 0)
            {
                // Distinguish "no births" from "births with no date of birth to measure against".
                int withoutDob = AnalyticsData.ScalarInRange(
                    "SELECT COUNT(*) FROM births " +
                    "WHERE date_of_birth IS NULL AND created_at >= @from AND created_at < @to", range);

                if (withoutDob > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — date_of_birth is unfilled in " + withoutDob + " of " + withoutDob + " records.",
                        "The lag cannot be measured without a date of birth to measure from.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No births registered in this period.", null);
                }
                return;
            }

            // Pivot year -> bucket counts.
            var years = new List<int>();
            var cells = new Dictionary<int, int[]>();
            var totals = new int[4];
            int flagged = 0, impossible = 0, all = 0;

            foreach (DataRow r in dt.Rows)
            {
                int y = AnalyticsData.Int(r, "y");
                int b = AnalyticsData.Int(r, "bucket");
                int n = AnalyticsData.Int(r, "n");
                if (b < 0 || b > 3) continue;

                if (!cells.ContainsKey(y)) { cells[y] = new int[4]; years.Add(y); }
                cells[y][b] += n;
                totals[b] += n;
                all += n;
                flagged += AnalyticsData.Int(r, "flagged");
                impossible += AnalyticsData.Int(r, "impossible");
            }
            years.Sort();

            ResetChart();
            for (int b = 0; b < 4; b++)
            {
                Series s = ChartStyle.StackedColumn(BucketNames[b], b, false);
                foreach (int y in years) s.Points.AddXY(y.ToString(), cells[y][b]);
                Chart.Series.Add(s);
            }

            ChartStyle.Axes(Chart, "Year registered", "Births");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(totals, all, flagged, impossible));
        }

        /// <summary>Deterministic string.Format over the bucket totals the columns were built from.</summary>
        private string BuildCaption(int[] totals, int all, int flagged, int impossible)
        {
            int late = totals[1] + totals[2] + totals[3];

            string note = "";
            if (impossible > 0)
            {
                // A negative lag means the record says the child was registered before being
                // born. Those land in the first bucket; the count is disclosed rather than
                // quietly absorbed.
                note += string.Format(" {0} record{1} show a registration date EARLIER than the date of birth and need checking.",
                    impossible, impossible == 1 ? "" : "s");
            }
            if (flagged != late)
            {
                note += string.Format(" The is_delayed flag is set on {0} record{1} while {2} are actually past 30 days — the flag and the dates disagree.",
                    flagged, flagged == 1 ? "" : "s", late);
            }

            if (all < 3)
            {
                return string.Format("{0} birth{1} with both dates recorded in this period.{2}",
                    all, all == 1 ? "" : "s", note);
            }

            if (late == 0)
            {
                return string.Format("All {0} births were registered within the 30-day reglementary period — no delayed-registration backlog in this range.{1}",
                    all, note);
            }

            return string.Format("{0} of {1} births ({2:0.#}%) were registered late: {3} within a year, {4} within five years, {5} after five years.{6}",
                late, all, AnalyticsData.Pct(late, all), totals[1], totals[2], totals[3], note);
        }
    }
}
