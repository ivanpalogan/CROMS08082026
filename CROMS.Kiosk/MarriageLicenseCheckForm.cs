using System;
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
    /// for? "No" swaps the selection to Marriage Application (MARRIAGE_APP) so the client is
    /// routed to the step they actually need — nothing about the visit is lost, they just were
    /// on the wrong card. "Yes" asks for the licence number, which travels with the ticket the
    /// same way <see cref="KioskSession.CtcRegistryNo"/> already does for a CTC request.
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
            LoadFromSession();
            CenterCard();
        }

        private void MarriageLicenseCheckForm_Resize(object sender, EventArgs e) => CenterCard();

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }

        private void No_Click(object sender, EventArgs e) => SetAnswer(false);
        private void Yes_Click(object sender, EventArgs e) => SetAnswer(true);

        private void SetAnswer(bool yes)
        {
            _answer = yes;
            ApplyAnswerStyle();
            _licenseNo.Visible = yes;
            _licenseCaption.Visible = yes;
            if (yes) _licenseNo.Focus();
        }

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
                Warn("Please tell us whether you already applied for your Marriage License.");
                return;
            }

            if (_answer == true)
            {
                if (string.IsNullOrWhiteSpace(_licenseNo.Text))
                {
                    Warn("Please enter your Marriage License Number.");
                    _licenseNo.Focus();
                    return;
                }
                _session.MarriageLicenseNo = _licenseNo.Text.Trim();
            }
            else
            {
                // Wrong card for this client — route them to Marriage Application instead.
                _session.MarriageLicenseNo = null;
                _session.Selected.Remove("MARRIAGE_REG");
                if (!_session.Selected.Contains("MARRIAGE_APP")) _session.Selected.Add("MARRIAGE_APP");
            }

            _navigating = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ------------------------------------------------------- session <-> fields
        private void LoadFromSession()
        {
            if (!string.IsNullOrWhiteSpace(_session.MarriageLicenseNo))
            {
                _licenseNo.Text = _session.MarriageLicenseNo;
                SetAnswer(true);
            }
            else
            {
                _answer = null;
                ApplyAnswerStyle();
                _licenseNo.Visible = false;
                _licenseCaption.Visible = false;
            }
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
