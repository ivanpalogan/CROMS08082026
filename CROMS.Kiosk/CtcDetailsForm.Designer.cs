using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    public sealed partial class CtcDetailsForm
    {
        private Panel _card;
        private Button _back, _next;

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

        private void InitializeComponent()
        {
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
            Load += new EventHandler(CtcDetailsForm_Load);
            Resize += new EventHandler(CtcDetailsForm_Resize);

            BuildCard();
            Controls.Add(_card);
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
            _document.SelectedIndexChanged += new EventHandler(Document_SelectedIndexChanged);
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

            _back = ActionButton("Back", KioskCore.Line, KioskCore.Ink);
            _back.Location = new Point(Pad, CardH - 86);
            _back.Click += new EventHandler(Back_Click);

            _next = ActionButton("Continue", KioskCore.Accent, Color.White);
            _next.Location = new Point(CardW - Pad - 220, CardH - 86);
            _next.Click += new EventHandler(Continue_Click);

            _card.Controls.Add(_back);
            _card.Controls.Add(_next);
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
    }
}
