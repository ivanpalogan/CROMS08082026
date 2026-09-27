using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Old, already-registered marriage records digitized (or hand-transcribed) from the
    /// paper registry books — committed straight to <c>marriages</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Marriage Registration (Form 97) screen. That screen is for a solemnization actually
    /// being registered today; this one is a full Add / View / Edit / Delete workbench over
    /// the backlog alone, so a decades-old registry-book entry never clutters — and never
    /// needs — the live Form 97 workflow (licence linking, OCR review, PSA endorsement,
    /// etc. all belong to a marriage being registered NOW, not one being typed in from a
    /// 1980s ledger).
    ///
    /// Layout mirrors Old Birth/Death Records' own card/list &lt;-&gt; card/entry pattern:
    /// a full-width list card with a search box and a grid, and — once a row is opened —
    /// a full-width entry card with the record's fields grouped into tabs (Registration /
    /// Husband / Wife / Marriage Details).
    /// </summary>
    public class OldMarriageRecordsForm : Form, IRefreshable
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
        private ComboBox cboStatus;

        private TextBox txtHFirst, txtHMiddle, txtHLast, txtHAge, txtHPlace, txtHCivil;
        private ComboBox cboHSex;
        private DateTimePicker dtpHDob;

        private TextBox txtWFirst, txtWMiddle, txtWLast, txtWAge, txtWPlace, txtWCivil;
        private ComboBox cboWSex;
        private DateTimePicker dtpWDob;

        private DateTimePicker dtpMarriage;
        private TextBox txtMarriageTime, txtPlaceOfMarriage, txtSolemnizer, txtRemarks;

        // Step 10 — read-only digitization metadata; never in _inputs, so it's never
        // enabled by SetMode and can never be hand-edited.
        private TextBox txtDigitizedBy, txtDateDigitized, txtEncodingMethod, txtSourceRef;

        private List<Control> _inputs;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;

        public OldMarriageRecordsForm()
        {
            Text = "Marriage Record";
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
                Text = "Marriage Record",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Backlog marriages digitized or hand-transcribed from old registry books. Not shown " +
                       "on the live Marriage Registration screen.",
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

            var tabHusband = NewTab("Husband");
            var gH = FieldGrid(tabHusband);
            txtHFirst = AddField(gH, "First Name");
            txtHMiddle = AddField(gH, "Middle Name");
            txtHLast = AddField(gH, "Last Name");
            cboHSex = AddCombo(gH, "Sex", new[] { "Male", "Female" });
            txtHAge = AddField(gH, "Age");
            dtpHDob = AddDate(gH, "Date of Birth");
            txtHPlace = AddField(gH, "Place of Birth");
            txtHCivil = AddField(gH, "Civil Status");

            var tabWife = NewTab("Wife");
            var gW = FieldGrid(tabWife);
            txtWFirst = AddField(gW, "First Name");
            txtWMiddle = AddField(gW, "Middle Name");
            txtWLast = AddField(gW, "Last Name");
            cboWSex = AddCombo(gW, "Sex", new[] { "Male", "Female" });
            txtWAge = AddField(gW, "Age");
            dtpWDob = AddDate(gW, "Date of Birth");
            txtWPlace = AddField(gW, "Place of Birth");
            txtWCivil = AddField(gW, "Civil Status");

            var tabDetails = NewTab("Marriage Details");
            var gDetails = FieldGrid(tabDetails);
            dtpMarriage = AddDate(gDetails, "Date of Marriage");
            txtMarriageTime = AddField(gDetails, "Time of Marriage");
            txtPlaceOfMarriage = AddField(gDetails, "Place of Marriage");
            txtSolemnizer = AddField(gDetails, "Solemnizing Officer");
            txtRemarks = AddField(gDetails, "Remarks", multiline: true);

            tabControl.TabPages.Add(tabReg);
            tabControl.TabPages.Add(tabHusband);
            tabControl.TabPages.Add(tabWife);
            tabControl.TabPages.Add(tabDetails);
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
            _inputs.Remove(box); // Step 10 metadata is display-only, never editable
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
            lblEntryTitle.Text = "New old marriage record";
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
                "SELECT id, registry_no AS 'Registry No.', " +
                "TRIM(CONCAT(husband_last_name,', ',husband_first_name)) AS Husband, " +
                "TRIM(CONCAT(wife_last_name,', ',wife_first_name)) AS Wife, " +
                "date_of_marriage AS 'Date of Marriage', book_volume AS 'Book/Vol', status AS Status " +
                "FROM marriages WHERE record_source = 'OCR-Backlog'";
            MySqlParameter[] ps;
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR husband_first_name LIKE @t OR husband_last_name LIKE @t " +
                       "OR wife_first_name LIKE @t OR wife_last_name LIKE @t)";
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
            DataTable dt = Db.Pull("SELECT * FROM marriages WHERE id = @id AND record_source = 'OCR-Backlog'",
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

            txtHFirst.Text = Str(r, "husband_first_name");
            txtHMiddle.Text = Str(r, "husband_middle_name");
            txtHLast.Text = Str(r, "husband_last_name");
            SetCombo(cboHSex, dt.Columns.Contains("husband_sex") ? Str(r, "husband_sex") : "");
            txtHAge.Text = Str(r, "husband_age");
            SetDate(dtpHDob, r["husband_date_of_birth"]);
            txtHPlace.Text = dt.Columns.Contains("husband_place_of_birth") ? Str(r, "husband_place_of_birth") : "";
            txtHCivil.Text = Str(r, "husband_civil_status");

            txtWFirst.Text = Str(r, "wife_first_name");
            txtWMiddle.Text = Str(r, "wife_middle_name");
            txtWLast.Text = Str(r, "wife_last_name");
            SetCombo(cboWSex, dt.Columns.Contains("wife_sex") ? Str(r, "wife_sex") : "");
            txtWAge.Text = Str(r, "wife_age");
            SetDate(dtpWDob, r["wife_date_of_birth"]);
            txtWPlace.Text = dt.Columns.Contains("wife_place_of_birth") ? Str(r, "wife_place_of_birth") : "";
            txtWCivil.Text = Str(r, "wife_civil_status");

            SetDate(dtpMarriage, r["date_of_marriage"]);
            txtMarriageTime.Text = Str(r, "time_of_marriage");
            txtPlaceOfMarriage.Text = dt.Columns.Contains("place_of_marriage") ? Str(r, "place_of_marriage") : "";
            txtSolemnizer.Text = Str(r, "solemnizer");
            txtRemarks.Text = dt.Columns.Contains("remarks") ? Str(r, "remarks") : "";

            // Guarded: migration 71 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = (txtHLast.Text + " & " + txtWLast.Text).Trim(new[] { ' ', '&' });
            if (lblEntryTitle.Text.Length == 0) lblEntryTitle.Text = "#" + id;
            lblEntrySub.Text = "Registry No. " + (txtReg.Text.Length > 0 ? txtReg.Text : "(none)") +
                                "  ·  Status: " + (cboStatus.SelectedItem?.ToString() ?? "—");
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            foreach (Control c in new Control[] {
                txtReg, txtBookVol, txtBookPage,
                txtHFirst, txtHMiddle, txtHLast, txtHAge, txtHPlace, txtHCivil,
                txtWFirst, txtWMiddle, txtWLast, txtWAge, txtWPlace, txtWCivil,
                txtMarriageTime, txtPlaceOfMarriage, txtSolemnizer, txtRemarks })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboHSex.SelectedIndex = -1;
            cboWSex.SelectedIndex = -1;
            foreach (DateTimePicker dtp in new[] { dtpHDob, dtpWDob, dtpMarriage })
                dtp.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtHFirst.Text) || string.IsNullOrWhiteSpace(txtHLast.Text) ||
                string.IsNullOrWhiteSpace(txtWFirst.Text) || string.IsNullOrWhiteSpace(txtWLast.Text))
            {
                MessageBox.Show("Enter at least the husband's and wife's first and last names.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),

                new MySqlParameter("@hfn", txtHFirst.Text.Trim()),
                new MySqlParameter("@hmn", NullIfEmpty(txtHMiddle.Text)),
                new MySqlParameter("@hln", txtHLast.Text.Trim()),
                new MySqlParameter("@hsex", (object)cboHSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@hage", int.TryParse(txtHAge.Text, out int ha) ? (object)ha : DBNull.Value),
                new MySqlParameter("@hdob", dtpHDob.Checked ? (object)dtpHDob.Value.Date : DBNull.Value),
                new MySqlParameter("@hplace", NullIfEmpty(txtHPlace.Text)),
                new MySqlParameter("@hcivil", NullIfEmpty(txtHCivil.Text)),

                new MySqlParameter("@wfn", txtWFirst.Text.Trim()),
                new MySqlParameter("@wmn", NullIfEmpty(txtWMiddle.Text)),
                new MySqlParameter("@wln", txtWLast.Text.Trim()),
                new MySqlParameter("@wsex", (object)cboWSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@wage", int.TryParse(txtWAge.Text, out int wa) ? (object)wa : DBNull.Value),
                new MySqlParameter("@wdob", dtpWDob.Checked ? (object)dtpWDob.Value.Date : DBNull.Value),
                new MySqlParameter("@wplace", NullIfEmpty(txtWPlace.Text)),
                new MySqlParameter("@wcivil", NullIfEmpty(txtWCivil.Text)),

                new MySqlParameter("@mdate", dtpMarriage.Checked ? (object)dtpMarriage.Value.Date : DBNull.Value),
                new MySqlParameter("@mtime", NullIfEmpty(txtMarriageTime.Text)),
                new MySqlParameter("@mplace", NullIfEmpty(txtPlaceOfMarriage.Text)),
                new MySqlParameter("@solemnizer", NullIfEmpty(txtSolemnizer.Text)),
                new MySqlParameter("@remarks", NullIfEmpty(txtRemarks.Text)),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: this screen is hand-transcription with no scan behind it —
                // encoding_method states that plainly, so it is never mistaken for an
                // OCR-read record on a later audit.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO marriages (registry_no, book_volume, book_page, status, " +
                    "husband_first_name, husband_middle_name, husband_last_name, husband_sex, husband_age, " +
                    "husband_date_of_birth, husband_place_of_birth, husband_civil_status, " +
                    "wife_first_name, wife_middle_name, wife_last_name, wife_sex, wife_age, " +
                    "wife_date_of_birth, wife_place_of_birth, wife_civil_status, " +
                    "date_of_marriage, time_of_marriage, place_of_marriage, solemnizer, remarks, " +
                    "scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, " +
                    "@hfn, @hmn, @hln, @hsex, @hage, @hdob, @hplace, @hcivil, " +
                    "@wfn, @wmn, @wln, @wsex, @wage, @wdob, @wplace, @wcivil, " +
                    "@mdate, @mtime, @mplace, @solemnizer, @remarks, " +
                    "@scan, 'OCR-Backlog', @digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "marriages", id, "Old marriage record added by hand (OCR-Backlog)");
                MessageBox.Show("Old marriage record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
                SetMode(view: true);
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE marriages SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "husband_first_name=@hfn, husband_middle_name=@hmn, husband_last_name=@hln, husband_sex=@hsex, " +
                    "husband_age=@hage, husband_date_of_birth=@hdob, husband_place_of_birth=@hplace, husband_civil_status=@hcivil, " +
                    "wife_first_name=@wfn, wife_middle_name=@wmn, wife_last_name=@wln, wife_sex=@wsex, " +
                    "wife_age=@wage, wife_date_of_birth=@wdob, wife_place_of_birth=@wplace, wife_civil_status=@wcivil, " +
                    "date_of_marriage=@mdate, time_of_marriage=@mtime, place_of_marriage=@mplace, " +
                    "solemnizer=@solemnizer, remarks=@remarks " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "marriages", _editingId.Value, "Old marriage record updated (OCR-Backlog)");
                MessageBox.Show("Old marriage record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (MessageBox.Show("Delete this old marriage record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            Db.Push("DELETE FROM marriages WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            Audit.Write(Audit.Delete, "marriages", id, "Old marriage record deleted (OCR-Backlog)");
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
            SoftcopyViewer.Show(_scanImage, "Old Marriage Record — " + txtHLast.Text + " & " + txtWLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : r[col].ToString();

        private static void SetCombo(ComboBox cbo, string value)
        {
            cbo.SelectedIndex = -1;
            if (string.IsNullOrEmpty(value)) return;
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
    }
}
