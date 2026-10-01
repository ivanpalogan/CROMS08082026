using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    public sealed partial class CtcDetailsForm
    {
        private Panel _card;
        private Panel _body;
        private TableLayoutPanel _root, _bar;
        private TableLayoutPanel _grid;
        private Label _title, _subtitle;
        private Button _back, _next;

        // Request
        private readonly ComboBox _document = new ComboBox();
        private readonly ComboBox _copies = new ComboBox();
        private readonly ComboBox _purpose = new ComboBox();
        private readonly ComboBox _relationship = new ComboBox();

        // Whose record. For a marriage the "owner" boxes are the HUSBAND and the "spouse" boxes
        // the WIFE; only the captions change (see ApplyDocType).
        private readonly TextBox _registryNo = new TextBox();
        private readonly TextBox _ownerFirst = new TextBox();
        private readonly TextBox _ownerMiddle = new TextBox();
        private readonly TextBox _ownerLast = new TextBox();
        private readonly TextBox _ownerSuffix = new TextBox();
        private readonly DateTimePicker _eventDate = new DateTimePicker();
        // Province first, then only that province's cities / municipalities (see SetupGeo).
        private readonly ComboBox _province = new ComboBox();
        private readonly ComboBox _city = new ComboBox();

        // Marriage only: the wife's name.
        private readonly TextBox _spouseFirst = new TextBox();
        private readonly TextBox _spouseMiddle = new TextBox();
        private readonly TextBox _spouseLast = new TextBox();
        private readonly TextBox _spouseSuffix = new TextBox();

        private readonly TextBox _remarks = new TextBox();
        private readonly Label _ownerCaption = new Label();
        private readonly Label _eventCaption = new Label();
        private readonly Label _provinceCaption = new Label();
        private readonly Label _cityCaption = new Label();
        private readonly Label _ownerFirstCap = new Label(), _ownerMiddleCap = new Label(), _ownerLastCap = new Label();

        // Each member of the wife's row is one field panel, so hiding the block is one Visible
        // flag per member and its table row collapses to 0.
        private readonly List<Control> _spouseBlock = new List<Control>();

        // Date / province / city of the event. Hidden until a document type is chosen, because the
        // captions name the event ("Date of Birth") and there is no honest generic wording.
        private readonly List<Control> _eventBlock = new List<Control>();

        // Everything that carries a font which follows the card size (see SizeCard).
        private readonly List<Label> _captions = new List<Label>();
        private readonly List<Control> _inputs = new List<Control>();
        private readonly List<Label> _sections = new List<Label>();
        private readonly List<Panel> _fieldPanels = new List<Panel>();

        // Designed (1x) sizes; SizeCard multiplies them by the card's scale.
        private const int GridCols = 12;

        // Sizes are in PIXELS (GraphicsUnit.Pixel), so they read the same on every display
        // scaling. Chosen for elderly and low-vision clients at arm's length: title 32,
        // section headings 24, labels 19, input text 20, button text 24.
        private const int TitlePx = 32, SubtitlePx = 18, SectionPx = 24, LabelPx = 19, InputPx = 20, ButtonPx = 24;
        private const int CaptionH = 28, TitleH = 44, SubtitleH = 28, ErrH = 24;
        private static readonly Color ErrorColor = Color.FromArgb(198, 50, 63);   // #C6323F

        private static Font Px(float px, FontStyle style = FontStyle.Regular)
            => new Font("Segoe UI", px, style, GraphicsUnit.Pixel);

        private void InitializeComponent()
        {
            Text = "Certified True Copy Details";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = KioskCore.Bg;
            Font = Px(InputPx);
            AutoScroll = true;

            _card = new Panel { BackColor = KioskCore.CardBg };
            Load += new EventHandler(CtcDetailsForm_Load);
            Resize += new EventHandler(CtcDetailsForm_Resize);

            BuildCard();
            Controls.Add(_card);
        }

        // ------------------------------------------------------------------ layout
        // The card is a three-row table: heading / scrolling body / button bar. The body is a
        // six-column grid, so every field gets a share of the card's width instead of a fixed
        // pixel box - at any monitor size the fields stretch with the card and never overlap.
        private void BuildCard()
        {
            var root = _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(44, 28, 44, 24),
                BackColor = Color.Transparent,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // --- heading ------------------------------------------------------
            var head = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent };
            _subtitle = new Label
            {
                Text = "Tell the records staff which entry to pull from the registry books. Only the document and the name are required.",
                Dock = DockStyle.Top,
                Height = SubtitleH,
                ForeColor = KioskCore.Muted,
                Font = Px(SubtitlePx),
                AutoEllipsis = true,
            };
            _title = new Label
            {
                Text = "Certified True Copy",
                Dock = DockStyle.Top,
                Height = TitleH,
                ForeColor = KioskCore.Ink,
                Font = Px(TitlePx, FontStyle.Bold),
            };
            head.Controls.Add(_subtitle);   // Dock=Top: last added sits on top
            head.Controls.Add(_title);
            root.Controls.Add(head, 0, 0);

            // --- body (scrolls only if the screen is genuinely too small) -----
            _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 4) };
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = GridCols,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
            };
            for (int i = 0; i < GridCols; i++) _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / GridCols));
            _body.Controls.Add(_grid);
            root.Controls.Add(_body, 0, 1);

            // --- the request --------------------------------------------------
            Row(Section("WHAT YOU NEED"), 0, GridCols);

            _document.DropDownStyle = ComboBoxStyle.DropDownList;
            _document.Items.AddRange(new object[] { "Birth", "Marriage", "Death" });
            _document.SelectedIndexChanged += new EventHandler(Document_SelectedIndexChanged);
            Row(Field("Document Type *", _document), 0, 3);

            _copies.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int i = 1; i <= 10; i++) _copies.Items.Add(i.ToString());
            Cell(Field("Number of Copies", _copies), 3, 3);

            _purpose.Items.AddRange(KioskCore.CtcPurposes);
            Cell(Field("Purpose of Request", _purpose), 6, 6);

            // --- whose record -------------------------------------------------
            _ownerCaption.Text = "WHOSE RECORD";
            Row(Section(_ownerCaption), 0, GridCols);

            Row(Field("Registry number (if you know it)", _registryNo), 0, 4);
            _relationship.DropDownStyle = ComboBoxStyle.DropDownList;   // pick one; "Other" opens a specify box
            _relationship.Items.AddRange(KioskCore.CtcRelationships);
            Cell(Field("Relationship to Record Owner", _relationship), 4, 8);

            // Name rows: First 4 | Middle 3 | Last 3 | Suffix 2 of 12 columns.
            _ownerFirstCap.Text = "First name *"; _ownerMiddleCap.Text = "Middle name"; _ownerLastCap.Text = "Last name *";
            Row(Field(_ownerFirstCap, _ownerFirst), 0, 4);
            Cell(Field(_ownerMiddleCap, _ownerMiddle), 4, 3);
            Cell(Field(_ownerLastCap, _ownerLast), 7, 3);
            Cell(Field("Suffix (if any)", _ownerSuffix), 10, 2);

            // The wife's row - marriage only.
            Panel w1 = Field("Wife's first name *", _spouseFirst);
            Row(w1, 0, 4);
            Panel w2 = Field("Wife's middle name", _spouseMiddle); Cell(w2, 4, 3);
            Panel w3 = Field("Wife's last name *", _spouseLast); Cell(w3, 7, 3);
            Panel w4 = Field("Suffix (if any)", _spouseSuffix); Cell(w4, 10, 2);
            _spouseBlock.AddRange(new Control[] { w1, w2, w3, w4 });

            // The event: date, province, city - captioned per document ("Date of birth",
            // "Province of marriage"...), see ApplyDocType.
            _eventDate.Format = DateTimePickerFormat.Custom;   // no weekday: it only crowds the field
            _eventDate.CustomFormat = "d MMMM yyyy";
            _eventDate.ShowCheckBox = true;   // unticked = "the client does not know the date"
            _eventDate.Checked = false;
            Panel e1 = Field(_eventCaption, _eventDate);
            Row(e1, 0, 4);
            Panel e2 = Field(_provinceCaption, _province); Cell(e2, 4, 4);
            Panel e3 = Field(_cityCaption, _city); Cell(e3, 8, 4);
            _eventBlock.AddRange(new Control[] { e1, e2, e3 });

            // --- remarks ------------------------------------------------------
            _remarks.MaxLength = 255;
            Row(Field("Anything else that helps find it (spelling variants, nickname, year only...)", _remarks), 0, GridCols);

            // --- button bar ---------------------------------------------------
            var bar = _bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Height = 92,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 296F));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _back = ActionButton("Back", KioskCore.Line, KioskCore.Ink);
            _back.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _back.Click += new EventHandler(Back_Click);

            _next = ActionButton("Continue", KioskCore.Accent, Color.White);
            _next.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _next.Margin = new Padding(3, 3, 35, 3);   // line up with the fields' right edge (they keep a 16px gutter)
            _next.Click += new EventHandler(Continue_Click);

            bar.Controls.Add(_back, 0, 0);
            bar.Controls.Add(_next, 2, 0);
            root.Controls.Add(bar, 0, 2);

            _card.Controls.Add(root);
        }

        /// <summary>Starts a new grid row and puts <paramref name="c"/> in it; follow with
        /// <see cref="Cell"/> for the other cells of the same row.</summary>
        private void Row(Control c, int col, int span)
        {
            _grid.RowCount += 1;
            _grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _grid.Controls.Add(c, col, _grid.RowCount - 1);
            _grid.SetColumnSpan(c, span);
        }

        /// <summary>Adds a cell to the row <see cref="Row"/> just started.</summary>
        private void Cell(Control c, int col, int span)
        {
            _grid.Controls.Add(c, col, _grid.RowCount - 1);
            _grid.SetColumnSpan(c, span);
        }

        /// <summary>One field = a transparent panel holding the bold caption over the input.
        /// It is a single control, so hiding it hides both and its row collapses.</summary>
        private Panel Field(string caption, Control input) => Field(new Label { Text = caption }, input);

        /// <summary>Overload taking a caption Label the caller keeps a reference to, so its text
        /// can follow the chosen document type ("Date of birth" / "Date of marriage").</summary>
        private Panel Field(Label caption, Control input)
        {
            caption.Dock = DockStyle.Top;
            caption.Height = CaptionH;
            caption.ForeColor = KioskCore.Ink;
            caption.Font = Px(LabelPx, FontStyle.Bold);
            caption.TextAlign = ContentAlignment.BottomLeft;
            caption.AutoEllipsis = true;
            caption.UseMnemonic = false;

            input.Dock = DockStyle.Top;
            input.Font = Px(InputPx);
            // A date picker's height is fixed by its font, so it gets a bigger one - it sets the
            // height every other input is matched to (see SizeCard).
            if (input is DateTimePicker) input.Font = Px(InputPx + 6);
            if (input is TextBox tb) { tb.AutoSize = false; tb.BorderStyle = BorderStyle.FixedSingle; }   // lets SizeCard make it taller than its font
            if (input is ComboBox cb)
            {
                // Owner-drawn so the box and its list rows can be as tall as a finger needs.
                cb.DrawMode = DrawMode.OwnerDrawFixed;
                cb.DrawItem += new DrawItemEventHandler(Combo_DrawItem);
            }

            var p = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 80,
                Margin = new Padding(0, 0, 16, 0),
                BackColor = Color.Transparent,
            };
            // The line shown BELOW the field when it fails validation. Added first so it docks last
            // (lowest); hidden until needed, and the panel only grows by ErrH while it shows.
            var err = new Label
            {
                Dock = DockStyle.Top,
                Height = ErrH,
                ForeColor = ErrorColor,
                Font = Px(16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                AutoEllipsis = true,
                Visible = false,
            };
            p.Tag = err;
            p.Controls.Add(err);
            p.Controls.Add(input);      // Dock=Top: added after err = above it, below the caption
            p.Controls.Add(caption);

            _captions.Add(caption);
            _inputs.Add(input);
            _fieldPanels.Add(p);
            return p;
        }

        private Label Section(string text) => Section(new Label { Text = text });

        private Label Section(Label label)
        {
            label.Dock = DockStyle.Fill;
            label.Height = 36;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Font = Px(SectionPx, FontStyle.Bold);
            label.ForeColor = KioskCore.Accent;
            label.Margin = new Padding(0, 6, 0, 2);
            label.UseMnemonic = false;
            _sections.Add(label);
            return label;
        }

        private static Button ActionButton(string text, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(260, 68),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = Px(ButtonPx, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
