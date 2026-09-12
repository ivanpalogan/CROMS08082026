using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Icons for the Step 1 service cards. Primary source is the embedded Material Icons
    /// Outlined font (see IconFont.cs) — a real, consistent, professionally-designed icon set,
    /// replacing both the original emoji-font glyphs and this file's earlier hand-drawn GDI+
    /// attempt. The hand-drawn paths below are kept ONLY as a fallback for the (should-never-
    /// happen) case where the embedded font resource is missing.
    /// </summary>
    public static class KioskIcons
    {
        // Phosphor Light codepoints (github.com/phosphor-icons/web, MIT). Chosen to match the
        // approved mockup's drawings as closely as the icon set allows -- see the note on each.
        // Built from the raw code point via char.ConvertFromUtf32 (not a literal glyph character
        // in source) so this file stays plain ASCII regardless of editor/encoding.
        private static readonly string GlyphNewReg   = char.ConvertFromUtf32(0xE34C); // note-pencil        (mockup: document + pencil)
        private static readonly string GlyphCtc      = char.ConvertFromUtf32(0xE23A); // file-text          (mockup: document + text lines)
        private static readonly string GlyphMarriage = char.ConvertFromUtf32(0xE2A8); // heart              (mockup: two rings -- Phosphor has no rings glyph)
        private static readonly string GlyphDeath    = char.ConvertFromUtf32(0xE766); // certificate        (mockup: candle -- certificate reads clearer for a registry)
        private static readonly string GlyphPetition = char.ConvertFromUtf32(0xE188); // check-square-offset (mockup: document + check badge)
        private static readonly string GlyphVerify   = char.ConvertFromUtf32(0xE30C); // magnifying-glass   (mockup: magnifying glass -- exact)
        private static readonly string GlyphClaim    = char.ConvertFromUtf32(0xE010); // tray-arrow-down    (mockup: tray + down arrow -- exact)

        private static string GlyphFor(string code)
        {
            switch (code)
            {
                case "BIRTHREG": return GlyphNewReg;
                case "MARRIAGE_APP": return GlyphNewReg;
                case "MARRIAGE_REG": return GlyphMarriage;
                case "LEGITIMATION": return GlyphMarriage;
                case "BREKS": return GlyphCtc;
                case "SUPPLEMENTAL": return GlyphNewReg;
                case "COURT_ORDER": return GlyphPetition;
                case "SUPPLEMENTAL_REPORT": return GlyphCtc;
                case "LEGAL_INSTRUMENTS": return GlyphDeath;
                case "LEGITIMATION_RA9255": return GlyphMarriage;
                case "NEWREG": return GlyphNewReg;
                case "CTC": return GlyphCtc;
                case "MARRIAGE": return GlyphMarriage;
                case "DEATH": return GlyphDeath;
                case "PETITION": return GlyphPetition;
                case "VERIFY": return GlyphVerify;
                case "CLAIM": return GlyphClaim;
                default: return GlyphCtc;
            }
        }

        /// <summary>Looks up the icon for a service code (see KioskCore.Catalogue), default = a plain document.</summary>
        public static Action<Graphics, RectangleF, Color> For(string code)
        {
            if (IconFont.IsAvailable)
            {
                string glyph = GlyphFor(code);
                return (g, r, c) => IconFont.Draw(g, r, glyph, c, r.Height * 0.9f);
            }

            switch (code)
            {
                case "BIRTHREG": return NewRegistration;
                case "MARRIAGE_APP": return NewRegistration;
                case "MARRIAGE_REG": return Marriage;
                case "LEGITIMATION": return Marriage;
                case "BREKS": return CertifiedCopy;
                case "SUPPLEMENTAL": return NewRegistration;
                case "COURT_ORDER": return Petition;
                case "SUPPLEMENTAL_REPORT": return CertifiedCopy;
                case "LEGAL_INSTRUMENTS": return Death;
                case "LEGITIMATION_RA9255": return Marriage;
                case "NEWREG": return NewRegistration;
                case "CTC": return CertifiedCopy;
                case "MARRIAGE": return Marriage;
                case "DEATH": return Death;
                case "PETITION": return Petition;
                case "VERIFY": return Verify;
                case "CLAIM": return Claim;
                default: return CertifiedCopy;
            }
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = radius * 2f;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>The plain document silhouette shared by several icons (a page with a folded corner).</summary>
        private static void DocumentPath(GraphicsPath path, RectangleF r, float fold)
        {
            path.AddLine(r.X, r.Y, r.Right - fold, r.Y);
            path.AddLine(r.Right - fold, r.Y, r.Right, r.Y + fold);
            path.AddLine(r.Right, r.Y + fold, r.Right, r.Bottom);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
        }

        private static void NewRegistration(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f) { LineJoin = LineJoin.Round })
            {
                var doc = new RectangleF(r.X, r.Y, r.Width * 0.62f, r.Height);
                float fold = doc.Width * 0.32f;
                using (var path = new GraphicsPath())
                {
                    DocumentPath(path, doc, fold);
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, doc.Right - fold, doc.Y, doc.Right - fold, doc.Y + fold);
                g.DrawLine(pen, doc.Right - fold, doc.Y + fold, doc.Right, doc.Y + fold);

                // pencil, lower-right, crossing the page corner
                PointF tip = new PointF(r.Right - r.Width * 0.06f, r.Bottom - r.Height * 0.04f);
                PointF tail = new PointF(r.X + r.Width * 0.42f, r.Bottom - r.Height * 0.62f);
                g.DrawLine(pen, tail, tip);
                using (var b = new SolidBrush(stroke))
                    g.FillPolygon(b, new[]
                    {
                        tip,
                        new PointF(tip.X - r.Width * 0.10f, tip.Y - r.Width * 0.02f),
                        new PointF(tip.X - r.Width * 0.02f, tip.Y - r.Width * 0.10f)
                    });
            }
        }

        private static void CertifiedCopy(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f) { LineJoin = LineJoin.Round })
            {
                var doc = new RectangleF(r.X + r.Width * 0.14f, r.Y, r.Width * 0.72f, r.Height);
                float fold = doc.Width * 0.34f;
                using (var path = new GraphicsPath())
                {
                    DocumentPath(path, doc, fold);
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, doc.Right - fold, doc.Y, doc.Right - fold, doc.Y + fold);
                g.DrawLine(pen, doc.Right - fold, doc.Y + fold, doc.Right, doc.Y + fold);

                float lineY1 = doc.Y + doc.Height * 0.55f;
                float lineY2 = doc.Y + doc.Height * 0.72f;
                g.DrawLine(pen, doc.X + doc.Width * 0.18f, lineY1, doc.Right - doc.Width * 0.18f, lineY1);
                g.DrawLine(pen, doc.X + doc.Width * 0.18f, lineY2, doc.Right - doc.Width * 0.32f, lineY2);
            }
        }

        private static void Marriage(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f))
            {
                float rad = r.Width * 0.28f;
                float cy = r.Y + r.Height * 0.55f;
                g.DrawEllipse(pen, r.X + r.Width * 0.14f, cy - rad, rad * 2, rad * 2);
                g.DrawEllipse(pen, r.Right - r.Width * 0.14f - rad * 2, cy - rad, rad * 2, rad * 2);
            }
        }

        private static void Death(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f) { LineJoin = LineJoin.Round })
            {
                var doc = new RectangleF(r.X + r.Width * 0.16f, r.Y, r.Width * 0.68f, r.Height * 0.82f);
                using (var path = RoundedRect(doc, 2f))
                    g.DrawPath(pen, path);

                float cx = doc.X + doc.Width / 2f;
                g.DrawLine(pen, cx, doc.Y + doc.Height * 0.28f, cx, doc.Y + doc.Height * 0.62f);
                g.DrawArc(pen, cx - doc.Width * 0.22f, doc.Y + doc.Height * 0.22f, doc.Width * 0.44f, doc.Width * 0.44f, 180, 180);

                // ribbon tails beneath the certificate
                g.DrawLine(pen, cx - 3f, doc.Bottom - 2f, cx - 6f, r.Bottom);
                g.DrawLine(pen, cx + 3f, doc.Bottom - 2f, cx + 6f, r.Bottom);
            }
        }

        private static void Petition(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                var doc = new RectangleF(r.X, r.Y, r.Width * 0.62f, r.Height);
                float fold = doc.Width * 0.32f;
                using (var path = new GraphicsPath())
                {
                    DocumentPath(path, doc, fold);
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, doc.Right - fold, doc.Y, doc.Right - fold, doc.Y + fold);
                g.DrawLine(pen, doc.Right - fold, doc.Y + fold, doc.Right, doc.Y + fold);
                g.DrawLine(pen, doc.X + doc.Width * 0.2f, doc.Y + doc.Height * 0.5f, doc.Right - doc.Width * 0.22f, doc.Y + doc.Height * 0.5f);

                // correction checkmark badge, lower-right
                float br = r.Width * 0.22f;
                var badge = new RectangleF(r.Right - br * 2, r.Bottom - br * 2, br * 2, br * 2);
                using (var b = new SolidBrush(stroke))
                    g.FillEllipse(b, badge);
                using (var white = new Pen(Color.White, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(white, new[]
                    {
                        new PointF(badge.X + badge.Width * 0.26f, badge.Y + badge.Height * 0.52f),
                        new PointF(badge.X + badge.Width * 0.44f, badge.Y + badge.Height * 0.70f),
                        new PointF(badge.X + badge.Width * 0.76f, badge.Y + badge.Height * 0.32f)
                    });
            }
        }

        private static void Verify(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                float rad = r.Width * 0.30f;
                var circle = new RectangleF(r.X, r.Y, rad * 2, rad * 2);
                g.DrawEllipse(pen, circle);
                PointF handleStart = new PointF(circle.Right - rad * 0.3f, circle.Bottom - rad * 0.3f);
                g.DrawLine(pen, handleStart, new PointF(r.Right, r.Bottom));
            }
        }

        private static void Claim(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                var tray = new RectangleF(r.X, r.Bottom - r.Height * 0.38f, r.Width, r.Height * 0.38f);
                using (var path = RoundedRect(tray, 2.5f))
                    g.DrawPath(pen, path);
                g.DrawLine(pen, tray.X + tray.Width * 0.28f, tray.Y, tray.X + tray.Width * 0.42f, tray.Y + tray.Height * 0.5f);
                g.DrawLine(pen, tray.Right - tray.Width * 0.28f, tray.Y, tray.Right - tray.Width * 0.42f, tray.Y + tray.Height * 0.5f);

                float cx = r.X + r.Width / 2f;
                g.DrawLine(pen, cx, r.Y, cx, r.Y + r.Height * 0.5f);
                g.DrawLine(pen, cx - r.Width * 0.18f, r.Y + r.Height * 0.32f, cx, r.Y + r.Height * 0.5f);
                g.DrawLine(pen, cx + r.Width * 0.18f, r.Y + r.Height * 0.32f, cx, r.Y + r.Height * 0.5f);
            }
        }
    }
}
