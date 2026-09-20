using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    internal sealed partial class TicketPrepForm
    {
        private Button _btnCall;
        private Label _lblProgress;

        private void InitializeComponent()
        {
            Text = "Prepare — " + _ticketCode;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ClientSize = new Size(520, 468);
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9.75F);

            // ---- header -------------------------------------------------------
            Controls.Add(new Label
            {
                Text = _ticketCode,
                Font = new Font("Consolas", 30F, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                Location = new Point(28, 20),
                AutoSize = true
            });
            Controls.Add(new Label
            {
                Text = "accepted at " + _windowName,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Location = new Point(30, 74),
                AutoSize = true
            });
            Controls.Add(new Label
            {
                Text = "Get these ready BEFORE calling the client, so they aren't left waiting\n" +
                       "at the counter while you look.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                Location = new Point(30, 98),
                Size = new Size(460, 36)
            });

            // ---- checklist card ----------------------------------------------
            var card = new Panel
            {
                Location = new Point(28, 144),
                Size = new Size(464, 232),
                BackColor = Color.White
            };
            PaintAsCard(card);
            Controls.Add(card);

            foreach (string text in BuildChecklist(_ticketId))
                AddItem(card, text);

            _lblProgress = new Label
            {
                Location = new Point(30, 386),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiTheme.Muted
            };
            Controls.Add(_lblProgress);

            // ---- footer -------------------------------------------------------
            var later = new Button
            {
                Text = "Not yet",
                Location = new Point(28, 412),
                Size = new Size(120, 40),
                BackColor = Color.White,
                ForeColor = UiTheme.Muted,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F)
            };
            later.FlatAppearance.BorderColor = UiTheme.CardLine;
            later.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(later);

            _btnCall = new Button
            {
                Text = "Call Client",
                Location = new Point(292, 412),
                Size = new Size(200, 40),
                BackColor = UiTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            _btnCall.FlatAppearance.BorderSize = 0;
            _btnCall.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(_btnCall);

            AcceptButton = _btnCall;
            UiTheme.PolishButtons(this);
            UpdateProgress();
        }
    }
}
