using System;
using CROMS.Analytics.Widgets;
using CROMS.Data;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Queuing tab. This one is OPERATIONAL rather than demographic, so its cards deliberately
    /// break the registry pattern: there is no "registered vs occurred" here and no
    /// month-on-month comparison. What the front desk needs at a glance is the state of the
    /// floor RIGHT NOW — who is waiting, how long they have waited, and when the rush is.
    /// </summary>
    public class QueuingAnalyticsTab : AnalyticsTab
    {
        private const int MaxPlausibleWaitHours = 8;

        private SummaryCard _cardServed;
        private SummaryCard _cardWaiting;
        private SummaryCard _cardWait;
        private SummaryCard _cardBusiest;

        protected override void Build()
        {
            _cardServed  = AddCard("TICKETS SERVED TODAY\r\ncompleted at a window", ChartStyle.Colour(2));
            _cardWaiting = AddCard("CURRENTLY WAITING\r\nnot yet called", ChartStyle.Colour(1));
            _cardWait    = AddCard("AVERAGE WAIT TODAY\r\nissued to called", ChartStyle.Colour(0));
            _cardBusiest = AddCard("BUSIEST HOUR TODAY\r\nby tickets issued", ChartStyle.Colour(3));

            AddWidget(new QueuePeakHeatmapWidget());
            AddWidget(new QueueWaitTrendWidget());
            AddWidget(new QueueServiceTimeWidget());
            AddWidget(new QueueWindowVolumeWidget());
            AddWidget(new QueueOutcomesWidget());

            // Stated once, on the tab, because it applies to a chart the spec named a different
            // source for and an operator comparing the two would otherwise assume a bug.
            ShowNote("Tickets handled per window is read from queue_tickets.window_no. " +
                     "window_transactions holds which services a window may accept, not what it did.");

            AddReportButton("Print Assessment Report", (s, e) =>
            {
                System.Data.DataTable t = AssessmentReport.BuildTable(AssessmentReport.Queue, DateTime.Today.Year);
                AssessmentReport.Show(AssessmentReport.Queue, t, FindForm());
            });
        }

        protected override void LoadCards()
        {
            DateRange today = DateRange.Today();

            // ---- served today
            int served = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to " +
                "  AND (completed_at IS NOT NULL OR status = 'Completed')", today);
            int issued = AnalyticsData.CountInRange("queue_tickets", "created_at", today);
            _cardServed.SetNoComparison(served.ToString("N0"),
                issued == 0 ? "no tickets issued today yet" : "of " + issued + " issued today");

            // ---- currently waiting. Deliberately NOT restricted to today: a ticket left open
            // from an earlier day is still an open ticket, and hiding it is how a queue quietly
            // accumulates stragglers nobody sees.
            int waiting = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM queue_tickets WHERE status = 'Waiting'");
            int waitingToday = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE status = 'Waiting' AND created_at >= @from AND created_at < @to", today);

            string waitingNote;
            if (waiting == 0) waitingNote = "nobody is waiting";
            else if (waiting == waitingToday) waitingNote = "all issued today";
            else waitingNote = waitingToday + " today, " + (waiting - waitingToday) + " from earlier days";
            _cardWaiting.SetNoComparison(waiting.ToString("N0"), waitingNote);

            // ---- average wait today, over tickets actually called
            double? avgSecs = AnalyticsData.ScalarDbl(
                "SELECT AVG(TIMESTAMPDIFF(SECOND, created_at, called_at)) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to AND called_at IS NOT NULL " +
                "  AND TIMESTAMPDIFF(SECOND, created_at, called_at) BETWEEN 0 AND @cap",
                new MySql.Data.MySqlClient.MySqlParameter("@from", today.From),
                new MySql.Data.MySqlClient.MySqlParameter("@to", today.ToExclusive),
                new MySql.Data.MySqlClient.MySqlParameter("@cap", MaxPlausibleWaitHours * 3600));

            int called = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to AND called_at IS NOT NULL", today);

            if (!avgSecs.HasValue || called == 0)
            {
                _cardWait.SetNoComparison("—", "no tickets called today yet");
            }
            else
            {
                _cardWait.SetNoComparison((avgSecs.Value / 60.0).ToString("0.#") + " min",
                    "across " + called + " ticket" + (called == 1 ? "" : "s") + " called today");
            }

            // ---- busiest hour today
            System.Data.DataTable peak = AnalyticsData.Query(
                "SELECT HOUR(created_at) AS h, COUNT(*) AS n FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to " +
                "GROUP BY h ORDER BY n DESC, h LIMIT 1", today);

            if (peak.Rows.Count == 0)
            {
                _cardBusiest.SetNoComparison("—", "no tickets issued today yet");
            }
            else
            {
                int h = AnalyticsData.Int(peak.Rows[0], "h");
                int n = AnalyticsData.Int(peak.Rows[0], "n");
                _cardBusiest.SetNoComparison(HourRange(h),
                    n + " ticket" + (n == 1 ? "" : "s") + " issued in that hour");
            }
        }

        /// <summary>"2–3pm" — an hour bucket reads better as the span it covers than as a point.</summary>
        private static string HourRange(int h)
        {
            return Clock(h) + "–" + Clock((h + 1) % 24);
        }

        private static string Clock(int h)
        {
            if (h == 0) return "12mn";
            if (h == 12) return "12nn";
            return (h % 12) + (h < 12 ? "am" : "pm");
        }
    }
}
