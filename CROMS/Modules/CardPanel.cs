using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// The one card surface the app draws its panels on: a rounded white rectangle with a
    /// hairline border and a soft shadow, painted rather than bordered.
    ///
    /// It exists because <see cref="BorderStyle.FixedSingle"/> — what every panel in this
    /// codebase used — draws a hard 1px system rectangle with square corners in a colour the
    /// palette does not own, which is why the dashboard read as a different application from
    /// the rest of the shell. A painted card also lets the corners blend into the page instead
    /// of leaving white squares, which a clipping Region cannot do (a Region cannot
    /// anti-alias; the same reason <see cref="UiTheme"/> owner-draws its buttons).
    ///
    /// Deliberately general: nothing here knows about the dashboard, so the other modules can
    /// adopt it as they are restyled.
    /// </summary>
    public class CardPanel : Panel
    {
        private int _radius = 10;
        private bool _drawShadow = true;
        private Color _lineColor = UiTheme.CardLine;

        public CardPanel()
        {
            // The dashboard repaints on a 3-second timer, and an unbuffered owner-drawn panel
            // flickers visibly at that rate.
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   // A body panel docked to Fill would otherwise paint an opaque white
                   // rectangle over the card's rounded corners and its border, putting the
                   // square edge straight back. Transparent children let the card show through.
                   | ControlStyles.SupportsTransparentBackColor, true);

            BorderStyle = BorderStyle.None;
            // The card's OWN BackColor is the page behind it, not the card face: the face is
            // painted inside the rounded path, so the corners show the page through instead of
            // four white squares.
            BackColor = UiTheme.PageBg;
            CardColor = UiTheme.Surface;
        }

        /// <summary>Corner radius of the card face.</summary>
        public int Radius
        {
            get { return _radius; }
            set { _radius = value < 0 ? 0 : value; Invalidate(); }
        }

        /// <summary>Soft drop shadow under the card. Off for a card nested inside another.</summary>
        public bool DrawShadow
        {
            get { return _drawShadow; }
            set { _drawShadow = value; Invalidate(); }
        }

        /// <summary>Hairline border colour.</summary>
        public Color LineColor
        {
            get { return _lineColor; }
            set { _lineColor = value; Invalidate(); }
        }

        /// <summary>The card face colour (what would otherwise be BackColor).</summary>
        public Color CardColor { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(BackColor);

            int shadowRoom = _drawShadow ? 2 : 0;
            var face = new Rectangle(0, 0, Width - 1, Height - 1 - shadowRoom);
            if (face.Width <= 0 || face.Height <= 0) { base.OnPaint(e); return; }

            if (_drawShadow)
            {
                // Two stacked offsets rather than a blur: a real Gaussian is not worth the cost
                // on a panel that repaints every 3 seconds, and at these alphas the eye reads
                // the pair as one soft edge.
                DrawShadowLayer(g, face, 1, 13);
                DrawShadowLayer(g, face, 2, 8);
            }

            using (GraphicsPath path = RoundedRect(face, _radius))
            using (var fill = new SolidBrush(CardColor))
            using (var pen = new Pen(_lineColor))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            base.OnPaint(e);
        }

        private void DrawShadowLayer(Graphics g, Rectangle face, int dy, int alpha)
        {
            var r = new Rectangle(face.X, face.Y + dy, face.Width, face.Height);
            using (GraphicsPath p = RoundedRect(r, _radius))
            using (var b = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                g.FillPath(b, p);
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        /// <summary>
        /// A rounded rectangle path. Shared by the cards, the icon chips and the pills so every
        /// rounded thing on a screen is rounded the same way.
        /// </summary>
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || d > r.Width || d > r.Height) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>A fully rounded (999px-radius) pill path for a rectangle.</summary>
        public static GraphicsPath Pill(Rectangle r)
        {
            return RoundedRect(r, r.Height / 2);
        }
    }

    /// <summary>
    /// A small rounded status chip — "Database connected", "Serving", "5 over 3 days". Auto-sizes
    /// to its text, optionally leads with a filled dot, and derives its border from its own two
    /// colours so a caller only ever names a tint and its matching ink.
    /// </summary>
    public class StatusPill : Control
    {
        private bool _dot;
        private Color _border = Color.Empty;
        private Padding _pad = new Padding(11, 5, 11, 5);

        public StatusPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = UiTheme.Surface;
            ForeColor = UiTheme.Muted;
            Font = new Font("Segoe UI", UiTheme.MinTextPt, FontStyle.Bold);
            AutoSize = false;
        }

        /// <summary>Lead the text with a 7px filled circle in the text colour.</summary>
        public bool ShowDot
        {
            get { return _dot; }
            set { _dot = value; ReSize(); Invalidate(); }
        }

        /// <summary>Border colour. Left unset it is a 13% blend of the fill toward the text.</summary>
        public Color BorderColor
        {
            get { return _border == Color.Empty ? UiTheme.Mix(BackColor, ForeColor, 0.13f) : _border; }
            set { _border = value; Invalidate(); }
        }

        public Padding Inset
        {
            get { return _pad; }
            set { _pad = value; ReSize(); Invalidate(); }
        }

        /// <summary>Sets fill + ink in one call, since the two always change together.</summary>
        public void SetTone(Color tint, Color ink)
        {
            _border = Color.Empty;
            BackColor = tint;
            ForeColor = ink;
            Invalidate();
        }

        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; ReSize(); Invalidate(); }
        }

        protected override void OnFontChanged(System.EventArgs e) { base.OnFontChanged(e); ReSize(); }

        /// <summary>Width follows the text; height follows the font. A pill never wraps.</summary>
        private void ReSize()
        {
            Size s = TextRenderer.MeasureText(Text ?? "", Font, new Size(int.MaxValue, int.MaxValue),
                                              TextFormatFlags.NoPadding);
            int dotRoom = _dot ? DotSize + 6 : 0;
            Size = new Size(s.Width + dotRoom + _pad.Left + _pad.Right,
                            s.Height + _pad.Top + _pad.Bottom);
        }

        private const int DotSize = 7;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color behind = UiTheme.PageBg;
            for (Control p = Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) { behind = p.BackColor; break; }
            g.Clear(behind);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = CardPanel.Pill(r))
            using (var fill = new SolidBrush(BackColor))
            using (var pen = new Pen(BorderColor))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            int x = _pad.Left;
            if (_dot)
            {
                using (var b = new SolidBrush(ForeColor))
                    g.FillEllipse(b, x, (Height - DotSize) / 2, DotSize, DotSize);
                x += DotSize + 6;
            }

            TextRenderer.DrawText(g, Text ?? "", Font,
                new Rectangle(x, 0, Width - x - _pad.Right, Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
        }
    }
}
