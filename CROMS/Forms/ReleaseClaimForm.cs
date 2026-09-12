using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Release &amp; Claim — the releasing window. Lists transactions that are
    /// <b>For Release</b>; the officer picks one, records who claimed it (owner or
    /// authorized representative + ID), captures a signature drawn on screen, and
    /// releases it — which closes the transaction (status Released). Controls in the
    /// Designer; the signature pad is wired here.
    /// </summary>
    public partial class ReleaseClaimForm : Form, IRefreshable
    {
        private int? _selectedTxnId;
        private PictureBox picClient;
        private Label lblPhotoCap;
        private PictureBox picUploadedId;   // ID photo uploaded via claimapp (claim_requests.id_image)
        private Label lblUploadedIdCap;
        private Label lblPhotoState, lblIdState;   // "On file" / "Not uploaded" under each photo
        private Label _chkSelected, _chkClaimant, _chkPhoto, _chkId;   // "before releasing" list

        // responsive-redesign controls (built in BuildResponsiveLayout)
        private TextBox txtSearch;
        private Label lblValidation;    // validation message under the claim fields
        private Label lblClaimStatus;   // the note under the request summary
        private Panel _camPanel;        // whole camera block (one collapsible row)

        // ---- rail (left): find the request ----
        private Control _tabRow;
        private Panel _findRow;
        private Button _btnHistory;     // footer line: released today / back to worklist
        private bool _historyOpen;      // rail is showing release history instead of the worklist

        // ---- workspace (right): one state, one action ----
        private Panel _wHead, _wBody, _pnlEmpty, _notePanel, _noteBar;
        private StatusPill _pill;
        private Label _wMeta, _nextLab;
        private TableLayoutPanel _pnlSummary, _pnlClaim, _repBlock, _claimStack;
        private int _repRow, _camRow;                 // collapsible rows in the claim block
        private const int RepRowHeight = 108;         // two ID field rows
        private const int CamRowHeight = 290;         // BuildCameraPanel's own height
        private string _state = "";     // the status ApplyState last rendered

        // Left-list mode: 0 = For Release (ready), 1 = Waiting to Release (parked).
        private int _listMode;
        private Button _tabPending, _tabWaiting;

        // Pickup claim loaded from the queue (no transaction yet). When set, Release
        // closes the claim_requests row directly (mirrors the Claim Form).
        private int? _pickupClaimId;
        private string _pickupClaimCode;

        // Palette — the app-wide tokens, not a private copy. This screen used to carry its
        // own Bootstrap set, which is how it drifted off the retinted shell.
        private static readonly Color Bg      = UiTheme.PageBg;
        private static readonly Color CardBg  = UiTheme.Surface;
        private static readonly Color Accent  = UiTheme.Accent;
        private static readonly Color Green   = UiTheme.Success;
        private static readonly Color Amber   = UiTheme.Warning;
        private static readonly Color Ink     = UiTheme.Ink;
        private static readonly Color Muted   = UiTheme.Muted;
        private static readonly Color Line    = UiTheme.CardLine;
        private static readonly Color Chip    = UiTheme.Chrome;      // inactive tab / neutral chip
        private static readonly Color Faint   = UiTheme.Faint;

        // ---- claimant webcam ----
        private VideoCaptureDevice _camera;
        private Bitmap _lastFrame;
        private readonly object _frameLock = new object();
        private byte[] _photoBytes;      // captured claimant JPEG (null = none)
        private FilterInfoCollection _videoDevices;  // available cameras

        public ReleaseClaimForm()
        {
            InitializeComponent();
            PopulateIdTypes();
            PopulateCameras();
            BuildResponsiveLayout();     // docked card layout (replaces the absolute one)
            dgvPending.CellClick += dgvPending_CellClick;
            txtClaimant.TextChanged += (s, e) => UpdateChecklist();
            this.Disposed += (s, e) => StopCamera();

            // Camera is optional per release — default to Off so nothing is initialized
            // until the officer flips the toggle on.
            tglUseCam.SetCheckedSilently(false);
            ApplyCameraOption();             // hide the camera panel to match the Off state

            SetFaceState(lblPhotoState, false, null, "No kiosk photo");
            SetFaceState(lblIdState, false, null, "No uploaded ID");
            LoadPending();
            LoadReleased();
            UpdateChecklist();
        }

        // ================================================================
        //  Responsive card layout — docked TableLayoutPanels only, so it
        //  never overlaps and adapts to any resolution / DPI. Reuses every
        //  Designer control (keeps their event wiring); the old absolute
        //  GroupBoxes are discarded.
        // ================================================================
        private void BuildResponsiveLayout()
        {
            SuspendLayout();
            BackColor = Bg;
            Controls.Clear();   // drop the designer's absolute containers (grpPending/grpClaim/…)

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Bg,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // 340 could not carry four columns: at that width "TXN-2026-000037" and
            // "Aug 27, 2026" both truncated, and a worklist you cannot read is not a
            // worklist. 400 fits them and still leaves the workspace ~960 at 1400.
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // --- title row (spans both columns) ---
            var titleBar = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            titleBar.Controls.Add(new Label
            {
                Text = "Release & Claim", AutoSize = true, ForeColor = Ink, UseMnemonic = false,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold), Location = new Point(2, 0)
            });
            // 19pt renders ~40px tall, so the subtitle has to clear 40 — at 36 the two
            // labels genuinely overlapped (caught by the sibling-overlap sweep, not by eye).
            titleBar.Controls.Add(new Label
            {
                Text = "Release documents · verify the claimant · capture proof",
                AutoSize = true, ForeColor = Muted,
                Font = new Font("Segoe UI", 9F), Location = new Point(4, 41)
            });
            root.Controls.Add(titleBar, 0, 0);
            root.SetColumnSpan(titleBar, 2);

            root.Controls.Add(BuildRail(), 0, 1);
            root.Controls.Add(BuildWorkspace(), 1, 1);

            Controls.Add(root);
            ResumeLayout(true);

            ApplyState(null);   // nothing selected yet
        }

        // ---- RAIL: "which request" — tabs, search, scan, worklist, release history ----
        private Control BuildRail()
        {
            var card = new Card("Find the request", 1) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };

            // The counts live IN the tab captions. Two of the three summary tiles this
            // replaces printed the same two numbers a second time, right above the tabs.
            var tabRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top, Height = 42, ColumnCount = 2, RowCount = 1, BackColor = CardBg
            };
            tabRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tabRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _tabPending = MakeTab("For Release", true);
            _tabWaiting = MakeTab("Waiting", false);
            _tabPending.Dock = DockStyle.Fill;
            _tabWaiting.Dock = DockStyle.Fill;
            _tabPending.Click += (s, e) => SetListMode(0);
            _tabWaiting.Click += (s, e) => SetListMode(1);
            tabRow.Controls.Add(_tabPending, 0, 0);
            tabRow.Controls.Add(_tabWaiting, 1, 0);
            _tabRow = tabRow;

            _findRow = BuildFindRow();

            dgvPending.Dock = DockStyle.Fill;
            dgvPending.Margin = new Padding(0);
            StyleGrid(dgvPending);

            // Release history moved out of the middle of the screen. It is reference
            // material, not the client in front of you, so it lives behind the footer
            // line and takes the SAME space as the worklist instead of competing with it.
            dgvReleased.Dock = DockStyle.Fill;
            dgvReleased.Margin = new Padding(0);
            dgvReleased.Visible = false;
            StyleGrid(dgvReleased);
            dgvReleased.CellDoubleClick += (s, e) => ShowReleaseDetails(e.RowIndex);

            var listHost = new Panel { Dock = DockStyle.Fill, BackColor = CardBg };
            listHost.Controls.Add(dgvPending);
            listHost.Controls.Add(dgvReleased);

            _btnHistory = new Button
            {
                Text = "Released today · 0", Dock = DockStyle.Bottom, Height = 40,
                FlatStyle = FlatStyle.Flat, BackColor = CardBg, ForeColor = Muted,
                Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0), Cursor = Cursors.Hand
            };
            _btnHistory.FlatAppearance.BorderSize = 0;
            _btnHistory.Click += (s, e) => ShowHistory(!_historyOpen);

            card.Content.Controls.Add(listHost);     // Fill added FIRST — see the note in BuildWorkspace
            card.Content.Controls.Add(_btnHistory);  // Bottom
            card.Content.Controls.Add(_findRow);     // Top
            card.Content.Controls.Add(tabRow);       // Top (outermost = highest)
            return card;
        }

        /// <summary>Swaps the rail between the worklist and the release history.</summary>
        private void ShowHistory(bool on)
        {
            _historyOpen = on;
            _tabRow.Visible = !on;
            _findRow.Visible = !on;
            dgvPending.Visible = !on;
            dgvReleased.Visible = on;
            if (on) LoadReleased();
            UpdateSummary();
        }

        private Button MakeTab(string text, bool active) => new Button
        {
            Text = text, AutoSize = false, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = active ? Accent : Chip,
            ForeColor = active ? Color.White : Ink,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand, Margin = new Padding(0, 4, 6, 4)
        };

        /// <summary>Switches the left list between For-Release and Waiting-to-Release.</summary>
        private void SetListMode(int mode)
        {
            if (_historyOpen) ShowHistory(false);
            _listMode = mode;
            bool waiting = mode == 1;
            _tabPending.BackColor = waiting ? Chip : Accent;
            _tabPending.ForeColor = waiting ? Ink : Color.White;
            _tabWaiting.BackColor = waiting ? Amber : Chip;
            _tabWaiting.ForeColor = waiting ? Color.White : Ink;
            _selectedTxnId = null;
            _pickupClaimId = null;
            if (lblValidation != null) lblValidation.Text = "";
            LoadPending();
            ApplyState(null);
        }

        /// <summary>Sends the selected parked request to Fees &amp; Payments (status → ForPayment).</summary>
        private void ResumeSelected()
        {
            if (_selectedTxnId == null)
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Select a waiting request first.";
                return;
            }
            long tid = _selectedTxnId.Value;
            try
            {
                Db.Push("UPDATE transactions SET status = 'ForPayment' WHERE id = @t",
                    new MySqlParameter("@t", tid));
                Db.Push("UPDATE certificate_requests SET status = 'Ready' WHERE transaction_id = @t",
                    new MySqlParameter("@t", tid));
                Audit.Write(Audit.Update, "transactions", tid, "Resumed from Waiting-to-Release → Payment");

                MainForm shell = Shell();
                if (shell != null && shell.GoToModule("fees") is FeesPaymentsForm fp)
                    fp.PreselectTransaction(tid);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not resume: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }

        /// <summary>
        /// Search + QR scan on one row. Both answer the SAME question — which request is
        /// this — so they sit together and both load into the workspace on the right. The
        /// QR button used to open a second release window (ClaimFormForm) with its own
        /// claimant fields and its own Release button: two code paths to the same handover.
        /// </summary>
        private Panel BuildFindRow()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = CardBg, Padding = new Padding(0, 8, 0, 8) };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Ink
            };
            SetCue(txtSearch, "Txn no., queue no., or name…");
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); } };

            var btnScan = MakeMiniButton("Scan QR", Chip, Accent);
            btnScan.Width = 86;
            btnScan.Click += (s, e) => ScanClaimQr();
            var btnSearch = MakeMiniButton("Find", Accent, Color.White);
            btnSearch.Width = 60;
            btnSearch.Click += (s, e) => DoSearch();
            var btnClear = MakeMiniButton("Clear", Chip, Ink);
            btnClear.Width = 60;
            btnClear.Click += (s, e) => { txtSearch.Text = ""; LoadPending(); lblValidation.Text = ""; };

            // right-docked buttons first (so the Fill textbox takes the rest)
            bar.Controls.Add(txtSearch);
            bar.Controls.Add(btnScan);
            bar.Controls.Add(btnSearch);
            bar.Controls.Add(btnClear);
            btnScan.Dock = DockStyle.Right;
            btnSearch.Dock = DockStyle.Right;
            btnClear.Dock = DockStyle.Right;
            var pad = new Panel { Dock = DockStyle.Right, Width = 6, BackColor = CardBg };
            bar.Controls.Add(pad);
            bar.Controls.SetChildIndex(txtSearch, 0);
            return bar;
        }

        /// <summary>
        /// Reads a scanned claim QR (handheld scanners type the token then Enter) and loads
        /// that claim into THIS workspace. Scanning identifies a claim; it does not verify a
        /// person, which is why the button no longer says "Verify by QR".
        /// </summary>
        private void ScanClaimQr()
        {
            string token = PromptForToken();
            if (string.IsNullOrWhiteSpace(token)) return;
            token = token.Trim();

            // A QR may carry the raw token or a URL ending in it; take the last segment.
            int cut = token.LastIndexOfAny(new[] { '/', '=', '?', '&' });
            if (cut >= 0 && cut < token.Length - 1) token = token.Substring(cut + 1);

            DataTable dt = Db.Pull(
                "SELECT id, queue_ticket_id, transaction_id, claim_ticket_no " +
                "FROM claim_requests WHERE qr_token = @t OR claim_ticket_no = @t LIMIT 1",
                new MySqlParameter("@t", token));
            if (dt.Rows.Count == 0)
            {
                if (lblValidation != null) lblValidation.Text = "⚠ No claim request matches that QR / token.";
                MessageBox.Show("No claim request matches that QR or token.", "Not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataRow r = dt.Rows[0];
            if (r["queue_ticket_id"] != DBNull.Value)
            {
                PrepareFromQueueTicket(Convert.ToInt32(r["queue_ticket_id"]));
                return;
            }
            if (r["transaction_id"] != DBNull.Value)
            {
                long txn = Convert.ToInt64(r["transaction_id"]);
                SetListMode(0);
                PreselectTransaction(txn);
                if (!_selectedTxnId.HasValue && lblValidation != null)
                    lblValidation.Text = "⚠ Claim " + r["claim_ticket_no"] +
                        " is not in the For Release list — check its payment status.";
                return;
            }
            if (lblValidation != null)
                lblValidation.Text = "⚠ Claim " + r["claim_ticket_no"] + " has no queue ticket or transaction yet.";
        }

        /// <summary>Small modal that takes the scanner's keystrokes (or a typed token).</summary>
        private string PromptForToken()
        {
            using (var dlg = new Form
            {
                Text = "Scan claim QR",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false,
                ClientSize = new Size(430, 150), BackColor = CardBg
            })
            {
                var lbl = new Label
                {
                    Text = "Scan the claimant's QR now, or type the claim token / ticket number.",
                    Location = new Point(18, 18), Size = new Size(394, 36),
                    ForeColor = Muted, Font = new Font("Segoe UI", 9F)
                };
                var box = new TextBox
                {
                    Location = new Point(18, 58), Size = new Size(394, 30),
                    Font = new Font("Segoe UI", 12F)
                };
                var ok = new Button
                {
                    Text = "Load", DialogResult = DialogResult.OK,
                    Location = new Point(292, 100), Size = new Size(120, 34),
                    FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
                };
                ok.FlatAppearance.BorderSize = 0;
                var cancel = new Button
                {
                    Text = "Cancel", DialogResult = DialogResult.Cancel,
                    Location = new Point(186, 100), Size = new Size(96, 34),
                    FlatStyle = FlatStyle.Flat, BackColor = Chip, ForeColor = Ink,
                    Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand
                };
                cancel.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(lbl);
                dlg.Controls.Add(box);
                dlg.Controls.Add(ok);
                dlg.Controls.Add(cancel);
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                return dlg.ShowDialog(this) == DialogResult.OK ? box.Text : null;
            }
        }

        /// <summary>Search filters the pending list by transaction code or client name; a
        /// number (queue / txn) also selects the matching release directly.</summary>
        private void DoSearch()
        {
            string q = (txtSearch?.Text ?? "").Trim();
            if (q.Length == 0) { LoadPending(); return; }

            LoadPending(q);   // filter the grid by code / name

            // If the text is a number or exact code, also try to jump-select the row.
            int txnId = ResolveTxnId(q, out string status, out string foundCode);
            if (txnId > 0)
            {
                foreach (DataGridViewRow row in dgvPending.Rows)
                {
                    object v = row.Cells["id"].Value;
                    if (v != null && v != DBNull.Value && Convert.ToInt32(v) == txnId)
                    {
                        row.Selected = true;
                        dgvPending.CurrentCell = row.Cells["Txn Code"];
                        SelectRow(row);
                        return;
                    }
                }
                if (dgvPending.Rows.Count == 0)
                    lblValidation.Text = foundCode + " is not ready for release (status: " +
                        (string.IsNullOrEmpty(status) ? "unknown" : status) + ").";
            }
            if (dgvPending.Rows.Count == 0 && lblValidation != null)
                lblValidation.Text = "No pending release matches \"" + q + "\".";
        }

        /// <summary>
        /// Maps a typed value to a transaction id: first tries an exact transaction code,
        /// then a queue number (Q-001 / 001 — matched against today's queue tickets that are
        /// linked to a transaction). Returns 0 if nothing matches. Also returns the found
        /// code + that transaction's status for a helpful message.
        /// </summary>
        private int ResolveTxnId(string q, out string status, out string foundCode)
        {
            status = null;
            foundCode = q;

            // 1) Exact transaction code (e.g. TXN-2026-0007).
            DataTable t = Db.Pull(
                "SELECT id, txn_code, status FROM transactions WHERE txn_code = @c LIMIT 1",
                new MySqlParameter("@c", q));
            if (t.Rows.Count > 0)
            {
                status = t.Rows[0]["status"].ToString();
                foundCode = t.Rows[0]["txn_code"].ToString();
                return Convert.ToInt32(t.Rows[0]["id"]);
            }

            // 2) Queue number → its linked transaction. Accept the bare number (001, 1) by
            //    matching the numeric tail of today's ticket codes, or the full code (Q-001).
            int n = 0;
            int.TryParse(new string(q.Where(char.IsDigit).ToArray()), out n);
            DataTable qt = Db.Pull(
                "SELECT qt.transaction_id AS tid, qt.ticket_code AS code " +
                "FROM queue_tickets qt " +
                "WHERE qt.transaction_id IS NOT NULL AND ( qt.ticket_code = @raw " +
                "  OR (@n > 0 AND CAST(SUBSTRING_INDEX(qt.ticket_code, '-', -1) AS UNSIGNED) = @n " +
                "      AND DATE(qt.created_at) = CURDATE()) ) " +
                "ORDER BY qt.id DESC LIMIT 1",
                new MySqlParameter("@raw", q),
                new MySqlParameter("@n", n));
            if (qt.Rows.Count > 0)
            {
                int tid = Convert.ToInt32(qt.Rows[0]["tid"]);
                foundCode = qt.Rows[0]["code"].ToString();
                DataTable st = Db.Pull("SELECT status FROM transactions WHERE id = @id LIMIT 1",
                    new MySqlParameter("@id", tid));
                if (st.Rows.Count > 0) status = st.Rows[0]["status"].ToString();
                return tid;
            }
            return 0;
        }

        // ---- WORKSPACE: "what to do about it" — header, state-driven body, one action ----
        private Control BuildWorkspace()
        {
            var card = new Card(null) { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };

            // ---------- header: status, who, what ----------
            _wHead = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = CardBg, Padding = new Padding(4, 8, 4, 6) };
            _pill = new StatusPill
            {
                Text = "No request selected",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(4, 4)
            };
            _pill.SetTone(Chip, Muted);

            lblSelected.AutoSize = true;
            lblSelected.UseMnemonic = false;
            lblSelected.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            lblSelected.ForeColor = Ink;
            lblSelected.Location = new Point(2, 30);
            lblSelected.Text = "—";

            _wMeta = new Label
            {
                AutoSize = false, Dock = DockStyle.Bottom, Height = 20, UseMnemonic = false,
                ForeColor = Muted, Font = new Font("Segoe UI", 9.5F), Text = ""
            };
            _wHead.Controls.Add(_wMeta);
            _wHead.Controls.Add(lblSelected);
            _wHead.Controls.Add(_pill);

            // ---------- body ----------
            _wBody = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, AutoScroll = true, Padding = new Padding(2, 6, 2, 6) };

            // Dock=Top children in an AutoScroll panel, with EXPLICIT heights. An AutoSize
            // TableLayoutPanel here overflows the stack instead: its own height depends on
            // its children while a Dock=Fill child's height depends on it, and WinForms
            // resolves that by recursing until the process dies (measured — StackOverflow
            // on construction, before anything reaches the screen).
            _pnlSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 2, BackColor = CardBg, Height = 0,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows, Margin = new Padding(0)
            };
            _pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            _pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _pnlClaim = BuildClaimBlock();
            _pnlEmpty = BuildEmptyBlock();

            // added in reverse: Dock=Top stacks the LAST added at the top.
            // NOTHING here is Dock=Fill: a Fill child inside an AutoScroll panel that also
            // holds Top children oscillates — the scrollbar appears, the display rect
            // shrinks, the Fill child resizes, the scrollbar goes away — and WinForms
            // resolves that by recursing until the process dies.
            _wBody.Controls.Add(_pnlEmpty);
            _wBody.Controls.Add(_pnlClaim);
            _wBody.Controls.Add(BuildNotePanel());
            _wBody.Controls.Add(_pnlSummary);

            // ---------- footer: where this goes, and the one action ----------
            var foot = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = CardBg, Padding = new Padding(0, 10, 0, 4) };
            btnRelease.Dock = DockStyle.Right;
            btnRelease.Width = 250;
            btnRelease.Text = "Release document";
            btnRelease.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnRelease.FlatStyle = FlatStyle.Flat;
            btnRelease.BackColor = Green;
            btnRelease.ForeColor = Color.White;
            btnRelease.Cursor = Cursors.Hand;
            btnRelease.FlatAppearance.BorderSize = 0;
            btnRelease.Margin = new Padding(0);
            _nextLab = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, ForeColor = Muted, UseMnemonic = false,
                Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 12, 0), Text = ""
            };
            foot.Controls.Add(_nextLab);     // Fill first
            foot.Controls.Add(btnRelease);   // Right

            // ORDER MATTERS: a docked child added LATER is laid out FIRST, so the Fill
            // control must be added FIRST to receive what is left. Adding Fill last is what
            // made the old Release button share pixels with the fields panel above it.
            card.Content.Controls.Add(_wBody);   // Fill
            card.Content.Controls.Add(foot);     // Bottom
            card.Content.Controls.Add(_wHead);   // Top
            return card;
        }

        /// <summary>The one-line explanation under the request summary (reuses lblClaimStatus).</summary>
        private Panel BuildNotePanel()
        {
            _notePanel = new Panel
            {
                Dock = DockStyle.Top, BackColor = UiTheme.AccentTint, Padding = new Padding(14, 10, 12, 10),
                Height = 62, Margin = new Padding(2, 2, 2, 10), Visible = false
            };
            _noteBar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = Accent };
            lblClaimStatus = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, UseMnemonic = false,
                ForeColor = Ink, Font = new Font("Segoe UI", 9.5F), Text = ""
            };
            _notePanel.Controls.Add(lblClaimStatus);   // Fill first
            _notePanel.Controls.Add(_noteBar);         // Left
            return _notePanel;
        }

        private void SetNote(string text, Color tint, Color bar)
        {
            if (_notePanel == null) return;
            bool has = !string.IsNullOrWhiteSpace(text);
            _notePanel.Visible = has;
            OrderBody();
            if (!has) { lblClaimStatus.Text = ""; return; }
            lblClaimStatus.Text = text;
            _notePanel.BackColor = tint;
            _noteBar.BackColor = bar;
        }

        /// <summary>Shown when nothing is picked — instead of live fields for a release
        /// that does not exist yet, and a button whose only answer is a message box.</summary>
        private Panel BuildEmptyBlock()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = CardBg, Visible = false };
            var big = new Label
            {
                Text = "No request selected", Dock = DockStyle.Top, Height = 30, AutoSize = false,
                ForeColor = Muted, Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var sm = new Label
            {
                Text = "Pick a request on the left, or scan the claimant's QR to load it here.",
                Dock = DockStyle.Top, Height = 26, AutoSize = false, ForeColor = Faint,
                Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleCenter
            };
            var spacer = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = CardBg };
            p.Controls.Add(sm);
            p.Controls.Add(big);
            p.Controls.Add(spacer);
            return p;
        }

        /// <summary>
        /// Everything the officer needs to hand a document over: who is claiming, the
        /// representative's ID (only once the box is ticked), the two faces side by side,
        /// the optional camera, and the before-releasing list. Hidden entirely while the
        /// request is not yet releasable — the fields cannot be acted on then.
        /// </summary>
        private TableLayoutPanel BuildClaimBlock()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 1, BackColor = CardBg,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows, Margin = new Padding(0)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddStack(t, Section("WHO IS CLAIMING"), 24);

            var nameRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = CardBg
            };
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lblClaimant.AutoSize = true;
            lblClaimant.Anchor = AnchorStyles.Left;
            lblClaimant.Margin = new Padding(4, 10, 8, 0);
            txtClaimant.Dock = DockStyle.Fill;
            txtClaimant.Font = new Font("Segoe UI", 11F);
            txtClaimant.Margin = new Padding(0, 5, 8, 5);
            nameRow.Controls.Add(lblClaimant, 0, 0);
            nameRow.Controls.Add(txtClaimant, 1, 0);
            AddStack(t, nameRow, 42);

            chkRep.AutoSize = true;
            chkRep.Margin = new Padding(4, 4, 4, 4);
            AddStack(t, chkRep, 28);

            // Progressive disclosure: the representative's ID fields only exist when a
            // representative is claiming. Live for every release, they read as two more
            // blanks the officer has failed to fill in.
            _repBlock = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = UiTheme.Surface,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Padding = new Padding(10, 2, 4, 2),
                Visible = false
            };
            _repBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            _repBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            txtIdType.Font = new Font("Segoe UI", 11F);
            txtIdNum.Font = new Font("Segoe UI", 11F);
            AddFieldRow(_repBlock, lblIdType, txtIdType);
            AddFieldRow(_repBlock, lblIdNum, txtIdNum);
            _repRow = t.RowCount;
            AddStack(t, _repBlock, 0);   // 0 until the box is ticked (see ShowRepFields)
            chkRep.CheckedChanged += (s, e) => ShowRepFields(chkRep.Checked);

            AddStack(t, Section("IDENTITY EVIDENCE"), 24);

            // The two faces sit SIDE BY SIDE because that is the officer's actual task —
            // compare the ID the claimant uploaded against the photo the kiosk took.
            lblPhotoCap = new Label
            {
                Text = "Kiosk photo", Dock = DockStyle.Top, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink
            };
            picClient = new PictureBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = UiTheme.PageBg
            };
            lblPhotoState = new Label
            {
                Text = "—", Dock = DockStyle.Bottom, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 8.25F), ForeColor = Faint
            };
            lblUploadedIdCap = new Label
            {
                Text = "Uploaded ID", Dock = DockStyle.Top, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink
            };
            picUploadedId = new PictureBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = UiTheme.PageBg
            };
            lblIdState = new Label
            {
                Text = "—", Dock = DockStyle.Bottom, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 8.25F), ForeColor = Faint
            };
            var faces = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = CardBg
            };
            faces.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            faces.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            faces.Controls.Add(FacePane(lblPhotoCap, picClient, lblPhotoState, new Padding(0, 0, 6, 0)), 0, 0);
            faces.Controls.Add(FacePane(lblUploadedIdCap, picUploadedId, lblIdState, new Padding(6, 0, 0, 0)), 1, 0);
            AddStack(t, faces, 250);

            // "Use Camera?" toggle — always visible (outside the collapsible block).
            var toggleRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = CardBg };
            lblUseCam.AutoSize = true; lblUseCam.Margin = new Padding(2, 8, 6, 0);
            tglUseCam.Margin = new Padding(0, 2, 6, 0);
            lblUseCamState.AutoSize = true; lblUseCamState.Margin = new Padding(0, 8, 0, 0);
            toggleRow.Controls.Add(lblUseCam);
            toggleRow.Controls.Add(tglUseCam);
            toggleRow.Controls.Add(lblUseCamState);
            AddStack(t, toggleRow, 38);

            // Camera preview block — collapses to zero height when the camera is Off.
            _camPanel = BuildCameraPanel();
            _camRow = t.RowCount;
            AddStack(t, _camPanel, 0);

            AddStack(t, BuildChecklist(), 132);

            lblValidation = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, ForeColor = UiTheme.Danger,
                Font = new Font("Segoe UI", 9F), Text = ""
            };
            AddStack(t, lblValidation, 30);
            _claimStack = t;
            SizeClaimBlock();
            return t;
        }

        /// <summary>Opens / closes the representative's ID fields and re-measures the block.</summary>
        private void ShowRepFields(bool on)
        {
            if (_repBlock == null) return;
            _repBlock.Visible = on;
            _claimStack.RowStyles[_repRow].Height = on ? RepRowHeight : 0;
            SizeClaimBlock();
            UpdateChecklist();
        }

        /// <summary>Opens / closes the camera preview row and re-measures the block.</summary>
        private void ShowCameraRow(bool on)
        {
            if (_camPanel == null || _claimStack == null) return;
            _claimStack.RowStyles[_camRow].Height = on ? CamRowHeight : 0;
            SizeClaimBlock();
        }

        /// <summary>
        /// The claim block states its own height from its rows. It cannot AutoSize: it holds
        /// Dock=Fill children whose height comes FROM it, so AutoSize makes the measurement
        /// circular (that is the StackOverflow noted in BuildWorkspace).
        /// </summary>
        private void SizeClaimBlock()
        {
            if (_claimStack == null) return;
            float h = 0;
            foreach (RowStyle rs in _claimStack.RowStyles) h += rs.Height;
            // AddStack gives every row a 3px margin top and bottom.
            _claimStack.Height = (int)h + _claimStack.RowCount * 6 + 4;
        }

        private static Label Section(string text) => new Label
        {
            Text = text, Dock = DockStyle.Fill, AutoSize = false,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = UiTheme.Faint,
            TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(2, 0, 0, 2)
        };

        /// <summary>Rebuilds the request summary rows (label / value pairs, in order).</summary>
        private void SetSummary(params string[] pairs)
        {
            if (_pnlSummary == null) return;
            _pnlSummary.SuspendLayout();
            _pnlSummary.Controls.Clear();
            _pnlSummary.RowStyles.Clear();
            _pnlSummary.RowCount = 0;
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                if (string.IsNullOrWhiteSpace(pairs[i + 1])) continue;
                int r = _pnlSummary.RowCount++;
                _pnlSummary.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                _pnlSummary.Controls.Add(new Label
                {
                    Text = pairs[i], Dock = DockStyle.Fill, AutoSize = false, UseMnemonic = false,
                    ForeColor = Muted, Font = new Font("Segoe UI", 9.5F),
                    TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4, 0, 0, 0)
                }, 0, r);
                _pnlSummary.Controls.Add(new Label
                {
                    Text = pairs[i + 1], Dock = DockStyle.Fill, AutoSize = false, UseMnemonic = false,
                    ForeColor = Ink, Font = new Font("Segoe UI", 9.5F),
                    TextAlign = ContentAlignment.MiddleLeft
                }, 1, r);
            }
            _pnlSummary.Height = _pnlSummary.RowCount * 26 + 8;
            _pnlSummary.Visible = _pnlSummary.RowCount > 0;
            _pnlSummary.ResumeLayout(true);
        }

        // ================================================================
        //  ApplyState — the whole screen in one place.
        //
        //  What the request IS decides what is on the screen and what the
        //  single action does. Showing every state at once is what produced
        //  two buttons for "send to payment", claimant fields on a request
        //  that cannot be released, and a Release button whose only possible
        //  answer was a message box.
        // ================================================================
        private void ApplyState(string status)
        {
            if (_pnlClaim == null) return;   // called before the layout exists

            bool picked = _selectedTxnId.HasValue || _pickupClaimId.HasValue;
            // A kiosk pickup claim has no transaction; it releases directly, so it is
            // treated as ready (that is what PrepareFromQueueTicket already decided).
            string st = _pickupClaimId.HasValue && !_selectedTxnId.HasValue
                ? "ForRelease"
                : (status ?? "");
            _state = picked ? st : "";

            _pnlEmpty.Visible = !picked;
            _pnlSummary.Visible = picked && _pnlSummary.RowCount > 0;
            _notePanel.Visible = picked && !string.IsNullOrWhiteSpace(lblClaimStatus.Text);
            _wHead.Visible = picked;
            if (!picked)
            {
                // Hide the claim block too. Leaving it up put live claimant fields, both
                // face panes and the checklist on screen for a release that has not been
                // chosen — the exact "looks ready, nothing behind it" state this screen
                // is meant to remove.
                _pnlClaim.Visible = false;
                btnRelease.Text = "Release document";
                btnRelease.Enabled = false;
                btnRelease.BackColor = Chip;
                btnRelease.ForeColor = Faint;
                _nextLab.Text = "";
                return;
            }

            bool ready = st == "ForRelease";
            _pnlClaim.Visible = ready;
            // Nothing below the claim block should keep the camera running when the block
            // itself is gone — a hidden preview still holds the device open.
            if (!ready && tglUseCam.Checked) tglUseCam.Checked = false;

            switch (st)
            {
                case "ForRelease":
                    _pill.Text = _pickupClaimId.HasValue ? "Claim · ready to release" : "Paid · ready to release";
                    _pill.SetTone(UiTheme.SuccessTint, Green);
                    PrimaryButton("Verify && Release…", Green, Color.White, true);
                    _nextLab.Text = "Next: confirm the ID matches, then hand over";
                    break;

                case "WaitingToRelease":
                case "ForPrint":
                    _pill.Text = st == "ForPrint" ? "Awaiting print" : "Waiting to release";
                    _pill.SetTone(UiTheme.WarningTint, Amber);
                    PrimaryButton("Send to Payment", Amber, Color.White, true);
                    _nextLab.Text = "Next: Fees & Payments";
                    break;

                case "ForPayment":
                    _pill.Text = "At the cashier";
                    _pill.SetTone(UiTheme.AccentTint, Accent);
                    PrimaryButton("Open in Fees && Payments", Chip, Accent, true);
                    _nextLab.Text = "Nothing to do here until the Official Receipt is issued";
                    break;

                case "Released":
                    _pill.Text = "Released";
                    _pill.SetTone(UiTheme.SuccessTint, Green);
                    PrimaryButton("View release details", Chip, Accent, true);
                    _nextLab.Text = "Transaction closed";
                    break;

                default:
                    _pill.Text = string.IsNullOrEmpty(st) ? "Unknown status" : st;
                    _pill.SetTone(Chip, Muted);
                    PrimaryButton("Release document", Chip, Faint, false);
                    _nextLab.Text = "This request is not at a releasing step";
                    break;
            }

            if (ready) UpdateChecklist();
            else btnRelease.Enabled = true;   // the non-release actions do not need the checklist
            OrderBody();
        }

        /// <summary>
        /// Re-asserts the body's top-to-bottom order: summary, note, claim, empty.
        /// Dock=Top order follows z-order, and a control that was hidden when the panel
        /// last laid out does NOT reclaim its place on its own — the note came back
        /// UNDER the claim block, explaining a state 600px below the state it described.
        /// Index 0 docks last (bottom), so the order here reads bottom-up.
        /// </summary>
        private void OrderBody()
        {
            if (_wBody == null) return;
            _wBody.Controls.SetChildIndex(_pnlEmpty, 0);
            _wBody.Controls.SetChildIndex(_pnlClaim, 1);
            _wBody.Controls.SetChildIndex(_notePanel, 2);
            _wBody.Controls.SetChildIndex(_pnlSummary, 3);
        }

        private void PrimaryButton(string text, Color back, Color fore, bool enabled)
        {
            btnRelease.Text = text;
            btnRelease.BackColor = back;
            btnRelease.ForeColor = fore;
            btnRelease.Enabled = enabled;
            btnRelease.FlatAppearance.MouseOverBackColor = UiTheme.Mix(back, Color.Black, 0.10f);
            btnRelease.FlatAppearance.BorderSize = back == Chip ? 1 : 0;
            btnRelease.FlatAppearance.BorderColor = UiTheme.CardLine;
        }

        /// <summary>
        /// "Before releasing" — the four things the officer should have in front of them.
        /// These are FACTS the screen already knows (is a release picked, is there a name,
        /// is each photo on file), not a verdict: CROMS does not match a face to an ID, so
        /// the list never claims the claimant was verified, only what is on file.
        /// </summary>
        private Control BuildChecklist()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CardBg };
            var cap = new Label
            {
                Text = "BEFORE RELEASING", Dock = DockStyle.Top, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Accent
            };
            _chkId       = CheckLine();
            _chkPhoto    = CheckLine();
            _chkClaimant = CheckLine();
            _chkSelected = CheckLine();
            p.Controls.Add(_chkId);
            p.Controls.Add(_chkPhoto);
            p.Controls.Add(_chkClaimant);
            p.Controls.Add(_chkSelected);
            p.Controls.Add(cap);
            return p;
        }

        private static Label CheckLine() => new Label
        {
            Dock = DockStyle.Top, Height = 25, AutoSize = false, Text = "",
            Font = new Font("Segoe UI", 9F), ForeColor = UiTheme.Faint,
            TextAlign = ContentAlignment.MiddleLeft
        };

        /// <summary>Refreshes the checklist and gates the Release button on a selection.</summary>
        private void UpdateChecklist()
        {
            if (_chkSelected == null) return;
            bool picked   = _selectedTxnId.HasValue || _pickupClaimId.HasValue;
            bool named    = txtClaimant != null && txtClaimant.Text.Trim().Length > 0;
            bool hasPhoto = picClient != null && picClient.Image != null;
            bool hasId    = picUploadedId != null && picUploadedId.Image != null;

            Mark(_chkSelected, picked,   "Release selected",        "Select a release on the left");
            Mark(_chkClaimant, named,    "Claimant name entered",   "Enter who is claiming");
            Mark(_chkPhoto,    hasPhoto, "Kiosk photo on file",     "No kiosk photo on file");
            Mark(_chkId,       hasId,    "Uploaded ID on file",     "No uploaded ID on file");

            // Nothing to release until something is picked. Leaving it live meant a click
            // that could only ever answer with a message box. Only the RELEASE state is
            // gated here — "Send to Payment" and the read-only actions do not need a
            // claimant, and ApplyState owns their enabled state.
            if (_state == "ForRelease") btnRelease.Enabled = picked;
        }

        private static void Mark(Label lbl, bool ok, string yes, string no)
        {
            lbl.Text = (ok ? "✓   " : "○   ") + (ok ? yes : no);
            lbl.ForeColor = ok ? UiTheme.Ink : UiTheme.Faint;
        }

        /// <summary>Camera preview + controls in one fixed-height panel (collapsible as a unit).</summary>
        private Panel BuildCameraPanel()
        {
            var p = new Panel { Dock = DockStyle.Fill, Height = 290, BackColor = CardBg };

            // Dock=Top stacks by add order (first added = topmost).
            lblCam.Dock = DockStyle.Top; lblCam.Height = 20; lblCam.AutoSize = false;
            pbCam.Dock = DockStyle.Top; pbCam.Height = 150;
            cboCamera.Dock = DockStyle.Top; cboCamera.Margin = new Padding(0, 4, 0, 4);

            var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, WrapContents = false, BackColor = CardBg };
            btnStartCam.Margin = new Padding(0, 4, 6, 4);
            btnCapture.Margin = new Padding(0, 4, 6, 4);
            btnRetake.Margin = new Padding(0, 4, 0, 4);
            btnRow.Controls.Add(btnStartCam);
            btnRow.Controls.Add(btnCapture);
            btnRow.Controls.Add(btnRetake);

            lblCamStatus.Dock = DockStyle.Top; lblCamStatus.Height = 40; lblCamStatus.AutoSize = false;

            // add reversed so visual order is: lblCam, pbCam, cboCamera, buttons, status
            p.Controls.Add(lblCamStatus);
            p.Controls.Add(btnRow);
            p.Controls.Add(cboCamera);
            p.Controls.Add(pbCam);
            p.Controls.Add(lblCam);
            return p;
        }

        /// <summary>One face pane: caption on top, picture filling, state line underneath.</summary>
        private static Panel FacePane(Label cap, PictureBox pic, Label state, Padding margin)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, Margin = margin };
            p.Controls.Add(pic);      // Fill added first so the docked edges keep their space
            p.Controls.Add(state);    // Bottom
            p.Controls.Add(cap);      // Top
            return p;
        }

        /// <summary>Adds a row that soaks up whatever height the column has left over.</summary>
        private static void AddStackFill(TableLayoutPanel t, Control c)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            c.Margin = new Padding(2, 3, 2, 3);
            t.Controls.Add(c, 0, r);
        }

        private static void AddStackAuto(TableLayoutPanel t, Control c)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            c.Margin = new Padding(2, 3, 2, 3);
            t.Controls.Add(c, 0, r);
        }

        // ------------------------------- layout helpers -------------------------------
        private static void AddFieldRow(TableLayoutPanel t, Label label, Control input)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(4, 17, 8, 0);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 12, 8, 12);
            t.Controls.Add(label, 0, r);
            t.Controls.Add(input, 1, r);
        }

        private static void AddRowSpan(TableLayoutPanel t, Control c, int height)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            c.Margin = new Padding(4, 4, 8, 4);
            t.Controls.Add(c, 0, r);
            t.SetColumnSpan(c, 2);
        }

        private static void AddStack(TableLayoutPanel t, Control c, int height)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            c.Margin = new Padding(2, 3, 2, 3);
            t.Controls.Add(c, 0, r);
        }

        private static Button MakeMiniButton(Color back, Color fore, string text) => new Button
        {
            Text = text, Width = 78, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false, Margin = new Padding(0)
        };
        private static Button MakeMiniButton(string text, Color back, Color fore)
        {
            var b = MakeMiniButton(back, fore, text);
            b.FlatAppearance.BorderSize = fore == Color.White ? 0 : 1;
            b.FlatAppearance.BorderColor = UiTheme.CardLine;
            return b;
        }

        private static void StyleGrid(DataGridView g)
        {
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToResizeRows = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Color.White;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.PageBg;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            g.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.RowLine;
            g.DefaultCellStyle.SelectionBackColor = UiTheme.AccentTint;
            g.DefaultCellStyle.SelectionForeColor = UiTheme.Accent;
        }

        /// <summary>Grey placeholder cue text on an empty textbox (WinForms lacks PlaceholderText on .NET FW).</summary>
        private static void SetCue(TextBox tb, string cue)
        {
            // EM_SETCUEBANNER = 0x1501
            NativeSetCue(tb.Handle, cue);
            tb.HandleCreated += (s, e) => NativeSetCue(tb.Handle, cue);
        }
        private static void NativeSetCue(IntPtr h, string cue)
        {
            try { SendMessage(h, 0x1501, (IntPtr)1, cue); } catch { }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        // ---------- optional camera ----------
        private void tglUseCam_CheckedChanged(object sender, EventArgs e) => ApplyCameraOption();

        /// <summary>
        /// Shows and enables the webcam panel when the toggle is On; hides it and releases
        /// the camera (without capturing a photo) when Off. The claimant photo stays optional
        /// either way — a release never requires it.
        /// </summary>
        private void ApplyCameraOption()
        {
            bool use = tglUseCam.Checked;
            lblUseCamState.Text = use ? "On" : "Off";
            lblUseCamState.ForeColor = use
                ? UiTheme.Success                                     // green when On
                : UiTheme.Muted;                                      // grey when Off

            if (!use) ResetCamera();            // stop capture + drop any pending photo

            // Collapse the whole preview block as a unit (no leftover gap when Off).
            if (_camPanel != null) _camPanel.Visible = use;
            ShowCameraRow(use);   // the row collapses with it, so no leftover gap

            if (use && cboCamera.Items.Count == 0) PopulateCameras();
        }

        /// <summary>Fills the camera dropdown with every connected video-input device.</summary>
        private void PopulateCameras()
        {
            cboCamera.Items.Clear();
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo dev in _videoDevices)
                    cboCamera.Items.Add(dev.Name);

                if (cboCamera.Items.Count > 0)
                {
                    cboCamera.SelectedIndex = 0;
                    btnStartCam.Enabled = true;
                }
                else
                {
                    lblCamStatus.Text = "No camera found — connect a webcam. (Photo is optional; you can still release.)";
                    btnStartCam.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                lblCamStatus.Text = "Could not list cameras: " + ex.Message;
            }
        }

        /// <summary>Switching the camera while it is live restarts the feed on the new device.</summary>
        private void cboCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_camera != null && _camera.IsRunning)
                StartCamera();   // re-open on the newly selected device
        }

        /// <summary>
        /// Government-issued IDs commonly accepted for identity verification across
        /// Philippine government offices (PSA / LGU front desks). The combo is editable
        /// (DropDown), so an ID not on the list can still be typed in, and "Other" is
        /// provided for anything uncommon.
        /// </summary>
        private void PopulateIdTypes()
        {
            txtIdType.Items.AddRange(new object[]
            {
                "Philippine National ID (PhilSys)",
                "Philippine Passport (DFA)",
                "Driver's License (LTO)",
                "UMID (Unified Multi-Purpose ID)",
                "SSS ID",
                "GSIS eCard",
                "PRC ID (Professional License)",
                "Voter's ID / COMELEC Certification",
                "Postal ID (PHLPost)",
                "PhilHealth ID",
                "TIN ID (BIR)",
                "Pag-IBIG Loyalty Card Plus",
                "Senior Citizen ID (OSCA)",
                "PWD ID",
                "Solo Parent ID",
                "Barangay ID / Certification (with photo)",
                "NBI Clearance",
                "Police Clearance",
                "OWWA ID / iDOLE",
                "Seafarer's Record Book (SIRB)",
                "IBP ID (Integrated Bar of the Philippines)",
                "AFP / PVAO ID",
                "Company / School ID",
                "Other"
            });
        }

        private void ShowPhotoFor(int txnId)
        {
            // Kiosk face photo (queue_tickets.id_image).
            picClient.Image?.Dispose();
            picClient.Image = null;
            DataTable dt = Db.Pull(
                "SELECT id_image FROM queue_tickets WHERE transaction_id = " + txnId +
                " AND id_image IS NOT NULL ORDER BY id DESC LIMIT 1");
            if (dt.Rows.Count > 0 && dt.Rows[0]["id_image"] != DBNull.Value)
            {
                try
                {
                    using (var ms = new MemoryStream((byte[])dt.Rows[0]["id_image"]))
                        picClient.Image = Image.FromStream(ms);
                }
                catch { /* stored value wasn't a readable image */ }
            }
            // States the FACT, not a verdict: CROMS does not match faces, the officer does.
            SetFaceState(lblPhotoState, picClient.Image != null, "On file", "No kiosk photo");

            ShowUploadedIdFor(txnId);
            UpdateChecklist();
        }

        /// <summary>
        /// Loads the ID image + OCR'd name uploaded via claimapp for this transaction. Every
        /// kiosk visit now creates a claim_requests row for its "Upload Your ID" QR (not just
        /// Release &amp; Claim pickups), so this is looked up two ways: linked directly by
        /// transaction_id (the reclaim/pickup path), or via the queue ticket that carried this
        /// transaction (a first-time visit — the QR was created before the transaction existed,
        /// so only queue_ticket_id was set at the time).
        /// </summary>
        private void ShowUploadedIdFor(int txnId)
        {
            picUploadedId.Image?.Dispose();
            picUploadedId.Image = null;
            DataTable dt = Db.Pull(
                "SELECT cr.id_image, cr.id_first_name, cr.id_middle_name, cr.id_last_name " +
                "FROM claim_requests cr LEFT JOIN queue_tickets qt ON qt.id = cr.queue_ticket_id " +
                "WHERE cr.transaction_id = " + txnId + " OR qt.transaction_id = " + txnId +
                " ORDER BY cr.id DESC LIMIT 1");
            if (dt.Rows.Count == 0 || dt.Rows[0]["id_image"] == DBNull.Value)
            {
                SetFaceState(lblIdState, false, null, "Not yet uploaded");
                return;
            }

            DataRow r = dt.Rows[0];
            string idName = JoinName(r["id_first_name"], r["id_middle_name"], r["id_last_name"]);
            try
            {
                using (var ms = new MemoryStream((byte[])r["id_image"]))
                    picUploadedId.Image = Image.FromStream(ms);
            }
            catch { /* stored value wasn't a readable image */ }
            // The name the claimant's ID was read as — the officer checks it against
            // the claimant name on the left. Shown, never auto-matched.
            SetFaceState(lblIdState, true,
                idName.Length > 0 ? "Name on ID: " + idName : "On file", null);
        }

        /// <summary>
        /// Writes the small line under a face pane. Present is stated in the ink tone,
        /// absent in the faint one, so a missing photo reads as missing at a glance.
        /// </summary>
        private static void SetFaceState(Label lbl, bool present, string yes, string no)
        {
            if (lbl == null) return;
            lbl.Text = present ? (yes ?? "On file") : (no ?? "—");
            lbl.ForeColor = present ? UiTheme.Ink : UiTheme.Faint;
        }

        private static string JoinName(object f, object m, object l)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (f != DBNull.Value && f != null && f.ToString().Length > 0) parts.Add(f.ToString());
            if (m != DBNull.Value && m != null && m.ToString().Length > 0) parts.Add(m.ToString());
            if (l != DBNull.Value && l != null && l.ToString().Length > 0) parts.Add(l.ToString());
            return string.Join(" ", parts);
        }

        /// <summary>Opens this window with a transaction pre-selected (from Fees &amp; Payments).</summary>
        public void PreselectTransaction(long txnId)
        {
            LoadPending();
            foreach (DataGridViewRow row in dgvPending.Rows)
            {
                object v = row.Cells["id"].Value;
                if (v != null && v != DBNull.Value && Convert.ToInt64(v) == txnId)
                {
                    row.Selected = true;
                    dgvPending.CurrentCell = row.Cells["Txn Code"];
                    SelectRow(row);
                    break;
                }
            }
        }

        /// <summary>
        /// Selects a pending row: fills the workspace header and request summary, loads the
        /// kiosk photo, and hands the row's STATUS to ApplyState — which decides what the
        /// screen shows and what its single action does.
        /// </summary>
        private void SelectRow(DataGridViewRow row)
        {
            _pickupClaimId = null;
            _selectedTxnId = Convert.ToInt32(row.Cells["id"].Value);
            string code = Convert.ToString(row.Cells["Txn Code"].Value);
            string client = Convert.ToString(row.Cells["Client"].Value);
            if (lblValidation != null) lblValidation.Text = "";

            DataTable stt = Db.Pull("SELECT status FROM transactions WHERE id = @t LIMIT 1",
                new MySqlParameter("@t", _selectedTxnId.Value));
            string st = stt.Rows.Count > 0 ? stt.Rows[0]["status"].ToString() : "";

            lblSelected.Text = string.IsNullOrWhiteSpace(client) ? code : client;
            _wMeta.Text = Join(" · ", code, Cell(row, "Type"), Cell(row, "Queue Ticket"));
            SetSummary(
                "Transaction", code,
                "Client", client,
                "Document", Cell(row, "Type"),
                "Queue ticket", Cell(row, "Queue Ticket"),
                "Requested", Cell(row, "Requested"));
            SetStateNote(st);
            ShowPhotoFor(_selectedTxnId.Value);
            ApplyState(st);
            if (st == "ForRelease") txtClaimant.Focus();
        }

        /// <summary>The one-line explanation of the state, in the officer's terms.</summary>
        private void SetStateNote(string st)
        {
            switch (st)
            {
                case "ForRelease":
                    SetNote("Paid and ready. Compare the uploaded ID against the kiosk photo, "
                          + "then Verify & Release.", UiTheme.SuccessTint, Green);
                    break;
                case "WaitingToRelease":
                    SetNote("Parked at the window — no fee has been assessed yet. Send it to the "
                          + "cashier to continue; claimant verification happens after payment.",
                          UiTheme.WarningTint, Amber);
                    break;
                case "ForPrint":
                    SetNote("Still awaiting print, so no fee has been assessed yet. Send it to the "
                          + "cashier once the document is ready.", UiTheme.WarningTint, Amber);
                    break;
                case "ForPayment":
                    SetNote("At the cashier. This request returns to the For Release list on its "
                          + "own once the Official Receipt is issued.", UiTheme.AccentTint, Accent);
                    break;
                case "Released":
                    SetNote("Already released — this transaction is closed and in the audit trail.",
                          UiTheme.SuccessTint, Green);
                    break;
                default:
                    SetNote("", UiTheme.AccentTint, Accent);
                    break;
            }
        }

        private static string Cell(DataGridViewRow row, string column)
        {
            if (!row.DataGridView.Columns.Contains(column)) return null;
            object v = row.Cells[column].Value;
            string s = v == null || v == DBNull.Value ? null : Convert.ToString(v);
            return s == "—" ? null : s;
        }

        private static string Join(string sep, params string[] parts)
        {
            var keep = new System.Collections.Generic.List<string>();
            foreach (string p in parts) if (!string.IsNullOrWhiteSpace(p)) keep.Add(p.Trim());
            return string.Join(sep, keep.ToArray());
        }

        /// <summary>
        /// Opens Release &amp; Claim pre-loaded from a kiosk pickup ticket (clicked in Queue
        /// Management). Fills the claimant name, kiosk face photo, and the claimapp-uploaded ID,
        /// so staff verify + release on this one screen. If the linked claim already has a
        /// paid ForRelease transaction it uses the normal transaction path; otherwise it loads
        /// the claim_requests row and Release closes the claim directly.
        /// </summary>
        public void PrepareFromQueueTicket(int ticketId)
        {
            _pickupClaimId = null;
            _pickupClaimCode = null;

            // Find the claim tied to this kiosk ticket (direct link, else via the ticket's txn).
            DataTable c = Db.Pull(
                "SELECT id, claim_ticket_no, transaction_id, first_name, middle_name, last_name, status " +
                "FROM claim_requests WHERE queue_ticket_id = @tid " +
                "OR (transaction_id IS NOT NULL AND transaction_id = " +
                "    (SELECT transaction_id FROM queue_tickets WHERE id = @tid)) " +
                "ORDER BY id DESC LIMIT 1",
                new MySqlParameter("@tid", ticketId));

            if (c.Rows.Count == 0)
            {
                // No claim row — just show the kiosk face photo so staff can still verify.
                ShowClaimImages(ticketId, null);
                if (lblClaimStatus != null)
                    lblClaimStatus.Text = "Loaded queue ticket, but no claim request is linked.";
                return;
            }

            DataRow r = c.Rows[0];
            int claimId = Convert.ToInt32(r["id"]);
            string claimNo = Convert.ToString(r["claim_ticket_no"]);
            string name = JoinName(r["first_name"], r["middle_name"], r["last_name"]);

            // If a paid, ready transaction exists, use the normal release path.
            if (r["transaction_id"] != DBNull.Value)
            {
                long txn = Convert.ToInt64(r["transaction_id"]);
                DataTable stt = Db.Pull("SELECT status FROM transactions WHERE id = @t LIMIT 1",
                    new MySqlParameter("@t", txn));
                if (stt.Rows.Count > 0 && stt.Rows[0]["status"].ToString() == "ForRelease")
                {
                    SetListMode(0);
                    PreselectTransaction(txn);
                    if (!string.IsNullOrWhiteSpace(name)) txtClaimant.Text = name;
                    return;
                }
            }

            // Fresh pickup claim (no ready transaction) — release closes the claim directly.
            _pickupClaimId = claimId;
            _pickupClaimCode = claimNo;
            _selectedTxnId = null;
            if (!string.IsNullOrWhiteSpace(name)) txtClaimant.Text = name;
            lblSelected.Text = string.IsNullOrWhiteSpace(name) ? claimNo : name;
            _wMeta.Text = Join(" · ", "Kiosk pickup claim", claimNo);
            SetSummary(
                "Claim ticket", claimNo,
                "Claimant on file", name,
                "Claim status", Convert.ToString(r["status"]));
            SetNote("Kiosk pickup claim — releasing here closes the claim directly. Compare the "
                  + "uploaded ID against the kiosk photo, then Verify & Release.",
                  UiTheme.SuccessTint, Green);
            if (lblValidation != null) lblValidation.Text = "";
            ShowClaimImages(ticketId, claimId);
            ApplyState("ForRelease");   // a pickup claim has no transaction; it releases here
            txtClaimant.Focus();
        }

        /// <summary>Loads the kiosk face photo (by ticket id) and the uploaded ID (by claim id).</summary>
        private void ShowClaimImages(int ticketId, int? claimId)
        {
            picClient.Image?.Dispose(); picClient.Image = null;
            DataTable pt = Db.Pull(
                "SELECT id_image FROM queue_tickets WHERE id = @id AND id_image IS NOT NULL LIMIT 1",
                new MySqlParameter("@id", ticketId));
            if (pt.Rows.Count > 0 && pt.Rows[0]["id_image"] != DBNull.Value)
            {
                try { using (var ms = new MemoryStream((byte[])pt.Rows[0]["id_image"])) picClient.Image = Image.FromStream(ms); }
                catch { }
            }

            picUploadedId.Image?.Dispose(); picUploadedId.Image = null;
            lblUploadedIdCap.Text = "Uploaded ID (from claimapp)";
            if (claimId == null) { lblUploadedIdCap.Text = "Uploaded ID — no claim linked"; return; }

            DataTable ct = Db.Pull(
                "SELECT id_image, id_first_name, id_middle_name, id_last_name " +
                "FROM claim_requests WHERE id = @id LIMIT 1", new MySqlParameter("@id", claimId.Value));
            if (ct.Rows.Count == 0) { lblUploadedIdCap.Text = "Uploaded ID — none"; return; }
            DataRow cr = ct.Rows[0];
            if (cr["id_image"] == DBNull.Value) { lblUploadedIdCap.Text = "Uploaded ID — not yet uploaded"; return; }
            try { using (var ms = new MemoryStream((byte[])cr["id_image"])) picUploadedId.Image = Image.FromStream(ms); }
            catch { }
            string idName = JoinName(cr["id_first_name"], cr["id_middle_name"], cr["id_last_name"]);
            lblUploadedIdCap.Text = idName.Length > 0 ? "Uploaded ID — name on ID: " + idName
                                                      : "Uploaded ID (from claimapp)";
        }

        // ---------- claimant webcam ----------
        private void btnStartCam_Click(object sender, EventArgs e) => StartCamera();

        private void StartCamera()
        {
            try
            {
                if (_videoDevices == null || _videoDevices.Count == 0) PopulateCameras();
                if (_videoDevices == null || _videoDevices.Count == 0)
                {
                    lblCamStatus.Text = "No camera found — connect a webcam. (Photo is optional; you can still release.)";
                    return;
                }
                int idx = cboCamera.SelectedIndex;
                if (idx < 0 || idx >= _videoDevices.Count) idx = 0;

                StopCamera();
                _camera = new VideoCaptureDevice(_videoDevices[idx].MonikerString);
                _camera.NewFrame += OnFrame;
                _camera.Start();
                btnStartCam.Enabled = false;
                btnCapture.Enabled = true;
                btnRetake.Enabled = false;
                lblCamStatus.Text = "Live: " + cboCamera.Text + " — position the claimant, then click Capture.";
            }
            catch (Exception ex)
            {
                lblCamStatus.Text = "Camera error: " + ex.Message;
            }
        }

        private void OnFrame(object sender, NewFrameEventArgs e)
        {
            var frame = (Bitmap)e.Frame.Clone();
            lock (_frameLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = frame;
            }
            try
            {
                var disp = (Bitmap)frame.Clone();
                var old = pbCam.Image;
                pbCam.Image = disp;
                old?.Dispose();
            }
            catch { /* form closing / cross-thread paint — safe to ignore */ }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            lock (_frameLock)
            {
                if (_lastFrame == null) { lblCamStatus.Text = "The camera is not ready yet."; return; }
                using (var ms = new MemoryStream())
                {
                    _lastFrame.Save(ms, ImageFormat.Jpeg);
                    _photoBytes = ms.ToArray();
                }
                // freeze the captured still in the preview
                var old = pbCam.Image;
                pbCam.Image = (Bitmap)_lastFrame.Clone();
                old?.Dispose();
            }
            StopCamera();
            btnStartCam.Enabled = true;
            btnCapture.Enabled = false;
            btnRetake.Enabled = true;
            lblCamStatus.Text = "Photo captured. Click Retake to redo, or Release to finish.";
        }

        private void btnRetake_Click(object sender, EventArgs e)
        {
            _photoBytes = null;
            StartCamera();
        }

        private void StopCamera()
        {
            if (_camera != null && _camera.IsRunning)
            {
                _camera.NewFrame -= OnFrame;
                _camera.SignalToStop();
                _camera.WaitForStop();
            }
            _camera = null;
        }

        /// <summary>Clears the capture + returns the camera panel to its idle state.</summary>
        private void ResetCamera()
        {
            StopCamera();
            _photoBytes = null;
            lock (_frameLock) { _lastFrame?.Dispose(); _lastFrame = null; }
            var old = pbCam.Image;
            pbCam.Image = null;
            old?.Dispose();
            btnStartCam.Enabled = true;
            btnCapture.Enabled = false;
            btnRetake.Enabled = false;
            lblCamStatus.Text = "Camera idle. Click Start Camera to begin.";
        }

        /// <summary>Called by the shell each time this module is shown.</summary>
        public void RefreshData()
        {
            LoadPending();
            LoadReleased();
        }

        // ---------- data ----------
        private void LoadPending(string filter = null)
        {
            // For-Release tab = ready to hand over. Waiting tab = parked (client left /
            // certificate hard to find) OR still awaiting print — anything not yet paid.
            string where = _listMode == 1
                ? "status IN ('WaitingToRelease','ForPrint')"
                : "status = 'ForRelease'";
            MySqlParameter[] ps = new MySqlParameter[0];
            if (!string.IsNullOrWhiteSpace(filter))
            {
                where += " AND (txn_code LIKE @f OR client_name LIKE @f OR parked_ticket LIKE @f)";
                ps = new[] { new MySqlParameter("@f", "%" + filter.Trim() + "%") };
            }

            string ticketCol = _listMode == 1
                ? "COALESCE(parked_ticket, '—') AS 'Queue Ticket', "
                : "";
            dgvPending.DataSource = Db.Pull(
                "SELECT id, txn_code AS 'Txn Code', client_name AS Client, " + ticketCol +
                "type AS Type, DATE_FORMAT(created_at, '%b %d, %Y') AS Requested " +
                "FROM transactions WHERE " + where + " ORDER BY created_at DESC, id DESC", ps);
            if (dgvPending.Columns.Contains("id")) dgvPending.Columns["id"].Visible = false;
            SizeWorklistColumns();
            UpdateSummary();
        }

        /// <summary>
        /// The rail is 340px wide, so four equal columns gave every one of them ~73px —
        /// the header "Txn Code" wrapped onto two lines and every code rendered as
        /// "TXN-2026-...". A worklist row only has to let the officer RECOGNISE the
        /// request; the document type and everything else about it is in the workspace
        /// header the moment the row is clicked, so Type is dropped here rather than
        /// squeezed, and the remaining columns are weighted by how much each needs.
        /// </summary>
        private void SizeWorklistColumns()
        {
            var cols = dgvPending.Columns;
            if (cols.Contains("Type")) cols["Type"].Visible = false;
            if (cols.Contains("Txn Code")) { cols["Txn Code"].FillWeight = 34; cols["Txn Code"].HeaderText = "Transaction"; }
            if (cols.Contains("Client")) cols["Client"].FillWeight = 30;
            if (cols.Contains("Requested")) cols["Requested"].FillWeight = 22;
            // Waiting mode adds the parked queue ticket — the one thing a returning
            // client can actually quote at the counter, so it keeps real room.
            if (cols.Contains("Queue Ticket")) cols["Queue Ticket"].FillWeight = 20;
        }

        private void LoadReleased()
        {
            dgvReleased.DataSource = Db.Pull(
                "SELECT t.txn_code AS 'Txn Code', r.claimant_name AS Claimant, " +
                "IF(r.is_representative, 'Representative', 'Owner') AS 'Claimed By', " +
                "r.released_at AS 'Released At' " +
                "FROM releases r JOIN transactions t ON t.id = r.transaction_id ORDER BY r.id DESC");
        }

        private void dgvPending_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            SelectRow(dgvPending.Rows[e.RowIndex]);
        }

        private void ShowReleaseDetails(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvReleased.Rows.Count) return;
            var r = dgvReleased.Rows[rowIndex];
            MessageBox.Show(
                "Transaction:  " + r.Cells["Txn Code"].Value +
                "\r\nClaimant:  " + r.Cells["Claimant"].Value +
                "\r\nClaimed By:  " + r.Cells["Claimed By"].Value +
                "\r\nReleased At:  " + r.Cells["Released At"].Value,
                "Release details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateSummary()
        {
            try
            {
                // The counts ride ON the tabs and the history line. As separate tiles they
                // printed the same two numbers directly above the tabs that filter by them.
                if (_tabPending != null)
                    _tabPending.Text = "For Release  " +
                        Scalar("SELECT COUNT(*) FROM transactions WHERE status='ForRelease'");
                if (_tabWaiting != null)
                    _tabWaiting.Text = "Waiting  " +
                        Scalar("SELECT COUNT(*) FROM transactions WHERE status IN ('WaitingToRelease','ForPrint')");
                if (_btnHistory != null)
                    _btnHistory.Text = _historyOpen
                        ? "‹  Back to the worklist"
                        : "Released today · " +
                          Scalar("SELECT COUNT(*) FROM releases WHERE DATE(released_at)=CURDATE()") + "   ›";
            }
            catch { /* summary is best-effort */ }
        }

        private static string Scalar(string sql)
        {
            DataTable dt = Db.Pull(sql);
            return dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0][0]) : "0";
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            if (lblValidation != null) lblValidation.Text = "";

            // Pickup claim loaded from the queue (no transaction) → close the claim directly.
            if (_pickupClaimId != null && _selectedTxnId == null) { ReleasePickupClaim(); return; }

            if (_selectedTxnId == null)
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Select a pending release on the left first.";
                return;
            }

            // The button is whatever the state says it is, so it does whatever that state's
            // next step is. Re-read the status here rather than trusting the rendered state:
            // another window may have moved this request since it was selected.
            DataTable stt = Db.Pull("SELECT status FROM transactions WHERE id = @t LIMIT 1",
                new MySqlParameter("@t", _selectedTxnId.Value));
            string st = stt.Rows.Count > 0 ? stt.Rows[0]["status"].ToString() : "";

            if (st == "Released")
            {
                // Already handed over — show the release record instead of releasing twice.
                ShowHistory(true);
                if (lblValidation != null)
                    lblValidation.Text = "This transaction was already released — see the history.";
                return;
            }
            if (st == "ForPayment")
            {
                // Already at the cashier; just follow it there. Running ResumeSelected would
                // re-stamp a status it already has.
                MainForm shell = Shell();
                if (shell != null && shell.GoToModule("fees") is FeesPaymentsForm fp)
                    fp.PreselectTransaction(_selectedTxnId.Value);
                return;
            }
            // Parked / awaiting print — send it to the cashier (status → ForPayment).
            if (st != "ForRelease") { ResumeSelected(); return; }

            // Paid → verify the claimant, then hand over.
            if (string.IsNullOrWhiteSpace(txtClaimant.Text))
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Claimant name is required.";
                txtClaimant.Focus();
                return;
            }
            if (chkRep.Checked && string.IsNullOrWhiteSpace(txtIdNum.Text))
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Representative ID number is required.";
                txtIdNum.Focus();
                return;
            }

            // Verification step — open the identity-verification window showing the request
            // details, the kiosk face photo and the uploaded valid ID side by side. The admin
            // confirms the ID matches, then presses Release inside that window.
            string repInfo = chkRep.Checked
                ? (txtIdType.Text.Trim() + "  " + txtIdNum.Text.Trim()).Trim()
                : null;
            using (var v = new ReleaseVerifyDialog(_selectedTxnId.Value, txtClaimant.Text.Trim(), repInfo))
                if (v.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var photoParam = new MySqlParameter("@photo", MySqlDbType.LongBlob)
                {
                    Value = (object)_photoBytes ?? DBNull.Value
                };
                Db.Push(
                    "INSERT INTO releases (transaction_id, claimant_name, is_representative, " +
                    "representative_id_type, representative_id_number, claimant_photo, released_by) " +
                    "VALUES (@txn, @name, @rep, @idtype, @idnum, @photo, @by)",
                    new MySqlParameter("@txn", _selectedTxnId.Value),
                    new MySqlParameter("@name", txtClaimant.Text.Trim()),
                    new MySqlParameter("@rep", chkRep.Checked ? 1 : 0),
                    new MySqlParameter("@idtype", chkRep.Checked ? NullIfEmpty(txtIdType.Text) : DBNull.Value),
                    new MySqlParameter("@idnum", chkRep.Checked ? NullIfEmpty(txtIdNum.Text) : DBNull.Value),
                    photoParam,
                    new MySqlParameter("@by", Session.UserIdParam));

                // close the transaction + mark any linked cert request / claim released
                Db.Push("UPDATE transactions SET status = 'Released' WHERE id = @txn",
                    new MySqlParameter("@txn", _selectedTxnId.Value));
                Db.Push("UPDATE certificate_requests SET status = 'Released' WHERE transaction_id = @txn",
                    new MySqlParameter("@txn", _selectedTxnId.Value));
                Db.Push("UPDATE claim_requests SET status = 'Released', released_by = @by, " +
                        "released_at = NOW() WHERE transaction_id = @txn AND status <> 'Released'",
                    new MySqlParameter("@by", Session.UserIdParam),
                    new MySqlParameter("@txn", _selectedTxnId.Value));

                Audit.Write(Audit.Update, "transactions", _selectedTxnId.Value,
                    "Released to " + txtClaimant.Text.Trim());

                MessageBox.Show("Document released successfully.", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Transaction is saved; wipe the form for the next client.
                ClearClaimForm();
                LoadPending();
                LoadReleased();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not release: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Releases a kiosk pickup claim that has no transaction (mirrors the Claim Form).</summary>
        private void ReleasePickupClaim()
        {
            if (string.IsNullOrWhiteSpace(txtClaimant.Text))
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Claimant name is required.";
                txtClaimant.Focus();
                return;
            }
            if (chkRep.Checked && string.IsNullOrWhiteSpace(txtIdNum.Text))
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Representative ID number is required.";
                txtIdNum.Focus();
                return;
            }
            if (MessageBox.Show(
                    "Release claim " + _pickupClaimCode + " to " + txtClaimant.Text.Trim() + "?\r\n\r\n" +
                    "This closes the claim and cannot be undone.",
                    "Confirm release", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                string info = "Released to " + txtClaimant.Text.Trim() +
                    (chkRep.Checked ? " (rep: " + txtIdType.Text + " " + txtIdNum.Text + ")" : "");
                Db.Push(
                    "UPDATE claim_requests SET status = 'Released', release_info = @ri, " +
                    "released_by = @by, released_at = NOW() WHERE id = @id",
                    new MySqlParameter("@ri", info),
                    new MySqlParameter("@by", Session.UserIdParam),
                    new MySqlParameter("@id", _pickupClaimId.Value));
                Audit.Write(Audit.Update, "claim_requests", _pickupClaimId.Value,
                    "Released claim " + _pickupClaimCode + " to " + txtClaimant.Text.Trim());

                MessageBox.Show("Released.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearClaimForm();
                LoadPending();
                LoadReleased();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not release: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Resets every claim field + the camera to prepare for the next client. Only the
        /// on-screen form is cleared — the released transaction stays saved in the database.
        /// </summary>
        private void ClearClaimForm()
        {
            _selectedTxnId = null;
            _pickupClaimId = null;
            _pickupClaimCode = null;
            lblSelected.Text = "—";
            if (_wMeta != null) _wMeta.Text = "";
            SetSummary();
            SetNote("", UiTheme.AccentTint, Accent);
            if (lblValidation != null) lblValidation.Text = "";
            txtClaimant.Clear();
            chkRep.Checked = false;
            txtIdType.SelectedIndex = -1;
            txtIdType.Text = "";
            txtIdNum.Clear();
            if (picClient != null) { picClient.Image?.Dispose(); picClient.Image = null; }
            if (picUploadedId != null) { picUploadedId.Image?.Dispose(); picUploadedId.Image = null; }
            if (lblUploadedIdCap != null) lblUploadedIdCap.Text = "Uploaded ID (from claimapp)";
            tglUseCam.Checked = false;   // fires ApplyCameraOption → hides + releases the camera
            ResetCamera();
            SetFaceState(lblPhotoState, false, null, "No kiosk photo");
            SetFaceState(lblIdState, false, null, "No uploaded ID");
            UpdateChecklist();
            ApplyState(null);   // back to the empty state, not a live form for no request
        }

        private static object NullIfEmpty(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
    }

    /// <summary>A white rounded-corner card with a soft shadow and a title bar.</summary>
    internal class Card : Panel
    {
        private const int Radius = 12;
        private const int Shadow = 6;
        private readonly Label _title;

        /// <summary>Put your content here (already padded).</summary>
        public Panel Content { get; }

        /// <param name="step">
        /// 1-based position in the release workflow. Draws a filled accent badge before the
        /// title so the three columns read as an order instead of three unrelated panels.
        /// 0 = no badge (a supporting card such as Recent Releases).
        /// </param>
        /// <remarks>
        /// A NULL title draws no title bar at all. The workspace card carries its own header
        /// (status pill, client name, transaction line), so a card title above it would just
        /// name the panel a second time and cost 44px doing it.
        /// </remarks>
        public Card(string title, int step = 0)
        {
            DoubleBuffered = true;
            BackColor = UiTheme.PageBg;   // page bg shows in the shadow gap
            Padding = new Padding(2, 2, 2 + Shadow, 2 + Shadow);

            Content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(14, 6, 14, 12) };

            var head = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 44, WrapContents = false, AutoSize = false,
                BackColor = UiTheme.Surface, Padding = new Padding(14, 10, 0, 0)
            };
            if (step > 0)
                head.Controls.Add(new StatusPill
                {
                    Text = step.ToString(),
                    BackColor = UiTheme.Accent,
                    ForeColor = UiTheme.Surface,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Inset = new Padding(10, 5, 10, 5),
                    Margin = new Padding(0, 1, 10, 0)
                });
            _title = new Label
            {
                AutoSize = true, Text = title, BackColor = Color.Transparent,
                ForeColor = UiTheme.Ink,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                // A Label eats "&" as a mnemonic prefix, which is why this screen's own
                // heading rendered as "Release  Claim".
                UseMnemonic = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            head.Controls.Add(_title);

            Controls.Add(Content);
            if (title != null) Controls.Add(head);
        }

        private static GraphicsPath RoundPath(Rectangle r, int rad)
        {
            int d = rad * 2;
            var p = new GraphicsPath();
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - Shadow - 1, Height - Shadow - 1);
            if (rect.Width < 4 || rect.Height < 4) { base.OnPaint(e); return; }

            for (int i = Shadow; i >= 1; i--)
            {
                var sr = new Rectangle(rect.X + i / 2, rect.Y + i, rect.Width, rect.Height);
                int a = Math.Max(3, 20 - i * 3);
                using (var sp = RoundPath(sr, Radius))
                using (var sb = new SolidBrush(Color.FromArgb(a, 0, 0, 0)))
                    g.FillPath(sb, sp);
            }
            using (var path = RoundPath(rect, Radius))
            {
                using (var b = new SolidBrush(UiTheme.Surface)) g.FillPath(b, path);
                using (var pen = new Pen(UiTheme.CardLine, 1f)) g.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }

    /// <summary>
    /// Identity-verification window shown before a paid document is released. Displays the
    /// request details, the kiosk face photo and the claimant's uploaded valid ID side by
    /// side so the officer can confirm the ID matches the claimant, then release. Returns
    /// <see cref="DialogResult.OK"/> when the officer presses Release (the caller performs
    /// the actual release). Purely a review screen — it never writes to the database.
    /// </summary>
    internal sealed class ReleaseVerifyDialog : Form
    {
        private static readonly Color Ink = UiTheme.Ink;
        private static readonly Color Muted = UiTheme.Muted;
        private static readonly Color Green = UiTheme.Success;

        public ReleaseVerifyDialog(long txnId, string claimant, string repInfo)
        {
            Text = "Identity Verification";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            ClientSize = new Size(880, 560);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            // ---- lookup transaction + linked details
            string txnCode = "—", client = "—", type = "—", idName = "";
            DataTable t = Db.Pull(
                "SELECT txn_code, client_name, type FROM transactions WHERE id = @t LIMIT 1",
                new MySqlParameter("@t", txnId));
            if (t.Rows.Count > 0)
            {
                txnCode = Str(t.Rows[0]["txn_code"]);
                client = Str(t.Rows[0]["client_name"]);
                type = Str(t.Rows[0]["type"]);
            }

            Image clientPhoto = LoadImage(
                "SELECT id_image FROM queue_tickets WHERE transaction_id = @t " +
                "AND id_image IS NOT NULL ORDER BY id DESC LIMIT 1", txnId);

            Image uploadedId = null;
            DataTable c = Db.Pull(
                "SELECT id_image, id_first_name, id_middle_name, id_last_name " +
                "FROM claim_requests WHERE transaction_id = @t ORDER BY id DESC LIMIT 1",
                new MySqlParameter("@t", txnId));
            if (c.Rows.Count > 0)
            {
                idName = JoinName(c.Rows[0]["id_first_name"], c.Rows[0]["id_middle_name"], c.Rows[0]["id_last_name"]);
                if (c.Rows[0]["id_image"] != DBNull.Value)
                    try { using (var ms = new MemoryStream((byte[])c.Rows[0]["id_image"])) uploadedId = Image.FromStream(ms); }
                    catch { }
            }

            // ---- header
            Controls.Add(new Label
            {
                Text = "Identity Verification", Location = new Point(24, 18), AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Ink
            });
            Controls.Add(new Label
            {
                Text = "Check that the uploaded ID matches the claimant, then release the document.",
                Location = new Point(26, 52), AutoSize = true, ForeColor = Muted
            });

            // ---- left: details
            var details = new TableLayoutPanel
            {
                Location = new Point(24, 96), Size = new Size(360, 400),
                ColumnCount = 1, BackColor = UiTheme.PageBg
            };
            details.Controls.Add(Field("Transaction", txnCode));
            details.Controls.Add(Field("Type", type));
            details.Controls.Add(Field("Client (record owner)", client));
            details.Controls.Add(Field("Claimant", string.IsNullOrWhiteSpace(claimant) ? "—" : claimant));
            details.Controls.Add(Field("Representative", string.IsNullOrWhiteSpace(repInfo) ? "Owner (not a representative)" : repInfo));
            details.Controls.Add(Field("Name on uploaded ID", idName.Length > 0 ? idName : "— (no ID uploaded)"));
            Controls.Add(details);

            // ---- right: photos
            Controls.Add(PhotoBox("CLIENT PHOTO (from kiosk)", clientPhoto, 404, 96, "No kiosk photo on file."));
            Controls.Add(PhotoBox("UPLOADED VALID ID (from claimapp)", uploadedId, 404, 300, "No ID uploaded yet."));

            // ---- buttons
            var release = new Button
            {
                Text = "✔  Release Document", Location = new Point(596, 504), Size = new Size(258, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Green, ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            release.FlatAppearance.BorderSize = 0;
            release.FlatAppearance.MouseOverBackColor = UiTheme.Mix(Green, Color.Black, 0.18f);
            var cancel = new Button
            {
                Text = "Cancel", Location = new Point(486, 504), Size = new Size(100, 44),
                FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Chrome, ForeColor = Ink,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            cancel.FlatAppearance.BorderSize = 0;
            Controls.Add(release); Controls.Add(cancel);
            AcceptButton = release; CancelButton = cancel;

            FormClosed += (s, e) => { clientPhoto?.Dispose(); uploadedId?.Dispose(); };
        }

        private static Control Field(string label, string value)
        {
            var p = new Panel { Width = 350, Height = 58, Margin = new Padding(6, 6, 6, 0) };
            p.Controls.Add(new Label
            {
                Text = label, Location = new Point(4, 4), AutoSize = true,
                Font = new Font("Segoe UI", 8.5F), ForeColor = Muted
            });
            p.Controls.Add(new Label
            {
                Text = value, Location = new Point(4, 24), Size = new Size(340, 30),
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Ink,
                AutoEllipsis = true
            });
            return p;
        }

        private static Control PhotoBox(string caption, Image img, int x, int y, string emptyText)
        {
            var host = new Panel { Location = new Point(x, y), Size = new Size(448, 186) };
            host.Controls.Add(new Label
            {
                Text = caption, Location = new Point(0, 0), AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Muted
            });
            var pic = new PictureBox
            {
                Location = new Point(0, 22), Size = new Size(448, 160),
                BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.PageBg,
                SizeMode = PictureBoxSizeMode.Zoom, Image = img
            };
            host.Controls.Add(pic);
            if (img == null)
                host.Controls.Add(new Label
                {
                    Text = emptyText, Location = new Point(0, 90), Size = new Size(448, 24),
                    TextAlign = ContentAlignment.MiddleCenter, ForeColor = Muted, BackColor = Color.Transparent
                });
            return host;
        }

        private static Image LoadImage(string sql, long txnId)
        {
            try
            {
                DataTable dt = Db.Pull(sql, new MySqlParameter("@t", txnId));
                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                    using (var ms = new MemoryStream((byte[])dt.Rows[0][0])) return Image.FromStream(ms);
            }
            catch { }
            return null;
        }

        private static string JoinName(object f, object m, object l)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (f != DBNull.Value && f != null && f.ToString().Length > 0) parts.Add(f.ToString());
            if (m != DBNull.Value && m != null && m.ToString().Length > 0) parts.Add(m.ToString());
            if (l != DBNull.Value && l != null && l.ToString().Length > 0) parts.Add(l.ToString());
            return string.Join(" ", parts);
        }

        private static string Str(object v) =>
            v == null || v == DBNull.Value || v.ToString().Length == 0 ? "—" : v.ToString();
    }
}
