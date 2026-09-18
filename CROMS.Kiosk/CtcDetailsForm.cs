using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>Conditional kiosk step shown only for a Certified True Copy request.</summary>
    public sealed class CtcDetailsForm : Form
    {
        private readonly KioskSession _session;
        private readonly ComboBox _document = new ComboBox();
        private readonly TextBox _details = new TextBox();
        private readonly Panel _card;

        public CtcDetailsForm(KioskSession session)
        {
            _session = session;
            Text = "Certified True Copy Details";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(244, 246, 249);
            Font = new Font("Segoe UI", 10F);
            AutoScroll = true;

            var card = new Panel
            {
                Size = new Size(760, 540),
                BackColor = Color.White,
                Padding = new Padding(44),
                Anchor = AnchorStyles.None
            };
            _card = card;
            AutoScrollMinSize = card.Size;
            // Centered against the form's OWN ClientSize on Load/Resize, not a Screen.PrimaryScreen
            // snapshot taken here in the constructor before the form has ever been laid out - that
            // static math is what put the card off in a corner, cut off, on this screen/DPI.
            Load += (s, e) => CenterCard();
            Resize += (s, e) => CenterCard();

            var title = new Label
            {
                Text = "Certified True Copy Details",
                AutoSize = false,
                Location = new Point(44, 38),
                Size = new Size(670, 42),
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 36, 65)
            };
            var hint = new Label
            {
                Text = "Tell the records staff which civil registry document you need.",
                Location = new Point(47, 88),
                Size = new Size(650, 28),
                ForeColor = Color.FromArgb(91, 105, 128)
            };
            var docLabel = LabelAt("Document type *", 47, 145);
            _document.Location = new Point(47, 174);
            _document.Size = new Size(650, 38);
            _document.DropDownStyle = ComboBoxStyle.DropDownList;
            _document.Items.AddRange(new object[] { "Birth", "Marriage", "Death" });
            if (!string.IsNullOrWhiteSpace(_session.CtcDocumentType))
                _document.SelectedItem = _session.CtcDocumentType;

            var detailsLabel = LabelAt("Record details (name, registry number, year, or other helpful information)", 47, 240);
            _details.Location = new Point(47, 270);
            _details.Size = new Size(650, 110);
            _details.Multiline = true;
            _details.MaxLength = 150;
            _details.Text = _session.CtcDetails ?? string.Empty;

            var back = ActionButton("Back", Color.FromArgb(226, 232, 240), Color.FromArgb(19, 36, 65));
            back.Location = new Point(47, 430);
            back.Click += (s, e) => { Save(); DialogResult = DialogResult.Cancel; Close(); };
            var next = ActionButton("Continue", Color.FromArgb(29, 78, 216), Color.White);
            next.Location = new Point(497, 430);
            next.Click += (s, e) =>
            {
                if (_document.SelectedItem == null)
                {
                    MessageBox.Show("Please choose Birth, Marriage, or Death.", "Please check",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Save();
                DialogResult = DialogResult.OK;
                Close();
            };

            card.Controls.Add(title); card.Controls.Add(hint); card.Controls.Add(docLabel);
            card.Controls.Add(_document); card.Controls.Add(detailsLabel); card.Controls.Add(_details);
            card.Controls.Add(back); card.Controls.Add(next);
            Controls.Add(card);
        }

        private void CenterCard()
        {
            int x = Math.Max(0, (ClientSize.Width - _card.Width) / 2);
            int y = Math.Max(0, (ClientSize.Height - _card.Height) / 2);
            _card.Location = new Point(x, y);
        }

        private void Save()
        {
            _session.CtcDocumentType = _document.SelectedItem?.ToString();
            _session.CtcDetails = _details.Text.Trim();
        }

        private static Label LabelAt(string text, int x, int y) => new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(650, 25),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(19, 36, 65)
        };

        private static Button ActionButton(string text, Color back, Color fore)
        {
            var button = new Button
            {
                Text = text, Size = new Size(200, 46), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }
}
