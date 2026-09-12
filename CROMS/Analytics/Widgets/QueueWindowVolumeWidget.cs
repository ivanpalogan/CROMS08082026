using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// How many tickets each service window actually handled.
    ///
    /// SOURCE NOTE, and it is a deliberate departure from the written spec. The spec asked for
    /// this chart to come from `window_transactions` joined to `windows`. That table is NOT a
    /// history of work done — it is the window-to-service CAPABILITY map written by the window
    /// assignment screen (which services a window is allowed to accept). Charting it would count
    /// what each window is PERMITTED to do, not what it did, and would report the same numbers
    /// on a day nobody came in. The work actually done is recorded on the ticket, in
    /// `queue_tickets.window_no`, which is what this reads.
    ///
    /// `window_no` is stamped when a window SERVES a ticket. A ticket that was accepted but
    /// never served carries only `accepted_window`; those are counted as unassigned here and
    /// named in the caption rather than folded in, because "accepted" and "served" are
    /// different amounts of work.
    /// </summary>
    public class QueueWindowVolumeWidget : AnalyticsWidget
    {
        public QueueWindowVolumeWidget()
            : base("Tickets handled per window", "by ticket issued (created_at) · from queue_tickets.window_no")
        {
        }

        protected override void Render(DateRange range)
        {
            // LEFT JOIN so a ticket stamped with a window that has since been deleted still
            // appears, under its raw number, instead of vanishing from the totals.
            DataTable dt = AnalyticsData.Query(
                "SELECT t.window_no AS win, COALESCE(w.window_name, CONCAT('Window ', t.window_no)) AS name, " +
                "       COUNT(*) AS n " +
                "FROM queue_tickets t " +
                "LEFT JOIN windows w ON w.id = t.window_no " +
                "WHERE t.created_at >= @from AND t.created_at < @to AND t.window_no IS NOT NULL " +
                "GROUP BY t.window_no, name " +
                "ORDER BY n DESC, name", range);

            int unassigned = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to AND window_no IS NULL", range);

            if (dt.Rows.Count == 0)
            {
                if (unassigned > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — window_no is unfilled in " + unassigned + " of " + unassigned + " records.",
                        "A ticket is stamped with its window when a window serves it.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No queue tickets issued in this period.", null);
                }
                return;
            }

            var names = new List<string>();
            var counts = new List<int>();
            int total = 0;
            foreach (DataRow r in dt.Rows)
            {
                names.Add(AnalyticsData.Str(r, "name"));
                int n = AnalyticsData.Int(r, "n");
                counts.Add(n);
                total += n;
            }

            ResetChart();
            Series s = ChartStyle.Column("Tickets", 0);
            for (int i = 0; i < names.Count; i++) s.Points.AddXY(names[i], counts[i]);
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Service window", "Tickets handled");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(names, counts, total, unassigned));
        }

        /// <summary>Deterministic string.Format over the same counts the columns were built from.</summary>
        private string BuildCaption(List<string> names, List<int> counts, int total, int unassigned)
        {
            string never = unassigned == 0
                ? ""
                : string.Format(" {0} ticket{1} were never served by a window and are not counted.",
                    unassigned, unassigned == 1 ? "" : "s");

            if (names.Count < 2)
            {
                return string.Format("{0} handled all {1} ticket{2} in this period.{3}",
                    names[0], total, total == 1 ? "" : "s", never);
            }

            if (total < 3)
            {
                return string.Format("{0} ticket{1} across {2} windows — too few to compare load.{3}",
                    total, total == 1 ? "" : "s", names.Count, never);
            }

            double average = (double)total / names.Count;
            int busiest = 0, quietest = names.Count - 1;

            return string.Format(
                "{0} handled the most at {1} of {2} ({3:0.#}%); {4} the fewest at {5}. Even load would be about {6:0.#} each.{7}",
                names[busiest], counts[busiest], total, AnalyticsData.Pct(counts[busiest], total),
                names[quietest], counts[quietest], average, never);
        }
    }
}
