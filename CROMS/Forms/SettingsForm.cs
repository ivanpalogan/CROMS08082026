using System;
using System.Collections.Generic;
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
    /// Settings module (Administration). Two tabs:
    ///   • Window Management — dynamic add / edit / enable / disable / reorder / delete
    ///     of service windows. Everything is driven from the `windows` table, so a new
    ///     window appears in the Login window picker, the Dashboard status panel, the
    ///     Now Serving board and the queue routing with no code change or restart.
    ///   • User Manual — a searchable, built-in guide to the whole system.
    /// The module is gated to Admin / Registrar in MainForm.AllowedKeys.
    /// <para/>
    /// UI layout lives in SettingsForm.Designer.cs; this file holds only the logic
    /// (event handlers, validation, database operations, manual content).
    /// </summary>
    public partial class SettingsForm : Form, IRefreshable
    {
        /// <summary>Minutes without a heartbeat before a window is treated as Offline.</summary>
        private const int StaleMinutes = WindowAssignmentForm.StaleMinutes;

        private readonly List<ManualTopic> _topics;
        private List<ManualTopic> _shown = new List<ManualTopic>();

        // Window Management stays LOCKED until an Admin/Registrar re-authenticates via
        // AdminVerificationForm. Valid only for this session (the form is recreated on
        // logout/restart, so verification is required again next time).
        private bool _verified;
        private CurrentUser _verifiedUser;
        private bool _promptedOnce;

        public SettingsForm()
        {
            InitializeComponent();
            _topics = BuildManual();
            dgvWindows.CellContentClick += dgvWindows_CellContentClick;
            LoadWindows();
            SetLocked(true);
            FilterManual("");
            BuildFormsTab();
            BuildUpdatesTab();
        }

        // =====================================================================
        //  Certificates & Forms tab — the office's logo and stamp, its own details,
        //  and what CROMS knows about each certificate form: which report lays it
        //  out and whether that report has been authored yet.
        // =====================================================================

        private void BuildFormsTab()
        {
            var tab = new TabPage("Certificates & Forms")
            { BackColor = Color.White, Padding = new Padding(3) };

            tab.Controls.Add(new Label
            {
                Text = "Certificates & Forms", AutoSize = true, Location = new Point(20, 18),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            });
            tab.Controls.Add(new Label
            {
                Text = "Branding and office details printed on every certificate, and the " +
                       "report layout used for each form.",
                AutoSize = true, Location = new Point(22, 50),
                Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            tab.Controls.Add(Section("Logo, stamp and office details:", 22, 88));
            var btnBrand = BigButton("Manage Logo, Stamp and Office Details", 22, 114,
                                     Color.FromArgb(13, 110, 253));
            btnBrand.Click += btnBrand_Click;
            tab.Controls.Add(btnBrand);
            tab.Controls.Add(new Label
            {
                Text = "The logo prints in the certificate header; the stamp prints in the " +
                       "position each form reserves for it.\r\n" +
                       "They are managed separately, and either can be set for one form only.",
                AutoSize = true, Location = new Point(24, 166),
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            var btnAlign = BigButton("Align Printing on Pre-printed Forms", 322, 114,
                                     Color.FromArgb(108, 117, 125));
            btnAlign.Click += btnAlign_Click;
            tab.Controls.Add(btnAlign);

            tab.Controls.Add(Section("Forms CROMS can read, store and print:", 22, 214));

            // A read-only picture of the form library. The point is the Report column:
            // it is the only place that tells the office whether a .rpt has actually been
            // authored for a form, or whether it is still printing on the built-in
            // renderer — which is otherwise invisible until someone prints one.
            var grid = new DataGridView
            {
                Location = new Point(22, 240),
                Size = new Size(900, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                       | AnchorStyles.Bottom,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Form", "Form Name");
            grid.Columns.Add("No", "Municipal Form No.");
            grid.Columns.Add("Rev", "Revision");
            grid.Columns.Add("Code", "Form Code");
            grid.Columns.Add("View", "Report Datasource");
            grid.Columns.Add("Report", "Report Layout");
            grid.Columns["No"].FillWeight = 55;
            grid.Columns["Code"].FillWeight = 60;

            foreach (FormDefinition d in FormCatalog.All)
            {
                string report = CertificateReport.ReportPath(d) != null
                    ? (CertificateReport.CrystalAvailable
                        ? "Crystal — " + d.RptFile
                        : d.RptFile + " (Crystal runtime not installed on this PC)")
                    : d.HasBlankForm && d.HasOverlay
                        ? "Built-in replica of the blank form"
                        : d.HasOverlay
                            ? "Positioned for the pre-printed form"
                              + (PrintCalibration.IsCalibrated(d.FormCode)
                                    ? " (aligned on this PC)" : " (not aligned yet)")
                            : "Built-in structured layout";
                grid.Rows.Add(d.FormName, "No. " + d.MunicipalFormNo, d.Revision,
                              d.FormCode, d.ReportView, report);
            }
            tab.Controls.Add(grid);

            tab.Controls.Add(new Label
            {
                Text = "To use a Crystal Reports layout for a form, put its .rpt file in the " +
                       "Reports folder beside CROMS.exe using the Form Code as its name, and " +
                       "bind it to that form's Report Datasource. Until then CROMS prints the " +
                       "certificate itself.",
                AutoSize = false, Location = new Point(24, 496), Size = new Size(898, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            tabs.TabPages.Add(tab);
        }

        /// <summary>
        /// Branding changes what every issued certificate looks like, so it is behind the
        /// same admin re-verification as the window configuration on this screen.
        /// </summary>
        private void btnBrand_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            using (var dlg = new OfficeAssetsForm()) dlg.ShowDialog(this);
        }

        /// <summary>
        /// Where a form's values land on the office's own pre-printed stock. Behind the same
        /// re-verification as branding: a bad alignment prints onto accountable forms.
        /// </summary>
        private void btnAlign_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            using (var dlg = new FormAlignForm()) dlg.ShowDialog(this);
            LoadWindows();
        }

        public void RefreshData() => LoadWindows();

        // =====================================================================
        //  App Updates tab — publish a new release to clients (server) + one-click
        //  update this PC (client). The server address shown auto-refreshes when the
        //  Wi-Fi/hotspot changes, so the update share path is always current.
        // =====================================================================
        private Label _lblServerIp;
        private Timer _ipTimer;

        private void BuildUpdatesTab()
        {
            var tab = new TabPage("App Updates") { BackColor = Color.White, Padding = new Padding(3) };

            tab.Controls.Add(new Label
            {
                Text = "App Updates", AutoSize = true, Location = new Point(20, 18),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
            });
            tab.Controls.Add(new Label
            {
                Text = "Publish a new build to the client PCs on this Wi-Fi, or update this PC.",
                AutoSize = true, Location = new Point(22, 50), Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(108, 117, 125)
            });

            // Live server address / share path (auto-updates on Wi-Fi change).
            _lblServerIp = new Label
            {
                AutoSize = true, Location = new Point(22, 92), Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 110, 253)
            };
            tab.Controls.Add(_lblServerIp);

            // --- Publish (server side) ---
            tab.Controls.Add(Section("For the SERVER PC (where the database runs):", 22, 132));
            var btnPublish = BigButton("⬆  Publish New Release to Clients", 22, 158, Color.FromArgb(25, 135, 84));
            btnPublish.Click += (s, e) => PublishRelease();
            tab.Controls.Add(btnPublish);
            tab.Controls.Add(new Label
            {
                Text = "Run this ONCE. It links the client share to your build folders, so every later\r\n" +
                       "rebuild in Visual Studio reaches the clients on its own. Publish again only if\r\n" +
                       "the build folders move. (Needs an admin prompt.)",
                AutoSize = true, Location = new Point(24, 210), Font = new Font("Segoe UI", 8.75F),
                ForeColor = Color.FromArgb(108, 117, 125)
            });

            // --- Update (client side) ---
            tab.Controls.Add(Section("For a CLIENT PC:", 22, 268));
            var btnUpdateNow = BigButton("⟳  Check for Updates && Install", 22, 294, Color.FromArgb(13, 110, 253));
            btnUpdateNow.Click += (s, e) => UpdateThisPc();
            tab.Controls.Add(btnUpdateNow);
            tab.Controls.Add(new Label
            {
                Text = "Pulls the latest build from the server share and restarts. Your connection settings\r\n" +
                       "and sign-in are kept. (On the server PC itself there is nothing to pull.)",
                AutoSize = true, Location = new Point(24, 346), Font = new Font("Segoe UI", 8.75F),
                ForeColor = Color.FromArgb(108, 117, 125)
            });

            tabs.TabPages.Add(tab);

            RefreshServerIp();
            _ipTimer = new Timer { Interval = 5000 };   // follow Wi-Fi/hotspot changes live
            _ipTimer.Tick += (s, e) => RefreshServerIp();
            _ipTimer.Start();
        }

        private void RefreshServerIp()
        {
            if (_lblServerIp == null) return;
            var lan = IonicServerManager.DetectLan();
            _lblServerIp.Text = "This PC: " + lan.ip + " (" + lan.type + ")     Update share:  " +
                                @"\\" + lan.ip + @"\CROMSRelease";
        }

        private void PublishRelease()
        {
            using (var v = new AdminVerificationForm())
                if (v.ShowDialog(this) != DialogResult.OK) return;

            Cursor = Cursors.WaitCursor;
            bool ok = ReleasePublisher.Publish(out string msg);
            Cursor = Cursors.Default;
            if (ok) Audit.Write("Update", "release", 0, "Published new release to CROMSRelease share");
            MessageBox.Show(msg, "Publish Release",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void UpdateThisPc()
        {
            bool available = AppUpdater.IsUpdateAvailable(out string detail);
            if (!available)
            {
                MessageBox.Show(detail, "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(detail + "\r\n\r\nUpdate now? CROMS will close and reopen automatically.",
                "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (AppUpdater.ApplyUpdate(out string err))
            {
                SessionResume.Write();      // stay signed in across the restart
                Application.Exit();
            }
            else
                MessageBox.Show("Update failed: " + err, "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static Label Section(string text, int x, int y) => new Label
        {
            Text = text, AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
        };
        private static Button BigButton(string text, int x, int y, Color back) => new Button
        {
            Text = text, Location = new Point(x, y), Size = new Size(340, 44),
            FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), UseVisualStyleBackColor = false
        };

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ipTimer?.Stop();
            _ipTimer?.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Window Management is the default tab — prompt for verification on first show.
            PromptVerificationOnce();
        }

        // =====================================================================
        //  Administrator verification gate
        // =====================================================================

        /// <summary>Enables/disables every CRUD control until verification succeeds.</summary>
        private void SetLocked(bool locked)
        {
            foreach (Control c in bar.Controls) c.Enabled = !locked;
            btnUnlock.Visible = locked;
        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs.SelectedTab == tabWindows) PromptVerificationOnce();
        }

        private void PromptVerificationOnce()
        {
            if (_verified || _promptedOnce) return;
            _promptedOnce = true;
            RequestVerification();
        }

        private void btnUnlock_Click(object sender, EventArgs e) => RequestVerification();

        /// <summary>
        /// Shows the secure re-authentication dialog. On success unlocks the CRUD
        /// controls for the rest of the session; on cancel leaves them locked.
        /// </summary>
        private void RequestVerification()
        {
            if (_verified) return;
            using (var dlg = new AdminVerificationForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;   // cancelled → stay locked
                _verified = true;
                _verifiedUser = dlg.VerifiedUser;
                SetLocked(false);
                Audit.Write(Audit.Login, "windows", 0,
                    "Window Management unlocked" + Stamp());
            }
        }

        /// <summary>Audit suffix: who verified + which machine the change was made on.</summary>
        private string Stamp() =>
            " [by " + (_verifiedUser?.Username ?? Session.User?.Username ?? "?") +
            " · " + (_verifiedUser?.Role ?? Session.User?.Role ?? "?") +
            " · PC: " + Environment.MachineName + "]";

        // =====================================================================
        //  Window Management
        // =====================================================================

        private void LoadWindows()
        {
            DataTable dt = Db.Pull(
                "SELECT id AS 'Window ID', window_name AS 'Window Name', " +
                "COALESCE(description, '') AS Description, " +
                "CASE WHEN is_priority = 1 THEN 'Priority' ELSE 'Regular' END AS Type, " +
                "is_priority AS _pri, status AS Status, " +
                "display_order AS 'Order' FROM windows ORDER BY display_order, id");

            // A one-click on/off column: ticked = Active (shows at login / board / display),
            // unticked = Inactive. Clicking it toggles the window's status (see the handler).
            dt.Columns.Add(new DataColumn("Active?", typeof(bool)));
            foreach (DataRow r in dt.Rows)
                r["Active?"] = string.Equals(r["Status"].ToString(), "Active", StringComparison.OrdinalIgnoreCase);
            dt.AcceptChanges();

            dgvWindows.DataSource = dt;
            if (dgvWindows.Columns.Contains("Order")) dgvWindows.Columns["Order"].Visible = false;
            if (dgvWindows.Columns.Contains("_pri")) dgvWindows.Columns["_pri"].Visible = false;

            // Only the toggle column is editable; the rest of the grid stays read-only.
            foreach (DataGridViewColumn c in dgvWindows.Columns) c.ReadOnly = true;
            if (dgvWindows.Columns.Contains("Active?"))
            {
                DataGridViewColumn toggle = dgvWindows.Columns["Active?"];
                toggle.ReadOnly = false;
                toggle.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                toggle.Width = 70;
                toggle.DisplayIndex = dgvWindows.Columns["Status"].DisplayIndex + 1;
                toggle.ToolTipText = "Tick to make this window Active, untick to make it Inactive.";
            }
        }

        private bool TryGetSelected(out int id, out string name, out string desc, out string status, out int order)
        {
            id = 0; name = null; desc = null; status = null; order = 0;
            if (dgvWindows.CurrentRow == null) { Warn("Select a window first."); return false; }
            id = Convert.ToInt32(dgvWindows.CurrentRow.Cells["Window ID"].Value);
            name = dgvWindows.CurrentRow.Cells["Window Name"].Value?.ToString();
            desc = dgvWindows.CurrentRow.Cells["Description"].Value?.ToString();
            status = dgvWindows.CurrentRow.Cells["Status"].Value?.ToString();
            order = Convert.ToInt32(dgvWindows.CurrentRow.Cells["Order"].Value);
            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            using (var dlg = new WindowEditDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                int nextOrder = Db.GetCount("SELECT id FROM windows") + 1;
                long id = Db.Insert(
                    "INSERT INTO windows (window_name, description, status, is_priority, display_order) " +
                    "VALUES (@n, @d, @s, @p, @o)",
                    new MySqlParameter("@n", dlg.WindowName),
                    new MySqlParameter("@d", (object)dlg.Description ?? DBNull.Value),
                    new MySqlParameter("@s", dlg.StatusValue),
                    new MySqlParameter("@p", dlg.IsPriority ? 1 : 0),
                    new MySqlParameter("@o", nextOrder));
                Audit.Write("Create", "windows", (int)id,
                    "Added window '" + dlg.WindowName + "' (" + dlg.StatusValue +
                    (dlg.IsPriority ? ", Priority" : "") + ")" + Stamp());
                LoadWindows();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            if (!TryGetSelected(out int id, out string name, out string desc, out string status, out _)) return;
            bool wasPriority = dgvWindows.CurrentRow.Cells["_pri"].Value != DBNull.Value
                && Convert.ToInt32(dgvWindows.CurrentRow.Cells["_pri"].Value) == 1;
            using (var dlg = new WindowEditDialog(name, desc, status, wasPriority))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                // Deactivating a window that is mid-transaction would strand the client.
                if (dlg.StatusValue == "Inactive" && status == "Active" && IsServing(id))
                {
                    Warn("'" + name + "' is currently serving a ticket. Finish it before setting it Inactive.");
                    return;
                }

                Db.Push("UPDATE windows SET window_name = @n, description = @d, status = @s, is_priority = @p WHERE id = @id",
                    new MySqlParameter("@n", dlg.WindowName),
                    new MySqlParameter("@d", (object)dlg.Description ?? DBNull.Value),
                    new MySqlParameter("@s", dlg.StatusValue),
                    new MySqlParameter("@p", dlg.IsPriority ? 1 : 0),
                    new MySqlParameter("@id", id));
                Audit.Write("Update", "windows", id, "Edited window '" + dlg.WindowName + "'" +
                    (dlg.IsPriority ? " [Priority]" : "") + Stamp());
                LoadWindows();
            }
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            SetStatus("Active");
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            if (!TryGetSelected(out int id, out string name, out _, out _, out _)) return;
            if (IsServing(id))
            {
                Warn("'" + name + "' is currently serving a ticket. Finish it before disabling.");
                return;
            }
            SetStatus("Inactive");
        }

        /// <summary>
        /// One-click per-window on/off: clicking the "Active?" checkbox flips that window
        /// between Active and Inactive. Inactive windows disappear from the login window
        /// picker, the Now Serving board, the dashboard and the public display. Same
        /// verification gate + serving guard as the Enable/Disable buttons.
        /// </summary>
        private void dgvWindows_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvWindows.Columns[e.ColumnIndex].Name != "Active?") return;
            int rowIndex = e.RowIndex;
            // Defer: reloading the grid inside the click handler while the cell is being
            // committed can throw — run it after the click finishes.
            BeginInvoke((Action)(() => ToggleActive(rowIndex)));
        }

        private void ToggleActive(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvWindows.Rows.Count) return;
            if (!_verified) { RequestVerification(); if (!_verified) { LoadWindows(); return; } }

            DataGridViewRow row = dgvWindows.Rows[rowIndex];
            int id = Convert.ToInt32(row.Cells["Window ID"].Value);
            string name = row.Cells["Window Name"].Value?.ToString();
            string current = row.Cells["Status"].Value?.ToString();
            string target = string.Equals(current, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Inactive" : "Active";

            if (target == "Inactive" && IsServing(id))
            {
                Warn("'" + name + "' is currently serving a ticket. Finish it before setting it Inactive.");
                LoadWindows();   // revert the checkbox to its true state
                return;
            }

            Db.Push("UPDATE windows SET status = @s WHERE id = @id",
                new MySqlParameter("@s", target), new MySqlParameter("@id", id));
            Audit.Write("Update", "windows", id,
                (target == "Active" ? "Enabled '" : "Disabled '") + name + "'" + Stamp());
            LoadWindows();
        }

        private void btnEnableAll_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            if (Db.GetCount("SELECT id FROM windows WHERE status <> 'Active'") == 0)
            { Warn("All windows are already Active."); return; }

            if (MessageBox.Show("Set every window to Active?", "Activate all windows",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Db.Push("UPDATE windows SET status = 'Active' WHERE status <> 'Active'");
            Audit.Write("Update", "windows", 0, "Activated all windows" + Stamp());
            LoadWindows();
        }

        private void btnDisableAll_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            if (Db.GetCount("SELECT id FROM windows WHERE status <> 'Inactive'") == 0)
            { Warn("All windows are already Inactive."); return; }

            if (MessageBox.Show("Set every window to Inactive? Windows currently serving a ticket " +
                    "will be skipped.", "Deactivate all windows",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Skip any Active window that is mid-transaction so a client is never stranded.
            DataTable actives = Db.Pull("SELECT id, window_name FROM windows WHERE status = 'Active'");
            int done = 0, skipped = 0;
            foreach (DataRow r in actives.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (IsServing(id)) { skipped++; continue; }
                Db.Push("UPDATE windows SET status = 'Inactive' WHERE id = @id",
                    new MySqlParameter("@id", id));
                done++;
            }
            Audit.Write("Update", "windows", 0,
                "Deactivated all windows (" + done + " set Inactive, " + skipped + " skipped)" + Stamp());
            LoadWindows();
            if (skipped > 0)
                Warn(skipped + " window(s) were skipped because they are still serving a ticket. " +
                     "Finish those, then Deactivate All again (or Disable them individually).");
        }

        private void SetStatus(string status)
        {
            if (!TryGetSelected(out int id, out string name, out _, out string current, out _)) return;
            if (current == status) return;   // no change
            Db.Push("UPDATE windows SET status = @s WHERE id = @id",
                new MySqlParameter("@s", status), new MySqlParameter("@id", id));
            Audit.Write("Update", "windows", id,
                (status == "Active" ? "Enabled '" : "Disabled '") + name + "'" + Stamp());
            LoadWindows();
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            MoveWindow(-1);
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            MoveWindow(1);
        }

        private void MoveWindow(int direction)
        {
            if (!TryGetSelected(out int id, out _, out _, out _, out int order)) return;

            DataTable nb = Db.Pull(
                "SELECT id, display_order FROM windows WHERE display_order " +
                (direction < 0 ? "< @o ORDER BY display_order DESC" : "> @o ORDER BY display_order ASC") +
                " LIMIT 1", new MySqlParameter("@o", order));
            if (nb.Rows.Count == 0) return;   // already at the edge

            int nbId = Convert.ToInt32(nb.Rows[0]["id"]);
            int nbOrder = Convert.ToInt32(nb.Rows[0]["display_order"]);

            Db.Push("UPDATE windows SET display_order = @o WHERE id = @id",
                new MySqlParameter("@o", nbOrder), new MySqlParameter("@id", id));
            Db.Push("UPDATE windows SET display_order = @o WHERE id = @id",
                new MySqlParameter("@o", order), new MySqlParameter("@id", nbId));
            LoadWindows();

            foreach (DataGridViewRow r in dgvWindows.Rows)
                if (Convert.ToInt32(r.Cells["Window ID"].Value) == id)
                { r.Selected = true; dgvWindows.CurrentCell = r.Cells["Window Name"]; break; }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            if (!TryGetSelected(out int id, out string name, out _, out _, out _)) return;

            if (IsInUse(id))
            {
                Warn("This window cannot be removed because it is currently assigned to an " +
                     "operator or has active queue transactions.");
                return;
            }

            if (MessageBox.Show(
                    "Are you sure you want to remove this service window? This action cannot be undone.",
                    "Remove window", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            Db.Push("DELETE FROM windows WHERE id = @id", new MySqlParameter("@id", id));
            Audit.Write("Delete", "windows", id, "Deleted window '" + name + "'" + Stamp());
            LoadWindows();
        }

        /// <summary>A ticket is currently being served at this window (today).</summary>
        private static bool IsServing(int windowId) => Db.GetCount(
            "SELECT id FROM queue_tickets WHERE status = 'Serving' AND window_no = " + windowId +
            " AND DATE(created_at) = CURDATE()") > 0;

        /// <summary>
        /// True when the window cannot be deleted: an operator is logged in (Online), or
        /// any non-completed ticket is assigned to it today (serving / pending release).
        /// </summary>
        private static bool IsInUse(int windowId)
        {
            bool operatorOn = Db.GetCount(
                "SELECT id FROM windows WHERE id = " + windowId +
                " AND current_operator IS NOT NULL AND last_heartbeat > (NOW() - INTERVAL " +
                StaleMinutes + " MINUTE)") > 0;
            bool activeTicket = Db.GetCount(
                "SELECT id FROM queue_tickets WHERE window_no = " + windowId +
                " AND status <> 'Completed' AND DATE(created_at) = CURDATE()") > 0;
            return operatorOn || activeTicket;
        }

        private void Warn(string m) =>
            MessageBox.Show(m, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // =====================================================================
        //  User Manual (searchable)
        // =====================================================================

        private void txtSearch_TextChanged(object sender, EventArgs e) => FilterManual(txtSearch.Text);

        private void FilterManual(string term)
        {
            term = (term ?? "").Trim().ToLowerInvariant();
            _shown = string.IsNullOrEmpty(term)
                ? _topics
                : _topics.Where(t =>
                    t.Title.ToLowerInvariant().Contains(term) ||
                    t.Category.ToLowerInvariant().Contains(term) ||
                    t.Body.ToLowerInvariant().Contains(term)).ToList();

            lstTopics.BeginUpdate();
            lstTopics.Items.Clear();
            foreach (var t in _shown) lstTopics.Items.Add(t.Category + " — " + t.Title);
            lstTopics.EndUpdate();

            if (lstTopics.Items.Count > 0) lstTopics.SelectedIndex = 0;
            else rtbContent.Text = "No help topics match \"" + term + "\".";
        }

        private void lstTopics_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = lstTopics.SelectedIndex;
            if (i < 0 || i >= _shown.Count) return;
            ManualTopic t = _shown[i];
            rtbContent.Text = t.Category.ToUpper() + "\n" + t.Title + "\n\n" + t.Body;

            // Bold the heading lines.
            rtbContent.Select(0, t.Category.Length);
            rtbContent.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            int titleStart = t.Category.Length + 1;
            rtbContent.Select(titleStart, t.Title.Length);
            rtbContent.SelectionFont = new Font("Segoe UI", 14F, FontStyle.Bold);
            rtbContent.Select(0, 0);
        }

        private sealed class ManualTopic
        {
            public string Category, Title, Body;
            public ManualTopic(string c, string t, string b) { Category = c; Title = t; Body = b; }
        }

        private static List<ManualTopic> BuildManual() => new List<ManualTopic>
        {
            new ManualTopic("Login", "How to log in",
                "1. Launch CROMS. The Sign In window appears.\n" +
                "2. Enter your username and password, then click Sign In (or press Enter).\n" +
                "3. If the details are correct you continue to Window Assignment.\n" +
                "Accounts are created by an administrator under Users & Audit Trail."),
            new ManualTopic("Login", "How to select a service window",
                "After signing in, the Window Assignment screen lists every Active window.\n" +
                "• Pick the window you will occupy. A window already taken by another operator " +
                "is shown as 'occupied' and cannot be chosen.\n" +
                "• Choosing a window sets it Online and locks it to you until you log out.\n" +
                "• If you are not manning a window (e.g. an administrator), click Skip."),
            new ManualTopic("Login", "How to select transaction types",
                "On the Window Assignment screen, tick the transactions your window will handle " +
                "(New Registration, CTC, Marriage, Death, Petition, Verification), or tick " +
                "'All Transactions'. Queue routing then only sends this window the tickets it is " +
                "authorized to process. You can change this by logging out and back in."),

            new ManualTopic("Queue Management", "Calling the next ticket",
                "Click 'Call Next'. CROMS finds the oldest waiting ticket (priority lane first) " +
                "that an online, free window is authorized to handle and assigns it there. " +
                "If no online window matches the waiting transactions, a message explains why."),
            new ManualTopic("Queue Management", "Processing requests",
                "Click the Now Serving card for your window to open the form for the current " +
                "service (e.g. Certificate Request). Complete and save that form."),
            new ManualTopic("Queue Management", "Completing transactions",
                "After saving the service form, click the window card again to mark the service " +
                "complete. When every service on the ticket is done the ticket is marked Completed " +
                "and the window is freed — press Call Next for the next client."),
            new ManualTopic("Queue Management", "Handling multiple transactions under one ticket",
                "A client can request several services on ONE queue number at the kiosk. CROMS keeps " +
                "the same ticket number and walks through the services one at a time. Each service is " +
                "processed, then the ticket moves on to the next required service until all are done."),

            new ManualTopic("Window Management", "Viewing online and offline windows",
                "The Dashboard 'Service Windows' panel lists every window as 🟢 Online (an operator " +
                "is signed in) or 🔴 Offline. It refreshes automatically every few seconds — a login " +
                "or logout anywhere shows up without restarting the app."),
            new ManualTopic("Window Management", "Understanding window status",
                "Two independent things: ACTIVE/INACTIVE is set by an admin in Settings and decides " +
                "whether the window exists on the board at all. ONLINE/OFFLINE reflects whether an " +
                "operator is currently signed in to that window. An Inactive window never receives " +
                "tickets; an Active but Offline window has nobody manning it yet."),
            new ManualTopic("Window Management", "Assigning operators to windows",
                "Operators assign themselves at login via Window Assignment — one operator per window. " +
                "There is no separate admin step; whoever signs in to a window owns it until logout."),

            new ManualTopic("Settings", "Adding a new window",
                "Settings → Window Management → Add Window. Enter a Window Name, an optional " +
                "Description, and a default Status (Active/Inactive), then Save. The new window " +
                "immediately appears in the Login picker, Dashboard, Now Serving board and routing — " +
                "no restart or code change."),
            new ManualTopic("Settings", "Editing windows",
                "Select a window and click Edit Window to rename it, change its description, or switch " +
                "its status. You cannot set a window Inactive while it is serving a ticket."),
            new ManualTopic("Settings", "Disabling windows",
                "Select a window and click Disable. An Inactive window cannot be selected at login and " +
                "receives no queue tickets. Disabling is blocked while the window is serving a ticket."),
            new ManualTopic("Settings", "Activating windows",
                "Select an Inactive window and click Enable to make it Active and available again."),
            new ManualTopic("Settings", "Deleting windows",
                "Select a window and click Delete Window. Deletion is allowed only when no operator is " +
                "logged in and no active/pending ticket is assigned; otherwise CROMS blocks it. You " +
                "must confirm — deletion cannot be undone."),

            new ManualTopic("Reports", "Viewing daily reports",
                "Open Reports & PSA, choose the month and year, and click Generate to see registered " +
                "births/marriages/deaths, the timely-vs-delayed split, collections, and a detail roster."),
            new ManualTopic("Reports", "Printing reports",
                "Generate the report first, then use your system print dialog on the exported file, or " +
                "print the exported CSV from a spreadsheet application."),
            new ManualTopic("Reports", "Exporting reports",
                "Click Export CSV on the report (and on the Queue screen) to save a spreadsheet-ready " +
                "file for the PSA submission packet or office records."),

            new ManualTopic("FAQ", "Why can't I log in?",
                "Check the username/password spelling (the message is the same for a wrong user or " +
                "wrong password). If it says the account is deactivated, ask an administrator to " +
                "re-activate it under Users & Audit Trail."),
            new ManualTopic("FAQ", "Why is my window unavailable?",
                "It is either Inactive (an admin disabled it in Settings) or already occupied by " +
                "another operator who is signed in. Pick a different window or ask an admin to enable one."),
            new ManualTopic("FAQ", "Why can't I call the next ticket?",
                "Either the queue is empty, the queue is paused, no window is Online, or no online " +
                "window is assigned to the transaction the waiting ticket needs. The on-screen message " +
                "says which."),
            new ManualTopic("FAQ", "Why is a ticket locked?",
                "A ticket/service being processed is locked to one window so two windows can't call the " +
                "same client. The lock clears when the service is completed or the ticket finishes."),
            new ManualTopic("FAQ", "How do I complete a transaction?",
                "Open the service form from your Now Serving card, fill and save it, then click the card " +
                "again to mark it done. Repeat for each service; the ticket closes when all are complete."),
            new ManualTopic("FAQ", "How do I add a new service window?",
                "Only an Administrator or Registrar can. Go to Settings → Window Management → Add Window, " +
                "fill in the details and Save. It appears everywhere automatically."),
        };
    }

    /// <summary>
    /// Add / Edit window dialog: Window Name, optional Description, and a default Status
    /// (Active/Inactive) chosen with radio buttons. Built in code (no Designer file), the
    /// same self-contained-dialog pattern used elsewhere in the project.
    /// </summary>
    internal sealed class WindowEditDialog : Form
    {
        private readonly TextBox _name;
        private readonly TextBox _desc;
        private readonly RadioButton _active;
        private readonly RadioButton _inactive;
        private readonly CheckBox _priority;

        public string WindowName => _name.Text.Trim();
        public string Description
        {
            get { string d = _desc.Text.Trim(); return d.Length == 0 ? null : d; }
        }
        public string StatusValue => _active.Checked ? "Active" : "Inactive";
        public bool IsPriority => _priority.Checked;

        public WindowEditDialog(string name = null, string desc = null, string status = "Active", bool priority = false)
        {
            Text = name == null ? "Add Window" : "Edit Window";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            BackColor = Color.White;
            ClientSize = new Size(420, 392);

            Controls.Add(Cap("Window Name", 20, 20));
            _name = Field(20, 44); _name.Text = name ?? "";
            Controls.Add(_name);

            Controls.Add(Cap("Description (optional)", 20, 92));
            _desc = Field(20, 116); _desc.Text = desc ?? "";
            Controls.Add(_desc);

            Controls.Add(Cap("Status", 20, 164));
            _active = new RadioButton
            {
                Text = "Active", Location = new Point(24, 190), AutoSize = true,
                Font = new Font("Segoe UI", 10F), Checked = status != "Inactive"
            };
            _inactive = new RadioButton
            {
                Text = "Inactive", Location = new Point(120, 190), AutoSize = true,
                Font = new Font("Segoe UI", 10F), Checked = status == "Inactive"
            };
            Controls.Add(_active); Controls.Add(_inactive);

            // Priority Window: handles ALL transaction types, keeps its ticket until every
            // service is done, and is never auto-forwarded (spec section 1).
            _priority = new CheckBox
            {
                Text = "Priority Window (handles all services, no forwarding)",
                Location = new Point(24, 226), AutoSize = true,
                Font = new Font("Segoe UI", 10F), Checked = priority
            };
            Controls.Add(_priority);
            Controls.Add(new Label
            {
                Text = "A Priority Window serves every transaction type and keeps a client " +
                       "until all their requests are completed.",
                Location = new Point(24, 252), Size = new Size(376, 40),
                Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            var save = new Button
            {
                Text = "Save", Location = new Point(20, 322), Size = new Size(180, 44),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                BackColor = Color.FromArgb(13, 110, 253), Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += Save_Click;
            var cancel = new Button
            {
                Text = "Cancel", Location = new Point(216, 322), Size = new Size(184, 44),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 10F)
            };
            cancel.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            Controls.Add(save); Controls.Add(cancel);
            AcceptButton = save; CancelButton = cancel;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (WindowName.Length == 0)
            {
                MessageBox.Show("Enter a window name.", "Add Window",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label Cap(string t, int x, int y) => new Label
        {
            Text = t, AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(73, 80, 87)
        };
        private static TextBox Field(int x, int y) => new TextBox
        {
            Location = new Point(x, y), Size = new Size(380, 28),
            Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.FixedSingle
        };
    }
}
