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
        private TableLayoutPanel _grid;
        private Label _title, _subtitle;
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

        // Every member of a block is one control (a field panel or a section heading), so
        // hiding the block is one Visible flag per member and its table row collapses to 0.
        private readonly List<Control> _spouseBlock = new List<Control>();
        private readonly List<Control> _parentBlock = new List<Control>();

        // Everything that carries a font which follows the card size (see SizeCard).
        private readonly List<Label> _captions = new List<Label>();
        private readonly List<Control> _inputs = new List<Control>();
        private readonly List<Label> _sections = new List<Label>();
        private readonly List<Panel> _fieldPanels = new List<Panel>();

        // Designed (1x) sizes; SizeCard multiplies them by the card's scale.
        private const int BaseFieldH = 60, BaseSectionH = 26, GridCols = 6;

        private void InitializeComponent()
        {
            Text = "Certified True Copy Details";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = KioskCore.Bg;
            Font = new Font("Segoe UI", 10F);
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
            var root = new TableLayoutPanel
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
                Height = 34,
                ForeColor = KioskCore.Muted,
                Font = new Font("Segoe UI", 10.5F),
                AutoEllipsis = true,
            };
            _title = new Label
            {
                Text = "Certified True Copy",
                Dock = DockStyle.Top,
                Height = 46,
                ForeColor = KioskCore.Ink,
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
            };
            head.Controls.Add(_subtitle);   // Dock=Top: last added sits on top
            head.Controls.Add(_title);
            root.Controls.Add(head, 0, 0);

            // --- body (scrolls only if the screen is genuinely too small) -----
            _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0, 8, 0, 8) };
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
            Row(Field("Document type *", _document), 0, 2, true);

            _copies.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int i = 1; i <= 10; i++) _copies.Items.Add(i.ToString());
            Field("Copies", _copies, out Panel pCopies);
            _grid.Controls.Add(pCopies, 2, _grid.RowCount - 1); _grid.SetColumnSpan(pCopies, 1);

            _purpose.Items.AddRange(KioskCore.CtcPurposes);
            Field("Purpose", _purpose, out Panel pPurpose);
            _grid.Controls.Add(pPurpose, 3, _grid.RowCount - 1); _grid.SetColumnSpan(pPurpose, 3);

            // --- whose record -------------------------------------------------
            _ownerCaption.Text = "WHOSE RECORD";
            Row(Section(_ownerCaption), 0, GridCols);

            Row(Field("Registry number (if you know it)", _registryNo), 0, 2, true);
            _relationship.Items.AddRange(KioskCore.CtcRelationships);
            Field("You are the record owner's...", _relationship, out Panel pRel);
            _grid.Controls.Add(pRel, 2, _grid.RowCount - 1); _grid.SetColumnSpan(pRel, 4);

            Row(Field("First name *", _ownerFirst), 0, 2, true);
            Field("Middle name", _ownerMiddle, out Panel pMid);
            _grid.Controls.Add(pMid, 2, _grid.RowCount - 1); _grid.SetColumnSpan(pMid, 2);
            Field("Last name *", _ownerLast, out Panel pLast);
            _grid.Controls.Add(pLast, 4, _grid.RowCount - 1); _grid.SetColumnSpan(pLast, 2);

            _eventDate.Format = DateTimePickerFormat.Long;
            _eventDate.ShowCheckBox = true;   // unticked = "the client does not know the date"
            _eventDate.Checked = false;
            _eventCaption.Text = "Date of the event";
            Row(Field(_eventCaption, _eventDate), 0, 2, true);
            Field("City / Municipality", _city, out Panel pCity);
            _grid.Controls.Add(pCity, 2, _grid.RowCount - 1); _grid.SetColumnSpan(pCity, 2);
            Field("Province", _province, out Panel pProv);
            _grid.Controls.Add(pProv, 4, _grid.RowCount - 1); _grid.SetColumnSpan(pProv, 2);

            // --- conditional block (marriage: spouse | birth: parents) --------
            _lblSpouse.Text = "THE OTHER SPOUSE";
            Label spouseHead = Section(_lblSpouse);
            Row(spouseHead, 0, GridCols);
            _spouseBlock.Add(spouseHead);
            Panel s1 = Field("First name", _spouseFirst);
            Row(s1, 0, 2, true);
            _spouseBlock.Add(s1);
            Field("Middle name", _spouseMiddle, out Panel s2);
            _grid.Controls.Add(s2, 2, _grid.RowCount - 1); _grid.SetColumnSpan(s2, 2); _spouseBlock.Add(s2);
            Field("Last name", _spouseLast, out Panel s3);
            _grid.Controls.Add(s3, 4, _grid.RowCount - 1); _grid.SetColumnSpan(s3, 2); _spouseBlock.Add(s3);

            _lblParents.Text = "PARENTS ON THE RECORD";
            Label parentHead = Section(_lblParents);
            Row(parentHead, 0, GridCols);
            _parentBlock.Add(parentHead);
            Panel p1 = Field("Father's full name", _father);
            Row(p1, 0, 3, true);
            _parentBlock.Add(p1);
            Field("Mother's maiden name", _mother, out Panel p2);
            _grid.Controls.Add(p2, 3, _grid.RowCount - 1); _grid.SetColumnSpan(p2, 3); _parentBlock.Add(p2);

            // --- remarks ------------------------------------------------------
            _remarks.MaxLength = 255;
            Row(Field("Anything else that helps find it (spelling variants, nickname, year only...)", _remarks), 0, GridCols);

            // --- button bar ---------------------------------------------------
            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Height = 72,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236F));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _back = ActionButton("Back", KioskCore.Line, KioskCore.Ink);
            _back.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _back.Click += new EventHandler(Back_Click);

            _next = ActionButton("Continue", KioskCore.Accent, Color.White);
            _next.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _next.Margin = new Padding(3, 3, 19, 3);   // line up with the fields' right edge (they keep a 16px gutter)
            _next.Click += new EventHandler(Continue_Click);

            bar.Controls.Add(_back, 0, 0);
            bar.Controls.Add(_next, 2, 0);
            root.Controls.Add(bar, 0, 2);

            _card.Controls.Add(root);
        }

        /// <summary>Starts a new grid row and puts <paramref name="c"/> in it. When
        /// <paramref name="keepOpen"/> is true the caller adds more cells to the SAME row.</summary>
        private void Row(Control c, int col, int span, bool keepOpen = false)
        {
            _grid.RowCount += 1;
            _grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _grid.Controls.Add(c, col, _grid.RowCount - 1);
            _grid.SetColumnSpan(c, span);
            // keepOpen: later cells of this row use _grid.RowCount - 1 as their row. A row that
            // is not kept open is simply never added to again.
        }

        /// <summary>One field = a transparent panel holding the bold caption over the input.
        /// It is a single control, so hiding it hides both and its row collapses.</summary>
        private Panel Field(string caption, Control input) => Field(new Label { Text = caption }, input);

        private void Field(string caption, Control input, out Panel panel) => panel = Field(new Label { Text = caption }, input);

        /// <summary>Overload taking a caption Label the caller keeps a reference to, so its text
        /// can follow the chosen document type ("Date of birth" / "Date of marriage").</summary>
        private Panel Field(Label caption, Control input)
        {
            caption.Dock = DockStyle.Top;
            caption.Height = 24;
            caption.ForeColor = KioskCore.Ink;
            caption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            caption.AutoEllipsis = true;
            caption.UseMnemonic = false;

            input.Dock = DockStyle.Top;
            input.Font = new Font("Segoe UI", 10.5F);

            var p = new Panel
            {
                Dock = DockStyle.Fill,
                Height = BaseFieldH,
                Margin = new Padding(0, 0, 16, 0),
                BackColor = Color.Transparent,
            };
            p.Controls.Add(input);      // Dock=Top: added first = below the caption
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
            label.Height = BaseSectionH;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            label.ForeColor = KioskCore.Accent;
            label.Margin = new Padding(0, 4, 0, 2);
            label.UseMnemonic = false;
            _sections.Add(label);
            return label;
        }

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
