using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// Base for every chart in the module. Gives each widget the same frame — title,
    /// an explicit "which date does this count?" basis line, a body that holds either a
    /// chart or an empty state, and a one-sentence insight caption underneath — so a
    /// subclass only has to write its query and bind its series.
    ///
    /// The same control is hosted by an analytics tab (full size) and by the Dashboard
    /// (<see cref="SetCompact"/>). There is exactly ONE implementation of each chart;
    /// nothing is copied between the two placements.
    /// </summary>
    public abstract class AnalyticsWidget : UserControl, IAnalyticsWidget
    {
        public const int FullWidth = 560;
        public const int FullHeight = 396;
        public const int CompactWidth = 268;
        public const int CompactHeight = 196;

        private readonly Label _titleLabel;
        private readonly Label _basisLabel;
        private readonly Panel _header;
        private readonly Panel _body;
        private readonly Label _captionLabel;
        private readonly EmptyStatePanel _empty;

        private Chart _chart;
        private Control _customBody;
        private Control _headerControl;
        private bool _compact;

        public string Title { get; private set; }

        /// <summary>
        /// Which date column the widget counts by, in the operator's words —
        /// "by date registered" / "by date of birth". Rendered under the title AND
        /// used as the X-axis title, so a screenshot is never ambiguous.
        /// </summary>
        public string Basis { get; private set; }

        public bool HasSufficientData { get; private set; }

        /// <summary>The range the widget was last loaded with (for captions and re-renders).</summary>
        protected DateRange Range { get; private set; }

        protected AnalyticsWidget(string title, string basis)
        {
            Title = title ?? "";
            Basis = basis ?? "";

            Size = new Size(FullWidth, FullHeight);
            Margin = new Padding(0, 0, 14, 14);
            BackColor = UiTheme.Surface;

            _titleLabel = new Label
            {
                Text = Title,
                AutoSize = false,
                Location = new Point(14, 10),
                Size = new Size(FullWidth - 28, 20),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                UseMnemonic = false
            };

            _basisLabel = new Label
            {
                Text = Basis,
                AutoSize = false,
                Location = new Point(14, 31),
                Size = new Size(FullWidth - 28, 16),
                Font = new Font("Segoe UI", 8F),
                ForeColor = UiTheme.Faint,
                UseMnemonic = false
            };

            _header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = UiTheme.Surface };
            _header.Controls.Add(_basisLabel);
            _header.Controls.Add(_titleLabel);

            _captionLabel = new Label
            {
                Text = "",
                AutoSize = false,
                Dock = DockStyle.Bottom,
                // Four lines at 8.5pt. The bucket and coverage captions genuinely run this
                // long, and a caption clipped mid-sentence is worse than no caption — the
                // reader cannot tell whether the missing half changes the meaning.
                Height = 64,
                Padding = new Padding(14, 4, 14, 8),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Muted,
                UseMnemonic = false
            };

            _body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(6, 0, 6, 0) };
            _empty = new EmptyStatePanel { Visible = false };
            _body.Controls.Add(_empty);

            Controls.Add(_body);
            Controls.Add(_captionLabel);
            Controls.Add(_header);

            Paint += DrawBorder;
        }

        /// <summary>
        /// The widget's own square hairline frame. On an analytics TAB the widget IS the card,
        /// so it draws its own edge; on the Dashboard it is hosted INSIDE a rounded
        /// <see cref="CardPanel"/>, where a second square rectangle would show through the
        /// rounded corners. The host turns this off — it is not a second implementation of the
        /// chart, just whether this control paints its own edge.
        /// </summary>
        public bool DrawFrame { get; set; } = true;

        private void DrawBorder(object sender, PaintEventArgs e)
        {
            if (!DrawFrame) return;
            using (var pen = new Pen(UiTheme.CardLine))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        // ------------------------------------------------------------------- body
        /// <summary>
        /// The widget's chart, created on first use. Subclasses that draw a chart use this;
        /// heat-map widgets call <see cref="SetBody"/> with a DataGridView instead (the spec
        /// builds heat maps from per-cell background colours, not from a charting library).
        /// </summary>
        protected Chart Chart
        {
            get
            {
                if (_chart == null)
                {
                    _chart = ChartStyle.Create();
                    _chart.Dock = DockStyle.Fill;
                    _body.Controls.Add(_chart);
                    _chart.SendToBack();
                }
                return _chart;
            }
        }

        /// <summary>Installs a non-chart body (used by the month-by-year heat maps).</summary>
        protected void SetBody(Control control)
        {
            if (_customBody == control) return;
            if (_customBody != null) _body.Controls.Remove(_customBody);
            _customBody = control;
            if (control != null)
            {
                control.Dock = DockStyle.Fill;
                _body.Controls.Add(control);
                control.SendToBack();
            }
        }

        /// <summary>
        /// Places a small control (a filter toggle) at the top-right of the widget header,
        /// and shortens the title/basis labels so they cannot run underneath it.
        ///
        /// A couple of charts genuinely need one — the spec asks for an under-5 restriction on
        /// leading causes of death, and a by-service-type switch on daily collection. Both are
        /// a different CUT of the same chart, not a different chart, so a toggle is right and a
        /// second widget would be wrong. Hidden in compact placements, which have no room.
        /// </summary>
        protected void SetHeaderControl(Control control)
        {
            if (_headerControl != null) _header.Controls.Remove(_headerControl);
            _headerControl = control;
            if (control == null) return;

            control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            control.Location = new Point(Width - control.Width - 14, 12);
            _header.Controls.Add(control);
            control.BringToFront();

            int room = control.Width + 22;
            _titleLabel.Size = new Size(Width - 28 - room, _titleLabel.Height);
            _basisLabel.Size = new Size(Width - 28 - room, _basisLabel.Height);
            control.Visible = !_compact;
        }

        /// <summary>Clears the chart down to a blank styled canvas before each render.</summary>
        protected void ResetChart()
        {
            Chart.Series.Clear();
            Chart.ChartAreas[0].AxisX.CustomLabels.Clear();
            Chart.ChartAreas[0].AxisY.CustomLabels.Clear();
            Chart.ChartAreas[0].AxisX.Title = "";
            Chart.ChartAreas[0].AxisY.Title = "";
            Chart.ChartAreas[0].AxisX.Interval = 0;
            Chart.ChartAreas[0].AxisY.Minimum = double.NaN;
            Chart.ChartAreas[0].AxisY.Maximum = double.NaN;
            Chart.ChartAreas[0].AxisY.LabelStyle.Format = "";
            Chart.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
            ChartStyle.ShowLegend(Chart, false);
            Chart.Visible = true;
        }

        // ------------------------------------------------------------- empty state
        /// <summary>
        /// Replaces the body with the empty-state panel and marks the widget as having
        /// insufficient data, which is what makes the Dashboard skip it.
        /// </summary>
        protected void ShowEmpty(EmptyStatePanel.Kind kind, string message, string hint)
        {
            HasSufficientData = false;
            if (_chart != null) _chart.Visible = false;
            if (_customBody != null) _customBody.Visible = false;
            _empty.SetCompact(_compact);
            _empty.Show(kind, message, hint);
            SetCaption("");
        }

        /// <summary>Convenience for the commonest case: the source column is unfilled.</summary>
        protected void ShowEmpty(AnalyticsData.Coverage coverage, string hint)
        {
            ShowEmpty(EmptyStatePanel.Kind.Unfilled, coverage.EmptyMessage, hint);
        }

        /// <summary>Hides the empty state and brings the chart (or custom body) back.</summary>
        protected void ShowBody()
        {
            _empty.Visible = false;
            if (_customBody != null) { _customBody.Visible = true; _customBody.BringToFront(); }
            else if (_chart != null) { _chart.Visible = true; _chart.BringToFront(); }
        }

        // ---------------------------------------------------------------- caption
        /// <summary>
        /// Sets the one-sentence takeaway under the chart.
        ///
        /// These captions are produced by <c>string.Format</c> over the SAME aggregate the
        /// chart was bound to — no external service, model or API is called, and none should
        /// be. The sentence is a restatement of the numbers on screen, so it is always
        /// consistent with them and always available offline.
        /// </summary>
        protected void SetCaption(string sentence)
        {
            _captionLabel.Text = sentence ?? "";
        }

        /// <summary>
        /// Caption for a chart the operator can read but should not draw conclusions from:
        /// under three data points the spec forbids trend lines, percentages and comparison
        /// language, so the sentence states the count and stops.
        /// </summary>
        protected void SetSparseCaption(int points, string noun)
        {
            SetCaption(points == 0
                ? "No " + noun + " in this period."
                : points + " " + noun + (points == 1 ? "" : "") +
                  " in this period — too few to describe a trend.");
        }

        // ------------------------------------------------------------------- load
        /// <summary>
        /// Re-queries and re-renders. Exceptions are contained: a widget whose query fails
        /// shows the error as an empty state rather than taking the whole tab down, which
        /// matters on a LAN database that can drop briefly.
        /// </summary>
        /// <remarks>
        /// <c>new</c> is deliberate: UserControl already has a Load EVENT, and the
        /// IAnalyticsWidget contract names this method Load. Hiding the event is intended —
        /// nothing in this module subscribes to it, and the base class still raises OnLoad
        /// normally.
        /// </remarks>
        public new void Load(DateTime from, DateTime to)
        {
            Load(new DateRange(from, to));
        }

        public new void Load(DateRange range)
        {
            Range = range;
            HasSufficientData = true;
            _empty.Visible = false;
            try
            {
                Render(range);
                if (HasSufficientData) ShowBody();
            }
            catch (Exception ex)
            {
                ShowEmpty(EmptyStatePanel.Kind.Error,
                    "This chart could not be loaded.",
                    ex.Message);
            }
        }

        /// <summary>
        /// Subclass hook: query and bind. Call <see cref="ShowEmpty(EmptyStatePanel.Kind,string,string)"/>
        /// (directly or via the coverage overload) whenever the data cannot support the chart.
        /// </summary>
        protected abstract void Render(DateRange range);

        // ---------------------------------------------------------------- compact
        /// <summary>
        /// Shrinks the widget for the Dashboard: smaller title, no basis line, no caption,
        /// no legend. The chart and its query are untouched — same control, same code.
        /// </summary>
        public virtual void SetCompact(bool compact)
        {
            _compact = compact;
            Size = compact ? new Size(CompactWidth, CompactHeight) : new Size(FullWidth, FullHeight);
            _titleLabel.Font = new Font("Segoe UI", compact ? 9F : 10.5F, FontStyle.Bold);
            int room = (!compact && _headerControl != null) ? _headerControl.Width + 22 : 0;
            _titleLabel.Size = new Size(Width - 28 - room, compact ? 17 : 20);
            if (_headerControl != null)
            {
                _headerControl.Visible = !compact;
                _headerControl.Location = new Point(Width - _headerControl.Width - 14, 12);
            }
            _basisLabel.Visible = !compact;
            _header.Height = compact ? 28 : 52;
            _captionLabel.Visible = !compact;
            _empty.SetCompact(compact);
            if (_chart != null) ChartStyle.ShowLegend(_chart, !compact && _chart.Series.Count > 1);
        }

        protected bool IsCompact { get { return _compact; } }

        /// <summary>Legend on only when it earns its space: more than one series, full size.</summary>
        protected void AutoLegend()
        {
            ChartStyle.ShowLegend(Chart, !_compact && Chart.Series.Count > 1);
        }
    }
}
