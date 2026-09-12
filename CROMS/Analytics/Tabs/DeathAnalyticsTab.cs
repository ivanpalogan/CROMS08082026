using System;
using CROMS.Analytics.Widgets;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Death tab — same structure as Birth: cards on fixed periods, charts on the range control.
    /// The trend chart and the seasonality heat map are the SHARED implementations, configured
    /// for `deaths` / `date_of_death`; only the pyramid, causes and disposal charts are specific
    /// to this domain.
    /// </summary>
    public class DeathAnalyticsTab : AnalyticsTab
    {
        private SummaryCard _cardTotal;
        private SummaryCard _cardRegMonth;
        private SummaryCard _cardOccMonth;
        private SummaryCard _cardAvgAge;

        protected override void Build()
        {
            _cardTotal    = AddCard("TOTAL REGISTERED\r\nall time", ChartStyle.Colour(0));
            _cardRegMonth = AddCard("REGISTERED THIS MONTH\r\nby date registered", ChartStyle.Colour(0));
            _cardOccMonth = AddCard("OCCURRED THIS MONTH\r\nby date of death", ChartStyle.Colour(1));
            _cardAvgAge   = AddCard("AVERAGE AGE AT DEATH\r\nall records with an age", ChartStyle.Colour(4));

            AddWidget(new RegistrationTrendWidget(
                "deaths", "date_of_death",
                "Deaths registered vs deaths occurred", "date of death",
                "death", "deaths"));
            AddWidget(new DeathPyramidWidget());
            AddWidget(new DeathCausesWidget());
            AddWidget(new MonthYearHeatmapWidget(
                "deaths", "date_of_death",
                "Season of death", "date of death (date_of_death)",
                "death", "deaths"));
            AddWidget(new DeathDisposalWidget());
        }

        protected override void LoadCards()
        {
            int total = AnalyticsData.Scalar("SELECT COUNT(*) FROM deaths");
            _cardTotal.SetNoComparison(total.ToString("N0"),
                total == 0 ? "No deaths on record yet" : "every death in the registry");

            int regThis = AnalyticsData.CountInRange("deaths", "created_at", DateRange.ThisMonth());
            int regLast = AnalyticsData.CountInRange("deaths", "created_at", DateRange.LastMonth());
            _cardRegMonth.Set(regThis.ToString("N0"),
                AnalyticsData.Compare(regThis, regLast, "last month"));

            int occThis = AnalyticsData.CountInRange("deaths", "date_of_death", DateRange.ThisMonth());
            int occLast = AnalyticsData.CountInRange("deaths", "date_of_death", DateRange.LastMonth());
            _cardOccMonth.Set(occThis.ToString("N0"),
                AnalyticsData.Compare(occThis, occLast, "last month"));

            // Average age is a whole-registry figure, not a this-month one: on a small office a
            // single month may hold no deaths at all, and a mean over one record is not a mean.
            // Ages outside 0-130 are excluded, since a single-digit OCR misread would drag it.
            double? mean = AnalyticsData.ScalarDbl(
                "SELECT AVG(age) FROM deaths WHERE age IS NOT NULL AND age BETWEEN 0 AND 130");
            int counted = AnalyticsData.Scalar(
                "SELECT COUNT(*) FROM deaths WHERE age IS NOT NULL AND age BETWEEN 0 AND 130");

            if (!mean.HasValue || counted == 0)
            {
                _cardAvgAge.SetUnavailable("No age recorded on any death");
            }
            else if (counted < 3)
            {
                _cardAvgAge.SetNoComparison(mean.Value.ToString("0.#"),
                    "from only " + counted + " record" + (counted == 1 ? "" : "s"));
            }
            else
            {
                _cardAvgAge.SetNoComparison(mean.Value.ToString("0.#"),
                    "across " + counted + " records with an age");
            }
        }
    }
}
