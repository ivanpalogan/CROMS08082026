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
    public class FormAlignForm : Form
    {
        private readonly ComboBox _form = new ComboBox();
        private readonly NumericUpDown _offX = Spin(-400, 400, 0.5m);
        private readonly NumericUpDown _offY = Spin(-400, 400, 0.5m);
        private readonly NumericUpDown _sclX = Spin(0.5m, 2m, 0.005m);
        private readonly NumericUpDown _sclY = Spin(0.5m, 2m, 0.005m);
        private readonly Label _state = new Label();

        public FormAlignForm()
        {
            Text = "Align printing on pre-printed forms";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 430);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            Controls.Add(new Label
            {
                Text = "Align printing on pre-printed forms",
                Location = new Point(20, 16), AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Ink
            });
            Controls.Add(new Label
            {
                Text = "Load the blank official form in the printer, print the alignment sheet, " +
                       "then nudge until the crosses sit in the form's boxes.",
                Location = new Point(22, 46), Size = new Size(580, 34),
                Font = new Font("Segoe UI", 8.75F), ForeColor = UiTheme.Muted
            });

            Controls.Add(Cap("Form", 22, 92));
            _form.SetBounds(22, 112, 400, 24);
            _form.DropDownStyle = ComboBoxStyle.DropDownList;
            _form.SelectedIndexChanged += (s, e) => LoadAlign();
            Controls.Add(_form);

            Controls.Add(Cap("Move right / left  (points)", 22, 152));
            _offX.SetBounds(22, 172, 110, 24); Controls.Add(_offX);
            Controls.Add(Cap("Move down / up  (points)", 152, 152));
            _offY.SetBounds(152, 172, 110, 24); Controls.Add(_offY);

            Controls.Add(Cap("Stretch across", 302, 152));
            _sclX.SetBounds(302, 172, 110, 24); _sclX.DecimalPlaces = 3; Controls.Add(_sclX);
            Controls.Add(Cap("Stretch down", 432, 152));
            _sclY.SetBounds(432, 172, 110, 24); _sclY.DecimalPlaces = 3; Controls.Add(_sclY);

            Controls.Add(new Label
            {
                Text = "72 points = 1 inch. Change the offsets first: they move everything " +
                       "together. Only touch the stretch if the values drift further off the " +
                       "further down the page they are.",
                Location = new Point(22, 204), Size = new Size(560, 34),
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Faint
            });

            var btnSheet = Btn("Print alignment sheet", 22, 250, 190, UiTheme.Accent, Color.White);
            btnSheet.Click += (s, e) => PrintSheet();
            Controls.Add(btnSheet);

            var btnSave = Btn("Save", 222, 250, 110, UiTheme.Success, Color.White);
            btnSave.Click += (s, e) => SaveAlign();
            Controls.Add(btnSave);

            var btnReset = Btn("Reset this form", 342, 250, 140, UiTheme.Chrome, UiTheme.Ink);
            btnReset.Click += (s, e) => ResetAlign();
            Controls.Add(btnReset);

            var btnClose = Btn("Close", 492, 250, 100, UiTheme.Chrome, UiTheme.Ink);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            _state.SetBounds(22, 296, 570, 40);
            _state.Font = new Font("Segoe UI", 8.75F);
            _state.ForeColor = UiTheme.Muted;
            Controls.Add(_state);

            Controls.Add(new Label
            {
                Text = "A form CROMS has a scan of the blank sheet for prints its own artwork " +
                       "and needs no alignment, so it is not listed here. Supplying a blank scan " +
                       "is the permanent fix for any form in this list.",
                Location = new Point(22, 348), Size = new Size(570, 46),
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Faint
            });

            LoadForms();
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

        // ---- small builders -------------------------------------------------

        private static NumericUpDown Spin(decimal min, decimal max, decimal step) =>
            new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = step,
                DecimalPlaces = 1,
                Font = new Font("Segoe UI", 9.5F),
            };

        private static decimal Clamp(NumericUpDown n, decimal v) =>
            v < n.Minimum ? n.Minimum : v > n.Maximum ? n.Maximum : v;

        private static Label Cap(string text, int x, int y) => new Label
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
            ForeColor = UiTheme.Muted
        };

        private static Button Btn(string text, int x, int y, int w, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 34),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // The dialog is not busy enough to warrant a Designer file, and every control here
        // is placed by a literal — the same call the other small admin dialogs make.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiTheme.Polish(this);
        }
    }
}
