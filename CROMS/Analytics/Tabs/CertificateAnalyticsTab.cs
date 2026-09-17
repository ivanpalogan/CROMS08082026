using System;
using CROMS.Analytics.Widgets;
using CROMS.Data;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Certificates &amp; Releases tab — the counter's own workload and money.
    ///
    /// Two of the four cards are BACKLOG figures (pending release, unclaimed over 30 days) and
    /// are deliberately whole-registry rather than this-month: a request from four months ago
    /// is precisely the one worth surfacing, and scoping it to a month is how it stops being
    /// visible.
    /// </summary>
    public class CertificateAnalyticsTab : AnalyticsTab
    {
        private const int UnclaimedDays = 30;

        private SummaryCard _cardRequests;
        private SummaryCard _cardPending;
        private SummaryCard _cardUnclaimed;
        private SummaryCard _cardTurnaround;

        protected override void Build()
        {
            _cardRequests   = AddCard("REQUESTS THIS MONTH\r\nby request date", ChartStyle.Colour(0));
            _cardPending    = AddCard("PENDING RELEASE\r\noutstanding, all time", ChartStyle.Colour(1));
            _cardUnclaimed  = AddCard("UNCLAIMED OVER 30 DAYS\r\noutstanding, all time", ChartStyle.Colour(4));
            _cardTurnaround = AddCard("AVERAGE TURNAROUND\r\nrequest to release", ChartStyle.Colour(2));

            AddWidget(new CertDemandWidget());
            AddWidget(new CertTurnaroundWidget());
            AddWidget(new CertAgingWidget());
            AddWidget(new CertRegistryYearsWidget());
            AddWidget(new CertCollectionWidget());

            AddReportButton("Print Assessment Report", (s, e) =>
            {
                System.Data.DataTable t = AssessmentReport.BuildTable(AssessmentReport.Certificate, DateTime.Today.Year);
                AssessmentReport.Show(AssessmentReport.Certificate, t, FindForm());
            });
        }

        protected override void LoadCards()
        {
            // ---- requests this month vs last month
            int thisMonth = AnalyticsData.CountInRange("certificate_requests", "created_at", DateRange.ThisMonth());
            int lastMonth = AnalyticsData.CountInRange("certificate_requests", "created_at", DateRange.LastMonth());
            _cardRequests.Set(thisMonth.ToString("N0"),
                AnalyticsData.Compare(thisMonth, lastMonth, "last month"));

            // ---- pending release. Outstanding = no releases row on the request's transaction.
            // The release row is the FACT of the handover; the status column is a label someone
            // set, so it is reported as a cross-check rather than used as the measure.
            int pending = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.id IS NULL");
            int markedReleased = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.id IS NULL AND cr.status = 'Released'");

            _cardPending.SetNoComparison(pending.ToString("N0"),
                markedReleased > 0
                    ? markedReleased + " marked Released with no release record"
                    : (pending == 0 ? "everything has been handed over" : "not yet handed over"));

            // ---- unclaimed over 30 days
            int unclaimed = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.id IS NULL AND DATEDIFF(CURDATE(), cr.created_at) > @days",
                new MySql.Data.MySqlClient.MySqlParameter("@days", UnclaimedDays));

            int oldest = AnalyticsData.Scalar(
                "SELECT COALESCE(MAX(DATEDIFF(CURDATE(), cr.created_at)), 0) FROM certificate_requests cr " +
                "LEFT JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.id IS NULL");

            _cardUnclaimed.Set(unclaimed.ToString("N0"),
                unclaimed == 0
                    ? "nothing has gone stale"
                    : "oldest has waited " + oldest + " days",
                unclaimed == 0 ? Modules.UiTheme.Faint : Modules.UiTheme.Warning);

            // ---- average turnaround, over released requests only
            double? meanDays = AnalyticsData.ScalarDbl(
                "SELECT AVG(DATEDIFF(r.released_at, cr.created_at)) " +
                "FROM certificate_requests cr " +
                "JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.released_at IS NOT NULL AND DATEDIFF(r.released_at, cr.created_at) >= 0");

            int released = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "JOIN releases r ON r.transaction_id = cr.transaction_id " +
                "WHERE r.released_at IS NOT NULL AND DATEDIFF(r.released_at, cr.created_at) >= 0");

            if (!meanDays.HasValue || released == 0)
            {
                _cardTurnaround.SetNoComparison("—", "nothing released yet to measure");
            }
            else if (released < 3)
            {
                _cardTurnaround.SetNoComparison(Days(meanDays.Value),
                    "from only " + released + " released request" + (released == 1 ? "" : "s"));
            }
            else
            {
                _cardTurnaround.SetNoComparison(Days(meanDays.Value),
                    "across " + released + " released requests");
            }
        }

        /// <summary>Reads a sub-day mean as "same day" rather than "0.4 days", which means nothing to staff.</summary>
        private static string Days(double d)
        {
            if (d < 0.5) return "Same day";
            return d.ToString("0.#") + (d < 1.5 ? " day" : " days");
        }
    }
}
