using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// Heat maps in this module are DataGridViews with per-cell background colours, not a
    /// charting control — the charting library has no heat-map type, and a grid keeps the
    /// exact counts readable in the cells, which matters on a registry where a month may
    /// hold two records and a colour alone would say almost nothing.
    ///
    /// Note UiTheme.Polish also styles every DataGridView in the app; these grids are built
    /// after that pass runs on a module form, but <see cref="Create"/> re-asserts the
    /// settings a heat map needs (no zebra striping, no selection tint) because a striped or
    /// highlighted row would be read as data.
    /// </summary>
    public static class HeatmapGrid
    {
        /// <summary>Colour of a cell holding nothing — distinct from the palest real value.</summary>
        public static readonly Color EmptyCell = Color.FromArgb(249, 250, 252);

        /// <summary>Builds an empty grid ready to take a heat-map table.</summary>
        public static DataGridView Create()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = UiTheme.Surface,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                MultiSelect = false,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                GridColor = Color.FromArgb(238, 241, 246)
            };

            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
            g.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Muted;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 251);

            g.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            g.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.DefaultCellStyle.Padding = new Padding(0);

            // A heat map's colour IS the data. Zebra striping and a selection tint would both
            // repaint cells with a colour that means nothing, so they are switched off.
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.Empty;
            g.RowTemplate.Height = 26;

            return g;
        }

        /// <summary>
        /// Paints one cell for <paramref name="value"/> on a 0..<paramref name="max"/> scale:
        /// white through the accent blue, with the text flipping to white once the background
        /// is dark enough to need it. A zero cell gets <see cref="EmptyCell"/> so "none" is
        /// visibly different from "the smallest count here".
        /// </summary>
        public static void Paint(DataGridViewCell cell, double value, double max)
        {
            if (value <= 0)
            {
                cell.Style.BackColor = EmptyCell;
                cell.Style.ForeColor = UiTheme.Faint;
                cell.Style.SelectionBackColor = EmptyCell;
                cell.Style.SelectionForeColor = UiTheme.Faint;
                return;
            }

            double t = max <= 0 ? 0 : value / max;
            if (t > 1) t = 1;

            // Floor the ramp at 0.15 so the smallest non-zero count still reads as coloured
            // rather than as an empty cell.
            t = 0.15 + t * 0.85;

            Color c = Blend(Color.White, UiTheme.Accent, t);
            cell.Style.BackColor = c;
            cell.Style.SelectionBackColor = c;

            Color ink = t > 0.55 ? Color.White : UiTheme.Ink;
            cell.Style.ForeColor = ink;
            cell.Style.SelectionForeColor = ink;
        }

        /// <summary>Styles the frozen left-hand label column (year / weekday).</summary>
        public static void PaintLabel(DataGridViewCell cell)
        {
            cell.Style.BackColor = Color.FromArgb(248, 249, 251);
            cell.Style.SelectionBackColor = Color.FromArgb(248, 249, 251);
            cell.Style.ForeColor = UiTheme.Muted;
            cell.Style.SelectionForeColor = UiTheme.Muted;
            cell.Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        }

        private static Color Blend(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>Short month names in calendar order, used as heat-map column headers.</summary>
        public static readonly string[] Months =
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };
    }
}
