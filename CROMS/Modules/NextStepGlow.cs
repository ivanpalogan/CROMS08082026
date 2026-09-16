using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// A shared pulsing highlight for "this is the one thing to click next" — used to walk a
    /// first-time operator through a multi-step workflow (Call Next -&gt; Call Client -&gt; click
    /// the window card) without adding new controls: whichever button or card is currently the
    /// valid next action just glows.
    ///
    /// One Timer drives every glow on screen, so N highlighted controls cost one Tick, not N.
    /// The ring is drawn INSET along the control's own edge rather than extending past its
    /// bounds, so it needs no extra padding/host panel and works on a Button (owner-drawn by
    /// <see cref="UiTheme"/>) or a <see cref="CardPanel"/> (owner-drawn itself) without
    /// disturbing either's layout.
    /// </summary>
    public static class NextStepGlow
    {
        private static readonly Timer _timer = new Timer { Interval = 45 };
        private static double _phase;
        private static readonly Dictionary<Control, int> _radius = new Dictionary<Control, int>();
        private static readonly HashSet<Control> _active = new HashSet<Control>();

        static NextStepGlow()
        {
            _timer.Tick += (s, e) =>
            {
                _phase += 0.11;
                if (_phase > Math.PI * 2) _phase -= Math.PI * 2;
                foreach (Control c in _active)
                    if (!c.IsDisposed && c.IsHandleCreated && c.Visible) c.Invalidate();
            };
        }

        /// <summary>
        /// Registers a control to draw the glow ring on top of its normal paint. Call once,
        /// right after the control is created; use <see cref="SetActive"/> every time its
        /// on/off state should change (it starts off).
        /// </summary>
        public static void Wire(Control c, int cornerRadius)
        {
            DoubleBuffer(c);
            _radius[c] = cornerRadius;
            c.Paint += (s, e) => DrawGlow(c, e.Graphics);
            c.Disposed += (s, e) => { _active.Remove(c); _radius.Remove(c); };
        }

        /// <summary>Turns the pulse on or off for a wired control.</summary>
        public static void SetActive(Control c, bool active)
        {
            if (active) _active.Add(c);
            else _active.Remove(c);
            if (!c.IsDisposed) c.Invalidate();

            if (_active.Count > 0 && !_timer.Enabled) _timer.Start();
            else if (_active.Count == 0 && _timer.Enabled) _timer.Stop();
        }

        private static void DrawGlow(Control c, Graphics g)
        {
            if (!_active.Contains(c)) return;

            double t = (Math.Sin(_phase) + 1) / 2; // breathes 0..1
            int alpha = 90 + (int)(t * 130);
            float width = 2f + (float)(t * 1.3);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            int r;
            if (!_radius.TryGetValue(c, out r)) r = 8;
            var rect = new Rectangle(1, 1, c.Width - 3, c.Height - 3);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (GraphicsPath path = CardPanel.RoundedRect(rect, r))
            using (var pen = new Pen(Color.FromArgb(Math.Min(255, alpha), UiTheme.Accent), width))
                g.DrawPath(pen, path);
        }

        private static void DoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }
    }
}
