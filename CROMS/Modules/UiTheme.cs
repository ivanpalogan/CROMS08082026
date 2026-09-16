using System;
using System.Collections.Generic;
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
        // ---------------------------------------------------------------- palette
        // The Navy Blue (light) token set shared by the desktop Launcher/Login, this shell, and
        // the kiosk. These are PUBLIC on purpose: module forms should reference UiTheme.Accent
        // rather than re-declaring their own Color.FromArgb literals, which is how the app drifted
        // into several palettes in the first place.
        public static readonly Color PageBg      = Color.FromArgb(244, 246, 249);   // #F4F6F9
        public static readonly Color Surface     = Color.White;
        public static readonly Color CardLine    = Color.FromArgb(225, 229, 236);   // #E1E5EC
        public static readonly Color Ink         = Color.FromArgb(23, 26, 36);      // #171B24
        public static readonly Color Muted       = Color.FromArgb(91, 100, 114);    // #5B6472
        public static readonly Color Faint       = Color.FromArgb(137, 145, 163);
        public static readonly Color Accent      = Color.FromArgb(29, 78, 216);     // #1D4ED8
        public static readonly Color AccentHover = Color.FromArgb(26, 68, 192);
        public static readonly Color AccentTint  = Color.FromArgb(234, 241, 254);   // #EAF1FE
        public static readonly Color Navy        = Color.FromArgb(19, 36, 65);      // #132441 sidebar
        public static readonly Color NavyHover   = Color.FromArgb(28, 48, 82);
        public static readonly Color Success     = Color.FromArgb(46, 148, 87);     // #2E9457
        public static readonly Color Warning     = Color.FromArgb(180, 83, 9);      // #B45309
        public static readonly Color Danger      = Color.FromArgb(198, 50, 63);     // #C6323F

        // Tint backgrounds for status chips and pills, plus the two neutrals the cards need.
        // A chip must read as "this is the success/warning/danger state" without shouting, so
        // the tint carries the meaning and the saturated token above it carries the text.
        public static readonly Color SuccessTint = Color.FromArgb(231, 244, 237);   // #E7F4ED
        public static readonly Color WarningTint = Color.FromArgb(253, 241, 227);   // #FDF1E3
        public static readonly Color DangerTint  = Color.FromArgb(251, 234, 236);   // #FBEAEC
        public static readonly Color Chrome      = Color.FromArgb(238, 241, 246);   // #EEF1F6 secondary chip / ghost button
        public static readonly Color RowLine     = Color.FromArgb(240, 242, 246);   // #F0F2F6 list row separator

        /// <summary>
        /// Linear blend of two palette colours, so a derived shade (a pill's border, a chip's
        /// edge) is stated as a relationship to the tokens rather than as yet another literal.
        /// <paramref name="t"/> = 0 returns <paramref name="a"/>, 1 returns <paramref name="b"/>.
        /// </summary>
        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            return Color.FromArgb(255,
                Clamp((int)(a.R + (b.R - a.R) * t)),
                Clamp((int)(a.G + (b.G - a.G) * t)),
                Clamp((int)(a.B + (b.B - a.B) * t)));
        }

        // Grid tokens derived from the set above.
        private static readonly Color HeaderBack = Color.FromArgb(248, 249, 251);
        private static readonly Color HeaderInk  = Muted;
        private static readonly Color GridLine   = Color.FromArgb(233, 236, 241);
        private static readonly Color Zebra      = Color.FromArgb(250, 251, 253);
        private static readonly Color SelBack    = AccentTint;
        private static readonly Color SelInk     = Accent;

        // The one font family the whole app uses.
        private const string BaseFamily = "Segoe UI";

        public static void Polish(Control root)
        {
            foreach (Control c in root.Controls)
            {
                NormalizeFont(c);
                // A button tagged "noskin" draws itself (e.g. a custom icon) — leave it alone.
                if (c is Button b)
                {
                    if (!(b.Tag is string s && s == "noskin")) PolishButton(b);
                    if (b is SplitButton sb && sb.Menu != null) StyleMenu(sb.Menu);
                }
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
            if (fam.IndexOf("Emoji", System.StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (fam == "Consolas") return;

            float size = BodySize(c, f.Size);
            if (fam == BaseFamily && size == f.Size) return;
            c.Font = new Font(BaseFamily, size, f.Style);
        }

        /// <summary>Every field label on every screen.</summary>
        public const float LabelSize = 9F;
        /// <summary>Every box the operator types or picks in.</summary>
        public const float InputSize = 9.75F;

        /// <summary>
        /// BR-18. One size for labels and one for inputs, across every screen.
        /// <para/>
        /// The forms were built at different times and drifted: the death screen types at
        /// 9.75pt, the marriage dialog at 10pt, the birth screen at the WinForms default of
        /// 8.25pt, and their captions at 8.5, 9 and 9.75 between them. Side by side that
        /// reads as three different applications.
        /// <para/>
        /// Snapping is deliberately NARROW - only a control already within a point of the
        /// target moves. A heading set at 12pt, a queue number at 30pt or a deliberately
        /// small hint stays exactly as designed; this pulls body text into line, it does not
        /// flatten the type scale. Keeping the step under a point also keeps text metrics
        /// close enough that nothing in a fixed-width box starts clipping.
        /// </summary>
        private static float BodySize(Control c, float current)
        {
            float target = (c is TextBox || c is ComboBox || c is DateTimePicker ||
                            c is NumericUpDown || c is MaskedTextBox)
                ? InputSize
                : (c is Label || c is CheckBox || c is RadioButton) ? LabelSize : current;

            return System.Math.Abs(target - current) <= 1f ? target : current;
        }

        // ---------------------------------------------------------------- buttons
        // Corner radius for the subtle rounded buttons (small = professional, not pill-shaped).
        private const int CornerRadius = 8;

        // Secondary ("white") button look — a clean light chip instead of native chrome.
        private static readonly Color SecondaryBack = Color.FromArgb(238, 241, 246);

        // Disabled look, shared by every owner-drawn button.
        private static readonly Color DisabledBack = Color.FromArgb(217, 220, 227);
        private static readonly Color DisabledInk  = Faint;

        // A button with a registered icon draws it before its text (or centered alone, when the
        // button's Text is empty — the sidebar's collapsed icon-rail state uses exactly that).
        // Keyed by reference so only buttons that opt in (the sidebar nav, so far) are affected.
        private static readonly Dictionary<Button, Action<Graphics, RectangleF, Color>> _icons =
            new Dictionary<Button, Action<Graphics, RectangleF, Color>>();

        public static void SetIcon(Button b, Action<Graphics, RectangleF, Color> draw)
        {
            _icons[b] = draw;
            b.Disposed += (s, e) => _icons.Remove(b);
            b.Invalidate();
        }

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
            b.EnabledChanged += (s, e) => b.Invalidate();
            b.Paint      += (s, e) =>
            {
                Color baseC = b.BackColor;
                Color fill = pressed
                    ? (b.FlatAppearance.MouseDownBackColor != Color.Empty ? b.FlatAppearance.MouseDownBackColor : Shade(baseC, 0.85f))
                    : hover
                        ? (b.FlatAppearance.MouseOverBackColor != Color.Empty ? b.FlatAppearance.MouseOverBackColor : Shade(baseC, 0.92f))
                        : baseC;

                // A DISABLED button must look disabled. This owner-draw previously painted the
                // button's own colours regardless of Enabled, so e.g. a greyed-out Back still
                // looked fully clickable and simply did nothing when pressed — which reads as a
                // broken button, not an unavailable one. Applies app-wide.
                if (!b.Enabled)
                {
                    fill = DisabledBack;
                    hover = pressed = false;
                }

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Backdrop(b));
                using (var path = RoundedRect(new Rectangle(0, 0, b.Width - 1, b.Height - 1), radius))
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                Color ink = b.Enabled ? b.ForeColor : DisabledInk;
                Action<Graphics, RectangleF, Color> icon;
                bool hasIcon = _icons.TryGetValue(b, out icon);
                // A blank Text with an icon registered is the "icon-only" state (the sidebar's
                // collapsed rail) — the icon centers itself instead of sitting at a fixed left
                // indent, since there is no label beside it to leave room for.
                bool iconOnly = hasIcon && string.IsNullOrEmpty(b.Text);
                const int iconBox = 18;
                int iconX = iconOnly ? (b.Width - iconBox) / 2 : 14;
                int iconY = (b.Height - iconBox) / 2;
                int textStart = hasIcon ? iconX + iconBox + 10 : 10;

                if (hasIcon) icon(g, new RectangleF(iconX, iconY, iconBox, iconBox), ink);

                if (!iconOnly)
                {
                    // Respect the button's own TextAlign (nav buttons are left-aligned/indented).
                    var textRect = new Rectangle(textStart, 0, b.Width - textStart - 10, b.Height);
                    TextRenderer.DrawText(g, b.Text, b.Font, textRect, ink,
                        AlignFlags(b.TextAlign) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            };
            b.Invalidate();
        }

        /// <summary>
        /// The colour actually painted behind a button, used to blend its rounded corners.
        /// <c>Parent.BackColor</c> alone is wrong whenever the parent is transparent (a container
        /// that shows its own parent through, common on card panels) — clearing to Transparent
        /// paints transparent-black and leaves a dark halo around the corners. Walk up until an
        /// opaque colour is found. Same defect this codebase already hit in the kiosk.
        /// </summary>
        private static Color Backdrop(Control c)
        {
            for (Control p = c.Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) return p.BackColor;
            return SystemColors.Control;
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

        // ------------------------------------------------------------------ menus
        // A ContextMenuStrip is a Component, not a child Control, so Polish's recursive walk
        // never reaches it on its own — a SplitButton's dropdown (Print Certificate's "View
        // Softcopy") was still rendering with the stock system blue highlight while every
        // button/grid around it had already moved onto the Navy Blue palette. Applied once,
        // lazily, the first time Polish encounters the SplitButton that owns the menu.
        private static readonly ToolStripProfessionalRenderer MenuRenderer =
            new ToolStripProfessionalRenderer(new MenuColors());

        internal static void StyleMenu(ContextMenuStrip menu)
        {
            if (menu.Renderer == MenuRenderer) return;   // already styled
            menu.Renderer = MenuRenderer;
            menu.Font = new Font(BaseFamily, 9F);
            menu.ShowImageMargin = false;
        }

        private sealed class MenuColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color MenuBorder => CardLine;
            public override Color MenuItemBorder => AccentTint;
            public override Color MenuItemSelected => AccentTint;
            public override Color MenuItemSelectedGradientBegin => AccentTint;
            public override Color MenuItemSelectedGradientEnd => AccentTint;
            public override Color MenuItemPressedGradientBegin => AccentTint;
            public override Color MenuItemPressedGradientEnd => AccentTint;
            public override Color SeparatorDark => RowLine;
            public override Color SeparatorLight => RowLine;
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
