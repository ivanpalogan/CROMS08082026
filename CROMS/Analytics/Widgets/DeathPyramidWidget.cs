using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Population pyramid of deaths: ten-year age bands down the side, males to the left and
    /// females to the right of a shared centre line.
    ///
    /// Drawn as two horizontal bar series with the male values NEGATED — that is what puts
    /// them on the opposite side of zero. The axis is then formatted "#;#;0" (positive;
    /// negative; zero) so the minus signs never reach the operator; a label reading "-4 deaths"
    /// would be nonsense.
    ///
    /// Counted by DATE OF DEATH, not registration: this describes the population that died,
    /// and a delayed registration would otherwise move an old death into this year's bands.
    /// </summary>
    public class DeathPyramidWidget : AnalyticsWidget
    {
        private const int BandSize = 10;
        private const int MaxPlausibleAge = 130;

        public DeathPyramidWidget()
            : base("Age and sex distribution", "by date of death (date_of_death) · 10-year bands")
        {
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("deaths", "age", "date_of_death", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No deaths with a date of death in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Age at death is Municipal Form 103 item 5.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT FLOOR(age / @band) * @band AS band, sex, COUNT(*) AS n " +
                "FROM deaths " +
                "WHERE date_of_death IS NOT NULL AND date_of_death >= @from AND date_of_death < @to " +
                "  AND age IS NOT NULL AND age BETWEEN 0 AND @maxAge " +
                "  AND sex IS NOT NULL AND TRIM(sex) <> '' " +
                "GROUP BY band, sex ORDER BY band", range,
                new MySql.Data.MySqlClient.MySqlParameter("@band", BandSize),
                new MySql.Data.MySqlClient.MySqlParameter("@maxAge", MaxPlausibleAge));

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No deaths in this period have both an age and a sex recorded.",
                    "The pyramid needs both to place a record.");
                return;
            }

            var male = new Dictionary<int, int>();
            var female = new Dictionary<int, int>();
            int lowBand = int.MaxValue, highBand = int.MinValue;
            int totalM = 0, totalF = 0;
            double sumAges = 0;

            foreach (DataRow r in dt.Rows)
            {
                int band = AnalyticsData.Int(r, "band");
                int n = AnalyticsData.Int(r, "n");
                string sex = AnalyticsData.Str(r, "sex");

                if (!male.ContainsKey(band)) { male[band] = 0; female[band] = 0; }
                if (string.Equals(sex, "Male", StringComparison.OrdinalIgnoreCase))
                { male[band] += n; totalM += n; }
                else
                { female[band] += n; totalF += n; }

                if (band < lowBand) lowBand = band;
                if (band > highBand) highBand = band;
                sumAges += (band + BandSize / 2.0) * n;    // band midpoint
            }

            ResetChart();
            Series sM = ChartStyle.Bar("Male", 0);
            Series sF = ChartStyle.Bar("Female", 1);

            // Bands run oldest at the top, which is how a pyramid is read, so the axis is
            // filled from the highest band down.
            for (int band = highBand; band >= lowBand; band -= BandSize)
            {
                int m, f;
                if (!male.TryGetValue(band, out m)) m = 0;
                if (!female.TryGetValue(band, out f)) f = 0;
                string label = band + "–" + (band + BandSize - 1);
                sM.Points.AddXY(label, -m);      // negated: draws to the LEFT of centre
                sF.Points.AddXY(label, f);
            }

            Chart.Series.Add(sM);
            Chart.Series.Add(sF);
            ChartStyle.Axes(Chart, "Age at death", "Deaths");

            // On a Bar chart the VALUE axis is Y. Format hides the sign the negation introduced.
            ChartArea a = Chart.ChartAreas[0];
            a.AxisY.LabelStyle.Format = "#;#;0";
            a.AxisY.Interval = 0;
            int span = Math.Max(totalM, totalF);
            if (span <= 4) a.AxisY.Interval = 1;
            AutoLegend();

            SetCaption(BuildCaption(totalM, totalF, male, female, sumAges, cov));
        }

        /// <summary>Deterministic string.Format over the same band counts the bars were built from.</summary>
        private string BuildCaption(int males, int females,
                                    Dictionary<int, int> male, Dictionary<int, int> female,
                                    double sumAges, AnalyticsData.Coverage cov)
        {
            int total = males + females;
            string coverage = cov.Partial ? " " + cov.PartialNote : "";

            if (total < 3)
            {
                return string.Format("{0} death{1} with both age and sex recorded — {2} male, {3} female. Too few to describe a distribution.{4}",
                    total, total == 1 ? "" : "s", males, females, coverage);
            }

            int peakBand = 0, peakCount = -1;
            foreach (KeyValuePair<int, int> kv in male)
            {
                int combined = kv.Value + female[kv.Key];
                if (combined > peakCount) { peakCount = combined; peakBand = kv.Key; }
            }

            return string.Format(
                "{0} deaths — {1} male, {2} female. Mean age at death is about {3:0.#}; the heaviest band is {4}–{5} with {6}.{7}",
                total, males, females, sumAges / total,
                peakBand, peakBand + BandSize - 1, peakCount, coverage);
        }
    }
}
