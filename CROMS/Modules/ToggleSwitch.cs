using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// A small sliding on/off toggle (like a dark-mode / light-mode switch): a rounded
    /// pill track with a round knob that slides from left (Off) to right (On) and animates
    /// the colour + knob position. Drop-in replacement for a Yes/No combo — expose
    /// <see cref="Checked"/> and the <see cref="CheckedChanged"/> event.
    /// </summary>
    public class ToggleSwitch : Control
    {
        private bool _checked;
        private float _pos;                 // 0 (off, knob left) .. 1 (on, knob right)
        private readonly Timer _anim;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(followSize(), 28);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;

            _anim = new Timer { Interval = 15 };
            _anim.Tick += Animate;
        }

        private static int followSize() => 52;   // default width

        /// <summary>Colour of the track when On (default: a modern green).</summary>
        public Color OnColor { get; set; } = Color.FromArgb(25, 135, 84);

        /// <summary>Colour of the track when Off (default: light grey).</summary>
        public Color OffColor { get; set; } = Color.FromArgb(206, 212, 218);

        /// <summary>Colour of the sliding knob.</summary>
        public Color KnobColor { get; set; } = Color.White;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                _anim.Start();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Set the state WITHOUT raising CheckedChanged (for programmatic sync).</summary>
        public void SetCheckedSilently(bool value)
        {
            _checked = value;
            _pos = value ? 1f : 0f;
            Invalidate();
        }

        private void Animate(object sender, EventArgs e)
        {
            float target = _checked ? 1f : 0f;
            float step = 0.18f;
            if (Math.Abs(_pos - target) <= step) { _pos = target; _anim.Stop(); }
            else _pos += _pos < target ? step : -step;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int h = Height - 1;
            int w = Width - 1;
            int pad = 3;
            int knob = h - pad * 2;

            // Blend the track colour between Off and On by the current slide position.
            Color track = Blend(OffColor, OnColor, _pos);
            using (var b = new SolidBrush(track))
            using (var path = Pill(new Rectangle(0, 0, w, h)))
                g.FillPath(b, path);

            // Knob slides across the track.
            int travel = w - pad * 2 - knob;
            int kx = pad + (int)Math.Round(travel * _pos);
            var knobRect = new Rectangle(kx, pad, knob, knob);
            using (var sb = new SolidBrush(KnobColor))
                g.FillEllipse(sb, knobRect);
            using (var pen = new Pen(Color.FromArgb(30, 0, 0, 0)))
                g.DrawEllipse(pen, knobRect);
        }

        private static GraphicsPath Pill(Rectangle r)
        {
            int d = r.Height;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 90, 180);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            p.CloseFigure();
            return p;
        }

        private static Color Blend(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _anim.Dispose();
            base.Dispose(disposing);
        }
    }
}
