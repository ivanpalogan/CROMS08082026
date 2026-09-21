using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// WINDOW 2 - Form 90, the marriage licence application. Five steps that follow the
    /// paper's own blocks (Applicants / Requirements / Consent &amp; Advice / Posting / Issue).
    /// The clerk can jump between them; the SEQUENCE is enforced where the law makes it real
    /// (posting before issue, issue not before the earliest date) and nowhere else.
    ///
    /// The form derives the obligations; the clerk supplies the facts. Ages come from dates
    /// of birth, posting and validity dates are computed, and every unavailable action says
    /// why beside it.
    /// </summary>
    internal sealed partial class MarriageLicenseForm : Form
    {
        private sealed class PartyBox
        {
            public TextBox First, Middle, Last, Residence;
            public DateTimePicker Dob;
            public Label Age;
            public ComboBox Cit, Civil, Religion, Sex;
            // Place of birth: country + province + city/municipality, not one free textbox.
            public ComboBox Country, Province, Municipality;

            // Form 90's parent, consent/advice and previously-married blocks (migration 38).
            // Names are three cells, as the form prints them - never one joined box.
            public TextBox FFirst, FMiddle, FLast, FRes, MFirst, MMiddle, MLast, MRes, CFirst, CMiddle, CLast, CRes;
            public ComboBox FCit, MCit, CCit, CRel;
            public ComboBox PrevHow, PrevProvince, PrevMunicipality;
            public DateTimePicker PrevDate;
            /// <summary>The two rows of the previously-married block, greyed together.</summary>
            public Control[] PrevRows;
            public Label PrevNote;

            /// <summary>Every input in the column, for the read-only switch.</summary>
            public Control[] Inputs
            {
                get
                {
                    return new Control[] { First, Middle, Last, Dob, Sex, Country, Province, Municipality, Residence, Cit, Civil, Religion,
                                           FFirst, FMiddle, FLast, FCit, FRes, MFirst, MMiddle, MLast, MCit, MRes,
                                           CFirst, CMiddle, CLast, CRel, CCit, CRes };
                }
            }
        }

        private List<string> _nationalities;
        private int _partyHeight;

        private LicenseFacts _l = new LicenseFacts { FiledDate = DateTime.Today };
        private List<ReqType> _catalog;
        private readonly MarriageSettings _s = MarriageService.Settings;
        private bool _dirty, _loading, _readOnly;
        private int _step;

        // page 1
        private readonly PartyBox _h = new PartyBox(), _w = new PartyBox();

        private static readonly string[] StepNames = { "Applicants", "Requirements", "Consent & Advice", "Posting", "Issue License" };

        public MarriageLicenseForm(int? licenseId)
        {
            try { _catalog = MarriageService.Catalog(); } catch { _catalog = new List<ReqType>(); }
            // Six citizenship boxes per applicant pair now read this list; load it once.
            _nationalities = Lookup("nationalities").ToList();
            InitializeComponent();

            if (licenseId.HasValue)
            {
                _l = MarriageService.LoadLicense(licenseId.Value) ?? _l;
                Fill();
            }
            else { _loading = true; _filed.Value = DateTime.Today; _postStart.Value = DateTime.Today; _loading = false; }
            RefreshAll();
            ShowStep(licenseId.HasValue ? SuggestedStep() : 0);
            FormClosing += OnClosingAsk;
        }

        /// <summary>
        /// "If previously married" applies only to a Widowed / Annulled / Divorced party. Otherwise
        /// the block is GREYED and CLEARED, not merely left blank: a blank box reads as one nobody
        /// filled in, a greyed one says the question does not apply (Birth's parents-married
        /// toggle, 2026-09-10). Cleared because a disabled box still holds its text and ReadParty
        /// would carry it onto the record; SaveLicense forces NULL as well.
        /// </summary>
        private void ApplyPrevMarried(PartyBox p)
        {
            string civil = p.Civil.SelectedItem as string;
            bool applies = MarriageRules.IsPreviouslyMarried(civil);
            foreach (Control row in p.PrevRows) row.Enabled = applies && !_readOnly;
            if (!applies)
            {
                if (!string.IsNullOrEmpty(p.PrevHow.Text)) { p.PrevHow.SelectedIndex = -1; p.PrevHow.Text = ""; }
                if (!string.IsNullOrEmpty(p.PrevProvince.Text)) GeoLookup.Select(p.PrevProvince, null);
                if (!string.IsNullOrEmpty(p.PrevMunicipality.Text)) GeoLookup.Select(p.PrevMunicipality, null);
                if (MUi.Val(p.PrevDate).HasValue) MUi.Put(p.PrevDate, null);
            }
            p.PrevNote.ForeColor = applies ? UiTheme.Muted : UiTheme.Faint;
            p.PrevNote.Text = applies ? civil + " - state how, where and when the previous marriage ended."
                : civil == null ? "Applies when civil status is Widowed, Annulled or Divorced."
                : "Does not apply - civil status is " + civil + ".";
        }

        private static IEnumerable<string> Lookup(string table)
        {
            try { return Db.Pull("SELECT name FROM " + table + " ORDER BY name").AsEnumerable().Select(r => r[0].ToString()).ToList(); }
            catch { return new string[0]; }
        }

        /// <summary>Usable width of a step page. Read from the FORM, not the page: a hidden page reports its default size.</summary>
        private int PageWidth { get { return Math.Max(420, ClientSize.Width - 320 - 64); } }

        // ================================================================= data
        private void Fill()
        {
            _loading = true;
            MUi.Put(_filed, _l.FiledDate);
            _remarks.Text = _l.Remarks;
            FillParty(_h, _l.Husband); FillParty(_w, _l.Wife);
            _orNo.Text = _l.PaymentOr; _orAmt.Text = _l.PaymentAmount.HasValue ? _l.PaymentAmount.Value.ToString("0.00") : "";
            MUi.Put(_orDate, _l.PaymentDate);
            _finding.Text = _l.ImpedimentNote;
            _postStart.Value = _l.PostingStart ?? DateTime.Today;
            _loading = false;
            _dirty = false;
        }

        private static void FillParty(PartyBox b, Party p)
        {
            b.First.Text = p.First; b.Middle.Text = p.Middle; b.Last.Text = p.Last;
            MUi.Put(b.Dob, p.Dob);
            b.Residence.Text = p.Residence;
            b.FFirst.Text = p.FatherFirst; b.FMiddle.Text = p.FatherMiddle; b.FLast.Text = p.FatherLast;
            b.FCit.Text = p.FatherCitizenship ?? ""; b.FRes.Text = p.FatherResidence;
            b.MFirst.Text = p.MotherFirst; b.MMiddle.Text = p.MotherMiddle; b.MLast.Text = p.MotherLast;
            b.MCit.Text = p.MotherCitizenship ?? ""; b.MRes.Text = p.MotherResidence;
            b.CFirst.Text = p.ConsentFirst; b.CMiddle.Text = p.ConsentMiddle; b.CLast.Text = p.ConsentLast;
            b.CRel.Text = p.ConsentRelationship ?? ""; b.CCit.Text = p.ConsentCitizenship ?? ""; b.CRes.Text = p.ConsentResidence;
            // Country first - it rebuilds the province list the next two select into.
            GeoLookup.SetCountryPlace(b.Country, b.Province, b.Municipality,
                                      p.BirthCountry, ProvinceOf(p.PlaceOfBirth), MunicipalityOf(p.PlaceOfBirth));
            b.Cit.Text = p.Citizenship ?? ""; b.Religion.Text = p.Religion ?? "";
            b.Civil.SelectedItem = MarriageRules.CivilStatuses.Contains(p.CivilStatus) ? p.CivilStatus : null;
            // Licences filed before this field existed hold no sex; show the column's own value
            // rather than an empty box the clerk has to notice.
            b.Sex.SelectedItem = MarriageRules.Sexes.Contains(p.Sex) ? p.Sex : p.ExpectedSex;
            // AFTER civil status: choosing it greys and clears this block when it does not apply,
            // so values put in first would be wiped by that change.
            b.PrevHow.Text = p.PrevDissolution ?? "";
            GeoLookup.Select(b.PrevProvince, p.PrevDissolvedProvince);   // rebuilds the city list
            GeoLookup.Select(b.PrevMunicipality, p.PrevDissolvedMunicipality);
            MUi.Put(b.PrevDate, p.PrevDissolvedDate);
        }

        private static void ReadParty(PartyBox b, Party p)
        {
            p.First = N(b.First.Text); p.Middle = N(b.Middle.Text); p.Last = N(b.Last.Text);
            p.Dob = MUi.Val(b.Dob);
            p.PlaceOfBirth = JoinPlace(b.Municipality.Text, b.Province.Text);
            p.BirthCountry = N(b.Country.Text);
            p.Residence = N(b.Residence.Text);
            p.Citizenship = N(b.Cit.Text); p.Religion = N(b.Religion.Text); p.CivilStatus = b.Civil.SelectedItem as string;
            p.Sex = b.Sex.SelectedItem as string;

            p.FatherFirst = N(b.FFirst.Text); p.FatherMiddle = N(b.FMiddle.Text); p.FatherLast = N(b.FLast.Text);
            p.FatherCitizenship = N(b.FCit.Text); p.FatherResidence = N(b.FRes.Text);
            p.MotherFirst = N(b.MFirst.Text); p.MotherMiddle = N(b.MMiddle.Text); p.MotherLast = N(b.MLast.Text);
            p.MotherCitizenship = N(b.MCit.Text); p.MotherResidence = N(b.MRes.Text);
            p.ConsentFirst = N(b.CFirst.Text); p.ConsentMiddle = N(b.CMiddle.Text); p.ConsentLast = N(b.CLast.Text);
            p.ConsentRelationship = N(b.CRel.Text); p.ConsentCitizenship = N(b.CCit.Text); p.ConsentResidence = N(b.CRes.Text);
            // Joined only for what reads one string (Form 97's copy, the printed licence).
            p.Father = MarriageRules.JoinName(p.FatherFirst, p.FatherMiddle, p.FatherLast);
            p.Mother = MarriageRules.JoinName(p.MotherFirst, p.MotherMiddle, p.MotherLast);

            if (MarriageRules.IsPreviouslyMarried(p.CivilStatus))
            {
                p.PrevDissolution = N(b.PrevHow.Text);
                p.PrevDissolvedProvince = N(b.PrevProvince.Text); p.PrevDissolvedMunicipality = N(b.PrevMunicipality.Text);
                p.PrevDissolvedDate = MUi.Val(b.PrevDate);
            }
            else p.ClearPrevMarriage();
        }

        private static string N(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }

        // JoinPlace/ProvinceOf/MunicipalityOf moved to GeoLookup (shared with MarriageEntryForm,
        // which needs the identical join/split for Form 97's place-of-birth on 2026-09-14).
        private static string JoinPlace(string municipality, string province) { return GeoLookup.JoinPlace(municipality, province); }
        private static string ProvinceOf(string place) { return GeoLookup.ProvinceOf(place); }
        private static string MunicipalityOf(string place) { return GeoLookup.MunicipalityOf(place); }

        /// <summary>The application as it stands on screen, unsaved edits included.</summary>
        private LicenseFacts Current()
        {
            ReadParty(_h, _l.Husband); ReadParty(_w, _l.Wife);
            _l.FiledDate = MUi.Val(_filed) ?? DateTime.Today;
            _l.Remarks = N(_remarks.Text);
            return _l;
        }

        private void Touched()
        {
            if (_loading) return;
            _dirty = true;
            RefreshAges();
            RefreshFooter();
        }

        private bool SaveDraft(bool explicitSave)
        {
            if (_readOnly) return true;
            Current();
            if (string.IsNullOrWhiteSpace(_l.Husband.Last) && string.IsNullOrWhiteSpace(_l.Wife.Last))
            {
                if (explicitSave)
                    MessageBox.Show(this, "Type at least one applicant's last name before saving the draft.", "Nothing to save",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                MarriageService.SaveLicense(_l);
                _l = MarriageService.LoadLicense(_l.Id);
                _dirty = false;
                RefreshAll();
                return true;
            }
            catch (Exception ex) { MUi.Fail(this, ex); return false; }
        }

        private void Reload()
        {
            if (_l.Id <= 0) return;
            _l = MarriageService.LoadLicense(_l.Id);
            Fill();
            RefreshAll();
        }

        // ================================================================= refresh
        private List<Need> Needs()
        {
            LicenseFacts l = Current();
            return MarriageRules.Needs(l.Husband, l.Wife, l.FiledDate ?? DateTime.Today, _catalog, "License", _s);
        }

        private void RefreshAll()
        {
            _readOnly = new[] { "Issued", "Used", "Expired", "Cancelled" }.Contains(_l.StoredStatus);
            List<Need> needs = Needs();
            if (_l.Id > 0)
            {
                _l.Requirements = MarriageService.Requirements("License", _l.Id);
                _docs.ReadOnlyGrid = _readOnly; _consent.ReadOnlyGrid = _readOnly;
                _docs.Bind("License", _l.Id, needs, r => !IsConsentCode(r.Code));
                _consent.Bind("License", _l.Id, needs, r => IsConsentCode(r.Code));
            }
            _docs.Height = Math.Max(120, _docs.PreferredHeight);
            _consent.Height = Math.Max(90, _consent.PreferredHeight);
            foreach (Control c in _h.Inputs.Concat(_w.Inputs).Concat(new Control[] { _filed, _remarks, _orNo, _orAmt, _orDate }))
                c.Enabled = !_readOnly;
            // The previously-married rows answer to civil status as well as to read-only.
            ApplyPrevMarried(_h); ApplyPrevMarried(_w);

            string display = _l.Id > 0 ? MarriageRules.LicenseDisplayStatus(_l, DateTime.Today, _s, MarriageService.OutstandingBlocking(_l, _catalog)) : "New";
            MUi.SetPill(_appPill, _l.ApplicationNo ?? "NEW APPLICATION", "Draft");
            MUi.SetPill(_statusPill, display.ToUpperInvariant(), display);
            _appPill.Left = _title.Right + 14; _statusPill.Left = _appPill.Right + 8;

            RefreshAges();
            RefreshConsentNotes();
            RefreshPayment();
            RefreshPosting();
            RefreshIssue();
            RefreshRail(needs, display);
            RefreshSteps();
            RefreshFooter();
        }

        private static bool IsConsentCode(string c) { return c == "PARENTAL_CONSENT" || c == "PARENTAL_ADVICE" || c == "COUNSELING"; }

        private void RefreshAges()
        {
            LicenseFacts l = Current();
            DateTime on = l.FiledDate ?? DateTime.Today;
            foreach (var pair in new[] { Tuple.Create(_h, l.Husband), Tuple.Create(_w, l.Wife) })
            {
                Party p = pair.Item2;
                if (p.Dob.HasValue)
                {
                    int a = MarriageRules.AgeOn(p.Dob.Value, on);
                    pair.Item1.Age.Text = a + " years";
                    pair.Item1.Age.ForeColor = a < 18 ? UiTheme.Danger : a <= _s.AdviceAgeTo ? UiTheme.Warning : UiTheme.Ink;
                }
                else { pair.Item1.Age.Text = "- enter date of birth"; pair.Item1.Age.ForeColor = UiTheme.Faint; }
            }
            FillNotes(_ageNotes, l, on, includeInfo: true);
        }

        private void FillNotes(FlowLayoutPanel host, LicenseFacts l, DateTime on, bool includeInfo)
        {
            host.SuspendLayout();
            foreach (Control c in host.Controls.Cast<Control>().ToList()) c.Dispose();
            host.Controls.Clear();
            int w = PageWidth;
            var findings = new List<RuleIssue>();
            foreach (Party p in new[] { l.Husband, l.Wife })
                findings.AddRange(MarriageRules.AgeFindings(p, on, _s, "Applicants").Where(f => f.Code != "DOB_MISSING" || l.Id > 0));
            findings.AddRange(MarriageRules.ImpedimentIssues(l.Husband, l.Wife, l.ImpedimentNote, "Posting"));
            foreach (Party p in new[] { l.Husband, l.Wife })
            {
                if (MarriageRules.IsForeign(p.Citizenship))
                    findings.Add(new RuleIssue(RuleSeverity.Info, "FOREIGN", p.Called + " is a " + p.Citizenship + " citizen: a certificate of legal capacity to contract marriage from " +
                        (p.Role == "Wife" ? "her" : "his") + " embassy or consulate is required (Family Code Art. 21).", "Requirements"));
                if (MarriageRules.IsPreviouslyMarried(p.CivilStatus))
                    findings.Add(new RuleIssue(RuleSeverity.Info, "PREV", p.Called + " is " + p.CivilStatus.ToLowerInvariant() +
                        ": proof the previous marriage ended (death certificate, or the annulment / nullity decree) is required (Family Code Art. 13).", "Requirements"));
            }
            foreach (RuleIssue f in findings.Where(x => includeInfo || x.Severity != RuleSeverity.Info))
            {
                var b = new Banner { Width = w, Dock = DockStyle.None };
                string title = f.Severity == RuleSeverity.HardStop ? "Cannot proceed" : f.Severity == RuleSeverity.Info ? "Requirement triggered" : "Needs attention";
                b.Set(f.Severity == RuleSeverity.Info ? RuleSeverity.Warning : f.Severity, title, f.Message);
                host.Controls.Add(b);
            }
            host.ResumeLayout();
        }

        private void RefreshConsentNotes()
        {
            LicenseFacts l = Current();
            DateTime on = l.FiledDate ?? DateTime.Today;
            _consentNotes.SuspendLayout();
            foreach (Control c in _consentNotes.Controls.Cast<Control>().ToList()) c.Dispose();
            _consentNotes.Controls.Clear();
            int w = PageWidth;
            foreach (Party p in new[] { l.Husband, l.Wife })
            {
                var b = new Banner { Width = w, Dock = DockStyle.None };
                if (!p.Dob.HasValue) b.Set(RuleSeverity.Blocking, p.RoleLabel + ": date of birth missing", "Enter it on Applicants - CROMS works out consent and advice from it.");
                else
                {
                    int a = MarriageRules.AgeOn(p.Dob.Value, on);
                    if (a < 18) b.Set(RuleSeverity.HardStop, "Cannot proceed - " + p.Called + " is " + a, "A marriage where either party is under 18 is void and prohibited (RA 11596).");
                    else if (MarriageRules.InConsentBand(p, on, _s)) b.Set(RuleSeverity.Warning, "Parental consent is required for " + p.Called + ".",
                        p.Called + " is " + a + " on the filing date. Record who gave written consent (father, mother, surviving parent or guardian, in that order) - Family Code Art. 14.");
                    else if (MarriageRules.InAdviceBand(p, on, _s)) b.Set(RuleSeverity.Warning, "Parental advice is required for " + p.Called + ".",
                        p.Called + " is " + a + ". If the advice is unfavourable or not obtained, the licence cannot issue until three months after posting ends - Family Code Art. 15.");
                    else b.Set(RuleSeverity.Info, "No consent or advice required for " + p.Called + ".", p.Called + " is " + a + " on the filing date.", true);
                }
                _consentNotes.Controls.Add(b);
            }
            if (Needs().Any(n => n.Code == "COUNSELING"))
            {
                var c = new Banner { Width = w, Dock = DockStyle.None };
                c.Set(RuleSeverity.Info, "A marriage counselling certificate applies to the couple (Art. 16).",
                    "If it is not attached, mark it Waived: the law then suspends issue for three months after posting, and CROMS moves the earliest issue date.");
                _consentNotes.Controls.Add(c);
            }
            _consentNotes.ResumeLayout();
        }

        private void RefreshPayment()
        {
            if (!string.IsNullOrWhiteSpace(_l.PaymentOr))
                _payBanner.Set(RuleSeverity.Info, "Payment recorded - O.R. " + _l.PaymentOr,
                    (_l.PaymentAmount.HasValue ? "PHP " + _l.PaymentAmount.Value.ToString("#,0.00") : "") + (_l.PaymentDate.HasValue ? "  on " + MUi.D(_l.PaymentDate) : ""), true);
            else if (_s.LicensePaymentRequired)
                _payBanner.Set(RuleSeverity.Warning, "No payment recorded yet", "The licence cannot issue until the Treasury official receipt is recorded.");
            else _payBanner.Set(RuleSeverity.Info, "Payment is not required before issue on this office's settings.", null);
        }

        private void UpdatePostingPreview()
        {
            DateTime st = _postStart.Value.Date;
            DateTime end = MarriageRules.PostingEnd(st, _s);
            DateTime earliest = MarriageRules.EarliestIssue(st, MarriageRules.Deferral(_l.Requirements) != null, _s);
            _postPreview.Text = "Posting " + MUi.D(st) + " - " + MUi.D(end) + "    Earliest issue " + MUi.D(earliest);
        }

        private void RefreshPosting()
        {
            string st = _l.StoredStatus;
            bool draft = st == "Draft" || _l.Id <= 0;
            _startPosting.Visible = draft; _postStart.Enabled = draft && !_readOnly; _postPreview.Visible = true;
            // The notice goes up today - it cannot be dated yesterday or last year. Only while the
            // picker is still editable: an already-posted licence keeps its (past) stored date.
            if (draft && !_readOnly) _postStart.MinDate = DateTime.Today;
            _hold.Visible = st == "Posting" || st == "On Hold" || (st == "Draft" && _l.Id > 0);
            _hold.Text = st == "On Hold" ? "Release hold" : "Put on hold";
            _cancelApp.Visible = _l.Id > 0 && !new[] { "Used", "Cancelled", "Issued", "Expired" }.Contains(st);
            _cancelApp.Enabled = MarriageService.IsRegistrar;
            _bar.Visible = _l.PostingStart.HasValue;
            _bar.SetData(_l, _s);
            UpdatePostingPreview();

            _postDetail.Controls.Clear();
            if (_l.PostingStart.HasValue)
            {
                var kv = new List<Control>
                {
                    MUi.Kv("Posting", MUi.D(_l.PostingStart) + " - " + MUi.D(_l.PostingEnd)),
                    MUi.Kv("Day", MarriageRules.PostingDay(_l, DateTime.Today, _s) + " of " + _s.PostingDays),
                    MUi.Kv("Earliest issue", MUi.D(_l.EarliestIssue), _l.DeferralReason != null ? UiTheme.Warning : (Color?)null),
                    MUi.Kv("If issued that day, valid until", _l.EarliestIssue.HasValue ? MUi.D(MarriageRules.Expiry(_l.EarliestIssue.Value, _s)) : "-"),
                };
                if (_l.DeferralReason != null) kv.Add(MUi.Kv("Deferred because", _l.DeferralReason, UiTheme.Warning));
                Stack(_postDetail, kv.ToArray());
                _postDetail.Height = kv.Count * 26 + 8;
            }
            else _postDetail.Height = 0;

            if (_l.Id <= 0) _postBanner.Set(RuleSeverity.Info, "Save the application first.", "Posting starts from a saved application.");
            else if (st == "Draft")
            {
                List<RuleIssue> pi = MarriageRules.ValidateForPosting(Current(), _s).Where(i => i.Blocks).ToList();
                if (pi.Count > 0) _postBanner.Set(pi.Any(i => i.Severity == RuleSeverity.HardStop) ? RuleSeverity.HardStop : RuleSeverity.Blocking,
                    "Posting cannot start yet", string.Join("\n", pi.Select(i => "- " + i.Message)));
                else _postBanner.Set(RuleSeverity.Info, "Ready to post.",
                    "Confirm the date the notice goes up on the bulletin board. Documents still outstanding may be completed during posting.");
                _startPosting.Enabled = pi.Count == 0 && !_readOnly;
            }
            else if (st == "Posting")
            {
                bool done = _l.EarliestIssue.HasValue && DateTime.Today >= _l.EarliestIssue.Value;
                if (done) _postBanner.Set(RuleSeverity.Info, "Posting completed. This application is now ready for final review.",
                    "CROMS does not issue the licence automatically - open Issue License when the final check passes.", true);
                else _postBanner.Set(RuleSeverity.Warning, "Posting in progress - day " + MarriageRules.PostingDay(_l, DateTime.Today, _s) + " of " + _s.PostingDays,
                    "The licence can issue from " + MUi.D(_l.EarliestIssue) + (_l.DeferralReason != null ? " (deferred: " + _l.DeferralReason + ")." : "."));
            }
            else if (st == "On Hold") _postBanner.Set(RuleSeverity.Blocking, "On hold", _l.HoldReason);
            else if (st == "Cancelled") _postBanner.Set(RuleSeverity.Blocking, "Application cancelled", _l.CancelReason);
            else _postBanner.Set(RuleSeverity.Info, "Posting completed " + MUi.D(_l.PostingEnd) + ".", null, true);

            bool impediment = MarriageRules.HasSubsistingMarriage(_l.Husband.CivilStatus) || MarriageRules.HasSubsistingMarriage(_l.Wife.CivilStatus);
            _findingPanel.Visible = impediment && _l.Id > 0;
            _finding.Enabled = MarriageService.IsRegistrar && !_readOnly;
            _saveFinding.Enabled = MarriageService.IsRegistrar && !_readOnly;
        }

        /// <summary>Every blocking issue, minus requirement/attachment issues an Admin has overridden.</summary>
        private List<RuleIssue> IssueIssues()
        {
            if (_l.Id <= 0) return new List<RuleIssue> { new RuleIssue(RuleSeverity.Blocking, "UNSAVED", "Save the application first.", "Applicants") };
            List<RuleIssue> issues = MarriageRules.ValidateForIssue(Current(), _catalog, DateTime.Today, _s).Where(i => i.Blocks).ToList();
            return MarriageRules.ApplyOverride(issues, _l);
        }

        /// <summary>The same issues, WITHOUT the override applied - used only to decide whether
        /// the Admin Override button has anything to offer (there is no point overriding when
        /// nothing is actually missing, and payment can never be overridden so it alone doesn't
        /// count).</summary>
        private bool HasOverridableIssues()
        {
            if (_l.Id <= 0) return false;
            return MarriageRules.ValidateForIssue(Current(), _catalog, DateTime.Today, _s).Where(i => i.Blocks).Any(i => i.Code != "PAYMENT");
        }

        private void DoAdminOverride()
        {
            if (_l.RequirementsOverrideBy.HasValue)
            {
                if (!MUi.Confirm(this, "Withdraw override", "Withdraw the requirements override on this application?",
                        "Reason on file|" + _l.RequirementsOverrideReason)) return;
                try { MarriageService.ClearRequirementsOverride(_l.Id); Reload(); }
                catch (Exception ex) { MUi.Fail(this, ex); }
                return;
            }

            List<RuleIssue> toBypass = MarriageRules.ValidateForIssue(Current(), _catalog, DateTime.Today, _s)
                .Where(i => i.Blocks && i.Code != "PAYMENT").ToList();
            if (toBypass.Count == 0)
            {
                MessageBox.Show(this, "Nothing to override - the only outstanding item is payment, and that can never be bypassed.",
                    "Admin Override", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string reason = MUi.AskWithChecklist(this, "Admin Override",
                "This will let the licence issue with the following still unresolved:",
                toBypass.Select(i => i.Message),
                "Issuing anyway is an Admin decision and will be permanently recorded on the application and in the audit trail.\n\n" +
                "Payment can NEVER be bypassed by this override - it will still be required.",
                "Proceed With Override");
            if (reason == null) return;
            try { MarriageService.OverrideRequirements(_l.Id, reason); Reload(); }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void RefreshIssue()
        {
            _checklist.SuspendLayout();
            foreach (Control c in _checklist.Controls.Cast<Control>().ToList()) c.Dispose();
            _checklist.Controls.Clear();
            _issuedPanel.Controls.Clear();

            if (_l.IssueDate.HasValue)
            {
                string disp = MarriageRules.LicenseDisplayStatus(_l, DateTime.Today, _s, 0);
                int left = MarriageRules.DaysRemaining(_l, DateTime.Today);
                var ban = new Banner();
                ban.Set(disp == "Expired" ? RuleSeverity.Blocking : disp == "Expiring" ? RuleSeverity.Warning : RuleSeverity.Info,
                    "LICENSE " + (disp == "Used" ? "USED - MARRIAGE REGISTERED" : disp.ToUpperInvariant()) + "  -  " + _l.LicenseNo,
                    "Issued " + MUi.D(_l.IssueDate) + ", valid until " + MUi.D(_l.ExpiryDate) +
                    (disp == "Valid" || disp == "Expiring" ? "  -  " + left + " day(s) remaining" : "") +
                    (_l.UsedByMarriageId.HasValue ? "\nCertificate of Marriage: " + (_l.UsedByRegistryNo ?? "draft #" + _l.UsedByMarriageId) : "\nAwaiting the Certificate of Marriage."),
                    disp == "Valid" || disp == "Used");
                var print = MUi.Btn("Print license", MUi.Kind.Secondary, 130);
                print.Click += (s, e) => LicensePrinter.Show(this, _l, false);
                var fp = new FlowLayoutPanel { Height = 44, BackColor = Color.Transparent };
                fp.Controls.Add(print);
                Stack(_issuedPanel, ban, fp);
                _issueList.Visible = false;
                _checklist.ResumeLayout();
                return;
            }

            List<RuleIssue> issues = IssueIssues();
            Func<Func<RuleIssue, bool>, string, string, Control> line = (pred, ok, label) =>
            {
                RuleIssue first = issues.FirstOrDefault(pred);
                var l = MUi.Txt((first == null ? "✓  " : "✕  ") + label + (first == null ? "" : " - " + first.Message), 9.5F,
                    first == null ? FontStyle.Bold : FontStyle.Regular, first == null ? UiTheme.Success : UiTheme.Danger);
                l.MaximumSize = new Size(PageWidth, 0);
                l.Margin = new Padding(0, 3, 0, 3);
                return l;
            };
            _checklist.Controls.Add(line(i => new[] { "NAME", "CITIZENSHIP", "CIVIL", "DOB_MISSING", "DOB_FUTURE", "UNDER_18", "IMPEDIMENT", "UNSAVED" }.Contains(i.Code), "", "Application complete"));
            _checklist.Controls.Add(line(i => new[] { "NOT_POSTED", "POSTING", "ON_HOLD", "STATE" }.Contains(i.Code), "", "Posting complete"));
            if (_l.RequirementsOverrideBy.HasValue)
            {
                var ovLine = MUi.Txt("⚠  Requirements complete - OVERRIDDEN BY ADMIN: " + _l.RequirementsOverrideReason, 9.5F, FontStyle.Bold, UiTheme.Warning);
                ovLine.MaximumSize = new Size(PageWidth, 0); ovLine.Margin = new Padding(0, 3, 0, 3);
                _checklist.Controls.Add(ovLine);
            }
            else _checklist.Controls.Add(line(i => i.Code.StartsWith("REQ_") && !i.Code.Contains("PARENTAL") && !i.Code.Contains("COUNSELING"), "", "Requirements complete"));
            _checklist.Controls.Add(line(i => i.Code.Contains("PARENTAL") || i.Code.Contains("COUNSELING"), "", "Consent & advice complete"));
            _checklist.Controls.Add(line(i => i.Code == "PAYMENT", "", "Payment recorded"));
            _checklist.ResumeLayout();
            _issueList.Visible = true;
            _issueList.SetIssues(issues, "All checks pass. Use Issue license to issue it on the actual date of issue.");
        }

        private void RefreshRail(List<Need> needs, string display)
        {
            _rail.SuspendLayout();
            foreach (Control c in _rail.Controls.Cast<Control>().ToList()) c.Dispose();
            _rail.Controls.Clear();
            var items = new List<Control>();
            items.Add(MUi.Cap("Application at a glance"));
            items.Add(MUi.Kv("Application No.", _l.ApplicationNo ?? "(assigned on save)"));
            items.Add(MUi.Kv("Filed", MUi.D(_l.FiledDate)));
            items.Add(MUi.Kv("Posting", _l.PostingStart.HasValue ? MUi.Short(_l.PostingStart) + " - " + MUi.Short(_l.PostingEnd) : "not started"));
            if (_l.StoredStatus == "Posting") items.Add(MUi.Kv("Day", MarriageRules.PostingDay(_l, DateTime.Today, _s) + " of " + _s.PostingDays));
            items.Add(MUi.Kv("Earliest issue", _l.EarliestIssue.HasValue ? MUi.D(_l.EarliestIssue) : "-", _l.DeferralReason != null ? UiTheme.Warning : (Color?)null));
            if (_l.IssueDate.HasValue)
            {
                items.Add(MUi.Kv("License No.", _l.LicenseNo));
                items.Add(MUi.Kv("Issued", MUi.D(_l.IssueDate)));
                items.Add(MUi.Kv("Valid until", MUi.D(_l.ExpiryDate)));
            }
            var stage = new FlowLayoutPanel { Height = 34, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
            stage.Controls.Add(MUi.Txt("Current stage", 9F, FontStyle.Regular, UiTheme.Muted));
            stage.Controls.Add(MUi.Pill(display.ToUpperInvariant(), display));
            items.Add(stage);

            items.Add(MUi.Cap("Outstanding"));
            bool overridden = _l.RequirementsOverrideBy.HasValue;
            int missing = 0;
            LicenseFacts cur = Current();
            foreach (Need n in needs)
            {
                ReqRow r = MarriageRules.RowFor(_l.Requirements, n);
                bool ok = MarriageRules.Satisfied(r);
                // An override lifts these off the gating count (see MarriageRules.ApplyOverride)
                // but the item still shows unresolved - the office still needs it, an Admin only
                // decided not to wait for it.
                if (!ok && n.Blocking && !overridden) missing++;
                string who = n.Party == "Both" ? "" : " - " + (n.Party == "Wife" ? cur.Wife.Called : cur.Husband.Called);
                string label = ShortLabel(n.Code) + who;
                var l = MUi.Txt((ok ? "✓ " : "✕ ") + label + (r != null && !ok && r.Status != "Missing" ? " (" + r.Status.ToLowerInvariant() + ")" : ""),
                    9F, FontStyle.Regular, ok ? UiTheme.Success : UiTheme.Danger);
                l.AutoSize = false; l.Height = 22; l.AutoEllipsis = true;
                items.Add(l);
            }
            if (_s.LicensePaymentRequired)
            {
                bool paid = !string.IsNullOrWhiteSpace(_l.PaymentOr);
                if (!paid) missing++;
                var l = MUi.Txt((paid ? "✓ " : "✕ ") + "Payment (Treasury O.R.)", 9F, FontStyle.Regular, paid ? UiTheme.Success : UiTheme.Danger);
                l.AutoSize = false; l.Height = 22; items.Add(l);
            }
            if (overridden)
            {
                var ov = MUi.Txt("⚠ Requirements overridden by Admin: " + _l.RequirementsOverrideReason, 9F, FontStyle.Bold, UiTheme.Warning);
                ov.AutoSize = false; ov.Height = 34; ov.MaximumSize = new Size(PageWidth, 0);
                items.Add(ov);
            }
            var sum = new Banner();
            if (_l.IssueDate.HasValue) sum.Set(RuleSeverity.Info, "Licence issued.", null, true);
            else if (overridden && missing == 0 && IssueIssues().Count == 0) sum.Set(RuleSeverity.Warning, "Ready to issue - requirements overridden.", _l.RequirementsOverrideReason, true);
            else if (missing == 0 && IssueIssues().Count == 0) sum.Set(RuleSeverity.Info, "Ready to issue.", "All checks pass.", true);
            else if (missing == 0) sum.Set(RuleSeverity.Warning, "Documents complete.", "Issue License waits for posting to complete.");
            else sum.Set(RuleSeverity.Blocking, missing + " ITEM" + (missing == 1 ? "" : "S") + " OUTSTANDING", "Issue License remains unavailable.");
            items.Add(new Panel { Height = 10, BackColor = Color.Transparent });
            items.Add(sum);
            Stack(_rail, items.ToArray());
            _rail.ResumeLayout();
        }

        private static string ShortLabel(string code)
        {
            switch (code)
            {
                case "BIRTH_CERT": return "Birth certificate";
                case "VALID_ID": return "Valid ID";
                case "CENOMAR": return "CENOMAR";
                case "RPFP_CERT": return "RPFP certificate (RA 10354)";
                case "PARENTAL_CONSENT": return "Parental consent";
                case "PARENTAL_ADVICE": return "Parental advice";
                case "COUNSELING": return "Marriage counselling";
                case "PREV_MARRIAGE": return "Previous-marriage proof";
                case "LEGAL_CAPACITY": return "Legal capacity (embassy)";
                default: return code;
            }
        }

        private void RefreshSteps()
        {
            List<RuleIssue> issues = IssueIssues();
            bool issued = _l.IssueDate.HasValue;
            for (int i = 0; i < StepNames.Length; i++)
            {
                int n = issues.Count(x => x.FixWhere == StepNames[i] && !(i == 4 && x.Code == "POSTING"));
                bool done = issued || (_l.Id > 0 && n == 0 && (i != 3 || _l.PostingStart.HasValue));
                StepStrip.State st = i == _step ? StepStrip.State.Current
                    : (i == 4 && !issued && issues.Count > 0) ? StepStrip.State.Locked
                    : done ? StepStrip.State.Done : StepStrip.State.Todo;
                _steps.SetState(i, st);
                _steps.SetBadge(i, issued ? 0 : n);
            }
            if (!issued && _l.EarliestIssue.HasValue && DateTime.Today < _l.EarliestIssue.Value)
                _steps.SetSub(4, "locked until " + MUi.Short(_l.EarliestIssue));
            else _steps.SetSub(4, "Art. 20 · valid " + _s.ValidityDays + " days");
        }

        private void RefreshFooter()
        {
            _back.Enabled = _step > 0;
            _next.Enabled = _step < StepNames.Length - 1;
            _save.Enabled = !_readOnly;
            _save.Text = _dirty ? "Save draft *" : "Save draft";
            if (_l.IssueDate.HasValue) { _issue.Enabled = false; _adminOverride.Visible = false; _footReason.Text = "Licence " + _l.LicenseNo + " issued " + MUi.D(_l.IssueDate) + "."; return; }

            bool overridden = _l.RequirementsOverrideBy.HasValue;
            _adminOverride.Visible = MarriageService.IsAdmin && !_dirty && (overridden || HasOverridableIssues());
            _adminOverride.Text = overridden ? "Withdraw Override" : "Admin Override";

            List<RuleIssue> issues = IssueIssues();
            _issue.Enabled = issues.Count == 0 && !_dirty;
            _footReason.ForeColor = issues.Count == 0 ? UiTheme.Success : UiTheme.Muted;
            _footReason.Text = _dirty ? "Unsaved changes - save the draft first."
                : overridden && issues.Count == 0 ? "Requirements overridden by Admin - ready to issue."
                : issues.Count == 0 ? "All checks pass - ready to issue."
                : "Issue unavailable: " + issues[0].Message + (issues.Count > 1 ? "  (+" + (issues.Count - 1) + " more)" : "");
        }

        private int SuggestedStep()
        {
            if (_l.IssueDate.HasValue) return 4;
            if (_l.StoredStatus == "Posting" || _l.StoredStatus == "On Hold") return IssueIssues().Count == 0 ? 4 : 3;
            return 0;
        }

        private void GoTo(int i)
        {
            if (i < 0 || i >= StepNames.Length || i == _step) return;
            if (_dirty && !SaveDraft(false) && _l.Id <= 0 && i > 0)
            {
                MessageBox.Show(this, "Type at least one applicant's name first - the application is saved as you move between steps.",
                    "Applicants", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (i > _step)
            {
                // A forward move (Next, or a click further along the strip) has to clear every
                // step it passes over, so the strip cannot be used to skip the age checks.
                for (int s = _step; s < i; s++)
                {
                    string stop = StepGate(s);
                    if (stop != null)
                    {
                        MessageBox.Show(this, stop, "Cannot continue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (s != _step) ShowStep(s);
                        return;
                    }
                }
            }
            ShowStep(i);
        }

        /// <summary>
        /// Why the operator may not leave <paramref name="step"/> going forward, or null when
        /// they may. Applicants: an age we cannot compute, or under 18 (RA 11596 - void, so no
        /// point collecting requirements). Consent &amp; Advice: parental consent (18-20) not yet
        /// verified, or advice (21-25) not yet recorded. Advice may be recorded as unfavourable /
        /// not obtained - that defers issue three months (Art. 15) but is still an answer.
        /// An Admin override on the licence lifts the consent gate, as it does at issue.
        /// </summary>
        private string StepGate(int step)
        {
            if (_readOnly) return null;
            LicenseFacts l = Current();
            DateTime on = l.FiledDate ?? DateTime.Today;
            if (step == 0)
            {
                foreach (Party p in new[] { l.Husband, l.Wife })
                {
                    if (!p.Dob.HasValue)
                        return p.RoleLabel + ": enter the date of birth first - CROMS works out the age, and with it whether parental consent or advice is required.";
                    int a = MarriageRules.AgeOn(p.Dob.Value, on);
                    if (a < 18)
                        return "Cannot proceed - " + p.Called + " is " + a + " years old on the filing date.\n\nA marriage where either party is under 18 is void and prohibited (RA 11596). Correct the date of birth if it was mistyped; otherwise the application cannot continue.";
                }
            }
            else if (step == 2 && !l.RequirementsOverrideBy.HasValue)
            {
                List<Need> needs = MarriageRules.Needs(l.Husband, l.Wife, on, _catalog, "License", _s);
                List<RuleIssue> open = MarriageRules.RequirementIssues(needs, _l.Requirements ?? new List<ReqRow>(), l.Husband, l.Wife, "Consent & Advice")
                    .Where(x => x.Code == "REQ_PARENTAL_CONSENT" || x.Code == "REQ_PARENTAL_ADVICE").ToList();
                if (open.Count > 0)
                    return string.Join("\n", open.Select(x => x.Message)) +
                        "\n\nConsent (age 18-20, Art. 14) and advice (age 21-25, Art. 15) must be recorded before moving on. " +
                        "If the advice was unfavourable or not obtained, mark it Verified with that outcome - issue is then deferred three months after posting.";
            }
            return null;
        }

        private void ShowStep(int i)
        {
            _step = i;
            for (int k = 0; k < _pages.Length; k++) _pages[k].Visible = k == i;
            _pages[i].BringToFront();
            RefreshSteps();
            RefreshFooter();
        }

        // ================================================================= actions
        private void SavePayment()
        {
            if (!SaveDraft(false) && _l.Id <= 0) return;
            decimal amt;
            decimal? amount = decimal.TryParse(_orAmt.Text.Trim(), out amt) ? amt : (decimal?)null;
            if (string.IsNullOrWhiteSpace(_orNo.Text)) { MessageBox.Show(this, "Enter the official receipt number.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try { MarriageService.RecordPayment(_l.Id, _orNo.Text.Trim(), amount, MUi.Val(_orDate) ?? DateTime.Today); Reload(); }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        /// <summary>
        /// Preview Form 90 filled in from the SAVED application, ready to print. Saved first on
        /// purpose: the paper the applicants sign must be the record CROMS holds, not unsaved
        /// boxes. An issued / closed licence is printed as it stands.
        /// </summary>
        private void PrintApplication()
        {
            if (!_readOnly && (_dirty || _l.Id <= 0) && !SaveDraft(true)) return;
            if (_l.Id <= 0) return;
            try { Mf90Form.Show(MarriageService.LoadLicense(_l.Id), this); }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        /// <summary>
        /// Preview MF-06 for whichever party is 18-20 on the filing date (FC Art. 14). Prints
        /// once per qualifying party - a couple where both are underage gets both forms shown
        /// in turn, not merged onto one.
        /// </summary>
        private void PrintConsent()
        {
            if (!_readOnly && (_dirty || _l.Id <= 0) && !SaveDraft(true)) return;
            if (_l.Id <= 0) return;
            try
            {
                LicenseFacts l = MarriageService.LoadLicense(_l.Id);
                DateTime on = l.FiledDate ?? DateTime.Today;
                MarriageSettings s = MarriageService.Settings;
                var need = new List<string>();
                if (MarriageRules.InConsentBand(l.Husband, on, s)) need.Add("Husband");
                if (MarriageRules.InConsentBand(l.Wife, on, s)) need.Add("Wife");
                if (need.Count == 0)
                {
                    MessageBox.Show(this, "Neither applicant is in the consent age band (" + s.ConsentAgeFrom + "-" + s.ConsentAgeTo +
                        ") on the filing date, so this form does not apply.", "Consent (MF-06)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (string role in need) ConsentForm.Show(l, role, this);
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        /// <summary>Preview MF-68: one page carrying both the husband's and the wife's half.
        /// Offered whenever EITHER is in the 21-25 advice band (FC Art. 15).</summary>
        private void PrintAdvice()
        {
            if (!_readOnly && (_dirty || _l.Id <= 0) && !SaveDraft(true)) return;
            if (_l.Id <= 0) return;
            try
            {
                LicenseFacts l = MarriageService.LoadLicense(_l.Id);
                DateTime on = l.FiledDate ?? DateTime.Today;
                MarriageSettings s = MarriageService.Settings;
                if (!MarriageRules.InAdviceBand(l.Husband, on, s) && !MarriageRules.InAdviceBand(l.Wife, on, s))
                {
                    MessageBox.Show(this, "Neither applicant is in the advice age band (" + s.AdviceAgeFrom + "-" + s.AdviceAgeTo +
                        ") on the filing date, so this form does not apply.", "Advice (MF-68)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                AdviceForm.Show(l, this);
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void StartPosting()
        {
            if (!SaveDraft(false)) return;
            DateTime st = _postStart.Value.Date;
            if (st != DateTime.Today)
            {
                MessageBox.Show(this, "The posting start is the day the notice actually goes up - it cannot be in the past or the future. Use today's date.", "Posting",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!MUi.Confirm(this, "Start posting", "Start the ten-day public posting?",
                    "Application|" + _l.ApplicationNo, "Applicants|" + _l.Husband.FullName + " & " + _l.Wife.FullName,
                    "Posting|" + MUi.D(st) + " - " + MUi.D(MarriageRules.PostingEnd(st, _s)),
                    "Earliest issue|" + MUi.D(MarriageRules.EarliestIssue(st, false, _s))))
                return;
            try
            {
                List<RuleIssue> issues = MarriageService.StartPosting(_l.Id, st);
                if (issues.Count > 0) MessageBox.Show(this, string.Join("\n", issues.Select(i => "- " + i.Message)), "Posting not started", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Reload();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void ToggleHold()
        {
            try
            {
                if (_l.StoredStatus == "On Hold") MarriageService.ReleaseHold(_l.Id);
                else
                {
                    string why = MUi.Ask(this, "Put on hold", "Why is this application on hold? (e.g. opposition filed, awaiting a document)");
                    if (why == null) return;
                    MarriageService.Hold(_l.Id, why);
                }
                Reload();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void CancelApplication()
        {
            string why = MUi.Ask(this, "Cancel application", "Reason for cancelling (kept in the history; nothing is deleted):");
            if (why == null) return;
            try { MarriageService.Cancel(_l.Id, why); Reload(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void SaveFinding()
        {
            if (string.IsNullOrWhiteSpace(_finding.Text)) return;
            try { MarriageService.SetImpedimentNote(_l.Id, _finding.Text.Trim()); Reload(); } catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void OpenIssue()
        {
            if (_dirty && !SaveDraft(false)) return;
            using (var dlg = new IssueLicenseForm(MarriageService.LoadLicense(_l.Id)))
                dlg.ShowDialog(this);
            Reload();
            ShowStep(4);
        }

        private void OnClosingAsk(object sender, FormClosingEventArgs e)
        {
            if (!_dirty || _readOnly) return;
            DialogResult r = MessageBox.Show(this, "Save the changes to this application before closing?", "Unsaved changes",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) e.Cancel = true;
            else if (r == DialogResult.Yes && !SaveDraft(true)) e.Cancel = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { SaveDraft(true); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    /// <summary>A small bar: the posting days, filled to today.</summary>
    internal sealed class PostingBar : Control
    {
        private LicenseFacts _l;
        private MarriageSettings _s = MarriageSettings.Defaults();

        public PostingBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.Surface;
        }

        public void SetData(LicenseFacts l, MarriageSettings s) { _l = l; _s = s; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor);
            if (_l == null || !_l.PostingStart.HasValue) return;
            int n = Math.Max(1, _s.PostingDays), day = MarriageRules.PostingDay(_l, DateTime.Today, _s);
            bool done = _l.EarliestIssue.HasValue && DateTime.Today >= _l.EarliestIssue.Value;
            float w = (Width - 4f) / n;
            for (int i = 0; i < n; i++)
            {
                var r = new Rectangle((int)(i * w) + 2, 8, (int)w - 4, 14);
                Color c = done || i < day ? (done ? UiTheme.Success : UiTheme.Warning) : UiTheme.RowLine;
                using (GraphicsPath p = CardPanel.RoundedRect(r, 3)) using (var b = new SolidBrush(c)) g.FillPath(b, p);
                TextRenderer.DrawText(g, _l.PostingStart.Value.AddDays(i).ToString("dd"), MUiFonts.Small, new Rectangle(r.X, 24, r.Width, 16), UiTheme.Faint,
                    TextFormatFlags.HorizontalCenter);
            }
        }
    }
}
