using System;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// The "Edit Profile" dialog. Reached from the header user menu's Edit Profile item (the
    /// simple "View My Profile" window is <see cref="ProfileViewForm"/> instead — this one is
    /// only shown when the user actually wants to change something).
    /// Self-service: Full Name + password. Work details (position, employment status, etc.)
    /// stay view-only — Admin is the only one who ADDS/EDITS biodata (Users &amp; Audit Trail
    /// -&gt; "Staff Biodata" tab); this screen just shows what the office has on file.
    /// </summary>
    public partial class StaffBiodataForm : Form
    {
        private const int MinPasswordLength = 8;

        public StaffBiodataForm()
        {
            InitializeComponent();
            LockAsReadOnly();
            LoadAccount();
            LoadOwnRow();
        }

        private void LoadAccount()
        {
            if (Session.User == null) return;
            txtFullName.Text = Session.User.FullName;
            lblUsernameValue.Text = Session.User.Username;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Session.User == null) { Close(); return; }

            string fullName = txtFullName.Text.Trim();
            if (fullName.Length == 0)
            {
                MessageBox.Show("Enter your full name.", "Edit Profile",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool changingPassword = txtCurPass.Text.Length > 0 || txtNewPass.Text.Length > 0 || txtConfirmPass.Text.Length > 0;
            string newHash = null;

            if (changingPassword)
            {
                var row = Db.Pull("SELECT password_hash FROM users WHERE id=@id",
                    new MySqlParameter("@id", Session.User.Id));
                if (row.Rows.Count == 0 || !PasswordHasher.Verify(txtCurPass.Text, row.Rows[0]["password_hash"].ToString()))
                {
                    MessageBox.Show("Current password is incorrect.", "Edit Profile",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (txtNewPass.Text.Length < MinPasswordLength)
                {
                    MessageBox.Show("New password must be at least " + MinPasswordLength + " characters.",
                        "Edit Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (txtNewPass.Text != txtConfirmPass.Text)
                {
                    MessageBox.Show("New password and confirmation do not match.", "Edit Profile",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                newHash = PasswordHasher.Hash(txtNewPass.Text);
            }

            if (changingPassword)
                Db.Push("UPDATE users SET full_name=@fn, password_hash=@ph WHERE id=@id",
                    new MySqlParameter("@fn", fullName), new MySqlParameter("@ph", newHash),
                    new MySqlParameter("@id", Session.User.Id));
            else
                Db.Push("UPDATE users SET full_name=@fn WHERE id=@id",
                    new MySqlParameter("@fn", fullName), new MySqlParameter("@id", Session.User.Id));

            Audit.Write(Audit.Update, "users", Session.User.Id,
                changingPassword ? "Edited own profile; changed password" : "Edited own profile");

            Session.User.FullName = fullName;
            txtCurPass.Clear(); txtNewPass.Clear(); txtConfirmPass.Clear();

            MessageBox.Show("Saved.", "Edit Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
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
