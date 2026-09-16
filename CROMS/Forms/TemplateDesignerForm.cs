using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// The visual certificate template editor: click, drag, resize and format elements
    /// on a page that represents the actual printed sheet. Everything typed here is
    /// LAYOUT — where a logo sits, how big a name prints, which column a field reads
    /// from. It never writes to a civil registry record; saving only changes how a
    /// certificate looks the next time it is printed.
    /// </summary>
    public class TemplateDesignerForm : Form
    {
        private readonly TemplateFormInfo _info;
        private bool _editable;
        private CertTemplate _current;
        private string _savedJson;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        private readonly Stack<string> _undo = new Stack<string>();
        private readonly Stack<string> _redo = new Stack<string>();
        private readonly Dictionary<int, Image> _imageCache = new Dictionary<int, Image>();

        private TemplateCanvas _canvas;
        private ListBox _lstElements;
        private Panel _propsHost;
        private Label _lblMode;
        private Button _btnSave, _btnUndo, _btnRedo, _btnDataSource, _btnEditToggle;
        private Button _btnApplyHeader, _btnApplyFooter;
        private string _realRecordLabel;

        public TemplateDesignerForm(TemplateFormInfo info, bool startInEditMode)
        {
            _info = info;
            _editable = startInEditMode;

            _current = TemplateStore.GetActive(info.FormCode) ?? new CertTemplate
            {
                FormCode = info.FormCode, Name = info.FormName,
                PageWidth = info.PageWidth, PageHeight = info.PageHeight
            };
            _savedJson = _json.Serialize(_current.Elements);

            Text = info.FormName + " — Template Designer";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1440, 920);
            WindowState = FormWindowState.Maximized;
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9f);
            FormClosing += TemplateDesignerForm_FormClosing;

            BuildLayout();
            LoadSampleValues();
            RebuildElementsList();
            UpdateModeUi();

            UiTheme.Polish(this);
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
            var btnClose = Btn("Close", UiTheme.Chrome, UiTheme.Ink);
            btnClose.Click += (s, e) => Close();
            _btnSave = Btn("Save Template", UiTheme.Accent, Color.White);
            _btnSave.Click += (s, e) => SaveTemplate();
            _btnEditToggle = Btn("Edit Template", UiTheme.Accent, Color.White);
            _btnEditToggle.Click += (s, e) => TryEnterEditMode();
            _btnDataSource = Btn("Preview: Sample Data", UiTheme.Chrome, UiTheme.Ink);
            _btnDataSource.Width = 220;
            _btnDataSource.Click += (s, e) => ShowDataSourceMenu();
            _btnRedo = Btn("Redo ↷", UiTheme.Chrome, UiTheme.Ink);
            _btnRedo.Click += (s, e) => DoRedo();
            _btnUndo = Btn("↶ Undo", UiTheme.Chrome, UiTheme.Ink);
            _btnUndo.Click += (s, e) => DoUndo();

            bool inFamily = TemplateStore.FactsCertificationFamily.Contains(_info.FormCode);
            if (inFamily)
            {
                _btnApplyFooter = Btn("Apply Footer to 1A/2A/3A", UiTheme.Chrome, UiTheme.Ink);
                _btnApplyFooter.Width = 172;
                _btnApplyFooter.Click += (s, e) => ApplyBandToFamily("Footer");
                _btnApplyHeader = Btn("Apply Header to 1A/2A/3A", UiTheme.Chrome, UiTheme.Ink);
                _btnApplyHeader.Width = 172;
                _btnApplyHeader.Click += (s, e) => ApplyBandToFamily("Header");
                bar.Controls.Add(_btnApplyFooter);
                bar.Controls.Add(_btnApplyHeader);
            }

            bar.Controls.Add(btnClose);
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
            var btnAddText = MakeAdd("＋ Text");
            var btnAddField = MakeAdd("＋ Data Field");
            var btnAddImage = MakeAdd("＋ Image");
            var btnAddLine = MakeAdd("＋ Line");
            var btnAddRect = MakeAdd("＋ Rectangle");
            btnAddText.Click += (s, e) => AddTextElement();
            btnAddField.Click += (s, e) => ShowAddFieldMenu(btnAddField);
            btnAddImage.Click += (s, e) => ShowAddImageMenu(btnAddImage);
            btnAddLine.Click += (s, e) => AddLineElement();
            btnAddRect.Click += (s, e) => AddRectElement();

            var lblElements = new Label { Text = "ELEMENTS ON THIS PAGE", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(12, y + 8), AutoSize = true };
            _lstElements = new ListBox
            {
                Location = new Point(12, y + 30),
                Size = new Size(184, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
                IntegralHeight = false,
                FormattingEnabled = true,
            };
            _lstElements.Format += ListBox_Format;
            _lstElements.SelectedIndexChanged += (s, e) =>
            {
                if (_lstElements.SelectedItem is TemplateElement el) _canvas.SelectedId = el.Id;
            };

            left.Controls.Add(_lstElements);
            left.Controls.Add(lblElements);
            left.Controls.Add(btnAddText);
            left.Controls.Add(btnAddField);
            left.Controls.Add(btnAddImage);
            left.Controls.Add(btnAddLine);
            left.Controls.Add(btnAddRect);
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
            _canvas.SelectionChanged += (s, e) => { RebuildPropertiesPanel(); SyncListSelection(); };
            _canvas.BeforeMutate += (s, e) => PushUndo();
            _canvas.ElementsChanged += (s, e) => { RebuildElementsList(); RebuildPropertiesPanel(); };

            Controls.Add(_canvas);
            Controls.Add(_propsHost);
            Controls.Add(left);
            Controls.Add(top);

            RebuildPropertiesPanel();
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

        private void UpdateModeUi()
        {
            _lblMode.Text = _editable ? "● EDITING — changes are not printed until you Save" : "● PREVIEW ONLY — this layout is not being edited";
            _lblMode.ForeColor = _editable ? UiTheme.Warning : UiTheme.Muted;
            _btnSave.Visible = _editable;
            _btnUndo.Visible = _editable;
            _btnRedo.Visible = _editable;
            if (_btnApplyHeader != null) _btnApplyHeader.Visible = _editable;
            if (_btnApplyFooter != null) _btnApplyFooter.Visible = _editable;
            _btnEditToggle.Visible = !_editable;
            _canvas.ReadOnly = !_editable;
            foreach (Control c in ((Panel)Controls.Cast<Control>().First(x => x.Dock == DockStyle.Left)).Controls)
                if (c is Button b && b.Text.StartsWith("＋")) b.Enabled = _editable;
            RebuildPropertiesPanel();
        }

        private void TryEnterEditMode()
        {
            var confirm = new AdminVerificationForm();
            if (confirm.ShowDialog(this) != DialogResult.OK) return;
            _editable = true;
            UpdateModeUi();
        }

        // ============================================================== undo/redo

        private void PushUndo()
        {
            _undo.Push(_json.Serialize(_current.Elements));
            _redo.Clear();
        }

        private void DoUndo()
        {
            if (_undo.Count == 0) return;
            _redo.Push(_json.Serialize(_current.Elements));
            _current.Elements = _json.Deserialize<List<TemplateElement>>(_undo.Pop());
            AfterHistoryChange();
        }

        private void DoRedo()
        {
            if (_redo.Count == 0) return;
            _undo.Push(_json.Serialize(_current.Elements));
            _current.Elements = _json.Deserialize<List<TemplateElement>>(_redo.Pop());
            AfterHistoryChange();
        }

        private void AfterHistoryChange()
        {
            _canvas.Template = _current;
            _canvas.SelectedId = null;
            _canvas.NotifyExternalEdit();
            RebuildElementsList();
            RebuildPropertiesPanel();
        }

        // ============================================================== elements list

        private void RebuildElementsList()
        {
            TemplateElement wasSelected = _canvas.Selected;
            _lstElements.Items.Clear();
            foreach (TemplateElement el in _current.Elements.OrderByDescending(e => e.ZIndex))
                _lstElements.Items.Add(el);
            SyncListSelection();
        }

        private void ListBox_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is TemplateElement el) e.Value = Describe(el);
        }

        private static string Describe(TemplateElement el)
        {
            string label;
            switch (el.Kind)
            {
                case "Text": label = "\"" + Truncate(el.Text, 22) + "\""; break;
                case "Field": label = "{{" + (el.Column ?? "?") + "}}"; break;
                case "Image": label = "Image" + (string.IsNullOrEmpty(el.OfficeAsset) ? "" : " (" + el.OfficeAsset + ")"); break;
                case "Line": label = "Line"; break;
                case "Rectangle": label = "Rectangle"; break;
                default: label = el.Kind; break;
            }
            return "[" + el.Band + "] " + label;
        }

        private static string Truncate(string s, int n) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n) + "…");

        private void SyncListSelection()
        {
            TemplateElement sel = _canvas.Selected;
            for (int i = 0; i < _lstElements.Items.Count; i++)
                if (ReferenceEquals(_lstElements.Items[i], sel)) { _lstElements.SelectedIndex = i; return; }
            _lstElements.SelectedIndex = -1;
        }

        // ============================================================== add element helpers

        private static PointF NextSpot(CertTemplate t) =>
            new PointF(60 + (t.Elements.Count % 8) * 10, 100 + (t.Elements.Count % 8) * 14);

        private void AddTextElement()
        {
            PointF p = NextSpot(_current);
            var el = new TemplateElement { Kind = "Text", Text = "New text", X = p.X, Y = p.Y, Width = 160, Height = 16, FontSize = 9f };
            _canvas.AddElement(el);
        }

        private void AddLineElement()
        {
            PointF p = NextSpot(_current);
            var el = new TemplateElement { Kind = "Line", X = p.X, Y = p.Y, Width = 180, Height = 0, StrokeWidth = 1f };
            _canvas.AddElement(el);
        }

        private void AddRectElement()
        {
            PointF p = NextSpot(_current);
            var el = new TemplateElement { Kind = "Rectangle", X = p.X, Y = p.Y, Width = 120, Height = 60, StrokeWidth = 1f };
            _canvas.AddElement(el);
        }

        private void ShowAddFieldMenu(Control anchor)
        {
            var menu = new ContextMenuStrip();
            List<TemplateFieldOption> fields = TemplateStore.FieldsFor(_info.FormCode);
            foreach (var group in fields.GroupBy(f => f.Group))
            {
                var top = new ToolStripMenuItem(group.Key);
                foreach (TemplateFieldOption f in group.OrderBy(x => x.Label))
                {
                    var item = new ToolStripMenuItem(f.Label);
                    item.Click += (s, e) => AddFieldElement(f);
                    top.DropDownItems.Add(item);
                }
                menu.Items.Add(top);
            }
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        private void AddFieldElement(TemplateFieldOption f)
        {
            PointF p = NextSpot(_current);
            var el = new TemplateElement
            {
                Kind = "Field", Column = f.Key, X = p.X, Y = p.Y, Width = 180, Height = 14, FontSize = 9f
            };
            _canvas.AddElement(el);
        }

        private void ShowAddImageMenu(Control anchor)
        {
            var menu = new ContextMenuStrip();
            var upload = new ToolStripMenuItem("Upload a Picture...");
            upload.Click += (s, e) => UploadImageElement(null);
            menu.Items.Add(upload);
            menu.Items.Add(new ToolStripSeparator());
            foreach (AssetKind kind in Enum.GetValues(typeof(AssetKind)))
            {
                var item = new ToolStripMenuItem("Use Office " + SplitCamel(kind.ToString()));
                item.Click += (s, e) => AddOfficeAssetElement(kind);
                menu.Items.Add(item);
            }
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        private static string SplitCamel(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s, "(\\B[A-Z])", " $1");

        private void AddOfficeAssetElement(AssetKind kind)
        {
            PointF p = NextSpot(_current);
            var el = new TemplateElement { Kind = "Image", OfficeAsset = kind.ToString(), X = p.X, Y = p.Y, Width = 60, Height = 60 };
            _canvas.AddElement(el);
        }

        private void UploadImageElement(TemplateElement existing)
        {
            using (var ofd = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                byte[] bytes;
                try
                {
                    bytes = File.ReadAllBytes(ofd.FileName);
                    if (bytes.Length > 5 * 1024 * 1024)
                    { MessageBox.Show(this, "That image is larger than 5 MB. Please choose a smaller file.", "Image too large", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    using (var ms = new MemoryStream(bytes)) using (Image.FromStream(ms)) { /* validates it decodes */ }
                }
                catch
                {
                    MessageBox.Show(this, "That file could not be read as an image.", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string contentType = ofd.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                int id = TemplateStore.SaveImage(_info.FormCode, Path.GetFileNameWithoutExtension(ofd.FileName), bytes, contentType);
                _imageCache[id] = Image.FromStream(new MemoryStream(bytes));

                if (existing != null)
                {
                    PushUndo();
                    existing.ImageId = id;
                    existing.OfficeAsset = null;
                    _canvas.NotifyExternalEdit();
                    RebuildElementsList(); RebuildPropertiesPanel();
                }
                else
                {
                    PointF p = NextSpot(_current);
                    Size natural = _imageCache[id].Size;
                    float box = 120f;
                    float scale = Math.Min(box / natural.Width, box / natural.Height);
                    var el = new TemplateElement { Kind = "Image", ImageId = id, X = p.X, Y = p.Y, Width = natural.Width * scale, Height = natural.Height * scale };
                    _canvas.AddElement(el);
                }
            }
        }

        // ============================================================== properties panel

        private void RebuildPropertiesPanel()
        {
            _propsHost.SuspendLayout();
            _propsHost.Controls.Clear();

            TemplateElement el = _canvas.Selected;
            if (el == null)
            {
                var hint = new Label
                {
                    Text = _editable
                        ? "Click an element on the page to edit it, or use Add on the left to create one."
                        : "This is a preview. Click Edit Template above to make changes.",
                    ForeColor = UiTheme.Muted, AutoSize = false, Size = new Size(240, 80), Location = new Point(0, 4),
                };
                _propsHost.Controls.Add(hint);
                _propsHost.ResumeLayout();
                return;
            }

            int y = 4;
            Label Section(string text) { var l = new Label { Text = text, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(0, y), AutoSize = true }; y += 22; return l; }

            _propsHost.Controls.Add(Section(el.Kind.ToUpperInvariant() + " ELEMENT"));

            if (!_editable)
            {
                var readOnlyNote = new Label { Text = "Read-only preview.", ForeColor = UiTheme.Muted, Location = new Point(0, y), AutoSize = true };
                _propsHost.Controls.Add(readOnlyNote);
                _propsHost.ResumeLayout();
                return;
            }

            if (el.Kind == "Text")
            {
                _propsHost.Controls.Add(new Label { Text = "Text", Location = new Point(0, y), AutoSize = true }); y += 18;
                var txt = new TextBox { Text = el.Text, Multiline = true, Location = new Point(0, y), Size = new Size(238, 50) };
                txt.Leave += (s, e) => { if (txt.Text != el.Text) { PushUndo(); el.Text = txt.Text; _canvas.NotifyExternalEdit(); RebuildElementsList(); } };
                _propsHost.Controls.Add(txt); y += 58;
            }
            else if (el.Kind == "Field")
            {
                _propsHost.Controls.Add(new Label { Text = "Data Field", Location = new Point(0, y), AutoSize = true }); y += 18;
                var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, y), Size = new Size(238, 24) };
                List<TemplateFieldOption> fields = TemplateStore.FieldsFor(_info.FormCode);
                foreach (TemplateFieldOption f in fields.OrderBy(x => x.Group).ThenBy(x => x.Label))
                    cbo.Items.Add(f);
                cbo.DisplayMember = "Label";
                cbo.SelectedItem = fields.FirstOrDefault(f => string.Equals(f.Key, el.Column, StringComparison.OrdinalIgnoreCase));
                cbo.SelectedIndexChanged += (s, e) =>
                {
                    if (cbo.SelectedItem is TemplateFieldOption f && f.Key != el.Column)
                    { PushUndo(); el.Column = f.Key; _canvas.NotifyExternalEdit(); RebuildElementsList(); }
                };
                _propsHost.Controls.Add(cbo); y += 32;
            }

            if (el.Kind == "Text" || el.Kind == "Field")
            {
                y = AddFontControls(el, y);
            }

            if (el.Kind == "Image")
            {
                var replace = new Button { Text = "Replace Image...", Location = new Point(0, y), Size = new Size(238, 30), BackColor = UiTheme.Chrome, FlatStyle = FlatStyle.Flat };
                replace.Click += (s, e) => ShowReplaceImageMenu(replace, el);
                _propsHost.Controls.Add(replace); y += 36;

                var chkLock = new CheckBox { Text = "Keep proportions (don't stretch)", Checked = el.LockAspect, Location = new Point(0, y), AutoSize = true };
                chkLock.CheckedChanged += (s, e) => { PushUndo(); el.LockAspect = chkLock.Checked; _canvas.NotifyExternalEdit(); };
                _propsHost.Controls.Add(chkLock); y += 30;
            }

            if (el.Kind == "Line" || el.Kind == "Rectangle")
            {
                _propsHost.Controls.Add(new Label { Text = "Line thickness", Location = new Point(0, y), AutoSize = true }); y += 18;
                var num = new NumericUpDown { Minimum = 0.5m, Maximum = 12m, DecimalPlaces = 1, Increment = 0.5m, Value = (decimal)el.StrokeWidth, Location = new Point(0, y), Width = 90 };
                num.ValueChanged += (s, e) => { PushUndo(); el.StrokeWidth = (float)num.Value; _canvas.NotifyExternalEdit(); };
                _propsHost.Controls.Add(num); y += 32;
            }

            y = AddPositionControls(el, y);
            y = AddBandControls(el, y);
            y = AddLayerControls(el, y);

            var btnDelete = new Button { Text = "Delete Element", Location = new Point(0, y), Size = new Size(238, 30), BackColor = UiTheme.DangerTint, ForeColor = UiTheme.Danger, FlatStyle = FlatStyle.Flat };
            btnDelete.Click += (s, e) => { PushUndo(); _current.Elements.Remove(el); _canvas.SelectedId = null; _canvas.NotifyExternalEdit(); RebuildElementsList(); };
            _propsHost.Controls.Add(btnDelete);

            _propsHost.ResumeLayout();
        }

        private int AddFontControls(TemplateElement el, int y)
        {
            _propsHost.Controls.Add(new Label { Text = "Font & Style", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(0, y), AutoSize = true }); y += 20;

            var cboFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, y), Size = new Size(150, 24) };
            cboFont.Items.AddRange(new object[] { "Arial", "Times New Roman", "Segoe UI", "Calibri", "Georgia" });
            cboFont.SelectedItem = el.FontFamily; if (cboFont.SelectedIndex < 0) cboFont.SelectedIndex = 0;
            cboFont.SelectedIndexChanged += (s, e) => { PushUndo(); el.FontFamily = cboFont.SelectedItem.ToString(); _canvas.NotifyExternalEdit(); };

            var numSize = new NumericUpDown { Minimum = 5, Maximum = 48, Value = (decimal)Math.Min(48, Math.Max(5, el.FontSize)), Location = new Point(158, y), Width = 60 };
            numSize.ValueChanged += (s, e) => { PushUndo(); el.FontSize = (float)numSize.Value; _canvas.NotifyExternalEdit(); };
            _propsHost.Controls.Add(cboFont); _propsHost.Controls.Add(numSize); y += 30;

            var chkB = new CheckBox { Text = "B", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Checked = el.Bold, Location = new Point(0, y), Size = new Size(34, 26), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            var chkI = new CheckBox { Text = "I", Font = new Font("Segoe UI", 9f, FontStyle.Italic), Checked = el.Italic, Location = new Point(38, y), Size = new Size(34, 26), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            var chkU = new CheckBox { Text = "U", Font = new Font("Segoe UI", 9f, FontStyle.Underline), Checked = el.Underline, Location = new Point(76, y), Size = new Size(34, 26), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            chkB.CheckedChanged += (s, e) => { PushUndo(); el.Bold = chkB.Checked; _canvas.NotifyExternalEdit(); };
            chkI.CheckedChanged += (s, e) => { PushUndo(); el.Italic = chkI.Checked; _canvas.NotifyExternalEdit(); };
            chkU.CheckedChanged += (s, e) => { PushUndo(); el.Underline = chkU.Checked; _canvas.NotifyExternalEdit(); };

            var btnL = new Button { Text = "⯇", Location = new Point(120, y), Size = new Size(34, 26), FlatStyle = FlatStyle.Flat, BackColor = el.Align == "Left" ? UiTheme.AccentTint : UiTheme.Chrome };
            var btnC = new Button { Text = "≡", Location = new Point(158, y), Size = new Size(34, 26), FlatStyle = FlatStyle.Flat, BackColor = el.Align == "Center" ? UiTheme.AccentTint : UiTheme.Chrome };
            var btnR = new Button { Text = "⯈", Location = new Point(196, y), Size = new Size(34, 26), FlatStyle = FlatStyle.Flat, BackColor = el.Align == "Right" ? UiTheme.AccentTint : UiTheme.Chrome };
            btnL.Click += (s, e) => { PushUndo(); el.Align = "Left"; _canvas.NotifyExternalEdit(); RebuildPropertiesPanel(); };
            btnC.Click += (s, e) => { PushUndo(); el.Align = "Center"; _canvas.NotifyExternalEdit(); RebuildPropertiesPanel(); };
            btnR.Click += (s, e) => { PushUndo(); el.Align = "Right"; _canvas.NotifyExternalEdit(); RebuildPropertiesPanel(); };

            _propsHost.Controls.Add(chkB); _propsHost.Controls.Add(chkI); _propsHost.Controls.Add(chkU);
            _propsHost.Controls.Add(btnL); _propsHost.Controls.Add(btnC); _propsHost.Controls.Add(btnR);
            y += 34;
            return y;
        }

        private int AddPositionControls(TemplateElement el, int y)
        {
            _propsHost.Controls.Add(new Label { Text = "Fine Position (points)", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = UiTheme.Faint, Location = new Point(0, y), AutoSize = true }); y += 20;

            NumericUpDown Num(string caption, int x, float value, Action<float> apply)
            {
                var lbl = new Label { Text = caption, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 7.5f) };
                var num = new NumericUpDown { Minimum = -2000, Maximum = 4000, DecimalPlaces = 0, Value = (decimal)Math.Max(-2000, Math.Min(4000, value)), Location = new Point(x, y + 14), Width = 100 };
                num.ValueChanged += (s, e) => { PushUndo(); apply((float)num.Value); _canvas.NotifyExternalEdit(); };
                _propsHost.Controls.Add(lbl); _propsHost.Controls.Add(num);
                return num;
            }
            Num("X", 0, el.X, v => el.X = v);
            Num("Y", 120, el.Y, v => el.Y = v);
            y += 40;
            Num("Width", 0, el.Width, v => el.Width = Math.Max(4, v));
            Num("Height", 120, el.Height, v => el.Height = Math.Max(4, v));
            y += 40;
            return y;
        }

        private int AddBandControls(TemplateElement el, int y)
        {
            _propsHost.Controls.Add(new Label { Text = "Section", Location = new Point(0, y), AutoSize = true }); y += 18;
            var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, y), Size = new Size(150, 24) };
            cbo.Items.AddRange(new object[] { "Header", "Body", "Footer" });
            cbo.SelectedItem = el.Band; if (cbo.SelectedIndex < 0) cbo.SelectedIndex = 1;
            cbo.SelectedIndexChanged += (s, e) => { PushUndo(); el.Band = cbo.SelectedItem.ToString(); RebuildElementsList(); };
            _propsHost.Controls.Add(cbo); y += 32;
            return y;
        }

        private int AddLayerControls(TemplateElement el, int y)
        {
            _propsHost.Controls.Add(new Label { Text = "Layer Order", Location = new Point(0, y), AutoSize = true }); y += 18;
            Button L(string text, int x, int dir) { var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(57, 26), FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Chrome, Font = new Font("Segoe UI", 7f) }; b.Click += (s, e) => _canvas.Reorder(el, dir); return b; }
            _propsHost.Controls.Add(L("To Front", 0, 1000));
            _propsHost.Controls.Add(L("Forward", 61, 1));
            _propsHost.Controls.Add(L("Back", 122, -1));
            _propsHost.Controls.Add(L("To Back", 183, -1000));
            y += 34;
            return y;
        }

        private void ShowReplaceImageMenu(Control anchor, TemplateElement el)
        {
            var menu = new ContextMenuStrip();
            var upload = new ToolStripMenuItem("Upload a Picture...");
            upload.Click += (s, e) => UploadImageElement(el);
            menu.Items.Add(upload);
            menu.Items.Add(new ToolStripSeparator());
            foreach (AssetKind kind in Enum.GetValues(typeof(AssetKind)))
            {
                var item = new ToolStripMenuItem("Use Office " + SplitCamel(kind.ToString()));
                item.Click += (s, e) => { PushUndo(); el.OfficeAsset = kind.ToString(); el.ImageId = null; _canvas.NotifyExternalEdit(); RebuildElementsList(); };
                menu.Items.Add(item);
            }
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        // ============================================================== data source (sample vs real record)

        private void LoadSampleValues()
        {
            var sample = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (TemplateFieldOption f in TemplateStore.FieldsFor(_info.FormCode))
                sample[f.Key] = "[" + f.Label + "]";
            _canvas.Values = sample;
            _btnDataSource.Text = "Preview: Sample Data";
        }

        private void ShowDataSourceMenu()
        {
            var menu = new ContextMenuStrip();
            var sampleItem = new ToolStripMenuItem("Sample Data");
            sampleItem.Click += (s, e) => { LoadSampleValues(); _canvas.NotifyExternalEdit(); };
            var pickItem = new ToolStripMenuItem("Preview With an Actual Record...");
            pickItem.Click += (s, e) => PickRealRecord();
            menu.Items.Add(sampleItem);
            menu.Items.Add(pickItem);
            menu.Show(_btnDataSource, new Point(0, _btnDataSource.Height));
        }

        private void PickRealRecord()
        {
            string view = _info.FormCode == Form3ACert.FormCode ? "v_marriage_certificate"
                        : _info.FormCode == Form3CCert.FormCode ? "v_death_certificate"
                        : "v_birth_certificate";
            string nameExpr = _info.FormCode == Form3ACert.FormCode ? "CONCAT(husband_full_name, ' & ', wife_full_name)"
                             : _info.FormCode == Form3CCert.FormCode ? "deceased_full_name"
                             : "child_full_name";

            DataTable dt;
            try
            {
                dt = Db.Pull("SELECT record_id, " + nameExpr + " AS who FROM " + view +
                             " ORDER BY record_id DESC LIMIT 40");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load records: " + ex.Message, "Preview with a record",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(this, "No registered records were found to preview with.", "Preview with a record",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var pick = new Form
            {
                Text = "Choose a record to preview with", Size = new Size(420, 460),
                StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
            })
            {
                var lst = new ListBox { Dock = DockStyle.Fill };
                foreach (DataRow r in dt.Rows)
                    lst.Items.Add(new RecordPick { Id = Convert.ToInt32(r["record_id"]), Who = r["who"]?.ToString() ?? "(unnamed)" });
                lst.DisplayMember = "Who";
                int chosen = -1;
                lst.DoubleClick += (s, e) => { if (lst.SelectedItem is RecordPick rp) { chosen = rp.Id; pick.DialogResult = DialogResult.OK; } };
                var btnOk = new Button { Text = "Preview With This Record", Dock = DockStyle.Bottom, Height = 34, DialogResult = DialogResult.OK };
                btnOk.Click += (s, e) => { if (lst.SelectedItem is RecordPick rp) chosen = rp.Id; };
                pick.Controls.Add(lst);
                pick.Controls.Add(btnOk);
                if (pick.ShowDialog(this) == DialogResult.OK && chosen >= 0)
                    ApplyRealRecord(chosen, dt.Rows.Cast<DataRow>().First(r => Convert.ToInt32(r["record_id"]) == chosen)["who"]?.ToString());
            }
        }

        private class RecordPick { public int Id; public string Who; public override string ToString() => Who; }

        private void ApplyRealRecord(int recordId, string who)
        {
            DataTable table = _info.FormCode == Form3ACert.FormCode ? Form3ACert.BuildTable(recordId)
                             : _info.FormCode == Form3CCert.FormCode ? Form3CCert.BuildTable(recordId)
                             : Form3BCert.BuildTable(recordId);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (table.Rows.Count > 0)
                foreach (DataColumn col in table.Columns)
                    values[col.ColumnName] = table.Rows[0][col] as string ?? "";

            _canvas.Values = values;
            _realRecordLabel = who;
            _btnDataSource.Text = "Preview: " + Truncate(who ?? "Record", 18);
            _canvas.NotifyExternalEdit();
        }

        // ============================================================== save / close

        private Image LoadTemplateImage(int id)
        {
            if (_imageCache.TryGetValue(id, out Image cached)) return cached;
            byte[] bytes = TemplateStore.LoadImage(id);
            if (bytes == null) return null;
            Image img = Image.FromStream(new MemoryStream(bytes));
            _imageCache[id] = img;
            return img;
        }

        private void SaveTemplate()
        {
            _current.Name = _info.FormName;
            _current.FormCode = _info.FormCode;
            TemplateStore.Save(_current, Session.User?.Id);
            Audit.Write("Update", "certificate_templates", 0, "Saved template for " + _info.FormCode);
            _savedJson = _json.Serialize(_current.Elements);
            MessageBox.Show(this, "Template saved. New certificates for this form will use this layout.",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>"Apply Header/Footer to 1A, 2A and 3A": takes this form's CURRENT (possibly
        /// unsaved) elements of the given band and overwrites that band on every OTHER form in the
        /// family, so the office's letterhead/footer stay one coordinated design across Marriage,
        /// Death and Birth Facts Certifications. Confirms first — this rewrites data the operator
        /// is not currently looking at. Does not touch this form's own template or its Save state.</summary>
        private void ApplyBandToFamily(string band)
        {
            List<TemplateElement> bandElements = _current.Elements
                .Where(e => string.Equals(e.Band, band, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (bandElements.Count == 0)
            {
                MessageBox.Show(this, "There is nothing in the " + band + " section of this page to apply.",
                    "Nothing to apply", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] others = TemplateStore.FactsCertificationFamily
                .Where(f => f != _info.FormCode).ToArray();
            string otherNames = string.Join(" and ", others.Select(f => TemplateStore.FindForm(f)?.FormName ?? f));

            DialogResult confirm = MessageBox.Show(this,
                "This will replace the " + band + " section on " + otherNames +
                " with the " + band.ToLowerInvariant() + " shown here" +
                (IsDirty() ? " (including your unsaved changes)" : "") +
                ".\n\nThis cannot be undone from this screen. Continue?",
                "Apply " + band + " to 1A, 2A and 3A", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                TemplateStore.ApplyBand(bandElements, band, others, Session.User?.Id);
                Audit.Write("Update", "certificate_templates", 0,
                    "Applied " + band + " from " + _info.FormCode + " to " + string.Join(", ", others));
                MessageBox.Show(this,
                    "The " + band.ToLowerInvariant() + " has been applied to " + otherNames + ".",
                    "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not apply the " + band.ToLowerInvariant() + ": " + ex.Message,
                    "Apply failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsDirty() => _editable && _json.Serialize(_current.Elements) != _savedJson;

        private void TemplateDesignerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsDirty()) return;
            DialogResult r = MessageBox.Show(this,
                "You have unsaved changes. Do you want to leave without saving?",
                "Unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) e.Cancel = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _canvas.LoadTemplateImage = LoadTemplateImage;
            _canvas.UpdateCanvasSize();
        }
    }
}
