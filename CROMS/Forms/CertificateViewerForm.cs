using System;
using System.Drawing;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Hosts a rendered Crystal report. Print, page setup, page navigation, search and export
    /// (PDF / Excel / Word / RTF / CSV) come from the viewer's own toolbar.
    /// <para/>
    /// ZOOM is CROMS's, not the toolbar's: Ctrl + mouse wheel, Ctrl + / Ctrl - / Ctrl 0, and a
    /// zoom bar with the current percentage. The viewer's own zoom dropdown is hidden because
    /// the viewer exposes no way to READ its zoom back - two controls changing it would leave
    /// the percentage shown here wrong.
    /// <para/>
    /// The viewer control is created in code rather than dropped from the toolbox, so the
    /// .Designer.cs never holds a licence-checked Crystal control that stops the project
    /// opening on a PC without the designer.
    /// <para/>
    /// This type mentions Crystal types, so - like <see cref="CROMS.Data.CrystalRunner"/> -
    /// it is only ever loaded on a machine that has the runtime.
    /// <para/>
    /// UI layout lives in CertificateViewerForm.Designer.cs; this file holds the logic.
    /// </summary>
    internal partial class CertificateViewerForm : Form
    {
        private readonly ReportDocument _report;
        private CtrlWheelZoom _wheel;
        private int _zoom = 100;

        /// <summary>The zoom last applied, in percent. Read by the test harness.</summary>
        public int ZoomPercent { get { return _zoom; } }

        public CertificateViewerForm(ReportDocument report, string caption) : this(report, caption, null) { }

        /// <param name="note">A warning shown above the page (e.g. values too long for their box), or null.</param>
        public CertificateViewerForm(ReportDocument report, string caption, string note)
        {
            InitializeComponent();

            _report = report;

            Text = caption;

            _viewer = new CrystalReportViewer
            {
                Dock = DockStyle.Fill,
                // The report is already bound to a DataTable, so there is no database to
                // log into and no parameter left to ask about.
                ShowParameterPanelButton = false,
                EnableRefresh = false,
                ToolPanelView = ToolPanelViewType.None,
                ShowCloseButton = false,
                ShowZoomButton = false,
                ReportSource = report
            };

            Controls.Add(_viewer);
            Controls.Add(BuildZoomBar());
            if (!string.IsNullOrEmpty(note)) Controls.Add(BuildNote(note));

            _wheel = new CtrlWheelZoom(_viewer, dir => SetZoom(CtrlWheelZoom.Next(_zoom, dir)));
            Shown += (s, e) => FitPage();
        }

        public void SetZoom(int percent)
        {
            // 1 and 2 are the viewer's own "page width" / "whole page" codes, not percentages.
            _zoom = Math.Max(10, Math.Min(400, percent));
            _viewer.Zoom(_zoom);
            _zoomLabel.Text = _zoom + "%";
        }

        /// <summary>The percentage at which one whole page fits the viewer, from the report's own page size.</summary>
        private int FitPercent(bool width)
        {
            try
            {
                var o = _report.PrintOptions;
                double pageW = (o.PageContentWidth + o.PageMargins.leftMargin + o.PageMargins.rightMargin) / 1440.0 * 96.0;
                double pageH = (o.PageContentHeight + o.PageMargins.topMargin + o.PageMargins.bottomMargin) / 1440.0 * 96.0;
                // Room taken by the viewer's toolbar, page gutter and scrollbar.
                double availW = Math.Max(200, _viewer.ClientSize.Width - 60);
                double availH = Math.Max(200, _viewer.ClientSize.Height - 90);
                double pct = width ? availW / pageW * 100.0 : Math.Min(availW / pageW, availH / pageH) * 100.0;
                return (int)Math.Floor(pct / 5.0) * 5;
            }
            catch { return 100; }
        }

        public void FitPage() { SetZoom(FitPercent(false)); }
        public void FitWidth() { SetZoom(FitPercent(true)); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Oemplus:
                case Keys.Control | Keys.Add:
                    SetZoom(CtrlWheelZoom.Next(_zoom, 1)); return true;
                case Keys.Control | Keys.OemMinus:
                case Keys.Control | Keys.Subtract:
                    SetZoom(CtrlWheelZoom.Next(_zoom, -1)); return true;
                case Keys.Control | Keys.D0:
                case Keys.Control | Keys.NumPad0:
                    FitPage(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// A ReportDocument holds a native Crystal handle and a temp copy of the .rpt. Leaving it
        /// open leaks both, and after enough opens Crystal refuses to render with a "maximum
        /// report processing jobs" error - so it is closed explicitly. The wheel filter is
        /// application-wide and must be removed with the window.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_wheel != null) { _wheel.Dispose(); _wheel = null; }
            try
            {
                _viewer.ReportSource = null;
                _report.Close();
                _report.Dispose();
            }
            catch { /* already torn down */ }
        }
    }
}
