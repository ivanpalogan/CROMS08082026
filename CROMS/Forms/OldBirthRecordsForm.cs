using System;
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
    /// is a full Add / Edit / View / Delete workbench over the backlog alone, so an old
    /// scanned record never clutters — and never needs — the live registration form.
    /// </summary>
    public class OldBirthRecordsForm : Form, IRefreshable
    {
        private DataGridView dgv;
        private TextBox txtSearch;
        private TextBox txtReg, txtBookVol, txtBookPage;
        private ComboBox cboStatus, cboSex;
        private TextBox txtFirst, txtMiddle, txtLast;
        private DateTimePicker dtpDob;
        private TextBox txtTob, txtPlace, txtTypeOfBirth, txtWeight;
        private TextBox txtMFirst, txtMMiddle, txtMLast, txtMOcc, txtMRel, txtMCit;
        private TextBox txtFFirst, txtFMiddle, txtFLast, txtFOcc, txtFRel, txtFCit;
        private TextBox txtRemarks;
        private Button btnNew, btnSave, btnUpdate, btnDelete, btnSoftcopy;
        private Label lblSelected;

        private long? _editingId;
        private byte[] _scanImage;

        public OldBirthRecordsForm()
        {
            Text = "Old Birth Records (OCR)";
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
                Text = "Old Birth Records (OCR)",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Backlog records digitized from old registry books — committed straight from " +
                       "Intelligent Document Processing. Not shown on the live Birth Registration screen.",
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

            SectionHeader(form, ref row, "Child");
            txtFirst = FieldRow(form, ref row, "First Name");
            txtMiddle = FieldRow(form, ref row, "Middle Name");
            txtLast = FieldRow(form, ref row, "Last Name");
            cboSex = ComboRow(form, ref row, "Sex", new[] { "Male", "Female" });
            dtpDob = DateRow(form, ref row, "Date of Birth");
            txtTob = FieldRow(form, ref row, "Time of Birth");
            txtPlace = FieldRow(form, ref row, "Place of Birth", span: 3);
            txtTypeOfBirth = FieldRow(form, ref row, "Type of Birth");
            txtWeight = FieldRow(form, ref row, "Weight (grams)");

            SectionHeader(form, ref row, "Mother");
            txtMFirst = FieldRow(form, ref row, "First Name");
            txtMMiddle = FieldRow(form, ref row, "Middle Name");
            txtMLast = FieldRow(form, ref row, "Last Name");
            txtMOcc = FieldRow(form, ref row, "Occupation");
            txtMRel = FieldRow(form, ref row, "Religion");
            txtMCit = FieldRow(form, ref row, "Citizenship");

            SectionHeader(form, ref row, "Father");
            txtFFirst = FieldRow(form, ref row, "First Name");
            txtFMiddle = FieldRow(form, ref row, "Middle Name");
            txtFLast = FieldRow(form, ref row, "Last Name");
            txtFOcc = FieldRow(form, ref row, "Occupation");
            txtFRel = FieldRow(form, ref row, "Religion");
            txtFCit = FieldRow(form, ref row, "Citizenship");

            SectionHeader(form, ref row, "Remarks");
            txtRemarks = FieldRow(form, ref row, "Remarks", span: 3, multiline: true);
        }

        /// <summary>Grows the panel to hold row index <paramref name="row"/> (adding an
        /// AutoSize RowStyle for it), WITHOUT moving <paramref name="row"/> itself — the
        /// caller advances the counter once it is done placing controls at this index.</summary>
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
                "SELECT id, registry_no AS 'Registry No.', " +
                "CONCAT_WS(' ', first_name, middle_name, last_name) AS 'Child Name', " +
                "sex AS Sex, date_of_birth AS 'Date of Birth', book_volume AS 'Book/Vol', status AS Status " +
                "FROM births WHERE record_source = 'OCR-Backlog'";
            MySqlParameter[] ps;
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR first_name LIKE @t OR middle_name LIKE @t OR last_name LIKE @t)";
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

            lblSelected.Text = "Editing: " + (txtReg.Text.Length > 0 ? txtReg.Text : "#" + id) +
                                " — " + txtFirst.Text + " " + txtLast.Text;
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
            lblSelected.Text = "New old birth record";
        }

        private void Save(bool insert)
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the child's first and last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!insert && _editingId == null)
            {
                MessageBox.Show("Select a record in the list first, or use Save to add a new one.",
                    "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ps = new System.Collections.Generic.List<MySqlParameter>
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

            if (insert)
            {
                ps.Add(new MySqlParameter("@img", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                long id = Db.Insert(
                    "INSERT INTO births (registry_no, book_volume, book_page, status, first_name, middle_name, " +
                    "last_name, sex, date_of_birth, time_of_birth, place_of_birth, type_of_birth, weight_grams, " +
                    "mother_first_name, mother_middle_name, mother_last_name, mother_occupation, mother_religion, mother_citizenship, " +
                    "father_first_name, father_middle_name, father_last_name, father_occupation, father_religion, father_citizenship, " +
                    "remarks, scan_image, record_source) " +
                    "VALUES (@reg, @book, @bookpage, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, @btype, @weight, " +
                    "@mf, @mm, @ml, @mocc, @mrel, @mcit, @ff, @fm, @fl, @focc, @frel, @fcit, @remarks, @img, 'OCR-Backlog')",
                    ps.ToArray());
                Audit.Write(Audit.Create, "births", id, "Old birth record added by hand (OCR-Backlog)");
                MessageBox.Show("Old birth record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
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
            if (MessageBox.Show("Delete this old birth record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            Db.Push("DELETE FROM births WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", _editingId.Value));
            Audit.Write(Audit.Delete, "births", _editingId.Value, "Old birth record deleted (OCR-Backlog)");
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
    }
}
