using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            SetupGeo();
            OthersBox.AttachInline(_purpose, 80);
            // One name rule for every name box (see NameField). A full stop is kept because "Ma.
            // Cristina" (first name), "A." (middle initial) and "Jr." (suffix) are all normal. The
            // middle name may be left blank. AutoCaps only capitalises the first letter of a word.
            foreach (TextBox t in new[] { _ownerFirst, _ownerLast, _spouseFirst, _spouseLast }) NameField.Attach(t, true);
            NameField.Attach(_ownerMiddle, true, "Full name or initial");
            NameField.Attach(_spouseMiddle, true, "Full name or initial");
            NameField.Attach(_ownerSuffix, true, "Jr., Sr., III");
            NameField.Attach(_spouseSuffix, true, "Jr., Sr., III");
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
            if (!NameField.HasLetter(_ownerFirst.Text) || !NameField.HasLetter(_ownerLast.Text))
            {
                Warn(_document.SelectedItem as string == "Marriage"
                    ? "Please enter the husband's first and last name."
                    : "Please enter the first and last name on the record you need a copy of.");
                (!NameField.HasLetter(_ownerFirst.Text) ? _ownerFirst : _ownerLast).Focus();
                return;
            }
            if (_document.SelectedItem as string == "Marriage"
                && (!NameField.HasLetter(_spouseFirst.Text) || !NameField.HasLetter(_spouseLast.Text)))
            {
                Warn("Please enter the wife's first and last name.");
                (!NameField.HasLetter(_spouseFirst.Text) ? _spouseFirst : _spouseLast).Focus();
                return;
            }
            if (!PlaceIsValid()) return;
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
            LoadPlace(_session.CtcEventProvince, _session.CtcEventCity);
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
            _session.CtcOwnerFirst = Trim(NameField.Clean(_ownerFirst.Text, true));
            _session.CtcOwnerMiddle = Trim(NameField.Clean(_ownerMiddle.Text, true));
            _session.CtcOwnerLast = Trim(NameField.Clean(_ownerLast.Text, true));
            _session.CtcOwnerSuffix = Trim(NameField.Clean(_ownerSuffix.Text, true));

            // A block that is not on screen must not contribute to the request — switching
            // Marriage to Death after typing a spouse would otherwise file a death record
            // with a spouse name nobody ever saw.
            bool marriage = _session.CtcDocumentType == "Marriage";
            _session.CtcSpouseFirst = marriage ? Trim(NameField.Clean(_spouseFirst.Text, true)) : null;
            _session.CtcSpouseMiddle = marriage ? Trim(NameField.Clean(_spouseMiddle.Text, true)) : null;
            _session.CtcSpouseLast = marriage ? Trim(NameField.Clean(_spouseLast.Text, true)) : null;
            _session.CtcSpouseSuffix = marriage ? Trim(NameField.Clean(_spouseSuffix.Text, true)) : null;

            _session.CtcEventDate = _eventDate.Checked ? _eventDate.Value.Date : (DateTime?)null;
            // The placeholder shown while the city box is locked must never be saved as a city.
            _session.CtcEventProvince = Trim(_province.Text);
            _session.CtcEventCity = _city.Enabled ? Trim(_city.Text) : null;
            _session.CtcDetails = Trim(_remarks.Text);
        }

        private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ------------------------------------------------------------------ helpers
        // ------------------------------------------------------------------ place of the event
        // Province -> City/Municipality, from the official PSGC lists the staff app uses. The city
        // box stays locked ("Select Province First") until a province is chosen, and then lists
        // only that province's cities and municipalities. Both boxes search as the client types.
        private const string CityLocked = "Select Province First";
        private readonly ToolTip _geoTip = new ToolTip { IsBalloon = false, AutoPopDelay = 3500 };
        private bool _geoOffline;      // lists could not be loaded: both boxes fall back to plain typing

        private void SetupGeo()
        {
            var provinces = GeoData.Provinces();
            _geoOffline = provinces.Count == 0;
            foreach (ComboBox c in new[] { _province, _city })
            {
                c.DropDownStyle = ComboBoxStyle.DropDown;
                SearchPick.Attach(c);
            }
            if (_geoOffline) return;   // no PSGC data reachable: leave both as ordinary text boxes

            _province.Items.AddRange(provinces.ToArray());
            SearchPick.Refresh(_province);
            _province.SelectedIndexChanged += (s, e) => ProvincePicked();
            _province.TextUpdate += (s, e) => { if (_city.Enabled) LockCity(); };   // typing again un-picks the province
            _province.Leave += (s, e) => CheckProvince();
            _city.Leave += (s, e) => CheckCity();
            LockCity();
        }

        private void LockCity()
        {
            _city.Items.Clear();
            SearchPick.Refresh(_city);
            _city.Text = CityLocked;
            _city.Enabled = false;
        }

        private void ProvincePicked()
        {
            int id = GeoData.ProvinceId(_province.Text);
            if (id == 0) { LockCity(); return; }
            _city.Enabled = true;
            _city.Items.Clear();
            _city.Items.AddRange(GeoData.Municipalities(id).ToArray());
            SearchPick.Refresh(_city);
            _city.Text = "";
            _city.SelectedIndex = -1;
        }

        /// <summary>Typed text that is not a real province is cleared - a wrong place on a record is worse than a blank one.</summary>
        private void CheckProvince()
        {
            if (_geoOffline || string.IsNullOrWhiteSpace(_province.Text)) return;
            int idx = FindListed(_province, _province.Text);
            if (idx >= 0) { if (_province.SelectedIndex != idx) _province.SelectedIndex = idx; return; }
            _province.Text = "";
            LockCity();
            _geoTip.Show("Please pick a province from the list.", _province, 0, _province.Height + 2, 3000);
        }

        private void CheckCity()
        {
            if (_geoOffline || !_city.Enabled || string.IsNullOrWhiteSpace(_city.Text)) return;
            int idx = FindListed(_city, _city.Text);
            if (idx >= 0) { if (_city.SelectedIndex != idx) _city.SelectedIndex = idx; return; }
            _city.Text = "";
            _geoTip.Show("Please pick a city or municipality from the list.", _city, 0, _city.Height + 2, 3000);
        }

        private static int FindListed(ComboBox cb, string text)
        {
            string n = GeoData.Normalize(text);
            for (int i = 0; i < cb.Items.Count; i++)
                if (GeoData.Normalize(cb.GetItemText(cb.Items[i])) == n) return i;
            return -1;
        }

        private void LoadPlace(string province, string city)
        {
            if (_geoOffline) { _province.Text = province ?? ""; _city.Text = city ?? ""; return; }
            int idx = string.IsNullOrWhiteSpace(province) ? -1 : FindListed(_province, province);
            if (idx < 0) { _province.SelectedIndex = -1; _province.Text = ""; LockCity(); return; }
            _province.SelectedIndex = idx;       // fires ProvincePicked, which fills the city list
            if (!string.IsNullOrWhiteSpace(city))
            {
                int c = FindListed(_city, city);
                if (c >= 0) _city.SelectedIndex = c; else _city.Text = city;
            }
        }

        private bool PlaceIsValid()
        {
            CheckProvince();
            CheckCity();
            return true;
        }

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
