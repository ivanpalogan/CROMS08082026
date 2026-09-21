using System;
using System.Collections.Generic;
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
    internal sealed partial class DelayedBirthCaseForm : Form
    {
        private readonly int _birthId;
        private DelayedBirthCase _c;

        public DelayedBirthCaseForm(int birthId)
        {
            _birthId = birthId;
            InitializeComponent();
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

                // Not yet posted: the notice goes up today, never backdated. Already posted: show the stored date.
                if (!_c.PostingStart.HasValue) _dtpPostingStart.MinDate = DateTime.Today;
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
            if (_dtpPostingStart.Value.Date < DateTime.Today)
            {
                MessageBox.Show(this, "The posting start is the day the notice actually goes up - it cannot be in the past. Use today's date.", "Posting",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
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

    }
}
