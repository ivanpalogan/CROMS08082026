using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>Final read-only checkpoint before one queue number is generated.</summary>
    public sealed class ReviewRequestForm : Form
    {
        private readonly KioskSession _session;
        private readonly Button _confirm;

        public ReviewRequestForm(KioskSession session)
        {
            _session = session;
            Text = "Review Request";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(244, 246, 249);
            Font = new Font("Segoe UI", 10F);

            var card = new Panel
            {
                Size = new Size(860, 680), BackColor = Color.White, Padding = new Padding(44),
                AutoScroll = true
            };
            card.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - card.Width) / 2,
                Math.Max(20, (Screen.PrimaryScreen.WorkingArea.Height - card.Height) / 2));
            card.Anchor = AnchorStyles.None;

            card.Controls.Add(new Label
            {
                Text = "Review Your Request", Location = new Point(44, 30), Size = new Size(760, 44),
                Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Color.FromArgb(19, 36, 65)
            });
            card.Controls.Add(new Label
            {
                Text = "One queue number will cover all services below.", Location = new Point(47, 76),
                Size = new Size(740, 27), ForeColor = Color.FromArgb(91, 105, 128)
            });

            var review = new TextBox
            {
                Location = new Point(47, 120), Size = new Size(760, 420), Multiline = true,
                ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11F), Text = BuildSummary()
            };

            var back = Button("Back", Color.FromArgb(226, 232, 240), Color.FromArgb(19, 36, 65), 47);
            back.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            var add = Button("Add Another Transaction", Color.FromArgb(234, 241, 254), Color.FromArgb(29, 78, 216), 260);
            add.Size = new Size(260, 46);
            add.Click += (s, e) => { DialogResult = DialogResult.Retry; Close(); };
            _confirm = Button("Confirm & Print Ticket", Color.FromArgb(22, 163, 74), Color.White, 574);
            _confirm.Size = new Size(233, 46);
            _confirm.Click += Confirm;

            card.Controls.Add(review); card.Controls.Add(back); card.Controls.Add(add); card.Controls.Add(_confirm);
            Controls.Add(card);
        }

        private string BuildSummary()
        {
            var b = new StringBuilder();
            b.AppendLine("CLIENT");
            b.AppendLine(KioskCore.FullName(_session));
            if (!string.IsNullOrWhiteSpace(_session.Contact)) b.AppendLine("Contact: " + _session.Contact);
            if (_session.HasMarriage) b.AppendLine("Spouse: " + KioskCore.FullName2(_session));
            b.AppendLine();
            b.AppendLine("SERVICES");
            int i = 1;
            foreach (string code in _session.Selected)
                b.AppendLine(i++ + ". " + KioskCore.Find(code).Label);

            if (_session.HasCtc)
            {
                b.AppendLine(); b.AppendLine("CERTIFIED TRUE COPY");
                b.AppendLine("Document: " + _session.CtcDocumentType);
                if (!string.IsNullOrWhiteSpace(_session.CtcDetails)) b.AppendLine("Details: " + _session.CtcDetails);
            }
            if (_session.HasBreqs)
            {
                b.AppendLine(); b.AppendLine("PSA COPY (BREQS)");
                b.AppendLine("Document: " + _session.BreqsDocType + " · Copies: " + _session.BreqsCopies);
                b.AppendLine("Document owner: " + Join(_session.OwnerFirst, _session.OwnerMiddle, _session.OwnerLast));
            }
            if (_session.HasClaim && !string.IsNullOrWhiteSpace(_session.ClaimTicketEntry))
            {
                b.AppendLine(); b.AppendLine("RELEASE & CLAIM");
                b.AppendLine("Previous queue number: " + _session.ClaimTicketEntry);
            }
            b.AppendLine();
            b.AppendLine("Priority lane: " + KioskCore.PriorityValue(_session));
            return b.ToString();
        }

        private void Confirm(object sender, EventArgs e)
        {
            _confirm.Enabled = false;
            try
            {
                if (!KioskCore.Submit(_session, out string error))
                {
                    MessageBox.Show(error, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _confirm.Enabled = true;
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, your request could not be submitted:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _confirm.Enabled = true;
            }
        }

        private static string Join(params string[] parts) => string.Join(" ", Array.FindAll(parts,
            p => !string.IsNullOrWhiteSpace(p)));

        private static Button Button(string text, Color back, Color fore, int x)
        {
            var button = new Button
            {
                Text = text, Location = new Point(x, 575), Size = new Size(190, 46),
                FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }
}
