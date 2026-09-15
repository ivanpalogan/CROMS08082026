using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// Draws a <see cref="CertTemplate"/> onto a <see cref="Graphics"/> in POINTS — the
    /// one place that turns a saved template into pixels/ink, used by the visual
    /// designer's canvas, its Preview dialog, and (once a form's template designer is
    /// wired into printing) the real certificate output. Using the same method for the
    /// on-screen design and the eventual print is what makes this a genuine WYSIWYG
    /// editor rather than a layout tool that merely looks close.
    /// <para/>
    /// <paramref name="values"/> supplies the live text for every Field element — sample
    /// placeholder text in Preview/design mode, the real record's values when actually
    /// printing. A Field with no matching key just shows its placeholder
    /// ("{{ColumnName}}") so a template can be designed before the data row exists.
    /// </summary>
    public static class TemplateRenderer
    {
        public static void Draw(Graphics g, CertTemplate t, IDictionary<string, string> values,
                                 bool designMode, string selectedId, Func<int, Image> loadTemplateImage)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var left = new StringFormat(StringFormat.GenericTypographic))
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center })
            using (var right = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far })
            {
                foreach (TemplateElement el in t.Elements.OrderBy(e => e.ZIndex))
                {
                    var rect = new RectangleF(el.X, el.Y, el.Width, el.Height);
                    StringFormat fmt = el.Align == "Center" ? center : el.Align == "Right" ? right : left;

                    switch (el.Kind)
                    {
                        case "Text":
                            DrawText(g, el.Text ?? "", el, rect, fmt);
                            break;

                        case "Field":
                        {
                            string val = null;
                            if (values != null && el.Column != null)
                                values.TryGetValue(el.Column, out val);
                            string shown = !string.IsNullOrEmpty(val) ? val
                                          : designMode ? "{{" + (el.Column ?? "Field") + "}}"
                                          : "";
                            DrawText(g, shown, el, rect, fmt, string.IsNullOrEmpty(val) && designMode);
                            break;
                        }

                        case "Image":
                        {
                            Image img = ResolveImage(el, loadTemplateImage);
                            if (img != null)
                            {
                                RectangleF fit = FitPreservingAspect(img, rect, el.LockAspect);
                                g.DrawImage(img, fit);
                            }
                            else if (designMode)
                            {
                                using (var pen = new Pen(Color.FromArgb(180, 180, 180)) { DashStyle = DashStyle.Dash })
                                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                                using (var f = new Font("Segoe UI", 7f))
                                    g.DrawString("Image", f, Brushes.Gray, rect, center);
                            }
                            break;
                        }

                        case "Line":
                            using (var pen = new Pen(Color.Black, el.StrokeWidth))
                                g.DrawLine(pen, el.X, el.Y, el.X + el.Width, el.Y + el.Height);
                            break;

                        case "Rectangle":
                            using (var pen = new Pen(Color.Black, el.StrokeWidth))
                                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                            break;
                    }

                    if (designMode && el.Id == selectedId)
                        DrawSelection(g, rect);
                    else if (designMode)
                        DrawGhost(g, rect);
                }
            }
        }

        private static void DrawText(Graphics g, string text, TemplateElement el, RectangleF rect,
                                     StringFormat fmt, bool placeholder = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            FontStyle style = FontStyle.Regular;
            if (el.Bold) style |= FontStyle.Bold;
            if (el.Italic) style |= FontStyle.Italic;
            if (el.Underline) style |= FontStyle.Underline;
            Brush brush = placeholder ? Brushes.SlateGray : Brushes.Black;
            string family = el.FontFamily;
            try
            {
                using (var f = new Font(family, el.FontSize, style))
                    g.DrawString(text, f, brush, rect, fmt);
            }
            catch
            {
                using (var f = new Font("Arial", el.FontSize, style))
                    g.DrawString(text, f, brush, rect, fmt);
            }
        }

        private static Image ResolveImage(TemplateElement el, Func<int, Image> loadTemplateImage)
        {
            if (el.ImageId.HasValue && loadTemplateImage != null)
            {
                try { return loadTemplateImage(el.ImageId.Value); } catch { return null; }
            }
            if (!string.IsNullOrEmpty(el.OfficeAsset))
            {
                if (Enum.TryParse(el.OfficeAsset, out AssetKind kind))
                {
                    try { return OfficeAssets.Get(kind); } catch { return null; }
                }
            }
            return null;
        }

        private static RectangleF FitPreservingAspect(Image img, RectangleF box, bool lockAspect)
        {
            if (!lockAspect) return box;
            float scale = Math.Min(box.Width / img.Width, box.Height / img.Height);
            float w = img.Width * scale, h = img.Height * scale;
            return new RectangleF(box.X + (box.Width - w) / 2f, box.Y + (box.Height - h) / 2f, w, h);
        }

        private static void DrawSelection(Graphics g, RectangleF rect)
        {
            using (var pen = new Pen(Color.FromArgb(29, 78, 216), 1.4f) { DashStyle = DashStyle.Solid })
                g.DrawRectangle(pen, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            foreach (PointF h in HandlePoints(rect))
            {
                using (var b = new SolidBrush(Color.White))
                using (var p = new Pen(Color.FromArgb(29, 78, 216), 1.2f))
                {
                    var hr = new RectangleF(h.X - 3.5f, h.Y - 3.5f, 7, 7);
                    g.FillEllipse(b, hr);
                    g.DrawEllipse(p, hr);
                }
            }
        }

        private static void DrawGhost(Graphics g, RectangleF rect)
        {
            using (var pen = new Pen(Color.FromArgb(60, 29, 78, 216), 0.75f) { DashStyle = DashStyle.Dot })
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>The 8 resize-handle positions around a selected element's rectangle,
        /// in the same order <see cref="HandleAt"/> expects.</summary>
        public static IEnumerable<PointF> HandlePoints(RectangleF r)
        {
            yield return new PointF(r.Left, r.Top);
            yield return new PointF(r.Left + r.Width / 2, r.Top);
            yield return new PointF(r.Right, r.Top);
            yield return new PointF(r.Right, r.Top + r.Height / 2);
            yield return new PointF(r.Right, r.Bottom);
            yield return new PointF(r.Left + r.Width / 2, r.Bottom);
            yield return new PointF(r.Left, r.Bottom);
            yield return new PointF(r.Left, r.Top + r.Height / 2);
        }

        /// <summary>Which handle (0-7, matching <see cref="HandlePoints"/>) is under a
        /// point, or -1. Hit radius is generous — this is a mouse target, not a pixel test.</summary>
        public static int HandleAt(RectangleF r, PointF p, float hitRadius)
        {
            int i = 0;
            foreach (PointF h in HandlePoints(r))
            {
                if (Math.Abs(h.X - p.X) <= hitRadius && Math.Abs(h.Y - p.Y) <= hitRadius) return i;
                i++;
            }
            return -1;
        }
    }
}
