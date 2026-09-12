using System;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// One place that makes every chart in the app look the same. Uses
    /// System.Windows.Forms.DataVisualization.Charting, which ships with .NET Framework —
    /// no NuGet package is added for charting.
    /// </summary>
    public static class ChartStyle
    {
        /// <summary>
        /// Categorical series colours. Ordered so the first two are distinguishable for the
        /// most common pairing in this module (registered vs occurred, male vs female) and
        /// the set stays readable when a chart uses only three or four of them.
        /// </summary>
        public static readonly Color[] Palette =
        {
            Color.FromArgb(29, 78, 216),    // accent blue
            Color.FromArgb(217, 119, 6),    // amber
            Color.FromArgb(46, 148, 87),    // green
            Color.FromArgb(147, 51, 234),   // violet
            Color.FromArgb(198, 50, 63),    // red
            Color.FromArgb(13, 148, 136),   // teal
            Color.FromArgb(120, 113, 108),  // stone
            Color.FromArgb(37, 99, 235)     // secondary blue
        };

        public static Color Colour(int index)
        {
            if (index < 0) index = 0;
            return Palette[index % Palette.Length];
        }

        private static readonly Color GridLine = Color.FromArgb(235, 238, 243);
        private static readonly Color AxisInk = Color.FromArgb(91, 100, 114);

        /// <summary>Builds a chart with a single styled plot area, ready for series.</summary>
        public static Chart Create()
        {
            var chart = new Chart
            {
                BackColor = UiTheme.Surface,
                BorderlineWidth = 0,
                AntiAliasing = AntiAliasingStyles.All,
                TextAntiAliasingQuality = TextAntiAliasingQuality.High
            };

            var area = new ChartArea("main")
            {
                BackColor = UiTheme.Surface
            };
            StyleAxis(area.AxisX);
            StyleAxis(area.AxisY);
            area.AxisX.MajorGrid.Enabled = false;             // vertical rules add noise, not meaning
            area.AxisY.MajorGrid.LineColor = GridLine;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 0;
            area.InnerPlotPosition = new ElementPosition(0, 0, 100, 100) { Auto = true };
            chart.ChartAreas.Add(area);

            var legend = new Legend("legend")
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                BackColor = Color.Transparent,
                BorderWidth = 0,
                Font = new Font("Segoe UI", 8F),
                ForeColor = AxisInk,
                Enabled = false                                // series-count decides; see ShowLegend
            };
            chart.Legends.Add(legend);

            return chart;
        }

        private static void StyleAxis(Axis axis)
        {
            axis.LineColor = GridLine;
            axis.MajorTickMark.Enabled = false;
            axis.LabelStyle.Font = new Font("Segoe UI", 8F);
            axis.LabelStyle.ForeColor = AxisInk;
            axis.TitleFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            axis.TitleForeColor = AxisInk;
            axis.IsMarginVisible = true;
        }

        /// <summary>Turns the legend on. Charts with one series do not need one.</summary>
        public static void ShowLegend(Chart chart, bool show)
        {
            if (chart.Legends.Count > 0) chart.Legends[0].Enabled = show;
        }

        /// <summary>
        /// Axis titles are mandatory in this module: every registry chart must say on its own
        /// face whether it counts by REGISTRATION date or by EVENT date, so a screenshot taken
        /// out of context cannot be misread.
        /// </summary>
        public static void Axes(Chart chart, string xTitle, string yTitle)
        {
            ChartArea a = chart.ChartAreas[0];
            a.AxisX.Title = xTitle ?? "";
            a.AxisY.Title = yTitle ?? "";
        }

        /// <summary>
        /// Forces whole-number Y ticks — half a birth is not a thing.
        ///
        /// Call AFTER the series are added: on a chart whose largest value is 1 or 2 the
        /// auto interval lands on fractions, and formatting those as "0" renders an axis
        /// reading 1, 1, 0, 0, 0 — every label a lie. So the interval is pinned to 1 (or a
        /// whole step) whenever the range is small enough for that to matter.
        /// </summary>
        public static void WholeNumberY(Chart chart)
        {
            ChartArea a = chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "0";

            double max = MaxPlotted(chart);
            if (max <= 6)
            {
                a.AxisY.Interval = 1;
                a.AxisY.Maximum = Math.Max(1, Math.Ceiling(max));
            }
            else
            {
                a.AxisY.Interval = 0;
                a.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            }
            a.RecalculateAxesScale();
        }

        /// <summary>
        /// Largest Y value the chart will draw. Stacked series are summed per category —
        /// on a stacked column the axis has to reach the top of the STACK, not the tallest
        /// single segment.
        /// </summary>
        private static double MaxPlotted(Chart chart)
        {
            bool stacked = false;
            foreach (Series s in chart.Series)
                if (s.ChartType == SeriesChartType.StackedColumn ||
                    s.ChartType == SeriesChartType.StackedArea) { stacked = true; break; }

            double max = 0;
            if (stacked)
            {
                int points = 0;
                foreach (Series s in chart.Series)
                    if (s.Points.Count > points) points = s.Points.Count;

                for (int i = 0; i < points; i++)
                {
                    double sum = 0;
                    foreach (Series s in chart.Series)
                        if (i < s.Points.Count && s.Points[i].YValues.Length > 0)
                            sum += s.Points[i].YValues[0];
                    if (sum > max) max = sum;
                }
            }
            else
            {
                foreach (Series s in chart.Series)
                    foreach (DataPoint p in s.Points)
                        if (p.YValues.Length > 0 && p.YValues[0] > max) max = p.YValues[0];
            }
            return max;
        }

        /// <summary>Y axis as 0-100% — used by every 100% stacked column in this module.</summary>
        public static void PercentY(Chart chart)
        {
            ChartArea a = chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.Maximum = 100;
            a.AxisY.Interval = 25;
            a.AxisY.LabelStyle.Format = "0'%'";
        }

        /// <summary>Rotates X labels when the category names are long enough to collide.</summary>
        public static void AngleXLabels(Chart chart, int angle)
        {
            ChartArea a = chart.ChartAreas[0];
            a.AxisX.LabelStyle.Angle = angle;
            a.AxisX.Interval = 1;
            a.AxisX.LabelStyle.IsStaggered = false;
        }

        /// <summary>A line series with points marked, for trend charts.</summary>
        public static Series Line(string name, int colourIndex)
        {
            return new Series(name)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Colour(colourIndex),
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 7,
                MarkerColor = Colour(colourIndex),
                ChartArea = "main",
                Legend = "legend",
                IsVisibleInLegend = true
            };
        }

        /// <summary>A plain column series.</summary>
        public static Series Column(string name, int colourIndex)
        {
            return new Series(name)
            {
                ChartType = SeriesChartType.Column,
                Color = Colour(colourIndex),
                BorderWidth = 0,
                ChartArea = "main",
                Legend = "legend",
                IsVisibleInLegend = true,
                ["PointWidth"] = "0.55"
            };
        }

        /// <summary>A horizontal bar series (top-N rankings read better this way).</summary>
        public static Series Bar(string name, int colourIndex)
        {
            return new Series(name)
            {
                ChartType = SeriesChartType.Bar,
                Color = Colour(colourIndex),
                BorderWidth = 0,
                ChartArea = "main",
                Legend = "legend",
                IsVisibleInLegend = true,
                ["PointWidth"] = "0.6"
            };
        }

        /// <summary>A stacked column series; all members of one stack share a StackGroup.</summary>
        public static Series StackedColumn(string name, int colourIndex, bool hundredPercent)
        {
            return new Series(name)
            {
                ChartType = hundredPercent
                    ? SeriesChartType.StackedColumn100
                    : SeriesChartType.StackedColumn,
                Color = Colour(colourIndex),
                BorderWidth = 0,
                ChartArea = "main",
                Legend = "legend",
                IsVisibleInLegend = true,
                ["PointWidth"] = "0.55"
            };
        }

        /// <summary>A stacked-area series, used for demand-over-time splits.</summary>
        public static Series StackedArea(string name, int colourIndex)
        {
            return new Series(name)
            {
                ChartType = SeriesChartType.StackedArea,
                Color = Color.FromArgb(190, Colour(colourIndex)),
                BorderColor = Colour(colourIndex),
                BorderWidth = 2,
                ChartArea = "main",
                Legend = "legend",
                IsVisibleInLegend = true
            };
        }

        /// <summary>
        /// Highlights one point in a series by recolouring it — used where the spec calls for
        /// a band to stand out (e.g. mothers under 20) without adding a second series that
        /// would appear in the legend as if it were a separate measure.
        /// </summary>
        public static void Highlight(Series s, int pointIndex, Color colour)
        {
            if (pointIndex >= 0 && pointIndex < s.Points.Count)
                s.Points[pointIndex].Color = colour;
        }
    }
}
