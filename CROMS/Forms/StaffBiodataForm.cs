using System;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// View-only display of the signed-in staff member's own biodata row. Admin is the only
    /// one who ADDS/EDITS biodata (Users &amp; Audit Trail -&gt; "Staff Biodata" tab) — this
    /// screen just lets each staff member see what the office has on file for them.
    /// </summary>
    public partial class StaffBiodataForm : Form
    {
        public StaffBiodataForm()
        {
            InitializeComponent();
            subtitleLabel.Text = "This is what the office has on file for you. Ask an admin to make changes.";
            btnSave.Visible = false; // admin-only action now
            LockAsReadOnly();
            LoadOwnRow();
        }

        private void LockAsReadOnly()
        {
            txtEmployeeNo.ReadOnly = true;
            cboEmploymentStatus.Enabled = false;
            txtPosition.ReadOnly = true;
            dtpDateHired.Enabled = false;
            chkDateHired.Enabled = false;
            dtpBirthdate.Enabled = false;
            chkBirthdate.Enabled = false;
            cboSex.Enabled = false;
            txtCivilStatus.ReadOnly = true;
            txtAddress.ReadOnly = true;
            txtContactNo.ReadOnly = true;
            txtEmergencyName.ReadOnly = true;
            txtEmergencyNo.ReadOnly = true;
        }

        private void chkDateHired_CheckedChanged(object sender, EventArgs e) { /* view-only, no-op */ }

        private void chkBirthdate_CheckedChanged(object sender, EventArgs e) { /* view-only, no-op */ }

        private void LoadOwnRow()
        {
            if (Session.User == null) return;
            DataTable dt = Db.Pull(
                "SELECT * FROM staff_biodata WHERE user_id=@id",
                new MySqlParameter("@id", Session.User.Id));
            if (dt.Rows.Count == 0)
            {
                cboEmploymentStatus.SelectedIndex = -1;
                lblUpdated.Text = "Nothing on file yet — ask an admin to enter your biodata.";
                return;
            }
            DataRow r = dt.Rows[0];
            txtEmployeeNo.Text = S(r, "employee_no");
            int idx = cboEmploymentStatus.Items.IndexOf(S(r, "employment_status"));
            cboEmploymentStatus.SelectedIndex = idx >= 0 ? idx : -1;
            txtPosition.Text = S(r, "position");

            if (r["date_hired"] != DBNull.Value)
            {
                chkDateHired.Checked = true;
                dtpDateHired.Value = Convert.ToDateTime(r["date_hired"]);
            }
            if (r["birthdate"] != DBNull.Value)
            {
                chkBirthdate.Checked = true;
                dtpBirthdate.Value = Convert.ToDateTime(r["birthdate"]);
            }

            int sexIdx = cboSex.Items.IndexOf(S(r, "sex"));
            cboSex.SelectedIndex = sexIdx;
            txtCivilStatus.Text = S(r, "civil_status");
            txtAddress.Text = S(r, "address");
            txtContactNo.Text = S(r, "contact_no");
            txtEmergencyName.Text = S(r, "emergency_contact_name");
            txtEmergencyNo.Text = S(r, "emergency_contact_no");

            lblUpdated.Text = "Last updated: " + Convert.ToDateTime(r["updated_at"]).ToString("MMM d, yyyy h:mm tt");
        }

        private static string S(DataRow r, string col) =>
            r[col] == DBNull.Value ? "" : r[col].ToString();

        private void btnClose_Click(object sender, EventArgs e) => Close();
    }
}
