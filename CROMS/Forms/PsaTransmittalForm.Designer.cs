using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    internal sealed partial class PsaTransmittalForm
    {
        private readonly FlowLayoutPanel _seg = new FlowLayoutPanel();
        private readonly DataGridView _queue = new DataGridView(), _batches = new DataGridView(), _items = new DataGridView();
        private readonly NumericUpDown _year = new NumericUpDown();
        private readonly ComboBox _month = MUi.Combo(false, CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12));
        private readonly Label _count = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted), _due = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Button _create = MUi.Btn("CREATE TRANSMITTAL BATCH", MUi.Kind.Primary), _gen = MUi.Btn("Generate Transmittal", MUi.Kind.Secondary),
                                _sent = MUi.Btn("Mark as Sent", MUi.Kind.Success), _ack = MUi.Btn("Record Acknowledgement", MUi.Kind.Secondary),
                                _remove = MUi.Btn("Remove from draft", MUi.Kind.Ghost), _return = MUi.Btn("Record returned record...", MUi.Kind.Ghost);

        private void InitializeComponent()
        {
            Text = "PSA / OCRG Transmittal";
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(1180, 800); MinimumSize = new Size(1000, 660);
            BackColor = UiTheme.PageBg; ShowInTaskbar = false;

            var head = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UiTheme.PageBg, Padding = new Padding(18, 10, 18, 0) };
            var t = MUi.Txt("PSA / OCRG TRANSMITTAL", 16F, FontStyle.Bold); t.Location = new Point(18, 8);
            var sub = MUi.Txt("Registered Certificates of Marriage sent to the Civil Registrar-General through the PSA. Sent is not the same as available at PSA.",
                9F, FontStyle.Regular, UiTheme.Muted);
            sub.Location = new Point(20, 40);
            head.Controls.Add(t); head.Controls.Add(sub);

            // SplitterDistance / min sizes are applied once the form has its real size: set in
            // the initializer they are checked against the default 150px container and throw.
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = UiTheme.PageBg };
            Shown += (s, e) =>
            {
                split.Panel1MinSize = 220; split.Panel2MinSize = 180;
                split.SplitterDistance = Math.Max(220, Math.Min(split.Height - 180, (int)(split.Height * 0.52)));
            };
            // --- top: records
            var top = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10), Margin = new Padding(16, 0, 16, 8) };
            _seg.Dock = DockStyle.Top; _seg.Height = 38; _seg.BackColor = Color.Transparent;
            foreach (string f in Filters)
            {
                string key = f;
                var b = MUi.Btn(f, f == _filter ? MUi.Kind.Primary : MUi.Kind.Secondary);
                b.Click += (s, e) => { _filter = key; RepaintSeg(); Bind(); };
                _seg.Controls.Add(b);
            }
            _seg.Controls.Add(_count);
            Style(_queue, false);
            _queue.ReadOnly = false;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
            _year.Minimum = 2000; _year.Maximum = 2100; _year.Width = 70; _year.Font = MUi.F(9.75F);
            DateTime last = DateTime.Today.AddMonths(-1);
            _year.Value = last.Year; _month.Width = 120; _month.Dock = DockStyle.None; _month.SelectedIndex = last.Month - 1;
            bar.Controls.Add(MUi.Txt("Reporting period", 9F, FontStyle.Bold, UiTheme.Muted));
            bar.Controls.Add(_month); bar.Controls.Add(_year); bar.Controls.Add(_create); bar.Controls.Add(_return); bar.Controls.Add(_due);
            top.Controls.Add(_queue); top.Controls.Add(bar); top.Controls.Add(_seg);
            split.Panel1.Padding = new Padding(16, 0, 16, 6);
            split.Panel1.Controls.Add(top);

            // --- bottom: batches
            var bottom = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10) };
            var bh = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
            bh.Controls.Add(MUi.Txt("BATCHES", 9F, FontStyle.Bold, UiTheme.Muted));
            bh.Controls.Add(_gen); bh.Controls.Add(_sent); bh.Controls.Add(_ack); bh.Controls.Add(_remove);
            var inner = new SplitContainer { Dock = DockStyle.Fill };
            Shown += (s, e) => inner.SplitterDistance = Math.Max(100, (int)(inner.Width * 0.55));
            Style(_batches, true); Style(_items, true);
            inner.Panel1.Controls.Add(_batches); inner.Panel2.Controls.Add(_items);
            bottom.Controls.Add(inner); bottom.Controls.Add(bh);
            split.Panel2.Padding = new Padding(16, 6, 16, 16);
            split.Panel2.Controls.Add(bottom);

            Controls.Add(split); Controls.Add(head);

            _create.Click += (s, e) => CreateBatch();
            _gen.Click += (s, e) => PrintTransmittal();
            _sent.Click += (s, e) => MarkSent();
            _ack.Click += (s, e) => Acknowledge();
            _remove.Click += (s, e) => RemoveItem();
            _return.Click += (s, e) => ReturnRecord();
            _batches.SelectionChanged += (s, e) => BindItems();
            _queue.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                using (var f = new MarriageRecordForm(Convert.ToInt32(_queue.Rows[e.RowIndex].Cells["id"].Value))) f.ShowDialog(this);
                Reload();
            };
            UiTheme.Polish(this);
            RepaintSeg();
            Reload();
            if (_preselect.HasValue) { _filter = "All"; RepaintSeg(); Bind(); }
        }

        private static void Style(DataGridView g, bool readOnly)
        {
            g.Dock = DockStyle.Fill; g.AllowUserToAddRows = false; g.RowHeadersVisible = false; g.ReadOnly = readOnly;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // The batch grid has eleven columns: filled into half a window every header was
            // cropped. Batch-type grids size to content and scroll sideways instead.
            g.AutoSizeColumnsMode = readOnly ? DataGridViewAutoSizeColumnsMode.AllCells : DataGridViewAutoSizeColumnsMode.Fill;
            g.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn c in g.Columns)
                    if (c.ValueType == typeof(DateTime)) c.DefaultCellStyle.Format = "dd MMM yyyy";
            };
            g.BackgroundColor = UiTheme.Surface; g.BorderStyle = BorderStyle.None; g.MultiSelect = false;
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string n = g.Columns[e.ColumnIndex].Name;
                if (n == "Status" || n == "Item Status") { e.CellStyle.ForeColor = MUi.InkOf(Convert.ToString(e.Value)); e.CellStyle.Font = MUiFonts.Bold9; }
            };
            g.DataError += (s, e) => e.ThrowException = false;
        }
    }
}
