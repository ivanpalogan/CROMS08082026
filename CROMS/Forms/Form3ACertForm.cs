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
    /// Prints Form 3A - CERTIFICATION (Marriage Available) for an already-registered marriage.
    /// Search a record -> every value comes back editable (a wrong reading on the underlying
    /// row can be corrected for THIS printout without touching the saved record, the same rule
    /// the OCR review grid and the out-of-province-licence panel already follow) -> Preview or
    /// Print, which dispatches to Crystal (CROMS\Reports\FORM-3A.rpt) when both the runtime and
    /// the file are present, else the built-in direct-draw renderer.
    /// <para/>
    /// Code-built modal (no Designer file), same pattern as MarriageLicenseForm/MarriageEntryForm.
    /// </summary>
    public partial class Form3ACertForm : Form
    {
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "office_contact_line", "Office contact line" },
            { "date_issued", "Date issued" },
            { "registry_page", "Register page no." },
            { "registry_book", "Register book no." },
            { "husband_full_name", "Husband - Name" },
            { "wife_full_name", "Wife - Name" },
            { "husband_dob_age", "Husband - Date of birth / Age" },
            { "wife_dob_age", "Wife - Date of birth / Age" },
            { "husband_civil_status", "Husband - Civil status" },
            { "wife_civil_status", "Wife - Civil status" },
            { "husband_nationality", "Husband - Nationality" },
            { "wife_nationality", "Wife - Nationality" },
            { "husband_father", "Husband - Father" },
            { "wife_father", "Wife - Father" },
            { "husband_father_nationality", "Husband's father - Nationality" },
            { "wife_father_nationality", "Wife's father - Nationality" },
            { "husband_mother", "Husband - Mother" },
            { "wife_mother", "Wife - Mother" },
            { "husband_mother_nationality", "Husband's mother - Nationality" },
            { "wife_mother_nationality", "Wife's mother - Nationality" },
            { "mcr_registry_number", "MCR registry number" },
            { "date_of_registration", "Date of registration" },
            { "date_of_marriage", "Date of marriage" },
            { "place_of_marriage", "Place of marriage" },
            { "purpose", "Issued for (purpose)" },
            { "registrar_name", "Municipal Civil Registrar (name)" },
            { "verified_by_name", "Verified by (name)" },
            { "verified_by_title", "Verified by (title)" },
            { "amount_paid", "Amount paid" },
            { "or_number", "OR No." },
            { "date_paid", "Date paid" },
        };

        private int? _marriageId;
        private DataTable _wide;

        public Form3ACertForm()
        {
            InitializeComponent();

            RunSearch();
        }

        /// <summary>Opens directly on one marriage record (e.g. from the records grid) - skips
        /// the search step.</summary>
        public Form3ACertForm(int marriageId) : this()
        {
            _marriageId = marriageId;
            LoadRecord(marriageId);
        }

        private void RunSearch()
        {
            try
            {
                string like = "%" + (_txtSearch.Text ?? "").Trim() + "%";
                DataTable dt = Db.Pull(
                    "SELECT record_id, registry_no, husband_full_name, wife_full_name, date_of_marriage " +
                    "FROM v_marriage_certificate " +
                    "WHERE husband_full_name LIKE @l OR wife_full_name LIKE @l OR registry_no LIKE @l " +
                    "ORDER BY date_of_marriage DESC LIMIT 200",
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

        private void LoadRecord(int marriageId)
        {
            _marriageId = marriageId;
            _wide = Form3ACert.BuildTable(marriageId);
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

        /// <summary>Rebuilds the wide table's one row from whatever the operator edited in the grid.</summary>
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
            if (_marriageId == null || _wide == null)
            {
                MessageBox.Show(this, "Load a marriage record first.", "Nothing to print",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DataTable t = Rebuild();
            Form3ACert.Show(t, this);
            if (audit)
            {
                DataRow r = t.Rows[0];
                Audit.Write("Create", "marriages", _marriageId.Value,
                    "Printed Form 3A Marriage Facts Certification" +
                    (string.IsNullOrWhiteSpace(r["or_number"] as string) ? "" : ", OR " + r["or_number"]));
                _lblStatus.Text = "Printed - logged to the audit trail.";
            }
        }
    }
}
