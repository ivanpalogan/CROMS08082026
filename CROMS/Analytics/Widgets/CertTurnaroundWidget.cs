using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How long a certificate request takes to reach the client's hands: request to release,
    /// as a distribution rather than a single average.
    ///
    /// A distribution is the right shape here and an average would be misleading. Most requests
    /// are handed over the same day; a handful sit for weeks because the record had to be found
    /// in an old book. One mean over those two populations describes neither, and the office
    /// cannot act on it. The bars show where requests actually land.
    ///
    /// Turnaround is measured from `certificate_requests.created_at` to `releases.released_at`,
    /// joined on the shared `transaction_id` — the release table is per transaction, which is
    /// the spine the whole flow already hangs on. Requests never released are excluded from the
    /// bars, since they have no end date yet, and counted in the caption.
    /// </summary>
    public class CertTurnaroundWidget : AnalyticsWidget
    {
        // Bucket upper bounds in days; the last bucket is everything beyond.
        private static readonly int[] DayBounds = { 0, 1, 3, 7, 30 };
        private static readonly string[] Labels =
        { "Same day", "1 day", "2–3 days", "4–7 days", "8–30 days", "Over 30 days" };

        public CertTurnaroundWidget()
            : base("Request to release turnaround", "certificate_requests.created_at to releases.released_at")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATEDIFF(r.released_at, cr.created_at) AS days " +
                "FROM certificate_requests cr " +
                "JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE cr.created_at >= @from AND cr.created_at < @to " +
                "  AND r.released_at IS NOT NULL " +
                "  AND DATEDIFF(r.released_at, cr.created_at) >= 0", range);

            int notReleased = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE cr.created_at >= @from AND cr.created_at < @to AND r.id IS NULL", range);

            if (dt.Rows.Count == 0)
            {
                if (notReleased > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "None of the " + notReleased + " request" + (notReleased == 1 ? "" : "s") +
                        " in this period has been released yet.",
                        "Turnaround can only be measured once a request has been handed over.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No certificate requests in this period.", null);
                }
                return;
            }

            var counts = new int[Labels.Length];
            int measured = 0, worst = 0;
            double sum = 0;

            foreach (DataRow r in dt.Rows)
            {
                int days = AnalyticsData.Int(r, "days");
                counts[Bucket(days)]++;
                measured++;
                sum += days;
                if (days > worst) worst = days;
            }

            ResetChart();
            Series s = ChartStyle.Column("Requests", 0);
            for (int i = 0; i < Labels.Length; i++)
            {
                s.Points.AddXY(Labels[i], counts[i]);
                // Tints match the caption's own threshold: amber is "getting slow", red is
                // past a week, which is what the sentence underneath calls out.
                if (i >= 4) ChartStyle.Highlight(s, s.Points.Count - 1, ChartStyle.Colour(4));
                else if (i == 3) ChartStyle.Highlight(s, s.Points.Count - 1, ChartStyle.Colour(1));
            }
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Days from request to release", "Requests");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(counts, measured, sum, worst, notReleased));
        }

        private static int Bucket(int days)
        {
            for (int i = 0; i < DayBounds.Length; i++)
                if (days <= DayBounds[i]) return i;
            return Labels.Length - 1;
        }

        /// <summary>Deterministic string.Format over the same bucket counts the bars were built from.</summary>
        private string BuildCaption(int[] counts, int measured, double sum, int worst, int notReleased)
        {
            string pending = notReleased == 0
                ? ""
                : string.Format(" {0} request{1} from this period are still unreleased and are not in these bars.",
                    notReleased, notReleased == 1 ? "" : "s");

            if (measured < 3)
            {
                return string.Format("{0} released request{1}, longest {2} day{3}. Too few to describe a turnaround.{4}",
                    measured, measured == 1 ? "" : "s", worst, worst == 1 ? "" : "s", pending);
            }

            int sameDay = counts[0];
            int overWeek = counts[4] + counts[5];

            string slow = overWeek == 0
                ? "none took more than a week"
                : string.Format("{0} ({1:0.#}%) took more than a week", overWeek, AnalyticsData.Pct(overWeek, measured));

            return string.Format(
                "{0} of {1} requests ({2:0.#}%) are released the same day and {3}; the mean is {4:0.#} days and the longest was {5}.{6}",
                sameDay, measured, AnalyticsData.Pct(sameDay, measured), slow,
                sum / measured, worst, pending);
        }
    }
}
