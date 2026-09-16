using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS
{
    /// <summary>
    /// Application shell. The left sidebar and its nav buttons are laid out in the
    /// Designer (MainForm.Designer.cs) so every button is editable on the canvas.
    /// Each button carries its module key in its <c>Tag</c>; clicking one swaps that
    /// module's Form into the content panel. Forms are created lazily and cached, so
    /// each module keeps its state when navigated away and back — a single-instance
    /// alternative to MDI.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, Form> _cache = new Dictionary<string, Form>();
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private string _activeKey;

        // Shell colours come from the one shared token set (Modules/UiTheme) so the sidebar,
        // the module surfaces and the kiosk can't drift apart again.
        private static readonly Color SidebarBack   = UiTheme.Navy;          // #132441
        private static readonly Color NavIdleFore   = Color.FromArgb(196, 206, 227);
        private static readonly Color NavActiveBack = UiTheme.Accent;        // #1D4ED8

        private Timer _heartbeat;

        public MainForm()
        {
            InitializeComponent();
            RegisterNavButtons();
            BuildUserBar();
            ApplyRoleAccess();
            UiTheme.PolishButtons(this);   // hand cursor + hover on nav + header buttons
            StartWindowHeartbeat();
            // Open on the first module (Dashboard) by default.
            if (ModuleRegistry.All.Count > 0)
                ShowModule(ModuleRegistry.All[0].Key);
        }

        /// <summary>
        /// Keeps the operator's claimed window Online: refreshes its heartbeat every
        /// 30s so it doesn't go stale, and frees the window when the app closes so a
        /// crash / normal exit doesn't leave a window falsely "occupied".
        /// </summary>
        private void StartWindowHeartbeat()
        {
            if (!Session.HasWindow) return;
            Forms.WindowAssignmentForm.Heartbeat(Session.WindowId);   // stamp now
            _heartbeat = new Timer { Interval = 30000 };
            _heartbeat.Tick += (s, e) => Forms.WindowAssignmentForm.Heartbeat(Session.WindowId);
            _heartbeat.Start();
            FormClosing += (s, e) =>
            {
                _heartbeat?.Stop();
                if (Session.HasWindow) Forms.WindowAssignmentForm.Release(Session.WindowId);
            };
        }

        /// <summary>
        /// Shows who is signed in as a single clickable "user chip", right-aligned in the
        /// header. Clicking it (or the little ▾) drops a menu with My Profile / Edit Profile /
        /// Logout — same pattern as the reference mockup (click the name to get an account
        /// menu) instead of always-visible Logout/Biodata buttons crowding the header.
        /// Laid out in a right-docked, right-to-left FlowLayoutPanel so the chip is always
        /// pinned to the right edge regardless of window width or name length.
        /// </summary>
        private Label _lblUserChip;

        private void BuildUserBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Width = 480,
                Padding = new Padding(0, 0, 20, 0),
                BackColor = headerPanel.BackColor
            };

            var chip = new Panel
            {
                AutoSize = false,
                Size = new Size(260, 44),
                Margin = new Padding(0, 6, 0, 6),
                Cursor = Cursors.Hand,
                BackColor = UiTheme.PageBg
            };
            chip.Paint += (s, e) =>
            {
                using (var path = RoundedRectPath(chip.ClientRectangle, 8))
                using (var brush = new SolidBrush(chip.BackColor))
                    e.Graphics.FillPath(brush, path);
            };

            _lblUserChip = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 26, 0),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Cursor = Cursors.Hand
            };
            RefreshUserChipText();
            chip.Controls.Add(_lblUserChip);

            var caret = new Label
            {
                Text = "▾",
                AutoSize = false,
                Size = new Size(20, 44),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                Cursor = Cursors.Hand
            };
            chip.Controls.Add(caret);
            caret.BringToFront();

            EventHandler openMenu = (s, e) => ShowUserMenu(chip);
            chip.Click += openMenu;
            _lblUserChip.Click += openMenu;
            caret.Click += openMenu;
            chip.MouseEnter += (s, e) => { chip.BackColor = UiTheme.AccentTint; chip.Invalidate(); };
            chip.MouseLeave += (s, e) => { chip.BackColor = UiTheme.PageBg; chip.Invalidate(); };

            bar.Controls.Add(chip);   // pinned to the far right

            // "Update" button — only on client PCs (a server share to pull from
            // exists). Lets staff pull the latest app build from the server over
            // the LAN with one click, no re-copying files.
            if (Data.AppUpdater.ShareRoot != null)
            {
                var btnUpdate = new Button
                {
                    Text = "⟳ Update",
                    AutoSize = false,
                    Size = new Size(104, 36),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(13, 110, 253),
                    Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 10, 10, 10)
                };
                btnUpdate.FlatAppearance.BorderSize = 0;
                btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(11, 94, 215);
                btnUpdate.Click += (s, e) => CheckForUpdate();
                bar.Controls.Add(btnUpdate);   // flows to the left of the chip
            }

            headerPanel.Controls.Add(bar);
            bar.BringToFront();

            // Thin divider under the header so it reads as a distinct bar.
            headerPanel.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(222, 226, 230)
            });
        }

        private void RefreshUserChipText()
        {
            if (_lblUserChip == null) return;
            _lblUserChip.Text = Session.User != null
                ? Session.User.FullName + "\n" + Session.User.Role
                : "Not signed in";
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            if (d >= r.Height || d >= r.Width) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// The account dropdown: signed-in name/role header, then My Profile / Edit Profile /
        /// Logout — opened by clicking the header user chip.
        /// </summary>
        private void ShowUserMenu(Control anchor)
        {
            var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F) };
            UiTheme.StyleMenu(menu);

            var header = new ToolStripMenuItem(
                (Session.User?.FullName ?? "Not signed in") + "  ·  " + (Session.User?.Role ?? ""))
            {
                Enabled = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());

            var viewProfile = new ToolStripMenuItem("View My Profile");
            viewProfile.Click += (s, e) =>
            {
                using (var f = new Forms.StaffBiodataForm())
                    f.ShowDialog(this);
            };
            menu.Items.Add(viewProfile);

            var editProfile = new ToolStripMenuItem("Edit Profile");
            editProfile.Click += (s, e) =>
            {
                using (var f = new Forms.EditProfileForm())
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        RefreshUserChipText();
                }
            };
            menu.Items.Add(editProfile);

            menu.Items.Add(new ToolStripSeparator());

            var logout = new ToolStripMenuItem("Logout") { ForeColor = UiTheme.Danger };
            logout.Click += (s, e) => Logout();
            menu.Items.Add(logout);

            menu.Show(anchor, new Point(0, anchor.Height));
        }

        /// <summary>
        /// Pull the latest app build from the server over the LAN (one click). Checks
        /// the server's release share for a newer CROMS.exe; if found, stages the new
        /// files and restarts CROMS to apply them. Config files are preserved, so this
        /// PC keeps its own connection settings. Fully offline (LAN only).
        /// </summary>
        private void CheckForUpdate()
        {
            string detail;
            bool available = Data.AppUpdater.IsUpdateAvailable(out detail);
            if (!available)
            {
                MessageBox.Show(detail, "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(detail + "\r\n\r\nUpdate now? CROMS will close and reopen automatically.",
                "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            string err;
            if (Data.AppUpdater.ApplyUpdate(out err))
            {
                // Keep this operator signed in across the update restart (they just
                // authorized it) — a one-time token the relaunched app consumes so it
                // skips the login screen. Written before Exit so Session is still set.
                Data.SessionResume.Write();

                // The helper batch is now waiting for this app to close; exit so it
                // can swap the files and relaunch. FormClosing frees the window +
                // stops the mobile servers as usual.
                Application.Exit();
            }
            else
            {
                MessageBox.Show("Update failed: " + err, "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Logout()
        {
            if (MessageBox.Show("Sign out of CROMS?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Session.User != null)
                Audit.Write(Audit.Logout, "users", Session.User.Id, "Signed out: " + Session.User.Username);
            _heartbeat?.Stop();
            if (Session.HasWindow) Forms.WindowAssignmentForm.Release(Session.WindowId);   // free the window
            Session.WindowId = 0; Session.WindowName = null;
            Session.User = null;
            Application.Restart();   // back to the login screen (Program.Main)
        }

        /// <summary>
        /// Hides sidebar buttons the signed-in role should not use. Admin sees everything.
        /// (Cross-module hand-offs still work programmatically; this only gates the menu.)
        /// </summary>
        private void ApplyRoleAccess()
        {
            HashSet<string> allowed = AllowedKeys(Session.User?.Role);
            if (allowed == null) return;   // Admin (or unknown) → full access
            foreach (var pair in _navButtons)
                pair.Value.Visible = allowed.Contains(pair.Key);
        }

        /// <summary>
        /// Module keys each role may open, or null for full access (Admin).
        /// The office runs a flexible three-stage workflow (receiving / processing / releasing)
        /// where any staff member may cover any stage on a given day, rather than CROMS assuming
        /// one fixed person per stage. So every non-Admin role gets the SAME broad operational
        /// set ("semi-admin") — the distinction that matters is operational vs true admin
        /// (Master Files / Settings / Users & Audit Trail / Records Archive stay Admin-only).
        /// </summary>
        private static readonly HashSet<string> OperationalKeys = new HashSet<string> {
            "dashboard", "queue", "transactions", "certrequest", "release", "breqs",
            "birth", "marriage", "death", "petitions", "books", "search", "ocr", "fees", "reports"
        };

        private static HashSet<string> AllowedKeys(string role)
        {
            switch (role)
            {
                case "Registrar":
                case "Staff":
                case "Cashier":
                case "Releasing":
                    return OperationalKeys;
                default:
                    return null;   // Admin → everything, incl. masterfiles/settings/users/archive
            }
        }

        /// <summary>
        /// Indexes the designer-placed nav buttons by their module key (Tag), so the
        /// active-highlight logic can find them. Add a new module button in the
        /// Designer, set its Tag to the module key, and wire Click to NavButton_Click.
        /// </summary>
        private void RegisterNavButtons()
        {
            foreach (Control c in navFlow.Controls)
            {
                if (c is Button btn && btn.Tag is string key)
                    _navButtons[key] = btn;
            }
        }

        /// <summary>Shared click handler for every sidebar nav button.</summary>
        private void NavButton_Click(object sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is string key)
                ShowModule(key);
        }

        /// <summary>
        /// Makes the given module the active view: builds and caches its form on
        /// first request, brings it to the front, and updates header + nav highlight.
        /// Each module is a real Form embedded inside the content panel with
        /// <c>TopLevel = false</c>, so it renders in-place instead of as a separate
        /// window while staying a fully designable Form in the IDE.
        /// </summary>
        private void ShowModule(string key)
        {
            if (key == _activeKey) return;

            ModuleInfo module = null;
            foreach (var m in ModuleRegistry.All)
            {
                if (m.Key == key) { module = m; break; }
            }
            if (module == null) return;

            if (!_cache.TryGetValue(key, out var form))
            {
                form = module.Factory();
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;
                // Some module forms are laid out wider/taller than the content panel on
                // smaller screens; AutoScroll makes any clipped controls (e.g. the right-
                // side action buttons/panels) reachable instead of being cut off.
                form.AutoScroll = true;
                _cache[key] = form;
                contentPanel.Controls.Add(form);
                form.Show();
                UiTheme.PolishButtons(form);   // consistent hand cursor + hover on every module's buttons
            }

            form.BringToFront();
            // Cached forms are reused, so re-pull their data every time the module is
            // shown — keeps cross-module views (e.g. Release & Claim's pending list) live.
            if (form is IRefreshable refreshable) refreshable.RefreshData();
            headerLabel.Text = module.Title;
            SetActiveButton(key);
            _activeKey = key;
        }

        private void SetActiveButton(string key)
        {
            foreach (var pair in _navButtons)
            {
                bool active = pair.Key == key;
                pair.Value.BackColor = active ? NavActiveBack : SidebarBack;
                pair.Value.ForeColor = active ? Color.White : NavIdleFore;
                pair.Value.Font = new Font("Segoe UI", 10F, active ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        /// <summary>
        /// Public navigation entry point for cross-module hand-offs (e.g. Queue
        /// Management opening Certificate Request for a called ticket). Shows the
        /// module and returns its cached Form so the caller can prime it.
        /// </summary>
        public Form GoToModule(string key)
        {
            ShowModule(key);
            return _cache.TryGetValue(key, out var form) ? form : null;
        }
    }
}
