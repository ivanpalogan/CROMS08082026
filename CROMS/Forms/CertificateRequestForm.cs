using System;
using System.Data;
using System.Drawing;
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

            // Pre-fill from what the client entered on the kiosk (name, purpose, photo).
            string contact = "";
            System.Data.DataTable dt = Db.Pull(
                "SELECT full_name, purpose, contact_no, id_image, document_type FROM queue_tickets WHERE id = " + ticketId);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                FillName(Text2(row["full_name"]));
                txtPurpose.Text = Text2(row["purpose"]);
                string requestedType = Text2(row["document_type"]);
                if (requestedType == "Birth" || requestedType == "Marriage" || requestedType == "Death")
                    cboRecordType.SelectedItem = requestedType;
                contact = Text2(row["contact_no"]);
                ShowPhoto(row["id_image"]);
            }

            PrefillFromCtcIntake(ticketId);

            pillQueueRef.Text = "Queue ticket " + ticketCode +
                (contact.Length > 0 ? "   ·   " + contact : "");
            pillQueueRef.Visible = true;
            cardPhoto.Visible = true;
            UpdateSummary();
            txtFirst.Focus();
        }

        /// <summary>
        /// Overwrites the kiosk's one-line note with the structured intake the client actually
        /// filled in (migration 55), so the clerk starts from the request rather than retyping
        /// it. The NAME on the form stays the person AT THE COUNTER — the record owner can be
        /// someone else entirely (a parent collecting a child's certificate), and confusing the
        /// two would file the request under the wrong requester.
        /// <para/>
        /// Silent when there is no intake row: a counter-created request and a database still on
        /// migration 54 both have to keep working.
        /// </summary>
        private void PrefillFromCtcIntake(int ticketId)
        {
            try
            {
                System.Data.DataTable c = Db.Pull(
                    "SELECT doc_type, copies, purpose, registry_no, owner_first, owner_middle, owner_last " +
                    "FROM ctc_requests WHERE queue_ticket_id = @id ORDER BY id DESC LIMIT 1",
                    new MySqlParameter("@id", ticketId));
                if (c.Rows.Count == 0) return;
                System.Data.DataRow r = c.Rows[0];

                string doc = Text2(r["doc_type"]);
                if (doc == "Birth" || doc == "Marriage" || doc == "Death") cboRecordType.SelectedItem = doc;

                int copies;
                if (r["copies"] != DBNull.Value && int.TryParse(r["copies"].ToString(), out copies) && copies > 0)
                    txtCopies.Text = copies.ToString();

                string purpose = Text2(r["purpose"]);
                if (purpose.Length > 0) txtPurpose.Text = purpose;

                // The record picker is a searchable combo bound to the registry rows, so typing
                // the owner's name into it filters straight to the entry. Deliberately only a
                // starting point: it is left for the clerk to CONFIRM against the list, since a
                // kiosk-typed name is the client's spelling, not the registry's.
                string owner = string.Join(" ", new[] { Text2(r["owner_last"]), Text2(r["owner_first"]) }
                    .Where(p => p.Length > 0));
                if (owner.Length > 0) cboRecord.Text = owner;
            }
            catch { /* no ctc_requests table yet, or a counter-created request */ }
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

        /// <summary>Forgets any linked queue ticket and hides its badge + photo card.</summary>
        private void ResetQueueLink()
        {
            _queueTicketId = 0;
            _queueTicketCode = null;
            pillQueueRef.Visible = false;
            picClient.Image = null;
            cardPhoto.Visible = false;
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
            cboRecordType.SelectedIndexChanged += (s, e) => { LoadRecords(); UpdateSummary(); };
            LoadRequests();
            LearningLibrary.Attach(txtFirst, LearningLibrary.GivenName);
            LearningLibrary.Attach(txtLast, LearningLibrary.Surname);
            AutoCaps.Attach(txtFirst, txtMiddle, txtLast);

            // Request summary panel — mirrors every field live so the officer can check the
            // request before creating it, rather than only after.
            txtFirst.TextChanged += (s, e) => UpdateSummary();
            txtMiddle.TextChanged += (s, e) => UpdateSummary();
            txtLast.TextChanged += (s, e) => UpdateSummary();
            cboCertType.SelectedIndexChanged += (s, e) => UpdateSummary();
            cboRecord.SelectedIndexChanged += (s, e) => UpdateSummary();
            cboRecord.TextChanged += (s, e) => UpdateSummary();
            txtCopies.TextChanged += (s, e) => UpdateSummary();
            txtPurpose.TextChanged += (s, e) => UpdateSummary();

            SetupRefreshIcon();
            UpdateSummary();
        }

        /// <summary>Refreshes the "Request summary" panel from the current field values.</summary>
        private void UpdateSummary()
        {
            SetSummary(lblSumClient, FullClientName());
            SetSummary(lblSumCertType, cboCertType.SelectedItem?.ToString());
            SetSummary(lblSumRecordType, cboRecordType.SelectedItem?.ToString());
            SetSummary(lblSumRecord, cboRecord.Text);
            SetSummary(lblSumCopies, txtCopies.Text);
            SetSummary(lblSumPurpose, txtPurpose.Text);

            // The callout says what to do NEXT, so it has to know whether the form is
            // actually ready — otherwise it is decoration that reads the same either way.
            bool ready = !string.IsNullOrWhiteSpace(txtFirst.Text)
                      && !string.IsNullOrWhiteSpace(txtLast.Text);
            lblNextStep.Text = ready
                ? "Click Create request to open the transaction, then print the certificate."
                : "Enter the client's first and last name, then click Create request.";
        }

        /// <summary>
        /// Writes one summary value. An empty entry is drawn in the faint tone so a filled
        /// row and a still-blank one are told apart at a glance, not only by reading them.
        /// </summary>
        private static void SetSummary(Label lbl, string value)
        {
            bool filled = !string.IsNullOrWhiteSpace(value);
            // AutoEllipsis trims at the label's real pixel width; a character count would
            // cut a name that still fits, or overflow one that does not.
            lbl.Text = filled ? value.Trim() : "—";
            lbl.ForeColor = filled ? UiTheme.Ink : UiTheme.Faint;
        }

        /// <summary>
        /// Draws the refresh control as a circular arrow. The button is tagged "noskin" so
        /// UiTheme leaves it alone — its owner-draw renders the caption with TextRenderer,
        /// which mangles a glyph like "↻" at this size.
        /// </summary>
        private void SetupRefreshIcon()
        {
            bool hover = false;
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.BackColor = UiTheme.Surface;
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(btnRefresh, true);

            btnRefresh.MouseEnter += (s, e) => { hover = true; btnRefresh.Invalidate(); };
            btnRefresh.MouseLeave += (s, e) => { hover = false; btnRefresh.Invalidate(); };
            btnRefresh.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var face = new Rectangle(0, 0, btnRefresh.Width - 1, btnRefresh.Height - 1);
                using (var path = CardPanel.RoundedRect(face, 8))
                using (var fill = new SolidBrush(hover ? UiTheme.AccentTint : UiTheme.Chrome))
                    g.FillPath(fill, path);

                Color ink = hover ? UiTheme.Accent : UiTheme.Muted;
                var box = new RectangleF(face.Width / 2f - 7.5f, face.Height / 2f - 7.5f, 15f, 15f);
                using (var pen = new Pen(ink, 1.9f))
                {
                    // Open arc + arrowhead — a circular arrow, drawn rather than typed.
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawArc(pen, box, 20f, 300f);
                }
                using (var brush = new SolidBrush(ink))
                {
                    float cx = box.Right, cy = box.Y + box.Height / 2f;
                    g.FillPolygon(brush, new[]
                    {
                        new PointF(cx - 0.5f, cy - 5.5f),
                        new PointF(cx + 4.5f, cy - 1.5f),
                        new PointF(cx - 1.5f, cy + 0.5f)
                    });
                }
            };
        }

        /// <summary>Shows the inline validation banner with the given message.</summary>
        private void ShowValidation(string msg)
        {
            lblValidation.Text = "⚠  " + msg;
            pnlValidation.Visible = true;
        }

        private void HideValidation() => pnlValidation.Visible = false;

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
                ShowValidation("Please complete the required field: Client name (first and last name).");
                return;
            }
            HideValidation();

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
                        "UPDATE queue_tickets SET transaction_id = @txn WHERE id = @tid",
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
            HideValidation();
            UpdateSummary();
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
        private int _recalls;        // how many times the client's number has been called

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
            ClientSize = new Size(560, 618);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var head = new Label
            {
                Text = "Step 2 of 4 — Print the Certificate",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(24, 18), AutoSize = true
            };
            var sub = new Label
            {
                Text = "Follow the steps below in order.",
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(26, 52), AutoSize = true
            };

            var info = new Label
            {
                Location = new Point(26, 82), Size = new Size(508, 72),
                Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(52, 58, 64),
                Text =
                    "Transaction:  " + _txnCode + "\r\n" +
                    "Client:  " + _client + "\r\n" +
                    "Certificate:  " + _certType + "  " + (_recordType ?? "—") +
                    "     Copies:  " + _copies
            };

            _lblFound = new Label
            {
                Location = new Point(26, 156), Size = new Size(508, 40),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                Text = _recordId > 0
                    ? "Record is linked. Start with Step 1 below."
                    : "⚠ No record was picked on the request. Skip to Step 3 and park it — you can locate the record later."
            };

            // ---- Step 1: Preview + Print --------------------------------------------------
            var lblStep1 = StepLabel(1, "Preview and print the certificate");
            lblStep1.Location = new Point(26, 202);

            _btnPrint = new Button
            {
                Text = "🖨  Preview & Print Certificate",
                Location = new Point(26, 226), Size = new Size(508, 46),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPrint.FlatAppearance.BorderSize = 0;
            _btnPrint.Click += (s, e) => FindAndPrint();
            _btnPrint.Enabled = _recordId > 0;

            var lblStep1Hint = new Label
            {
                Text = "Opens the same certificate print preview used everywhere else — check it, then print.",
                Location = new Point(26, 274), Size = new Size(508, 18),
                Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(134, 142, 150)
            };

            // ---- Step 2: Call the client (optional, any time) --------------------
            var lblStep2 = StepLabel(2, "Call the client to the counter (optional)");
            lblStep2.Location = new Point(26, 306);

            // Call / Recall the client's queue number to the window (voice callout). Use it
            // when the client stepped away; if they still don't come, park the request.
            _btnCall = new Button
            {
                Text = _queueCode != null
                    ? "📢  Call Client  (" + _queueCode + ")"
                    : "📢  Call Client",
                Location = new Point(26, 330), Size = new Size(508, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(102, 16, 242),
                ForeColor = Color.White, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCall.FlatAppearance.BorderSize = 0;
            _btnCall.Click += (s, e) => CallClient();

            var lblStep2Hint = new Label
            {
                Text = "Announces the queue number at the window. Use if the client stepped away.",
                Location = new Point(26, 376), Size = new Size(508, 18),
                Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(134, 142, 150)
            };

            // ---- Step 3: Choose what happens next ---------------------------------
            var lblStep3 = StepLabel(3, "Choose what happens next");
            lblStep3.Location = new Point(26, 410);

            _btnPay = new Button
            {
                Text = "✔  Certificate Ready — Proceed to Payment",
                Location = new Point(26, 434), Size = new Size(508, 46),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 135, 84),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPay.FlatAppearance.BorderSize = 0;
            _btnPay.Click += (s, e) => { Result = CertNextStep.ProceedToPayment; Close(); };
            _btnPay.Enabled = false;   // stays off until Step 1 has printed the certificate

            var lblStep3HintA = new Label
            {
                Text = "Unlocks after the certificate is printed in Step 1.",
                Location = new Point(26, 482), Size = new Size(508, 18),
                Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(134, 142, 150)
            };

            var btnPark = new Button
            {
                Text = "⏸  Client Not Present — Hold for Release",
                Location = new Point(26, 508), Size = new Size(508, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(233, 236, 239),
                ForeColor = Color.FromArgb(33, 37, 41), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPark.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnPark.Click += (s, e) => Park();

            var lblStep3HintB = new Label
            {
                Text = "Use this any time — before or after printing — if the client isn't here.",
                Location = new Point(26, 554), Size = new Size(508, 18),
                Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(134, 142, 150)
            };

            var btnCancel = new Button
            {
                Text = "Close",
                Location = new Point(408, 580), Size = new Size(126, 32),
                FlatStyle = FlatStyle.Flat, BackColor = Color.White,
                ForeColor = Color.FromArgb(73, 80, 87), Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnCancel.Click += (s, e) => Close();

            Controls.Add(head); Controls.Add(sub); Controls.Add(info);
            Controls.Add(_lblFound);
            Controls.Add(lblStep1); Controls.Add(_btnPrint); Controls.Add(lblStep1Hint);
            Controls.Add(lblStep2); Controls.Add(_btnCall); Controls.Add(lblStep2Hint);
            Controls.Add(lblStep3); Controls.Add(_btnPay); Controls.Add(lblStep3HintA);
            Controls.Add(btnPark); Controls.Add(lblStep3HintB);
            Controls.Add(btnCancel);
        }

        /// <summary>Small bold "STEP N — Title" caption placed above each action button.</summary>
        private static Label StepLabel(int n, string title)
        {
            return new Label
            {
                Text = "STEP " + n + "   " + title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87)
            };
        }

        /// <summary>
        /// Voice-calls the client's queue number to the window (waiting-area callout, same
        /// System.Speech engine the queue board uses). Guarded — a PC with no audio never
        /// crashes. After the 2nd call, the button hints that a no-show should be parked.
        /// </summary>
        private const int MaxCalls = 2;   // hard cap so the callout can't be spammed

        private void CallClient()
        {
            // Cap at MaxCalls total (first call + one recall). Past that the button is disabled
            // and the officer should park the request as a no-show.
            if (_recalls >= MaxCalls)
            {
                _btnCall.Enabled = false;
                _lblFound.Text = "Called " + _recalls + " times (limit reached). If the client does " +
                    "not come, use \"Client Not Present — Hold for Release\".";
                _lblFound.ForeColor = Color.FromArgb(200, 35, 51);
                return;
            }

            _recalls++;

            // Bump the queue ticket's recall counter so the public Display board (a separate
            // PC by the waiting area) announces this number too — the board watches recall_count.
            if (_queueCode != null)
            {
                try
                {
                    Db.Push(
                        "UPDATE queue_tickets SET recall_count = COALESCE(recall_count,0) + 1 " +
                        "WHERE ticket_code = @c AND status = 'Serving' AND DATE(created_at) = CURDATE()",
                        new MySqlParameter("@c", _queueCode));
                }
                catch { /* board just won't re-announce — never block the counter call */ }
            }

            // Voice is spoken ONLY by the public queue Display PC (it watches the ticket's
            // Serving status + recall_count, bumped above). Staff PC stays silent so no one
            // has to mute it.

            bool capped = _recalls >= MaxCalls;
            _btnCall.Text = _queueCode != null
                ? "📢  Recall Client  (" + _queueCode + ")   ·  called " + _recalls + "×"
                : "📢  Recall Client   ·  called " + _recalls + "×";
            _btnCall.Enabled = !capped;   // no more calls after the limit — can't be spammed
            _lblFound.Text = capped
                ? "Called " + _recalls + " times (limit reached). If the client does not come, use \"Client Not Present — Hold for Release\"."
                : "Client called. Waiting for them at the window…";
            _lblFound.ForeColor = capped ? Color.FromArgb(200, 35, 51) : Color.FromArgb(102, 16, 242);
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
                OthersBox.AttachInline(cbo, 160);
                d.AcceptButton = ok;
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    string reason = OthersBox.Value(cbo);
                    ParkReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
                    Result = CertNextStep.WaitingToRelease;
                    Close();
                }
            }
        }

        // ---------------------------------------------------------- find + preview + print
        /// <summary>
        /// Finds the linked record and opens the SAME certificate print preview used by
        /// Birth / Marriage / Death Registration (<see cref="CertificateReport.ShowFor"/>) —
        /// the operator sees the certificate before it goes to paper, exactly like printing
        /// it from the registration screen, instead of a separate ad-hoc printout built just
        /// for this dialog.
        /// </summary>
        private void FindAndPrint()
        {
            if (_recordId <= 0 || string.IsNullOrEmpty(_recordType))
            {
                MessageBox.Show(
                    "No record is linked to this request, so there is nothing to preview yet.\n\n" +
                    "Park it to Waiting-to-Release, locate the record, then finish it from Release & Claim.",
                    "No record", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DocKind kind;
            if (!Enum.TryParse(_recordType, out kind) || kind == DocKind.Unknown)
            {
                MessageBox.Show("Unknown record type: " + _recordType, "Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ReportEngine? engine = CertificateReport.ShowFor(kind, _recordId, this);
                if (engine == null) return;   // CertificateReport already explained why (not found / unknown form)

                _lblFound.Text = "✔ Step 1 done — certificate previewed. Now do Step 3: Proceed to Payment.";
                _lblFound.ForeColor = Color.FromArgb(25, 135, 84);
                _btnPay.Enabled = true;
                Audit.Write(Audit.Update, "transactions", _txnId, "Certificate previewed/printed (CTC)");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open the certificate preview: " + ex.Message, "Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
