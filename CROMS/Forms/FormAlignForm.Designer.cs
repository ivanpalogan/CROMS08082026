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
    public partial class FormAlignForm
    {
        private readonly ComboBox _form = new ComboBox();
        private readonly NumericUpDown _offX = Spin(-400, 400, 0.5m);
        private readonly NumericUpDown _offY = Spin(-400, 400, 0.5m);
        private readonly NumericUpDown _sclX = Spin(0.5m, 2m, 0.005m);
        private readonly NumericUpDown _sclY = Spin(0.5m, 2m, 0.005m);
        private readonly Label _state = new Label();

        private void InitializeComponent()
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
    }
}
