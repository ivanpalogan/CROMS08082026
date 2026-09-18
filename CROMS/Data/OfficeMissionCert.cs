using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// The office's Vision / Mission / Goal / Objectives / Core Values, as a printable,
    /// wall-poster-style page — built the SAME way Form3ACert/Form3BCert/Form3CCert are: a
    /// fixed list of Static cells (<see cref="Form3ACell"/>, the shape those three already
    /// use), registered in <see cref="TemplateStore.KnownForms"/> so it opens in the same
    /// visual Template Designer every certificate does, and shown through the same
    /// <see cref="TemplateReportBridge"/> so the operator's saved edits — reworded text,
    /// moved boxes, a different office logo — are what actually prints, not a hardcoded
    /// layout nobody can change without a rebuild.
    /// <para/>
    /// Unlike A1/A2/A3 this page carries NO per-record data at all — there is no "record" to
    /// certify, only the office's own standing statements — so every cell is Static text and
    /// <see cref="BuildTable"/> returns a table with no columns. That is also why this is
    /// the one form in the family that is safe to hand an operator to edit outright: there is
    /// no field mapping to get wrong.
    /// <para/>
    /// The wording is transcribed from the office's own printed poster (photographed by the
    /// user, 2026-09-18) — nothing here is invented copy.
    /// </summary>
    public static class OfficeMissionCert
    {
        public const string FormCode = "OFFICE-MVC";
        public const string FormName = "Mission, Vision, Goal, Objectives & Core Values";
        public const string RptFile = "OFFICE-MVC.rpt";
        public const string TableName = "office_mission_vision";
        public const float PageWidth = 612f, PageHeight = 792f;

        private static List<Form3ACell> _cells;
        public static IReadOnlyList<Form3ACell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<Form3ACell> BuildCells()
        {
            var c = new List<Form3ACell>();
            Action<string, float, float, float, float, float, bool> statC = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold, Center = true });
            Action<string, float, float, float, float, float, bool> stat = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<float, float, float> rule = (x, top, w) =>
                c.Add(new Form3ACell { Kind = "Rule", X = x, Top = top, Width = w, Height = 1f });
            Action<string, float, float, float, float> bundledImg = (file, x, top, w, h) =>
                c.Add(new Form3ACell { Kind = "BundledImage", ImageFile = file, X = x, Top = top, Width = w, Height = h });

            statC("MISSION, VISION, GOAL, OBJECTIVES & CORE VALUES", 40f, 18f, 532f, 16f, 13f, true);
            statC(OfficeAssets.Profile.HeaderLine, 40f, 36f, 532f, 12f, 9f, false);
            rule(40f, 54f, 532f);

            stat("VISION", 40f, 64f, 532f, 14f, 11f, true);
            stat("A SELF DIRECTING, DYNAMIC and COMPETENT civil registration for the entire humanity.",
                40f, 80f, 532f, 34f, 9.5f, false);

            stat("MISSION", 40f, 122f, 532f, 14f, 11f, true);
            stat("Promotion of the constituents' participation on civil registration and " +
                 "preservation of legal documents that are used to establish and protect the " +
                 "civil rights of the individual.",
                40f, 138f, 532f, 48f, 9.5f, false);

            stat("GOAL", 40f, 194f, 532f, 14f, 11f, true);
            stat("- To provide precise, judicious, accessible and consistent recording of vital " +
                 "events through the use of new and appropriate technology.",
                40f, 210f, 532f, 28f, 9.5f, false);
            stat("- To preserve the archive as the basis for the legal status of the people in " +
                 "the municipality.",
                40f, 240f, 532f, 28f, 9.5f, false);

            stat("OBJECTIVES", 40f, 276f, 532f, 14f, 11f, true);
            stat("- To implement policies, rules and regulations on civil registration.",
                40f, 292f, 532f, 16f, 9.5f, false);
            stat("- To publicize the civil registration program pursuant to the Registry Laws, " +
                 "the Civil Code and other pertinent laws, rules and regulations.",
                40f, 308f, 532f, 28f, 9.5f, false);
            stat("- To assist in the preparation of demographic and other statistics of the " +
                 "Local Government Unit.",
                40f, 338f, 532f, 28f, 9.5f, false);
            stat("- To develop plans and strategies and implement them upon the approval of the " +
                 "Local Chief Executive.",
                40f, 368f, 532f, 28f, 9.5f, false);

            rule(40f, 408f, 532f);

            // The office's own MCRO CORE VALUES chart, copied as-is (the graphic + all four
            // values baked into it — Modesty / Client-Focused / On-Time Service /
            // Responsiveness — plus its own title and border) rather than retyped, so nothing
            // about the office's real chart is lost or paraphrased. Bundled under Assets\ (see
            // BundledAsset) since it is fixed reference content, not office branding an office
            // would re-upload. Sized to the source photo's own aspect ratio (1205x1343) and
            // centered in the content column.
            bundledImg("CoreValuesGraphic.jpg", 185f, 414f, 242f, 270f);

            statC("Municipal Civil Registry Office (MCRO) - Local Civil Registry Office of Penablanca, Cagayan",
                40f, 700f, 532f, 12f, 8f, false);

            return c;
        }

        // ================================================================ data

        /// <summary>No per-record data at all — every value on this page is a standing office
        /// statement, not something tied to a birth/marriage/death row, so the table carries
        /// no columns (there is nothing for a Field cell to bind to; the list above is all
        /// Static text) and one empty row so the shared <see cref="Draw"/> loop has something
        /// to iterate against.</summary>
        public static DataTable BuildTable()
        {
            var t = new DataTable(TableName);
            t.Rows.Add(t.NewRow());
            t.AcceptChanges();
            return t;
        }

        /// <summary>A small on-screen preview bitmap of the whole page (this class's own
        /// current template — Static/Rule/BundledImage cells, no editable-template lookup),
        /// for embedding directly on a screen without opening the print/preview dialog.
        /// <paramref name="width"/> is the target pixel width; height follows the page's
        /// own aspect ratio.</summary>
        public static Bitmap RenderPreview(int width)
        {
            float scale = width / PageWidth;
            int h = (int)(PageHeight * scale);
            var bmp = new Bitmap(width, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                // Draw() sets g.PageUnit = Point, which converts every subsequent world-space
                // unit to pixels at 96/72 DPI (1.333x) ON TOP of this transform - the same
                // double-scale trap already hit and fixed elsewhere in this app (2026-09-07,
                // MF-90/97/103 print calibration). Divide it back out here so the transform
                // maps our point-space coordinates 1:1 onto this bitmap's pixels.
                g.ScaleTransform(scale * 72f / 96f, scale * 72f / 96f);
                Draw(g, BuildTable());
            }
            return bmp;
        }

        public static string RenderBlankTemplate(string outDir)
        {
            const float scale = 2f;
            int w = (int)(PageWidth * scale), h = (int)(PageHeight * scale);
            using (var bmp = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    // Same PageUnit=Point double-scale correction as RenderPreview above.
                    g.ScaleTransform(scale * 72f / 96f, scale * 72f / 96f);
                    Draw(g, new DataTable());
                }
                System.IO.Directory.CreateDirectory(outDir);
                string path = System.IO.Path.Combine(outDir, "OfficeMvcBlank_generated.png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                return path;
            }
        }

        // ================================================================ show / print

        public static void Show(DataTable t, System.Windows.Forms.IWin32Window owner)
        {
            if (TemplateReportBridge.TryShow(FormCode, FormName, t, owner)) return;

            string rpt = System.IO.Path.Combine(CertificateReport.ReportsFolder, RptFile);
            if (CertificateReport.CrystalAvailable && System.IO.File.Exists(rpt))
            {
                try { CrystalRunner.ShowTable(rpt, t, FormName, null, owner); return; }
                catch { /* fall through to the built-in renderer */ }
            }
            using (var doc = BuiltInDocument(t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, FormName, null))
                f.ShowDialog(owner);
        }

        public static System.Drawing.Printing.PrintDocument BuiltInDocument(DataTable t)
        {
            var doc = new System.Drawing.Printing.PrintDocument { DocumentName = FormName };
            try { doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100); } catch { }
            doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            doc.DefaultPageSettings.Landscape = false;
            doc.PrintPage += (s, e) =>
            {
                e.Graphics.PageUnit = GraphicsUnit.Point;
                if (!doc.PrintController.IsPreview)
                    e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX * 0.72f, -e.PageSettings.HardMarginY * 0.72f);
                Draw(e.Graphics, t);
                e.HasMorePages = false;
            };
            return doc;
        }

        public static void Draw(Graphics g, DataTable t)
        {
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                foreach (Form3ACell c in Cells)
                {
                    if (c.Kind == "Rule")
                    {
                        using (var pen = new Pen(Color.Black, 0.75f))
                            g.DrawLine(pen, c.X, c.Top, c.X + c.Width, c.Top);
                        continue;
                    }
                    if (c.Kind == "Static")
                    {
                        FontStyle style = (c.Bold ? FontStyle.Bold : FontStyle.Regular) | (c.Italic ? FontStyle.Italic : FontStyle.Regular);
                        using (var f = new Font("Arial", c.FontSize, style))
                            g.DrawString(c.Text, f, Brushes.Black, c.Rect, c.Center ? center : left);
                        continue;
                    }
                    if (c.Kind == "BundledImage")
                    {
                        Image img = BundledAsset.Load(c.ImageFile);
                        if (img != null) g.DrawImage(img, c.Rect);
                        continue;
                    }
                }
            }
        }
    }
}
