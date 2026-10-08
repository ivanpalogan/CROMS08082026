using System;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>The dialog behind <see cref="ReasonPrompt.Ask"/>.</summary>
    public partial class ReasonPromptForm : Form
    {
        public string Reason { get; private set; }

        public ReasonPromptForm(string action, string target)
        {
            InitializeComponent();
            FieldLimit.Cap(255, txtReason);              // every reason column is 255
            Text = action + " - Reason Required";
            lblTitle.Text = action + " requires a reason";
            lblSub.Text = "State why " + (target ?? "this record") + " is being " + action.ToLower() + "d. " +
                          "This is written to the audit trail and cannot be left blank.";
            btnOk.Text = "Confirm " + action;
            UiTheme.Polish(this);
            Shown += (s, e) => txtReason.Focus();
        }

        private void txtReason_TextChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = txtReason.Text.Trim().Length > 0;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Reason = txtReason.Text.Trim();
        }
    }
}
