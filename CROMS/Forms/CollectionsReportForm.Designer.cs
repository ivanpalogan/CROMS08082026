using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    public partial class CollectionsReportForm
    {
        private readonly TabControl _tabs = new TabControl();
        private TabPage _pgLog, _pgMonthly;

        // ================================================================ payment log
        private readonly DateTimePicker _lFrom = MUi.Date(false), _lTo = MUi.Date(false);
        private readonly TextBox _lSearch = MUi.Box();
        private readonly DataGridView _lGrid = new DataGridView();
        private readonly Label _lSummary = MUi.Txt("", 10F, FontStyle.Bold);

        // ================================================================ monthly collection
        private readonly ComboBox _mMonth = MUi.Combo(false, CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12)), _mYear = MUi.Combo(false);
        private readonly Label _mSummary = MUi.Txt("", 10F, FontStyle.Bold), _mNote = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly DataGridView _mByFee = new DataGridView(), _mBySource = new DataGridView(), _mByMethod = new DataGridView();

        private void InitializeComponent()
        {
            Text = "Fees & Collections";
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = UiTheme.PageBg, Padding = new Padding(0, 8, 0, 0) };
            var btnPrint = new Button
            {
                Text = "Print Assessment Report (Annual)",
                Dock = DockStyle.Right,
                Width = 240,
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnPrint.Click += (s, e) =>
            {
                DataTable t = CollectionsReport.BuildTable(DateTime.Today.Year);
                CollectionsReport.Show(t, this);
            };
            bar.Controls.Add(btnPrint);

            _pgLog = new TabPage("Payment log") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            _pgMonthly = new TabPage("Monthly collection") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            BuildLog();
            BuildMonthly();

            _tabs.Dock = DockStyle.Fill;
            _tabs.Font = new Font("Segoe UI", 10F);
            _tabs.Padding = new Point(14, 6);
            _tabs.TabPages.AddRange(new[] { _pgLog, _pgMonthly });
            _tabs.SelectedIndexChanged += (s, e) => RefreshData();

            // Fill child added FIRST, Top bar added SECOND - a docked child added LATER is
            // laid out FIRST, so Fill would otherwise claim the whole page and overlap the bar.
            Controls.Add(_tabs);
            Controls.Add(bar);

            Load += (s, e) => RefreshData();
        }
    }
}
