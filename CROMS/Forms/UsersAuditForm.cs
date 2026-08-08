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
    /// Users &amp; Audit Trail (Administration). Two tabs: <b>Users</b> — create accounts,
    /// edit role, reset password (PBKDF2 hash, never plaintext), activate/deactivate; and
    /// <b>Audit Trail</b> — the tamper-evidence log of every create/update/delete/login,
    /// joined to the user who did it. UI is declared in the Designer (UsersAuditForm.Designer.cs)
    /// so every control is visible/editable on the WinForms design canvas; this file holds only
    /// the data access + event handlers.
    /// </summary>
    public partial class UsersAuditForm : Form, IRefreshable
    {
        private const int MinPasswordLength = 8;

        private int? _selUserId;

        public UsersAuditForm()
        {
            InitializeComponent();
            LoadUsers();
            LoadAudit();
        }

        public void RefreshData()
        {
            LoadUsers();
            LoadAudit();
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

        private void LoadAudit()
        {
            gridAudit.DataSource = Db.Pull(
                "SELECT a.created_at AS 'When', COALESCE(u.username, '—') AS 'User', a.action AS Action, " +
                "a.table_name AS 'Table', a.record_id AS 'Record', a.details AS Details " +
                "FROM audit_log a LEFT JOIN users u ON u.id = a.user_id " +
                "ORDER BY a.id DESC LIMIT 500");
        }

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
                LoadAudit();
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
                LoadAudit();
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
