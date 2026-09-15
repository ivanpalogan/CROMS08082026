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
    /// Form 3B - CERTIFICATION (Birth Available): the birth-registry counterpart of
    /// <see cref="Form3ACert"/> - a "TO WHOM IT MAY CONCERN" letter certifying facts already
    /// entered in the Register of Births, Page X of Book No. Y. NOT a copy of the Certificate
    /// of Live Birth (MF-102) - a separate document, same kind as a Negative Certification.
    /// <para/>
    /// Single-person layout (the child), unlike Form 3A's husband/wife columns. Shares the
    /// office letterhead/footer assets and the "Verified by" officer added on migration 47 -
    /// no new schema. Every value is editable before printing, same rule as Form 3A: a wrong
    /// reading is corrected on the printout, never silently written back to the saved record.
    /// <para/>
    /// Coordinates are a reasonable first cut, not measured against an office blank (none on
    /// file for this letter, same as Form 3A) - flagged rather than presented as exact.
    /// </summary>
    public static class Form3BCert
    {
        public const string FormCode = "FORM-3B-BIRTH-AVAILABLE";
        public const string FormName = "Certification (Birth Available)";
        public const string RptFile = "FORM-3B.rpt";
        public const string TableName = "form3b_certification";
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

            pic(AssetKind.HeaderLogoLeft, 40f, 20f, 58f, 58f);
            pic(AssetKind.HeaderLogoRight1, 452f, 20f, 54f, 54f);
            pic(AssetKind.HeaderLogoRight2, 510f, 20f, 54f, 54f);

            stat("FORM 3B", 8f, 4f, 90f, 11f, 7f, true);
            stat("(Birth Available)", 8f, 15f, 90f, 10f, 6.5f, false);
            statC("Republic of the Philippines", 40f, 24f, 532f, 12f, 10f, false);
            statC("Province of Cagayan", 40f, 38f, 532f, 12f, 9f, false);
            statC("MUNICIPALITY OF PENABLANCA", 40f, 54f, 532f, 16f, 14f, true);
            statC("LOCAL CIVIL REGISTRY OFFICE", 40f, 72f, 532f, 14f, 11.5f, true);
            field("office_contact_line", 40f, 90f, 532f, 11f, 8f, true);
            rule(40f, 104f, 532f);

            field("date_issued", 420f, 110f, 152f, 12f, 9f, false);

            stat("TO WHOM IT MAY CONCERN:", 40f, 138f, 250f, 12f, 9.5f, true);
            stat("We certify that among others, the following facts of birth appear in our Register of Births on Page",
                40f, 156f, 532f, 11f, 8.5f, false);
            field("registry_page", 60f, 168f, 60f, 11f, 9f, true);
            stat("of Book No.", 130f, 168f, 60f, 11f, 8.5f, false);
            field("registry_book", 192f, 168f, 100f, 11f, 9f, true);
            stat(":", 292f, 168f, 6f, 11f, 8.5f, false);

            Action<string, string, float> row = (label, col, y) =>
            {
                stat(label, 40f, y, 150f, 12f, 8.5f, false);
                stat(":", 194f, y, 6f, 12f, 8.5f, false);
                field(col, 200f, y, 372f, 12f, 9f, false);
            };
            row("NAME OF CHILD", "child_name", 196f);
            row("SEX", "sex", 214f);
            row("DATE OF BIRTH", "date_of_birth", 232f);
            row("PLACE OF BIRTH", "place_of_birth", 250f);
            row("NAME OF MOTHER", "mother_name", 268f);
            row("NATIONALITY", "mother_nationality", 286f);
            row("NAME OF FATHER", "father_name", 304f);
            row("NATIONALITY", "father_nationality", 322f);

            stat("REGISTRY NUMBER", 40f, 348f, 150f, 12f, 8.5f, false);
            stat(":", 194f, 348f, 6f, 12f, 8.5f, false);
            field("registry_number", 200f, 348f, 372f, 12f, 9f, false);

            stat("DATE OF REGISTRATION", 40f, 366f, 150f, 12f, 8.5f, false);
            stat(":", 194f, 366f, 6f, 12f, 8.5f, false);
            field("date_of_registration", 200f, 366f, 372f, 12f, 9f, false);

            stat("This certification is issued for", 40f, 400f, 170f, 12f, 9f, false);
            field("purpose", 210f, 400f, 260f, 12f, 9f, false);
            stat(".", 470f, 400f, 6f, 12f, 9f, false);

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

            pic(AssetKind.FooterBanner, 40f, 700f, 532f, 70f);

            return c;
        }

        // ================================================================ data

        public static DataTable BuildTable(int birthId)
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
                    "SELECT child_full_name, sex, date_of_birth, place_of_birth, " +
                    "mother_maiden_name, mother_citizenship, father_full_name, father_citizenship, " +
                    "registry_no, book_volume, book_page, created_at " +
                    "FROM v_birth_certificate WHERE record_id = @id",
                    new MySqlParameter("@id", birthId));
                if (rec.Rows.Count > 0)
                {
                    DataRow b = rec.Rows[0];
                    string S(string col) => rec.Columns.Contains(col) && b[col] != DBNull.Value ? b[col].ToString() : "";
                    r["child_name"] = S("child_full_name");
                    r["sex"] = S("sex");
                    r["date_of_birth"] = FmtDate(S("date_of_birth"));
                    r["place_of_birth"] = S("place_of_birth");
                    r["mother_name"] = S("mother_maiden_name");
                    r["mother_nationality"] = S("mother_citizenship");
                    r["father_name"] = S("father_full_name");
                    r["father_nationality"] = S("father_citizenship");
                    r["registry_number"] = S("registry_no");
                    r["registry_book"] = S("book_volume");
                    r["registry_page"] = S("book_page");
                }
            }
            catch (Exception) { /* record not found, no DB, or a hiccup - leave blank, editable */ }

            r["office_contact_line"] = string.Join("   |   ", new[]
            {
                string.IsNullOrWhiteSpace(office.Contact) ? null : "Tel. No. " + office.Contact,
                string.IsNullOrWhiteSpace(office.Email) ? null : "Email: " + office.Email,
            }.Where(s => s != null));
            r["date_issued"] = DateTime.Today.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            r["date_of_registration"] = DateTime.Today.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            r["purpose"] = "general purpose/s";
            r["registrar_name"] = office.RegistrarName ?? "";
            r["verified_by_name"] = office.VerifyingOfficerName ?? "";
            r["verified_by_title"] = office.VerifyingOfficerTitle ?? "Registration Officer II";

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        private static string FmtDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)
                ? dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) : s;
        }

        /// <summary>Same technique as Form3ACert.RenderBlankTemplate - the Crystal report's
        /// background is generated from this class's own Static/Picture cells, so it and the
        /// fallback printer can never draw a label in a different place.</summary>
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
                string path = System.IO.Path.Combine(outDir, "Form3BBlank_generated.png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                return path;
            }
        }

        // ================================================================ show / print

        public static void Show(DataTable t, System.Windows.Forms.IWin32Window owner)
        {
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
                        Image img = OfficeAssets.Get(c.Asset);
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
