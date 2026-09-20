using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class IssueLicenseForm
    {
        private readonly DateTimePicker _date = MUi.Date(false);
        private readonly Label _valid = MUi.Txt("", 10F, FontStyle.Bold), _no = MUi.Txt("", 10F, FontStyle.Bold);
        private readonly IssueList _issues = new IssueList();
        // "&&": UiTheme owner-draws buttons with TextRenderer, which eats a single '&' as a
        // mnemonic - the render showed "ISSUE _PRINT LICENSE". Same convention as the sidebar.
        private readonly Button _go = MUi.Btn("ISSUE && PRINT LICENSE", MUi.Kind.Success, 210);
        // No emoji glyph - UiTheme owner-draws buttons via TextRenderer, which cannot render a
        // colour emoji (same trap fixed on the Login eye button 2026-08-29). Plain text only.
        private readonly Button _override = MUi.Btn("Admin Override - Missing Requirements", MUi.Kind.Secondary, 300);
        private readonly Label _overrideStatus = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Warning);

        private void InitializeComponent()
        {
            Text = "Issue Marriage License";
            StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(620, 640); BackColor = UiTheme.Surface;
        }
    }
}
