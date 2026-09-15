using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Registry Books - the digital shelf list of the office's paper registry books.
    /// Birth/Marriage/Death Registration each let staff type which book (volume) and
    /// page a record was written into (book_volume / book_page columns); this screen
    /// is where that shows up as a shelf: one row per volume/type with how many
    /// records and how many distinct pages are on file, and a second grid listing
    /// the individual records in whichever book is selected. Read-only - nothing
    /// here writes to births/marriages/deaths; book/page is entered on the
    /// registration forms themselves.
    /// UI is declared in RegistryBooksForm.Designer.cs; this file holds only the
    /// data access + event handlers.
    /// </summary>
    public partial class RegistryBooksForm : Form, IRefreshable
    {
        public RegistryBooksForm()
        {
            InitializeComponent();
            LoadBooks();
        }

        public void RefreshData() => LoadBooks();

        // ---------------------------------------------------------------- books shelf
        private void LoadBooks()
        {
            const string sql =
                "SELECT 'Birth' AS Type, book_volume AS VolRaw, " +
                "COALESCE(book_volume,'(no volume recorded)') AS Volume, " +
                "COUNT(*) AS Records, " +
                "COUNT(DISTINCT CASE WHEN book_page IS NOT NULL AND book_page<>'' THEN book_page END) AS Pages " +
                "FROM births GROUP BY book_volume " +
                "UNION ALL " +
                "SELECT 'Marriage', book_volume, COALESCE(book_volume,'(no volume recorded)'), COUNT(*), " +
                "COUNT(DISTINCT CASE WHEN book_page IS NOT NULL AND book_page<>'' THEN book_page END) " +
                "FROM marriages GROUP BY book_volume " +
                "UNION ALL " +
                "SELECT 'Death', book_volume, COALESCE(book_volume,'(no volume recorded)'), COUNT(*), " +
                "COUNT(DISTINCT CASE WHEN book_page IS NOT NULL AND book_page<>'' THEN book_page END) " +
                "FROM deaths GROUP BY book_volume " +
                "ORDER BY Volume DESC, Type";
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
            dgvRecords.DataSource = null;
            lblRecordsHeader.Text = "Click a book above to see its records";
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadBooks();

        // ------------------------------------------------------------ book -> records
        private void dgvBooks_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvBooks.Rows[e.RowIndex];
            string type = row.Cells["Type"].Value?.ToString();
            object volRawObj = row.Cells["VolRaw"].Value;
            string volRaw = volRawObj == null || volRawObj == DBNull.Value ? null : volRawObj.ToString();
            string volDisplay = row.Cells["Volume"].Value?.ToString();
            LoadRecords(type, volRaw, volDisplay);
        }

        /// <summary>Type is always one of the three literals below (never user text),
        /// so it is safe to use as the table name.</summary>
        private void LoadRecords(string type, string volRaw, string volDisplay)
        {
            string table, nameExpr, dateCol;
            switch (type)
            {
                case "Birth":
                    table = "births";
                    nameExpr = "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,'')))";
                    dateCol = "date_of_birth";
                    break;
                case "Marriage":
                    table = "marriages";
                    nameExpr = "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                               "wife_last_name, ', ', wife_first_name))";
                    dateCol = "date_of_marriage";
                    break;
                case "Death":
                    table = "deaths";
                    nameExpr = "full_name";
                    dateCol = "date_of_death";
                    break;
                default:
                    return;
            }

            string where = volRaw == null ? " WHERE book_volume IS NULL" : " WHERE book_volume = @vol";
            string sql = "SELECT '" + type + "' AS Type, id, registry_no AS 'Registry No', " +
                         "book_page AS Page, " + nameExpr + " AS Name, " +
                         dateCol + " AS 'Event Date', status AS Status FROM " + table + where +
                         " ORDER BY book_page, registry_no";
            try
            {
                var ps = new List<MySqlParameter>();
                if (volRaw != null) ps.Add(new MySqlParameter("@vol", volRaw));
                DataTable dt = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
                dgvRecords.DataSource = dt;
                if (dgvRecords.Columns.Contains("id")) dgvRecords.Columns["id"].Visible = false;
                lblRecordsHeader.Text = type + " — Book " + volDisplay + "  (" + dt.Rows.Count + " record(s))";
            }
            catch (Exception ex)
            {
                lblRecordsHeader.Text = "Could not load records: " + ex.Message;
            }
        }

        // --------------------------------------------------------------- jump
        private void dgvRecords_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvRecords.Rows[e.RowIndex];
            string type = row.Cells["Type"].Value?.ToString();
            string name = row.Cells["Name"].Value?.ToString();
            object regObj = row.Cells["Registry No"].Value;
            string reg = regObj == null || regObj == DBNull.Value ? "(unnumbered)" : regObj.ToString();

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
