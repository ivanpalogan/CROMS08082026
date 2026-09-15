using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CROMS.Data;

namespace CROMS.Forms
{
    /// <summary>
    /// Prints Form 3B - CERTIFICATION (Birth Available) for an already-registered birth.
    /// Same editable-before-print rule and search/grid layout as <see cref="Form3ACertForm"/>
    /// (its marriage counterpart) - a wrong reading on the saved record can be corrected for
    /// THIS printout without touching the row itself.
    /// </summary>
    public class Form3BCertForm : Form
    {
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "office_contact_line", "Office contact line" },
            { "date_issued", "Date issued" },
            { "registry_page", "Register page no." },
            { "registry_book", "Register book no." },
            { "child_name", "Name of child" },
            { "sex", "Sex" },
            { "date_of_birth", "Date of birth" },
            { "place_of_birth", "Place of birth" },
            { "mother_name", "Name of mother" },
            { "mother_nationality", "Mother's nationality" },
            { "father_name", "Name of father" },
            { "father_nationality", "Father's nationality" },
            { "registry_number", "Registry number" },
            { "date_of_registration", "Date of registration" },
            { "purpose", "Issued for (purpose)" },
            { "registrar_name", "Municipal Civil Registrar (name)" },
            { "verified_by_name", "Verified by (name)" },
            { "verified_by_title", "Verified by (title)" },
            { "amount_paid", "Amount paid" },
            { "or_number", "OR No." },
            { "date_paid", "Date paid" },
        };

        private readonly TextBox _txtSearch = new TextBox { Left = 16, Top = 16, Width = 320 };
        private readonly Button _btnSearch = new Button { Left = 344, Top = 14, Width = 90, Text = "Search" };
        private readonly DataGridView _dgvResults = new DataGridView
        {
            Left = 16, Top = 48, Width = 720, Height = 150,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private readonly DataGridView _dgvFields = new DataGridView
        {
            Left = 16, Top = 210, Width = 720, Height = 360,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private readonly Button _btnPreview = new Button { Left = 16, Top = 580, Width = 160, Text = "Preview" };
        private readonly Button _btnPrint = new Button { Left = 184, Top = 580, Width = 160, Text = "Print" };
        private readonly Label _lblStatus = new Label { Left = 16, Top = 616, Width = 720, Height = 20, ForeColor = Color.DimGray };

        private int? _birthId;
        private DataTable _wide;

        public Form3BCertForm()
        {
            Text = "Print Certification (Form 3B - Birth Available)";
            Width = 780; Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            _dgvFields.Columns.Add("Field", "Field");
            _dgvFields.Columns.Add("Value", "Value");
            _dgvFields.Columns[0].ReadOnly = true;

            _btnSearch.Click += (s, e) => RunSearch();
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; RunSearch(); } };
            _dgvResults.CellDoubleClick += (s, e) => LoadSelected();
            _btnPreview.Click += (s, e) => PrintOrPreview(false);
            _btnPrint.Click += (s, e) => PrintOrPreview(true);

            Controls.Add(_txtSearch);
            Controls.Add(_btnSearch);
            Controls.Add(_dgvResults);
            Controls.Add(_dgvFields);
            Controls.Add(_btnPreview);
            Controls.Add(_btnPrint);
            Controls.Add(_lblStatus);

            RunSearch();
        }

        /// <summary>Opens directly on one birth record - skips the search step.</summary>
        public Form3BCertForm(int birthId) : this()
        {
            _birthId = birthId;
            LoadRecord(birthId);
        }

        private void RunSearch()
        {
            try
            {
                string like = "%" + (_txtSearch.Text ?? "").Trim() + "%";
                DataTable dt = Db.Pull(
                    "SELECT record_id, registry_no, child_full_name, date_of_birth " +
                    "FROM v_birth_certificate " +
                    "WHERE child_full_name LIKE @l OR registry_no LIKE @l " +
                    "ORDER BY date_of_birth DESC LIMIT 200",
                    new MySqlParameter("@l", like));
                _dgvResults.DataSource = dt;
            }
            catch (MySqlException ex) { _lblStatus.Text = "Search failed: " + ex.Message; }
        }

        private void LoadSelected()
        {
            if (_dgvResults.CurrentRow == null) return;
            var row = (DataRowView)_dgvResults.CurrentRow.DataBoundItem;
            LoadRecord(Convert.ToInt32(row["record_id"]));
        }

        private void LoadRecord(int birthId)
        {
            _birthId = birthId;
            _wide = Form3BCert.BuildTable(birthId);
            DataRow r = _wide.Rows[0];

            _dgvFields.Rows.Clear();
            foreach (DataColumn col in _wide.Columns)
            {
                string label = Labels.TryGetValue(col.ColumnName, out string lbl) ? lbl : col.ColumnName;
                _dgvFields.Rows.Add(label, r[col] as string ?? "");
                _dgvFields.Rows[_dgvFields.Rows.Count - 1].Tag = col.ColumnName;
            }
            _lblStatus.Text = "Loaded. Every value can be corrected below before printing.";
        }

        private DataTable Rebuild()
        {
            DataRow r = _wide.Rows[0];
            foreach (DataGridViewRow row in _dgvFields.Rows)
            {
                string col = row.Tag as string;
                if (col == null || !_wide.Columns.Contains(col)) continue;
                r[col] = Convert.ToString(row.Cells[1].Value) ?? "";
            }
            return _wide;
        }

        private void PrintOrPreview(bool audit)
        {
            if (_birthId == null || _wide == null)
            {
                MessageBox.Show(this, "Load a birth record first.", "Nothing to print",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DataTable t = Rebuild();
            Form3BCert.Show(t, this);
            if (audit)
            {
                DataRow r = t.Rows[0];
                Audit.Write("Create", "births", _birthId.Value,
                    "Printed Form 3B Birth Facts Certification" +
                    (string.IsNullOrWhiteSpace(r["or_number"] as string) ? "" : ", OR " + r["or_number"]));
                _lblStatus.Text = "Printed - logged to the audit trail.";
            }
        }
    }
}
