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
    /// BREQS desk - requests for PSA-issued copies of birth, marriage and death certificates.
    /// <para/>
    /// The list on the left is every open request (or all, with "Show closed"); the panel on the
    /// right is the one selected: who asked, for which document, where it is, and the ONE next
    /// action its status allows (record payment / submit to PSA / receive and scan / release),
    /// plus the two exits (no record at PSA, cancel) and its full history. Moves are decided by
    /// BreqsService, never by this screen.
    /// </summary>
    internal sealed class BreqsForm : Form, IRefreshable
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = new TextBox();
        private readonly CheckBox _showClosed = new CheckBox();
        private readonly Label _kRequested = new Label(), _kPaid = new Label(), _kAtPsa = new Label(), _kReady = new Label();
        private readonly Label _kRequestedSub = new Label(), _kPaidSub = new Label(), _kAtPsaSub = new Label(), _kReadySub = new Label();
        private readonly Panel _detail = new Panel();
        private List<BreqsRequest> _rows = new List<BreqsRequest>();
        private BreqsSettings _s = BreqsService.Settings;
        private int _selectedId;
        private readonly Timer _searchDelay = new Timer { Interval = 300 };

        public BreqsForm()
        {
            Text = "PSA Copies (BREQS)";
            BackColor = UiTheme.PageBg;
            ClientSize = new Size(1400, 900);
            AutoScrollMinSize = new Size(1100, 700);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24, 18, 24, 18), BackColor = UiTheme.PageBg };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildKpis(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            Controls.Add(root);

            _searchDelay.Tick += (s, e) => { _searchDelay.Stop(); LoadList(); };
            UiTheme.Polish(this);
            LoadList();
        }

        public void RefreshData() { _s = BreqsService.Settings; LoadList(); }

        // ================================================================ layout
        private Control BuildHeader()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var title = MUi.Txt("PSA Copies (BREQS)", 17F, FontStyle.Bold);
            title.Location = new Point(0, 0);
            var sub = MUi.Txt("Requests for PSA-issued birth, marriage and death certificates - logged here or at the kiosk, submitted to PSA, collected, scanned and released.",
                9.5F, FontStyle.Regular, UiTheme.Muted);
            sub.Location = new Point(2, 40);

            var bar = new FlowLayoutPanel { Location = new Point(0, 64), Height = 38, Width = 1300, BackColor = Color.Transparent, WrapContents = false, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            var add = MUi.Btn("+ New request", MUi.Kind.Primary, 140);
            add.Click += (s, e) => NewRequest(null);
            _search.Width = 320; _search.Font = MUi.F(10F); _search.Margin = new Padding(16, 5, 6, 0);
            SetCue(_search, "Search request no., requester, name on the document, PSA ref...");
            _search.TextChanged += (s, e) => { _searchDelay.Stop(); _searchDelay.Start(); };
            _showClosed.Text = "Show released / closed"; _showClosed.AutoSize = true; _showClosed.Margin = new Padding(12, 9, 0, 0);
            _showClosed.CheckedChanged += (s, e) => LoadList();
            var refresh = MUi.Btn("Refresh", MUi.Kind.Secondary, 90);
            refresh.Margin = new Padding(12, 0, 0, 0);
            refresh.Click += (s, e) => RefreshData();
            bar.Controls.AddRange(new Control[] { add, _search, _showClosed, refresh });

            p.Controls.Add(title); p.Controls.Add(sub); p.Controls.Add(bar);
            p.Resize += (s, e) => bar.Width = p.Width;
            return p;
        }

        private Control BuildKpis()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };
            for (int i = 0; i < 4; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            t.Controls.Add(Tile("TO BE PAID", _kRequested, _kRequestedSub, UiTheme.Warning), 0, 0);
            t.Controls.Add(Tile("PAID - SUBMIT TO PSA", _kPaid, _kPaidSub, UiTheme.Accent), 1, 0);
            t.Controls.Add(Tile("AT PSA", _kAtPsa, _kAtPsaSub, UiTheme.Accent), 2, 0);
            t.Controls.Add(Tile("READY FOR RELEASE", _kReady, _kReadySub, UiTheme.Success), 3, 0);
            return t;
        }

        private static Control Tile(string caption, Label value, Label sub, Color tone)
        {
            var card = MUi.Card(new Padding(18, 12, 18, 10));
            card.Dock = DockStyle.Fill; card.Margin = new Padding(0, 0, 14, 0);
            var cap = MUi.Txt(caption, 8.5F, FontStyle.Bold, UiTheme.Muted); cap.Dock = DockStyle.Top; cap.AutoSize = false; cap.Height = 20;
            value.Font = MUi.F(22F, FontStyle.Bold); value.ForeColor = tone; value.Dock = DockStyle.Top; value.Height = 40; value.BackColor = Color.Transparent;
            sub.Font = MUi.F(8.5F); sub.ForeColor = UiTheme.Muted; sub.Dock = DockStyle.Top; sub.Height = 18; sub.BackColor = Color.Transparent; sub.AutoEllipsis = true;
            card.Controls.Add(sub); card.Controls.Add(value); card.Controls.Add(cap);
            return card;
        }

        private Control BuildBody()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460));

            var listCard = MUi.Card(new Padding(2));
            listCard.Dock = DockStyle.Fill; listCard.Margin = new Padding(0, 0, 14, 0);
            _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.RowHeadersVisible = false;
            _grid.BackgroundColor = UiTheme.Surface; _grid.BorderStyle = BorderStyle.None;
            _grid.SelectionChanged += (s, e) => { if (_grid.CurrentRow != null && _grid.CurrentRow.Cells["Id"].Value is int id) ShowDetail(id); };
            _grid.CellFormatting += GridFormatting;
            listCard.Controls.Add(_grid);

            var detailCard = MUi.Card(new Padding(18, 14, 12, 14));
            detailCard.Dock = DockStyle.Fill; detailCard.Margin = new Padding(0);
            _detail.Dock = DockStyle.Fill; _detail.AutoScroll = true; _detail.BackColor = Color.Transparent;
            detailCard.Controls.Add(_detail);

            t.Controls.Add(listCard, 0, 0); t.Controls.Add(detailCard, 1, 0);
            return t;
        }

        // ================================================================ data
        private void LoadList()
        {
            try { _rows = BreqsService.List(_search.Text, _showClosed.Checked); }
            catch (Exception ex) { _rows = new List<BreqsRequest>(); MUi.Fail(this, ex); }

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int)); dt.Columns.Add("Request No."); dt.Columns.Add("Logged");
            dt.Columns.Add("Requester"); dt.Columns.Add("Document"); dt.Columns.Add("Copies", typeof(int));
            dt.Columns.Add("Status"); dt.Columns.Add("Expected from PSA");
            DateTime today = DateTime.Today;
            foreach (BreqsRequest r in _rows)
                dt.Rows.Add(r.Id, r.RequestNo, r.CreatedAt.HasValue ? r.CreatedAt.Value.ToString("dd MMM yyyy") : "", r.RequesterName, r.DocumentLine,
                            r.Copies, BreqsService.DisplayStatus(r, today, _s), r.Status == BreqsService.Submitted ? MUi.D(r.ExpectedDate) : "");
            int keep = _selectedId;
            _grid.DataSource = dt;
            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
            if (_grid.Columns.Contains("Document")) _grid.Columns["Document"].FillWeight = 180;
            if (_grid.Columns.Contains("Copies")) _grid.Columns["Copies"].FillWeight = 62;

            RefreshKpis();
            if (keep > 0 && SelectRow(keep)) return;
            if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
            else ShowDetail(0);
        }

        private bool SelectRow(int id)
        {
            foreach (DataGridViewRow row in _grid.Rows)
                if (row.Cells["Id"].Value is int v && v == id)
                {
                    _grid.CurrentCell = row.Cells[1];
                    ShowDetail(id);
                    return true;
                }
            return false;
        }

        private void RefreshKpis()
        {
            try
            {
                // Counted over ALL open requests, not the filtered list - a search must not make
                // the desk look empty.
                List<BreqsRequest> open = string.IsNullOrWhiteSpace(_search.Text) && !_showClosed.Checked ? _rows : BreqsService.List(null, false);
                DateTime today = DateTime.Today;
                _kRequested.Text = open.Count(r => r.Status == BreqsService.Requested).ToString();
                _kRequestedSub.Text = "logged, awaiting the Treasury O.R.";
                _kPaid.Text = open.Count(r => r.Status == BreqsService.Paid).ToString();
                _kPaidSub.Text = "paid, not yet sent through BREQS";
                _kAtPsa.Text = open.Count(r => r.Status == BreqsService.Submitted).ToString();
                int overdue = open.Count(r => BreqsService.IsOverdue(r, today));
                _kAtPsaSub.Text = overdue == 0 ? "none overdue (" + _s.TurnaroundDays + "-day estimate)" : overdue + " overdue - past the " + _s.TurnaroundDays + "-day estimate";
                _kAtPsaSub.ForeColor = overdue == 0 ? UiTheme.Muted : UiTheme.Danger;
                _kReady.Text = open.Count(r => r.Status == BreqsService.Received).ToString();
                int unclaimed = open.Count(r => BreqsService.IsUnclaimed(r, today, _s));
                _kReadySub.Text = unclaimed == 0 ? "scanned and waiting for the client" : unclaimed + " unclaimed over " + _s.UnclaimedDays + " days";
                _kReadySub.ForeColor = unclaimed == 0 ? UiTheme.Muted : UiTheme.Warning;
            }
            catch { }
        }

        private void GridFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Status" || e.Value == null) return;
            string v = e.Value.ToString();
            e.CellStyle.Font = MUiFonts.Bold9;
            e.CellStyle.ForeColor = v == "Overdue at PSA" ? UiTheme.Danger
                : v == "Ready for release" ? UiTheme.Success
                : v == "Unclaimed" || v == BreqsService.Requested ? UiTheme.Warning
                : v == BreqsService.Released ? UiTheme.Muted
                : v == BreqsService.Cancelled || v == BreqsService.NoRecord ? UiTheme.Faint : UiTheme.Accent;
        }

        // ================================================================ detail
        private void ShowDetail(int id)
        {
            _selectedId = id;
            _detail.SuspendLayout();
            foreach (Control c in _detail.Controls.Cast<Control>().ToList()) c.Dispose();
            _detail.Controls.Clear();

            BreqsRequest r = id > 0 ? BreqsService.Load(id) : null;
            if (r == null)
            {
                var empty = MUi.Txt("Select a request on the left, or log a new one with + New request.", 10F, FontStyle.Regular, UiTheme.Muted);
                empty.MaximumSize = new Size(400, 0); empty.Dock = DockStyle.Top;
                _detail.Controls.Add(empty);
                _detail.ResumeLayout();
                return;
            }

            DateTime today = DateTime.Today;
            string display = BreqsService.DisplayStatus(r, today, _s);
            var items = new List<Control>();

            var head = new Panel { Height = 64, BackColor = Color.Transparent };
            var no = MUi.Txt(r.RequestNo, 15F, FontStyle.Bold); no.Location = new Point(0, 2);
            var pill = MUi.Pill(display.ToUpperInvariant(), PillTone(display)); pill.Location = new Point(0, 36);
            var src = MUi.Txt((r.Source == BreqsService.SourceKiosk ? "logged at the kiosk" : "logged at the counter") + (r.CreatedAt.HasValue ? " - " + r.CreatedAt.Value.ToString("dd MMM yyyy h:mm tt") : ""),
                8.5F, FontStyle.Regular, UiTheme.Muted);
            src.Location = new Point(0, 36);
            head.Controls.Add(no); head.Controls.Add(pill); head.Controls.Add(src);
            head.Layout += (s, e) => src.Left = pill.Right + 10;
            items.Add(head);

            items.Add(NextStep(r, display));
            items.Add(Actions(r));

            items.Add(MUi.Cap("Requester"));
            items.Add(MUi.Kv("Name", r.RequesterName));
            items.Add(MUi.Kv("Relationship to owner", r.Relationship ?? "-"));
            items.Add(MUi.Kv("Valid ID", (r.ValidIdType ?? "-") + (string.IsNullOrEmpty(r.ValidIdNo) ? "" : " - " + r.ValidIdNo)));
            items.Add(MUi.Kv("Contact", r.ContactNo ?? "-"));

            items.Add(MUi.Cap("Document requested"));
            items.Add(MUi.Kv("Certificate", r.DocType + " - " + r.Copies + " cop" + (r.Copies == 1 ? "y" : "ies")));
            items.Add(MUi.Kv(r.DocType == BreqsService.Marriage ? "Husband" : r.DocType == BreqsService.Death ? "Deceased" : "Name on certificate", r.OwnerName));
            if (r.DocType == BreqsService.Marriage) items.Add(MUi.Kv("Wife", r.SpouseName));
            items.Add(MUi.Kv("Date of " + r.DocType.ToLowerInvariant(), MUi.D(r.EventDate)));
            items.Add(MUi.Kv("Place", string.Join(", ", new[] { r.EventCity, r.EventProvince }.Where(x => !string.IsNullOrWhiteSpace(x))) is string pl && pl.Length > 0 ? pl : "-"));
            if (r.DocType == BreqsService.Birth)
            {
                items.Add(MUi.Kv("Father", r.FatherName ?? "-"));
                items.Add(MUi.Kv("Mother (maiden name)", r.MotherMaidenName ?? "-"));
            }
            items.Add(MUi.Kv("Purpose", r.Purpose ?? "-"));

            items.Add(MUi.Cap("Payment and PSA"));
            items.Add(MUi.Kv("Fee", r.FeeAmount.HasValue ? "PHP " + r.FeeAmount.Value.ToString("#,0.00") : "-"));
            items.Add(MUi.Kv("Treasury O.R.", string.IsNullOrEmpty(r.OrNo) ? "not recorded" : r.OrNo + "  (" + MUi.D(r.OrDate) + ")"));
            items.Add(MUi.Kv("Submitted to PSA", r.SubmittedAt.HasValue ? MUi.D(r.SubmittedAt) + (string.IsNullOrEmpty(r.PsaReferenceNo) ? "" : " - ref " + r.PsaReferenceNo) : "-"));
            if (r.ExpectedDate.HasValue)
                items.Add(MUi.Kv("Expected", MUi.D(r.ExpectedDate), BreqsService.IsOverdue(r, today) ? UiTheme.Danger : (Color?)null));

            if (r.ReceivedAt.HasValue || r.HasScan)
            {
                items.Add(MUi.Cap("PSA copy received"));
                items.Add(MUi.Kv("Received", MUi.D(r.ReceivedAt)));
                items.Add(MUi.Kv("OCR read", (r.OcrDocKind ?? "-") + (r.OcrConfidence.HasValue ? "  " + r.OcrConfidence + "%" : "")));
                items.Add(MUi.Kv("Name on the copy", r.OcrName ?? "(not read)"));
                items.Add(MUi.Kv("Matches the request", r.OcrMatch ?? "-", MatchColor(r.OcrMatch)));
                if (!string.IsNullOrEmpty(r.PsaSecurityNo)) items.Add(MUi.Kv("Security paper no.", r.PsaSecurityNo));
            }
            if (r.ReleasedAt.HasValue)
            {
                items.Add(MUi.Cap("Released"));
                items.Add(MUi.Kv("Released", MUi.D(r.ReleasedAt)));
                items.Add(MUi.Kv(r.ClaimantIsRep ? "Representative" : "Claimant", r.ClaimantName ?? "-"));
                items.Add(MUi.Kv("Claimant ID", (r.ClaimantIdType ?? "-") + (string.IsNullOrEmpty(r.ClaimantIdNo) ? "" : " - " + r.ClaimantIdNo)));
            }
            if (!string.IsNullOrEmpty(r.OutcomeReason)) items.Add(MUi.Kv(r.Status == BreqsService.Cancelled ? "Cancelled because" : "PSA result", r.OutcomeReason, UiTheme.Warning));

            items.Add(MUi.Cap("History"));
            var hist = new DataGridView
            {
                Height = 170, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, BorderStyle = BorderStyle.None,
                BackgroundColor = UiTheme.Surface, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            try { hist.DataSource = BreqsService.History(r.Id); } catch { }
            UiTheme.Polish(hist);
            items.Add(hist);

            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; _detail.Controls.Add(items[i]); }
            for (int i = 0; i < items.Count; i++) items[i].TabIndex = i;
            UiTheme.Polish(_detail);
            _detail.ResumeLayout();
        }

        private static string PillTone(string display)
        {
            switch (display)
            {
                case "Ready for release": case BreqsService.Released: return "Valid";
                case "Overdue at PSA": case BreqsService.Cancelled: case BreqsService.NoRecord: return "Expired";
                case BreqsService.Requested: case "Unclaimed": return "Expiring";
                default: return "Sent";
            }
        }

        private static Color? MatchColor(string m)
        {
            return m == "Match" ? UiTheme.Success : m == "Partial" || m == "Unread" ? UiTheme.Warning : m == null ? (Color?)null : UiTheme.Danger;
        }

        /// <summary>One sentence: what happens next for this request, and who does it.</summary>
        private Control NextStep(BreqsRequest r, string display)
        {
            string text;
            RuleSeverity sev = RuleSeverity.Info;
            bool ok = false;
            switch (r.Status)
            {
                case BreqsService.Requested: text = "Next: the client pays PHP " + (r.FeeAmount ?? 0).ToString("#,0.00") + " at the Treasury. Record the O.R. here."; sev = RuleSeverity.Warning; break;
                case BreqsService.Paid: text = "Next: submit the request through PSA's BREQS and record the reference number."; break;
                case BreqsService.Submitted:
                    text = BreqsService.IsOverdue(r, DateTime.Today)
                        ? "Overdue: expected from PSA on " + MUi.D(r.ExpectedDate) + ". Follow up with the PSA office."
                        : "Waiting for PSA - expected " + MUi.D(r.ExpectedDate) + ". When the copy is collected, receive and scan it here.";
                    sev = BreqsService.IsOverdue(r, DateTime.Today) ? RuleSeverity.Blocking : RuleSeverity.Info; break;
                case BreqsService.Received:
                    text = r.OcrMatch == "Match" ? "The scanned PSA copy names the requested person. Ready to release."
                        : "Check the scanned copy before releasing - the name on it " + (r.OcrMatch == "Unread" ? "could not be read." : "is flagged: " + (r.OcrMatch ?? "not checked") + ".");
                    ok = r.OcrMatch == "Match"; sev = ok ? RuleSeverity.Info : RuleSeverity.Warning; break;
                case BreqsService.Released: text = "Released " + MUi.D(r.ReleasedAt) + ". This request is closed."; ok = true; break;
                case BreqsService.NoRecord: text = "PSA returned no record. Closed - tell the client what PSA returned."; sev = RuleSeverity.Warning; break;
                default: text = "Cancelled. Closed."; break;
            }
            var b = new Banner();
            b.Set(sev, display, text, ok);
            return b;
        }

        private Control Actions(BreqsRequest r)
        {
            var f = new FlowLayoutPanel { Height = 86, BackColor = Color.Transparent, WrapContents = true, Padding = new Padding(0, 6, 0, 6) };
            Func<string, MUi.Kind, int, Action, Button> btn = (text, kind, w, act) =>
            {
                var b = MUi.Btn(text, kind, w); b.Margin = new Padding(0, 0, 8, 6);
                b.Click += (s, e) => { try { act(); } catch (Exception ex) { MUi.Fail(this, ex); } };
                f.Controls.Add(b);
                return b;
            };
            int? uid = Session.User == null ? (int?)null : Session.User.Id;
            switch (r.Status)
            {
                case BreqsService.Requested:
                    btn("Record payment", MUi.Kind.Primary, 150, () => { if (BreqsDialogs.Payment(this, r, _s)) Done(r.Id); });
                    btn("Edit details", MUi.Kind.Secondary, 120, () => EditRequest(r));
                    btn("Cancel request", MUi.Kind.Ghost, 130, () => Exit(r, false));
                    break;
                case BreqsService.Paid:
                    btn("Submit to PSA", MUi.Kind.Primary, 150, () => { if (BreqsDialogs.Submit(this, r, _s)) Done(r.Id); });
                    btn("Edit details", MUi.Kind.Secondary, 120, () => EditRequest(r));
                    btn("Cancel request", MUi.Kind.Ghost, 130, () => Exit(r, false));
                    break;
                case BreqsService.Submitted:
                    btn("Receive && scan PSA copy", MUi.Kind.Success, 200, () => { if (BreqsDialogs.Receive(this, r)) Done(r.Id); });
                    btn("No record at PSA", MUi.Kind.Ghost, 150, () => Exit(r, true));
                    break;
                case BreqsService.Received:
                    btn("Release to client", MUi.Kind.Success, 160, () => { if (BreqsDialogs.Release(this, r)) Done(r.Id); });
                    btn("View scanned copy", MUi.Kind.Secondary, 160, () => ViewScan(r));
                    break;
                default:
                    if (r.HasScan) btn("View scanned copy", MUi.Kind.Secondary, 160, () => ViewScan(r));
                    break;
            }
            if (f.Controls.Count <= 2) f.Height = 50;
            return f;
        }

        private void Exit(BreqsRequest r, bool noRecord)
        {
            int? uid = Session.User == null ? (int?)null : Session.User.Id;
            string why = MUi.Ask(this, noRecord ? "No record at PSA" : "Cancel request",
                noRecord ? "What did PSA return? (e.g. no record found, negative certification issued)" : "Why is this request being cancelled?");
            if (why == null) return;
            if (noRecord) BreqsService.MarkNoRecord(r.Id, why, uid); else BreqsService.Cancel(r.Id, why, uid);
            Done(r.Id);
        }

        private void ViewScan(BreqsRequest r)
        {
            byte[] img = BreqsService.ScanImage(r.Id);
            if (img == null) { MessageBox.Show(this, "No scan is stored for this request.", "BREQS", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            SoftcopyViewer.Show(img, r.RequestNo + " - PSA " + r.DocType.ToLowerInvariant() + " copy - " + r.OwnerName, this);
        }

        private void Done(int id) { _selectedId = id; LoadList(); }

        private void NewRequest(BreqsRequest prefill)
        {
            using (var d = new BreqsRequestDialog(prefill ?? new BreqsRequest(), _s))
                if (d.ShowDialog(this) == DialogResult.OK) Done(d.SavedId);
        }

        private void EditRequest(BreqsRequest r)
        {
            using (var d = new BreqsRequestDialog(r, _s))
                if (d.ShowDialog(this) == DialogResult.OK) Done(d.SavedId);
        }

        /// <summary>
        /// Called from Queue Management when a BREQS ticket is served. A kiosk request already
        /// exists for the ticket - open it. A ticket with no request (staff-issued, or the kiosk
        /// could not save one) opens a new request pre-filled with what the ticket knows.
        /// </summary>
        public void PrepareFromQueueTicket(int ticketId)
        {
            BreqsRequest existing = null;
            try { existing = BreqsService.LoadByTicket(ticketId); } catch { }
            if (existing != null)
            {
                _search.Text = "";
                _showClosed.Checked = true;
                _selectedId = existing.Id;
                LoadList();
                return;
            }
            var r = new BreqsRequest { QueueTicketId = ticketId, Source = BreqsService.SourceCounter };
            string hint = null;
            try
            {
                DataTable t = Db.Pull("SELECT ticket_code, full_name, contact_no, valid_id_type FROM queue_tickets WHERE id = @id", new MySql.Data.MySqlClient.MySqlParameter("@id", ticketId));
                if (t.Rows.Count > 0)
                {
                    // The ticket stores the name JOINED ("First Middle Last"). It is shown to staff
                    // as a hint, NOT split into the three boxes: a split cannot tell "Juan Dela Cruz"
                    // from "Juan Dela / Cruz" - the two-word-surname failure recorded 2026-09-02.
                    hint = "Queue ticket " + t.Rows[0]["ticket_code"] + ": " + (t.Rows[0]["full_name"] as string ?? "(no name)");
                    r.ContactNo = t.Rows[0]["contact_no"] as string;
                    r.ValidIdType = t.Rows[0]["valid_id_type"] as string;
                }
            }
            catch { }
            BeginInvoke(new Action(() =>
            {
                using (var d = new BreqsRequestDialog(r, _s) { Hint = hint })
                    if (d.ShowDialog(this) == DialogResult.OK) Done(d.SavedId);
            }));
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);
        private static void SetCue(TextBox t, string cue) { t.HandleCreated += (s, e) => SendMessage(t.Handle, 0x1501, (IntPtr)1, cue); }
    }
}
