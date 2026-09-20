using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    public sealed partial class ReviewRequestForm
    {
        private Panel _host;
        private RoundPanel _card;
        private FlowLayoutPanel _flow;
        private Button _confirm;
        private Panel _header;
        private Label _title;
        private StepIndicator _stepInd;
        private Panel _footer;
        private Panel _footerDivider;
        private Button _back;
        private Button _addAnother;
        private Label _hint;
        private Panel _scroll;

        private void InitializeComponent()
        {
            Text = "Review Request";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = KioskCore.Bg;
            Font = new Font("Segoe UI", 10F);

            // ---------------------------------------------------------------- header
            _header = new Panel { Dock = DockStyle.Top, Height = 128, BackColor = KioskCore.Bg };
            _title = new Label
            {
                Text = "Review Your Request", AutoSize = true, Location = new Point(40, 18),
                Font = new Font("Segoe UI", 30F, FontStyle.Bold), ForeColor = KioskCore.Ink
            };
            _stepInd = new StepIndicator
            {
                Steps = _session.StepLabels(), Location = new Point(40, 74), Size = new Size(700, 52)
            };
            _stepInd.SetStep(_stepInd.Steps.Length - 1);
            _header.Controls.Add(_title);
            _header.Controls.Add(_stepInd);
            // A fixed Width truncated the capsule's last label ("Review") whenever the step
            // count/labels (which vary with the session - up to 5 with BREQS+CTC both picked)
            // needed more room than a guessed constant gave them; StepIndicator itself clamps
            // its capsule to Width and silently clips. Recomputed off the header's OWN current
            // size on Load/Resize instead, same fix already applied to the card below.
            _header.Resize += new EventHandler(Header_Resize);

            // ---------------------------------------------------------------- footer
            _footer = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.White };
            _footerDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = KioskCore.Line };

            _back = new Button { Text = "Back", Size = new Size(190, 64), Location = new Point(40, 18) };
            _back.Click += new EventHandler(Back_Click);

            _addAnother = new Button
            {
                Text = "Add Another Transaction", Size = new Size(280, 64), Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _addAnother.Click += new EventHandler(AddAnother_Click);

            _confirm = new Button
            {
                Text = "Confirm && Print Ticket", Size = new Size(260, 64), Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _confirm.Click += new EventHandler(Confirm);

            _footer.Controls.Add(_footerDivider);
            _footer.Controls.Add(_back);
            _footer.Controls.Add(_addAnother);
            _footer.Controls.Add(_confirm);
            KioskButtons.Style(_back, KioskButtonKind.Secondary, KioskCore.IconArrowLeft, backdrop: _footer.BackColor);
            KioskButtons.Style(_addAnother, KioskButtonKind.Secondary, backdrop: _footer.BackColor);
            KioskButtons.Style(_confirm, KioskButtonKind.Success, KioskCore.IconPrinter, iconRight: true, backdrop: _footer.BackColor);
            _footer.Resize += new EventHandler(Footer_Resize);

            // ---------------------------------------------------------------- card
            _card = new RoundPanel { Radius = 16, Fill = Color.White, BorderColor = KioskCore.Line, Shadow = 6 };
            _hint = new Label
            {
                Text = "One queue number will cover all services below.", Dock = DockStyle.Top, Height = 32,
                Font = new Font("Segoe UI", 10.5F), ForeColor = KioskCore.Muted
            };
            _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            _flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.White
            };
            _scroll.Controls.Add(_flow);
            _card.Padding = new Padding(36, 28, 36, 24);
            _card.Controls.Add(_scroll);
            _card.Controls.Add(_hint);

            _host = new Panel { Dock = DockStyle.Fill, BackColor = KioskCore.Bg };
            _host.Controls.Add(_card);
            _host.Resize += new EventHandler(Host_Resize);

            Controls.Add(_host);
            Controls.Add(_footer);
            Controls.Add(_header);

            Load += new EventHandler(ReviewRequestForm_Load);
        }
    }
}
