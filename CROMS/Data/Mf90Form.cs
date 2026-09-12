using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>One printed box on Municipal Form 90, in points from the page's top-left.</summary>
    public sealed class Mf90Cell
    {
        public string Column;
        public float X, Top, Width, Height, FontSize = 8f;
        /// <summary>Centred under a printed "(First)" / "(Day)" hint, or left-aligned in a wide row.</summary>
        public bool Center;

        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
    }

    /// <summary>
    /// Municipal Form 90 - APPLICATION FOR MARRIAGE LICENSE (Revised January 1993, Form No. 2).
    /// <para/>
    /// The single source of truth for where each value prints. The Crystal report is GENERATED
    /// from <see cref="Cells"/> (CROMS.ReportGen), the no-runtime fallback draws the same cells,
    /// and the test harness asserts against them - so the report, the fallback and the
    /// verification cannot drift apart.
    /// <para/>
    /// MEASURED, not estimated. The office's blank is a vector PDF (612 x 936 pt, 8.5 x 13 in
    /// long bond), so every coordinate below comes from its own content stream
    /// (Docs/AgencyForms/extract_mf90.py): each row is the band between two printed horizontal
    /// rules, and each First/Middle/Last or Day/Month/Year cell is centred on the form's own
    /// printed hint. PDF y runs bottom-up; these are converted to top-down (top = 936 - y).
    /// The form is two columns - husband left, wife right - and the right column is the left
    /// one shifted 305 pt (its "(First)" hint sits at 373.7 against 68.7).
    /// </summary>
    public static class Mf90Form
    {
        public const string FormCode = "MF-90-1993";
        public const string FormName = "Application for Marriage License";
        public const string RptFile = FormCode + ".rpt";
        public const string BlankAsset = "Form90Blank.png";
        public const string TableName = "mf90_application";
        public const float PageWidth = 612f, PageHeight = 936f;

        private const float RightShift = 305f;

        private static List<Mf90Cell> _cells;

        public static IReadOnlyList<Mf90Cell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<Mf90Cell> BuildCells()
        {
            var c = new List<Mf90Cell>();
            Action<string, float, float, float, float, bool> add = (col, x, top, w, h, center) =>
                c.Add(new Mf90Cell { Column = col, X = x, Top = top, Width = w, Height = h, Center = center });

            // ---- header. "Registry No." is the OFFICE's register number for the application;
            // CROMS's own application number is not necessarily that, so the column exists (a
            // .rpt author or a later decision can fill it) but CROMS leaves it blank.
            add("registry_no", 102f, 41f, 133f, 11f, false);       // rule y883 x100.8-235.3
            add("date_receipt", 351f, 41f, 223f, 11f, false);      // rule y883 x349.9-574
            add("date_issued", 332f, 65f, 242f, 11f, false);       // rule y859 x330.1-574

            foreach (string pre in new[] { "husband", "wife" })
            {
                float dx = pre == "husband" ? 0f : RightShift;
                // "May I apply for license to contract marriage with ____" - rule y731,
                // x64-276 left / x336-548 right (272 apart, not 305: this line is indented).
                add(pre + "_apply_with", (pre == "husband" ? 66f : 338f), 192f, 208f, 12f, true);

                NameRow(add, pre + "_", 268f, 13f, dx);                         // rules 681/649, hints y672
                add(pre + "_dob_day", 36f + dx, 300f, 60f, 13f, true);          // rules 649/617, hints y640
                add(pre + "_dob_month", 96f + dx, 300f, 80f, 13f, true);
                add(pre + "_dob_year", 176f + dx, 300f, 52f, 13f, true);
                add(pre + "_age", 228f + dx, 300f, 46f, 13f, true);
                add(pre + "_birth_city", 36f + dx, 331f, 118f, 12f, true);      // rules 617/591, hints y608
                add(pre + "_birth_province", 154f + dx, 331f, 120f, 12f, true);

                Line(add, pre + "_sex", 349f, dx);             // rules 591/571
                Line(add, pre + "_citizenship", 369f, dx);     // 571/551
                Line(add, pre + "_residence", 389f, dx);       // 551/531
                Line(add, pre + "_religion", 409f, dx);        // 531/511
                Line(add, pre + "_civil_status", 429f, dx);    // 511/491

                add(pre + "_prev_dissolution", 40f + dx, 452f, 232f, 14f, false);     // 491/463
                add(pre + "_prev_city", 36f + dx, 484f, 118f, 12f, true);             // 463/439, hints y454
                add(pre + "_prev_province", 154f + dx, 484f, 120f, 12f, true);
                add(pre + "_prev_day", 36f + dx, 508f, 74f, 12f, true);               // 439/415, hints y430
                add(pre + "_prev_month", 110f + dx, 508f, 86f, 12f, true);
                add(pre + "_prev_year", 196f + dx, 508f, 78f, 12f, true);
                // Degree of Relationship (415/387) is deliberately not printed - CROMS does not
                // capture it (backlog §14).

                NameRow(add, pre + "_father_", 562f, 13f, dx);   // 387/359, hints y378
                Line(add, pre + "_father_citizenship", 581f, dx); // 359/339
                Line(add, pre + "_father_residence", 601f, dx);   // 339/319
                NameRow(add, pre + "_mother_", 630f, 13f, dx);   // 319/291, hints y310
                Line(add, pre + "_mother_citizenship", 649f, dx); // 291/271
                Line(add, pre + "_mother_residence", 669f, dx);   // 271/251
                NameRow(add, pre + "_consent_", 697f, 12f, dx);  // 251/225, hints y242
                Line(add, pre + "_consent_relationship", 715f, dx); // 225/205
                Line(add, pre + "_consent_citizenship", 735f, dx);  // 205/185
                Line(add, pre + "_consent_residence", 755f, dx);    // 185/165
            }
            return c;
        }

        /// <summary>First / Middle / Last, centred on the form's own "(First) (Middle) (Last)" hints.</summary>
        private static void NameRow(Action<string, float, float, float, float, bool> add, string pre, float top, float h, float dx)
        {
            add(pre + "first_name", 36f + dx, top, 86f, h, true);
            add(pre + "middle_name", 122f + dx, top, 80f, h, true);
            add(pre + "last_name", 202f + dx, top, 72f, h, true);
        }

        /// <summary>A single value in a 20 pt row with no hint: left-aligned across the column.</summary>
        private static void Line(Action<string, float, float, float, float, bool> add, string col, float top, float dx)
        {
            // +2 pt: measured on the Crystal render, a value at the row's top + 4 read high
            // against the vertically-centred label in the gutter.
            add(col, 40f + dx, top + 2f, 232f, 13f, false);
        }

        // ================================================================ data

        /// <summary>
        /// The application as ONE flat row whose columns are exactly <see cref="Cells"/>, all
        /// strings - already split into the form's boxes, so the report only places text and
        /// the fallback printer draws the same strings.
        /// </summary>
        public static DataTable BuildTable(LicenseFacts l)
        {
            var t = new DataTable(TableName);
            foreach (Mf90Cell cell in Cells) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            r["date_receipt"] = LongDate(l.FiledDate);
            r["date_issued"] = LongDate(l.IssueDate);
            DateTime on = l.FiledDate ?? DateTime.Today;
            Party(r, "husband", l.Husband, l.Wife, on);
            Party(r, "wife", l.Wife, l.Husband, on);

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        private static void Party(DataRow r, string pre, Party p, Party other, DateTime on)
        {
            Func<string, string> s = v => string.IsNullOrWhiteSpace(v) ? "" : v.Trim();
            r[pre + "_apply_with"] = s(other.FullName);
            r[pre + "_first_name"] = s(p.First); r[pre + "_middle_name"] = s(p.Middle); r[pre + "_last_name"] = s(p.Last);
            if (p.Dob.HasValue)
            {
                r[pre + "_dob_day"] = p.Dob.Value.Day.ToString(CultureInfo.InvariantCulture);
                r[pre + "_dob_month"] = p.Dob.Value.ToString("MMMM", CultureInfo.InvariantCulture);
                r[pre + "_dob_year"] = p.Dob.Value.Year.ToString(CultureInfo.InvariantCulture);
                // Age on the FILING date - the same figure the application screen shows.
                r[pre + "_age"] = MarriageRules.AgeOn(p.Dob.Value, on).ToString(CultureInfo.InvariantCulture);
            }
            // Place of birth is stored "City, Province" by the licence screen itself. A birth
            // abroad has no Philippine province, so the country rides in the province box -
            // the form has no country box and dropping it would lose where the person was born.
            r[pre + "_birth_city"] = CityOf(p.PlaceOfBirth);
            string prov = ProvinceOf(p.PlaceOfBirth);
            if (!string.IsNullOrWhiteSpace(p.BirthCountry) && !GeoLookup.IsHome(p.BirthCountry))
                prov = string.IsNullOrEmpty(prov) ? p.BirthCountry.Trim() : prov + ", " + p.BirthCountry.Trim();
            r[pre + "_birth_province"] = prov;

            r[pre + "_sex"] = s(p.Sex);
            r[pre + "_citizenship"] = s(p.Citizenship);
            r[pre + "_residence"] = s(p.Residence);
            r[pre + "_religion"] = s(p.Religion);
            r[pre + "_civil_status"] = s(p.CivilStatus);

            // Only a dissolved previous marriage prints - the same rule the screen and
            // SaveLicense apply, repeated here so a stale value can never reach paper.
            if (MarriageRules.IsPreviouslyMarried(p.CivilStatus))
            {
                r[pre + "_prev_dissolution"] = s(p.PrevDissolution);
                r[pre + "_prev_city"] = s(p.PrevDissolvedMunicipality);
                r[pre + "_prev_province"] = s(p.PrevDissolvedProvince);
                if (p.PrevDissolvedDate.HasValue)
                {
                    r[pre + "_prev_day"] = p.PrevDissolvedDate.Value.Day.ToString(CultureInfo.InvariantCulture);
                    r[pre + "_prev_month"] = p.PrevDissolvedDate.Value.ToString("MMMM", CultureInfo.InvariantCulture);
                    r[pre + "_prev_year"] = p.PrevDissolvedDate.Value.Year.ToString(CultureInfo.InvariantCulture);
                }
            }

            r[pre + "_father_first_name"] = s(p.FatherFirst); r[pre + "_father_middle_name"] = s(p.FatherMiddle);
            r[pre + "_father_last_name"] = s(p.FatherLast);
            r[pre + "_father_citizenship"] = s(p.FatherCitizenship); r[pre + "_father_residence"] = s(p.FatherResidence);
            r[pre + "_mother_first_name"] = s(p.MotherFirst); r[pre + "_mother_middle_name"] = s(p.MotherMiddle);
            r[pre + "_mother_last_name"] = s(p.MotherLast);
            r[pre + "_mother_citizenship"] = s(p.MotherCitizenship); r[pre + "_mother_residence"] = s(p.MotherResidence);
            r[pre + "_consent_first_name"] = s(p.ConsentFirst); r[pre + "_consent_middle_name"] = s(p.ConsentMiddle);
            r[pre + "_consent_last_name"] = s(p.ConsentLast);
            r[pre + "_consent_relationship"] = s(p.ConsentRelationship);
            r[pre + "_consent_citizenship"] = s(p.ConsentCitizenship); r[pre + "_consent_residence"] = s(p.ConsentResidence);
        }

        private static string LongDate(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) : "";
        }

        /// <summary>The province: the LAST comma part of the licence screen's own "City, Province".</summary>
        private static string ProvinceOf(string place)
        {
            string[] bits = (place ?? "").Split(',');
            return bits.Length < 2 ? "" : bits[bits.Length - 1].Trim();
        }

        /// <summary>Everything before the province, kept whole (a legacy three-part value is not truncated).</summary>
        private static string CityOf(string place)
        {
            string[] bits = (place ?? "").Split(',');
            if (bits.Length < 2) return (place ?? "").Trim();
            return string.Join(", ", bits.Take(bits.Length - 1).Select(x => x.Trim()));
        }

        /// <summary>
        /// Values too long for their printed box. A box on paper cannot grow, so a long
        /// residence is cut at the box edge by both renderers - and a clipped government
        /// record that LOOKS complete is the failure this project keeps refusing. The preview
        /// says so up front instead.
        /// </summary>
        public static List<string> Overflows(DataTable t)
        {
            var list = new List<string>();
            if (t.Rows.Count == 0) return list;
            DataRow r = t.Rows[0];
            using (var bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.PageUnit = GraphicsUnit.Point;
                foreach (Mf90Cell c in Cells)
                {
                    string v = r[c.Column] as string;
                    if (string.IsNullOrEmpty(v)) continue;
                    using (var f = new Font("Arial", c.FontSize))
                        if (g.MeasureString(v, f, PointF.Empty, StringFormat.GenericTypographic).Width > c.Width - 2f)
                            list.Add(Label(c.Column) + ": \"" + v + "\"");
                }
            }
            return list;
        }

        private static string Label(string column)
        {
            string s = column.Replace("husband_", "Husband ").Replace("wife_", "Wife ").Replace('_', ' ');
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
        }

        // ================================================================ show

        public static string ReportPath
        {
            get { return Path.Combine(CertificateReport.ReportsFolder, RptFile); }
        }

        /// <summary>
        /// Preview the application, ready to print. Crystal when its report is deployed and the
        /// runtime is on this PC; otherwise CROMS draws the identical page itself. Returns which
        /// one showed ("Crystal" / "Built-in").
        /// </summary>
        public static string Show(LicenseFacts l, System.Windows.Forms.IWin32Window owner)
        {
            DataTable t = BuildTable(l);
            string caption = "Application for Marriage License  -  Municipal Form 90" +
                             (string.IsNullOrEmpty(l.ApplicationNo) ? "" : "  -  " + l.ApplicationNo);
            string note = OverflowNote(t);

            if (File.Exists(ReportPath) && CertificateReport.CrystalAvailable)
            {
                try
                {
                    CrystalRunner.ShowTable(ReportPath, t, caption, note, owner);
                    return "Crystal";
                }
                catch (Exception ex)
                {
                    // A broken report must not leave the clerk without the form: say so and
                    // print it anyway, from the same cells.
                    System.Windows.Forms.MessageBox.Show(owner,
                        "The Crystal report for Form 90 could not be opened, so CROMS will show the form itself.\n\n" + ex.Message,
                        "Crystal report unavailable", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            using (var doc = BuiltInDocument(t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, caption, note))
                f.ShowDialog(owner);
            return "Built-in";
        }

        public static string OverflowNote(DataTable t)
        {
            List<string> over = Overflows(t);
            if (over.Count == 0) return null;
            return "Too long for the box on the paper form - will be cut off when printed. Shorten it on the application first:\n" +
                   string.Join("\n", over.Take(5)) + (over.Count > 5 ? "\n(+" + (over.Count - 5) + " more)" : "");
        }

        /// <summary>A PrintDocument on the form's own 8.5 x 13 in page that draws <see cref="Draw"/>.</summary>
        public static System.Drawing.Printing.PrintDocument BuiltInDocument(DataTable t)
        {
            var doc = new System.Drawing.Printing.PrintDocument { DocumentName = FormName };
            try { doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Long bond 8.5x13", 850, 1300); } catch { }
            doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            doc.DefaultPageSettings.Landscape = false;
            Image blank = null;
            try { if (File.Exists(BlankPath)) blank = Image.FromFile(BlankPath); } catch { blank = null; }
            doc.PrintPage += (s, e) =>
            {
                // A printer cannot print into its hard margin, and the Graphics origin sits at
                // that margin; shift back so the page lands where the form's rules are.
                // HardMargin is in 1/100 inch; the drawing is in points, so convert (x 0.72).
                e.Graphics.PageUnit = GraphicsUnit.Point;
                if (!doc.PrintController.IsPreview)
                    e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX * 0.72f, -e.PageSettings.HardMarginY * 0.72f);
                Draw(e.Graphics, t, blank);
                e.HasMorePages = false;
            };
            doc.Disposed += (s, e) => { if (blank != null) blank.Dispose(); };
            return doc;
        }

        // ================================================================ built-in renderer

        public static string BlankPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", BlankAsset); }
        }

        /// <summary>
        /// Draw the application onto a page, in points: the blank form, then every value in
        /// its box. Used when the Crystal runtime is not installed on this PC, and by the test
        /// harness to compare against the Crystal output. Same cells, same strings.
        /// </summary>
        public static void Draw(Graphics g, DataTable t, Image blank)
        {
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            if (blank != null) g.DrawImage(blank, 0, 0, PageWidth, PageHeight);
            if (t.Rows.Count == 0) return;
            DataRow r = t.Rows[0];
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            {
                foreach (Mf90Cell c in Cells)
                {
                    string v = r[c.Column] as string;
                    if (string.IsNullOrEmpty(v)) continue;
                    using (var f = new Font("Arial", c.FontSize))
                        g.DrawString(v, f, Brushes.Black, c.Rect, c.Center ? center : left);
                }
            }
        }
    }
}
