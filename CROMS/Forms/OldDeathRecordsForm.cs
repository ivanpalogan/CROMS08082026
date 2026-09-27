using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Old, already-registered death records digitized through Intelligent Document
    /// Processing (OCR) — committed straight to <c>deaths</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Death Registration screen. That screen is for today's walk-in registrations; this one
    /// is a full Add / View / Edit / Delete workbench over the backlog alone.
    ///
    /// Layout mirrors Birth/Death Registration's own card/list <-> card/entry pattern: a
    /// full-width list card with a search box and a grid, and — once a row is opened — a
    /// full-width entry card with the record's fields grouped into tabs (Registration /
    /// Deceased / Cause &amp; Disposal / Informant / Certification), the same grouping the
    /// live forms use.
    /// </summary>
    public class OldDeathRecordsForm : Form, IRefreshable
    {
        // ---- list view ----------------------------------------------------------
        private CardPanel cardList;
        private DataGridView dgv;
        private TextBox txtSearch;
        private Button btnNewFromList, btnViewFromList, btnEditFromList, btnDeleteFromList, btnRefresh;

        // ---- entry view ----------------------------------------------------------
        private CardPanel cardEntry;
        private Label lblEntryTitle, lblEntrySub;
        private Button btnBack, btnEdit, btnSave, btnCancel, btnDeleteEntry, btnSoftcopy;
        private TabControl tabControl;

        private TextBox txtReg, txtBookVol, txtBookPage;
        private ComboBox cboStatus, cboSex;
        private TextBox txtFirst, txtMiddle, txtLast;
        private TextBox txtCivil, txtAge, txtCitizenship;
        private DateTimePicker dtpDod;
        private TextBox txtPlace, txtReligion;
        private TextBox txtImmediate, txtAntecedent, txtUnderlying;
        private TextBox txtDisposal, txtDisposalPlace;
        private DateTimePicker dtpDisposalDate;
        private TextBox txtInfName, txtInfRel, txtInfAddr;
        private DateTimePicker dtpInfDate;
        private TextBox txtPrepName, txtPrepTitle;
        private DateTimePicker dtpPrepDate;
        private TextBox txtRecvName, txtRecvTitle;
        private DateTimePicker dtpRecvDate;
        private TextBox txtRegByName, txtRegByTitle;

        // Step 10 — read-only digitization metadata; never in _inputs.
        private TextBox txtDigitizedBy, txtDateDigitized, txtEncodingMethod, txtSourceRef;
        private DateTimePicker dtpRegByDate;

        private List<Control> _inputs;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;

        public OldDeathRecordsForm()
        {
            Text = "Old Death Records (OCR)";
            BuildUi();
            LoadGrid();
            ShowListView();
        }

        public void RefreshData()
        {
            LoadGrid();
            if (!cardEntry.Visible) ShowListView();
        }

        // ---- UI: shell ---------------------------------------------------------

        private void BuildUi()
        {
            Width = 1400;
            Height = 900;
            BackColor = UiTheme.PageBg;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 12, 24, 6), BackColor = UiTheme.PageBg };
            var title = new Label
            {
                Text = "Old Death Records (OCR)",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Backlog records digitized from old registry books — committed straight from " +
                       "Intelligent Document Processing. Not shown on the live Death Registration screen.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(0, 28)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 20) };
            root.Controls.Add(body, 0, 1);

            BuildListCard(body);
            BuildEntryCard(body);
        }

        // ---- UI: list card ---------------------------------------------------------

        private void BuildListCard(Panel host)
        {
            cardList = new CardPanel { Dock = DockStyle.Fill, Radius = 12 };
            host.Controls.Add(cardList);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardList.Controls.Add(layout);

            var toolbar = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.Controls.Add(toolbar, 0, 0);

            var searchRow = new TableLayoutPanel { Dock = DockStyle.Left, AutoSize = true, ColumnCount = 2 };
            var lblSearch = new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) };
            txtSearch = new TextBox { Width = 320 };
            txtSearch.TextChanged += (s, e) => LoadGrid();
            searchRow.Controls.Add(lblSearch, 0, 0);
            searchRow.Controls.Add(txtSearch, 1, 0);
            toolbar.Controls.Add(searchRow, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            btnNewFromList = new Button { Text = "+ New Record", Width = 120 };
            btnViewFromList = new Button { Text = "View", Width = 90, Enabled = false };
            btnEditFromList = new Button { Text = "Edit", Width = 90, Enabled = false };
            btnDeleteFromList = new Button { Text = "Delete", Width = 90, Enabled = false };
            btnRefresh = new Button { Text = "Refresh", Width = 90 };
            btnNewFromList.Click += (s, e) => OpenNew();
            btnViewFromList.Click += (s, e) => OpenSelected(view: true);
            btnEditFromList.Click += (s, e) => OpenSelected(view: false);
            btnDeleteFromList.Click += (s, e) => DeleteFromList();
            btnRefresh.Click += (s, e) => LoadGrid();
            actions.Controls.Add(btnNewFromList);
            actions.Controls.Add(btnViewFromList);
            actions.Controls.Add(btnEditFromList);
            actions.Controls.Add(btnDeleteFromList);
            actions.Controls.Add(btnRefresh);
            toolbar.Controls.Add(actions, 1, 0);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.SelectionChanged += (s, e) => UpdateListButtons();
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenSelected(view: true); };
            layout.Controls.Add(dgv, 0, 1);
        }

        private void UpdateListButtons()
        {
            bool has = dgv.CurrentRow != null;
            btnViewFromList.Enabled = has;
            btnEditFromList.Enabled = has;
            btnDeleteFromList.Enabled = has;
        }

        private long? SelectedId()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt64(dgv.CurrentRow.Cells["id"].Value);
        }

        // ---- UI: entry card ---------------------------------------------------------

        private void BuildEntryCard(Panel host)
        {
            cardEntry = new CardPanel { Dock = DockStyle.Fill, Radius = 12, Visible = false };
            host.Controls.Add(cardEntry);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardEntry.Controls.Add(layout);

            var top = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.Controls.Add(top, 0, 0);

            var titleBox = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.TopDown };
            btnBack = new Button { Text = "← Back to List", Width = 130 };
            btnBack.Click += (s, e) => ShowListView();
            lblEntryTitle = new Label { Text = "New record", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UiTheme.Ink, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            lblEntrySub = new Label { Text = "", Font = new Font("Segoe UI", 9F), ForeColor = UiTheme.Muted, AutoSize = true };
            titleBox.Controls.Add(btnBack);
            titleBox.Controls.Add(lblEntryTitle);
            titleBox.Controls.Add(lblEntrySub);
            top.Controls.Add(titleBox, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            btnEdit = new Button { Text = "Edit", Width = 90 };
            btnSave = new Button { Text = "Save", Width = 90 };
            btnCancel = new Button { Text = "Cancel", Width = 90 };
            btnDeleteEntry = new Button { Text = "Delete", Width = 90 };
            btnSoftcopy = new Button { Text = "View Softcopy", Width = 130 };
            btnEdit.Click += (s, e) => EnterEditMode();
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => CancelEdit();
            btnDeleteEntry.Click += (s, e) => DeleteFromEntry();
            btnSoftcopy.Click += (s, e) => ShowSoftcopy();
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnCancel);
            actions.Controls.Add(btnDeleteEntry);
            actions.Controls.Add(btnSoftcopy);
            top.Controls.Add(actions, 1, 0);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            layout.Controls.Add(tabControl, 0, 1);

            _inputs = new List<Control>();

            var tabReg = NewTab("Registration");
            var gReg = FieldGrid(tabReg);
            txtReg = AddField(gReg, "Registry No.");
            txtBookVol = AddField(gReg, "Book / Volume (Year)");
            txtBookPage = AddField(gReg, "Book Page");
            cboStatus = AddCombo(gReg, "Status", new[] { "Draft", "Registered" });
            txtDigitizedBy = AddReadOnlyField(gReg, "Digitized By");
            txtDateDigitized = AddReadOnlyField(gReg, "Date Digitized");
            txtEncodingMethod = AddReadOnlyField(gReg, "Encoding Method");
            txtSourceRef = AddReadOnlyField(gReg, "Source Reference");

            var tabDeceased = NewTab("Deceased");
            var gDec = FieldGrid(tabDeceased);
            txtFirst = AddField(gDec, "First Name");
            txtMiddle = AddField(gDec, "Middle Name");
            txtLast = AddField(gDec, "Last Name");
            cboSex = AddCombo(gDec, "Sex", new[] { "Male", "Female" });
            txtCivil = AddField(gDec, "Civil Status");
            txtAge = AddField(gDec, "Age");
            txtCitizenship = AddField(gDec, "Citizenship");
            dtpDod = AddDate(gDec, "Date of Death");
            txtPlace = AddField(gDec, "Place of Death");
            txtReligion = AddField(gDec, "Religion");

            var tabCause = NewTab("Cause & Disposal");
            var gCause = FieldGrid(tabCause);
            txtImmediate = AddField(gCause, "Immediate Cause");
            txtAntecedent = AddField(gCause, "Antecedent Cause");
            txtUnderlying = AddField(gCause, "Underlying Cause");
            txtDisposal = AddField(gCause, "Disposal Method");
            txtDisposalPlace = AddField(gCause, "Place of Disposal");
            dtpDisposalDate = AddDate(gCause, "Date of Disposal");

            var tabInformant = NewTab("Informant");
            var gInf = FieldGrid(tabInformant);
            txtInfName = AddField(gInf, "Name");
            txtInfRel = AddField(gInf, "Relationship");
            txtInfAddr = AddField(gInf, "Address");
            dtpInfDate = AddDate(gInf, "Date Signed");

            var tabCert = NewTab("Certification");
            var gCert = FieldGrid(tabCert);
            txtPrepName = AddField(gCert, "Prepared By");
            txtPrepTitle = AddField(gCert, "Prepared By Title");
            dtpPrepDate = AddDate(gCert, "Prepared By Date");
            txtRecvName = AddField(gCert, "Received By");
            txtRecvTitle = AddField(gCert, "Received By Title");
            dtpRecvDate = AddDate(gCert, "Received By Date");
            txtRegByName = AddField(gCert, "Registered By");
            txtRegByTitle = AddField(gCert, "Registered By Title");
            dtpRegByDate = AddDate(gCert, "Registered By Date");

            tabControl.TabPages.Add(tabReg);
            tabControl.TabPages.Add(tabDeceased);
            tabControl.TabPages.Add(tabCause);
            tabControl.TabPages.Add(tabInformant);
            tabControl.TabPages.Add(tabCert);
        }

        private static TabPage NewTab(string title) => new TabPage(title) { Padding = new Padding(12) };

        private TableLayoutPanel FieldGrid(TabPage page)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(4, 6, 4, 6) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(t);
            return t;
        }

        private TextBox AddField(TableLayoutPanel g, string label, bool multiline = false)
        {
            int row = g.RowCount;
            g.RowCount = row + 1;
            g.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var cap = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, multiline ? 8 : 6, 8, 6) };
            g.Controls.Add(cap, 0, row);
            var box = new TextBox { Dock = DockStyle.Top, Margin = new Padding(0, 4, 0, 4) };
            if (multiline) { box.Multiline = true; box.Height = 90; box.Dock = DockStyle.Fill; }
            g.Controls.Add(box, 1, row);
            _inputs.Add(box);
            return box;
        }

        private TextBox AddReadOnlyField(TableLayoutPanel g, string label)
        {
            var box = AddField(g, label);
            box.ReadOnly = true;
            box.TabStop = false;
            box.BackColor = UiTheme.PageBg;
            _inputs.Remove(box);
            return box;
        }

        private ComboBox AddCombo(TableLayoutPanel g, string label, string[] items)
        {
            int row = g.RowCount;
            g.RowCount = row + 1;
            g.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var cap = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) };
            g.Controls.Add(cap, 0, row);
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Margin = new Padding(0, 4, 0, 4) };
            box.Items.AddRange(items);
            g.Controls.Add(box, 1, row);
            _inputs.Add(box);
            return box;
        }

        private DateTimePicker AddDate(TableLayoutPanel g, string label)
        {
            int row = g.RowCount;
            g.RowCount = row + 1;
            g.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var cap = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) };
            g.Controls.Add(cap, 0, row);
            var dtp = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 240, Margin = new Padding(0, 4, 0, 4) };
            g.Controls.Add(dtp, 1, row);
            _inputs.Add(dtp);
            return dtp;
        }

        // ---- view switching ---------------------------------------------------------

        private void ShowListView()
        {
            cardEntry.Visible = false;
            cardList.Visible = true;
        }

        private void ShowEntryView()
        {
            cardList.Visible = false;
            cardEntry.Visible = true;
        }

        private void OpenNew()
        {
            ClearForm();
            SetMode(view: false);
            lblEntryTitle.Text = "New old death record";
            lblEntrySub.Text = "Not yet saved";
            ShowEntryView();
        }

        private void OpenSelected(bool view)
        {
            long? id = SelectedId();
            if (id == null) return;
            LoadRecord(id.Value);
            SetMode(view);
            ShowEntryView();
        }

        private void SetMode(bool view)
        {
            _viewOnly = view;
            foreach (var c in _inputs) c.Enabled = !view;
            btnEdit.Visible = view;
            btnSave.Visible = !view;
            btnCancel.Visible = !view && _editingId != null;
            btnDeleteEntry.Visible = view;
        }

        private void EnterEditMode() => SetMode(view: false);

        private void CancelEdit()
        {
            if (_editingId != null) { LoadRecord(_editingId.Value); SetMode(view: true); }
            else ShowListView();
        }

        // ---- data ---------------------------------------------------------------

        private void LoadGrid()
        {
            string term = (txtSearch?.Text ?? "").Trim();
            string sql =
                "SELECT id, registry_no AS 'Registry No.', full_name AS 'Deceased', sex AS Sex, " +
                "date_of_death AS 'Date of Death', book_volume AS 'Book/Vol', status AS Status " +
                "FROM deaths WHERE record_source = 'OCR-Backlog'";
            MySqlParameter[] ps;
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR full_name LIKE @t)";
                ps = new[] { new MySqlParameter("@t", "%" + term + "%") };
            }
            else ps = new MySqlParameter[0];
            sql += " ORDER BY id DESC";

            DataTable dt = ps.Length > 0 ? Db.Pull(sql, ps) : Db.Pull(sql);
            dgv.DataSource = dt;
            if (dgv.Columns.Contains("id")) dgv.Columns["id"].Visible = false;
            UpdateListButtons();
        }

        private void LoadRecord(long id)
        {
            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            _scanImage = dt.Columns.Contains("scan_image") && r["scan_image"] != DBNull.Value
                ? (byte[])r["scan_image"] : null;

            txtReg.Text = Str(r, "registry_no");
            txtBookVol.Text = Str(r, "book_volume");
            txtBookPage.Text = Str(r, "book_page");
            SetCombo(cboStatus, Str(r, "status"));

            SplitFullName(Str(r, "full_name"), out string first, out string middle, out string last);
            txtFirst.Text = first;
            txtMiddle.Text = middle;
            txtLast.Text = last;

            SetCombo(cboSex, Str(r, "sex"));
            txtCivil.Text = Str(r, "civil_status");
            txtAge.Text = Str(r, "age");
            txtCitizenship.Text = Str(r, "citizenship");
            SetDate(dtpDod, r["date_of_death"]);
            txtPlace.Text = Str(r, "place_of_death");
            txtReligion.Text = Str(r, "religion_name");
            txtImmediate.Text = Str(r, "immediate_cause");
            txtAntecedent.Text = Str(r, "antecedent_cause");
            txtUnderlying.Text = Str(r, "underlying_cause");
            txtDisposal.Text = Str(r, "disposal_method");
            txtDisposalPlace.Text = Str(r, "place_of_disposal");
            SetDate(dtpDisposalDate, r["date_of_disposal"]);
            txtInfName.Text = Str(r, "informant_name");
            txtInfRel.Text = Str(r, "informant_relationship");
            txtInfAddr.Text = Str(r, "informant_address");
            SetDate(dtpInfDate, r["informant_date"]);
            txtPrepName.Text = Str(r, "prepared_by");
            txtPrepTitle.Text = Str(r, "prepared_by_title");
            SetDate(dtpPrepDate, r["prepared_by_date"]);
            txtRecvName.Text = Str(r, "received_by");
            txtRecvTitle.Text = Str(r, "received_by_title");
            SetDate(dtpRecvDate, r["received_by_date"]);
            txtRegByName.Text = Str(r, "registered_by");
            txtRegByTitle.Text = Str(r, "registered_by_title");
            SetDate(dtpRegByDate, r["registered_by_date"]);

            // Guarded: migration 70 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = Str(r, "full_name");
            if (lblEntryTitle.Text.Length == 0) lblEntryTitle.Text = "#" + id;
            lblEntrySub.Text = "Registry No. " + (txtReg.Text.Length > 0 ? txtReg.Text : "(none)") +
                                "  ·  Status: " + (cboStatus.SelectedItem?.ToString() ?? "—");
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            foreach (Control c in new Control[] {
                txtReg, txtBookVol, txtBookPage, txtFirst, txtMiddle, txtLast, txtCivil, txtAge,
                txtCitizenship, txtPlace, txtReligion, txtImmediate, txtAntecedent, txtUnderlying,
                txtDisposal, txtDisposalPlace, txtInfName, txtInfRel, txtInfAddr,
                txtPrepName, txtPrepTitle, txtRecvName, txtRecvTitle, txtRegByName, txtRegByTitle })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboSex.SelectedIndex = -1;
            foreach (DateTimePicker dtp in new[] { dtpDod, dtpDisposalDate, dtpInfDate, dtpPrepDate, dtpRecvDate, dtpRegByDate })
                dtp.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) && string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the deceased's first or last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fullName = string.Join(" ", new[] { txtFirst.Text, txtMiddle.Text, txtLast.Text }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),
                new MySqlParameter("@name", NullIfEmpty(fullName)),
                new MySqlParameter("@sex", (object)cboSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@civil", NullIfEmpty(txtCivil.Text)),
                new MySqlParameter("@age", int.TryParse(txtAge.Text, out int a) ? (object)a : DBNull.Value),
                new MySqlParameter("@cit", NullIfEmpty(txtCitizenship.Text)),
                new MySqlParameter("@dod", dtpDod.Checked ? (object)dtpDod.Value.Date : DBNull.Value),
                new MySqlParameter("@place", NullIfEmpty(txtPlace.Text)),
                new MySqlParameter("@religion", NullIfEmpty(txtReligion.Text)),
                new MySqlParameter("@imm", NullIfEmpty(txtImmediate.Text)),
                new MySqlParameter("@ant", NullIfEmpty(txtAntecedent.Text)),
                new MySqlParameter("@und", NullIfEmpty(txtUnderlying.Text)),
                new MySqlParameter("@disp", NullIfEmpty(txtDisposal.Text)),
                new MySqlParameter("@dplace", NullIfEmpty(txtDisposalPlace.Text)),
                new MySqlParameter("@ddate", dtpDisposalDate.Checked ? (object)dtpDisposalDate.Value.Date : DBNull.Value),
                new MySqlParameter("@iname", NullIfEmpty(txtInfName.Text)),
                new MySqlParameter("@irel", NullIfEmpty(txtInfRel.Text)),
                new MySqlParameter("@iaddr", NullIfEmpty(txtInfAddr.Text)),
                new MySqlParameter("@idate", dtpInfDate.Checked ? (object)dtpInfDate.Value.Date : DBNull.Value),
                new MySqlParameter("@prep", NullIfEmpty(txtPrepName.Text)),
                new MySqlParameter("@preptitle", NullIfEmpty(txtPrepTitle.Text)),
                new MySqlParameter("@prepdate", dtpPrepDate.Checked ? (object)dtpPrepDate.Value.Date : DBNull.Value),
                new MySqlParameter("@recv", NullIfEmpty(txtRecvName.Text)),
                new MySqlParameter("@recvtitle", NullIfEmpty(txtRecvTitle.Text)),
                new MySqlParameter("@recvdate", dtpRecvDate.Checked ? (object)dtpRecvDate.Value.Date : DBNull.Value),
                new MySqlParameter("@regby", NullIfEmpty(txtRegByName.Text)),
                new MySqlParameter("@regbytitle", NullIfEmpty(txtRegByTitle.Text)),
                new MySqlParameter("@regbydate", dtpRegByDate.Checked ? (object)dtpRegByDate.Value.Date : DBNull.Value),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: hand-transcription, no scan behind it — encoding_method says so.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO deaths (registry_no, book_volume, book_page, status, full_name, sex, " +
                    "civil_status, age, citizenship, date_of_death, place_of_death, religion_name, " +
                    "immediate_cause, antecedent_cause, underlying_cause, disposal_method, place_of_disposal, date_of_disposal, " +
                    "informant_name, informant_relationship, informant_address, informant_date, " +
                    "prepared_by, prepared_by_title, prepared_by_date, " +
                    "received_by, received_by_title, received_by_date, " +
                    "registered_by, registered_by_title, registered_by_date, " +
                    "scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, @name, @sex, " +
                    "@civil, @age, @cit, @dod, @place, @religion, " +
                    "@imm, @ant, @und, @disp, @dplace, @ddate, " +
                    "@iname, @irel, @iaddr, @idate, " +
                    "@prep, @preptitle, @prepdate, " +
                    "@recv, @recvtitle, @recvdate, " +
                    "@regby, @regbytitle, @regbydate, " +
                    "@scan, 'OCR-Backlog', @digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "deaths", id, "Old death record added by hand (OCR-Backlog)");
                MessageBox.Show("Old death record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
                SetMode(view: true);
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE deaths SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "full_name=@name, sex=@sex, civil_status=@civil, age=@age, citizenship=@cit, " +
                    "date_of_death=@dod, place_of_death=@place, religion_name=@religion, " +
                    "immediate_cause=@imm, antecedent_cause=@ant, underlying_cause=@und, " +
                    "disposal_method=@disp, place_of_disposal=@dplace, date_of_disposal=@ddate, " +
                    "informant_name=@iname, informant_relationship=@irel, informant_address=@iaddr, informant_date=@idate, " +
                    "prepared_by=@prep, prepared_by_title=@preptitle, prepared_by_date=@prepdate, " +
                    "received_by=@recv, received_by_title=@recvtitle, received_by_date=@recvdate, " +
                    "registered_by=@regby, registered_by_title=@regbytitle, registered_by_date=@regbydate " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "deaths", _editingId.Value, "Old death record updated (OCR-Backlog)");
                MessageBox.Show("Old death record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(_editingId.Value);
                SetMode(view: true);
            }
        }

        private void DeleteFromList()
        {
            long? id = SelectedId();
            if (id == null) return;
            DoDelete(id.Value);
        }

        private void DeleteFromEntry()
        {
            if (_editingId == null) return;
            if (DoDelete(_editingId.Value)) ShowListView();
        }

        private bool DoDelete(long id)
        {
            if (MessageBox.Show("Delete this old death record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            Db.Push("DELETE FROM deaths WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            Audit.Write(Audit.Delete, "deaths", id, "Old death record deleted (OCR-Backlog)");
            LoadGrid();
            return true;
        }

        private void ShowSoftcopy()
        {
            if (_scanImage == null)
            {
                MessageBox.Show("No scanned image saved with this record.", "No softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(_scanImage, "Old Death Record — " + txtFirst.Text + " " + txtLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : r[col].ToString();

        private static void SetCombo(ComboBox cbo, string value)
        {
            cbo.SelectedIndex = -1;
            for (int i = 0; i < cbo.Items.Count; i++)
                if (string.Equals(cbo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                { cbo.SelectedIndex = i; break; }
        }

        private static void SetDate(DateTimePicker dtp, object value)
        {
            if (value == null || value == DBNull.Value) { dtp.Checked = false; return; }
            dtp.Value = Convert.ToDateTime(value);
            dtp.Checked = true;
        }

        private static object NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static object DigitizedBy()
        {
            var u = Session.User;
            if (u == null) return DBNull.Value;
            string name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
            return NullIfEmpty(name);
        }

        /// <summary>
        /// `deaths` keeps one joined <c>full_name</c> column, so the screen splits it back
        /// for editing (BR-2026-09-19 convention): two tokens = First/Last with no Middle,
        /// three or more = first token First, last token Last, everything between is
        /// Middle; a single unsplittable token is put whole in Last for the clerk to correct.
        /// </summary>
        private static void SplitFullName(string full, out string first, out string middle, out string last)
        {
            first = middle = last = "";
            if (string.IsNullOrWhiteSpace(full)) return;
            string[] parts = full.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) { last = parts[0]; return; }
            if (parts.Length == 2) { first = parts[0]; last = parts[1]; return; }
            first = parts[0];
            last = parts[parts.Length - 1];
            middle = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
        }
    }
}
