using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Certificate Request — intake for a Certified True Copy (CTC) or a Negative
    /// Certification. Creating a request opens a <b>transaction</b> (type Certification,
    /// status ForRelease) and links a certificate_requests row to it — that transaction
    /// is what Release &amp; Claim later closes, and what Transactions lists. Controls in
    /// the Designer.
    /// </summary>
    public partial class CertificateRequestForm : Form, IRefreshable
    {
        // Set when Queue Management hands off a "Request CTC" ticket; a created
        // request is then written back to this queue ticket for traceability.
        private int _queueTicketId;
        private string _queueTicketCode;
        private System.Windows.Forms.Label lblQueueRef;
        private System.Windows.Forms.Label lblPhotoCap;
        private System.Windows.Forms.PictureBox picClient;

        public void RefreshData() => LoadRequests();

        /// <summary>
        /// Called by Queue Management when a "Request CTC" ticket is served: opens a
        /// fresh form, shows the linked ticket, and remembers it so the created
        /// request ties back to the queue.
        /// </summary>
        public void PrepareForQueueTicket(int ticketId, string ticketCode)
        {
            ClearForm();
            _queueTicketId = ticketId;
            _queueTicketCode = ticketCode;
            EnsureQueueRefLabel();
            EnsurePhotoBox();

            // Pre-fill from what the client entered on the kiosk (name, purpose, photo).
            string contact = "";
            System.Data.DataTable dt = Db.Pull(
                "SELECT full_name, purpose, contact_no, id_image FROM queue_tickets WHERE id = " + ticketId);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                FillName(Text2(row["full_name"]));
                txtPurpose.Text = Text2(row["purpose"]);
                contact = Text2(row["contact_no"]);
                ShowPhoto(row["id_image"]);
            }

            lblQueueRef.Text = "Linked to queue ticket:  " + ticketCode +
                (contact.Length > 0 ? "     ·     Contact: " + contact : "");
            lblQueueRef.Visible = true;
            picClient.Visible = true;
            lblPhotoCap.Visible = true;
            txtFirst.Focus();
        }

        private static string Text2(object v) => v == null || v == System.DBNull.Value ? "" : v.ToString();

        /// <summary>Splits "First Middle Last" back into the three name boxes.</summary>
        private void FillName(string full)
        {
            string[] p = (full ?? "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 0) return;
            if (p.Length == 1) { txtFirst.Text = p[0]; }
            else if (p.Length == 2) { txtFirst.Text = p[0]; txtLast.Text = p[1]; }
            else
            {
                txtFirst.Text = p[0];
                txtLast.Text = p[p.Length - 1];
                txtMiddle.Text = string.Join(" ", p, 1, p.Length - 2);
            }
        }

        /// <summary>Lazily builds the panel that shows the client's captured photo.</summary>
        private void EnsurePhotoBox()
        {
            if (picClient != null) return;
            lblPhotoCap = new System.Windows.Forms.Label
            {
                Text = "Client Photo (from kiosk)",
                AutoSize = true,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(73, 80, 87),
                Location = new System.Drawing.Point(1168, 90),
                Visible = false
            };
            picClient = new System.Windows.Forms.PictureBox
            {
                Location = new System.Drawing.Point(1168, 112),
                Size = new System.Drawing.Size(260, 283),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                BackColor = System.Drawing.Color.FromArgb(248, 249, 250),
                Visible = false
            };
            Controls.Add(lblPhotoCap);
            Controls.Add(picClient);
            lblPhotoCap.BringToFront();
            picClient.BringToFront();
        }

        private void ShowPhoto(object idImage)
        {
            picClient.Image = null;
            if (idImage == null || idImage == System.DBNull.Value) return;
            try
            {
                byte[] bytes = (byte[])idImage;
                using (var ms = new System.IO.MemoryStream(bytes))
                    picClient.Image = System.Drawing.Image.FromStream(ms);
            }
            catch { /* stored value wasn't a readable image */ }
        }

        /// <summary>Lazily builds the small badge that shows the linked queue ticket.</summary>
        private void EnsureQueueRefLabel()
        {
            if (lblQueueRef != null) return;
            lblQueueRef = new System.Windows.Forms.Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(13, 110, 253),
                Location = new System.Drawing.Point(530, 266),
                Name = "lblQueueRef",
                Visible = false
            };
            grpDetails.Controls.Add(lblQueueRef);
        }

        /// <summary>Forgets any linked queue ticket and hides its badge.</summary>
        private void ResetQueueLink()
        {
            _queueTicketId = 0;
            _queueTicketCode = null;
            if (lblQueueRef != null) lblQueueRef.Visible = false;
            if (picClient != null) { picClient.Image = null; picClient.Visible = false; }
            if (lblPhotoCap != null) lblPhotoCap.Visible = false;
        }

        /// <summary>Joins the three name parts into "First Middle Last", skipping blanks.</summary>
        private string FullClientName()
        {
            string[] parts = { txtFirst.Text.Trim(), txtMiddle.Text.Trim(), txtLast.Text.Trim() };
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadRequests();

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }

        public CertificateRequestForm()
        {
            InitializeComponent();
            cboCertType.Items.AddRange(new object[] { "CTC", "Negative" });
            cboCertType.SelectedItem = "CTC";
            cboRecordType.Items.AddRange(new object[] { "Birth", "Marriage", "Death" });
            cboRecordType.SelectedIndexChanged += (s, e) => LoadRecords();
            LoadRequests();
            LearningLibrary.Attach(txtFirst, LearningLibrary.GivenName);
            LearningLibrary.Attach(txtLast, LearningLibrary.Surname);
        }

        /// <summary>Fills the Record dropdown with records of the chosen type.</summary>
        private void LoadRecords()
        {
            string type = cboRecordType.SelectedItem?.ToString();
            string sql;
            switch (type)
            {
                case "Birth":
                    sql = "SELECT id, TRIM(CONCAT(last_name, ', ', first_name)) AS name FROM births ORDER BY last_name";
                    break;
                case "Death":
                    sql = "SELECT id, full_name AS name FROM deaths ORDER BY full_name";
                    break;
                case "Marriage":
                    sql = "SELECT id, TRIM(CONCAT(husband_last_name, ' & ', wife_last_name)) AS name FROM marriages ORDER BY id DESC";
                    break;
                default:
                    cboRecord.DataSource = null;
                    return;
            }
            DataTable dt = Db.Pull(sql);
            cboRecord.DataSource = dt;
            cboRecord.DisplayMember = "name";
            cboRecord.ValueMember = "id";
            // Re-assert type-to-filter autocomplete after the DataSource swap (a rebind
            // can drop the ListItems source), so typing part of a name filters the list.
            cboRecord.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cboRecord.AutoCompleteSource = AutoCompleteSource.ListItems;
            cboRecord.SelectedIndex = -1;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("First name and last name are required.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string txnCode = NextTxnCode();
            string clientName = FullClientName();   // captured before ClearForm() wipes the boxes
            int copies = int.TryParse(txtCopies.Text, out int c) && c > 0 ? c : 1;
            object recordType = cboRecordType.SelectedItem == null
                ? (object)DBNull.Value : cboRecordType.SelectedItem.ToString();
            object recordId = cboRecord.SelectedValue is int rid ? (object)rid : DBNull.Value;

            try
            {
                // 1) open the transaction (the spine), stamped with who created it. The new
                //    flow starts at ForPrint — staff first locate + print the certificate
                //    BEFORE any payment is assessed.
                long txnId = Db.Insert(
                    "INSERT INTO transactions (txn_code, client_name, type, status, created_by) " +
                    "VALUES (@code, @client, 'Certification', 'ForPrint', @by)",
                    new MySqlParameter("@code", txnCode),
                    new MySqlParameter("@client", clientName),
                    new MySqlParameter("@by", Session.UserIdParam));

                // 2) link the certificate request to it
                Db.Push(
                    "INSERT INTO certificate_requests " +
                    "(transaction_id, record_type, record_id, cert_type, copies, purpose, status) " +
                    "VALUES (@txn, @rt, @rid, @ct, @copies, @purpose, 'Processing')",
                    new MySqlParameter("@txn", txnId),
                    new MySqlParameter("@rt", recordType),
                    new MySqlParameter("@rid", recordId),
                    new MySqlParameter("@ct", cboCertType.SelectedItem?.ToString() ?? "CTC"),
                    new MySqlParameter("@copies", copies),
                    new MySqlParameter("@purpose", string.IsNullOrWhiteSpace(txtPurpose.Text)
                        ? (object)DBNull.Value : txtPurpose.Text.Trim()));

                // 3) if this came from a called queue ticket, tie it to the txn. The queue
                //    number becomes the pull key: if the request is later parked, the client
                //    types this number back at the kiosk to reclaim it.
                string queueCode = _queueTicketCode;
                if (_queueTicketId > 0)
                    Db.Push(
                        "UPDATE queue_tickets SET transaction_id = @txn, status = 'Completed', " +
                        "window_no = NULL WHERE id = @tid",
                        new MySqlParameter("@txn", txnId),
                        new MySqlParameter("@tid", _queueTicketId));

                Audit.Write(Audit.Create, "transactions", txnId,
                    "Certificate request " + txnCode + " for " + clientName);

                ClearForm();
                ResetQueueLink();
                LoadRequests();

                // 4) Find / Print step — locate the record, print the CTC, then decide:
                //    proceed to payment (found) OR park to Waiting-to-Release (hard to find
                //    / client left). Payment is never assessed until the cert is produced.
                string recTypeStr = recordType == DBNull.Value ? null : recordType.ToString();
                int recIdInt = recordId is int ri ? ri : 0;
                string certType = cboCertType.SelectedItem?.ToString() ?? "CTC";

                using (var dlg = new CertificatePrintForm(
                    txnId, txnCode, clientName, recTypeStr, recIdInt, certType, copies, queueCode))
                {
                    dlg.ShowDialog(this);

                    if (dlg.Result == CertNextStep.ProceedToPayment)
                    {
                        Db.Push("UPDATE transactions SET status = 'ForPayment' WHERE id = @t",
                            new MySqlParameter("@t", txnId));
                        Db.Push("UPDATE certificate_requests SET status = 'Ready' WHERE transaction_id = @t",
                            new MySqlParameter("@t", txnId));
                        MainForm shell = Shell();
                        if (shell != null && shell.GoToModule("fees") is FeesPaymentsForm fp)
                            fp.PreselectTransaction(txnId);
                    }
                    else if (dlg.Result == CertNextStep.WaitingToRelease)
                    {
                        Db.Push(
                            "UPDATE transactions SET status = 'WaitingToRelease', parked_at = NOW(), " +
                            "parked_reason = @r, parked_ticket = @tk WHERE id = @t",
                            new MySqlParameter("@r", (object)dlg.ParkReason ?? DBNull.Value),
                            new MySqlParameter("@tk", (object)queueCode ?? DBNull.Value),
                            new MySqlParameter("@t", txnId));
                        Audit.Write(Audit.Update, "transactions", txnId,
                            "Parked to Waiting-to-Release" +
                            (queueCode != null ? " (queue " + queueCode + ")" : "") +
                            (dlg.ParkReason != null ? " — " + dlg.ParkReason : ""));
                        MessageBox.Show(
                            "Request held at Waiting-to-Release.\n\n" +
                            "Tell the client to KEEP their queue ticket" +
                            (queueCode != null ? "  (" + queueCode + ")" : "") +
                            ".\nWhen they return, they go to the kiosk → Release & Claim → " +
                            "enter that number to reclaim this request.",
                            "Waiting to Release", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    // dialog closed with no choice → stays ForPrint (shows in the
                    // Release & Claim "Waiting to Release" list to finish later).

                    LoadRequests();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create request: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            ResetQueueLink();
        }

        private void ClearForm()
        {
            txtFirst.Clear();
            txtMiddle.Clear();
            txtLast.Clear();
            cboCertType.SelectedItem = "CTC";
            cboRecordType.SelectedIndex = -1;
            cboRecord.DataSource = null;
            txtCopies.Text = "1";
            txtPurpose.Clear();
        }

        private void LoadRequests()
        {
            dgvReq.DataSource = Db.Pull(
                "SELECT t.txn_code AS 'Txn Code', t.client_name AS Client, c.cert_type AS 'Cert Type', " +
                "COALESCE(c.record_type,'—') AS 'Record', c.copies AS Copies, t.status AS Status, " +
                "c.created_at AS Created " +
                "FROM certificate_requests c JOIN transactions t ON t.id = c.transaction_id " +
                "ORDER BY c.id DESC");
        }

        private static string NextTxnCode()
        {
            int year = DateTime.Now.Year;
            // Use the highest sequence actually in use for the year (+1), not a row
            // count — counts collide when codes have gaps (e.g. 000001, 000003).
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(txn_code, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM transactions WHERE txn_code LIKE 'TXN-" + year + "-%'");
            int n = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value)
                ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("TXN-{0}-{1:D6}", year, n);
        }
    }

    /// <summary>What the officer chose on the Find/Print step.</summary>
    internal enum CertNextStep { None, ProceedToPayment, WaitingToRelease }

    /// <summary>
    /// Find / Print Certificate — the step that now sits between Create Request and Fees.
    /// The officer locates the record, prints the Certified True Copy on a real printer,
    /// and then either proceeds to payment (certificate produced) or parks the request to
    /// Waiting-to-Release (certificate hard to find, or the client had to leave). No fee is
    /// assessed until the certificate is actually produced.
    /// </summary>
    internal class CertificatePrintForm : Form
    {
        private readonly long _txnId;
        private readonly string _txnCode, _client, _recordType, _certType, _queueCode;
        private readonly int _recordId, _copies;
        private DataRow _record;     // the located record row (null until Find runs)
        private int _recalls;        // how many times the client's number has been called
        private System.Speech.Synthesis.SpeechSynthesizer _voice;

        public CertNextStep Result { get; private set; } = CertNextStep.None;
        public string ParkReason { get; private set; }

        private readonly Label _lblFound;
        private readonly Button _btnPrint, _btnPay, _btnCall;

        public CertificatePrintForm(long txnId, string txnCode, string client,
            string recordType, int recordId, string certType, int copies, string queueCode)
        {
            _txnId = txnId; _txnCode = txnCode; _client = client;
            _recordType = recordType; _recordId = recordId; _certType = certType; _copies = copies;
            _queueCode = queueCode;

            Text = "Find / Print Certificate";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            ClientSize = new Size(560, 476);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var head = new Label
            {
                Text = "Step 2 — Find & Print the Certificate",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(24, 18), AutoSize = true
            };
            var sub = new Label
            {
                Text = "Locate the record and print the Certified True Copy before payment.",
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(26, 52), AutoSize = true
            };

            var info = new Label
            {
                Location = new Point(26, 88), Size = new Size(508, 96),
                Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(52, 58, 64),
                Text =
                    "Transaction:  " + _txnCode + "\r\n" +
                    "Client:  " + _client + "\r\n" +
                    "Certificate:  " + _certType + "  " + (_recordType ?? "—") +
                    "     Copies:  " + _copies
            };

            _lblFound = new Label
            {
                Location = new Point(26, 190), Size = new Size(508, 40),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                Text = _recordId > 0
                    ? "Record is linked. Click \"Find & Print\" to print the certificate."
                    : "⚠ No record was picked on the request. You can still park it to Waiting-to-Release and locate the record later."
            };

            _btnPrint = new Button
            {
                Text = "🖨  Find & Print Certificate",
                Location = new Point(26, 236), Size = new Size(508, 46),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPrint.FlatAppearance.BorderSize = 0;
            _btnPrint.Click += (s, e) => FindAndPrint();

            // Call / Recall the client's queue number to the window (voice callout). Use it
            // when the client stepped away; if they still don't come, park the request.
            _btnCall = new Button
            {
                Text = _queueCode != null
                    ? "📢  Call Client  (" + _queueCode + ")"
                    : "📢  Call Client",
                Location = new Point(26, 290), Size = new Size(508, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(102, 16, 242),
                ForeColor = Color.White, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCall.FlatAppearance.BorderSize = 0;
            _btnCall.Click += (s, e) => CallClient();

            _btnPay = new Button
            {
                Text = "✔  Certificate ready — Proceed to Payment",
                Location = new Point(26, 346), Size = new Size(508, 46),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 135, 84),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPay.FlatAppearance.BorderSize = 0;
            _btnPay.Click += (s, e) => { Result = CertNextStep.ProceedToPayment; Close(); };

            var btnPark = new Button
            {
                Text = "⏸  No-show — Put to Waiting-to-Release",
                Location = new Point(26, 406), Size = new Size(360, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(233, 236, 239),
                ForeColor = Color.FromArgb(33, 37, 41), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPark.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnPark.Click += (s, e) => Park();

            var btnCancel = new Button
            {
                Text = "Close",
                Location = new Point(396, 406), Size = new Size(138, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.White,
                ForeColor = Color.FromArgb(73, 80, 87), Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnCancel.Click += (s, e) => Close();

            Controls.Add(head); Controls.Add(sub); Controls.Add(info);
            Controls.Add(_lblFound); Controls.Add(_btnPrint); Controls.Add(_btnCall);
            Controls.Add(_btnPay); Controls.Add(btnPark); Controls.Add(btnCancel);

            FormClosed += (s, e) => { try { _voice?.Dispose(); } catch { } };
        }

        /// <summary>
        /// Voice-calls the client's queue number to the window (waiting-area callout, same
        /// System.Speech engine the queue board uses). Guarded — a PC with no audio never
        /// crashes. After the 2nd call, the button hints that a no-show should be parked.
        /// </summary>
        private void CallClient()
        {
            _recalls++;
            try
            {
                if (_voice == null) _voice = new System.Speech.Synthesis.SpeechSynthesizer();
                string who = _queueCode != null ? Spell(_queueCode) : _client;
                _voice.SpeakAsync("Now serving " + who + ", please proceed to the window.");
            }
            catch { /* no audio device — the on-screen prompt still updates */ }

            _btnCall.Text = _queueCode != null
                ? "📢  Recall Client  (" + _queueCode + ")   ·  called " + _recalls + "×"
                : "📢  Recall Client   ·  called " + _recalls + "×";
            _lblFound.Text = _recalls >= 2
                ? "Called " + _recalls + " times. If the client does not come, use \"No-show — Put to Waiting-to-Release\"."
                : "Client called. Waiting for them at the window…";
            _lblFound.ForeColor = Color.FromArgb(102, 16, 242);
        }

        // "Q-006" reads better spoken as "Q 0 0 6".
        private static string Spell(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in code.Replace('-', ' '))
                sb.Append(char.IsDigit(c) ? " " + c : c.ToString());
            return sb.ToString();
        }

        private void Park()
        {
            using (var d = new Form
            {
                Text = "Reason (optional)", FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(420, 150),
                MinimizeBox = false, MaximizeBox = false, BackColor = Color.White
            })
            {
                var l = new Label { Text = "Why is this held? (optional)", Location = new Point(16, 14), AutoSize = true };
                var tb = new TextBox { Location = new Point(16, 40), Size = new Size(388, 25) };
                var cbo = new ComboBox
                {
                    Location = new Point(16, 40), Size = new Size(388, 25),
                    DropDownStyle = ComboBoxStyle.DropDown
                };
                cbo.Items.AddRange(new object[]
                {
                    "Certificate hard to find", "Client had to leave / come back later",
                    "Record needs verification", "Other"
                });
                var ok = new Button
                {
                    Text = "Hold at Waiting-to-Release", Location = new Point(150, 96), Size = new Size(254, 36),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(13, 110, 253),
                    ForeColor = Color.White, DialogResult = DialogResult.OK
                };
                ok.FlatAppearance.BorderSize = 0;
                d.Controls.Add(l); d.Controls.Add(cbo); d.Controls.Add(ok);
                d.AcceptButton = ok;
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    ParkReason = string.IsNullOrWhiteSpace(cbo.Text) ? null : cbo.Text.Trim();
                    Result = CertNextStep.WaitingToRelease;
                    Close();
                }
            }
        }

        // ---------------------------------------------------------- find + print
        private void FindAndPrint()
        {
            if (_recordId <= 0 || string.IsNullOrEmpty(_recordType))
            {
                MessageBox.Show(
                    "No record is linked to this request, so there is nothing to print yet.\n\n" +
                    "Park it to Waiting-to-Release, locate the record, then finish it from Release & Claim.",
                    "No record", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_record == null && !LoadRecord())
            {
                _lblFound.Text = "❌ Record not found in the registry. Park it and verify the record.";
                _lblFound.ForeColor = Color.FromArgb(200, 35, 51);
                return;
            }

            try
            {
                var doc = new PrintDocument { DocumentName = "CTC " + _txnCode };
                doc.PrintPage += DrawCertificate;
                using (var dlg = new PrintDialog { Document = doc })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        doc.Print();
                        _lblFound.Text = "✔ Certificate found and printed. Proceed to payment when the client is ready.";
                        _lblFound.ForeColor = Color.FromArgb(25, 135, 84);
                        Audit.Write(Audit.Update, "transactions", _txnId, "Certificate printed (CTC)");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not print: " + ex.Message, "Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Loads the linked record row from births/deaths/marriages.</summary>
        private bool LoadRecord()
        {
            string table = _recordType == "Birth" ? "births"
                         : _recordType == "Death" ? "deaths"
                         : _recordType == "Marriage" ? "marriages" : null;
            if (table == null) return false;
            DataTable dt = Db.Pull("SELECT * FROM " + table + " WHERE id = @id",
                new MySqlParameter("@id", _recordId));
            if (dt.Rows.Count == 0) return false;
            _record = dt.Rows[0];
            return true;
        }

        private void DrawCertificate(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left, right = e.MarginBounds.Right;
            float w = e.MarginBounds.Width;
            float y = e.MarginBounds.Top;

            using (var fHead = new Font("Times New Roman", 13F, FontStyle.Bold))
            using (var fSub = new Font("Times New Roman", 10F))
            using (var fTitle = new Font("Times New Roman", 16F, FontStyle.Bold))
            using (var fLbl = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fVal = new Font("Segoe UI", 10F))
            using (var fBody = new Font("Times New Roman", 11F))
            using (var center = new StringFormat { Alignment = StringAlignment.Center })
            {
                Action<string, Font> mid = (t, f) =>
                {
                    var sz = g.MeasureString(t, f, (int)w);
                    g.DrawString(t, f, Brushes.Black, new RectangleF(left, y, w, sz.Height), center);
                    y += sz.Height + 2;
                };
                Action rule = () => { y += 4; g.DrawLine(Pens.Black, left, y, right, y); y += 8; };
                Action<string, string> row = (lbl, val) =>
                {
                    g.DrawString(lbl, fLbl, Brushes.Black, left, y);
                    g.DrawString(val ?? "—", fVal, Brushes.Black, left + 190, y);
                    y += 22;
                };

                mid("Republic of the Philippines", fSub);
                mid("Municipality of Peñablanca, Cagayan", fHead);
                mid("LOCAL CIVIL REGISTRY OFFICE", fSub);
                y += 8;
                mid(_certType == "Negative" ? "CERTIFICATION" : "CERTIFIED TRUE COPY", fTitle);
                mid("(" + (_recordType ?? "") + " Record)", fSub);
                rule();

                foreach (var kv in RecordLines()) row(kv.Key, kv.Value);
                rule();

                string stmt = _certType == "Negative"
                    ? "This is to certify that after a diligent search of the registry of this office, " +
                      "no record of the above-stated event was found registered."
                    : "This is to certify that the foregoing is a true and faithful reproduction of the " +
                      "entry appearing in the Register of this office.";
                var box = new RectangleF(left, y, w, 60);
                g.DrawString(stmt, fBody, Brushes.Black, box);
                y += 66;

                row("Transaction No.", _txnCode);
                row("Requested by", _client);
                row("Copies", _copies.ToString());
                row("Date issued", DateTime.Now.ToString("dd MMMM yyyy"));
                y += 40;

                float sx = right - 240;
                g.DrawLine(Pens.Black, sx, y, right, y);
                y += 4;
                g.DrawString("Municipal Civil Registrar", fVal, Brushes.Black, sx, y);

                y = e.MarginBounds.Bottom - 20;
                g.DrawString("CROMS — printed " + DateTime.Now.ToString("g"),
                    new Font("Segoe UI", 7.5F), Brushes.Gray, left, y);
            }
        }

        /// <summary>Picks a readable set of label:value lines from the located record row.</summary>
        private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> RecordLines()
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();
            void Add(string label, params string[] cols)
            {
                foreach (string c in cols)
                    if (_record.Table.Columns.Contains(c) && _record[c] != DBNull.Value)
                    {
                        string v = _record[c].ToString().Trim();
                        if (v.Length > 0) { list.Add(new System.Collections.Generic.KeyValuePair<string, string>(label, v)); return; }
                    }
            }

            if (_recordType == "Birth")
            {
                Add("Registry No.", "registry_no");
                string name = Join("first_name", "middle_name", "last_name");
                list.Add(new System.Collections.Generic.KeyValuePair<string, string>("Name", name));
                Add("Sex", "sex");
                Add("Date of Birth", "date_of_birth");
                Add("Place of Birth", "place_of_birth");
                Add("Mother", "mother_maiden_name", "mother_name");
                Add("Father", "father_name");
            }
            else if (_recordType == "Death")
            {
                Add("Registry No.", "registry_no");
                Add("Name", "full_name");
                Add("Sex", "sex");
                Add("Date of Death", "date_of_death");
                Add("Place of Death", "place_of_death");
            }
            else if (_recordType == "Marriage")
            {
                Add("Registry No.", "registry_no");
                Add("Husband", "husband_first_name", "husband_name");
                Add("Wife", "wife_first_name", "wife_name");
                Add("Date of Marriage", "date_of_marriage", "marriage_date");
            }
            if (list.Count == 0)
                list.Add(new System.Collections.Generic.KeyValuePair<string, string>("Record ID", _recordId.ToString()));
            return list;
        }

        private string Join(params string[] cols)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (string c in cols)
                if (_record.Table.Columns.Contains(c) && _record[c] != DBNull.Value)
                {
                    string v = _record[c].ToString().Trim();
                    if (v.Length > 0) parts.Add(v);
                }
            return parts.Count > 0 ? string.Join(" ", parts) : "—";
        }
    }
}
