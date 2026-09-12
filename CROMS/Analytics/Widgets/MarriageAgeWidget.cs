using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Ages of husbands and wives in five-year bands, side by side, with the under-18 band
    /// flagged in red.
    ///
    /// The under-18 flag is not decoration. Under RA 11596 (2021) marriage below 18 is void
    /// and a criminal offence, so a record showing it is either a data-entry error or something
    /// the registrar has to act on — either way it must be visible on sight, not buried in a
    /// percentage. Any such record is named in the caption with its count.
    ///
    /// The age GAP is reported in the caption rather than given its own chart: on a registry
    /// this size a second histogram of eleven gap buckets would be almost entirely empty, and
    /// the mean and the widest gap answer the same question in one line.
    /// </summary>
    public class MarriageAgeWidget : AnalyticsWidget
    {
        private const int BandSize = 5;
        private const int MinPlausible = 12;
        private const int MaxPlausible = 100;
        private const int LegalAge = 18;

        private static readonly Color UnderAge = Color.FromArgb(198, 50, 63);

        public MarriageAgeWidget()
            : base("Age profile of spouses", "by date of marriage (date_of_marriage) · MF-97 items 2a/2b")
        {
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage("marriages", "husband_age", "date_of_marriage", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No marriages with a date of marriage in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Ages of the contracting parties are Municipal Form 97 items 2a and 2b.");
                return;
            }

            DataTable husbands = BandQuery("husband_age", range);
            DataTable wives = BandQuery("wife_age", range);

            // The gap and the under-age check need the pair, so they are read per record.
            DataTable pairs = AnalyticsData.Query(
                "SELECT husband_age AS h, wife_age AS w " +
                "FROM marriages " +
                "WHERE date_of_marriage IS NOT NULL AND date_of_marriage >= @from AND date_of_marriage < @to " +
                "  AND husband_age IS NOT NULL AND wife_age IS NOT NULL", range);

            if (husbands.Rows.Count == 0 && wives.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                    "No spouse age in this period falls in a plausible range (" +
                    MinPlausible + "–" + MaxPlausible + ").", null);
                return;
            }

            var hMap = Bands(husbands);
            var wMap = Bands(wives);

            int lowBand = int.MaxValue, highBand = int.MinValue;
            foreach (int b in hMap.Keys) { if (b < lowBand) lowBand = b; if (b > highBand) highBand = b; }
            foreach (int b in wMap.Keys) { if (b < lowBand) lowBand = b; if (b > highBand) highBand = b; }

            ResetChart();
            Series sH = ChartStyle.Column("Husband", 0);
            Series sW = ChartStyle.Column("Wife", 1);

            for (int band = lowBand; band <= highBand; band += BandSize)
            {
                int h, w;
                if (!hMap.TryGetValue(band, out h)) h = 0;
                if (!wMap.TryGetValue(band, out w)) w = 0;
                string label = band + "–" + (band + BandSize - 1);
                sH.Points.AddXY(label, h);
                sW.Points.AddXY(label, w);

                // A band that can contain a below-legal-age spouse is coloured on BOTH series.
                if (band < LegalAge)
                {
                    ChartStyle.Highlight(sH, sH.Points.Count - 1, UnderAge);
                    ChartStyle.Highlight(sW, sW.Points.Count - 1, UnderAge);
                }
            }

            Chart.Series.Add(sH);
            Chart.Series.Add(sW);
            ChartStyle.Axes(Chart, "Age at marriage", "Spouses");
            ChartStyle.WholeNumberY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(hMap, wMap, pairs, cov));
        }

        private DataTable BandQuery(string column, DateRange range)
        {
            string c = AnalyticsData.Ident(column);
            return AnalyticsData.Query(
                "SELECT FLOOR(" + c + " / @band) * @band AS band, COUNT(*) AS n " +
                "FROM marriages " +
                "WHERE date_of_marriage IS NOT NULL AND date_of_marriage >= @from AND date_of_marriage < @to " +
                "  AND " + c + " IS NOT NULL AND " + c + " BETWEEN @lo AND @hi " +
                "GROUP BY band ORDER BY band", range,
                new MySql.Data.MySqlClient.MySqlParameter("@band", BandSize),
                new MySql.Data.MySqlClient.MySqlParameter("@lo", MinPlausible),
                new MySql.Data.MySqlClient.MySqlParameter("@hi", MaxPlausible));
        }

        private static Dictionary<int, int> Bands(DataTable dt)
        {
            var map = new Dictionary<int, int>();
            foreach (DataRow r in dt.Rows)
                map[AnalyticsData.Int(r, "band")] = AnalyticsData.Int(r, "n");
            return map;
        }

        /// <summary>Deterministic string.Format over the same bands and pairs the chart was built from.</summary>
        private string BuildCaption(Dictionary<int, int> hMap, Dictionary<int, int> wMap,
                                    DataTable pairs, AnalyticsData.Coverage cov)
        {
            int hTotal = 0, wTotal = 0;
            double hSum = 0, wSum = 0;
            foreach (KeyValuePair<int, int> kv in hMap) { hTotal += kv.Value; hSum += (kv.Key + BandSize / 2.0) * kv.Value; }
            foreach (KeyValuePair<int, int> kv in wMap) { wTotal += kv.Value; wSum += (kv.Key + BandSize / 2.0) * kv.Value; }

            int underAge = 0, gapCount = 0, widestGap = 0;
            double gapSum = 0;
            foreach (DataRow r in pairs.Rows)
            {
                int h = AnalyticsData.Int(r, "h");
                int w = AnalyticsData.Int(r, "w");
                if ((h > 0 && h < LegalAge) || (w > 0 && w < LegalAge)) underAge++;
                if (h >= MinPlausible && w >= MinPlausible && h <= MaxPlausible && w <= MaxPlausible)
                {
                    int gap = Math.Abs(h - w);
                    gapSum += gap;
                    gapCount++;
                    if (gap > widestGap) widestGap = gap;
                }
            }

            // Stated first when it applies — it is the only thing on this chart that needs acting on.
            string flag = underAge == 0
                ? ""
                : string.Format("{0} record{1} show a spouse under {2}, which RA 11596 makes void — check {3}. ",
                    underAge, underAge == 1 ? "" : "s", LegalAge, underAge == 1 ? "it" : "them");

            string coverage = cov.Partial ? " " + cov.PartialNote : "";
            int couples = pairs.Rows.Count;

            if (couples < 3)
            {
                return flag + string.Format("{0} marriage{1} with both ages recorded. Too few to describe a profile.{2}",
                    couples, couples == 1 ? "" : "s", coverage);
            }

            string gapPhrase = gapCount == 0
                ? ""
                : string.Format(" Mean age gap is {0:0.#} years, widest {1}.", gapSum / gapCount, widestGap);

            return flag + string.Format("Husbands average about {0:0.#}, wives about {1:0.#}, over {2} marriage{3}.{4}{5}",
                hTotal == 0 ? 0 : hSum / hTotal,
                wTotal == 0 ? 0 : wSum / wTotal,
                couples, couples == 1 ? "" : "s", gapPhrase, coverage);
        }
    }
}
