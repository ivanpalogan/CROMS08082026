using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class TemplateDesignerForm
    {
        private TemplateCanvas _canvas;
        private ListBox _lstElements;
        private Panel _propsHost;
        private Label _lblMode;
        private Button _btnSave, _btnUndo, _btnRedo, _btnDataSource, _btnEditToggle;
        private Button _btnApplyHeader, _btnApplyFooter;
        private Button _btnClose, _btnAddText, _btnAddField, _btnAddImage, _btnAddLine, _btnAddRect;

        private void InitializeComponent()
        {
            Text = _info.FormName + " — Template Designer";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1440, 920);
            WindowState = FormWindowState.Maximized;
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9f);
            FormClosing += new FormClosingEventHandler(TemplateDesignerForm_FormClosing);

            BuildLayout();
        }

        // ============================================================== layout

        private void BuildLayout()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = UiTheme.Surface };
            top.Paint += (s, e) => e.Graphics.DrawLine(Pens.Gainsboro, 0, top.Height - 1, top.Width, top.Height - 1);

            var title = new Label
            {
                Text = _info.FormName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(18, 14),
            };
            _lblMode = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(20, 36),
            };

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                WrapContents = false,
            };
            _btnClose = Btn("Close", UiTheme.Chrome, UiTheme.Ink);
            _btnClose.Click += new EventHandler(BtnClose_Click);
            _btnSave = Btn("Save Template", UiTheme.Accent, Color.White);
            _btnSave.Click += new EventHandler(BtnSave_Click);
            _btnEditToggle = Btn("Edit Template", UiTheme.Accent, Color.White);
            _btnEditToggle.Click += new EventHandler(BtnEditToggle_Click);
            _btnDataSource = Btn("Preview: Sample Data", UiTheme.Chrome, UiTheme.Ink);
            _btnDataSource.Width = 220;
            _btnDataSource.Click += new EventHandler(BtnDataSource_Click);
            _btnRedo = Btn("Redo ↷", UiTheme.Chrome, UiTheme.Ink);
            _btnRedo.Click += new EventHandler(BtnRedo_Click);
            _btnUndo = Btn("↶ Undo", UiTheme.Chrome, UiTheme.Ink);
            _btnUndo.Click += new EventHandler(BtnUndo_Click);

            bool inFamily = TemplateStore.FactsCertificationFamily.Contains(_info.FormCode);
            if (inFamily)
            {
                _btnApplyFooter = Btn("Apply Footer to 1A/2A/3A", UiTheme.Chrome, UiTheme.Ink);
                _btnApplyFooter.Width = 172;
                _btnApplyFooter.Click += new EventHandler(BtnApplyFooter_Click);
                _btnApplyHeader = Btn("Apply Header to 1A/2A/3A", UiTheme.Chrome, UiTheme.Ink);
                _btnApplyHeader.Width = 172;
                _btnApplyHeader.Click += new EventHandler(BtnApplyHeader_Click);
                bar.Controls.Add(_btnApplyFooter);
                bar.Controls.Add(_btnApplyHeader);
            }

            bar.Controls.Add(_btnClose);
            bar.Controls.Add(_btnSave);
            bar.Controls.Add(_btnEditToggle);
            bar.Controls.Add(_btnDataSource);
            bar.Controls.Add(_btnRedo);
            bar.Controls.Add(_btnUndo);

            top.Controls.Add(bar);
            top.Controls.Add(_lblMode);
            top.Controls.Add(title);

            // ---- left: toolbox + elements list
            var left = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = UiTheme.Surface, Padding = new Padding(12) };
            left.Paint += (s, e) => e.Graphics.DrawLine(Pens.Gainsboro, left.Width - 1, 0, left.Width - 1, left.Height);

            var lblAdd = new Label { Text = "COMPONENTS", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(12, 8), AutoSize = true };
            int y = 30;
            Button MakeAdd(string text) { var b = ToolboxButton(text); b.Location = new Point(12, y); y += 40; return b; }
            _btnAddText = MakeAdd("＋ Text");
            _btnAddField = MakeAdd("＋ Data Field");
            _btnAddImage = MakeAdd("＋ Image");
            _btnAddLine = MakeAdd("＋ Line");
            _btnAddRect = MakeAdd("＋ Rectangle");
            _btnAddText.Click += new EventHandler(BtnAddText_Click);
            _btnAddField.Click += new EventHandler(BtnAddField_Click);
            _btnAddImage.Click += new EventHandler(BtnAddImage_Click);
            _btnAddLine.Click += new EventHandler(BtnAddLine_Click);
            _btnAddRect.Click += new EventHandler(BtnAddRect_Click);

            var lblElements = new Label { Text = "ELEMENTS ON THIS PAGE", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(12, y + 8), AutoSize = true };
            _lstElements = new ListBox
            {
                Location = new Point(12, y + 30),
                Size = new Size(184, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
                IntegralHeight = false,
                FormattingEnabled = true,
            };
            _lstElements.Format += new ListControlConvertEventHandler(ListBox_Format);
            _lstElements.SelectedIndexChanged += new EventHandler(LstElements_SelectedIndexChanged);

            left.Controls.Add(_lstElements);
            left.Controls.Add(lblElements);
            left.Controls.Add(_btnAddText);
            left.Controls.Add(_btnAddField);
            left.Controls.Add(_btnAddImage);
            left.Controls.Add(_btnAddLine);
            left.Controls.Add(_btnAddRect);
            left.Controls.Add(lblAdd);

            // ---- right: properties
            _propsHost = new Panel { Dock = DockStyle.Right, Width = 270, BackColor = UiTheme.Surface, AutoScroll = true, Padding = new Padding(14) };
            _propsHost.Paint += (s, e) => e.Graphics.DrawLine(Pens.Gainsboro, 0, 0, 0, _propsHost.Height);

            // ---- center: canvas
            _canvas = new TemplateCanvas
            {
                Dock = DockStyle.Fill,
                Template = _current,
            };
            _canvas.SelectionChanged += new EventHandler(Canvas_SelectionChanged);
            _canvas.BeforeMutate += new EventHandler(Canvas_BeforeMutate);
            _canvas.ElementsChanged += new EventHandler(Canvas_ElementsChanged);

            Controls.Add(_canvas);
            Controls.Add(_propsHost);
            Controls.Add(left);
            Controls.Add(top);

        }

        private static Button Btn(string text, Color back, Color fore) => new Button
        {
            Text = text, AutoSize = false, Size = new Size(120, 32),
            BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 0, 0, 0),
        };

        private static Button ToolboxButton(string text) => new Button
        {
            Text = text, Size = new Size(184, 34), TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0), BackColor = UiTheme.Chrome, ForeColor = UiTheme.Ink,
            FlatStyle = FlatStyle.Flat,
        };
    }
}
