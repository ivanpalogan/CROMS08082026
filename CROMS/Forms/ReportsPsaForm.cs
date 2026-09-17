using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Reports &amp; PSA — the monthly Report of Registered Vital Events the LCRO submits
    /// to PSA. Pick a month + year and Generate: it shows how many births, marriages and
    /// deaths were REGISTERED that month (by registration date = created_at), the
    /// timely-vs-delayed split for births (RA 3753 30-day reglementary period, from
    /// `is_delayed`), and a detail roster per event type that can be exported to CSV for
    /// the submission packet. Collections are reported under Reports &amp; Analytics ->
    /// Fees &amp; Collections, not here. UI is built in code (the designer holds only the
    /// title). Read-only.
    /// </summary>
    public partial class ReportsPsaForm : Form, IRefreshable
    {
        // Suppresses Generate()/LoadRoster() while the designer-wired combo handlers fire
        // during InitializeComponent + the ctor's month/year defaults, before data is ready.
        private bool _ready;

        public ReportsPsaForm()
        {
            InitializeComponent();
            // Year list is dynamic (this year back 6), so it's filled here, not in the designer.
            int thisYear = DateTime.Now.Year;
            for (int yy = thisYear; yy >= thisYear - 6; yy--) cboYear.Items.Add(yy.ToString());
            cboYear.SelectedIndex = 0;
            cboMonth.SelectedIndex = DateTime.Now.Month - 1;
            cboType.SelectedIndex = 0;
            _ready = true;
            Generate();
        }

        /// <summary>Called by the shell each time the module is shown — keep it live.</summary>
        public void RefreshData() => Generate();

        private int SelectedYear => int.TryParse(cboYear.SelectedItem?.ToString(), out int y) ? y : DateTime.Now.Year;
        private int SelectedMonth => cboMonth.SelectedIndex + 1;

        // ------------------------------------------------------------ event handlers
        private void cboMonth_SelectedIndexChanged(object sender, EventArgs e) => Generate();
        private void cboYear_SelectedIndexChanged(object sender, EventArgs e) => Generate();
        private void cboType_SelectedIndexChanged(object sender, EventArgs e) => LoadRoster();
        private void btnExport_Click(object sender, EventArgs e) => ExportCsv();

        // ---------------------------------------------------------------- data
        private void Generate()
        {
            if (!_ready) return;
            int y = SelectedYear, m = SelectedMonth;
            string mf = " AND YEAR(created_at) = " + y + " AND MONTH(created_at) = " + m;

            int births = Scalar("SELECT COUNT(*) FROM births WHERE status = 'Registered'" + mf);
            int marriages = Scalar("SELECT COUNT(*) FROM marriages WHERE status = 'Registered'" + mf);
            int deaths = Scalar("SELECT COUNT(*) FROM deaths WHERE status = 'Registered'" + mf);
            // The timely/delayed split is counted ONLY over births whose registration date
            // the database actually knows (`date_registered`, added in migration 27). A row
            // without one was migrated from the old system or digitized from the paper
            // books, so the office registered it at some unrecorded time — quite possibly
            // on time. Counting those as timely, which is what reading `is_delayed` alone
            // used to do, puts a figure in a statutory return that nothing supports.
            string known = " AND date_registered IS NOT NULL";
            int timely = Scalar("SELECT COUNT(*) FROM births WHERE status = 'Registered' AND is_delayed = 0" + known + mf);
            int delayed = Scalar("SELECT COUNT(*) FROM births WHERE status = 'Registered' AND is_delayed = 1" + known + mf);
            int undated = Scalar("SELECT COUNT(*) FROM births WHERE status = 'Registered' AND date_registered IS NULL" + mf);

            lblBirthVal.Text = births.ToString();
            lblMarriageVal.Text = marriages.ToString();
            lblDeathVal.Text = deaths.ToString();
            lblSplit.Text = "Births — Timely: " + timely + "   ·   Delayed: " + delayed +
                "   (delayed = registered beyond the 30-day reglementary period, RA 3753)" +
                (undated > 0
                    ? "   ·   " + undated + " not counted — no registration date on record " +
                      "(registered before CROMS, or digitized from the registry books)"
                    : "");

            LoadRoster();
        }

        private void LoadRoster()
        {
            if (!_ready) return;
            int y = SelectedYear, m = SelectedMonth;
            string mf = " AND YEAR(created_at) = " + y + " AND MONTH(created_at) = " + m;
            string sql;

            switch (cboType.SelectedItem?.ToString())
            {
                case "Marriages":
                    sql = "SELECT registry_no AS 'Registry No', " +
                          "TRIM(CONCAT(COALESCE(husband_last_name,''), ', ', COALESCE(husband_first_name,''), " +
                          "'  &  ', COALESCE(wife_last_name,''), ', ', COALESCE(wife_first_name,''))) AS 'Parties', " +
                          "date_of_marriage AS 'Date of Marriage', DATE(created_at) AS 'Date Registered', " +
                          "status AS Status FROM marriages WHERE status = 'Registered'" + mf + " ORDER BY created_at, id";
                    break;
                case "Deaths":
                    sql = "SELECT registry_no AS 'Registry No', full_name AS 'Deceased', " +
                          "date_of_death AS 'Date of Death', DATE(created_at) AS 'Date Registered', " +
                          "status AS Status FROM deaths WHERE status = 'Registered'" + mf + " ORDER BY created_at, id";
                    break;
                default: // Births
                    sql = "SELECT registry_no AS 'Registry No', " +
                          "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS 'Child', " +
                          "sex AS Sex, date_of_birth AS 'Date of Birth', date_registered AS 'Date Registered', " +
                          "IF(date_registered IS NULL, 'Not recorded', IF(is_delayed, 'Delayed', 'Timely')) AS 'Registration', " +
                          "status AS Status " +
                          "FROM births WHERE status = 'Registered'" + mf + " ORDER BY created_at, id";
                    break;
            }
            grid.DataSource = Db.Pull(sql);
        }

        private void ExportCsv()
        {
            if (!(grid.DataSource is DataTable dt) || dt.Rows.Count == 0)
            {
                MessageBox.Show("Nothing to export for this month.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "psa_report_" + cboType.SelectedItem + "_" + SelectedYear + "_" +
                           SelectedMonth.ToString("D2") + ".csv"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                var sb = new StringBuilder();
                for (int c = 0; c < dt.Columns.Count; c++)
                    sb.Append(c == 0 ? "" : ",").Append(Csv(dt.Columns[c].ColumnName));
                sb.AppendLine();
                foreach (DataRow row in dt.Rows)
                {
                    for (int c = 0; c < dt.Columns.Count; c++)
                        sb.Append(c == 0 ? "" : ",").Append(Csv(row[c].ToString()));
                    sb.AppendLine();
                }
                File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Exported to " + sfd.FileName, "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        // scalar helpers (use COUNT/SUM correctly — not the id-returning Db.GetCount)
        private static int Scalar(string sql)
        {
            DataTable dt = Db.Pull(sql);
            return dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
    }
}
