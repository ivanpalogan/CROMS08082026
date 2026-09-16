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
    /// "View My Profile" — a simple, read-only card: avatar (uploaded photo, or initials when
    /// none is set), full name, role, username, and a short work-details summary pulled
    /// from <c>staff_biodata</c>. No editable fields live here — the detailed, editable screen
    /// (name/password self-edit) only opens from the "Edit Profile" button below, or the
    /// header menu's own Edit Profile item.
    /// </summary>
    internal sealed class ProfileViewForm : Form
    {
        private const int AvatarSize = 96;
        private byte[] _photoBytes;

        public ProfileViewForm()
        {
            Text = "My Profile";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            BackColor = UiTheme.PageBg;
            ClientSize = new Size(360, 460);

            try { _photoBytes = Session.User != null ? ProfilePhoto.Load(Session.User.Id) : null; }
            catch { _photoBytes = null; }

            var avatar = new Panel { Location = new Point((360 - AvatarSize) / 2, 24), Size = new Size(AvatarSize, AvatarSize) };
            avatar.Paint += (s, e) => AvatarPainter.Draw(e.Graphics, avatar.ClientRectangle, _photoBytes, Session.User?.FullName);
            Controls.Add(avatar);

            var name = new Label
            {
                Text = Session.User?.FullName ?? "Not signed in",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(16, 132), Size = new Size(328, 30)
            };
            Controls.Add(name);

            var role = new Label
            {
                Text = Session.User?.Role ?? "",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(16, 162), Size = new Size(328, 22)
            };
            Controls.Add(role);

            var card = new Panel
            {
                Location = new Point(24, 200),
                Size = new Size(312, 168),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(card);

            var row = 16;
            row = AddRow(card, "Username", Session.User?.Username ?? "—", row);
            row = AddRow(card, "Employee No.", "—", row, out Label empNoVal);
            row = AddRow(card, "Position", "—", row, out Label posVal);
            row = AddRow(card, "Employment Status", "—", row, out Label statusVal);
            row = AddRow(card, "Contact No.", "—", row, out Label contactVal);

            LoadBiodataSummary(empNoVal, posVal, statusVal, contactVal);

            var btnEdit = new Button
            {
                Text = "Edit Profile",
                Location = new Point(24, 384), Size = new Size(150, 40),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                BackColor = UiTheme.Accent, Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += (s, e) =>
            {
                using (var f = new StaffBiodataForm())
                    f.ShowDialog(this);
                name.Text = Session.User?.FullName ?? "Not signed in";
                try { _photoBytes = Session.User != null ? ProfilePhoto.Load(Session.User.Id) : null; }
                catch { _photoBytes = null; }
                avatar.Invalidate();
            };
            Controls.Add(btnEdit);

            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(186, 384), Size = new Size(150, 40),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 10F)
            };
            btnClose.FlatAppearance.BorderColor = UiTheme.CardLine;
            Controls.Add(btnClose);
            CancelButton = btnClose;

            UiTheme.Polish(this);
        }

        private static int AddRow(Panel card, string caption, string value, int y, out Label valueLabel)
        {
            card.Controls.Add(new Label
            {
                Text = caption, AutoSize = true, Location = new Point(16, y),
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Muted
            });
            valueLabel = new Label
            {
                Text = value, AutoSize = false, Location = new Point(150, y - 2), Size = new Size(146, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = UiTheme.Ink
            };
            card.Controls.Add(valueLabel);
            return y + 30;
        }

        private static int AddRow(Panel card, string caption, string value, int y)
        {
            return AddRow(card, caption, value, y, out _);
        }

        private void LoadBiodataSummary(Label empNo, Label position, Label status, Label contact)
        {
            if (Session.User == null) return;
            DataTable dt = Db.Pull(
                "SELECT employee_no, position, employment_status, contact_no FROM staff_biodata WHERE user_id=@id",
                new MySqlParameter("@id", Session.User.Id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            empNo.Text = S(r, "employee_no");
            position.Text = S(r, "position");
            status.Text = S(r, "employment_status");
            contact.Text = S(r, "contact_no");
        }

        private static string S(DataRow r, string col)
        {
            string v = r[col] == DBNull.Value ? "" : r[col].ToString();
            return v.Length == 0 ? "—" : v;
        }

    }
}
