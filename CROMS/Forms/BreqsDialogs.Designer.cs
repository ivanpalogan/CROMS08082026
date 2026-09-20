using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    internal partial class BreqsRequestDialog
    {
        private readonly TextBox _rFirst = MUi.Box(), _rMiddle = MUi.Box(), _rLast = MUi.Box(), _contact = MUi.Box(), _idNo = MUi.Box();
        private readonly ComboBox _relationship = MUi.Combo(true, BreqsService.Relationships), _idType = MUi.Combo(true, GovIds.All);
        private readonly ComboBox _docType = MUi.Combo(false, BreqsService.DocTypes), _purpose = MUi.Combo(true, BreqsService.Purposes);
        private readonly NumericUpDown _copies = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 1, Dock = DockStyle.Fill, Font = MUi.F(9.75F), Margin = new Padding(0, 0, 10, 0) };
        private readonly TextBox _oFirst = MUi.Box(), _oMiddle = MUi.Box(), _oLast = MUi.Box(), _sFirst = MUi.Box(), _sMiddle = MUi.Box(), _sLast = MUi.Box();
        private readonly DateTimePicker _eventDate = MUi.Date(true);
        private readonly ComboBox _province = MUi.Combo(true), _city = MUi.Combo(true);
        private readonly TextBox _father = MUi.Box(), _mother = MUi.Box();
        private readonly Label _ownerHead = MUi.Txt("", 10F, FontStyle.Bold), _fee = MUi.Txt("", 9.5F, FontStyle.Bold, UiTheme.Accent);
        private Label _dateCap;
        private Control _spouseBlock, _parentsBlock;
        private readonly Panel _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(22, 12, 22, 12), BackColor = UiTheme.Surface };
        private readonly Label _hint = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Warning);

        private void InitializeComponent()
        {
            Text = _r.Id > 0 ? "Edit PSA copy request " + _r.RequestNo : "New PSA copy request (BREQS)";
            StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(780, 820); MinimumSize = new Size(700, 600); BackColor = UiTheme.Surface;

            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 10), BackColor = Color.FromArgb(250, 251, 253) };
            var save = MUi.Btn(_r.Id > 0 ? "Save changes" : "Log request", MUi.Kind.Primary, 140);
            var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
            foot.Controls.Add(save); foot.Controls.Add(cancel); foot.Controls.Add(_fee);
            _fee.Margin = new Padding(0, 9, 20, 0);
            save.Click += (x, e) => DoSave();
            CancelButton = cancel;

            _hint.Dock = DockStyle.Top; _hint.AutoSize = false; _hint.Height = 0; _hint.Padding = new Padding(0, 0, 0, 6);

            var docRow = MUi.Grid(3, 1, 56);
            docRow.Controls.Add(MUi.Field("Certificate needed", _docType), 0, 0);
            docRow.Controls.Add(MUi.Field("Copies", _copies), 1, 0);
            docRow.Controls.Add(MUi.Field("Purpose", _purpose), 2, 0);

            _ownerHead.Height = 30; _ownerHead.AutoSize = false; _ownerHead.Padding = new Padding(0, 8, 0, 0);
            var ownerNames = Names(_oFirst, _oMiddle, _oLast);
            var spouseHead = MUi.Txt("Wife", 10F, FontStyle.Bold); spouseHead.AutoSize = false; spouseHead.Height = 30; spouseHead.Padding = new Padding(0, 8, 0, 0);
            var spouseNames = Names(_sFirst, _sMiddle, _sLast);
            _spouseBlock = Block(spouseHead, spouseNames);

            var eventRow = MUi.Grid(3, 1, 56);
            Control dateField = MUi.Field("Date", _eventDate);
            _dateCap = (Label)dateField.Controls[0];
            eventRow.Controls.Add(dateField, 0, 0);
            eventRow.Controls.Add(MUi.Field("Province", _province), 1, 0);
            eventRow.Controls.Add(MUi.Field("City / municipality", _city), 2, 0);
            var parents = MUi.Grid(2, 1, 56);
            parents.Controls.Add(MUi.Field("Father's full name", _father), 0, 0);
            parents.Controls.Add(MUi.Field("Mother's full maiden name", _mother), 1, 0);
            _parentsBlock = parents;

            var req1 = Names(_rFirst, _rMiddle, _rLast);
            var req2 = MUi.Grid(2, 1, 56);
            req2.Controls.Add(MUi.Field("Relationship to the document owner", _relationship), 0, 0);
            req2.Controls.Add(MUi.Field("Contact number", _contact), 1, 0);
            var req3 = MUi.Grid(2, 1, 56);
            req3.Controls.Add(MUi.Field("Valid ID presented", _idType), 0, 0);
            req3.Controls.Add(MUi.Field("ID number", _idNo), 1, 0);

            Stack(_body, _hint,
                  MUi.SectionHeader("Requester", "The person asking for the PSA copy. PSA releases a civil registry document only to its owner or someone with a right to it."),
                  req1, req2, req3,
                  MUi.SectionHeader("Document", "What PSA is being asked for. The more of it is filled in, the faster PSA finds the record."),
                  docRow, _ownerHead, ownerNames, _spouseBlock, eventRow, _parentsBlock);

            Controls.Add(_body); Controls.Add(foot);

            GeoLookup.LoadProvinces(_province);
            GeoLookup.CascadePlace(_province, _city);
            _docType.SelectedIndexChanged += (x, e) => ApplyDocType();
            _copies.ValueChanged += (x, e) => UpdateFee();
            Fill();
            UiTheme.Polish(this);
            Shown += (x, e) =>
            {
                if (!string.IsNullOrEmpty(Hint)) { _hint.Text = Hint + "  -  type the name into First / Middle / Last below."; _hint.Height = 26; }
                _rFirst.Focus();
            };
        }

        private static TableLayoutPanel Names(TextBox f, TextBox m, TextBox l)
        {
            var g = MUi.Grid(3, 1, 56);
            g.Controls.Add(MUi.Field("First name", f), 0, 0);
            g.Controls.Add(MUi.Field("Middle name", m), 1, 0);
            g.Controls.Add(MUi.Field("Last name", l), 2, 0);
            return g;
        }

        private static Control Block(params Control[] parts)
        {
            var p = new Panel { Height = parts.Sum(c => c.Height), BackColor = Color.Transparent };
            Stack(p, parts);
            return p;
        }

        private static void Stack(Control host, params Control[] topToBottom)
        {
            for (int i = topToBottom.Length - 1; i >= 0; i--) { topToBottom[i].Dock = DockStyle.Top; host.Controls.Add(topToBottom[i]); }
            for (int i = 0; i < topToBottom.Length; i++) topToBottom[i].TabIndex = i;
        }
    }

    internal static partial class BreqsDialogs
    {
        private static Form Dialog(string title, int w, int h)
        {
            return new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(w, h), BackColor = UiTheme.Surface, ShowInTaskbar = false
            };
        }

        private static Control Body(Form f, string intro, params Control[] rows)
        {
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 14, 22, 8), BackColor = UiTheme.Surface };
            var list = new List<Control>();
            if (intro != null)
            {
                var l = MUi.Txt(intro, 9.5F, FontStyle.Regular, UiTheme.Muted); l.AutoSize = false; l.Height = 44;
                list.Add(l);
            }
            list.AddRange(rows);
            for (int i = list.Count - 1; i >= 0; i--) { list[i].Dock = DockStyle.Top; body.Controls.Add(list[i]); }
            for (int i = 0; i < list.Count; i++) list[i].TabIndex = i;
            return body;
        }

        private static FlowLayoutPanel Foot(Form f, string okText, MUi.Kind kind, Func<bool> onOk)
        {
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 11, 16, 9), BackColor = Color.FromArgb(250, 251, 253) };
            var ok = MUi.Btn(okText, kind, 170);
            var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
            ok.Click += (x, e) =>
            {
                try { if (onOk()) { f.DialogResult = DialogResult.OK; f.Close(); } }
                catch (Exception ex) { MUi.Fail(f, ex); }
            };
            foot.Controls.Add(ok); foot.Controls.Add(cancel);
            f.CancelButton = cancel;
            return foot;
        }
    }
}
