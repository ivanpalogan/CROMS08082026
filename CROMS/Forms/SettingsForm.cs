using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
            BuildMonitoringTab();
            BuildSettingsShell();   // tabs -> left page list (must run after every page exists)
        }

        // =====================================================================
        //  Settings shell — a left page list, the selected page on the right.
        //
        //  This screen used to be a row of browser-style tabs, and it grew every time an
        //  admin tool landed here (Window Management, User Manual, Certificates & Forms,
        //  App Updates, Activity Monitoring) — a strip a non-technical clerk has to read
        //  end to end to find anything. The pages themselves are UNCHANGED: each tab's own
        //  controls are re-parented into a panel, so every grid, handler and anchor that
        //  worked on a TabPage still works here. The tab control is kept alive (emptied and
        //  detached) rather than disposed, because tabWindows/tabManual are Designer fields.
        //
        //  Two of the pages are whole modules that used to have their own sidebar button
        //  (Users & Access, Master Files). They are hosted here, not rewritten. Settings
        //  itself is Admin-only (MainForm.AllowedKeys), so moving them in does not hand an
        //  operational role anything it could not reach before.
        // =====================================================================

        private Panel _pageHost;
        private FlowLayoutPanel _pageNav;
        private readonly Dictionary<string, Control> _pages = new Dictionary<string, Control>();
        private readonly Dictionary<string, Button> _pageButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Func<Control>> _lazyPages = new Dictionary<string, Func<Control>>();
        private readonly HashSet<string> _adminPages = new HashSet<string>();
        private string _activePage;

        private const string PageGeneral = "General";
        private const string PageUsers   = "Users & Access";
        private const string PageMaster  = "Master Files";
        private const string PageForms   = "Forms & Templates";
        private const string PageWindows = "Window Management";
        private const string PageAudit   = "Audit Trail";
        private const string PageUpdates = "App Updates";
        private const string PageManual  = "User Manual";

        private void BuildSettingsShell()
        {
            BackColor = UiTheme.PageBg;

            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pageNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 236,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = UiTheme.PageBg,
                Padding = new Padding(0, 10, 0, 10)
            };

            // The page host has to be on the form and SIZED before any page is added to it.
            // Measured: adding a page to a still-default 200x100 Panel and docking it there
            // shrank every anchored child by that delta and then grew it back by the host's
            // real width, leaving the forms grid 1630px wide inside a 948px page.
            Controls.Add(_pageNav);
            Controls.Add(_pageHost);
            _pageHost.BringToFront();   // the Fill child must lay out last, or it eats the nav's strip
            PerformLayout();

            // Take each existing TabPage's content across before the tab control goes away.
            // The panel is created at the tab page's own size FIRST so every anchored child
            // (dgvWindows fills all four sides) keeps the offsets it was laid out with; only
            // then is it docked, which is what makes it resize correctly from here on.
            var carried = new Dictionary<string, Control>();
            foreach (TabPage tp in tabs.TabPages)
            {
                var children = new Control[tp.Controls.Count];
                tp.Controls.CopyTo(children, 0);

                // The panel has to start at the size the children were LAID OUT for, or every
                // anchored child is resized by the difference on the first dock. tp.ClientSize
                // cannot supply it: a TabPage this screen built in code has never been laid out
                // by the tab control, so it still reports the 200x100 default — measured, that
                // grew the forms grid to 1608px inside a 948px page. The children's own extent
                // is the honest answer for both the designer pages and the code-built ones.
                int w = 200, h = 100;
                foreach (Control c in children)
                {
                    if (c.Right + 3 > w) w = c.Right + 3;
                    if (c.Bottom + 3 > h) h = c.Bottom + 3;
                }

                var panel = new Panel { Size = new Size(w, h), BackColor = Color.White };
                tp.Controls.Clear();
                panel.Controls.AddRange(children);
                DisableMnemonics(panel);
                carried[tp.Text] = panel;
            }
            Controls.Remove(tabs);

            AddPage(PageGeneral, BuildGeneralPage(), admin: false);
            AddLazyPage(PageUsers, () => HostModule(new UsersAuditForm()), admin: true);
            AddLazyPage(PageMaster, () => HostModule(new MasterFilesForm()), admin: true);
            AddPage(PageForms, carried["Certificates & Forms"], admin: true);
            AddPage(PageWindows, carried["Window Management"], admin: true);
            AddPage(PageAudit, carried["Activity Monitoring"], admin: true);
            AddPage(PageUpdates, carried["App Updates"], admin: false);
            AddPage(PageManual, carried["User Manual"], admin: false);

            ShowPage(PageGeneral);
        }

        /// <summary>
        /// A Label eats a single "&amp;" as a mnemonic prefix and swallows the letter after it, so
        /// "Forms &amp; Templates" renders as "Forms  Templates". Fourth time in this codebase.
        /// </summary>
        private static void DisableMnemonics(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                var lbl = c as Label;
                if (lbl != null) lbl.UseMnemonic = false;
                if (c.HasChildren) DisableMnemonics(c);
            }
        }

        /// <summary>Puts a whole module Form inside a Settings page, the same way MainForm embeds one.</summary>
        private static Control HostModule(Form form)
        {
            var holder = new Panel { BackColor = Color.White, AutoScroll = true };
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;

            // These modules were laid out full-window, and their grids are anchored to all four
            // sides. Docked into a page that is narrower than that (the page list takes 236px,
            // and the shell is not always maximised) they do not merely tighten — measured, the
            // Master Files list came out 75px wide with no columns visible. Floor the form at
            // the size it was laid out for and let the HOLDER scroll instead.
            form.MinimumSize = form.ClientSize;
            form.AutoScroll = false;   // one scroller, not two nested ones
            form.Dock = DockStyle.Fill;
            holder.Controls.Add(form);
            form.Show();
            UiTheme.PolishButtons(form);
            return holder;
        }

        private void AddPage(string name, Control page, bool admin)
        {
            page.Visible = false;
            page.Dock = DockStyle.Fill;
            _pageHost.Controls.Add(page);
            _pages[name] = page;
            if (admin) _adminPages.Add(name);
            AddPageButton(name);
        }

        private void AddLazyPage(string name, Func<Control> factory, bool admin)
        {
            // Users & Access and Master Files each open the database on construction, and
            // Master Files' first category is 42,029 barangays — building them for a visit
            // to App Updates would cost that for nothing. Built on first selection instead.
            _lazyPages[name] = factory;
            if (admin) _adminPages.Add(name);
            AddPageButton(name);
        }

        private void AddPageButton(string name)
        {
            var b = new Button
            {
                Text = name.Replace("&", "&&"),   // a Button eats a single & as a mnemonic prefix
                Width = 212,
                Height = 40,
                Margin = new Padding(12, 2, 12, 2),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Font = new Font("Segoe UI", 9.75F),
                BackColor = UiTheme.PageBg,
                ForeColor = UiTheme.Ink,
                Cursor = Cursors.Hand,
                Tag = name
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = UiTheme.Chrome;
            b.Click += (s, e) => ShowPage(name);
            _pageNav.Controls.Add(b);
            _pageButtons[name] = b;
        }

        private void ShowPage(string name)
        {
            if (name == _activePage) return;

            Control page;
            if (!_pages.TryGetValue(name, out page))
            {
                Func<Control> factory;
                if (!_lazyPages.TryGetValue(name, out factory)) return;
                Cursor = Cursors.WaitCursor;
                try
                {
                    page = factory();
                    page.Visible = false;
                    page.Dock = DockStyle.Fill;
                    _pageHost.Controls.Add(page);
                    _pages[name] = page;
                }
                finally { Cursor = Cursors.Default; }
            }

            foreach (var p in _pages) p.Value.Visible = (p.Key == name);
            page.BringToFront();

            foreach (var pair in _pageButtons)
            {
                bool active = pair.Key == name;
                pair.Value.BackColor = active ? UiTheme.AccentTint : UiTheme.PageBg;
                pair.Value.ForeColor = active ? UiTheme.Accent : UiTheme.Ink;
                pair.Value.Font = new Font("Segoe UI", 9.75F, active ? FontStyle.Bold : FontStyle.Regular);
            }

            _activePage = name;
            // Window Management was the tab this screen opened on, so verification used to be
            // asked for on load. It opens on General now, so the prompt follows the operator
            // to whichever administrative page they actually asked for.
            if (_adminPages.Contains(name)) PromptVerificationOnce();
            if (name == PageAudit) LoadMonitoring();
            if (name == PageWindows) LoadWindows();
            if (name == PageGeneral) RefreshGeneralPage();
        }

        // ---------------------------------------------------------------- General page

        private Label _genOffice, _genServer, _genUser, _genLock;

        private Control BuildGeneralPage()
        {
            var page = new Panel { BackColor = Color.White };

            page.Controls.Add(new Label
            {
                Text = "General", AutoSize = true, Location = new Point(20, 18),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
            });
            page.Controls.Add(new Label
            {
                Text = "What this copy of CROMS is connected to, and who is using it.",
                AutoSize = true, Location = new Point(22, 50),
                Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            page.Controls.Add(Section("Office", 22, 92));
            _genOffice = new Label
            {
                AutoSize = false, Location = new Point(24, 118), Size = new Size(760, 44),
                Font = new Font("Segoe UI", 9.75F), ForeColor = Color.FromArgb(33, 37, 41)
            };
            page.Controls.Add(_genOffice);

            page.Controls.Add(Section("Database server", 22, 174));
            _genServer = new Label
            {
                AutoSize = false, Location = new Point(24, 200), Size = new Size(760, 40),
                Font = new Font("Segoe UI", 9.75F), ForeColor = Color.FromArgb(33, 37, 41)
            };
            page.Controls.Add(_genServer);

            page.Controls.Add(Section("Signed in", 22, 252));
            _genUser = new Label
            {
                AutoSize = false, Location = new Point(24, 278), Size = new Size(760, 40),
                Font = new Font("Segoe UI", 9.75F), ForeColor = Color.FromArgb(33, 37, 41)
            };
            page.Controls.Add(_genUser);

            page.Controls.Add(Section("Administrator verification", 22, 330));
            _genLock = new Label
            {
                AutoSize = false, Location = new Point(24, 356), Size = new Size(760, 30),
                Font = new Font("Segoe UI", 9.75F), ForeColor = Color.FromArgb(108, 117, 125)
            };
            page.Controls.Add(_genLock);
            var btnVerify = BigButton("Verify Administrator", 24, 390, Color.FromArgb(13, 110, 253));
            btnVerify.Click += (s, e) => { RequestVerification(); RefreshGeneralPage(); };
            page.Controls.Add(btnVerify);
            page.Controls.Add(new Label
            {
                Text = "Changing service windows, branding or print alignment asks for an administrator's\r\n" +
                       "password once per session. Verifying here does it up front.",
                AutoSize = true, Location = new Point(26, 442),
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            page.Controls.Add(Section("Mission, Vision, Goal, Objectives & Core Values", 22, 500));
            page.Controls.Add(new Label
            {
                Text = "The office's own standing statements — not tied to any record. Opens as a printable\r\n" +
                       "page you can reword and reposition in the same template designer every certificate uses.",
                AutoSize = true, Location = new Point(24, 526),
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });
            var btnMvc = BigButton("Open / Print / Edit", 24, 566, Color.FromArgb(13, 110, 253));
            btnMvc.Click += (s, e) => OfficeMissionCert.Show(OfficeMissionCert.BuildTable(), this);
            page.Controls.Add(btnMvc);

            DisableMnemonics(page);
            return page;
        }

        private void RefreshGeneralPage()
        {
            if (_genOffice == null) return;
            try
            {
                OfficeProfile p = OfficeAssets.Profile;
                _genOffice.Text = p.HeaderLine + "\r\n" +
                                  (string.IsNullOrWhiteSpace(p.RegistrarName)
                                      ? "No registrar recorded — set one under Forms & Templates."
                                      : p.RegistrarName + "  ·  " + p.RegistrarTitle);
            }
            catch { _genOffice.Text = "Office details could not be read."; }

            _genServer.Text = ServerConfig.EffectiveHost + " : " + ServerConfig.Port + "\r\n" +
                              (Db.IsConnected() ? "Connected." : "NOT reachable from this PC right now.");

            _genUser.Text = (Session.User?.FullName ?? Session.User?.Username ?? "—") +
                            "  ·  " + (Session.User?.Role ?? "—") + "\r\n" +
                            (Session.HasWindow ? "Serving at " + Session.WindowName : "No service window claimed") +
                            "  ·  PC: " + Environment.MachineName;

            _genLock.Text = _verified
                ? "Verified this session as " + (_verifiedUser?.Username ?? "?") + "."
                : "Not verified yet this session.";
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
                Text = "Forms & Templates", AutoSize = true, Location = new Point(20, 18),
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
                AutoSize = false, Size = new Size(700, 34), Location = new Point(24, 166),
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            var btnAlign = BigButton("Align Printing on Pre-printed Forms", 382, 114,
                                     Color.FromArgb(108, 117, 125));
            btnAlign.Click += btnAlign_Click;
            tab.Controls.Add(btnAlign);

            // Certificate Templates — the visual layout editor. It is system configuration
            // (where the logo, the text and each field PRINT), not a client transaction, which
            // is why it sits here and not beside Certificate Request.
            var btnTemplates = BigButton("Edit Certificate Layout Templates", 22, 208,
                                         Color.FromArgb(108, 117, 125));
            btnTemplates.Click += btnTemplates_Click;
            tab.Controls.Add(btnTemplates);
            tab.Controls.Add(new Label
            {
                Text = "Drag the header, footer, text and data fields of a certification form, then "
                     + "preview it. Nothing here changes a registry record.",
                AutoSize = false, Size = new Size(460, 34), Location = new Point(382, 214),
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            tab.Controls.Add(Section("Forms CROMS can read, store and print:", 22, 272));

            // A read-only picture of the form library. The point is the Report column:
            // it is the only place that tells the office whether a .rpt has actually been
            // authored for a form, or whether it is still printing on the built-in
            // renderer — which is otherwise invisible until someone prints one.
            var grid = new DataGridView
            {
                Location = new Point(22, 298),
                Size = new Size(860, 222),
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
                AutoSize = false, Location = new Point(24, 532), Size = new Size(858, 34),
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
        /// The certificate layout editor. Same re-verification as branding: it decides what
        /// every issued certificate looks like.
        /// </summary>
        private void btnTemplates_Click(object sender, EventArgs e)
        {
            if (!_verified) { RequestVerification(); if (!_verified) return; }
            using (var dlg = new TemplateManagementForm()) dlg.ShowDialog(this);
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

        public void RefreshData() { LoadWindows(); LoadMonitoring(); }

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

        // =====================================================================
        //  Activity Monitoring tab — VIEW ONLY, nothing here can be edited or
        //  deleted from the screen. Answers, for any date range: who signed in,
        //  what they did (create/update/delete/login/logout), which record/client
        //  it touched, and — separately flagged — any requirement that was
        //  OVERRIDDEN or WAIVED instead of actually satisfied (marriage licence
        //  requirements and delayed-birth-registration requirements share the same
        //  marriage_requirements/marriage_history tables, so one query covers both).
        //  Source data: `audit_log` (every Create/Update/Delete/Login/Logout the app
        //  writes — see Data/Audit.cs) plus `marriage_history` rows where a
        //  requirement's status was set to Waived, or an admin override was recorded
        //  (MarriageService.RecordOverride/WithdrawOverride). Nothing is aggregated
        //  or summarized away — every row is a real, individually-attributed event.
        // =====================================================================
        private DataGridView dgvMonitor;
        private DateTimePicker dtpMonFrom, dtpMonTo;
        private ComboBox cboMonUser;
        private CheckBox chkMonFlaggedOnly;
        private TextBox txtMonSearch;
        private Label lblMonCount;

        private void BuildMonitoringTab()
        {
            var tab = new TabPage("Activity Monitoring") { BackColor = Color.White, Padding = new Padding(3) };
            // Page name: "Audit Trail". This IS the audit trail — Users & Access used to carry a
            // second, thinner view of the same audit_log table (last 500 rows, no filters); that
            // one was removed rather than keeping two answers to the same question.

            tab.Controls.Add(new Label
            {
                Text = "Audit Trail", AutoSize = true, Location = new Point(20, 18),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
            });
            tab.Controls.Add(new Label
            {
                Text = "Who signed in and what they did, by date. VIEW ONLY — nothing on this " +
                       "screen can be changed. Rows marked ⚠ Bypass are a requirement that was " +
                       "overridden or waived instead of satisfied.",
                AutoSize = false, Size = new Size(820, 34),
                Location = new Point(22, 50), Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(108, 117, 125)
            });

            // --- filter bar ---
            int fy = 86;
            tab.Controls.Add(Cap2("From", 22, fy));
            dtpMonFrom = new DateTimePicker
            {
                Location = new Point(22, fy + 20), Size = new Size(130, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today
            };
            tab.Controls.Add(dtpMonFrom);

            tab.Controls.Add(Cap2("To", 164, fy));
            dtpMonTo = new DateTimePicker
            {
                Location = new Point(164, fy + 20), Size = new Size(130, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today
            };
            tab.Controls.Add(dtpMonTo);

            var btnToday = new Button
            {
                Text = "Today", Location = new Point(304, fy + 20), Size = new Size(70, 26),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F)
            };
            btnToday.Click += (s, e) => { dtpMonFrom.Value = DateTime.Today; dtpMonTo.Value = DateTime.Today; LoadMonitoring(); };
            tab.Controls.Add(btnToday);

            tab.Controls.Add(Cap2("User", 388, fy));
            cboMonUser = new ComboBox
            {
                Location = new Point(388, fy + 20), Size = new Size(170, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            tab.Controls.Add(cboMonUser);

            tab.Controls.Add(Cap2("Search (client, transaction, record...)", 572, fy));
            txtMonSearch = new TextBox { Location = new Point(572, fy + 20), Size = new Size(260, 26) };
            tab.Controls.Add(txtMonSearch);

            chkMonFlaggedOnly = new CheckBox
            {
                // Was at x=844 and 377px wide, i.e. running to 1221 — off the right edge of the
                // tab it was written for, and further off the narrower page area. Second row.
                Text = "⚠ Flagged only (bypasses + failed sign-ins)",
                Location = new Point(572, fy + 58), AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(180, 83, 9)
            };
            tab.Controls.Add(chkMonFlaggedOnly);

            var btnRefresh = new Button
            {
                Text = "Refresh", Location = new Point(22, fy + 54), Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                BackColor = Color.FromArgb(13, 110, 253), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnRefresh.Click += (s, e) => LoadMonitoring();
            tab.Controls.Add(btnRefresh);

            var btnExport = new Button
            {
                Text = "Export CSV", Location = new Point(118, fy + 54), Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F)
            };
            btnExport.Click += (s, e) => ExportMonitoringCsv();
            tab.Controls.Add(btnExport);

            lblMonCount = new Label
            {
                Text = "", AutoSize = true, Location = new Point(230, fy + 62),
                Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(108, 117, 125)
            };
            tab.Controls.Add(lblMonCount);

            // --- the log itself: strictly read-only ---
            dgvMonitor = new DataGridView
            {
                Location = new Point(22, fy + 96),
                Size = new Size(900, 380),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeRows = false,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                MultiSelect = false
            };
            dgvMonitor.CellFormatting += DgvMonitor_CellFormatting;
            dgvMonitor.CellDoubleClick += DgvMonitor_CellDoubleClick;
            tab.Controls.Add(dgvMonitor);

            tab.Controls.Add(new Label
            {
                Text = "Every create, update, delete, sign-in and sign-out the app records, plus any " +
                       "licence/registration requirement an officer marked Waived or overrode instead " +
                       "of verifying. This is the one tamper-evident log CROMS keeps — Users & " +
                       "Access no longer carries a second, thinner view of it.",
                AutoSize = false, Location = new Point(24, fy + 480), Size = new Size(898, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.75F), ForeColor = Color.FromArgb(108, 117, 125)
            });

            tabs.TabPages.Add(tab);

            dtpMonFrom.ValueChanged += (s, e) => LoadMonitoring();
            dtpMonTo.ValueChanged += (s, e) => LoadMonitoring();
            cboMonUser.SelectedIndexChanged += (s, e) => LoadMonitoring();
            chkMonFlaggedOnly.CheckedChanged += (s, e) => LoadMonitoring();
            txtMonSearch.TextChanged += (s, e) => LoadMonitoring();

            LoadMonUsers();
            LoadMonitoring();
        }

        private void LoadMonUsers()
        {
            try
            {
                DataTable dt = Db.Pull("SELECT username FROM users ORDER BY username");
                cboMonUser.Items.Clear();
                cboMonUser.Items.Add("All users");
                foreach (DataRow r in dt.Rows) cboMonUser.Items.Add(r["username"].ToString());
                cboMonUser.SelectedIndex = 0;
            }
            catch { /* degrade quietly — the filter just stays empty */ }
        }

        private void LoadMonitoring()
        {
            if (dgvMonitor == null) return;
            try
            {
                var ps = new List<MySqlParameter>
                {
                    new MySqlParameter("@from", dtpMonFrom.Value.Date),
                    new MySqlParameter("@to", dtpMonTo.Value.Date)
                };

                string userClause = "";
                if (cboMonUser.SelectedIndex > 0)
                {
                    userClause = "AND Username = @u";
                    ps.Add(new MySqlParameter("@u", cboMonUser.SelectedItem.ToString()));
                }

                string flagClause = chkMonFlaggedOnly.Checked ? "AND FlagReason <> ''" : "";

                string searchClause = "";
                string term = txtMonSearch.Text.Trim();
                if (term.Length > 0)
                {
                    searchClause = "AND (Details LIKE @q OR Area LIKE @q OR Username LIKE @q OR RecordRef LIKE @q)";
                    ps.Add(new MySqlParameter("@q", "%" + term + "%"));
                }

                // audit_log = every create/update/delete/login/logout the app writes (Data/Audit.cs).
                // marriage_history = requirement-level status moves; only WAIVED rows are surfaced
                // here as a bypass (Verified/Submitted/Rejected/Missing are ordinary progress, not
                // an overbypass of a requirement).
                string sql =
                    "SELECT * FROM (" +
                    "  SELECT a.created_at AS EventAt, COALESCE(u.username,'—') AS Username, " +
                    "         COALESCE(u.full_name,'—') AS FullName, COALESCE(u.role,'—') AS Role, " +
                    "         a.action AS ActionType, COALESCE(a.table_name,'—') AS Area, " +
                    "         COALESCE(a.record_id,'—') AS RecordRef, COALESCE(a.details,'') AS Details, " +
                    "         CASE WHEN a.action = 'Login' AND a.details LIKE 'Failed sign-in%' THEN 'Failed sign-in' " +
                    "              WHEN a.details LIKE '%Requirements override%' THEN 'Requirement bypass' " +
                    "              ELSE '' END AS FlagReason, " +
                    "         COALESCE(w.window_name,'—') AS WindowName, " +
                    "         a.table_name AS TableRaw, a.record_id AS RecordRaw " +
                    "  FROM audit_log a LEFT JOIN users u ON u.id = a.user_id " +
                    "                   LEFT JOIN windows w ON w.id = a.window_id " +
                    "  UNION ALL " +
                    "  SELECT h.created_at, COALESCE(u2.username,'—'), COALESCE(u2.full_name,'—'), COALESCE(u2.role,'—'), " +
                    "         'Bypass' AS ActionType, h.entity AS Area, CAST(h.entity_id AS CHAR) AS RecordRef, " +
                    "         CONCAT(h.event, ': ', COALESCE(h.details,'no reason given'), " +
                    "                ' [', COALESCE(h.from_status,'—'), ' -> Waived]') AS Details, " +
                    "         'Requirement waived' AS FlagReason, " +
                    "         '—' AS WindowName, NULL AS TableRaw, NULL AS RecordRaw " +
                    "  FROM marriage_history h LEFT JOIN users u2 ON u2.id = h.user_id " +
                    "  WHERE h.to_status = 'Waived'" +
                    ") x " +
                    "WHERE DATE(EventAt) BETWEEN @from AND @to " + userClause + " " + flagClause + " " + searchClause +
                    " ORDER BY EventAt DESC LIMIT 1000";

                DataTable dt = Db.Pull(sql, ps.ToArray());
                dgvMonitor.DataSource = dt;

                if (dgvMonitor.Columns.Contains("EventAt"))
                {
                    dgvMonitor.Columns["EventAt"].HeaderText = "Date & Time";
                    dgvMonitor.Columns["EventAt"].DefaultCellStyle.Format = "MMM d, yyyy  h:mm:ss tt";
                }
                if (dgvMonitor.Columns.Contains("Username")) dgvMonitor.Columns["Username"].HeaderText = "Username";
                if (dgvMonitor.Columns.Contains("FullName")) dgvMonitor.Columns["FullName"].HeaderText = "Full Name";
                if (dgvMonitor.Columns.Contains("Role")) dgvMonitor.Columns["Role"].HeaderText = "Role";
                if (dgvMonitor.Columns.Contains("ActionType")) dgvMonitor.Columns["ActionType"].HeaderText = "Action";
                if (dgvMonitor.Columns.Contains("Area")) dgvMonitor.Columns["Area"].HeaderText = "Area / Table";
                if (dgvMonitor.Columns.Contains("RecordRef")) dgvMonitor.Columns["RecordRef"].HeaderText = "Record";
                if (dgvMonitor.Columns.Contains("Details")) dgvMonitor.Columns["Details"].HeaderText = "Details (transaction / client)";
                if (dgvMonitor.Columns.Contains("FlagReason")) dgvMonitor.Columns["FlagReason"].HeaderText = "Flag";
                if (dgvMonitor.Columns.Contains("WindowName")) dgvMonitor.Columns["WindowName"].HeaderText = "Window";
                if (dgvMonitor.Columns.Contains("TableRaw")) dgvMonitor.Columns["TableRaw"].Visible = false;
                if (dgvMonitor.Columns.Contains("RecordRaw")) dgvMonitor.Columns["RecordRaw"].Visible = false;

                lblMonCount.Text = dt.Rows.Count + " event(s) — double-click a row for details" +
                    (dt.Rows.Count == 1000 ? " (showing the most recent 1000 — narrow the date range for the rest)" : "");
            }
            catch (Exception ex)
            {
                dgvMonitor.DataSource = null;
                lblMonCount.Text = "Could not load activity: " + ex.Message;
            }
        }

        /// <summary>Tints a flagged row so an override/waiver/failed sign-in stands out at a glance.</summary>
        private void DgvMonitor_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvMonitor.Columns.Contains("FlagReason")) return;
            object flagVal = dgvMonitor.Rows[e.RowIndex].Cells["FlagReason"].Value;
            string flag = flagVal?.ToString() ?? "";
            if (flag.Length == 0) return;

            e.CellStyle.BackColor = Color.FromArgb(255, 243, 224);
            e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9);
            if (dgvMonitor.Columns[e.ColumnIndex].Name == "FlagReason" && e.Value != null)
                e.Value = "⚠ " + e.Value;
        }

        private void DgvMonitor_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowRowDetail(dgvMonitor.Rows[e.RowIndex]);
        }

        /// <summary>
        /// Row-detail popup: record type, window, client name and (for a payment row)
        /// the payment that was handled. Resolved from TableRaw/RecordRaw — the raw
        /// table_name/record_id behind that audit_log row — by a per-table lookup,
        /// since audit_log itself only ever stores a table + numeric id, never a name.
        /// </summary>
        private void ShowRowDetail(DataGridViewRow row)
        {
            string tableRaw = row.Cells["TableRaw"].Value?.ToString() ?? "";
            string recordRawStr = row.Cells["RecordRaw"].Value?.ToString() ?? "";
            string action = row.Cells["ActionType"].Value?.ToString() ?? "";
            string username = row.Cells["Username"].Value?.ToString() ?? "—";
            string fullName = row.Cells["FullName"].Value?.ToString() ?? "—";
            string role = row.Cells["Role"].Value?.ToString() ?? "—";
            string windowName = row.Cells["WindowName"].Value?.ToString() ?? "—";
            string eventAt = row.Cells["EventAt"].Value is DateTime dt ? dt.ToString("MMM d, yyyy  h:mm:ss tt") : "—";
            string details = row.Cells["Details"].Value?.ToString() ?? "";
            string flag = row.Cells["FlagReason"].Value?.ToString() ?? "";

            string recordType = FriendlyTableName(tableRaw);
            string clientName = "—";
            string paymentInfo = null;

            int recId;
            if (!string.IsNullOrEmpty(tableRaw) && int.TryParse(recordRawStr, out recId))
                ResolveClientAndPayment(tableRaw, recId, out clientName, out paymentInfo);

            var sb = new StringBuilder();
            sb.AppendLine("Date & Time:   " + eventAt);
            sb.AppendLine("Action:        " + action);
            sb.AppendLine("User:          " + fullName + " (" + username + ")  ·  " + role);
            sb.AppendLine("Window:        " + windowName);
            sb.AppendLine("Record type:   " + recordType);
            sb.AppendLine("Client served: " + clientName);
            if (paymentInfo != null)
            {
                sb.AppendLine();
                sb.AppendLine("Payment handled:");
                sb.AppendLine(paymentInfo);
            }
            if (flag.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("⚠ Flag: " + flag);
            }
            if (details.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Details: " + details);
            }

            MessageBox.Show(sb.ToString(), "Activity Detail", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string FriendlyTableName(string tableRaw)
        {
            switch (tableRaw)
            {
                case "births": return "Birth Registration";
                case "deaths": return "Death Registration";
                case "marriages": return "Marriage Registration";
                case "marriage_licenses": return "Marriage License";
                case "transactions": return "Transaction";
                case "certificate_requests": return "Certificate Request";
                case "releases": return "Release / Claim";
                case "payments": return "Payment";
                case "petitions": return "Petition";
                case "claim_requests": return "Claim Request";
                case "breqs_requests": return "PSA Copy Request (BREQS)";
                case "queue_tickets": return "Queue Ticket";
                case "users": return "User Account";
                case "windows": return "Service Window";
                case "": return "—";
                case null: return "—";
                default: return tableRaw;
            }
        }

        /// <summary>
        /// Best-effort client-name (and, for payments, payment-detail) lookup for one
        /// audit_log row. Every table needs its own query since there is no common
        /// "name" column across births/marriages/deaths/transactions/etc. — a table
        /// this doesn't recognise (users, windows, master-file lookups, ...) just
        /// reports "—", since there is no client involved.
        /// </summary>
        private static void ResolveClientAndPayment(string tableRaw, int recordId, out string clientName, out string paymentInfo)
        {
            clientName = "—";
            paymentInfo = null;
            try
            {
                switch (tableRaw)
                {
                    case "births":
                        clientName = MonScalar(
                            "SELECT TRIM(CONCAT(first_name,' ',COALESCE(middle_name,''),' ',last_name)) FROM births WHERE id=@id",
                            recordId);
                        break;
                    case "deaths":
                        clientName = MonScalar(
                            "SELECT COALESCE(NULLIF(full_name,''), TRIM(CONCAT(first_name,' ',COALESCE(middle_name,''),' ',last_name))) FROM deaths WHERE id=@id",
                            recordId);
                        break;
                    case "marriages":
                        clientName = MonScalar(
                            "SELECT CONCAT(TRIM(CONCAT(husband_first_name,' ',husband_last_name)), ' & ', " +
                            "              TRIM(CONCAT(wife_first_name,' ',wife_last_name))) FROM marriages WHERE id=@id",
                            recordId);
                        break;
                    case "marriage_licenses":
                        clientName = MonScalar(
                            "SELECT CONCAT(COALESCE(husband_first_name,'?'), ' & ', COALESCE(wife_first_name,'?')) FROM marriage_licenses WHERE id=@id",
                            recordId);
                        break;
                    case "transactions":
                        clientName = MonScalar("SELECT client_name FROM transactions WHERE id=@id", recordId);
                        break;
                    case "certificate_requests":
                        clientName = MonScalar(
                            "SELECT t.client_name FROM certificate_requests c LEFT JOIN transactions t ON t.id=c.transaction_id WHERE c.id=@id",
                            recordId);
                        break;
                    case "releases":
                        clientName = MonScalar("SELECT claimant_name FROM releases WHERE id=@id", recordId);
                        break;
                    case "petitions":
                        clientName = MonScalar(
                            "SELECT CASE p.record_type " +
                            "  WHEN 'Birth' THEN (SELECT TRIM(CONCAT(first_name,' ',last_name)) FROM births WHERE id=p.record_id) " +
                            "  WHEN 'Death' THEN (SELECT full_name FROM deaths WHERE id=p.record_id) " +
                            "  WHEN 'Marriage' THEN (SELECT TRIM(CONCAT(husband_last_name,' & ',wife_last_name)) FROM marriages WHERE id=p.record_id) " +
                            "  ELSE '—' END " +
                            "FROM petitions p WHERE p.id=@id",
                            recordId);
                        break;
                    case "claim_requests":
                        clientName = MonScalar(
                            "SELECT COALESCE(NULLIF(TRIM(CONCAT(COALESCE(id_first_name,''),' ',COALESCE(id_last_name,''))),''), 'Not yet identified') " +
                            "FROM claim_requests WHERE id=@id",
                            recordId);
                        break;
                    case "payments":
                        var row = Db.Pull(
                            "SELECT t.client_name, p.or_number, p.payment_method, p.reference_no, " +
                            "       p.gross_amount, p.additional_fee, p.net_amount, p.amount_tendered, p.change_amount " +
                            "FROM payments p LEFT JOIN transactions t ON t.id = p.transaction_id WHERE p.id=@id",
                            new MySqlParameter("@id", recordId));
                        if (row.Rows.Count > 0)
                        {
                            var r = row.Rows[0];
                            clientName = r["client_name"] == DBNull.Value ? "—" : r["client_name"].ToString();
                            paymentInfo =
                                "  OR No.:      " + (r["or_number"] == DBNull.Value ? "—" : r["or_number"]) + "\r\n" +
                                "  Method:      " + r["payment_method"] + "\r\n" +
                                "  Reference:   " + (r["reference_no"] == DBNull.Value ? "—" : r["reference_no"]) + "\r\n" +
                                "  Document fee:" + string.Format("  ₱{0:N2}", r["gross_amount"]) + "\r\n" +
                                "  Additional:  " + string.Format("  ₱{0:N2}", r["additional_fee"]) + "\r\n" +
                                "  Total paid:  " + string.Format("  ₱{0:N2}", r["net_amount"]);
                        }
                        break;
                }
                if (string.IsNullOrWhiteSpace(clientName)) clientName = "—";
            }
            catch { clientName = "—"; }
        }

        private static string MonScalar(string sql, int id)
        {
            var dt = Db.Pull(sql, new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return "—";
            string v = dt.Rows[0][0].ToString().Trim();
            return v.Length == 0 ? "—" : v;
        }

        private void ExportMonitoringCsv()
        {
            var dt = dgvMonitor.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Nothing to export.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "activity_" + dtpMonFrom.Value.ToString("yyyyMMdd") + "_" + dtpMonTo.Value.ToString("yyyyMMdd") + ".csv"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                var sb = new StringBuilder();
                for (int c = 0; c < dt.Columns.Count; c++)
                    sb.Append(c == 0 ? "" : ",").Append(MonCsv(dt.Columns[c].ColumnName));
                sb.AppendLine();
                foreach (DataRow row in dt.Rows)
                {
                    for (int c = 0; c < dt.Columns.Count; c++)
                        sb.Append(c == 0 ? "" : ",").Append(MonCsv(row[c].ToString()));
                    sb.AppendLine();
                }
                File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Exported to " + sfd.FileName, "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string MonCsv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static Label Cap2(string text, int x, int y) => new Label
        {
            Text = text, AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(108, 117, 125)
        };

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
            // No prompt here any more: this screen opens on General, which needs no
            // verification. ShowPage() asks the moment an administrative page is opened.
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
            // ------------------------------------------------------------ Getting started
            new ManualTopic("Getting Started", "What CROMS does",
                "CROMS follows one client from the front desk to release:\n" +
                "1. The client takes a queue number at the kiosk.\n" +
                "2. A window accepts the ticket and calls the client.\n" +
                "3. The request is processed (registration, certificate request, PSA copy, petition...).\n" +
                "4. The fee is paid at the Treasury and the O.R. number is recorded in Fees & Payments.\n" +
                "5. The certificate is handed over in Release & Claim.\n" +
                "Every step is written to the audit trail. The sidebar groups the modules the same way: " +
                "Transactions, Civil Registration, Petitions & Cases, Records & Documents, Reports, System."),
            new ManualTopic("Getting Started", "Which screens can I see?",
                "Every signed-in role sees the same operational modules (queue, certificates, PSA copies, " +
                "release, fees, birth/marriage/death, case tracking, record search, document processing, reports).\n" +
                "Only an Administrator also sees Records Archive and Settings (users, master files, forms & " +
                "templates, windows, audit trail, app updates).\n" +
                "Use the Collapse button at the bottom of the sidebar to shrink it to an icon rail."),

            // ------------------------------------------------------------ Login
            new ManualTopic("Login", "How to log in",
                "1. Launch CROMS. The Sign In window appears.\n" +
                "2. Enter your username and password, then click Sign In (or press Enter). The eye button " +
                "shows or hides the password.\n" +
                "3. After 5 wrong attempts that username is locked for 60 seconds.\n" +
                "4. On your first sign-in, or after an administrator resets your password, you must choose " +
                "a new password (at least 8 characters) before you continue.\n" +
                "Accounts are created by an administrator under Settings > Users & Access."),
            new ManualTopic("Login", "How to select your service window",
                "After signing in, the Window Assignment screen lists every Active window with its status:\n" +
                "• Available - free to take.\n" +
                "• In use by <name> - occupied by another operator; it is greyed out.\n" +
                "• Priority - handles ALL transactions.\n" +
                "Pick your window, choose the transactions it handles, then click Start Serving. " +
                "Skip for now enters CROMS without a window (for administrators or monitoring). " +
                "Log out returns to the sign-in screen. You configure only YOUR window."),
            new ManualTopic("Login", "How to choose transaction types",
                "On the right of Window Assignment, tick the services your window will handle " +
                "(New Registration, CTC, Marriage, Death, Petition, Verification, ...). " +
                "Several windows may handle the same service - that is allowed and lets the queue clear faster.\n" +
                "A Priority window handles every transaction, keeps its ticket until it is completed, and " +
                "cannot forward tickets without administrator approval.\n" +
                "Your choices are saved when you press Start Serving, Skip for now or Log out."),

            // ------------------------------------------------------------ Queue
            new ManualTopic("Queue Management", "The screen at a glance",
                "Top: live clock and four tiles - Waiting now, Average wait, Served today, Longest wait.\n" +
                "Middle: the Now Serving card for your window (an administrator with no window sees every window).\n" +
                "Toolbar: Call Next, Call Client, Recall, Forward, and a line that states the NEXT step in words.\n" +
                "Below: the Priority Lane table (Senior/PWD/Pregnant, served first) and the regular queue. " +
                "Use the service chips and the search box to filter both; Export CSV saves what is on screen.\n" +
                "Only the buttons that apply to the current ticket are enabled."),
            new ManualTopic("Queue Management", "Calling the next client",
                "1. Press Call Next. CROMS picks the oldest waiting ticket (priority lane first) that your " +
                "window may handle and ACCEPTS it. The client is not yet on the public board.\n" +
                "2. A checklist appears with the documents to get ready for this client's services. Tick them " +
                "as you locate them - it is guidance, not a lock.\n" +
                "3. Press Call Client (or click your window card). The ticket becomes Serving, the number is " +
                "announced by voice and shown on the public display.\n" +
                "This lets you find the documents BEFORE the client stands at the counter."),
            new ManualTopic("Queue Management", "Processing services (Client Tasks)",
                "After the client is called, the Client Tasks panel shows who is at the window and what they " +
                "asked for: name, contact, priority lane, the ID they will present, and each service with the " +
                "details entered at the kiosk.\n" +
                "Open each task to work on it (it opens the right module, e.g. Certificate Request, prefilled " +
                "from the kiosk). Mark a task complete when it is done. Press Complete Client Visit when all " +
                "tasks are finished - the ticket closes and your window is freed for Call Next."),
            new ManualTopic("Queue Management", "Recall, Forward and Abandon",
                "Recall - calls the same number again (client stepped away) and counts the recall.\n" +
                "Forward - moves the ticket's next service to another window that handles it; the queue " +
                "number stays the same.\n" +
                "Abandon This Task / Abandon All Tasks - use when the client leaves or the request cannot " +
                "be done today. A reason is REQUIRED and is stored; nothing is deleted. Abandon All ends the " +
                "visit and frees the window."),
            new ManualTopic("Queue Management", "Priority lane and pausing",
                "Senior citizens, PWD and pregnant clients are tagged at the kiosk and appear in the Priority " +
                "Lane table, which is served ahead of the regular queue (RA 11261).\n" +
                "Pause Queue stops Call Next from issuing new tickets until you resume."),

            // ------------------------------------------------------------ Certificates
            new ManualTopic("Certificates & Releasing", "Certificate Request (CTC)",
                "Flow: Create request > Find & Print the certificate > Payment > Release.\n" +
                "1. Enter the requester, choose the certificate type and the record (type part of the name to " +
                "search). Opened from the queue, the form is prefilled from what the client typed at the kiosk.\n" +
                "2. Create Request opens the Find/Print window. Print the certificate on the printer.\n" +
                "3. Proceed to Payment sends it to Fees & Payments. Or use No-show - Put to Waiting-to-Release " +
                "if the client left; the Call/Recall Client button announces the number first."),
            new ManualTopic("Certificates & Releasing", "PSA Copies (BREQS)",
                "For certificates issued by PSA. Status flow: Requested > Paid > Submitted to PSA > Received " +
                "from PSA > Released (or No Record at PSA / Cancelled).\n" +
                "• New request captures the certificate type, copies and the details of the record.\n" +
                "• Record the Treasury O.R. number when paid, then Submit to PSA.\n" +
                "• Receive & scan PSA copy attaches the scan and checks that the name matches the request.\n" +
                "• Release asks for the claimant and ID. Details are locked once sent to PSA.\n" +
                "Overdue at PSA and Unclaimed are flagged automatically on the tiles."),
            new ManualTopic("Certificates & Releasing", "Release & Claim",
                "Pick a request from the list on the left; the workspace shows only what applies to its state:\n" +
                "• Waiting / awaiting print - press Send to Payment.\n" +
                "• For Payment - opens Fees & Payments.\n" +
                "• For Release - enter the claimant (tick 'representative' for ID type and number), compare the " +
                "kiosk photo with the uploaded ID, optionally take a camera photo, then Verify & Release.\n" +
                "• Released - read-only receipt.\n" +
                "Find by number: type a queue number or transaction code and press Enter. Use Scan claim QR " +
                "for clients who uploaded their ID from the claim app."),
            new ManualTopic("Certificates & Releasing", "Claim QR and ID upload",
                "A claim request prints a QR code. The client scans it with their phone camera to open the " +
                "claim app and upload a photo of their valid ID. The releasing officer then sees the uploaded " +
                "ID beside the kiosk photo in Release & Claim. CROMS never matches faces automatically - the " +
                "officer compares them."),

            // ------------------------------------------------------------ Fees
            new ManualTopic("Fees & Payments", "Recording a payment",
                "Collection is done by the Municipal Treasury, which issues the Official Receipt. CROMS " +
                "records it.\n" +
                "1. Awaiting payment tab - pick the request; the assessed fee (fee x copies) is shown.\n" +
                "2. Choose the payment method. Cash needs the amount tendered; GCash and Bank Transfer need a " +
                "reference number.\n" +
                "3. Enter the Official Receipt number (required) and press Record Payment.\n" +
                "The request then moves to For Release. Print Receipt prints a 58mm payment slip - it is NOT an " +
                "Official Receipt."),
            new ManualTopic("Fees & Payments", "Walk-in payments, log and schedule",
                "Walk-in / other payment - record a payment that does not come from a CROMS request: payer, " +
                "purpose, one or more fee lines, method, O.R. number.\n" +
                "Payment log - every payment from every source, filter by date and export CSV.\n" +
                "Monthly collection - the month's total by fee, by source and by method.\n" +
                "Fee schedule - the fees from the office fee card. Changing an amount is recorded in the audit " +
                "trail. A fee with no amount is typed in by the cashier."),

            // ------------------------------------------------------------ Civil registration
            new ManualTopic("Civil Registration", "Birth Registration (Form 102)",
                "New Birth opens the entry form; the step strip (Child, Mother, Father, Marriage of Parents, " +
                "Attendant, Informant, Certification) shows your progress and the panel on the right lists " +
                "what is still missing.\n" +
                "• Required: child's first and last name and sex. Save Draft keeps an unfinished record " +
                "(no registry number); Submit assigns the registry number.\n" +
                "• Place of birth: choose Country, then Province, City/Municipality. Foreign births type the place.\n" +
                "• 'Parents married?' - switch off to grey out the marriage date and place.\n" +
                "• A birth registered more than 30 days late is flagged Delayed. Use Delayed Registration for " +
                "its checklist and 10-day posting.\n" +
                "Print Certificate saves first if needed, then prints on the form."),
            new ManualTopic("Civil Registration", "Marriage: application, licence and registration",
                "Marriage Registration is the desk for the whole flow:\n" +
                "1. Application (Form 90) - both applicants, parents, consent (age 18-20) or advice (age 21-25), " +
                "and previous marriage if widowed/annulled. Under-18 cannot be filed.\n" +
                "2. Posting - a 10-day posting period; the licence can be issued from day 11. Missing advice " +
                "defers issue by three months.\n" +
                "3. Issue - after the requirements and the Treasury O.R. are recorded. The licence is valid " +
                "120 days.\n" +
                "4. Register the marriage (Form 97) by picking the issued licence, then attach the scan.\n" +
                "5. Copies and PSA transmittal are recorded on the marriage record.\n" +
                "Print application prints Municipal Form 90; Consent and Advice forms print from the licence."),
            new ManualTopic("Civil Registration", "Death Registration (Form 103)",
                "Enter the deceased (last, first, middle name), date and place of death, cause of death, " +
                "disposal, informant and the certification block. Use the Medical & Permits tab for the " +
                "medical items and permit numbers.\n" +
                "Register Death assigns the registry number. Print Certificate prints on Form 103 and can " +
                "also print the Burial / Transfer Permit; Facts Certification (Form 2A) is under the same menu. " +
                "View Softcopy opens the saved scan."),
            new ManualTopic("Civil Registration", "Softcopy scan and printing",
                "A scanned certificate saved with a record can be reopened with View Softcopy and reprinted " +
                "exactly as scanned. Certificates print through the Crystal Reports layout when the runtime is " +
                "installed and otherwise through the built-in layout on the same blank form.\n" +
                "Administrators can adjust the letterhead, text and fields under Settings > Forms & Templates."),

            // ------------------------------------------------------------ Cases and records
            new ManualTopic("Cases & Records", "Case Tracking (petitions)",
                "Tracks RA 9048 and RA 10172 petitions, legitimation, supplemental reports, legal instruments " +
                "and court orders. Choose the case type, the record it concerns and the filing date, then " +
                "Advance Stage as it progresses.\n" +
                "RA 9048/10172 run Filed > Posted > Decision > PSA Endorsement. The other types run Filed > " +
                "Under Review > Decision > PSA Endorsement.\n" +
                "Case Documents attaches the supporting papers (e.g. decision and certificate of finality " +
                "for a court order). CROMS tracks the case; it does not apply the correction to the record."),
            new ManualTopic("Cases & Records", "Record Search",
                "One search across births, marriages and deaths. Type part of a name or a registry number. " +
                "Tick Sound-alike matching to find spelling variants (Dela Cruz / de la Cruz). Filter by " +
                "register with the buttons above the results.\n" +
                "Select a result to see its registry book, page, registry year and form revision on the right. " +
                "Double-click it to open the record in its registration screen."),
            new ManualTopic("Cases & Records", "Records Archive (administrators)",
                "A read-only browser over everything saved, grouped by category (civil registry, marriage " +
                "licensing, petitions and cases, certificates and PSA copies, claims and releases, front desk). " +
                "View Full Record shows every column and a button for each stored scan or photo."),
            new ManualTopic("Cases & Records", "Document Processing (OCR)",
                "Load a scanned certificate, or open a scan sent from the mobile scanner. CROMS reads the form, " +
                "identifies the type and fills a review grid with a confidence for each field.\n" +
                "• Orange or red rows need checking - handwriting and poor scans are not read reliably; type " +
                "them in. Editing a value re-checks it immediately.\n" +
                "• Preview on Form shows the values on the certificate (watermarked, not an official copy).\n" +
                "• Commit to Birth Registry saves a birth. Auto-Fill opens the marriage/death screen with the " +
                "values. Send to Manual Review holds a doubtful scan.\n" +
                "Nothing is saved until you confirm."),

            // ------------------------------------------------------------ Reports
            new ManualTopic("Reports", "Reports & Analytics",
                "Tabs: PSA / Statutory, Fees & Collections, Birth, Death, Marriage, Queuing, Certificates. " +
                "A tab loads only when you open it. Charts show 'not enough data' instead of guessing when a " +
                "field is empty in the records.\n" +
                "PSA / Statutory - choose the month and year and Generate the monthly report of registered " +
                "births, marriages and deaths with the timely-vs-delayed split and a roster; Export CSV for " +
                "the submission packet.\n" +
                "Print Assessment Report prints the tab's figures. On the Birth tab, Customize Report chooses " +
                "which charts are included."),
            new ManualTopic("Reports", "Transaction History",
                "A searchable, read-only ledger of every transaction from queue to release. Filter by status " +
                "and search by code or client name."),

            // ------------------------------------------------------------ Settings
            new ManualTopic("Settings", "The Settings pages",
                "Settings (administrators only) lists its pages on the left:\n" +
                "General - office identity, database status and who is signed in.\n" +
                "Users & Access - create users, set roles, reset passwords, activate or deactivate accounts. " +
                "Passwords need at least 8 characters and a reset forces a new password at next sign-in.\n" +
                "Master Files - the dropdown lists (barangays, hospitals, religions...). Search to find a " +
                "value; the country's provinces, cities and barangays are already loaded.\n" +
                "Forms & Templates - logo and stamp, certificate templates, print alignment, and which " +
                "certificate forms have a Crystal report.\n" +
                "Window Management, Audit Trail, App Updates and this User Manual."),
            new ManualTopic("Settings", "Managing service windows",
                "Settings > Window Management. The first time you open it you re-enter your administrator " +
                "password; the controls stay unlocked for the session.\n" +
                "• Add Window: name, optional description, Active/Inactive, and Priority type if it should " +
                "handle every transaction.\n" +
                "• Edit, Enable, Disable, Move Up/Down. The Active? tick box switches one window on or off.\n" +
                "• Activate All / Deactivate All switch every window; busy windows are skipped.\n" +
                "• Delete is blocked while an operator is signed in or a ticket is assigned.\n" +
                "Changes show immediately on the login picker, dashboard, queue board and the public display."),
            new ManualTopic("Settings", "Online and offline windows",
                "Active/Inactive is set by an administrator and decides whether a window exists on the board. " +
                "Online/Offline shows whether an operator is signed in (the app confirms every 30 seconds). " +
                "An Inactive window never receives tickets; an Active but Offline window has nobody at it yet.\n" +
                "The Dashboard 'Service Windows' panel shows each window's state, current ticket, priority " +
                "type and assigned services, refreshed automatically."),
            new ManualTopic("Settings", "Audit Trail",
                "Every create, update, delete, sign-in and failed sign-in is recorded with the user, time and " +
                "details. Filter by date range and user, search the text, tick 'Flagged only' for bypasses and " +
                "failed sign-ins, double-click a row for the full entry, and Export CSV."),
            new ManualTopic("Settings", "App Updates",
                "On the server PC, Publish New Release to Clients makes the latest build available on the " +
                "network share (administrator password required). On any other PC, Check for Updates & Install " +
                "downloads it, restarts CROMS and keeps you signed in. Kiosk and Display apps are installed " +
                "from the same release."),

            // ------------------------------------------------------------ Other apps
            new ManualTopic("Other Apps", "Client kiosk",
                "The client takes a number in two steps: choose the services (a service is greyed out when no " +
                "window handles it), then enter their name, priority, valid ID type and photo, and scan the QR " +
                "with their phone to upload the ID. A queue ticket prints with the number and the people ahead. " +
                "If no window is online the kiosk shows that the office is unavailable."),
            new ManualTopic("Other Apps", "Mobile scanner and public display",
                "Mobile scanner: the Dashboard shows a QR; open it on a phone on the same Wi-Fi, accept the " +
                "one-time certificate warning, and photograph a certificate. It is sent to Document " +
                "Processing for reading - the phone does not save records.\n" +
                "Public display (CROMS.Display) shows only the queue number being served at each online window."),

            // ------------------------------------------------------------ FAQ
            new ManualTopic("FAQ", "Why can't I log in?",
                "Check the spelling (the message is the same for a wrong user or wrong password). After 5 " +
                "failed tries wait 60 seconds. A deactivated account must be re-activated by an administrator " +
                "in Settings > Users & Access."),
            new ManualTopic("FAQ", "Why is my window unavailable?",
                "It is either Inactive or already occupied by another signed-in operator. Choose another " +
                "window or ask an administrator to enable one."),
            new ManualTopic("FAQ", "Why can't I call the next ticket?",
                "The queue is empty or paused, your window is not Online, or no waiting ticket needs a " +
                "transaction your window handles. The on-screen message says which."),
            new ManualTopic("FAQ", "Why is a button greyed out?",
                "CROMS only enables what applies right now. For example Call Client is enabled only when a " +
                "ticket is accepted, and Recall/Forward only while a ticket is being served. The line under " +
                "the queue toolbar tells you the next step."),
            new ManualTopic("FAQ", "How do I complete a transaction?",
                "Work each task in Client Tasks, mark it complete, then press Complete Client Visit. For a " +
                "certificate the full path is Create > Print > Pay (O.R. number) > Verify & Release."),
            new ManualTopic("FAQ", "A client left before paying or claiming",
                "In Certificate Request use 'No-show - Put to Waiting-to-Release'. The request appears on the " +
                "Waiting to Release tab of Release & Claim. When the client returns, take their old queue " +
                "number at the kiosk (Release & Claim) so they are served first, then Resume - Send to Payment."),
            new ManualTopic("FAQ", "How do I add a new service window?",
                "An administrator goes to Settings > Window Management > Add Window, fills in the details and " +
                "saves. It appears everywhere automatically."),
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
