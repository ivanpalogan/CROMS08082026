using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Sign-in screen shown before the main app (from Program.cs). Verifies the username
    /// + password against the `users` table (PBKDF2 hash, never plaintext), rejects
    /// deactivated accounts, sets <see cref="Session.User"/>, and writes a Login row to
    /// the audit trail. The app only opens on DialogResult.OK.
    /// <para/>
    /// UI layout lives in LoginForm.Designer.cs; this file holds only the login logic,
    /// validation, and database access (standard WinForms code/design separation).
    /// </summary>
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();

            // Show / hide the password — btnEye is owner-drawn as a vector eye icon
            // (Tag="noskin" tells UiTheme to leave its painting to us).
            btnEye.FlatStyle = FlatStyle.Flat;
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.Cursor = Cursors.Hand;
            btnEye.Paint += btnEye_Paint;
            btnEye.Click += (s, e) =>
            {
                txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
                btnEye.Invalidate();   // redraw the icon (open eye ↔ eye with slash)
                txtPass.Focus();
            };

            // Caps Lock hint — refresh on load and on any typing in either box.
            Load += (s, e) => { UpdateCaps(); txtUser.Focus(); };
            txtUser.KeyUp += (s, e) => UpdateCaps();
            txtPass.KeyUp += (s, e) => UpdateCaps();
            txtPass.Enter += (s, e) => UpdateCaps();

            CROMS.Modules.UiTheme.PolishButtons(this);   // hand cursor + hover on Sign In / eye / Exit
        }

        private void UpdateCaps() => lblCaps.Visible = Control.IsKeyLocked(Keys.CapsLock);

        /// <summary>
        /// Draws a crisp vector eye icon on the show/hide button (no emoji — emoji don't render
        /// under the app's owner-drawn buttons). Open eye = password hidden; eye with a slash =
        /// password visible.
        /// </summary>
        private void btnEye_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(btnEye.BackColor);

            RectangleF r = btnEye.ClientRectangle;
            float cx = r.Width / 2f, cy = r.Height / 2f;
            float w = 20f, h = 12f;                       // eye almond size
            var eye = new RectangleF(cx - w / 2f, cy - h / 2f, w, h);

            using (var pen = new Pen(Color.White, 1.8f))
            {
                // Almond outline: two arcs (top + bottom) meeting at the corners.
                g.DrawArc(pen, cx - w / 2f, cy - h / 2f - 3f, w, h + 6f, 20, 140);   // top lid
                g.DrawArc(pen, cx - w / 2f, cy - h / 2f - 3f, w, h + 6f, 200, 140);  // bottom lid

                // Pupil.
                float pr = 3.2f;
                using (var b = new SolidBrush(Color.White))
                    g.FillEllipse(b, cx - pr, cy - pr, pr * 2f, pr * 2f);

                // Slash when the password is visible ("eye off").
                if (!txtPass.UseSystemPasswordChar)
                    g.DrawLine(pen, cx - w / 2f - 1f, cy + h / 2f + 2f, cx + w / 2f + 1f, cy - h / 2f - 2f);
            }
        }

        /// <summary>Cancel sign-in and close the app (Program.Main exits on non-OK).</summary>
        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ---- Failed-attempt lockout (in-memory, per running app instance) ----
        private const int MaxFails = 5;
        private const int LockSeconds = 60;
        private static readonly Dictionary<string, (int fails, DateTime until)> _attempts =
            new Dictionary<string, (int, DateTime)>();

        private static bool IsLocked(string key, out int secondsLeft)
        {
            secondsLeft = 0;
            if (_attempts.TryGetValue(key, out var a) && a.until > DateTime.Now)
            {
                secondsLeft = (int)Math.Ceiling((a.until - DateTime.Now).TotalSeconds);
                return true;
            }
            return false;
        }

        private static void RegisterFail(string key)
        {
            _attempts.TryGetValue(key, out var a);
            // If a previous lock already lapsed, start counting fresh.
            int fails = (a.until != default(DateTime) && a.until <= DateTime.Now) ? 1 : a.fails + 1;
            DateTime until = fails >= MaxFails ? DateTime.Now.AddSeconds(LockSeconds) : default(DateTime);
            _attempts[key] = (fails, until);
        }

        private static void ClearFails(string key) => _attempts.Remove(key);

        /// <summary>Validate input, verify the credentials, and start the session.</summary>
        private void btnSignIn_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            if (user.Length == 0 || txtPass.Text.Length == 0)
            {
                Msg("Enter your username and password.");
                return;
            }

            string key = user.ToLowerInvariant();
            if (IsLocked(key, out int wait))
            {
                Msg("Too many attempts. Try again in " + wait + "s.");
                return;
            }

            // Block double-submit while the DB check runs.
            SetBusy(true);

            DataTable dt;
            try
            {
                dt = Db.Pull(
                    "SELECT id, username, password_hash, full_name, role, is_active, must_change_password " +
                    "FROM users WHERE username = @u",
                    new MySqlParameter("@u", user));
            }
            catch (Exception ex)
            {
                SetBusy(false);
                Msg("Cannot reach the database: " + ex.Message);
                return;
            }

            // Same message for unknown user and wrong password — don't reveal which.
            if (dt.Rows.Count == 0 ||
                !PasswordHasher.Verify(txtPass.Text, dt.Rows[0]["password_hash"].ToString()))
            {
                RegisterFail(key);
                Audit.Write(Audit.Login, "users", null, "Failed sign-in for '" + user + "'");
                SetBusy(false);
                Msg(IsLocked(key, out int w)
                    ? "Too many attempts. Try again in " + w + "s."
                    : "Invalid username or password.");
                return;
            }

            DataRow r = dt.Rows[0];
            if (Convert.ToInt32(r["is_active"]) == 0)
            {
                SetBusy(false);
                Msg("This account is deactivated. Contact an administrator.");
                return;
            }

            ClearFails(key);
            Session.User = new CurrentUser
            {
                Id = Convert.ToInt32(r["id"]),
                Username = r["username"].ToString(),
                FullName = r["full_name"].ToString(),
                Role = r["role"].ToString()
            };

            // Force a password change on first login (or after an admin reset).
            if (Convert.ToInt32(r["must_change_password"]) == 1)
            {
                using (var chg = new ForceChangePasswordForm(Session.User.Id, Session.User.Username))
                {
                    if (chg.ShowDialog(this) != DialogResult.OK)
                    {
                        Session.User = null;
                        SetBusy(false);
                        Msg("You must set a new password to sign in.");
                        return;
                    }
                }
            }

            Audit.Write(Audit.Login, "users", Session.User.Id, "Signed in: " + Session.User.Username);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Msg(string text) => lblMsg.Text = text;

        /// <summary>Disable the form + show progress while the credential check runs.</summary>
        private void SetBusy(bool busy)
        {
            btnSignIn.Enabled = !busy;
            btnSignIn.Text = busy ? "Signing in…" : "Sign In";
            if (busy) lblMsg.Text = "";
            Refresh();
        }
    }

    /// <summary>
    /// Forced password-change dialog shown at sign-in when users.must_change_password = 1
    /// (a fresh account or an admin reset). The user cannot enter the app until they set a
    /// new password of at least <see cref="MinLength"/> characters; on success it writes the
    /// new PBKDF2 hash, clears the flag, and audits the change. Cancel aborts the sign-in.
    /// </summary>
    internal sealed class ForceChangePasswordForm : Form
    {
        public const int MinLength = 8;

        private readonly int _userId;
        private readonly string _username;
        private readonly TextBox _p1, _p2;
        private readonly Label _msg;

        public ForceChangePasswordForm(int userId, string username)
        {
            _userId = userId;
            _username = username;

            Text = "Set a New Password";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;   // no X — they must set a password or press Cancel
            ClientSize = new Size(420, 300);
            BackColor = Color.FromArgb(33, 37, 41);
            Font = new Font("Segoe UI", 10F);

            Controls.Add(new Label
            {
                Text = "Set a new password", AutoSize = true, ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold), Location = new Point(24, 20)
            });
            Controls.Add(new Label
            {
                Text = "Required before you can use CROMS (" + _username + ").",
                AutoSize = true, ForeColor = Color.FromArgb(173, 181, 189),
                Font = new Font("Segoe UI", 9F), Location = new Point(26, 56)
            });

            Controls.Add(Cap("New password (min " + MinLength + " characters)", 24, 88));
            _p1 = Box(24, 110);
            Controls.Add(_p1);
            Controls.Add(Cap("Confirm new password", 24, 150));
            _p2 = Box(24, 172);
            Controls.Add(_p2);

            _msg = new Label
            {
                AutoSize = true, ForeColor = Color.FromArgb(255, 138, 128),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(24, 210)
            };
            Controls.Add(_msg);

            var save = new Button
            {
                Text = "Save and Continue", Location = new Point(24, 240), Size = new Size(240, 42),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                BackColor = Color.FromArgb(25, 135, 84), Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += (s, e) => Save();
            var cancel = new Button
            {
                Text = "Cancel", Location = new Point(276, 240), Size = new Size(120, 42),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(206, 212, 218),
                BackColor = Color.FromArgb(52, 58, 64), Font = new Font("Segoe UI", 10F)
            };
            cancel.FlatAppearance.BorderSize = 0;
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(save);
            Controls.Add(cancel);
            AcceptButton = save;
        }

        private static Label Cap(string t, int x, int y) => new Label
        {
            Text = t, AutoSize = true, ForeColor = Color.FromArgb(206, 212, 218),
            Font = new Font("Segoe UI", 9F), Location = new Point(x, y)
        };
        private static TextBox Box(int x, int y) => new TextBox
        {
            Location = new Point(x, y), Size = new Size(372, 30), Font = new Font("Segoe UI", 12F),
            BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true
        };

        private void Save()
        {
            if (_p1.Text.Length < MinLength) { _msg.Text = "Password must be at least " + MinLength + " characters."; return; }
            if (_p1.Text != _p2.Text) { _msg.Text = "The two passwords do not match."; return; }

            try
            {
                Db.Push("UPDATE users SET password_hash = @h, must_change_password = 0 WHERE id = @id",
                    new MySqlParameter("@h", PasswordHasher.Hash(_p1.Text)),
                    new MySqlParameter("@id", _userId));
                Audit.Write(Audit.Update, "users", _userId, "Password changed on first login: " + _username);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _msg.Text = "Could not save: " + ex.Message;
            }
        }
    }
}
