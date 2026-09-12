using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// WINDOW 1 - the Marriage Desk. Marriage is the only registry event with a BEFORE: a
    /// licence posted ten days, then valid a hundred and twenty. The desk is a console for
    /// couples sitting at different points on those clocks, then for the certificates that
    /// come back and the transmittals that go out.
    ///
    /// Read-only here: every write goes through the Form 90 / Form 97 / PSA windows and
    /// <see cref="MarriageService"/>. Designer holds the static layout; this file fills it.
    /// </summary>
    public partial class MarriageRegistrationForm : Form, IRefreshable
    {
        private List<LicenseFacts> _licenses = new List<LicenseFacts>();
        private List<ReqType> _catalog = new List<ReqType>();
        private DataTable _marriages;
        private string _tab = "Licenses";
        private string _statusFilter;
        private readonly MarriageSettings _s = MarriageService.Settings;

        public MarriageRegistrationForm()
        {
            InitializeComponent();
            SetupKpi(kpiPosting, "In posting", KpiCard.Icon.InboxTray, UiTheme.WarningTint, UiTheme.Warning);
            SetupKpi(kpiReady, "Ready to issue", KpiCard.Icon.DocumentTick, UiTheme.SuccessTint, UiTheme.Success);
            SetupKpi(kpiValid, "Valid licences", KpiCard.Icon.DocumentTick, UiTheme.AccentTint, UiTheme.Accent);
            SetupKpi(kpiExpiring, "Expiring soon", KpiCard.Icon.QueuePerson, UiTheme.DangerTint, UiTheme.Danger);
            SetupKpi(kpiAwaiting, "Awaiting Form 97", KpiCard.Icon.InboxTray, UiTheme.AccentTint, UiTheme.Accent);
            SetupKpi(kpiPsa, "Pending PSA transmittal", KpiCard.Icon.InboxTray, UiTheme.Chrome, UiTheme.Muted);
            kpiPosting.Click += (s, e) => Filter("Licenses", "posting");
            kpiReady.Click += (s, e) => Filter("Licenses", "Ready to Issue");
            kpiValid.Click += (s, e) => Filter("Licenses", "valid");
            kpiExpiring.Click += (s, e) => Filter("Licenses", "Expiring");
            kpiAwaiting.Click += (s, e) => Filter("Licenses", "valid");
            kpiPsa.Click += (s, e) => OpenPsa();
            board.ItemClicked += OnBoardItem;
            dgvList.CellFormatting += dgvList_CellFormatting;
            // Hidden here, not right after DataSource is set: before the form is shown the grid
            // generates its columns late and the earlier hide was silently lost (the render
            // showed the internal id column).
            dgvList.DataBindingComplete += (s, e) =>
            {
                foreach (string c in new[] { "id", "kind", "search" }) if (dgvList.Columns.Contains(c)) dgvList.Columns[c].Visible = false;
                if (dgvList.Columns.Contains("Applicants")) dgvList.Columns["Applicants"].FillWeight = 240;
            };
            RefreshData();
        }

        private static void SetupKpi(KpiCard k, string label, KpiCard.Icon icon, Color tint, Color accent)
        {
            k.Label = label; k.IconKind = icon; k.Tint = tint; k.Accent = accent; k.Cursor = Cursors.Hand; k.CaptionIsAction = false;
        }

        public void RefreshData()
        {
            try
            {
                MarriageService.SweepExpired();
                _catalog = MarriageService.Catalog();
                _licenses = MarriageService.ListLicenses();
                _marriages = LoadMarriages();
            }
            catch (Exception ex)
            {
                lblListCount.Text = "Could not load the marriage workflow: " + ex.Message + "  (is migration 33_marriage_workflow.sql applied?)";
                return;
            }
            board.Settings = _s;
            FillKpis();
            FillBoard();
            FillGrid();
        }

        // ------------------------------------------------------------------ data
        private string Display(LicenseFacts l) { return MarriageRules.LicenseDisplayStatus(l, DateTime.Today, _s, MarriageService.OutstandingBlocking(l, _catalog)); }

        private static DataTable LoadMarriages()
        {
            DataTable dt = Db.Pull(
                "SELECT m.id, m.registry_no, m.husband_first_name, m.husband_last_name, m.wife_first_name, m.wife_last_name, m.date_of_marriage, " +
                "m.license_basis, m.exemption_basis, COALESCE(ml.license_no, m.license_no) AS lic_no, m.license_id, m.registration_type, m.status, " +
                "m.date_registered, m.ocr_review_status, m.return_reason, " +
                "(SELECT i.status FROM psa_transmittal_items i WHERE i.record_table='marriages' AND i.record_id=m.id ORDER BY i.id DESC LIMIT 1) AS psa_item " +
                "FROM marriages m LEFT JOIN marriage_licenses ml ON ml.id = m.license_id ORDER BY m.id DESC");
            return dt;
        }

        private void FillKpis()
        {
            var disp = _licenses.Select(l => new { L = l, D = Display(l) }).ToList();
            int posting = disp.Count(x => x.D == "Posting" || x.D == "Posting Complete");
            int ready = disp.Count(x => x.D == "Ready to Issue");
            int valid = disp.Count(x => x.D == "Valid" || x.D == "Expiring");
            int expiring = disp.Count(x => x.D == "Expiring");
            int awaiting = disp.Count(x => (x.D == "Valid" || x.D == "Expiring") && !x.L.UsedByMarriageId.HasValue);
            int psa = _marriages.AsEnumerable().Count(r => r["status"].ToString() == "Registered" && r["date_registered"] != DBNull.Value &&
                                                           (r["psa_item"] == DBNull.Value || r["psa_item"].ToString() == "Returned"));

            LicenseFacts nextPost = disp.Where(x => x.D == "Posting").Select(x => x.L).OrderBy(l => l.EarliestIssue).FirstOrDefault();
            Kpi(kpiPosting, posting, nextPost != null ? "earliest issue " + MUi.Short(nextPost.EarliestIssue) + " · Art. 17" : "10-day posting · Art. 17");
            Kpi(kpiReady, ready, ready > 0 ? "posting complete - final review" : "nothing waiting to issue");
            Kpi(kpiValid, valid, "issued, within 120 days · Art. 20");
            LicenseFacts soon = disp.Where(x => x.D == "Expiring").Select(x => x.L).OrderBy(l => l.ExpiryDate).FirstOrDefault();
            // "and", not "&": KpiCard draws captions with TextRenderer, which eats '&'.
            Kpi(kpiExpiring, expiring, soon != null ? soon.Husband.Last + " and " + soon.Wife.Last + " lapses in " + MarriageRules.DaysRemaining(soon, DateTime.Today) + " d"
                                                     : "none within " + _s.ExpiringSoonDays + " days");
            kpiExpiring.ValueColor = expiring > 0 ? UiTheme.Danger : UiTheme.Ink;
            Kpi(kpiAwaiting, awaiting, "valid licence, no certificate yet");
            Kpi(kpiPsa, psa, psa > 0 ? "registered, not yet sent - click to transmit" : "all registrations transmitted");
            kpiPsa.CaptionIsAction = psa > 0;
            kpiPsa.CaptionColor = psa > 0 ? UiTheme.Accent : UiTheme.Faint;
            foreach (KpiCard k in new[] { kpiPosting, kpiReady, kpiValid, kpiExpiring, kpiAwaiting, kpiPsa }) k.Invalidate();
        }

        private static void Kpi(KpiCard k, int value, string caption) { k.Value = value.ToString(); k.Caption = caption; }

        private void FillBoard()
        {
            var items = new List<Tuple<int, int, LifecycleBoard.Item>>();
            foreach (LicenseFacts l in _licenses)
            {
                string d = Display(l);
                string who = MarriageRules.Title(l.Husband.Last ?? "?") + " & " + MarriageRules.Title(l.Wife.Last ?? "?");
                var it = new LifecycleBoard.Item
                {
                    Who = who, Number = l.LicenseNo ?? l.ApplicationNo, PostingStart = l.PostingStart, EarliestIssue = l.EarliestIssue,
                    IssueDate = l.IssueDate, Expiry = l.ExpiryDate, Tag = l, Pill = d, PillTone = d
                };
                int rank, sub = 0;
                int left = MarriageRules.DaysRemaining(l, DateTime.Today);
                switch (d)
                {
                    case "Expiring": rank = 0; sub = left; it.Pill = "Expiring - " + left + " d"; it.Next = "Awaiting Certificate of Marriage - expires " + MUi.D(l.ExpiryDate); break;
                    case "Ready to Issue": rank = 1; it.Next = "Posting complete - final review, then issue"; break;
                    case "Posting Complete": rank = 2; it.Next = "Posting done - " + MarriageService.OutstandingBlocking(l, _catalog) + " requirement(s) outstanding"; break;
                    case "Posting":
                        rank = 3; sub = MarriageRules.PostingDay(l, DateTime.Today, _s);
                        it.Pill = "Posting - day " + sub + "/" + _s.PostingDays;
                        it.Next = "Posting completes " + MUi.D(l.PostingEnd) + (l.DeferralReason != null ? "; issue deferred to " + MUi.D(l.EarliestIssue) : "");
                        break;
                    case "On Hold": rank = 4; it.Next = "On hold: " + l.HoldReason; break;
                    case "Requirements Incomplete": rank = 5; it.Next = MarriageService.OutstandingBlocking(l, _catalog) + " requirement(s) outstanding - posting not started"; break;
                    case "Ready for Posting": rank = 5; it.Next = "Documents in order - start the posting"; break;
                    case "Valid":
                        if (l.UsedByMarriageId.HasValue) continue;
                        rank = 7; sub = left; it.Pill = "Valid - " + left + " d left"; it.Next = "Awaiting Certificate of Marriage"; break;
                    case "Expired":
                        if (l.UsedByMarriageId.HasValue || !l.ExpiryDate.HasValue || l.ExpiryDate.Value < DateTime.Today.AddDays(-30)) continue;
                        rank = 8; it.Next = "Expired unused " + MUi.D(l.ExpiryDate) + " - kept on record; a new application is needed"; break;
                    default: continue;
                }
                items.Add(Tuple.Create(rank, sub, it));
            }
            foreach (DataRow r in _marriages.Rows)
            {
                string st = r["status"].ToString();
                string psa = r["psa_item"] == DBNull.Value ? null : r["psa_item"].ToString();
                if (st == "Registered" && psa != "Returned") continue;
                var it = new LifecycleBoard.Item
                {
                    Who = MarriageRules.Title(Convert.ToString(r["husband_last_name"])) + " & " + MarriageRules.Title(Convert.ToString(r["wife_last_name"])),
                    Number = r["registry_no"] == DBNull.Value ? "Form 97 draft #" + r["id"] : r["registry_no"].ToString(),
                    TrackLess = true, Tag = Convert.ToInt32(r["id"])
                };
                if (st == "Registered") { it.Pill = "Returned by PSA"; it.PillTone = "Returned"; it.Next = "Correct and include in a new batch"; }
                else if (st == "Returned") { it.Pill = "Returned"; it.Next = "Returned for correction: " + r["return_reason"]; }
                else if (r["ocr_review_status"].ToString() == "Required") { it.Pill = "OCR review"; it.PillTone = "Required"; it.Next = "Compare weak fields with the scan"; }
                else { it.Pill = st; it.Next = st == "For Review" ? "Awaiting registrar - open to register" : "Certificate being encoded"; }
                items.Add(Tuple.Create(6, 0, it));
            }
            board.SetItems(items.OrderBy(t => t.Item1).ThenBy(t => t.Item2).Select(t => t.Item3));
            lblAttention.Text = "ATTENTION / LIFECYCLE   ·   " + items.Count + " open";
        }

        private void FillGrid()
        {
            btnTabLicenses.BackColor = _tab == "Licenses" ? UiTheme.Accent : UiTheme.Chrome;
            btnTabLicenses.ForeColor = _tab == "Licenses" ? Color.White : UiTheme.Ink;
            btnTabMarriages.BackColor = _tab == "Marriages" ? UiTheme.Accent : UiTheme.Chrome;
            btnTabMarriages.ForeColor = _tab == "Marriages" ? Color.White : UiTheme.Ink;
            btnTabLicenses.Text = "APPLICATIONS & LICENSES · " + _licenses.Count;
            btnTabMarriages.Text = "MARRIAGES · " + _marriages.Rows.Count;
            btnTabLicenses.Invalidate(); btnTabMarriages.Invalidate();

            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int)); dt.Columns.Add("kind"); dt.Columns.Add("search");
            if (_tab == "Licenses")
            {
                foreach (string c in new[] { "Application No.", "License No.", "Applicants", "Filed", "Posting", "Issued", "Valid Until", "Days Left", "Status", "Certificate" })
                    dt.Columns.Add(c);
                foreach (LicenseFacts l in _licenses)
                {
                    string d = Display(l);
                    string who = l.Husband.FullName + "  &  " + l.Wife.FullName;
                    dt.Rows.Add(l.Id, "L", LearningLibrary.Normalize(who + " " + l.ApplicationNo + " " + l.LicenseNo + " " + d),
                        l.ApplicationNo, l.LicenseNo ?? "-", who, MUi.D(l.FiledDate),
                        l.PostingStart.HasValue ? MUi.Short(l.PostingStart) + " - " + MUi.Short(l.PostingEnd) : "-",
                        MUi.D(l.IssueDate), MUi.D(l.ExpiryDate),
                        (d == "Valid" || d == "Expiring") ? MarriageRules.DaysRemaining(l, DateTime.Today).ToString() : "",
                        d, l.UsedByMarriageId.HasValue ? (l.UsedByRegistryNo ?? "draft #" + l.UsedByMarriageId) : "-");
                }
            }
            else
            {
                foreach (string c in new[] { "Registry No.", "Husband / Party 1", "Wife / Party 2", "Marriage Date", "License", "Registration", "Status", "PSA" })
                    dt.Columns.Add(c);
                foreach (DataRow r in _marriages.Rows)
                {
                    string h = r["husband_last_name"] + ", " + r["husband_first_name"], w = r["wife_last_name"] + ", " + r["wife_first_name"];
                    string basis = r["license_basis"].ToString() == "Exempt" ? "Exempt (" + Convert.ToString(r["exemption_basis"]).Replace("ART", "Art. ") + ")"
                                 : r["lic_no"] != DBNull.Value ? r["lic_no"] + (r["license_id"] == DBNull.Value ? " (typed)" : "") : "(legacy)";
                    bool legacy = r["date_registered"] == DBNull.Value;
                    string psa = r["status"].ToString() == "Registered"
                        ? MarriageService.PsaStatusText(r["psa_item"] == DBNull.Value ? null : r["psa_item"].ToString(), legacy) : "-";
                    string reg = r["registry_no"] == DBNull.Value ? "-" : r["registry_no"].ToString();
                    dt.Rows.Add(r["id"], "M", LearningLibrary.Normalize(h + " " + w + " " + reg + " " + basis + " " + r["status"] + " " + psa),
                        reg, h, w, r["date_of_marriage"] == DBNull.Value ? "-" : MUi.D(Convert.ToDateTime(r["date_of_marriage"])), basis,
                        r["registration_type"] == DBNull.Value ? (legacy && r["status"].ToString() == "Registered" ? "(legacy)" : "-") : r["registration_type"].ToString(),
                        r["status"], psa);
                }
            }
            dgvList.DataSource = dt;
            foreach (string c in new[] { "id", "kind", "search" }) if (dgvList.Columns.Contains(c)) dgvList.Columns[c].Visible = false;
            if (dgvList.Columns.Contains("Applicants")) dgvList.Columns["Applicants"].FillWeight = 240;
            ApplySearch();
        }

        private void ApplySearch()
        {
            var dt = dgvList.DataSource as DataTable;
            if (dt == null) return;
            var parts = new List<string>();
            string q = LearningLibrary.Normalize(txtSearch.Text ?? "").Replace("'", "''");
            if (q.Length > 0) parts.Add("search LIKE '%" + q + "%'");
            if (!string.IsNullOrEmpty(_statusFilter)) parts.Add("search LIKE '%" + LearningLibrary.Normalize(_statusFilter).Replace("'", "''") + "%'");
            dt.DefaultView.RowFilter = string.Join(" AND ", parts);
            lblListCount.Text = dt.DefaultView.Count + " of " + dt.Rows.Count + (string.IsNullOrEmpty(_statusFilter) ? "" : "   ·   filter: " + _statusFilter + "  (clear the search box to reset)");
        }

        private void Filter(string tab, string status)
        {
            _tab = tab; _statusFilter = status;
            FillGrid();
        }

        // ------------------------------------------------------------------ events
        private void dgvList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string col = dgvList.Columns[e.ColumnIndex].Name;
            if (col == "Status" || col == "PSA" || col == "Registration")
            {
                e.CellStyle.ForeColor = MUi.InkOf(Convert.ToString(e.Value));
                e.CellStyle.Font = MUiFonts.Bold9;
            }
            else if (col == "License No." || col == "Registry No." || col == "Application No.") e.CellStyle.Font = MUiFonts.Mono9;
        }

        private void dgvList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow r = dgvList.Rows[e.RowIndex];
            int id = Convert.ToInt32(r.Cells["id"].Value);
            if (r.Cells["kind"].Value.ToString() == "L") OpenLicense(id);
            else OpenMarriage(id);
        }

        private void OnBoardItem(object tag)
        {
            var l = tag as LicenseFacts;
            if (l != null) OpenLicense(l.Id);
            else if (tag is int) OpenMarriage((int)tag);
        }

        private void OpenLicense(int? id)
        {
            using (var f = new MarriageLicenseForm(id)) f.ShowDialog(this);
            RefreshData();
        }

        private void OpenMarriage(int id)
        {
            DataRow r = _marriages.AsEnumerable().FirstOrDefault(x => Convert.ToInt32(x["id"]) == id);
            if (r != null && r["status"].ToString() == "Registered")
                using (var f = new MarriageRecordForm(id)) f.ShowDialog(this);
            else
                using (var f = new MarriageEntryForm(id)) f.ShowDialog(this);
            RefreshData();
        }

        private void OpenPsa()
        {
            using (var f = new PsaTransmittalForm()) f.ShowDialog(this);
            RefreshData();
        }

        private void btnLicense_Click(object sender, EventArgs e) { OpenLicense(null); }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            using (var f = new MarriageEntryForm(null)) f.ShowDialog(this);
            RefreshData();
        }

        private void btnPsa_Click(object sender, EventArgs e) { OpenPsa(); }

        private void btnTabLicenses_Click(object sender, EventArgs e) { _tab = "Licenses"; _statusFilter = null; FillGrid(); }

        private void btnTabMarriages_Click(object sender, EventArgs e) { _tab = "Marriages"; _statusFilter = null; FillGrid(); }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text.Length == 0) _statusFilter = null;
            ApplySearch();
        }
    }
}
