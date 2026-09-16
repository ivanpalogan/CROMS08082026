using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Form 2A - CERTIFICATION (Death Available): the death-registry counterpart of
    /// <see cref="Form3ACert"/> (marriage, printed "FORM 3A") and <see cref="Form3BCert"/>
    /// (birth, printed "Civil Registry Form No. 1A"). A "TO WHOM IT MAY CONCERN" letter
    /// certifying facts already entered in the Register of Deaths, Page X Book No. Y. NOT a
    /// copy of the Certificate of Death (MF-103) - a separate document, same kind as a
    /// Negative Certification.
    /// <para/>
    /// UNLIKE the other two, Form 2A's letterhead is genuinely different - matched against the
    /// office's own real issued copy (photographed sample, 2026-09-16), not assumed shared:
    /// no Tel/Email contact line, "Municipality of Peñablanca" printed plain (not bold caps),
    /// a single bold title line "OFFICE OF THE MUNICIPAL CIVIL REGISTRAR" (not the two-line
    /// "MUNICIPALITY OF ... / LOCAL CIVIL REGISTRY OFFICE" the other two carry), and only two
    /// logo slots (the municipal seal left, the national badge right - no third badge, since the
    /// real form does not print one). A footer banner slot IS included, at the same coordinates
    /// Form 3A/Form 1A use, below the note and well inside the page. The opening sentence, facts
    /// table and REMARKS close still follow the same per-cell layout convention as
    /// Form3ACert/Form3BCert (Kind Static/Field/Picture/Rule).
    /// <para/>
    /// Every value is editable before printing, same rule as the other two: a wrong reading is
    /// corrected on the printout, never silently written back to the saved record.
    /// <para/>
    /// Coordinates are a reasonable first cut, not measured against an office blank - flagged
    /// rather than presented as exact.
    /// </summary>
    public static class Form3CCert
    {
        public const string FormCode = "FORM-3C-DEATH-AVAILABLE";
        public const string FormName = "Certification (Death Available)";
        public const string RptFile = "FORM-3C.rpt";
        public const string TableName = "form3c_certification";
        public const float PageWidth = 612f, PageHeight = 792f;

        private static List<Form3ACell> _cells;
        public static IReadOnlyList<Form3ACell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<Form3ACell> BuildCells()
        {
            var c = new List<Form3ACell>();
            Action<string, float, float, float, float, float, bool> stat = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<string, float, float, float, float, float, bool> statC = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold, Center = true });
            Action<string, float, float, float, float, float> noteItalic = (text, x, top, w, h, size) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Italic = true });
            Action<string, float, float, float, float, float, bool> field = (col, x, top, w, h, size, center) =>
                c.Add(new Form3ACell { Kind = "Field", Column = col, X = x, Top = top, Width = w, Height = h, FontSize = size, Center = center });
            Action<AssetKind, float, float, float, float> pic = (kind, x, top, w, h) =>
                c.Add(new Form3ACell { Kind = "Picture", Asset = kind, X = x, Top = top, Width = w, Height = h });
            Action<float, float, float> rule = (x, top, w) =>
                c.Add(new Form3ACell { Kind = "Rule", X = x, Top = top, Width = w, Height = 1f });

            // ---- Form 2A's own letterhead: municipal seal + national badge only (no third
            // logo slot), no contact line, one bold title line instead of two.
            pic(AssetKind.HeaderLogoLeft, 40f, 20f, 62f, 62f);
            pic(AssetKind.HeaderLogoRight1, 512f, 20f, 60f, 60f);

            stat("Form 2A", 8f, 4f, 90f, 11f, 7f, false);
            stat("(Death Available)", 8f, 15f, 90f, 10f, 6.5f, false);
            statC("Republic of the Philippines", 40f, 24f, 532f, 12f, 10f, false);
            statC("Province of " + OfficeAssets.Profile.ProvinceForPrint, 40f, 38f, 532f, 12f, 9f, false);
            statC("Municipality of " + OfficeAssets.Profile.MunicipalityForPrint, 40f, 52f, 532f, 12f, 10f, false);
            statC("OFFICE OF THE MUNICIPAL CIVIL REGISTRAR", 40f, 72f, 532f, 14f, 11f, true);
            rule(40f, 96f, 532f);

            field("date_issued", 420f, 102f, 152f, 12f, 9f, false);

            // ---- shared opening sentence, "death(s)" in place of "marriage" / "birth(s)".
            stat("TO WHOM IT MAY CONCERN:", 40f, 138f, 250f, 12f, 9.5f, true);
            stat("We certify that among others, the following facts of death appear in our Register of Deaths on Page",
                40f, 156f, 532f, 11f, 8.5f, false);
            field("registry_page", 60f, 168f, 60f, 11f, 9f, true);
            stat("of Book No.", 130f, 168f, 60f, 11f, 8.5f, false);
            field("registry_book", 192f, 168f, 100f, 11f, 9f, true);
            stat(":", 292f, 168f, 6f, 11f, 8.5f, false);

            // ---- single-person facts table (deceased) - same row geometry as Form3BCert's
            // child-facts table, different labels/fields.
            Action<string, string, float> row = (label, col, y) =>
            {
                stat(label, 40f, y, 150f, 12f, 8.5f, false);
                stat(":", 194f, y, 6f, 12f, 8.5f, false);
                field(col, 200f, y, 372f, 12f, 9f, false);
            };
            row("MCR REGISTRY NUMBER", "registry_number", 196f);
            row("DATE OF REGISTRATION", "date_of_registration", 214f);
            row("NAME OF DECEASED", "deceased_name", 232f);
            row("SEX", "sex", 250f);
            row("AGE", "age", 268f);
            row("PLACE OF DEATH", "place_of_death", 286f);
            row("DATE OF DEATH", "date_of_death", 304f);
            row("CAUSE OF DEATH", "cause_of_death", 322f);

            stat("REMARKS:", 40f, 348f, 120f, 12f, 9.5f, true);
            field("remarks_text", 40f, 366f, 532f, 30f, 9f, false);

            // ---- shared footer: registrar / verified-by / payment / note / footer banner -
            // SAME coordinates as Form3ACert/Form3BCert.
            field("registrar_name", 380f, 470f, 192f, 12f, 9.5f, true);
            stat("Municipal Civil Registrar", 380f, 486f, 192f, 11f, 8f, false);

            stat("VERIFIED BY:", 40f, 510f, 120f, 12f, 9f, true);
            field("verified_by_name", 40f, 540f, 220f, 12f, 9.5f, false);
            field("verified_by_title", 40f, 554f, 220f, 11f, 8f, false);

            stat("Amount paid", 40f, 600f, 70f, 11f, 8f, false);
            stat(":", 112f, 600f, 6f, 11f, 8f, false);
            field("amount_paid", 118f, 600f, 160f, 11f, 8f, false);
            stat("OR No.", 40f, 613f, 70f, 11f, 8f, false);
            stat(":", 112f, 613f, 6f, 11f, 8f, false);
            field("or_number", 118f, 613f, 160f, 11f, 8f, false);
            stat("Date paid", 40f, 626f, 70f, 11f, 8f, false);
            stat(":", 112f, 626f, 6f, 11f, 8f, false);
            field("date_paid", 118f, 626f, 160f, 11f, 8f, false);

            noteItalic("Note: This certification is not valid if it has mark of erasure or alteration of any entry.",
                40f, 660f, 532f, 11f, 7.5f);

            // Footer banner, added per the office's Header/Footer Images setup - the same
            // slot Form 3A/Form 1A carry, placed below the note (671pt) with room to spare
            // before the page ends (792pt) so it never overlaps the certification text,
            // signatures, payment fields or the note above it.
            pic(AssetKind.FooterBanner, 40f, 700f, 532f, 70f);

            return c;
        }

        // ================================================================ data

        /// <summary>Builds the editable, printable row for one REGISTERED death (`deaths.id`).
        /// Every value can be corrected on screen before printing - nothing here writes back to
        /// the record.</summary>
        public static DataTable BuildTable(int deathId)
        {
            OfficeProfile office = OfficeAssets.Profile;
            var t = new DataTable(TableName);
            foreach (Form3ACell cell in Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            try
            {
                DataTable rec = Db.Pull(
                    "SELECT deceased_full_name, sex, age, date_of_death, place_of_death, " +
                    "immediate_cause, registry_no, book_volume, book_page " +
                    "FROM v_death_certificate WHERE record_id = @id",
                    new MySqlParameter("@id", deathId));
                if (rec.Rows.Count > 0)
                {
                    DataRow d = rec.Rows[0];
                    string S(string col) => rec.Columns.Contains(col) && d[col] != DBNull.Value ? d[col].ToString() : "";
                    r["deceased_name"] = S("deceased_full_name");
                    r["sex"] = S("sex");
                    r["age"] = S("age");
                    r["date_of_death"] = Form3ACert.FmtDateCell(rec, d, "date_of_death");
                    // DATE OF REGISTRATION is deliberately NOT filled: v_death_certificate
                    // carries no date_registered column (births and marriages do), so the
                    // office's own registration date for a death is not recorded anywhere
                    // this can read. It prints blank and stays editable rather than being
                    // filled with the print date, which would state a fact nothing supports.
                    r["place_of_death"] = S("place_of_death");
                    r["cause_of_death"] = S("immediate_cause");
                    r["registry_number"] = S("registry_no");
                    r["registry_book"] = S("book_volume");
                    r["registry_page"] = S("book_page");

                    r["remarks_text"] = "This certification is issued to Mr./Ms. _______________ upon his/her request.";
                }
            }
            catch (Exception) { /* record not found, no DB, or a hiccup - leave blank, editable.
                Broad on purpose: BuildTable(0) is also called with no live connection by
                CROMS.ReportGen just to get the column shape for the Crystal datasource. */ }

            r["date_issued"] = DateTime.Today.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            r["registrar_name"] = office.RegistrarName ?? "";
            r["verified_by_name"] = office.VerifyingOfficerName ?? "";
            r["verified_by_title"] = office.VerifyingOfficerTitle ?? "Registration Officer II";

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }


        /// <summary>Same technique as Form3ACert.RenderBlankTemplate / Form3BCert.RenderBlankTemplate
        /// - the Crystal report's background is generated from this class's own Static/Picture
        /// cells, so it and the fallback printer can never draw a label in a different place.</summary>
        public static string RenderBlankTemplate(string outDir)
        {
            const float scale = 2f;
            int w = (int)(PageWidth * scale), h = (int)(PageHeight * scale);
            using (var bmp = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.ScaleTransform(scale, scale);
                    Draw(g, new DataTable());
                }
                System.IO.Directory.CreateDirectory(outDir);
                string path = System.IO.Path.Combine(outDir, "Form3CBlank_generated.png");
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
            DataRow r = t.Rows.Count > 0 ? t.Rows[0] : null;
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
                    if (c.Kind == "Picture")
                    {
                        Image img = OfficeAssets.Get(c.Asset, FormCode);
                        if (img != null) g.DrawImage(img, c.Rect);
                        continue;
                    }
                    string v = r != null && t.Columns.Contains(c.Column) ? r[c.Column] as string : null;
                    if (string.IsNullOrEmpty(v)) continue;
                    using (var f = new Font("Arial", c.FontSize, c.Bold ? FontStyle.Bold : FontStyle.Regular))
                        g.DrawString(v, f, Brushes.Black, c.Rect, c.Center ? center : left);
                }
            }
        }
    }
}
