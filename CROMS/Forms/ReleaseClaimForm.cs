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

        // responsive-redesign controls (built in BuildResponsiveLayout)
        private TextBox txtSearch;
        private Label lblTilePending, lblTileToday, lblTileWaiting;
        private Label lblValidation;    // validation message under the claim fields
        private Label lblClaimStatus;   // claim/verification status in the right card
        private Panel _camPanel;        // whole camera block (one collapsible row)

        // Left-list mode: 0 = For Release (ready), 1 = Waiting to Release (parked).
        private int _listMode;
        private Button _btnResume, _tabPending, _tabWaiting;

        // Palette
        private static readonly Color Bg      = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg  = Color.White;
        private static readonly Color Accent  = Color.FromArgb(13, 110, 253);
        private static readonly Color Green    = Color.FromArgb(25, 135, 84);
        private static readonly Color Ink     = Color.FromArgb(33, 37, 41);
        private static readonly Color Muted   = Color.FromArgb(108, 117, 125);
        private static readonly Color Line    = Color.FromArgb(222, 226, 230);

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
            this.Disposed += (s, e) => StopCamera();

            // Camera is optional per release — default to Off so nothing is initialized
            // until the officer flips the toggle on.
            tglUseCam.SetCheckedSilently(false);
            ApplyCameraOption();             // hide the camera panel to match the Off state

            LoadPending();
            LoadReleased();
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
                ColumnCount = 3,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            // --- title row (spans all 3 columns) ---
            var titleBar = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            titleBar.Controls.Add(new Label
            {
                Text = "Release & Claim", AutoSize = true, ForeColor = Ink,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold), Location = new Point(2, 0)
            });
            titleBar.Controls.Add(new Label
            {
                Text = "Release documents · verify the claimant · capture proof",
                AutoSize = true, ForeColor = Muted,
                Font = new Font("Segoe UI", 9F), Location = new Point(4, 36)
            });
            root.Controls.Add(titleBar, 0, 0);
            root.SetColumnSpan(titleBar, 3);

            root.Controls.Add(BuildLeftCard(), 0, 1);
            root.Controls.Add(BuildCenterColumn(), 1, 1);
            root.Controls.Add(BuildRightCard(), 2, 1);

            Controls.Add(root);
            ResumeLayout(true);
        }

        // ---- LEFT: summary tiles + tabs + search + pending grid ----
        private Control BuildLeftCard()
        {
            var card = new Card("Releases") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };

            var tiles = new TableLayoutPanel
            {
                Dock = DockStyle.Top, Height = 78, ColumnCount = 3, RowCount = 1, BackColor = CardBg
            };
            for (int i = 0; i < 3; i++) tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            tiles.Controls.Add(Tile("FOR RELEASE", out lblTilePending, Accent), 0, 0);
            tiles.Controls.Add(Tile("WAITING", out lblTileWaiting, Color.FromArgb(214, 137, 16)), 1, 0);
            tiles.Controls.Add(Tile("RELEASED TODAY", out lblTileToday, Green), 2, 0);

            // Two tabs: For Release (ready to hand over) / Waiting to Release (parked).
            var tabRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false, BackColor = CardBg };
            _tabPending = MakeTab("For Release", true);
            _tabWaiting = MakeTab("Waiting to Release", false);
            _tabPending.Click += (s, e) => SetListMode(0);
            _tabWaiting.Click += (s, e) => SetListMode(1);
            tabRow.Controls.Add(_tabPending);
            tabRow.Controls.Add(_tabWaiting);

            var search = BuildSearchBar();

            dgvPending.Dock = DockStyle.Fill;
            dgvPending.Margin = new Padding(0);
            StyleGrid(dgvPending);

            // Resume button — only for the Waiting tab. Sends a parked request to payment.
            _btnResume = new Button
            {
                Text = "➡  Resume — Send to Payment",
                Dock = DockStyle.Bottom, Height = 46, Visible = false,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(214, 137, 16),
                ForeColor = Color.White, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 6, 0, 0)
            };
            _btnResume.FlatAppearance.BorderSize = 0;
            _btnResume.Click += (s, e) => ResumeSelected();

            card.Content.Controls.Add(dgvPending);   // fill (add first)
            card.Content.Controls.Add(_btnResume);   // bottom
            card.Content.Controls.Add(search);       // top
            card.Content.Controls.Add(tabRow);       // top
            card.Content.Controls.Add(tiles);        // top (outermost = highest)
            return card;
        }

        private Button MakeTab(string text, bool active) => new Button
        {
            Text = text, AutoSize = false, Width = 168, Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = active ? Accent : Color.FromArgb(233, 236, 239),
            ForeColor = active ? Color.White : Ink,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand, Margin = new Padding(0, 4, 8, 4)
        };

        /// <summary>Switches the left list between For-Release and Waiting-to-Release.</summary>
        private void SetListMode(int mode)
        {
            _listMode = mode;
            bool waiting = mode == 1;
            _tabPending.BackColor = waiting ? Color.FromArgb(233, 236, 239) : Accent;
            _tabPending.ForeColor = waiting ? Ink : Color.White;
            _tabWaiting.BackColor = waiting ? Color.FromArgb(214, 137, 16) : Color.FromArgb(233, 236, 239);
            _tabWaiting.ForeColor = waiting ? Color.White : Ink;
            _btnResume.Visible = waiting;
            _selectedTxnId = null;
            if (lblValidation != null) lblValidation.Text = "";
            LoadPending();
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

        private Panel BuildSearchBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = CardBg, Padding = new Padding(0, 8, 0, 8) };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Ink
            };
            SetCue(txtSearch, "Search transaction no. or name…");
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); } };

            var btnSearch = MakeMiniButton("Search", Accent, Color.White);
            btnSearch.Click += (s, e) => DoSearch();
            var btnClear = MakeMiniButton("Clear", Color.FromArgb(233, 236, 239), Ink);
            btnClear.Click += (s, e) => { txtSearch.Text = ""; LoadPending(); lblValidation.Text = ""; };

            // right-docked buttons first (so Fill textbox takes the rest)
            bar.Controls.Add(txtSearch);
            bar.Controls.Add(btnSearch);
            bar.Controls.Add(btnClear);
            btnSearch.Dock = DockStyle.Right;
            btnClear.Dock = DockStyle.Right;
            var pad = new Panel { Dock = DockStyle.Right, Width = 6, BackColor = CardBg };
            bar.Controls.Add(pad);
            bar.Controls.SetChildIndex(txtSearch, 0);
            return bar;
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

        // ---- CENTER: claim details card + recent releases card ----
        private Control BuildCenterColumn()
        {
            var col = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                BackColor = Bg, Margin = new Padding(8, 0, 8, 0)
            };
            col.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            col.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            col.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            // -- Claim Details --
            var claim = new Card("Claim Details") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };

            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = CardBg,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows, AutoScroll = true
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            lblSelected.AutoSize = false;
            lblSelected.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            AddRowSpan(fields, lblSelected, 30);

            txtClaimant.Font = new Font("Segoe UI", 11F);
            txtIdType.Font = new Font("Segoe UI", 11F);
            txtIdNum.Font = new Font("Segoe UI", 11F);
            AddFieldRow(fields, lblClaimant, txtClaimant);
            AddRowSpan(fields, chkRep, 30);
            AddFieldRow(fields, lblIdType, txtIdType);
            AddFieldRow(fields, lblIdNum, txtIdNum);

            lblValidation = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false, ForeColor = Color.FromArgb(200, 35, 51),
                Font = new Font("Segoe UI", 9F), Text = ""
            };
            AddRowSpan(fields, lblValidation, 36);

            // Release button — full-width, prominent green, docked at the card bottom.
            btnRelease.Dock = DockStyle.Bottom;
            btnRelease.Height = 56;
            btnRelease.Text = "✔  Release Document";
            btnRelease.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnRelease.FlatStyle = FlatStyle.Flat;
            btnRelease.BackColor = Green;
            btnRelease.ForeColor = Color.White;
            btnRelease.Cursor = Cursors.Hand;
            btnRelease.FlatAppearance.BorderSize = 0;
            btnRelease.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 108, 67);
            btnRelease.Margin = new Padding(0, 8, 0, 0);

            claim.Content.Controls.Add(btnRelease);   // bottom
            claim.Content.Controls.Add(fields);       // fill (add last)
            col.Controls.Add(claim, 0, 0);

            // -- Recent Releases --
            var recent = new Card("Recent Releases") { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
            dgvReleased.Dock = DockStyle.Fill;
            dgvReleased.Margin = new Padding(0);
            StyleGrid(dgvReleased);
            dgvReleased.CellDoubleClick += (s, e) => ShowReleaseDetails(e.RowIndex);
            recent.Content.Controls.Add(dgvReleased);
            col.Controls.Add(recent, 0, 1);

            return col;
        }

        // ---- RIGHT: QR claim, status, photo, optional camera ----
        private Control BuildRightCard()
        {
            var card = new Card("Claim & Verification") { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, BackColor = CardBg,
                AutoScroll = true, GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var qr = new Button
            {
                Text = "🔎  Claim by QR", Dock = DockStyle.Fill, Height = 48,
                FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            qr.FlatAppearance.BorderSize = 0;
            qr.FlatAppearance.MouseOverBackColor = Color.FromArgb(11, 94, 215);
            qr.Click += (s, e) =>
            {
                using (var dlg = new ClaimFormForm()) dlg.ShowDialog(this);
                LoadReleased(); UpdateSummary();
            };
            AddStack(stack, qr, 52);

            lblClaimStatus = new Label
            {
                Text = "No release selected.", Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 9.5F), ForeColor = Muted
            };
            AddStack(stack, lblClaimStatus, 42);

            lblPhotoCap = new Label
            {
                Text = "Client Photo (from kiosk)", Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(73, 80, 87)
            };
            AddStack(stack, lblPhotoCap, 22);
            picClient = new PictureBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(248, 249, 250)
            };
            AddStack(stack, picClient, 230);

            // Uploaded ID photo (from claimapp) — the officer compares this face against
            // the kiosk photo above / the person in front of them, then releases.
            lblUploadedIdCap = new Label
            {
                Text = "Uploaded ID (from claimapp)", Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(73, 80, 87)
            };
            AddStack(stack, lblUploadedIdCap, 22);
            picUploadedId = new PictureBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(248, 249, 250)
            };
            AddStack(stack, picUploadedId, 230);

            // "Use Camera?" toggle — always visible (outside the collapsible block).
            var toggleRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 34, WrapContents = false, BackColor = CardBg };
            lblUseCam.AutoSize = true; lblUseCam.Margin = new Padding(0, 8, 6, 0);
            tglUseCam.Margin = new Padding(0, 2, 6, 0);
            lblUseCamState.AutoSize = true; lblUseCamState.Margin = new Padding(0, 8, 0, 0);
            toggleRow.Controls.Add(lblUseCam);
            toggleRow.Controls.Add(tglUseCam);
            toggleRow.Controls.Add(lblUseCamState);
            AddStack(stack, toggleRow, 38);

            // Camera preview block — collapses to zero height when the camera is Off.
            _camPanel = BuildCameraPanel();
            AddStackAuto(stack, _camPanel);

            card.Content.Controls.Add(stack);
            return card;
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

        private Panel Tile(string caption, out Label value, Color accent)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252), Margin = new Padding(4) };
            var stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
            value = new Label
            {
                Text = "0", Dock = DockStyle.Top, Height = 34, ForeColor = Ink,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 4, 0, 0)
            };
            var cap = new Label
            {
                Text = caption, Dock = DockStyle.Top, Height = 22, ForeColor = Muted,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            p.Controls.Add(cap);
            p.Controls.Add(value);
            p.Controls.Add(stripe);
            return p;
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
            b.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
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
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(73, 80, 87);
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(13, 71, 161);
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
                ? System.Drawing.Color.FromArgb(25, 135, 84)         // green when On
                : System.Drawing.Color.FromArgb(108, 117, 125);      // grey when Off

            if (!use) ResetCamera();            // stop capture + drop any pending photo

            // Collapse the whole preview block as a unit (no leftover gap when Off).
            if (_camPanel != null) _camPanel.Visible = use;

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

            ShowUploadedIdFor(txnId);
        }

        /// <summary>Loads the ID image + OCR'd name uploaded via claimapp for this transaction.</summary>
        private void ShowUploadedIdFor(int txnId)
        {
            picUploadedId.Image?.Dispose();
            picUploadedId.Image = null;
            lblUploadedIdCap.Text = "Uploaded ID (from claimapp)";
            DataTable dt = Db.Pull(
                "SELECT id_image, id_first_name, id_middle_name, id_last_name " +
                "FROM claim_requests WHERE transaction_id = " + txnId +
                " ORDER BY id DESC LIMIT 1");
            if (dt.Rows.Count == 0) { lblUploadedIdCap.Text = "Uploaded ID — none (no claimapp upload)"; return; }

            DataRow r = dt.Rows[0];
            string idName = JoinName(r["id_first_name"], r["id_middle_name"], r["id_last_name"]);
            if (r["id_image"] == DBNull.Value)
                lblUploadedIdCap.Text = "Uploaded ID — not yet uploaded";
            else
            {
                try
                {
                    using (var ms = new MemoryStream((byte[])r["id_image"]))
                        picUploadedId.Image = Image.FromStream(ms);
                }
                catch { /* stored value wasn't a readable image */ }
                lblUploadedIdCap.Text = idName.Length > 0
                    ? "Uploaded ID — name on ID: " + idName
                    : "Uploaded ID (from claimapp)";
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

        /// <summary>Selects a pending row: fills the claim header, status, and kiosk photo.</summary>
        private void SelectRow(DataGridViewRow row)
        {
            _selectedTxnId = Convert.ToInt32(row.Cells["id"].Value);
            string code = Convert.ToString(row.Cells["Txn Code"].Value);
            string client = Convert.ToString(row.Cells["Client"].Value);
            lblSelected.Text = "Releasing:  " + code + "  —  " + client;
            if (lblValidation != null) lblValidation.Text = "";
            if (lblClaimStatus != null)
                lblClaimStatus.Text = "Selected: " + code + "\r\nClient: " + client +
                    "\r\nVerify the claimant, then Release.";
            ShowPhotoFor(_selectedTxnId.Value);
            txtClaimant.Focus();
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
            UpdateSummary();
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
                lblTilePending.Text = Scalar("SELECT COUNT(*) FROM transactions WHERE status='ForRelease'");
                lblTileWaiting.Text = Scalar("SELECT COUNT(*) FROM transactions WHERE status IN ('WaitingToRelease','ForPrint')");
                lblTileToday.Text = Scalar("SELECT COUNT(*) FROM releases WHERE DATE(released_at)=CURDATE()");
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
            if (_selectedTxnId == null)
            {
                if (lblValidation != null) lblValidation.Text = "⚠ Select a pending release on the left first.";
                return;
            }
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

            // Only a paid, For-Release request can be handed over. A parked / awaiting-print
            // one must first be resumed to payment (Waiting tab → Resume).
            DataTable stt = Db.Pull("SELECT status FROM transactions WHERE id = @t LIMIT 1",
                new MySqlParameter("@t", _selectedTxnId.Value));
            string st = stt.Rows.Count > 0 ? stt.Rows[0]["status"].ToString() : "";
            if (st != "ForRelease")
            {
                if (lblValidation != null)
                    lblValidation.Text = "⚠ This request is not paid yet (status: " + st +
                        "). Open the Waiting-to-Release tab and Resume it to payment first.";
                return;
            }

            if (MessageBox.Show(
                    "Release this document to " + txtClaimant.Text.Trim() + "?\r\n\r\n" +
                    "This closes the transaction and cannot be undone.",
                    "Confirm release", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

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

                // close the transaction + mark any linked cert request released
                Db.Push("UPDATE transactions SET status = 'Released' WHERE id = @txn",
                    new MySqlParameter("@txn", _selectedTxnId.Value));
                Db.Push("UPDATE certificate_requests SET status = 'Released' WHERE transaction_id = @txn",
                    new MySqlParameter("@txn", _selectedTxnId.Value));

                Audit.Write(Audit.Update, "transactions", _selectedTxnId.Value,
                    "Released to " + txtClaimant.Text.Trim());

                MessageBox.Show("Released and closed.", "Done",
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

        /// <summary>
        /// Resets every claim field + the camera to prepare for the next client. Only the
        /// on-screen form is cleared — the released transaction stays saved in the database.
        /// </summary>
        private void ClearClaimForm()
        {
            _selectedTxnId = null;
            lblSelected.Text = "Select a pending release on the left →";
            if (lblValidation != null) lblValidation.Text = "";
            if (lblClaimStatus != null) lblClaimStatus.Text = "No release selected.";
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

        public Card(string title)
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(245, 247, 250);   // page bg shows in the shadow gap
            Padding = new Padding(2, 2, 2 + Shadow, 2 + Shadow);

            Content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 6, 14, 12) };
            _title = new Label
            {
                Dock = DockStyle.Top, Height = 36, Text = title, BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                Padding = new Padding(14, 9, 0, 0)
            };
            Controls.Add(Content);
            Controls.Add(_title);
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
                using (var b = new SolidBrush(Color.White)) g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(230, 233, 237), 1f)) g.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }
}
