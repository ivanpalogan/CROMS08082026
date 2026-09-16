using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// Makes the operator's saved <see cref="CertTemplate"/> the PRIMARY print layout for a
    /// form registered in <see cref="TemplateStore.KnownForms"/> (A1/A3 today) — the same
    /// renderer (<see cref="TemplateRenderer"/>) the designer's own canvas and Preview use,
    /// so what was approved in the editor is exactly what prints, never a stale hardcoded
    /// layout the operator has no way to see or change.
    /// <para/>
    /// Crystal Reports and the form's own hardcoded direct-draw layout stay as an EMERGENCY
    /// fallback only — used when there is no active template row (shouldn't happen once a
    /// form has been opened once in the designer, since that seeds one) or rendering it
    /// throws for any reason. A broken template must never leave the operator with nothing
    /// to hand the client, so every failure here is swallowed and reported as "no template
    /// available" rather than surfaced as an error.
    /// </summary>
    public static class TemplateReportBridge
    {
        /// <summary>Tries to preview/print using the form's saved template. Returns false
        /// (having shown nothing) when the caller should fall back to its own legacy path.</summary>
        public static bool TryShow(string formCode, string formName, DataTable t, IWin32Window owner)
        {
            CertTemplate tmpl;
            try { tmpl = TemplateStore.GetActive(formCode); }
            catch { return false; }
            if (tmpl == null || tmpl.Elements == null || tmpl.Elements.Count == 0) return false;

            try
            {
                IDictionary<string, string> values = ToValues(t);
                // The preview offers "Edit Layout..." only for a form the designer knows, and
                // rebuilds THIS record's page from the saved template when the designer closes.
                TemplateFormInfo info = TemplateStore.FindForm(formCode);
                using (PrintDocument doc = BuildDocument(tmpl, formName, values))
                using (var f = new CROMS.Forms.ZoomPrintPreviewForm(
                           doc, formName, null, info,
                           info == null ? (Func<PrintDocument>)null
                                        : () => BuildDocument(TemplateStore.GetActive(formCode) ?? tmpl, formName, values)))
                    f.ShowDialog(owner);
                return true;
            }
            catch
            {
                // A template that fails to render (a bad image id, a corrupt row, ...) falls
                // back to the legacy renderer rather than leaving the operator with an error
                // and no certificate.
                return false;
            }
        }

        private static IDictionary<string, string> ToValues(DataTable t)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (t != null && t.Rows.Count > 0)
            {
                DataRow r = t.Rows[0];
                foreach (DataColumn col in t.Columns)
                    values[col.ColumnName] = r[col] as string ?? "";
            }
            return values;
        }

        private static PrintDocument BuildDocument(CertTemplate tmpl, string formName, IDictionary<string, string> values)
        {
            var doc = new PrintDocument { DocumentName = formName };
            try
            {
                int wHundredths = (int)Math.Round(tmpl.PageWidth / 72f * 100f);
                int hHundredths = (int)Math.Round(tmpl.PageHeight / 72f * 100f);
                doc.DefaultPageSettings.PaperSize = new PaperSize("Custom", wHundredths, hHundredths);
            }
            catch { /* keep the printer's default size rather than fail the print */ }
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            doc.DefaultPageSettings.Landscape = string.Equals(tmpl.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase);
            var imageCache = new Dictionary<int, Image>();
            Image LoadImage(int id)
            {
                if (imageCache.TryGetValue(id, out Image cached)) return cached;
                byte[] bytes = TemplateStore.LoadImage(id);
                Image img = bytes != null ? Image.FromStream(new System.IO.MemoryStream(bytes)) : null;
                imageCache[id] = img;
                return img;
            }

            doc.PrintPage += (s, e) =>
            {
                e.Graphics.PageUnit = GraphicsUnit.Point;
                if (!doc.PrintController.IsPreview)
                    e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX * 0.72f, -e.PageSettings.HardMarginY * 0.72f);
                TemplateRenderer.Draw(e.Graphics, tmpl, values, false, null, LoadImage);
                e.HasMorePages = false;
            };
            return doc;
        }
    }
}
