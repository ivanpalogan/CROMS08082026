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
    /// Prints Form 2A - CERTIFICATION (Death Available) for an already-registered death.
    /// Same editable-before-print rule and search/grid layout as <see cref="Form3ACertForm"/>
    /// (marriage, Form 3A) and <see cref="Form3BCertForm"/> (birth, Form 1A) - a wrong reading
    /// on the saved record can be corrected for THIS printout without touching the row itself.
    /// <para/>
    /// UI layout lives in Form3CCertForm.Designer.cs; this file holds the data/logic.
    /// </summary>
    public partial class Form3CCertForm : Form
    {
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "office_contact_line", "Office contact line" },
            { "date_issued", "Date issued" },
            { "registry_page", "Register page no." },
            { "registry_book", "Register book no." },
            { "deceased_name", "Name of deceased" },
            { "sex", "Sex" },
            { "civil_status", "Civil status" },
            { "date_of_death", "Date of death" },
            { "place_of_death", "Place of death" },
            { "cause_of_death", "Cause of death" },
            { "citizenship", "Citizenship" },
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

        private int? _deathId;
        private DataTable _wide;

        public Form3CCertForm()
        {
            InitializeComponent();
        }

        /// <summary>Opens directly on one death record - skips the search step.</summary>
        public Form3CCertForm(int deathId) : this()
        {
            _deathId = deathId;
            LoadRecord(deathId);
        }

        private void RunSearch()
        {
            try
            {
                string like = "%" + (_txtSearch.Text ?? "").Trim() + "%";
                DataTable dt = Db.Pull(
                    "SELECT record_id, registry_no, deceased_full_name, date_of_death " +
                    "FROM v_death_certificate " +
                    "WHERE deceased_full_name LIKE @l OR registry_no LIKE @l " +
                    "ORDER BY date_of_death DESC LIMIT 200",
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

        private void LoadRecord(int deathId)
        {
            _deathId = deathId;
            _wide = Form3CCert.BuildTable(deathId);
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
            if (_deathId == null || _wide == null)
            {
                MessageBox.Show(this, "Load a death record first.", "Nothing to print",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DataTable t = Rebuild();
            Form3CCert.Show(t, this);
            if (audit)
            {
                DataRow r = t.Rows[0];
                Audit.Write("Create", "deaths", _deathId.Value,
                    "Printed Form 2A Death Facts Certification" +
                    (string.IsNullOrWhiteSpace(r["or_number"] as string) ? "" : ", OR " + r["or_number"]));
                _lblStatus.Text = "Printed - logged to the audit trail.";
            }
        }
    }
}
