using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// The delayed birth registration case for one record - PSA MC 2024-17's checklist, the
    /// 10-day posting, and the registrar's own evaluation. Reuses the marriage licence's
    /// requirements engine and grid unchanged (RequirementsGrid, MarriageService.Requirements/
    /// SaveRequirement are already generic on owner type) rather than a second implementation.
    /// Self-contained code-built dialog, same in-file pattern as MarriageLicenseForm.
    /// </summary>
    internal sealed class DelayedBirthCaseForm : Form
    {
        private readonly int _birthId;
        private DelayedBirthCase _c;

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
        private readonly Button _btnAdminOverride = MUi.Btn("Admin Override - Bypass Requirements", MUi.Kind.Danger, 260);
        private readonly Button _btnClose = MUi.Btn("Close", MUi.Kind.Ghost, 90);
        private readonly Panel _root;

        public DelayedBirthCaseForm(int birthId)
        {
            _birthId = birthId;
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
            _grid.Changed += () => Refresh_();

            var evalCard = MUi.Card(new Padding(16, 12, 16, 12));
            evalCard.Dock = DockStyle.Top; evalCard.Height = 158;
            var evalHead = MUi.Txt("REGISTRAR'S EVALUATION", 9F, FontStyle.Bold, UiTheme.Muted);
            evalHead.Dock = DockStyle.Top; evalHead.Height = 22; evalHead.BackColor = Color.Transparent;
            _txtEvaluation.Dock = DockStyle.Top;
            _lblEvaluated.Dock = DockStyle.Top; _lblEvaluated.AutoSize = false; _lblEvaluated.Height = 18; _lblEvaluated.Margin = new Padding(0, 4, 0, 6);
            var evalBtnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            evalBtnRow.Controls.Add(_btnSaveEvaluation);
            // Admin-only escape hatch: hidden entirely for anyone not signed in as Admin, so a
            // Registrar/Staff session never even sees a control that could bypass the checklist.
            _btnAdminOverride.Visible = Session.User != null && Session.User.Role == "Admin";
            if (_btnAdminOverride.Visible) evalBtnRow.Controls.Add(_btnAdminOverride);
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
            _btnAdminOverride.Click += (s, e) => DoAdminOverride();
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

        private void LoadCase()
        {
            try
            {
                _c = DelayedBirthService.Load(_birthId);
                if (_c == null) { MUi.Fail(this, new InvalidOperationException("This birth record no longer exists.")); Close(); return; }

                _lblWho.Text = string.IsNullOrWhiteSpace(_c.ChildName) ? "(unnamed record)" : _c.ChildName;
                int days = _c.DateOfBirth.HasValue ? (int)(DateTime.Today - _c.DateOfBirth.Value.Date).TotalDays : 0;
                _lblWhen.Text = "Registry No. " + (_c.RegistryNo ?? "(not yet assigned)") + "   -   born " +
                                DelayedBirthRules.D(_c.DateOfBirth) + "   -   " + days + " days ago (over 30 days = delayed, RA 3753)";

                _tglRegistrantDeceased.SetCheckedSilently(_c.RegistrantDeceased);
                _tglMotherUnavailable.SetCheckedSilently(_c.MotherUnavailable);
                _tglParentDeceased.SetCheckedSilently(_c.ParentDeceased);
                _lblParentsMarried.Text = _c.ParentsMarried == true ? "Married" : _c.ParentsMarried == false ? "Not married" : "Not stated on the record";

                _dtpPostingStart.Value = _c.PostingStart ?? DateTime.Today;
                _dtpPostingStart.Enabled = !_c.PostingStart.HasValue;
                _btnStartPosting.Enabled = !_c.PostingStart.HasValue;
                _lblPosting.Text = _c.PostingStart.HasValue
                    ? "Posted " + DelayedBirthRules.D(_c.PostingStart) + " - " + DelayedBirthRules.D(_c.PostingEnd)
                    : "Not yet started.";

                _txtEvaluation.Text = _c.Evaluation ?? "";
                _lblEvaluated.Text = _c.EvaluationAt.HasValue ? "Last recorded " + _c.EvaluationAt.Value.ToString("dd MMM yyyy h:mm tt") : "Not yet evaluated.";

                RefreshRequirements();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void RefreshRequirements()
        {
            var needs = DelayedBirthRules.Needs(_c, DelayedBirthService.Catalog());
            _grid.Bind("Birth", _birthId, needs);
            Refresh_();
        }

        private void Refresh_()
        {
            var rows = DelayedBirthService.Requirements(_birthId);
            var catalog = DelayedBirthService.Catalog();
            var needs = DelayedBirthRules.Needs(_c, catalog);
            bool groupOk = DelayedBirthRules.EvidenceGroupSatisfied(rows, catalog, DelayedBirthService.EvidenceGroup, out int have, out int need);
            _lblEvidence.Text = "Evidence of birth (item c): " + have + " of " + need + " required verified" + (groupOk ? " - satisfied" : "");
            _lblEvidence.ForeColor = groupOk ? UiTheme.Success : UiTheme.Warning;

            bool all = DelayedBirthRules.AllSatisfied(needs, rows, catalog, DelayedBirthService.EvidenceGroup);
            _banner.Set(all ? RuleSeverity.Info : RuleSeverity.Warning,
                all ? "Every checklist item is on file." : "The checklist is not yet complete.",
                all ? "This is what the requirements say - whether to proceed is the registrar's own finding, recorded below."
                    : "Verify each requirement in the grid, and any two of the eight birth-evidence items.",
                all);
        }

        private void SaveFacts()
        {
            if (_c == null) return;
            _c.RegistrantDeceased = _tglRegistrantDeceased.Checked;
            _c.MotherUnavailable = _tglMotherUnavailable.Checked;
            _c.ParentDeceased = _tglParentDeceased.Checked;
            try { DelayedBirthService.SaveCase(_c, Session.User == null ? (int?)null : Session.User.Id); RefreshRequirements(); }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void DoStartPosting()
        {
            if (!MUi.Confirm(this, "Start posting", "Start the 10-day posting period for this delayed registration?",
                    "Start date|" + _dtpPostingStart.Value.ToString("dd MMM yyyy"),
                    "Ends|" + DelayedBirthRules.PostingEnd(_dtpPostingStart.Value.Date, DelayedBirthService.PostingDays).ToString("dd MMM yyyy")))
                return;
            try
            {
                DelayedBirthService.StartPosting(_birthId, _dtpPostingStart.Value.Date, Session.User == null ? (int?)null : Session.User.Id);
                LoadCase();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void DoSaveEvaluation()
        {
            try
            {
                DelayedBirthService.SetEvaluation(_birthId, _txtEvaluation.Text, Session.User == null ? (int?)null : Session.User.Id);
                LoadCase();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        /// <summary>
        /// Admin-only: bypasses the requirements checklist entirely - every requirement is marked
        /// Verified even with no attachment on file. Button is already hidden for anyone not
        /// signed in as Admin, but re-verifies with a fresh username/password (the same
        /// AdminVerificationForm gate Settings uses) and checks the ROLE ON THAT VERIFIED ACCOUNT
        /// is Admin - not merely Admin-or-Registrar, which is all that dialog itself guarantees -
        /// so this cannot be triggered by someone who walked up to an already-open admin session.
        /// </summary>
        private void DoAdminOverride()
        {
            if (!MUi.Confirm(this, "Admin override",
                    "This marks EVERY requirement on this case as Verified, including any with no " +
                    "document attached. It does not check any paperwork - it records that an " +
                    "administrator chose to proceed despite the checklist being incomplete. Type the " +
                    "reason in the Registrar's Evaluation box below first if you want it kept with the record.",
                    "Case|" + (_c != null ? _c.ChildName : ""),
                    "Registry No.|" + (_c != null ? (_c.RegistryNo ?? "(not yet assigned)") : "")))
                return;

            using (var dlg = new AdminVerificationForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.VerifiedUser == null || dlg.VerifiedUser.Role != "Admin")
                {
                    MessageBox.Show(this, "Only an Administrator account can bypass delayed-registration requirements.",
                        "Not allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    DelayedBirthService.AdminOverride(_birthId, _txtEvaluation.Text, dlg.VerifiedUser.Id, dlg.VerifiedUser.Username);
                    LoadCase();
                }
                catch (Exception ex) { MUi.Fail(this, ex); }
            }
        }
    }
}
