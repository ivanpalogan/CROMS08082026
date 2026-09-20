using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Final read-only checkpoint before one queue number is generated. Restyled onto the same
    /// system every other kiosk step already uses (KioskCore palette, RoundPanel card, StepIndicator,
    /// KioskButtons) instead of a plain white Panel with the summary dumped into a readonly TextBox -
    /// that plain version predates the rest of the kiosk's design pass and had drifted badly, right
    /// down to centering the card off a Screen.PrimaryScreen snapshot taken before the form was ever
    /// laid out (the same bug already fixed on CtcDetailsForm's card - see its CenterCard comment).
    /// </summary>
    public sealed partial class ReviewRequestForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Action _resetIdle;
        // Set by every deliberate close (Back / Add Another / idle / submit) so OnFormClosing can
        // tell our own navigation apart from the operator really quitting the kiosk - same trap and
        // same fix already established across the other kiosk step forms.
        private bool _navigating;

        public ReviewRequestForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();

            BuildRows();

            // ---------------------------------------------------------------- idle timeout
            // A client can walk away right here (their name is already on screen) exactly as
            // easily as on the personal-info step - this screen previously had no idle guard at
            // all, unlike every other step that carries the client's details.
            _idle = new Timer { Interval = 1000 };
            int idleTicks = 0;
            _idle.Tick += (s, e) =>
            {
                idleTicks++;
                if (idleTicks >= KioskCore.IdleSeconds)
                {
                    idleTicks = 0;
                    _session.Reset();
                    _navigating = true;
                    DialogResult = DialogResult.Abort;
                    Close();
                }
            };
            _resetIdle = () => idleTicks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        private void SizeStepIndicator() => _stepInd.Width = Math.Max(200, _header.ClientSize.Width - 80);

        private void PlaceFooterButtons()
        {
            _confirm.Location = new Point(_footer.Width - 40 - _confirm.Width, 18);
            _addAnother.Location = new Point(_confirm.Left - 16 - _addAnother.Width, 18);
        }

        private void Header_Resize(object sender, EventArgs e) => SizeStepIndicator();

        private void Footer_Resize(object sender, EventArgs e) => PlaceFooterButtons();

        private void Host_Resize(object sender, EventArgs e) => LayoutCard();

        private void ReviewRequestForm_Load(object sender, EventArgs e) { LayoutCard(); PlaceFooterButtons(); SizeStepIndicator(); }

        private void Back_Click(object sender, EventArgs e) { _navigating = true; DialogResult = DialogResult.Cancel; Close(); }

        private void AddAnother_Click(object sender, EventArgs e) { _navigating = true; DialogResult = DialogResult.Retry; Close(); }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A)
                _resetIdle?.Invoke();
            return false;
        }

        /// <summary>Centers the card against the host's OWN current size, recomputed on every
        /// resize/load rather than off a Screen.PrimaryScreen snapshot taken in the constructor
        /// before the form has ever been laid out - that static math is what put the old card off
        /// in a corner, cut off, whenever the running screen/DPI didn't match the assumption.</summary>
        private void LayoutCard()
        {
            int w = Math.Max(560, Math.Min(920, _host.ClientSize.Width - 64));
            int h = Math.Max(360, _host.ClientSize.Height - 48);
            _card.Size = new Size(w, h);
            _card.Location = new Point((_host.ClientSize.Width - w) / 2, Math.Max(0, (_host.ClientSize.Height - h) / 2));
        }

        // ------------------------------------------------------------- content
        private void BuildRows()
        {
            _flow.Controls.Clear();
            bool first = true;

            void Section(string text)
            {
                _flow.Controls.Add(new Label
                {
                    Text = text, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = KioskCore.Accent, Margin = new Padding(0, first ? 0 : 18, 0, 8)
                });
                first = false;
            }

            void Row(string cap, string val, Color? valColor = null)
            {
                var p = new Panel { Size = new Size(800, 46), Margin = new Padding(0, 0, 0, 2) };
                p.Controls.Add(new Label
                {
                    Text = cap, Location = new Point(0, 0), Size = new Size(800, 16),
                    Font = new Font("Segoe UI", 8.5F), ForeColor = KioskCore.Muted
                });
                p.Controls.Add(new Label
                {
                    Text = val, Location = new Point(0, 17), Size = new Size(800, 26), AutoEllipsis = true,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = valColor ?? KioskCore.Ink
                });
                _flow.Controls.Add(p);
            }

            Section("CLIENT");
            Row("Name", KioskCore.FullName(_session));
            if (!string.IsNullOrWhiteSpace(_session.Contact)) Row("Contact", _session.Contact);
            if (_session.HasMarriage) Row("Spouse", KioskCore.FullName2(_session));

            Section("SERVICES SELECTED");
            int i = 1;
            foreach (string code in _session.Selected)
                Row((i++).ToString(), KioskCore.Find(code).Label);

            if (_session.HasCtc)
            {
                Section("CERTIFIED TRUE COPY");
                Row("Document", _session.CtcDocumentType + "  ·  Copies: " + Math.Max(1, _session.CtcCopies));
                Row("Record owner", Join(_session.CtcOwnerFirst, _session.CtcOwnerMiddle, _session.CtcOwnerLast));
                if (!string.IsNullOrWhiteSpace(_session.CtcSpouseLast))
                    Row("Other spouse", Join(_session.CtcSpouseFirst, _session.CtcSpouseMiddle, _session.CtcSpouseLast));
                if (!string.IsNullOrWhiteSpace(_session.CtcRegistryNo)) Row("Registry number", _session.CtcRegistryNo);
                if (_session.CtcEventDate.HasValue) Row("Date of event", _session.CtcEventDate.Value.ToString("MMMM d, yyyy"));
                string place = Join(_session.CtcEventCity, _session.CtcEventProvince);
                if (!string.IsNullOrWhiteSpace(place)) Row("Place of event", place);
                if (!string.IsNullOrWhiteSpace(_session.CtcFatherName)) Row("Father", _session.CtcFatherName);
                if (!string.IsNullOrWhiteSpace(_session.CtcMotherMaidenName)) Row("Mother (maiden)", _session.CtcMotherMaidenName);
                if (!string.IsNullOrWhiteSpace(_session.CtcPurpose)) Row("Purpose", _session.CtcPurpose);
                if (!string.IsNullOrWhiteSpace(_session.CtcRelationship)) Row("Requester is the", _session.CtcRelationship);
                if (!string.IsNullOrWhiteSpace(_session.CtcDetails)) Row("Notes", _session.CtcDetails);
            }
            if (_session.HasBreqs)
            {
                Section("PSA COPY (BREQS)");
                Row("Document", _session.BreqsDocType + "  ·  Copies: " + _session.BreqsCopies);
                Row("Document owner", Join(_session.OwnerFirst, _session.OwnerMiddle, _session.OwnerLast));
            }
            if (_session.HasClaim && !string.IsNullOrWhiteSpace(_session.ClaimTicketEntry))
            {
                Section("RELEASE & CLAIM");
                Row("Previous queue number", _session.ClaimTicketEntry);
            }

            Section("PRIORITY LANE");
            string lane = KioskCore.PriorityValue(_session);
            Row("Lane", lane, lane == "Regular" ? KioskCore.Muted : KioskCore.Accent);
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
                _navigating = true;
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            if (!_navigating && e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
            base.OnFormClosing(e);
        }

        private static string Join(params string[] parts) => string.Join(" ", Array.FindAll(parts,
            p => !string.IsNullOrWhiteSpace(p)));
    }
}