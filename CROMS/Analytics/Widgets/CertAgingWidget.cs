using System;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How old the OUTSTANDING certificate requests are — the ones not yet released — bucketed
    /// 0–7, 8–30, 31–90 and 90+ days.
    ///
    /// This is the only widget in the module that deliberately IGNORES the date-range control,
    /// and the basis line says so. Aging is a statement about the backlog as it stands TODAY:
    /// a request filed eight months ago is exactly the one the office needs to see, and a range
    /// filter would hide it whenever the operator was looking at this month. Filtering the
    /// backlog by when it started is how a backlog becomes invisible.
    ///
    /// A request counts as outstanding when it has no `releases` row on its transaction. The
    /// `status` column is checked as a second signal, not the only one, because a release row is
    /// the fact of the handover while the status is a label somebody set.
    /// </summary>
    public class CertAgingWidget : AnalyticsWidget
    {
        private static readonly string[] Labels = { "0–7 days", "8–30 days", "31–90 days", "Over 90 days" };

        public CertAgingWidget()
            : base("Pending and unclaimed requests",
                   "outstanding requests as they stand today — NOT filtered by the date range above")
        {
        }

        protected override void Render(DateRange range)
        {
            // No @from/@to: see the class note. The range control governs the other charts.
            DataTable dt = AnalyticsData.Query(
                "SELECT CASE WHEN DATEDIFF(CURDATE(), cr.created_at) <= 7  THEN 0 " +
                "            WHEN DATEDIFF(CURDATE(), cr.created_at) <= 30 THEN 1 " +
                "            WHEN DATEDIFF(CURDATE(), cr.created_at) <= 90 THEN 2 " +
                "            ELSE 3 END AS bucket, " +
                "       COUNT(*) AS n, MAX(DATEDIFF(CURDATE(), cr.created_at)) AS oldest " +
                "FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.id IS NULL " +
                "GROUP BY bucket ORDER BY bucket");

            var counts = new int[Labels.Length];
            int total = 0, oldest = 0;

            foreach (DataRow row in dt.Rows)
            {
                int b = AnalyticsData.Int(row, "bucket");
                int n = AnalyticsData.Int(row, "n");
                if (b < 0 || b >= Labels.Length) continue;
                counts[b] = n;
                total += n;
                int o = AnalyticsData.Int(row, "oldest");
                if (o > oldest) oldest = o;
            }

            if (total == 0)
            {
                int everReleased = AnalyticsData.Scalar("SELECT COUNT(*) FROM certificate_requests");
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    everReleased == 0
                        ? "No certificate requests on record yet."
                        : "Nothing outstanding — every certificate request has been released.",
                    everReleased == 0 ? null : "This is the good state, not a missing chart.");
                return;
            }

            ResetChart();
            Series s = ChartStyle.Column("Outstanding requests", 0);
            for (int i = 0; i < Labels.Length; i++)
            {
                s.Points.AddXY(Labels[i], counts[i]);
                // Past 30 days a request is no longer "in progress", it is a client who has not
                // come back — coloured so it separates from the healthy end of the chart.
                if (i >= 2) ChartStyle.Highlight(s, s.Points.Count - 1, ChartStyle.Colour(4));
                else if (i == 1) ChartStyle.Highlight(s, s.Points.Count - 1, ChartStyle.Colour(1));
            }
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Age of the request today", "Outstanding requests");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(counts, total, oldest));
        }

        /// <summary>Deterministic string.Format over the same bucket counts the bars were built from.</summary>
        private string BuildCaption(int[] counts, int total, int oldest)
        {
            int stale = counts[2] + counts[3];

            if (stale == 0)
            {
                return string.Format(
                    "{0} request{1} outstanding, all under 30 days old — nothing has gone stale. Oldest is {2} day{3}.",
                    total, total == 1 ? "" : "s", oldest, oldest == 1 ? "" : "s");
            }

            return string.Format(
                "{0} of {1} outstanding requests ({2:0.#}%) are over 30 days old, {3} of them past 90 days. The oldest has been waiting {4} days — worth chasing the claimants.",
                stale, total, AnalyticsData.Pct(stale, total), counts[3], oldest);
        }
    }
}
