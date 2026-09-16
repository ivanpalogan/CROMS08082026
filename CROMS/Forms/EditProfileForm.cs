using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Self-service "Edit Profile" — lets the SIGNED-IN user change their own full name and,
    /// optionally, their own password (current password required to change it). Does not touch
    /// role, username or account status — those stay Admin-only via Users &amp; Audit Trail, per
    /// the account model decided 2026-07-19 (admin-only creation, no self-registration).
    /// </summary>
    internal sealed class EditProfileForm : Form
    {
        private const int MinPasswordLength = 8;

        private readonly TextBox _fullName;
        private readonly TextBox _curPass;
        private readonly TextBox _newPass;
        private readonly TextBox _confirmPass;

        /// <summary>Full name after a successful save (caller refreshes the header with this).</summary>
        public string SavedFullName { get; private set; }

        public EditProfileForm()
        {
            Text = "Edit Profile";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            BackColor = Color.White;
            ClientSize = new Size(420, 430);

            Controls.Add(Cap("Username", 20, 16));
            var lblUsername = new Label
            {
                Text = Session.User?.Username ?? "",
                Location = new Point(20, 36), AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiTheme.Ink
            };
            Controls.Add(lblUsername);

            Controls.Add(Cap("Full Name", 20, 70));
            _fullName = Field(20, 94);
            _fullName.Text = Session.User?.FullName ?? "";
            Controls.Add(_fullName);

            Controls.Add(new Label
            {
                Text = "Change Password (leave blank to keep your current password)",
                Location = new Point(20, 138), AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiTheme.Muted
            });

            Controls.Add(Cap("Current Password", 20, 164));
            _curPass = Field(20, 188); _curPass.UseSystemPasswordChar = true;
            Controls.Add(_curPass);

            Controls.Add(Cap("New Password", 20, 232));
            _newPass = Field(20, 256); _newPass.UseSystemPasswordChar = true;
            Controls.Add(_newPass);

            Controls.Add(Cap("Confirm New Password", 20, 300));
            _confirmPass = Field(20, 324); _confirmPass.UseSystemPasswordChar = true;
            Controls.Add(_confirmPass);

            var save = new Button
            {
                Text = "Save Changes", Location = new Point(20, 372), Size = new Size(180, 44),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                BackColor = UiTheme.Accent, Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += Save_Click;
            var cancel = new Button
            {
                Text = "Cancel", Location = new Point(216, 372), Size = new Size(184, 44),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 10F)
            };
            cancel.FlatAppearance.BorderColor = UiTheme.CardLine;
            Controls.Add(save); Controls.Add(cancel);
            AcceptButton = save; CancelButton = cancel;

            UiTheme.Polish(this);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (Session.User == null) { Close(); return; }

            string fullName = _fullName.Text.Trim();
            if (fullName.Length == 0)
            {
                MessageBox.Show("Enter your full name.", "Edit Profile",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool changingPassword = _curPass.Text.Length > 0 || _newPass.Text.Length > 0 || _confirmPass.Text.Length > 0;
            string newHash = null;

            if (changingPassword)
            {
                var row = Db.Pull("SELECT password_hash FROM users WHERE id=@id",
                    new MySqlParameter("@id", Session.User.Id));
                if (row.Rows.Count == 0 || !PasswordHasher.Verify(_curPass.Text, row.Rows[0]["password_hash"].ToString()))
                {
                    MessageBox.Show("Current password is incorrect.", "Edit Profile",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_newPass.Text.Length < MinPasswordLength)
                {
                    MessageBox.Show("New password must be at least " + MinPasswordLength + " characters.",
                        "Edit Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_newPass.Text != _confirmPass.Text)
                {
                    MessageBox.Show("New password and confirmation do not match.", "Edit Profile",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                newHash = PasswordHasher.Hash(_newPass.Text);
            }

            string sql = changingPassword
                ? "UPDATE users SET full_name=@fn, password_hash=@ph WHERE id=@id"
                : "UPDATE users SET full_name=@fn WHERE id=@id";
            if (changingPassword)
                Db.Push(sql, new MySqlParameter("@fn", fullName), new MySqlParameter("@ph", newHash),
                    new MySqlParameter("@id", Session.User.Id));
            else
                Db.Push(sql, new MySqlParameter("@fn", fullName), new MySqlParameter("@id", Session.User.Id));

            Audit.Write(Audit.Update, "users", Session.User.Id,
                changingPassword ? "Edited own profile; changed password" : "Edited own profile");

            Session.User.FullName = fullName;
            SavedFullName = fullName;

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label Cap(string t, int x, int y) => new Label
        {
            Text = t, AutoSize = true, Location = new Point(x, y),
            Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(73, 80, 87)
        };

        private static TextBox Field(int x, int y) => new TextBox
        {
            Location = new Point(x, y), Size = new Size(380, 28),
            Font = new Font("Segoe UI", 10.5F)
        };
    }
}
