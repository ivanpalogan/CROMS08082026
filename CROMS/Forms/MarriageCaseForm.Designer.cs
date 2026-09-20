using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class MarriageCaseForm
    {
        private readonly Panel _body = new Panel();
        private readonly ComboBox _basis = MUi.Combo(false);
        private readonly TextBox _basisNotes = MUi.Box(), _delayReason = MUi.Box(), _reviewNotes = MUi.Box();
        private readonly DateTimePicker _postStart = MUi.Date(false);
        private readonly RequirementsGrid _docs = new RequirementsGrid();
        private readonly IssueList _issues = new IssueList();

        private void InitializeComponent()
        {
            Text = "Delayed / License-Exempt Case";
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(900, 760); MinimumSize = new Size(820, 600);
            BackColor = UiTheme.Surface; ShowInTaskbar = false;
            var head = new Label { Text = "  DELAYED / LICENSE-EXEMPT CASE WORKFLOW", Dock = DockStyle.Top, Height = 46, BackColor = UiTheme.Navy,
                                   ForeColor = Color.White, Font = MUi.F(12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            _body.Dock = DockStyle.Fill; _body.AutoScroll = true; _body.Padding = new Padding(22, 14, 22, 14);
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 11, 16, 8),
                                             BackColor = Color.FromArgb(250, 251, 253) };
            var close = MUi.Btn("Close", MUi.Kind.Secondary, 100); close.Click += (s, e) => Close();
            foot.Controls.Add(close);
            Controls.Add(_body); Controls.Add(foot); Controls.Add(head);
            for (int i = 0; i < MarriageRules.ExemptionBases.GetLength(0); i++) _basis.Items.Add(MarriageRules.ExemptionBases[i, 1]);
            _docs.AllowBypass = MarriageService.IsAdmin;
            _docs.Changed += () => Rebuild();
            _issues.FixRequested += w => { if (w != "Case Workflow") Close(); };
            // Opening focused the posting date picker and scrolled the panel to it, so the
            // case summary at the top was off-screen on arrival.
            Shown += (s, e) => { ActiveControl = null; _body.AutoScrollPosition = Point.Empty; };
            Rebuild();
        }
    }
}
