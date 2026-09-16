using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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
            SetupNavIcons();
            BuildUserBar();
            ApplyRoleAccess();   // also fills _navAllowed
            SetupGroupAccordion();
            SetupSidebarRail();
            SetupBrandMark();
            DarkenSidebarScrollbar();
            UiTheme.PolishButtons(this);   // hand cursor + hover on nav + header buttons
            StartWindowHeartbeat();
            FormClosed += (s, e) => _lcroLogo?.Dispose();
            // Open on the first module (Dashboard) by default.
            if (ModuleRegistry.All.Count > 0)
                ShowModule(ModuleRegistry.All[0].Key);
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        /// <summary>
        /// The nav list's own scrollbar is the OS's default light one, which reads as a stray
        /// white stripe against the navy sidebar. This is the standard Win10/11 trick for a
        /// dark-mode scrollbar on a plain control's own non-client area — best-effort only
        /// (older Windows builds just keep the light scrollbar; nothing else breaks either way).
        /// </summary>
        private void DarkenSidebarScrollbar()
        {
            if (navFlow.IsHandleCreated) SetWindowTheme(navFlow.Handle, "DarkMode_Explorer", null);
            else navFlow.HandleCreated += (s, e) => SetWindowTheme(navFlow.Handle, "DarkMode_Explorer", null);
        }

        // ================================================================
        //  Brand mark — the LCRO seal beside "CROMS".
        // ================================================================

        private Panel _brandMark;
        private Image _lcroLogo;
        private Label _brandSubtitle;

        /// <summary>
        /// Loads the office's own seal from Assets\lcro_logo.png (next to the built exe) when one
        /// has been supplied there; falls back to a drawn line-art mark (the same one Login and
        /// Launcher already use) so the sidebar never shows a blank hole while waiting for the
        /// office to hand over the real image. Drop the PNG in and it appears on the next launch
        /// — no rebuild needed. Laid out as seal + "CROMS" + "LCRO Peñablanca" (matching the
        /// approved mockup's brand block), not just a bare wordmark.
        /// </summary>
        private void SetupBrandMark()
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "Assets", "lcro_logo.png");
            try { if (System.IO.File.Exists(path)) _lcroLogo = Image.FromFile(path); }
            catch { _lcroLogo = null; }

            brandPanel.Height = 68;

            var mark = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(14, 14),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            mark.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(brandPanel.BackColor);
                if (_lcroLogo != null)
                {
                    using (var clip = new GraphicsPath())
                    {
                        clip.AddEllipse(0, 0, mark.Width - 1, mark.Height - 1);
                        e.Graphics.SetClip(clip);
                        e.Graphics.DrawImage(_lcroLogo, 0, 0, mark.Width, mark.Height);
                        e.Graphics.ResetClip();
                    }
                }
                else
                {
                    using (GraphicsPath path2 = CardPanel.RoundedRect(
                               new Rectangle(0, 0, mark.Width - 1, mark.Height - 1), 10))
                    using (var b = new SolidBrush(UiTheme.NavyHover))
                        e.Graphics.FillPath(b, path2);
                    DrawInstitutionIcon(e.Graphics, new RectangleF(9, 9, 22, 22), Color.White);
                }
            };
            brandPanel.Controls.Add(mark);
            mark.BringToFront();

            brandLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            brandLabel.Location = new Point(mark.Right + 10, 14);

            _brandSubtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 162, 188),
                BackColor = Color.Transparent,
                Text = "LCRO Peñablanca",
                Location = new Point(mark.Right + 10, 38)
            };
            brandPanel.Controls.Add(_brandSubtitle);
            _brandSubtitle.BringToFront();

            _brandMark = mark;
        }

        /// <summary>Plain line-art municipal building — matches LoginForm/LauncherForm's mark.</summary>
        private static void DrawInstitutionIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.5f))
            {
                float cx = r.X + r.Width / 2f;
                float roofBaseY = r.Y + r.Height * 0.32f;
                float baseY = r.Bottom - r.Height * 0.06f;
                float halfW = r.Width * 0.44f;

                g.DrawLine(pen, cx, r.Y, cx - halfW, roofBaseY);
                g.DrawLine(pen, cx, r.Y, cx + halfW, roofBaseY);
                g.DrawLine(pen, cx - halfW, roofBaseY, cx + halfW, roofBaseY);

                float colTop = roofBaseY + r.Height * 0.08f;
                float[] colXs = { cx - halfW * 0.55f, cx, cx + halfW * 0.55f };
                foreach (float x in colXs) g.DrawLine(pen, x, colTop, x, baseY);

                g.DrawLine(pen, cx - halfW - 1.5f, baseY, cx + halfW + 1.5f, baseY);
            }
        }

        // ================================================================
        //  Nav icons — one line-art glyph per module key, drawn by the shared
        //  button painter (Modules/UiTheme.SetIcon).
        // ================================================================

        private readonly Dictionary<Button, int> _navButtonHeight = new Dictionary<Button, int>();
        private readonly Dictionary<Button, int> _navButtonWidth = new Dictionary<Button, int>();
        private readonly Dictionary<Button, string> _navButtonText = new Dictionary<Button, string>();

        // Side margin for an expanded nav button. Small on purpose -- see SetupNavIcons.
        private const int NavMargin = 5;

        private void SetupNavIcons()
        {
            // The Designer sizes every nav button 204 wide with an 8+8 Margin, i.e. exactly the
            // 220px sidebar. So the moment the list is long enough to need a VERTICAL scrollbar,
            // the 17px it takes leaves 203px of viewport for 220px of content and a HORIZONTAL
            // scrollbar appears too -- that is the dark strip cutting through the nav captions,
            // and it eats another 17px off the bottom, which is what pushed the last button
            // (Certificate Templates) under the Collapse bar. Size the buttons to the viewport
            // that is actually left once the vertical scrollbar is there.
            int navWidth = SidebarExpandedWidth - SystemInformation.VerticalScrollBarWidth - NavMargin * 2;

            foreach (var pair in _navButtons)
            {
                Button b = pair.Value;
                // Tighter side margin than the Designer's 8: every pixel given back here is a
                // pixel of caption, and at 8 the longest labels ("Marriage Registration",
                // "Intelligent Document Processing") ellipsised once the width had to shrink to
                // clear the vertical scrollbar.
                b.Margin = new Padding(NavMargin, 1, NavMargin, 1);
                b.Width = navWidth;
                // The Designer text carries manual leading spaces ("   Dashboard") as a stand-in
                // indent for the icon that didn't exist yet — the icon now provides that gap.
                b.Text = (b.Text ?? "").TrimStart();
                _navButtonText[b] = b.Text;
                _navButtonHeight[b] = b.Height;
                _navButtonWidth[b] = b.Width;
                UiTheme.SetIcon(b, NavIcons.For(pair.Key));
            }

            // A little air under the last button so it never sits flush against the bottom-pinned
            // Collapse bar.
            navFlow.Padding = new Padding(0, 0, 0, 10);

            // A group header is an AutoSize Label: left to itself its preferred width can exceed
            // the scrolled viewport and re-trigger the horizontal scrollbar on its own, so cap it
            // to the same content width the buttons use.
            foreach (Control c in navFlow.Controls)
            {
                var lbl = c as Label;
                if (lbl == null) continue;
                lbl.AutoSize = false;
                lbl.AutoEllipsis = true;
                lbl.Size = new Size(navWidth - (lbl.Margin.Left - NavMargin), lbl.PreferredHeight);
            }
        }

        // ================================================================
        //  Group accordion — clicking a section header (CLIENT SERVICES, CERTIFICATION, ...)
        //  shows/hides its buttons with a short slide, instead of every group always being
        //  fully expanded down a long scrolling list.
        // ================================================================

        private sealed class NavGroup
        {
            public Label Header;
            public readonly List<Button> Members = new List<Button>();
            public bool Expanded = true;
        }

        private readonly List<NavGroup> _navGroups = new List<NavGroup>();
        // Buttons the signed-in role is actually allowed to see (ApplyRoleAccess already decided
        // this once) — the accordion must never make a role-hidden button visible again.
        private readonly HashSet<Button> _navAllowed = new HashSet<Button>();


        /// <summary>Groups the flat nav list by the Label headers already placed between runs of buttons.</summary>
        private void SetupGroupAccordion()
        {
            NavGroup current = null;
            foreach (Control c in navFlow.Controls)
            {
                if (c is Label lbl)
                {
                    var group = new NavGroup { Header = lbl, Expanded = true };
                    _navGroups.Add(group);
                    current = group;
                    lbl.Cursor = Cursors.Hand;
                    lbl.Click += (s, e) => ToggleGroup(group);
                    lbl.Paint += (s, e) => DrawChevron(lbl, e.Graphics, group);
                }
                else if (c is Button btn && current != null)
                {
                    current.Members.Add(btn);
                }
            }
        }

        private static void DrawChevron(Label lbl, Graphics g, NavGroup group)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float x = lbl.Width - 14, y = lbl.Height - 13;
            using (var pen = new Pen(lbl.ForeColor, 1.6f)
                   { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if (group.Expanded)   // pointing down
                    g.DrawLines(pen, new[] { new PointF(x - 4, y - 2), new PointF(x, y + 2), new PointF(x + 4, y - 2) });
                else                  // pointing right
                    g.DrawLines(pen, new[] { new PointF(x - 2, y - 4), new PointF(x + 2, y), new PointF(x - 2, y + 4) });
            }
        }

        private void ToggleGroup(NavGroup group)
        {
            if (_railCollapsed) return;   // the collapsed icon-rail has no groups to fold
            bool expand = !group.Expanded;
            group.Expanded = expand;
            group.Header.Invalidate();

            var members = new List<Button>();
            foreach (var b in group.Members)
                if (_navAllowed.Contains(b)) members.Add(b);
            AnimateGroup(members, expand);
        }

        /// <summary>
        /// Slides a group open/closed by animating each button's own Height (a FlowLayoutPanel
        /// reflows around whatever height a child currently reports, so this reads as a real
        /// expand/collapse instead of an instant show/hide).
        /// </summary>
        private void AnimateGroup(List<Button> members, bool expand)
        {
            if (members.Count == 0) return;
            if (expand)
                foreach (var b in members) { b.Visible = true; b.Height = 0; }

            var timer = new Timer { Interval = 15 };
            int steps = 8, i = 0;
            timer.Tick += (s, e) =>
            {
                i++;
                float t = Math.Min(1f, (float)i / steps);
                float frac = expand ? t : 1f - t;
                foreach (var b in members)
                {
                    int natural;
                    if (!_navButtonHeight.TryGetValue(b, out natural)) natural = 34;
                    b.Height = Math.Max(expand ? 1 : 0, (int)(natural * frac));
                }
                navFlow.PerformLayout();
                if (i >= steps)
                {
                    timer.Stop();
                    timer.Dispose();
                    foreach (var b in members)
                    {
                        int natural;
                        if (!_navButtonHeight.TryGetValue(b, out natural)) natural = 34;
                        if (expand) b.Height = natural;
                        else { b.Height = 0; b.Visible = false; }
                    }
                    navFlow.PerformLayout();
                }
            };
            timer.Start();
        }

        // ================================================================
        //  Collapse to an icon-only rail — pinned at the BOTTOM of the sidebar (matching the
        //  approved mockup), narrows the sidebar to just the icons and widens the module area,
        //  reacting to screen size the same way as always (both panels stay Dock-based).
        // ================================================================

        private const int SidebarExpandedWidth = 220;
        private const int SidebarCollapsedWidth = 64;
        private bool _railCollapsed;
        private ToolTip _navTip;
        private Button _railButton;

        private void SetupSidebarRail()
        {
            _navTip = new ToolTip();
            // Always on, not just in the rail: at 220px the longest module name ("Intelligent
            // Document Processing") cannot fit whatever the margins are, so hovering has to be
            // able to tell you what the ellipsis is hiding.
            foreach (var pair in _navButtons)
                _navTip.SetToolTip(pair.Value, ModuleTitle(pair.Key));

            _railButton = new Button
            {
                Text = "«  Collapse",
                Dock = DockStyle.Bottom,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.NavyHover,
                ForeColor = NavIdleFore,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _railButton.FlatAppearance.BorderSize = 0;
            _railButton.Click += (s, e) => ToggleRail();
            sidebarPanel.Controls.Add(_railButton);
            // The Dock=Fill child must be laid out LAST or it claims the whole client area and
            // the bottom-docked bar is drawn ON TOP of the end of the list -- measured: navFlow
            // ran 68..1057 while the rail sat 1015..1057, hiding the final nav button. Docking
            // order follows z-order, so bringing navFlow to the front (not the rail) is what
            // makes the rail reserve its 42px first. This is the user-reported "the collapse
            // button even covered a button".
            navFlow.BringToFront();
        }

        // In the rail the buttons drop to a 4+4 Margin, so the widest they can be without
        // provoking a horizontal scrollbar is the 64px sidebar less the vertical scrollbar and
        // those margins. Computed, not a literal 44: at 44 + 8 + 8 = 60 against a 47px viewport
        // the panel grew a horizontal scrollbar and slid the icons out of sight.
        private const int RailMargin = 4;
        private static int RailButtonWidth
        {
            get { return SidebarCollapsedWidth - SystemInformation.VerticalScrollBarWidth - RailMargin * 2; }
        }

        private void ToggleRail()
        {
            if (_railAnimating) return;   // ignore a second click mid-slide
            _railCollapsed = !_railCollapsed;
            int fromWidth = sidebarPanel.Width;
            int toWidth = _railCollapsed ? SidebarCollapsedWidth : SidebarExpandedWidth;

            // Sequencing matters, and getting it wrong is what made the slide look broken:
            // COLLAPSING swaps to icons first, so the panel narrows around content that already
            // fits. EXPANDING waits for the slide to finish before putting the captions back --
            // restoring 204px-wide labelled buttons into a still-64px panel is what clipped the
            // text mid-animation and flashed a scrollbar.
            if (_railCollapsed)
            {
                ApplyRailContent();
                AnimateSidebarWidth(fromWidth, toWidth, null);
            }
            else
            {
                AnimateSidebarWidth(fromWidth, toWidth, ApplyExpandedContent);
            }
        }

        /// <summary>
        /// The rail shows every allowed icon flat, ignoring whatever the accordion state was --
        /// there is no room for section headers at this width, and an icon rail is meant to be a
        /// complete, ungrouped list of everything reachable.
        /// </summary>
        private void ApplyRailContent()
        {
            navFlow.SuspendLayout();
            foreach (var group in _navGroups)
            {
                group.Header.Visible = false;
                foreach (var b in group.Members)
                {
                    if (!_navAllowed.Contains(b)) continue;
                    int natural;
                    b.Height = _navButtonHeight.TryGetValue(b, out natural) ? natural : 34;
                    b.Margin = new Padding(RailMargin, 1, RailMargin, 1);
                    b.Width = RailButtonWidth;
                    b.Visible = true;
                }
            }
            foreach (var pair in _navButtons)
            {
                Button b = pair.Value;
                if (!_navAllowed.Contains(b)) continue;
                _navTip.SetToolTip(b, ModuleTitle(pair.Key));
                b.Text = "";                     // blank text -> UiTheme centres the icon alone
            }
            brandLabel.Visible = false;
            if (_brandSubtitle != null) _brandSubtitle.Visible = false;
            if (_brandMark != null) _brandMark.Location = new Point((SidebarCollapsedWidth - _brandMark.Width) / 2, 14);
            _railButton.Text = "»";
            navFlow.AutoScrollPosition = new Point(0, 0);
            navFlow.ResumeLayout(true);
        }

        private void ApplyExpandedContent()
        {
            navFlow.SuspendLayout();
            foreach (var pair in _navButtons)
            {
                Button b = pair.Value;
                if (!_navAllowed.Contains(b)) continue;
                _navTip.SetToolTip(b, ModuleTitle(pair.Key));
                string original;
                b.Text = _navButtonText.TryGetValue(b, out original) ? original : b.Text;
            }
            foreach (var group in _navGroups)
            {
                group.Header.Visible = true;
                foreach (var b in group.Members)
                {
                    if (!_navAllowed.Contains(b)) continue;
                    int natural, naturalW;
                    b.Height = _navButtonHeight.TryGetValue(b, out natural) ? natural : 34;
                    b.Margin = new Padding(NavMargin, 1, NavMargin, 1);
                    b.Width = _navButtonWidth.TryGetValue(b, out naturalW) ? naturalW : 204;
                    b.Visible = group.Expanded;
                }
            }
            brandLabel.Visible = true;
            if (_brandSubtitle != null) _brandSubtitle.Visible = true;
            if (_brandMark != null) _brandMark.Location = new Point(14, 14);
            _railButton.Text = "«  Collapse";
            navFlow.AutoScrollPosition = new Point(0, 0);
            navFlow.ResumeLayout(true);
        }

        private bool _railAnimating;

        /// <summary>
        /// Slides the sidebar's own width from one size to the other, then runs <paramref name="onDone"/>.
        /// The "animation sucks" complaint was really the button-width bug (icons drawing off the
        /// visible edge of a still-204px-wide button) plus the content/slide ordering above; this
        /// is the polish on top of those fixes, not a substitute for them.
        /// </summary>
        private void AnimateSidebarWidth(int from, int to, Action onDone)
        {
            _railAnimating = true;
            var timer = new Timer { Interval = 12 };
            int steps = 10, i = 0;
            timer.Tick += (s, e) =>
            {
                i++;
                float t = Math.Min(1f, (float)i / steps);
                // Ease-out so the slide decelerates into place instead of stopping dead.
                float eased = 1f - (1f - t) * (1f - t);
                sidebarPanel.Width = (int)(from + (to - from) * eased);
                if (i >= steps)
                {
                    timer.Stop();
                    timer.Dispose();
                    sidebarPanel.Width = to;
                    _railAnimating = false;
                    if (onDone != null) onDone();
                }
            };
            timer.Start();
        }

        private static string ModuleTitle(string key)
        {
            foreach (var m in ModuleRegistry.All) if (m.Key == key) return m.Title;
            return key;
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
        private Panel _userChip;
        private byte[] _userPhotoBytes;
        private const int AvatarSize = 32;

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

            // Owner-drawn chip: round avatar (photo when one is on file, else the user's
            // initials on a navy circle — same fallback the reference mockup uses) + name
            // (bold, ink) + role (accent) stacked underneath, and a caret to hint "clickable".
            var chip = new Panel
            {
                AutoSize = false,
                Size = new Size(230, 44),
                Margin = new Padding(0, 6, 0, 6),
                Cursor = Cursors.Hand,
                BackColor = UiTheme.PageBg
            };
            chip.Paint += (s, e) => PaintUserChip(chip, e.Graphics);
            chip.Click += (s, e) => ShowUserMenu(chip);
            chip.MouseEnter += (s, e) => { chip.BackColor = UiTheme.AccentTint; chip.Invalidate(); };
            chip.MouseLeave += (s, e) => { chip.BackColor = UiTheme.PageBg; chip.Invalidate(); };
            _userChip = chip;
            RefreshUserChipText();   // loads the photo (or clears to initials) before first paint

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
            try { _userPhotoBytes = Session.User != null ? Data.ProfilePhoto.Load(Session.User.Id) : null; }
            catch { _userPhotoBytes = null; }
            _userChip?.Invalidate();
        }

        /// <summary>
        /// Draws the header user chip: a round avatar (photo if one is on file — no upload
        /// screen exists yet, so today this is always the initials fallback — else the user's
        /// initials on a navy circle), name + role, and a caret. Matches the reference mockup's
        /// "small avatar, click the name for a menu" layout.
        /// </summary>
        private void PaintUserChip(Panel chip, Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var path = RoundedRectPath(chip.ClientRectangle, 8))
            using (var brush = new SolidBrush(chip.BackColor))
                g.FillPath(brush, path);

            int cy = (chip.Height - AvatarSize) / 2;
            var avatarRect = new Rectangle(8, cy, AvatarSize, AvatarSize);
            AvatarPainter.Draw(g, avatarRect, _userPhotoBytes, Session.User?.FullName);

            string name = Session.User?.FullName ?? "Not signed in";
            string role = Session.User?.Role ?? "";
            int textX = avatarRect.Right + 10;
            int textW = chip.Width - textX - 18;   // leaves room for the caret
            var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            var roleFont = new Font("Segoe UI", 8F);
            var nameSize = g.MeasureString(name, nameFont);
            var roleSize = g.MeasureString(role, roleFont);
            float totalH = nameSize.Height + roleSize.Height - 2;
            float top = (chip.Height - totalH) / 2f;

            TextRenderer.DrawText(g, name, nameFont,
                new Rectangle(textX, (int)top, textW, (int)nameSize.Height + 2),
                UiTheme.Ink, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (role.Length > 0)
                TextRenderer.DrawText(g, role, roleFont,
                    new Rectangle(textX, (int)(top + nameSize.Height - 2), textW, (int)roleSize.Height + 2),
                    UiTheme.Accent, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            nameFont.Dispose(); roleFont.Dispose();

            // caret
            var caret = new[]
            {
                new PointF(chip.Width - 14, chip.Height / 2f - 2),
                new PointF(chip.Width - 8,  chip.Height / 2f - 2),
                new PointF(chip.Width - 11, chip.Height / 2f + 3)
            };
            using (var caretBrush = new SolidBrush(UiTheme.Muted))
                g.FillPolygon(caretBrush, caret);
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
                using (var f = new Forms.ProfileViewForm())
                    f.ShowDialog(this);
                RefreshUserChipText();   // picks up any name change made from inside the view
            };
            menu.Items.Add(viewProfile);

            var editProfile = new ToolStripMenuItem("Edit Profile");
            editProfile.Click += (s, e) =>
            {
                using (var f = new Forms.StaffBiodataForm())
                    f.ShowDialog(this);
                RefreshUserChipText();
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
            foreach (var pair in _navButtons)
            {
                // Derived from the role map, NEVER read back off Control.Visible: this runs in
                // the constructor, where the form has not been shown yet, so Visible reports
                // EFFECTIVE visibility (false for every child) and _navAllowed came out EMPTY.
                // That is what made the collapsed rail render blank and the accordion do nothing
                // -- both gate on _navAllowed. Same trap already recorded for the Dashboard on
                // 2026-09-07 and the kiosk Others box on 2026-09-10.
                bool ok = allowed == null || allowed.Contains(pair.Key);
                if (allowed != null) pair.Value.Visible = ok;
                if (ok) _navAllowed.Add(pair.Value);
            }
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
