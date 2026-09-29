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
            using (var f = new ReasonPromptForm(action, target))
            {
                var result = owner != null ? f.ShowDialog(owner) : f.ShowDialog();
                return result == DialogResult.OK ? f.Reason : null;
            }
        }
    }
}
