using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    public sealed partial class MarriageLicenseCheckForm
    {
        private Panel _card;
        private Button _back, _next;
        private Button _btnNo, _btnYes;
        private readonly Label _photoCaption = new Label();
        private readonly PictureBox _picLicense = new PictureBox();
        private readonly Label _lblCamStatus = new Label();
        private Button _btnCapture;

        private const int CardW = 760, CardH = 860;
        private const int Pad = 44, FieldW = 672;

        private void InitializeComponent()
        {
            Text = "Marriage License";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = KioskCore.Bg;
            Font = new Font("Segoe UI", 10F);
            AutoScroll = true;

            _card = new Panel
            {
                Size = new Size(CardW, CardH),
                BackColor = KioskCore.CardBg,
                Anchor = AnchorStyles.None,
            };
            AutoScrollMinSize = _card.Size;
            Load += new EventHandler(MarriageLicenseCheckForm_Load);
            Resize += new EventHandler(MarriageLicenseCheckForm_Resize);

            BuildCard();
            Controls.Add(_card);
        }

        // ------------------------------------------------------------------ layout
        private void BuildCard()
        {
            int y = 34;
            _card.Controls.Add(Title("Marriage License", Pad, y, 24F, KioskCore.Ink)); y += 42;
            _card.Controls.Add(Title(
                "Marriage Registration records a wedding that has already taken place under a " +
                "Marriage License. Have you already applied for and obtained your license?",
                Pad, y, 10.5F, KioskCore.Muted, 2)); y += 66;

            _btnNo = OptionButton("No — I still need to apply", Pad, y, FieldW, 84);
            _btnNo.Click += new EventHandler(No_Click);
            _card.Controls.Add(_btnNo);
            y += 96;

            _btnYes = OptionButton("Yes — I already have my Marriage License", Pad, y, FieldW, 84);
            _btnYes.Click += new EventHandler(Yes_Click);
            _card.Controls.Add(_btnYes);
            y += 100;

            _photoCaption.Text = "Take a photo of your Marriage License *";
            _photoCaption.Location = new Point(Pad, y);
            _photoCaption.Size = new Size(FieldW, 22);
            _photoCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _photoCaption.ForeColor = KioskCore.Ink;
            _photoCaption.Visible = false;
            _card.Controls.Add(_photoCaption);
            y += 26;

            _picLicense.Location = new Point(Pad, y);
            _picLicense.Size = new Size(FieldW, 260);
            _picLicense.SizeMode = PictureBoxSizeMode.Zoom;
            _picLicense.BackColor = Color.FromArgb(30, 34, 40);
            _picLicense.Visible = false;
            _card.Controls.Add(_picLicense);
            y += 272;

            _lblCamStatus.Text = "";
            _lblCamStatus.Location = new Point(Pad, y);
            _lblCamStatus.Size = new Size(FieldW, 20);
            _lblCamStatus.Font = new Font("Segoe UI", 9F);
            _lblCamStatus.ForeColor = KioskCore.Muted;
            _lblCamStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblCamStatus.Visible = false;
            _card.Controls.Add(_lblCamStatus);
            y += 30;

            _btnCapture = ActionButton("📷 Capture Photo", KioskCore.Accent, Color.White);
            _btnCapture.Size = new Size(280, 48);
            _btnCapture.Location = new Point(Pad + (FieldW - 280) / 2, y);
            _btnCapture.Click += new EventHandler(Capture_Click);
            _btnCapture.Visible = false;
            _card.Controls.Add(_btnCapture);

            _back = ActionButton("Back", KioskCore.Line, KioskCore.Ink);
            _back.Location = new Point(Pad, CardH - 86);
            _back.Click += new EventHandler(Back_Click);

            _next = ActionButton("Continue", KioskCore.Accent, Color.White);
            _next.Location = new Point(CardW - Pad - 220, CardH - 86);
            _next.Click += new EventHandler(Continue_Click);

            _card.Controls.Add(_back);
            _card.Controls.Add(_next);
        }

        private static Button OptionButton(string text, int x, int y, int width, int height)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = KioskCore.CardBg,
                ForeColor = KioskCore.Ink,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
            };
            b.FlatAppearance.BorderSize = 2;
            b.FlatAppearance.BorderColor = KioskCore.Line;
            return b;
        }

        private static Label Title(string text, int x, int y, float size, Color color, int lines = 1) => new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(FieldW, lines > 1 ? 44 : (size > 14 ? 40 : 30)),
            Font = new Font("Segoe UI", size, size > 14 ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color,
        };

        private static Button ActionButton(string text, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(220, 52),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
