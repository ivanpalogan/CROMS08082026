using System;
using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>
    /// Step 3 of the Civil Registry Record digitization workflow — shown the moment
    /// "+ Digitize Old Record" is pressed on a Birth/Marriage/Death Record gallery. Offers
    /// exactly the two starting points the workflow allows: Scan/Upload with OCR (Step 4) or
    /// Manual Entry (Step 5). Neither is a new registration — both end on the same record
    /// form the corresponding backlog screen already owns.
    /// </summary>
    public partial class DigitizeChoiceForm : Form
    {
        /// <summary>True if the operator picked OCR; false for Manual Entry. Only meaningful
        /// when <see cref="Form.ShowDialog()"/> returned <see cref="DialogResult.OK"/>.</summary>
        public bool UseOcr { get; private set; }

        public DigitizeChoiceForm(string recordTypeLabel)
        {
            InitializeComponent();
            ApplyRecordType(recordTypeLabel);
            CancelButton = null;
            CROMS.Modules.UiTheme.Polish(this);
        }

        private void ApplyRecordType(string recordTypeLabel)
        {
            Text = recordTypeLabel + " Record Digitization";
            lblTitle.Text = recordTypeLabel + " Record Digitization";
            lblBody.Text = "This is for an already-registered physical " + recordTypeLabel.ToLowerInvariant() +
                           " record from the old registry books — not a new registration.\n\nHow do you " +
                           "want to add it?";
        }

        private void btnOcr_Click(object sender, EventArgs e)
        {
            UseOcr = true;
            DialogResult = DialogResult.OK;
        }

        private void btnManual_Click(object sender, EventArgs e)
        {
            UseOcr = false;
            DialogResult = DialogResult.OK;
        }
    }
}
