using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// One headline number on the dashboard: icon chip, optional delta pill, label, value,
    /// caption and a sparkline — all painted, no image assets.
    ///
    /// Everything below the chip is DRAWN rather than laid out as child Labels, for one reason
    /// that matters: the four cards sit side by side and their big numbers must share a single
    /// baseline. Collections renders "₱" and ".00" at a smaller size than its digits, so with
    /// separate auto-sized Labels the currency card would sit a few pixels off from the other
    /// three — which is exactly why the old dashboard dropped it to 20pt and still looked
    /// misaligned. Painting lets the small glyphs be placed on the SAME baseline as the digits.
    /// </summary>
    public class KpiCard : CardPanel
    {
        public enum Icon { QueuePerson, DocumentTick, PesoCoin, InboxTray }
        public enum Spark { None, Area, Bars }

        private static readonly Padding Inset = new Padding(16, 9, 16, 8);

        private readonly Font _labelFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        private readonly Font _valueFont = new Font("Segoe UI", 24F, FontStyle.Bold);
        private readonly Font _affixFont = new Font("Segoe UI Semibold", 12.75F, FontStyle.Bold);
        private readonly Font _captionFont = new Font("Segoe UI", 8.5F);
        private readonly Font _capBoldFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        private readonly Font _deltaFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);

        private double[] _spark = new double[0];

        public KpiCard()
        {
            Radius = 10;
            Padding = Inset;
            Cursor = Cursors.Default;
            Label = "";
            Value = "";
            Caption = "";
            Prefix = "";
            Suffix = "";
            Tint = UiTheme.AccentTint;
            Accent = UiTheme.Accent;
            ValueColor = UiTheme.Ink;
            CaptionColor = UiTheme.Faint;
            SparkStyle = Spark.None;
            SparkColor = UiTheme.Accent;
            DeltaVisible = false;
            DeltaTint = UiTheme.Chrome;
            DeltaInk = UiTheme.Muted;
            DeltaText = "";
        }

        // ------------------------------------------------------------------ content
        /// <summary>UPPERCASE metric name, e.g. WAITING NOW.</summary>
        public string Label { get; set; }

        /// <summary>The number itself, without any currency decoration.</summary>
        public string Value { get; set; }

        /// <summary>Drawn small, on the value's baseline, before the number (e.g. "₱").</summary>
        public string Prefix { get; set; }

        /// <summary>Drawn small, on the value's baseline, after the number (e.g. ".00").</summary>
        public string Suffix { get; set; }

        public string Caption { get; set; }

        /// <summary>Chip background. The card's colour identity.</summary>
        public Color Tint { get; set; }

        /// <summary>Icon stroke colour inside the chip.</summary>
        public Color Accent { get; set; }

        public Color ValueColor { get; set; }

        /// <summary>Accent + bold on a card that navigates somewhere; Faint on one that does not.</summary>
        public Color CaptionColor { get; set; }

        public bool CaptionIsAction { get; set; }

        public Icon IconKind { get; set; }

        // -------------------------------------------------------------------- delta
        /// <summary>
        /// False hides the pill outright. A delta with no comparison basis is not rendered as
        /// "0%" or as a guess — an invented comparison on a registry dashboard is worse than a
        /// blank corner.
        /// </summary>
        public bool DeltaVisible { get; set; }
        public string DeltaText { get; set; }
        public Color DeltaTint { get; set; }
        public Color DeltaInk { get; set; }

        public void SetDelta(string text, Color tint, Color ink)
        {
            DeltaText = text ?? "";
            DeltaTint = tint;
            DeltaInk = ink;
            DeltaVisible = DeltaText.Length > 0;
        }

        public void HideDelta() { DeltaVisible = false; DeltaText = ""; }

        // ---------------------------------------------------------------- sparkline
        public Spark SparkStyle { get; set; }
        public Color SparkColor { get; set; }

        /// <summary>
        /// The series behind the number. An all-zero or empty series draws NOTHING rather than a
        /// flat line, because a flat line reads as a measured result and this one would not be.
        /// </summary>
        public void SetSpark(double[] values)
        {
            _spark = values ?? new double[0];
        }

        // ------------------------------------------------------------------- paint
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle inner = Rectangle.FromLTRB(
                Inset.Left, Inset.Top, Width - Inset.Right, Height - Inset.Bottom - 2);
            if (inner.Width < 40 || inner.Height < 40) return;

            // ---- top row: chip left, delta pill right
            var chip = new Rectangle(inner.X, inner.Y, 32, 32);
            using (GraphicsPath p = RoundedRect(chip, 9))
            using (var b = new SolidBrush(Tint))
                g.FillPath(b, p);
            DrawIcon(g, new Rectangle(chip.X + 8, chip.Y + 8, 17, 17), Accent);

            if (DeltaVisible && DeltaText.Length > 0)
            {
                Size t = TextRenderer.MeasureText(g, DeltaText, _deltaFont,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                var pill = new Rectangle(inner.Right - (t.Width + 16), chip.Y + (32 - (t.Height + 6)) / 2,
                                         t.Width + 16, t.Height + 6);
                if (pill.X > chip.Right + 6)
                {
                    using (GraphicsPath p = Pill(pill))
                    using (var b = new SolidBrush(DeltaTint))
                        g.FillPath(b, p);
                    TextRenderer.DrawText(g, DeltaText, _deltaFont, pill, DeltaInk,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.NoPadding);
                }
            }

            // ---- label
            int y = chip.Bottom + 8;
            int labelH = LineHeight(g, _labelFont);
            DrawTracked(g, (Label ?? "").ToUpperInvariant(), _labelFont, UiTheme.Muted,
                        inner.X, y, 0.7f);
            y += labelH + 2;

            // ---- value, with prefix/suffix on the SAME baseline
            int bigAscent = Ascent(g, _valueFont);
            int smallAscent = Ascent(g, _affixFont);
            int baseline = y + bigAscent;

            int x = inner.X;
            if (!string.IsNullOrEmpty(Prefix))
                x += DrawAt(g, Prefix, _affixFont, UiTheme.Muted, x, baseline - smallAscent);
            x += DrawAt(g, Value ?? "", _valueFont, ValueColor, x, y);
            if (!string.IsNullOrEmpty(Suffix))
                DrawAt(g, Suffix, _affixFont, UiTheme.Muted, x, baseline - smallAscent);

            // Only the ascent + a little descent, not the full line box: the 24pt line box is
            // ~44px and the card is 172px tall, which leaves no room for the sparkline.
            y += bigAscent + 3 + 2;

            // ---- caption
            Font capFont = CaptionIsAction ? _capBoldFont : _captionFont;
            int capH = LineHeight(g, capFont);
            TextRenderer.DrawText(g, Caption ?? "", capFont,
                new Rectangle(inner.X, y, inner.Width, capH + 2), CaptionColor,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            // ---- sparkline, bottom-anchored so it can never push the numbers around.
            // At the design height these two land exactly flush, so the test is >= — with a
            // strict > the whole sparkline silently vanished off every card.
            var spark = new Rectangle(inner.X, inner.Bottom - 22, inner.Width, 22);
            if (spark.Top >= y + capH) DrawSpark(g, spark);
        }

        // ------------------------------------------------------------------- text
        /// <summary>Draws text and returns its advance width.</summary>
        private static int DrawAt(Graphics g, string s, Font f, Color c, int x, int y)
        {
            Size m = TextRenderer.MeasureText(g, s, f, new Size(int.MaxValue, int.MaxValue),
                                              TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, s, f, new Point(x, y), c, TextFormatFlags.NoPadding);
            return m.Width;
        }

        /// <summary>
        /// Per-character draw so a small uppercase label can carry letter-spacing. GDI text has
        /// no tracking, and at 11px an untracked uppercase label reads as a solid block.
        /// </summary>
        private static void DrawTracked(Graphics g, string s, Font f, Color c, int x, int y, float track)
        {
            float fx = x;
            foreach (char ch in s)
            {
                string one = ch.ToString();
                TextRenderer.DrawText(g, one, f, new Point((int)Math.Round(fx), y), c,
                                      TextFormatFlags.NoPadding);
                fx += TextRenderer.MeasureText(g, one, f, new Size(int.MaxValue, int.MaxValue),
                                               TextFormatFlags.NoPadding).Width + track;
            }
        }

        private static int LineHeight(Graphics g, Font f)
        {
            return TextRenderer.MeasureText(g, "Hg", f, new Size(int.MaxValue, int.MaxValue),
                                            TextFormatFlags.NoPadding).Height;
        }

        /// <summary>
        /// Distance from the top of a drawn text box to its baseline. Two fonts positioned by
        /// this share a baseline, which is the whole reason the currency card can mix sizes.
        /// </summary>
        private static int Ascent(Graphics g, Font f)
        {
            FontFamily fam = f.FontFamily;
            float lineSpacing = fam.GetLineSpacing(f.Style);
            if (lineSpacing <= 0) return LineHeight(g, f);
            return (int)Math.Round(LineHeight(g, f) * fam.GetCellAscent(f.Style) / lineSpacing);
        }

        // ------------------------------------------------------------------ icons
        private void DrawIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, 2.1f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (IconKind)
                {
                    case Icon.QueuePerson:
                        // a head + shoulders, with two queue lines behind it
                        g.DrawEllipse(pen, r.X + 1.5f, r.Y + 1f, 5.5f, 5.5f);
                        g.DrawArc(pen, r.X + 0.5f, r.Y + 8f, 7.5f, 7.5f, 180, 180);
                        g.DrawLine(pen, r.X + 10.5f, r.Y + 4f, r.Right - 0.5f, r.Y + 4f);
                        g.DrawLine(pen, r.X + 10.5f, r.Y + 8.5f, r.Right - 0.5f, r.Y + 8.5f);
                        g.DrawLine(pen, r.X + 10.5f, r.Y + 13f, r.Right - 3.5f, r.Y + 13f);
                        break;

                    case Icon.DocumentTick:
                        // a page with a folded corner and a tick inside
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(r.X + 2.5f, r.Y + 0.5f, r.X + 9.5f, r.Y + 0.5f);
                            path.AddLine(r.X + 9.5f, r.Y + 0.5f, r.X + 13.5f, r.Y + 4.5f);
                            path.AddLine(r.X + 13.5f, r.Y + 4.5f, r.X + 13.5f, r.Bottom - 0.5f);
                            path.AddLine(r.X + 13.5f, r.Bottom - 0.5f, r.X + 2.5f, r.Bottom - 0.5f);
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                        }
                        g.DrawLine(pen, r.X + 9.5f, r.Y + 0.5f, r.X + 9.5f, r.Y + 4.5f);
                        g.DrawLine(pen, r.X + 9.5f, r.Y + 4.5f, r.X + 13.5f, r.Y + 4.5f);
                        g.DrawLines(pen, new[]
                        {
                            new PointF(r.X + 5f,  r.Y + 10.5f),
                            new PointF(r.X + 7.5f, r.Y + 13f),
                            new PointF(r.X + 11.5f, r.Y + 8f)
                        });
                        break;

                    case Icon.PesoCoin:
                        // a coin with a P and the peso bar through it
                        g.DrawEllipse(pen, r.X + 1f, r.Y + 1f, r.Width - 2.5f, r.Height - 2.5f);
                        g.DrawLine(pen, r.X + 6f, r.Y + 4.5f, r.X + 6f, r.Bottom - 4.5f);
                        g.DrawArc(pen, r.X + 5f, r.Y + 4.5f, 6f, 5.5f, 270, 180);
                        g.DrawLine(pen, r.X + 4f, r.Y + 7.5f, r.X + 12f, r.Y + 7.5f);
                        break;

                    default: // InboxTray
                        g.DrawLines(pen, new[]
                        {
                            new PointF(r.X + 1f,  r.Y + 9.5f),
                            new PointF(r.X + 4.5f, r.Y + 1.5f),
                            new PointF(r.Right - 4.5f, r.Y + 1.5f),
                            new PointF(r.Right - 1f, r.Y + 9.5f)
                        });
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(r.X + 1f, r.Y + 9.5f, r.X + 5.5f, r.Y + 9.5f);
                            path.AddLine(r.X + 5.5f, r.Y + 9.5f, r.X + 6.8f, r.Y + 12f);
                            path.AddLine(r.X + 6.8f, r.Y + 12f, r.Right - 6.8f, r.Y + 12f);
                            path.AddLine(r.Right - 6.8f, r.Y + 12f, r.Right - 5.5f, r.Y + 9.5f);
                            path.AddLine(r.Right - 5.5f, r.Y + 9.5f, r.Right - 1f, r.Y + 9.5f);
                            path.AddLine(r.Right - 1f, r.Y + 9.5f, r.Right - 1f, r.Bottom - 1.5f);
                            path.AddLine(r.Right - 1f, r.Bottom - 1.5f, r.X + 1f, r.Bottom - 1.5f);
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                        }
                        break;
                }
            }
        }

        // -------------------------------------------------------------- sparkline
        private void DrawSpark(Graphics g, Rectangle r)
        {
            if (SparkStyle == Spark.None || _spark.Length < 2) return;

            double max = 0;
            foreach (double v in _spark) if (v > max) max = v;
            if (max <= 0) return;      // nothing measured — draw nothing, not a flat line

            if (SparkStyle == Spark.Bars)
            {
                int n = _spark.Length;
                float slot = r.Width / (float)n;
                float w = Math.Max(3f, slot * 0.62f);
                using (var b = new SolidBrush(Color.FromArgb(217, SparkColor)))    // 85%
                    for (int i = 0; i < n; i++)
                    {
                        // A day with nothing registered draws NOTHING. A minimum-height stub
                        // would read as "one" at this size, which is the wrong number.
                        if (_spark[i] <= 0) continue;
                        float h = (float)(_spark[i] / max) * (r.Height - 2);
                        if (h < 3f) h = 3f;
                        var bar = new RectangleF(r.X + i * slot + (slot - w) / 2f,
                                                 r.Bottom - h, w, h);
                        using (GraphicsPath p = RoundedRect(Rectangle.Round(bar), 2))
                            g.FillPath(b, p);
                    }
                return;
            }

            // Area line
            var pts = new PointF[_spark.Length];
            for (int i = 0; i < _spark.Length; i++)
            {
                float x = r.X + (r.Width - 1f) * i / (_spark.Length - 1);
                float y = r.Bottom - 1f - (float)(_spark[i] / max) * (r.Height - 3f);
                pts[i] = new PointF(x, y);
            }

            using (var fillPath = new GraphicsPath())
            {
                fillPath.AddLines(pts);
                fillPath.AddLine(pts[pts.Length - 1].X, r.Bottom, pts[0].X, r.Bottom);
                fillPath.CloseFigure();
                using (var b = new SolidBrush(Color.FromArgb(20, SparkColor)))     // ~8%
                    g.FillPath(b, fillPath);
            }
            using (var pen = new Pen(SparkColor, 1.8f) { LineJoin = LineJoin.Round })
                g.DrawLines(pen, pts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _labelFont.Dispose(); _valueFont.Dispose(); _affixFont.Dispose();
                _captionFont.Dispose(); _capBoldFont.Dispose(); _deltaFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
