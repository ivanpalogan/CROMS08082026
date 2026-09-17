using System;
using System.Data;
using CROMS.Analytics.Widgets;
using CROMS.Data;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Marriage tab. The registry currently holds ONE marriage, so every widget here is
    /// expected to show its sparse or empty state — which is the point: the tab is built
    /// correctly now so it fills itself as the office registers marriages, rather than being
    /// bolted on later against whatever data happens to exist then.
    /// </summary>
    public class MarriageAnalyticsTab : AnalyticsTab
    {
        private SummaryCard _cardTotal;
        private SummaryCard _cardRegMonth;
        private SummaryCard _cardOccYear;
        private SummaryCard _cardLicences;

        protected override void Build()
        {
            _cardTotal    = AddCard("TOTAL REGISTERED\r\nall time", ChartStyle.Colour(0));
            _cardRegMonth = AddCard("REGISTERED THIS MONTH\r\nby date registered", ChartStyle.Colour(0));
            _cardOccYear  = AddCard("OCCURRED THIS YEAR\r\nby date of marriage", ChartStyle.Colour(1));
            _cardLicences = AddCard("LICENCES vs MARRIAGES\r\nthis year", ChartStyle.Colour(3));

            AddWidget(new RegistrationTrendWidget(
                "marriages", "date_of_marriage",
                "Marriages registered vs marriages occurred", "date of marriage",
                "marriage", "marriages"));
            AddWidget(new MonthYearHeatmapWidget(
                "marriages", "date_of_marriage",
                "Seasonality of marriages", "date of marriage (date_of_marriage)",
                "marriage", "marriages"));
            AddWidget(new MarriageAgeWidget());
            AddWidget(new MarriageCeremonyWidget());
            AddWidget(new MarriageLicenceWidget());

            AddReportButton("Print Assessment Report", (s, e) =>
            {
                DataTable t = AssessmentReport.BuildTable(AssessmentReport.Marriage, DateTime.Today.Year);
                AssessmentReport.Show(AssessmentReport.Marriage, t, FindForm());
            });
        }

        protected override void LoadCards()
        {
            int total = AnalyticsData.Scalar("SELECT COUNT(*) FROM marriages");
            _cardTotal.SetNoComparison(total.ToString("N0"),
                total == 0 ? "No marriages on record yet" : "every marriage in the registry");

            int regThis = AnalyticsData.CountInRange("marriages", "created_at", DateRange.ThisMonth());
            int regLast = AnalyticsData.CountInRange("marriages", "created_at", DateRange.LastMonth());
            _cardRegMonth.Set(regThis.ToString("N0"),
                AnalyticsData.Compare(regThis, regLast, "last month"));

            // Marriages are counted per YEAR here, not per month: a small office registers a
            // handful a year, so a month-on-month percentage would swing on a single record.
            int occThis = AnalyticsData.CountInRange("marriages", "date_of_marriage", DateRange.ThisYear());
            int occLast = AnalyticsData.CountInRange("marriages", "date_of_marriage", DateRange.LastYear());

            // A year-over-year line needs TWO years that actually hold records. Counting
            // distinct years is the honest test: comparing this year's 0 against last year's
            // 1 reads as a "-100% collapse" when the registry has only ever seen one year.
            int yearsWithData = AnalyticsData.Scalar(
                "SELECT COUNT(DISTINCT YEAR(date_of_marriage)) FROM marriages WHERE date_of_marriage IS NOT NULL");

            if (yearsWithData < 2)
            {
                _cardOccYear.SetNoComparison(occThis.ToString("N0"),
                    "first year of data — no year-over-year yet");
            }
            else
            {
                _cardOccYear.Set(occThis.ToString("N0"),
                    AnalyticsData.Compare(occThis, occLast, "last year"));
            }

            // Licences issued against marriages registered, both this year. Stated as a pair
            // rather than a ratio: with a handful of records a ratio reads as precision the
            // numbers do not have.
            int licences = AnalyticsData.CountInRange("marriage_licenses", "filed_date", DateRange.ThisYear());
            int marriages = AnalyticsData.CountInRange("marriages", "created_at", DateRange.ThisYear());

            _cardLicences.SetNoComparison(licences + " / " + marriages,
                licences == 0 && marriages == 0
                    ? "no licences filed or marriages registered yet"
                    : "licences filed / marriages registered");
        }
    }
}
