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
        private readonly Label _lblHandOver = new Label();
        private readonly Label _lblSubCap = new Label();
        private Button _btnSubOfficer, _btnSubHusband, _btnSubWife, _btnSubRep;
        private readonly Label _lblOrgCap = new Label();
        private readonly TextBox _txtOrg = new TextBox();
        private readonly Label _lblNameHint = new Label();

        private const int CardW = 760, CardH = 800;
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

            _lblHandOver.Text = "Please bring your Marriage License and Certificate of Marriage to the window. " +
                "The staff will take pictures of them for you.";
            _lblHandOver.Location = new Point(Pad, y);
            _lblHandOver.Size = new Size(FieldW, 60);
            _lblHandOver.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblHandOver.ForeColor = KioskCore.Accent;
            _lblHandOver.Visible = false;
            _card.Controls.Add(_lblHandOver);
            y += 70;

            // Who is handing in the Certificate of Marriage - asked only after "Yes".
            _lblSubCap.Text = "Who is submitting the Certificate of Marriage? *";
            _lblSubCap.Location = new Point(Pad, y);
            _lblSubCap.Size = new Size(FieldW, 26);
            _lblSubCap.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblSubCap.ForeColor = KioskCore.Ink;
            _card.Controls.Add(_lblSubCap);
            y += 32;

            int half = (FieldW - 12) / 2;
            _btnSubOfficer = OptionButton("Solemnizing Officer", Pad, y, half, 60);
            _btnSubHusband = OptionButton("Husband", Pad + half + 12, y, half, 60);
            y += 70;
            _btnSubWife = OptionButton("Wife", Pad, y, half, 60);
            _btnSubRep = OptionButton("Authorized Representative", Pad + half + 12, y, half, 60);
            y += 74;
            foreach (Button b in new[] { _btnSubOfficer, _btnSubHusband, _btnSubWife, _btnSubRep })
            {
                b.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
                b.Click += new EventHandler(Submitter_Click);
                _card.Controls.Add(b);
            }

            _lblOrgCap.Text = "Your office / organization (optional)";
            _lblOrgCap.Location = new Point(Pad, y);
            _lblOrgCap.Size = new Size(FieldW, 22);
            _lblOrgCap.Font = new Font("Segoe UI", 9.5F);
            _lblOrgCap.ForeColor = KioskCore.Muted;
            _card.Controls.Add(_lblOrgCap);
            _txtOrg.Location = new Point(Pad, y + 24);
            _txtOrg.Size = new Size(FieldW, 32);
            _txtOrg.Font = new Font("Segoe UI", 12F);
            _card.Controls.Add(_txtOrg);
            y += 66;

            _lblNameHint.Text = "On the next screen, type YOUR own name - the person submitting.";
            _lblNameHint.Location = new Point(Pad, y);
            _lblNameHint.Size = new Size(FieldW, 24);
            _lblNameHint.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            _lblNameHint.ForeColor = KioskCore.Muted;
            _card.Controls.Add(_lblNameHint);

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
