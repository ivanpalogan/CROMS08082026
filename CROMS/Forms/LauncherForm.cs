using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>What the operator chose on the launcher.</summary>
    public enum LauncherChoice { Exit, Admin, Display, Kiosk, All }

    /// <summary>
    /// Startup chooser shown when CROMS.exe runs: pick which app to open on THIS computer —
    /// Admin (this app), the public Display board, or the client Kiosk — or "Run All Three".
    /// Admin runs in this process; Display/Kiosk are launched as their own sibling .exe.
    /// Lets one machine (dev / demo) decide exactly what opens instead of everything at once.
    /// <para/>
    /// Visual style matches LoginForm: Navy Blue (light), rounded cards, minimal single-color
    /// line icons (no emoji) — see Modules/UiTheme.cs for the palette used app-wide.
    /// </summary>
    public sealed class LauncherForm : Form
    {
        public LauncherChoice Choice { get; private set; } = LauncherChoice.Exit;

        private static readonly Color PageBg   = Color.FromArgb(244, 246, 249);  // #F4F6F9
        private static readonly Color CardBg   = Color.White;
        private static readonly Color CardHover = Color.FromArgb(247, 249, 252);
        private static readonly Color HostLine = Color.FromArgb(225, 229, 236);  // #E1E5EC
        private static readonly Color Ink      = Color.FromArgb(23, 26, 36);     // #171B24
        private static readonly Color SoftInk  = Color.FromArgb(91, 100, 114);   // #5B6472
        private static readonly Color Accent   = Color.FromArgb(29, 78, 216);    // #1D4ED8
        private static readonly Color AccentTint = Color.FromArgb(234, 241, 254);// #EAF1FE
        private static readonly Color Teal     = Color.FromArgb(11, 140, 130);   // #0B8C82
        private static readonly Color TealTint = Color.FromArgb(227, 243, 241);
        private static readonly Color Green    = Color.FromArgb(46, 148, 87);    // #2E9457
        private static readonly Color GreenTint = Color.FromArgb(233, 245, 236);
        private static readonly Color Navy     = Color.FromArgb(19, 36, 65);     // #132441

        public LauncherForm()
        {
            Text = "CROMS Launcher";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false; MaximizeBox = false;
            ClientSize = new Size(560, 600);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 10F);

            // Brand mark, top-left — same navy badge + wordmark as the Login screen.
            var logo = new Panel { Location = new Point(40, 28), Size = new Size(28, 28) };
            logo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(PageBg);
                FillRounded(e.Graphics, logo.ClientRectangle, Navy, 7);
                DrawBuildingIcon(e.Graphics, new RectangleF(6, 6, 16, 16), Color.White);
            };
            Controls.Add(logo);
            Controls.Add(new Label
            {
                Text = "CROMS", Location = new Point(76, 33), AutoSize = true,
                ForeColor = Ink, Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            });

            Controls.Add(new Label
            {
                Text = "Choose what to open", Location = new Point(0, 72), Size = new Size(560, 32),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Ink,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold)
            });
            Controls.Add(new Label
            {
                Text = "Select the app to run on this computer.",
                Location = new Point(0, 106), Size = new Size(560, 20),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = SoftInk,
                Font = new Font("Segoe UI", 10F)
            });

            int y = 140;
            AddOption("Admin System", "Queue, records, releases, reports — the staff workbench.",
                Accent, AccentTint, DrawGridIcon, y, primary: true, () => Pick(LauncherChoice.Admin)); y += 110;
            AddOption("Queue Display", "The public “Now Serving” board for the waiting area.",
                Teal, TealTint, DrawMonitorIcon, y, primary: false, () => Pick(LauncherChoice.Display)); y += 110;
            AddOption("Client Kiosk", "The self-service touch kiosk that issues queue tickets.",
                Green, GreenTint, DrawTicketIcon, y, primary: false, () => Pick(LauncherChoice.Kiosk)); y += 110;

            var all = new Button
            {
                Text = "Run All Three",
                Location = new Point(40, y + 18), Size = new Size(480, 44),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(238, 240, 244),
                ForeColor = SoftInk, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter
            };
            all.FlatAppearance.BorderSize = 0;
            all.FlatAppearance.MouseOverBackColor = Color.FromArgb(228, 231, 237);
            all.Click += (s, e) => Pick(LauncherChoice.All);
            Controls.Add(all);

            var exit = new Button
            {
                Text = "Exit",
                Location = new Point(180, y + 76), Size = new Size(200, 32),
                FlatStyle = FlatStyle.Flat, BackColor = PageBg, ForeColor = SoftInk,
                Font = new Font("Segoe UI", 9.5F), Cursor = Cursors.Hand
            };
            exit.FlatAppearance.BorderSize = 0;
            exit.FlatAppearance.MouseOverBackColor = Color.FromArgb(233, 236, 239);
            exit.Click += (s, e) => { Choice = LauncherChoice.Exit; DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(exit);
        }

        private void Pick(LauncherChoice c) { Choice = c; DialogResult = DialogResult.OK; Close(); }

        /// <summary>
        /// A clickable card: icon chip + bold title + description + a colored left accent bar.
        /// The primary (Admin) card gets an accent-colored outline so it reads as the default pick.
        /// </summary>
        private void AddOption(string title, string desc, Color accent, Color tint,
            Action<Graphics, RectangleF, Color> icon, int y, bool primary, Action onClick)
        {
            var card = new Panel
            {
                Location = new Point(40, y), Size = new Size(480, 96),
                BackColor = CardBg, Cursor = Cursors.Hand
            };
            // Owner-drawn animation flickers on a plain Panel unless it's double-buffered —
            // same reflection trick UiTheme.RoundButton uses for the same reason.
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(card, true);

            // Smooth hover: bg tint, border color/weight, and a gentle upward nudge of the
            // content all ease in over ~180ms instead of snapping — see Modules/HoverFade.cs.
            // Idle state is the SAME for every card (thin neutral border) so nothing looks
            // pre-selected; only the actually-hovered card lights up.
            float hoverT = 0f;
            HoverFade.Attach(card, 180, t => { hoverT = t; card.Invalidate(); });

            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(PageBg);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                Color bgNow = HoverFade.Lerp(CardBg, CardHover, hoverT);
                Color borderNow = HoverFade.Lerp(HostLine, accent, hoverT);
                float borderW = HoverFade.Lerp(1f, 1.6f, hoverT);
                using (var path = RoundedRect(rect, 10))
                {
                    using (var fill = new SolidBrush(bgNow)) g.FillPath(fill, path);
                    using (var pen = new Pen(borderNow, borderW)) g.DrawPath(pen, path);
                }
                using (var stripe = RoundedRect(new Rectangle(0, 8, 4, card.Height - 16), 2))
                using (var b = new SolidBrush(accent))
                    g.FillPath(b, stripe);

                // Content eases up by 1.5px at full hover — a subtle "lift" without moving
                // the card's own bounds (which would risk clipping/overlap with its neighbors).
                float lift = HoverFade.Lerp(0f, 1.5f, hoverT);

                var chip = new Rectangle(20, (int)((card.Height - 40) / 2 - lift), 40, 40);
                FillRounded(g, chip, tint, 9);
                icon(g, new RectangleF(chip.X + 12, chip.Y + 12, 16, 16), accent);

                using (var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold))
                    g.DrawString(title, titleFont, new SolidBrush(Ink), new PointF(76, 18f - lift));
                using (var descFont = new Font("Segoe UI", 9F))
                using (var descBrush = new SolidBrush(SoftInk))
                    g.DrawString(desc, descFont, descBrush, new RectangleF(78, 44f - lift, 384, 36));

                // Quiet "Recommended" tag on the default pick — a label, not a glowing border,
                // so it can't be mistaken for an active/hover state.
                if (primary)
                {
                    string tag = "Recommended";
                    using (var tagFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
                    {
                        SizeF tsz = g.MeasureString(tag, tagFont);
                        var pill = new Rectangle(card.Width - (int)tsz.Width - 34, 14, (int)tsz.Width + 18, 20);
                        FillRounded(g, pill, tint, 10);
                        var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                        using (var tb = new SolidBrush(accent))
                            g.DrawString(tag, tagFont, tb, pill, fmt);
                    }
                }
            };

            card.Click += (s, e) => onClick();
            Controls.Add(card);
        }

        // ---------------------------------------------------------- minimal line icons
        private static void DrawGridIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.6f))
            {
                float gap = r.Width * 0.18f;
                float cell = (r.Width - gap) / 2f;
                DrawSquare(g, pen, new RectangleF(r.X, r.Y, cell, cell), 2f);
                DrawSquare(g, pen, new RectangleF(r.X + cell + gap, r.Y, cell, cell), 2f);
                DrawSquare(g, pen, new RectangleF(r.X, r.Y + cell + gap, cell, cell), 2f);
                DrawSquare(g, pen, new RectangleF(r.X + cell + gap, r.Y + cell + gap, cell, cell), 2f);
            }
        }
        private static void DrawSquare(Graphics g, Pen pen, RectangleF r, float radius)
        {
            using (var path = RoundedRect(Rectangle.Round(r), (int)radius))
                g.DrawPath(pen, path);
        }

        private static void DrawMonitorIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.6f))
            {
                var screen = new RectangleF(r.X, r.Y, r.Width, r.Height * 0.66f);
                using (var path = RoundedRect(Rectangle.Round(screen), 2))
                    g.DrawPath(pen, path);
                float cx = r.X + r.Width / 2f;
                float standTop = screen.Bottom;
                float standBottom = r.Bottom;
                g.DrawLine(pen, cx, standTop, cx, standTop + (standBottom - standTop) * 0.45f);
                g.DrawLine(pen, cx - r.Width * 0.22f, standBottom, cx + r.Width * 0.22f, standBottom);
            }
        }

        private static void DrawTicketIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.6f))
            {
                using (var path = RoundedRect(Rectangle.Round(r), 3))
                    g.DrawPath(pen, path);
                float cx = r.X + r.Width * 0.58f;
                float dash = 2.4f, gap = 2.2f, yy = r.Y + 2f;
                while (yy < r.Bottom - 2f)
                {
                    g.DrawLine(pen, cx, yy, cx, Math.Min(yy + dash, r.Bottom - 2f));
                    yy += dash + gap;
                }
            }
        }

        private static void DrawBuildingIcon(Graphics g, RectangleF r, Color stroke)
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

        private static void FillRounded(Graphics g, Rectangle r, Color color, int radius)
        {
            using (var path = RoundedRect(r, radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ------------------------------------------------------------ static API
        /// <summary>Shows the chooser and returns the operator's choice.</summary>
        public static LauncherChoice Ask()
        {
            using (var f = new LauncherForm())
            {
                f.ShowDialog();
                return f.Choice;
            }
        }

        /// <summary>Starts a sibling app (CROMS.Display / CROMS.Kiosk) as its own process.</summary>
        public static void Start(string projectName)
        {
            string exe = ResolveExe(projectName);
            if (exe == null)
            {
                MessageBox.Show(
                    "Could not find " + projectName + ".exe.\r\n\r\nLooked in:\r\n  " + Application.StartupPath +
                    "\r\n\r\nOn a client PC all three apps must sit in the SAME folder - copy " +
                    projectName + ".exe and its files next to CROMS.exe. Scripts\\Build-Bundle.ps1 makes that folder.",
                    "CROMS Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) }); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start " + projectName + ": " + ex.Message,
                    "CROMS Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Finds a sibling app's .exe. Tries the same folder (an all-in-one deployment), then
        /// the dev solution layout &lt;solution&gt;\&lt;project&gt;\bin\&lt;config&gt;\&lt;project&gt;.exe.
        /// </summary>
        private static string ResolveExe(string projectName)
        {
            string exeName = projectName + ".exe";
            string here = Application.StartupPath;   // ...\CROMS\bin\Debug

            string sameFolder = Path.Combine(here, exeName);
            if (File.Exists(sameFolder)) return sameFolder;

            // Sibling-folder deployment (what AppUpdater installs into):
            //   ...\CROMS\Main\CROMS.exe + ...\CROMS\Kiosk\CROMS.Kiosk.exe + ...\Display\...
            try
            {
                string parent = Directory.GetParent(here)?.FullName;
                if (parent != null)
                {
                    string atParent = Path.Combine(parent, exeName);
                    if (File.Exists(atParent)) return atParent;
                    foreach (string d in Directory.GetDirectories(parent))
                    {
                        string p = Path.Combine(d, exeName);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { }

            // Dev layout: up from <project>\bin\<config> to the solution root, then across.
            try
            {
                string config = new DirectoryInfo(here).Name;                 // Debug / Release
                string solutionRoot = Directory.GetParent(here)?.Parent?.Parent?.FullName;
                if (solutionRoot != null)
                {
                    string devPath = Path.Combine(solutionRoot, projectName, "bin", config, exeName);
                    if (File.Exists(devPath)) return devPath;
                    // Fall back to the other config if the chosen one isn't built.
                    foreach (string cfg in new[] { "Debug", "Release" })
                    {
                        string p = Path.Combine(solutionRoot, projectName, "bin", cfg, exeName);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { /* fall through to null */ }
            return null;
        }
    }
}
