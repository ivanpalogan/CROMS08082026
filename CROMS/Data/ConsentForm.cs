using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// One printed item on Municipal Form No. 06 (Consent to Marriage of a Person Underage),
    /// in points from the page's top-left. "Static" draws <see cref="Text"/> verbatim; "Field"
    /// draws the value of <see cref="Column"/> from the built table - or, for a column whose
    /// name ends "_signature", a blank rule to sign on, since CROMS never captures a signature
    /// image for this form.
    /// </summary>
    public sealed class ConsentCell
    {
        public string Kind;      // "Static" or "Field"
        public string Text;      // Kind == "Static"
        public string Column;    // Kind == "Field"
        public float X, Top, Width, Height, FontSize = 8f;
        public bool Bold, Center;
        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
        public bool IsSignatureLine { get { return Kind == "Field" && Column != null && Column.EndsWith("_signature", StringComparison.Ordinal); } }
    }

    /// <summary>
    /// Municipal Form No. 06 - CONSENT TO MARRIAGE OF A PERSON UNDERAGE (Family Code Art. 14).
    /// <para/>
    /// There is no scanned blank for this form - the office supplied a RE-TYPED Word document,
    /// not a scan of their own stock (recorded 2026-09-13), so an image overlay would not be a
    /// true replica anyway. Every printed element - heading, paragraph, label, and signature
    /// line - is instead measured and drawn directly by CROMS: same fidelity, no image to chase.
    /// Coordinates are Letter page (612 x 792 pt), top-down, measured from the office's document.
    /// <para/>
    /// One applicant per print: whichever party is 18-20 needs their OWN consent form, naming
    /// the other party as the intended spouse. The two never share one sheet.
    /// </summary>
    public static class ConsentForm
    {
        public const string FormCode = "MF-06-CONSENT";
        public const string FormName = "Consent to Marriage of a Person Underage";
        public const float PageWidth = 612f, PageHeight = 792f;

        private static List<ConsentCell> _cells;
        public static IReadOnlyList<ConsentCell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<ConsentCell> BuildCells()
        {
            var c = new List<ConsentCell>();
            Action<string, float, float, float, float, float, bool> stat = (text, x, top, w, h, size, bold) =>
                c.Add(new ConsentCell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<string, float, float, float, float, float, bool> field = (col, x, top, w, h, size, center) =>
                c.Add(new ConsentCell { Kind = "Field", Column = col, X = x, Top = top, Width = w, Height = h, FontSize = size, Center = center });

            stat("CONSENT TO MARRIAGE OF A PERSON UNDERAGE", 142.68f, 37.84f, 326.20f, 12.5f, 11f, true);
            stat("Municipality of", 198.48f, 58.30f, 62.47f, 9.95f, 8.5f, false);
            field("municipality", 263.4f, 58.30f, 150.05f, 9.95f, 8.5f, false);
            stat("Province of", 199.08f, 69.82f, 46.27f, 9.95f, 8.5f, false);
            field("province", 247.92f, 69.82f, 165.05f, 9.95f, 8.5f, false);

            stat("I,", 50.4f, 86.38f, 5.85f, 9.95f, 8.5f, false);
            field("consent_person_full_name", 58.68f, 86.38f, 220.0f, 9.95f, 8.5f, false);
            stat(", resident of", 278.75f, 86.38f, 47.48f, 9.95f, 8.5f, false);
            field("consent_person_residence", 328.68f, 86.38f, 220.0f, 9.95f, 8.5f, false);
            stat(",", 548.75f, 86.38f, 2.49f, 9.95f, 8.5f, false);

            field("consent_person_relationship", 50.4f, 100.78f, 114.31f, 9.95f, 8.5f, false);
            stat("of", 167.28f, 100.78f, 8.35f, 9.95f, 8.5f, false);
            field("applicant_full_name", 178.08f, 100.78f, 240.04f, 9.95f, 8.5f, false);
            stat(".", 418.07f, 100.78f, 2.49f, 9.95f, 8.5f, false);

            stat("A resident of", 73.44f, 119.38f, 52.15f, 9.95f, 8.5f, false);
            field("applicant_residence", 128.16f, 119.38f, 220.0f, 9.95f, 8.5f, false);
            stat(", single and less than (twenty-one) years of age,", 348.23f, 119.38f, 190.30f, 9.95f, 8.5f, false);

            stat("being duly sworn, do hereby depose and say that I freely consent to", 50.4f, 131.74f, 269.57f, 9.95f, 8.5f, false);
            // The applicant's own name is repeated here (the paper says it twice) - same value,
            // drawn a second time. Not a bug: two Cells sharing one Column is expected.
            field("applicant_full_name", 322.44f, 131.74f, 180.05f, 9.95f, 8.5f, false);
            stat("marrying", 504.96f, 131.74f, 36.53f, 9.95f, 8.5f, false);

            field("intended_spouse_full_name", 50.4f, 144.10f, 180.05f, 9.95f, 8.5f, false);
            stat(", resident of", 230.39f, 144.10f, 47.48f, 9.95f, 8.5f, false);
            field("intended_spouse_residence", 280.32f, 144.10f, 220.0f, 9.95f, 8.5f, false);
            stat(", and that I", 500.39f, 144.10f, 42.68f, 9.95f, 8.5f, false);

            stat("know of no legal impediment to such marriage.", 50.4f, 156.58f, 189.32f, 9.95f, 8.5f, false);

            field("consent_person_signature", 359.76f, 184.72f, 144.05f, 9.0f, 8f, false);
            stat("Signature of Father, Mother, or Guardian", 355.92f, 195.04f, 151.74f, 9.0f, 7.5f, false);

            stat("WITNESSES (Not necessary if this affidavit is subscribed before the Local Civil Registrar concerned.)",
                50.4f, 210.74f, 361.63f, 9.5f, 7.5f, false);
            field("witness_1_signature", 104.16f, 231.28f, 144.05f, 9.0f, 8f, false);
            field("witness_2_signature", 359.76f, 231.28f, 144.05f, 9.0f, 8f, false);
            stat("Witness", 162.24f, 241.60f, 27.98f, 9.0f, 7.5f, false);
            stat("Witness", 417.84f, 241.60f, 27.98f, 9.0f, 7.5f, false);

            stat("SUBSCRIBED AND SWORN to before me this", 50.4f, 259.34f, 251.97f, 9.5f, 8.5f, false);
            field("date_signed_day", 314.76f, 259.34f, 28.51f, 9.5f, 8.5f, true);
            stat("day of", 355.68f, 259.34f, 35.08f, 9.5f, 8.5f, false);
            field("date_signed_month", 403.08f, 259.34f, 94.99f, 9.5f, 8.5f, true);
            stat(", 20", 498.12f, 259.34f, 24.19f, 9.5f, 8.5f, false);
            // Measured 18.91pt only fits 2 digits; a 4-digit year ("2026") needs the extra room
            // before "at" (the next static sits at 553.68, leaving slack unused by the label).
            field("date_signed_year", 522.36f, 259.34f, 30.0f, 9.5f, 8.5f, true);
            stat("at", 553.68f, 259.34f, 7.96f, 9.5f, 8.5f, false);

            field("municipality", 50.4f, 270.26f, 142.51f, 9.5f, 8.5f, false);
            stat(", Philippines.", 192.96f, 270.26f, 52.42f, 9.5f, 8.5f, false);

            field("oath_administering_person_signature", 359.76f, 290.80f, 144.05f, 9.0f, 8f, false);
            stat("Signature of Person Administering Oath", 359.04f, 301.12f, 145.50f, 9.0f, 7.5f, false);
            stat("Title / Position:", 349.08f, 311.31f, 52.75f, 8.5f, 7.5f, false);
            // Left blank on purpose - whoever administers the oath is decided at signing, not
            // something CROMS has on file to print in advance.
            field("oath_administering_person_title", 404.04f, 311.31f, 110.57f, 8.5f, 7.5f, false);

            stat("INSTRUCTIONS", 269.76f, 333.62f, 72.24f, 9.5f, 8.5f, true);
            stat("In case either or both of the contracting parties, being single, or less than twenty-one years of age, they shall exhibit to the LCR concerned the consent to",
                68.4f, 346.90f, 493.26f, 8.05f, 7f, false);
            stat("their marriage of their father, mother, guardian, or person having legal charge of them, in the order mentioned. Such consent shall be in writing under oath",
                50.4f, 356.14f, 511.26f, 8.05f, 7f, false);
            stat("taken with the appearance of the interested parties before the LCR or in the form of an affidavit made in the presence of two witnesses and subscribed before",
                50.4f, 365.38f, 511.16f, 8.05f, 7f, false);
            stat("any official authorized by law to administer oaths. (Rep. Act 236 Art. 61).",
                50.4f, 374.50f, 237.33f, 8.05f, 7f, false);
            stat("For the purpose of the Marriage Law, by guardian is meant a guardian legally appointed by will or by a competent court for the person, or both the person",
                68.4f, 386.74f, 493.14f, 8.05f, 7f, false);
            stat("and estate, of a minor. By person having legal charge is meant a person actually in lawful charge of a minor who has no father or legal guardian.",
                50.4f, 395.98f, 462.09f, 8.05f, 7f, false);
            stat("Municipal Form No. 06", 50.4f, 766.66f, 76.02f, 8.05f, 7f, false);

            return c;
        }

        // ================================================================ data

        /// <summary>
        /// The consent form as ONE flat row for whichever party (Husband/Wife) needs it. The
        /// other party is printed as the intended spouse. Nothing is invented: a blank Party
        /// field prints as a blank box, never guessed.
        /// </summary>
        public static DataTable BuildTable(LicenseFacts l, string partyRole)
        {
            Party p = string.Equals(partyRole, "Wife", StringComparison.OrdinalIgnoreCase) ? l.Wife : l.Husband;
            Party other = ReferenceEquals(p, l.Wife) ? l.Husband : l.Wife;
            OfficeProfile office = OfficeAssets.Profile;

            var t = new DataTable("mf06_consent");
            foreach (ConsentCell cell in Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            r["municipality"] = office.Municipality ?? "";
            r["province"] = office.Province ?? "";
            r["consent_person_full_name"] = MarriageRules.JoinName(p.ConsentFirst, p.ConsentMiddle, p.ConsentLast) ?? "";
            r["consent_person_residence"] = p.ConsentResidence ?? "";
            r["consent_person_relationship"] = p.ConsentRelationship ?? "";
            r["applicant_full_name"] = p.FullName;
            r["applicant_residence"] = p.Residence ?? "";
            r["intended_spouse_full_name"] = other.FullName;
            r["intended_spouse_residence"] = other.Residence ?? "";

            DateTime on = l.FiledDate ?? DateTime.Today;
            r["date_signed_day"] = on.Day.ToString(CultureInfo.InvariantCulture);
            r["date_signed_month"] = on.ToString("MMMM", CultureInfo.InvariantCulture);
            // The paper prints ", 20__" - "20" is already static text (see BuildCells), so only
            // the last two digits go in the blank. Writing the full year here would print
            // "202026" - caught by rendering the actual page, not by reading the code.
            r["date_signed_year"] = (on.Year % 100).ToString("00", CultureInfo.InvariantCulture);
            // consent_person_signature / witness_1_signature / witness_2_signature /
            // oath_administering_person_signature / oath_administering_person_title are left
            // "" - see the field comments above.

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        /// <summary>
        /// Values too long for their printed line. A line on paper cannot grow, so a long
        /// residence would be cut at the line's edge by the renderer - and a clipped
        /// government record that LOOKS complete is the failure this project keeps refusing.
        /// The preview says so up front instead.
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
                foreach (ConsentCell c in Cells)
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

        /// <summary>Preview the consent form for one party, ready to print. No Crystal path -
        /// there is no scanned artwork to embed, so the built-in renderer IS the fidelity.</summary>
        public static void Show(LicenseFacts l, string partyRole, System.Windows.Forms.IWin32Window owner)
        {
            Party p = string.Equals(partyRole, "Wife", StringComparison.OrdinalIgnoreCase) ? l.Wife : l.Husband;
            DataTable t = BuildTable(l, partyRole);
            string caption = FormName + " - " + p.FullName;
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

        /// <summary>Draw every static line, then every field value, then a blank rule for every
        /// signature - the whole page, since there is no background image to lay values onto.</summary>
        public static void Draw(Graphics g, DataTable t)
        {
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            if (t.Rows.Count == 0) return;
            DataRow r = t.Rows[0];
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap })
            {
                foreach (ConsentCell c in Cells)
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
