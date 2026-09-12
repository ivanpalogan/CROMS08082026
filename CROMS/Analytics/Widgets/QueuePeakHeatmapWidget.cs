using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// When clients actually arrive: weekday down the side, hour of day across, each cell
    /// shaded by how many tickets were issued in that slot. This is the chart that decides
    /// how many windows to open and when to take lunch.
    ///
    /// Counted by ticket creation, which on this system is the moment the kiosk issues the
    /// number — the client's real arrival time, not when a window got to them.
    ///
    /// The hour columns are derived from the DATA, not hard-coded to office hours: a fixed
    /// 8–17 grid would silently hide tickets issued outside it, and hiding rows is exactly
    /// what a workload chart must not do.
    /// </summary>
    public class QueuePeakHeatmapWidget : AnalyticsWidget
    {
        // Monday-first, matching how the office reads a week (and DateRange.ThisWeek).
        private static readonly string[] DayNames =
        { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        private DataGridView _grid;

        public QueuePeakHeatmapWidget()
            : base("Peak day and hour", "by ticket issued (queue_tickets.created_at)")
        {
        }

        protected override void Render(DateRange range)
        {
            // MySQL DAYOFWEEK() is 1=Sunday..7=Saturday; WEEKDAY() is 0=Monday..6=Sunday,
            // which is the order this grid uses, so WEEKDAY is what the query returns.
            DataTable dt = AnalyticsData.Query(
                "SELECT WEEKDAY(created_at) AS d, HOUR(created_at) AS h, COUNT(*) AS n " +
                "FROM queue_tickets " +
                "WHERE created_at >= @from AND created_at < @to " +
                "GROUP BY d, h ORDER BY d, h", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No queue tickets issued in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            int minHour = 23, maxHour = 0, total = 0, max = 0;
            var cells = new Dictionary<int, Dictionary<int, int>>();
            var dayTotals = new int[7];

            for (int d = 0; d < 7; d++) cells[d] = new Dictionary<int, int>();

            foreach (DataRow r in dt.Rows)
            {
                int d = AnalyticsData.Int(r, "d");
                int h = AnalyticsData.Int(r, "h");
                int n = AnalyticsData.Int(r, "n");
                if (d < 0 || d > 6 || h < 0 || h > 23) continue;

                if (!cells[d].ContainsKey(h)) cells[d][h] = 0;
                cells[d][h] += n;
                dayTotals[d] += n;
                total += n;
                if (cells[d][h] > max) max = cells[d][h];
                if (h < minHour) minHour = h;
                if (h > maxHour) maxHour = h;
            }

            if (total == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows, "No queue tickets issued in this period.", null);
                return;
            }

            BuildGrid(cells, minHour, maxHour, max);
            SetCaption(BuildCaption(cells, dayTotals, total, minHour, maxHour));
        }

        private void BuildGrid(Dictionary<int, Dictionary<int, int>> cells,
                               int minHour, int maxHour, int max)
        {
            if (_grid == null)
            {
                _grid = HeatmapGrid.Create();
                SetBody(_grid);
            }

            _grid.Columns.Clear();
            _grid.Rows.Clear();

            _grid.Columns.Add("day", "");
            // Fixed width, not Fill: with a dozen-plus value columns the Fill share
            // left the label column too narrow and clipped the names to "M.." / "T..".
            // Frozen so the labels stay put when a wide grid scrolls sideways.
            _grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _grid.Columns[0].Width = 52;
            _grid.Columns[0].Frozen = true;
            for (int h = minHour; h <= maxHour; h++)
                _grid.Columns.Add("h" + h, HourLabel(h));

            for (int d = 0; d < 7; d++)
            {
                int idx = _grid.Rows.Add();
                DataGridViewRow row = _grid.Rows[idx];
                row.Cells[0].Value = DayNames[d];
                HeatmapGrid.PaintLabel(row.Cells[0]);

                for (int h = minHour; h <= maxHour; h++)
                {
                    int n;
                    if (!cells[d].TryGetValue(h, out n)) n = 0;
                    DataGridViewCell cell = row.Cells[h - minHour + 1];
                    cell.Value = n == 0 ? "" : n.ToString();
                    HeatmapGrid.Paint(cell, n, max);
                }
            }
            _grid.ClearSelection();
        }

        /// <summary>Compact 12-hour label so a wide grid stays readable: 8a, 12n, 3p.</summary>
        private static string HourLabel(int h)
        {
            if (h == 0) return "12m";
            if (h == 12) return "12n";
            return (h % 12) + (h < 12 ? "a" : "p");
        }

        /// <summary>Deterministic string.Format over the same cell counts the grid was built from.</summary>
        private string BuildCaption(Dictionary<int, Dictionary<int, int>> cells,
                                    int[] dayTotals, int total, int minHour, int maxHour)
        {
            if (total < 3)
            {
                return string.Format("{0} ticket{1} in this period — too few to show a pattern.",
                    total, total == 1 ? "" : "s");
            }

            // Busiest single slot.
            int bestDay = 0, bestHour = minHour, bestSlot = 0;
            for (int d = 0; d < 7; d++)
                foreach (KeyValuePair<int, int> kv in cells[d])
                    if (kv.Value > bestSlot) { bestSlot = kv.Value; bestDay = d; bestHour = kv.Key; }

            // Busiest day, and how far above the average of the days that saw any clients.
            int busiestDay = 0, busiestDayCount = 0, activeDays = 0;
            for (int d = 0; d < 7; d++)
            {
                if (dayTotals[d] > 0) activeDays++;
                if (dayTotals[d] > busiestDayCount) { busiestDayCount = dayTotals[d]; busiestDay = d; }
            }

            string dayPhrase;
            if (activeDays < 2)
            {
                dayPhrase = string.Format("All {0} tickets fall on {1}", total, DayNames[busiestDay]);
            }
            else
            {
                double average = (double)total / activeDays;
                dayPhrase = string.Format("Peak day is {0} with {1} client{2}, {3:0.#}% above the {4:0.#} averaged over the {5} days that saw any",
                    DayNames[busiestDay], busiestDayCount, busiestDayCount == 1 ? "" : "s",
                    AnalyticsData.Pct(busiestDayCount - average, average), average, activeDays);
            }

            return string.Format("{0}. Busiest single hour is {1} {2} with {3}. Tickets span {4} to {5}.",
                dayPhrase, DayNames[bestDay], HourLabel(bestHour), bestSlot,
                HourLabel(minHour), HourLabel(maxHour));
        }
    }
}
