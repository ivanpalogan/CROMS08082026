using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    internal sealed partial class DelayedBirthCaseForm
    {
        private readonly Label _lblWho = MUi.Txt("", 12F, FontStyle.Bold);
        private readonly Label _lblWhen = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Banner _banner = new Banner();

        private readonly ToggleSwitch _tglRegistrantDeceased = new ToggleSwitch(), _tglMotherUnavailable = new ToggleSwitch(), _tglParentDeceased = new ToggleSwitch();
        private readonly Label _lblParentsMarried = MUi.Txt("", 9.5F, FontStyle.Regular);

        private readonly DateTimePicker _dtpPostingStart = MUi.Date(false);
        private readonly Label _lblPosting = MUi.Txt("", 9.5F);
        private readonly Button _btnStartPosting = MUi.Btn("Start 10-Day Posting", MUi.Kind.Secondary, 190);

        private readonly Label _lblEvidence = MUi.Txt("", 9.5F, FontStyle.Bold);
        private readonly RequirementsGrid _grid = new RequirementsGrid();

        private readonly TextBox _txtEvaluation = new TextBox { Multiline = true, Height = 64, ScrollBars = ScrollBars.Vertical, Font = MUi.F(9.5F) };
        private readonly Label _lblEvaluated = MUi.Txt("", 8.5F, FontStyle.Regular, UiTheme.Muted);
        private readonly Button _btnSaveEvaluation = MUi.Btn("Save Evaluation", MUi.Kind.Primary, 150);
        private readonly Button _btnClose = MUi.Btn("Close", MUi.Kind.Ghost, 90);
        private Panel _root;

        private void InitializeComponent()
        {
            Text = "Delayed Birth Registration"; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 760); MinimumSize = new Size(760, 600);
            BackColor = UiTheme.Surface; ShowIcon = false; MaximizeBox = false;

            _root = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20), BackColor = UiTheme.Surface };
            Panel root = _root;
            _lblWho.Dock = DockStyle.Top; _lblWho.AutoSize = false; _lblWho.Height = 26; _lblWho.UseMnemonic = false;
            _lblWhen.Dock = DockStyle.Top; _lblWhen.AutoSize = false; _lblWhen.Height = 20; _lblWhen.Margin = new Padding(0, 0, 0, 8);

            var facts = MUi.Card(new Padding(16, 12, 16, 14));
            facts.Dock = DockStyle.Top; facts.Height = 148;
            var factsHead = MUi.Txt("CASE FACTS (PSA MC 2024-17)", 9F, FontStyle.Bold, UiTheme.Muted);
            factsHead.Dock = DockStyle.Top; factsHead.Height = 22; factsHead.BackColor = Color.Transparent;
            var factsGrid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 2, Height = 100, BackColor = Color.Transparent };
            for (int i = 0; i < 4; i++) factsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            factsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); factsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            factsGrid.Controls.Add(ToggleCell("Registrant deceased", _tglRegistrantDeceased), 0, 0);
            factsGrid.Controls.Add(ToggleCell("Mother unavailable", _tglMotherUnavailable), 1, 0);
            factsGrid.Controls.Add(ToggleCell("A parent deceased", _tglParentDeceased), 2, 0);
            _lblParentsMarried.BackColor = Color.Transparent; _lblParentsMarried.AutoSize = false; _lblParentsMarried.Dock = DockStyle.Fill;
            factsGrid.Controls.Add(Labelled("Parents married (from the record)", _lblParentsMarried), 3, 0);
            facts.Controls.Add(factsGrid); facts.Controls.Add(factsHead);

            var posting = MUi.Card(new Padding(16, 12, 16, 12));
            posting.Dock = DockStyle.Top; posting.Height = 104; posting.Margin = new Padding(0, 12, 0, 0);
            var postingHead = MUi.Txt("10-DAY POSTING", 9F, FontStyle.Bold, UiTheme.Muted);
            postingHead.Dock = DockStyle.Top; postingHead.Height = 22; postingHead.BackColor = Color.Transparent;
            var postingRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, Height = 58, BackColor = Color.Transparent };
            postingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); postingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            postingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            postingRow.Controls.Add(MUi.Field("Posting start", _dtpPostingStart), 0, 0);
            var startCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 0, 0), BackColor = Color.Transparent };
            _btnStartPosting.Dock = DockStyle.Left; startCell.Controls.Add(_btnStartPosting);
            postingRow.Controls.Add(startCell, 1, 0);
            _lblPosting.Dock = DockStyle.Fill; _lblPosting.TextAlign = ContentAlignment.MiddleLeft; _lblPosting.BackColor = Color.Transparent;
            postingRow.Controls.Add(_lblPosting, 2, 0);
            posting.Controls.Add(postingRow); posting.Controls.Add(postingHead);

            _lblEvidence.Dock = DockStyle.Top; _lblEvidence.AutoSize = false; _lblEvidence.Height = 24; _lblEvidence.Margin = new Padding(0, 12, 0, 4); _lblEvidence.BackColor = Color.Transparent;
            var reqHead = MUi.Txt("REQUIREMENTS CHECKLIST", 9F, FontStyle.Bold, UiTheme.Muted);
            reqHead.Dock = DockStyle.Top; reqHead.Height = 22; reqHead.BackColor = Color.Transparent;
            _grid.Dock = DockStyle.Top; _grid.Margin = new Padding(0, 0, 0, 10);
            _grid.AllowAddCustom = true; // this case may need a document PSA MC 2024-17's own checklist doesn't name
            // Per-row Admin bypass replaces the old whole-checklist "Admin Override" button - the
            // grid itself hides the control for anyone not signed in as Admin, and the service
            // layer (MarriageService.BypassRequirement) re-checks the role regardless.
            _grid.AllowBypass = Session.User != null && Session.User.Role == "Admin";
            _grid.Changed += () => Refresh_();

            var evalCard = MUi.Card(new Padding(16, 12, 16, 12));
            evalCard.Dock = DockStyle.Top; evalCard.Height = 158;
            var evalHead = MUi.Txt("REGISTRAR'S EVALUATION", 9F, FontStyle.Bold, UiTheme.Muted);
            evalHead.Dock = DockStyle.Top; evalHead.Height = 22; evalHead.BackColor = Color.Transparent;
            _txtEvaluation.Dock = DockStyle.Top;
            _lblEvaluated.Dock = DockStyle.Top; _lblEvaluated.AutoSize = false; _lblEvaluated.Height = 18; _lblEvaluated.Margin = new Padding(0, 4, 0, 6);
            var evalBtnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            evalBtnRow.Controls.Add(_btnSaveEvaluation);
            var evalParts = new Control[] { evalBtnRow, _lblEvaluated, _txtEvaluation, evalHead };
            foreach (Control c in evalParts) evalCard.Controls.Add(c);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
            footer.Controls.Add(_btnClose);

            var parts = new Control[] { _lblWho, _lblWhen, _banner, facts, posting, _lblEvidence, reqHead, _grid, evalCard };
            // Controls.Add ORDER decides both the Dock=Top stacking (last added ends up
            // topmost - see the loop below) AND the default Tab order (first added is tabbed to
            // first). Adding in reverse for the stacking left Tab order running bottom-to-top,
            // so WinForms focused a control at the visual BOTTOM on Show and the AutoScroll
            // panel scrolled it into view - the window opened part-way down the page with
            // nothing above the fold visible. Same trap recorded for the MF-90 screen on
            // 2026-09-13. Fixed by stating TabIndex explicitly in the VISUAL order instead of
            // leaving it to fall out of the Add order.
            for (int i = parts.Length - 1; i >= 0; i--) root.Controls.Add(parts[i]);
            for (int i = 0; i < parts.Length; i++) parts[i].TabIndex = i;
            Controls.Add(root); Controls.Add(footer);

            _btnStartPosting.Click += (s, e) => DoStartPosting();
            _btnSaveEvaluation.Click += (s, e) => DoSaveEvaluation();
            _btnClose.Click += (s, e) => Close();
            _tglRegistrantDeceased.CheckedChanged += (s, e) => SaveFacts();
            _tglMotherUnavailable.CheckedChanged += (s, e) => SaveFacts();
            _tglParentDeceased.CheckedChanged += (s, e) => SaveFacts();

            Load += (s, e) => { LoadCase(); _root.AutoScrollPosition = new Point(0, 0); };

            // This is a modal dialog, not a MainForm module - it is never passed through
            // ShowModule's own polish pass, so it has to style itself (same as every other
            // stand-alone dialog in Forms/: MarriageLicenseForm, BreqsForm).
            UiTheme.Polish(this);
        }

        private static Control ToggleCell(string caption, ToggleSwitch t)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 0, 8, 0) };
            var cap = MUi.Txt(caption, 8.75F, FontStyle.Regular, UiTheme.Muted);
            cap.Dock = DockStyle.Top; cap.AutoSize = false; cap.Height = 30; cap.BackColor = Color.Transparent;
            t.Location = new Point(0, 30);
            p.Controls.Add(t); p.Controls.Add(cap);
            return p;
        }

        private static Control Labelled(string caption, Control value)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var cap = MUi.Txt(caption, 8.75F, FontStyle.Regular, UiTheme.Muted);
            cap.Dock = DockStyle.Top; cap.AutoSize = false; cap.Height = 30; cap.BackColor = Color.Transparent;
            value.Location = new Point(0, 30); value.Size = new Size(p.Width, 24);
            p.Controls.Add(value); p.Controls.Add(cap);
            return p;
        }
    }
}
