using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// "Find Record" for Certificate Request — a small search dialog over ONE registry
    /// (Birth, Marriage or Death), chosen on the request form first. It replaces the old
    /// always-loaded, type-to-search dropdown, which pulled the whole register into a combo
    /// box. Here the list is only fetched for what the clerk types, capped at 200 rows.
    /// </summary>
    public partial class FindRecordDialog : Form
    {
        private const int MaxRows = 200;

        private readonly string _recordType;

        /// <summary>The chosen record's id (registry table row), or 0 when none was chosen.</summary>
        public int PickedId { get; private set; }

        /// <summary>A one-line description of the chosen record, for the request form to show.</summary>
        public string PickedName { get; private set; }

        public FindRecordDialog(string recordType, string startingText)
        {
            _recordType = recordType;
            InitializeComponent();
            Text = "Find Record — " + recordType;
            lblHeading.Text = "Find a " + recordType + " record";

            UiTheme.Polish(this);
            SetCue(txtSearch, recordType == "Marriage"
                ? "Type the husband's or wife's name, or a registry number"
                : "Type a name or a registry number");

            txtSearch.Text = startingText ?? "";   // runs the first search via TextChanged
            if (string.IsNullOrEmpty(txtSearch.Text)) Search();
            txtSearch.SelectionStart = txtSearch.TextLength;
        }

        private static void SetCue(TextBox box, string cue)
        {
            EventHandler set = (s, e) => SendMessage(box.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, cue);
            if (box.IsHandleCreated) set(box, EventArgs.Empty); else box.HandleCreated += set;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        // ------------------------------------------------------------ search
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Debounced: one query when typing pauses, not one per keystroke.
            tmrSearch.Stop();
            tmrSearch.Start();
        }

        private void tmrSearch_Tick(object sender, EventArgs e)
        {
            tmrSearch.Stop();
            Search();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // Down arrow moves into the list so the clerk never has to reach for the mouse.
            if (e.KeyCode == Keys.Down && grid.RowCount > 0)
            {
                grid.Focus();
                e.Handled = true;
            }
        }

        private void Search()
        {
            string term = txtSearch.Text.Trim();
            string sql;
            switch (_recordType)
            {
                case "Birth":
                    sql = "SELECT id, registry_no AS 'Registry No', " +
                          "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Name, " +
                          "date_of_birth AS 'Date of birth' FROM births " +
                          (term.Length == 0 ? "" :
                           "WHERE first_name LIKE @like OR middle_name LIKE @like OR last_name LIKE @like OR " +
                           "CONCAT(first_name,' ',last_name) LIKE @like OR registry_no LIKE @like ") +
                          "ORDER BY last_name, first_name LIMIT " + MaxRows;
                    break;
                case "Death":
                    sql = "SELECT id, registry_no AS 'Registry No', full_name AS Name, " +
                          "date_of_death AS 'Date of death' FROM deaths " +
                          (term.Length == 0 ? "" : "WHERE full_name LIKE @like OR registry_no LIKE @like ") +
                          "ORDER BY full_name LIMIT " + MaxRows;
                    break;
                case "Marriage":
                    sql = "SELECT id, registry_no AS 'Registry No', " +
                          "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                          "wife_last_name, ', ', wife_first_name)) AS Name, " +
                          "date_of_marriage AS 'Date of marriage' FROM marriages " +
                          (term.Length == 0 ? "" :
                           "WHERE husband_first_name LIKE @like OR husband_last_name LIKE @like OR " +
                           "wife_first_name LIKE @like OR wife_last_name LIKE @like OR registry_no LIKE @like ") +
                          "ORDER BY id DESC LIMIT " + MaxRows;
                    break;
                default:
                    return;
            }

            try
            {
                DataTable dt = term.Length == 0
                    ? Db.Pull(sql)
                    : Db.Pull(sql, new MySqlParameter("@like", "%" + term + "%"));
                grid.DataSource = dt;
                if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
                if (grid.Columns.Contains("Name")) grid.Columns["Name"].FillWeight = 220;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                lblCount.Text = dt.Rows.Count == 0
                    ? "No " + _recordType.ToLowerInvariant() + " record matches that. Try fewer letters or a different spelling."
                    : dt.Rows.Count >= MaxRows
                        ? "Showing the first " + MaxRows + " matches — type more to narrow the list."
                        : dt.Rows.Count + (dt.Rows.Count == 1 ? " record found." : " records found.");
                lblCount.ForeColor = dt.Rows.Count == 0 ? UiTheme.Danger : UiTheme.Muted;
            }
            catch (Exception ex)
            {
                ErrorLog.Report(this, "FindRecordDialog", ex, "search");
                lblCount.Text = "The records could not be searched right now.";
                lblCount.ForeColor = UiTheme.Danger;
            }
        }

        // ------------------------------------------------------------ choosing
        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) Choose();
        }

        private void grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { Choose(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        private void btnSelect_Click(object sender, EventArgs e) => Choose();

        private void Choose()
        {
            DataGridViewRow row = grid.SelectedRows.Count > 0 ? grid.SelectedRows[0] : grid.CurrentRow;
            if (row == null || row.IsNewRow)
            {
                lblCount.Text = "Click a record in the list first.";
                lblCount.ForeColor = UiTheme.Danger;
                return;
            }

            int id;
            if (!int.TryParse(Convert.ToString(row.Cells["id"].Value), out id)) return;

            string name = Convert.ToString(row.Cells["Name"].Value) ?? "";
            string reg = grid.Columns.Contains("Registry No") ? Convert.ToString(row.Cells["Registry No"].Value) : "";
            PickedId = id;
            PickedName = string.IsNullOrWhiteSpace(reg) ? name : name + "  (" + reg + ")";
            DialogResult = DialogResult.OK;
        }
    }
}
