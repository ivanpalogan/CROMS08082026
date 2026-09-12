using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How long a transaction takes at the window, by transaction type — the number a window
    /// roster is actually built from.
    ///
    /// HOW A MULTI-SERVICE TICKET IS COUNTED, because it changes what the bars mean. One queue
    /// number can carry several services (`queue_ticket_services`), and the system times the
    /// TICKET, not each service — there is one started_at and one completed_at per ticket. So a
    /// ticket bundling CTC and a new registration contributes its whole duration to BOTH bars.
    /// That makes each bar "how long a visit including this service takes", not "how long this
    /// service takes alone". Splitting the time between services would need per-service
    /// timestamps the schema does not have, and inventing a split would be worse than saying so.
    /// The caption states it whenever bundled tickets are present.
    /// </summary>
    public class QueueServiceTimeWidget : AnalyticsWidget
    {
        private const int MaxPlausibleHours = 8;

        public QueueServiceTimeWidget()
            : base("Average service time by transaction",
                   "started_at to completed_at, per service on the ticket")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT s.service_label AS label, s.service_code AS code, " +
                "       AVG(TIMESTAMPDIFF(SECOND, t.started_at, t.completed_at)) AS avg_secs, " +
                "       COUNT(*) AS n " +
                "FROM queue_tickets t " +
                "JOIN queue_ticket_services s ON s.ticket_id = t.id " +
                "WHERE t.created_at >= @from AND t.created_at < @to " +
                "  AND t.started_at IS NOT NULL AND t.completed_at IS NOT NULL " +
                "  AND TIMESTAMPDIFF(SECOND, t.started_at, t.completed_at) >= 0 " +
                "  AND TIMESTAMPDIFF(SECOND, t.started_at, t.completed_at) <= @capSecs " +
                "GROUP BY s.service_code, s.service_label " +
                "ORDER BY avg_secs DESC", range,
                new MySql.Data.MySqlClient.MySqlParameter("@capSecs", MaxPlausibleHours * 3600));

            int notCompleted = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to " +
                "  AND (started_at IS NULL OR completed_at IS NULL)", range);

            // Tickets carrying more than one service — the number that decides whether the
            // bundling caveat below applies at all.
            int bundled = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM (" +
                "  SELECT t.id FROM queue_tickets t " +
                "  JOIN queue_ticket_services s ON s.ticket_id = t.id " +
                "  WHERE t.created_at >= @from AND t.created_at < @to " +
                "    AND t.started_at IS NOT NULL AND t.completed_at IS NOT NULL " +
                "  GROUP BY t.id HAVING COUNT(*) > 1) AS multi", range);

            if (dt.Rows.Count == 0)
            {
                if (notCompleted > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — completed_at is unfilled in " + notCompleted + " of " + notCompleted + " records.",
                        "A service time needs both a start and a completion on the ticket.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No completed queue tickets in this period.", null);
                }
                return;
            }

            var labels = new List<string>();
            var minutes = new List<double>();
            var counts = new List<int>();

            foreach (DataRow r in dt.Rows)
            {
                string label = AnalyticsData.Str(r, "label");
                if (label.Length == 0) label = AnalyticsData.Str(r, "code");
                labels.Add(label);
                minutes.Add(AnalyticsData.Dbl(r, "avg_secs") / 60.0);
                counts.Add(AnalyticsData.Int(r, "n"));
            }

            ResetChart();
            Series s2 = ChartStyle.Bar("Average minutes", 0);
            // A Bar chart draws its first point at the bottom, so the slowest transaction is
            // added last to put it at the top where the eye lands.
            for (int i = labels.Count - 1; i >= 0; i--)
                s2.Points.AddXY(Shorten(labels[i]), Math.Round(minutes[i], 1));
            Chart.Series.Add(s2);

            ChartStyle.Axes(Chart, "Transaction", "Average minutes at the window");
            ChartArea a = Chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "0.#";
            AutoLegend();

            SetCaption(BuildCaption(labels, minutes, counts, notCompleted, bundled));
        }

        private static string Shorten(string s)
        {
            if (s.Length <= 26) return s;
            return s.Substring(0, 24).TrimEnd() + "…";
        }

        /// <summary>Deterministic string.Format over the same averages the bars were built from.</summary>
        private string BuildCaption(List<string> labels, List<double> minutes, List<int> counts,
                                    int notCompleted, int bundled)
        {
            int measured = 0;
            foreach (int n in counts) measured += n;

            string open = notCompleted == 0
                ? ""
                : string.Format(" {0} ticket{1} in this period are not completed and are not counted.",
                    notCompleted, notCompleted == 1 ? "" : "s");

            string bundling = bundled == 0
                ? ""
                : string.Format(" {0} ticket{1} carried more than one service, so their full visit time counts towards each service on them.",
                    bundled, bundled == 1 ? "" : "s");

            if (measured < 3)
            {
                return string.Format("{0} completed service{1} measured, slowest \"{2}\" at {3:0.#} minutes. Too few to average reliably.{4}{5}",
                    measured, measured == 1 ? "" : "s", labels[0], minutes[0], open, bundling);
            }

            int fastest = labels.Count - 1;
            return string.Format("\"{0}\" takes longest at {1:0.#} minutes ({2} ticket{3}); \"{4}\" is quickest at {5:0.#}.{6}{7}",
                labels[0], minutes[0], counts[0], counts[0] == 1 ? "" : "s",
                labels[fastest], minutes[fastest], open, bundling);
        }
    }
}
