// DeathRegistrationForm.Wizard.cs - the numbered step strip, the "at a glance" rail, the
// per-step required-field gate and the automatic age. Built in code (not the Designer)
// for the same reason Birth Registration's wizard is: a Designer regeneration has silently
// deleted hand-added controls on these screens before.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    public partial class DeathRegistrationForm
    {
        private StepStrip _stepStrip;
        private Panel _tabHost;
        private Panel _wizardHost;
        private Panel _railPanel;
        private string _loadedRegistryNo = "";
        private bool _suspendAge;

        /// <summary>One missing or invalid entry: what is wrong, which step it is on, where to put the cursor,
        /// and the short red sentence to show under that field (longest wording first).</summary>
        private sealed class StepIssue
        {
            public readonly string Message;
            public readonly int Step;
            public readonly Control Focus;
            public readonly string[] Msg;
            public StepIssue(string message, int step, Control focus, params string[] msg)
            { Message = message; Step = step; Focus = focus; Msg = msg; }
        }

        // The five steps, in the order of tabControl.TabPages (the Medical & Permits page is
        // appended in the constructor, after the four Designer pages).
        private static readonly string[] StepSubs =
        {
            "who died, when and where",
            "cause of death, disposal",
            "who reported the death",
            "registry book & signatures",
            "medical certificate, permits"
        };

        /// <summary>
        /// Rebuilds the entry panel around a numbered step strip (top), the page content
        /// (centre), a "summary at a glance" rail (right) and a Back / Next footer - the same
        /// StepStrip / IssueList / Banner the marriage license and birth screens use, reused
        /// rather than reinvented so the registration screens read as one system.
        /// <para/>
        /// The native tab headers are hidden by positioning the TabControl OUTSIDE its host's
        /// top edge: the step strip is the only navigator, so the gate in
        /// <see cref="GoToStep"/> cannot be bypassed by clicking a tab.
        /// </summary>
        private void InitializeWizardChrome()
        {
            if (_wizardHost != null) return;

            cardForm.Controls.Remove(tabControl);
            cardForm.Controls.Remove(pnlRecordActions);
            tabControl.Dock = DockStyle.None;
            tabControl.Multiline = false;
            tabControl.SizeMode = TabSizeMode.Fixed;
            foreach (TabPage p in tabControl.TabPages) p.AutoScroll = true;   // narrow screens scroll, never clip

            _tabHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.White };
            _tabHost.Controls.Add(tabControl);
            _tabHost.Resize += delegate { PositionHiddenTabHeader(); };

            _stepStrip = new StepStrip(true) { Large = true, Height = 76 };
            for (int i = 0; i < tabControl.TabPages.Count; i++)
                _stepStrip.AddStep(tabControl.TabPages[i].Text.Replace("&&", "&"), i < StepSubs.Length ? StepSubs[i] : "");
            _stepStrip.StepClicked += i => GoToStep(i);
            tabControl.SelectedIndexChanged += delegate { UpdateStepNavigation(); if (IsHandleCreated) BeginInvoke(new Action(DeselectCombos)); };

            _railPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 14, 16, 14),
                BackColor = Color.FromArgb(250, 251, 253)
            };
            _railPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, 0, _railPanel.Height);
            };

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
            body.Controls.Add(_tabHost, 0, 0);
            body.Controls.Add(_railPanel, 1, 0);

            // Footer: a one-line "what is next" hint on the left and EVERY action on the right, in
            // the order the eye finishes a form: Back, Next, then Register Death. Register Death and
            // the print buttons used to float in a header band above the form, far from where the
            // clerk's attention is by the time the entries are done.
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0), Padding = new Padding(4, 0, 4, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(lblStepHint, 0, 0);
            footer.Controls.Add(pnlRecordActions, 1, 0);
            pnlRecordActions.AutoSize = true;
            pnlRecordActions.Dock = DockStyle.Fill;
            // The card face is white; a panel that inherits the form's page grey paints a grey bar
            // across the bottom of it, which also swallows the pale buttons.
            footer.BackColor = Color.White;
            pnlRecordActions.BackColor = Color.White;
            lblStepHint.BackColor = Color.White;
            // The bar is right-to-left: the first control is the right-most one.
            pnlRecordActions.Controls.Add(btnSave);
            pnlRecordActions.Controls.SetChildIndex(btnSave, 0);
            pnlRecordActions.Controls.Add(btnAckSlip);
            pnlRecordActions.Controls.Add(btnCertificate);
            btnSave.TabIndex = 2;

            // Explicit rows (strip / body / footer) rather than a Dock stack, so dock order
            // can never squeeze the strip into a sliver.
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
            // An unstyled column AutoSizes to its widest child, which pushed the footer past the
            // window's right edge once the buttons grew; one column that is exactly the width given.
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lblStepHint.AutoSize = false;   // a fixed-size label with an ellipsis, not one that demands its full text width
            lblStepHint.AutoEllipsis = true;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, _stepStrip.Height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.Controls.Add(_stepStrip, 0, 0);
            root.Controls.Add(body, 0, 1);
            root.Controls.Add(footer, 0, 2);
            _wizardRoot = root;
            _wizardBody = body;

            _wizardHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _wizardHost.Controls.Add(root);
            cardForm.Controls.Add(_wizardHost);
            PositionHiddenTabHeader();

            // Keeps the rail current as the operator types (debounced - once, ~300 ms after
            // the last keystroke).
            var railTimer = new System.Windows.Forms.Timer { Interval = 300 };
            railTimer.Tick += delegate { railTimer.Stop(); if (!IsDisposed) RefreshRail(); };
            Disposed += delegate { railTimer.Dispose(); };
            EventHandler touch = delegate { railTimer.Stop(); railTimer.Start(); };
            foreach (TextBox t in new[] { txtLastName, txtFirstName, txtAge, txtDispPlace, txtCInfName, txtCInfAddr })
                t.TextChanged += touch;
            cboSex.SelectedIndexChanged += touch;
            cboCivil.SelectedIndexChanged += touch;
            cboDisposal.SelectedIndexChanged += touch;
            dtpDob.ValueChanged += touch;
            dtpDod.ValueChanged += touch;
            foreach (ComboBox c in new[] { _cboDCit, _cboDImm, _cboInfRel })
                if (c != null) { c.TextChanged += touch; c.SelectedIndexChanged += touch; }
            if (_pod != null) foreach (ComboBox c in _pod) { c.TextChanged += touch; c.SelectedIndexChanged += touch; }

            UpdateStepNavigation();
        }

        /// <summary>Pushes the native tab header band above the host's visible top edge.</summary>
        private void PositionHiddenTabHeader()
        {
            if (_tabHost == null || tabControl == null) return;
            const int headerH = 36;
            tabControl.SetBounds(-2, -headerH, _tabHost.Width + 4, _tabHost.Height + headerH);
        }

        // ---------------------------------------------------------------- required fields

        /// <summary>
        /// The entries each step needs before the operator may move past it. These are the
        /// core facts of a Municipal Form 103 entry; anything not listed (middle name,
        /// religion, the medical certifier, permit numbers, the office signatures) stays
        /// optional - a person may have no middle name, a death may have no medical
        /// attendant - so refusing the step over them would only block valid registrations.
        /// </summary>
        private List<StepIssue> IssuesForStep(int step)
        {
            var list = new List<StepIssue>();
            // msg = the sentence shown under the field, longest wording first; the last one is short
            // enough for any column, so it is never cut off.
            void Need(bool ok, string what, Control at, params string[] msg) { if (!ok) list.Add(new StepIssue(what, step, at, msg)); }
            bool Has(string s) => !string.IsNullOrWhiteSpace(s);

            switch (step)
            {
                case 0:
                    Need(Has(txtLastName.Text), "Last name", txtLastName,
                        "Please enter the last name.", "Enter the last name.", "Required.");
                    Need(Has(txtFirstName.Text), "First name", txtFirstName,
                        "Please enter the first name.", "Enter the first name.", "Required.");
                    Need(cboSex.SelectedItem != null, "Sex", cboSex,
                        "Please choose the sex.", "Choose the sex.", "Required.");
                    Need(cboCivil.SelectedItem != null, "Civil status", cboCivil,
                        "Please choose the civil status.", "Choose the status.", "Required.");
                    Need(_cboDCit != null && Has(_cboDCit.Text), "Citizenship", _cboDCit,
                        "Please choose the citizenship.", "Choose citizenship.", "Required.");
                    if (dtpDob.Checked && dtpDob.Value.Date > dtpDod.Value.Date)
                        list.Add(new StepIssue("Date of birth is after the date of death", step, dtpDob,
                            "The date of birth is after the date of death.", "Birth is after death.", "Check dates."));
                    else
                    {
                        int age;
                        bool okAge = int.TryParse((txtAge.Text ?? "").Trim(), out age) && age >= 0 && age <= 130;
                        if (!okAge && dtpDob.Checked)
                            list.Add(new StepIssue("Age (could not be computed - check the dates)", step, dtpDob,
                                "The age could not be worked out - check the dates.", "Check the two dates.", "Check dates."));
                        else if (!okAge)
                            list.Add(new StepIssue("Age at death (or tick Date of Birth)", step, txtAge,
                                "Please enter the age, or tick Date of Birth.", "Enter the age.", "Required."));
                    }
                    if (_pod != null)
                    {
                        bool prov = Has(_pod[1].Text), muni = Has(_pod[2].Text);
                        if (!prov && !muni)
                            list.Add(new StepIssue("Place of death - province and city / municipality", step, _pod[1],
                                "Please choose the province and the city / municipality.", "Choose province and city.", "Required."));
                        else if (!prov)
                            list.Add(new StepIssue("Place of death - province", step, _pod[1],
                                "Please choose the province.", "Choose the province.", "Required."));
                        else if (!muni)
                            list.Add(new StepIssue("Place of death - city / municipality", step, _pod[2],
                                "Please choose the city / municipality.", "Choose the city.", "Required."));
                    }
                    break;

                case 1:
                    Need(_cboDImm != null && Has(_cboDImm.Text), "Immediate cause of death", _cboDImm,
                        "Please enter the immediate cause of death.", "Enter the immediate cause.", "Required.");
                    Need(cboDisposal.SelectedItem != null, "Disposal method", cboDisposal,
                        "Please choose the disposal method.", "Choose the method.", "Required.");
                    Need(Has(txtDispPlace.Text), "Place of disposal", txtDispPlace,
                        "Please enter the place of disposal.", "Enter the place.", "Required.");
                    break;

                case 2:
                    Need(Has(txtCInfName.Text), "Informant's name", txtCInfName,
                        "Please enter the informant's name.", "Enter the informant's name.", "Required.");
                    Need(_cboInfRel != null && Has(_cboInfRel.Text), "Informant's relationship to the deceased", _cboInfRel,
                        "Please choose the relationship to the deceased.", "Choose the relationship.", "Required.");
                    Need(Has(txtCInfAddr.Text), "Informant's address", txtCInfAddr,
                        "Please enter the informant's address.", "Enter the address.", "Required.");
                    break;
            }
            return list;
        }

        private string StepName(int step)
        {
            return step >= 0 && step < tabControl.TabPages.Count ? tabControl.TabPages[step].Text.Replace("&&", "&") : "";
        }

        /// <summary>Every step before <paramref name="upTo"/> must be complete. Returns the first problem step, or -1.</summary>
        private int FirstIncompleteStepBefore(int upTo)
        {
            for (int s = 0; s < upTo; s++)
                if (IssuesForStep(s).Count > 0) return s;
            return -1;
        }

        /// <summary>
        /// Puts a short red sentence under EVERY required field that is still wrong on steps 1 to
        /// <paramref name="throughStep"/> + 1, takes the operator to the first one and puts the cursor
        /// on it. Used by the step gate and by Register / Save. No pop-up: the sentence sits on the
        /// field itself and goes the moment that field is fixed.
        /// </summary>
        private void ShowIssues(int throughStep)
        {
            var all = new List<StepIssue>();
            for (int s = 0; s <= Math.Min(throughStep, 2); s++) all.AddRange(IssuesForStep(s));
            if (all.Count == 0) return;

            ClearAllFieldMessages();
            foreach (StepIssue i in all) ShowFieldMessage(i.Focus, i.Msg);
            FocusField(all[0].Focus, all[0].Step);
            UpdateStepNavigation();
        }

        private bool HasIssues(int throughStep)
        {
            for (int s = 0; s <= Math.Min(throughStep, 2); s++)
                if (IssuesForStep(s).Count > 0) return true;
            return false;
        }

        /// <summary>True when pressing the save button would be accepted right now.</summary>
        private bool IsReadyToSave()
        {
            if (_editingId != null)
                return !string.IsNullOrWhiteSpace(txtFirstName.Text) && !string.IsNullOrWhiteSpace(txtLastName.Text);
            return !HasIssues(2);
        }

        /// <summary>
        /// A NEW registration cannot be saved with a required entry empty. An existing record
        /// only needs a name to be saved again (a record migrated from the old register may
        /// legitimately lack a citizenship, and that must not stop a typo being corrected),
        /// but the rail still lists what it lacks.
        /// </summary>
        private bool ValidateAllSteps()
        {
            if (_editingId != null) return ValidateName();
            if (!HasIssues(2)) return true;
            ShowIssues(2);
            return false;
        }

        // ---------------------------------------------------------------- navigation

        private void GoToStep(int index)
        {
            if (index < 0 || index >= tabControl.TabPages.Count) return;

            // Moving forward is allowed only when every step it passes is complete; going
            // back is always allowed. An existing record navigates freely.
            if (index > tabControl.SelectedIndex && _editingId == null)
            {
                int bad = FirstIncompleteStepBefore(index);
                if (bad >= 0) { ShowIssues(index - 1); return; }
            }
            tabControl.SelectedIndex = index;
            UpdateStepNavigation();
        }

        private void btnBack_Click(object sender, EventArgs e) { GoToStep(tabControl.SelectedIndex - 1); }

        private void btnNext_Click(object sender, EventArgs e)
        {
            int last = tabControl.TabPages.Count - 1;
            if (tabControl.SelectedIndex >= last) { btnSave_Click(sender, e); return; }
            GoToStep(tabControl.SelectedIndex + 1);
        }

        private void UpdateStepNavigation()
        {
            if (tabControl.SelectedIndex < 0) return;
            int cur = tabControl.SelectedIndex, last = tabControl.TabPages.Count - 1;

            if (_stepStrip != null)
                for (int i = 0; i < _stepStrip.Count; i++)
                {
                    // A step earlier than the current one counts as Done only when it really
                    // is complete (an existing record can be opened past an incomplete step),
                    // and a step with entries still missing carries its count as a red badge.
                    int missing = i <= 2 ? IssuesForStep(i).Count : 0;
                    _stepStrip.SetBadge(i, missing);
                    _stepStrip.SetState(i, i == cur ? StepStrip.State.Current
                        : i < cur && missing == 0 ? StepStrip.State.Done : StepStrip.State.Todo);
                }

            btnBack.Enabled = cur > 0;
            btnNext.Text = "Next >";
            btnSave.Text = SaveCaption();
            RefreshRail();
        }

        /// <summary>
        /// Hint line, which button is the solid one, and whether Next is offered at all - on the
        /// last step there is nowhere to go next, so the save button is the only forward action.
        /// </summary>
        private void UpdateFooterState()
        {
            if (tabControl.SelectedIndex < 0 || btnNext == null) return;
            int cur = tabControl.SelectedIndex, last = tabControl.TabPages.Count - 1;
            btnNext.Visible = cur < last;
            lblStepHint.Text = FooterHint(cur, last);
            UpdateFooterEmphasis();
        }

        private string SaveCaption() { return _editingId == null ? "Register Death" : "Save Changes"; }

        // ---------------------------------------------------------------- rail

        private void RefreshRail()
        {
            if (_railPanel == null) return;
            _railPanel.SuspendLayout();
            var stale = new List<Control>();
            foreach (Control c in _railPanel.Controls) stale.Add(c);
            foreach (Control c in stale) c.Dispose();
            _railPanel.Controls.Clear();

            var items = new List<Control>();
            items.Add(RailCap("Registration at a glance"));
            items.Add(RailKv("Registry No.", string.IsNullOrWhiteSpace(_loadedRegistryNo) ? "(on save)" : _loadedRegistryNo));
            string name = ((txtLastName.Text ?? "").Trim() + ", " + (txtFirstName.Text ?? "").Trim()).Trim(' ', ',');
            items.Add(RailKv("Deceased", string.IsNullOrWhiteSpace(name) ? "-" : name));
            items.Add(RailKv("Date of death", dtpDod.Value.ToString("dd MMM yyyy")));
            items.Add(RailKv("Age", string.IsNullOrWhiteSpace(txtAge.Text) ? "-" : txtAge.Text.Trim()));

            var stage = new FlowLayoutPanel { Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0), Dock = DockStyle.Top };
            var stageKey = MUi.Txt("Status", 9F, FontStyle.Regular, UiTheme.Muted);
            stageKey.Font = Px(17f);
            stageKey.Margin = new Padding(0, 5, 8, 0);
            stage.Controls.Add(stageKey);
            StatusPill pill = MUi.Pill(_editingId == null ? "NEW" : "SAVED", _editingId == null ? "Draft" : "Registered");
            pill.Font = Px(15f, FontStyle.Bold);
            stage.Controls.Add(pill);
            items.Add(stage);

            // The strip already says which step this is; on a short screen the line is the first thing to go.
            if (tabControl.SelectedIndex >= 0 && _appliedKey / 10 != 2)
            {
                var stepLine = new FlowLayoutPanel { Height = 34, BackColor = Color.Transparent, Dock = DockStyle.Top };
                var stepTxt = MUi.Txt("Step " + (tabControl.SelectedIndex + 1) + " of " + tabControl.TabPages.Count +
                    " - " + StepName(tabControl.SelectedIndex), 9F, FontStyle.Regular, UiTheme.Muted);
                stepTxt.Font = Px(17f);
                stepLine.Controls.Add(stepTxt);
                items.Add(stepLine);
            }

            items.Add(RailCap("Outstanding"));
            var issues = new List<RuleIssue>();
            for (int s = 0; s <= 2; s++)
                foreach (StepIssue i in IssuesForStep(s))
                    issues.Add(new RuleIssue(RuleSeverity.Blocking, "DEATH_" + s, i.Message, StepName(s)));

            // The list takes what is left of the rail after the fixed rows above and the banner below.
            int listH = Math.Max(120, Math.Min(300, _railPanel.ClientSize.Height - 400));
            var issueList = new IssueList { Large = true, Height = listH, Dock = DockStyle.Top };
            issueList.FixRequested += tabName => { int idx = TabIndexByText(tabName); if (idx >= 0) { tabControl.SelectedIndex = idx; UpdateStepNavigation(); } };
            issueList.SetIssues(issues, "Every required entry is filled in.");

            var banner = new Banner { Large = true };
            if (issues.Count == 0) banner.Set(RuleSeverity.Info, "Ready to save.", "All required entries are complete.", true);
            else banner.Set(RuleSeverity.Blocking, issues.Count + " REQUIRED ENTR" + (issues.Count == 1 ? "Y" : "IES") + " MISSING",
                _editingId == null ? "A new registration cannot be saved until these are filled." : "Listed for your information.");
            // The verdict sits ABOVE the list so it is never scrolled out of sight on a short screen.
            items.Add(banner);
            items.Add(issueList);

            // Bottom-first, so Dock=Top lays them out top-to-bottom (the codebase convention).
            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; _railPanel.Controls.Add(items[i]); }
            _railPanel.ResumeLayout();
            UpdatePrintVisibility();
            UpdateFooterState();
        }

        /// <summary>
        /// The certificate / acknowledgment print buttons only appear once the registration
        /// is DONE: the record is saved and every required entry on steps 1-3 is filled.
        /// Hidden in the list view, on an unsaved form, and while anything is outstanding.
        /// </summary>
        private void UpdatePrintVisibility()
        {
            // _entryDialog, not Control.Visible: Visible is EFFECTIVE visibility and reads false
            // for anything inside a dialog that has not been shown yet.
            bool inEntry = _entryDialog != null;
            bool done = inEntry && _editingId != null && FirstIncompleteStepBefore(3) < 0;
            btnCertificate.Visible = done;
            btnAckSlip.Visible = done;
        }

        private int TabIndexByText(string text)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
                if (string.Equals(StepName(i), text, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // ---------------------------------------------------------------- automatic age

        /// <summary>
        /// Age at death from the date of birth and the date of death. Ticking Date of Birth
        /// makes the age READ-ONLY and computed - years completed on the day of death, with
        /// the months and days shown beside it for a child under one. With no date of birth
        /// (the sheet often states only an age) the box stays typeable.
        /// <para/>
        /// Deliberately one-way: the age never back-fills a Date of Birth. "Aged 30" fixes a
        /// year, not a day, so deriving a birth date from it would put a date on a legal
        /// record that nothing on the certificate states.
        /// </summary>
        private void ApplyAgeMode() { RecomputeAge(false); }

        private void RecomputeAge(bool writeAge = true)
        {
            if (_suspendAge) return;
            if (!dtpDob.Checked)
            {
                txtAge.ReadOnly = false;
                txtAge.BackColor = SystemColors.Window;
                lblAgeNote.ForeColor = UiTheme.Muted;
                lblAgeNote.Text = "Date of birth not known - type the age stated on the certificate. " +
                                  "Tick Date of Birth to compute it automatically.";
                return;
            }

            txtAge.ReadOnly = true;
            txtAge.BackColor = Color.FromArgb(241, 243, 247);
            DateTime dob = dtpDob.Value.Date, dod = dtpDod.Value.Date;
            if (dob > dod)
            {
                if (writeAge) txtAge.Text = "";
                lblAgeNote.ForeColor = UiTheme.Danger;
                lblAgeNote.Text = "Date of birth is after the date of death - check both dates.";
                return;
            }

            int years = dod.Year - dob.Year;
            if (dod < dob.AddYears(years)) years--;
            DateTime afterYears = dob.AddYears(years);
            int months = 0;
            while (afterYears.AddMonths(months + 1) <= dod) months++;
            int days = (dod - afterYears.AddMonths(months)).Days;

            // Loading a saved record keeps the age it was saved with; the note says so if the
            // dates now disagree with it, and the next change to either date recomputes it.
            string kept = "";
            if (!writeAge && (txtAge.Text ?? "").Trim() != years.ToString())
                kept = "  (Saved age " + ((txtAge.Text ?? "").Trim().Length == 0 ? "blank" : txtAge.Text.Trim()) +
                       " differs - change a date to recompute.)";
            if (writeAge) txtAge.Text = years.ToString();
            lblAgeNote.ForeColor = kept.Length > 0 ? UiTheme.Warning : UiTheme.Muted;
            lblAgeNote.Text = "Computed from the dates: " + years + (years == 1 ? " year, " : " years, ") +
                months + (months == 1 ? " month, " : " months, ") + days + (days == 1 ? " day." : " days.") + kept;
        }
    }
}
