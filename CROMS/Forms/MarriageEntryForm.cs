using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// WINDOW 4 - Form 97, the Certificate of Marriage. Five tabs carrying the paper's own
    /// blocks; the two contracting parties side by side because MF-97 IS a two-column table.
    ///
    /// The single most important change from the old dialog: a licensed marriage LINKS to the
    /// licence record (marriages.license_id -> marriage_licenses.id) instead of retyping its
    /// number, so CROMS can check the licence exists, was issued, covers the wedding date, names
    /// the same couple, and has not already been spent. Licence-exempt marriages take their own
    /// path and never get a fabricated licence number.
    ///
    /// Nothing is registered from here without passing MarriageService.Register, which
    /// re-validates on the server side - the checks on this screen are for the clerk, not the
    /// only line of defence.
    /// <para/>
    /// UI chrome and field controls are declared in MarriageEntryForm.Designer.cs; this file
    /// holds the constructor's data/logic tail plus every other method.
    /// </summary>
    public partial class MarriageEntryForm : Form
    {
        private sealed class SP
        {
            public TextBox First, Middle, Last, Father, Mother, ConsentName, ConsentRel, ConsentRes;
            public ComboBox Sex, FatherCit, MotherCit;
            public DateTimePicker Dob;
            public Label Age;
            public ComboBox BirthCountry, BirthProv, BirthMuni, Cit, Rel, Res, Civil;
        }

        private ComboBox _settle;   // marriage settlement: None / Entered (null = not stated)
        private int? _id;
        private string _status = "Draft";
        private string _formCode = FormCatalog.Current(DocKind.Marriage)?.FormCode;
        private string _formName = FormCatalog.Current(DocKind.Marriage)?.FormName;
        private byte[] _scanImage;
        private string _ocrScanId;
        private DocAiResult _ocr;
        private bool _ocrPending, _loading, _dirty;
        private LicenseFacts _lic;
        private List<ReqType> _catalog;
        private readonly MarriageSettings _s = MarriageService.Settings;
        private readonly Dictionary<string, Control> _keyControls = new Dictionary<string, Control>();
        private readonly ToolTip _tip = new ToolTip();
        private int _tab;

        private static readonly string[] TabNames = { "Contracting Parties", "Parents", "Consent & License", "Solemnization", "Certification" };

        // tab 1-2
        private readonly SP _h = new SP(), _w = new SP();
        // "licence obtained in another province" - backlog Sec.4.1: trigger confirmed 2026-09-13,
        // which document proves it is still open, so this is a generic attachment slot, not a
        // named-document requirement.
        // Held only between "Scan..." and the next Save - the image is written to the
        // OUT_OF_PROVINCE_LICENSE requirement row's own attachment, which needs the row to
        // exist first (it is created by SyncMarriageRequirements inside SaveMarriage).
        private byte[] _oopScanImage;
        private string _oopScanImageName;
        private List<LicenseFacts> _allLicenses = new List<LicenseFacts>();

        public MarriageEntryForm(int? marriageId)
        {
            InitializeComponent();

            _id = marriageId;
            Text = marriageId == null ? "Register Marriage - Municipal Form 97" : "Certificate of Marriage - Municipal Form 97";
            try { _catalog = MarriageService.Catalog(); } catch { _catalog = new List<ReqType>(); }

            BuildChrome();
            BuildParties();
            BuildParents();
            BuildLicense();
            BuildSolemnization();
            BuildCertification();
            UiTheme.Polish(this);

            _loading = true;
            if (_id.HasValue) LoadMarriage(_id.Value);
            else { _rbLic.Checked = true; MUi.Put(_recv, DateTime.Today); }
            _loading = false;
            _dirty = false;
            ReloadLicenses();
            RefreshAll();
            ShowTab(0);
            FormClosing += (s, e) =>
            {
                if (!_dirty || _status == "Registered") return;
                DialogResult r = MessageBox.Show(this, "Save this certificate as a draft before closing?", "Unsaved changes",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) e.Cancel = true;
                else if (r == DialogResult.Yes && !Save(null)) e.Cancel = true;
            };
        }

        // ===================================================================== public API (OCR hand-off)
        public void SetScanImage(byte[] bytes) { _scanImage = bytes; RefreshRail(); }

        /// <summary>The OCR run this certificate came from. Weak fields are highlighted and hold registration until reviewed.</summary>
        public void SetOcrContext(string scanId, DocAiResult result)
        {
            _ocrScanId = scanId; _ocr = result; _ocrPending = true;
            HighlightWeak();
            RefreshAll();
        }

        /// <summary>Fill the form from Document AI extraction (new records only).</summary>
        public void PrimeFromExtraction(IDictionary<string, string> f)
        {
            _loading = true;
            string v;
            if (f.TryGetValue("FormCode", out v) && !string.IsNullOrWhiteSpace(v)) _formCode = v.Trim();
            if (f.TryGetValue("FormName", out v) && !string.IsNullOrWhiteSpace(v)) _formName = v.Trim();
            Action<TextBox, string> set = (t, k) => { string x; if (f.TryGetValue(k, out x) && !string.IsNullOrWhiteSpace(x)) t.Text = x.Trim(); };
            Action<DateTimePicker, string> date = (d, k) =>
            {
                string x; DateTime p;
                // yyyy-MM-dd only: "2-6-2018" could be 2 June or 6 February, and the form must not commit to a guess.
                if (f.TryGetValue(k, out x) && DateTime.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out p)) MUi.Put(d, p);
            };
            set(_reg, "RegistryNo");
            set(_h.First, "HusbandFirst"); set(_h.Middle, "HusbandMiddle"); set(_h.Last, "HusbandLast");
            set(_w.First, "WifeFirst"); set(_w.Middle, "WifeMiddle"); set(_w.Last, "WifeLast");
            set(_h.Father, "HusbandFatherName"); set(_h.Mother, "HusbandMotherName");
            set(_w.Father, "WifeFatherName"); set(_w.Mother, "WifeMotherName");
            set(_sol, "Solemnizer"); set(_solPos, "SolemnizerPosition"); set(_w1, "Witness1"); set(_w2, "Witness2");
            set(_recvBy, "ReceivedByName"); set(_recvTitle, "ReceivedByTitle");
            date(_dom, "DateOfMarriage"); date(_recv, "ReceivedByDate");
            if (f.TryGetValue("Nationality", out v) && !string.IsNullOrWhiteSpace(v)) { SelectByName(_h.Cit, v); SelectByName(_w.Cit, v); }
            if (f.TryGetValue("PlaceOfMarriage", out v) && !string.IsNullOrWhiteSpace(v)) SelectByName(_church, v);
            if (f.TryGetValue("LicenseNo", out v) && !string.IsNullOrWhiteSpace(v))
            {
                _licSearch.Text = v.Trim();
                LicenseFacts match = _allLicenses.FirstOrDefault(l => string.Equals(l.LicenseNo, v.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null) { _rbLic.Checked = true; SelectLicense(match); }
            }
            _loading = false;
            _dirty = true;
            RefreshAll();
        }

        // ===================================================================== layout
        private static void Stack(Control host, params Control[] topToBottom)
        {
            for (int i = topToBottom.Length - 1; i >= 0; i--) { topToBottom[i].Dock = DockStyle.Top; host.Controls.Add(topToBottom[i]); }
        }

        private void BuildChrome()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = UiTheme.Navy };
            var t = MUi.Txt(_id == null ? "Register Marriage  -  Municipal Form 97" : "Certificate of Marriage  -  Municipal Form 97", 12.5F, FontStyle.Bold, Color.White);
            t.Location = new Point(18, 15);
            header.Controls.Add(t); header.Controls.Add(_formPill); header.Controls.Add(_statusPill);
            header.Layout += (s, e) => { _formPill.Location = new Point(t.Right + 14, 16); _statusPill.Location = new Point(_formPill.Right + 8, 16); };

            _tabs.AddStep("Contracting Parties", "items 1-8");
            _tabs.AddStep("Parents", "items 9-12");
            _tabs.AddStep("Consent & License", "items 13-16 · licence link");
            _tabs.AddStep("Solemnization", "items 17-19");
            _tabs.AddStep("Certification", "items 20-22 · receipt");
            _tabs.StepClicked += ShowTab;

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(250, 251, 253), Padding = new Padding(14, 12, 14, 12) };
            footer.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 590, BackColor = Color.Transparent };
            left.Controls.Add(_btnSoft); left.Controls.Add(_btnPreview); left.Controls.Add(_btnCase); left.Controls.Add(_btnAckSlip);
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 470, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent };
            right.Controls.Add(_btnRegister); right.Controls.Add(_btnReview); right.Controls.Add(_btnDraft);
            _footInfo.AutoSize = false; _footInfo.Dock = DockStyle.Fill; _footInfo.TextAlign = ContentAlignment.MiddleRight; _footInfo.AutoEllipsis = true;
            footer.Controls.Add(_footInfo); footer.Controls.Add(right); footer.Controls.Add(left);
            _btnSoft.Click += (s, e) => ViewSoftcopy();
            _btnPreview.Click += (s, e) => PreviewOnForm();
            _btnCase.Click += (s, e) => OpenCase();
            _btnAckSlip.Click += (s, e) => PrintAckSlip();
            _btnDraft.Click += (s, e) => Save("Draft");
            _btnReview.Click += (s, e) =>
            {
                if (!Save("For Review")) return;
                if (MessageBox.Show(this,
                        "Sent for review.\n\nPrint an acknowledgment slip for this submission?\n" +
                        "It only acknowledges receipt - it is not a certificate.",
                        "Acknowledgment Slip", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes) PrintAckSlip();
            };
            _btnRegister.Click += (s, e) => Register();

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = UiTheme.Surface };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            var host = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i] = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 14, 18, 14), Visible = false, BackColor = UiTheme.Surface };
                host.Controls.Add(_pages[i]);
            }
            _rail.Dock = DockStyle.Fill; _rail.AutoScroll = true; _rail.Padding = new Padding(14, 12, 14, 12); _rail.BackColor = Color.FromArgb(250, 251, 253);
            _rail.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, 0, _rail.Height); };
            body.Controls.Add(host, 0, 0); body.Controls.Add(_rail, 1, 0);
            Controls.Add(body); Controls.Add(footer); Controls.Add(_tabs); Controls.Add(header);

            _issues.FixRequested += where =>
            {
                int i = Array.IndexOf(TabNames, where);
                if (i >= 0) ShowTab(i);
                else if (where == "Case Workflow") OpenCase();
                else if (where == "Source Document") CompareWithScan();
            };
        }

        private static Control Section(string t, string sub) { return MUi.SectionHeader(t, sub); }

        private Control SpouseCard(string title, Color tint, Color ink, Control inner)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 12, 0) };
            card.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1); };
            var head = new Label { Text = title, Dock = DockStyle.Top, Height = 30, BackColor = tint, ForeColor = ink, Font = MUi.F(9F, FontStyle.Bold),
                                   TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), UseMnemonic = false };
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner); card.Controls.Add(head);
            return card;
        }

        private TableLayoutPanel TwoColumns(int height, Control left, Control right)
        {
            var t = new TableLayoutPanel { ColumnCount = 2, Height = height, BackColor = Color.Transparent };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.Controls.Add(left, 0, 0); t.Controls.Add(right, 1, 0);
            return t;
        }

        private Control PartyInner(SP p, string pre)
        {
            var inner = new Panel { Padding = new Padding(12, 6, 2, 6), BackColor = Color.Transparent };
            p.First = MUi.Box(); p.Middle = MUi.Box(); p.Last = MUi.Box();
            p.Dob = MUi.Date(true); p.Age = MUi.Txt("-", 10F, FontStyle.Bold, UiTheme.Muted);
            // Country/Province/Municipality, same trio and the same GeoLookup wiring as Birth
            // Registration and the Marriage Licence (Form 90) - editable so a foreign locality
            // can be typed, matching every other place-of-birth field in the app (2026-09-14).
            p.BirthCountry = MUi.Combo(true); p.BirthProv = MUi.Combo(true); p.BirthMuni = MUi.Combo(true);
            p.Cit = MUi.Combo(false); p.Rel = MUi.Combo(false); p.Res = MUi.Combo(false); p.Civil = MUi.Combo(false, MarriageRules.CivilStatuses);
            Bind(p.Cit, Read("nationalities")); Bind(p.Rel, Read("religions")); Bind(p.Res, Read("residences"));
            GeoLookup.LoadCountries(p.BirthCountry);
            GeoLookup.CascadeCountry(p.BirthCountry, p.BirthProv, p.BirthMuni, null);
            GeoLookup.Select(p.BirthCountry, GeoLookup.HomeCountry);

            TableLayoutPanel names = MUi.Grid(3, 1, 56);
            names.Controls.Add(MUi.Field("First", p.First), 0, 0); names.Controls.Add(MUi.Field("Middle", p.Middle), 1, 0); names.Controls.Add(MUi.Field("Last", p.Last), 2, 0);
            TableLayoutPanel dob = MUi.Grid(2, 1, 56);
            dob.Controls.Add(MUi.Field("Date of birth", p.Dob), 0, 0);
            var ap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }; p.Age.AutoSize = false; p.Age.Dock = DockStyle.Fill; p.Age.TextAlign = ContentAlignment.MiddleLeft; ap.Controls.Add(p.Age);
            dob.Controls.Add(MUi.Field("Age (computed)", ap), 1, 0);
            // Two rows, not three columns on one - "City / municipality of birth" is the longest
            // caption on the card, same reasoning as Form 90's own place-of-birth layout.
            TableLayoutPanel pob1 = MUi.Grid(2, 1, 56);
            pob1.Controls.Add(MUi.Field("Country of birth", p.BirthCountry), 0, 0); pob1.Controls.Add(MUi.Field("Province of birth", p.BirthProv), 1, 0);
            TableLayoutPanel pob2 = MUi.Grid(1, 1, 56);
            pob2.Controls.Add(MUi.Field("City / municipality of birth", p.BirthMuni), 0, 0);
            TableLayoutPanel cr = MUi.Grid(2, 1, 56);
            cr.Controls.Add(MUi.Field("Citizenship", p.Cit), 0, 0); cr.Controls.Add(MUi.Field("Religion", p.Rel), 1, 0);
            TableLayoutPanel cs = MUi.Grid(2, 1, 56);
            cs.Controls.Add(MUi.Field("Civil status", p.Civil), 0, 0); cs.Controls.Add(MUi.Field("Residence", p.Res), 1, 0);
            // Sex (item on the printed sheet): pre-picked from the column - the Family Code makes the
            // husband male and the wife female - but stored and editable, never derived at print time.
            p.Sex = MUi.Combo(false, new[] { "Male", "Female" });
            p.Sex.SelectedItem = pre == "Husband" ? "Male" : "Female";
            p.Sex.SelectedIndexChanged += (s, e) => Changed(p.Sex);
            TableLayoutPanel sx = MUi.Grid(2, 1, 56);
            sx.Controls.Add(MUi.Field("Sex", p.Sex), 0, 0);
            Stack(inner, names, dob, pob1, pob2, cr, cs, sx);

            _keyControls[pre + "First"] = p.First; _keyControls[pre + "Middle"] = p.Middle; _keyControls[pre + "Last"] = p.Last;
            foreach (Control c in new Control[] { p.First, p.Middle, p.Last }) c.TextChanged += (s, e) => Changed(c);
            foreach (ComboBox c in new[] { p.Cit, p.Rel, p.Res, p.Civil }) c.SelectedIndexChanged += (s, e) => Changed(c);
            foreach (ComboBox c in new[] { p.BirthCountry, p.BirthProv, p.BirthMuni }) { c.SelectedIndexChanged += (s, e) => Changed(c); c.TextChanged += (s, e) => Changed(c); }
            p.Dob.ValueChanged += (s, e) => Changed(p.Dob);
            LearningLibrary.Attach(p.First, LearningLibrary.GivenName);
            LearningLibrary.Attach(p.Last, LearningLibrary.Surname);
            return inner;
        }

        private void BuildParties()
        {
            Panel pg = _pages[0];
            TableLayoutPanel top = MUi.Grid(3, 1, 58);
            top.Controls.Add(MUi.Field("Registry No.", _reg), 0, 0);
            _tip.SetToolTip(_reg, "Leave blank - CROMS assigns it on registration. Fill it only when digitizing a certificate that already carries one.");
            top.Controls.Add(MUi.Field("Book / volume", _book), 1, 0);
            top.Controls.Add(MUi.Field("Page", _page), 2, 0);
            _reg.TextChanged += (s, e) => Changed(_reg); _book.TextChanged += (s, e) => Changed(_book); _page.TextChanged += (s, e) => Changed(_page);
            _keyControls["RegistryNo"] = _reg;
            // 400, not 344: the place-of-birth block grew from one 56px row (province+municipality)
            // to two (country+province, then municipality alone) when Country was added
            // 2026-09-14 - `inner` has no AutoScroll of its own, so a card shorter than its
            // content would silently clip the Civil status/Residence row off the bottom.
            var cols = TwoColumns(456,
                SpouseCard("HUSBAND / PARTY 1", UiTheme.AccentTint, Color.FromArgb(27, 62, 158), PartyInner(_h, "Husband")),
                SpouseCard("WIFE / PARTY 2", Color.FromArgb(245, 237, 251), Color.FromArgb(107, 48, 150), PartyInner(_w, "Wife")));
            Stack(pg, Section("Contracting parties", "Read down the paper: the husband's column on the left, the wife's on the right. Age is computed from the date of birth on the marriage date."),
                  top, cols);
        }

        private void BuildParents()
        {
            Panel pg = _pages[1];
            List<string> nations = Read("nationalities").Rows.Cast<DataRow>().Select(x => Convert.ToString(x["name"])).Where(x => x.Length > 0).ToList();
            Func<SP, string, Control> par = (p, pre) =>
            {
                var inner = new Panel { Padding = new Padding(12, 6, 2, 6), BackColor = Color.Transparent };
                p.Father = MUi.Box(); p.Mother = MUi.Box();
                p.FatherCit = MUi.Combo(true, nations); p.MotherCit = MUi.Combo(true, nations);
                p.ConsentName = MUi.Box(); p.ConsentRel = MUi.Box(); p.ConsentRes = MUi.Box();
                TableLayoutPanel g1 = MUi.Grid(2, 1, 56);
                g1.Controls.Add(MUi.Field("Name of father", p.Father), 0, 0); g1.Controls.Add(MUi.Field("Father's citizenship", p.FatherCit), 1, 0);
                TableLayoutPanel g2 = MUi.Grid(2, 1, 56);
                g2.Controls.Add(MUi.Field("Maiden name of mother", p.Mother), 0, 0); g2.Controls.Add(MUi.Field("Mother's citizenship", p.MotherCit), 1, 0);
                // "Persons who gave consent or advice": ONE person per party, as on the sheet and Form 90.
                TableLayoutPanel g3 = MUi.Grid(1, 1, 56); g3.Controls.Add(MUi.Field("Person who gave consent or advice (name)", p.ConsentName), 0, 0);
                TableLayoutPanel g4 = MUi.Grid(2, 1, 56);
                g4.Controls.Add(MUi.Field("Relationship", p.ConsentRel), 0, 0); g4.Controls.Add(MUi.Field("Residence", p.ConsentRes), 1, 0);
                Stack(inner, g1, g2, g3, g4);
                _keyControls[pre + "FatherName"] = p.Father; _keyControls[pre + "MotherName"] = p.Mother;
                foreach (Control c in new Control[] { p.Father, p.Mother, p.FatherCit, p.MotherCit, p.ConsentName, p.ConsentRel, p.ConsentRes })
                    c.TextChanged += (s, e) => Changed(c);
                return inner;
            };
            var cols = TwoColumns(250,
                SpouseCard("HUSBAND'S PARENTS", UiTheme.AccentTint, Color.FromArgb(27, 62, 158), par(_h, "Husband")),
                SpouseCard("WIFE'S PARENTS", Color.FromArgb(245, 237, 251), Color.FromArgb(107, 48, 150), par(_w, "Wife")));
            Stack(pg, Section("Parents of the contracting parties", "Items 9-12, and the person who gave consent or advice."), cols);
        }

        private void BuildLicense()
        {
            Panel pg = _pages[2];
            var basis = new FlowLayoutPanel { Height = 40, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
            basis.Controls.Add(_rbLic); basis.Controls.Add(_rbEx);
            _rbLic.CheckedChanged += (s, e) => { if (!_loading) { _dirty = true; RefreshAll(); } };
            _rbEx.CheckedChanged += (s, e) => { if (!_loading) { _dirty = true; RefreshAll(); } };

            // licensed - locally, or obtained in another province (backlog Sec.4.1)
            var oopRow = new FlowLayoutPanel { Height = 30, BackColor = Color.Transparent, Dock = DockStyle.Top };
            oopRow.Controls.Add(_oop);
            _oop.CheckedChanged += (s, e) => { if (!_loading) { _dirty = true; RefreshAll(); } };

            var searchRow = MUi.Grid(3, 1, 58);
            var sf = MUi.Field("Select marriage license - search name or licence number", _licSearch);
            searchRow.Controls.Add(sf, 0, 0); searchRow.SetColumnSpan(sf, 2);
            var allP = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 28, 0, 0), BackColor = Color.Transparent };
            allP.Controls.Add(_licAll); _licAll.Dock = DockStyle.Left;
            searchRow.Controls.Add(allP, 2, 0);
            _licSearch.TextChanged += (s, e) => FilterLicenses();
            _licAll.CheckedChanged += (s, e) => FilterLicenses();
            _keyControls["LicenseNo"] = _licSearch;
            _licList.Height = 128; _licList.DrawMode = DrawMode.OwnerDrawFixed; _licList.ItemHeight = 40;
            _licList.DrawItem += DrawLicenseItem;
            _licList.SelectedIndexChanged += (s, e) => { if (!_loading && _licList.SelectedItem is LicenseFacts) { SelectLicense((LicenseFacts)_licList.SelectedItem); _dirty = true; } };
            _licSummary.Height = 120; _licSummary.BackColor = Color.Transparent;
            var copy = MUi.Btn("Copy applicants from licence", MUi.Kind.Secondary);
            copy.Click += (s, e) => CopyFromLicense();
            Stack(_localPanel, searchRow, _licList, _licSummary);
            _localPanel.Height = 58 + 128 + 120 + 6;

            var oopFieldsRow = MUi.Grid(2, 1, 58);
            oopFieldsRow.Controls.Add(MUi.Field("Licence number (from the issuing LCRO)", _oopLicNo), 0, 0);
            oopFieldsRow.Controls.Add(MUi.Field("Date issued", _oopLicDate), 1, 0);
            _oopLicNo.TextChanged += (s, e) => Changed(_oopLicNo);
            _oopLicDate.ValueChanged += (s, e) => Changed(_oopLicDate);
            var oopScanRow = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 2, 0, 0), BackColor = Color.Transparent };
            var oopScanBtn = MUi.Btn("Scan / attach license image...", MUi.Kind.Secondary);
            oopScanBtn.Click += (s, e) => ScanOopLicense();
            oopScanRow.Controls.Add(oopScanBtn); oopScanBtn.Dock = DockStyle.Left;
            var oopHint = new Banner();
            oopHint.Set(RuleSeverity.Info, "Licence from another province - no proof document is required.",
                "It is lawful to apply for the licence in one province and marry in another; type the number and date (or Scan the image to read them off it, verify what was read). Attaching a copy is optional - only if the applicant happens to have one. The licence's own 120-day validity still applies regardless of which office issued it. The image and typed data are both saved on this record.");
            Stack(_oopPanel, oopFieldsRow, oopScanRow, oopHint);
            _oopPanel.Height = 58 + 34 + 76;

            var placeRow = MUi.Grid(2, 1, 58);
            placeRow.Controls.Add(MUi.Field("Licence issued at (place)", _licPlace), 0, 0);
            var cp = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 0, 0), BackColor = Color.Transparent };
            cp.Controls.Add(copy); copy.Dock = DockStyle.Left;
            placeRow.Controls.Add(cp, 1, 0);
            _licPlace.TextChanged += (s, e) => Changed(_licPlace);
            Stack(_licPanel, oopRow, _localPanel, _oopPanel, placeRow);

            // exempt
            for (int i = 0; i < MarriageRules.ExemptionBases.GetLength(0); i++) _exBasis.Items.Add(MarriageRules.ExemptionBases[i, 1]);
            var exRow = MUi.Grid(2, 1, 58);
            exRow.Controls.Add(MUi.Field("Legal basis of the exemption", _exBasis), 0, 0);
            exRow.Controls.Add(MUi.Field("Notes", _exNotes), 1, 0);
            _exBasis.SelectedIndexChanged += (s, e) => Changed(_exBasis); _exNotes.TextChanged += (s, e) => Changed(_exNotes);
            var exHint = new Banner();
            exHint.Set(RuleSeverity.Info, "No licence number is created for an exempt marriage.",
                "Record the basis, attach the supporting affidavit below or in the case workflow, and have the registrar review it.");
            Stack(_exPanel, exRow, exHint);
            _exPanel.Height = 58 + 70;

            _docs.AllowBypass = MarriageService.IsAdmin;
            _docs.Changed += () => RefreshAll();
            Stack(pg, Section("Consent & license", "Is this marriage under a licence (select it - never retype it) or licence-exempt (state the law)?"),
                  basis, _licPanel, _exPanel, Section("Supporting documents on this record", null), _docsHint, _docs);
        }

        private void BuildSolemnization()
        {
            Panel pg = _pages[3];
            Bind(_church, Read("churches")); Bind(_prov, Read("provinces")); Bind(_muni, Empty());
            _prov.SelectedIndexChanged += (s, e) => { ReloadMunis(_prov, _muni); Changed(_prov); };
            TableLayoutPanel d = MUi.Grid(3, 1, 58);
            d.Controls.Add(MUi.Field("Date of marriage", _dom), 0, 0); d.Controls.Add(MUi.Field("Time", _tom), 1, 0);
            TableLayoutPanel p = MUi.Grid(3, 1, 58);
            p.Controls.Add(MUi.Field("Church / venue", _church), 0, 0); p.Controls.Add(MUi.Field("Province", _prov), 1, 0); p.Controls.Add(MUi.Field("City / municipality", _muni), 2, 0);
            TableLayoutPanel so = MUi.Grid(2, 1, 58);
            so.Controls.Add(MUi.Field("Solemnizing officer", _sol), 0, 0); so.Controls.Add(MUi.Field("Position / designation", _solPos), 1, 0);
            TableLayoutPanel wi = MUi.Grid(2, 1, 58);
            wi.Controls.Add(MUi.Field("Witness 1", _w1), 0, 0); wi.Controls.Add(MUi.Field("Witness 2", _w2), 1, 0);
            _settle = MUi.Combo(false, new[] { "None", "Entered" });
            _settle.SelectedIndexChanged += (s, e) => Changed(_settle);
            TableLayoutPanel st = MUi.Grid(2, 1, 58);
            st.Controls.Add(MUi.Field("Marriage settlement (have not entered / have entered)", _settle), 0, 0);
            var note = new Banner();
            note.Set(RuleSeverity.Info, "CROMS records the officer; it does not certify the officer's authority.",
                "Whether this officer may solemnize (Family Code Art. 7) is confirmed by the registrar from the office's own records.");
            _keyControls["DateOfMarriage"] = _dom; _keyControls["PlaceOfMarriage"] = _church; _keyControls["Solemnizer"] = _sol;
            _keyControls["SolemnizerPosition"] = _solPos; _keyControls["Witness1"] = _w1; _keyControls["Witness2"] = _w2;
            _dom.ValueChanged += (s, e) => Changed(_dom);
            foreach (Control c in new Control[] { _tom, _sol, _solPos, _w1, _w2 }) c.TextChanged += (s, e) => Changed(c);
            _church.SelectedIndexChanged += (s, e) => Changed(_church); _muni.SelectedIndexChanged += (s, e) => Changed(_muni);
            LearningLibrary.Attach(_sol, LearningLibrary.Officer);
            Stack(pg, Section("Solemnization", "Items 17-19: when, where, by whom, before whom."), d, p, so, wi, st, note);
        }

        private void BuildCertification()
        {
            Panel pg = _pages[4];
            TableLayoutPanel r = MUi.Grid(3, 1, 58);
            r.Controls.Add(MUi.Field("Received at this office by", _recvBy), 0, 0); r.Controls.Add(MUi.Field("Title / position", _recvTitle), 1, 0);
            r.Controls.Add(MUi.Field("Date received", _recv), 2, 0);
            TableLayoutPanel dl = MUi.Grid(1, 1, 58); dl.Controls.Add(MUi.Field("Reason for delay (delayed registrations only)", _delay), 0, 0);
            TableLayoutPanel rm = MUi.Grid(1, 1, 58); rm.Controls.Add(MUi.Field("Remarks", _remarks), 0, 0);
            _keyControls["ReceivedByName"] = _recvBy; _keyControls["ReceivedByTitle"] = _recvTitle; _keyControls["ReceivedByDate"] = _recv;
            foreach (Control c in new Control[] { _recvBy, _recvTitle, _delay, _remarks }) c.TextChanged += (s, e) => Changed(c);
            _recv.ValueChanged += (s, e) => Changed(_recv);
            Stack(pg, Section("Certification and receipt", "The date the certificate reached this office decides timely vs delayed registration."), r, _regBanner, dl, rm);
        }

        // ===================================================================== lookups
        private static DataTable Empty() { var dt = new DataTable(); dt.Columns.Add("id", typeof(int)); dt.Columns.Add("name", typeof(string)); return dt; }

        private static DataTable Read(string table)
        {
            try { return Db.Pull("SELECT id, name FROM " + table + " ORDER BY name"); } catch { return Empty(); }
        }

        private static void Bind(ComboBox c, DataTable dt)
        {
            DataRow blank = dt.NewRow(); blank["id"] = 0; blank["name"] = ""; dt.Rows.InsertAt(blank, 0);
            c.DataSource = dt; c.DisplayMember = "name"; c.ValueMember = "id"; c.SelectedValue = 0;
        }

        private static int Id(ComboBox c) { int n; return c.SelectedValue != null && int.TryParse(c.SelectedValue.ToString(), out n) ? n : 0; }

        private static void SetId(ComboBox c, object v) { c.SelectedValue = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v); }

        private static void SelectByName(ComboBox c, string name)
        {
            for (int i = 0; i < c.Items.Count; i++)
                if (c.Items[i] is DataRowView && string.Equals(((DataRowView)c.Items[i])["name"].ToString(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                { c.SelectedIndex = i; return; }
        }

        private static void ReloadMunis(ComboBox prov, ComboBox muni)
        {
            int p = Id(prov), keep = Id(muni);
            DataTable dt = p > 0 ? Db.Pull("SELECT id, name FROM municipalities WHERE province_id=@p ORDER BY name", new MySqlParameter("@p", p)) : Empty();
            Bind(muni, dt);
            if (keep > 0) SetId(muni, keep);
        }

        private static void SetPlace(ComboBox prov, ComboBox muni, object municipalityId, object provinceId)
        {
            object pid = provinceId;
            if ((pid == null || pid == DBNull.Value) && municipalityId != null && municipalityId != DBNull.Value)
            {
                DataTable m = Db.Pull("SELECT province_id FROM municipalities WHERE id=@id", new MySqlParameter("@id", municipalityId));
                if (m.Rows.Count > 0) pid = m.Rows[0][0];
            }
            SetId(prov, pid);
            ReloadMunis(prov, muni);
            SetId(muni, municipalityId);
        }

        // ===================================================================== licences
        private void ReloadLicenses()
        {
            try { _allLicenses = MarriageService.ListLicenses().Where(l => l.IssueDate.HasValue).ToList(); } catch { _allLicenses = new List<LicenseFacts>(); }
            FilterLicenses();
        }

        private void FilterLicenses()
        {
            string q = LearningLibrary.Normalize(_licSearch.Text ?? "");
            IEnumerable<LicenseFacts> src = _allLicenses;
            if (!_licAll.Checked)
                src = src.Where(l => (l.StoredStatus == "Issued" || l.StoredStatus == "Expired") && (!l.UsedByMarriageId.HasValue || l.UsedByMarriageId == _id)
                                     && (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= DateTime.Today.AddDays(-365)) || (_lic != null && l.Id == _lic.Id));
            if (q.Length > 0)
                src = src.Where(l => LearningLibrary.Normalize((l.LicenseNo ?? "") + " " + l.Husband.FullName + " " + l.Wife.FullName).Contains(q));
            bool was = _loading; _loading = true;
            _licList.BeginUpdate(); _licList.Items.Clear();
            foreach (LicenseFacts l in src.OrderByDescending(x => x.IssueDate)) _licList.Items.Add(l);
            _licList.EndUpdate();
            if (_lic != null) for (int i = 0; i < _licList.Items.Count; i++) if (((LicenseFacts)_licList.Items[i]).Id == _lic.Id) _licList.SelectedIndex = i;
            _loading = was;
        }

        private void DrawLicenseItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0) return;
            var l = (LicenseFacts)_licList.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(sel ? UiTheme.AccentTint : UiTheme.Surface)) e.Graphics.FillRectangle(b, e.Bounds);
            string disp = MarriageRules.LicenseDisplayStatus(l, DateTime.Today, _s, 0);
            TextRenderer.DrawText(e.Graphics, l.LicenseNo + "    " + l.Husband.FullName.ToUpperInvariant() + "  &  " + l.Wife.FullName.ToUpperInvariant(),
                MUiFonts.Bold9, new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 3, e.Bounds.Width - 130, 18), UiTheme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, "Issued " + MUi.D(l.IssueDate) + "  -  valid until " + MUi.D(l.ExpiryDate) +
                (l.UsedByMarriageId.HasValue && l.UsedByMarriageId != _id ? "  -  already used" : ""),
                MUiFonts.Small, new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 21, e.Bounds.Width - 130, 16), UiTheme.Muted, TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, disp.ToUpperInvariant(), MUiFonts.Bold8, new Rectangle(e.Bounds.Right - 120, e.Bounds.Y, 112, e.Bounds.Height),
                MUi.InkOf(disp), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            using (var p = new Pen(UiTheme.RowLine)) e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void SelectLicense(LicenseFacts l)
        {
            _lic = l == null ? null : MarriageService.LoadLicense(l.Id);
            RefreshAll();
        }

        private void CopyFromLicense()
        {
            if (_lic == null) { MessageBox.Show(this, "Select a licence first.", "Licence", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (!MUi.Confirm(this, "Copy", "Copy the applicants' names, dates of birth and civil status from licence " + _lic.LicenseNo + "?",
                    "Husband|" + _lic.Husband.FullName, "Wife|" + _lic.Wife.FullName)) return;
            foreach (var pair in new[] { Tuple.Create(_h, _lic.Husband), Tuple.Create(_w, _lic.Wife) })
            {
                pair.Item1.First.Text = pair.Item2.First; pair.Item1.Middle.Text = pair.Item2.Middle; pair.Item1.Last.Text = pair.Item2.Last;
                MUi.Put(pair.Item1.Dob, pair.Item2.Dob);
                if (pair.Item2.CivilStatus != null) pair.Item1.Civil.SelectedItem = pair.Item2.CivilStatus;
                if (pair.Item2.Citizenship != null) SelectByName(pair.Item1.Cit, pair.Item2.Citizenship);
                if (pair.Item2.Religion != null) SelectByName(pair.Item1.Rel, pair.Item2.Religion);
                if (string.IsNullOrWhiteSpace(pair.Item1.Father.Text)) pair.Item1.Father.Text = pair.Item2.Father;
                if (string.IsNullOrWhiteSpace(pair.Item1.Mother.Text)) pair.Item1.Mother.Text = pair.Item2.Mother;
            }
            _dirty = true;
            RefreshAll();
        }

        // ===================================================================== data
        private void LoadMarriage(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM marriages WHERE id=@id", new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            Func<string, string> S = c => r[c] == DBNull.Value ? "" : r[c].ToString();
            Func<string, DateTime?> D = c => r[c] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[c]);
            _status = S("status");
            if (S("form_code") != "") _formCode = S("form_code");
            if (S("form_name") != "") _formName = S("form_name");
            _reg.Text = S("registry_no"); _book.Text = S("book_volume"); _page.Text = S("book_page");
            foreach (var pair in new[] { Tuple.Create(_h, "husband"), Tuple.Create(_w, "wife") })
            {
                SP p = pair.Item1; string pre = pair.Item2;
                p.First.Text = S(pre + "_first_name"); p.Middle.Text = S(pre + "_middle_name"); p.Last.Text = S(pre + "_last_name");
                MUi.Put(p.Dob, D(pre + "_date_of_birth"));
                SetId(p.Cit, r[pre + "_citizenship_id"]); SetId(p.Rel, r[pre + "_religion_id"]); SetId(p.Res, r[pre + "_residence_id"]);
                p.Civil.SelectedItem = MarriageRules.CivilStatuses.Contains(S(pre + "_civil_status")) ? S(pre + "_civil_status") : null;
                // Country first - it rebuilds the province list the next two select into.
                string place = dt.Columns.Contains(pre + "_place_of_birth") ? S(pre + "_place_of_birth") : null;
                string country = dt.Columns.Contains(pre + "_birth_country") ? S(pre + "_birth_country") : null;
                if (!string.IsNullOrEmpty(place) || !string.IsNullOrEmpty(country))
                    GeoLookup.SetCountryPlace(p.BirthCountry, p.BirthProv, p.BirthMuni,
                        country, GeoLookup.ProvinceOf(place), GeoLookup.MunicipalityOf(place));
                else
                    GeoLookup.Select(p.BirthCountry, GeoLookup.HomeCountry);   // pre-2026-09-14 row: nothing stored, default to home
                p.Father.Text = S(pre + "_father_name"); p.Mother.Text = S(pre + "_mother_name");
                Func<string, string> SC = c => dt.Columns.Contains(c) ? S(c) : "";
                // NULL in the column means "not stated": show blank rather than re-deriving Male/Female.
                p.Sex.SelectedItem = SC(pre + "_sex") == "" ? null : SC(pre + "_sex");
                p.FatherCit.Text = SC(pre + "_father_citizenship"); p.MotherCit.Text = SC(pre + "_mother_citizenship");
                p.ConsentName.Text = SC(pre + "_consent_name"); p.ConsentRel.Text = SC(pre + "_consent_relationship");
                p.ConsentRes.Text = SC(pre + "_consent_residence");
            }
            _settle.SelectedItem = dt.Columns.Contains("marriage_settlement") && S("marriage_settlement") != "" ? S("marriage_settlement") : null;
            MUi.Put(_dom, D("date_of_marriage")); _tom.Text = S("time_of_marriage");
            SetId(_church, r["church_id"]);
            SetPlace(_prov, _muni, r["place_municipality_id"], r["place_province_id"]);
            _sol.Text = S("solemnizer"); _solPos.Text = S("solemnizer_position"); _w1.Text = S("witness1_name"); _w2.Text = S("witness2_name");
            _recvBy.Text = S("received_by"); _recvTitle.Text = S("received_by_title"); MUi.Put(_recv, D("received_by_date"));
            _remarks.Text = S("remarks"); _delay.Text = S("delay_reason"); _licPlace.Text = S("license_place");
            string basis = S("license_basis");
            _rbEx.Checked = basis == "Exempt"; _rbLic.Checked = basis != "Exempt";
            for (int i = 0; i < MarriageRules.ExemptionBases.GetLength(0); i++) if (MarriageRules.ExemptionBases[i, 0] == S("exemption_basis")) _exBasis.SelectedIndex = i;
            _exNotes.Text = S("exemption_notes");
            bool oop = dt.Columns.Contains("license_out_of_province") && S("license_out_of_province") == "1";
            _oop.Checked = oop;
            if (r["license_id"] != DBNull.Value) _lic = MarriageService.LoadLicense(Convert.ToInt32(r["license_id"]));
            else if (oop) { _oopLicNo.Text = S("license_no"); MUi.Put(_oopLicDate, D("license_date")); }
            else if (S("license_no") != "") _licSearch.Text = S("license_no");
            _scanImage = r["scan_image"] == DBNull.Value ? null : (byte[])r["scan_image"];
            _ocrScanId = S("ocr_scan_id") == "" ? null : S("ocr_scan_id");
        }

        private MarriageFacts UiFacts()
        {
            var m = new MarriageFacts { Id = _id ?? 0, Status = _status };
            foreach (var pair in new[] { Tuple.Create(_h, m.Husband), Tuple.Create(_w, m.Wife) })
            {
                SP b = pair.Item1; Party p = pair.Item2;
                p.First = N(b.First.Text); p.Middle = N(b.Middle.Text); p.Last = N(b.Last.Text); p.Dob = MUi.Val(b.Dob);
                p.CivilStatus = b.Civil.SelectedItem as string; p.Citizenship = Id(b.Cit) > 0 ? b.Cit.Text : null;
            }
            m.DateOfMarriage = MUi.Val(_dom); m.DateReceived = MUi.Val(_recv);
            m.HasPlace = Id(_prov) > 0 && Id(_muni) > 0;
            m.Solemnizer = N(_sol.Text); m.SolemnizerPosition = N(_solPos.Text); m.Witness1 = N(_w1.Text); m.Witness2 = N(_w2.Text);
            m.Basis = _rbEx.Checked ? "Exempt" : "Licensed";
            m.OutOfProvinceLicense = m.Basis == "Licensed" && _oop.Checked;
            m.LicenseId = m.Basis == "Licensed" && !m.OutOfProvinceLicense && _lic != null ? (int?)_lic.Id : null;
            m.ExternalLicenseNo = m.OutOfProvinceLicense ? N(_oopLicNo.Text) : null;
            m.ExternalLicenseDate = m.OutOfProvinceLicense ? MUi.Val(_oopLicDate) : null;
            m.ExemptionBasis = _exBasis.SelectedIndex >= 0 ? MarriageRules.ExemptionBases[_exBasis.SelectedIndex, 0] : null;
            m.DelayReason = N(_delay.Text);
            if (_id.HasValue)
            {
                MarriageFacts db = MarriageService.LoadMarriageFacts(_id.Value);
                if (db != null)
                {
                    m.Requirements = db.Requirements; m.CasePostingStart = db.CasePostingStart; m.RegistrarReview = db.RegistrarReview;
                    m.OcrReviewStatus = db.OcrReviewStatus; m.OcrWeakFields = db.OcrWeakFields;
                }
            }
            if (_ocrPending) { m.OcrReviewStatus = WeakCount() > 0 || (_ocr != null && _ocr.NeedsManualReview) ? "Required" : "Completed"; m.OcrWeakFields = WeakCount(); }
            return m;
        }

        private static string N(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
        private static object Nz(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
        private static object FkVal(ComboBox c) { int n = Id(c); return n == 0 ? null : (object)n; }

        private Dictionary<string, object> Values(string status)
        {
            MarriageFacts m = UiFacts();
            DateTime on = m.DateOfMarriage ?? DateTime.Today;
            var v = new Dictionary<string, object>
            {
                { "form_code", _formCode }, { "form_name", _formName },
                { "registry_no", Nz(_reg.Text) }, { "book_volume", Nz(_book.Text) }, { "book_page", Nz(_page.Text) },
                { "solemnizer", Nz(_sol.Text) }, { "solemnizer_position", Nz(_solPos.Text) },
                { "witness1_name", Nz(_w1.Text) }, { "witness2_name", Nz(_w2.Text) },
                { "church_id", FkVal(_church) }, { "place_municipality_id", FkVal(_muni) }, { "place_province_id", FkVal(_prov) },
                { "date_of_marriage", MUi.Val(_dom) }, { "time_of_marriage", Nz(_tom.Text) },
                { "received_by", Nz(_recvBy.Text) }, { "received_by_title", Nz(_recvTitle.Text) }, { "received_by_date", MUi.Val(_recv) },
                { "remarks", Nz(_remarks.Text) }, { "delay_reason", Nz(_delay.Text) },
                { "license_basis", m.Basis }, { "license_id", m.LicenseId },
                { "license_out_of_province", m.OutOfProvinceLicense ? 1 : 0 },
                { "exemption_basis", m.Basis == "Exempt" ? m.ExemptionBasis : null }, { "exemption_notes", m.Basis == "Exempt" ? Nz(_exNotes.Text) : null },
                // The typed-licence columns are kept IN STEP with the linked licence so the
                // printed certificate and legacy reports read the same number - or, for a
                // licence obtained elsewhere, carry the number/date typed straight from the
                // paper, since there is no local licence row to read them from.
                { "license_no", m.Basis == "Licensed" ? (m.OutOfProvinceLicense ? m.ExternalLicenseNo : _lic != null ? _lic.LicenseNo : null) : null },
                { "license_date", m.Basis == "Licensed" ? (m.OutOfProvinceLicense ? (object)m.ExternalLicenseDate : _lic != null ? (object)_lic.IssueDate : null) : null },
                { "license_place", m.Basis == "Licensed" ? Nz(_licPlace.Text) : null },
            };
            foreach (var pair in new[] { Tuple.Create(_h, "husband"), Tuple.Create(_w, "wife") })
            {
                SP p = pair.Item1; string pre = pair.Item2;
                DateTime? dob = MUi.Val(p.Dob);
                v[pre + "_first_name"] = Nz(p.First.Text); v[pre + "_middle_name"] = Nz(p.Middle.Text); v[pre + "_last_name"] = Nz(p.Last.Text);
                v[pre + "_date_of_birth"] = dob;
                v[pre + "_age"] = dob.HasValue ? (object)MarriageRules.AgeOn(dob.Value, on) : null;
                v[pre + "_place_of_birth"] = GeoLookup.JoinPlace(p.BirthMuni.Text, p.BirthProv.Text);
                v[pre + "_birth_country"] = N(p.BirthCountry.Text);
                v[pre + "_citizenship_id"] = FkVal(p.Cit); v[pre + "_religion_id"] = FkVal(p.Rel); v[pre + "_residence_id"] = FkVal(p.Res);
                v[pre + "_civil_status"] = p.Civil.SelectedItem as string;
                v[pre + "_father_name"] = Nz(p.Father.Text); v[pre + "_mother_name"] = Nz(p.Mother.Text);
                v[pre + "_sex"] = p.Sex.SelectedItem as string;
                v[pre + "_father_citizenship"] = Nz(p.FatherCit.Text); v[pre + "_mother_citizenship"] = Nz(p.MotherCit.Text);
                v[pre + "_consent_name"] = Nz(p.ConsentName.Text); v[pre + "_consent_relationship"] = Nz(p.ConsentRel.Text);
                v[pre + "_consent_residence"] = Nz(p.ConsentRes.Text);
            }
            v["marriage_settlement"] = _settle.SelectedItem as string;
            if (_scanImage != null) v["scan_image"] = _scanImage;
            if (status != null && _status != "Registered") v["status"] = status;
            return v;
        }

        private bool Save(string status)
        {
            if (string.IsNullOrWhiteSpace(_h.Last.Text) && string.IsNullOrWhiteSpace(_w.Last.Text))
            {
                MessageBox.Show(this, "Type at least one party's last name before saving.", "Nothing to save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowTab(0);
                return false;
            }
            try
            {
                _id = MarriageService.SaveMarriage(_id, Values(status ?? (_status == "Registered" ? null : _status)));
                if (status != null && _status != "Registered") _status = status;
                if (_ocrPending)
                {
                    MarriageService.SetOcrContext(_id.Value, _ocrScanId, _ocr != null ? _ocr.OverallConfidence : 0, WeakCount(), _ocr != null && _ocr.NeedsManualReview);
                    // Close the loop back to the upload that produced this record — see the
                    // matching comment on BirthRegistrationForm.Create(). Blank stays blank,
                    // never the upload id standing in for a registry number.
                    OcrAudit.MarkProcessed(_ocrScanId, "marriages", _id.Value, _reg.Text);
                    _ocrPending = false;
                }
                // SaveMarriage already synced marriage_requirements (it runs SyncMarriageRequirements
                // internally), so the OUT_OF_PROVINCE_LICENSE row now exists if this marriage needs
                // it - only now can the scanned image actually be attached to it.
                if (_oopScanImage != null && _id.HasValue)
                {
                    ReqRow oopRow = MarriageService.Requirements("Marriage", _id.Value).FirstOrDefault(x => x.Code == "OUT_OF_PROVINCE_LICENSE");
                    if (oopRow != null)
                    {
                        try { MarriageService.AttachRequirement(oopRow.Id, _oopScanImage, _oopScanImageName); _oopScanImage = null; _oopScanImageName = null; }
                        catch { /* the marriage record itself still saved; retry the attach from the grid below */ }
                    }
                }
                _dirty = false;
                RefreshAll();
                return true;
            }
            catch (Exception ex) { MUi.Fail(this, ex); return false; }
        }

        /// <summary>
        /// Scan or re-attach the image proving the out-of-province licence. Reads it with plain
        /// OCR (the issuing LCRO's form is unknown, so no template applies - unlike Birth/
        /// Marriage/Death there is no DocLayouts entry to read this against) and offers a licence
        /// number and date it can find in the text; the fields stay editable either way, and a
        /// failed or absent read just leaves them for the operator to type. The image itself is
        /// held until the next Save, because it is attached to the marriage_requirements ROW,
        /// which does not exist until the marriage record it belongs to has been saved once.
        /// </summary>
        private void ScanOopLicense()
        {
            using (var dlg = new OpenFileDialog { Title = "Scan / attach license image", Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                byte[] bytes;
                try { bytes = File.ReadAllBytes(dlg.FileName); }
                catch (Exception ex) { MUi.Fail(this, ex); return; }

                Bitmap bmp = null;
                try { using (var ms = new MemoryStream(bytes)) bmp = new Bitmap(Image.FromStream(ms)); }
                catch
                {
                    MessageBox.Show(this, "That file could not be read as an image.", "Not an image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _oopScanImage = bytes; _oopScanImageName = Path.GetFileName(dlg.FileName);

                string no = null; DateTime? date = null;
                if (OcrService.IsAvailable())
                {
                    try { OcrResult r = OcrService.Run(bmp); no = GuessLicenseNo(r.Text); date = GuessDate(r.Text); }
                    catch { /* best-effort - manual entry still works either way */ }
                }
                bmp.Dispose();

                if (!string.IsNullOrWhiteSpace(no) && string.IsNullOrWhiteSpace(_oopLicNo.Text)) _oopLicNo.Text = no;
                if (date.HasValue && !MUi.Val(_oopLicDate).HasValue) MUi.Put(_oopLicDate, date);
                _dirty = true; RefreshAll();

                string readBack = (no != null ? "License number: " + no + "\n" : "") + (date.HasValue ? "Date issued: " + MUi.Short(date) + "\n" : "");
                MessageBox.Show(this,
                    (readBack.Length > 0 ? "Read from the image - check this against the picture, then correct it if needed:\n" + readBack
                                          : "Could not read a number or date automatically - type them in.") +
                    "\nThe image will be saved as the attachment for this requirement when you save this record.",
                    "License image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>Best-effort: a licence-number-looking token near the word License/No, else the
        /// office's own YYYY-#### numbering shape. Never guaranteed - the issuing office is not
        /// one CROMS has a template for.</summary>
        private static string GuessLicenseNo(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            Match m = Regex.Match(text, @"(?:Lic\w*\.?\s*(?:No\.?)?|No\.?)\s*[:\-]?\s*([A-Za-z0-9][A-Za-z0-9\-\/]{2,19})", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();
            m = Regex.Match(text, @"\b(19|20)\d{2}-\d{2,6}\b");
            return m.Success ? m.Value : null;
        }

        /// <summary>Best-effort date read - accepts a named-month date or a numeric one, never
        /// invents a year or a month that is not on the page (same fabrication guard used
        /// throughout DocumentAI's own date parsing).</summary>
        private static DateTime? GuessDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            Match m = Regex.Match(text,
                @"\b(\d{1,2})(?:st|nd|rd|th)?\s+(Jan\w*|Feb\w*|Mar\w*|Apr\w*|May\w*|Jun\w*|Jul\w*|Aug\w*|Sep\w*|Oct\w*|Nov\w*|Dec\w*)\.?\s+(\d{4})\b",
                RegexOptions.IgnoreCase);
            DateTime dt;
            if (m.Success && DateTime.TryParse(m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.Date;
            m = Regex.Match(text, @"\b(19|20)\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])\b");
            if (m.Success && DateTime.TryParseExact(m.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.Date;
            return null;
        }

        // ===================================================================== OCR
        private IEnumerable<DocField> WeakFields()
        {
            if (_ocr == null) return Enumerable.Empty<DocField>();
            return _ocr.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value) &&
                (f.Status == FieldStatus.Uncertain || f.Status == FieldStatus.Invalid || f.Status.ToString() == "Conflict"));
        }

        private int WeakCount() { return WeakFields().Count(); }

        private void HighlightWeak()
        {
            foreach (DocField f in WeakFields())
            {
                Control c;
                if (!_keyControls.TryGetValue(f.Key, out c)) continue;
                c.BackColor = UiTheme.WarningTint;
                _tip.SetToolTip(c, "OCR " + f.Confidence + "%: read \"" + f.OcrValue + "\"" + (string.IsNullOrEmpty(f.Issue) ? "" : " - " + f.Issue) + ". Check it against the scan.");
            }
        }

        private void Changed(Control c)
        {
            if (_loading) return;
            _dirty = true;
            if (c != null && c.BackColor == UiTheme.WarningTint) { c.BackColor = c is ComboBox || c is TextBox ? Color.White : c.BackColor; _tip.SetToolTip(c, null); }
            RefreshLight();
        }

        // ===================================================================== refresh
        private void RefreshAll()
        {
            bool registered = _status == "Registered";
            bool editable = !registered || MarriageService.IsRegistrar;
            foreach (Panel p in _pages) foreach (Control c in p.Controls) c.Enabled = editable;
            _docs.ReadOnlyGrid = !editable;
            MUi.SetPill(_formPill, _formCode ?? "MF-97", "Draft");
            MUi.SetPill(_statusPill, (_status ?? "Draft").ToUpperInvariant() + (_reg.Text.Length > 0 && registered ? "  " + _reg.Text : ""), _status);
            _licPanel.Visible = _rbLic.Checked; _exPanel.Visible = _rbEx.Checked;
            _localPanel.Visible = !_oop.Checked; _oopPanel.Visible = _oop.Checked;
            _licPanel.Height = 30 + (_oop.Checked ? _oopPanel.Height : _localPanel.Height) + 58 + 6;

            MarriageFacts m = UiFacts();
            bool delayed = MarriageRules.WouldBeDelayed(m, _s);
            List<Need> needs = MarriageRules.Needs(m.Husband, m.Wife, m.DateOfMarriage ?? DateTime.Today, _catalog, "Marriage", _s, m.Basis == "Exempt", delayed, m.OutOfProvinceLicense);
            if (_id.HasValue)
            {
                if (!_dirty) MarriageService.SyncMarriageRequirements(_id.Value);
                _docs.Bind("Marriage", _id.Value, needs);
                _docsHint.Text = needs.Count == 0 ? "No record-level documents needed. (A licensed marriage proves an ended previous marriage from its licence file.)" : "";
            }
            else _docsHint.Text = "Save the draft to record supporting documents.";
            _docs.Height = Math.Max(90, _docs.PreferredHeight);

            RefreshLight();
        }

        /// <summary>Everything derived from the fields - cheap enough to run on every keystroke.</summary>
        private void RefreshLight()
        {
            MarriageFacts m = UiFacts();
            DateTime on = m.DateOfMarriage ?? DateTime.Today;
            foreach (var pair in new[] { Tuple.Create(_h, m.Husband), Tuple.Create(_w, m.Wife) })
            {
                if (pair.Item2.Dob.HasValue)
                {
                    int a = MarriageRules.AgeOn(pair.Item2.Dob.Value, on);
                    pair.Item1.Age.Text = a + " years" + (m.DateOfMarriage.HasValue ? " on " + MUi.Short(m.DateOfMarriage) : " (today - enter marriage date)");
                    pair.Item1.Age.ForeColor = a < 18 ? UiTheme.Danger : UiTheme.Ink;
                }
                else { pair.Item1.Age.Text = "- enter date of birth"; pair.Item1.Age.ForeColor = UiTheme.Faint; }
            }

            bool exempt = m.Basis == "Exempt";
            if (m.DateOfMarriage.HasValue && m.DateReceived.HasValue)
            {
                DateTime deadline = MarriageRules.ReportingDeadline(m.DateOfMarriage.Value, exempt, _s);
                bool delayed = MarriageRules.WouldBeDelayed(m, _s);
                _regBanner.Set(delayed ? RuleSeverity.Warning : RuleSeverity.Info,
                    delayed ? "DELAYED REGISTRATION" : "NORMAL (TIMELY) REGISTRATION",
                    "Reporting period " + (exempt ? _s.ReportDaysExempt : _s.ReportDaysLicensed) + " days (Family Code Art. " + (exempt ? "30" : "23") +
                    "): due by " + MUi.D(deadline) + ", received " + MUi.D(m.DateReceived) + "." +
                    (delayed ? " This goes through the delayed-registration workflow: reason, affidavit, " + _s.DelayedPostingDays + "-day notice and registrar review." : ""),
                    !delayed);
            }
            else _regBanner.Set(RuleSeverity.Warning, "Timely or delayed?", "Enter the date of marriage and the date received to determine it.");

            RefreshLicenseSummary(m);
            RefreshRail();
            RefreshChecks(m);
        }

        private void RefreshLicenseSummary(MarriageFacts m)
        {
            if (m.OutOfProvinceLicense) return; // shown in _oopPanel instead
            _licSummary.Controls.Clear();
            if (_lic == null)
            {
                var b = new Banner();
                b.Set(RuleSeverity.Warning, "No licence selected.", "Search above - only issued, unused licences are listed by default.");
                Stack(_licSummary, b);
                return;
            }
            bool matchH = MarriageRules.NamesMatch(m.Husband, _lic.Husband), matchW = MarriageRules.NamesMatch(m.Wife, _lic.Wife);
            bool inWindow = m.DateOfMarriage.HasValue && _lic.IssueDate.HasValue && _lic.ExpiryDate.HasValue &&
                            m.DateOfMarriage.Value >= _lic.IssueDate.Value && m.DateOfMarriage.Value <= _lic.ExpiryDate.Value;
            Stack(_licSummary,
                MUi.Kv("Licence", _lic.LicenseNo + "   (" + MarriageRules.LicenseDisplayStatus(_lic, DateTime.Today, _s, 0) + ")"),
                MUi.Kv("Issued  /  valid until", MUi.D(_lic.IssueDate) + "  /  " + MUi.D(_lic.ExpiryDate)),
                MUi.Kv("Applicants on licence", _lic.Husband.FullName + " & " + _lic.Wife.FullName, matchH && matchW ? UiTheme.Success : UiTheme.Danger),
                MUi.Kv("Marriage date within validity", !m.DateOfMarriage.HasValue ? "enter the date of marriage" : inWindow ? "yes ✓" : "NO ✕",
                       !m.DateOfMarriage.HasValue ? UiTheme.Muted : inWindow ? UiTheme.Success : UiTheme.Danger));
        }

        private void RefreshChecks(MarriageFacts m)
        {
            List<RuleIssue> issues = MarriageRules.ValidateMarriage(m, m.Basis == "Licensed" ? _lic : null, _catalog, DateTime.Today, _s);
            _issues.SetIssues(issues, _status == "Registered" ? "Registered." : "Every check passes - ready to register.");
            for (int i = 0; i < TabNames.Length; i++)
            {
                int n = issues.Count(x => x.Blocks && (x.FixWhere == TabNames[i] || (i == 2 && x.FixWhere == "Case Workflow")));
                _tabs.SetBadge(i, _status == "Registered" ? 0 : n);
                _tabs.SetState(i, i == _tab ? StepStrip.State.Current : StepStrip.State.Todo);
            }
            int blocks = issues.Count(x => x.Blocks);
            bool registered = _status == "Registered";
            _btnRegister.Enabled = !registered && blocks == 0 && MarriageService.IsRegistrar;
            _btnReview.Visible = !registered && _status != "For Review";
            _btnDraft.Text = registered ? "Save correction" : "Save as draft";
            _btnDraft.Enabled = !registered || MarriageService.IsRegistrar;
            _btnCase.Enabled = _id.HasValue && (m.Basis == "Exempt" || MarriageRules.WouldBeDelayed(m, _s));
            _btnSoft.Enabled = _scanImage != null;
            _btnAckSlip.Enabled = _id.HasValue && _status == "For Review";
            _footInfo.ForeColor = blocks == 0 ? UiTheme.Success : UiTheme.Muted;
            _footInfo.Text = registered ? "Registered " + _reg.Text + " - corrections to a registered entry go through Petitions."
                : !MarriageService.IsRegistrar && blocks == 0 ? "Checks pass - a Registrar registers the marriage."
                : blocks == 0 ? (_dirty ? "Ready - unsaved changes will be saved on register." : "All checks pass.")
                : blocks + " blocking check" + (blocks == 1 ? "" : "s") + (WeakCount() > 0 && _ocrPending ? " · " + WeakCount() + " weak OCR field(s)" : "") +
                  " - see the list on the right.";
        }

        private void RefreshRail()
        {
            _rail.SuspendLayout();
            foreach (Control c in _rail.Controls.Cast<Control>().ToList()) if (c != _issues) c.Dispose();
            _rail.Controls.Clear();
            var items = new List<Control>();
            items.Add(MUi.Cap(_rbEx.Checked ? "License exemption" : _oop.Checked ? "License (out of province)" : "License on file"));
            if (_rbEx.Checked)
                items.Add(MUi.Kv("Basis", _exBasis.SelectedIndex >= 0 ? MarriageRules.ExemptionBases[_exBasis.SelectedIndex, 0].Replace("ART", "Art. ") : "not stated",
                    _exBasis.SelectedIndex >= 0 ? (Color?)null : UiTheme.Danger));
            else if (_oop.Checked)
            {
                items.Add(MUi.Kv("Licence no.", string.IsNullOrWhiteSpace(_oopLicNo.Text) ? "not entered" : _oopLicNo.Text,
                    string.IsNullOrWhiteSpace(_oopLicNo.Text) ? UiTheme.Danger : (Color?)null));
                items.Add(MUi.Kv("Issued", MUi.Val(_oopLicDate).HasValue ? MUi.Short(MUi.Val(_oopLicDate)) : "not entered",
                    MUi.Val(_oopLicDate).HasValue ? (Color?)null : UiTheme.Danger));
                items.Add(MUi.Kv("Issuing office", "another province - attach proof"));
            }
            else if (_lic == null) items.Add(MUi.Kv("Licence", "none selected", UiTheme.Danger));
            else
            {
                items.Add(MUi.Kv("Licence", _lic.LicenseNo));
                items.Add(MUi.Kv("Valid until", MUi.D(_lic.ExpiryDate)));
                MarriageFacts m = UiFacts();
                bool match = MarriageRules.NamesMatch(m.Husband, _lic.Husband) && MarriageRules.NamesMatch(m.Wife, _lic.Wife);
                items.Add(MUi.Kv("Applicants", match ? "match ✓" : "do NOT match ✕", match ? UiTheme.Success : UiTheme.Danger));
                if (m.DateOfMarriage.HasValue && _lic.IssueDate.HasValue)
                {
                    int day = (m.DateOfMarriage.Value - _lic.IssueDate.Value).Days;
                    bool ok = day >= 0 && m.DateOfMarriage.Value <= _lic.ExpiryDate;
                    items.Add(MUi.Kv("Marriage date", ok ? "day " + day + " of " + _s.ValidityDays + " ✓" : "outside validity ✕", ok ? UiTheme.Success : UiTheme.Danger));
                }
            }

            items.Add(MUi.Cap("Source document"));
            if (_scanImage == null && _ocrScanId == null) items.Add(MUi.Txt("Typed from the paper - no scan attached.", 9F, FontStyle.Regular, UiTheme.Muted));
            else
            {
                if (_scanImage != null)
                {
                    var pic = new PictureBox { Height = 110, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(245, 247, 250), Cursor = Cursors.Hand };
                    try { using (var ms = new MemoryStream(_scanImage)) pic.Image = new Bitmap(Image.FromStream(ms)); } catch { }
                    pic.Click += (s, e) => CompareWithScan();
                    items.Add(pic);
                }
                items.Add(MUi.Kv("Scan", _ocrScanId ?? "attached"));
                MarriageFacts m = UiFacts();
                int? conf = _ocr != null ? _ocr.OverallConfidence : (int?)null;
                if (!conf.HasValue && _id.HasValue)
                {
                    object c = Db.Pull("SELECT ocr_confidence FROM marriages WHERE id=@id", new MySqlParameter("@id", _id.Value)).Rows[0][0];
                    conf = c == DBNull.Value ? (int?)null : Convert.ToInt32(c);
                }
                items.Add(MUi.Kv("OCR confidence", conf.HasValue ? conf + "%" : "-"));
                items.Add(MUi.Kv("Weak fields", (m.OcrWeakFields ?? 0).ToString(), (m.OcrWeakFields ?? 0) > 0 ? UiTheme.Warning : (Color?)null));
                var rp = new FlowLayoutPanel { Height = 32, BackColor = Color.Transparent };
                rp.Controls.Add(MUi.Txt("Review", 9F, FontStyle.Regular, UiTheme.Muted));
                string rs = m.OcrReviewStatus ?? "Not applicable";
                rp.Controls.Add(MUi.Pill(rs.ToUpperInvariant(), rs));
                items.Add(rp);
                var b1 = MUi.Btn("Compare with scan", MUi.Kind.Secondary); b1.Click += (s, e) => CompareWithScan();
                var b2 = MUi.Btn("Mark review complete", MUi.Kind.Secondary);
                b2.Enabled = rs == "Required";
                b2.Click += (s, e) =>
                {
                    if (!_id.HasValue && !Save(null)) return;
                    if (!MUi.Confirm(this, "Review complete", "Have you compared every weak field against the source certificate?")) return;
                    MarriageService.MarkOcrReviewed(_id.Value); _ocrPending = false; RefreshAll();
                };
                var row = new FlowLayoutPanel { Height = 76, BackColor = Color.Transparent };
                row.Controls.Add(b1); row.Controls.Add(b2);
                items.Add(row);
            }

            items.Add(MUi.Cap("Checks"));
            _issues.Height = 300;
            items.Add(_issues);
            Stack(_rail, items.ToArray());
            _rail.ResumeLayout();
        }

        private void ShowTab(int i)
        {
            _tab = i;
            for (int k = 0; k < _pages.Length; k++) _pages[k].Visible = k == i;
            _pages[i].BringToFront();
            for (int k = 0; k < TabNames.Length; k++) _tabs.SetState(k, k == i ? StepStrip.State.Current : StepStrip.State.Todo);
        }

        // ===================================================================== actions
        private void Register()
        {
            if (_dirty || !_id.HasValue) if (!Save(null)) return;
            List<RuleIssue> issues = MarriageService.ValidateMarriage(_id.Value).Where(x => x.Blocks).ToList();
            if (issues.Count > 0)
            {
                _issues.SetIssues(issues);
                int t = Array.IndexOf(TabNames, issues[0].FixWhere);
                if (t >= 0) ShowTab(t);
                return;
            }
            MarriageFacts m = MarriageService.LoadMarriageFacts(_id.Value);
            string regNo = string.IsNullOrWhiteSpace(m.RegistryNo) ? RegistryNumber.Next("marriages", 'M') + " (next available)" : m.RegistryNo;
            if (!MUi.Confirm(this, "Register marriage", "Register this marriage?",
                    "Registry number|" + regNo, "Marriage date|" + MUi.D(m.DateOfMarriage),
                    "Parties|" + m.Husband.FullName, "|" + m.Wife.FullName,
                    "License|" + (m.Basis == "Exempt" ? "Exempt - " + m.ExemptionBasis :
                                  m.OutOfProvinceLicense ? m.ExternalLicenseNo + " (another province)" :
                                  _lic != null ? _lic.LicenseNo : "-"),
                    "Registration|" + (MarriageRules.WouldBeDelayed(m, _s) ? "DELAYED" : "Timely")))
                return;
            try
            {
                string reg;
                issues = MarriageService.Register(_id.Value, out reg);
                if (issues.Count > 0) { _issues.SetIssues(issues); return; }
                _status = "Registered"; _reg.Text = reg; _dirty = false;
                int id = _id.Value;
                DialogResult = DialogResult.OK;
                Hide();
                using (var rec = new MarriageRecordForm(id)) rec.ShowDialog(Owner);
                Close();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void OpenCase()
        {
            if ((_dirty || !_id.HasValue) && !Save(null)) return;
            using (var f = new MarriageCaseForm(_id.Value)) f.ShowDialog(this);
            _loading = true; LoadMarriage(_id.Value); _loading = false; _dirty = false;
            RefreshAll();
        }

        private void ViewSoftcopy()
        {
            if (_scanImage == null)
            {
                if (_id.HasValue)
                {
                    // No scanned original on file — fall back to the official Municipal
                    // Form 97 blank already in Assets, filled in from the saved record.
                    CertificateReport.Show(_formCode, _id.Value, this);
                    return;
                }
                MessageBox.Show("No softcopy is saved for this record.", "Softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(_scanImage, "Certificate of Marriage - original softcopy", this);
        }

        /// <summary>Prints a receipt for a marriage submission that has been sent For Review
        /// but not yet registered. Only acknowledges the transaction was received - never a
        /// certificate, never tied to Fees & Payments. A Draft has not been submitted yet
        /// (nothing to acknowledge), and a Registered marriage has nothing left pending, so
        /// both are refused rather than printing a slip that would misstate the record.</summary>
        private void PrintAckSlip()
        {
            if (!_id.HasValue || _status != "For Review")
            {
                MessageBox.Show(this, "The acknowledgment slip is only for a submission that " +
                    "has been sent for review and is still awaiting registration. Use \"Send " +
                    "for review\" first.", "Print Acknowledgment Slip",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataTable dt = Db.Pull("SELECT created_at FROM marriages WHERE id=@id", new MySqlParameter("@id", _id.Value));
            DateTime submitted = dt.Rows.Count > 0 && dt.Rows[0]["created_at"] != DBNull.Value
                ? Convert.ToDateTime(dt.Rows[0]["created_at"]) : DateTime.Now;

            string husband = JoinNonEmpty(_h.First.Text, _h.Middle.Text, _h.Last.Text);
            string wife = JoinNonEmpty(_w.First.Text, _w.Middle.Text, _w.Last.Text);
            string names = string.IsNullOrWhiteSpace(husband) && string.IsNullOrWhiteSpace(wife)
                ? "" : husband + (husband.Length > 0 && wife.Length > 0 ? " & " : "") + wife;

            string reg = _reg.Text;
            string reference = string.IsNullOrWhiteSpace(reg) ? "MR-PENDING-" + _id.Value : reg;

            AcknowledgmentSlip.Print(this, reference, names,
                "Marriage Registration (Municipal Form 97)", submitted, "For Review",
                new[]
                {
                    "Certificate of Marriage (Municipal Form 97), accomplished",
                    "Marriage License (or exemption basis, if applicable)",
                    "Valid ID of both contracting parties"
                },
                Session.User != null ? Session.User.FullName : "Front Desk");
        }

        private static string JoinNonEmpty(params string[] parts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string p in parts)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(p.Trim());
            }
            return sb.ToString();
        }

        private IDictionary<string, string> PreviewValues()
        {
            Func<DateTime?, string> d = x => x.HasValue ? x.Value.ToString("yyyy-MM-dd") : "";
            return new Dictionary<string, string>
            {
                { "RegistryNo", _reg.Text }, { "HusbandFirst", _h.First.Text }, { "HusbandMiddle", _h.Middle.Text }, { "HusbandLast", _h.Last.Text },
                { "WifeFirst", _w.First.Text }, { "WifeMiddle", _w.Middle.Text }, { "WifeLast", _w.Last.Text },
                { "HusbandFatherName", _h.Father.Text }, { "HusbandMotherName", _h.Mother.Text },
                { "WifeFatherName", _w.Father.Text }, { "WifeMotherName", _w.Mother.Text },
                { "DateOfMarriage", d(MUi.Val(_dom)) }, { "PlaceOfMarriage", _church.Text }, { "Solemnizer", _sol.Text },
                { "SolemnizerPosition", _solPos.Text }, { "Witness1", _w1.Text }, { "Witness2", _w2.Text },
                { "LicenseNo", !_rbLic.Checked ? "" : _oop.Checked ? _oopLicNo.Text : _lic != null ? _lic.LicenseNo : "" },
                { "LicenseDate", !_rbLic.Checked ? "" : _oop.Checked ? d(MUi.Val(_oopLicDate)) : _lic != null ? d(_lic.IssueDate) : "" },
                { "ReceivedByName", _recvBy.Text }, { "ReceivedByTitle", _recvTitle.Text }, { "ReceivedByDate", d(MUi.Val(_recv)) },
            };
        }

        private void PreviewOnForm()
        {
            FormDefinition def = FormCatalog.ByCode(_formCode) ?? FormCatalog.Current(DocKind.Marriage);
            CertificateReport.ShowOcrPreview(def, PreviewValues(), this);
        }

        /// <summary>The source certificate beside what was entered, weak fields first.</summary>
        private void CompareWithScan()
        {
            var rows = new DataTable();
            rows.Columns.Add("Field"); rows.Columns.Add("Entered"); rows.Columns.Add("OCR read"); rows.Columns.Add("Conf.", typeof(int)); rows.Columns.Add("Check");
            IDictionary<string, string> now = PreviewValues();
            if (_ocr != null)
                foreach (DocField f in _ocr.Fields.OrderBy(x => x.Status == FieldStatus.Ok ? 1 : 0))
                {
                    string entered; now.TryGetValue(f.Key, out entered);
                    rows.Rows.Add(f.Label, entered ?? f.Value, f.OcrValue, f.Confidence, f.Status == FieldStatus.Ok ? "ok" : f.Status + (string.IsNullOrEmpty(f.Issue) ? "" : ": " + f.Issue));
                }
            else if (_ocrScanId != null)
            {
                DataTable a = Db.Pull("SELECT field_label, field_key, ocr_value, final_value, confidence, status, issue FROM ocr_field_audit " +
                                      "WHERE scan_id=@s AND id IN (SELECT MAX(id) FROM ocr_field_audit WHERE scan_id=@s GROUP BY field_key) ORDER BY confidence",
                    new MySqlParameter("@s", _ocrScanId));
                foreach (DataRow r in a.Rows)
                {
                    string entered; now.TryGetValue(r["field_key"].ToString(), out entered);
                    rows.Rows.Add(r["field_label"], entered ?? r["final_value"], r["ocr_value"], r["confidence"] == DBNull.Value ? 0 : Convert.ToInt32(r["confidence"]),
                        Convert.ToString(r["status"]) + (r["issue"] == DBNull.Value ? "" : ": " + r["issue"]));
                }
            }
            using (var f = new Form { Text = "Compare with scan", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(1280, 800), ShowInTaskbar = false, BackColor = UiTheme.PageBg })
            {
                var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 640 };
                var pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(40, 44, 52) };
                if (_scanImage != null) try { using (var ms = new MemoryStream(_scanImage)) pic.Image = new Bitmap(Image.FromStream(ms)); } catch { }
                var g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                                           AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, DataSource = rows };
                g.CellFormatting += (s, e) =>
                {
                    if (e.RowIndex < 0) return;
                    string chk = Convert.ToString(g.Rows[e.RowIndex].Cells["Check"].Value);
                    if (chk != "ok" && chk != "") e.CellStyle.BackColor = UiTheme.WarningTint;
                };
                split.Panel1.Controls.Add(pic);
                if (rows.Rows.Count == 0) split.Panel2.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "No per-field OCR data is stored for this record - compare the scan with the tabs directly.",
                                                                                TextAlign = ContentAlignment.MiddleCenter, Font = MUi.F(10F) });
                else split.Panel2.Controls.Add(g);
                f.Controls.Add(split);
                UiTheme.Polish(f);
                f.ShowDialog(this);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { Save(null); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
