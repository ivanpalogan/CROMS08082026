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
    /// <summary>
    /// Fees &amp; Collections, under Reports &amp; Analytics. Replaces the old Fees &amp;
    /// Payments "Payment log" and "Monthly collection" tabs (moved here verbatim - same
    /// queries, same grids, same CSV export) and adds the printable annual assessment report:
    /// a <see cref="CollectionsReport"/> page with a conclusion at the bottom, editable through
    /// Settings -&gt; Forms &amp; Templates like every other certificate template.
    /// <list type="bullet">
    /// <item><b>Payment log</b> - every payment, from here, BREQS and marriage licences.</item>
    /// <item><b>Monthly collection</b> - the month's collections by fee, by source, by method.</item>
    /// </list>
    /// Read-only throughout: SELECT statements only, same as every other Reports &amp;
    /// Analytics tab.
    /// </summary>
    public partial class CollectionsReportForm : Form, IRefreshable
    {
        public CollectionsReportForm()
        {
            InitializeComponent();
        }

        public void RefreshData()
        {
            if (_tabs.SelectedTab == _pgLog) LoadLog();
            else if (_tabs.SelectedTab == _pgMonthly) LoadMonthly();
        }

        // ================================================================ payment log
        private void BuildLog()
        {
            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = UiTheme.PageBg };
            var bar = new TableLayoutPanel { Dock = DockStyle.Top, Height = 62, ColumnCount = 5, BackColor = Color.Transparent };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            bar.Controls.Add(MUi.Field("From", _lFrom), 0, 0);
            bar.Controls.Add(MUi.Field("To", _lTo), 1, 0);
            bar.Controls.Add(MUi.Field("Search O.R., payer, purpose, transaction", _lSearch), 2, 0);
            var show = MUi.Btn("Show", MUi.Kind.Primary, 100);
            var export = MUi.Btn("Export CSV", MUi.Kind.Secondary, 120);
            bar.Controls.Add(Cell(show), 3, 0); bar.Controls.Add(Cell(export), 4, 0);
            _lFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); _lTo.Value = DateTime.Today;
            show.Click += (s, e) => LoadLog();
            export.Click += (s, e) => ExportCsv((DataTable)_lGrid.DataSource, "payment-log-" + _lFrom.Value.ToString("yyyyMMdd") + "-" + _lTo.Value.ToString("yyyyMMdd") + ".csv");
            _lSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadLog(); e.SuppressKeyPress = true; } };

            _lSummary.Dock = DockStyle.Top; _lSummary.AutoSize = false; _lSummary.Height = 34; _lSummary.TextAlign = ContentAlignment.MiddleLeft;
            _lGrid.Dock = DockStyle.Fill; _lGrid.ReadOnly = true; _lGrid.AllowUserToAddRows = false; _lGrid.RowHeadersVisible = false;
            _lGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _lGrid.BackgroundColor = UiTheme.Surface; _lGrid.BorderStyle = BorderStyle.None;
            _lGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _lGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                string col = _lGrid.Columns[e.ColumnIndex].Name;
                if (col == "Amount" && e.Value is decimal d) { e.Value = d.ToString("N2"); e.FormattingApplied = true; }
                if (col == "Paid at" && e.Value is DateTime dt) { e.Value = dt.ToString("dd MMM yyyy h:mm tt"); e.FormattingApplied = true; }
                if (col == "Fees" && (string)e.Value == "(not itemised)") e.CellStyle.ForeColor = UiTheme.Faint;
            };
            root.Controls.Add(_lGrid); root.Controls.Add(_lSummary); root.Controls.Add(bar);
            _pgLog.Controls.Add(root);
        }

        private static Control Cell(Control c)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 10, 0), BackColor = Color.Transparent };
            c.Dock = DockStyle.Fill; p.Controls.Add(c);
            return p;
        }

        private void LoadLog()
        {
            try
            {
                DataTable t = PaymentService.Log(_lFrom.Value.Date, _lTo.Value.Date, _lSearch.Text);
                _lGrid.DataSource = t;
                if (_lGrid.Columns.Contains("Id")) _lGrid.Columns["Id"].Visible = false;
                if (_lGrid.Columns.Contains("Fees")) _lGrid.Columns["Fees"].FillWeight = 220;
                if (_lGrid.Columns.Contains("Amount")) _lGrid.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                decimal sum = t.AsEnumerable().Sum(r => Convert.ToDecimal(r["Amount"]));
                _lSummary.Text = t.Rows.Count + " payment" + (t.Rows.Count == 1 ? "" : "s") + "   -   total PHP " + sum.ToString("N2");
            }
            catch (Exception ex) { _lSummary.Text = "Could not load the log: " + ex.Message; }
        }

        // ================================================================ monthly collection
        private PaymentService.MonthlyCollection _month;

        /// <summary>
        /// The month's collections from the one payment log: by fee (the fee lines), by where the payment
        /// came from, and by method. Each table adds up to the month's total - the fee table carries
        /// "additional charges" and "not itemised" rows for exactly that reason.
        /// </summary>
        private void BuildMonthly()
        {
            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = UiTheme.PageBg };
            var bar = new TableLayoutPanel { Dock = DockStyle.Top, Height = 62, ColumnCount = 6, BackColor = Color.Transparent };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            bar.Controls.Add(MUi.Field("Month", _mMonth), 0, 0);
            bar.Controls.Add(MUi.Field("Year", _mYear), 1, 0);
            var show = MUi.Btn("Show", MUi.Kind.Primary, 100);
            var exportFees = MUi.Btn("Export by fee (CSV)", MUi.Kind.Secondary, 170);
            var exportList = MUi.Btn("Export payments (CSV)", MUi.Kind.Secondary, 190);
            bar.Controls.Add(Cell(show), 2, 0); bar.Controls.Add(Cell(exportFees), 3, 0); bar.Controls.Add(Cell(exportList), 4, 0);
            for (int y = DateTime.Today.Year; y >= 2020; y--) _mYear.Items.Add(y.ToString(CultureInfo.InvariantCulture));
            _mMonth.SelectedIndex = DateTime.Today.Month - 1; _mYear.SelectedIndex = 0;
            show.Click += (s, e) => LoadMonthly();
            exportFees.Click += (s, e) => { if (_month != null) ExportCsv(_month.ByFee, "collection-by-fee-" + _month.Year + "-" + _month.Month.ToString("00") + ".csv"); };
            exportList.Click += (s, e) => { if (_month != null) ExportCsv(_month.Detail, "collection-payments-" + _month.Year + "-" + _month.Month.ToString("00") + ".csv"); };

            _mSummary.Dock = DockStyle.Top; _mSummary.AutoSize = false; _mSummary.Height = 30; _mSummary.TextAlign = ContentAlignment.MiddleLeft;
            _mNote.Dock = DockStyle.Top; _mNote.AutoSize = false; _mNote.Height = 24; _mNote.TextAlign = ContentAlignment.TopLeft;

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            Control fees = Titled("By fee", _mByFee);
            body.Controls.Add(fees, 0, 0); body.SetRowSpan(fees, 2);
            body.Controls.Add(Titled("By source", _mBySource), 1, 0);
            body.Controls.Add(Titled("By payment method", _mByMethod), 1, 1);

            root.Controls.Add(body); root.Controls.Add(_mNote); root.Controls.Add(_mSummary); root.Controls.Add(bar);
            _pgMonthly.Controls.Add(root);
        }

        private static Control Titled(string title, DataGridView grid)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 12), Margin = new Padding(0), BackColor = Color.Transparent };
            var t = MUi.Txt(title.ToUpperInvariant(), 9F, FontStyle.Bold, UiTheme.Muted);
            t.Dock = DockStyle.Top; t.AutoSize = false; t.Height = 24;
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.BackgroundColor = UiTheme.Surface; grid.BorderStyle = BorderStyle.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null || e.Value == DBNull.Value) return;
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col == "Amount" && e.Value is decimal d) { e.Value = d.ToString("N2"); e.FormattingApplied = true; }
                if (col == "Quantity" && e.Value is decimal q) { e.Value = q.ToString("0"); e.FormattingApplied = true; }
            };
            p.Controls.Add(grid); p.Controls.Add(t);
            return p;
        }

        private void LoadMonthly()
        {
            try
            {
                int year = int.Parse((string)_mYear.SelectedItem, CultureInfo.InvariantCulture), month = _mMonth.SelectedIndex + 1;
                _month = PaymentService.Monthly(year, month);
                _mByFee.DataSource = _month.ByFee; _mBySource.DataSource = _month.BySource; _mByMethod.DataSource = _month.ByMethod;
                foreach (DataGridView g in new[] { _mByFee, _mBySource, _mByMethod })
                    if (g.Columns.Contains("Amount")) g.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (_mByFee.Columns.Contains("Fee")) _mByFee.Columns["Fee"].FillWeight = 260;
                _mSummary.Text = _month.Label + "   -   " + _month.Payments + " receipt" + (_month.Payments == 1 ? "" : "s") + "   -   total PHP " + _month.Total.ToString("N2");
                _mNote.Text = _month.Unitemised > 0
                    ? "PHP " + _month.Unitemised.ToString("N2") + " was recorded before fee lines existed and is shown as not itemised - which fees it covered was never recorded."
                    : _month.Payments == 0 ? "No payments recorded in this month." : "";
            }
            catch (Exception ex) { _mSummary.Text = "Could not load the month: " + ex.Message; }
        }

        // ================================================================ shared
        private static void ExportCsv(DataTable t, string suggestedName)
        {
            if (t == null || t.Rows.Count == 0) { MessageBox.Show("Nothing to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dlg = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = suggestedName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var sb = new StringBuilder();
                var cols = t.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "Id").ToList();
                sb.AppendLine(string.Join(",", cols.Select(c => Csv(c.ColumnName))));
                foreach (DataRow r in t.Rows)
                    sb.AppendLine(string.Join(",", cols.Select(c => Csv(r[c] is DateTime d ? d.ToString("yyyy-MM-dd HH:mm") : r[c] is decimal m ? m.ToString("0.00", CultureInfo.InvariantCulture) : Convert.ToString(r[c])))));
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            }
        }

        private static string Csv(string v) { v = v ?? ""; return v.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v; }
    }
}
