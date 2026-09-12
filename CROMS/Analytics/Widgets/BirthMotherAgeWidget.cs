using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Mothers' ages in five-year bands, with the under-20 bands highlighted — adolescent
    /// pregnancy is the figure health offices act on, and it is invisible in a plain average.
    ///
    /// Ages outside 10–60 are excluded from the histogram and counted separately: on this
    /// data mother_age arrives from OCR, and a single-digit misread ("99" for "22" is
    /// recorded in the project log) would otherwise stretch the axis and bury the real bands.
    ///
    /// On the office's current data this widget is EXPECTED to show its empty state —
    /// mother_age is unfilled in every row.
    /// </summary>
    public class BirthMotherAgeWidget : AnalyticsWidget
    {
        private const int BandSize = 5;
        private const int MinPlausible = 10;
        private const int MaxPlausible = 60;

        private static readonly Color UnderTwenty = Color.FromArgb(198, 50, 63);

        public BirthMotherAgeWidget()
            : base("Mother's age profile", "by date of birth (date_of_birth) · MF-102 item 8")
        {
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("births", "mother_age", "date_of_birth", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No births with a date of birth in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Mother's age is Municipal Form 102 item 8 — capture it on registration to fill this chart.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT FLOOR(mother_age / @band) * @band AS band, COUNT(*) AS n " +
                "FROM births " +
                "WHERE date_of_birth IS NOT NULL AND date_of_birth >= @from AND date_of_birth < @to " +
                "  AND mother_age IS NOT NULL AND mother_age BETWEEN @lo AND @hi " +
                "GROUP BY band ORDER BY band", range,
                new MySql.Data.MySqlClient.MySqlParameter("@band", BandSize),
                new MySql.Data.MySqlClient.MySqlParameter("@lo", MinPlausible),
                new MySql.Data.MySqlClient.MySqlParameter("@hi", MaxPlausible));

            int outOfRange = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM births " +
                "WHERE date_of_birth IS NOT NULL AND date_of_birth >= @from AND date_of_birth < @to " +
                "  AND mother_age IS NOT NULL AND (mother_age < @lo OR mother_age > @hi)", range,
                new MySql.Data.MySqlClient.MySqlParameter("@lo", MinPlausible),
                new MySql.Data.MySqlClient.MySqlParameter("@hi", MaxPlausible));

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                    "No mother's age in this period falls in a plausible range (" +
                    MinPlausible + "–" + MaxPlausible + ").",
                    outOfRange > 0
                        ? outOfRange + " record(s) hold an age outside that range and need checking."
                        : null);
                return;
            }

            // Fill every band between the lowest and highest present, so a gap reads as a gap.
            var counts = new Dictionary<int, int>();
            int lowBand = int.MaxValue, highBand = int.MinValue, total = 0, under20 = 0;
            double weightedSum = 0;

            foreach (DataRow r in dt.Rows)
            {
                int band = AnalyticsData.Int(r, "band");
                int n = AnalyticsData.Int(r, "n");
                counts[band] = n;
                total += n;
                if (band < lowBand) lowBand = band;
                if (band > highBand) highBand = band;
                if (band < 20) under20 += n;
                weightedSum += (band + BandSize / 2.0) * n;    // band midpoint, for a mean estimate
            }

            ResetChart();
            Series s = ChartStyle.Column("Mothers", 0);
            for (int band = lowBand; band <= highBand; band += BandSize)
            {
                int n;
                if (!counts.TryGetValue(band, out n)) n = 0;
                s.Points.AddXY(band + "–" + (band + BandSize - 1), n);
                if (band < 20) ChartStyle.Highlight(s, s.Points.Count - 1, UnderTwenty);
            }
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Mother's age at the birth", "Births");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(total, under20, weightedSum, counts, cov, outOfRange));
        }

        /// <summary>Deterministic string.Format over the same band counts the columns were built from.</summary>
        private string BuildCaption(int total, int under20, double weightedSum,
                                    Dictionary<int, int> counts, AnalyticsData.Coverage cov,
                                    int outOfRange)
        {
            string coverage = cov.Partial ? " " + cov.PartialNote : "";
            string excluded = outOfRange == 0
                ? ""
                : string.Format(" {0} record{1} with an age outside {2}–{3} were excluded and need checking.",
                    outOfRange, outOfRange == 1 ? "" : "s", MinPlausible, MaxPlausible);

            if (total < 3)
            {
                return string.Format("{0} birth{1} with the mother's age recorded, {2} of them to a mother under 20. Too few to describe a profile.{3}{4}",
                    total, total == 1 ? "" : "s", under20, coverage, excluded);
            }

            int peakBand = 0, peakCount = -1;
            foreach (KeyValuePair<int, int> kv in counts)
                if (kv.Value > peakCount) { peakCount = kv.Value; peakBand = kv.Key; }

            string teen = under20 == 0
                ? "none were to a mother under 20"
                : string.Format("{0} ({1:0.#}%) were to a mother under 20", under20, AnalyticsData.Pct(under20, total));

            return string.Format("Most births are to mothers aged {0}–{1} ({2} of {3}); {4}. Mean age is about {5:0.#}.{6}{7}",
                peakBand, peakBand + BandSize - 1, peakCount, total, teen, weightedSum / total, coverage, excluded);
        }
    }
}
