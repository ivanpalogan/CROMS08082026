using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Events REGISTERED against events that OCCURRED, both by month, on one pair of lines.
    ///
    /// ONE implementation serves Birth, Marriage and Death: the three tabs differ only in
    /// which table and which event-date column they read, so they pass those in rather than
    /// each carrying a copy of this chart.
    ///
    /// The two dates mean different things and the module keeps them apart everywhere:
    /// created_at is when the office did the work, the event column is when the event
    /// happened. The vertical gap between the lines IS the delayed-registration backlog — a
    /// month where registrations run above events is the office clearing older work, and a
    /// month the other way round is work still to come.
    /// </summary>
    public class RegistrationTrendWidget : AnalyticsWidget
    {
        private readonly string _table;
        private readonly string _eventColumn;
        private readonly string _noun;         // "birth" / "marriage" / "death"
        private readonly string _nounPlural;

        /// <param name="table">Registry table — a compile-time literal, validated by Ident().</param>
        /// <param name="eventColumn">date_of_birth / date_of_marriage / date_of_death.</param>
        public RegistrationTrendWidget(string table, string eventColumn,
                                       string title, string eventLabel,
                                       string noun, string nounPlural)
            : base(title,
                   "blue line by date registered (created_at) · amber line by " + eventLabel)
        {
            _table = AnalyticsData.Ident(table);
            _eventColumn = AnalyticsData.Ident(eventColumn);
            _noun = noun;
            _nounPlural = nounPlural;
        }

        protected override void Render(DateRange range)
        {
            // Two GROUP BY month pulls, one per date meaning. Values are parameters; the only
            // literals in the SQL are the identifiers checked in the constructor.
            DataTable registered = AnalyticsData.Query(
                "SELECT DATE_FORMAT(created_at, '" + TimeBuckets.MonthFormat + "') AS ym, COUNT(*) AS n " +
                "FROM " + _table + " " +
                "WHERE created_at >= @from AND created_at < @to " +
                "GROUP BY ym ORDER BY ym", range);

            DataTable occurred = AnalyticsData.Query(
                "SELECT DATE_FORMAT(" + _eventColumn + ", '" + TimeBuckets.MonthFormat + "') AS ym, COUNT(*) AS n " +
                "FROM " + _table + " " +
                "WHERE " + _eventColumn + " IS NOT NULL AND " +
                _eventColumn + " >= @from AND " + _eventColumn + " < @to " +
                "GROUP BY ym ORDER BY ym", range);

            List<string> axis = TimeBuckets.MonthAxis(registered, occurred);
            if (axis.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No " + _nounPlural + " registered or occurring in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            Dictionary<string, double> reg = TimeBuckets.ToMap(registered, "ym", "n");
            Dictionary<string, double> occ = TimeBuckets.ToMap(occurred, "ym", "n");

            ResetChart();

            // Under three months a LINE implies a trend that two points cannot support, so the
            // same numbers are drawn as columns instead (spec: suppress trend lines below three
            // data points). The query and the values are identical either way.
            bool sparse = axis.Count < 3;
            Series sReg = sparse ? ChartStyle.Column("Registered", 0) : ChartStyle.Line("Registered", 0);
            Series sOcc = sparse ? ChartStyle.Column("Occurred", 1) : ChartStyle.Line("Occurred", 1);

            foreach (string key in axis)
            {
                string label = TimeBuckets.MonthLabel(key);
                sReg.Points.AddXY(label, TimeBuckets.At(reg, key));
                sOcc.Points.AddXY(label, TimeBuckets.At(occ, key));
            }

            Chart.Series.Add(sReg);
            Chart.Series.Add(sOcc);
            ChartStyle.Axes(Chart, "Month", Cap(_nounPlural));
            ChartStyle.WholeNumberY(Chart);
            if (axis.Count > 8) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildCaption(axis, reg, occ, sparse));
        }

        /// <summary>
        /// The insight sentence. Plain string formatting over the SAME totals the chart is
        /// bound to — no model or service is involved, so the sentence can never disagree
        /// with the picture above it.
        /// </summary>
        private string BuildCaption(List<string> axis,
                                    Dictionary<string, double> reg,
                                    Dictionary<string, double> occ,
                                    bool sparse)
        {
            int totalReg = (int)Math.Round(TimeBuckets.Total(reg));
            int totalOcc = (int)Math.Round(TimeBuckets.Total(occ));

            if (sparse)
            {
                return string.Format(
                    "{0} registered and {1} occurring in this period — too few months to describe a trend.",
                    Count(totalReg), Count(totalOcc));
            }

            int gap = totalReg - totalOcc;
            string gapPhrase = gap == 0
                ? "the two match, so the office is registering " + _nounPlural + " as fast as they occur"
                : gap > 0
                    ? string.Format("{0} more registrations than {1}, so the office is clearing a backlog of earlier events", gap, _nounPlural)
                    : string.Format("{0} more {1} than registrations, so {0} event{2} in this window are still to be registered",
                                    Math.Abs(gap), _nounPlural, Math.Abs(gap) == 1 ? "" : "s");

            string peakKey = TimeBuckets.Peak(reg, axis);
            string peakPhrase = "";
            if (peakKey.Length > 0)
            {
                int peak = (int)TimeBuckets.At(reg, peakKey);
                peakPhrase = string.Format(" Busiest month for the office was {0} with {1} registration{2}.",
                    TimeBuckets.MonthLabel(peakKey), peak, peak == 1 ? "" : "s");
            }

            return string.Format("{0} registered against {1} occurring over {2} months — {3}.{4}",
                totalReg, totalOcc, axis.Count, gapPhrase, peakPhrase);
        }

        private string Count(int n)
        {
            return n + " " + (n == 1 ? _noun : _nounPlural);
        }

        private static string Cap(string s)
        {
            return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
