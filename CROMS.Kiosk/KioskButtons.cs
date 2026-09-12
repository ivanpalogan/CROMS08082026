using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>What role a kiosk button plays, which decides its colour treatment.</summary>
    public enum KioskButtonKind
    {
        /// <summary>The step's main action (Next / Capture). Accent blue + elevation.</summary>
        Primary,
        /// <summary>The terminal commit action (Get Queue Number). Green + elevation.</summary>
        Success,
        /// <summary>A quiet alternative (Back, or a done-with action). Neutral chip, flat.</summary>
        Secondary
    }

    /// <summary>
    /// One owner-draw treatment for every kiosk button, so Step 1 and Step 2 can't drift apart
    /// (they already had, which is what prompted this). Draws a rounded fill with an optional
    /// coloured elevation shadow — a flat fill alone reads too weak for a primary action — an
    /// optional Phosphor icon beside the label, an eased hover, and a visibly greyed disabled
    /// state so "you can act on this" is never carried by colour alone.
    /// </summary>
    public static class KioskButtons
    {
        private const int Radius = 12;
        private const int ShadowRoom = 8;   // px reserved at the bottom for the drop shadow
        private const float IconPx = 22f;
        private const int IconGap = 10;

        private sealed class State
        {
            public int Glyph;
            public bool IconRight;
            public KioskButtonKind Kind;
            public float HoverT;
            public Color? Backdrop;   // explicit colour behind the button, when the parent lies
        }

        /// <summary>
        /// The colour actually painted behind the button, used to blend the rounded corners.
        /// <para/>
        /// Two cases make <c>Parent.BackColor</c> the wrong answer, and both were visibly wrong
        /// on the kiosk before this existed: a <see cref="RoundPanel"/> parent reports
        /// <c>Transparent</c> (its real colour is <c>Fill</c>), which cleared to transparent-black
        /// and left a dark edge; and a button parented to the FORM but sitting over the docked
        /// white footer cleared to the form's grey and left a visible grey patch around itself.
        /// Callers pass <paramref name="over"/> for that second case.
        /// </summary>
        private static Color Backdrop(Control c, Color? over)
        {
            if (over.HasValue) return over.Value;
            for (Control p = c.Parent; p != null; p = p.Parent)
            {
                if (p is RoundPanel rp) return rp.Fill;
                if (p.BackColor.A == 255) return p.BackColor;
            }
            return SystemColors.Control;
        }

        private static readonly Dictionary<Button, State> _state = new Dictionary<Button, State>();

        /// <summary>Swap the icon on an already-styled button (e.g. Capture -> Retake).</summary>
        public static void SetGlyph(Button b, int glyphCode)
        {
            if (_state.TryGetValue(b, out State st)) { st.Glyph = glyphCode; b.Invalidate(); }
        }

        /// <summary>
        /// Change an already-styled button's role (e.g. Capture drops to Secondary once a photo
        /// exists, so the eye is drawn to Print instead). Safe to call repeatedly — unlike
        /// re-calling <see cref="Style"/>, which would stack another Paint handler.
        /// </summary>
        public static void SetKind(Button b, KioskButtonKind kind)
        {
            if (_state.TryGetValue(b, out State st)) { st.Kind = kind; b.Invalidate(); }
        }

        public static void Style(Button b, KioskButtonKind kind, int glyphCode = 0, bool iconRight = false,
                                 Color? backdrop = null)
        {
            if (_state.ContainsKey(b))          // already styled — just update, don't re-wire
            {
                State existing = _state[b];
                existing.Kind = kind; existing.Glyph = glyphCode; existing.IconRight = iconRight;
                existing.Backdrop = backdrop;
                b.Invalidate();
                return;
            }

            var st = new State { Glyph = glyphCode, Kind = kind, IconRight = iconRight, Backdrop = backdrop };
            _state[b] = st;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(b, true);

            HoverFade.Attach(b, 150, t => { st.HoverT = t; b.Invalidate(); });
            b.Disposed += (s, e) => _state.Remove(b);

            b.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Backdrop(b, st.Backdrop));

                bool on = b.Enabled;
                var fillRect = new Rectangle(0, 0, b.Width - 1, b.Height - ShadowRoom - 1);

                Color baseC, hoverC, textC;
                switch (st.Kind)
                {
                    case KioskButtonKind.Success:
                        baseC = KioskCore.Success; hoverC = KioskCore.SuccessHover; textC = Color.White; break;
                    case KioskButtonKind.Secondary:
                        baseC = Color.FromArgb(238, 241, 246); hoverC = Color.FromArgb(228, 232, 239);
                        textC = KioskCore.Ink; break;
                    default:
                        baseC = KioskCore.Accent; hoverC = KioskCore.AccentHover; textC = Color.White; break;
                }
                if (!on) { baseC = Color.FromArgb(217, 220, 227); textC = Color.FromArgb(137, 145, 163); }

                // Coloured elevation; hover lifts it further. Secondary stays flat on purpose.
                if (on && st.Kind != KioskButtonKind.Secondary)
                {
                    int alpha = (int)HoverFade.Lerp(58, 88, st.HoverT);
                    int drop = (int)HoverFade.Lerp(4, 6, st.HoverT);
                    using (var sb = new SolidBrush(Color.FromArgb(alpha, baseC)))
                    using (var sp = Rounded(new Rectangle(3, drop, fillRect.Width - 6, fillRect.Height), Radius))
                        g.FillPath(sb, sp);
                }

                Color fill = on ? HoverFade.Lerp(baseC, hoverC, st.HoverT) : baseC;
                using (var path = Rounded(fillRect, Radius))
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                // Icon + label drawn as one centred group (icon leads, or trails for "Next").
                bool hasIcon = st.Glyph != 0 && IconFont.IsAvailable;
                Size textSize = TextRenderer.MeasureText(g, b.Text, b.Font,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
                int groupW = textSize.Width + (hasIcon ? (int)IconPx + IconGap : 0);
                int x = fillRect.X + (fillRect.Width - groupW) / 2;
                int midY = fillRect.Y + fillRect.Height / 2;

                if (hasIcon && !st.IconRight)
                {
                    IconFont.Draw(g, new RectangleF(x, midY - IconPx / 2f, IconPx, IconPx),
                        char.ConvertFromUtf32(st.Glyph), textC, IconPx);
                    x += (int)IconPx + IconGap;
                }
                TextRenderer.DrawText(g, b.Text, b.Font,
                    new Rectangle(x, midY - textSize.Height / 2, textSize.Width, textSize.Height),
                    textC, TextFormatFlags.NoPrefix);
                if (hasIcon && st.IconRight)
                {
                    x += textSize.Width + IconGap;
                    IconFont.Draw(g, new RectangleF(x, midY - IconPx / 2f, IconPx, IconPx),
                        char.ConvertFromUtf32(st.Glyph), textC, IconPx);
                }
            };
            b.Invalidate();
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
    }
}
