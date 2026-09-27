using System;
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
    /// is a full Add / Edit / View / Delete workbench over the backlog alone.
    /// </summary>
    public class OldDeathRecordsForm : Form, IRefreshable
    {
        private DataGridView dgv;
        private TextBox txtSearch;
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
        private DateTimePicker dtpRegByDate;
        private Button btnNew, btnSave, btnUpdate, btnDelete, btnSoftcopy;
        private Label lblSelected;

        private long? _editingId;
        private byte[] _scanImage;

        public OldDeathRecordsForm()
        {
            Text = "Old Death Records (OCR)";
            BuildUi();
            LoadGrid();
            ClearForm();
        }

        public void RefreshData()
        {
            LoadGrid();
        }

        // ---- UI ---------------------------------------------------------------

        private void BuildUi()
        {
            Width = 1400;
            Height = 900;
            BackColor = Color.White;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(20, 10, 20, 6) };
            var title = new Label
            {
                Text = "Old Death Records (OCR)",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Backlog records digitized from old registry books — committed straight from " +
                       "Intelligent Document Processing. Not shown on the live Death Registration screen.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(91, 100, 114),
                AutoSize = true,
                Location = new Point(0, 30)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.Controls.Add(body, 0, 1);

            // ---- Left: list -----------------------------------------------------
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Padding = new Padding(20, 0, 10, 20)
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Controls.Add(left, 0, 0);

            var searchRow = new Panel { Dock = DockStyle.Fill, Height = 32 };
            txtSearch = new TextBox { Dock = DockStyle.Fill };
            txtSearch.TextChanged += (s, e) => LoadGrid();
            var lblSearch = new Label { Text = "Search:", Dock = DockStyle.Left, Width = 60, TextAlign = ContentAlignment.MiddleLeft };
            searchRow.Controls.Add(txtSearch);
            searchRow.Controls.Add(lblSearch);
            left.Controls.Add(searchRow, 0, 0);

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
            dgv.CellClick += Dgv_CellClick;
            left.Controls.Add(dgv, 0, 1);

            // ---- Right: edit panel -----------------------------------------------
            var right = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10, 0, 20, 20) };
            body.Controls.Add(right, 1, 0);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            btnNew = new Button { Text = "New", Width = 90 };
            btnSave = new Button { Text = "Save", Width = 90 };
            btnUpdate = new Button { Text = "Update", Width = 90 };
            btnDelete = new Button { Text = "Delete", Width = 90 };
            btnSoftcopy = new Button { Text = "View Softcopy", Width = 130 };
            btnNew.Click += (s, e) => ClearForm();
            btnSave.Click += (s, e) => Save(insert: true);
            btnUpdate.Click += (s, e) => Save(insert: false);
            btnDelete.Click += (s, e) => DeleteSelected();
            btnSoftcopy.Click += (s, e) => ShowSoftcopy();
            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnSave);
            toolbar.Controls.Add(btnUpdate);
            toolbar.Controls.Add(btnDelete);
            toolbar.Controls.Add(btnSoftcopy);
            right.Controls.Add(toolbar);

            lblSelected = new Label { Dock = DockStyle.Top, Height = 24, Text = "New record", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            right.Controls.Add(lblSelected);

            var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            for (int i = 0; i < 4; i++) form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            right.Controls.Add(form);

            int row = 0;
            txtReg = FieldRow(form, ref row, "Registry No.");
            txtBookVol = FieldRow(form, ref row, "Book / Volume (Year)");
            txtBookPage = FieldRow(form, ref row, "Book Page");
            cboStatus = ComboRow(form, ref row, "Status", new[] { "Draft", "Registered" });

            SectionHeader(form, ref row, "Deceased");
            txtFirst = FieldRow(form, ref row, "First Name");
            txtMiddle = FieldRow(form, ref row, "Middle Name");
            txtLast = FieldRow(form, ref row, "Last Name");
            cboSex = ComboRow(form, ref row, "Sex", new[] { "Male", "Female" });
            txtCivil = FieldRow(form, ref row, "Civil Status");
            txtAge = FieldRow(form, ref row, "Age");
            txtCitizenship = FieldRow(form, ref row, "Citizenship");
            dtpDod = DateRow(form, ref row, "Date of Death");
            txtPlace = FieldRow(form, ref row, "Place of Death", span: 3);
            txtReligion = FieldRow(form, ref row, "Religion");

            SectionHeader(form, ref row, "Cause of Death & Disposal");
            txtImmediate = FieldRow(form, ref row, "Immediate Cause", span: 3);
            txtAntecedent = FieldRow(form, ref row, "Antecedent Cause", span: 3);
            txtUnderlying = FieldRow(form, ref row, "Underlying Cause", span: 3);
            txtDisposal = FieldRow(form, ref row, "Disposal Method");
            txtDisposalPlace = FieldRow(form, ref row, "Place of Disposal", span: 2);
            dtpDisposalDate = DateRow(form, ref row, "Date of Disposal");

            SectionHeader(form, ref row, "Informant");
            txtInfName = FieldRow(form, ref row, "Name");
            txtInfRel = FieldRow(form, ref row, "Relationship");
            txtInfAddr = FieldRow(form, ref row, "Address", span: 2);
            dtpInfDate = DateRow(form, ref row, "Date Signed");

            SectionHeader(form, ref row, "Certification");
            txtPrepName = FieldRow(form, ref row, "Prepared By");
            txtPrepTitle = FieldRow(form, ref row, "Prepared By Title");
            dtpPrepDate = DateRow(form, ref row, "Prepared By Date");
            txtRecvName = FieldRow(form, ref row, "Received By");
            txtRecvTitle = FieldRow(form, ref row, "Received By Title");
            dtpRecvDate = DateRow(form, ref row, "Received By Date");
            txtRegByName = FieldRow(form, ref row, "Registered By");
            txtRegByTitle = FieldRow(form, ref row, "Registered By Title");
            dtpRegByDate = DateRow(form, ref row, "Registered By Date");
        }

        private static void EnsureRow(TableLayoutPanel form, int row)
        {
            if (form.RowCount <= row) form.RowCount = row + 1;
            while (form.RowStyles.Count <= row) form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        private static void SectionHeader(TableLayoutPanel form, ref int row, string text)
        {
            EnsureRow(form, row);
            var lbl = new Label
            {
                Text = text.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 2)
            };
            form.Controls.Add(lbl, 0, row);
            form.SetColumnSpan(lbl, 4);
            row++;
        }

        private static TextBox FieldRow(TableLayoutPanel form, ref int row, string label, int span = 1, bool multiline = false)
        {
            EnsureRow(form, row);
            var cap = new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            form.Controls.Add(cap, 0, row);
            row++;

            EnsureRow(form, row);
            var box = new TextBox { Width = 220 * span + 8 * (span - 1) };
            if (multiline) { box.Multiline = true; box.Height = 60; }
            form.Controls.Add(box, 0, row);
            form.SetColumnSpan(box, Math.Min(span, 4));
            row++;
            return box;
        }

        private static ComboBox ComboRow(TableLayoutPanel form, ref int row, string label, string[] items)
        {
            EnsureRow(form, row);
            var cap = new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            form.Controls.Add(cap, 0, row);
            row++;

            EnsureRow(form, row);
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            box.Items.AddRange(items);
            form.Controls.Add(box, 0, row);
            row++;
            return box;
        }

        private static DateTimePicker DateRow(TableLayoutPanel form, ref int row, string label)
        {
            EnsureRow(form, row);
            var cap = new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            form.Controls.Add(cap, 0, row);
            row++;

            EnsureRow(form, row);
            var dtp = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 220 };
            form.Controls.Add(dtp, 0, row);
            row++;
            return dtp;
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
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long id = Convert.ToInt64(dgv.Rows[e.RowIndex].Cells["id"].Value);
            LoadRecord(id);
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

            lblSelected.Text = "Editing: " + (txtReg.Text.Length > 0 ? txtReg.Text : "#" + id) +
                                " — " + Str(r, "full_name");
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
            lblSelected.Text = "New old death record";
        }

        private void Save(bool insert)
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) && string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the deceased's first or last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!insert && _editingId == null)
            {
                MessageBox.Show("Select a record in the list first, or use Save to add a new one.",
                    "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fullName = string.Join(" ", new[] { txtFirst.Text, txtMiddle.Text, txtLast.Text }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var ps = new System.Collections.Generic.List<MySqlParameter>
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

            if (insert)
            {
                ps.Add(new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                long id = Db.Insert(
                    "INSERT INTO deaths (registry_no, book_volume, book_page, status, full_name, sex, " +
                    "civil_status, age, citizenship, date_of_death, place_of_death, religion_name, " +
                    "immediate_cause, antecedent_cause, underlying_cause, disposal_method, place_of_disposal, date_of_disposal, " +
                    "informant_name, informant_relationship, informant_address, informant_date, " +
                    "prepared_by, prepared_by_title, prepared_by_date, " +
                    "received_by, received_by_title, received_by_date, " +
                    "registered_by, registered_by_title, registered_by_date, " +
                    "scan_image, record_source) " +
                    "VALUES (@reg, @book, @bookpage, @status, @name, @sex, " +
                    "@civil, @age, @cit, @dod, @place, @religion, " +
                    "@imm, @ant, @und, @disp, @dplace, @ddate, " +
                    "@iname, @irel, @iaddr, @idate, " +
                    "@prep, @preptitle, @prepdate, " +
                    "@recv, @recvtitle, @recvdate, " +
                    "@regby, @regbytitle, @regbydate, " +
                    "@scan, 'OCR-Backlog')",
                    ps.ToArray());
                Audit.Write(Audit.Create, "deaths", id, "Old death record added by hand (OCR-Backlog)");
                MessageBox.Show("Old death record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
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
            }
        }

        private void DeleteSelected()
        {
            if (_editingId == null)
            {
                MessageBox.Show("Select a record in the list first.", "Nothing selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Delete this old death record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            Db.Push("DELETE FROM deaths WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", _editingId.Value));
            Audit.Write(Audit.Delete, "deaths", _editingId.Value, "Old death record deleted (OCR-Backlog)");
            ClearForm();
            LoadGrid();
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
