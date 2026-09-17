using System;
using System.Data;
using System.Drawing;
using CROMS.Analytics.Widgets;
using CROMS.Data;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Birth tab — the reference implementation the other domain tabs follow.
    ///
    /// Cards count by REGISTRATION date (created_at) because that is office workload, except
    /// the one that deliberately counts by EVENT date (date_of_birth) to show the population
    /// side. Each card names its own basis, so the pair cannot be misread as disagreeing.
    /// The date-range control below the cards affects the CHARTS only.
    /// </summary>
    public class BirthAnalyticsTab : AnalyticsTab
    {
        private SummaryCard _cardTotal;
        private SummaryCard _cardRegMonth;
        private SummaryCard _cardOccMonth;
        private SummaryCard _cardRegYear;

        protected override void Build()
        {
            _cardTotal    = AddCard("TOTAL REGISTERED\r\nall time", ChartStyle.Colour(0));
            _cardRegMonth = AddCard("REGISTERED THIS MONTH\r\nby date registered", ChartStyle.Colour(0));
            _cardOccMonth = AddCard("OCCURRED THIS MONTH\r\nby date of birth", ChartStyle.Colour(1));
            _cardRegYear  = AddCard("REGISTERED THIS YEAR\r\nby date registered", ChartStyle.Colour(2));

            AddWidget(new RegistrationTrendWidget(
                "births", "date_of_birth",
                "Births registered vs births occurred", "date of birth",
                "birth", "births"));
            AddWidget(new BirthSexWidget());
            AddWidget(new BirthLagWidget());
            AddWidget(new BirthAttendantWidget());
            AddWidget(new BirthMotherAgeWidget());

            AddReportButton("Print Assessment Report", (s, e) =>
            {
                DataTable t = AssessmentReport.BuildTable(AssessmentReport.Birth, DateTime.Today.Year);
                AssessmentReport.Show(AssessmentReport.Birth, t, FindForm());
            });
        }

        protected override void LoadCards()
        {
            // ---- total registered, all time (no comparison period exists by definition)
            int total = AnalyticsData.Scalar("SELECT COUNT(*) FROM births");
            _cardTotal.SetNoComparison(total.ToString("N0"),
                total == 0 ? "No births on record yet" : "every birth in the registry");

            // ---- registered this month vs last month (created_at)
            int regThis = AnalyticsData.CountInRange("births", "created_at", DateRange.ThisMonth());
            int regLast = AnalyticsData.CountInRange("births", "created_at", DateRange.LastMonth());
            _cardRegMonth.Set(regThis.ToString("N0"),
                AnalyticsData.Compare(regThis, regLast, "last month"));

            // ---- occurred this month vs last month (date_of_birth)
            int occThis = AnalyticsData.CountInRange("births", "date_of_birth", DateRange.ThisMonth());
            int occLast = AnalyticsData.CountInRange("births", "date_of_birth", DateRange.LastMonth());
            _cardOccMonth.Set(occThis.ToString("N0"),
                AnalyticsData.Compare(occThis, occLast, "last month"));

            // ---- registered this year vs last year (created_at)
            // A year-over-year line is only drawn where a previous year actually exists;
            // with one year of data the card states the single period plainly instead.
            int yearThis = AnalyticsData.CountInRange("births", "created_at", DateRange.ThisYear());
            int yearLast = AnalyticsData.CountInRange("births", "created_at", DateRange.LastYear());
            int earliestYear = AnalyticsData.Scalar("SELECT YEAR(MIN(created_at)) FROM births");

            if (earliestYear >= DateTime.Today.Year || yearLast == 0)
            {
                _cardRegYear.SetNoComparison(yearThis.ToString("N0"),
                    "first year of data — no year-over-year yet");
            }
            else
            {
                _cardRegYear.Set(yearThis.ToString("N0"),
                    AnalyticsData.Compare(yearThis, yearLast, "last year"));
            }
        }
    }
}
