using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// The annual "Fees &amp; Collections" assessment report — the printable replacement for
    /// the old Fees &amp; Payments "Payment log" / "Monthly collection" tabs, moved to Reports
    /// &amp; Analytics. Built the SAME way as <see cref="AssessmentReport"/> (a
    /// <see cref="Form3ACell"/> list, drawn by the same routine, editable through the SAME
    /// visual designer via <see cref="TemplateStore.KnownForms"/> and
    /// <see cref="TemplateReportBridge"/>) — not a separate <see cref="AssessmentReport.Def"/>
    /// because its twelve monthly figures are money (decimal, PHP), not event counts, and it
    /// carries two extra data rows (top fee type, total receipts) that the count-based reports
    /// have no use for.
    /// <para/>
    /// The conclusion is COMPOSED from the counted collections (never invented - a month with
    /// nothing collected reports zero, a year with no payments says so) but is stored as an
    /// ordinary Field element, so once opened in the Template Designer the wording is exactly
    /// as editable as every other value on the page.
    /// </summary>
    public static class CollectionsReport
    {
        public const string FormCode = "ASSESS-COLLECTIONS-ANNUAL";
        public const string FormName = "Annual Assessment Report — Fees & Collections";
        public const float PageWidth = 612f, PageHeight = 792f;

        private static readonly string[] MonthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12).ToArray();
        private static System.Collections.Generic.List<Form3ACell> _cells;

        public static System.Collections.Generic.IReadOnlyList<Form3ACell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        // ------------------------------------------------------------------ layout

        private static System.Collections.Generic.List<Form3ACell> BuildCells()
        {
            var c = new System.Collections.Generic.List<Form3ACell>();
            Action<string, float, float, float, float, float, bool> stat = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<string, float, float, float, float, float, bool> statC = (text, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold, Center = true });
            Action<string, float, float, float, float, float, bool> field = (col, x, top, w, h, size, bold) =>
                c.Add(new Form3ACell { Kind = "Field", Column = col, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<AssetKind, float, float, float, float> pic = (kind, x, top, w, h) =>
                c.Add(new Form3ACell { Kind = "Picture", Asset = kind, X = x, Top = top, Width = w, Height = h });
            Action<float, float, float> rule = (x, top, w) =>
                c.Add(new Form3ACell { Kind = "Rule", X = x, Top = top, Width = w, Height = 1f });

            pic(AssetKind.HeaderLogoLeft, 40f, 18f, 52f, 52f);
            pic(AssetKind.HeaderLogoRight1, 462f, 18f, 48f, 48f);
            pic(AssetKind.HeaderLogoRight2, 514f, 18f, 48f, 48f);

            OfficeProfile head = OfficeAssets.Profile;
            statC("Republic of the Philippines", 40f, 22f, 532f, 12f, 9.5f, false);
            statC("Province of " + head.ProvinceForPrint, 40f, 36f, 532f, 12f, 8.5f, false);
            statC("MUNICIPALITY OF " + head.MunicipalityForPrint.ToUpperInvariant(), 40f, 50f, 532f, 15f, 12.5f, true);
            statC("LOCAL CIVIL REGISTRY OFFICE", 40f, 67f, 532f, 13f, 10.5f, true);
            rule(40f, 84f, 532f);

            statC("ANNUAL ASSESSMENT REPORT", 40f, 96f, 532f, 16f, 14f, true);
            statC("Fees & Collections", 40f, 116f, 532f, 13f, 11.5f, true);

            stat("Calendar Year:", 40f, 142f, 100f, 12f, 9f, false);
            field("report_year", 142f, 142f, 100f, 12f, 9.5f, true);
            stat("Generated on:", 380f, 142f, 100f, 12f, 9f, false);
            field("generated_on", 456f, 142f, 116f, 12f, 9f, false);
            rule(40f, 160f, 532f);

            stat("MONTH", 60f, 170f, 200f, 12f, 9f, true);
            stat("Collections (PHP)", 300f, 170f, 200f, 12f, 9f, true);
            rule(40f, 186f, 532f);

            float rowY = 190f;
            for (int i = 0; i < 12; i++)
            {
                stat(MonthNames[i], 60f, rowY, 200f, 16f, 9f, false);
                field("m" + (i + 1).ToString("00"), 300f, rowY, 120f, 16f, 9f, false);
                rowY += 18f;
            }

            rule(40f, rowY + 2f, 532f);
            stat("TOTAL COLLECTED FOR THE YEAR", 60f, rowY + 8f, 260f, 14f, 10f, true);
            field("total_amount", 300f, rowY + 8f, 120f, 14f, 10f, true);

            float y2 = rowY + 32f;
            stat("Month with the Most Collected:", 40f, y2, 260f, 12f, 9f, false);
            field("peak_month", 300f, y2, 272f, 12f, 9f, true);

            stat("Top Fee Type for the Year:", 40f, y2 + 20f, 260f, 12f, 9f, false);
            field("top_fee_type", 300f, y2 + 20f, 272f, 12f, 9f, true);

            stat("Total Receipts Issued:", 40f, y2 + 40f, 260f, 12f, 9f, false);
            field("total_receipts", 300f, y2 + 40f, 272f, 12f, 9f, true);

            stat("Average per Month:", 40f, y2 + 60f, 260f, 12f, 9f, false);
            field("average_amount", 300f, y2 + 60f, 272f, 12f, 9f, true);

            float y3 = y2 + 88f;
            rule(40f, y3, 532f);
            stat("CONCLUSION", 40f, y3 + 8f, 200f, 13f, 10f, true);
            field("conclusion_text", 40f, y3 + 26f, 532f, 96f, 9f, false);

            float y4 = y3 + 134f;
            rule(40f, y4, 532f);
            field("prepared_by_name", 60f, y4 + 14f, 220f, 12f, 9.5f, true);
            stat("Prepared by / Cashier", 60f, y4 + 30f, 220f, 11f, 8f, false);
            field("reviewed_by_name", 340f, y4 + 14f, 220f, 12f, 9.5f, true);
            stat("Reviewed by / Municipal Civil Registrar", 340f, y4 + 30f, 220f, 11f, 8f, false);

            return c;
        }

        // ------------------------------------------------------------------ data

        private static decimal[] MonthAmounts(int year)
        {
            var amounts = new decimal[12];
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT MONTH(paid_at), SUM(net_amount) FROM payments WHERE YEAR(paid_at) = @y GROUP BY MONTH(paid_at)",
                    new MySqlParameter("@y", year));
                foreach (DataRow r in dt.Rows)
                {
                    int mo = Convert.ToInt32(r[0]);
                    if (mo >= 1 && mo <= 12) amounts[mo - 1] = Convert.ToDecimal(r[1]);
                }
            }
            catch { /* no DB — reports a year of zeros, never blocks the page */ }
            return amounts;
        }

        private static string TopFeeType(int year)
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT COALESCE((SELECT f.description FROM fees f WHERE f.code = COALESCE(MIN(i.fee_code), '')), MIN(i.description)) AS Fee, " +
                    "SUM(i.line_amount) AS Amount " +
                    "FROM payment_items i JOIN payments p ON p.id = i.payment_id WHERE YEAR(p.paid_at) = @y " +
                    "GROUP BY COALESCE(i.fee_code, CONCAT('#', i.description)) ORDER BY Amount DESC LIMIT 1",
                    new MySqlParameter("@y", year));
                if (dt.Rows.Count == 0) return "None recorded";
                return dt.Rows[0]["Fee"] + " (₱" + Convert.ToDecimal(dt.Rows[0]["Amount"]).ToString("N2", CultureInfo.InvariantCulture) + ")";
            }
            catch { return "-"; }
        }

        private static int TotalReceipts(int year)
        {
            try
            {
                DataTable dt = Db.Pull("SELECT COUNT(*) FROM payments WHERE YEAR(paid_at) = @y", new MySqlParameter("@y", year));
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch { return 0; }
        }

        public static DataTable BuildTable(int year)
        {
            var t = new DataTable(FormCode.ToLowerInvariant().Replace("-", "_"));
            foreach (Form3ACell cell in Cells)
                if (cell.Kind == "Field" && !t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            decimal[] amounts = MonthAmounts(year);
            decimal total = amounts.Sum();
            int peakIdx = 0;
            for (int i = 1; i < 12; i++) if (amounts[i] > amounts[peakIdx]) peakIdx = i;
            string topFee = TopFeeType(year);
            int receipts = TotalReceipts(year);

            for (int i = 0; i < 12; i++) r["m" + (i + 1).ToString("00")] = "₱ " + amounts[i].ToString("N2", CultureInfo.InvariantCulture);
            r["total_amount"] = "₱ " + total.ToString("N2", CultureInfo.InvariantCulture);
            r["report_year"] = year.ToString(CultureInfo.InvariantCulture);
            r["generated_on"] = DateTime.Today.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            r["top_fee_type"] = topFee;
            r["total_receipts"] = receipts.ToString("N0", CultureInfo.InvariantCulture);
            r["average_amount"] = "₱ " + (total / 12m).ToString("N2", CultureInfo.InvariantCulture);

            r["peak_month"] = total == 0
                ? "None recorded for " + year
                : MonthNames[peakIdx] + " (₱" + amounts[peakIdx].ToString("N2", CultureInfo.InvariantCulture) + ")";

            r["conclusion_text"] = BuildConclusion(year, amounts, total, peakIdx, receipts, topFee);

            OfficeProfile office = OfficeAssets.Profile;
            r["reviewed_by_name"] = office.RegistrarName ?? "";
            r["prepared_by_name"] = "";

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        /// <summary>Composed strictly from the counted collections - never a figure the
        /// counting did not produce. Stored as an ordinary Field, so it is fully rewritable
        /// once the report is opened in the Template Designer.</summary>
        private static string BuildConclusion(int year, decimal[] amounts, decimal total, int peakIdx, int receipts, string topFee)
        {
            if (total == 0)
                return string.Format(
                    "No collections were recorded for calendar year {0}. There is no basis yet " +
                    "for a monthly trend or a busiest-month comparison.", year);

            string peakSentence = amounts[peakIdx] > 0
                ? string.Format("The month with the most collected was {0}, with ₱{1} recorded.",
                    MonthNames[peakIdx], amounts[peakIdx].ToString("N2", CultureInfo.InvariantCulture))
                : "";

            return string.Format(
                "During calendar year {0}, the Local Civil Registry Office collected a total of ₱{1} " +
                "across {2} official receipt{3}. {4} The most collected fee type for the year was {5}.",
                year, total.ToString("N2", CultureInfo.InvariantCulture), receipts, receipts == 1 ? "" : "s",
                peakSentence, topFee).Trim();
        }

        // ------------------------------------------------------------------ show / print

        public static void Show(DataTable t, System.Windows.Forms.IWin32Window owner)
        {
            if (TemplateReportBridge.TryShow(FormCode, FormName, t, owner)) return;
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
                        FontStyle style = c.Bold ? FontStyle.Bold : FontStyle.Regular;
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
