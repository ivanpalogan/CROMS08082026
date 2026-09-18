using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    // =====================================================================
    // Shared building blocks for the marriage windows (Desk, Form 90, Issue,
    // Form 97, Case, Record, PSA Transmittal). One file so every window states a
    // status, a finding and a requirement the same way. Built on UiTheme tokens,
    // CardPanel and StatusPill - no new visual language, no third-party kit.
    // =====================================================================

    internal static class MUi
    {
        public static Font F(float size, FontStyle style = FontStyle.Regular) { return new Font("Segoe UI", size, style); }

        /// <summary>Status -> (tint, ink). The pill always carries the WORD too - colour is never the only signal.</summary>
        public static void ToneOf(string status, out Color tint, out Color ink)
        {
            switch (status ?? "")
            {
                case "Ready to Issue": case "Ready for Posting": case "Valid": case "Registered": case "Acknowledged":
                case "Verified": case "Filed": case "Released": case "Approved": case "Completed": case "Timely":
                case "Ready to Register":
                    tint = UiTheme.SuccessTint; ink = UiTheme.Success; return;
                case "Posting": case "Posting Complete": case "Expiring": case "Requirements Incomplete": case "Delayed":
                case "In Batch": case "Required": case "Submitted": case "On Hold": case "Pending": case "For Review":
                case "Pending Transmittal": case "Waived": case "Prepared":
                    tint = UiTheme.WarningTint; ink = UiTheme.Warning; return;
                case "Expired": case "Returned": case "Rejected": case "Cancelled": case "Missing":
                case "Returned - Needs Correction": case "Hard stop":
                    tint = UiTheme.DangerTint; ink = UiTheme.Danger; return;
                case "Issued": case "Sent": case "Sent to PSA/OCRG": case "Licensed": case "Exempt":
                    tint = UiTheme.AccentTint; ink = UiTheme.Accent; return;
                default:
                    tint = UiTheme.Chrome; ink = UiTheme.Muted; return;
            }
        }

        public static Color InkOf(string status) { Color t, i; ToneOf(status, out t, out i); return i; }

        public static StatusPill Pill(string text, string toneStatus = null)
        {
            var p = new StatusPill { Text = text ?? "-", Margin = new Padding(0, 2, 6, 2) };
            Color t, i; ToneOf(toneStatus ?? text, out t, out i);
            p.SetTone(t, i);
            return p;
        }

        public static void SetPill(StatusPill p, string text, string toneStatus = null)
        {
            Color t, i; ToneOf(toneStatus ?? text, out t, out i);
            p.Text = text ?? "-";
            p.SetTone(t, i);
        }

        // ---------------------------------------------------------------- record type
        /// <summary>
        /// Civil register -> (tint, ink) for a record-type badge. Birth and Death REUSE the
        /// palette's existing blue and neutral (the same Accent/Chrome pair every status pill
        /// on the marriage windows already uses); Marriage is the one hue UiTheme had to add.
        ///
        /// Colour is NEVER the only signal: every caller also prints the word "Birth",
        /// "Marriage" or "Death", so the badge reads correctly in greyscale, on a projector,
        /// and to a colour-blind operator. Same rule as ToneOf above.
        /// </summary>
        public static void RecordTone(string type, out Color tint, out Color ink)
        {
            switch (type ?? "")
            {
                case "Birth":    tint = UiTheme.AccentTint;   ink = UiTheme.Accent;   return;
                case "Marriage": tint = UiTheme.MarriageTint; ink = UiTheme.Marriage; return;
                // The death badge is the palette's neutral chip, deepened a little: plain Chrome
                // on a zebra-striped row is only a few points off the row behind it, and a
                // badge that cannot be told from its own background is not a badge. Stated as
                // a relationship to the two tokens (UiTheme.Mix) rather than as a new literal.
                case "Death":    tint = UiTheme.Mix(UiTheme.Chrome, UiTheme.Muted, 0.12f); ink = UiTheme.Muted; return;
                default:         tint = UiTheme.Chrome;       ink = UiTheme.Muted;    return;
            }
        }

        /// <summary>A record-type badge: the register's own colour, with its name spelled out.</summary>
        public static StatusPill RecordPill(string type)
        {
            var p = new StatusPill
            {
                Text = string.IsNullOrEmpty(type) ? "-" : type,
                Font = F(9.5F, FontStyle.Bold),
                Inset = new Padding(13, 6, 13, 6),
                Margin = new Padding(0, 2, 6, 2)
            };
            Color t, i; RecordTone(type, out t, out i);
            p.SetTone(t, i);
            return p;
        }

        public static void SetRecordPill(StatusPill p, string type)
        {
            Color t, i; RecordTone(type, out t, out i);
            p.Text = string.IsNullOrEmpty(type) ? "-" : type;
            p.SetTone(t, i);
        }

        public static Label Cap(string text)
        {
            return new Label
            {
                Text = (text ?? "").ToUpperInvariant(), AutoSize = true, Font = F(8.25F, FontStyle.Bold),
                ForeColor = UiTheme.Muted, BackColor = Color.Transparent, Margin = new Padding(0, 6, 0, 3), UseMnemonic = false
            };
        }

        public static Label Txt(string text, float size = 9F, FontStyle st = FontStyle.Regular, Color? color = null)
        {
            return new Label
            {
                Text = text ?? "", AutoSize = true, Font = F(size, st), ForeColor = color ?? UiTheme.Ink,
                BackColor = Color.Transparent, Margin = new Padding(0, 2, 0, 2), UseMnemonic = false
            };
        }

        public static TextBox Box(bool readOnly = false)
        {
            var t = new TextBox { Dock = DockStyle.Fill, Font = F(9.75F), Margin = new Padding(0, 0, 10, 0), ReadOnly = readOnly };
            if (readOnly) t.BackColor = Color.FromArgb(245, 247, 250);
            return t;
        }

        public static ComboBox Combo(bool editable, IEnumerable<string> items = null)
        {
            var c = new ComboBox
            {
                Dock = DockStyle.Fill, Font = F(9.75F), Margin = new Padding(0, 0, 10, 0),
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList
            };
            if (editable) { c.AutoCompleteMode = AutoCompleteMode.SuggestAppend; c.AutoCompleteSource = AutoCompleteSource.ListItems; }
            if (items != null) foreach (string s in items) c.Items.Add(s);
            return c;
        }

        /// <summary>An optional date: unticked means "the paper states none" and saves NULL, never today.</summary>
        public static DateTimePicker Date(bool optional = true)
        {
            return new DateTimePicker
            {
                Dock = DockStyle.Fill, Font = F(9.75F), Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy",
                ShowCheckBox = optional, Checked = !optional, Margin = new Padding(0, 0, 10, 0)
            };
        }

        public static DateTime? Val(DateTimePicker d) { return d.ShowCheckBox && !d.Checked ? (DateTime?)null : d.Value.Date; }

        public static void Put(DateTimePicker d, DateTime? v)
        {
            if (v.HasValue) { d.Value = v.Value; if (d.ShowCheckBox) d.Checked = true; }
            else if (d.ShowCheckBox) d.Checked = false;
        }

        public enum Kind { Primary, Success, Danger, Secondary, Ghost }

        public static Button Btn(string text, Kind kind, int width = 0)
        {
            var b = new Button
            {
                Text = text, AutoSize = width == 0, Height = 34, FlatStyle = FlatStyle.Flat, UseMnemonic = false,
                Font = F(9F, FontStyle.Bold), Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10, 0, 10, 0),
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            if (width > 0) b.Width = width;
            b.FlatAppearance.BorderSize = 0;
            switch (kind)
            {
                case Kind.Primary: b.BackColor = UiTheme.Accent; b.ForeColor = Color.White; break;
                case Kind.Success: b.BackColor = UiTheme.Success; b.ForeColor = Color.White; break;
                case Kind.Danger: b.BackColor = UiTheme.Danger; b.ForeColor = Color.White; break;
                case Kind.Ghost: b.BackColor = UiTheme.Surface; b.ForeColor = UiTheme.Muted; break;
                default: b.BackColor = UiTheme.Chrome; b.ForeColor = UiTheme.Ink; break;
            }
            return b;
        }

        /// <summary>A caption stacked over an input, sized for a TableLayoutPanel cell.</summary>
        public static Control Field(string caption, Control input, int inputHeight = 28)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.Transparent, Height = inputHeight + 26
            };
            // Without a column style the one column AUTOSIZES to its widest child. A TextBox or
            // ComboBox prefers less than the cell so that never showed; a DateTimePicker prefers
            // 200px, so every date on the marriage windows ran past its cell - clipped (Date of
            // birth lost its dropdown arrow) or into the card border. Measured, 2026-09-13.
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, inputHeight));
            Label cap = Cap(caption); cap.Margin = new Padding(0, 4, 0, 0);
            t.Controls.Add(cap, 0, 0);
            input.Dock = DockStyle.Fill;
            t.Controls.Add(input, 0, 1);
            return t;
        }

        /// <summary>A grid of equal-percentage columns and fixed-height rows.</summary>
        public static TableLayoutPanel Grid(int cols, int rows, int rowHeight)
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = cols, RowCount = rows, Dock = DockStyle.Top, BackColor = Color.Transparent,
                Height = rows * rowHeight, Margin = new Padding(0)
            };
            for (int c = 0; c < cols; c++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            return t;
        }

        public static CardPanel Card(Padding pad)
        {
            return new CardPanel { Dock = DockStyle.Fill, Padding = pad, Margin = new Padding(0, 0, 0, 12) };
        }

        /// <summary>A key/value line on a side rail.</summary>
        public static Control Kv(string key, string value, Color? valueColor = null)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, Height = 26, ColumnCount = 2, BackColor = Color.Transparent, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            var k = Txt(key, 9F, FontStyle.Regular, UiTheme.Muted); k.AutoSize = false; k.Dock = DockStyle.Fill; k.TextAlign = ContentAlignment.MiddleLeft;
            var v = Txt(value, 9F, FontStyle.Bold, valueColor); v.AutoSize = false; v.Dock = DockStyle.Fill;
            v.TextAlign = ContentAlignment.MiddleRight; v.AutoEllipsis = true;
            t.Controls.Add(k, 0, 0); t.Controls.Add(v, 1, 0);
            t.Paint += (s, e) => { using (var p = new Pen(UiTheme.RowLine)) e.Graphics.DrawLine(p, 0, t.Height - 1, t.Width, t.Height - 1); };
            return t;
        }

        /// <summary>
        /// A section title with a WRAPPING explanation under it. A single label clipped the
        /// explanation at the first line on every window - measured in the renders, not guessed.
        /// </summary>
        public static Control SectionHeader(string title, string sub)
        {
            var p = new Panel { BackColor = Color.Transparent, Height = sub == null ? 34 : 64, Margin = new Padding(0) };
            if (sub != null)
                p.Controls.Add(new Label { Text = sub, Dock = DockStyle.Fill, Font = F(9F), ForeColor = UiTheme.Muted, UseMnemonic = false, BackColor = Color.Transparent });
            p.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 26, Font = F(11F, FontStyle.Bold), ForeColor = UiTheme.Ink, UseMnemonic = false, BackColor = Color.Transparent });
            return p;
        }

        public static string D(DateTime? d) { return MarriageRules.D(d); }

        public static string Short(DateTime? d) { return d.HasValue ? d.Value.ToString("dd MMM", CultureInfo.InvariantCulture) : "-"; }

        /// <summary>Confirm an important action in words. Default button is Cancel.</summary>
        public static bool Confirm(IWin32Window owner, string title, string question, params string[] facts)
        {
            using (var f = new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(480, 150 + facts.Length * 26), BackColor = UiTheme.Surface,
                ShowInTaskbar = false
            })
            {
                var q = Txt(question, 11F, FontStyle.Bold); q.Location = new Point(22, 18); q.MaximumSize = new Size(436, 0);
                f.Controls.Add(q);
                int y = 58;
                foreach (string fact in facts)
                {
                    string[] kv = fact.Split(new[] { '|' }, 2);
                    var k = Txt(kv[0], 9F, FontStyle.Regular, UiTheme.Muted); k.Location = new Point(22, y);
                    var v = Txt(kv.Length > 1 ? kv[1] : "", 9.5F, FontStyle.Bold); v.Location = new Point(180, y); v.MaximumSize = new Size(280, 0);
                    f.Controls.Add(k); f.Controls.Add(v);
                    y += 26;
                }
                var ok = Btn(title.ToUpperInvariant(), Kind.Primary); ok.DialogResult = DialogResult.OK;
                var cancel = Btn("Cancel", Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
                ok.Location = new Point(f.ClientSize.Width - 22 - ok.PreferredSize.Width - 10, f.ClientSize.Height - 50);
                cancel.Location = new Point(ok.Left - 100, f.ClientSize.Height - 50);
                f.Controls.Add(ok); f.Controls.Add(cancel);
                f.AcceptButton = cancel; f.CancelButton = cancel;
                UiTheme.Polish(f);
                return f.ShowDialog(owner) == DialogResult.OK;
            }
        }

        /// <summary>Ask for one line of text (a reason, a reference). Null when cancelled or left blank.</summary>
        public static string Ask(IWin32Window owner, string title, string prompt, string initial = "")
        {
            using (var f = new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(460, 150), BackColor = UiTheme.Surface, ShowInTaskbar = false
            })
            {
                var l = Txt(prompt, 9.5F); l.Location = new Point(20, 16); l.MaximumSize = new Size(420, 0);
                var t = new TextBox { Location = new Point(20, 62), Width = 420, Font = F(9.75F), Text = initial };
                var ok = Btn("OK", Kind.Primary, 90); ok.DialogResult = DialogResult.OK; ok.Location = new Point(350, 102);
                var cancel = Btn("Cancel", Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel; cancel.Location = new Point(250, 102);
                f.Controls.AddRange(new Control[] { l, t, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                UiTheme.Polish(f);
                if (f.ShowDialog(owner) != DialogResult.OK || string.IsNullOrWhiteSpace(t.Text)) return null;
                return t.Text.Trim();
            }
        }

        /// <summary>
        /// A warning that lists SPECIFIC outstanding items, then requires a typed reason before
        /// an override can proceed - Proceed/Cancel are pinned at a fixed position regardless of
        /// how many items there are, so a long checklist can never push them off the visible
        /// dialog (MUi.Ask's fixed one-line-prompt height did exactly that once the prompt grew
        /// past a couple of lines). The item list scrolls instead of the dialog growing.
        /// Returns the typed reason, or null if the admin cancelled or left it blank.
        /// </summary>
        public static string AskWithChecklist(IWin32Window owner, string title, string headline,
            IEnumerable<string> items, string note, string proceedLabel)
        {
            using (var f = new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(560, 480), BackColor = UiTheme.Surface, ShowInTaskbar = false
            })
            {
                var head = Txt(headline, 9.5F, FontStyle.Bold);
                head.Location = new Point(20, 16); head.MaximumSize = new Size(520, 0);
                f.Controls.Add(head);

                var list = new TextBox
                {
                    Location = new Point(20, 44), Size = new Size(520, 190), Font = F(9.25F),
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White,
                    Text = string.Join("\r\n", items.Select(i => "• " + i))
                };
                f.Controls.Add(list);

                var noteLbl = Txt(note, 9F, FontStyle.Regular, UiTheme.Muted);
                noteLbl.Location = new Point(20, 242); noteLbl.MaximumSize = new Size(520, 0);
                f.Controls.Add(noteLbl);

                var reasonLbl = Txt("Reason for overriding:", 9.5F, FontStyle.Bold); reasonLbl.Location = new Point(20, 320);
                var reasonBox = new TextBox { Location = new Point(20, 344), Width = 520, Font = F(9.75F) };
                f.Controls.Add(reasonLbl); f.Controls.Add(reasonBox);

                var ok = Btn(proceedLabel, Kind.Danger, 200); ok.DialogResult = DialogResult.OK;
                var cancel = Btn("Cancel", Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
                ok.Location = new Point(f.ClientSize.Width - 20 - ok.Width - 10, f.ClientSize.Height - 50);
                cancel.Location = new Point(ok.Left - 100, f.ClientSize.Height - 50);
                f.Controls.Add(ok); f.Controls.Add(cancel);
                f.AcceptButton = null; f.CancelButton = cancel;
                UiTheme.Polish(f);

                if (f.ShowDialog(owner) != DialogResult.OK || string.IsNullOrWhiteSpace(reasonBox.Text)) return null;
                return reasonBox.Text.Trim();
            }
        }

        public static void Fail(IWin32Window owner, Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "Could not complete", MessageBoxButtons.OK,
                ex is UnauthorizedAccessException ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }

        /// <summary>Open a stored attachment: images in the softcopy viewer, anything else with its program.</summary>
        public static void OpenAttachment(IWin32Window owner, byte[] bytes, string name)
        {
            if (bytes == null) return;
            string ext = Path.GetExtension(name ?? "").ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".tif" || ext == ".tiff" || ext == "")
            {
                try { SoftcopyViewer.Show(bytes, name ?? "Attachment", owner); return; } catch { }
            }
            string tmp = Path.Combine(Path.GetTempPath(), "croms_" + Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileName(name ?? "file.bin"));
            File.WriteAllBytes(tmp, bytes);
            try { System.Diagnostics.Process.Start(tmp); } catch (Exception ex) { Fail(owner, ex); }
        }

        public static void HistoryDialog(IWin32Window owner, string entity, int id, string title)
        {
            using (var f = new Form
            {
                Text = title, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(860, 480),
                BackColor = UiTheme.PageBg, MinimizeBox = false, ShowInTaskbar = false
            })
            {
                var g = new DataGridView
                {
                    Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };
                try { g.DataSource = MarriageService.HistoryOf(entity, id); } catch (Exception ex) { Fail(owner, ex); }
                f.Controls.Add(g);
                f.Padding = new Padding(12);
                UiTheme.Polish(f);
                g.DataBindingComplete += (s, e) =>
                {
                    if (g.Columns.Contains("Details")) g.Columns["Details"].FillWeight = 260;
                    if (g.Columns.Contains("When")) g.Columns["When"].DefaultCellStyle.Format = "dd MMM yyyy HH:mm";
                };
                f.ShowDialog(owner);
            }
        }
    }

    // ---------------------------------------------------------------- banner
    /// <summary>Inline message box that lives next to the thing it is about.</summary>
    internal class Banner : Panel
    {
        private readonly Label _title = new Label(), _body = new Label();
        private RuleSeverity _sev = RuleSeverity.Info;
        private bool _ok;
        // Tracked, never read back from _body.Visible: Visible is EFFECTIVE visibility, false
        // while the step page holding the banner is hidden, which laid the body out on top of
        // the title (seen in the Form 90 posting render).
        private bool _hasBody;

        public Banner()
        {
            Dock = DockStyle.Top; Padding = new Padding(16, 9, 12, 9); Margin = new Padding(0, 0, 0, 10);
            DoubleBuffered = true;
            // Sized by MEASURING the text, not by AutoSize: an AutoSize label reports its new
            // height only after the next layout pass, so the banner was cut off mid-sentence.
            _title.AutoSize = false; _title.Font = MUi.F(9.25F, FontStyle.Bold); _title.UseMnemonic = false; _title.BackColor = Color.Transparent;
            _body.AutoSize = false; _body.Font = MUi.F(9F); _body.UseMnemonic = false; _body.BackColor = Color.Transparent;
            Controls.Add(_body); Controls.Add(_title);
            Resize += (s, e) => Reflow();
        }

        public void Set(RuleSeverity sev, string title, string body, bool success = false)
        {
            _sev = sev; _ok = success;
            _title.Text = title ?? ""; _body.Text = body ?? "";
            _hasBody = !string.IsNullOrEmpty(body);
            _body.Visible = _hasBody;
            Color tint, ink;
            if (success) { tint = UiTheme.SuccessTint; ink = Color.FromArgb(29, 107, 62); }
            else if (sev == RuleSeverity.HardStop || sev == RuleSeverity.Blocking) { tint = UiTheme.DangerTint; ink = Color.FromArgb(142, 32, 41); }
            else if (sev == RuleSeverity.Warning) { tint = UiTheme.WarningTint; ink = Color.FromArgb(122, 59, 6); }
            else { tint = UiTheme.AccentTint; ink = Color.FromArgb(21, 58, 158); }
            BackColor = tint; _title.ForeColor = ink; _body.ForeColor = ink;
            Visible = true;
            Reflow(); Invalidate();
        }

        private void Reflow()
        {
            int w = Math.Max(80, Width - Padding.Horizontal);
            const TextFormatFlags wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
            int th = TextRenderer.MeasureText(_title.Text ?? "", _title.Font, new Size(w, int.MaxValue), wrap).Height;
            _title.Bounds = new Rectangle(Padding.Left, Padding.Top, w, th);
            int bottom = _title.Bottom;
            if (_hasBody)
            {
                int bh = TextRenderer.MeasureText(_body.Text ?? "", _body.Font, new Size(w, int.MaxValue), wrap).Height;
                _body.Bounds = new Rectangle(Padding.Left, _title.Bottom + 2, w, bh);
                bottom = _body.Bottom;
            }
            Height = bottom + Padding.Bottom;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color bar = _ok ? UiTheme.Success : (_sev == RuleSeverity.HardStop || _sev == RuleSeverity.Blocking) ? UiTheme.Danger
                      : _sev == RuleSeverity.Warning ? UiTheme.Warning : UiTheme.Accent;
            using (var b = new SolidBrush(bar)) e.Graphics.FillRectangle(b, 0, 0, 4, Height);
        }
    }

    // ---------------------------------------------------------------- issue list
    /// <summary>
    /// Findings as sentences, each with a link to the place it is fixed. Replaces the
    /// message-box-after-the-click pattern: the reason is on screen before the click.
    /// </summary>
    internal class IssueList : FlowLayoutPanel
    {
        public event Action<string> FixRequested;

        public IssueList()
        {
            FlowDirection = FlowDirection.TopDown; WrapContents = false; AutoScroll = true;
            BackColor = Color.Transparent; Margin = new Padding(0); Padding = new Padding(0);
            Resize += (s, e) => { foreach (Control c in Controls) Fit(c); };
        }

        public void SetIssues(IEnumerable<RuleIssue> issues, string emptyText = null)
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) c.Dispose();
            Controls.Clear();
            var list = issues.OrderBy(i => i.Severity).ToList();
            if (list.Count == 0 && emptyText != null)
            {
                var ok = MUi.Txt("✓  " + emptyText, 9F, FontStyle.Bold, UiTheme.Success);
                Controls.Add(ok); Fit(ok);
            }
            foreach (RuleIssue i in list) Controls.Add(Row(i));
            ResumeLayout();
        }

        private Control Row(RuleIssue i)
        {
            var p = new Panel { BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };
            string tag = i.Severity == RuleSeverity.HardStop ? "STOP" : i.Severity == RuleSeverity.Blocking ? "BLOCKS"
                       : i.Severity == RuleSeverity.Warning ? "CHECK" : "NOTE";
            Color ink = i.Severity == RuleSeverity.Warning ? UiTheme.Warning : i.Severity == RuleSeverity.Info ? UiTheme.Accent : UiTheme.Danger;
            var t = MUi.Txt(tag, 7.5F, FontStyle.Bold, ink); t.Location = new Point(0, 2);
            var m = MUi.Txt(i.Message, 9F); m.Location = new Point(52, 0);
            p.Controls.Add(t); p.Controls.Add(m);
            if (!string.IsNullOrEmpty(i.FixWhere))
            {
                var link = new LinkLabel
                {
                    Text = "Open " + i.FixWhere, AutoSize = true, Font = MUi.F(8.5F, FontStyle.Bold), LinkColor = UiTheme.Accent,
                    ActiveLinkColor = UiTheme.AccentHover, BackColor = Color.Transparent, UseMnemonic = false, Tag = i.FixWhere
                };
                link.LinkClicked += (s, e) => { var h = FixRequested; if (h != null) h((string)link.Tag); };
                p.Controls.Add(link);
            }
            Fit(p);
            return p;
        }

        private void Fit(Control row)
        {
            int w = Math.Max(120, ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            row.Width = w;
            if (row is Label) { ((Label)row).MaximumSize = new Size(w, 0); return; }
            Label msg = row.Controls.OfType<Label>().Skip(1).FirstOrDefault();
            LinkLabel link = row.Controls.OfType<LinkLabel>().FirstOrDefault();
            if (msg == null) return;
            msg.MaximumSize = new Size(Math.Max(60, w - 56), 0);
            int bottom = msg.Bottom;
            if (link != null) { link.Location = new Point(52, msg.Bottom + 1); bottom = link.Bottom; }
            row.Height = bottom + 2;
        }
    }

    // ---------------------------------------------------------------- stepper / tab strip
    /// <summary>
    /// The Form 90 stepper and the Form 97 tab strip. Every step can be revisited - staff use
    /// this forty times a week, it is not a citizen wizard - and a step can carry a count of
    /// open findings so a problem is visible before its tab is opened.
    /// </summary>
    internal class StepStrip : Control
    {
        public enum State { Todo, Current, Done, Locked }
        private readonly List<string> _titles = new List<string>(), _subs = new List<string>();
        private readonly List<State> _states = new List<State>();
        private readonly List<int> _badges = new List<int>();
        private readonly bool _numbered;
        private int _hover = -1;
        public event Action<int> StepClicked;

        public StepStrip(bool numbered)
        {
            _numbered = numbered;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top; Height = 54; Cursor = Cursors.Hand; BackColor = Color.FromArgb(250, 251, 253);
        }

        public void AddStep(string title, string sub) { _titles.Add(title); _subs.Add(sub); _states.Add(State.Todo); _badges.Add(0); Invalidate(); }
        public int Count { get { return _titles.Count; } }
        public void SetState(int i, State s) { _states[i] = s; Invalidate(); }
        public void SetSub(int i, string s) { _subs[i] = s; Invalidate(); }
        public void SetBadge(int i, int n) { _badges[i] = n; Invalidate(); }
        public int IndexOf(string title) { return _titles.FindIndex(t => string.Equals(t, title, StringComparison.OrdinalIgnoreCase)); }

        private Rectangle Cell(int i)
        {
            int w = Width / Math.Max(1, _titles.Count);
            return new Rectangle(i * w, 0, i == _titles.Count - 1 ? Width - i * w : w, Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = Width <= 0 ? -1 : Math.Min(_titles.Count - 1, e.X / Math.Max(1, Width / Math.Max(1, _titles.Count)));
            if (h != _hover) { _hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int i = Math.Min(_titles.Count - 1, e.X / Math.Max(1, Width / Math.Max(1, _titles.Count)));
            if (i >= 0 && StepClicked != null) StepClicked(i);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            using (var line = new Pen(UiTheme.CardLine))
            {
                for (int i = 0; i < _titles.Count; i++)
                {
                    Rectangle r = Cell(i);
                    State st = _states[i];
                    if (st == State.Current) using (var b = new SolidBrush(Color.White)) g.FillRectangle(b, r);
                    else if (i == _hover) using (var b = new SolidBrush(Color.FromArgb(243, 246, 251))) g.FillRectangle(b, r);
                    if (i < _titles.Count - 1) g.DrawLine(line, r.Right - 1, 8, r.Right - 1, r.Bottom - 8);

                    int x = r.X + 14;
                    Color ink = st == State.Locked ? UiTheme.Faint : UiTheme.Ink;
                    if (_numbered)
                    {
                        var dot = new Rectangle(x, 11, 20, 20);
                        Color fill = st == State.Done ? UiTheme.Success : st == State.Current ? UiTheme.Accent : UiTheme.Chrome;
                        using (var b = new SolidBrush(fill)) g.FillEllipse(b, dot);
                        string n = st == State.Done ? "✓" : (i + 1).ToString();
                        TextRenderer.DrawText(g, n, MUiFonts.Bold8, dot, st == State.Done || st == State.Current ? Color.White : UiTheme.Muted,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        x += 27;
                    }
                    Color tcol = st == State.Current ? UiTheme.Accent : ink;
                    TextRenderer.DrawText(g, _titles[i], MUiFonts.Bold9, new Rectangle(x, 9, r.Right - x - 26, 20), tcol,
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, _subs[i] ?? "", MUiFonts.Small, new Rectangle(x, 29, r.Right - x - 8, 18), UiTheme.Faint,
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    if (_badges[i] > 0)
                    {
                        var bd = new Rectangle(r.Right - 30, 11, 20, 18);
                        using (GraphicsPath p = CardPanel.Pill(bd)) using (var b = new SolidBrush(UiTheme.Danger)) g.FillPath(b, p);
                        TextRenderer.DrawText(g, _badges[i].ToString(), MUiFonts.Bold8, bd, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                    if (st == State.Current)
                        using (var b = new SolidBrush(UiTheme.Accent)) g.FillRectangle(b, r.X, r.Bottom - 3, r.Width, 3);
                }
                g.DrawLine(line, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    internal static class MUiFonts
    {
        public static readonly Font Bold8 = new Font("Segoe UI", 8F, FontStyle.Bold);
        public static readonly Font Bold9 = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font Small = new Font("Segoe UI", 8F);
        public static readonly Font Reg9 = new Font("Segoe UI", 9F);
        public static readonly Font Mono9 = new Font("Consolas", 9F);
    }

    // ---------------------------------------------------------------- lifecycle board
    /// <summary>
    /// The Desk's attention list. For a licence it draws BOTH legal clocks on one scale - the
    /// 10-day posting as the first part of the track, the 120-day validity as the rest - with
    /// today's marker at its true position, so "six days left" and "ninety-seven days left" are
    /// visibly different lengths, which a grid column of numbers cannot convey.
    /// </summary>
    internal class LifecycleBoard : Control
    {
        public sealed class Item
        {
            public string Who, Number, Pill, PillTone, Next;
            public DateTime? PostingStart, EarliestIssue, IssueDate, Expiry;
            public bool TrackLess;
            public object Tag;
        }

        private readonly List<Item> _items = new List<Item>();
        private int _hover = -1;
        private const int RowH = 50, HeadH = 26;
        public event Action<object> ItemClicked;
        public MarriageSettings Settings = MarriageSettings.Defaults();
        public string EmptyText = "Nothing needs attention.";

        public LifecycleBoard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.Surface;
        }

        public void SetItems(IEnumerable<Item> items)
        {
            _items.Clear(); _items.AddRange(items);
            Height = HeadH + Math.Max(1, _items.Count) * RowH + 4;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int i = e.Y < HeadH ? -1 : (e.Y - HeadH) / RowH;
            if (i >= _items.Count) i = -1;
            if (i != _hover) { _hover = i; Cursor = i >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_hover >= 0 && ItemClicked != null) ItemClicked(_items[_hover].Tag);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            int whoW = 220, nextW = 250, pillW = 150;
            int trackX = whoW + 10, trackW = Math.Max(80, Width - whoW - nextW - pillW - 30);
            float postFrac = 0.18f;

            TextRenderer.DrawText(g, "COUPLE / NUMBER", MUiFonts.Bold8, new Point(0, 6), UiTheme.Muted);
            TextRenderer.DrawText(g, "POSTING " + Settings.PostingDays + "d", MUiFonts.Bold8, new Point(trackX, 6), UiTheme.Faint);
            TextRenderer.DrawText(g, "VALIDITY " + Settings.ValidityDays + " DAYS FROM ISSUE", MUiFonts.Bold8,
                new Point(trackX + (int)(trackW * postFrac) + 6, 6), UiTheme.Faint);
            TextRenderer.DrawText(g, "STATUS", MUiFonts.Bold8, new Point(trackX + trackW + 12, 6), UiTheme.Muted);
            TextRenderer.DrawText(g, "NEXT", MUiFonts.Bold8, new Point(Width - nextW, 6), UiTheme.Muted);
            using (var pen = new Pen(UiTheme.CardLine)) g.DrawLine(pen, 0, HeadH - 2, Width, HeadH - 2);

            if (_items.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, MUiFonts.Reg9, new Rectangle(0, HeadH, Width, RowH), UiTheme.Faint,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }

            DateTime today = DateTime.Today;
            for (int i = 0; i < _items.Count; i++)
            {
                Item it = _items[i];
                int y = HeadH + i * RowH;
                if (i == _hover) using (var b = new SolidBrush(Color.FromArgb(246, 248, 252))) g.FillRectangle(b, 0, y, Width, RowH);
                TextRenderer.DrawText(g, it.Who ?? "", MUiFonts.Bold9, new Rectangle(0, y + 7, whoW, 20), UiTheme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, it.Number ?? "", MUiFonts.Mono9, new Rectangle(0, y + 26, whoW, 18), UiTheme.Faint, TextFormatFlags.EndEllipsis);

                var lane = new Rectangle(trackX, y + 20, trackW, 8);
                if (it.TrackLess)
                {
                    TextRenderer.DrawText(g, "Form 97 record - no licence clock", MUiFonts.Small, new Rectangle(trackX, y + 16, trackW, 18), UiTheme.Faint);
                }
                else
                {
                    using (GraphicsPath p = CardPanel.RoundedRect(lane, 4)) using (var b = new SolidBrush(UiTheme.RowLine)) g.FillPath(b, p);
                    int postEndX = lane.X + (int)(lane.Width * postFrac);
                    float perPost = (lane.Width * postFrac) / Math.Max(1, Settings.PostingDays);
                    float perValid = (lane.Width * (1 - postFrac)) / Math.Max(1, Settings.ValidityDays);
                    int? nowX = null;
                    if (it.PostingStart.HasValue)
                    {
                        int postedDays = Math.Max(0, Math.Min(Settings.PostingDays, (today - it.PostingStart.Value.Date).Days + 1));
                        bool done = it.IssueDate.HasValue || (it.EarliestIssue.HasValue && today >= it.EarliestIssue.Value.Date);
                        int w = done ? postEndX - lane.X : (int)(postedDays * perPost);
                        Fill(g, new Rectangle(lane.X, lane.Y, Math.Max(2, w), lane.Height), done ? UiTheme.Success : UiTheme.Warning);
                        if (!it.IssueDate.HasValue) nowX = lane.X + (done ? postEndX - lane.X : w);
                    }
                    if (it.IssueDate.HasValue && it.Expiry.HasValue)
                    {
                        int used = Math.Max(0, Math.Min(Settings.ValidityDays, (today - it.IssueDate.Value.Date).Days));
                        int left = (it.Expiry.Value.Date - today).Days;
                        Color c = left < 0 ? UiTheme.Faint : left <= Settings.ExpiringSoonDays ? UiTheme.Danger : UiTheme.Accent;
                        Fill(g, new Rectangle(postEndX, lane.Y, Math.Max(2, (int)(used * perValid)), lane.Height), c);
                        nowX = postEndX + (int)(used * perValid);
                    }
                    using (var pen = new Pen(UiTheme.CardLine, 2)) g.DrawLine(pen, postEndX, lane.Y - 6, postEndX, lane.Bottom + 6);
                    if (nowX.HasValue)
                        using (var pen = new Pen(UiTheme.Ink, 2)) { g.DrawLine(pen, nowX.Value, lane.Y - 8, nowX.Value, lane.Bottom + 8); }
                }

                // status pill (word + tone)
                Color tint, ink; MUi.ToneOf(it.PillTone ?? it.Pill, out tint, out ink);
                Size ts = TextRenderer.MeasureText(it.Pill ?? "", MUiFonts.Bold8);
                var pill = new Rectangle(trackX + trackW + 12, y + 14, Math.Min(pillW - 8, ts.Width + 14), 22);
                using (GraphicsPath p = CardPanel.Pill(pill)) using (var b = new SolidBrush(tint)) g.FillPath(b, p);
                TextRenderer.DrawText(g, it.Pill ?? "", MUiFonts.Bold8, pill, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                TextRenderer.DrawText(g, it.Next ?? "", MUiFonts.Reg9, new Rectangle(Width - nextW, y + 6, nextW, RowH - 10), UiTheme.Muted,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                using (var pen = new Pen(UiTheme.RowLine)) g.DrawLine(pen, 0, y + RowH - 1, Width, y + RowH - 1);
            }
        }

        private static void Fill(Graphics g, Rectangle r, Color c)
        {
            using (GraphicsPath p = CardPanel.RoundedRect(r, 4)) using (var b = new SolidBrush(c)) g.FillPath(b, p);
        }
    }

    // ---------------------------------------------------------------- requirements grid
    /// <summary>
    /// Supporting documents for a licence or a marriage record: what applies, WHY it applies,
    /// and its status. Edits save immediately through MarriageService (drafts are never lost),
    /// and marking Verified stamps who checked it. CROMS records that a CENOMAR etc. was
    /// presented; it never issues one.
    /// </summary>
    internal class RequirementsGrid : UserControl
    {
        private readonly DataGridView _g = new DataGridView();
        private string _owner;
        private int _ownerId;
        private List<ReqRow> _rows = new List<ReqRow>();
        private Dictionary<string, Need> _needs = new Dictionary<string, Need>();
        private Func<ReqRow, bool> _filter = r => true;
        private bool _loading;
        public event Action Changed;
        public bool ReadOnlyGrid { get; set; }

        private readonly Panel _addBar = new Panel { Dock = DockStyle.Top, Height = 0, Visible = false };
        private readonly Button _btnAdd = new Button { Text = "+ Add Requirement", Width = 160, Height = 28, FlatStyle = FlatStyle.Flat };
        private bool _allowAddCustom;
        /// <summary>
        /// Shows a "+ Add Requirement" bar above the grid so staff/admin can attach an ad hoc
        /// document the office's catalogue doesn't list (this case turned out to need one) -
        /// e.g. an extra piece of evidence on a delayed birth, or an extra document a marriage
        /// application needs beyond the standard set. Off by default; a screen opts in.
        /// </summary>
        public bool AllowAddCustom
        {
            get { return _allowAddCustom; }
            set { _allowAddCustom = value; _addBar.Visible = value; _addBar.Height = value ? 40 : 0; }
        }
        /// <summary>Party choices offered when adding a custom requirement. Default is the single
        /// "Both" a one-party owner (a delayed birth case) has; a two-party owner (a marriage
        /// licence/registration) sets {"Both","Husband","Wife"}.</summary>
        public string[] PartyOptions { get; set; } = { "Both" };

        private bool _allowBypass;
        /// <summary>
        /// Shows a per-row "Bypass" action so an Admin can let ONE requirement count as satisfied
        /// without its paperwork being checked - the office asked for this per document, not the
        /// old whole-checklist "Admin Override" button. Off by default; a screen opts in AND the
        /// signed-in user must be Admin (checked again server-side by MarriageService.
        /// BypassRequirement/ClearBypass, so hiding this column is a convenience, not the gate).
        /// </summary>
        public bool AllowBypass
        {
            get { return _allowBypass; }
            set { _allowBypass = value; _g.Columns["Bypass"].Visible = value; }
        }

        public RequirementsGrid()
        {
            _btnAdd.Margin = new Padding(6);
            _btnAdd.Location = new Point(6, 5);
            _btnAdd.Click += (s, e) => AddCustom();
            _addBar.Controls.Add(_btnAdd);
            Controls.Add(_addBar);
            _g.Dock = DockStyle.Fill; _g.AllowUserToAddRows = false; _g.AllowUserToDeleteRows = false;
            _g.RowHeadersVisible = false; _g.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _g.EditMode = DataGridViewEditMode.EditOnEnter;
            _g.BackgroundColor = UiTheme.Surface; _g.BorderStyle = BorderStyle.None;
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Party", HeaderText = "For", ReadOnly = true, FillWeight = 55 });
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Req", HeaderText = "Requirement", ReadOnly = true, FillWeight = 240 });
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Why", HeaderText = "Why it applies", ReadOnly = true, FillWeight = 170 });
            var st = new DataGridViewComboBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 78, FlatStyle = FlatStyle.Flat };
            st.Items.AddRange(MarriageService.RequirementStatuses);
            _g.Columns.Add(st);
            var oc = new DataGridViewComboBoxColumn { Name = "Outcome", HeaderText = "Advice", FillWeight = 72, FlatStyle = FlatStyle.Flat };
            oc.Items.AddRange(MarriageService.AdviceOutcomes);
            _g.Columns.Add(oc);
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Given", HeaderText = "Given by", FillWeight = 80 });
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ref", HeaderText = "Reference / notes", FillWeight = 100 });
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "DocDate", HeaderText = "Doc. date", FillWeight = 62 });
            _g.Columns.Add(new DataGridViewButtonColumn { Name = "File", HeaderText = "File", FillWeight = 52, FlatStyle = FlatStyle.Flat });
            _g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Checked", HeaderText = "Verified", ReadOnly = true, FillWeight = 70 });
            _g.Columns.Add(new DataGridViewButtonColumn { Name = "Bypass", HeaderText = "Admin", FillWeight = 62, FlatStyle = FlatStyle.Flat, Visible = false });
            // Measured in the renders: at FillWeight alone the status combo showed "Veri..." and
            // the file button "ttach". Combos draw as plain text until clicked, narrow columns
            // get a floor, and the two long text columns wrap instead of ellipsizing.
            st.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            oc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            _g.Columns["Party"].MinimumWidth = 58; _g.Columns["Status"].MinimumWidth = 88; _g.Columns["Outcome"].MinimumWidth = 86;
            _g.Columns["DocDate"].MinimumWidth = 84; _g.Columns["File"].MinimumWidth = 66; _g.Columns["Checked"].MinimumWidth = 86;
            _g.Columns["Bypass"].MinimumWidth = 74;
            _g.Columns["Req"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _g.Columns["Why"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (DataGridViewColumn c in _g.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;
            _g.DataError += (s, e) => { e.ThrowException = false; };
            _g.CellEndEdit += (s, e) => SaveRow(e.RowIndex);
            _g.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_g.IsCurrentCellDirty && _g.CurrentCell is DataGridViewComboBoxCell) _g.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _g.CellValueChanged += (s, e) =>
            {
                if (!_loading && e.RowIndex >= 0 && _g.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn) SaveRow(e.RowIndex);
            };
            _g.CellContentClick += Grid_CellContentClick;
            _g.CellFormatting += Grid_CellFormatting;
            Controls.Add(_g);
        }

        public DataGridView Inner { get { return _g; } }
        public int PreferredHeight { get; private set; }

        public void Bind(string ownerType, int ownerId, IEnumerable<Need> needs, Func<ReqRow, bool> filter = null)
        {
            _owner = ownerType; _ownerId = ownerId; _filter = filter ?? (r => true);
            _needs = new Dictionary<string, Need>();
            foreach (Need n in needs) _needs[n.Code + "|" + n.Party] = n;
            _rows = ownerId > 0 ? MarriageService.Requirements(ownerType, ownerId) : new List<ReqRow>();
            _loading = true;
            _g.Rows.Clear();
            foreach (ReqRow r in _rows.Where(_filter).OrderBy(r => _needs.ContainsKey(r.Code + "|" + r.Party) ? _needs[r.Code + "|" + r.Party].Sort : 999).ThenBy(r => r.Party))
            {
                Need n;
                _needs.TryGetValue(r.Code + "|" + r.Party, out n);
                string why = n != null ? n.Reason
                           : MarriageService.IsCustomCode(r.Code) ? "Added manually - not part of the standard checklist"
                           : "No longer required - kept because it holds a record";
                int i = _g.Rows.Add(r.Party == "Both" ? "Both" : r.Party, r.Label ?? r.Code, why,
                    r.Status, r.Outcome ?? "", r.GivenBy, r.ReferenceNo, r.DocDate.HasValue ? MUi.D(r.DocDate) : "",
                    r.HasAttachment ? "View" : "Attach", r.VerifiedAt.HasValue ? MUi.D(r.VerifiedAt) : "",
                    r.IsBypassed ? "Un-bypass" : "Bypass");
                DataGridViewRow row = _g.Rows[i];
                row.Tag = r;
                if (n != null && !string.IsNullOrEmpty(n.Basis)) row.Cells["Req"].ToolTipText = n.Basis;
                bool isAdvice = r.Code == "PARENTAL_ADVICE", isConsent = r.Code == "PARENTAL_CONSENT" || isAdvice;
                row.Cells["Outcome"].ReadOnly = !isAdvice || ReadOnlyGrid;
                row.Cells["Given"].ReadOnly = !isConsent || ReadOnlyGrid;
                if (ReadOnlyGrid) foreach (DataGridViewCell c in row.Cells) if (!(c is DataGridViewButtonCell)) c.ReadOnly = true;
                if (n == null && !MarriageService.IsCustomCode(r.Code)) row.DefaultCellStyle.ForeColor = UiTheme.Faint;
                if (r.IsBypassed)
                {
                    row.DefaultCellStyle.BackColor = UiTheme.WarningTint;
                    string who = string.IsNullOrEmpty(r.BypassedByName) ? "an Admin" : r.BypassedByName;
                    string when = r.BypassedAt.HasValue ? MUi.D(r.BypassedAt) : "";
                    row.Cells["Bypass"].ToolTipText = "Bypassed by " + who + (when == "" ? "" : " on " + when) +
                        (string.IsNullOrEmpty(r.BypassReason) ? "" : ": " + r.BypassReason);
                    row.Cells["Status"].ToolTipText = "This requirement is bypassed - it counts as satisfied even though " +
                        "its status still says \"" + r.Status + "\".";
                }
            }
            _loading = false;
            // Advice / Given-by only mean something on consent rows; hidden elsewhere they
            // give their width to the requirement text instead of wrapping it three lines deep.
            bool consent = _rows.Where(_filter).Any(r => r.Code == "PARENTAL_CONSENT" || r.Code == "PARENTAL_ADVICE");
            _g.Columns["Outcome"].Visible = _rows.Where(_filter).Any(r => r.Code == "PARENTAL_ADVICE");
            _g.Columns["Given"].Visible = consent;
            // Height is sized on the CONTROL from a row estimate. Measuring rows here is wrong
            // twice over: the grid is Dock=Fill (its own Height is overwritten by layout) and
            // the page is often hidden, so rows are measured at a stale width.
            PreferredHeight = (AllowAddCustom ? _addBar.Height : 0) + Math.Min(460, 44 + Math.Max(1, _g.Rows.Count) * 46);
            Height = PreferredHeight;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _g.Columns[e.ColumnIndex].Name != "Status") return;
            string v = Convert.ToString(e.Value);
            e.CellStyle.ForeColor = MUi.InkOf(v);
            e.CellStyle.Font = MUiFonts.Bold9;
        }

        private void SaveRow(int index)
        {
            if (_loading || index < 0 || ReadOnlyGrid) return;
            DataGridViewRow row = _g.Rows[index];
            var r = row.Tag as ReqRow;
            if (r == null) return;
            string status = Convert.ToString(row.Cells["Status"].Value);
            if (status == "Waived" && r.Code != "COUNSELING")
            {
                MessageBox.Show(this, "Only the counselling certificate can be waived - the law then defers the licence " +
                                "three months (Family Code Art. 16). Other documents must be presented.", "Not allowed",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                _loading = true; row.Cells["Status"].Value = r.Status; _loading = false;
                return;
            }
            string dd = Convert.ToString(row.Cells["DocDate"].Value);
            DateTime parsed;
            DateTime? docDate = null;
            if (!string.IsNullOrWhiteSpace(dd))
            {
                if (DateTime.TryParseExact(dd.Trim(), new[] { "dd MMM yyyy", "d MMM yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) docDate = parsed;
                else
                {
                    _loading = true; row.Cells["DocDate"].Value = r.DocDate.HasValue ? MUi.D(r.DocDate) : ""; _loading = false;
                    MessageBox.Show(this, "Type the document date as e.g. 05 Sep 2026.", "Date not understood", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            r.Status = status;
            r.Outcome = Convert.ToString(row.Cells["Outcome"].Value);
            r.GivenBy = Convert.ToString(row.Cells["Given"].Value);
            r.ReferenceNo = Convert.ToString(row.Cells["Ref"].Value);
            r.DocDate = docDate;
            try
            {
                MarriageService.SaveRequirement(r);
                ReqRow fresh = MarriageService.Requirements(_owner, _ownerId).FirstOrDefault(x => x.Id == r.Id);
                if (fresh != null)
                {
                    row.Tag = fresh;
                    _loading = true;
                    row.Cells["Checked"].Value = fresh.VerifiedAt.HasValue ? MUi.D(fresh.VerifiedAt) : "";
                    row.Cells["DocDate"].Value = fresh.DocDate.HasValue ? MUi.D(fresh.DocDate) : "";
                    _loading = false;
                }
                var h = Changed; if (h != null) h();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_g.Columns[e.ColumnIndex].Name == "Bypass") { ToggleBypass(e.RowIndex); return; }
            if (_g.Columns[e.ColumnIndex].Name != "File") return;
            var r = _g.Rows[e.RowIndex].Tag as ReqRow;
            if (r == null) return;
            if (r.HasAttachment)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("View", null, (s, a) => { string n; MUi.OpenAttachment(this, MarriageService.RequirementAttachment(r.Id, out n), n); });
                if (!ReadOnlyGrid) menu.Items.Add("Replace...", null, (s, a) => Upload(r, e.RowIndex));
                Rectangle cell = _g.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                menu.Show(_g, new Point(cell.Left, cell.Bottom));
            }
            else if (!ReadOnlyGrid) Upload(r, e.RowIndex);
        }

        private void Upload(ReqRow r, int rowIndex)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Attach " + (r.Label ?? r.Code),
                Filter = "Scans and documents|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.pdf|All files|*.*"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    MarriageService.AttachRequirement(r.Id, File.ReadAllBytes(dlg.FileName), Path.GetFileName(dlg.FileName));
                    Bind(_owner, _ownerId, _needs.Values, _filter);
                    var h = Changed; if (h != null) h();
                }
                catch (Exception ex) { MUi.Fail(this, ex); }
            }
        }

        /// <summary>
        /// Per-row Admin bypass, replacing the old whole-checklist "Admin Override" button.
        /// Not gated only on ReadOnlyGrid/AllowBypass here - MarriageService.BypassRequirement/
        /// ClearBypass re-check the Admin role server-side, so a stale or tampered client can't
        /// bypass a requirement it merely still shows the button for.
        /// </summary>
        private void ToggleBypass(int rowIndex)
        {
            if (ReadOnlyGrid) return;
            var r = _g.Rows[rowIndex].Tag as ReqRow;
            if (r == null) return;
            try
            {
                if (r.IsBypassed)
                {
                    if (!MUi.Confirm(this, "Withdraw bypass", "Withdraw the bypass on \"" + (r.Label ?? r.Code) + "\"?",
                            "Reason on file|" + r.BypassReason)) return;
                    MarriageService.ClearBypass(r.Id);
                }
                else
                {
                    string reason = MUi.Ask(this, "Bypass requirement",
                        "\"" + (r.Label ?? r.Code) + "\" will count as satisfied even though it has not been checked. " +
                        "This is an Admin decision and is permanently recorded in the audit trail.\n\nReason for bypassing:", "");
                    if (reason == null) return;
                    MarriageService.BypassRequirement(r.Id, reason);
                }
                Bind(_owner, _ownerId, _needs.Values, _filter);
                var h = Changed; if (h != null) h();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(this, ex.Message, "Not allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void AddCustom()
        {
            if (ReadOnlyGrid) return;
            if (_ownerId <= 0) { MessageBox.Show(this, "Save the record first, then add an extra requirement.", "Not saved yet", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dlg = new AddRequirementDialog(PartyOptions))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    MarriageService.AddCustomRequirement(_owner, _ownerId, dlg.Party, dlg.Label);
                    Bind(_owner, _ownerId, _needs.Values, _filter);
                    var h = Changed; if (h != null) h();
                }
                catch (Exception ex) { MUi.Fail(this, ex); }
            }
        }
    }

    /// <summary>Small modal for "+ Add Requirement": a label the office needs for this case that
    /// isn't in the catalogue, plus which party it belongs to (hidden entirely when the owner
    /// only ever has one, e.g. a delayed birth case).</summary>
    internal sealed class AddRequirementDialog : Form
    {
        private readonly TextBox _txtLabel = new TextBox { Left = 16, Top = 40, Width = 340 };
        private readonly ComboBox _cboParty = new ComboBox { Left = 16, Top = 96, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        public string Label { get { return _txtLabel.Text.Trim(); } }
        public string Party { get { return _cboParty.Visible ? Convert.ToString(_cboParty.SelectedItem) : "Both"; } }

        public AddRequirementDialog(string[] partyOptions)
        {
            Text = "Add Requirement"; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(372, partyOptions != null && partyOptions.Length > 1 ? 190 : 130);
            BackColor = UiTheme.Surface;

            var lbl1 = new Label { Text = "What document or requirement?", Left = 16, Top = 16, Width = 340, ForeColor = UiTheme.Ink };
            Controls.Add(lbl1); Controls.Add(_txtLabel);

            bool showParty = partyOptions != null && partyOptions.Length > 1;
            if (showParty)
            {
                var lbl2 = new Label { Text = "For", Left = 16, Top = 76, Width = 100, ForeColor = UiTheme.Ink };
                Controls.Add(lbl2);
                _cboParty.Items.AddRange(partyOptions);
                _cboParty.SelectedIndex = 0;
                Controls.Add(_cboParty);
            }
            else _cboParty.Visible = false;

            int btnTop = showParty ? 136 : 76;
            var ok = new Button { Text = "Add", Left = 196, Top = btnTop, Width = 80, Height = 30, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", Left = 284, Top = btnTop, Width = 80, Height = 30, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtLabel.Text))
                {
                    MessageBox.Show(this, "Type what the requirement is.", "Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                }
            };
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;
            _txtLabel.Focus();
        }
    }
}
