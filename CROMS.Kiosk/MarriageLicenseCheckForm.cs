using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Shown only when the client picked "Marriage Registration". Registering a marriage in
    /// CROMS is recording a wedding that already happened under a licence the couple already
    /// obtained (Forms/MarriageEntryForm.cs's own workflow) — it is not the same thing as
    /// APPLYING for that licence (MARRIAGE_APP / Forms/MarriageLicenseForm.cs). A client who taps
    /// "Marriage Registration" without having applied yet has picked the wrong card, and the
    /// staff window would otherwise find that out only after the ticket is called.
    /// <para/>
    /// So the kiosk asks here, before the ticket is issued: has the licence already been applied
    /// for? "No" swaps the selection to Marriage Application (MARRIAGE_APP). "Yes" only tells the
    /// client to bring the paper to the window — the kiosk takes NO picture. Staff photograph the
    /// Certificate of Marriage and the Marriage License together with Mobile Capture (Form 97
    /// wizard), so the documents are captured once, by staff, on the office phone.
    /// </summary>
    public sealed partial class MarriageLicenseCheckForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Action _resetIdle;
        private bool? _answer;   // null = unanswered, true = "Yes, I have a licence", false = "No"
        // Set by every DELIBERATE close so OnFormClosing can tell navigation from a real quit.
        private bool _navigating;

        public MarriageLicenseCheckForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();

            _idle = new Timer { Interval = 1000 };
            int ticks = 0;
            _idle.Tick += (s, e) =>
            {
                if (++ticks < KioskCore.IdleSeconds) return;
                ticks = 0;
                _session.Reset();
                _navigating = true;
                DialogResult = DialogResult.Abort;
                Close();
            };
            _resetIdle = () => ticks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        private void MarriageLicenseCheckForm_Load(object sender, EventArgs e)
        {
            _answer = _session.HasMarriageLicense ? true : (bool?)null;
            ApplyAnswerStyle();
            CenterCard();
        }

        private void MarriageLicenseCheckForm_Resize(object sender, EventArgs e) => CenterCard();

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }

        private void No_Click(object sender, EventArgs e) { _answer = false; ApplyAnswerStyle(); }
        private void Yes_Click(object sender, EventArgs e) { _answer = true; ApplyAnswerStyle(); }

        private void Back_Click(object sender, EventArgs e)
        {
            _navigating = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Continue_Click(object sender, EventArgs e)
        {
            if (_answer == null)
            {
                MessageBox.Show("Please tell us whether you already applied for your Marriage License.",
                    "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _session.HasMarriageLicense = _answer == true;
            if (_answer == false)
            {
                // Wrong card for this client — route them to Marriage Application instead.
                _session.Selected.Remove("MARRIAGE_REG");
                if (!_session.Selected.Contains("MARRIAGE_APP")) _session.Selected.Add("MARRIAGE_APP");
            }

            _navigating = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ------------------------------------------------------------------ helpers
        private void ApplyAnswerStyle()
        {
            bool yes = _answer == true, no = _answer == false;
            _btnYes.BackColor = yes ? KioskCore.Accent : KioskCore.CardBg;
            _btnYes.ForeColor = yes ? System.Drawing.Color.White : KioskCore.Ink;
            _btnYes.FlatAppearance.BorderColor = yes ? KioskCore.Accent : KioskCore.Line;
            _btnNo.BackColor = no ? KioskCore.Accent : KioskCore.CardBg;
            _btnNo.ForeColor = no ? System.Drawing.Color.White : KioskCore.Ink;
            _btnNo.FlatAppearance.BorderColor = no ? KioskCore.Accent : KioskCore.Line;
            _lblHandOver.Visible = yes;
        }

        private void CenterCard()
        {
            _card.Location = new System.Drawing.Point(
                Math.Max(0, (ClientSize.Width - _card.Width) / 2),
                Math.Max(0, (ClientSize.Height - _card.Height) / 2));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            if (!_navigating && e.CloseReason == CloseReason.UserClosing) Environment.Exit(0);
            base.OnFormClosing(e);
        }
    }
}
