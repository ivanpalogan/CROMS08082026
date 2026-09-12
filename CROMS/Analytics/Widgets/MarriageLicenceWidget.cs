using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Marriage licences filed against marriages registered, by month.
    ///
    /// The two lines answer a real office question: a licence is applied for, posted for ten
    /// days (Family Code Art. 17) and then used, so licences should lead registrations by
    /// roughly a month. A month where licences run far above registrations is either a batch
    /// of couples yet to marry or licences that were never used; the other way round means
    /// marriages are being registered without a licence on file here — which for most couples
    /// needs an Art. 34 exemption on the record.
    ///
    /// The licence line counts by FILED DATE. `marriage_licenses` has a `status` of
    /// Posting/Issued but no issued-on date, so "issued this month" is not derivable — filed
    /// is the only date the table actually holds, and the axis title says so rather than
    /// implying otherwise.
    /// </summary>
    public class MarriageLicenceWidget : AnalyticsWidget
    {
        public MarriageLicenceWidget()
            : base("Licences filed vs marriages registered",
                   "blue line by licence filed_date · amber line by date registered (created_at)")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable licences = AnalyticsData.Query(
                "SELECT DATE_FORMAT(filed_date, '" + TimeBuckets.MonthFormat + "') AS ym, COUNT(*) AS n " +
                "FROM marriage_licenses " +
                "WHERE filed_date IS NOT NULL AND filed_date >= @from AND filed_date < @to " +
                "GROUP BY ym ORDER BY ym", range);

            DataTable registered = AnalyticsData.Query(
                "SELECT DATE_FORMAT(created_at, '" + TimeBuckets.MonthFormat + "') AS ym, COUNT(*) AS n " +
                "FROM marriages " +
                "WHERE created_at >= @from AND created_at < @to " +
                "GROUP BY ym ORDER BY ym", range);

            List<string> axis = TimeBuckets.MonthAxis(licences, registered);
            if (axis.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No licences filed and no marriages registered in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            Dictionary<string, double> lic = TimeBuckets.ToMap(licences, "ym", "n");
            Dictionary<string, double> reg = TimeBuckets.ToMap(registered, "ym", "n");

            ResetChart();
            bool sparse = axis.Count < 3;
            Series sLic = sparse ? ChartStyle.Column("Licences filed", 0) : ChartStyle.Line("Licences filed", 0);
            Series sReg = sparse ? ChartStyle.Column("Marriages registered", 1) : ChartStyle.Line("Marriages registered", 1);

            foreach (string key in axis)
            {
                string label = TimeBuckets.MonthLabel(key);
                sLic.Points.AddXY(label, TimeBuckets.At(lic, key));
                sReg.Points.AddXY(label, TimeBuckets.At(reg, key));
            }

            Chart.Series.Add(sLic);
            Chart.Series.Add(sReg);
            ChartStyle.Axes(Chart, "Month", "Count");
            ChartStyle.WholeNumberY(Chart);
            if (axis.Count > 8) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            // Licences still in posting — the office's live workload, not derivable from the chart.
            int posting = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM marriage_licenses WHERE status = 'Posting'");

            SetCaption(BuildCaption(lic, reg, axis.Count, sparse, posting));
        }

        /// <summary>Deterministic string.Format over the same totals the lines were built from.</summary>
        private string BuildCaption(Dictionary<string, double> lic, Dictionary<string, double> reg,
                                    int months, bool sparse, int posting)
        {
            int totalLic = (int)Math.Round(TimeBuckets.Total(lic));
            int totalReg = (int)Math.Round(TimeBuckets.Total(reg));

            string postingNote = posting == 0
                ? ""
                : string.Format(" {0} licence{1} currently in the 10-day posting period.",
                    posting, posting == 1 ? " is" : "s are");

            if (sparse)
            {
                return string.Format("{0} licence{1} filed and {2} marriage{3} registered in this period — too few months to compare the two.{4}",
                    totalLic, totalLic == 1 ? "" : "s",
                    totalReg, totalReg == 1 ? "" : "s", postingNote);
            }

            int gap = totalLic - totalReg;
            string gapPhrase;
            if (gap == 0)
                gapPhrase = "the two match over the period";
            else if (gap > 0)
                gapPhrase = string.Format("{0} more licence{1} than registrations — couples yet to marry, or licences that lapsed unused",
                    gap, gap == 1 ? "" : "s");
            else
                gapPhrase = string.Format("{0} more registration{1} than licences — check those records carry a licence or an Art. 34 exemption",
                    Math.Abs(gap), Math.Abs(gap) == 1 ? "" : "s");

            return string.Format("{0} licences filed against {1} marriages registered over {2} months — {3}.{4}",
                totalLic, totalReg, months, gapPhrase, postingNote);
        }
    }
}
