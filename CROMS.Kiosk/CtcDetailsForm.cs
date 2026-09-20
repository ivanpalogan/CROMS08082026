using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Certified True Copy step — shown only when the client picked "Certified True Copy (CTC)".
    /// <para/>
    /// This screen used to ask for the document type and then ONE free-text box ("name, registry
    /// number, year, or other helpful information"). What reached the counter therefore depended
    /// entirely on what the client thought to type, and most of the time it was a name with no
    /// year — not enough to find a registry entry, so the clerk had to ask everything again with
    /// the client standing there. It now asks the office's own questions, one per field, the same
    /// way <see cref="BreqsDetailsForm"/> already does for a PSA copy: whose record, when and
    /// where, plus the parents for a birth or the spouse for a marriage. The registry number is
    /// asked FIRST because a client who has it turns the whole search into one lookup.
    /// <para/>
    /// Only the document type and the owner's first + last name are required. Everything else is
    /// optional on purpose — a grandchild asking for a 1962 birth record may genuinely not know
    /// the date, and refusing the request over it would just push them back to the paper queue.
    /// </summary>
    public sealed partial class CtcDetailsForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Action _resetIdle;
        // Set by every DELIBERATE close so OnFormClosing can tell navigation from a real quit
        // (a programmatic Close() also reports UserClosing — the trap recorded 2026-08-29).
        private bool _navigating;

        public CtcDetailsForm(KioskSession session)
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

        // Centred against the form's OWN ClientSize on Load/Resize, never a
        // Screen.PrimaryScreen snapshot taken in the constructor before the form has been
        // laid out — that static maths is what put the card off in a corner on this DPI.
        private void CtcDetailsForm_Load(object sender, EventArgs e) { LoadFromSession(); ApplyDocType(); CenterCard(); }

        private void CtcDetailsForm_Resize(object sender, EventArgs e) => CenterCard();

        private void Document_SelectedIndexChanged(object sender, EventArgs e) => ApplyDocType();

        private void Back_Click(object sender, EventArgs e)
        {
            SaveToSession();
            _navigating = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }


        /// <summary>
        /// A marriage has a second spouse and no parent block; a birth has parents and no spouse;
        /// a death has neither. Hiding the block that does not apply is the point of asking the
        /// document type first — a death certificate request should never show an empty
        /// "the other spouse" section for the client to wonder about.
        /// </summary>
        private void ApplyDocType()
        {
            string doc = _document.SelectedItem as string;
            bool marriage = doc == "Marriage", birth = doc == "Birth";

            // Each block member is a caption whose paired input hangs off its Tag (see Field),
            // so one pass hides the label AND the box under it.
            foreach (Control c in _spouseBlock) { c.Visible = marriage; if (c.Tag is Control i) i.Visible = marriage; }
            foreach (Control c in _parentBlock) { c.Visible = birth; if (c.Tag is Control i) i.Visible = birth; }

            _ownerCaption.Text = marriage ? "WHOSE RECORD — ONE SPOUSE" : birth ? "WHOSE RECORD — THE CHILD"
                : doc == "Death" ? "WHOSE RECORD — THE DECEASED" : "WHOSE RECORD";
            _eventCaption.Text = marriage ? "Date of marriage" : birth ? "Date of birth"
                : doc == "Death" ? "Date of death" : "Date of the event";
        }

        private void Continue_Click(object sender, EventArgs e)
        {
            if (_document.SelectedItem == null)
            {
                Warn("Please choose Birth, Marriage, or Death.");
                _document.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_ownerFirst.Text) || string.IsNullOrWhiteSpace(_ownerLast.Text))
            {
                Warn("Please enter the first and last name on the record you need a copy of.");
                (string.IsNullOrWhiteSpace(_ownerFirst.Text) ? _ownerFirst : _ownerLast).Focus();
                return;
            }
            SaveToSession();
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
            if (!string.IsNullOrWhiteSpace(_session.CtcDocumentType)) _document.SelectedItem = _session.CtcDocumentType;
            _copies.SelectedItem = Math.Max(1, Math.Min(10, _session.CtcCopies)).ToString();
            _purpose.Text = _session.CtcPurpose ?? "";
            _relationship.Text = _session.CtcRelationship ?? "";
            _registryNo.Text = _session.CtcRegistryNo ?? "";
            _ownerFirst.Text = _session.CtcOwnerFirst ?? "";
            _ownerMiddle.Text = _session.CtcOwnerMiddle ?? "";
            _ownerLast.Text = _session.CtcOwnerLast ?? "";
            _spouseFirst.Text = _session.CtcSpouseFirst ?? "";
            _spouseMiddle.Text = _session.CtcSpouseMiddle ?? "";
            _spouseLast.Text = _session.CtcSpouseLast ?? "";
            if (_session.CtcEventDate.HasValue) { _eventDate.Value = _session.CtcEventDate.Value; _eventDate.Checked = true; }
            else _eventDate.Checked = false;
            _city.Text = _session.CtcEventCity ?? "";
            _province.Text = _session.CtcEventProvince ?? "";
            _father.Text = _session.CtcFatherName ?? "";
            _mother.Text = _session.CtcMotherMaidenName ?? "";
            _remarks.Text = _session.CtcDetails ?? "";
        }

        private void SaveToSession()
        {
            _session.CtcDocumentType = _document.SelectedItem as string;
            int copies;
            _session.CtcCopies = int.TryParse(_copies.SelectedItem as string, out copies) ? copies : 1;
            _session.CtcPurpose = Trim(_purpose.Text);
            _session.CtcRelationship = Trim(_relationship.Text);
            _session.CtcRegistryNo = Trim(_registryNo.Text);
            _session.CtcOwnerFirst = Trim(_ownerFirst.Text);
            _session.CtcOwnerMiddle = Trim(_ownerMiddle.Text);
            _session.CtcOwnerLast = Trim(_ownerLast.Text);

            // A block that is not on screen must not contribute to the request — switching
            // Marriage to Death after typing a spouse would otherwise file a death record
            // with a spouse name nobody ever saw.
            bool marriage = _session.CtcDocumentType == "Marriage", birth = _session.CtcDocumentType == "Birth";
            _session.CtcSpouseFirst = marriage ? Trim(_spouseFirst.Text) : null;
            _session.CtcSpouseMiddle = marriage ? Trim(_spouseMiddle.Text) : null;
            _session.CtcSpouseLast = marriage ? Trim(_spouseLast.Text) : null;
            _session.CtcFatherName = birth ? Trim(_father.Text) : null;
            _session.CtcMotherMaidenName = birth ? Trim(_mother.Text) : null;

            _session.CtcEventDate = _eventDate.Checked ? _eventDate.Value.Date : (DateTime?)null;
            _session.CtcEventCity = Trim(_city.Text);
            _session.CtcEventProvince = Trim(_province.Text);
            _session.CtcDetails = Trim(_remarks.Text);
        }

        private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ------------------------------------------------------------------ helpers
        private void CenterCard()
        {
            _card.Location = new Point(
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
