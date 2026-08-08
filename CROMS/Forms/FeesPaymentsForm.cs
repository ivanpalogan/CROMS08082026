using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Fees &amp; Payments — the cashier window. Lists transactions that are awaiting
    /// payment (status <b>ForPayment</b>), auto-assesses the document fee for the request
    /// type from the <c>fees</c> table, lets the cashier add an additional charge, pick a
    /// payment method, enter the tendered amount (change auto-computed) and the Official
    /// Receipt / reference number, records the payment, advances the transaction to
    /// <b>ForRelease</b>, and prints a small 58mm payment slip. Every recorded payment is
    /// Paid (no pending/cancelled/refunded — this office doesn't process those here).
    /// The UI is declared in FeesPaymentsForm.Designer.cs; this file holds only the data
    /// access + event handlers.
    /// </summary>
    public partial class FeesPaymentsForm : Form, IRefreshable
    {
        private int? _txnId;
        private string _feeName = "";
        private decimal _gross;   // document fee
        private bool _ready;
        private Receipt _last;    // snapshot of the last recorded payment (for reprint)

        /// <summary>Everything the printed slip needs — captured at record time.</summary>
        private class Receipt
        {
            public string TxnCode, Client, FeeName, Method, Reference, Or, By, Remarks;
            public decimal DocFee, Additional, Total, Tendered, Change;
            public DateTime When;
        }

        public FeesPaymentsForm()
        {
            InitializeComponent();

            _cboMethod.Items.AddRange(new object[] { "Cash", "GCash", "Bank Transfer" });
            _cboMethod.SelectedIndex = 0;

            _txtAdd.TextChanged += (s, e) => Recalc();
            _txtTendered.TextChanged += (s, e) => Recalc();

            _ready = true;
            UpdateReferenceField();   // Cash selected by default → reference disabled
            LoadPending();
        }

        public void RefreshData() => LoadPending();

        // ------------------------------------------------------------ event handlers
        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e) => OnSelect();
        private void method_Changed(object sender, EventArgs e) { UpdateReferenceField(); Recalc(); }

        /// <summary>
        /// Cash needs no GCash/Bank reference number, so the field is disabled and cleared.
        /// GCash / Bank Transfer enable it and mark it required (enforced in RecordPayment).
        /// </summary>
        private void UpdateReferenceField()
        {
            string method = _cboMethod.SelectedItem == null ? "Cash" : _cboMethod.SelectedItem.ToString();
            bool isCash = method.StartsWith("Cash", StringComparison.OrdinalIgnoreCase);
            _txtRef.Enabled = !isCash;
            if (isCash) _txtRef.Clear();
            lblRefCap.Text = isCash ? "Reference No. (not required for Cash)" : "Reference No. (required)";
            lblRefCap.ForeColor = isCash
                ? Color.FromArgb(73, 80, 87)
                : Color.FromArgb(13, 110, 253);
        }
        private void btnPay_Click(object sender, EventArgs e) => RecordPayment();
        private void btnPrint_Click(object sender, EventArgs e) => PrintReceipt(true);

        private void LoadPending()
        {
            _dgv.DataSource = Db.Pull(
                "SELECT id, txn_code AS 'Txn Code', client_name AS Client, type AS Type " +
                "FROM transactions WHERE status = 'ForPayment' ORDER BY id");
            if (_dgv.Columns.Contains("id")) _dgv.Columns["id"].Visible = false;
        }

        private void OnSelect()
        {
            if (_dgv.CurrentRow == null) return;
            object idCell = _dgv.CurrentRow.Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            _txnId = Convert.ToInt32(idCell);
            _lblSel.Text = "Payment for:  " + _dgv.CurrentRow.Cells["Txn Code"].Value +
                "   —   " + _dgv.CurrentRow.Cells["Client"].Value;
            _lblBy.Text = "Processed By:  " + (Session.User != null ? Session.User.FullName : "—");
            _lblWhen.Text = "Date & Time Paid:  " + DateTime.Now.ToString("dd MMM yyyy  h:mm tt");
            AssessFee();
        }

        /// <summary>
        /// Looks up the document fee for the selected transaction's request type. Maps the
        /// certificate request (cert_type + record_type) to a fee <c>code</c> in the
        /// `fees` table (e.g. Birth CTC -> CTC-BIRTH, Negative -> NEG-CERT) and pulls
        /// that row's amount + description. Any DB hiccup falls back to a 0 fee instead
        /// of throwing, so selecting a row never crashes the cashier window.
        /// </summary>
        private void AssessFee()
        {
            string feeCode = "CTC-BIRTH";
            _feeName = "Certified True Copy - Birth Certificate";
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT cert_type, record_type FROM certificate_requests WHERE transaction_id = @t",
                    new MySqlParameter("@t", _txnId));
                if (dt.Rows.Count > 0)
                {
                    string ct = dt.Rows[0]["cert_type"] == DBNull.Value ? "CTC" : dt.Rows[0]["cert_type"].ToString();
                    string rt = dt.Rows[0]["record_type"] == DBNull.Value ? "Birth" : dt.Rows[0]["record_type"].ToString();
                    feeCode = ct == "Negative" ? "NEG-CERT" : "CTC-" + rt.ToUpperInvariant();
                }

                DataTable f = Db.Pull(
                    "SELECT amount, description FROM fees WHERE code = @c AND is_active = 1",
                    new MySqlParameter("@c", feeCode));
                if (f.Rows.Count > 0)
                {
                    _gross = Convert.ToDecimal(f.Rows[0]["amount"]);
                    _feeName = f.Rows[0]["description"].ToString();
                }
                else
                {
                    _gross = 0m;
                    _feeName = feeCode + " (fee not found)";
                }
            }
            catch (Exception ex)
            {
                _gross = 0m;
                _feeName = "Fee lookup failed: " + ex.Message;
            }
            Recalc();
        }

        /// <summary>Reads a peso amount from a textbox; blank/garbage -> 0.</summary>
        private static decimal ParseMoney(string s)
        {
            decimal v;
            return decimal.TryParse((s ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }

        private decimal Additional() => Math.Max(0m, ParseMoney(_txtAdd.Text));
        private decimal Total() => _gross + Additional();
        private decimal Tendered() => ParseMoney(_txtTendered.Text);

        private void Recalc()
        {
            if (!_ready) return;
            decimal total = Total();
            decimal change = Tendered() - total;
            _lblDocFee.Text = "₱ " + _gross.ToString("N2");
            _lblTotal.Text = "₱ " + total.ToString("N2");
            _lblChange.Text = "₱ " + (change > 0 ? change : 0m).ToString("N2");
        }

        private void RecordPayment()
        {
            if (_txnId == null)
            {
                MessageBox.Show("Select a transaction on the left first.", "Fees & Payments",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string method = _cboMethod.SelectedItem == null ? "Cash" : _cboMethod.SelectedItem.ToString();
            bool isCash = method.StartsWith("Cash", StringComparison.OrdinalIgnoreCase);
            decimal total = Total();
            decimal tendered = Tendered();
            decimal change = tendered - total;

            if (string.IsNullOrWhiteSpace(_txtOr.Text))
            {
                MessageBox.Show("Enter the Official Receipt number.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (isCash && tendered < total)
            {
                MessageBox.Show("Amount tendered is less than the total amount due.", "Insufficient payment",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!isCash && string.IsNullOrWhiteSpace(_txtRef.Text))
            {
                MessageBox.Show("Enter the reference number for the " + method + " payment.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            long tid = _txnId.Value;
            decimal? tenderVal = tendered > 0 ? (decimal?)tendered : null;
            decimal? changeVal = (isCash && change > 0) ? (decimal?)change : null;

            try
            {
                Db.Push(
                    "INSERT INTO payments (transaction_id, or_number, reference_no, payment_method, " +
                    "gross_amount, additional_fee, net_amount, amount_tendered, change_amount, remarks, cashier_id) " +
                    "VALUES (@t, @or, @ref, @m, @g, @add, @n, @tend, @chg, @rem, @by)",
                    new MySqlParameter("@t", tid),
                    new MySqlParameter("@or", NullIfBlank(_txtOr.Text)),
                    new MySqlParameter("@ref", NullIfBlank(_txtRef.Text)),
                    new MySqlParameter("@m", method),
                    new MySqlParameter("@g", _gross),
                    new MySqlParameter("@add", Additional()),
                    new MySqlParameter("@n", total),
                    new MySqlParameter("@tend", (object)tenderVal ?? DBNull.Value),
                    new MySqlParameter("@chg", (object)changeVal ?? DBNull.Value),
                    new MySqlParameter("@rem", NullIfBlank(_txtRemarks.Text)),
                    new MySqlParameter("@by", Session.UserIdParam));

                Db.Push("UPDATE transactions SET status = 'ForRelease' WHERE id = @t",
                    new MySqlParameter("@t", tid));

                Audit.Write(Audit.Create, "payments", tid,
                    "Payment " + method + " OR " + _txtOr.Text.Trim() + " total PHP " + total.ToString("N2"));

                // Snapshot for the printed slip (+ reprint button).
                _last = new Receipt
                {
                    TxnCode = _dgv.CurrentRow != null ? Convert.ToString(_dgv.CurrentRow.Cells["Txn Code"].Value) : "",
                    Client = _dgv.CurrentRow != null ? Convert.ToString(_dgv.CurrentRow.Cells["Client"].Value) : "",
                    FeeName = _feeName,
                    Method = method,
                    Reference = _txtRef.Text.Trim(),
                    Or = _txtOr.Text.Trim(),
                    By = Session.User != null ? Session.User.FullName : "—",
                    Remarks = _txtRemarks.Text.Trim(),
                    DocFee = _gross,
                    Additional = Additional(),
                    Total = total,
                    Tendered = tendered,
                    Change = change > 0 ? change : 0m,
                    When = DateTime.Now
                };
                btnPrint.Enabled = true;

                MessageBox.Show("Payment recorded.  Total PHP " + total.ToString("N2") +
                    (isCash && change > 0 ? "\nChange:  PHP " + change.ToString("N2") : "") +
                    "\nSending to Release & Claim.", "Paid",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                PrintReceipt(false);   // offer to print the slip right away

                Receipt keep = _last;  // ResetPanel clears fields; keep the snapshot for reprint
                ResetPanel();
                _last = keep;
                btnPrint.Enabled = true;
                LoadPending();

                // Hand off to the releasing window with this transaction pre-selected.
                MainForm shell = Shell();
                if (shell != null && shell.GoToModule("release") is ReleaseClaimForm rc)
                    rc.PreselectTransaction(tid);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not record payment: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------ printing (58mm slip)
        private void PrintReceipt(bool reprint)
        {
            if (_last == null)
            {
                MessageBox.Show("No payment to print yet.", "Print Receipt",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var doc = new PrintDocument();
                doc.DocumentName = "Payment Slip " + _last.Or;
                // 58mm thermal roll: 228 hundredths-inch wide; height estimated from line count.
                int h = 430 + (string.IsNullOrEmpty(_last.Reference) ? 0 : 22) +
                        (string.IsNullOrEmpty(_last.Remarks) ? 0 : 40);
                doc.DefaultPageSettings.PaperSize = new PaperSize("Slip58", 228, h);
                doc.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
                doc.PrintPage += DrawReceipt;

                using (var dlg = new PrintDialog { Document = doc })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                        doc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not print the slip: " + ex.Message, "Print Receipt",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DrawReceipt(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float w = e.PageBounds.Width;
            float x = 8, y = 6;

            using (var fBig = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fHdr = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var fSml = new Font("Segoe UI", 7.5F, FontStyle.Regular))
            using (var fLbl = new Font("Segoe UI", 7.5F, FontStyle.Regular))
            using (var fVal = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var fTot = new Font("Consolas", 11F, FontStyle.Bold))
            {
                var center = new StringFormat { Alignment = StringAlignment.Center };

                Action<string, Font> mid = (t, f) =>
                {
                    var sz = g.MeasureString(t, f, (int)(w - 16));
                    g.DrawString(t, f, Brushes.Black, new RectangleF(x, y, w - 16, sz.Height + 2), center);
                    y += sz.Height + 2;
                };
                Action rule = () =>
                {
                    y += 2;
                    using (var p = new Pen(Color.Gray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        g.DrawLine(p, x, y, w - 8, y);
                    y += 4;
                };
                // label:value row (label left, value right)
                Action<string, string> row = (lbl, val) =>
                {
                    g.DrawString(lbl, fLbl, Brushes.Black, x, y);
                    var vs = g.MeasureString(val, fVal);
                    g.DrawString(val, fVal, Brushes.Black, w - 8 - vs.Width, y);
                    y += Math.Max(fLbl.Height, fVal.Height) + 2;
                };

                mid("CROMS — LCRO Peñablanca", fHdr);
                mid("Local Civil Registry Office", fSml);
                mid("Municipality of Peñablanca, Cagayan", fSml);
                rule();
                mid("PAYMENT SLIP", fBig);
                mid("(not an Official Receipt)", fSml);
                rule();

                row("Txn Code", string.IsNullOrEmpty(_last.TxnCode) ? "—" : _last.TxnCode);
                row("Client", string.IsNullOrEmpty(_last.Client) ? "—" : _last.Client);
                // fee description wraps under its own small line
                g.DrawString(_last.FeeName, fSml, Brushes.Black, new RectangleF(x, y, w - 16, 40));
                y += g.MeasureString(_last.FeeName, fSml, (int)(w - 16)).Height + 4;
                rule();

                row("Document Fee", "P " + _last.DocFee.ToString("N2"));
                if (_last.Additional > 0) row("Additional Fee", "P " + _last.Additional.ToString("N2"));
                rule();

                // total, prominent
                g.DrawString("TOTAL", fVal, Brushes.Black, x, y + 4);
                var ts = g.MeasureString("P " + _last.Total.ToString("N2"), fTot);
                g.DrawString("P " + _last.Total.ToString("N2"), fTot, Brushes.Black, w - 8 - ts.Width, y);
                y += ts.Height + 4;
                rule();

                row("Method", _last.Method);
                if (!string.IsNullOrEmpty(_last.Reference)) row("Reference No.", _last.Reference);
                row("O.R. No.", string.IsNullOrEmpty(_last.Or) ? "—" : _last.Or);
                if (_last.Tendered > 0) row("Tendered", "P " + _last.Tendered.ToString("N2"));
                if (_last.Change > 0) row("Change", "P " + _last.Change.ToString("N2"));
                rule();

                row("Processed By", string.IsNullOrEmpty(_last.By) ? "—" : _last.By);
                row("Date & Time", _last.When.ToString("dd MMM yyyy h:mm tt"));
                if (!string.IsNullOrEmpty(_last.Remarks))
                {
                    y += 2;
                    g.DrawString("Remarks: " + _last.Remarks, fSml, Brushes.Black,
                        new RectangleF(x, y, w - 16, 40));
                    y += g.MeasureString("Remarks: " + _last.Remarks, fSml, (int)(w - 16)).Height + 2;
                }
                rule();
                mid("This slip is your proof of payment.", fSml);
                mid("Please keep it. Thank you!", fSml);
            }
        }

        private static object NullIfBlank(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        /// <summary>Opens this module with a transaction pre-selected (from Certificate Request).</summary>
        public void PreselectTransaction(long txnId)
        {
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
            _txnId = null;
            _feeName = "";
            _gross = 0m;
            _txtAdd.Text = "0.00";
            _txtTendered.Text = "0.00";
            _txtOr.Clear();
            _txtRef.Clear();
            _txtRemarks.Clear();
            _cboMethod.SelectedIndex = 0;
            _lblSel.Text = "Select a payment on the left  →";
            _lblDocFee.Text = "₱ 0.00";
            _lblTotal.Text = "₱ 0.00";
            _lblChange.Text = "₱ 0.00";
            _lblBy.Text = "Processed By:  —";
            _lblWhen.Text = "Date & Time Paid:  —";
        }

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
