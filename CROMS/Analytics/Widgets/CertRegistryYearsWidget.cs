using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Which YEARS of the registry the public keeps asking for — the requested record's event
    /// year, not the year of the request. This is the digitisation priority list: the book the
    /// counter reaches for most is the one worth scanning first.
    ///
    /// SCOPE NOTE. The spec named births only. This joins all three registries (births,
    /// marriages, deaths) back through `certificate_requests.record_type` + `record_id`,
    /// because the office keeps a book per registry per year and the question — which book to
    /// digitise first — is the same for all three. Births will dominate on this data anyway;
    /// including the other two cannot hide a birth year, only add years that would otherwise be
    /// missing from the ranking.
    ///
    /// A request whose record_id points at nothing (record never picked, or the record was
    /// deleted) contributes no year and is counted in the caption instead of being dropped
    /// silently.
    /// </summary>
    public class CertRegistryYearsWidget : AnalyticsWidget
    {
        private const int TopN = 12;

        public CertRegistryYearsWidget()
            : base("Most requested registry years", "by the requested record's own event year")
        {
        }

        protected override void Render(DateRange range)
        {
            // One UNION arm per registry. record_id points at a different table depending on
            // record_type, so there is no single join that resolves it.
            DataTable dt = AnalyticsData.Query(
                "SELECT y, SUM(n) AS n FROM (" +
                "  SELECT YEAR(b.date_of_birth) AS y, COUNT(*) AS n " +
                "  FROM certificate_requests cr JOIN births b ON b.id = cr.record_id " +
                "  WHERE cr.record_type = 'Birth' AND b.date_of_birth IS NOT NULL " +
                "    AND cr.created_at >= @from AND cr.created_at < @to " +
                "  GROUP BY y " +
                "  UNION ALL " +
                "  SELECT YEAR(m.date_of_marriage) AS y, COUNT(*) AS n " +
                "  FROM certificate_requests cr JOIN marriages m ON m.id = cr.record_id " +
                "  WHERE cr.record_type = 'Marriage' AND m.date_of_marriage IS NOT NULL " +
                "    AND cr.created_at >= @from AND cr.created_at < @to " +
                "  GROUP BY y " +
                "  UNION ALL " +
                "  SELECT YEAR(d.date_of_death) AS y, COUNT(*) AS n " +
                "  FROM certificate_requests cr JOIN deaths d ON d.id = cr.record_id " +
                "  WHERE cr.record_type = 'Death' AND d.date_of_death IS NOT NULL " +
                "    AND cr.created_at >= @from AND cr.created_at < @to " +
                "  GROUP BY y" +
                ") AS years GROUP BY y ORDER BY n DESC, y DESC LIMIT " + TopN, range);

            int unresolved = AnalyticsData.ScalarInRange(
                "SELECT COUNT(*) FROM certificate_requests cr " +
                "LEFT JOIN births    b ON cr.record_type = 'Birth'    AND b.id = cr.record_id " +
                "LEFT JOIN marriages m ON cr.record_type = 'Marriage' AND m.id = cr.record_id " +
                "LEFT JOIN deaths    d ON cr.record_type = 'Death'    AND d.id = cr.record_id " +
                "WHERE cr.created_at >= @from AND cr.created_at < @to " +
                "  AND COALESCE(b.date_of_birth, m.date_of_marriage, d.date_of_death) IS NULL", range);

            if (dt.Rows.Count == 0)
            {
                int requests = AnalyticsData.CountInRange("certificate_requests", "created_at", range);
                if (requests == 0)
                {
                    ShowEmpty(EmptyStatePanel.Kind.NoRows,
                        "No certificate requests in this period.", null);
                }
                else
                {
                    ShowEmpty(EmptyStatePanel.Kind.Unfilled,
                        "No data yet — record_id resolves to no dated record in " +
                        unresolved + " of " + requests + " records.",
                        "A request has to be linked to a registry record before its year can be counted.");
                }
                return;
            }

            var years = new List<int>();
            var counts = new List<int>();
            int total = 0;
            foreach (DataRow r in dt.Rows)
            {
                years.Add(AnalyticsData.Int(r, "y"));
                int n = AnalyticsData.Int(r, "n");
                counts.Add(n);
                total += n;
            }

            ResetChart();
            Series s = ChartStyle.Bar("Requests", 0);
            // A Bar chart draws the first point at the bottom, so the list is reversed to put
            // the most-requested year at the top.
            for (int i = years.Count - 1; i >= 0; i--)
                s.Points.AddXY(years[i].ToString(), counts[i]);
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Registry year of the record", "Requests");
            ChartArea a = Chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "0";
            if (counts[0] <= 6) { a.AxisY.Interval = 1; a.AxisY.Maximum = Math.Max(1, counts[0]); }
            AutoLegend();

            SetCaption(BuildCaption(years, counts, total, unresolved));
        }

        /// <summary>Deterministic string.Format over the same year counts the bars were built from.</summary>
        private string BuildCaption(List<int> years, List<int> counts, int total, int unresolved)
        {
            string unlinked = unresolved == 0
                ? ""
                : string.Format(" {0} request{1} are not linked to a dated record and contribute no year.",
                    unresolved, unresolved == 1 ? "" : "s");

            if (total < 3)
            {
                return string.Format("{0} request{1} resolved to a registry year, led by {2}. Too few to set a digitisation priority.{3}",
                    total, total == 1 ? "" : "s", years[0], unlinked);
            }

            // The actionable sentence: which books to scan first.
            int topSlice = Math.Min(3, years.Count);
            var top = new List<string>();
            int covered = 0;
            for (int i = 0; i < topSlice; i++) { top.Add(years[i].ToString()); covered += counts[i]; }

            return string.Format(
                "{0} is the most requested year with {1} of {2} ({3:0.#}%). Digitise {4} first — those {5} year{6} cover {7:0.#}% of demand.{8}",
                years[0], counts[0], total, AnalyticsData.Pct(counts[0], total),
                string.Join(", ", top.ToArray()), topSlice, topSlice == 1 ? "" : "s",
                AnalyticsData.Pct(covered, total), unlinked);
        }
    }
}
