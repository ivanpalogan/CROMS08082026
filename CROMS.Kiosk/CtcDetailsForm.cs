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
    public sealed class CtcDetailsForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private readonly Panel _card;
        private Timer _idle;
        private Action _resetIdle;
        // Set by every DELIBERATE close so OnFormClosing can tell navigation from a real quit
        // (a programmatic Close() also reports UserClosing — the trap recorded 2026-08-29).
        private bool _navigating;

        // Request
        private readonly ComboBox _document = new ComboBox();
        private readonly ComboBox _copies = new ComboBox();
        private readonly ComboBox _purpose = new ComboBox();
        private readonly ComboBox _relationship = new ComboBox();

        // Whose record
        private readonly TextBox _registryNo = new TextBox();
        private readonly TextBox _ownerFirst = new TextBox();
        private readonly TextBox _ownerMiddle = new TextBox();
        private readonly TextBox _ownerLast = new TextBox();
        private readonly DateTimePicker _eventDate = new DateTimePicker();
        private readonly TextBox _city = new TextBox();
        private readonly TextBox _province = new TextBox();

        // Conditional blocks
        private readonly Label _lblSpouse = new Label();
        private readonly TextBox _spouseFirst = new TextBox();
        private readonly TextBox _spouseMiddle = new TextBox();
        private readonly TextBox _spouseLast = new TextBox();
        private readonly Label _lblParents = new Label();
        private readonly TextBox _father = new TextBox();
        private readonly TextBox _mother = new TextBox();

        private readonly TextBox _remarks = new TextBox();
        private readonly Label _ownerCaption = new Label();
        private readonly Label _eventCaption = new Label();

        private readonly List<Control> _spouseBlock = new List<Control>();
        private readonly List<Control> _parentBlock = new List<Control>();

        private const int CardW = 980, CardH = 760;
        private const int Pad = 44, FieldW = 892;

        public CtcDetailsForm(KioskSession session)
        {
            _session = session;
            Text = "Certified True Copy Details";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = KioskCore.Bg;
            Font = new Font("Segoe UI", 10F);
            AutoScroll = true;

            _card = new Panel
            {
                Size = new Size(CardW, CardH),
                BackColor = KioskCore.CardBg,
                Anchor = AnchorStyles.None,
            };
            AutoScrollMinSize = _card.Size;
            // Centred against the form's OWN ClientSize on Load/Resize, never a
            // Screen.PrimaryScreen snapshot taken in the constructor before the form has been
            // laid out — that static maths is what put the card off in a corner on this DPI.
            Load += (s, e) => { LoadFromSession(); ApplyDocType(); CenterCard(); };
            Resize += (s, e) => CenterCard();

            BuildCard();
            Controls.Add(_card);

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

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }

        // ------------------------------------------------------------------ layout
        private void BuildCard()
        {
            int y = 34;
            _card.Controls.Add(Title("Certified True Copy", Pad, y, 26F, KioskCore.Ink)); y += 44;
            _card.Controls.Add(Title("Tell the records staff which entry to pull from the registry books. " +
                "Only the document and the name are required.", Pad, y, 10.5F, KioskCore.Muted)); y += 34;

            // --- the request -------------------------------------------------
            _card.Controls.Add(Section("WHAT YOU NEED", Pad, y)); y += 26;

            _document.DropDownStyle = ComboBoxStyle.DropDownList;
            _document.Items.AddRange(new object[] { "Birth", "Marriage", "Death" });
            _document.SelectedIndexChanged += (s, e) => ApplyDocType();
            Field("Document type *", _document, Pad, y, 280);

            _copies.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int i = 1; i <= 10; i++) _copies.Items.Add(i.ToString());
            Field("Copies", _copies, Pad + 300, y, 120);

            _purpose.Items.AddRange(KioskCore.CtcPurposes);
            Field("Purpose", _purpose, Pad + 440, y, 452);
            y += 74;

            // --- whose record -------------------------------------------------
            _ownerCaption.Text = "WHOSE RECORD";
            _card.Controls.Add(Section(_ownerCaption, Pad, y)); y += 26;

            Field("Registry number (if you know it)", _registryNo, Pad, y, 280);
            _relationship.Items.AddRange(KioskCore.CtcRelationships);
            Field("You are the record owner's...", _relationship, Pad + 300, y, 592);
            y += 74;

            Field("First name *", _ownerFirst, Pad, y, 290);
            Field("Middle name", _ownerMiddle, Pad + 302, y, 288);
            Field("Last name *", _ownerLast, Pad + 602, y, 290);
            y += 74;

            _eventDate.Format = DateTimePickerFormat.Long;
            _eventDate.ShowCheckBox = true;   // unticked = "the client does not know the date"
            _eventDate.Checked = false;
            _eventCaption.Text = "Date of the event";
            Field(_eventCaption, _eventDate, Pad, y, 290);
            Field("City / Municipality", _city, Pad + 302, y, 288);
            Field("Province", _province, Pad + 602, y, 290);
            y += 78;

            // --- conditional block (marriage: spouse | birth: parents) --------
            int blockY = y;
            _lblSpouse.Text = "THE OTHER SPOUSE";
            Label spouseHead = Section(_lblSpouse, Pad, blockY);
            _spouseBlock.Add(spouseHead);
            _spouseBlock.Add(Field("First name", _spouseFirst, Pad, blockY + 26, 290));
            _spouseBlock.Add(Field("Middle name", _spouseMiddle, Pad + 302, blockY + 26, 288));
            _spouseBlock.Add(Field("Last name", _spouseLast, Pad + 602, blockY + 26, 290));

            _lblParents.Text = "PARENTS ON THE RECORD";
            Label parentHead = Section(_lblParents, Pad, blockY);
            _parentBlock.Add(parentHead);
            _parentBlock.Add(Field("Father's full name", _father, Pad, blockY + 26, 440));
            _parentBlock.Add(Field("Mother's maiden name", _mother, Pad + 452, blockY + 26, 440));
            y = blockY + 100;

            // --- remarks ------------------------------------------------------
            _remarks.MaxLength = 255;
            Field("Anything else that helps find it (spelling variants, nickname, year only...)",
                _remarks, Pad, y, FieldW);
            y += 78;

            Button back = ActionButton("Back", KioskCore.Line, KioskCore.Ink);
            back.Location = new Point(Pad, CardH - 86);
            back.Click += (s, e) => { SaveToSession(); _navigating = true; DialogResult = DialogResult.Cancel; Close(); };

            Button next = ActionButton("Continue", KioskCore.Accent, Color.White);
            next.Location = new Point(CardW - Pad - 220, CardH - 86);
            next.Click += Continue_Click;

            _card.Controls.Add(back);
            _card.Controls.Add(next);
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

        private Control Field(string caption, Control input, int x, int y, int width)
        {
            var cap = new Label
            {
                Text = caption,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = KioskCore.Ink,
            };
            return Field(cap, input, x, y, width);
        }

        /// <summary>Overload taking a caption Label the caller keeps a reference to, so its text
        /// can follow the chosen document type ("Date of birth" / "Date of marriage").</summary>
        private Control Field(Label caption, Control input, int x, int y, int width)
        {
            caption.Location = new Point(x, y);
            caption.Size = new Size(width, 22);
            caption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            caption.ForeColor = KioskCore.Ink;

            input.Location = new Point(x, y + 24);
            input.Size = new Size(width, 34);
            input.Font = new Font("Segoe UI", 10.5F);

            _card.Controls.Add(caption);
            _card.Controls.Add(input);

            // The caption is returned as the block member so a hidden block hides BOTH — the
            // input follows its caption's visibility through the paired lists.
            caption.Tag = input;
            return caption;
        }

        private Label Section(string text, int x, int y) => Section(new Label { Text = text }, x, y);

        private Label Section(Label label, int x, int y)
        {
            label.Location = new Point(x, y);
            label.Size = new Size(FieldW, 20);
            label.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            label.ForeColor = KioskCore.Accent;
            _card.Controls.Add(label);
            return label;
        }

        private static Label Title(string text, int x, int y, float size, Color color) => new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(FieldW, size > 14 ? 40 : 30),
            Font = new Font("Segoe UI", size, size > 14 ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color,
        };

        private static Button ActionButton(string text, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(220, 52),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
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
