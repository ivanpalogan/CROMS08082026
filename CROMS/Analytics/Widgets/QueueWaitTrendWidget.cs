using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Average wait per day — from the moment the kiosk issues a ticket to the moment a window
    /// calls the client.
    ///
    /// Only tickets that were actually CALLED can be measured, so a day where everyone
    /// abandoned the queue looks quiet rather than terrible. The caption states how many
    /// tickets in the period were never called, because that number is the missing half of the
    /// picture.
    ///
    /// Waits are clamped to a plausible window: a negative duration means the timestamps are
    /// inconsistent, and anything past <see cref="MaxPlausibleHours"/> hours is a ticket left
    /// open overnight rather than a client who waited that long. Both are excluded from the
    /// average and reported instead of quietly inflating it.
    /// </summary>
    public class QueueWaitTrendWidget : AnalyticsWidget
    {
        private const int MaxPlausibleHours = 8;

        public QueueWaitTrendWidget()
            : base("Average wait time", "ticket issued (created_at) to called (called_at), by day")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATE(created_at) AS d, " +
                "       AVG(TIMESTAMPDIFF(SECOND, created_at, called_at)) AS avg_secs, " +
                "       MAX(TIMESTAMPDIFF(SECOND, created_at, called_at)) AS max_secs, " +
                "       COUNT(*) AS n " +
                "FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to " +
                "  AND called_at IS NOT NULL " +
                "  AND TIMESTAMPDIFF(SECOND, created_at, called_at) >= 0 " +
                "  AND TIMESTAMPDIFF(SECOND, created_at, called_at) <= @capSecs " +
                "GROUP BY d ORDER BY d", range,
                new MySql.Data.MySqlClient.MySqlParameter("@capSecs", MaxPlausibleHours * 3600));

            int neverCalled = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to AND called_at IS NULL", range);

            int implausible = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to AND called_at IS NOT NULL " +
                "  AND (TIMESTAMPDIFF(SECOND, created_at, called_at) < 0 " +
                "    OR TIMESTAMPDIFF(SECOND, created_at, called_at) > @capSecs)", range,
                new MySql.Data.MySqlClient.MySqlParameter("@capSecs", MaxPlausibleHours * 3600));

            if (dt.Rows.Count == 0)
            {
                if (neverCalled > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — called_at is unfilled in " + neverCalled + " of " + neverCalled + " records.",
                        "A wait can only be measured once a window has called the ticket.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No queue tickets were called in this period.", null);
                }
                return;
            }

            var labels = new List<string>();
            var averages = new List<double>();
            double weightedSum = 0, worst = 0;
            string worstDay = "";
            int measured = 0;

            foreach (DataRow r in dt.Rows)
            {
                DateTime day = Convert.ToDateTime(r["d"]);
                double avgMin = AnalyticsData.Dbl(r, "avg_secs") / 60.0;
                double maxMin = AnalyticsData.Dbl(r, "max_secs") / 60.0;
                int n = AnalyticsData.Int(r, "n");

                labels.Add(day.ToString("dd MMM"));
                averages.Add(avgMin);
                weightedSum += avgMin * n;
                measured += n;
                if (maxMin > worst) { worst = maxMin; worstDay = day.ToString("dd MMM"); }
            }

            ResetChart();

            // Under three days a line asserts a trend two points cannot support.
            bool sparse = labels.Count < 3;
            Series s = sparse ? ChartStyle.Column("Average wait", 0) : ChartStyle.Line("Average wait", 0);
            for (int i = 0; i < labels.Count; i++) s.Points.AddXY(labels[i], Math.Round(averages[i], 1));
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Day", "Minutes waiting");
            ChartArea a = Chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "0.#";
            if (labels.Count > 10) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildCaption(labels, averages, weightedSum, measured,
                                    worst, worstDay, neverCalled, implausible, sparse));
        }

        /// <summary>Deterministic string.Format over the same per-day averages the line was built from.</summary>
        private string BuildCaption(List<string> labels, List<double> averages,
                                    double weightedSum, int measured,
                                    double worst, string worstDay,
                                    int neverCalled, int implausible, bool sparse)
        {
            string uncalled = neverCalled == 0
                ? ""
                : string.Format(" {0} ticket{1} in this period were never called and are not in this average.",
                    neverCalled, neverCalled == 1 ? "" : "s");

            string excluded = implausible == 0
                ? ""
                : string.Format(" {0} ticket{1} had an implausible wait (negative, or over {2} hours) and were excluded.",
                    implausible, implausible == 1 ? "" : "s", MaxPlausibleHours);

            double overall = measured == 0 ? 0 : weightedSum / measured;

            if (sparse)
            {
                return string.Format("Average wait {0:0.#} minutes across {1} ticket{2} on {3} day{4}. Too few days to show a trend.{5}{6}",
                    overall, measured, measured == 1 ? "" : "s",
                    labels.Count, labels.Count == 1 ? "" : "s", uncalled, excluded);
            }

            // Direction over the period, stated as first-half vs second-half rather than as a
            // regression: on a dozen noisy days a slope reads as more precision than exists.
            int half = averages.Count / 2;
            double firstHalf = 0, secondHalf = 0;
            for (int i = 0; i < half; i++) firstHalf += averages[i];
            for (int i = half; i < averages.Count; i++) secondHalf += averages[i];
            firstHalf /= Math.Max(1, half);
            secondHalf /= Math.Max(1, averages.Count - half);

            string direction;
            if (Math.Abs(secondHalf - firstHalf) < 0.5)
                direction = "waits are steady across the period";
            else if (secondHalf > firstHalf)
                direction = string.Format("waits are rising — {0:0.#} min in the later half against {1:0.#} earlier", secondHalf, firstHalf);
            else
                direction = string.Format("waits are falling — {0:0.#} min in the later half against {1:0.#} earlier", secondHalf, firstHalf);

            return string.Format("Average wait {0:0.#} minutes over {1} days ({2} tickets); {3}. Longest single wait was {4:0.#} minutes on {5}.{6}{7}",
                overall, labels.Count, measured, direction, worst, worstDay, uncalled, excluded);
        }
    }
}
