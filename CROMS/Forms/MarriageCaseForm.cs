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
    /// WINDOW 7 - the delayed / licence-exempt case workflow. Opened from Form 97 whenever the
    /// certificate is licence-exempt (Family Code Arts. 27-34) or reached the office after the
    /// reporting period (15 days, 30 for exempt - Arts. 23, 30). Both routes end in registrar
    /// review; neither is ever handled by inventing a licence number.
    /// </summary>
    internal sealed class MarriageCaseForm : Form
    {
        private readonly int _id;
        private MarriageFacts _m;
        private readonly MarriageSettings _s = MarriageService.Settings;
        private readonly Panel _body = new Panel();
        private readonly ComboBox _basis = MUi.Combo(false);
        private readonly TextBox _basisNotes = MUi.Box(), _delayReason = MUi.Box(), _reviewNotes = MUi.Box();
        private readonly DateTimePicker _postStart = MUi.Date(false);
        private readonly RequirementsGrid _docs = new RequirementsGrid();
        private readonly IssueList _issues = new IssueList();

        public MarriageCaseForm(int marriageId)
        {
            _id = marriageId;
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
            _docs.Changed += () => Rebuild();
            _issues.FixRequested += w => { if (w != "Case Workflow") Close(); };
            // Opening focused the posting date picker and scrolled the panel to it, so the
            // case summary at the top was off-screen on arrival.
            Shown += (s, e) => { ActiveControl = null; _body.AutoScrollPosition = Point.Empty; };
            Rebuild();
        }

        private static void Stack(Control host, List<Control> items)
        {
            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; host.Controls.Add(items[i]); }
        }

        private static Control H(string t, string sub) { return MUi.SectionHeader(t, sub); }

        private void Rebuild()
        {
            _m = MarriageService.LoadMarriageFacts(_id);
            bool exempt = _m.Basis == "Exempt", delayed = MarriageRules.WouldBeDelayed(_m, _s);
            _body.SuspendLayout();
            foreach (Control c in _body.Controls.Cast<Control>().ToList()) { if (c != _docs && c != _issues && !(c is TextBox) && !(c is ComboBox)) c.Dispose(); }
            _body.Controls.Clear();
            var items = new List<Control>();

            var intro = new Banner();
            if (!exempt && !delayed)
                intro.Set(RuleSeverity.Info, "No case workflow applies.", "This is a timely registration under a licence. Nothing here is needed.", true);
            else
                intro.Set(RuleSeverity.Warning, (exempt && delayed ? "Licence-exempt AND delayed" : exempt ? "Licence-exempt marriage" : "Delayed registration"),
                    "Registrar review is required before this certificate can be registered. CROMS records the basis and the evidence; the decision is the registrar's.");
            items.Add(intro);

            if (exempt)
            {
                items.Add(H("License exemption", "The legal basis for solemnizing without a licence. No licence number is created."));
                _basis.SelectedIndex = -1;
                for (int i = 0; i < MarriageRules.ExemptionBases.GetLength(0); i++)
                    if (MarriageRules.ExemptionBases[i, 0] == _m.ExemptionBasis) _basis.SelectedIndex = i;
                TableLayoutPanel g = MUi.Grid(2, 1, 58);
                g.Controls.Add(MUi.Field("Basis", _basis), 0, 0);
                g.Controls.Add(MUi.Field("Notes (e.g. where the affidavit was executed)", _basisNotes), 1, 0);
                items.Add(g);
                var save = MUi.Btn("Save basis", MUi.Kind.Secondary, 120);
                save.Click += (s, e) => Save(new Dictionary<string, object>
                {
                    { "exemption_basis", _basis.SelectedIndex >= 0 ? MarriageRules.ExemptionBases[_basis.SelectedIndex, 0] : null },
                    { "exemption_notes", string.IsNullOrWhiteSpace(_basisNotes.Text) ? null : _basisNotes.Text.Trim() }
                });
                items.Add(Row(save));
            }

            if (delayed)
            {
                DateTime deadline = MarriageRules.ReportingDeadline(_m.DateOfMarriage.Value, exempt, _s);
                items.Add(H("Delayed registration", "Married " + MUi.D(_m.DateOfMarriage) + "; the certificate was due by " + MUi.D(deadline) +
                             " and was received " + MUi.D(_m.DateReceived) + " (" + (_m.DateReceived.Value - deadline).Days + " day(s) late)."));
                _delayReason.Text = _m.DelayReason;
                TableLayoutPanel g = MUi.Grid(1, 1, 58);
                g.Controls.Add(MUi.Field("Reason for the delay (from the affidavit)", _delayReason), 0, 0);
                items.Add(g);
                var save = MUi.Btn("Save reason", MUi.Kind.Secondary, 120);
                save.Click += (s, e) => Save(new Dictionary<string, object> { { "delay_reason", string.IsNullOrWhiteSpace(_delayReason.Text) ? null : _delayReason.Text.Trim() } });
                items.Add(Row(save));

                var post = new Banner();
                if (_m.CasePostingStart.HasValue)
                {
                    DateTime end = _m.CasePostingStart.Value.AddDays(_s.DelayedPostingDays - 1);
                    bool done = DateTime.Today > end;
                    post.Set(done ? RuleSeverity.Info : RuleSeverity.Warning,
                        "Notice of delayed registration posted " + MUi.D(_m.CasePostingStart) + " - " + MUi.D(end),
                        done ? "Posting complete." : "Registration is possible from " + MUi.D(end.AddDays(1)) + ".", done);
                    items.Add(post);
                }
                else
                {
                    post.Set(RuleSeverity.Warning, "Notice not yet posted", "Post the notice of the pending delayed registration for " + _s.DelayedPostingDays + " days (setting MARRIAGE_DELAYED_POSTING_DAYS).");
                    items.Add(post);
                    _postStart.Value = DateTime.Today; _postStart.MaxDate = DateTime.Today;
                    TableLayoutPanel pg = MUi.Grid(3, 1, 58);
                    pg.Controls.Add(MUi.Field("Posted on", _postStart), 0, 0);
                    items.Add(pg);
                    var b = MUi.Btn("Record posting", MUi.Kind.Primary, 150);
                    b.Click += (s, e) =>
                    {
                        try { MarriageService.StartCasePosting(_id, _postStart.Value.Date); Rebuild(); } catch (Exception ex) { MUi.Fail(this, ex); }
                    };
                    items.Add(Row(b));
                }
            }

            items.Add(H("Supporting documents", "Affidavits and evidence the case rests on. Mark each Verified once checked."));
            List<Need> needs = MarriageRules.Needs(_m.Husband, _m.Wife, _m.DateOfMarriage ?? DateTime.Today, MarriageService.Catalog(), "Marriage", _s, exempt, delayed);
            _docs.Bind("Marriage", _id, needs);
            _docs.Height = Math.Max(90, _docs.PreferredHeight);
            items.Add(_docs);

            if (exempt || delayed)
            {
                items.Add(H("Registrar review", null));
                var rp = new FlowLayoutPanel { Height = 34, BackColor = Color.Transparent };
                rp.Controls.Add(MUi.Txt("Status", 9F, FontStyle.Regular, UiTheme.Muted));
                rp.Controls.Add(MUi.Pill(_m.RegistrarReview ?? "Pending", _m.RegistrarReview ?? "Pending"));
                items.Add(rp);
                TableLayoutPanel g = MUi.Grid(1, 1, 58);
                g.Controls.Add(MUi.Field("Registrar's notes", _reviewNotes), 0, 0);
                items.Add(g);
                var approve = MUi.Btn("Approve for registration", MUi.Kind.Success, 200);
                var ret = MUi.Btn("Return", MUi.Kind.Secondary, 100);
                approve.Enabled = ret.Enabled = MarriageService.IsRegistrar;
                approve.Click += (s, e) => Review(true);
                ret.Click += (s, e) => Review(false);
                var row = Row(approve); row.Controls.Add(ret);
                if (!MarriageService.IsRegistrar) row.Controls.Add(MUi.Txt("Only a Registrar or Admin can decide the case.", 9F, FontStyle.Regular, UiTheme.Muted));
                items.Add(row);
            }

            items.Add(H("Open checks for this case", null));
            List<RuleIssue> all = MarriageService.ValidateMarriage(_id);
            _issues.Height = 150;
            _issues.SetIssues(all.Where(i => i.FixWhere == "Case Workflow" || i.Code.StartsWith("MREQ_")), "Nothing outstanding in the case workflow.");
            items.Add(_issues);
            Stack(_body, items);
            _body.ResumeLayout();
            UiTheme.Polish(this);
        }

        private static FlowLayoutPanel Row(params Control[] c)
        {
            var f = new FlowLayoutPanel { Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
            f.Controls.AddRange(c);
            return f;
        }

        private void Save(Dictionary<string, object> values)
        {
            try { MarriageService.SaveMarriage(_id, values); Rebuild(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void Review(bool approve)
        {
            try
            {
                if (approve && !MUi.Confirm(this, "Approve", "Approve this case for registration?",
                        "Parties|" + _m.Husband.FullName + " & " + _m.Wife.FullName, "Married|" + MUi.D(_m.DateOfMarriage)))
                    return;
                MarriageService.RegistrarReview(_id, approve, string.IsNullOrWhiteSpace(_reviewNotes.Text) ? null : _reviewNotes.Text.Trim());
                Rebuild();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }
    }
}
