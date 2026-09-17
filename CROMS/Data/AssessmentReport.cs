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
    /// One annual assessment report — the yearly counterpart of the A1/A2/A3 "Facts
    /// Certification" family, built the SAME way (a <see cref="Form3ACell"/> list, drawn by
    /// the same routine, editable through the SAME visual designer via
    /// <see cref="TemplateStore.KnownForms"/> and <see cref="TemplateReportBridge"/>). Where
    /// A1/A2/A3 certify ONE record, this certifies a YEAR: how many events happened each
    /// month, which month had the most, and a plain-language conclusion.
    /// <para/>
    /// Six of these exist — Birth, Death, Marriage, Queuing, Certificates, PSA/Statutory —
    /// one per Reports &amp; Analytics tab, sharing this one class and differing only in
    /// which table/date column they count and the wording that names the domain. Adding a
    /// seventh report is one more <see cref="Def"/>, not a new file.
    /// <para/>
    /// The conclusion is COMPOSED from the counted data (never invented — a month with zero
    /// events reports zero, a year with no data says so) but is stored as an ordinary Field
    /// element, so once opened in the Template Designer the wording is exactly as editable
    /// as every other value on the page.
    /// </summary>
    public static class AssessmentReport
    {
        public const float PageWidth = 612f, PageHeight = 792f;

        // ------------------------------------------------------------------ definitions

        /// <summary>Everything one assessment report needs beyond the shared layout: its
        /// identity, the wording that names its domain, and how to count a year of it.</summary>
        public sealed class Def
        {
            public string FormCode, FormName, DomainTitle, MetricLabel, Noun, SecondaryLabel;
            private readonly Func<int, int[]> _monthCounts;
            private readonly Func<int, int[], int, string> _secondaryValue;
            private List<Form3ACell> _cells;

            public Def(string formCode, string formName, string domainTitle, string metricLabel,
                       string noun, string secondaryLabel,
                       Func<int, int[]> monthCounts, Func<int, int[], int, string> secondaryValue)
            {
                FormCode = formCode; FormName = formName; DomainTitle = domainTitle;
                MetricLabel = metricLabel; Noun = noun; SecondaryLabel = secondaryLabel;
                _monthCounts = monthCounts; _secondaryValue = secondaryValue;
            }

            /// <summary>The twelve-month counts for a year, never throwing — a query failure
            /// (no DB, a locked table) reports a year of zeros rather than crashing the page.</summary>
            public int[] MonthCounts(int year)
            {
                try { return _monthCounts(year) ?? new int[12]; }
                catch { return new int[12]; }
            }

            public string SecondaryValue(int year, int[] counts, int total)
            {
                try { return _secondaryValue(year, counts, total); }
                catch { return "—"; }
            }

            public IReadOnlyList<Form3ACell> Cells { get { return _cells ?? (_cells = BuildCells(DomainTitle, MetricLabel, SecondaryLabel)); } }
        }

        private static int[] Counts(string sql, int year)
        {
            var counts = new int[12];
            DataTable dt = Db.Pull(sql, new MySqlParameter("@y", year));
            foreach (DataRow r in dt.Rows)
            {
                int mo = Convert.ToInt32(r[0]);
                if (mo >= 1 && mo <= 12) counts[mo - 1] = Convert.ToInt32(r[1]);
            }
            return counts;
        }

        private static string Average(int total) => (total / 12.0).ToString("0.#", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ the six reports

        public static readonly Def Birth = new Def(
            "ASSESS-BIRTH-ANNUAL", "Annual Assessment Report — Birth Registration",
            "Birth Registration", "Births Registered", "birth registrations", "Average per Month",
            year => Counts(
                "SELECT MONTH(date_registered), COUNT(*) FROM births " +
                "WHERE date_registered IS NOT NULL AND YEAR(date_registered) = @y " +
                "GROUP BY MONTH(date_registered)", year),
            (year, counts, total) => Average(total));

        public static readonly Def Death = new Def(
            "ASSESS-DEATH-ANNUAL", "Annual Assessment Report — Death Registration",
            "Death Registration", "Deaths Registered", "death registrations", "Average per Month",
            year => Counts(
                "SELECT MONTH(created_at), COUNT(*) FROM deaths " +
                "WHERE YEAR(created_at) = @y GROUP BY MONTH(created_at)", year),
            (year, counts, total) => Average(total));

        public static readonly Def Marriage = new Def(
            "ASSESS-MARRIAGE-ANNUAL", "Annual Assessment Report — Marriage Registration",
            "Marriage Registration", "Marriages Registered", "marriage registrations", "Average per Month",
            year => Counts(
                "SELECT MONTH(date_registered), COUNT(*) FROM marriages " +
                "WHERE date_registered IS NOT NULL AND YEAR(date_registered) = @y " +
                "GROUP BY MONTH(date_registered)", year),
            (year, counts, total) => Average(total));

        public static readonly Def Queue = new Def(
            "ASSESS-QUEUE-ANNUAL", "Annual Assessment Report — Queuing Operations",
            "Queuing Operations", "Queue Tickets Issued", "queue tickets", "Average per Month",
            year => Counts(
                "SELECT MONTH(created_at), COUNT(*) FROM queue_tickets " +
                "WHERE YEAR(created_at) = @y GROUP BY MONTH(created_at)", year),
            (year, counts, total) => Average(total));

        public static readonly Def Certificate = new Def(
            "ASSESS-CERTIFICATE-ANNUAL", "Annual Assessment Report — Certificate Requests",
            "Certificate Requests", "Certificate Requests Filed", "certificate requests", "Average per Month",
            year => Counts(
                "SELECT MONTH(created_at), COUNT(*) FROM certificate_requests " +
                "WHERE YEAR(created_at) = @y GROUP BY MONTH(created_at)", year),
            (year, counts, total) => Average(total));

        public static readonly Def Psa = new Def(
            "ASSESS-PSA-ANNUAL", "Annual Assessment Report — PSA / Statutory Submission",
            "PSA / Statutory Submission", "Vital Events Registered (Birth + Marriage + Death)",
            "vital events registered", "Total Collections for the Year (PHP)",
            year =>
            {
                int[] b = Counts("SELECT MONTH(date_registered), COUNT(*) FROM births " +
                    "WHERE date_registered IS NOT NULL AND YEAR(date_registered) = @y GROUP BY MONTH(date_registered)", year);
                int[] m = Counts("SELECT MONTH(date_registered), COUNT(*) FROM marriages " +
                    "WHERE date_registered IS NOT NULL AND YEAR(date_registered) = @y GROUP BY MONTH(date_registered)", year);
                int[] d = Counts("SELECT MONTH(created_at), COUNT(*) FROM deaths " +
                    "WHERE YEAR(created_at) = @y GROUP BY MONTH(created_at)", year);
                var total = new int[12];
                for (int i = 0; i < 12; i++) total[i] = b[i] + m[i] + d[i];
                return total;
            },
            (year, counts, total) =>
            {
                decimal collected = 0m;
                try
                {
                    DataTable dt = Db.Pull(
                        "SELECT COALESCE(SUM(net_amount), 0) FROM payments WHERE YEAR(paid_at) = @y",
                        new MySqlParameter("@y", year));
                    if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value) collected = Convert.ToDecimal(dt.Rows[0][0]);
                }
                catch { /* no DB — reports 0.00, never blocks the page */ }
                return "₱ " + collected.ToString("N2", CultureInfo.InvariantCulture);
            });

        public static readonly IReadOnlyList<Def> All = new[] { Birth, Death, Marriage, Queue, Certificate, Psa };

        public static Def FindDef(string formCode) =>
            All.FirstOrDefault(d => string.Equals(d.FormCode, formCode, StringComparison.OrdinalIgnoreCase));

        /// <summary>Used by <see cref="TemplateStore.FieldsFor"/> so the field picker in the
        /// designer can offer this report's columns without that class needing to know the
        /// six form codes itself.</summary>
        public static DataTable ShapeFor(string formCode)
        {
            Def def = FindDef(formCode);
            return def == null ? null : BuildTable(def, DateTime.Today.Year);
        }

        // ------------------------------------------------------------------ layout

        private static readonly string[] MonthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12).ToArray();

        private static List<Form3ACell> BuildCells(string domainTitle, string metricLabel, string secondaryLabel)
        {
            var c = new List<Form3ACell>();
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
            statC(domainTitle, 40f, 116f, 532f, 13f, 11.5f, true);

            stat("Calendar Year:", 40f, 142f, 100f, 12f, 9f, false);
            field("report_year", 142f, 142f, 100f, 12f, 9.5f, true);
            stat("Generated on:", 380f, 142f, 100f, 12f, 9f, false);
            field("generated_on", 456f, 142f, 116f, 12f, 9f, false);
            rule(40f, 160f, 532f);

            stat("MONTH", 60f, 170f, 200f, 12f, 9f, true);
            stat(metricLabel, 300f, 170f, 200f, 12f, 9f, true);
            rule(40f, 186f, 532f);

            float rowY = 190f;
            for (int i = 0; i < 12; i++)
            {
                stat(MonthNames[i], 60f, rowY, 200f, 16f, 9f, false);
                field("m" + (i + 1).ToString("00"), 300f, rowY, 120f, 16f, 9f, false);
                rowY += 18f;
            }

            rule(40f, rowY + 2f, 532f);
            stat("TOTAL FOR THE YEAR", 60f, rowY + 8f, 200f, 14f, 10f, true);
            field("total_count", 300f, rowY + 8f, 120f, 14f, 10f, true);

            float y2 = rowY + 32f;
            stat("Month with the Most " + metricLabel + ":", 40f, y2, 260f, 12f, 9f, false);
            field("peak_month", 300f, y2, 272f, 12f, 9f, true);

            stat(secondaryLabel + ":", 40f, y2 + 20f, 260f, 12f, 9f, false);
            field("secondary_value", 300f, y2 + 20f, 272f, 12f, 9f, true);

            float y3 = y2 + 48f;
            rule(40f, y3, 532f);
            stat("CONCLUSION", 40f, y3 + 8f, 200f, 13f, 10f, true);
            field("conclusion_text", 40f, y3 + 26f, 532f, 96f, 9f, false);

            float y4 = y3 + 134f;
            rule(40f, y4, 532f);
            field("prepared_by_name", 60f, y4 + 14f, 220f, 12f, 9.5f, true);
            stat("Prepared by", 60f, y4 + 30f, 220f, 11f, 8f, false);
            field("reviewed_by_name", 340f, y4 + 14f, 220f, 12f, 9.5f, true);
            stat("Reviewed by / Municipal Civil Registrar", 340f, y4 + 30f, 220f, 11f, 8f, false);

            return c;
        }

        // ------------------------------------------------------------------ data

        public static DataTable BuildTable(Def def, int year)
        {
            var t = new DataTable(def.FormCode.ToLowerInvariant().Replace("-", "_"));
            foreach (Form3ACell cell in def.Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            int[] counts = def.MonthCounts(year);
            int total = counts.Sum();
            int peakIdx = 0;
            for (int i = 1; i < 12; i++) if (counts[i] > counts[peakIdx]) peakIdx = i;
            string secondary = def.SecondaryValue(year, counts, total);

            for (int i = 0; i < 12; i++) r["m" + (i + 1).ToString("00")] = counts[i].ToString("N0");
            r["total_count"] = total.ToString("N0");
            r["report_year"] = year.ToString(CultureInfo.InvariantCulture);
            r["generated_on"] = DateTime.Today.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            r["secondary_value"] = secondary;

            r["peak_month"] = total == 0
                ? "None recorded for " + year
                : MonthNames[peakIdx] + " (" + counts[peakIdx].ToString("N0") + ")";

            r["conclusion_text"] = BuildConclusion(def, year, counts, total, peakIdx, secondary);

            OfficeProfile office = OfficeAssets.Profile;
            r["reviewed_by_name"] = office.RegistrarName ?? "";
            r["prepared_by_name"] = "";

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        /// <summary>Composed strictly from the counted data — never a figure the counting
        /// did not produce. Stored as an ordinary Field, so it is fully rewritable once the
        /// report is opened in the Template Designer.</summary>
        private static string BuildConclusion(Def def, int year, int[] counts, int total, int peakIdx, string secondary)
        {
            if (total == 0)
                return string.Format(
                    "No {0} were recorded for calendar year {1}. There is no basis yet for a " +
                    "monthly trend or a busiest-month comparison.", def.Noun, year);

            string peakSentence = counts[peakIdx] > 0
                ? string.Format("The month with the most {0} was {1}, with {2} recorded.",
                    def.Noun, MonthNames[peakIdx], counts[peakIdx].ToString("N0"))
                : "";

            string secondarySentence = def.SecondaryLabel.StartsWith("Total Collections", StringComparison.OrdinalIgnoreCase)
                ? "Total collections for the year amounted to " + secondary + "."
                : string.Format("On average, {0} {1} were recorded per month.", secondary, def.Noun);

            return string.Format(
                "During calendar year {0}, the Local Civil Registry Office recorded a total of {1} {2}. {3} {4}",
                year, total.ToString("N0"), def.Noun, peakSentence, secondarySentence).Trim();
        }

        // ------------------------------------------------------------------ show / print

        public static void Show(Def def, DataTable t, System.Windows.Forms.IWin32Window owner)
        {
            if (TemplateReportBridge.TryShow(def.FormCode, def.FormName, t, owner)) return;
            using (var doc = BuiltInDocument(def, t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, def.FormName, null))
                f.ShowDialog(owner);
        }

        public static System.Drawing.Printing.PrintDocument BuiltInDocument(Def def, DataTable t)
        {
            var doc = new System.Drawing.Printing.PrintDocument { DocumentName = def.FormName };
            try { doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100); } catch { }
            doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            doc.DefaultPageSettings.Landscape = false;
            doc.PrintPage += (s, e) =>
            {
                e.Graphics.PageUnit = GraphicsUnit.Point;
                if (!doc.PrintController.IsPreview)
                    e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX * 0.72f, -e.PageSettings.HardMarginY * 0.72f);
                Draw(def, e.Graphics, t);
                e.HasMorePages = false;
            };
            return doc;
        }

        public static void Draw(Def def, Graphics g, DataTable t)
        {
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DataRow r = t.Rows.Count > 0 ? t.Rows[0] : null;
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                foreach (Form3ACell c in def.Cells)
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
                        Image img = OfficeAssets.Get(c.Asset, def.FormCode);
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
