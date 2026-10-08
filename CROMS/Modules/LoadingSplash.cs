using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// The "CROMS is starting" window shown while the app connects to the database, starts its
    /// services and builds the main window. Before this, nothing at all was on screen for those
    /// seconds and the next window simply popped up.
    ///
    /// It runs on its OWN UI thread with its own message loop, because the work it covers
    /// (database search, server start, building MainForm) blocks the main thread - a splash on the
    /// main thread would freeze mid-animation, which looks worse than no splash.
    /// </summary>
    public static class LoadingSplash
    {
        private static readonly object _lock = new object();

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam; public uint time; public int x; public int y; }
        [DllImport("user32.dll")] private static extern int GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);
        [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG msg);
        [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref MSG msg);
        [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
        private static SplashForm _form;
        private static Thread _thread;

        /// <summary>Shows the splash (or just changes its line when it is already up).</summary>
        public static void Show(string status)
        {
            lock (_lock)
            {
                if (_form != null) { SetStatus(status); return; }

                var ready = new ManualResetEvent(false);
                _thread = new Thread(() =>
                {
                    var f = new SplashForm(status);
                    f.Shown += (s, e) => ready.Set();
                    f.FormClosed += (s, e) => ready.Set();
                    _form = f;
                    try
                    {
                        // NOT Application.Run: when a second thread's Application.Run ends, WinForms
                        // raises Application.ApplicationExit (measured), and Program hooks that event
                        // to stop the save-API and mobile servers. A plain message pump does not.
                        f.FormClosed += (s, e) => PostQuitMessage(0);
                        f.Show();
                        MSG msg;
                        while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
                        {
                            TranslateMessage(ref msg);
                            DispatchMessage(ref msg);
                        }
                        f.Dispose();
                    }
                    catch { ready.Set(); }
                });
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.IsBackground = true;
                _thread.Name = "CROMS splash";
                _thread.Start();
                ready.WaitOne(3000);
            }
        }

        public static void SetStatus(string status)
        {
            SplashForm f = _form;
            if (f == null) return;
            try
            {
                if (f.IsHandleCreated && !f.IsDisposed)
                    f.BeginInvoke(new Action(() => f.Status = status));
            }
            catch { }
        }

        /// <summary>Fades the splash out. <paramref name="wait"/> blocks (briefly) until it is gone,
        /// which a caller about to show another window wants; a window that is itself fading in
        /// passes false so its own animation is not held up.</summary>
        public static void Close(bool wait = true)
        {
            SplashForm f;
            Thread t;
            lock (_lock)
            {
                f = _form; t = _thread;
                _form = null; _thread = null;
            }
            if (f == null) return;
            try
            {
                if (f.IsHandleCreated && !f.IsDisposed)
                    f.BeginInvoke(new Action(f.FadeOutAndClose));
            }
            catch { }
            if (wait && t != null) t.Join(1500);
        }

        private sealed class SplashForm : Form
        {
            private string _status;
            private readonly System.Windows.Forms.Timer _anim = new System.Windows.Forms.Timer { Interval = 16 };
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private readonly Image _logo;
            private bool _closing;
            private long _fadeStart;

            private readonly Font _title = new Font("Segoe UI", 26f, FontStyle.Bold);
            private readonly Font _sub = new Font("Segoe UI", 10f);
            private readonly Font _office = new Font("Segoe UI", 9f);
            private readonly Font _statusFont = new Font("Segoe UI", 9.75f);

            public SplashForm(string status)
            {
                _status = status ?? "";
                _logo = LoadLogo();
                Text = "CROMS";
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                ClientSize = new Size(520, 300);
                BackColor = UiTheme.Surface;
                ShowInTaskbar = true;
                DoubleBuffered = true;
                Opacity = 0;
                _anim.Tick += (s, e) => Tick();
            }

            public string Status
            {
                get => _status;
                set { _status = value ?? ""; Invalidate(); }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ClassStyle |= 0x00020000;   // CS_DROPSHADOW: a soft edge instead of a flat box
                    return cp;
                }
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                _anim.Start();
            }

            public void FadeOutAndClose()
            {
                if (_closing) return;
                _closing = true;
                _fadeStart = _clock.ElapsedMilliseconds;
            }

            private void Tick()
            {
                long now = _clock.ElapsedMilliseconds;
                if (_closing)
                {
                    double p = (now - _fadeStart) / 160.0;
                    if (p >= 1) { _anim.Stop(); Close(); return; }
                    Opacity = Math.Max(0, 1 - p);
                }
                else if (Opacity < 1)
                {
                    Opacity = Math.Min(1, now / 180.0);
                }
                Invalidate(new Rectangle(0, 236, ClientSize.Width, 30));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                int w = ClientSize.Width, h = ClientSize.Height;

                // Navy band with the seal and name.
                using (var band = new SolidBrush(UiTheme.Navy))
                    g.FillRectangle(band, 0, 0, w, 150);

                var seal = new Rectangle(36, 39, 72, 72);
                if (_logo != null)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(seal);
                        Region old = g.Clip;
                        g.SetClip(path);
                        g.DrawImage(_logo, seal);
                        g.Clip = old;
                    }
                }
                else
                {
                    using (var b = new SolidBrush(UiTheme.NavyHover)) g.FillEllipse(b, seal);
                    TextRenderer.DrawText(g, "LCRO", _sub, seal, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                TextRenderer.DrawText(g, "CROMS", _title, new Point(124, 38), Color.White, TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "Civil Registry Operations Management System", _sub,
                    new Point(128, 86), Color.FromArgb(196, 206, 227), TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "LGU Pe\u00F1ablanca, Cagayan \u2014 Local Civil Registry Office", _office,
                    new Point(128, 108), Color.FromArgb(150, 163, 189), TextFormatFlags.NoPrefix);

                // What is happening right now, in words.
                TextRenderer.DrawText(g, _status, _statusFont, new Rectangle(36, 190, w - 72, 24),
                    UiTheme.Ink, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                // Indeterminate progress: a segment gliding along a track. It never claims a
                // percentage, because none of the steps it covers can report one honestly.
                var track = new Rectangle(36, 244, w - 72, 6);
                using (var tb = new SolidBrush(UiTheme.Chrome)) FillRound(g, tb, track);
                double t = (_clock.ElapsedMilliseconds % 1400) / 1400.0;
                double ease = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                int seg = track.Width / 3;
                int x = track.Left - seg + (int)((track.Width + seg) * ease);
                var bar = Rectangle.Intersect(track, new Rectangle(x, track.Top, seg, track.Height));
                if (bar.Width > 0)
                    using (var ab = new SolidBrush(UiTheme.Accent)) FillRound(g, ab, bar);

                using (var line = new Pen(UiTheme.CardLine)) g.DrawRectangle(line, 0, 0, w - 1, h - 1);
            }

            private static void FillRound(Graphics g, Brush b, Rectangle r)
            {
                int d = Math.Min(r.Height, r.Width);
                if (d <= 1) { g.FillRectangle(b, r); return; }
                using (var p = new GraphicsPath())
                {
                    p.AddArc(r.Left, r.Top, d, d, 90, 180);
                    p.AddArc(r.Right - d, r.Top, d, d, 270, 180);
                    p.CloseFigure();
                    g.FillPath(b, p);
                }
            }

            // Own copy of the seal: GDI+ images are not thread-safe, and BrandAssets.Logo is drawn by
            // the main thread at the same time.
            private static Image LoadLogo()
            {
                try
                {
                    string path = Path.Combine(Application.StartupPath, "Assets", "lcro_logo.png");
                    if (!File.Exists(path)) return null;
                    using (var src = Image.FromFile(path)) return new Bitmap(src);
                }
                catch { return null; }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _anim.Dispose();
                    _logo?.Dispose();
                    _title.Dispose(); _sub.Dispose(); _office.Dispose(); _statusFont.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// Fades a top-level window in when it first appears, so it arrives instead of popping.
    /// Attached from Program only (not in the forms' constructors), so test harnesses that keep a
    /// form invisible with Opacity 0 are not made visible by it.
    /// </summary>
    public static class FormFade
    {
        public static T In<T>(T form, int ms = 200) where T : Form
        {
            form.Opacity = 0;
            form.Shown += (s, e) =>
            {
                form.Activate();
                var sw = Stopwatch.StartNew();
                var t = new System.Windows.Forms.Timer { Interval = 15 };
                t.Tick += (a, b) =>
                {
                    double p = sw.ElapsedMilliseconds / (double)ms;
                    if (p >= 1 || form.IsDisposed)
                    {
                        t.Stop(); t.Dispose();
                        if (!form.IsDisposed) form.Opacity = 1;
                        return;
                    }
                    form.Opacity = 1 - Math.Pow(1 - p, 3);   // ease out
                };
                t.Start();
            };
            return form;
        }
    }
}
