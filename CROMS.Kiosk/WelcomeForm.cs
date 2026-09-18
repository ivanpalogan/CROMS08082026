using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// The kiosk's attract / "touch to start" screen, shown between clients.
    /// <para/>
    /// Its real job is a provable session boundary: the flow only leaves this screen on a
    /// deliberate tap, so a client can never walk up to the previous person's half-made
    /// selections still on screen. (Step 1 also drops back here after
    /// <see cref="KioskCore.IdleSecondsSelect"/> of inactivity.) The gentle pulse on the
    /// call-to-action is the standard kiosk cue that the screen is interactive at all.
    /// </summary>
    public partial class WelcomeForm : Form
    {
        private Timer _pulse;
        private Timer _availability;
        private float _phase;
        private bool _open = true;   // office has at least one window online
        // Set by every DELIBERATE close (Next / Back / idle / submit) so OnFormClosing can tell
        // our own navigation apart from the operator really quitting the kiosk. CloseReason is
        // NOT usable for this: a programmatic Close() also reports UserClosing, and WinForms
        // auto-assigns DialogResult.Cancel when the X is clicked, so neither one discriminates.
        private bool _navigating;


        public WelcomeForm()
        {
            InitializeComponent();

            _cta.Paint += Cta_Paint;

            // Owner-drawn; the CTA repaints ~25x/sec for its pulse, so without
            // double-buffering it tears badly (the same trap the launcher cards hit).
            DoubleBuffer(_cta);

            // Any tap anywhere starts a session — the whole screen is the button.
            WireStart(this);

            Resize += (s, e) => CenterContent();
            Shown += (s, e) => CenterContent();
            Load += (s, e) => UpdateAvailability();

            _availability = new Timer { Interval = 4000 };
            _availability.Tick += (s, e) => UpdateAvailability();
            _availability.Start();

            _pulse = new Timer { Interval = 40 };
            _pulse.Tick += (s, e) => { _phase += 0.055f; _cta.Invalidate(); };
            _pulse.Start();
        }

        private static void DoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true);
        }

        /// <summary>Recursively makes every control start the session, so there is no dead spot.</summary>
        private void WireStart(Control root)
        {
            root.Click += Start_Click;
            foreach (Control c in root.Controls) WireStart(c);
        }

        private void Start_Click(object sender, EventArgs e)
        {
            if (!_open) return;            // office closed — tapping does nothing
            _navigating = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CenterContent()
        {
            _center.Left = Math.Max(0, (ClientSize.Width - _center.Width) / 2);
            _center.Top = Math.Max(0, (ClientSize.Height - _center.Height) / 2);
        }

        private void UpdateAvailability()
        {
            bool open = KioskCore.OfficeOnline();
            if (open == _open && _lblStatus.Text.Length > 0) return;
            _open = open;
            _lblStatus.Text = open
                ? "The office is open. Tap the screen to begin."
                : "The office is currently unavailable. Please try again later.";
            _lblStatus.ForeColor = open ? Color.FromArgb(91, 100, 114) : Color.FromArgb(160, 40, 50);
            _lblHeadline.Text = open ? "Welcome" : "Currently Closed";
            _cta.Visible = open;
            Cursor = open ? Cursors.Hand : Cursors.Default;
        }

        // ------------------------------------------------------------------ painting
        private void Cta_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_cta.Parent.BackColor);

            // 0..1 sine so the pulse eases at both ends instead of ping-ponging linearly.
            float t = (float)((Math.Sin(_phase) + 1.0) / 2.0);

            var pill = new Rectangle(10, 10, _cta.Width - 21, _cta.Height - 21);
            int radius = pill.Height / 2 - 1;

            // Soft halo that breathes outward — the "I am interactive" cue. Drawn as a few
            // stacked low-alpha rings rather than one ring whose SIZE animates: an integer
            // spread stepping 4->11px reads as a jerky jump, whereas fading alpha is smooth.
            for (int i = 0; i < 3; i++)
            {
                int spread = 4 + i * 4;
                int a = (int)(20 * (1f - t) * (1f - i * 0.3f));
                if (a <= 0) continue;
                using (var hb = new SolidBrush(Color.FromArgb(a, KioskCore.Accent)))
                using (var hp = Rounded(new Rectangle(pill.X - spread, pill.Y - spread,
                                                      pill.Width + spread * 2, pill.Height + spread * 2),
                                        radius + spread))
                    g.FillPath(hb, hp);
            }

            using (var path = Rounded(pill, radius))
            using (var b = new SolidBrush(HoverFade.Lerp(KioskCore.Accent, KioskCore.AccentHover, t * 0.35f)))
                g.FillPath(b, path);

            using (var f = new Font("Segoe UI", 15F, FontStyle.Bold))
                TextRenderer.DrawText(g, "Touch anywhere to start", f, pill, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _pulse?.Stop();
            _availability?.Stop();
            // A hard window close (Alt+F4 / X) quits the kiosk, same as the step forms.
            if (!_navigating && e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
            base.OnFormClosing(e);
        }
    }
}
