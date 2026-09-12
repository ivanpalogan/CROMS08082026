using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Certificate demand by month, split by which registry the copy was asked for, as a
    /// stacked area. The stack height is total counter demand; the bands say which registry
    /// book the office keeps reaching for.
    ///
    /// `certificate_requests.record_type` is nullable and is NULL on some rows here — a request
    /// taken before the record was picked. Those are shown as "Not specified" rather than
    /// dropped, because a request with no record type is still a client at the counter, and
    /// silently excluding them would under-report demand.
    /// </summary>
    public class CertDemandWidget : AnalyticsWidget
    {
        private static readonly string[] Types = { "Birth", "Marriage", "Death" };
        private const string Unspecified = "Not specified";

        public CertDemandWidget()
            : base("Certificate demand by record type", "by request date (certificate_requests.created_at)")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATE_FORMAT(created_at, '" + TimeBuckets.MonthFormat + "') AS ym, " +
                "       COALESCE(record_type, '') AS rtype, COUNT(*) AS n " +
                "FROM certificate_requests " +
                "WHERE created_at >= @from AND created_at < @to " +
                "GROUP BY ym, rtype ORDER BY ym", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No certificate requests in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            List<string> axis = TimeBuckets.MonthAxis(dt);
            var series = new Dictionary<string, Dictionary<string, int>>();
            var totals = new Dictionary<string, int>();
            var order = new List<string>(Types);
            int total = 0;

            foreach (string t in Types) { series[t] = new Dictionary<string, int>(); totals[t] = 0; }
            series[Unspecified] = new Dictionary<string, int>();
            totals[Unspecified] = 0;

            foreach (DataRow r in dt.Rows)
            {
                string ym = AnalyticsData.Str(r, "ym");
                string rtype = AnalyticsData.Str(r, "rtype");
                int n = AnalyticsData.Int(r, "n");

                string key = Array.IndexOf(Types, rtype) >= 0 ? rtype : Unspecified;
                if (!series[key].ContainsKey(ym)) series[key][ym] = 0;
                series[key][ym] += n;
                totals[key] += n;
                total += n;
            }
            if (totals[Unspecified] > 0) order.Add(Unspecified);

            ResetChart();
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                // A record type nobody requested would add an invisible band and a legend entry
                // that means nothing, so it is left out.
                if (totals[key] == 0) continue;

                // Under three months a stacked AREA has nothing to slope between, so the same
                // numbers are drawn as stacked columns instead.
                Series s = axis.Count < 3
                    ? ChartStyle.StackedColumn(key, i, false)
                    : ChartStyle.StackedArea(key, i);

                foreach (string ym in axis)
                {
                    int n;
                    if (!series[key].TryGetValue(ym, out n)) n = 0;
                    s.Points.AddXY(TimeBuckets.MonthLabel(ym), n);
                }
                Chart.Series.Add(s);
            }

            ChartStyle.Axes(Chart, "Month requested", "Requests");
            ChartStyle.WholeNumberY(Chart);
            if (axis.Count > 8) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildCaption(order, totals, total, axis.Count));
        }

        /// <summary>Deterministic string.Format over the same totals the bands were built from.</summary>
        private string BuildCaption(List<string> order, Dictionary<string, int> totals,
                                    int total, int months)
        {
            string unspecified = totals[Unspecified] == 0
                ? ""
                : string.Format(" {0} request{1} have no record type set.",
                    totals[Unspecified], totals[Unspecified] == 1 ? "" : "s");

            if (total < 3)
            {
                return string.Format("{0} certificate request{1} over {2} month{3} — too few to describe demand.{4}",
                    total, total == 1 ? "" : "s", months, months == 1 ? "" : "s", unspecified);
            }

            string lead = "";
            int best = 0;
            foreach (string t in Types) if (totals[t] > best) { best = totals[t]; lead = t; }

            double perMonth = (double)total / Math.Max(1, months);

            return string.Format(
                "{0} requests over {1} month{2}, about {3:0.#} a month. {4} copies are the bulk of it — {5} of {6} ({7:0.#}%).{8}",
                total, months, months == 1 ? "" : "s", perMonth,
                lead, best, total, AnalyticsData.Pct(best, total), unspecified);
        }
    }
}
