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
    internal sealed class MarriageLicenseForm : Form
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
        private readonly PartyBox _h = new PartyBox(), _w = new PartyBox();
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

        private static readonly string[] StepNames = { "Applicants", "Requirements", "Consent & Advice", "Posting", "Issue License" };

        public MarriageLicenseForm(int? licenseId)
        {
            Text = "Marriage License Application - Municipal Form 90";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1220, 780);
            MinimumSize = new Size(1080, 700);
            BackColor = UiTheme.PageBg;
            ShowInTaskbar = false;
            KeyPreview = true;

            try { _catalog = MarriageService.Catalog(); } catch { _catalog = new List<ReqType>(); }
            // Six citizenship boxes per applicant pair now read this list; load it once.
            _nationalities = Lookup("nationalities").ToList();
            BuildChrome();
            BuildApplicants();
            BuildRequirements();
            BuildConsent();
            BuildPosting();
            BuildIssue();
            UiTheme.Polish(this);

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

        private static Control Section(string title, string sub) { return MUi.SectionHeader(title, sub); }

        /// <summary>Usable width of a step page. Read from the FORM, not the page: a hidden page reports its default size.</summary>
        private int PageWidth { get { return Math.Max(420, ClientSize.Width - 320 - 64); } }

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
        /// nothing is actually missing).</summary>
        private bool HasOverridableIssues()
        {
            if (_l.Id <= 0) return false;
            return MarriageRules.ValidateForIssue(Current(), _catalog, DateTime.Today, _s).Where(i => i.Blocks).Any(i => i.Code.StartsWith("REQ_"));
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
            string reason = MUi.Ask(this, "Admin Override",
                "This application has missing or unverified requirement attachments (birth certificate, valid ID, CENOMAR, parental consent/advice, etc.). " +
                "Issuing it anyway is an Admin decision and will be permanently recorded on the application and in the audit trail.\n\n" +
                "This does NOT bypass posting, payment, an unresolved impediment, or the under-18 rule.\n\nReason for overriding:", "");
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
            ShowStep(i);
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
            if (st > DateTime.Today)
            {
                MessageBox.Show(this, "The posting start is the day the notice actually goes up - it cannot be in the future.", "Posting",
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

    /// <summary>
    /// WINDOW 3 - the focused Issue License window. Issuing is a deliberate act with its own
    /// screen, not a click in a table: it restates the checklist, shows the number the licence
    /// will take and computes the validity from the ACTUAL date of issue.
    /// </summary>
    internal sealed class IssueLicenseForm : Form
    {
        private readonly LicenseFacts _l;
        private readonly MarriageSettings _s = MarriageService.Settings;
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

        public IssueLicenseForm(LicenseFacts l)
        {
            _l = l;
            Text = "Issue Marriage License";
            StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(620, 640); BackColor = UiTheme.Surface;

            var head = new Label { Text = "  ISSUE MARRIAGE LICENSE", Dock = DockStyle.Top, Height = 46, BackColor = UiTheme.Navy, ForeColor = Color.White,
                                   Font = MUi.F(12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 14, 22, 8), AutoScroll = true, BackColor = UiTheme.Surface };
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 10),
                                             BackColor = Color.FromArgb(250, 251, 253) };
            var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
            var preview = MUi.Btn("Preview license", MUi.Kind.Secondary, 140);
            foot.Controls.Add(_go); foot.Controls.Add(preview); foot.Controls.Add(cancel);
            CancelButton = cancel;

            List<RuleIssue> all = MarriageRules.ValidateForIssue(l, MarriageService.Catalog(), DateTime.Today, _s).Where(i => i.Blocks).ToList();
            var items = new List<Control>
            {
                MUi.Cap("Application"),
                MUi.Kv("Application No.", l.ApplicationNo),
                MUi.Kv("Husband / Party 1", l.Husband.FullName),
                MUi.Kv("Wife / Party 2", l.Wife.FullName),
                MUi.Cap("Checklist"),
                Check("Application complete", !all.Any(i => new[] { "NAME", "CITIZENSHIP", "CIVIL", "DOB_MISSING", "UNDER_18", "IMPEDIMENT" }.Contains(i.Code))),
                Check("Posting complete (" + MUi.Short(l.PostingStart) + " - " + MUi.D(l.PostingEnd) + ")", !all.Any(i => i.Code == "POSTING" || i.Code == "NOT_POSTED")),
                Check("Requirements complete", !all.Any(i => i.Code.StartsWith("REQ_") && !i.Code.Contains("PARENTAL") && !i.Code.Contains("COUNSELING"))),
                Check("Consent / advice complete", !all.Any(i => i.Code.Contains("PARENTAL") || i.Code.Contains("COUNSELING"))),
                OverrideRow(),
                Check("Payment recorded" + (string.IsNullOrEmpty(l.PaymentOr) ? "" : " - O.R. " + l.PaymentOr), !all.Any(i => i.Code == "PAYMENT")),
                MUi.Cap("License"),
            };
            var no = new TableLayoutPanel { Height = 30, ColumnCount = 2, BackColor = Color.Transparent };
            no.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); no.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            no.Controls.Add(MUi.Txt("License No.", 9F, FontStyle.Regular, UiTheme.Muted), 0, 0); no.Controls.Add(_no, 1, 0);
            TableLayoutPanel dt = MUi.Grid(2, 1, 58);
            dt.Controls.Add(MUi.Field("Issue date (actual)", _date), 0, 0);
            var vp = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0), BackColor = Color.Transparent };
            vp.Controls.Add(_valid); _valid.Dock = DockStyle.Fill;
            dt.Controls.Add(vp, 1, 0);
            items.Add(no); items.Add(dt);
            items.Add(MUi.Kv("Validity", _s.ValidityDays + " days from the date of issue, anywhere in the Philippines (Family Code Art. 20)"));
            items.Add(new Panel { Height = 10, BackColor = Color.Transparent });
            _issues.Height = 110;
            items.Add(_issues);
            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; body.Controls.Add(items[i]); }

            Controls.Add(body); Controls.Add(foot); Controls.Add(head);

            _date.MaxDate = DateTime.Today;
            if (l.EarliestIssue.HasValue && l.EarliestIssue.Value <= DateTime.Today) _date.MinDate = l.EarliestIssue.Value;
            _date.Value = DateTime.Today;
            _date.ValueChanged += (s, e) => Recompute();
            preview.Click += (s, e) => LicensePrinter.Show(this, Hypothetical(), true);
            _go.Click += (s, e) => DoIssue();
            _override.Click += (s, e) => DoOverride();
            UiTheme.Polish(this);
            Recompute();
        }

        /// <summary>
        /// Admin-only row: lets an Admin record (or withdraw) a requirements override on this
        /// application. Hidden for a Registrar - the power is intentionally narrower than the
        /// usual Registrar-or-Admin gate elsewhere on this table (see MarriageService.OverrideRequirements).
        /// </summary>
        private Control OverrideRow()
        {
            var p = new Panel { Height = 66, BackColor = Color.Transparent, Visible = MarriageService.IsAdmin };
            _override.Location = new Point(0, 0);
            _overrideStatus.AutoSize = false; _overrideStatus.Location = new Point(0, 38); _overrideStatus.Size = new Size(560, 26);
            p.Controls.Add(_override); p.Controls.Add(_overrideStatus);
            return p;
        }

        private void DoOverride()
        {
            if (_l.RequirementsOverrideBy.HasValue)
            {
                if (!MUi.Confirm(this, "Withdraw override", "Withdraw the requirements override on this application?",
                        "Reason on file|" + _l.RequirementsOverrideReason)) return;
                try { MarriageService.ClearRequirementsOverride(_l.Id); _l.RequirementsOverrideBy = null; _l.RequirementsOverrideReason = null; Recompute(); }
                catch (Exception ex) { MUi.Fail(this, ex); }
                return;
            }
            string reason = MUi.Ask(this, "Admin Override",
                "This application is missing or has unverified requirement attachments. Issuing it anyway is an Admin decision and will be permanently recorded on the application and in the audit trail.\n\nReason for overriding:", "");
            if (reason == null) return;
            try
            {
                MarriageService.OverrideRequirements(_l.Id, reason);
                LicenseFacts fresh = MarriageService.LoadLicense(_l.Id);
                _l.RequirementsOverrideBy = fresh.RequirementsOverrideBy;
                _l.RequirementsOverrideAt = fresh.RequirementsOverrideAt;
                _l.RequirementsOverrideReason = fresh.RequirementsOverrideReason;
                Recompute();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private static Control Check(string text, bool ok)
        {
            var l = MUi.Txt((ok ? "✓  " : "✕  ") + text, 9.5F, ok ? FontStyle.Bold : FontStyle.Regular, ok ? UiTheme.Success : UiTheme.Danger);
            l.AutoSize = false; l.Height = 24;
            return l;
        }

        private void Recompute()
        {
            DateTime d = _date.Value.Date;
            _no.Text = MarriageService.PeekNextLicenseNo(d) + "   (assigned on issue)";
            _valid.Text = "Valid until  " + MUi.D(MarriageRules.Expiry(d, _s));
            List<RuleIssue> rawIssues = MarriageRules.ValidateForIssue(_l, MarriageService.Catalog(), d, _s).Where(i => i.Blocks).ToList();
            List<RuleIssue> issues = MarriageRules.ApplyOverride(rawIssues, _l);
            _issues.SetIssues(issues, "Every check passes for this issue date.");
            _go.Enabled = issues.Count == 0 && MarriageService.IsRegistrar;
            if (!MarriageService.IsRegistrar) _issues.SetIssues(new[] { new RuleIssue(RuleSeverity.Blocking, "ROLE", "Only a Registrar or Admin can issue a licence.", null) });

            bool hasReqIssues = rawIssues.Any(i => i.Code.StartsWith("REQ_"));
            bool active = _l.RequirementsOverrideBy.HasValue;
            _override.Text = active ? "Withdraw Requirements Override" : "Admin Override - Missing Requirements";
            _override.Visible = MarriageService.IsAdmin && (hasReqIssues || active);
            _overrideStatus.Text = active
                ? "OVERRIDDEN: " + _l.RequirementsOverrideReason
                : (hasReqIssues ? "Missing/unverified requirement attachments are blocking this licence." : "");
        }

        private LicenseFacts Hypothetical()
        {
            return new LicenseFacts
            {
                Id = _l.Id, ApplicationNo = _l.ApplicationNo, LicenseNo = MarriageService.PeekNextLicenseNo(_date.Value), Husband = _l.Husband, Wife = _l.Wife,
                IssueDate = _date.Value.Date, ExpiryDate = MarriageRules.Expiry(_date.Value.Date, _s), StoredStatus = "Preview"
            };
        }

        private void DoIssue()
        {
            DateTime d = _date.Value.Date;
            if (!MUi.Confirm(this, "Issue license", "Issue this marriage license?",
                    "Applicants|" + _l.Husband.FullName + " & " + _l.Wife.FullName, "Issue date|" + MUi.D(d),
                    "Valid until|" + MUi.D(MarriageRules.Expiry(d, _s))))
                return;
            try
            {
                string no;
                List<RuleIssue> issues = MarriageService.IssueLicense(_l.Id, d, out no);
                if (issues.Count > 0) { _issues.SetIssues(issues); return; }
                LicensePrinter.Show(this, MarriageService.LoadLicense(_l.Id), false);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }
    }

    /// <summary>
    /// Prints the licence CROMS issued. A PREVIEW carries a banner and a diagonal wash on the
    /// page so it can never pass as an issued licence - the same rule the OCR form preview holds.
    /// </summary>
    internal static class LicensePrinter
    {
        public static void Show(IWin32Window owner, LicenseFacts l, bool preview)
        {
            string office = "Office of the Local Civil Registrar", muni = "", prov = "", registrar = "", title = "Municipal Civil Registrar";
            try
            {
                DataTable o = Db.Pull("SELECT * FROM office_profile LIMIT 1");
                if (o.Rows.Count > 0)
                {
                    DataRow r = o.Rows[0];
                    office = r["office_name"] as string ?? office; muni = r["municipality"] as string ?? ""; prov = r["province"] as string ?? "";
                    registrar = r["registrar_name"] as string ?? ""; title = r["registrar_title"] as string ?? title;
                }
            }
            catch { }

            var doc = new PrintDocument { DocumentName = (preview ? "PREVIEW " : "") + "Marriage License " + l.LicenseNo };
            doc.PrintPage += (s, e) => Draw(e, l, preview, office, muni, prov, registrar, title);
            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 1000, Text = preview ? "LICENSE PREVIEW (not a license)" : "Marriage License " + l.LicenseNo })
                dlg.ShowDialog(owner);
        }

        private static void Draw(PrintPageEventArgs e, LicenseFacts l, bool preview, string office, string muni, string prov, string registrar, string rtitle)
        {
            Graphics g = e.Graphics;
            Rectangle m = e.MarginBounds;
            float y = m.Top;
            using (var fS = new Font("Times New Roman", 10F))
            using (var fB = new Font("Times New Roman", 10.5F, FontStyle.Bold))
            using (var fT = new Font("Times New Roman", 18F, FontStyle.Bold))
            using (var fH = new Font("Times New Roman", 11F, FontStyle.Bold))
            {
                var center = new StringFormat { Alignment = StringAlignment.Center };
                Action<string, Font> C = (t, f) => { g.DrawString(t, f, Brushes.Black, new RectangleF(m.Left, y, m.Width, 40), center); y += f.GetHeight(g) + 2; };
                C("Republic of the Philippines", fS);
                if (!string.IsNullOrEmpty(prov)) C("Province of " + prov, fS);
                if (!string.IsNullOrEmpty(muni)) C("Municipality of " + muni, fS);
                C(office.ToUpperInvariant(), fB);
                y += 18; C("MARRIAGE LICENSE", fT); y += 4;
                C("License No. " + (l.LicenseNo ?? "-") + "     Application No. " + (l.ApplicationNo ?? "-"), fB);
                y += 16;
                g.DrawString("This is to certify that, the requirements of law having been complied with, a license to contract marriage is hereby issued to:",
                    fS, Brushes.Black, new RectangleF(m.Left, y, m.Width, 40));
                y += 42;
                float colW = m.Width / 2f - 10;
                float y0 = y;
                y = Party(g, l.Husband, "HUSBAND / CONTRACTING PARTY", m.Left, y0, colW, fH, fS, l.IssueDate ?? DateTime.Today);
                float y2 = Party(g, l.Wife, "WIFE / CONTRACTING PARTY", m.Left + colW + 20, y0, colW, fH, fS, l.IssueDate ?? DateTime.Today);
                y = Math.Max(y, y2) + 20;
                g.DrawString("Date of issue:  " + MarriageRules.D(l.IssueDate), fB, Brushes.Black, m.Left, y); y += 22;
                g.DrawString("Valid until:      " + MarriageRules.D(l.ExpiryDate), fB, Brushes.Black, m.Left, y); y += 30;
                g.DrawString("This license is valid in any part of the Philippines for a period of one hundred twenty (120) days from the date of issue, " +
                             "and shall be deemed automatically cancelled at the expiration of that period if the contracting parties have not made use of it " +
                             "(Family Code, Art. 20).", fS, Brushes.Black, new RectangleF(m.Left, y, m.Width, 60));
                y += 90;
                float sx = m.Right - 280;
                g.DrawLine(Pens.Black, sx, y, m.Right, y);
                g.DrawString(string.IsNullOrEmpty(registrar) ? "(name of the Civil Registrar)" : registrar.ToUpperInvariant(), fB, Brushes.Black, new RectangleF(sx, y + 2, 280, 20), center);
                g.DrawString(rtitle, fS, Brushes.Black, new RectangleF(sx, y + 20, 280, 20), center);
                g.DrawString("Printed from CROMS " + DateTime.Now.ToString("dd MMM yyyy HH:mm"), new Font("Segoe UI", 7F), Brushes.Gray, m.Left, m.Bottom - 12);
            }
            if (preview)
            {
                using (var red = new SolidBrush(Color.FromArgb(198, 50, 63)))
                using (var f = new Font("Segoe UI", 12F, FontStyle.Bold))
                {
                    g.FillRectangle(red, e.PageBounds.Left, e.PageBounds.Top, e.PageBounds.Width, 34);
                    g.DrawString("PREVIEW - NOT A MARRIAGE LICENSE - NOT ISSUED", f, Brushes.White, e.PageBounds.Left + 20, e.PageBounds.Top + 7);
                }
                var st = g.Save();
                g.TranslateTransform(e.PageBounds.Width / 2f, e.PageBounds.Height / 2f);
                g.RotateTransform(-35);
                using (var wash = new SolidBrush(Color.FromArgb(40, 198, 50, 63)))
                using (var f = new Font("Segoe UI", 54F, FontStyle.Bold))
                    g.DrawString("PREVIEW", f, wash, -170, -40);
                g.Restore(st);
            }
        }

        private static float Party(Graphics g, Party p, string head, float x, float y, float w, Font fH, Font fS, DateTime on)
        {
            g.DrawString(head, fH, Brushes.Black, x, y); y += 22;
            Action<string, string> L = (k, v) => { g.DrawString(k + ":  " + (string.IsNullOrWhiteSpace(v) ? "-" : v), fS, Brushes.Black, new RectangleF(x, y, w, 34)); y += 19; };
            L("Name", p.FullName.ToUpperInvariant());
            L("Date of birth", MarriageRules.D(p.Dob) + (p.Dob.HasValue ? "  (age " + MarriageRules.AgeOn(p.Dob.Value, on) + ")" : ""));
            L("Place of birth", p.PlaceOfBirth);
            L("Sex", p.Sex);
            L("Citizenship", p.Citizenship);
            L("Civil status", p.CivilStatus);
            L("Residence", p.Residence);
            // Print the structured first/middle/last blocks (migration 38) rather than the
            // single joined line - falls back to p.Father/p.Mother only for a pre-38 licence
            // that has no separate cells (see MarriageRules.Party.Father/Mother comment).
            string fatherName = MarriageRules.JoinName(p.FatherFirst, p.FatherMiddle, p.FatherLast) ?? p.Father;
            string motherName = MarriageRules.JoinName(p.MotherFirst, p.MotherMiddle, p.MotherLast) ?? p.Mother;
            L("Father's Name", fatherName);
            L("Father's Citizenship", p.FatherCitizenship);
            L("Father's Residence", p.FatherResidence);
            L("Mother's Name", motherName);
            L("Mother's Citizenship", p.MotherCitizenship);
            L("Mother's Residence", p.MotherResidence);
            return y;
        }
    }
}
