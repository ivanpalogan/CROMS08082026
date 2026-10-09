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
    /// records and how many distinct pages are on file. Clicking a book OPENS it -
    /// a second view (Back / book identity / search / records table) rather than a
    /// grid stacked below the shelf. Read-only - nothing here writes to
    /// births/marriages/deaths; book/page is entered on the registration forms
    /// themselves, and the digitization entry point (OCR module) is reached from
    /// the shelf gallery, never from inside an opened book.
    /// UI is declared in RegistryBooksForm.Designer.cs; this file holds only the
    /// data access + event handlers.
    /// </summary>
    public partial class RegistryBooksForm : Form, IRefreshable
    {
        private string _openType;
        private string _openVolRaw;
        private string _openVolDisplay;

        public RegistryBooksForm()
        {
            InitializeComponent();
            LoadBooks();
        }

        public void RefreshData()
        {
            if (pnlBook.Visible)
                LoadRecords(_openType, _openVolRaw, _openVolDisplay);
            else
                LoadBooks();
        }

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
                lblBooksHeader.Text = "Could not load books: " + CROMS.Data.ErrorLog.Reason(ex);
            }
            ShowGallery();
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
            OpenBook(type, volRaw, volDisplay);
        }

        private void OpenBook(string type, string volRaw, string volDisplay)
        {
            _openType = type;
            _openVolRaw = volRaw;
            _openVolDisplay = volDisplay;
            txtSearch.Text = "";
            lblBookHeader.Text = "Book " + volDisplay + "  —  " + type;
            LoadRecords(type, volRaw, volDisplay);
            ShowBook();
        }

        private void btnBack_Click(object sender, EventArgs e) => LoadBooks();

        private void ShowGallery()
        {
            pnlBook.Visible = false;
            pnlGallery.Visible = true;
        }

        private void ShowBook()
        {
            pnlGallery.Visible = false;
            pnlBook.Visible = true;
        }

        /// <summary>Type is always one of the three literals below (never user text),
        /// so it is safe to use as the table name.</summary>
        private void LoadRecords(string type, string volRaw, string volDisplay)
        {
            string table, nameExpr, dateCol, dateLabel, regDateExpr;
            switch (type)
            {
                case "Birth":
                    table = "births";
                    nameExpr = "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,'')))";
                    dateCol = "date_of_birth";
                    dateLabel = "Date of Birth";
                    regDateExpr = "date_registered";
                    break;
                case "Marriage":
                    table = "marriages";
                    nameExpr = "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                               "wife_last_name, ', ', wife_first_name))";
                    dateCol = "date_of_marriage";
                    dateLabel = "Date of Marriage";
                    regDateExpr = "date_registered";
                    break;
                case "Death":
                    table = "deaths";
                    nameExpr = "full_name";
                    dateCol = "date_of_death";
                    dateLabel = "Date of Death";
                    regDateExpr = "NULL"; // deaths carries no date_registered column
                    break;
                default:
                    return;
            }

            string where = volRaw == null ? " WHERE book_volume IS NULL" : " WHERE book_volume = @vol";
            string sql = "SELECT '" + type + "' AS Type, id, registry_no AS 'Registry No', " +
                         nameExpr + " AS Name, " +
                         dateCol + " AS '" + dateLabel + "', " +
                         regDateExpr + " AS 'Date Registered', " +
                         "book_page AS 'Page No.', status AS 'Status' FROM " + table + where +
                         " ORDER BY book_page, registry_no";
            try
            {
                var ps = new List<MySqlParameter>();
                if (volRaw != null) ps.Add(new MySqlParameter("@vol", volRaw));
                DataTable dt = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
                if (!dt.Columns.Contains("View")) dt.Columns.Add("View");
                dgvOpenedRecords.DataSource = dt;
                if (dgvOpenedRecords.Columns.Contains("id")) dgvOpenedRecords.Columns["id"].Visible = false;
                if (dgvOpenedRecords.Columns.Contains("Type")) dgvOpenedRecords.Columns["Type"].Visible = false;
                EnsureViewButton();
                lblBookMeta.Text = "Year: " + volDisplay + "   •   " + dt.Rows.Count + " record(s) on file";
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                lblBookMeta.Text = "Could not load records: " + CROMS.Data.ErrorLog.Reason(ex);
            }
        }

        private void EnsureViewButton()
        {
            foreach (DataGridViewColumn c in dgvOpenedRecords.Columns)
                if (c is DataGridViewButtonColumn) return; // already added once
            if (dgvOpenedRecords.Columns.Contains("View"))
                dgvOpenedRecords.Columns.Remove("View");
            var btnCol = new DataGridViewButtonColumn
            {
                Name = "View",
                HeaderText = "",
                Text = "View",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            dgvOpenedRecords.Columns.Add(btnCol);
        }

        // ------------------------------------------------------------------ search
        private void txtSearch_TextChanged(object sender, EventArgs e) => ApplySearchFilter();

        private void ApplySearchFilter()
        {
            if (!(dgvOpenedRecords.DataSource is DataTable dt)) return;

            string q = EscapeFilterValue((txtSearch?.Text ?? "").Trim());
            if (q.Length == 0)
            {
                dt.DefaultView.RowFilter = "";
                lblBookMeta.Text = "Year: " + _openVolDisplay + "   •   " + dt.Rows.Count + " record(s) on file";
                return;
            }

            var parts = new List<string>();
            foreach (string col in new[] { "Registry No", "Name", "Status" })
                if (dt.Columns.Contains(col))
                    parts.Add("[" + col + "] LIKE '%" + q + "%'");

            dt.DefaultView.RowFilter = parts.Count == 0 ? "" : string.Join(" OR ", parts);
            lblBookMeta.Text = "Year: " + _openVolDisplay + "   •   " +
                dt.DefaultView.Count + " of " + dt.Rows.Count + " record(s)";
        }

        /// <summary>Makes typed text safe inside a DataView RowFilter LIKE pattern. A quote
        /// has to be doubled, and the wildcard characters have to be wrapped in brackets so
        /// typing one searches for that literal character instead of silently widening the
        /// match.</summary>
        private static string EscapeFilterValue(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\'': sb.Append("''"); break;
                    case '[': sb.Append("[[]"); break;
                    case '%': sb.Append("[%]"); break;
                    case '*': sb.Append("[*]"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        // --------------------------------------------------------------- jump
        private void dgvOpenedRecords_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvOpenedRecords.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
                GoToRecord(e.RowIndex);
        }

        private void dgvOpenedRecords_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            GoToRecord(e.RowIndex);
        }

        private void GoToRecord(int rowIndex)
        {
            var row = dgvOpenedRecords.Rows[rowIndex];
            string name = row.Cells["Name"].Value?.ToString();
            object regObj = row.Cells["Registry No"].Value;
            string reg = regObj == null || regObj == DBNull.Value ? "(unnumbered)" : regObj.ToString();

            string key;
            switch (_openType)
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
                "Opened " + _openType + " Registration.\nFind this record in the list:\n\n" +
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
