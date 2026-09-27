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
    /// Old, already-registered birth records digitized through Intelligent Document
    /// Processing (OCR) — committed straight to <c>births</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Birth Registration screen. That screen is for today's walk-in registrations; this one
    /// is a full Add / View / Edit / Delete workbench over the backlog alone, so an old
    /// scanned record never clutters — and never needs — the live registration form.
    ///
    /// Layout mirrors Birth Registration's own card/list <-> card/entry pattern: a full-width
    /// list card with a search box and a grid, and — once a row is opened — a full-width
    /// entry card with the record's fields grouped into tabs (Registration / Child / Mother /
    /// Father / Remarks), the same grouping the live form uses.
    /// </summary>
    public class OldBirthRecordsForm : Form, IRefreshable
    {
        // ---- books gallery (top level) -------------------------------------------
        // Hierarchy: Birth Records (this form) -> Registry Books -> Selected Book ->
        // Individual Birth Records -> Record Details. The gallery groups the backlog
        // by book_volume the same way RegistryBooksForm does (2026-09-15) so a book's
        // record count and any brand-new book both just fall out of a live re-query —
        // nothing here is tracked separately from what's already on the rows.
        private CardPanel cardBooks;
        private DataGridView dgvBooks;
        private Label lblBooksHeader;
        private Button btnBooksRefresh;

        // ---- list view (records inside the opened book) --------------------------
        private CardPanel cardList;
        private DataGridView dgv;
        private TextBox txtSearch;
        private Label lblListHeader;
        private Button btnBackToBooks, btnNewFromList, btnViewFromList, btnEditFromList, btnDeleteFromList, btnRefresh;

        // which book is currently opened (null = the "(no volume recorded)" group)
        private string _bookVolRaw;
        private string _bookVolDisplay;
        private bool _bookIsNullGroup;

        // ---- entry view ----------------------------------------------------------
        private CardPanel cardEntry;
        private Label lblEntryTitle, lblEntrySub;
        private Button btnBack, btnEdit, btnSave, btnCancel, btnDeleteEntry, btnSoftcopy;
        private TabControl tabControl;

        private TextBox txtReg, txtBookVol, txtBookPage;
        private ComboBox cboStatus, cboSex;
        private TextBox txtFirst, txtMiddle, txtLast;
        private DateTimePicker dtpDob;
        private TextBox txtTob, txtPlace, txtTypeOfBirth, txtWeight;
        private TextBox txtMFirst, txtMMiddle, txtMLast, txtMOcc, txtMRel, txtMCit;
        private TextBox txtFFirst, txtFMiddle, txtFLast, txtFOcc, txtFRel, txtFCit;
        private TextBox txtRemarks;

        // Step 10 — read-only digitization metadata; never in _inputs, so it's never
        // enabled by SetMode and can never be hand-edited.
        private TextBox txtDigitizedBy, txtDateDigitized, txtEncodingMethod, txtSourceRef;

        private List<Control> _inputs;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;

        public OldBirthRecordsForm()
        {
            Text = "Birth Record";
            BuildUi();
            LoadBooks();
        }

        public void RefreshData()
        {
            if (cardEntry.Visible) return; // don't yank focus off an open record
            if (cardList.Visible) LoadGrid();
            else LoadBooks();
        }

        /// <summary>
        /// Cross-module hand-off: after a Birth Record Digitization commit/draft/update,
        /// land here on the record's own book (creating the book's card the moment this
        /// re-queries, if it's brand new) with the record itself opened — Step 11's
        /// "Result After Saving".
        /// </summary>
        public void OpenToRecord(long id)
        {
            DataTable dt = Db.Pull(
                "SELECT book_volume FROM births WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            object volObj = dt.Rows[0]["book_volume"];
            string volRaw = volObj == DBNull.Value ? null : volObj.ToString();
            string volDisplay = string.IsNullOrEmpty(volRaw) ? "(no volume recorded)" : volRaw;

            LoadBooks();
            OpenBook(volRaw, volDisplay);
            OpenSelected(id, view: true);
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
                Text = "Birth Record",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Backlog records digitized from old registry books — committed straight from " +
                       "Intelligent Document Processing. Not shown on the live Birth Registration screen.",
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

            BuildBooksCard(body);
            BuildListCard(body);
            BuildEntryCard(body);
        }

        // ---- UI: books gallery (top level) ---------------------------------------

        private void BuildBooksCard(Panel host)
        {
            cardBooks = new CardPanel { Dock = DockStyle.Fill, Radius = 12 };
            host.Controls.Add(cardBooks);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardBooks.Controls.Add(layout);

            var toolbar = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.Controls.Add(toolbar, 0, 0);

            lblBooksHeader = new Label
            {
                Text = "BOOKS ON FILE",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 0)
            };
            toolbar.Controls.Add(lblBooksHeader, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            btnBooksRefresh = new Button { Text = "Refresh", Width = 90 };
            btnBooksRefresh.Click += (s, e) => LoadBooks();
            actions.Controls.Add(btnBooksRefresh);
            toolbar.Controls.Add(actions, 1, 0);

            dgvBooks = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvBooks.CellClick += (s, e) => { if (e.RowIndex >= 0) OpenBookFromRow(e.RowIndex); };
            dgvBooks.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenBookFromRow(e.RowIndex); };
            layout.Controls.Add(dgvBooks, 0, 1);
        }

        private void LoadBooks()
        {
            const string sql =
                "SELECT COALESCE(book_volume,'(no volume recorded)') AS 'Book / Year', " +
                "COUNT(*) AS 'Records', " +
                "COUNT(DISTINCT CASE WHEN book_page IS NOT NULL AND book_page<>'' THEN book_page END) AS 'Pages', " +
                "book_volume AS VolRaw " +
                "FROM births WHERE record_source = 'OCR-Backlog' " +
                "GROUP BY book_volume ORDER BY book_volume DESC";
            try
            {
                DataTable dt = Db.Pull(sql);
                dgvBooks.DataSource = dt;
                if (dgvBooks.Columns.Contains("VolRaw")) dgvBooks.Columns["VolRaw"].Visible = false;
                lblBooksHeader.Text = "BOOKS ON FILE  (" + dt.Rows.Count + ")";
            }
            catch (Exception ex)
            {
                lblBooksHeader.Text = "Could not load books: " + ex.Message;
            }
            ShowBooksGallery();
        }

        private void OpenBookFromRow(int rowIndex)
        {
            var row = dgvBooks.Rows[rowIndex];
            object volRawObj = row.Cells["VolRaw"].Value;
            string volRaw = volRawObj == null || volRawObj == DBNull.Value ? null : volRawObj.ToString();
            string volDisplay = row.Cells["Book / Year"].Value?.ToString();
            OpenBook(volRaw, volDisplay);
        }

        private void OpenBook(string volRaw, string volDisplay)
        {
            _bookVolRaw = volRaw;
            _bookVolDisplay = volDisplay;
            _bookIsNullGroup = volRaw == null;
            txtSearch.Text = "";
            ShowListView();
            LoadGrid();
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

            var searchRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.TopDown };
            btnBackToBooks = new Button { Text = "← Books", Width = 100 };
            btnBackToBooks.Click += (s, e) => ShowBooksGallery();
            lblListHeader = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 4)
            };
            var searchSub = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
            var lblSearch = new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) };
            txtSearch = new TextBox { Width = 320 };
            txtSearch.TextChanged += (s, e) => LoadGrid();
            searchSub.Controls.Add(lblSearch, 0, 0);
            searchSub.Controls.Add(txtSearch, 1, 0);
            searchRow.Controls.Add(btnBackToBooks);
            searchRow.Controls.Add(lblListHeader);
            searchRow.Controls.Add(searchSub);
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

            var tabChild = NewTab("Child");
            var gChild = FieldGrid(tabChild);
            txtFirst = AddField(gChild, "First Name");
            txtMiddle = AddField(gChild, "Middle Name");
            txtLast = AddField(gChild, "Last Name");
            cboSex = AddCombo(gChild, "Sex", new[] { "Male", "Female" });
            dtpDob = AddDate(gChild, "Date of Birth");
            txtTob = AddField(gChild, "Time of Birth");
            txtPlace = AddField(gChild, "Place of Birth");
            txtTypeOfBirth = AddField(gChild, "Type of Birth");
            txtWeight = AddField(gChild, "Weight (grams)");

            var tabMother = NewTab("Mother");
            var gMother = FieldGrid(tabMother);
            txtMFirst = AddField(gMother, "First Name");
            txtMMiddle = AddField(gMother, "Middle Name");
            txtMLast = AddField(gMother, "Last Name");
            txtMOcc = AddField(gMother, "Occupation");
            txtMRel = AddField(gMother, "Religion");
            txtMCit = AddField(gMother, "Citizenship");

            var tabFather = NewTab("Father");
            var gFather = FieldGrid(tabFather);
            txtFFirst = AddField(gFather, "First Name");
            txtFMiddle = AddField(gFather, "Middle Name");
            txtFLast = AddField(gFather, "Last Name");
            txtFOcc = AddField(gFather, "Occupation");
            txtFRel = AddField(gFather, "Religion");
            txtFCit = AddField(gFather, "Citizenship");

            var tabRemarks = NewTab("Remarks");
            var gRemarks = FieldGrid(tabRemarks);
            txtRemarks = AddField(gRemarks, "Remarks", multiline: true);

            tabControl.TabPages.Add(tabReg);
            tabControl.TabPages.Add(tabChild);
            tabControl.TabPages.Add(tabMother);
            tabControl.TabPages.Add(tabFather);
            tabControl.TabPages.Add(tabRemarks);
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

        private void ShowBooksGallery()
        {
            cardList.Visible = false;
            cardEntry.Visible = false;
            cardBooks.Visible = true;
        }

        private void ShowListView()
        {
            cardBooks.Visible = false;
            cardEntry.Visible = false;
            cardList.Visible = true;
            lblListHeader.Text = "Book " + (_bookVolDisplay ?? "(no volume recorded)");
        }

        private void ShowEntryView()
        {
            cardBooks.Visible = false;
            cardList.Visible = false;
            cardEntry.Visible = true;
        }

        private void OpenNew()
        {
            ClearForm();
            // pre-fill with the book this record is being added under, unless the
            // opened group is the "no volume recorded" bucket — nothing to prefill there.
            if (!_bookIsNullGroup && !string.IsNullOrEmpty(_bookVolRaw)) txtBookVol.Text = _bookVolRaw;
            SetMode(view: false);
            lblEntryTitle.Text = "New old birth record";
            lblEntrySub.Text = "Not yet saved";
            ShowEntryView();
        }

        private void OpenSelected(bool view)
        {
            long? id = SelectedId();
            if (id == null) return;
            OpenSelected(id.Value, view);
        }

        private void OpenSelected(long id, bool view)
        {
            LoadRecord(id);
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
                "CONCAT_WS(' ', first_name, middle_name, last_name) AS 'Child Name', " +
                "sex AS Sex, date_of_birth AS 'Date of Birth', book_page AS 'Page', status AS Status " +
                "FROM births WHERE record_source = 'OCR-Backlog'";
            var ps = new List<MySqlParameter>();
            sql += _bookIsNullGroup ? " AND book_volume IS NULL" : " AND book_volume = @vol";
            if (!_bookIsNullGroup) ps.Add(new MySqlParameter("@vol", _bookVolRaw));
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR first_name LIKE @t OR middle_name LIKE @t OR last_name LIKE @t)";
                ps.Add(new MySqlParameter("@t", "%" + term + "%"));
            }
            sql += " ORDER BY book_page, id DESC";

            DataTable dt = ps.Count > 0 ? Db.Pull(sql, ps.ToArray()) : Db.Pull(sql);
            dgv.DataSource = dt;
            if (dgv.Columns.Contains("id")) dgv.Columns["id"].Visible = false;
            lblListHeader.Text = "Book " + (_bookVolDisplay ?? "(no volume recorded)") + "  ·  " + dt.Rows.Count + " record(s)";
            UpdateListButtons();
        }

        private void LoadRecord(long id)
        {
            DataTable dt = Db.Pull("SELECT * FROM births WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            _scanImage = r["scan_image"] == DBNull.Value ? null : (byte[])r["scan_image"];

            txtReg.Text = Str(r, "registry_no");
            txtBookVol.Text = Str(r, "book_volume");
            txtBookPage.Text = Str(r, "book_page");
            SetCombo(cboStatus, Str(r, "status"));
            txtFirst.Text = Str(r, "first_name");
            txtMiddle.Text = Str(r, "middle_name");
            txtLast.Text = Str(r, "last_name");
            SetCombo(cboSex, Str(r, "sex"));
            SetDate(dtpDob, r["date_of_birth"]);
            txtTob.Text = Str(r, "time_of_birth");
            txtPlace.Text = Str(r, "place_of_birth");
            txtTypeOfBirth.Text = Str(r, "type_of_birth");
            txtWeight.Text = Str(r, "weight_grams");
            txtMFirst.Text = Str(r, "mother_first_name");
            txtMMiddle.Text = Str(r, "mother_middle_name");
            txtMLast.Text = Str(r, "mother_last_name");
            txtMOcc.Text = Str(r, "mother_occupation");
            txtMRel.Text = Str(r, "mother_religion");
            txtMCit.Text = Str(r, "mother_citizenship");
            txtFFirst.Text = Str(r, "father_first_name");
            txtFMiddle.Text = Str(r, "father_middle_name");
            txtFLast.Text = Str(r, "father_last_name");
            txtFOcc.Text = Str(r, "father_occupation");
            txtFRel.Text = Str(r, "father_religion");
            txtFCit.Text = Str(r, "father_citizenship");
            txtRemarks.Text = Str(r, "remarks");

            // Guarded: migration 70 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = (txtFirst.Text + " " + txtLast.Text).Trim();
            if (lblEntryTitle.Text.Length == 0) lblEntryTitle.Text = "#" + id;
            lblEntrySub.Text = "Registry No. " + (txtReg.Text.Length > 0 ? txtReg.Text : "(none)") +
                                "  ·  Status: " + (cboStatus.SelectedItem?.ToString() ?? "—");
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            foreach (Control c in new Control[] {
                txtReg, txtBookVol, txtBookPage, txtFirst, txtMiddle, txtLast, txtTob, txtPlace,
                txtTypeOfBirth, txtWeight, txtMFirst, txtMMiddle, txtMLast, txtMOcc, txtMRel, txtMCit,
                txtFFirst, txtFMiddle, txtFLast, txtFOcc, txtFRel, txtFCit, txtRemarks })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboSex.SelectedIndex = -1;
            dtpDob.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the child's first and last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),
                new MySqlParameter("@fn", txtFirst.Text.Trim()),
                new MySqlParameter("@mn", NullIfEmpty(txtMiddle.Text)),
                new MySqlParameter("@ln", txtLast.Text.Trim()),
                new MySqlParameter("@sex", (object)cboSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@dob", dtpDob.Checked ? (object)dtpDob.Value.Date : DBNull.Value),
                new MySqlParameter("@tob", NullIfEmpty(txtTob.Text)),
                new MySqlParameter("@place", NullIfEmpty(txtPlace.Text)),
                new MySqlParameter("@btype", NullIfEmpty(txtTypeOfBirth.Text)),
                new MySqlParameter("@weight", int.TryParse(txtWeight.Text, out int w) ? (object)w : DBNull.Value),
                new MySqlParameter("@mf", NullIfEmpty(txtMFirst.Text)),
                new MySqlParameter("@mm", NullIfEmpty(txtMMiddle.Text)),
                new MySqlParameter("@ml", NullIfEmpty(txtMLast.Text)),
                new MySqlParameter("@mocc", NullIfEmpty(txtMOcc.Text)),
                new MySqlParameter("@mrel", NullIfEmpty(txtMRel.Text)),
                new MySqlParameter("@mcit", NullIfEmpty(txtMCit.Text)),
                new MySqlParameter("@ff", NullIfEmpty(txtFFirst.Text)),
                new MySqlParameter("@fm", NullIfEmpty(txtFMiddle.Text)),
                new MySqlParameter("@fl", NullIfEmpty(txtFLast.Text)),
                new MySqlParameter("@focc", NullIfEmpty(txtFOcc.Text)),
                new MySqlParameter("@frel", NullIfEmpty(txtFRel.Text)),
                new MySqlParameter("@fcit", NullIfEmpty(txtFCit.Text)),
                new MySqlParameter("@remarks", NullIfEmpty(txtRemarks.Text)),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@img", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: this screen is hand-transcription with no scan behind it —
                // encoding_method states that plainly, so it is never mistaken for an
                // OCR-read record on a later audit.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO births (registry_no, book_volume, book_page, status, first_name, middle_name, " +
                    "last_name, sex, date_of_birth, time_of_birth, place_of_birth, type_of_birth, weight_grams, " +
                    "mother_first_name, mother_middle_name, mother_last_name, mother_occupation, mother_religion, mother_citizenship, " +
                    "father_first_name, father_middle_name, father_last_name, father_occupation, father_religion, father_citizenship, " +
                    "remarks, scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, @btype, @weight, " +
                    "@mf, @mm, @ml, @mocc, @mrel, @mcit, @ff, @fm, @fl, @focc, @frel, @fcit, @remarks, @img, 'OCR-Backlog', " +
                    "@digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "births", id, "Old birth record added by hand (OCR-Backlog)");
                MessageBox.Show("Old birth record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Step 11 — result after saving: back to Birth Records (the books gallery),
                // reloaded so the record's book (new or existing) shows its updated count.
                LoadBooks();
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE births SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "first_name=@fn, middle_name=@mn, last_name=@ln, sex=@sex, date_of_birth=@dob, time_of_birth=@tob, " +
                    "place_of_birth=@place, type_of_birth=@btype, weight_grams=@weight, " +
                    "mother_first_name=@mf, mother_middle_name=@mm, mother_last_name=@ml, mother_occupation=@mocc, " +
                    "mother_religion=@mrel, mother_citizenship=@mcit, " +
                    "father_first_name=@ff, father_middle_name=@fm, father_last_name=@fl, father_occupation=@focc, " +
                    "father_religion=@frel, father_citizenship=@fcit, remarks=@remarks " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "births", _editingId.Value, "Old birth record updated (OCR-Backlog)");
                MessageBox.Show("Old birth record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Step 11 — same result-after-saving path: the edit may have moved the
                // record to a different book, so land back on the gallery, not the old list.
                LoadBooks();
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
            if (MessageBox.Show("Delete this old birth record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            Db.Push("DELETE FROM births WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            Audit.Write(Audit.Delete, "births", id, "Old birth record deleted (OCR-Backlog)");
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
            SoftcopyViewer.Show(_scanImage, "Old Birth Record — " + txtFirst.Text + " " + txtLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) => r[col] == DBNull.Value ? "" : r[col].ToString();

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
    }
}
