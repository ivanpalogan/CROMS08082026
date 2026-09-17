using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Users &amp; Access (a page of Settings). Two tabs: <b>Users</b> — create accounts,
    /// edit role, reset password (PBKDF2 hash, never plaintext), activate/deactivate; and
    /// <b>Staff Biodata</b>. The audit log itself is NOT here: this screen used to carry a
    /// plain "last 500 rows" Audit Trail tab over the same audit_log table that Settings'
    /// own activity screen already reads with a date range, user filter, search, bypass
    /// flagging, row drill-down and CSV export. Two views of one table is one view too many,
    /// so the thin one was dropped and the richer one is now the Audit Trail page of
    /// Settings. UI is declared in the Designer (UsersAuditForm.Designer.cs)
    /// so every control is visible/editable on the WinForms design canvas; this file holds only
    /// the data access + event handlers.
    /// </summary>
    public partial class UsersAuditForm : Form, IRefreshable
    {
        private const int MinPasswordLength = 8;

        private int? _selUserId;
        private bool _loadingBio;

        public UsersAuditForm()
        {
            InitializeComponent();
            LoadUsers();
            LoadUserPicker();
            LoadBiodata();
        }

        public void RefreshData()
        {
            LoadUsers();
            LoadUserPicker();
            LoadBiodata();
        }

        // ------------------------------------------------------------ event handlers
        private void btnAdd_Click(object sender, EventArgs e) => AddUser();
        private void btnUpdate_Click(object sender, EventArgs e) => UpdateUser();
        private void btnNew_Click(object sender, EventArgs e) => ClearForm();

        // ---------------------------------------------------------------- data
        private void LoadUsers()
        {
            gridUsers.DataSource = Db.Pull(
                "SELECT id, username AS Username, full_name AS 'Full Name', role AS Role, " +
                "IF(is_active, 'Active', 'Inactive') AS Status FROM users ORDER BY username");
            if (gridUsers.Columns.Contains("id")) gridUsers.Columns["id"].Visible = false;
        }

        /// <summary>
        /// Every staff member's biodata, admin-managed: who is Permanent vs Casual/Job Order/
        /// Contractual/Probationary. The admin adds and edits here via the picker on the right
        /// (pick a staff member, fill it in, Save) — staff themselves only VIEW their own record
        /// (header "My Biodata" button, StaffBiodataForm, read-only).
        /// </summary>
        private void LoadBiodata()
        {
            try
            {
                gridBiodata.DataSource = Db.Pull(
                    "SELECT u.id AS id, u.full_name AS 'Full Name', u.username AS Username, u.role AS Role, " +
                    "COALESCE(b.employment_status, '—') AS 'Employment Status', b.position AS Position, " +
                    "b.date_hired AS 'Date Hired', b.contact_no AS 'Contact No.', " +
                    "b.updated_at AS 'Last Updated' " +
                    "FROM users u LEFT JOIN staff_biodata b ON b.user_id = u.id " +
                    "ORDER BY (b.employment_status = 'Permanent') DESC, u.full_name");
                if (gridBiodata.Columns.Contains("id")) gridBiodata.Columns["id"].Visible = false;
            }
            catch
            {
                // staff_biodata may not exist yet (migration 47 not applied) — degrade quietly.
                gridBiodata.DataSource = null;
            }
        }

        /// <summary>Fills the "Staff Member" picker the admin uses to add/edit a specific person's biodata.</summary>
        private void LoadUserPicker()
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT id, CONCAT(username, ' — ', full_name) AS Label FROM users ORDER BY full_name");
                cboBioUser.DataSource = dt;
                cboBioUser.DisplayMember = "Label";
                cboBioUser.ValueMember = "id";
                cboBioUser.SelectedIndex = -1;
            }
            catch { /* users table always exists; degrade quietly if the connection is down */ }
        }

        private void gridBiodata_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = gridBiodata.Rows[e.RowIndex];
            if (row.Cells["id"].Value == null) return;
            cboBioUser.SelectedValue = Convert.ToInt32(row.Cells["id"].Value);
        }

        private void cboBioUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingBio) return;
            ClearBioFields();
            if (cboBioUser.SelectedValue == null || !(cboBioUser.SelectedValue is int)) return;

            int userId = (int)cboBioUser.SelectedValue;
            DataTable dt;
            try
            {
                dt = Db.Pull("SELECT * FROM staff_biodata WHERE user_id=@id", new MySqlParameter("@id", userId));
            }
            catch
            {
                lblBioUpdated.Text = "staff_biodata table not found — run migration 47.";
                return;
            }

            if (dt.Rows.Count == 0)
            {
                lblBioUpdated.Text = "No biodata on file yet for this staff member.";
                return;
            }

            _loadingBio = true;
            DataRow r = dt.Rows[0];
            txtBioEmployeeNo.Text = BioStr(r, "employee_no");
            int idx = cboBioStatus.Items.IndexOf(BioStr(r, "employment_status"));
            cboBioStatus.SelectedIndex = idx >= 0 ? idx : -1;
            txtBioPosition.Text = BioStr(r, "position");

            if (r["date_hired"] != DBNull.Value)
            {
                chkBioDateHired.Checked = true;
                dtpBioDateHired.Value = Convert.ToDateTime(r["date_hired"]);
            }
            if (r["birthdate"] != DBNull.Value)
            {
                chkBioBirthdate.Checked = true;
                dtpBioBirthdate.Value = Convert.ToDateTime(r["birthdate"]);
            }

            cboBioSex.SelectedIndex = cboBioSex.Items.IndexOf(BioStr(r, "sex"));
            txtBioCivilStatus.Text = BioStr(r, "civil_status");
            txtBioAddress.Text = BioStr(r, "address");
            txtBioContactNo.Text = BioStr(r, "contact_no");
            txtBioEmergencyName.Text = BioStr(r, "emergency_contact_name");
            txtBioEmergencyNo.Text = BioStr(r, "emergency_contact_no");
            _loadingBio = false;

            lblBioUpdated.Text = "Last saved: " + Convert.ToDateTime(r["updated_at"]).ToString("MMM d, yyyy h:mm tt");
        }

        private static string BioStr(DataRow r, string col) =>
            r[col] == DBNull.Value ? "" : r[col].ToString();

        private void chkBioDateHired_CheckedChanged(object sender, EventArgs e) =>
            dtpBioDateHired.Enabled = chkBioDateHired.Checked;

        private void chkBioBirthdate_CheckedChanged(object sender, EventArgs e) =>
            dtpBioBirthdate.Enabled = chkBioBirthdate.Checked;

        private void ClearBioFields()
        {
            txtBioEmployeeNo.Clear();
            cboBioStatus.SelectedIndex = -1;
            txtBioPosition.Clear();
            chkBioDateHired.Checked = false;
            chkBioBirthdate.Checked = false;
            cboBioSex.SelectedIndex = -1;
            txtBioCivilStatus.Clear();
            txtBioAddress.Clear();
            txtBioContactNo.Clear();
            txtBioEmergencyName.Clear();
            txtBioEmergencyNo.Clear();
            lblBioUpdated.Text = "Pick a staff member above.";
        }

        private void btnBioNew_Click(object sender, EventArgs e)
        {
            _loadingBio = true;
            cboBioUser.SelectedIndex = -1;
            _loadingBio = false;
            ClearBioFields();
        }

        private void btnBioSave_Click(object sender, EventArgs e)
        {
            if (cboBioUser.SelectedValue == null || !(cboBioUser.SelectedValue is int))
            {
                MessageBox.Show("Pick a staff member first.", "Staff Biodata",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (cboBioStatus.SelectedItem == null)
            {
                MessageBox.Show("Pick an employment status.", "Staff Biodata",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int userId = (int)cboBioUser.SelectedValue;
            try
            {
                Db.Push(
                    "INSERT INTO staff_biodata " +
                    "(user_id, employee_no, employment_status, position, date_hired, birthdate, sex, " +
                    " civil_status, address, contact_no, emergency_contact_name, emergency_contact_no) " +
                    "VALUES (@id, @eno, @status, @pos, @hired, @bday, @sex, @civ, @addr, @cno, @en, @eno2) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "employee_no=@eno, employment_status=@status, position=@pos, date_hired=@hired, " +
                    "birthdate=@bday, sex=@sex, civil_status=@civ, address=@addr, contact_no=@cno, " +
                    "emergency_contact_name=@en, emergency_contact_no=@eno2",
                    new MySqlParameter("@id", userId),
                    new MySqlParameter("@eno", BioN(txtBioEmployeeNo.Text)),
                    new MySqlParameter("@status", cboBioStatus.SelectedItem.ToString()),
                    new MySqlParameter("@pos", BioN(txtBioPosition.Text)),
                    new MySqlParameter("@hired", chkBioDateHired.Checked ? (object)dtpBioDateHired.Value.Date : DBNull.Value),
                    new MySqlParameter("@bday", chkBioBirthdate.Checked ? (object)dtpBioBirthdate.Value.Date : DBNull.Value),
                    new MySqlParameter("@sex", cboBioSex.SelectedItem?.ToString() ?? (object)DBNull.Value),
                    new MySqlParameter("@civ", BioN(txtBioCivilStatus.Text)),
                    new MySqlParameter("@addr", BioN(txtBioAddress.Text)),
                    new MySqlParameter("@cno", BioN(txtBioContactNo.Text)),
                    new MySqlParameter("@en", BioN(txtBioEmergencyName.Text)),
                    new MySqlParameter("@eno2", BioN(txtBioEmergencyNo.Text)));

                Audit.Write(Audit.Update, "staff_biodata", userId,
                    "Admin set biodata for " + cboBioUser.Text + " (" + cboBioStatus.SelectedItem + ")");

                MessageBox.Show("Biodata saved.", "Staff Biodata",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBiodata();
                cboBioUser_SelectedIndexChanged(null, EventArgs.Empty); // re-show the saved row + timestamp
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static object BioN(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private void gridUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = gridUsers.Rows[e.RowIndex];
            _selUserId = Convert.ToInt32(row.Cells["id"].Value);
            txtUsername.Text = row.Cells["Username"].Value?.ToString() ?? "";
            txtFullName.Text = row.Cells["Full Name"].Value?.ToString() ?? "";
            cboRole.SelectedItem = row.Cells["Role"].Value?.ToString();
            chkActive.Checked = (row.Cells["Status"].Value?.ToString() == "Active");
            txtPassword.Clear();
            txtConfirm.Clear();
        }

        private void AddUser()
        {
            if (!Require(true)) return;
            try
            {
                Db.Push(
                    "INSERT INTO users (username, password_hash, full_name, role, is_active) " +
                    "VALUES (@u, @h, @f, @r, @a)",
                    new MySqlParameter("@u", txtUsername.Text.Trim()),
                    new MySqlParameter("@h", PasswordHasher.Hash(txtPassword.Text)),
                    new MySqlParameter("@f", txtFullName.Text.Trim()),
                    new MySqlParameter("@r", cboRole.SelectedItem.ToString()),
                    new MySqlParameter("@a", chkActive.Checked ? 1 : 0));
                Audit.Write(Audit.Create, "users", null, "Created user " + txtUsername.Text.Trim());
                MessageBox.Show("User created.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadUsers();
                }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("That username is already taken.", "Users",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void UpdateUser()
        {
            if (_selUserId == null)
            {
                MessageBox.Show("Pick a user from the list first.", "Users",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Require(false)) return;
            try
            {
                // Update the core fields; only reset the password when one was typed.
                Db.Push("UPDATE users SET username=@u, full_name=@f, role=@r, is_active=@a WHERE id=@id",
                    new MySqlParameter("@u", txtUsername.Text.Trim()),
                    new MySqlParameter("@f", txtFullName.Text.Trim()),
                    new MySqlParameter("@r", cboRole.SelectedItem.ToString()),
                    new MySqlParameter("@a", chkActive.Checked ? 1 : 0),
                    new MySqlParameter("@id", _selUserId.Value));

                // An admin reset forces the user to set their own password on next login.
                if (!string.IsNullOrEmpty(txtPassword.Text))
                    Db.Push("UPDATE users SET password_hash=@h, must_change_password=1 WHERE id=@id",
                        new MySqlParameter("@h", PasswordHasher.Hash(txtPassword.Text)),
                        new MySqlParameter("@id", _selUserId.Value));

                Audit.Write(Audit.Update, "users", _selUserId.Value,
                    "Updated user " + txtUsername.Text.Trim() +
                    (string.IsNullOrEmpty(txtPassword.Text) ? "" : " (password reset)"));
                MessageBox.Show("User updated.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadUsers();
                }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("That username is already taken.", "Users",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private bool Require(bool needPassword)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtFullName.Text) || cboRole.SelectedItem == null)
            {
                MessageBox.Show("Username, full name and role are required.", "Users",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (needPassword && string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Set a password for the new user.", "Users",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            // Whenever a password is being set (new user, or a reset on edit), enforce policy.
            if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                if (txtPassword.Text.Length < MinPasswordLength)
                {
                    MessageBox.Show("Password must be at least " + MinPasswordLength + " characters.",
                        "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (txtPassword.Text != txtConfirm.Text)
                {
                    MessageBox.Show("The password and confirmation do not match.", "Users",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private void ClearForm()
        {
            _selUserId = null;
            txtUsername.Clear();
            txtFullName.Clear();
            txtPassword.Clear();
            txtConfirm.Clear();
            cboRole.SelectedIndex = -1;
            chkActive.Checked = true;
        }

        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
