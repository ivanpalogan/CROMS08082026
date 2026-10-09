using CROMS.Modules;
using System;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Secure re-authentication dialog shown before the Window Management CRUD in
    /// Settings. Verifies a username + password against the `users` table using the
    /// existing PBKDF2 hash (never plaintext, never stored/logged) and only accepts
    /// Administrator accounts (plus Staff when <see cref="AllowStaff"/> is set). On success <see cref="VerifiedUser"/> holds
    /// the confirmed account and the dialog returns OK.
    /// <para/>
    /// UI layout lives in AdminVerificationForm.Designer.cs; this file holds only the
    /// verification logic.
    /// </summary>
    public partial class AdminVerificationForm : Form
    {
        /// <summary>The account that passed verification (null until success).</summary>
        public CurrentUser VerifiedUser { get; private set; }

        /// <summary>
        /// False (default): only an Administrator account passes - window management,
        /// templates, publishing, priority-window override. True: a Staff account passes
        /// too - used only by the document-bypass prompts, which Staff may perform.
        /// </summary>
        public bool AllowStaff { get; set; }

        public AdminVerificationForm()
        {
            InitializeComponent();
            FieldLimit.Cap(50, txtUser);                 // users.username
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            if (user.Length == 0 || txtPass.Text.Length == 0)
            {
                Msg("Enter your username and password.");
                return;
            }

            DataTable dt;
            try
            {
                dt = Db.Pull(
                    "SELECT id, username, password_hash, full_name, role, is_active " +
                    "FROM users WHERE username = @u",
                    new MySqlParameter("@u", user));
            }
            catch (Exception ex)
            {
                Msg(CROMS.Data.ErrorLog.Text(ex));
                return;
            }

            // Same message for unknown user and wrong password — don't reveal which.
            if (dt.Rows.Count == 0 ||
                !PasswordHasher.Verify(txtPass.Text, dt.Rows[0]["password_hash"].ToString()))
            {
                Msg("Invalid username or password.");
                return;
            }

            DataRow r = dt.Rows[0];
            if (Convert.ToInt32(r["is_active"]) == 0)
            {
                Msg("This account is deactivated. Contact an administrator.");
                return;
            }

            string role = r["role"].ToString();
            bool ok = role == "Admin" || (AllowStaff && role == "Staff");
            if (!ok)
            {
                Msg(AllowStaff
                    ? "This account cannot authorize this action."
                    : "Only an Administrator account can authorize this action.");
                return;
            }

            VerifiedUser = new CurrentUser
            {
                Id = Convert.ToInt32(r["id"]),
                Username = r["username"].ToString(),
                FullName = r["full_name"].ToString(),
                Role = role
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Msg(string text) => lblMsg.Text = text;
    }
}
