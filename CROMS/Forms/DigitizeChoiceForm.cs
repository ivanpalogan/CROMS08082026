using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Step 3 of the Civil Registry Record digitization workflow — shown the moment
    /// "+ Digitize Old Record" is pressed on a Birth/Marriage/Death Record gallery. Offers
    /// exactly the two starting points the workflow allows: Scan/Upload with OCR (Step 4) or
    /// Manual Entry (Step 5). Neither is a new registration — both end on the same record
    /// form the corresponding backlog screen already owns.
    /// </summary>
    public class DigitizeChoiceForm : Form
    {
        /// <summary>True if the operator picked OCR; false for Manual Entry. Only meaningful
        /// when <see cref="Form.ShowDialog()"/> returned <see cref="DialogResult.OK"/>.</summary>
        public bool UseOcr { get; private set; }

        public DigitizeChoiceForm(string recordTypeLabel)
        {
            Text = recordTypeLabel + " Record Digitization";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 300);
            BackColor = UiTheme.PageBg;

            var lblTitle = new Label
            {
                Text = recordTypeLabel + " Record Digitization",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            var lblBody = new Label
            {
                Text = "This is for an already-registered physical " + recordTypeLabel.ToLowerInvariant() +
                       " record from the old registry books — not a new registration.\n\nHow do you " +
                       "want to add it?",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                AutoSize = false,
                Size = new Size(412, 70),
                Location = new Point(24, 54)
            };

            var btnOcr = new Button
            {
                Text = "📷  Scan / Upload with OCR",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Size = new Size(412, 56),
                Location = new Point(24, 136)
            };
            var lblOcrHint = new Label
            {
                Text = "Reads the page automatically, then you review and correct the values.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 196)
            };

            var btnManual = new Button
            {
                Text = "✏  Manual Entry",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Size = new Size(412, 56),
                Location = new Point(24, 222)
            };
            var lblManualHint = new Label
            {
                Text = "Type the record in yourself, straight from the ledger.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 282)
            };

            btnOcr.Click += (s, e) => { UseOcr = true; DialogResult = DialogResult.OK; };
            btnManual.Click += (s, e) => { UseOcr = false; DialogResult = DialogResult.OK; };

            Controls.Add(lblTitle);
            Controls.Add(lblBody);
            Controls.Add(btnOcr);
            Controls.Add(lblOcrHint);
            Controls.Add(btnManual);
            Controls.Add(lblManualHint);

            ClientSize = new Size(460, 316);
            CancelButton = null;

            UiTheme.Polish(this);
        }
    }
}
