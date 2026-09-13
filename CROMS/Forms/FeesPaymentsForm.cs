using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Fees &amp; Payments. The Municipal Treasury collects (the office's fee card is headed "PLS. PAY AT
    /// TREASURY OFFICE"); this screen records the Official Receipt it issued, what it covered and why.
    /// <list type="bullet">
    /// <item><b>Awaiting payment</b> - transactions at ForPayment. The fee is assessed from the
    /// office's schedule times the copies requested; recording it advances the transaction to
    /// ForRelease (the original flow, now itemised).</item>
    /// <item><b>Walk-in payment</b> - a payment that starts in no CROMS module: payer, purpose, and
    /// one or more fee lines on one O.R.</item>
    /// <item><b>Payment log</b> - every payment, from here, BREQS and marriage licences.</item>
    /// <item><b>Fee schedule</b> - the office's fees; amounts editable by Admin / Registrar, audited.</item>
    /// </list>
    /// The Awaiting-payment controls are declared in FeesPaymentsForm.Designer.cs; the other tabs are
    /// built here.
    /// </summary>
    public partial class FeesPaymentsForm : Form, IRefreshable
    {
        private int? _txnId;
        private PaymentLine _assessed;
        private bool _ready;
        private Receipt _last;

        private readonly TabControl _tabs = new TabControl();
        private TabPage _pgPending, _pgWalkIn, _pgLog, _pgMonthly, _pgFees;

        /// <summary>Everything the printed slip needs - captured at record time.</summary>
        private class Receipt
        {
            public string TxnCode, Client, Purpose, Method, Reference, Or, By, Remarks;
            public List<PaymentLine> Lines = new List<PaymentLine>();
            public decimal Additional, Total, Tendered, Change;
            public DateTime When;
        }

        public FeesPaymentsForm()
        {
            InitializeComponent();

            _cboMethod.Items.AddRange(PaymentService.Methods);
            _cboMethod.SelectedIndex = 0;
            _txtAdd.TextChanged += (s, e) => Recalc();
            _txtTendered.TextChanged += (s, e) => Recalc();
            lblDocFeeCap.Text = "Assessed Fee";
            // The designer caption ran under the bold total beside it ("Total Amou"), and a Label eats
            // '&' as a mnemonic, so "Date & Time Paid" drew as "Date  Time Paid".
            lblTotalCap.Text = "Total";
            _lblBy.UseMnemonic = false; _lblWhen.UseMnemonic = false;

            BuildTabs();
            _ready = true;
            UpdateReferenceField();
            LoadPending();
        }

        public void RefreshData()
        {
            LoadPending();
            if (_tabs.SelectedTab == _pgLog) LoadLog();
            if (_tabs.SelectedTab == _pgFees) LoadFees();
            if (_tabs.SelectedTab == _pgMonthly) LoadMonthly();
            if (_tabs.SelectedTab == _pgWalkIn) ReloadFeeCombo();
        }

        // ================================================================ tabs
        private void BuildTabs()
        {
            // Re-host the designer's pending-payment layout inside the first tab, in the same dock order.
            Controls.Remove(_dgv); Controls.Remove(pnlRight);
            _pgPending = new TabPage("Awaiting payment") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _pgPending.Controls.Add(pnlRight);
            _pgPending.Controls.Add(_dgv);

            _pgWalkIn = new TabPage("Walk-in / other payment") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            _pgLog = new TabPage("Payment log") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            _pgMonthly = new TabPage("Monthly collection") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            _pgFees = new TabPage("Fee schedule") { BackColor = UiTheme.PageBg, UseVisualStyleBackColor = false };
            BuildWalkIn(); BuildLog(); BuildMonthly(); BuildFees();

            _tabs.Dock = DockStyle.Fill;
            _tabs.Font = new Font("Segoe UI", 10F);
            _tabs.Padding = new Point(14, 6);
            _tabs.TabPages.AddRange(new[] { _pgPending, _pgWalkIn, _pgLog, _pgMonthly, _pgFees });
            _tabs.SelectedIndexChanged += (s, e) => RefreshData();
            Controls.Add(_tabs);
            _tabs.BringToFront();
        }

        // ================================================================ awaiting payment (transactions)
        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e) => OnSelect();
        private void method_Changed(object sender, EventArgs e) { UpdateReferenceField(); Recalc(); }
        private void btnPay_Click(object sender, EventArgs e) => RecordPayment();
        private void btnPrint_Click(object sender, EventArgs e) => PrintReceipt();

        private void UpdateReferenceField()
        {
            bool isCash = IsCash(_cboMethod.SelectedItem as string);
            _txtRef.Enabled = !isCash;
            if (isCash) _txtRef.Clear();
            lblRefCap.Text = isCash ? "Reference No. (not required for Cash)" : "Reference No. (required)";
            lblRefCap.ForeColor = isCash ? UiTheme.Muted : UiTheme.Accent;
        }

        private static bool IsCash(string method) { return (method ?? "Cash").StartsWith("Cash", StringComparison.OrdinalIgnoreCase); }

        private void LoadPending()
        {
            _dgv.DataSource = Db.Pull(
                "SELECT t.id, t.txn_code AS 'Txn Code', t.client_name AS Client, t.type AS Type, " +
                "COALESCE(c.copies, 1) AS Copies FROM transactions t LEFT JOIN certificate_requests c ON c.transaction_id = t.id " +
                "WHERE t.status = 'ForPayment' ORDER BY t.id");
            if (_dgv.Columns.Contains("id")) _dgv.Columns["id"].Visible = false;
        }

        private void OnSelect()
        {
            if (_dgv.CurrentRow == null) return;
            object idCell = _dgv.CurrentRow.Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            _txnId = Convert.ToInt32(idCell);
            _lblSel.Text = "Payment for:  " + _dgv.CurrentRow.Cells["Txn Code"].Value + "   -   " + _dgv.CurrentRow.Cells["Client"].Value;
            _lblBy.Text = "Processed By:  " + (Session.User != null ? Session.User.FullName : "-");
            _lblWhen.Text = "Date & Time Paid:  " + DateTime.Now.ToString("dd MMM yyyy  h:mm tt");
            AssessFee();
        }

        /// <summary>
        /// The request's fee from the office schedule, TIMES ITS COPIES. The earlier version charged one
        /// copy whatever was requested. A DB hiccup assesses nothing rather than crashing the window.
        /// </summary>
        private void AssessFee()
        {
            try
            {
                DataTable dt = Db.Pull("SELECT cert_type, record_type, copies FROM certificate_requests WHERE transaction_id = @t", new MySqlParameter("@t", _txnId));
                string ct = dt.Rows.Count > 0 && dt.Rows[0]["cert_type"] != DBNull.Value ? dt.Rows[0]["cert_type"].ToString() : "CTC";
                string rt = dt.Rows.Count > 0 && dt.Rows[0]["record_type"] != DBNull.Value ? dt.Rows[0]["record_type"].ToString() : "Birth";
                int copies = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["copies"]) : 1;
                _assessed = PaymentService.AssessCertificate(ct, rt, copies);
            }
            catch (Exception ex)
            {
                _assessed = new PaymentLine { Description = "Fee lookup failed: " + ex.Message, Quantity = 1, UnitAmount = 0m };
            }
            Recalc();
        }

        private static decimal ParseMoney(string s)
        {
            decimal v;
            return decimal.TryParse((s ?? "").Replace("PHP", "").Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }

        private decimal Additional() => Math.Max(0m, ParseMoney(_txtAdd.Text));
        private decimal Gross() => _assessed == null ? 0m : _assessed.LineAmount;
        private decimal Total() => Gross() + Additional();

        private void Recalc()
        {
            if (!_ready) return;
            decimal total = Total(), change = ParseMoney(_txtTendered.Text) - total;
            _lblDocFee.Text = _assessed == null ? "PHP 0.00"
                : "PHP " + Gross().ToString("N2") + (_assessed.Quantity > 1 ? "  (" + _assessed.Quantity + " x " + _assessed.UnitAmount.ToString("N2") + ")" : "");
            _lblTotal.Text = "PHP " + total.ToString("N2");
            _lblChange.Text = "PHP " + (change > 0 ? change : 0m).ToString("N2");
        }

        private void RecordPayment()
        {
            if (_txnId == null || _assessed == null)
            {
                MessageBox.Show("Select a transaction on the left first.", "Fees & Payments", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string method = _cboMethod.SelectedItem as string ?? "Cash";
            decimal tendered = ParseMoney(_txtTendered.Text);
            var entry = new PaymentEntry
            {
                TransactionId = _txnId, Source = PaymentService.SourceTransaction,
                PayerName = _dgv.CurrentRow != null ? Convert.ToString(_dgv.CurrentRow.Cells["Client"].Value) : null,
                Purpose = _assessed.Description, OrNumber = _txtOr.Text, Method = method, ReferenceNo = _txtRef.Text,
                Additional = Additional(), Tendered = IsCash(method) ? tendered : (decimal?)null, Remarks = _txtRemarks.Text,
                Lines = { _assessed }
            };
            // The cash check is a hard rule at the counter: an empty tendered box is not "not captured".
            if (IsCash(method) && tendered < entry.Total)
            {
                MessageBox.Show("Amount tendered is less than the total amount due.", "Insufficient payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            List<string> errors = PaymentService.Validate(entry);
            if (errors.Count > 0) { MessageBox.Show(string.Join("\n", errors), "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            long tid = _txnId.Value;
            try
            {
                PaymentService.Record(entry, Session.User == null ? (int?)null : Session.User.Id);
                Db.Push("UPDATE transactions SET status = 'ForRelease' WHERE id = @t", new MySqlParameter("@t", tid));

                _last = Snapshot(entry, _dgv.CurrentRow != null ? Convert.ToString(_dgv.CurrentRow.Cells["Txn Code"].Value) : "");
                btnPrint.Enabled = true;
                MessageBox.Show("Payment recorded.  Total PHP " + entry.Total.ToString("N2") +
                    (_last.Change > 0 ? "\nChange:  PHP " + _last.Change.ToString("N2") : "") + "\nSending to Release & Claim.", "Paid",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                PrintReceipt();

                Receipt keep = _last;
                ResetPanel();
                _last = keep; btnPrint.Enabled = true;
                LoadPending();

                MainForm shell = Shell();
                if (shell != null && shell.GoToModule("release") is ReleaseClaimForm rc) rc.PreselectTransaction(tid);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not record payment:\n" + ex.Message, "Not recorded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private Receipt Snapshot(PaymentEntry e, string txnCode)
        {
            decimal change = IsCash(e.Method) && e.Tendered.HasValue && e.Tendered.Value > e.Total ? e.Tendered.Value - e.Total : 0m;
            return new Receipt
            {
                TxnCode = txnCode, Client = e.PayerName, Purpose = e.Purpose, Method = e.Method, Reference = (e.ReferenceNo ?? "").Trim(),
                Or = e.OrNumber.Trim(), By = Session.User != null ? Session.User.FullName : "-", Remarks = (e.Remarks ?? "").Trim(),
                Lines = e.Lines.ToList(), Additional = e.Additional, Total = e.Total, Tendered = e.Tendered ?? 0m, Change = change, When = DateTime.Now
            };
        }

        /// <summary>Opens this module with a transaction pre-selected (from Certificate Request).</summary>
        public void PreselectTransaction(long txnId)
        {
            _tabs.SelectedTab = _pgPending;
            LoadPending();
            foreach (DataGridViewRow row in _dgv.Rows)
            {
                object v = row.Cells["id"].Value;
                if (v != null && v != DBNull.Value && Convert.ToInt64(v) == txnId)
                {
                    row.Selected = true;
                    _dgv.CurrentCell = row.Cells["Txn Code"];
                    OnSelect();
                    break;
                }
            }
        }

        private void ResetPanel()
        {
            _txnId = null; _assessed = null;
            _txtAdd.Text = "0.00"; _txtTendered.Text = "0.00";
            _txtOr.Clear(); _txtRef.Clear(); _txtRemarks.Clear();
            _cboMethod.SelectedIndex = 0;
            _lblSel.Text = "Select a payment on the left  ->";
            _lblDocFee.Text = "PHP 0.00"; _lblTotal.Text = "PHP 0.00"; _lblChange.Text = "PHP 0.00";
            _lblBy.Text = "Processed By:  -"; _lblWhen.Text = "Date & Time Paid:  -";
        }

        // ================================================================ walk-in payment
        private readonly TextBox _wPayer = MUi.Box(), _wOr = MUi.Box(), _wRef = MUi.Box(), _wTendered = MUi.Box(), _wAdditional = MUi.Box(), _wRemarks = MUi.Box();
        private readonly ComboBox _wPurpose = MUi.Combo(true, PaymentService.Purposes), _wMethod = MUi.Combo(false, PaymentService.Methods), _wFee = MUi.Combo(false);
        private readonly NumericUpDown _wQty = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Dock = DockStyle.Fill, Font = MUi.F(9.75F), Margin = new Padding(0, 0, 10, 0) };
        private readonly TextBox _wUnit = MUi.Box();
        private readonly DataGridView _wLines = new DataGridView();
        private readonly Label _wTotal = MUi.Txt("PHP 0.00", 16F, FontStyle.Bold, UiTheme.Accent), _wChange = MUi.Txt("", 10F, FontStyle.Bold, UiTheme.Success);
        private readonly List<PaymentLine> _wItems = new List<PaymentLine>();
        private Button _wPrint;

        private void BuildWalkIn()
        {
            var root = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 16, 24, 16), BackColor = UiTheme.PageBg };
            var card = MUi.Card(new Padding(22, 16, 22, 18));
            card.Dock = DockStyle.Top; card.Height = 700;

            var intro = MUi.SectionHeader("Walk-in / other payment",
                "For a payment that starts in no CROMS transaction. Record the Treasury O.R., who paid, why, and every fee it covered.");

            var who = MUi.Grid(2, 1, 58);
            who.Controls.Add(MUi.Field("Payer's name", _wPayer), 0, 0);
            who.Controls.Add(MUi.Field("Purpose of payment", _wPurpose), 1, 0);

            var add = new TableLayoutPanel { Height = 58, ColumnCount = 4, BackColor = Color.Transparent };
            add.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); add.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            add.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); add.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            add.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            add.Controls.Add(MUi.Field("Fee (office schedule)", _wFee), 0, 0);
            add.Controls.Add(MUi.Field("Qty", _wQty), 1, 0);
            add.Controls.Add(MUi.Field("Amount each (PHP)", _wUnit), 2, 0);
            var addBtn = MUi.Btn("+ Add fee", MUi.Kind.Secondary, 120);
            var addCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 0, 0), BackColor = Color.Transparent };
            addCell.Controls.Add(addBtn); addBtn.Dock = DockStyle.Left;
            add.Controls.Add(addCell, 3, 0);
            _wFee.SelectedIndexChanged += (s, e) => FeePicked();
            addBtn.Click += (s, e) => AddLine();

            _wLines.Height = 180; _wLines.ReadOnly = true; _wLines.AllowUserToAddRows = false; _wLines.RowHeadersVisible = false;
            _wLines.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _wLines.BackgroundColor = UiTheme.Surface; _wLines.BorderStyle = BorderStyle.None;
            _wLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            var remove = MUi.Btn("Remove selected fee", MUi.Kind.Ghost, 170);
            remove.Click += (s, e) => { if (_wLines.CurrentRow != null && _wLines.CurrentRow.Index < _wItems.Count) { _wItems.RemoveAt(_wLines.CurrentRow.Index); BindLines(); } };
            var removeRow = new FlowLayoutPanel { Height = 40, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
            removeRow.Controls.Add(remove);

            var money = MUi.Grid(4, 1, 58);
            money.Controls.Add(MUi.Field("Additional charge (PHP)", _wAdditional), 0, 0);
            money.Controls.Add(MUi.Field("Payment method", _wMethod), 1, 0);
            money.Controls.Add(MUi.Field("Reference no. (GCash / bank)", _wRef), 2, 0);
            money.Controls.Add(MUi.Field("Treasury O.R. no.", _wOr), 3, 0);
            var money2 = MUi.Grid(4, 1, 58);
            money2.Controls.Add(MUi.Field("Amount tendered (cash)", _wTendered), 0, 0);
            var remarksField = MUi.Field("Remarks", _wRemarks);
            money2.Controls.Add(remarksField, 1, 0); money2.SetColumnSpan(remarksField, 3);

            var totals = new FlowLayoutPanel { Height = 60, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
            totals.Controls.Add(MUi.Txt("TOTAL", 10F, FontStyle.Bold, UiTheme.Muted));
            totals.Controls.Add(_wTotal);
            _wChange.Margin = new Padding(24, 8, 0, 0);
            totals.Controls.Add(_wChange);

            var actions = new FlowLayoutPanel { Height = 50, BackColor = Color.Transparent };
            var record = MUi.Btn("Record payment", MUi.Kind.Success, 170);
            _wPrint = MUi.Btn("Print slip", MUi.Kind.Secondary, 120); _wPrint.Enabled = false;
            var clear = MUi.Btn("Clear", MUi.Kind.Ghost, 90);
            actions.Controls.AddRange(new Control[] { record, _wPrint, clear });
            record.Click += (s, e) => RecordWalkIn();
            _wPrint.Click += (s, e) => PrintReceipt();
            clear.Click += (s, e) => ClearWalkIn();

            var parts = new Control[] { intro, who, add, _wLines, removeRow, money, money2, totals, actions };
            for (int i = parts.Length - 1; i >= 0; i--) { parts[i].Dock = DockStyle.Top; card.Controls.Add(parts[i]); }
            for (int i = 0; i < parts.Length; i++) parts[i].TabIndex = i;
            card.Height = parts.Sum(p => p.Height) + 40;
            root.Controls.Add(card);
            _pgWalkIn.Controls.Add(root);

            _wMethod.SelectedIndex = 0;
            _wMethod.SelectedIndexChanged += (s, e) => { bool cash = IsCash(_wMethod.SelectedItem as string); _wRef.Enabled = !cash; if (cash) _wRef.Clear(); _wTendered.Enabled = cash; WalkInTotals(); };
            _wRef.Enabled = false;
            _wAdditional.Text = "0.00"; _wTendered.Text = "0.00";
            _wAdditional.TextChanged += (s, e) => WalkInTotals();
            _wTendered.TextChanged += (s, e) => WalkInTotals();
            ReloadFeeCombo();
            BindLines();
        }

        private void ReloadFeeCombo()
        {
            object keep = _wFee.SelectedItem;
            _wFee.Items.Clear();
            try { foreach (FeeItem f in PaymentService.Fees(true)) _wFee.Items.Add(f); } catch { }
            if (keep is FeeItem k) foreach (object o in _wFee.Items) if (((FeeItem)o).Code == k.Code) { _wFee.SelectedItem = o; break; }
        }

        /// <summary>
        /// A scheduled fee's amount is filled in and LOCKED - it is what the office charges. A fee with
        /// no amount set (burial permit, transfer of cadaver) is left open for the cashier to type.
        /// </summary>
        private void FeePicked()
        {
            var f = _wFee.SelectedItem as FeeItem;
            if (f == null) return;
            _wUnit.Text = f.Amount.HasValue ? f.Amount.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";
            _wUnit.ReadOnly = f.Amount.HasValue;
            _wUnit.BackColor = f.Amount.HasValue ? Color.FromArgb(245, 247, 250) : Color.White;
            if (!f.Amount.HasValue) _wUnit.Focus();
            if (string.IsNullOrWhiteSpace(_wPurpose.Text)) _wPurpose.Text = f.Description;
        }

        private void AddLine()
        {
            var f = _wFee.SelectedItem as FeeItem;
            if (f == null) { MessageBox.Show(this, "Choose a fee from the schedule.", "Walk-in payment", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            decimal unit = ParseMoney(_wUnit.Text);
            if (unit <= 0) { MessageBox.Show(this, f.Description + " has no amount on the schedule - type the amount on the receipt.", "Walk-in payment", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            _wItems.Add(new PaymentLine { FeeCode = f.Code, Description = f.Description, Quantity = (int)_wQty.Value, UnitAmount = unit });
            _wQty.Value = 1;
            BindLines();
        }

        private void BindLines()
        {
            var t = new DataTable();
            t.Columns.Add("Fee"); t.Columns.Add("Qty", typeof(int)); t.Columns.Add("Each"); t.Columns.Add("Amount");
            foreach (PaymentLine l in _wItems) t.Rows.Add(l.Description, l.Quantity, l.UnitAmount.ToString("N2"), l.LineAmount.ToString("N2"));
            _wLines.DataSource = t;
            if (_wLines.Columns.Contains("Fee")) _wLines.Columns["Fee"].FillWeight = 260;
            WalkInTotals();
        }

        private decimal WalkInTotal() { return _wItems.Sum(l => l.LineAmount) + Math.Max(0m, ParseMoney(_wAdditional.Text)); }

        private void WalkInTotals()
        {
            decimal total = WalkInTotal();
            _wTotal.Text = "PHP " + total.ToString("N2");
            decimal change = ParseMoney(_wTendered.Text) - total;
            _wChange.Text = IsCash(_wMethod.SelectedItem as string) && change > 0 && total > 0 ? "Change: PHP " + change.ToString("N2") : "";
        }

        private void RecordWalkIn()
        {
            string method = _wMethod.SelectedItem as string ?? "Cash";
            var entry = new PaymentEntry
            {
                Source = PaymentService.SourceWalkIn, PayerName = _wPayer.Text, Purpose = _wPurpose.Text, OrNumber = _wOr.Text, Method = method,
                ReferenceNo = _wRef.Text, Additional = Math.Max(0m, ParseMoney(_wAdditional.Text)),
                Tendered = IsCash(method) ? ParseMoney(_wTendered.Text) : (decimal?)null, Remarks = _wRemarks.Text, Lines = _wItems.ToList()
            };
            List<string> errors = PaymentService.Validate(entry);
            if (errors.Count > 0) { MessageBox.Show(this, string.Join("\n", errors.Select(x => "- " + x)), "Not recorded yet", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (!MUi.Confirm(this, "Record payment", "Record this payment?", "Payer|" + entry.PayerName.Trim(), "Purpose|" + entry.Purpose.Trim(),
                             "Fees|" + string.Join("; ", entry.Lines.Select(l => l.Description + (l.Quantity > 1 ? " x" + l.Quantity : ""))),
                             "Total|PHP " + entry.Total.ToString("N2"), "O.R.|" + entry.OrNumber.Trim()))
                return;
            try
            {
                PaymentService.Record(entry, Session.User == null ? (int?)null : Session.User.Id);
                _last = Snapshot(entry, "");
                _wPrint.Enabled = true; btnPrint.Enabled = true;
                PrintReceipt();
                ClearWalkIn();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Not recorded", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void ClearWalkIn()
        {
            _wItems.Clear(); BindLines();
            _wPayer.Clear(); _wPurpose.Text = ""; _wOr.Clear(); _wRef.Clear(); _wRemarks.Clear();
            _wAdditional.Text = "0.00"; _wTendered.Text = "0.00"; _wMethod.SelectedIndex = 0; _wFee.SelectedIndex = -1; _wUnit.Clear();
        }

        // ================================================================ payment log
        private readonly DateTimePicker _lFrom = MUi.Date(false), _lTo = MUi.Date(false);
        private readonly TextBox _lSearch = MUi.Box();
        private readonly DataGridView _lGrid = new DataGridView();
        private readonly Label _lSummary = MUi.Txt("", 10F, FontStyle.Bold);

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
        private readonly ComboBox _mMonth = MUi.Combo(false, CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12)), _mYear = MUi.Combo(false);
        private readonly Label _mSummary = MUi.Txt("", 10F, FontStyle.Bold), _mNote = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly DataGridView _mByFee = new DataGridView(), _mBySource = new DataGridView(), _mByMethod = new DataGridView();
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

        // ================================================================ fee schedule
        private readonly DataGridView _fGrid = new DataGridView();
        private Button _fSave;

        private static bool CanEditFees { get { return Session.User != null && (Session.User.Role == "Admin" || Session.User.Role == "Registrar"); } }

        private void BuildFees()
        {
            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = UiTheme.PageBg };
            var head = MUi.SectionHeader("Fee schedule",
                "From the office's fee card (PLS. PAY AT TREASURY OFFICE). A blank amount means the office has not stated one - the cashier types it. " +
                (CanEditFees ? "Change an amount and press Save; every change is audited." : "Only an Admin or Registrar can change amounts."));
            head.Dock = DockStyle.Top;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
            _fSave = MUi.Btn("Save changes", MUi.Kind.Primary, 140); _fSave.Enabled = CanEditFees;
            var reload = MUi.Btn("Discard changes", MUi.Kind.Secondary, 150);
            bar.Controls.Add(_fSave); bar.Controls.Add(reload);
            _fSave.Click += (s, e) => SaveFees();
            reload.Click += (s, e) => LoadFees();

            _fGrid.Dock = DockStyle.Fill; _fGrid.AllowUserToAddRows = false; _fGrid.AllowUserToDeleteRows = false; _fGrid.RowHeadersVisible = false;
            _fGrid.BackgroundColor = UiTheme.Surface; _fGrid.BorderStyle = BorderStyle.None; _fGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _fGrid.DataError += (s, e) => { e.ThrowException = false; MessageBox.Show(this, "Type an amount like 150.00, or leave it blank for 'not set'.", "Fee schedule", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            root.Controls.Add(_fGrid); root.Controls.Add(bar); root.Controls.Add(head);
            _pgFees.Controls.Add(root);
        }

        private void LoadFees()
        {
            try
            {
                var t = new DataTable();
                t.Columns.Add("Code"); t.Columns.Add("Fee"); t.Columns.Add("Category"); t.Columns.Add("Amount (PHP)", typeof(decimal));
                t.Columns.Add("Active", typeof(bool)); t.Columns.Add("On the office card");
                foreach (FeeItem f in PaymentService.Fees(false))
                    t.Rows.Add(f.Code, f.Description, f.Category, f.Amount.HasValue ? (object)f.Amount.Value : DBNull.Value, f.Active, f.CardNote);
                t.AcceptChanges();
                _fGrid.DataSource = t;
                foreach (DataGridViewColumn c in _fGrid.Columns) c.ReadOnly = !(CanEditFees && (c.Name == "Amount (PHP)" || c.Name == "Active"));
                _fGrid.Columns["Amount (PHP)"].DefaultCellStyle.Format = "N2";
                _fGrid.Columns["Amount (PHP)"].DefaultCellStyle.NullValue = "not set";
                _fGrid.Columns["Amount (PHP)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _fGrid.Columns["Fee"].FillWeight = 200; _fGrid.Columns["On the office card"].FillWeight = 180;
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Fee schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void SaveFees()
        {
            _fGrid.EndEdit();
            var t = _fGrid.DataSource as DataTable;
            if (t == null) return;
            DataTable changed = t.GetChanges(DataRowState.Modified);
            if (changed == null || changed.Rows.Count == 0) { MessageBox.Show(this, "Nothing changed.", "Fee schedule", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var lines = changed.AsEnumerable().Select(r =>
            {
                object before = r["Amount (PHP)", DataRowVersion.Original], after = r["Amount (PHP)"];
                return r["Fee"] + "|" + PaymentService.Money(before == DBNull.Value ? (decimal?)null : (decimal)before) + " -> " +
                       PaymentService.Money(after == DBNull.Value ? (decimal?)null : (decimal)after) + ((bool)r["Active"] ? "" : " (inactive)");
            }).ToArray();
            if (!MUi.Confirm(this, "Change fees", "Save these fee changes? They apply to every payment recorded from now on.", lines)) return;
            try
            {
                foreach (DataRow r in changed.Rows)
                    PaymentService.UpdateFee((string)r["Code"], r["Amount (PHP)"] == DBNull.Value ? (decimal?)null : (decimal)r["Amount (PHP)"], (bool)r["Active"],
                                             Session.User == null ? (int?)null : Session.User.Id);
                LoadFees();
                ReloadFeeCombo();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Fee schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        // ================================================================ printing (58mm slip)
        private void PrintReceipt()
        {
            if (_last == null) { MessageBox.Show("No payment to print yet.", "Print slip", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                var doc = new PrintDocument { DocumentName = "Payment Slip " + _last.Or };
                int h = 440 + _last.Lines.Count * 34 + (string.IsNullOrEmpty(_last.Reference) ? 0 : 22) + (string.IsNullOrEmpty(_last.Remarks) ? 0 : 40);
                doc.DefaultPageSettings.PaperSize = new PaperSize("Slip58", 228, h);
                doc.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
                doc.PrintPage += DrawReceipt;
                using (var dlg = new PrintDialog { Document = doc })
                    if (dlg.ShowDialog() == DialogResult.OK) doc.Print();
            }
            catch (Exception ex) { MessageBox.Show("Could not print the slip: " + ex.Message, "Print slip", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void DrawReceipt(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float w = e.PageBounds.Width, x = 8, y = 6;
            using (var fBig = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fHdr = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var fSml = new Font("Segoe UI", 7.5F, FontStyle.Regular))
            using (var fVal = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var fTot = new Font("Consolas", 11F, FontStyle.Bold))
            {
                var center = new StringFormat { Alignment = StringAlignment.Center };
                Action<string, Font> mid = (t, f) => { var sz = g.MeasureString(t, f, (int)(w - 16)); g.DrawString(t, f, Brushes.Black, new RectangleF(x, y, w - 16, sz.Height + 2), center); y += sz.Height + 2; };
                Action rule = () => { y += 2; using (var p = new Pen(Color.Gray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }) g.DrawLine(p, x, y, w - 8, y); y += 4; };
                Action<string, string> row = (lbl, val) =>
                {
                    g.DrawString(lbl, fSml, Brushes.Black, x, y);
                    var vs = g.MeasureString(val, fVal);
                    g.DrawString(val, fVal, Brushes.Black, w - 8 - vs.Width, y);
                    y += Math.Max(fSml.Height, fVal.Height) + 2;
                };
                OfficeProfile office = OfficeAssets.Profile;
                mid(office.OfficeName ?? "Local Civil Registry Office", fHdr);
                mid("Municipality of " + (office.Municipality ?? "") + ", " + (office.Province ?? ""), fSml);
                rule();
                mid("PAYMENT SLIP", fBig);
                mid("(not an Official Receipt)", fSml);
                rule();
                if (!string.IsNullOrEmpty(_last.TxnCode)) row("Txn Code", _last.TxnCode);
                row("Payer", string.IsNullOrEmpty(_last.Client) ? "-" : _last.Client);
                if (!string.IsNullOrEmpty(_last.Purpose)) { g.DrawString("Purpose: " + _last.Purpose, fSml, Brushes.Black, new RectangleF(x, y, w - 16, 30)); y += g.MeasureString("Purpose: " + _last.Purpose, fSml, (int)(w - 16)).Height + 2; }
                rule();
                foreach (PaymentLine l in _last.Lines)
                {
                    string d = l.Description + (l.Quantity > 1 ? "  x" + l.Quantity : "");
                    g.DrawString(d, fSml, Brushes.Black, new RectangleF(x, y, w - 16, 30));
                    y += g.MeasureString(d, fSml, (int)(w - 16)).Height;
                    row("", "P " + l.LineAmount.ToString("N2"));
                }
                if (_last.Additional > 0) row("Additional", "P " + _last.Additional.ToString("N2"));
                rule();
                g.DrawString("TOTAL", fVal, Brushes.Black, x, y + 4);
                var ts = g.MeasureString("P " + _last.Total.ToString("N2"), fTot);
                g.DrawString("P " + _last.Total.ToString("N2"), fTot, Brushes.Black, w - 8 - ts.Width, y);
                y += ts.Height + 4;
                rule();
                row("Method", _last.Method);
                if (!string.IsNullOrEmpty(_last.Reference)) row("Reference No.", _last.Reference);
                row("O.R. No.", string.IsNullOrEmpty(_last.Or) ? "-" : _last.Or);
                if (_last.Tendered > 0) row("Tendered", "P " + _last.Tendered.ToString("N2"));
                if (_last.Change > 0) row("Change", "P " + _last.Change.ToString("N2"));
                rule();
                row("Processed By", string.IsNullOrEmpty(_last.By) ? "-" : _last.By);
                row("Date & Time", _last.When.ToString("dd MMM yyyy h:mm tt"));
                if (!string.IsNullOrEmpty(_last.Remarks)) { y += 2; g.DrawString("Remarks: " + _last.Remarks, fSml, Brushes.Black, new RectangleF(x, y, w - 16, 40)); y += g.MeasureString("Remarks: " + _last.Remarks, fSml, (int)(w - 16)).Height + 2; }
                rule();
                mid("This slip is your proof of payment.", fSml);
                mid("Please keep it. Thank you!", fSml);
            }
        }

        // ================================================================ shared
        internal static void ExportCsv(DataTable t, string suggestedName)
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

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
