using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Modal "why are you changing/removing this?" prompt. Blocks (returns null) until a
    /// non-blank reason is entered or the operator cancels - callers must not proceed on null.
    /// The reason is meant to be folded into the audit_log details string by the caller, e.g.
    /// Audit.Write(Audit.Update, "births", id, "Reason: " + reason + ". " + otherDetails).
    /// </summary>
    public static class ReasonPrompt
    {
        /// <param name="owner">Form to center over.</param>
        /// <param name="action">Verb shown in the title, e.g. "Edit", "Delete".</param>
        /// <param name="target">What is being acted on, e.g. "this birth record".</param>
        /// <returns>Trimmed non-empty reason, or null if cancelled/left blank.</returns>
        public static string Ask(IWin32Window owner, string action, string target)
        {
            using (var f = new PromptForm(action, target))
            {
                var result = owner != null ? f.ShowDialog(owner) : f.ShowDialog();
                return result == DialogResult.OK ? f.Reason : null;
            }
        }

        private sealed class PromptForm : Form
        {
            private readonly TextBox _txt;
            private readonly Button _ok;
            public string Reason { get; private set; }

            public PromptForm(string action, string target)
            {
                Text = action + " - Reason Required";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(440, 230);
                Font = new Font("Segoe UI", 9.75F);

                var lblTitle = new Label
                {
                    Text = action + " requires a reason",
                    Location = new Point(20, 16),
                    Size = new Size(400, 24),
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold)
                };
                var lblSub = new Label
                {
                    Text = "State why " + (target ?? "this record") + " is being " + action.ToLower() + "d. " +
                           "This is written to the audit trail and cannot be left blank.",
                    Location = new Point(20, 44),
                    Size = new Size(400, 40),
                    ForeColor = Color.FromArgb(0x5B, 0x64, 0x72)
                };
                _txt = new TextBox
                {
                    Location = new Point(20, 90),
                    Size = new Size(400, 70),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Segoe UI", 9.75F)
                };
                _txt.TextChanged += (s, e) => { _ok.Enabled = _txt.Text.Trim().Length > 0; };

                var cancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(240, 175),
                    Size = new Size(85, 34),
                    DialogResult = DialogResult.Cancel
                };
                _ok = new Button
                {
                    Text = "Confirm " + action,
                    Location = new Point(335, 175),
                    Size = new Size(85, 34),
                    DialogResult = DialogResult.OK,
                    Enabled = false
                };
                _ok.Click += (s, e) => { Reason = _txt.Text.Trim(); };

                Controls.Add(lblTitle);
                Controls.Add(lblSub);
                Controls.Add(_txt);
                Controls.Add(cancel);
                Controls.Add(_ok);
                AcceptButton = _ok;
                CancelButton = cancel;

                UiTheme.Polish(this);
                Shown += (s, e) => _txt.Focus();
            }
        }
    }
}
