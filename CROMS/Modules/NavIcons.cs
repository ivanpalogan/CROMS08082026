using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CROMS.Modules
{
    /// <summary>
    /// One small monochrome line-art icon per sidebar module key — same "no emoji, single-color
    /// line art" convention the Login/Launcher/Kiosk screens already use, so the sidebar reads as
    /// part of the same app rather than a different visual language. Hand-drawn with GDI+ (no
    /// font/image asset) so it never depends on what's installed on the deployment PC.
    /// </summary>
    public static class NavIcons
    {
        public static Action<Graphics, RectangleF, Color> For(string moduleKey)
        {
            switch (moduleKey)
            {
                case "dashboard": return Dashboard;
                case "queue": return Queue;
                case "transactions": return Transactions;
                case "certrequest": return Inbox;
                case "release": return CheckCircle;
                case "breqs": return Layers;
                case "birth": return Document;
                case "marriage": return Heart;
                case "death": return PlusCircle;
                case "petitions": return Flag;
                case "books": return Book;
                case "search": return Search;
                case "ocr": return Scan;
                case "fees": return Wallet;
                case "reports": return BarChart;
                case "masterfiles": return Database;
                case "certtemplates": return Layout;
                case "settings": return Gear;
                case "users": return ShieldPerson;
                case "archive": return ArchiveBox;
                default: return Dot;
            }
        }

        private static Pen P(Color c) => new Pen(c, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

        private static void Dot(Graphics g, RectangleF r, Color c)
        {
            using (var b = new SolidBrush(c))
                g.FillEllipse(b, r.X + r.Width * 0.35f, r.Y + r.Height * 0.35f, r.Width * 0.3f, r.Height * 0.3f);
        }

        private static void Dashboard(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float m = r.Width * 0.08f, gap = r.Width * 0.14f;
                float cell = (r.Width - gap) / 2f - m;
                g.DrawRectangle(pen, r.X + m, r.Y + m, cell, cell);
                g.DrawRectangle(pen, r.X + m + cell + gap, r.Y + m, cell, cell);
                g.DrawRectangle(pen, r.X + m, r.Y + m + cell + gap, cell, cell);
                g.DrawRectangle(pen, r.X + m + cell + gap, r.Y + m + cell + gap, cell, cell);
            }
        }

        private static void Queue(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawEllipse(pen, r.X + r.Width * 0.28f, r.Y, r.Width * 0.44f, r.Width * 0.44f);
                using (var path = new GraphicsPath())
                {
                    path.AddArc(r.X + r.Width * 0.05f, r.Y + r.Height * 0.5f, r.Width * 0.9f, r.Height * 0.62f, 180, 180);
                    g.DrawPath(pen, path);
                }
            }
        }

        private static void Transactions(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float y1 = r.Y + r.Height * 0.22f, y2 = r.Y + r.Height * 0.52f, y3 = r.Y + r.Height * 0.82f;
                g.DrawLine(pen, r.X, y1, r.Right, y1);
                g.DrawLine(pen, r.X, y2, r.Right - r.Width * 0.25f, y2);
                g.DrawLine(pen, r.X, y3, r.Right - r.Width * 0.45f, y3);
            }
        }

        private static void Inbox(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                using (var path = new GraphicsPath())
                {
                    path.AddLine(r.X, r.Y + r.Height * 0.55f, r.X + r.Width * 0.32f, r.Y + r.Height * 0.55f);
                    path.AddLine(r.X + r.Width * 0.32f, r.Y + r.Height * 0.55f, r.X + r.Width * 0.42f, r.Y + r.Height * 0.75f);
                    path.AddLine(r.X + r.Width * 0.42f, r.Y + r.Height * 0.75f, r.Right - r.Width * 0.42f, r.Y + r.Height * 0.75f);
                    path.AddLine(r.Right - r.Width * 0.42f, r.Y + r.Height * 0.75f, r.Right - r.Width * 0.32f, r.Y + r.Height * 0.55f);
                    path.AddLine(r.Right - r.Width * 0.32f, r.Y + r.Height * 0.55f, r.Right, r.Y + r.Height * 0.55f);
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, r.X, r.Y + r.Height * 0.55f, r.X, r.Bottom - 1);
                g.DrawLine(pen, r.Right, r.Y + r.Height * 0.55f, r.Right, r.Bottom - 1);
                g.DrawLine(pen, r.X, r.Bottom - 1, r.Right, r.Bottom - 1);
                g.DrawLine(pen, r.X + r.Width * 0.18f, r.Y, r.Right - r.Width * 0.18f, r.Y);
                g.DrawLine(pen, r.X + r.Width * 0.18f, r.Y, r.X, r.Y + r.Height * 0.55f);
                g.DrawLine(pen, r.Right - r.Width * 0.18f, r.Y, r.Right, r.Y + r.Height * 0.55f);
            }
        }

        private static void CheckCircle(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawEllipse(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
                g.DrawLines(pen, new[]
                {
                    new PointF(r.X + r.Width * 0.27f, r.Y + r.Height * 0.52f),
                    new PointF(r.X + r.Width * 0.44f, r.Y + r.Height * 0.68f),
                    new PointF(r.X + r.Width * 0.74f, r.Y + r.Height * 0.32f)
                });
            }
        }

        private static void Layers(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                DrawDiamond(g, pen, r.X, r.Y, r.Width, r.Height * 0.4f);
                DrawDiamond(g, pen, r.X, r.Y + r.Height * 0.3f, r.Width, r.Height * 0.4f);
                DrawDiamond(g, pen, r.X, r.Y + r.Height * 0.6f, r.Width, r.Height * 0.4f);
            }
        }

        private static void DrawDiamond(Graphics g, Pen pen, float x, float y, float w, float h)
        {
            g.DrawLines(pen, new[]
            {
                new PointF(x + w / 2f, y),
                new PointF(x + w, y + h / 2f),
                new PointF(x + w / 2f, y + h),
                new PointF(x, y + h / 2f),
                new PointF(x + w / 2f, y)
            });
        }

        private static void Document(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float fold = r.Width * 0.3f;
                using (var path = new GraphicsPath())
                {
                    path.AddLine(r.X + r.Width * 0.15f, r.Y, r.Right - fold, r.Y);
                    path.AddLine(r.Right - fold, r.Y, r.Right - r.Width * 0.15f, r.Y + fold * 0.75f);
                    path.AddLine(r.Right - r.Width * 0.15f, r.Y + fold * 0.75f, r.Right - r.Width * 0.15f, r.Bottom);
                    path.AddLine(r.Right - r.Width * 0.15f, r.Bottom, r.X + r.Width * 0.15f, r.Bottom);
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }
                float lx = r.X + r.Width * 0.3f, rx = r.Right - r.Width * 0.3f;
                g.DrawLine(pen, lx, r.Y + r.Height * 0.5f, rx, r.Y + r.Height * 0.5f);
                g.DrawLine(pen, lx, r.Y + r.Height * 0.7f, rx - r.Width * 0.15f, r.Y + r.Height * 0.7f);
            }
        }

        private static void Heart(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            using (var path = new GraphicsPath())
            {
                float cx = r.X + r.Width / 2f, top = r.Y + r.Height * 0.28f;
                path.AddBezier(new PointF(cx, r.Bottom), new PointF(r.X, r.Y + r.Height * 0.55f),
                                new PointF(r.X, r.Y), new PointF(cx, top));
                path.AddBezier(new PointF(cx, top), new PointF(r.Right, r.Y),
                                new PointF(r.Right, r.Y + r.Height * 0.55f), new PointF(cx, r.Bottom));
                g.DrawPath(pen, path);
            }
        }

        private static void PlusCircle(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawEllipse(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, half = r.Width * 0.2f;
                g.DrawLine(pen, cx - half, cy, cx + half, cy);
                g.DrawLine(pen, cx, cy - half, cx, cy + half);
            }
        }

        private static void Flag(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawLine(pen, r.X + r.Width * 0.2f, r.Y, r.X + r.Width * 0.2f, r.Bottom);
                using (var path = new GraphicsPath())
                {
                    path.AddLine(r.X + r.Width * 0.2f, r.Y + r.Height * 0.08f, r.Right, r.Y + r.Height * 0.08f);
                    path.AddLine(r.Right, r.Y + r.Height * 0.08f, r.Right - r.Width * 0.18f, r.Y + r.Height * 0.32f);
                    path.AddLine(r.Right - r.Width * 0.18f, r.Y + r.Height * 0.32f, r.Right, r.Y + r.Height * 0.56f);
                    path.AddLine(r.Right, r.Y + r.Height * 0.56f, r.X + r.Width * 0.2f, r.Y + r.Height * 0.56f);
                    g.DrawPath(pen, path);
                }
            }
        }

        private static void Book(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float cx = r.X + r.Width / 2f;
                using (var path = new GraphicsPath())
                {
                    path.AddBezier(new PointF(r.X, r.Y + r.Height * 0.08f), new PointF(cx - r.Width * 0.14f, r.Y),
                                    new PointF(cx - r.Width * 0.02f, r.Y + r.Height * 0.15f), new PointF(cx, r.Y + r.Height * 0.2f));
                    path.AddBezier(new PointF(cx, r.Y + r.Height * 0.2f), new PointF(cx + r.Width * 0.02f, r.Y + r.Height * 0.15f),
                                    new PointF(cx + r.Width * 0.14f, r.Y), new PointF(r.Right, r.Y + r.Height * 0.08f));
                    path.AddLine(r.Right, r.Y + r.Height * 0.08f, r.Right, r.Bottom - r.Height * 0.06f);
                    path.AddBezier(new PointF(r.Right, r.Bottom - r.Height * 0.06f), new PointF(cx + r.Width * 0.14f, r.Bottom - r.Height * 0.16f),
                                    new PointF(cx + r.Width * 0.02f, r.Bottom - r.Height * 0.05f), new PointF(cx, r.Bottom));
                    path.AddBezier(new PointF(cx, r.Bottom), new PointF(cx - r.Width * 0.02f, r.Bottom - r.Height * 0.05f),
                                    new PointF(cx - r.Width * 0.14f, r.Bottom - r.Height * 0.16f), new PointF(r.X, r.Bottom - r.Height * 0.06f));
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, cx, r.Y + r.Height * 0.2f, cx, r.Bottom);
            }
        }

        private static void Search(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float d = r.Width * 0.62f;
                g.DrawEllipse(pen, r.X, r.Y, d, d);
                g.DrawLine(pen, r.X + d * 0.82f, r.Y + d * 0.82f, r.Right, r.Bottom);
            }
        }

        private static void Scan(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float m = r.Width * 0.08f, corner = r.Width * 0.28f;
                // four corner brackets, like a scan-frame
                g.DrawLines(pen, new[] { new PointF(r.X + m, r.Y + m + corner), new PointF(r.X + m, r.Y + m), new PointF(r.X + m + corner, r.Y + m) });
                g.DrawLines(pen, new[] { new PointF(r.Right - m - corner, r.Y + m), new PointF(r.Right - m, r.Y + m), new PointF(r.Right - m, r.Y + m + corner) });
                g.DrawLines(pen, new[] { new PointF(r.X + m, r.Bottom - m - corner), new PointF(r.X + m, r.Bottom - m), new PointF(r.X + m + corner, r.Bottom - m) });
                g.DrawLines(pen, new[] { new PointF(r.Right - m - corner, r.Bottom - m), new PointF(r.Right - m, r.Bottom - m), new PointF(r.Right - m, r.Bottom - m - corner) });
                g.DrawLine(pen, r.X + m, r.Y + r.Height / 2f, r.Right - m, r.Y + r.Height / 2f);
            }
        }

        private static void Wallet(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                var body = new RectangleF(r.X, r.Y + r.Height * 0.12f, r.Width, r.Height * 0.76f);
                using (var path = CardPanel.RoundedRect(Rectangle.Round(body), (int)(r.Height * 0.12f)))
                    g.DrawPath(pen, path);
                float flapW = r.Width * 0.32f;
                g.DrawEllipse(pen, r.Right - flapW, body.Y + body.Height * 0.28f, flapW * 0.62f, body.Height * 0.44f);
            }
        }

        private static void BarChart(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawLine(pen, r.X, r.Bottom, r.Right, r.Bottom);
                float w = r.Width * 0.2f, gap = r.Width * 0.12f;
                g.DrawLine(pen, r.X + gap, r.Bottom, r.X + gap, r.Y + r.Height * 0.45f);
                g.DrawLine(pen, r.X + gap * 2 + w, r.Bottom, r.X + gap * 2 + w, r.Y + r.Height * 0.15f);
                g.DrawLine(pen, r.X + gap * 3 + w * 2, r.Bottom, r.X + gap * 3 + w * 2, r.Y + r.Height * 0.65f);
            }
        }

        private static void Database(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float rx = r.Width / 2f, ry = r.Height * 0.14f;
                float cx = r.X + rx;
                g.DrawEllipse(pen, r.X, r.Y, r.Width - 1, ry * 2);
                g.DrawLine(pen, r.X, r.Y + ry, r.X, r.Bottom - ry);
                g.DrawLine(pen, r.Right - 1, r.Y + ry, r.Right - 1, r.Bottom - ry);
                g.DrawArc(pen, r.X, r.Bottom - ry * 2 - 1, r.Width - 1, ry * 2, 0, 180);
                g.DrawArc(pen, r.X, r.Y + r.Height * 0.36f - ry, r.Width - 1, ry * 2, 0, 180);
            }
        }

        private static void Layout(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
                float headH = r.Height * 0.3f;
                g.DrawLine(pen, r.X, r.Y + headH, r.Right, r.Y + headH);
                g.DrawLine(pen, r.X + r.Width * 0.4f, r.Y + headH, r.X + r.Width * 0.4f, r.Bottom);
            }
        }

        private static void Gear(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                float outer = r.Width * 0.44f, inner = r.Width * 0.18f;
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    float x1 = cx + (float)(Math.Cos(a) * outer * 0.72f), y1 = cy + (float)(Math.Sin(a) * outer * 0.72f);
                    float x2 = cx + (float)(Math.Cos(a) * outer), y2 = cy + (float)(Math.Sin(a) * outer);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
                g.DrawEllipse(pen, cx - outer * 0.72f, cy - outer * 0.72f, outer * 1.44f, outer * 1.44f);
                g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);
            }
        }

        private static void ShieldPerson(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float cx = r.X + r.Width / 2f;
                using (var path = new GraphicsPath())
                {
                    path.AddLine(cx, r.Y, r.Right, r.Y + r.Height * 0.2f);
                    path.AddLine(r.Right, r.Y + r.Height * 0.2f, r.Right, r.Y + r.Height * 0.55f);
                    path.AddBezier(new PointF(r.Right, r.Y + r.Height * 0.55f), new PointF(r.Right, r.Bottom * 0.9f),
                                    new PointF(cx + 2, r.Bottom), new PointF(cx, r.Bottom));
                    path.AddBezier(new PointF(cx, r.Bottom), new PointF(cx - 2, r.Bottom),
                                    new PointF(r.X, r.Bottom * 0.9f), new PointF(r.X, r.Y + r.Height * 0.55f));
                    path.AddLine(r.X, r.Y + r.Height * 0.55f, r.X, r.Y + r.Height * 0.2f);
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }
                g.DrawEllipse(pen, cx - r.Width * 0.12f, r.Y + r.Height * 0.24f, r.Width * 0.24f, r.Width * 0.24f);
                g.DrawArc(pen, cx - r.Width * 0.18f, r.Y + r.Height * 0.42f, r.Width * 0.36f, r.Height * 0.28f, 180, 180);
            }
        }

        private static void ArchiveBox(Graphics g, RectangleF r, Color c)
        {
            using (var pen = P(c))
            {
                float lidH = r.Height * 0.22f;
                g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, lidH);
                g.DrawRectangle(pen, r.X + r.Width * 0.06f, r.Y + lidH, r.Width * 0.88f, r.Height - lidH - 1);
                g.DrawLine(pen, r.X + r.Width * 0.36f, r.Y + lidH + r.Height * 0.18f,
                                 r.Right - r.Width * 0.36f, r.Y + lidH + r.Height * 0.18f);
            }
        }
    }
}
