using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Record Search — one search box across births, marriages and deaths. Matches by
    /// name (LIKE) and, when "fuzzy" is on, also by MySQL SOUNDEX so spelling variants
    /// match (e.g. "Dela Cruz" finds "dela cruz" / "de la Cruz"). Results UNION into one
    /// grid; double-clicking a row jumps to that record's module. Read-only. UI is declared
    /// in RecordSearchForm.Designer.cs so every control is visible/editable on the design
    /// canvas; this file holds only the data access + event handlers.
    /// </summary>
    public partial class RecordSearchForm : Form, IRefreshable
    {
        // Suppresses Search() while the designer-wired handlers fire during InitializeComponent
        // (setting cboType default / chkFuzzy checked would otherwise query before we're ready).
        private bool _ready;

        public RecordSearchForm()
        {
            InitializeComponent();
            cboType.SelectedIndex = 0;
            _ready = true;
            Search();
        }

        public void RefreshData() => Search();

        // ------------------------------------------------------------ event handlers
        private void txtQuery_TextChanged(object sender, EventArgs e) => Search();
        private void cboType_SelectedIndexChanged(object sender, EventArgs e) => Search();
        private void chkFuzzy_CheckedChanged(object sender, EventArgs e) => Search();

        // ---------------------------------------------------------------- data
        private void Search()
        {
            if (!_ready) return;
            string term = txtQuery.Text.Trim();
            bool fuzzy = chkFuzzy.Checked && term.Length > 0;
            string type = cboType.SelectedItem?.ToString() ?? "All Records";

            var parts = new List<string>();
            if (type == "All Records" || type == "Birth") parts.Add(BirthQuery(term, fuzzy));
            if (type == "All Records" || type == "Marriage") parts.Add(MarriageQuery(term, fuzzy));
            if (type == "All Records" || type == "Death") parts.Add(DeathQuery(term, fuzzy));

            string sql = string.Join(" UNION ALL ", parts) + " ORDER BY Name LIMIT 300";

            var ps = new List<MySqlParameter>();
            if (term.Length > 0)
            {
                ps.Add(new MySqlParameter("@like", "%" + term + "%"));
                ps.Add(new MySqlParameter("@q", term));
            }

            try
            {
                DataTable dt = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
                grid.DataSource = dt;
                if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
                lblCount.Text = dt.Rows.Count + " record(s) found" +
                    (term.Length == 0 ? "  (showing all — type a name to search)" :
                     fuzzy ? "  ·  fuzzy match on" : "  ·  exact match on");
            }
            catch (Exception ex)
            {
                lblCount.Text = "Search failed: " + ex.Message;
            }
        }

        // Each sub-query returns the same 6 columns so they can be UNIONed.
        private static string BirthQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "(first_name LIKE @like OR middle_name LIKE @like OR last_name LIKE @like OR " +
                "CONCAT(first_name,' ',last_name) LIKE @like)",
                "SOUNDEX(last_name) = SOUNDEX(@q) OR SOUNDEX(first_name) = SOUNDEX(@q)");
            return "SELECT 'Birth' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Name, " +
                   "date_of_birth AS 'Event Date', status AS Status FROM births" + where;
        }

        private static string DeathQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "full_name LIKE @like",
                "SOUNDEX(full_name) = SOUNDEX(@q)");
            return "SELECT 'Death' AS Type, id, registry_no AS 'Registry No', " +
                   "full_name AS Name, date_of_death AS 'Event Date', status AS Status FROM deaths" + where;
        }

        private static string MarriageQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "(husband_first_name LIKE @like OR husband_last_name LIKE @like OR " +
                "wife_first_name LIKE @like OR wife_last_name LIKE @like)",
                "SOUNDEX(husband_last_name) = SOUNDEX(@q) OR SOUNDEX(wife_last_name) = SOUNDEX(@q)");
            return "SELECT 'Marriage' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                   "wife_last_name, ', ', wife_first_name)) AS Name, " +
                   "date_of_marriage AS 'Event Date', status AS Status FROM marriages" + where;
        }

        /// <summary>Builds the WHERE clause: nothing when the term is blank (show all),
        /// LIKE match otherwise, plus the SOUNDEX clause when fuzzy is on.</summary>
        private static string Where(string term, bool fuzzy, string likeClause, string soundexClause)
        {
            if (term.Length == 0) return "";
            return " WHERE (" + likeClause + (fuzzy ? " OR " + soundexClause : "") + ")";
        }

        // --------------------------------------------------------------- jump
        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            string type = row.Cells["Type"].Value?.ToString();
            string name = row.Cells["Name"].Value?.ToString();
            string reg = row.Cells["Registry No"].Value == null ||
                         row.Cells["Registry No"].Value == DBNull.Value
                         ? "(unnumbered)" : row.Cells["Registry No"].Value.ToString();

            string key;
            switch (type)
            {
                case "Birth": key = "birth"; break;
                case "Marriage": key = "marriage"; break;
                case "Death": key = "death"; break;
                default: return;
            }

            MainForm shell = Shell();
            if (shell == null) return;
            shell.GoToModule(key);
            MessageBox.Show(
                "Opened " + type + " Registration.\nFind this record in the list:\n\n" +
                reg + "  —  " + name,
                "Go to record", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
