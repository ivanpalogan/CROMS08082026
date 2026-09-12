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
    /// <summary>New / edit a BREQS request. The document section follows the certificate type.</summary>
    internal sealed class BreqsRequestDialog : Form
    {
        private readonly BreqsRequest _r;
        private readonly BreqsSettings _s;
        public int SavedId { get; private set; }
        /// <summary>Shown above the form - e.g. the joined name from a queue ticket, for staff to type in properly.</summary>
        public string Hint { get; set; }

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

        public BreqsRequestDialog(BreqsRequest r, BreqsSettings s)
        {
            _r = r; _s = s;
            Text = r.Id > 0 ? "Edit PSA copy request " + r.RequestNo : "New PSA copy request (BREQS)";
            StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(780, 820); MinimumSize = new Size(700, 600); BackColor = UiTheme.Surface;

            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 10), BackColor = Color.FromArgb(250, 251, 253) };
            var save = MUi.Btn(r.Id > 0 ? "Save changes" : "Log request", MUi.Kind.Primary, 140);
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

        private void Fill()
        {
            _rFirst.Text = _r.RequesterFirst; _rMiddle.Text = _r.RequesterMiddle; _rLast.Text = _r.RequesterLast;
            _contact.Text = _r.ContactNo; _relationship.Text = _r.Relationship ?? ""; _idType.Text = _r.ValidIdType ?? ""; _idNo.Text = _r.ValidIdNo;
            _docType.SelectedItem = BreqsService.DocTypes.Contains(_r.DocType) ? _r.DocType : BreqsService.Birth;
            _copies.Value = Math.Max(1, Math.Min(20, _r.Copies));
            _purpose.Text = _r.Purpose ?? "";
            _oFirst.Text = _r.OwnerFirst; _oMiddle.Text = _r.OwnerMiddle; _oLast.Text = _r.OwnerLast;
            _sFirst.Text = _r.SpouseFirst; _sMiddle.Text = _r.SpouseMiddle; _sLast.Text = _r.SpouseLast;
            MUi.Put(_eventDate, _r.EventDate);
            GeoLookup.Select(_province, _r.EventProvince);
            GeoLookup.Select(_city, _r.EventCity);
            _father.Text = _r.FatherName; _mother.Text = _r.MotherMaidenName;
            ApplyDocType();
        }

        private void ApplyDocType()
        {
            string t = _docType.SelectedItem as string ?? BreqsService.Birth;
            _ownerHead.Text = t == BreqsService.Marriage ? "Husband" : t == BreqsService.Death ? "Name of the deceased" : "Name on the birth certificate";
            _dateCap.Text = ("Date of " + t).ToUpperInvariant();
            _spouseBlock.Visible = t == BreqsService.Marriage;
            _parentsBlock.Visible = t == BreqsService.Birth;
            UpdateFee();
        }

        private void UpdateFee()
        {
            _fee.Text = "Fee: PHP " + (_s.FeePerCopy * _copies.Value).ToString("#,0.00") + "  (" + _s.FeePerCopy.ToString("0") + " x " + _copies.Value + ")";
        }

        private void DoSave()
        {
            _r.RequesterFirst = _rFirst.Text; _r.RequesterMiddle = _rMiddle.Text; _r.RequesterLast = _rLast.Text;
            _r.ContactNo = _contact.Text; _r.Relationship = _relationship.Text; _r.ValidIdType = _idType.Text; _r.ValidIdNo = _idNo.Text;
            _r.DocType = _docType.SelectedItem as string; _r.Copies = (int)_copies.Value; _r.Purpose = _purpose.Text;
            _r.OwnerFirst = _oFirst.Text; _r.OwnerMiddle = _oMiddle.Text; _r.OwnerLast = _oLast.Text;
            _r.SpouseFirst = _sFirst.Text; _r.SpouseMiddle = _sMiddle.Text; _r.SpouseLast = _sLast.Text;
            _r.EventDate = MUi.Val(_eventDate); _r.EventProvince = _province.Text; _r.EventCity = _city.Text;
            _r.FatherName = _father.Text; _r.MotherMaidenName = _mother.Text;

            List<string> errors = BreqsService.Validate(_r);
            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", errors.Select(x => "- " + x)), "Not saved yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                SavedId = BreqsService.Save(_r, Session.User == null ? (int?)null : Session.User.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }
    }

    /// <summary>The small step dialogs of a BREQS request.</summary>
    internal static class BreqsDialogs
    {
        private static int? Uid { get { return Session.User == null ? (int?)null : Session.User.Id; } }

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

        public static bool Payment(IWin32Window owner, BreqsRequest r, BreqsSettings s)
        {
            using (Form f = Dialog("Record payment - " + r.RequestNo, 520, 330))
            {
                TextBox or = MUi.Box(); DateTimePicker date = MUi.Date(false); TextBox amount = MUi.Box();
                date.MaxDate = DateTime.Today; date.Value = DateTime.Today;
                amount.Text = (r.FeeAmount ?? s.FeePerCopy * r.Copies).ToString("0.00", CultureInfo.InvariantCulture);
                var g = MUi.Grid(2, 1, 56);
                g.Controls.Add(MUi.Field("Date paid", date), 0, 0); g.Controls.Add(MUi.Field("Amount (PHP)", amount), 1, 0);
                Control body = Body(f, "The fee is paid at the Municipal Treasury. Record the official receipt it issued - CROMS does not collect money.",
                                    MUi.Field("Treasury official receipt no.", or), g);
                f.Controls.Add(body);
                f.Controls.Add(Foot(f, "Record payment", MUi.Kind.Primary, () =>
                {
                    decimal amt;
                    if (!decimal.TryParse(amount.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amt) || amt < 0)
                    { MessageBox.Show(f, "Enter the amount on the receipt.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
                    BreqsService.RecordPayment(r.Id, or.Text, date.Value.Date, amt, Uid);
                    return true;
                }));
                UiTheme.Polish(f);
                f.Shown += (x, e) => or.Focus();
                return f.ShowDialog(owner) == DialogResult.OK;
            }
        }

        public static bool Submit(IWin32Window owner, BreqsRequest r, BreqsSettings s)
        {
            using (Form f = Dialog("Submit to PSA - " + r.RequestNo, 520, 330))
            {
                TextBox reference = MUi.Box(); DateTimePicker date = MUi.Date(false);
                date.MaxDate = DateTime.Today; date.Value = DateTime.Today;
                Label expected = MUi.Txt("", 9.5F, FontStyle.Bold, UiTheme.Accent); expected.AutoSize = false; expected.Height = 30;
                Action upd = () => expected.Text = "Expected from PSA: " + MUi.D(date.Value.Date.AddDays(s.TurnaroundDays)) + "  (" + s.TurnaroundDays + "-day office estimate)";
                date.ValueChanged += (x, e) => upd(); upd();
                var g = MUi.Grid(2, 1, 56);
                g.Controls.Add(MUi.Field("BREQS / PSA reference no.", reference), 0, 0); g.Controls.Add(MUi.Field("Date submitted", date), 1, 0);
                f.Controls.Add(Body(f, "Record the request as sent through PSA's BREQS. The reference is what to quote when following up with PSA.", g, expected));
                f.Controls.Add(Foot(f, "Mark submitted", MUi.Kind.Primary, () =>
                {
                    if (string.IsNullOrWhiteSpace(reference.Text) &&
                        MessageBox.Show(f, "No PSA reference number was entered. Submit without one?", "Submit to PSA", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return false;
                    BreqsService.SubmitToPsa(r.Id, reference.Text, date.Value.Date, Uid);
                    return true;
                }));
                UiTheme.Polish(f);
                return f.ShowDialog(owner) == DialogResult.OK;
            }
        }

        public static bool Release(IWin32Window owner, BreqsRequest r)
        {
            if (r.OcrMatch != "Match" &&
                MessageBox.Show(owner, "The scanned PSA copy was flagged: " + (r.OcrMatch ?? "not checked") + (string.IsNullOrEmpty(r.OcrName) ? "" : " (name read: " + r.OcrName + ")") +
                    ".\n\nHave you checked with your own eyes that it is the document " + r.RequesterName + " asked for?",
                    "Check the document first", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return false;

            using (Form f = Dialog("Release PSA copy - " + r.RequestNo, 560, 380))
            {
                TextBox claimant = MUi.Box(); ComboBox idType = MUi.Combo(true, GovIds.All); TextBox idNo = MUi.Box();
                var rep = new CheckBox { Text = "Claimed by an authorised representative (not the requester)", AutoSize = true, Height = 30, Font = MUi.F(9.5F) };
                claimant.Text = r.RequesterName; idType.Text = r.ValidIdType ?? ""; idNo.Text = r.ValidIdNo ?? "";
                rep.CheckedChanged += (x, e) =>
                {
                    if (rep.Checked) { claimant.Text = ""; idType.Text = ""; idNo.Text = ""; claimant.Focus(); }
                    else { claimant.Text = r.RequesterName; idType.Text = r.ValidIdType ?? ""; idNo.Text = r.ValidIdNo ?? ""; }
                };
                var g = MUi.Grid(2, 1, 56);
                g.Controls.Add(MUi.Field("Valid ID presented", idType), 0, 0); g.Controls.Add(MUi.Field("ID number", idNo), 1, 0);
                f.Controls.Add(Body(f, "Hand over " + r.Copies + " PSA " + r.DocType.ToLowerInvariant() + " cop" + (r.Copies == 1 ? "y" : "ies") + " for " + r.OwnerName + ". Check the claimant's ID against what is recorded.",
                                    rep, MUi.Field("Claimant's full name", claimant), g));
                f.Controls.Add(Foot(f, "Release", MUi.Kind.Success, () =>
                {
                    BreqsService.Release(r.Id, claimant.Text, idType.Text, idNo.Text, rep.Checked, Uid);
                    return true;
                }));
                UiTheme.Polish(f);
                return f.ShowDialog(owner) == DialogResult.OK;
            }
        }

        /// <summary>
        /// Receive the PSA copy: choose the scan, run it through the same OCR engine as Intelligent
        /// Document Processing, show what it read and whether the name matches the request, then attach.
        /// OCR is evidence for the staff member, not a gate - an unreadable copy can still be attached.
        /// </summary>
        public static bool Receive(IWin32Window owner, BreqsRequest r)
        {
            using (Form f = Dialog("Receive PSA copy - " + r.RequestNo, 1040, 700))
            {
                f.FormBorderStyle = FormBorderStyle.Sizable; f.MaximizeBox = true; f.MinimumSize = new Size(900, 600);
                byte[] bytes = null;
                DocAiResult ocr = null;

                var pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(226, 230, 236) };
                var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 14, 8, 14), BackColor = UiTheme.Surface };
                var choose = MUi.Btn("Choose scanned PSA copy...", MUi.Kind.Primary, 230); choose.Dock = DockStyle.Top;
                left.Controls.Add(pic); left.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 }); left.Controls.Add(choose);

                var right = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(10, 14, 18, 14), BackColor = UiTheme.Surface };
                Label status = MUi.Txt("Scan the PSA copy that arrived for this request, then choose the image file.", 9.5F, FontStyle.Regular, UiTheme.Muted);
                status.AutoSize = false; status.Height = 60;
                Label read = MUi.Txt("", 9.5F); read.AutoSize = false; read.Height = 130;
                Label verdict = MUi.Txt("", 10.5F, FontStyle.Bold); verdict.AutoSize = false; verdict.Height = 56;
                TextBox security = MUi.Box();
                var expect = MUi.Kv("Requested", r.DocumentLine);
                var rows = new Control[] { MUi.Cap("This request"), expect, MUi.Cap("What OCR read"), status, read, verdict,
                                           MUi.Field("PSA security paper control no. (optional)", security) };
                for (int i = rows.Length - 1; i >= 0; i--) { rows[i].Dock = DockStyle.Top; right.Controls.Add(rows[i]); }

                FlowLayoutPanel foot = null;
                Button ok = null;
                foot = Foot(f, "Attach and mark received", MUi.Kind.Success, () =>
                {
                    if (bytes == null) return false;
                    string first, last; BreqsService.OcrOwner(r.DocType, ocr, out first, out last);
                    string match = BreqsService.CompareName(r, first, last);
                    bool wrongKind = ocr != null && ocr.Kind != DocKind.Unknown && ocr.Kind.ToString() != r.DocType;
                    if ((wrongKind || match == "Mismatch") &&
                        MessageBox.Show(f, "OCR says this copy may not be the requested document (" + (wrongKind ? "it reads as a " + ocr.Kind + " certificate" : "the name is different") +
                            ").\n\nAttach it anyway? It will stay flagged for checking before release.", "Check the copy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return false;
                    BreqsService.ReceiveFromPsa(r.Id, bytes, ocr, security.Text, Uid);
                    return true;
                });
                ok = foot.Controls.OfType<Button>().First();
                ok.Enabled = false;

                choose.Click += async (x, e) =>
                {
                    using (var dlg = new OpenFileDialog { Filter = "Scanned image (*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Title = "Choose the scanned PSA copy" })
                    {
                        if (dlg.ShowDialog(f) != DialogResult.OK) return;
                        try
                        {
                            bytes = File.ReadAllBytes(dlg.FileName);
                            Bitmap bmp = DocumentAI.LoadImage(dlg.FileName);
                            if (pic.Image != null) pic.Image.Dispose();
                            pic.Image = new Bitmap(bmp);
                            ok.Enabled = false; choose.Enabled = false;
                            ocr = null; read.Text = ""; verdict.Text = "";
                            status.Text = "Reading the copy... (this takes a few seconds)";
                            if (OcrService.IsAvailable())
                                ocr = await Task.Run(() => DocumentAI.Analyze(bmp));
                            ShowResult(r, ocr, status, read, verdict);
                        }
                        catch (Exception ex)
                        {
                            ocr = null;
                            status.Text = "OCR could not read this file (" + ex.Message + "). You can still attach it after checking it yourself.";
                            verdict.Text = "Not read - check by eye"; verdict.ForeColor = UiTheme.Warning;
                        }
                        finally { choose.Enabled = true; ok.Enabled = bytes != null; }
                    }
                };

                f.Controls.Add(left); f.Controls.Add(right); f.Controls.Add(foot);
                UiTheme.Polish(f);
                f.FormClosed += (x, e) => { if (pic.Image != null) pic.Image.Dispose(); };
                return f.ShowDialog(owner) == DialogResult.OK;
            }
        }

        private static void ShowResult(BreqsRequest r, DocAiResult ocr, Label status, Label read, Label verdict)
        {
            if (ocr == null)
            {
                status.Text = "The OCR engine is not available on this PC. Check the copy by eye before attaching.";
                verdict.Text = "Not read - check by eye"; verdict.ForeColor = UiTheme.Warning;
                return;
            }
            string first, last;
            BreqsService.OcrOwner(r.DocType, ocr, out first, out last);
            string match = BreqsService.CompareName(r, first, last);
            bool wrongKind = ocr.Kind != DocKind.Unknown && ocr.Kind.ToString() != r.DocType;
            status.Text = "Read at " + ocr.OcrConfidence + "% recognition.";
            read.Text = "Document type:  " + (ocr.Kind == DocKind.Unknown ? "not recognised" : ocr.Kind + " certificate (" + ocr.ClassifyConfidence + "%)") +
                        "\nName on the copy:  " + (MarriageRules.JoinName(first, null, last) ?? "(not read)") +
                        "\nName requested:  " + r.OwnerName;
            if (wrongKind) { verdict.Text = "Different document type - check it"; verdict.ForeColor = UiTheme.Danger; }
            else if (match == "Match") { verdict.Text = "Name matches the request"; verdict.ForeColor = UiTheme.Success; }
            else if (match == "Partial") { verdict.Text = "Last name matches - check the first name"; verdict.ForeColor = UiTheme.Warning; }
            else if (match == "Unread") { verdict.Text = "Name not read - check by eye"; verdict.ForeColor = UiTheme.Warning; }
            else { verdict.Text = "Name does NOT match - check it"; verdict.ForeColor = UiTheme.Danger; }
        }
    }
}
