using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// What became of each queue ticket, stacked by month: served, forwarded to another window,
    /// or abandoned.
    ///
    /// HOW EACH OUTCOME IS DECIDED, since the schema has no single outcome column:
    ///   Forwarded — the ticket has a row in `queue_ticket_forwards`. Checked FIRST, because a
    ///               forwarded ticket is usually also completed later, and counting it in both
    ///               would make the columns add up to more than the tickets issued.
    ///   Served    — completed_at is set, or the status reads Completed.
    ///   Abandoned — neither, and the ticket is from a day that has already ended. A client who
    ///               is still waiting right now has not abandoned anything, so TODAY's open
    ///               tickets are excluded and counted separately in the caption.
    ///
    /// On the office's current data `queue_ticket_forwards` is empty, so the forwarded series
    /// will be flat zero — that is a true reading of the data, not a broken chart.
    /// </summary>
    public class QueueOutcomesWidget : AnalyticsWidget
    {
        public QueueOutcomesWidget()
            : base("Ticket outcomes", "by ticket issued (created_at) · served / forwarded / abandoned")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATE_FORMAT(t.created_at, '" + TimeBuckets.MonthFormat + "') AS ym, " +
                "  SUM(CASE WHEN f.ticket_id IS NOT NULL THEN 1 ELSE 0 END) AS forwarded, " +
                "  SUM(CASE WHEN f.ticket_id IS NULL " +
                "            AND (t.completed_at IS NOT NULL OR t.status = 'Completed') THEN 1 ELSE 0 END) AS served, " +
                "  SUM(CASE WHEN f.ticket_id IS NULL " +
                "            AND t.completed_at IS NULL AND t.status <> 'Completed' " +
                "            AND DATE(t.created_at) < CURDATE() THEN 1 ELSE 0 END) AS abandoned, " +
                "  SUM(CASE WHEN f.ticket_id IS NULL " +
                "            AND t.completed_at IS NULL AND t.status <> 'Completed' " +
                "            AND DATE(t.created_at) = CURDATE() THEN 1 ELSE 0 END) AS still_open " +
                "FROM queue_tickets t " +
                "LEFT JOIN (SELECT DISTINCT ticket_id FROM queue_ticket_forwards) f ON f.ticket_id = t.id " +
                "WHERE t.created_at >= @from AND t.created_at < @to " +
                "GROUP BY ym ORDER BY ym", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No queue tickets issued in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            var axis = new List<string>();
            var served = new List<int>();
            var forwarded = new List<int>();
            var abandoned = new List<int>();
            int tServed = 0, tForwarded = 0, tAbandoned = 0, tOpen = 0;

            foreach (DataRow r in dt.Rows)
            {
                axis.Add(TimeBuckets.MonthLabel(AnalyticsData.Str(r, "ym")));
                int s = AnalyticsData.Int(r, "served");
                int f = AnalyticsData.Int(r, "forwarded");
                int a = AnalyticsData.Int(r, "abandoned");
                served.Add(s); forwarded.Add(f); abandoned.Add(a);
                tServed += s; tForwarded += f; tAbandoned += a;
                tOpen += AnalyticsData.Int(r, "still_open");
            }

            ResetChart();
            Series sServed = ChartStyle.StackedColumn("Served", 2, false);
            Series sFwd = ChartStyle.StackedColumn("Forwarded", 1, false);
            Series sAband = ChartStyle.StackedColumn("Abandoned", 4, false);

            for (int i = 0; i < axis.Count; i++)
            {
                sServed.Points.AddXY(axis[i], served[i]);
                sFwd.Points.AddXY(axis[i], forwarded[i]);
                sAband.Points.AddXY(axis[i], abandoned[i]);
            }

            Chart.Series.Add(sServed);
            // Only shown when it happens — an always-zero series in the legend reads as a
            // broken chart rather than as "this office does not forward tickets".
            if (tForwarded > 0) Chart.Series.Add(sFwd);
            Chart.Series.Add(sAband);

            ChartStyle.Axes(Chart, "Month issued", "Tickets");
            ChartStyle.WholeNumberY(Chart);
            if (axis.Count > 8) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildCaption(tServed, tForwarded, tAbandoned, tOpen, axis.Count));
        }

        /// <summary>Deterministic string.Format over the same totals the columns were built from.</summary>
        private string BuildCaption(int served, int forwarded, int abandoned, int stillOpen, int months)
        {
            int closed = served + forwarded + abandoned;

            string open = stillOpen == 0
                ? ""
                : string.Format(" {0} ticket{1} issued today are still open and are not counted as either.",
                    stillOpen, stillOpen == 1 ? "" : "s");

            string forwardNote = forwarded == 0
                ? " No tickets were forwarded between windows in this period."
                : string.Format(" {0} ticket{1} were forwarded to another window.", forwarded, forwarded == 1 ? "" : "s");

            if (closed < 3)
            {
                return string.Format("{0} closed ticket{1} over {2} month{3} — {4} served, {5} abandoned. Too few to state a rate.{6}",
                    closed, closed == 1 ? "" : "s", months, months == 1 ? "" : "s",
                    served, abandoned, open);
            }

            if (abandoned == 0)
            {
                return string.Format("All {0} closed tickets were served — nobody left the queue unserved.{1}{2}",
                    closed, forwardNote, open);
            }

            return string.Format("{0} of {1} tickets ({2:0.#}%) were abandoned before being served; {3} were served.{4}{5}",
                abandoned, closed, AnalyticsData.Pct(abandoned, closed), served, forwardNote, open);
        }
    }
}
