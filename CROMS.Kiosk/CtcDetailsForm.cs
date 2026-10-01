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
            OthersBox.AttachInline(_purpose, 80);
            AutoCaps.Attach(_ownerFirst, _ownerMiddle, _ownerLast, _ownerSuffix,
                _spouseFirst, _spouseMiddle, _spouseLast, _spouseSuffix);

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

        // Sized and centred against the form's OWN ClientSize on Load/Resize, never a
        // Screen.PrimaryScreen snapshot taken in the constructor before the form has been
        // laid out — that static maths is what put the card off in a corner on this DPI.
        private void CtcDetailsForm_Load(object sender, EventArgs e) { LoadFromSession(); ApplyDocType(); SizeCard(); }

        private void CtcDetailsForm_Resize(object sender, EventArgs e) => SizeCard();

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
        /// The fields follow the document. A birth or a death asks for ONE person and the event;
        /// a marriage asks for the husband and the wife and the event. No parent information is
        /// asked at the kiosk - staff verify anything further at the counter. Hiding the wife's
        /// row is the point of asking the document type first: a birth request should never
        /// show a "wife" row for the client to wonder about.
        /// </summary>
        private void ApplyDocType()
        {
            string doc = _document.SelectedItem as string;
            bool marriage = doc == "Marriage", birth = doc == "Birth", death = doc == "Death";

            foreach (Control c in _spouseBlock) c.Visible = marriage;

            string who = marriage ? "Husband's " : "";
            _ownerFirstCap.Text = who + (marriage ? "first name *" : "First name *");
            _ownerMiddleCap.Text = who + (marriage ? "middle name" : "Middle name");
            _ownerLastCap.Text = who + (marriage ? "last name *" : "Last name *");

            _ownerCaption.Text = marriage ? "WHOSE RECORD — THE COUPLE" : birth ? "WHOSE RECORD — THE CHILD"
                : death ? "WHOSE RECORD — THE DECEASED" : "WHOSE RECORD";
            string ev = marriage ? "marriage" : birth ? "birth" : death ? "death" : null;
            _eventCaption.Text = ev == null ? "Date of the event" : "Date of " + ev;
            _provinceCaption.Text = ev == null ? "Province" : "Province of " + ev;
            _cityCaption.Text = ev == null ? "City / Municipality" : "City / Municipality of " + ev;
            SizeCard();
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
                Warn(_document.SelectedItem as string == "Marriage"
                    ? "Please enter the husband's first and last name."
                    : "Please enter the first and last name on the record you need a copy of.");
                (string.IsNullOrWhiteSpace(_ownerFirst.Text) ? _ownerFirst : _ownerLast).Focus();
                return;
            }
            if (_document.SelectedItem as string == "Marriage"
                && (string.IsNullOrWhiteSpace(_spouseFirst.Text) || string.IsNullOrWhiteSpace(_spouseLast.Text)))
            {
                Warn("Please enter the wife's first and last name.");
                (string.IsNullOrWhiteSpace(_spouseFirst.Text) ? _spouseFirst : _spouseLast).Focus();
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
            OthersBox.SetValue(_purpose, _session.CtcPurpose);
            _relationship.Text = _session.CtcRelationship ?? "";
            _registryNo.Text = _session.CtcRegistryNo ?? "";
            _ownerFirst.Text = _session.CtcOwnerFirst ?? "";
            _ownerMiddle.Text = _session.CtcOwnerMiddle ?? "";
            _ownerLast.Text = _session.CtcOwnerLast ?? "";
            _ownerSuffix.Text = _session.CtcOwnerSuffix ?? "";
            _spouseFirst.Text = _session.CtcSpouseFirst ?? "";
            _spouseMiddle.Text = _session.CtcSpouseMiddle ?? "";
            _spouseLast.Text = _session.CtcSpouseLast ?? "";
            _spouseSuffix.Text = _session.CtcSpouseSuffix ?? "";
            if (_session.CtcEventDate.HasValue) { _eventDate.Value = _session.CtcEventDate.Value; _eventDate.Checked = true; }
            else _eventDate.Checked = false;
            _city.Text = _session.CtcEventCity ?? "";
            _province.Text = _session.CtcEventProvince ?? "";
            _remarks.Text = _session.CtcDetails ?? "";
        }

        private void SaveToSession()
        {
            _session.CtcDocumentType = _document.SelectedItem as string;
            int copies;
            _session.CtcCopies = int.TryParse(_copies.SelectedItem as string, out copies) ? copies : 1;
            _session.CtcPurpose = Trim(OthersBox.Value(_purpose));
            _session.CtcRelationship = Trim(_relationship.Text);
            _session.CtcRegistryNo = Trim(_registryNo.Text);
            _session.CtcOwnerFirst = Trim(_ownerFirst.Text);
            _session.CtcOwnerMiddle = Trim(_ownerMiddle.Text);
            _session.CtcOwnerLast = Trim(_ownerLast.Text);
            _session.CtcOwnerSuffix = Trim(_ownerSuffix.Text);

            // A block that is not on screen must not contribute to the request — switching
            // Marriage to Death after typing a spouse would otherwise file a death record
            // with a spouse name nobody ever saw.
            bool marriage = _session.CtcDocumentType == "Marriage";
            _session.CtcSpouseFirst = marriage ? Trim(_spouseFirst.Text) : null;
            _session.CtcSpouseMiddle = marriage ? Trim(_spouseMiddle.Text) : null;
            _session.CtcSpouseLast = marriage ? Trim(_spouseLast.Text) : null;
            _session.CtcSpouseSuffix = marriage ? Trim(_spouseSuffix.Text) : null;

            _session.CtcEventDate = _eventDate.Checked ? _eventDate.Value.Date : (DateTime?)null;
            _session.CtcEventCity = Trim(_city.Text);
            _session.CtcEventProvince = Trim(_province.Text);
            _session.CtcDetails = Trim(_remarks.Text);
        }

        private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ------------------------------------------------------------------ helpers
        private bool _sizing;

        // Vertical spacing tiers, roomiest first. A tall screen gets the first; a short one
        // (1366x768) falls through to the first tier whose rows still fit the card, and only if
        // none fits does the body scroll. Type size never changes - it is the readable minimum.
        private struct Tier
        {
            public int Gap, SecH, BarH, PadV;
            public bool Sub;   // show the one-line instruction under the title
            public Tier(int g, int s, int b, int p, bool sub) { Gap = g; SecH = s; BarH = b; PadV = p; Sub = sub; }
        }

        private static readonly Tier[] Tiers =
        {
            new Tier(20, 44, 96, 32, true), new Tier(14, 40, 88, 24, true), new Tier(8, 36, 80, 16, true), new Tier(2, 30, 72, 8, false),
        };

        /// <summary>
        /// The card takes ~88% of the screen width and ~86% of its height (92% on a short
        /// screen such as 1366x768), centred. Every input is the same height - that of the date
        /// picker, whose height Windows fixes from its font - and spare vertical room is shared
        /// between the visible rows. Below a minimum size the FORM scrolls rather than squashing.
        /// </summary>
        private void SizeCard()
        {
            if (_sizing || _card == null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            _sizing = true;
            try
            {
                const int MinW = 900, MinH = 600;
                double hFrac = ClientSize.Height < 900 ? 0.94 : 0.86;
                int w = Math.Max(MinW, (int)(ClientSize.Width * 0.88));
                int h = Math.Max(MinH, (int)(ClientSize.Height * hFrac));
                _card.Size = new Size(w, h);
                AutoScrollMinSize = new Size(w, h);

                int inputH = Math.Max(40, _eventDate.Height);
                string doc = _document.SelectedItem as string;
                bool marriage = doc == "Marriage";
                int fieldRows = 5 + (marriage ? 1 : 0);
                int sections = 2;

                Tier t = Tiers[Tiers.Length - 1];
                int avail = 0, needed = 0;
                foreach (Tier c in Tiers)
                {
                    avail = h - c.PadV * 2 - TitleH - (c.Sub ? SubtitleH : 0) - c.BarH - 8 - 12;
                    needed = fieldRows * (CaptionH + inputH + c.Gap) + sections * (c.SecH + 8);
                    t = c;
                    if (needed <= avail) break;
                }
                int extra = Math.Max(0, Math.Min((avail - needed) / fieldRows, 24));

                _root.Padding = new Padding(44, t.PadV, 44, t.PadV);
                _bar.Height = t.BarH;
                _subtitle.Visible = t.Sub;
                foreach (var sec in _sections) sec.Height = t.SecH;
                foreach (var inp in _inputs) SetInputHeight(inp, inputH);
                foreach (var fp in _fieldPanels) fp.Height = CaptionH + inputH + t.Gap + extra;

                _card.Location = new Point(
                    Math.Max(0, (ClientSize.Width - _card.Width) / 2),
                    Math.Max(0, (ClientSize.Height - _card.Height) / 2));
            }
            finally { _sizing = false; }
        }

        /// <summary>Gives one input its height. A combo that OthersBox wrapped sits inside a host
        /// panel whose height was fixed at wrap time, so the host is resized too.</summary>
        private void SetInputHeight(Control input, int h)
        {
            if (input is DateTimePicker) return;                   // fixed by its font
            if (input is ComboBox cb) cb.ItemHeight = h - 6;       // control height = item height + 6
            else input.Height = h;

            Control host = input.Parent;
            if (host != null && !_fieldPanels.Contains(host as Panel))
            {
                host.Height = h;
                if (input is ComboBox c2) OthersBox.Relayout(c2);
            }
        }

        private void Combo_DrawItem(object sender, DrawItemEventArgs e)
        {
            var cb = (ComboBox)sender;
            e.DrawBackground();
            if (e.Index >= 0)
            {
                string text = cb.GetItemText(cb.Items[e.Index]);
                var r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, text, cb.Font, r, e.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            e.DrawFocusRectangle();
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
