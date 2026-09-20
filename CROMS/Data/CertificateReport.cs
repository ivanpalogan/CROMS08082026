using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>How a certificate was actually rendered, so the caller can say so.</summary>
    public enum ReportEngine
    {
        /// <summary>A Crystal .rpt was found and rendered by the Crystal engine.</summary>
        Crystal,
        /// <summary>Drawn over a scan of the blank form — a visual replica of the paper.</summary>
        Overlay,
        /// <summary>Drawn from the form's own sections and labels, in printed order.</summary>
        Structured
    }

    /// <summary>
    /// The one way a certificate gets printed, previewed or exported, for every form the
    /// office handles.
    /// <para/>
    /// It resolves the form from <see cref="FormCatalog"/>, pulls the record's single flat
    /// row from that form's report view, and renders it by the best means available:
    /// <list type="number">
    /// <item><b>Crystal Reports</b> — when a <c>.rpt</c> exists for this form under the
    ///   app's <c>Reports</c> folder and the Crystal runtime is installed on the machine.
    ///   The report is bound to the same view row, and the logo and stamp are pushed in as
    ///   report parameters/objects rather than being baked into the layout.</item>
    /// <item><b>Overlay</b> — a scan of the BLANK form drawn as the page background with
    ///   each value placed at its measured coordinate. A true visual replica.</item>
    /// <item><b>Structured</b> — the form's own sections, labels and printed order, drawn
    ///   as a clean document with the office header, logo and stamp.</item>
    /// </list>
    /// Nothing here is specific to a certificate type. Adding a form is a
    /// <see cref="FormDefinition"/> entry plus, optionally, a <c>.rpt</c> and a blank-form
    /// scan; no code in this file changes.
    /// </summary>
    public static class CertificateReport
    {
        /// <summary>Where .rpt files live, beside the executable.</summary>
        public static string ReportsFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

        /// <summary>The .rpt for this form, or null when the office has not authored one.</summary>
        public static string ReportPath(FormDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.RptFile)) return null;
            string p = Path.Combine(ReportsFolder, def.RptFile);
            return File.Exists(p) ? p : null;
        }

        /// <summary>
        /// Whether the Crystal Reports runtime is installed on THIS machine. Probed once
        /// and cached. A PC without it still prints — the overlay and structured renderers
        /// need nothing but GDI+.
        /// </summary>
        public static bool CrystalAvailable
        {
            get
            {
                if (_crystal.HasValue) return _crystal.Value;
                try
                {
                    // Load by name rather than touching a Crystal type here: this property
                    // must be safe to JIT on a machine where the assemblies are absent.
                    System.Reflection.Assembly.Load(
                        "CrystalDecisions.CrystalReports.Engine, Version=13.0.4000.0, " +
                        "Culture=neutral, PublicKeyToken=692fbea5521e1304");
                    System.Reflection.Assembly.Load(
                        "CrystalDecisions.Windows.Forms, Version=13.0.4000.0, " +
                        "Culture=neutral, PublicKeyToken=692fbea5521e1304");
                    _crystal = true;
                }
                catch { _crystal = false; }
                return _crystal.Value;
            }
        }
        private static bool? _crystal;

        // ===================================================================
        // Entry points
        // ===================================================================

        /// <summary>
        /// Show the certificate for a record whose form identity is already known.
        /// Returns how it was rendered, or null if it could not be shown.
        /// </summary>
        public static ReportEngine? Show(string formCode, long recordId, IWin32Window owner,
                                         bool applyStamp = true)
        {
            FormDefinition def = FormCatalog.ByCode(formCode);
            if (def == null)
            {
                MessageBox.Show(
                    "This record's form (" + (formCode ?? "none recorded") + ") is not in the " +
                    "form library, so CROMS does not know how to lay it out.\n\n" +
                    "Add it to FormCatalog, or set the record's Form Name in its " +
                    "registration screen.",
                    "Unknown form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return Render(def, recordId, owner, applyStamp);
        }

        /// <summary>
        /// Show the certificate for a record identified only by its table and id: the form
        /// identity is read off the row itself, falling back to the revision the office
        /// issues today when the row predates form identity being stored.
        /// </summary>
        public static ReportEngine? ShowFor(DocKind kind, long recordId, IWin32Window owner,
                                            bool applyStamp = true)
        {
            FormDefinition fallback = FormCatalog.Current(kind);
            if (fallback == null) return null;

            string code = null;
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT form_code FROM `" + Safe(fallback.RecordTable) + "` WHERE id = @id",
                    new MySqlParameter("@id", recordId));
                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                    code = dt.Rows[0][0].ToString();
            }
            catch (MySqlException) { /* pre-migration-26 row: use the current revision */ }

            FormDefinition def = FormCatalog.ByCode(code) ?? fallback;
            return Render(def, recordId, owner, applyStamp);
        }

        // ===================================================================
        // Rendering
        // ===================================================================

        private static ReportEngine? Render(FormDefinition def, long recordId,
                                            IWin32Window owner, bool applyStamp)
        {
            DataTable data = LoadRow(def, recordId);
            if (data == null || data.Rows.Count == 0)
            {
                MessageBox.Show("Record not found, or it has not been saved yet.",
                    def.FormName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string rpt = ReportPath(def);
            FormDefinition layout = def;
            if (rpt == null && !def.HasBlankForm)
            {
                // This revision has no blank-form image, so it cannot be drawn as a replica
                // and has no report of its own. Print it through the Crystal report of the
                // birth / marriage / death certificate the office issues today instead of
                // dropping to the plain listing - the record's values are the same view
                // columns whichever revision it was registered on.
                FormDefinition cur = FormCatalog.Current(def.FormType);
                string curRpt = cur != null && !ReferenceEquals(cur, def) && cur.HasBlankForm
                    ? ReportPath(cur) : null;
                if (curRpt != null) { rpt = curRpt; layout = cur; }
            }
            if (rpt != null && CrystalAvailable)
            {
                // Isolated in its own type so this method stays JIT-safe on a machine with
                // no Crystal runtime — nothing in this file's signatures is a Crystal type.
                // Crystal takes the branding as raw bytes (a Blob field bound to a byte[]
                // column); the built-in renderers below take decoded Images.
                try
                {
                    CrystalRunner.Show(rpt, def, data,
                        OfficeAssets.GetBytes(AssetKind.Logo, def.FormCode),
                        applyStamp ? OfficeAssets.GetBytes(AssetKind.Stamp, def.FormCode) : null,
                        owner, layout);
                    return ReportEngine.Crystal;
                }
                catch (Exception ex)
                {
                    // A broken or mismatched .rpt must not leave the operator with no
                    // certificate at all: say what happened, then print it ourselves.
                    MessageBox.Show(
                        "The Crystal report for this form could not be opened, so CROMS " +
                        "printed the certificate itself instead.\n\n" + ex.Message,
                        "Crystal report unavailable", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            Image logo = OfficeAssets.Get(AssetKind.Logo, def.FormCode);
            Image stamp = applyStamp ? OfficeAssets.Get(AssetKind.Stamp, def.FormCode) : null;
            return PrintBuiltIn(def, data.Rows[0], logo, stamp, owner);
        }

        /// <summary>
        /// Which of the two built-in renderings to use.
        /// <para/>
        /// A form with a blank scan on file prints as a replica with no question asked - the
        /// page carries its own artwork, so it reads correctly on any paper.
        /// <para/>
        /// A form that knows its positions but has NO blank scan is the interesting case. Its
        /// values land in the right boxes, but only if they are printed onto the office's
        /// official pre-printed sheet; on plain paper the same page is text floating with no
        /// labels. That is a real choice about what is in the printer, and it is the
        /// operator's to make, so it is asked rather than assumed - silently switching those
        /// forms to positioned output would take away the readable listing they print today.
        /// </summary>
        private static bool ChooseOverlay(FormDefinition def, IWin32Window owner,
                                          bool allowPrePrinted)
        {
            if (!def.HasOverlay) return false;
            if (def.HasBlankForm && BlankForm(def) != null) return true;

            // Position-only output is meaningful on ONE surface: the official pre-printed
            // sheet. On screen it is values floating with no labels, so a caller that is not
            // printing onto that stock — a review preview — takes the labelled listing and is
            // never asked a question about the printer that does not apply to it.
            if (!allowPrePrinted) return false;

            DialogResult r = MessageBox.Show(owner,
                "Is the official pre-printed " + def.FormName + " form (Municipal Form No. " +
                def.MunicipalFormNo + ") loaded in the printer?\n\n" +
                "YES  -  print only the values, positioned to land in the form's own boxes.\n" +
                "NO   -  print the full certificate as a labelled listing on plain paper.\n\n" +
                "CROMS has no scan of this blank form on file, so it cannot draw the form " +
                "itself. Supplying one makes this question go away.",
                "How should this print?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            return r == DialogResult.Yes;
        }

        /// <summary>
        /// Paints the page as provisional: a solid banner across the top and a repeating
        /// diagonal wash of the same words. Drawn OVER the finished content so no value can
        /// hide it, and on every page.
        /// </summary>
        private static void DrawWatermark(PrintPageEventArgs e, string text)
        {
            Graphics g = e.Graphics;
            Rectangle page = e.PageBounds;
            GraphicsState saved = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // The banner is opaque on purpose. A light diagonal wash can disappear entirely
            // through a photocopier, which is exactly how an unmarked page gets into a file.
            using (var band = new SolidBrush(Color.FromArgb(198, 50, 63)))
            using (var f = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var w = new SolidBrush(Color.White))
            {
                var bar = new Rectangle(page.Left, page.Top, page.Width, 34);
                g.FillRectangle(band, bar);
                g.DrawString(text, f, w, bar,
                    new StringFormat { Alignment = StringAlignment.Center,
                                       LineAlignment = StringAlignment.Center });
            }

            using (var f = new Font("Segoe UI", 34F, FontStyle.Bold))
            using (var ink = new SolidBrush(Color.FromArgb(38, 198, 50, 63)))
            {
                g.TranslateTransform(page.Left + page.Width / 2f, page.Top + page.Height / 2f);
                g.RotateTransform(-32f);
                SizeF m = g.MeasureString(text, f);
                float stepY = m.Height * 2.4f;
                float reach = (page.Width + page.Height) / 1.4f;
                for (float y = -reach; y < reach; y += stepY)
                    for (float x = -reach; x < reach; x += m.Width + 70f)
                        g.DrawString(text, f, ink, x, y);
            }

            g.Restore(saved);
        }

        /// <summary>
        /// Show the operator the reviewed OCR values laid out on the certificate they came
        /// off, before anything is written to the registry. Seeing values in their real
        /// boxes catches a class of error the review grid cannot show - a value read into
        /// the wrong row, or a whole block the scan never filled - because the page can be
        /// compared against the paper in the operator's hand.
        /// <para/>
        /// Three constraints are what make this safe to put in front of a front-desk clerk:
        /// every page is watermarked as an unsaved preview, the office STAMP is never
        /// applied, and it is drawn by CROMS's own printer even when a Crystal .rpt exists
        /// for the form - an authored .rpt has no watermark field, so routing a preview
        /// through Crystal would produce a page indistinguishable from an issued
        /// certificate. Nothing here reads or writes the registry.
        /// </summary>
        public static ReportEngine? ShowOcrPreview(FormDefinition def,
                                                   IDictionary<string, string> values,
                                                   IWin32Window owner)
        {
            if (def == null || values == null) return null;

            DataRow row = BuildOcrPreviewRow(def, values);
            Image logo = OfficeAssets.Get(AssetKind.Logo, def.FormCode);

            // allowPrePrinted: false — the operator is comparing this against the scan in
            // front of them, not feeding official stock through a printer. A form whose
            // artwork CROMS can actually draw still previews as the replica; one whose
            // positions exist only for pre-printed paper previews as the labelled listing,
            // because unlabelled floating values cannot be checked against anything.
            return PrintBuiltIn(def, row, logo, null, owner,
                                "UNSAVED OCR PREVIEW — NOT A CERTIFICATE",
                                allowPrePrinted: false);
        }

        /// <summary>
        /// Reviewed extraction values shaped into the one flat row the printers read, so a
        /// preview and a saved record go through identical drawing code. A key the form does
        /// not print is left out; nothing is invented to fill a column.
        /// </summary>
        private static DataRow BuildOcrPreviewRow(FormDefinition def,
                                                  IDictionary<string, string> values)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ReportSection section in def.ReportSections)
                foreach (ReportField field in section.Fields) columns.Add(field.Column);

            var mapped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
            {
                string column = FormCatalog.ViewColumn(def, pair.Key);
                if (column == null) continue;
                columns.Add(column);
                mapped[column] = pair.Value ?? "";
            }

            var table = new DataTable(def.ReportView);
            foreach (string column in columns) table.Columns.Add(column, typeof(string));

            DataRow row = table.NewRow();
            foreach (var pair in mapped) row[pair.Key] = pair.Value;

            // Office identity is not on the scan, so it comes from the office profile - but
            // only where the scan did not already supply it.
            OfficeProfile office = OfficeAssets.Profile;
            FillIfBlank(row, "office_province", office.Province);
            FillIfBlank(row, "office_municipality", office.Municipality);
            FillIfBlank(row, "form_name", def.FormName);
            table.Rows.Add(row);
            return row;
        }

        private static void FillIfBlank(DataRow row, string column, string value)
        {
            if (!row.Table.Columns.Contains(column)) return;
            if (!string.IsNullOrWhiteSpace(row[column] as string)) return;
            row[column] = value ?? "";
        }

        /// <summary>
        /// The record as one flat row of its form's report view — the same shape a Crystal
        /// report binds to, so both renderers read identical values.
        /// </summary>
        public static DataTable LoadRow(FormDefinition def, long recordId)
        {
            if (def == null) return null;
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT * FROM `" + Safe(def.ReportView) + "` WHERE record_id = @id",
                    new MySqlParameter("@id", recordId));
                dt.TableName = def.ReportView;
                return dt;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(
                    "The report view `" + def.ReportView + "` is missing. Run database " +
                    "migration 26_form_identity.sql, which creates it.\n\n" + ex.Message,
                    "Report view missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Identifiers come from <see cref="FormCatalog"/>, never from user input, so they
        /// cannot carry an injection — but they are still checked here so a future caller
        /// cannot pass a table name through by mistake.
        /// </summary>
        private static string Safe(string identifier)
        {
            if (string.IsNullOrEmpty(identifier) ||
                !identifier.All(c => char.IsLetterOrDigit(c) || c == '_'))
                throw new ArgumentException("Unsafe identifier: " + identifier);
            return identifier;
        }

        // ===================================================================
        // Built-in renderers (no report engine, no per-form code)
        // ===================================================================

        private static ReportEngine PrintBuiltIn(FormDefinition def, DataRow row,
                                                 Image logo, Image stamp, IWin32Window owner,
                                                 string watermark = null,
                                                 bool allowPrePrinted = true)
        {
            bool overlay = ChooseOverlay(def, owner, allowPrePrinted);

            var pd = new PrintDocument { DocumentName = def.FormName };
            pd.DefaultPageSettings.Landscape = false;
            if (overlay)
            {
                // The overlay's coordinates were measured against this page size, so the
                // page must be that size or every value lands off its box.
                try
                {
                    pd.DefaultPageSettings.PaperSize = new PaperSize(
                        "Form " + def.MunicipalFormNo,
                        (int)(def.PrintPage.Width / 72f * 100f),
                        (int)(def.PrintPage.Height / 72f * 100f));
                }
                catch { /* driver refused a custom size: fall back to its default */ }
            }

            var pages = overlay ? null : BuildStructuredPages(def, row);
            int pageIndex = 0;

            pd.PrintPage += (s, e) =>
            {
                if (overlay)
                {
                    DrawOverlay(e, def, row, stamp);
                    e.HasMorePages = false;
                }
                else
                {
                    DrawStructuredPage(e, def, row, logo, stamp, pages, pageIndex,
                                       pages.Count);
                    pageIndex++;
                    e.HasMorePages = pageIndex < pages.Count;
                }
                // LAST, so it lands on top of the certificate rather than under it, and on
                // EVERY page - a marking that only appears on page one is not a marking.
                if (watermark != null) DrawWatermark(e, watermark);
            };
            pd.EndPrint += (s, e) => pageIndex = 0;

            using (var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 980,
                Height = 1000,
                StartPosition = FormStartPosition.CenterParent,
                Text = watermark != null
                    ? watermark + "  —  " + def.FormName + " (not a certificate)"
                    : def.FormName + "  —  Municipal Form No. " + def.MunicipalFormNo
            })
            {
                preview.ShowDialog(owner);
            }

            return overlay ? ReportEngine.Overlay : ReportEngine.Structured;
        }

        // ---- shared value access --------------------------------------------------

        /// <summary>One value from the report row, formatted the way the form prints it.</summary>
        private static string Value(DataRow r, string column, bool asDate = false)
        {
            if (column == null || !r.Table.Columns.Contains(column)) return "";
            object v = r[column];
            if (v == DBNull.Value || v == null) return "";

            if (asDate || v is DateTime)
            {
                DateTime d;
                if (v is DateTime dt) d = dt;
                else if (!DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out d))
                    return v.ToString().Trim();
                // A DATE column with no time reads as a date; a real timestamp keeps it.
                return d.TimeOfDay == TimeSpan.Zero
                    ? d.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)
                    : d.ToString("dd MMMM yyyy h:mm tt", CultureInfo.InvariantCulture);
            }
            return v.ToString().Trim();
        }

        /// <summary>The part of a combined value that belongs in one printed box.</summary>
        private static string Part(string value, int part)
        {
            if (part < 0) return value;
            string[] bits = value.Split(',');
            return part < bits.Length ? bits[part].Trim() : "";
        }

        // ---- print table: what a generated Crystal report binds to -----------------

        /// <summary>Name of the one-row table a generated registry-certificate .rpt binds to.
        /// CrystalRunner recognises a report by it.</summary>
        public const string PrintTableName = "cert_print";
        public const string StampColumn = "stamp_image";

        /// <summary>One box on the generated report: where a value (or a tick, or the stamp)
        /// is placed, in points from the top-left of the page.</summary>
        public class PrintBox
        {
            public string Column;
            public float X, Y, Width, Height, FontSize;
            public bool Bold, IsStamp;
        }

        /// <summary>
        /// The boxes of a form's report, read from the SAME print map (Cells / Marks /
        /// StampRect) the built-in overlay draws from, so the Crystal report and the overlay
        /// cannot place a value in different spots. Column names are positional (c000, k000)
        /// because one stored column can feed several boxes (place of birth feeds three).
        /// </summary>
        public static List<PrintBox> PrintBoxes(FormDefinition def)
        {
            var list = new List<PrintBox>();
            float W = def.PrintPage.Width, H = def.PrintPage.Height;
            for (int i = 0; i < def.Cells.Count; i++)
            {
                PrintCell c = def.Cells[i];
                float x = c.At.X * W, y = c.At.Y * H;
                list.Add(new PrintBox
                {
                    Column = "c" + i.ToString("000"),
                    X = x, Y = y,
                    // Wide on purpose: a box on paper cannot grow, so it only clips at the
                    // page edge, exactly like the overlay's DrawString.
                    Width = Math.Max(20f, W - x - 6f),
                    Height = c.FontSize * 1.6f,
                    FontSize = c.FontSize
                });
            }
            for (int i = 0; i < def.Marks.Count; i++)
            {
                PrintMark m = def.Marks[i];
                list.Add(new PrintBox
                {
                    Column = "k" + i.ToString("000"),
                    X = m.At.X * W, Y = m.At.Y * H, Width = 16f, Height = 15f,
                    FontSize = 9f, Bold = true
                });
            }
            if (!def.StampRect.IsEmpty)
                list.Add(new PrintBox
                {
                    Column = StampColumn, IsStamp = true,
                    X = def.StampRect.X * W, Y = def.StampRect.Y * H,
                    Width = def.StampRect.Width * W, Height = def.StampRect.Height * H
                });
            return list;
        }

        /// <summary>
        /// The one-row table a generated report binds to: every printed box already resolved
        /// to its final text (dates formatted, place split into its boxes, a tick box as "X"
        /// or blank), so the report only PLACES text and holds no logic of its own. Pass a
        /// null row for the empty schema the generator builds the .rpt from.
        /// </summary>
        public static DataTable BuildPrintTable(FormDefinition def, DataRow row, byte[] stamp = null)
        {
            var t = new DataTable(PrintTableName);
            foreach (PrintBox b in PrintBoxes(def))
                t.Columns.Add(b.Column, b.IsStamp ? typeof(byte[]) : typeof(string));

            DataRow r = t.NewRow();
            foreach (DataColumn c in t.Columns)
                if (c.DataType == typeof(string)) r[c] = "";

            if (row != null)
            {
                for (int i = 0; i < def.Cells.Count; i++)
                {
                    PrintCell c = def.Cells[i];
                    r["c" + i.ToString("000")] = Part(Value(row, c.Column, c.IsDate), c.Part);
                }
                // Same "which option matched" rule as the overlay: the catch-all "*" only
                // fires when the value matched none of that column's listed options.
                foreach (var byColumn in def.Marks.GroupBy(m => m.Column,
                                                           StringComparer.OrdinalIgnoreCase))
                {
                    string v = Value(row, byColumn.Key);
                    if (v.Length == 0) continue;
                    PrintMark hit = byColumn.FirstOrDefault(m => m.WhenValue != "*" &&
                        v.StartsWith(m.WhenValue, StringComparison.OrdinalIgnoreCase))
                        ?? byColumn.FirstOrDefault(m => m.WhenValue == "*");
                    if (hit != null) r["k" + def.Marks.IndexOf(hit).ToString("000")] = "X";
                }
            }
            if (t.Columns.Contains(StampColumn))
                r[StampColumn] = (object)stamp ?? DBNull.Value;
            t.Rows.Add(r);
            t.AcceptChanges();
            return t;
        }

        /// <summary>Full path of a form's blank-sheet image, or null if it is not on file.</summary>
        public static string BlankPath(FormDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.BlankAsset)) return null;
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", def.BlankAsset);
            return File.Exists(p) ? p : null;
        }

        // ---- overlay: a visual replica of the paper form --------------------------

        private static readonly Dictionary<string, Image> _blanks =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The scan of this form's blank sheet, cached for the process.</summary>
        private static Image BlankForm(FormDefinition def)
        {
            if (string.IsNullOrEmpty(def.BlankAsset)) return null;
            if (_blanks.TryGetValue(def.BlankAsset, out Image cached)) return cached;

            Image img = null;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", def.BlankAsset);
            if (File.Exists(path))
            {
                try { img = Image.FromFile(path); } catch { img = null; }
            }
            _blanks[def.BlankAsset] = img;
            return img;
        }

        private static void DrawOverlay(PrintPageEventArgs e, FormDefinition def,
                                        DataRow row, Image stamp)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Point;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float W = def.PrintPage.Width, H = def.PrintPage.Height;

            Image bg = BlankForm(def);
            if (bg != null) g.DrawImage(bg, 0, 0, W, H);

            // Printing onto pre-printed stock, the map's own framing is not the sheet's.
            // One correction per form moves the whole page together; on a form that draws
            // its own artwork it is identity, because there is nothing to line up against.
            PrintAlign align = bg != null
                ? new PrintAlign() : PrintCalibration.For(def.FormCode);

            using (var brush = new SolidBrush(Color.Black))
            {
                foreach (PrintCell c in def.Cells)
                {
                    string text = Part(Value(row, c.Column, c.IsDate), c.Part);
                    if (text.Length == 0) continue;
                    PointF p = align.Apply(c.At, W, H);
                    using (var f = new Font("Arial", c.FontSize))
                        g.DrawString(text, f, brush, p.X, p.Y);
                }

                using (var fMark = new Font("Arial", 9f, FontStyle.Bold))
                {
                    // Group the marks by column so the catch-all "*" only fires when the
                    // value matched none of that column's listed options.
                    foreach (var byColumn in def.Marks.GroupBy(m => m.Column,
                                                               StringComparer.OrdinalIgnoreCase))
                    {
                        string v = Value(row, byColumn.Key);
                        if (v.Length == 0) continue;

                        PrintMark hit = byColumn.FirstOrDefault(m =>
                            m.WhenValue != "*" &&
                            v.StartsWith(m.WhenValue, StringComparison.OrdinalIgnoreCase));
                        if (hit == null)
                            hit = byColumn.FirstOrDefault(m => m.WhenValue == "*");
                        if (hit == null) continue;

                        PointF mp = align.Apply(hit.At, W, H);
                        g.DrawString("X", fMark, brush, mp.X, mp.Y);
                    }
                }
            }

            DrawStamp(g, stamp, def.StampRect, W, H);
        }

        /// <summary>
        /// The office stamp, drawn semi-transparently at the position this form reserves
        /// for it so it reads as applied over the document rather than covering the values
        /// underneath. Kept in its own method so the overlay and structured renderers
        /// place it identically.
        /// </summary>
        private static void DrawStamp(Graphics g, Image stamp, RectangleF rect,
                                      float pageW, float pageH)
        {
            if (stamp == null || rect.IsEmpty) return;

            var target = new RectangleF(rect.X * pageW, rect.Y * pageH,
                                        rect.Width * pageW, rect.Height * pageH);
            // Keep the stamp's own proportions inside the reserved box.
            float scale = Math.Min(target.Width / stamp.Width, target.Height / stamp.Height);
            var fit = new RectangleF(
                target.X + (target.Width - stamp.Width * scale) / 2f,
                target.Y + (target.Height - stamp.Height * scale) / 2f,
                stamp.Width * scale, stamp.Height * scale);

            using (var ia = new System.Drawing.Imaging.ImageAttributes())
            {
                var m = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.82f };
                ia.SetColorMatrix(m);
                g.DrawImage(stamp,
                    new Rectangle((int)fit.X, (int)fit.Y, (int)fit.Width, (int)fit.Height),
                    0, 0, stamp.Width, stamp.Height, GraphicsUnit.Pixel, ia);
            }
        }

        // ---- structured: the form's own sections, in printed order ----------------

        /// <summary>One drawable line of the structured document.</summary>
        private class Line
        {
            public string Label;     // null for a section heading
            public string Text;
            public bool Heading;
        }

        /// <summary>
        /// Turn the record into lines grouped by the form's printed sections, then split
        /// them into pages. A section is never left with its heading alone at the foot of
        /// a page.
        /// </summary>
        private static List<List<Line>> BuildStructuredPages(FormDefinition def, DataRow row)
        {
            var lines = new List<Line>();
            foreach (ReportSection sec in def.ReportSections)
            {
                var body = new List<Line>();
                foreach (ReportField f in sec.Fields)
                {
                    string v = Value(row, f.Column, f.IsDate);
                    // A blank entry is printed as a rule, not skipped: on a certificate an
                    // omitted item and an empty item are different statements, and the
                    // paper form shows the empty box.
                    body.Add(new Line { Label = f.Label, Text = v.Length == 0 ? "—" : v });
                }
                if (body.Count == 0) continue;
                lines.Add(new Line { Text = sec.Title, Heading = true });
                lines.AddRange(body);
            }

            const int firstPageLines = 34;   // the header block takes the rest
            const int laterPageLines = 46;

            var pages = new List<List<Line>>();
            var current = new List<Line>();
            int limit = firstPageLines;

            foreach (Line ln in lines)
            {
                bool orphanHeading = ln.Heading && current.Count >= limit - 2;
                if (current.Count >= limit || orphanHeading)
                {
                    pages.Add(current);
                    current = new List<Line>();
                    limit = laterPageLines;
                }
                current.Add(ln);
            }
            if (current.Count > 0) pages.Add(current);
            if (pages.Count == 0) pages.Add(new List<Line>());
            return pages;
        }

        private static void DrawStructuredPage(PrintPageEventArgs e, FormDefinition def,
                                               DataRow row, Image logo, Image stamp,
                                               List<List<Line>> pages, int index, int total)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Point;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF m = e.MarginBounds;
            float pageW = e.PageBounds.Width, pageH = e.PageBounds.Height;
            float x = m.Left, y = m.Top, w = m.Width;

            using (var fTitle = new Font("Times New Roman", 15f, FontStyle.Bold))
            using (var fSub = new Font("Times New Roman", 9.5f))
            using (var fHead = new Font("Arial", 9f, FontStyle.Bold))
            using (var fLabel = new Font("Arial", 8f))
            using (var fValue = new Font("Arial", 9f, FontStyle.Bold))
            using (var fFoot = new Font("Arial", 7.5f))
            using (var ink = new SolidBrush(Color.Black))
            using (var soft = new SolidBrush(Color.FromArgb(90, 90, 90)))
            using (var rule = new Pen(Color.FromArgb(160, 160, 160), 0.6f))
            using (var heavy = new Pen(Color.Black, 1.1f))
            {
                var centre = new StringFormat { Alignment = StringAlignment.Center };

                if (index == 0)
                {
                    OfficeProfile office = OfficeAssets.Profile;

                    // Logo in the header area this form reserves for it. The office seal
                    // belongs beside the office's own name, not over the data.
                    if (logo != null && !def.LogoRect.IsEmpty)
                    {
                        var lr = new RectangleF(def.LogoRect.X * pageW, def.LogoRect.Y * pageH,
                                                def.LogoRect.Width * pageW,
                                                def.LogoRect.Height * pageH);
                        float s = Math.Min(lr.Width / logo.Width, lr.Height / logo.Height);
                        g.DrawImage(logo, lr.X, lr.Y, logo.Width * s, logo.Height * s);
                    }

                    g.DrawString("Republic of the Philippines", fSub, ink,
                                 new RectangleF(x, y, w, 14), centre); y += 13;
                    g.DrawString(office.OfficeName, fSub, ink,
                                 new RectangleF(x, y, w, 14), centre); y += 13;
                    string place = string.Join(", ", new[] { office.Municipality, office.Province }
                        .Where(t => !string.IsNullOrWhiteSpace(t)));
                    if (place.Length > 0)
                    {
                        g.DrawString(place, fSub, ink,
                                     new RectangleF(x, y, w, 14), centre); y += 16;
                    }

                    // FORM IDENTIFICATION — the certificate says which form it is.
                    g.DrawString(def.FormName.ToUpperInvariant(), fTitle, ink,
                                 new RectangleF(x, y, w, 22), centre); y += 21;
                    g.DrawString("Municipal Form No. " + def.MunicipalFormNo +
                                 "   ·   " + def.Revision +
                                 "   ·   Form Code " + def.FormCode, fFoot, soft,
                                 new RectangleF(x, y, w, 12), centre); y += 16;

                    // Registry number, given the prominence the paper form gives it.
                    string reg = Value(row, "registry_no");
                    string book = Value(row, "book_volume"), pg = Value(row, "book_page");
                    string regLine = "Registry No.: " + (reg.Length == 0 ? "—" : reg);
                    if (book.Length > 0) regLine += "     Book / Volume: " + book;
                    if (pg.Length > 0) regLine += "     Page: " + pg;
                    g.DrawString(regLine, fHead, ink,
                                 new RectangleF(x, y, w, 14), centre); y += 18;

                    g.DrawLine(heavy, x, y, x + w, y); y += 10;
                }
                else
                {
                    g.DrawString(def.FormName + "  —  Registry No. " +
                                 (Value(row, "registry_no").Length == 0
                                     ? "—" : Value(row, "registry_no")),
                                 fFoot, soft, x, y);
                    y += 14;
                    g.DrawLine(rule, x, y, x + w, y); y += 8;
                }

                // ---- the section body ----
                const float labelW = 190f;
                float lineH = 15.5f;

                foreach (Line ln in pages[index])
                {
                    if (ln.Heading)
                    {
                        y += 5;
                        g.DrawString(ln.Text.ToUpperInvariant(), fHead, ink, x, y);
                        y += 13;
                        g.DrawLine(rule, x, y, x + w, y);
                        y += 4;
                        continue;
                    }

                    g.DrawString(ln.Label, fLabel, soft,
                                 new RectangleF(x, y + 1, labelW - 6, lineH));
                    g.DrawString(ln.Text, fValue, ink,
                                 new RectangleF(x + labelW, y, w - labelW, lineH));
                    y += lineH;
                }

                // ---- signature block + stamp, on the last page only ----
                if (index == total - 1)
                {
                    OfficeProfile office = OfficeAssets.Profile;
                    float sigY = Math.Max(y + 30, m.Bottom - 96);

                    g.DrawLine(heavy, x + w - 250, sigY, x + w - 20, sigY);
                    string name = string.IsNullOrWhiteSpace(office.RegistrarName)
                        ? "" : office.RegistrarName.ToUpperInvariant();
                    if (name.Length > 0)
                    {
                        g.DrawString(name, fValue, ink,
                            new RectangleF(x + w - 250, sigY + 2, 230, 14), centre);
                    }
                    g.DrawString(office.RegistrarTitle, fFoot, soft,
                        new RectangleF(x + w - 250, sigY + (name.Length > 0 ? 16 : 3), 230, 12),
                        centre);

                    DrawStamp(g, stamp, def.StampRect, pageW, pageH);
                }

                // ---- footer ----
                string foot = "Page " + (index + 1) + " of " + total +
                              "   ·   " + def.FormName +
                              "   ·   printed " +
                              DateTime.Now.ToString("dd MMM yyyy h:mm tt",
                                                    CultureInfo.InvariantCulture) +
                              "   ·   " + (Session.User?.Username ?? "");
                g.DrawString(foot, fFoot, soft,
                             new RectangleF(x, m.Bottom + 6, w, 12), centre);
            }
        }
    }
}
