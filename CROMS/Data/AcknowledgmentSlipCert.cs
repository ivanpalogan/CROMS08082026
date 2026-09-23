using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>One printed item on the Acknowledgment Slip, in points from the page's
    /// top-left. Same shape as <see cref="Form3ACell"/> ("Static" draws <see cref="Text"/>
    /// verbatim; "Field" draws the value of <see cref="Column"/> from the built table;
    /// "Picture" draws an office asset; "Rule" draws a line) — kept as its own type rather
    /// than reusing Form3ACell so this template can evolve without touching the Facts
    /// Certification family it has nothing to do with.</summary>
    public sealed class AckSlipCell
    {
        public string Kind;      // "Static" / "Field" / "Picture" / "Rule" / "Box"
        public string Text;      // Kind == "Static"
        public string Column;    // Kind == "Field"
        public AssetKind Asset;  // Kind == "Picture"
        public float X, Top, Width, Height, FontSize = 8f;
        public bool Bold, Center, Italic, Warn; // Warn = the disclaimer notes, drawn in red
        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
    }

    /// <summary>
    /// The Acknowledgment of Submission slip — shared by Birth, Marriage and Death
    /// Registration for a transaction that has been submitted but is still pending
    /// verification. Registered in <see cref="TemplateStore.KnownForms"/> like every other
    /// certificate, so an admin can open it in Certificate Templates and move/reword/restyle
    /// every field (including both disclaimer notes and the documents-received list) exactly
    /// the way Form 1A/2A/3A already can — the office's letterhead assets apply here too.
    /// <para/>
    /// It deliberately carries NO fee/amount/O.R. field: acknowledging a submission and
    /// assessing a fee are two separate events in this app (Fees &amp; Payments), and this
    /// slip must never be mistaken for either a certificate or a receipt of payment. Both
    /// disclaimer notes ("NOT a Certificate of Registration...") are Static text, so they can
    /// be reworded but can never be accidentally deleted by a blank record value the way a
    /// Field cell could.
    /// </summary>
    public static class AcknowledgmentSlipCert
    {
        public const string FormCode = "ACK-SLIP-SUBMISSION";
        public const string FormName = "Acknowledgment of Submission";
        public const float PageWidth = 612f, PageHeight = 500f; // Letter width, receipt-length page

        private static List<AckSlipCell> _cells;
        public static IReadOnlyList<AckSlipCell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        private static List<AckSlipCell> BuildCells()
        {
            var c = new List<AckSlipCell>();
            Action<string, float, float, float, float, float, bool> stat = (text, x, top, w, h, size, bold) =>
                c.Add(new AckSlipCell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<string, float, float, float, float, float, bool> statC = (text, x, top, w, h, size, bold) =>
                c.Add(new AckSlipCell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold, Center = true });
            Action<string, float, float, float, float, float> noteBold = (text, x, top, w, h, size) =>
                c.Add(new AckSlipCell { Kind = "Static", Text = text, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = true, Center = true, Warn = true });
            Action<string, float, float, float, float, float, bool> field = (col, x, top, w, h, size, bold) =>
                c.Add(new AckSlipCell { Kind = "Field", Column = col, X = x, Top = top, Width = w, Height = h, FontSize = size, Bold = bold });
            Action<AssetKind, float, float, float, float> pic = (kind, x, top, w, h) =>
                c.Add(new AckSlipCell { Kind = "Picture", Asset = kind, X = x, Top = top, Width = w, Height = h });
            Action<float, float, float> rule = (x, top, w) =>
                c.Add(new AckSlipCell { Kind = "Rule", X = x, Top = top, Width = w, Height = 1f });
            Action<float, float, float, float> box = (x, top, w, h) =>
                c.Add(new AckSlipCell { Kind = "Box", X = x, Top = top, Width = w, Height = h });

            pic(AssetKind.HeaderLogoLeft, 40f, 16f, 46f, 46f);
            pic(AssetKind.HeaderLogoRight1, 480f, 16f, 46f, 46f);

            statC("Republic of the Philippines", 40f, 18f, 532f, 12f, 9f, false);
            field("office_header_line", 40f, 30f, 532f, 14f, 11f, true);
            statC("ACKNOWLEDGMENT OF SUBMISSION", 40f, 48f, 532f, 18f, 14.5f, true);
            statC("(Transaction Receipt)", 40f, 66f, 532f, 12f, 8.5f, false);
            rule(40f, 82f, 532f);

            box(40f, 90f, 532f, 30f);
            noteBold("This document acknowledges receipt of transaction only. It is NOT a Certificate " +
                     "of Registration or a Civil Registry certificate.", 46f, 96f, 520f, 22f, 8.5f);

            float y = 132f;
            Action<string, string> row = (label, col) =>
            {
                stat(label, 40f, y, 190f, 12f, 9F, true);
                field(col, 236f, y, 336f, 12f, 9.5F, false);
                y += 22f;
            };
            row("Transaction / Reference No.", "reference_no");
            row("Registrant Name", "registrant_name");
            row("Registration Type", "registration_type");
            row("Date Submitted", "submitted_date");
            row("Status", "status_text");
            rule(40f, y + 2f, 532f); y += 16f;

            stat("Documents Received:", 40f, y, 200f, 12f, 9F, true); y += 18f;
            field("documents_received", 46f, y, 520f, 80f, 9F, false); y += 88f;
            rule(40f, y, 532f); y += 14f;

            row("Received By", "receiving_staff");
            y += 24f;
            stat("_______________________________", 40f, y, 260f, 12f, 9.5F, false); y += 16f;
            stat("Signature over printed name", 40f, y, 260f, 10f, 8F, false); y += 26f;

            box(40f, y, 532f, 30f);
            noteBold("This document acknowledges receipt of transaction only. It is NOT a Certificate " +
                     "of Registration or a Civil Registry certificate. Present this slip when following " +
                     "up on this submission.", 46f, y + 6f, 520f, 22f, 8f);
            y += 42f;

            field("printed_line", 40f, y, 400f, 11f, 7.5F, false);

            return c;
        }

        // ================================================================ data

        /// <summary>Builds the editable, printable row from values already computed by the
        /// calling module (Birth/Marriage/Death) — this template never queries the database
        /// itself, since a slip is printed at the moment of submission, from what the form
        /// already knows, not from a saved report view.</summary>
        public static DataTable BuildTable(string referenceNo, string registrantName,
            string registrationType, DateTime submittedDate, string statusText,
            IEnumerable<string> documentsReceived, string receivingStaff)
        {
            var t = new DataTable(FormCode.Replace('-', '_').ToLowerInvariant());
            foreach (AckSlipCell cell in Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow r = t.NewRow();
            foreach (DataColumn col in t.Columns) r[col] = "";

            OfficeProfile office = OfficeAssets.Profile;
            r["office_header_line"] = (string.IsNullOrWhiteSpace(office.MunicipalityForPrint) ? "" : office.MunicipalityForPrint.ToUpperInvariant() + ", ") +
                                       office.ProvinceForPrint + " — " + office.OfficeName;
            r["reference_no"] = referenceNo ?? "";
            r["registrant_name"] = registrantName ?? "";
            r["registration_type"] = registrationType ?? "";
            r["submitted_date"] = submittedDate.ToString("dd MMMM yyyy, h:mm tt");
            r["status_text"] = statusText ?? "";

            string docs = documentsReceived == null ? "" :
                string.Join("\n", documentsReceived.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => "•  " + d));
            r["documents_received"] = string.IsNullOrEmpty(docs) ? "—  none listed" : docs;

            r["receiving_staff"] = receivingStaff ?? "";
            r["printed_line"] = "Printed " + DateTime.Now.ToString("ddd, dd MMM yyyy  h:mm tt") + " · CROMS";

            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        // ================================================================ draw

        public static System.Drawing.Printing.PrintDocument BuiltInDocument(DataTable t)
        {
            var doc = new System.Drawing.Printing.PrintDocument { DocumentName = FormName };
            try { doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Custom",
                (int)(PageWidth / 72f * 100f), (int)(PageHeight / 72f * 100f)); } catch { }
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
            using (var notePen = new Pen(Color.FromArgb(150, 20, 20), 1.2f))
            using (var noteBrush = new SolidBrush(Color.FromArgb(150, 20, 20)))
            {
                foreach (AckSlipCell c in Cells)
                {
                    if (c.Kind == "Rule")
                    {
                        using (var pen = new Pen(Color.Black, 0.75f))
                            g.DrawLine(pen, c.X, c.Top, c.X + c.Width, c.Top);
                        continue;
                    }
                    if (c.Kind == "Box")
                    {
                        g.DrawRectangle(notePen, c.X, c.Top, c.Width, c.Height);
                        continue;
                    }
                    if (c.Kind == "Static")
                    {
                        FontStyle style = (c.Bold ? FontStyle.Bold : FontStyle.Regular) | (c.Italic ? FontStyle.Italic : FontStyle.Regular);
                        Brush brush = c.Warn ? noteBrush : Brushes.Black;
                        using (var f = new Font("Arial", c.FontSize, style))
                            g.DrawString(c.Text, f, brush, c.Rect, c.Center ? center : left);
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

        /// <summary>Preview/print. The saved template (editable in Certificate Templates) is
        /// the primary layout, exactly like every other form in <see cref="TemplateStore"/>;
        /// the direct-draw renderer above is only the emergency fallback used when no template
        /// row exists yet or rendering it throws — a broken template must never leave the
        /// operator with no slip to hand the client.</summary>
        public static void Show(DataTable t, System.Windows.Forms.IWin32Window owner)
        {
            if (TemplateReportBridge.TryShow(FormCode, FormName, t, owner)) return;

            using (var doc = BuiltInDocument(t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, FormName, null))
                f.ShowDialog(owner);
        }
    }
}
