using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Church against civil ceremonies, as a 100% stacked column per year.
    ///
    /// HOW "CIVIL" IS DECIDED, because it is an inference and the caption says so. The schema
    /// has no ceremony-type column; the spec's rule is `church_id IS NULL` means civil. That is
    /// a reasonable reading but not a certain one — a church wedding whose church was simply
    /// never picked from the lookup also lands there. So the solemnizing officer is
    /// cross-referenced: where `church_id` is NULL but the officer reads as clergy
    /// (Rev/Fr/Pastor/Imam/Bishop/Minister), the record is reported as UNCLEAR rather than
    /// silently counted as civil, and the caption gives that count. Better an honest third
    /// category than a clean two-way split that is quietly wrong.
    /// </summary>
    public class MarriageCeremonyWidget : AnalyticsWidget
    {
        // Titles that indicate a religious solemnizing officer. Matched case-insensitively as
        // whole-ish tokens; a civil officer reads "Judge", "Mayor", "Municipal Trial Court".
        private static readonly string[] ClergyMarkers =
        {
            "rev.", "reverend", "fr.", "father ", "pastor", "bishop", "imam",
            "minister", "ptr.", "pr.", "sis.", "bro.", "deacon"
        };

        public MarriageCeremonyWidget()
            : base("Church vs civil ceremonies",
                   "by date of marriage (date_of_marriage) · civil inferred from church_id + solemnizer")
        {
        }

        protected override void Render(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT YEAR(date_of_marriage) AS y, " +
                "       CASE WHEN church_id IS NOT NULL AND church_id > 0 THEN 1 ELSE 0 END AS has_church, " +
                "       COALESCE(solemnizer, '') AS officer " +
                "FROM marriages " +
                "WHERE date_of_marriage IS NOT NULL AND date_of_marriage >= @from AND date_of_marriage < @to " +
                "ORDER BY y", range);

            if (dt.Rows.Count == 0)
            {
                int noDate = AnalyticsData.ScalarInRange(
                    "SELECT COUNT(*) FROM marriages " +
                    "WHERE date_of_marriage IS NULL AND created_at >= @from AND created_at < @to", range);

                if (noDate > 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — date_of_marriage is unfilled in " + noDate + " of " + noDate + " records.",
                        "Without a date of marriage the record cannot be placed in a year.");
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No marriages with a date of marriage in this period.", null);
                }
                return;
            }

            var years = new List<int>();
            var church = new Dictionary<int, int>();
            var civil = new Dictionary<int, int>();
            var unclear = new Dictionary<int, int>();
            int tChurch = 0, tCivil = 0, tUnclear = 0;

            foreach (DataRow r in dt.Rows)
            {
                int y = AnalyticsData.Int(r, "y");
                if (!church.ContainsKey(y)) { church[y] = 0; civil[y] = 0; unclear[y] = 0; years.Add(y); }

                bool hasChurch = AnalyticsData.Int(r, "has_church") == 1;
                string officer = AnalyticsData.Str(r, "officer");

                if (hasChurch) { church[y]++; tChurch++; }
                else if (LooksLikeClergy(officer)) { unclear[y]++; tUnclear++; }
                else { civil[y]++; tCivil++; }
            }
            years.Sort();

            ResetChart();
            Series sChurch = ChartStyle.StackedColumn("Church", 0, true);
            Series sCivil = ChartStyle.StackedColumn("Civil", 2, true);
            Series sUnclear = ChartStyle.StackedColumn("Unclear", 6, true);

            foreach (int y in years)
            {
                string label = y.ToString();
                sChurch.Points.AddXY(label, church[y]);
                sCivil.Points.AddXY(label, civil[y]);
                sUnclear.Points.AddXY(label, unclear[y]);
            }

            Chart.Series.Add(sChurch);
            Chart.Series.Add(sCivil);
            if (tUnclear > 0) Chart.Series.Add(sUnclear);   // only shown when it actually occurs

            ChartStyle.Axes(Chart, "Year of marriage", "Share of marriages");
            ChartStyle.PercentY(Chart);
            AutoLegend();

            SetCaption(BuildCaption(tChurch, tCivil, tUnclear));
        }

        private static bool LooksLikeClergy(string officer)
        {
            if (officer.Length == 0) return false;
            string s = officer.ToLowerInvariant();
            foreach (string m in ClergyMarkers)
                if (s.IndexOf(m, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>Deterministic string.Format over the same counts the columns were built from.</summary>
        private string BuildCaption(int church, int civil, int unclear)
        {
            int total = church + civil + unclear;

            string unclearNote = unclear == 0
                ? ""
                : string.Format(" {0} record{1} have no church but a clergy solemnizer, so the ceremony type is unclear — pick the church on those records to resolve it.",
                    unclear, unclear == 1 ? "" : "s");

            if (total < 3)
            {
                return string.Format("{0} marriage{1} in this period — {2} church, {3} civil. Too few to state a split.{4}",
                    total, total == 1 ? "" : "s", church, civil, unclearNote);
            }

            string lead = church >= civil
                ? string.Format("Church ceremonies lead with {0} of {1} ({2:0.#}%)", church, total, AnalyticsData.Pct(church, total))
                : string.Format("Civil ceremonies lead with {0} of {1} ({2:0.#}%)", civil, total, AnalyticsData.Pct(civil, total));

            return lead + "." + unclearNote;
        }
    }
}
