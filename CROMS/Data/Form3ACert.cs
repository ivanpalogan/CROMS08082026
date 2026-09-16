using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>One printed item on Form 3A, in points from the page's top-left.
    /// "Static" draws <see cref="Text"/> verbatim; "Field" draws the value of
    /// <see cref="Column"/> from the built table; "Picture" draws the office asset named by
    /// <see cref="Asset"/> (blank if the office has not supplied one - never blocks a print).</summary>
    public sealed class Form3ACell
    {
        public string Kind;      // "Static" / "Field" / "Picture" / "Rule"
        public string Text;      // Kind == "Static"
        public string Column;    // Kind == "Field"
        public AssetKind Asset;  // Kind == "Picture"
        public float X, Top, Width, Height, FontSize = 8f;
        public bool Bold, Center, Italic;
        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
    }

    /// <summary>
    /// Form 3A - CERTIFICATION (Marriage Available): a "TO WHOM IT MAY CONCERN" letter
    /// certifying facts already entered in the Register of Marriage. NOT a copy of the
    /// Certificate of Marriage (MF-97) - a separate document, closer in kind to a Negative
    /// Certification than to a CTC.
    /// <para/>
    /// Every value is EDITABLE by the operator before printing (<see cref="BuildTable"/> fills
    /// best-effort defaults off the saved record + the office profile; the screen lets a wrong
    /// one be corrected without touching the underlying `marriages` row - the same "correct at
    /// print time, never silently overwrite the record" rule the OCR review grid and the
    /// out-of-province-license panel already use elsewhere in this app). Father/Mother names are
    /// not reliably captured on a REGISTERED marriage row (they live on the license application,
    /// migration 38, only when one is linked) so they start blank and are typed in rather than
    /// guessed.
    /// <para/>
    /// Coordinates are a reasonable first cut on a Letter page (612 x 792 pt) - there is no
    /// scanned blank Form 3A on file the way MF-90/97/102/103 eventually got one, so unlike
    /// those forms this is NOT measured against the office's own paper yet. Flagged here rather
    /// than silently presented as exact, per this project's own standing rule.
    /// </summary>
    public static class Form3ACert
    {
        public const string FormCode = "FORM-3A-MARRIAGE-AVAILABLE";
        public const string FormName = "Certification (Marriage Available)";
        public const string RptFile = "FORM-3A.rpt";
        public const string TableName = "form3a_certification";
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

            // ---- header letterhead: three logo slots (left seal, two right badges), all
            // optional - a missing one just leaves that rectangle blank, never fails a print.
            // Title block is CENTERED across the printable width, matching the office's own
            // "FORM 3A" letterhead layout - seal top-left, badges top-right, everything else
            // centered between them, a rule under the contact line, then the date top-right.
            pic(AssetKind.HeaderLogoLeft, 40f, 20f, 58f, 58f);
            pic(AssetKind.HeaderLogoRight1, 452f, 20f, 54f, 54f);
            pic(AssetKind.HeaderLogoRight2, 510f, 20f, 54f, 54f);

            stat("FORM 3A", 8f, 4f, 90f, 11f, 7f, true);
            stat("(Marriage Available)", 8f, 15f, 90f, 10f, 6.5f, false);
            statC("Republic of the Philippines", 40f, 24f, 532f, 12f, 10f, false);
            statC("Province of Cagayan", 40f, 38f, 532f, 12f, 9f, false);
            statC("MUNICIPALITY OF PENABLANCA", 40f, 54f, 532f, 16f, 14f, true);
            statC("LOCAL CIVIL REGISTRY OFFICE", 40f, 72f, 532f, 14f, 11.5f, true);
            field("office_contact_line", 40f, 90f, 532f, 11f, 8f, true);
            rule(40f, 104f, 532f);

            field("date_issued", 420f, 110f, 152f, 12f, 9f, false);

            stat("TO WHOM IT MAY CONCERN:", 40f, 138f, 250f, 12f, 9.5f, true);
            stat("We certify that among others, the following facts of marriage appear in our Register of Marriage on Page",
                40f, 156f, 532f, 11f, 8.5f, false);
            field("registry_page", 60f, 168f, 60f, 11f, 9f, true);
            stat("of Book No.", 130f, 168f, 60f, 11f, 8.5f, false);
            field("registry_book", 192f, 168f, 100f, 11f, 9f, true);
            stat(":", 292f, 168f, 6f, 11f, 8.5f, false);

            // ---- two-column HUSBAND / WIFE table
            float col1 = 40f, colW = 260f, col2 = 312f;
            stat("HUSBAND", col1, 190f, colW, 12f, 9.5f, true);
            stat("WIFE", col2, 190f, colW, 12f, 9.5f, true);

            Action<string, string, string, float> row = (label, colH, colW2, y) =>
            {
                stat(label, col1, y, 90f, 11f, 8f, false);
                stat(":", 130f, y, 6f, 11f, 8f, false);
                field(colH, 136f, y, colW - 96f, 11f, 8.5f, false);
                field(colW2, col2 + 96f, y, colW - 96f, 11f, 8.5f, false);
            };
            row("NAME", "husband_full_name", "wife_full_name", 208f);
            row("DATE OF BIRTH/AGE", "husband_dob_age", "wife_dob_age", 224f);
            row("CIVIL STATUS", "husband_civil_status", "wife_civil_status", 240f);
            row("NATIONALITY", "husband_nationality", "wife_nationality", 256f);
            row("FATHER", "husband_father", "wife_father", 272f);
            row("NATIONALITY", "husband_father_nationality", "wife_father_nationality", 288f);
            row("MOTHER", "husband_mother", "wife_mother", 304f);
            row("NATIONALITY", "husband_mother_nationality", "wife_mother_nationality", 320f);

            stat("MCR REGISTRY NUMBER", col1, 340f, 130f, 11f, 8f, false);
            stat(":", 170f, 340f, 6f, 11f, 8f, false);
            field("mcr_registry_number", 176f, 340f, 380f, 11f, 8.5f, false);

            stat("DATE OF REGISTRATION", col1, 356f, 130f, 11f, 8f, false);
            stat(":", 170f, 356f, 6f, 11f, 8f, false);
            field("date_of_registration", 176f, 356f, 200f, 11f, 8.5f, false);

            stat("DATE OF MARRIAGE", col1, 372f, 130f, 11f, 8f, false);
            stat(":", 170f, 372f, 6f, 11f, 8f, false);
            field("date_of_marriage", 176f, 372f, 200f, 11f, 8.5f, false);

            stat("PLACE OF MARRIAGE", col1, 388f, 130f, 11f, 8f, false);
            stat(":", 170f, 388f, 6f, 11f, 8f, false);
            field("place_of_marriage", 176f, 388f, 380f, 11f, 8.5f, false);

            stat("This certification is issued for", 40f, 420f, 170f, 12f, 9f, false);
            field("purpose", 210f, 420f, 260f, 12f, 9f, false);
            stat(".", 470f, 420f, 6f, 12f, 9f, false);

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

        /// <summary>Builds the editable, printable row for one REGISTERED marriage
        /// (`marriages.id`). Every value can be corrected on screen before printing - nothing
        /// here writes back to the record.</summary>
        public static DataTable BuildTable(int marriageId)
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
                    "SELECT husband_full_name, wife_full_name, husband_age, wife_age, " +
                    "husband_date_of_birth, wife_date_of_birth, husband_civil_status, wife_civil_status, " +
                    "husband_citizenship, wife_citizenship, place_of_marriage, date_of_marriage, " +
                    "registry_no, book_volume, book_page " +
                    "FROM v_marriage_certificate WHERE record_id = @id",
                    new MySqlParameter("@id", marriageId));
                if (rec.Rows.Count > 0)
                {
                    DataRow m = rec.Rows[0];
                    string S(string col) => rec.Columns.Contains(col) && m[col] != DBNull.Value ? m[col].ToString() : "";
                    r["husband_full_name"] = S("husband_full_name");
                    r["wife_full_name"] = S("wife_full_name");
                    r["husband_dob_age"] = JoinDobAge(S("husband_date_of_birth"), S("husband_age"));
                    r["wife_dob_age"] = JoinDobAge(S("wife_date_of_birth"), S("wife_age"));
                    r["husband_civil_status"] = S("husband_civil_status");
                    r["wife_civil_status"] = S("wife_civil_status");
                    r["husband_nationality"] = S("husband_citizenship");
                    r["wife_nationality"] = S("wife_citizenship");
                    r["place_of_marriage"] = S("place_of_marriage");
                    string dom = S("date_of_marriage");
                    r["date_of_marriage"] = FmtDate(dom);
                    r["mcr_registry_number"] = S("registry_no");
                    r["registry_book"] = S("book_volume");
                    r["registry_page"] = S("book_page");
                }
            }
            catch (Exception) { /* record not found, no DB, or a hiccup - leave blank, editable.
                Broad on purpose: BuildTable(0) is also called with no live connection by
                CROMS.ReportGen just to get the column shape for the Crystal datasource. */ }

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

        private static string JoinDobAge(string dob, string age)
        {
            string d = FmtDate(dob);
            if (string.IsNullOrWhiteSpace(d) && string.IsNullOrWhiteSpace(age)) return "";
            if (string.IsNullOrWhiteSpace(age)) return d;
            if (string.IsNullOrWhiteSpace(d)) return age;
            return d + " / " + age;
        }

        private static string FmtDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)
                ? dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) : s;
        }

        /// <summary>
        /// Renders everything EXCEPT the Field cells (every Static label + the header/footer
        /// Picture cells) to a PNG and returns its path. This is the "blank form" CROMS.ReportGen
        /// embeds as the .rpt's full-page background - the same technique MF-90's generator uses
        /// on its scanned blank, except here the blank does not exist as a scan; it is generated
        /// from the same Cells list the fallback printer draws, so the Crystal report and the
        /// fallback can never disagree on where a label sits.
        /// </summary>
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
                    var empty = new DataTable();
                    Draw(g, empty);   // Field cells are skipped when the table has no rows
                }
                System.IO.Directory.CreateDirectory(outDir);
                string path = System.IO.Path.Combine(outDir, "Form3ABlank_generated.png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                return path;
            }
        }

        // ================================================================ show / print

        /// <summary>Preview/print, dispatching to Crystal when CROMS\Reports\FORM-3A.rpt exists
        /// and the runtime is present, else the built-in direct-draw renderer - same
        /// Crystal-if-available-else-fallback rule <see cref="CertificateReport"/> uses for the
        /// registry certificates. Crystal path uses <see cref="CrystalRunner.ShowTable"/>, the
        /// same table-report call MF-90's printed application uses - Form 3A has no view of its
        /// own either, so CROMS builds the one row itself and the report only places it.</summary>
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
