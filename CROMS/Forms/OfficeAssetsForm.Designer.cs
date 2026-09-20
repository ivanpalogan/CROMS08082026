using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    partial class OfficeAssetsForm
    {
        // ---- logo / stamp -------------------------------------------------------
        private readonly ComboBox _cboScope = new ComboBox();
        private readonly PictureBox _picLogo = new PictureBox();
        private readonly PictureBox _picStamp = new PictureBox();
        private readonly Label _lblLogoInfo = new Label();
        private readonly Label _lblStampInfo = new Label();

        // ---- office profile -----------------------------------------------------
        private readonly TextBox _txtOffice = new TextBox();
        private readonly TextBox _txtMunicipality = new TextBox();
        private readonly TextBox _txtProvince = new TextBox();
        private readonly TextBox _txtRegistrar = new TextBox();
        private readonly TextBox _txtTitle = new TextBox();

        private readonly Label _lblStatus = new Label();

        private void InitializeComponent()
        {
            Text = "Logo, Stamp and Office Details";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 640);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.PageBg;

            BuildUi();
            LoadScopes();
            LoadProfile();
            LoadAssets();
            UiTheme.Polish(this);
        }

        // ===================================================================
        // Layout
        // ===================================================================

        private void BuildUi()
        {
            var title = new Label
            {
                Text = "Logo, Stamp and Office Details",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var sub = new Label
            {
                Text = "These appear in the header and stamp area of every printed " +
                       "certificate and Crystal report.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(22, 44)
            };
            Controls.Add(title);
            Controls.Add(sub);

            // ---- scope ----
            var lblScope = Cap("Applies to", 20, 78);
            _cboScope.SetBounds(120, 74, 380, 24);
            _cboScope.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboScope.SelectedIndexChanged += Scope_Changed;
            var hintScope = new Label
            {
                Text = "Choose a specific form only when it needs its own stamp.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = UiTheme.Faint,
                AutoSize = true,
                Location = new Point(510, 78)
            };
            Controls.Add(lblScope);
            Controls.Add(_cboScope);
            Controls.Add(hintScope);

            // ---- logo box ----
            Controls.Add(Group("LOGO  (office seal, printed in the header)", 20, 112, 350, 210));
            Frame(_picLogo, 34, 140);
            Controls.Add(_picLogo);
            _lblLogoInfo.SetBounds(34, 250, 320, 16);
            Style(_lblLogoInfo);
            Controls.Add(_lblLogoInfo);
            Controls.Add(Btn("Choose Logo…", 34, 272, 120, ChooseLogo_Click));
            Controls.Add(Btn("Save Logo", 160, 272, 100, SaveLogo_Click));
            Controls.Add(Btn("Remove", 266, 272, 88, RemoveLogo_Click));

            // ---- stamp box ----
            Controls.Add(Group("STAMP  (applied to issued copies)", 390, 112, 350, 210));
            Frame(_picStamp, 404, 140);
            Controls.Add(_picStamp);
            _lblStampInfo.SetBounds(404, 250, 320, 16);
            Style(_lblStampInfo);
            Controls.Add(_lblStampInfo);
            Controls.Add(Btn("Choose Stamp…", 404, 272, 124, ChooseStamp_Click));
            Controls.Add(Btn("Save Stamp", 534, 272, 104, SaveStamp_Click));
            Controls.Add(Btn("Remove", 644, 272, 88, RemoveStamp_Click));

            // ---- office profile ----
            Controls.Add(Group("OFFICE DETAILS  (header and signature block)", 20, 340, 720, 210));
            Field("Office name", _txtOffice, 34, 372, 400);
            Field("City / Municipality", _txtMunicipality, 34, 404, 220);
            Field("Province", _txtProvince, 430, 404, 220, labelLeft: 320);
            Field("Registrar name", _txtRegistrar, 34, 436, 400);
            Field("Registrar title", _txtTitle, 34, 468, 400);
            Controls.Add(Btn("Save Office Details", 34, 504, 170, SaveProfile_Click));

            _lblStatus.SetBounds(20, 566, 590, 34);
            _lblStatus.Font = new Font("Segoe UI", 9f);
            _lblStatus.ForeColor = UiTheme.Muted;
            Controls.Add(_lblStatus);

            var close = Btn("Close", 640, 570, 100, (s, e) => Close());
            close.BackColor = UiTheme.Surface;
            Controls.Add(close);
        }

        private static void Style(Label l)
        {
            l.Font = new Font("Segoe UI", 8.5f);
            l.ForeColor = UiTheme.Muted;
            l.AutoEllipsis = true;
        }

        private static Label Cap(string text, int x, int y) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(x, y)
        };

        private static GroupBox Group(string text, int x, int y, int w, int h) => new GroupBox
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = UiTheme.Muted,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.Transparent
        };

        private static void Frame(PictureBox p, int x, int y)
        {
            p.SetBounds(x, y, 320, 104);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BackColor = UiTheme.Surface;
            p.BorderStyle = BorderStyle.FixedSingle;
        }

        private Button Btn(string text, int x, int y, int w, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            b.Click += onClick;
            return b;
        }

        private void Field(string label, TextBox box, int x, int y, int w, int labelLeft = 0)
        {
            Controls.Add(Cap(label, labelLeft > 0 ? labelLeft : x, y + 3));
            box.SetBounds(labelLeft > 0 ? x : x + 130, y, w, 24);
            Controls.Add(box);
        }
    }
}
