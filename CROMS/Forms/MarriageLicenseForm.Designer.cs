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
    partial class MarriageLicenseForm
    {
        // chrome
        private readonly Label _title = MUi.Txt("", 12.5F, FontStyle.Bold, Color.White);
        private readonly StatusPill _appPill = new StatusPill(), _statusPill = new StatusPill();
        private readonly StepStrip _steps = new StepStrip(true);
        private readonly Panel[] _pages = new Panel[5];
        private readonly Panel _rail = new Panel();
        private readonly Button _back = MUi.Btn("< Back", MUi.Kind.Ghost, 90), _save = MUi.Btn("Save draft", MUi.Kind.Secondary, 110),
                                _next = MUi.Btn("Next >", MUi.Kind.Primary, 110), _issue = MUi.Btn("Issue license", MUi.Kind.Success, 130);
        // Admin-only: bypass missing/unverified requirement attachments so the licence can still
        // issue (backlog request 2026-09-16). Never bypasses posting/payment/impediment/under-18.
        private readonly Button _adminOverride = MUi.Btn("Admin Override", MUi.Kind.Secondary, 140);
        // Municipal Form 90 itself, filled in - the paper the applicants sign.
        private readonly Button _printApp = MUi.Btn("Print application (MF-90)", MUi.Kind.Secondary, 200);
        // Consent (MF-06) / Advice (MF-68) - printed only for whichever party the age band applies to.
        private readonly Button _printConsent = MUi.Btn("Consent (MF-06)", MUi.Kind.Secondary, 150);
        private readonly Button _printAdvice = MUi.Btn("Advice (MF-68)", MUi.Kind.Secondary, 150);
        private readonly Label _footReason = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);

        // page 1
        private readonly DateTimePicker _filed = MUi.Date(false);
        private readonly TextBox _remarks = MUi.Box();
        private readonly FlowLayoutPanel _ageNotes = new FlowLayoutPanel();
        // page 2
        private readonly RequirementsGrid _docs = new RequirementsGrid();
        private readonly TextBox _orNo = MUi.Box(), _orAmt = MUi.Box();
        private readonly DateTimePicker _orDate = MUi.Date(true);
        private readonly Banner _payBanner = new Banner();
        // page 3
        private readonly FlowLayoutPanel _consentNotes = new FlowLayoutPanel();
        private readonly RequirementsGrid _consent = new RequirementsGrid();
        // page 4
        private readonly Banner _postBanner = new Banner();
        private readonly DateTimePicker _postStart = MUi.Date(false);
        private readonly Label _postPreview = MUi.Txt("", 9.5F, FontStyle.Bold);
        private readonly Button _startPosting = MUi.Btn("Start posting", MUi.Kind.Primary, 150), _hold = MUi.Btn("Put on hold", MUi.Kind.Secondary, 130),
                                _cancelApp = MUi.Btn("Cancel application", MUi.Kind.Ghost, 160), _saveFinding = MUi.Btn("Record registrar finding", MUi.Kind.Secondary, 200);
        private readonly Panel _postDetail = new Panel(), _findingPanel = new Panel();
        private readonly TextBox _finding = MUi.Box();
        private readonly PostingBar _bar = new PostingBar();
        // page 5
        private readonly FlowLayoutPanel _checklist = new FlowLayoutPanel();
        private readonly IssueList _issueList = new IssueList();
        private readonly Panel _issuedPanel = new Panel();

        private void InitializeComponent()
        {
            Text = "Marriage License Application - Municipal Form 90";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1220, 780);
            MinimumSize = new Size(1080, 700);
            WindowState = FormWindowState.Maximized;
            BackColor = UiTheme.PageBg;
            ShowInTaskbar = false;
            KeyPreview = true;

            BuildChrome();
            BuildApplicants();
            BuildRequirements();
            BuildConsent();
            BuildPosting();
            BuildIssue();
            UiTheme.Polish(this);
        }

        // ================================================================= layout
        private static void Stack(Control host, params Control[] topToBottom)
        {
            for (int i = topToBottom.Length - 1; i >= 0; i--) { topToBottom[i].Dock = DockStyle.Top; host.Controls.Add(topToBottom[i]); }
            // Added bottom-first for Dock=Top, which also made TAB run bottom-to-top - so the
            // window opened focused (and auto-scrolled) on the LAST box of the applicants card,
            // halfway down the page, once Form 90's parent blocks made that card ~1000px tall.
            for (int i = 0; i < topToBottom.Length; i++) topToBottom[i].TabIndex = i;
        }

        private void BuildChrome()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = UiTheme.Navy, Padding = new Padding(18, 0, 18, 0) };
            _title.Text = "Marriage License Application  -  Municipal Form 90";
            _title.Location = new Point(18, 15);
            _appPill.Location = new Point(520, 16); _statusPill.Location = new Point(660, 16);
            header.Controls.Add(_title); header.Controls.Add(_appPill); header.Controls.Add(_statusPill);
            header.Resize += (s, e) => { _appPill.Left = _title.Right + 14; _statusPill.Left = _appPill.Right + 8; };

            for (int i = 0; i < StepNames.Length; i++) _steps.AddStep(StepNames[i], "");
            _steps.SetSub(0, "items 1-12 · both applicants");
            _steps.SetSub(1, "documents · payment");
            _steps.SetSub(2, "Family Code Arts. 14-16");
            _steps.SetSub(3, "Art. 17 · " + _s.PostingDays + " days");
            _steps.SetSub(4, "Art. 20 · valid " + _s.ValidityDays + " days");
            _steps.StepClicked += i => GoTo(i);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(250, 251, 253), Padding = new Padding(16, 12, 16, 12) };
            footer.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 440, BackColor = Color.Transparent };
            left.Controls.Add(_back); left.Controls.Add(_save); left.Controls.Add(_printApp);
            left.Controls.Add(_printConsent); left.Controls.Add(_printAdvice);
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 420, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent };
            right.Controls.Add(_next); right.Controls.Add(_issue); right.Controls.Add(_adminOverride);
            _footReason.AutoSize = false; _footReason.Dock = DockStyle.Fill; _footReason.TextAlign = ContentAlignment.MiddleRight; _footReason.AutoEllipsis = true;
            footer.Controls.Add(_footReason); footer.Controls.Add(right); footer.Controls.Add(left);
            _back.Click += (s, e) => GoTo(_step - 1);
            _next.Click += (s, e) => GoTo(_step + 1);
            _save.Click += (s, e) => SaveDraft(true);
            _printApp.Click += (s, e) => PrintApplication();
            _printConsent.Click += (s, e) => PrintConsent();
            _printAdvice.Click += (s, e) => PrintAdvice();
            _issue.Click += (s, e) => OpenIssue();
            _adminOverride.Click += (s, e) => DoAdminOverride();

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Surface, Margin = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = UiTheme.Surface };
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i] = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20, 16, 20, 16), BackColor = UiTheme.Surface, Visible = false };
                host.Controls.Add(_pages[i]);
            }
            _rail.Dock = DockStyle.Fill; _rail.AutoScroll = true; _rail.Padding = new Padding(16, 14, 16, 14); _rail.BackColor = Color.FromArgb(250, 251, 253);
            _rail.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, 0, _rail.Height); };
            body.Controls.Add(host, 0, 0); body.Controls.Add(_rail, 1, 0);

            Controls.Add(body); Controls.Add(footer); Controls.Add(_steps); Controls.Add(header);
        }

        private Control PartyColumn(string title, PartyBox p, string role, Color headTint, Color headInk)
        {
            var card = new Panel { BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 14, 0), Dock = DockStyle.Fill };
            card.Paint += (s, e) => { using (var pen = new Pen(UiTheme.CardLine)) e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            var head = new Label { Text = title, Dock = DockStyle.Top, Height = 32, BackColor = headTint, ForeColor = headInk,
                                   Font = MUi.F(9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), UseMnemonic = false };
            var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 4, 8), BackColor = Color.Transparent };

            p.First = MUi.Box(); p.Middle = MUi.Box(); p.Last = MUi.Box();
            p.Dob = MUi.Date(true); p.Age = MUi.Txt("-", 10F, FontStyle.Bold, UiTheme.Muted);
            p.Residence = MUi.Box();
            p.Country = MUi.Combo(true); p.Province = MUi.Combo(true); p.Municipality = MUi.Combo(true);
            p.Cit = MUi.Combo(true, _nationalities); p.Civil = MUi.Combo(false, MarriageRules.CivilStatuses);
            p.Religion = MUi.Combo(true, Lookup("religions"));
            // Form 90 prints a sex for each party. The column is already Husband / Wife, so the
            // lawful answer is pre-picked rather than left blank - it stays visible and editable.
            p.Sex = MUi.Combo(false, MarriageRules.Sexes);
            p.Sex.SelectedItem = MarriageRules.SexForRole(role);

            TableLayoutPanel names = MUi.Grid(3, 1, 56);
            names.Controls.Add(MUi.Field("First name", p.First), 0, 0);
            names.Controls.Add(MUi.Field("Middle name", p.Middle), 1, 0);
            names.Controls.Add(MUi.Field("Last name", p.Last), 2, 0);
            TableLayoutPanel dob = MUi.Grid(3, 1, 56);
            dob.Controls.Add(MUi.Field("Date of birth", p.Dob), 0, 0);
            var agePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            p.Age.AutoSize = false; p.Age.Dock = DockStyle.Fill; p.Age.TextAlign = ContentAlignment.MiddleLeft;
            agePanel.Controls.Add(p.Age);
            // "Age on filing - computed" ran straight into the Sex caption once this row went
            // to three columns; the value beside it already says it is derived.
            dob.Controls.Add(MUi.Field("Age on filing", agePanel), 1, 0);
            dob.Controls.Add(MUi.Field("Sex", p.Sex), 2, 0);
            // Two rows, not three columns: "City / municipality of birth" is the longest
            // caption on the card and clipped at a third of the card's width. Top-down also
            // matches the order the pickers actually cascade in.
            TableLayoutPanel place = MUi.Grid(2, 1, 56);
            place.Controls.Add(MUi.Field("Country of birth", p.Country), 0, 0);
            place.Controls.Add(MUi.Field("Province of birth", p.Province), 1, 0);
            TableLayoutPanel city = MUi.Grid(1, 1, 56);
            city.Controls.Add(MUi.Field("City / municipality of birth", p.Municipality), 0, 0);
            TableLayoutPanel cit = MUi.Grid(2, 1, 56);
            cit.Controls.Add(MUi.Field("Citizenship", p.Cit), 0, 0); cit.Controls.Add(MUi.Field("Civil status", p.Civil), 1, 0);
            TableLayoutPanel rel = MUi.Grid(2, 1, 56);
            rel.Controls.Add(MUi.Field("Religion", p.Religion), 0, 0); rel.Controls.Add(MUi.Field("Residence", p.Residence), 1, 0);
            // ---- Form 90's parent / consent / previously-married blocks, in the form's order.
            p.FFirst = MUi.Box(); p.FMiddle = MUi.Box(); p.FLast = MUi.Box(); p.FRes = MUi.Box(); p.FCit = MUi.Combo(true, _nationalities);
            p.MFirst = MUi.Box(); p.MMiddle = MUi.Box(); p.MLast = MUi.Box(); p.MRes = MUi.Box(); p.MCit = MUi.Combo(true, _nationalities);
            p.CFirst = MUi.Box(); p.CMiddle = MUi.Box(); p.CLast = MUi.Box(); p.CRes = MUi.Box(); p.CCit = MUi.Combo(true, _nationalities);
            p.CRel = MUi.Combo(true, MarriageRules.ConsentRelationships);
            AutoCaps.Attach(p.First, p.Middle, p.Last,
                p.FFirst, p.FMiddle, p.FLast, p.MFirst, p.MMiddle, p.MLast,
                p.CFirst, p.CMiddle, p.CLast);
            p.PrevHow = MUi.Combo(true, MarriageRules.Dissolutions);
            p.PrevProvince = MUi.Combo(true); p.PrevMunicipality = MUi.Combo(true);
            p.PrevDate = MUi.Date(true);

            Func<TextBox, TextBox, TextBox, string, TableLayoutPanel> nameRow = (f, m, l, lastCap) =>
            {
                TableLayoutPanel g = MUi.Grid(3, 1, 56);
                g.Controls.Add(MUi.Field("First name", f), 0, 0);
                g.Controls.Add(MUi.Field("Middle name", m), 1, 0);
                g.Controls.Add(MUi.Field(lastCap, l), 2, 0);
                return g;
            };
            Func<string, Control, string, Control, TableLayoutPanel> pairRow = (c1, i1, c2, i2) =>
            {
                TableLayoutPanel g = MUi.Grid(2, 1, 56);
                g.Controls.Add(MUi.Field(c1, i1), 0, 0); g.Controls.Add(MUi.Field(c2, i2), 1, 0);
                return g;
            };

            TableLayoutPanel cRes = MUi.Grid(1, 1, 56);
            cRes.Controls.Add(MUi.Field("Residence", p.CRes), 0, 0);
            TableLayoutPanel prevA = pairRow("How it was dissolved", p.PrevHow, "Date dissolved", p.PrevDate);
            // Province before city, the order the two pickers cascade in - same as place of birth.
            TableLayoutPanel prevB = pairRow("Province dissolved", p.PrevProvince, "City / municipality", p.PrevMunicipality);
            p.PrevRows = new Control[] { prevA, prevB };
            p.PrevNote = MUi.Txt("", 8.5F, FontStyle.Regular, UiTheme.Muted);

            var stack = new List<Control> { names, dob, place, city, cit, rel,
                SubHead("Father", null), nameRow(p.FFirst, p.FMiddle, p.FLast, "Last name"), pairRow("Citizenship", p.FCit, "Residence", p.FRes),
                SubHead("Mother", null), nameRow(p.MFirst, p.MMiddle, p.MLast, "Last name"), pairRow("Citizenship", p.MCit, "Residence", p.MRes),
                SubHead("Person who gave consent or advice", null), nameRow(p.CFirst, p.CMiddle, p.CLast, "Last name"),
                pairRow("Relationship", p.CRel, "Citizenship", p.CCit), cRes,
                SubHead("If previously married", p.PrevNote), prevA, prevB };
            Stack(inner, stack.ToArray());
            card.Controls.Add(inner); card.Controls.Add(head);
            // Measured, not a row count times a guess: the header + every stacked block + padding.
            _partyHeight = head.Height + inner.Padding.Vertical + stack.Sum(c => c.Height);

            // Geography BEFORE the Touched handlers, and the order inside matters twice over.
            // Province list first, then the country that rebuilds it: wiring the country
            // before the province list exists would fire the cascade into an unbuilt list.
            // And picking the default country has to happen while nothing is listening - the
            // Touched handler recomputes ages by reading BOTH party boxes, and the second
            // party does not exist yet while the first column is being built. Selecting a
            // default here with the handler already attached dereferenced those null boxes.
            GeoLookup.LoadProvinces(p.Province);
            GeoLookup.CascadePlace(p.Province, p.Municipality);
            GeoLookup.LoadCountries(p.Country);
            GeoLookup.CascadeCountry(p.Country, p.Province, p.Municipality, null);
            GeoLookup.Select(p.Country, GeoLookup.HomeCountry);
            // Place dissolved: the same cascade the place of birth uses. No default picked - a
            // dissolved-marriage place is never assumed.
            GeoLookup.LoadProvinces(p.PrevProvince);
            GeoLookup.CascadePlace(p.PrevProvince, p.PrevMunicipality);
            // Initial grey-out, still before any handler listens (see the trap noted above).
            ApplyPrevMarried(p);

            foreach (Control c in new Control[] { p.First, p.Middle, p.Last, p.Residence,
                                                  p.Cit, p.Civil, p.Religion, p.Country, p.Province, p.Municipality,
                                                  p.FFirst, p.FMiddle, p.FLast, p.FCit, p.FRes, p.MFirst, p.MMiddle, p.MLast, p.MCit, p.MRes,
                                                  p.CFirst, p.CMiddle, p.CLast, p.CRel, p.CCit, p.CRes,
                                                  p.PrevHow, p.PrevProvince, p.PrevMunicipality })
                c.TextChanged += (s, e) => Touched();
            p.PrevDate.ValueChanged += (s, e) => Touched();
            // Grey-out BEFORE Touched, so the refresh that follows reads an already-cleared block.
            p.Civil.SelectedIndexChanged += (s, e) => ApplyPrevMarried(p);
            p.Civil.SelectedIndexChanged += (s, e) => Touched();
            p.Sex.SelectedIndexChanged += (s, e) => Touched();
            p.Dob.ValueChanged += (s, e) => Touched();
            LearningLibrary.Attach(p.First, LearningLibrary.GivenName);
            LearningLibrary.Attach(p.Last, LearningLibrary.Surname);
            return card;
        }

        /// <summary>A block heading inside a party column, ruled above so the blocks read apart.</summary>
        private static Control SubHead(string title, Label note)
        {
            var p = new Panel { Height = note == null ? 34 : 52, BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0, 10, 10, 0) };
            p.Paint += (s, e) => { using (var pen = new Pen(UiTheme.RowLine)) e.Graphics.DrawLine(pen, 0, 4, p.Width - 10, 4); };
            if (note != null)
            {
                note.AutoSize = false; note.Dock = DockStyle.Fill; note.AutoEllipsis = true; note.TextAlign = ContentAlignment.TopLeft;
                p.Controls.Add(note);
            }
            var t = MUi.Txt(title, 9F, FontStyle.Bold, UiTheme.Ink);
            t.AutoSize = false; t.Dock = DockStyle.Top; t.Height = 22;
            p.Controls.Add(t);
            return p;
        }

        private static Control Section(string title, string sub) { return MUi.SectionHeader(title, sub); }

        private void BuildApplicants()
        {
            Panel pg = _pages[0];
            TableLayoutPanel top = MUi.Grid(3, 1, 58);
            top.Controls.Add(MUi.Field("Date filed", _filed), 0, 0);
            var remarksField = MUi.Field("Remarks", _remarks);
            top.Controls.Add(remarksField, 1, 0); top.SetColumnSpan(remarksField, 2);
            _filed.ValueChanged += (s, e) => Touched();
            _remarks.TextChanged += (s, e) => Touched();

            // Height is MEASURED from the stacked blocks (PartyColumn sets _partyHeight), not a
            // hand count: the card was 450 for seven rows, went to ~1000 with Form 90's parent /
            // consent / previously-married blocks, and a guessed number is how the parents'
            // names were cut in half at the card's edge on 2026-09-12.
            var cols = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            cols.Controls.Add(PartyColumn("HUSBAND / PARTY 1", _h, "Husband", UiTheme.AccentTint, Color.FromArgb(27, 62, 158)), 0, 0);
            cols.Controls.Add(PartyColumn("WIFE / PARTY 2", _w, "Wife", Color.FromArgb(245, 237, 251), Color.FromArgb(107, 48, 150)), 1, 0);
            cols.Height = _partyHeight;

            _ageNotes.FlowDirection = FlowDirection.TopDown; _ageNotes.WrapContents = false; _ageNotes.AutoSize = true;
            _ageNotes.AutoSizeMode = AutoSizeMode.GrowAndShrink; _ageNotes.Padding = new Padding(0, 12, 0, 0); _ageNotes.BackColor = Color.Transparent;
            Stack(pg, Section("Applicants", "Both contracting parties, as written on the application. Ages and the requirements they trigger are computed from the dates of birth."),
                  top, cols, _ageNotes);
        }

        private void BuildRequirements()
        {
            Panel pg = _pages[1];
            _docs.Height = 300;
            _docs.AllowAddCustom = true; // a particular application may need a document beyond the standard set
            _docs.PartyOptions = new[] { "Both", "Husband", "Wife" };
            _docs.AllowBypass = MarriageService.IsAdmin;
            _docs.Changed += () => RefreshAll();
            TableLayoutPanel pay = MUi.Grid(4, 1, 58);
            pay.Controls.Add(MUi.Field("Official receipt no. (Treasury)", _orNo), 0, 0);
            pay.Controls.Add(MUi.Field("Amount (PHP)", _orAmt), 1, 0);
            pay.Controls.Add(MUi.Field("Date paid", _orDate), 2, 0);
            var savePay = MUi.Btn("Save payment", MUi.Kind.Secondary, 130);
            var payCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 0, 0), BackColor = Color.Transparent };
            payCell.Controls.Add(savePay); savePay.Dock = DockStyle.Left;
            pay.Controls.Add(payCell, 3, 0);
            savePay.Click += (s, e) => SavePayment();
            Stack(pg, Section("Supporting documents", "Mark each document Submitted when received and Verified once checked. CROMS records the CENOMAR - it is PSA-issued and is never generated here."),
                  _docs, Section("Payment", "Fees are collected by the Municipal Treasury; record the official receipt it issued."), pay, _payBanner);
        }

        private void BuildConsent()
        {
            Panel pg = _pages[2];
            _consentNotes.FlowDirection = FlowDirection.TopDown; _consentNotes.WrapContents = false; _consentNotes.AutoSize = true;
            _consentNotes.AutoSizeMode = AutoSizeMode.GrowAndShrink; _consentNotes.BackColor = Color.Transparent;
            _consent.Height = 220;
            _consent.AllowBypass = MarriageService.IsAdmin;
            _consent.Changed += () => RefreshAll();
            Stack(pg, Section("Consent, advice and counselling", "Derived from each applicant's age on the filing date. Record who gave consent or advice and, for advice, whether it was favourable."),
                  _consentNotes, _consent);
        }

        private void BuildPosting()
        {
            Panel pg = _pages[3];
            TableLayoutPanel start = MUi.Grid(3, 1, 58);
            start.Controls.Add(MUi.Field("Posting start (day 1)", _postStart), 0, 0);
            var prev = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 26, 0, 0) };
            prev.Controls.Add(_postPreview); _postPreview.Dock = DockStyle.Fill;
            start.Controls.Add(prev, 1, 0); start.SetColumnSpan(prev, 2);
            _postStart.ValueChanged += (s, e) => UpdatePostingPreview();
            var actions = new FlowLayoutPanel { Height = 46, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
            actions.Controls.Add(_startPosting); actions.Controls.Add(_hold); actions.Controls.Add(_cancelApp);
            _startPosting.Click += (s, e) => StartPosting();
            _hold.Click += (s, e) => ToggleHold();
            _cancelApp.Click += (s, e) => CancelApplication();

            _postDetail.Height = 150; _postDetail.BackColor = Color.Transparent;
            _bar.Dock = DockStyle.Top; _bar.Height = 44;

            var fl = MUi.Field("Registrar's finding on the impediment (Family Code Art. 18)", _finding);
            var fBtn = new FlowLayoutPanel { Height = 44, BackColor = Color.Transparent };
            fBtn.Controls.Add(_saveFinding);
            _saveFinding.Click += (s, e) => SaveFinding();
            Stack(_findingPanel, fl, fBtn);
            _findingPanel.Height = 110; _findingPanel.BackColor = Color.Transparent;

            Stack(pg, Section("Ten-day public posting", "Staff confirm when the notice goes up. Dates are computed - never type the end date."),
                  _postBanner, start, _bar, _postDetail, actions, _findingPanel);
        }

        private void BuildIssue()
        {
            Panel pg = _pages[4];
            _checklist.FlowDirection = FlowDirection.TopDown; _checklist.WrapContents = false; _checklist.AutoSize = true;
            _checklist.AutoSizeMode = AutoSizeMode.GrowAndShrink; _checklist.BackColor = Color.Transparent;
            _issueList.Height = 240;
            _issuedPanel.Height = 200; _issuedPanel.BackColor = Color.Transparent;
            Stack(pg, Section("Final check before issue", "The licence issues on the ACTUAL date of issue, from the Issue window. Its " + _s.ValidityDays + "-day validity starts that day."),
                  _checklist, _issueList, _issuedPanel);
            _issueList.FixRequested += where => { int i = Array.IndexOf(StepNames, where); if (i >= 0) GoTo(i); };
        }
    }
}
