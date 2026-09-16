using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Single source of truth for the office's LCRO seal (Assets\lcro_logo.png, next to the
    /// built exe). Loaded ONCE per process and cached — every screen that shows the seal
    /// (sidebar brand mark, Dashboard header, ...) reads the same cached bitmap instead of each
    /// re-opening the file, so the logo can never "flicker missing" because one screen's read
    /// raced a rebuild that was still copying the file, and a stale/locked handle on one screen
    /// can't affect another.
    ///
    /// If the PNG is missing or fails to decode, <see cref="Logo"/> is null and every caller
    /// falls back to <see cref="DrawFallbackMark"/> — a drawn line-art mark — so the app never
    /// shows a blank hole where the seal should be.
    /// </summary>
    public static class BrandAssets
    {
        private static Image _logo;
        private static bool _attempted;

        /// <summary>The cached seal image, or null if none is deployed / it failed to decode.</summary>
        public static Image Logo
        {
            get
            {
                if (!_attempted)
                {
                    _attempted = true;
                    _logo = TryLoad();
                }
                return _logo;
            }
        }

        /// <summary>
        /// Re-reads the seal from disk. Call after a rebuild replaces the PNG while CROMS is
        /// still open, if a screen wants the newest copy without restarting the app.
        /// </summary>
        public static void Reload()
        {
            _attempted = true;
            var old = _logo;
            _logo = TryLoad();
            if (old != null && !ReferenceEquals(old, _logo)) old.Dispose();
        }

        private static Image TryLoad()
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "Assets", "lcro_logo.png");
                if (!File.Exists(path)) return null;
                // Clone into memory so the file handle from Image.FromFile is not held open for
                // the lifetime of the app — a locked PNG would block a later rebuild from
                // replacing it.
                using (var source = Image.FromFile(path))
                    return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Draws the seal circularly clipped into <paramref name="bounds"/>, or the drawn
        /// fallback mark when no seal is available. <paramref name="behind"/> is the colour
        /// painted first so the circle's corners blend into whatever the mark sits on.
        /// </summary>
        public static void DrawMark(Graphics g, Rectangle bounds, Color behind, Color fallbackFill, Color fallbackStroke)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(behind);

            Image logo = Logo;
            if (logo != null)
            {
                using (var clip = new GraphicsPath())
                {
                    clip.AddEllipse(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                    Region old = g.Clip;
                    g.SetClip(clip, CombineMode.Intersect);
                    g.DrawImage(logo, bounds);
                    g.Clip = old;
                }
                return;
            }

            using (GraphicsPath path = CardPanel.RoundedRect(bounds, Math.Max(4, bounds.Width / 4)))
            using (var b = new SolidBrush(fallbackFill))
                g.FillPath(b, path);

            var icon = Rectangle.Inflate(bounds, -(int)(bounds.Width * 0.18f), -(int)(bounds.Height * 0.18f));
            DrawFallbackMark(g, icon, fallbackStroke);
        }

        /// <summary>Plain line-art municipal building — the app's mark when no seal is deployed.</summary>
        public static void DrawFallbackMark(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.5f))
            {
                float cx = r.X + r.Width / 2f;
                float roofBaseY = r.Y + r.Height * 0.32f;
                float baseY = r.Bottom - r.Height * 0.06f;
                float halfW = r.Width * 0.44f;

                g.DrawLine(pen, cx, r.Y, cx - halfW, roofBaseY);
                g.DrawLine(pen, cx, r.Y, cx + halfW, roofBaseY);
                g.DrawLine(pen, cx - halfW, roofBaseY, cx + halfW, roofBaseY);

                float colTop = roofBaseY + r.Height * 0.08f;
                float[] colXs = { cx - halfW * 0.55f, cx, cx + halfW * 0.55f };
                foreach (float x in colXs) g.DrawLine(pen, x, colTop, x, baseY);

                g.DrawLine(pen, cx - halfW - 1.5f, baseY, cx + halfW + 1.5f, baseY);
            }
        }
    }
}
