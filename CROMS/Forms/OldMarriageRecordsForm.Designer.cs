using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class OldMarriageRecordsForm
    {
        // ---- books gallery (top level) -------------------------------------------
        // Hierarchy: Marriage Record (this form) -> Registry Books -> Selected Book ->
        // Individual Marriage Records -> Record Details — the same shape OldBirthRecordsForm
        // uses, via the shared RegistryBookGallery control.
        private CardPanel cardBooks;
        private RegistryBookGallery _gallery;


        // ---- list view ----------------------------------------------------------
        private CardPanel cardList;
        private DataGridView dgv;
        private TextBox txtSearch;
        private Label lblListHeader;
        private Button btnBackToBooks, btnNewFromList, btnViewFromList, btnEditFromList, btnDeleteFromList, btnRefresh;

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

        private void InitializeComponent()
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

            BuildBooksCard(body);
            BuildListCard(body);
            BuildEntryCard(body);
        }
        private void BuildBooksCard(Panel host)
        {
            cardBooks = new CardPanel { Dock = DockStyle.Fill, Radius = 12 };
            host.Controls.Add(cardBooks);

            var pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            cardBooks.Controls.Add(pad);

            _gallery = new RegistryBookGallery("marriages", "Marriage");
            _gallery.BookOpened += new System.Action<RegistryBookGallery.BookInfo>(this._gallery_BookOpened);
            _gallery.DigitizeRequested += new System.Action(this.StartDigitizeWizard);
            pad.Controls.Add(_gallery);
        }
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
            btnBackToBooks.Click += new System.EventHandler(this.btnBackToBooks_Click);
            lblListHeader = new Label
            {
                Text = "", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = UiTheme.Ink,
                AutoSize = true, Margin = new Padding(0, 6, 0, 4)
            };
            var searchSub = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
            var lblSearch = new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) };
            txtSearch = new TextBox { Width = 320 };
            txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
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
            btnNewFromList.Click += new System.EventHandler(this.btnNewFromList_Click);
            btnViewFromList.Click += new System.EventHandler(this.btnViewFromList_Click);
            btnEditFromList.Click += new System.EventHandler(this.btnEditFromList_Click);
            btnDeleteFromList.Click += new System.EventHandler(this.btnDeleteFromList_Click);
            btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
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
            dgv.SelectionChanged += new System.EventHandler(this.dgv_SelectionChanged);
            dgv.CellDoubleClick += new DataGridViewCellEventHandler(this.dgv_CellDoubleClick);
            layout.Controls.Add(dgv, 0, 1);
        }
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
            btnBack.Click += new System.EventHandler(this.btnBack_Click);
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
            btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            btnSave.Click += new System.EventHandler(this.btnSave_Click);
            btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            btnDeleteEntry.Click += new System.EventHandler(this.btnDeleteEntry_Click);
            btnSoftcopy.Click += new System.EventHandler(this.btnSoftcopy_Click);
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
    }
}
