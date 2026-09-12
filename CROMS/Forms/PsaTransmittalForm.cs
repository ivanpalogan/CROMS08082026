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
    /// <summary>
    /// WINDOW 6 - PSA / OCRG transmittal of registered marriages. Records are grouped into a
    /// batch, the batch is printed as the transmittal, marked sent with HOW and TO WHOM it went,
    /// then acknowledged - or a record comes back for correction and goes into a later batch.
    /// The submission mechanism is recorded, not assumed: offices send physical batches,
    /// electronic endorsements, or something else.
    /// </summary>
    internal sealed class PsaTransmittalForm : Form
    {
        private static readonly string[] Filters = { "Unsent", "In Batch", "Sent", "Acknowledged", "Returned", "All" };
        private string _filter = "Unsent";
        private readonly int? _preselect;
        private readonly FlowLayoutPanel _seg = new FlowLayoutPanel();
        private readonly DataGridView _queue = new DataGridView(), _batches = new DataGridView(), _items = new DataGridView();
        private readonly NumericUpDown _year = new NumericUpDown();
        private readonly ComboBox _month = MUi.Combo(false, CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12));
        private readonly Label _count = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted), _due = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Button _create = MUi.Btn("CREATE TRANSMITTAL BATCH", MUi.Kind.Primary), _gen = MUi.Btn("Generate Transmittal", MUi.Kind.Secondary),
                                _sent = MUi.Btn("Mark as Sent", MUi.Kind.Success), _ack = MUi.Btn("Record Acknowledgement", MUi.Kind.Secondary),
                                _remove = MUi.Btn("Remove from draft", MUi.Kind.Ghost), _return = MUi.Btn("Record returned record...", MUi.Kind.Ghost);
        private DataTable _q;

        public PsaTransmittalForm(int? preselectMarriageId = null)
        {
            _preselect = preselectMarriageId;
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

        private void RepaintSeg()
        {
            foreach (Button b in _seg.Controls.OfType<Button>())
            {
                bool on = b.Text == _filter;
                b.BackColor = on ? UiTheme.Accent : UiTheme.Chrome;
                b.ForeColor = on ? Color.White : UiTheme.Ink;
                b.Invalidate();
            }
        }

        private void Reload()
        {
            try { _q = MarriageService.PsaQueue(); } catch (Exception ex) { MUi.Fail(this, ex); return; }
            Bind();
            _batches.DataSource = MarriageService.Batches();
            if (_batches.Columns.Contains("id")) _batches.Columns["id"].Visible = false;
            BindItems();
            int dueDay = MarriageService.Settings.PsaDueDay;
            DateTime due = new DateTime(DateTime.Today.Year, DateTime.Today.Month, Math.Min(dueDay, 28));
            _due.Text = "   Last month's registrations are due on or before " + MUi.D(due) + " (setting PSA_TRANSMITTAL_DUE_DAY - confirm with the PSO).";
        }

        private void Bind()
        {
            if (_q == null) return;
            DataTable v = _q.Clone();
            v.Columns.Add("Select", typeof(bool));
            v.Columns["Select"].SetOrdinal(0);
            foreach (DataRow r in _q.Rows)
            {
                string st = r["Status"].ToString();
                bool show = _filter == "All" || (_filter == "Unsent" && (st == "Pending Transmittal" || st.StartsWith("Returned")))
                         || (_filter == "In Batch" && st == "In Batch") || (_filter == "Sent" && st == "Sent to PSA/OCRG")
                         || (_filter == "Acknowledged" && st == "Acknowledged") || (_filter == "Returned" && st.StartsWith("Returned"));
                if (!show) continue;
                DataRow n = v.NewRow();
                foreach (DataColumn c in _q.Columns) n[c.ColumnName] = r[c];
                n["Select"] = _preselect.HasValue && Convert.ToInt32(r["id"]) == _preselect.Value;
                v.Rows.Add(n);
            }
            _queue.DataSource = v;
            foreach (string hide in new[] { "id", "item_status", "legacy", "psa_ref" })
                if (_queue.Columns.Contains(hide)) _queue.Columns[hide].Visible = false;
            foreach (DataGridViewColumn c in _queue.Columns) c.ReadOnly = c.Name != "Select";
            if (_queue.Columns.Contains("Select")) _queue.Columns["Select"].FillWeight = 30;
            if (_queue.Columns.Contains("Couple")) _queue.Columns["Couple"].FillWeight = 220;
            _count.Text = "   " + v.Rows.Count + " record(s)";
        }

        private int? SelectedBatch()
        {
            if (_batches.CurrentRow == null || !_batches.Columns.Contains("id")) return null;
            return Convert.ToInt32(_batches.CurrentRow.Cells["id"].Value);
        }

        private string SelectedBatchStatus() { return _batches.CurrentRow == null ? null : Convert.ToString(_batches.CurrentRow.Cells["Status"].Value); }

        private void BindItems()
        {
            int? b = SelectedBatch();
            _items.DataSource = b.HasValue ? MarriageService.BatchItems(b.Value) : null;
            if (_items.Columns.Contains("id")) _items.Columns["id"].Visible = false;
            string st = SelectedBatchStatus();
            _gen.Enabled = b.HasValue;
            _sent.Enabled = st == "Draft";
            _ack.Enabled = st == "Sent";
            _remove.Enabled = st == "Draft";
        }

        private void CreateBatch()
        {
            _queue.EndEdit();
            var ids = new List<int>();
            foreach (DataGridViewRow r in _queue.Rows)
                if (r.Cells["Select"].Value is bool && (bool)r.Cells["Select"].Value) ids.Add(Convert.ToInt32(r.Cells["id"].Value));
            if (ids.Count == 0) { MessageBox.Show(this, "Tick the registered marriages to include.", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int y = (int)_year.Value, m = _month.SelectedIndex + 1;
            if (!MUi.Confirm(this, "Create batch", "Create a transmittal batch?", "Period|" + CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m) + " " + y,
                    "Records|" + ids.Count)) return;
            try
            {
                string no;
                MarriageService.CreateBatch(ids, y, m, out no);
                _filter = "In Batch"; RepaintSeg();
                Reload();
                MessageBox.Show(this, "Batch " + no + " created with " + ids.Count + " record(s). Generate the transmittal, then mark it sent when it leaves the office.",
                    "Batch created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void MarkSent()
        {
            int? b = SelectedBatch(); if (!b.HasValue) return;
            using (var f = new Form
            {
                Text = "Mark batch as sent", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(500, 390), MinimizeBox = false, MaximizeBox = false, BackColor = UiTheme.Surface, ShowInTaskbar = false
            })
            {
                var date = MUi.Date(false); date.MaxDate = DateTime.Today; date.Value = DateTime.Today;
                var method = MUi.Combo(false, MarriageService.SubmissionMethods); method.SelectedIndex = 0;
                var office = MUi.Box();
                try { office.Text = "PSA Provincial Statistical Office - " + Db.Pull("SELECT province FROM office_profile LIMIT 1").Rows[0][0]; } catch { }
                var refNo = MUi.Box(); var rem = MUi.Box();
                var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 12, 18, 0) };
                var list = new List<Control> { MUi.Field("Submission date", date), MUi.Field("Submission method", method), MUi.Field("Receiving office", office),
                                               MUi.Field("Reference number (receipt / tracking / endorsement no.)", refNo), MUi.Field("Remarks", rem) };
                for (int i = list.Count - 1; i >= 0; i--) { list[i].Dock = DockStyle.Top; list[i].Height = 56; body.Controls.Add(list[i]); }
                var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 9, 12, 8) };
                var ok = MUi.Btn("MARK AS SENT", MUi.Kind.Success); var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90);
                cancel.DialogResult = DialogResult.Cancel;
                ok.Click += (s, e) =>
                {
                    if (!MUi.Confirm(f, "Mark sent", "Record this batch as sent to PSA/OCRG?", "Batch|" + _batches.CurrentRow.Cells["Batch"].Value,
                            "Method|" + method.Text, "To|" + office.Text)) return;
                    try
                    {
                        MarriageService.MarkBatchSent(b.Value, date.Value.Date, method.SelectedItem as string, office.Text.Trim(),
                            string.IsNullOrWhiteSpace(refNo.Text) ? null : refNo.Text.Trim(), string.IsNullOrWhiteSpace(rem.Text) ? null : rem.Text.Trim());
                        f.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex) { MUi.Fail(f, ex); }
                };
                foot.Controls.Add(ok); foot.Controls.Add(cancel);
                f.Controls.Add(body); f.Controls.Add(foot); f.CancelButton = cancel;
                UiTheme.Polish(f);
                if (f.ShowDialog(this) == DialogResult.OK) Reload();
            }
        }

        private void Acknowledge()
        {
            int? b = SelectedBatch(); if (!b.HasValue) return;
            string refNo = MUi.Ask(this, "Acknowledgement", "PSA/OCRG acknowledgement reference (as written on the acknowledgement):");
            if (refNo == null) return;
            try { MarriageService.AcknowledgeBatch(b.Value, DateTime.Today, refNo); Reload(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void RemoveItem()
        {
            int? b = SelectedBatch(); if (!b.HasValue || _items.CurrentRow == null) return;
            try { MarriageService.RemoveFromBatch(b.Value, Convert.ToInt32(_items.CurrentRow.Cells["id"].Value)); Reload(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void ReturnRecord()
        {
            int id;
            if (_items.Focused && _items.CurrentRow != null) id = Convert.ToInt32(_items.CurrentRow.Cells["id"].Value);
            else if (_queue.CurrentRow != null) id = Convert.ToInt32(_queue.CurrentRow.Cells["id"].Value);
            else return;
            string why = MUi.Ask(this, "Returned by PSA/OCRG", "What did PSA/OCRG ask to be corrected?");
            if (why == null) return;
            try { MarriageService.ReturnItem(id, why); Reload(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void PrintTransmittal()
        {
            int? b = SelectedBatch(); if (!b.HasValue) return;
            DataRow batch = MarriageService.Batches().AsEnumerable().First(x => Convert.ToInt32(x["id"]) == b.Value);
            DataTable items = MarriageService.BatchItems(b.Value);
            string office = "", prov = "", muni = "";
            try { DataRow o = Db.Pull("SELECT * FROM office_profile LIMIT 1").Rows[0]; office = o["office_name"].ToString(); muni = o["municipality"].ToString(); prov = o["province"].ToString(); } catch { }
            int rowIdx = 0;
            var doc = new PrintDocument { DocumentName = "Transmittal " + batch["Batch"] };
            doc.BeginPrint += (s, e) => rowIdx = 0;
            doc.PrintPage += (s, e) =>
            {
                Graphics g = e.Graphics; Rectangle m = e.MarginBounds; float y = m.Top;
                using (var f = new Font("Segoe UI", 9F)) using (var fb = new Font("Segoe UI", 9F, FontStyle.Bold)) using (var ft = new Font("Segoe UI", 13F, FontStyle.Bold))
                {
                    if (rowIdx == 0)
                    {
                        g.DrawString(office + " - " + muni + ", " + prov, fb, Brushes.Black, m.Left, y); y += 20;
                        g.DrawString("TRANSMITTAL OF REGISTERED CERTIFICATES OF MARRIAGE", ft, Brushes.Black, m.Left, y); y += 28;
                        g.DrawString("Batch " + batch["Batch"] + "    Reporting period " + batch["Period"] + "    Records " + items.Rows.Count +
                                     "    Status " + batch["Status"], f, Brushes.Black, m.Left, y); y += 26;
                    }
                    float[] cx = { m.Left, m.Left + 130, m.Left + 460, m.Left + 580 };
                    string[] hd = { "Registry No.", "Contracting parties", "Date of marriage", "Remarks" };
                    for (int i = 0; i < hd.Length; i++) g.DrawString(hd[i], fb, Brushes.Black, cx[i], y);
                    y += 18; g.DrawLine(Pens.Black, m.Left, y, m.Right, y); y += 4;
                    while (rowIdx < items.Rows.Count && y < m.Bottom - 120)
                    {
                        DataRow r = items.Rows[rowIdx++];
                        g.DrawString(Convert.ToString(r["Registry No"]), f, Brushes.Black, cx[0], y);
                        g.DrawString(Convert.ToString(r["Couple"]), f, Brushes.Black, cx[1], y);
                        g.DrawString(r["Marriage Date"] == DBNull.Value ? "" : Convert.ToDateTime(r["Marriage Date"]).ToString("dd MMM yyyy", CultureInfo.InvariantCulture), f, Brushes.Black, cx[2], y);
                        g.DrawString(Convert.ToString(r["Item Status"]), f, Brushes.Black, cx[3], y);
                        y += 18;
                    }
                    if (rowIdx >= items.Rows.Count)
                    {
                        y += 40;
                        g.DrawLine(Pens.Black, m.Left, y, m.Left + 240, y); g.DrawLine(Pens.Black, m.Right - 240, y, m.Right, y);
                        g.DrawString("Prepared by (LCRO)", f, Brushes.Black, m.Left, y + 2); g.DrawString("Received by (PSA / OCRG) - date", f, Brushes.Black, m.Right - 240, y + 2);
                        e.HasMorePages = false;
                    }
                    else e.HasMorePages = true;
                }
            };
            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 1000 }) dlg.ShowDialog(this);
        }
    }
}
