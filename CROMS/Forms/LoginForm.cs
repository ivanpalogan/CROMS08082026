using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
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
        // Navy Blue palette (light) — the app-wide default; see Modules/UiTheme.cs for the
        // rest of the app. Icons on this screen are all single-color line art (no emoji),
        // drawn to match whichever surface they sit on.
        private static readonly Color PageBg   = Color.FromArgb(244, 246, 249);  // #F4F6F9
        private static readonly Color HostBg   = Color.White;
        private static readonly Color HostLine = Color.FromArgb(225, 229, 236);  // #E1E5EC
        private static readonly Color Ink      = Color.FromArgb(23, 26, 36);     // #171B24
        private static readonly Color SoftInk  = Color.FromArgb(91, 100, 114);   // #5B6472
        private static readonly Color Accent   = Color.FromArgb(29, 78, 216);    // #1D4ED8
        private static readonly Color AccentTint = Color.FromArgb(234, 241, 254);// #EAF1FE
        private static readonly Color Navy     = Color.FromArgb(19, 36, 65);     // #132441
        private static readonly Color Warn     = Color.FromArgb(180, 83, 9);     // #B45309

        private float _eyeHoverT = 0f;

        public LoginForm()
        {
            InitializeComponent();

            // Show / hide the password — btnEye is owner-drawn as a vector eye icon
            // (Tag="noskin" tells UiTheme to leave its painting to us).
            btnEye.FlatStyle = FlatStyle.Flat;
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.Cursor = Cursors.Hand;
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(btnEye, true);
            HoverFade.Attach(btnEye, 150, t => { _eyeHoverT = t; btnEye.Invalidate(); });
            btnEye.Paint += btnEye_Paint;
            btnEye.Click += (s, e) =>
            {
                txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
                btnEye.Invalidate();   // redraw the icon (open eye ↔ eye with slash)
                txtPass.Focus();
            };

            // Brand mark (top-left) and the "Secure Access" badge (top-right) — static line icons.
            pnlLogo.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.Clear(pnlLogo.Parent.BackColor); FillRounded(e.Graphics, pnlLogo.ClientRectangle, Navy, 7); DrawBuildingIcon(e.Graphics, new RectangleF(6, 6, 16, 16), Color.White); };
            pnlBadge.Paint += PnlBadge_Paint;

            // Username / password fields — rounded host with a leading icon, border lights up
            // to the accent color while the field inside it has focus.
            AttachFieldHost(pnlUserHost, txtUser, DrawPersonIcon);
            AttachFieldHost(pnlPassHost, txtPass, DrawLockIcon);
            txtPass.Enter += (s, e) => btnEye.Invalidate();
            txtPass.Leave += (s, e) => btnEye.Invalidate();
            pnlCapsIcon.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.Clear(pnlCapsIcon.Parent.BackColor); DrawWarningIcon(e.Graphics, pnlCapsIcon.ClientRectangle, Warn); };

            // Caps Lock hint — refresh on load and on any typing in either box.
            Load += (s, e) => { UpdateCaps(); txtUser.Focus(); };
            txtUser.KeyUp += (s, e) => UpdateCaps();
            txtPass.KeyUp += (s, e) => UpdateCaps();
            txtPass.Enter += (s, e) => UpdateCaps();

            CROMS.Modules.UiTheme.PolishButtons(this);   // hand cursor + hover on Sign In / Exit
        }

        private void UpdateCaps()
        {
            bool on = Control.IsKeyLocked(Keys.CapsLock);
            lblCaps.Visible = on;
            pnlCapsIcon.Visible = on;
        }

        private void PnlBadge_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(pnlBadge.Parent.BackColor);

            const string text = "Secure Access";
            using (var font = new Font("Segoe UI", 8.25f, FontStyle.Bold))
            {
                var fmt = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                SizeF textSize = g.MeasureString(text, font, pnlBadge.Width, fmt);

                // Pill sized to the actual measured text so it can never wrap/clip, capped to
                // the panel's own width and centered inside it.
                int pillW = Math.Min(pnlBadge.Width, (int)Math.Ceiling(textSize.Width) + 40);
                var rect = new Rectangle((pnlBadge.Width - pillW) / 2, 0, pillW - 1, pnlBadge.Height - 1);
                using (var path = RoundedRect(rect, rect.Height / 2))
                using (var fill = new SolidBrush(AccentTint))
                    g.FillPath(fill, path);

                DrawLockIcon(g, new RectangleF(rect.X + 12, (pnlBadge.Height - 12) / 2f, 12, 12), Accent);
                var textRect = new RectangleF(rect.X + 30, 0, rect.Width - 32, pnlBadge.Height);
                using (var brush = new SolidBrush(Accent))
                    g.DrawString(text, font, brush, textRect, fmt);
            }
        }

        /// <summary>
        /// Wires a rounded "field host" panel around a textbox: white fill, a hairline border
        /// that switches to the accent color while the textbox has focus, and a leading line
        /// icon drawn by <paramref name="drawIcon"/>. Mirrors the focus-highlight pattern the
        /// kiosk already uses for its detail fields.
        /// </summary>
        private void AttachFieldHost(Panel host, TextBox tb, Action<Graphics, RectangleF, Color> drawIcon)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(host, true);

            float focusT = 0f;
            HoverFade.AttachFocus(tb, 150, t => { focusT = t; host.Invalidate(); });

            host.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(host.Parent.BackColor);
                var rect = new Rectangle(0, 0, host.Width - 1, host.Height - 1);
                Color borderNow = HoverFade.Lerp(HostLine, Accent, focusT);
                float borderW = HoverFade.Lerp(1f, 1.6f, focusT);
                using (var path = RoundedRect(rect, 8))
                {
                    using (var fill = new SolidBrush(HostBg)) g.FillPath(fill, path);
                    using (var pen = new Pen(borderNow, borderW)) g.DrawPath(pen, path);
                }
                drawIcon(g, new RectangleF(14, (host.Height - 16) / 2f, 16, 16), HoverFade.Lerp(SoftInk, Accent, focusT));
            };
        }

        private static void FillRounded(Graphics g, Rectangle r, Color color, int radius)
        {
            using (var path = RoundedRect(r, radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Minimal line-art icons — single stroke color, no fills, no emoji.</summary>
        private static void DrawPersonIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.6f))
            {
                float cx = r.X + r.Width / 2f;
                float headR = r.Width * 0.16f;
                float headCy = r.Y + r.Height * 0.28f;
                g.DrawEllipse(pen, cx - headR, headCy - headR, headR * 2, headR * 2);

                var body = new RectangleF(r.X + r.Width * 0.14f, r.Y + r.Height * 0.5f, r.Width * 0.72f, r.Height * 0.62f);
                g.DrawArc(pen, body, 180, 180);
            }
        }

        private static void DrawLockIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.6f))
            {
                float cx = r.X + r.Width / 2f;
                float bodyTop = r.Y + r.Height * 0.42f;
                var body = new RectangleF(r.X + r.Width * 0.10f, bodyTop, r.Width * 0.80f, r.Height * 0.52f);
                using (var path = RoundedRect(Rectangle.Round(body), (int)(r.Height * 0.10f)))
                    g.DrawPath(pen, path);

                float shackleW = body.Width * 0.62f;
                var shackle = new RectangleF(cx - shackleW / 2f, bodyTop - r.Height * 0.36f, shackleW, r.Height * 0.5f);
                g.DrawArc(pen, shackle, 180, 180);
            }
        }

        private static void DrawBuildingIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.5f))
            {
                float cx = r.X + r.Width / 2f;
                float roofBaseY = r.Y + r.Height * 0.32f;
                float baseY = r.Bottom - r.Height * 0.06f;
                float halfW = r.Width * 0.44f;

                g.DrawLine(pen, cx, r.Y, cx - halfW, roofBaseY);
                g.DrawLine(pen, cx, r.Y, cx + halfW, roofBaseY);
                g.DrawLine(pen, cx - halfW, roofBaseY, cx + halfW, roofBaseY);

                float colTop = roofBaseY + r.Height * 0.08f;
                float[] colXs = { cx - halfW * 0.55f, cx, cx + halfW * 0.55f };
                foreach (float x in colXs) g.DrawLine(pen, x, colTop, x, baseY);

                g.DrawLine(pen, cx - halfW - 1.5f, baseY, cx + halfW + 1.5f, baseY);
            }
        }

        private static void DrawWarningIcon(Graphics g, RectangleF r, Color stroke)
        {
            using (var pen = new Pen(stroke, 1.5f))
            {
                float cx = r.X + r.Width / 2f;
                var top = new PointF(cx, r.Y + 1f);
                var left = new PointF(r.X + 1f, r.Bottom - 1f);
                var right = new PointF(r.Right - 1f, r.Bottom - 1f);
                g.DrawLine(pen, top, left);
                g.DrawLine(pen, left, right);
                g.DrawLine(pen, right, top);

                g.DrawLine(pen, cx, r.Y + r.Height * 0.42f, cx, r.Y + r.Height * 0.66f);
                using (var brush = new SolidBrush(stroke))
                    g.FillEllipse(brush, cx - 1.1f, r.Y + r.Height * 0.76f, 2.2f, 2.2f);
            }
        }

        /// <summary>
        /// Draws a bold, rounded show/hide eye icon (no emoji — emoji don't render under the
        /// app's owner-drawn buttons): a full almond outline with a large solid pupil, matching
        /// the classic password-field eye glyph. Open eye = password hidden; eye with a slash =
        /// password visible. Color blends toward the accent on hover OR while the password
        /// field has focus, whichever is stronger.
        /// </summary>
        private void btnEye_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(btnEye.BackColor);

            RectangleF r = btnEye.ClientRectangle;
            float cx = r.Width / 2f, cy = r.Height / 2f;
            float halfW = 9.5f, lidH = 7.5f;               // fuller/rounder almond than a sharp point
            float emphasis = Math.Max(_eyeHoverT, txtPass.Focused ? 1f : 0f);
            Color stroke = HoverFade.Lerp(SoftInk, Accent, emphasis);

            using (var pen = new Pen(stroke, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                var left = new PointF(cx - halfW, cy);
                var right = new PointF(cx + halfW, cy);
                var topC1 = new PointF(cx - halfW * 0.55f, cy - lidH);
                var topC2 = new PointF(cx + halfW * 0.55f, cy - lidH);
                var botC1 = new PointF(cx + halfW * 0.55f, cy + lidH);
                var botC2 = new PointF(cx - halfW * 0.55f, cy + lidH);

                using (var path = new GraphicsPath())
                {
                    path.AddBezier(left, topC1, topC2, right);
                    path.AddBezier(right, botC1, botC2, left);
                    g.DrawPath(pen, path);
                }

                // Large, bold pupil — the defining feature of the reference icon.
                float pr = 4.2f;
                using (var b = new SolidBrush(stroke))
                    g.FillEllipse(b, cx - pr, cy - pr, pr * 2f, pr * 2f);

                // Slash when the password is visible ("eye off").
                if (!txtPass.UseSystemPasswordChar)
                    g.DrawLine(pen, cx - halfW - 1.5f, cy + lidH + 1.5f, cx + halfW + 1.5f, cy - lidH - 1.5f);
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

        private void lblHeadline_Click(object sender, EventArgs e)
        {

        }

        private void lblSubtitle_Click(object sender, EventArgs e)
        {

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
