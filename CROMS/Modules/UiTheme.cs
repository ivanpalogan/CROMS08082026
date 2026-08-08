using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// One place that gives the whole app a consistent, professional look. <see cref="Polish"/>
    /// walks a control tree once (call it right after a form is built) and styles:
    ///   • Buttons — hand cursor + a subtle hover/pressed tint (flat buttons that don't already
    ///     define their own hover are left to the derived tint; nav/logout keep their custom one).
    ///   • DataGridViews — a clean modern table style: light bold header, thin horizontal row
    ///     separators, zebra striping, a soft blue selection, consistent Segoe UI + padding.
    /// Cosmetic only — never touches data binding or behaviour.
    /// </summary>
    public static class UiTheme
    {
        // Palette
        private static readonly Color HeaderBack = Color.FromArgb(248, 249, 250);
        private static readonly Color HeaderInk  = Color.FromArgb(73, 80, 87);
        private static readonly Color Ink         = Color.FromArgb(33, 37, 41);
        private static readonly Color GridLine    = Color.FromArgb(233, 236, 239);
        private static readonly Color Zebra       = Color.FromArgb(250, 251, 252);
        private static readonly Color SelBack     = Color.FromArgb(232, 240, 254);
        private static readonly Color SelInk      = Color.FromArgb(13, 71, 161);

        // The one font family the whole app uses.
        private const string BaseFamily = "Segoe UI";

        public static void Polish(Control root)
        {
            foreach (Control c in root.Controls)
            {
                NormalizeFont(c);
                // A button tagged "noskin" draws itself (e.g. a custom icon) — leave it alone.
                if (c is Button b) { if (!(b.Tag is string s && s == "noskin")) PolishButton(b); }
                else if (c is DataGridView g) StyleGrid(g);
                if (c.HasChildren) Polish(c);
            }
        }

        // Kept for existing call sites — same as Polish (buttons + grids + fonts).
        public static void PolishButtons(Control root) => Polish(root);

        // ------------------------------------------------------------------ fonts
        /// <summary>
        /// Forces every control onto the one base family (Segoe UI) while KEEPING its size and
        /// weight, so the whole app reads consistently. Intentional special fonts are left alone:
        /// emoji glyphs (Segoe UI Emoji) and the monospace displays (Consolas) used for queue
        /// numbers / receipts.
        /// </summary>
        private static void NormalizeFont(Control c)
        {
            Font f = c.Font;
            string fam = f.FontFamily.Name;
            if (fam == BaseFamily) return;
            if (fam.IndexOf("Emoji", System.StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (fam == "Consolas") return;
            c.Font = new Font(BaseFamily, f.Size, f.Style);
        }

        // ---------------------------------------------------------------- buttons
        // Corner radius for the subtle rounded buttons (small = professional, not pill-shaped).
        private const int CornerRadius = 6;

        // Secondary ("white") button look — a clean light-grey chip instead of native chrome.
        private static readonly Color SecondaryBack = Color.FromArgb(233, 236, 239);   // #E9ECEF

        /// <summary>
        /// Gives EVERY button one consistent flat style so nothing shows the native grey chrome:
        /// colored buttons keep their colour (solid, white text); plain/"white"/native buttons
        /// become a light-grey secondary chip with dark (or preserved danger-red) text. Unified
        /// bold Segoe UI at each button's own size, hover/press tint, and rounded corners.
        /// </summary>
        private static void PolishButton(Button b)
        {
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;                 // no border → clean rounded corners
            b.Font = new Font("Segoe UI", b.Font.Size, FontStyle.Bold);

            Color bg = b.BackColor;
            bool neutral = bg == Color.Empty || bg == SystemColors.Control || bg == Color.White
                           || (bg.R >= 230 && bg.G >= 230 && bg.B >= 230);

            if (neutral)
            {
                b.BackColor = SecondaryBack;
                Color fg = b.ForeColor;
                bool defaultFg = fg == Color.Empty || fg == SystemColors.ControlText
                                 || (fg.R < 70 && fg.G < 70 && fg.B < 70);
                if (defaultFg) b.ForeColor = Ink;            // else keep an intentional colour (e.g. danger red)
                b.FlatAppearance.MouseOverBackColor = Shade(SecondaryBack, 0.94f);
                b.FlatAppearance.MouseDownBackColor = Shade(SecondaryBack, 0.88f);
            }
            else
            {
                if (b.FlatAppearance.MouseOverBackColor == Color.Empty)
                    b.FlatAppearance.MouseOverBackColor = Shade(bg, 0.90f);
                if (b.FlatAppearance.MouseDownBackColor == Color.Empty)
                    b.FlatAppearance.MouseDownBackColor = Shade(bg, 0.82f);
            }

            RoundButton(b, CornerRadius);
        }

        /// <summary>
        /// Owner-draws the button as an ANTI-ALIASED rounded rectangle (smooth corners — a
        /// clipping Region can't anti-alias, so we paint it instead). The corners are filled
        /// with the parent's background so the rounding blends in; hover/press use the button's
        /// FlatAppearance shades. Per-button hover/press state is captured in the closures.
        /// </summary>
        private static void RoundButton(Button b, int radius)
        {
            // Double-buffer the button so the owner-draw doesn't flicker on hover/press.
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(b, true);

            bool hover = false, pressed = false;
            b.MouseEnter += (s, e) => { hover = true; b.Invalidate(); };
            b.MouseLeave += (s, e) => { hover = false; pressed = false; b.Invalidate(); };
            b.MouseDown  += (s, e) => { pressed = true; b.Invalidate(); };
            b.MouseUp    += (s, e) => { pressed = false; b.Invalidate(); };
            b.Resize     += (s, e) => b.Invalidate();
            b.Paint      += (s, e) =>
            {
                Color baseC = b.BackColor;
                Color fill = pressed
                    ? (b.FlatAppearance.MouseDownBackColor != Color.Empty ? b.FlatAppearance.MouseDownBackColor : Shade(baseC, 0.85f))
                    : hover
                        ? (b.FlatAppearance.MouseOverBackColor != Color.Empty ? b.FlatAppearance.MouseOverBackColor : Shade(baseC, 0.92f))
                        : baseC;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(b.Parent != null ? b.Parent.BackColor : SystemColors.Control);
                using (var path = RoundedRect(new Rectangle(0, 0, b.Width - 1, b.Height - 1), radius))
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                // Respect the button's own TextAlign (nav buttons are left-aligned/indented).
                var textRect = new Rectangle(10, 0, b.Width - 20, b.Height);
                TextRenderer.DrawText(g, b.Text, b.Font, textRect, b.ForeColor,
                    AlignFlags(b.TextAlign) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            b.Invalidate();
        }

        /// <summary>Horizontal part of a ContentAlignment as a TextFormatFlags value.</summary>
        private static TextFormatFlags AlignFlags(ContentAlignment a)
        {
            if (a == ContentAlignment.TopLeft || a == ContentAlignment.MiddleLeft || a == ContentAlignment.BottomLeft)
                return TextFormatFlags.Left;
            if (a == ContentAlignment.TopRight || a == ContentAlignment.MiddleRight || a == ContentAlignment.BottomRight)
                return TextFormatFlags.Right;
            return TextFormatFlags.HorizontalCenter;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);                 // top-left
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);         // top-right
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);  // bottom-right
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);         // bottom-left
            path.CloseFigure();
            return path;
        }

        // ------------------------------------------------------------------ grids
        private static void StyleGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;     // so our header colours actually show
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Color.White;
            g.GridColor = GridLine;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;   // thin row rules only
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.RowHeadersVisible = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToAddRows = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;

            // Stretch columns to fill the grid width (fixes grids that forgot to set this and
            // left a narrow table with blank space to the right).
            if (g.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.None)
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 40;

            var h = g.ColumnHeadersDefaultCellStyle;
            h.BackColor = HeaderBack;
            h.ForeColor = HeaderInk;
            h.SelectionBackColor = HeaderBack;
            h.SelectionForeColor = HeaderInk;
            h.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            h.Padding = new Padding(8, 0, 8, 0);
            h.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var c = g.DefaultCellStyle;
            c.Font = new Font("Segoe UI", 9.5f);
            c.ForeColor = Ink;
            c.BackColor = Color.White;
            c.SelectionBackColor = SelBack;
            c.SelectionForeColor = SelInk;
            c.Padding = new Padding(8, 4, 8, 4);

            g.AlternatingRowsDefaultCellStyle.BackColor = Zebra;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelBack;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = SelInk;

            // Only force a row height where the grid isn't auto-sizing its rows to content.
            if (g.AutoSizeRowsMode == DataGridViewAutoSizeRowsMode.None)
                g.RowTemplate.Height = 34;
        }

        /// <summary>Multiplies each RGB channel by <paramref name="f"/> (｢0.9｣ ≈ 10% darker).</summary>
        private static Color Shade(Color c, float f)
        {
            if (c == Color.Empty) c = SystemColors.Control;
            return Color.FromArgb(c.A, Clamp((int)(c.R * f)), Clamp((int)(c.G * f)), Clamp((int)(c.B * f)));
        }

        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}
