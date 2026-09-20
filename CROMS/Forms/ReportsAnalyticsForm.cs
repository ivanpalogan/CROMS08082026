using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Analytics;
using CROMS.Analytics.Tabs;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Reports &amp; Analytics — the tabbed host for the reporting domains.
    ///
    /// Fees &amp; Collections rehosts the former Fees &amp; Payments "Payment log" / "Monthly
    /// collection" tabs (same queries, same grids, same CSV export) and adds the printable
    /// annual <see cref="CollectionsReport"/> - a Form3ACell page with a conclusion at the
    /// bottom, editable through Settings -&gt; Forms &amp; Templates like every other
    /// certificate template.
    ///
    /// PSA / Statutory REHOSTS the existing <see cref="ReportsPsaForm"/> unchanged:
    /// it is embedded with TopLevel = false, exactly the way MainForm embeds every module,
    /// so its logic, queries, output and CSV export are the ones the office already checks
    /// against PSA. Nothing in that form was rewritten. Keeping statutory output physically
    /// separate from analytical output is the point of the tab.
    ///
    /// Every tab loads LAZILY — a tab queries nothing until it is selected — and the module
    /// is read-only throughout: SELECT statements only.
    /// </summary>
    public partial class ReportsAnalyticsForm : Form, IRefreshable
    {
        private readonly Dictionary<TabPage, AnalyticsTab> _analytics = new Dictionary<TabPage, AnalyticsTab>();
        private ReportsPsaForm _psa;
        private TabPage _psaPage;
        private CollectionsReportForm _collections;
        private TabPage _collectionsPage;

        public ReportsAnalyticsForm()
        {
            InitializeComponent();
        }

        // ------------------------------------------------------------------- tabs
        private void AddAnalyticsTab(string caption, AnalyticsTab tab)
        {
            var page = new TabPage(caption) { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            page.Controls.Add(tab);
            _tabs.TabPages.Add(page);
            _analytics[page] = tab;
        }

        private void AddPendingTab(string caption, string[] planned)
        {
            var page = new TabPage(caption) { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            page.Controls.Add(new PendingAnalyticsTab(caption, planned));
            _tabs.TabPages.Add(page);
        }

        /// <summary>
        /// Rehosts the existing PSA form. It is embedded, not reimplemented: the same Form,
        /// the same queries, the same CSV export. Embedding a Form with TopLevel = false is
        /// the pattern MainForm already uses for every module, so the form stays fully
        /// designable and behaves identically to the standalone screen it was.
        /// </summary>
        private void AddPsaTab()
        {
            _psaPage = new TabPage("PSA / Statutory") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };

            // Fill child added FIRST, Top bar added SECOND — the same order this form's own
            // constructor uses (_tabs then header) and the one this codebase has repeatedly
            // had to fix elsewhere: a docked child added LATER is laid out FIRST, so adding
            // the bar before a Fill host would let Fill claim the whole page and overlap it.
            _psaHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PageBg };

            var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.PageBg, Padding = new Padding(0, 6, 12, 6) };
            var btnPrint = new Button
            {
                Text = "Print Assessment Report",
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnPrint.Click += (s, e) =>
            {
                DataTable t = AssessmentReport.BuildTable(AssessmentReport.Psa, DateTime.Today.Year);
                AssessmentReport.Show(AssessmentReport.Psa, t, this);
            };
            bar.Controls.Add(btnPrint);

            _psaPage.Controls.Add(_psaHost);
            _psaPage.Controls.Add(bar);

            _tabs.TabPages.Add(_psaPage);
        }

        /// <summary>
        /// Builds the embedded PSA form on first view. Deferred like the analytics tabs —
        /// its constructor runs its own Generate(), and doing that on module open would query
        /// for a tab nobody has looked at.
        /// </summary>
        private void EnsurePsa()
        {
            if (_psa != null) return;
            _psa = new ReportsPsaForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            _psaHost.Controls.Add(_psa);
            _psa.Show();
            UiTheme.PolishButtons(_psa);
        }

        /// <summary>
        /// Fees &amp; Collections — the old Fees &amp; Payments "Payment log" / "Monthly
        /// collection" tabs, rehosted here (same pattern as the PSA tab), plus the printable
        /// annual assessment report.
        /// </summary>
        private void AddCollectionsTab()
        {
            _collectionsPage = new TabPage("Fees & Collections") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            _tabs.TabPages.Add(_collectionsPage);
        }

        private void EnsureCollections()
        {
            if (_collections != null) return;
            _collections = new CollectionsReportForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            _collectionsPage.Controls.Add(_collections);
            _collections.Show();
            UiTheme.PolishButtons(_collections);
        }

        // ------------------------------------------------------------------- load
        /// <summary>Loads only the tab the user is actually looking at.</summary>
        private void LoadSelected()
        {
            TabPage page = _tabs.SelectedTab;
            if (page == null) return;

            if (page == _psaPage)
            {
                EnsurePsa();
                _psa.RefreshData();
                return;
            }

            if (page == _collectionsPage)
            {
                EnsureCollections();
                _collections.RefreshData();
                return;
            }

            AnalyticsTab tab;
            if (_analytics.TryGetValue(page, out tab)) tab.EnsureLoaded();
        }

        /// <summary>
        /// Called by the shell each time the module is navigated to. Refreshes the VISIBLE
        /// tab only — re-running all six domains' queries on every navigation is exactly the
        /// load the office LAN will not absorb.
        /// </summary>
        public void RefreshData()
        {
            TabPage page = _tabs.SelectedTab;
            if (page == null) return;

            if (page == _psaPage) { EnsurePsa(); _psa.RefreshData(); return; }
            if (page == _collectionsPage) { EnsureCollections(); _collections.RefreshData(); return; }

            AnalyticsTab tab;
            if (_analytics.TryGetValue(page, out tab)) tab.ReloadAll();
        }

        // ------------------------------------------------------------------ paint
        /// <summary>
        /// Flat tab strip: selected tab on the page surface with an accent underline, the
        /// rest muted on the page background. WinForms' native tabs read as a 2005 dialog
        /// next to the rest of this app.
        /// </summary>
        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            TabPage page = _tabs.TabPages[e.Index];
            bool selected = _tabs.SelectedIndex == e.Index;
            Rectangle r = e.Bounds;

            using (var back = new SolidBrush(selected ? UiTheme.Surface : UiTheme.PageBg))
                e.Graphics.FillRectangle(back, r);

            if (selected)
            {
                using (var accent = new SolidBrush(UiTheme.Accent))
                    e.Graphics.FillRectangle(accent, r.Left, r.Bottom - 3, r.Width, 3);
            }

            TextRenderer.DrawText(e.Graphics, page.Text,
                new Font("Segoe UI", 9.5F, selected ? FontStyle.Bold : FontStyle.Regular),
                r, selected ? UiTheme.Accent : UiTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
