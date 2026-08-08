using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Claim Form (spec section 2 &amp; the claiming half of the Claim Request flow).
    /// Stays inside the main desktop system (NOT the mobile app). Staff scan the QR the
    /// client received when the request was saved — a USB QR/barcode scanner types the
    /// payload + Enter, so the token box + Load button need no camera library — and the
    /// system loads the request, populates the client's details, shows the ID photo the
    /// client uploaded from the claimapp mobile app, and lets staff verify identity and
    /// release. No manual encoding required.
    ///
    /// Built in code (data-driven module, like the other dynamic module forms).
    /// </summary>
    public class ClaimFormForm : Form, IRefreshable
    {
        private static readonly Color Ink = Color.FromArgb(33, 37, 41);
        private static readonly Color Muted = Color.FromArgb(108, 117, 125);
        private static readonly Color Accent = Color.FromArgb(13, 110, 253);

        private TextBox _txtToken;
        private Button _btnLoad, _btnVerify, _btnRelease;
        private Label _lblTicket, _lblName, _lblDetails, _lblStatus, _lblIdName, _lblRelease, _lblHint;
        private TextBox _txtReleaseInfo;
        private PictureBox _picId;

        private int _claimId;         // currently-loaded claim_requests.id (0 = none)
        private string _claimStatus;

        public ClaimFormForm()
        {
            Text = "Claim by QR";
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 10F);
            // Opened as a modal dialog from Release & Claim.
            ClientSize = new Size(1060, 900);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildUi();
        }

        private void BuildUi()
        {
            Controls.Add(new Label
            {
                Text = "Claim Form", AutoSize = true, Location = new Point(21, 14),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Ink
            });
            Controls.Add(new Label
            {
                Text = "Scan the client's claim QR to load the request, verify the uploaded ID, and release.",
                AutoSize = true, Location = new Point(22, 52), Font = new Font("Segoe UI", 9F), ForeColor = Muted
            });

            // --- Scan / enter QR row ---
            Controls.Add(Cap("Scan QR / enter claim token", 21, 84));
            _txtToken = new TextBox
            {
                Location = new Point(21, 108), Size = new Size(520, 30),
                Font = new Font("Segoe UI", 12F)
            };
            _txtToken.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadClaim(); } };
            Controls.Add(_txtToken);
            _btnLoad = Primary("Load", 553, 106, Accent);
            _btnLoad.Click += (s, e) => LoadClaim();
            Controls.Add(_btnLoad);

            _lblHint = new Label
            {
                Text = "Ready. Ask the client for their claim QR, or type the claim token.",
                AutoSize = true, Location = new Point(21, 148), Font = new Font("Segoe UI", 9F), ForeColor = Muted
            };
            Controls.Add(_lblHint);

            // --- Left: request + claimant details ---
            var grp = new GroupBox
            {
                Text = "REQUEST DETAILS", Location = new Point(21, 180), Size = new Size(560, 360),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), ForeColor = Ink
            };
            int y = 34;
            grp.Controls.Add(Cap("Claim Ticket No", 18, y)); _lblTicket = Val(230, y); grp.Controls.Add(_lblTicket); y += 40;
            grp.Controls.Add(Cap("Requester Name", 18, y)); _lblName = Val(230, y); grp.Controls.Add(_lblName); y += 40;
            grp.Controls.Add(Cap("Request Details", 18, y)); _lblDetails = Val(230, y); grp.Controls.Add(_lblDetails); y += 40;
            grp.Controls.Add(Cap("Claim Status", 18, y)); _lblStatus = Val(230, y); grp.Controls.Add(_lblStatus); y += 40;
            grp.Controls.Add(Cap("Name on uploaded ID", 18, y)); _lblIdName = Val(230, y); grp.Controls.Add(_lblIdName); y += 40;
            grp.Controls.Add(Cap("Release Info", 18, y)); y += 26;
            _txtReleaseInfo = new TextBox
            {
                Location = new Point(18, y), Size = new Size(520, 46), Multiline = true,
                Font = new Font("Segoe UI", 10F)
            };
            grp.Controls.Add(_txtReleaseInfo); y += 54;
            _lblRelease = new Label { AutoSize = true, Location = new Point(18, y), Font = new Font("Segoe UI", 8.5F), ForeColor = Muted };
            grp.Controls.Add(_lblRelease);
            Controls.Add(grp);

            // --- Right: uploaded ID photo ---
            var grpId = new GroupBox
            {
                Text = "UPLOADED VALID ID", Location = new Point(600, 180), Size = new Size(420, 360),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), ForeColor = Ink,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _picId = new PictureBox
            {
                Location = new Point(18, 34), Size = new Size(384, 308),
                SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(233, 236, 239)
            };
            grpId.Controls.Add(_picId);
            Controls.Add(grpId);

            // --- Action buttons ---
            _btnVerify = Primary("Verify Identity", 600, 556, Color.FromArgb(25, 135, 84));
            _btnVerify.Size = new Size(200, 44);
            _btnVerify.Click += (s, e) => VerifyIdentity();
            Controls.Add(_btnVerify);

            _btnRelease = Primary("Release Document", 812, 556, Accent);
            _btnRelease.Size = new Size(208, 44);
            _btnRelease.Click += (s, e) => ReleaseDocument();
            Controls.Add(_btnRelease);

            // --- Stored kiosk claims (so staff can process one even without the QR,
            //     e.g. the client left). Tap a row to load it. ---
            Controls.Add(new Label
            {
                Text = "PENDING CLAIMS (from kiosk / requests — tap one to load)",
                AutoSize = true, Location = new Point(21, 616),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), ForeColor = Ink
            });
            _grid = new DataGridView
            {
                Location = new Point(21, 642), Size = new Size(1018, 236),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            _grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || !_grid.Columns.Contains("qr_token")) return;
                object tok = _grid.Rows[e.RowIndex].Cells["qr_token"].Value;
                if (tok != null && tok != DBNull.Value) LoadToken(tok.ToString());
            };
            Controls.Add(_grid);

            LoadClaimsList();
            SetLoaded(false);
        }

        private DataGridView _grid;

        /// <summary>Lists claim requests not yet released (newest first), for click-to-load.</summary>
        private void LoadClaimsList()
        {
            if (_grid == null) return;
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT qr_token, claim_ticket_no AS 'Claim Ticket', " +
                    "TRIM(CONCAT_WS(' ', first_name, middle_name, last_name)) AS Name, " +
                    "request_details AS Request, status AS Status, " +
                    "CASE WHEN id_image IS NULL THEN 'No' ELSE 'Yes' END AS 'ID Uploaded', " +
                    "DATE_FORMAT(created_at, '%d %b %Y %H:%i') AS Requested " +
                    "FROM claim_requests WHERE status <> 'Released' ORDER BY created_at DESC LIMIT 200");
                _grid.DataSource = dt;
                if (_grid.Columns.Contains("qr_token")) _grid.Columns["qr_token"].Visible = false;
            }
            catch { /* leave the grid empty on a DB hiccup */ }
        }

        // ---------------------------------------------------------------- load
        private void LoadClaim() => LoadToken(ExtractToken(_txtToken.Text));

        private void LoadToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) { Info("Scan a claim QR, type the token, or tap a claim in the list."); return; }

            DataTable dt = Db.Pull(
                "SELECT id, claim_ticket_no, first_name, middle_name, last_name, request_details, " +
                "status, id_first_name, id_middle_name, id_last_name, id_image, release_info, " +
                "released_at FROM claim_requests WHERE qr_token = @t LIMIT 1",
                new MySqlParameter("@t", token));
            if (dt.Rows.Count == 0)
            {
                SetLoaded(false);
                Info("No claim request matches that QR / token.");
                return;
            }

            DataRow r = dt.Rows[0];
            _claimId = Convert.ToInt32(r["id"]);
            _claimStatus = r["status"].ToString();

            _lblTicket.Text = Str(r["claim_ticket_no"], "—");
            _lblName.Text = JoinName(r["first_name"], r["middle_name"], r["last_name"]);
            _lblDetails.Text = Str(r["request_details"], "—");
            _lblStatus.Text = _claimStatus;
            _lblStatus.ForeColor = StatusColor(_claimStatus);
            _lblIdName.Text = JoinName(r["id_first_name"], r["id_middle_name"], r["id_last_name"]);
            if (_lblIdName.Text.Length == 0) _lblIdName.Text = "(ID not uploaded yet)";

            _picId.Image?.Dispose();
            _picId.Image = null;
            if (r["id_image"] != DBNull.Value)
            {
                try { using (var ms = new MemoryStream((byte[])r["id_image"])) _picId.Image = new Bitmap(ms); }
                catch { /* unreadable blob — leave blank */ }
            }

            _txtReleaseInfo.Text = Str(r["release_info"], "");
            _lblRelease.Text = r["released_at"] != DBNull.Value
                ? "Released at " + Convert.ToDateTime(r["released_at"]).ToString("dd MMM yyyy hh:mm tt") : "";

            SetLoaded(true);
            _lblHint.Text = "Loaded claim " + _lblTicket.Text + ". Verify the ID matches the client, then release.";
            _lblHint.ForeColor = StatusColor(_claimStatus);
        }

        private void VerifyIdentity()
        {
            if (_claimId == 0) return;
            if (_picId.Image == null &&
                MessageBox.Show("No ID has been uploaded for this claim yet.\nVerify anyway?",
                    "Verify", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            Db.Push("UPDATE claim_requests SET status = 'Verified' WHERE id = @id",
                new MySqlParameter("@id", _claimId));
            Audit.Write("Update", "claim_requests", _claimId, "Verified claim " + _lblTicket.Text);
            _claimStatus = "Verified";
            _lblStatus.Text = _claimStatus; _lblStatus.ForeColor = StatusColor(_claimStatus);
            Info("Identity verified. You may now release the document.");
        }

        private void ReleaseDocument()
        {
            if (_claimId == 0) return;
            if (string.Equals(_claimStatus, "Released", StringComparison.OrdinalIgnoreCase))
            { Info("This claim was already released."); return; }

            Db.Push("UPDATE claim_requests SET status = 'Released', release_info = @ri, " +
                    "released_by = @by, released_at = NOW() WHERE id = @id",
                new MySqlParameter("@ri", string.IsNullOrWhiteSpace(_txtReleaseInfo.Text)
                    ? (object)DBNull.Value : _txtReleaseInfo.Text.Trim()),
                new MySqlParameter("@by", Session.UserIdParam),
                new MySqlParameter("@id", _claimId));
            Audit.Write("Update", "claim_requests", _claimId, "Released claim " + _lblTicket.Text);
            _claimStatus = "Released";
            _lblStatus.Text = _claimStatus; _lblStatus.ForeColor = StatusColor(_claimStatus);
            _lblRelease.Text = "Released at " + DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            LoadClaimsList();   // drop it from the pending list
            Info("Document released to " + _lblName.Text + ".");
        }

        private void SetLoaded(bool on)
        {
            _btnVerify.Enabled = on;
            _btnRelease.Enabled = on;
            _txtReleaseInfo.Enabled = on;
        }

        public void RefreshData() { if (_claimId != 0) LoadClaim(); }

        // -------------------------------------------------------------- helpers
        /// <summary>Accepts a deep-link URL (?claim=/?token=), a "CROMS-CLAIM:&lt;token&gt;" payload, or a raw token.</summary>
        internal static string ExtractToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();
            // Deep-link URL form: https://<host>:4300/?claim=<token> (or ?token=).
            var m = System.Text.RegularExpressions.Regex.Match(
                raw, @"[?&](?:claim|token)=([^&#]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return Uri.UnescapeDataString(m.Groups[1].Value).Trim();
            // Legacy "CROMS-CLAIM:<token>" payload.
            int i = raw.IndexOf(':');
            if (raw.StartsWith("CROMS-CLAIM", StringComparison.OrdinalIgnoreCase) && i >= 0)
                return raw.Substring(i + 1).Trim();
            return raw;
        }

        private static Color StatusColor(string s)
        {
            switch ((s ?? "").ToLowerInvariant())
            {
                case "released": return Color.FromArgb(25, 135, 84);
                case "verified": return Color.FromArgb(13, 110, 253);
                case "iduploaded": return Color.FromArgb(255, 140, 0);
                default: return Color.FromArgb(108, 117, 125);
            }
        }

        private static string JoinName(object f, object m, object l)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (f != DBNull.Value && f != null && f.ToString().Length > 0) parts.Add(f.ToString());
            if (m != DBNull.Value && m != null && m.ToString().Length > 0) parts.Add(m.ToString());
            if (l != DBNull.Value && l != null && l.ToString().Length > 0) parts.Add(l.ToString());
            return string.Join(" ", parts);
        }

        private static string Str(object v, string fallback) =>
            v == DBNull.Value || v == null || v.ToString().Length == 0 ? fallback : v.ToString();

        private static Label Cap(string t, int x, int y) => new Label
        {
            Text = t, AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Muted
        };
        private static Label Val(int x, int y) => new Label
        {
            Text = "—", AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(33, 37, 41)
        };
        private static Button Primary(string t, int x, int y, Color back) => new Button
        {
            Text = t, Location = new Point(x, y), Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold), UseVisualStyleBackColor = false
        };
        private void Info(string m) => MessageBox.Show(m, "Claim Form",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Creates a Claim Request row (spec section 2) and shows/prints its QR code. The QR
    /// encodes a unique token; the client scans it in the claimapp mobile app to upload
    /// their valid ID, and staff scan the same QR in the Claim Form to release.
    /// </summary>
    public static class ClaimQr
    {
        public const string Prefix = "CROMS-CLAIM:";

        /// <summary>
        /// Inserts a claim_requests row for the given requester and returns (token, ticket).
        /// </summary>
        public static (string token, string ticket) Create(
            string first, string middle, string last, string requestDetails, long? transactionId)
        {
            string token = Guid.NewGuid().ToString("N");
            string ticket = NextTicketNo();
            Db.Push(
                "INSERT INTO claim_requests " +
                "(qr_token, claim_ticket_no, transaction_id, first_name, middle_name, last_name, " +
                "request_details, status) VALUES (@t, @tk, @txn, @f, @m, @l, @rd, 'Pending')",
                new MySqlParameter("@t", token),
                new MySqlParameter("@tk", ticket),
                new MySqlParameter("@txn", transactionId.HasValue ? (object)transactionId.Value : DBNull.Value),
                new MySqlParameter("@f", (object)first ?? DBNull.Value),
                new MySqlParameter("@m", (object)middle ?? DBNull.Value),
                new MySqlParameter("@l", (object)last ?? DBNull.Value),
                new MySqlParameter("@rd", (object)requestDetails ?? DBNull.Value));
            Audit.Write("Create", "claim_requests", ticket, "Claim request " + ticket + " for " +
                string.Join(" ", first, last));
            return (token, ticket);
        }

        private static string NextTicketNo()
        {
            string year = DateTime.Now.Year.ToString();
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(claim_ticket_no,'-',-1) AS UNSIGNED)),0)+1 AS n " +
                "FROM claim_requests WHERE claim_ticket_no LIKE 'CLM-" + year + "-%'");
            int n = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return "CLM-" + year + "-" + n.ToString("D4");
        }

        /// <summary>Shows the generated QR with a Print button (58mm-friendly slip).</summary>
        public static void Show(IWin32Window owner, string token, string ticket, string clientName)
        {
            string payload = ClaimLink.Build(token);
            using (var dlg = new Form())
            {
                dlg.Text = "Claim QR — " + ticket;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(360, 470);
                dlg.BackColor = Color.White;

                dlg.Controls.Add(new Label
                {
                    Text = "CLAIM QR CODE", Dock = DockStyle.Top, Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
                });

                Bitmap qr = QrHelper.TryCreate(payload, 8);
                var pic = new PictureBox
                {
                    Location = new Point(50, 56), Size = new Size(260, 260),
                    SizeMode = PictureBoxSizeMode.Zoom, Image = qr,
                    BorderStyle = BorderStyle.FixedSingle
                };
                if (qr == null)
                    dlg.Controls.Add(new Label
                    {
                        Text = "Token:\n" + token, Location = new Point(30, 120), Size = new Size(300, 120),
                        Font = new Font("Consolas", 9F), TextAlign = ContentAlignment.MiddleCenter
                    });
                dlg.Controls.Add(pic);

                dlg.Controls.Add(new Label
                {
                    Text = "Claim Ticket: " + ticket + "\n" + (clientName ?? "") +
                           "\n\nScan with your phone camera to upload your ID." +
                           "\nNo camera? Open " + ClaimLink.BaseUrl() + " and enter ticket " + ticket + ".",
                    Location = new Point(20, 314), Size = new Size(320, 110),
                    TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Color.FromArgb(73, 80, 87)
                });

                var print = new Button
                {
                    Text = "🖨  Print", Location = new Point(20, 416), Size = new Size(160, 40),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(13, 110, 253),
                    ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    UseVisualStyleBackColor = false
                };
                print.Click += (s, e) => PrintSlip(payload, ticket, clientName);
                var close = new Button
                {
                    Text = "Close", Location = new Point(190, 416), Size = new Size(150, 40),
                    FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK, Font = new Font("Segoe UI", 10F)
                };
                dlg.Controls.Add(print); dlg.Controls.Add(close);
                dlg.AcceptButton = close;
                dlg.ShowDialog(owner);
            }
        }

        private static void PrintSlip(string payload, string ticket, string clientName)
        {
            try
            {
                using (var doc = new PrintDocument())
                {
                    doc.DefaultPageSettings.PaperSize = new PaperSize("Q58", 228, 360);
                    doc.DefaultPageSettings.Margins = new Margins(8, 8, 10, 10);
                    doc.DocumentName = "Claim QR " + ticket;
                    doc.PrintPage += (s, e) =>
                    {
                        Graphics g = e.Graphics;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                        float left = e.MarginBounds.Left, width = e.MarginBounds.Width, y = e.MarginBounds.Top;
                        using (var fTitle = new Font("Segoe UI", 10F, FontStyle.Bold))
                        using (var fBody = new Font("Segoe UI", 8.5F))
                        using (var center = new StringFormat { Alignment = StringAlignment.Center })
                        {
                            Action<string, Font> mid = (t, f) =>
                            {
                                SizeF sz = g.MeasureString(t, f, (int)width);
                                g.DrawString(t, f, Brushes.Black, new RectangleF(left, y, width, sz.Height), center);
                                y += sz.Height + 2;
                            };
                            mid("CROMS — LCRO Peñablanca", fTitle);
                            mid("CLAIM QR", fBody);
                            mid("Ticket: " + ticket, fBody);
                            if (!string.IsNullOrEmpty(clientName)) mid(clientName, fBody);
                            y += 6;
                            Bitmap qr = QrHelper.TryCreate(payload, 6);
                            if (qr != null)
                            {
                                float size = Math.Min(width, 150);
                                g.DrawImage(qr, left + (width - size) / 2, y, size, size);
                                y += size + 6;
                            }
                            mid("Scan with your phone camera to upload your ID.", fBody);
                            mid("No camera? Open " + ClaimLink.BaseUrl(), fBody);
                            mid("and enter ticket " + ticket + ".", fBody);
                            mid("Present when you claim.", fBody);
                        }
                    };
                    doc.Print();
                }
            }
            catch { /* no printer — the on-screen QR is still shown */ }
        }
    }
}
