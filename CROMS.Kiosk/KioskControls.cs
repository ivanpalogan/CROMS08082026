using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    // ============================ modern UI controls ============================
    // Shared custom controls used by the kiosk's step forms. All are parameterless-
    // constructible with public properties so the Visual Studio designer can drop them.

    /// <summary>
    /// Shared with every kiosk step form that scales a fixed-size box to fit the screen
    /// (<c>Control.Scale</c>). Scale moves and resizes controls but does NOT touch fonts
    /// (AutoScaleMode.None), so on a short screen (e.g. 1366x768) the boxes shrink around
    /// full-size text and captions clip/overlap — first found and fixed on the BREQS step,
    /// generalised here so Personal Info & Photo gets the same fix instead of drifting.
    /// </summary>
    internal static class FontScaler
    {
        /// <summary>Scales every EXPLICITLY set font under <paramref name="root"/>. A control
        /// that inherits its parent's font (same Font instance) is skipped, or it would be
        /// scaled twice.</summary>
        public static void Scale(Control root, float f)
        {
            var explicitFonts = new System.Collections.Generic.List<Tuple<Control, Font>>();
            Action<Control> walk = null;
            walk = c =>
            {
                foreach (Control child in c.Controls)
                {
                    if (!ReferenceEquals(child.Font, c.Font)) explicitFonts.Add(Tuple.Create(child, child.Font));
                    walk(child);
                }
            };
            walk(root);
            foreach (var cf in explicitFonts)
                cf.Item1.Font = new Font(cf.Item2.FontFamily, Math.Max(7f, cf.Item2.Size * f), cf.Item2.Style);
        }
    }

    /// <summary>A panel with rounded corners, an optional soft shadow, and a border.</summary>
    public class RoundPanel : Panel
    {
        public int Radius { get; set; } = 12;
        public Color Fill { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Empty;
        public float BorderWidth { get; set; } = 1f;
        public int Shadow { get; set; } = 0;   // px of soft drop shadow (0 = none)

        public RoundPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        /// <summary>Rounded-rectangle path (also used to draw the camera guide + pills).</summary>
        public static GraphicsPath Round(Rectangle r, int rad)
        {
            int d = Math.Max(1, rad * 2);
            var p = new GraphicsPath();
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var full = ClientRectangle;

            if (Shadow > 0)
            {
                for (int i = Shadow; i >= 1; i--)
                {
                    var sr = new Rectangle(full.X + i / 2, full.Y + i,
                        full.Width - i - 1, full.Height - i - 1);
                    int a = Math.Max(3, 26 - i * 3);
                    using (var sp = Round(sr, Radius))
                    using (var sb = new SolidBrush(Color.FromArgb(a, 0, 0, 0)))
                        g.FillPath(sb, sp);
                }
            }

            var rr = new Rectangle(full.X, full.Y,
                full.Width - Shadow - 1, full.Height - Shadow - 1);
            using (var path = Round(rr, Radius))
            {
                using (var b = new SolidBrush(Fill)) g.FillPath(b, path);
                if (BorderColor != Color.Empty)
                    using (var pen = new Pen(BorderColor, BorderWidth)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>
    /// A selectable priority-lane CARD: icon above a centred label, with a "Tap to select"
    /// hint and a check badge once chosen. Deliberately mirrors the Step 1 service cards
    /// (same rounded shape, same tint/border/badge language) so the whole kiosk reads as one
    /// system. Replaced an earlier full-width row layout, which looked like a static list.
    /// </summary>
    public class PillToggle : Panel
    {
        private float _hoverT;

        /// <summary>Legacy emoji glyph. Only used when <see cref="GlyphCode"/> is 0.</summary>
        public string Glyph { get; set; } = "";

        /// <summary>
        /// Phosphor Light codepoint (e.g. KioskCore.IconWheelchair). Set as a plain hex int
        /// rather than a literal glyph character so the Designer file stays ASCII.
        /// </summary>
        public int GlyphCode { get; set; }

        public string LabelText { get; set; } = "";

        public bool Checked { get; private set; }
        public event EventHandler CheckedChanged;

        private static readonly Color Accent = Color.FromArgb(29, 78, 216);
        private static readonly Color SelBg = Color.FromArgb(234, 241, 254);
        private static readonly Color Line = Color.FromArgb(216, 220, 227);
        private static readonly Color Ink = Color.FromArgb(23, 26, 36);

        public PillToggle()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Click += (s, e) => Toggle();
            // Eased hover to match the Step 1 service cards rather than snapping.
            HoverFade.Attach(this, 160, t => { _hoverT = t; Invalidate(); });
        }

        private void Toggle()
        {
            Checked = !Checked;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetChecked(bool v)
        {
            if (Checked == v) return;
            Checked = v;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null) g.Clear(Parent is RoundPanel rp ? rp.Fill : Parent.BackColor);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            float t = Checked ? 0f : _hoverT;

            Color fill = Checked ? SelBg : HoverFade.Lerp(Color.White, Color.FromArgb(250, 251, 254), t);
            Color border = Checked ? Accent : HoverFade.Lerp(Line, Accent, t);
            float borderW = Checked ? 2.5f : HoverFade.Lerp(1.5f, 2f, t);
            Color content = Checked ? Accent : HoverFade.Lerp(Ink, Accent, t);

            using (var path = RoundPanel.Round(r, 12))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border, borderW)) g.DrawPath(pen, path);
            }

            // Icon centred near the top, label under it, hint under that — every offset is a
            // FRACTION of the card's own Height (derived from the 170x112 design size), not a
            // fixed pixel count. A fixed iconPx/gap left the label and hint fixed in place while
            // FitToScreen shrank the card around them (a small-screen render), so the hint line
            // was drawn below the card's actual bottom edge and never appeared at 1366x768.
            const float DesignH = 112f;
            float iconY = Height * 0.16f;
            float iconPx = Height * (30f / DesignH);
            float labelTop = iconY + iconPx + Height * (6f / DesignH);
            float labelH = Height * (22f / DesignH);
            float hintTop = iconY + iconPx + Height * (28f / DesignH);
            float hintH = Height * (16f / DesignH);
            // Fonts shrink with the card too (clamped so text never vanishes on a tiny card).
            float fontScale = Math.Min(1f, Height / DesignH);
            float labelPt = Math.Max(6.5f, 10F * fontScale);
            float hintPt = Math.Max(5.5f, 7.5F * fontScale);

            if (GlyphCode != 0 && IconFont.IsAvailable)
                IconFont.Draw(g, new RectangleF(0, iconY, Width, iconPx),
                    char.ConvertFromUtf32(GlyphCode), content, iconPx);
            else
                using (var fg = new Font("Segoe UI Emoji", 15F))
                    TextRenderer.DrawText(g, Glyph, fg,
                        new Rectangle(0, (int)iconY, Width, (int)iconPx), content,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            using (var fmt = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            })
            {
                using (var ft = new Font("Segoe UI", labelPt, FontStyle.Bold))
                using (var tb = new SolidBrush(content))
                    g.DrawString(LabelText, ft, tb,
                        new RectangleF(4, labelTop, Width - 8, labelH), fmt);

                using (var fh = new Font("Segoe UI", hintPt))
                using (var hb = new SolidBrush(Checked ? Accent : Color.FromArgb(137, 145, 163)))
                    g.DrawString(Checked ? "SELECTED" : "TAP TO SELECT", fh, hb,
                        new RectangleF(4, hintTop, Width - 8, hintH), fmt);
            }

            // Check badge, top-right — same language as the Step 1 service cards.
            if (Checked)
            {
                var badge = new Rectangle(Width - 28, 9, 19, 19);
                using (var b = new SolidBrush(Accent)) g.FillEllipse(b, badge);
                using (var wp = new Pen(Color.White, 2f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(wp, new[]
                    {
                        new PointF(badge.X + badge.Width * 0.27f, badge.Y + badge.Height * 0.52f),
                        new PointF(badge.X + badge.Width * 0.44f, badge.Y + badge.Height * 0.70f),
                        new PointF(badge.X + badge.Width * 0.74f, badge.Y + badge.Height * 0.32f)
                    });
            }
        }
    }

    /// <summary>
    /// A numbered step indicator (done ✓ / current / upcoming), wrapped in a single rounded
    /// capsule (white fill, hairline border, soft shadow) so the whole sequence reads as ONE
    /// "you are here" control instead of loose labels floating on the page.
    /// </summary>
    public class StepIndicator : Panel
    {
        private int _current;

        /// <summary>Designer-settable step labels.</summary>
        public string[] Steps { get; set; } = new string[0];

        private static readonly Color Accent = Color.FromArgb(29, 78, 216);
        private static readonly Color Done = Color.FromArgb(46, 148, 87);
        private static readonly Color Idle = Color.FromArgb(216, 220, 227);
        private static readonly Color IdleDot = Color.FromArgb(238, 241, 246);
        private static readonly Color Muted = Color.FromArgb(91, 100, 114);
        private static readonly Color CapsuleBg = Color.White;
        private static readonly Color CapsuleLine = Color.FromArgb(225, 229, 236);

        // TextRenderer treats '&' as a mnemonic prefix by default: it swallows the '&' and
        // underlines the next character, so a step label like "Personal Info & Photo" renders
        // as "Personal Info _Photo". NoPrefix makes '&' render literally. Must be passed to
        // MeasureText as well as DrawText, or the measured width won't match what is drawn.
        private const TextFormatFlags NoAmpersand = TextFormatFlags.NoPrefix;
        private static readonly Size MaxSize = new Size(int.MaxValue, int.MaxValue);

        public StepIndicator()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        // Layout constants shared between the measure pass and the draw pass, and now also
        // FitToContent - all three MUST advance by exactly the same amounts, or the capsule
        // (and the control's own resized bounds) won't match what actually gets drawn.
        private const int D = 26;          // step dot diameter
        private const int PadX = 22;       // capsule left/right padding
        private const int PadY = 10;       // capsule top/bottom padding
        private const int DotGap = 10;     // dot -> its own label
        private const int StepGap = 16;    // label <-> connector line
        private const int Connector = 28;  // connector line length

        public void SetStep(int i) { _current = i; Invalidate(); }

        private static int MeasureContentWidth(string[] labels, int current)
        {
            int contentW = 0;
            using (var fCur = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var fOther = new Font("Segoe UI", 10.5F))
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    var lf = i == current ? fCur : fOther;
                    contentW += D + DotGap + TextRenderer.MeasureText(labels[i], lf, MaxSize, NoAmpersand).Width;
                }
            }
            if (labels.Length > 1)
                contentW += (labels.Length - 1) * (StepGap + Connector + StepGap);
            return contentW;
        }

        /// <summary>
        /// Resizes the control to fit the CURRENT Steps/step exactly (capped at maxWidth, so it
        /// still respects the space the host form actually has). Call after setting Steps and
        /// SetStep - a fixed Designer Size cannot know the step count/labels chosen at runtime
        /// (e.g. "CTC Details" only appears when HasCtc), and a control narrower than its content
        /// silently clips the last label instead of shrinking the capsule around it.
        /// </summary>
        public void FitToContent(int maxWidth)
        {
            string[] labels = Steps ?? new string[0];
            int contentW = MeasureContentWidth(labels, _current);
            int w = Math.Min(maxWidth, contentW + PadX * 2);
            if (w != Width) Width = w;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            string[] labels = Steps ?? new string[0];
            const int d = D, padX = PadX, padY = PadY, dotGap = DotGap, stepGap = StepGap, connector = Connector;

            using (var fCur = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (var fOther = new Font("Segoe UI", 10.5F))
            {
                int contentW = MeasureContentWidth(labels, _current);
                int capsuleW = Math.Min(Width, contentW + padX * 2);
                int capsuleH = Math.Min(Height, d + padY * 2);
                var capsule = new Rectangle(0, (Height - capsuleH) / 2, capsuleW, capsuleH);

                // radius = exactly half the height would make diameter == height, which trips
                // RoundPanel.Round's "too big to round" guard and silently falls back to a
                // square rectangle — back it off by 1px so it stays a true pill shape.
                using (var path = RoundPanel.Round(capsule, capsuleH / 2 - 1))
                {
                    using (var b = new SolidBrush(CapsuleBg)) g.FillPath(b, path);
                    using (var pen = new Pen(CapsuleLine, 1f)) g.DrawPath(pen, path);
                }

                int x = capsule.X + padX;
                int y = capsule.Y + (capsuleH - d) / 2;
                for (int i = 0; i < labels.Length; i++)
                {
                    bool done = i < _current;
                    bool cur = i == _current;
                    Color dot = done ? Done : (cur ? Accent : IdleDot);
                    Color numColor = (done || cur) ? Color.White : Muted;

                    using (var b = new SolidBrush(dot)) g.FillEllipse(b, x, y, d, d);
                    string num = done ? "✓" : (i + 1).ToString();
                    using (var fNum = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                        TextRenderer.DrawText(g, num, fNum, new Rectangle(x, y, d, d), numColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    var lf = cur ? fCur : fOther;
                    Color lc = cur ? Accent : Muted;
                    int lblW = TextRenderer.MeasureText(labels[i], lf, MaxSize, NoAmpersand).Width;
                    TextRenderer.DrawText(g, labels[i], lf,
                        new Rectangle(x + d + dotGap, y, lblW + 4, d), lc,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    x += d + dotGap + lblW;
                    if (i < labels.Length - 1)
                    {
                        x += stepGap;
                        using (var pen = new Pen(Idle, 2f))
                            g.DrawLine(pen, x, y + d / 2, x + connector, y + d / 2);
                        x += connector + stepGap;
                    }
                }
            }
        }
    }
}
