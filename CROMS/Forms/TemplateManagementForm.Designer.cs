using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class TemplateManagementForm
    {
        private FlowLayoutPanel _flow;
        private Label _lblEmpty;

        private void InitializeComponent()
        {
            Text = "Certificate Templates";
            BackColor = UiTheme.PageBg;
            AutoScroll = true;
            Font = new Font("Segoe UI", 9.5f);

            var title = new Label
            {
                Text = "Certificate Templates",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(24, 20),
            };
            var subtitle = new Label
            {
                Text = "Design how each certificate looks — logos, text, fields and lines — without touching code.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 52),
            };

            _lblEmpty = new Label
            {
                Text = "No certificate forms are registered for template editing yet.",
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 100),
                Visible = false,
            };

            _flow = new FlowLayoutPanel
            {
                Location = new Point(20, 84),
                Size = new Size(1160, 700),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = UiTheme.PageBg,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
            };

            Controls.Add(_flow);
            Controls.Add(_lblEmpty);
            Controls.Add(subtitle);
            Controls.Add(title);

            BuildCards();
        }
    }
}
