using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Lines a form's printed values up with the office's own pre-printed stock.
    /// <para/>
    /// A form CROMS has no blank scan of is printed straight onto the official sheet, and its
    /// positions come from the OCR layout — measured against a reference scan, not against
    /// the paper in the tray. The two framings differ by one scale and one offset, the same
    /// for every field on the page, so a single correction per form lines the whole thing up.
    /// Printers add their own share of it, which is why this is kept per machine.
    /// <para/>
    /// The alignment sheet is the point of this screen: it prints a cross at every position a
    /// value will occupy, with the field's name beside it. Run it on the real form and the
    /// drift is visible directly — no guessing from a printed certificate that may be
    /// misaligned for a different reason.
    /// </summary>
    public partial class FormAlignForm : Form
    {
        public FormAlignForm()
        {
            InitializeComponent();
        }

        // ---- data ----------------------------------------------------------

        private List<FormDefinition> _defs = new List<FormDefinition>();

        private void LoadForms()
        {
            // Only forms that print BY POSITION onto paper CROMS cannot draw. A form with a
            // blank scan draws its own boxes, so there is nothing to line up against.
            _defs = FormCatalog.All.Where(d => d.HasOverlay && !d.HasBlankForm).ToList();

            _form.Items.Clear();
            foreach (FormDefinition d in _defs)
                _form.Items.Add(d.FormName + "  —  Form No. " + d.MunicipalFormNo +
                                ", " + d.Revision);

            if (_defs.Count == 0)
            {
                _form.Enabled = false;
                _state.Text = "Every form on file either draws its own blank sheet or has no " +
                              "print positions, so there is nothing to align.";
                return;
            }
            _form.SelectedIndex = 0;
        }

        private FormDefinition Current =>
            _form.SelectedIndex >= 0 && _form.SelectedIndex < _defs.Count
                ? _defs[_form.SelectedIndex] : null;

        private void LoadAlign()
        {
            FormDefinition d = Current;
            if (d == null) return;

            PrintAlign a = PrintCalibration.For(d.FormCode);
            _offX.Value = Clamp(_offX, (decimal)a.OffsetX);
            _offY.Value = Clamp(_offY, (decimal)a.OffsetY);
            _sclX.Value = Clamp(_sclX, (decimal)a.ScaleX);
            _sclY.Value = Clamp(_sclY, (decimal)a.ScaleY);

            _state.Text = (PrintCalibration.IsCalibrated(d.FormCode)
                              ? "Aligned on this PC."
                              : "Not aligned on this PC yet — printing uses the layout as measured.")
                          + "  " + d.Cells.Count + " value positions, page "
                          + d.PrintPage.Width + " x " + d.PrintPage.Height + " points.";
        }

        private PrintAlign Edited()
        {
            return new PrintAlign
            {
                OffsetX = (float)_offX.Value,
                OffsetY = (float)_offY.Value,
                ScaleX = (float)_sclX.Value,
                ScaleY = (float)_sclY.Value,
            };
        }

        private void SaveAlign()
        {
            FormDefinition d = Current;
            if (d == null) return;
            PrintCalibration.Save(d.FormCode, Edited());
            Audit.Write(Audit.Update, "print_align", 0,
                "Aligned " + d.FormCode + " on " + Environment.MachineName);
            LoadAlign();
            MessageBox.Show(this, "Saved for this PC.", d.FormName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetAlign()
        {
            FormDefinition d = Current;
            if (d == null) return;
            PrintCalibration.Clear(d.FormCode);
            LoadAlign();
        }

        // ---- the alignment sheet -------------------------------------------

        /// <summary>
        /// One cross per value position, labelled. Printed on the real form it shows exactly
        /// where each value will land, which a printed certificate cannot: a blank field
        /// leaves no mark, so the fields most likely to be wrong are the ones you cannot see.
        /// </summary>
        private void PrintSheet()
        {
            FormDefinition d = Current;
            if (d == null) return;

            PrintAlign align = Edited();
            float W = d.PrintPage.Width, H = d.PrintPage.Height;

            var pd = new PrintDocument { DocumentName = d.FormName + " alignment" };
            try
            {
                pd.DefaultPageSettings.PaperSize =
                    new PaperSize("Form " + d.MunicipalFormNo,
                                  (int)(W / 72f * 100f), (int)(H / 72f * 100f));
            }
            catch { /* driver refused a custom size: its default is close enough to judge by */ }

            pd.PrintPage += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.PageUnit = GraphicsUnit.Point;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var pen = new Pen(Color.Black, 0.6f))
                using (var ink = new SolidBrush(Color.Black))
                using (var f = new Font("Arial", 5.5f))
                {
                    foreach (PrintCell c in d.Cells)
                    {
                        PointF p = align.Apply(c.At, W, H);
                        g.DrawLine(pen, p.X - 4, p.Y, p.X + 4, p.Y);
                        g.DrawLine(pen, p.X, p.Y - 4, p.X, p.Y + 4);
                        g.DrawString(Label(d, c.Column), f, ink, p.X + 5, p.Y - 3);
                    }
                }
                e.HasMorePages = false;
            };

            using (var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 900,
                Height = 950,
                StartPosition = FormStartPosition.CenterParent,
                Text = d.FormName + " — alignment sheet"
            })
            {
                preview.ShowDialog(this);
            }
        }

        private static string Label(FormDefinition d, string column)
        {
            foreach (ReportSection s in d.ReportSections)
                foreach (ReportField f in s.Fields)
                    if (string.Equals(f.Column, column, StringComparison.OrdinalIgnoreCase))
                        return f.Label;
            return column;
        }

        private static decimal Clamp(NumericUpDown n, decimal v) =>
            v < n.Minimum ? n.Minimum : v > n.Maximum ? n.Maximum : v;

        // The dialog is not busy enough to warrant a Designer file, and every control here
        // is placed by a literal — the same call the other small admin dialogs make.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiTheme.Polish(this);
        }
    }
}
