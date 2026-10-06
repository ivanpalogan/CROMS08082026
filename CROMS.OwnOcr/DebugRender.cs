using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Draws what the engine did, stage by stage, as one stacked picture:
    /// 1 the image as given, 2 the thresholded ink (after enlarging), 3 the printed rules it
    /// found (red), 4 the cleaned ink with the chosen line and a box round every character,
    /// 5 the normalised picture of every character exactly as the classifier will see it.
    /// <para/>
    /// Every stage is drawn because the question is never "did it work" but "where did it
    /// stop working", and only the intermediate pictures answer that.
    /// </summary>
    public static class DebugRender
    {
        private const int CaptionHeight = 20;
        private const int CellPx = 3;   // each 20x20 glyph picture is drawn 3x

        public static Bitmap Render(Bitmap original, Analysis a, string title)
        {
            int w = a.Ink.Width, h = a.Ink.Height;
            int scale = Math.Max(1, Math.Min(6, 1000 / Math.Max(1, w)));
            int pw = w * scale, ph = h * scale;
            int canvasW = Math.Max(pw, 560);

            // Panel 5 wraps its glyph pictures, so its height depends on how many there are.
            int cellDraw = GlyphCell.Size * CellPx;
            int perRow = Math.Max(1, (canvasW - 8) / (cellDraw + 6));
            int rows = Math.Max(1, (a.Cells.Count + perRow - 1) / perRow);
            int stripH = rows * (cellDraw + 6) + CaptionHeight;
            int total = 4 * (ph + CaptionHeight) + 24 + stripH + 10;

            var canvas = new Bitmap(canvasW, total, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(canvas))
            using (var font = new Font("Segoe UI", 9f))
            using (var bold = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.Clear(Color.FromArgb(244, 246, 249));
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                int y = 4;
                g.DrawString(title, bold, Brushes.Black, 4, y); y += CaptionHeight;

                // 1. original
                g.DrawString("1  image as given   " + original.Width + " x " + original.Height +
                             (a.Scale > 1 ? "   enlarged x" + a.Scale.ToString("0.0") + " for reading" : ""),
                             font, Brushes.DimGray, 4, y - 2);
                y += CaptionHeight - 4;
                g.DrawImage(original, new Rectangle(0, y, pw, ph));
                y += ph + 6;

                // 2. ink
                g.DrawString("2  adaptive threshold   ink " + Percent(a.Ink.InkCount(), w * h), font, Brushes.DimGray, 4, y - 2);
                y += CaptionHeight - 4;
                using (Bitmap ink = a.Ink.ToBitmap())
                    g.DrawImage(ink, new Rectangle(0, y, pw, ph));
                y += ph + 6;

                // 3. rules in red over the ink in grey
                g.DrawString("3  table rules found: " + a.Rules.HorizontalRules + " horizontal, " +
                             a.Rules.VerticalRules + " vertical   (red = removed)", font, Brushes.DimGray, 4, y - 2);
                y += CaptionHeight - 4;
                using (Bitmap rb = RulesBitmap(a))
                    g.DrawImage(rb, new Rectangle(0, y, pw, ph));
                y += ph + 6;

                // 4. cleaned ink + line + boxes
                string lineNote = a.Line == null ? "   NO LINE FOUND"
                    : "   cap height " + a.Line.CapHeight.ToString("0") + " px, typical width " +
                      a.Line.TypicalWidth.ToString("0") + " px";
                g.DrawString("4  chosen line: " + a.Cells.Count + " characters" + lineNote +
                             "   (green = whole, magenta = cut, grey = other lines, orange = oversized)",
                             font, Brushes.DimGray, 4, y - 2);
                y += CaptionHeight - 4;
                using (Bitmap cl = a.Cleaned.ToBitmap())
                    g.DrawImage(cl, new Rectangle(0, y, pw, ph));
                DrawBoxes(g, a, scale, y, pw);
                y += ph + 6;

                // 5. what the classifier will see
                g.DrawString("5  normalised characters (20 x 20, frame fixed to the line)", font, Brushes.DimGray, 4, y - 2);
                y += CaptionHeight - 4;
                for (int i = 0; i < a.Cells.Count; i++)
                {
                    int cx = 4 + (i % perRow) * (cellDraw + 6);
                    int cy = y + (i / perRow) * (cellDraw + 6);
                    using (Bitmap cb = CellBitmap(a.Cells[i]))
                        g.DrawImage(cb, new Rectangle(cx, cy, cellDraw, cellDraw));
                    using (var pen = new Pen(a.Cells[i].FromSplit ? Color.Magenta : Color.FromArgb(180, 180, 190), 1f))
                        g.DrawRectangle(pen, cx, cy, cellDraw, cellDraw);
                }
            }
            return canvas;
        }

        private static void DrawBoxes(Graphics g, Analysis a, int scale, int top, int width)
        {
            var inLine = new HashSet<Blob>(a.Line == null ? new List<Blob>() : a.Line.Blobs);
            using (var grey = new Pen(Color.FromArgb(170, 170, 180), 1f))
            using (var orange = new Pen(Color.FromArgb(240, 140, 0), 2f))
            using (var blue = new Pen(Color.FromArgb(150, 175, 235), 1f))
            using (var green = new Pen(Color.FromArgb(40, 160, 60), 1f))
            using (var magenta = new Pen(Color.Magenta, 1f))
            using (var dash = new Pen(Color.FromArgb(40, 100, 220), 1f) { DashStyle = DashStyle.Dash })
            {
                foreach (Blob b in a.Noise)
                    g.DrawRectangle(blue, b.Left * scale, top + b.Top * scale, b.Width * scale, b.Height * scale);
                foreach (Blob b in a.Glyphs)
                {
                    if (!inLine.Contains(b))
                        g.DrawRectangle(grey, b.Left * scale, top + b.Top * scale, b.Width * scale, b.Height * scale);
                    else if (a.Suspicious.Contains(b))
                        g.DrawRectangle(orange, b.Left * scale, top + b.Top * scale, b.Width * scale, b.Height * scale);
                }
                foreach (GlyphCell c in a.Cells)
                    g.DrawRectangle(c.FromSplit ? magenta : green, c.Left * scale, top + c.Top * scale,
                        c.Width * scale, c.Height * scale);

                if (a.Line != null)
                {
                    float yc = top + (float)a.Line.CapTop * scale, yb = top + (float)a.Line.Baseline * scale;
                    g.DrawLine(dash, 0, yc, width, yc);
                    g.DrawLine(dash, 0, yb, width, yb);
                }
            }
        }

        private static Bitmap CellBitmap(GlyphCell c)
        {
            int n = GlyphCell.Size;
            var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int v = 255 - (int)Math.Round(Math.Max(0f, Math.Min(1f, c.Image[y * n + x])) * 255);
                    bmp.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            return bmp;
        }

        private static Bitmap RulesBitmap(Analysis a)
        {
            int w = a.Ink.Width, h = a.Ink.Height;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                var buf = new byte[stride * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * stride + x * 4;
                        int p = y * w + x;
                        byte r = 255, gr = 255, b = 255;
                        if (a.Rules.Mask[p] != 0) { r = 220; gr = 40; b = 40; }
                        else if (a.Ink.Ink[p] != 0) { r = 90; gr = 90; b = 90; }
                        buf[i] = b; buf[i + 1] = gr; buf[i + 2] = r; buf[i + 3] = 255;
                    }
                }
                Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        private static string Percent(int part, int whole)
        {
            return whole == 0 ? "0%" : (part * 100.0 / whole).ToString("0.0") + "%";
        }
    }
}
