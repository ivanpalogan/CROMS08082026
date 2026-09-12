using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Seasonality: months across, years down, each cell shaded by how many events fell in
    /// that month. Built from a DataGridView with per-cell background colours, not a chart.
    ///
    /// ONE implementation serves the Death tab's "Season of death" and the Marriage tab's
    /// "Seasonality" — same picture, different table and event column.
    ///
    /// Counted by the EVENT date, never by registration: a run of delayed registrations
    /// entered in one afternoon would otherwise paint a hot month that nothing actually
    /// happened in.
    /// </summary>
    public class MonthYearHeatmapWidget : AnalyticsWidget
    {
        private readonly string _table;
        private readonly string _dateColumn;
        private readonly string _noun;
        private readonly string _nounPlural;
        private DataGridView _grid;

        public MonthYearHeatmapWidget(string table, string dateColumn,
                                      string title, string eventLabel,
                                      string noun, string nounPlural)
            : base(title, "by " + eventLabel + " — cells shaded by count")
        {
            _table = AnalyticsData.Ident(table);
            _dateColumn = AnalyticsData.Ident(dateColumn);
            _noun = noun;
            _nounPlural = nounPlural;
        }

        protected override void Render(DateRange range)
        {
            AnalyticsData.Coverage cov =
                AnalyticsData.GetCoverage(_table, _dateColumn, "created_at", range);

            if (cov.Total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No " + _nounPlural + " registered in this period.", null);
                return;
            }
            if (cov.None)
            {
                ShowEmpty(cov, "Without an event date there is no month to place the record in.");
                return;
            }

            DataTable dt = AnalyticsData.Query(
                "SELECT YEAR(" + _dateColumn + ") AS y, MONTH(" + _dateColumn + ") AS m, COUNT(*) AS n " +
                "FROM " + _table + " " +
                "WHERE " + _dateColumn + " IS NOT NULL AND " +
                _dateColumn + " >= @from AND " + _dateColumn + " < @to " +
                "GROUP BY y, m ORDER BY y, m", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No " + _nounPlural + " with an event date inside this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            // Pivot year -> 12 months.
            var years = new List<int>();
            var cells = new Dictionary<int, int[]>();
            var monthTotals = new int[12];
            int total = 0, max = 0;

            foreach (DataRow r in dt.Rows)
            {
                int y = AnalyticsData.Int(r, "y");
                int m = AnalyticsData.Int(r, "m");
                int n = AnalyticsData.Int(r, "n");
                if (m < 1 || m > 12) continue;

                if (!cells.ContainsKey(y)) { cells[y] = new int[12]; years.Add(y); }
                cells[y][m - 1] += n;
                monthTotals[m - 1] += n;
                total += n;
                if (cells[y][m - 1] > max) max = cells[y][m - 1];
            }
            years.Sort();

            BuildGrid(years, cells, max);
            SetCaption(BuildCaption(total, monthTotals, years.Count, cov));
        }

        private void BuildGrid(List<int> years, Dictionary<int, int[]> cells, int max)
        {
            if (_grid == null)
            {
                _grid = HeatmapGrid.Create();
                SetBody(_grid);
            }

            _grid.Columns.Clear();
            _grid.Rows.Clear();

            _grid.Columns.Add("year", "");
            // Fixed width, not Fill: with a dozen-plus value columns the Fill share
            // left the label column too narrow and clipped the names to "M.." / "T..".
            // Frozen so the labels stay put when a wide grid scrolls sideways.
            _grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _grid.Columns[0].Width = 52;
            _grid.Columns[0].Frozen = true;
            foreach (string m in HeatmapGrid.Months)
                _grid.Columns.Add("m" + m, m);

            foreach (int y in years)
            {
                int idx = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[idx];
                row.Cells[0].Value = y.ToString();
                HeatmapGrid.PaintLabel(row.Cells[0]);

                for (int m = 0; m < 12; m++)
                {
                    int n = cells[y][m];
                    row.Cells[m + 1].Value = n == 0 ? "" : n.ToString();
                    HeatmapGrid.Paint(row.Cells[m + 1], n, max);
                }
            }
            _grid.ClearSelection();
        }

        /// <summary>Deterministic string.Format over the same month totals the cells were built from.</summary>
        private string BuildCaption(int total, int[] monthTotals, int yearCount,
                                    AnalyticsData.Coverage cov)
        {
            string coverage = cov.Partial ? " " + cov.PartialNote : "";

            if (total < 3)
            {
                return string.Format("{0} {1} in this period — too few to show a seasonal pattern.{2}",
                    total, total == 1 ? _noun : _nounPlural, coverage);
            }

            int peak = 0, peakMonth = 0, low = int.MaxValue, lowMonth = 0, monthsWithAny = 0;
            for (int m = 0; m < 12; m++)
            {
                if (monthTotals[m] > peak) { peak = monthTotals[m]; peakMonth = m; }
                if (monthTotals[m] > 0) monthsWithAny++;
                if (monthTotals[m] < low) { low = monthTotals[m]; lowMonth = m; }
            }

            // A "season" claim needs more than one year AND more than a couple of live months;
            // below that the shading is just where the few records happen to sit.
            if (yearCount < 2 || monthsWithAny < 4)
            {
                return string.Format(
                    "{0} {1} across {2} month{3} of {4} year{5}. Busiest month so far is {6} with {7}. Too little history to call it seasonal.{8}",
                    total, _nounPlural, monthsWithAny, monthsWithAny == 1 ? "" : "s",
                    yearCount, yearCount == 1 ? "" : "s",
                    HeatmapGrid.Months[peakMonth], peak, coverage);
            }

            double average = total / 12.0;
            return string.Format(
                "{0} {1} over {2} years. {3} is the busiest month with {4} ({5:0.#}% above the monthly average of {6:0.#}); {7} is the quietest with {8}.{9}",
                total, _nounPlural, yearCount,
                HeatmapGrid.Months[peakMonth], peak,
                AnalyticsData.Pct(peak - average, average), average,
                HeatmapGrid.Months[lowMonth], low, coverage);
        }
    }
}
