using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>One printed item on Municipal Form No. 68 (Advice Upon Intended Marriage), in
    /// points from the page's top-left. Same "Static" / "Field" shape as <see cref="ConsentCell"/>;
    /// a Field column ending "_signature" draws a blank rule, never text.</summary>
    public sealed class AdviceCell
    {
        public string Kind;
        public string Text;
        public string Column;
        public float X, Top, Width, Height, FontSize = 8f;
        public bool Bold, Center;
        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
        public bool IsSignatureLine { get { return Kind == "Field" && Column != null && Column.EndsWith("_signature", StringComparison.Ordinal); } }
    }

    /// <summary>
    /// Municipal Form No. 68 (Form No. 6) - ADVICE UPON INTENDED MARRIAGE (Family Code Art. 15).
    /// <para/>
    /// Measured, ONE physical Letter page (612 x 792 pt) carrying BOTH the MALE and FEMALE
    /// sections, one directly above the other - the office's own re-typed Word document is not
    /// two separate sheets. The FEMALE block is the MALE block's own layout shifted down
    /// 249.84 pt (measured across every line - "To:", the SUBSCRIBED clause, the oath signature
    /// - and consistent to within a point), so it is built from one shared template rather than
    /// re-typed twice. No scanned artwork exists for this form either, so it is drawn directly,
    /// the same reasoning as <see cref="ConsentForm"/>.
    /// <para/>
    /// Both halves print every time: the husband's name/date/spouse always belongs on the MALE
    /// half and the wife's on the FEMALE half regardless of whether either currently needs
    /// advice (21-25) - that is simple fact, not a finding. What is NEVER printed is a
    /// signature or a "yes advice was given" mark; those stay blank rules, because an
    /// unfavourable or withheld advice is exactly what triggers the Art. 15 deferral and CROMS
    /// must not manufacture the appearance that it was given.
    /// </summary>
    public static class AdviceForm
    {
        public const string FormCode = "MF-68-ADVICE";
        public const string FormName = "Advice Upon Intended Marriage";
        public const float PageWidth = 612f, PageHeight = 792f;
        private const float FemaleShift = 249.84f;

        private static List<AdviceCell> _cells;
        public static IReadOnlyList<AdviceCell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<AdviceCell> BuildCells()
        {
            var c = new List<AdviceCell>();
            Action<string, float, float, float, float, float, bool, bool> stat = (text, x, top, w, h, size, bold, center) =>
                c.Add(new AdviceCell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold, Center = center });
            Action<string, float, float, float, float, float, bool> field = (col, x, top, w, h, size, center) =>
                c.Add(new AdviceCell { Kind = "Field", Column = col, X = x, Top = top, Width = w, Height = h, FontSize = size, Center = center });

            Block(stat, field, "male", "MALE", 0f);
            Block(stat, field, "female", "FEMALE", FemaleShift);
            return c;
        }

        private static void Block(Action<string, float, float, float, float, float, bool, bool> stat,
                                   Action<string, float, float, float, float, float, bool> field,
                                   string prefix, string sexWord, float dy)
        {
            stat("Municipal Form No. 68 (Form No. 6)", 34.559f, 29.2633f + dy, 120.52f, 8.05f, 7f, false, false);
            stat("ADVICE UPON INTENDED MARRIAGE", 200.759f, 39.2934f + dy, 209.92f, 11.05f, 9.5f, true, false);
            // Centred; the printed word's own width differs slightly by sex on the real form,
            // but one common centred box reads correctly for either.
            stat("(" + sexWord + ")", 270f, 51.0f + dy, 72f, 8.5f, 8f, false, true);

            stat("To:", 34.559f, 62.91f + dy, 11.71f, 8.5f, 7.5f, false, false);
            field(prefix + "_applicant_full_name", 50.64f, 62.91f + dy, 255.04f, 8.5f, 7.5f, false);

            stat("Our / My advice upon the intended marriage with", 34.559f, 76.71f + dy, 183.77f, 8.5f, 7.5f, false, false);
            field(prefix + "_intended_spouse_full_name", 222.24f, 76.71f + dy, 170.08f, 8.5f, 7.5f, false);
            stat("having been asked by you, and knowing no legal", 396.24f, 76.71f + dy, 181.15f, 8.5f, 7.5f, false, false);

            stat("impediment to this marriage, we / I hereby advise you to marry him / her LEGALLY.",
                34.559f, 86.551f + dy, 290.96f, 8.5f, 7.5f, false, false);

            field(prefix + "_father_signature", 100.2f, 119.191f + dy, 136.0f, 8.5f, 7.5f, false);
            field(prefix + "_mother_signature", 371.64f, 119.191f + dy, 136.0f, 8.5f, 7.5f, false);
            stat("(Signature of Father)", 131.88f, 129.031f + dy, 72.67f, 8.5f, 7f, false, false);
            stat("(Signature of Mother)", 402.359f, 129.031f + dy, 74.59f, 8.5f, 7f, false, false);

            field(prefix + "_guardian_signature", 235.92f, 156.751f + dy, 136.0f, 8.5f, 7.5f, false);
            stat("(Signature of Legal Guardian or Head of", 233.88f, 166.591f + dy, 140.23f, 8.5f, 7f, false, false);
            stat("Institution)", 285.0f, 176.31f + dy, 37.87f, 8.5f, 7f, false, false);

            stat("SUBSCRIBED AND SWORN to before me this", 34.559f, 200.9833f + dy, 155.53f, 8.05f, 7.5f, false, false);
            field(prefix + "_date_signed_day", 192.12f, 200.9833f + dy, 24.06f, 8.05f, 7.5f, true);
            stat("day of", 218.04f, 200.9833f + dy, 20.20f, 8.05f, 7.5f, false, false);
            field(prefix + "_date_signed_month", 240.24f, 200.9833f + dy, 80.10f, 8.05f, 7.5f, true);
            stat(", 20", 320.2701f, 200.9833f + dy, 12.07f, 8.05f, 7.5f, false, false);
            field(prefix + "_date_signed_year", 332.2791f, 200.9833f + dy, 16.02f, 8.05f, 7.5f, true);
            stat("at", 350.28f, 200.9833f + dy, 5.83f, 8.05f, 7.5f, false, false);
            field("municipality", 358.08f, 200.9833f + dy, 120.05f, 8.05f, 7.5f, false);
            stat(".", 478.065f, 200.9833f + dy, 2.01f, 8.05f, 7.5f, false, false);

            field(prefix + "_oath_administering_person_signature", 371.64f, 230.31f + dy, 136.0f, 8.5f, 7.5f, false);
            stat("Signature of Person Administering Oath", 371.04f, 240.151f + dy, 137.45f, 8.5f, 7f, false, false);
        }

        // ================================================================ data

        /// <summary>The advice form as ONE flat row: both halves, from the same LicenseFacts -
        /// male_* from the husband, female_* from the wife (per Family Code sex convention).</summary>
        public static DataTable BuildTable(LicenseFacts l)
        {
            var t = new DataTable("mf68_advice");
            foreach (AdviceCell cell in Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            r["municipality"] = (OfficeAssets.Profile.Municipality ?? "");
            DateTime on = l.FiledDate ?? DateTime.Today;
            Fill(r, "male", l.Husband, l.Wife, on);
            Fill(r, "female", l.Wife, l.Husband, on);

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        private static void Fill(DataRow r, string prefix, Party p, Party other, DateTime on)
        {
            r[prefix + "_applicant_full_name"] = p.FullName;
            r[prefix + "_intended_spouse_full_name"] = other.FullName;
            r[prefix + "_date_signed_day"] = on.Day.ToString(CultureInfo.InvariantCulture);
            r[prefix + "_date_signed_month"] = on.ToString("MMMM", CultureInfo.InvariantCulture);
            // The paper prints ", 20__" - "20" is already static text (see Block), so only the
            // last two digits go in the blank; the full year would print "202026".
            r[prefix + "_date_signed_year"] = (on.Year % 100).ToString("00", CultureInfo.InvariantCulture);
            // father_signature / mother_signature / guardian_signature /
            // oath_administering_person_signature stay "" - see the class doc.
        }

        public static List<string> Overflows(DataTable t)
        {
            var list = new List<string>();
            if (t.Rows.Count == 0) return list;
            DataRow r = t.Rows[0];
            using (var bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.PageUnit = GraphicsUnit.Point;
                foreach (AdviceCell c in Cells)
                {
                    if (c.Kind != "Field" || c.IsSignatureLine) continue;
                    string v = t.Columns.Contains(c.Column) ? r[c.Column] as string : null;
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
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(column.Replace('_', ' '));
        }

        public static string OverflowNote(DataTable t)
        {
            List<string> over = Overflows(t);
            if (over.Count == 0) return null;
            return "Too long for its printed line - will be cut off when printed. Shorten it first:\n" +
                   string.Join("\n", over.Take(5)) + (over.Count > 5 ? "\n(+" + (over.Count - 5) + " more)" : "");
        }

        // ================================================================ show / print

        public static void Show(LicenseFacts l, System.Windows.Forms.IWin32Window owner)
        {
            DataTable t = BuildTable(l);
            string caption = FormName + " - " + l.Husband.FullName + " & " + l.Wife.FullName;
            using (var doc = BuiltInDocument(t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, caption, OverflowNote(t)))
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
            if (t.Rows.Count == 0) return;
            DataRow r = t.Rows[0];
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            {
                foreach (AdviceCell c in Cells)
                {
                    if (c.Kind == "Static")
                    {
                        using (var f = new Font("Arial", c.FontSize, c.Bold ? FontStyle.Bold : FontStyle.Regular))
                            g.DrawString(c.Text, f, Brushes.Black, c.Rect, c.Center ? center : left);
                        continue;
                    }
                    if (c.IsSignatureLine)
                    {
                        using (var pen = new Pen(Color.Black, 0.75f))
                            g.DrawLine(pen, c.X, c.Top + c.Height, c.X + c.Width, c.Top + c.Height);
                        continue;
                    }
                    string v = t.Columns.Contains(c.Column) ? r[c.Column] as string : null;
                    if (string.IsNullOrEmpty(v)) continue;
                    using (var f = new Font("Arial", c.FontSize))
                        g.DrawString(v, f, Brushes.Black, c.Rect, c.Center ? center : left);
                }
            }
        }
    }
}
