using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// The shared body of every domain tab in Reports &amp; Analytics. Fixes the vertical
    /// structure the spec calls for — summary-card strip, then the date-range control, then
    /// a reflowing chart area — so all six tabs read identically and a new tab only has to
    /// declare its cards and its widgets.
    ///
    /// Loading is LAZY: a tab queries nothing until it is actually shown. Six tabs eagerly
    /// running five aggregate queries each would hammer the office LAN every time the module
    /// opened, for five tabs nobody is looking at.
    /// </summary>
    public abstract class AnalyticsTab : UserControl
    {
        protected FlowLayoutPanel CardStrip { get; private set; }
        protected DateRangeBar RangeBar { get; private set; }
        protected FlowLayoutPanel ChartHost { get; private set; }

        private readonly List<AnalyticsWidget> _widgets = new List<AnalyticsWidget>();
        private readonly Label _banner;
        private string _note;          // survives HideBanner; see ShowNote
        private bool _built;
        private bool _loaded;

        protected AnalyticsTab()
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.PageBg;
            Padding = new Padding(18, 14, 18, 10);
            AutoScroll = false;                  // the chart area does its own scrolling

            ChartHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                BackColor = UiTheme.PageBg,
                Padding = new Padding(0, 10, 0, 0)
            };

            RangeBar = new DateRangeBar();
            RangeBar.RangeChanged += (s, e) => LoadWidgets();

            CardStrip = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = SummaryCard.CardHeight + 14,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = UiTheme.PageBg
            };

            _banner = new Label
            {
                Dock = DockStyle.Top,
                Height = 0,
                Visible = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Warning,
                BackColor = Color.FromArgb(254, 248, 235),
                Padding = new Padding(12, 6, 12, 6),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Dock order: the Fill control is added first, then Top controls bottom-up.
            Controls.Add(ChartHost);
            Controls.Add(RangeBar);
            Controls.Add(CardStrip);
            Controls.Add(_banner);
        }

        // ------------------------------------------------------------------ build
        /// <summary>
        /// Subclass hook, called once: add summary cards with <see cref="AddCard"/> and
        /// charts with <see cref="AddWidget"/>. No queries here — see <see cref="LoadCards"/>.
        /// </summary>
        protected abstract void Build();

        /// <summary>
        /// Subclass hook: fill the summary cards. Cards use their own FIXED periods
        /// (this month, last month, this year), never <see cref="DateRangeBar.Range"/>.
        /// </summary>
        protected abstract void LoadCards();

        protected SummaryCard AddCard(string caption, Color accent)
        {
            var card = new SummaryCard(caption, accent);
            CardStrip.Controls.Add(card);
            return card;
        }

        protected T AddWidget<T>(T widget) where T : AnalyticsWidget
        {
            _widgets.Add(widget);
            ChartHost.Controls.Add(widget);
            return widget;
        }

        /// <summary>
        /// Adds the "Print Assessment Report" button every domain tab ends its card strip
        /// with — same size/margin as a <see cref="SummaryCard"/> so it sits in the strip
        /// rather than looking bolted on. <see cref="Modules.UiTheme"/> polishes it (rounded,
        /// hover, hand cursor) the first time this module is shown, same as every other
        /// button in the app.
        /// </summary>
        protected Button AddReportButton(string caption, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = caption,
                Size = new Size(176, SummaryCard.CardHeight),
                Margin = new Padding(4, 0, 12, 12),
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.Click += onClick;
            CardStrip.Controls.Add(btn);
            return btn;
        }

        /// <summary>Every widget on this tab, in the order it was added.</summary>
        public IList<AnalyticsWidget> Widgets { get { return _widgets; } }

        // ------------------------------------------------------------------- load
        /// <summary>Called by the host the first time this tab is shown, and on refresh.</summary>
        public void EnsureLoaded()
        {
            if (!_built) { Build(); _built = true; }
            if (_loaded) return;
            ReloadAll();
        }

        /// <summary>Re-runs the card queries and every widget's query.</summary>
        public void ReloadAll()
        {
            if (!_built) { Build(); _built = true; }
            _loaded = true;
            HideBanner();

            Cursor prev = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                try
                {
                    LoadCards();
                }
                catch (Exception ex)
                {
                    ShowBanner("Summary figures could not be loaded: " + ex.Message);
                }
                LoadWidgets();
            }
            finally
            {
                Cursor = prev;
            }
        }

        /// <summary>Re-runs only the chart queries (what the date-range control affects).</summary>
        protected void LoadWidgets()
        {
            DateRange r = RangeBar.Range;
            foreach (AnalyticsWidget w in _widgets)
                w.Load(r);            // each widget contains its own failures
        }

        // ----------------------------------------------------------------- banner
        protected void ShowBanner(string message)
        {
            _banner.Text = message;
            _banner.Height = 30;
            _banner.Visible = true;
        }

        /// <summary>
        /// Clears a transient error banner. A PERSISTENT note set by <see cref="ShowNote"/> is
        /// restored rather than cleared — it explains something about the tab itself, so it has
        /// to survive every reload. (It did not, before: ReloadAll hid the banner on entry and
        /// the note set in Build never reappeared.)
        /// </summary>
        protected void HideBanner()
        {
            if (_note != null) { RenderNote(); return; }
            _banner.Visible = false;
            _banner.Height = 0;
        }

        /// <summary>
        /// Adds a note under the card strip explaining something the tab as a whole needs
        /// stated once — e.g. that a table the spec named holds configuration rather than
        /// history. Rendered in the same banner, in muted rather than warning colours.
        /// </summary>
        protected void ShowNote(string message)
        {
            _note = message;
            RenderNote();
        }

        private void RenderNote()
        {
            _banner.Text = _note;
            _banner.ForeColor = UiTheme.Muted;
            _banner.BackColor = Color.FromArgb(245, 247, 250);
            _banner.Height = 30;
            _banner.Visible = true;
        }
    }
}
