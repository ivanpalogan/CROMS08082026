using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// WINDOW 3 - the focused Issue License window. Issuing is a deliberate act with its own
    /// screen, not a click in a table: it restates the checklist, shows the number the licence
    /// will take and computes the validity from the ACTUAL date of issue.
    /// </summary>
    internal sealed partial class IssueLicenseForm : Form
    {
        private readonly LicenseFacts _l;
        private readonly MarriageSettings _s = MarriageService.Settings;

        public IssueLicenseForm(LicenseFacts l)
        {
            _l = l;
            InitializeComponent();

            var head = new Label { Text = "  ISSUE MARRIAGE LICENSE", Dock = DockStyle.Top, Height = 46, BackColor = UiTheme.Navy, ForeColor = Color.White,
                                   Font = MUi.F(12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 14, 22, 8), AutoScroll = true, BackColor = UiTheme.Surface };
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 10),
                                             BackColor = Color.FromArgb(250, 251, 253) };
            var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90); cancel.DialogResult = DialogResult.Cancel;
            var preview = MUi.Btn("Preview license", MUi.Kind.Secondary, 140);
            foot.Controls.Add(_go); foot.Controls.Add(preview); foot.Controls.Add(cancel);
            CancelButton = cancel;

            List<RuleIssue> all = MarriageRules.ValidateForIssue(l, MarriageService.Catalog(), DateTime.Today, _s).Where(i => i.Blocks).ToList();
            var items = new List<Control>
            {
                MUi.Cap("Application"),
                MUi.Kv("Application No.", l.ApplicationNo),
                MUi.Kv("Husband / Party 1", l.Husband.FullName),
                MUi.Kv("Wife / Party 2", l.Wife.FullName),
                MUi.Cap("Checklist"),
                Check("Application complete", !all.Any(i => new[] { "NAME", "CITIZENSHIP", "CIVIL", "DOB_MISSING", "UNDER_18", "IMPEDIMENT" }.Contains(i.Code))),
                Check("Posting complete (" + MUi.Short(l.PostingStart) + " - " + MUi.D(l.PostingEnd) + ")", !all.Any(i => i.Code == "POSTING" || i.Code == "NOT_POSTED")),
                Check("Requirements complete", !all.Any(i => i.Code.StartsWith("REQ_") && !i.Code.Contains("PARENTAL") && !i.Code.Contains("COUNSELING"))),
                Check("Consent / advice complete", !all.Any(i => i.Code.Contains("PARENTAL") || i.Code.Contains("COUNSELING"))),
                OverrideRow(),
                Check("Payment recorded" + (string.IsNullOrEmpty(l.PaymentOr) ? "" : " - O.R. " + l.PaymentOr), !all.Any(i => i.Code == "PAYMENT")),
                MUi.Cap("License"),
            };
            var no = new TableLayoutPanel { Height = 30, ColumnCount = 2, BackColor = Color.Transparent };
            no.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); no.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            no.Controls.Add(MUi.Txt("License No.", 9F, FontStyle.Regular, UiTheme.Muted), 0, 0); no.Controls.Add(_no, 1, 0);
            TableLayoutPanel dt = MUi.Grid(2, 1, 58);
            dt.Controls.Add(MUi.Field("Issue date (actual)", _date), 0, 0);
            var vp = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 26, 0, 0), BackColor = Color.Transparent };
            vp.Controls.Add(_valid); _valid.Dock = DockStyle.Fill;
            dt.Controls.Add(vp, 1, 0);
            items.Add(no); items.Add(dt);
            items.Add(MUi.Kv("Validity", _s.ValidityDays + " days from the date of issue, anywhere in the Philippines (Family Code Art. 20)"));
            items.Add(new Panel { Height = 10, BackColor = Color.Transparent });
            _issues.Height = 110;
            items.Add(_issues);
            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; body.Controls.Add(items[i]); }

            Controls.Add(body); Controls.Add(foot); Controls.Add(head);

            _date.MaxDate = DateTime.Today;
            if (l.EarliestIssue.HasValue && l.EarliestIssue.Value <= DateTime.Today) _date.MinDate = l.EarliestIssue.Value;
            _date.Value = DateTime.Today;
            _date.ValueChanged += (s, e) => Recompute();
            preview.Click += (s, e) => LicensePrinter.Show(this, Hypothetical(), true);
            _go.Click += (s, e) => DoIssue();
            _override.Click += (s, e) => DoOverride();
            UiTheme.Polish(this);
            Recompute();
        }

        /// <summary>
        /// Admin-only row: lets an Admin record (or withdraw) a requirements override on this
        /// application. Hidden for a Registrar - the power is intentionally narrower than the
        /// usual Registrar-or-Admin gate elsewhere on this table (see MarriageService.OverrideRequirements).
        /// </summary>
        private Control OverrideRow()
        {
            var p = new Panel { Height = 66, BackColor = Color.Transparent, Visible = MarriageService.IsAdmin };
            _override.Location = new Point(0, 0);
            _overrideStatus.AutoSize = false; _overrideStatus.Location = new Point(0, 38); _overrideStatus.Size = new Size(560, 26);
            p.Controls.Add(_override); p.Controls.Add(_overrideStatus);
            return p;
        }

        private void DoOverride()
        {
            if (_l.RequirementsOverrideBy.HasValue)
            {
                if (!MUi.Confirm(this, "Withdraw override", "Withdraw the requirements override on this application?",
                        "Reason on file|" + _l.RequirementsOverrideReason)) return;
                try { MarriageService.ClearRequirementsOverride(_l.Id); _l.RequirementsOverrideBy = null; _l.RequirementsOverrideReason = null; Recompute(); }
                catch (Exception ex) { MUi.Fail(this, ex); }
                return;
            }
            string reason = MUi.Ask(this, "Admin Override",
                "This application is missing or has unverified requirement attachments. Issuing it anyway is an Admin decision and will be permanently recorded on the application and in the audit trail.\n\nReason for overriding:", "");
            if (reason == null) return;
            try
            {
                MarriageService.OverrideRequirements(_l.Id, reason);
                LicenseFacts fresh = MarriageService.LoadLicense(_l.Id);
                _l.RequirementsOverrideBy = fresh.RequirementsOverrideBy;
                _l.RequirementsOverrideAt = fresh.RequirementsOverrideAt;
                _l.RequirementsOverrideReason = fresh.RequirementsOverrideReason;
                Recompute();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private static Control Check(string text, bool ok)
        {
            var l = MUi.Txt((ok ? "✓  " : "✕  ") + text, 9.5F, ok ? FontStyle.Bold : FontStyle.Regular, ok ? UiTheme.Success : UiTheme.Danger);
            l.AutoSize = false; l.Height = 24;
            return l;
        }

        private void Recompute()
        {
            DateTime d = _date.Value.Date;
            _no.Text = MarriageService.PeekNextLicenseNo(d) + "   (assigned on issue)";
            _valid.Text = "Valid until  " + MUi.D(MarriageRules.Expiry(d, _s));
            List<RuleIssue> rawIssues = MarriageRules.ValidateForIssue(_l, MarriageService.Catalog(), d, _s).Where(i => i.Blocks).ToList();
            List<RuleIssue> issues = MarriageRules.ApplyOverride(rawIssues, _l);
            _issues.SetIssues(issues, "Every check passes for this issue date.");
            _go.Enabled = issues.Count == 0 && MarriageService.IsRegistrar;
            if (!MarriageService.IsRegistrar) _issues.SetIssues(new[] { new RuleIssue(RuleSeverity.Blocking, "ROLE", "Only a Registrar or Admin can issue a licence.", null) });

            bool hasReqIssues = rawIssues.Any(i => i.Code.StartsWith("REQ_"));
            bool active = _l.RequirementsOverrideBy.HasValue;
            _override.Text = active ? "Withdraw Requirements Override" : "Admin Override - Missing Requirements";
            _override.Visible = MarriageService.IsAdmin && (hasReqIssues || active);
            _overrideStatus.Text = active
                ? "OVERRIDDEN: " + _l.RequirementsOverrideReason
                : (hasReqIssues ? "Missing/unverified requirement attachments are blocking this licence." : "");
        }

        private LicenseFacts Hypothetical()
        {
            return new LicenseFacts
            {
                Id = _l.Id, ApplicationNo = _l.ApplicationNo, LicenseNo = MarriageService.PeekNextLicenseNo(_date.Value), Husband = _l.Husband, Wife = _l.Wife,
                IssueDate = _date.Value.Date, ExpiryDate = MarriageRules.Expiry(_date.Value.Date, _s), StoredStatus = "Preview"
            };
        }

        private void DoIssue()
        {
            DateTime d = _date.Value.Date;
            if (!MUi.Confirm(this, "Issue license", "Issue this marriage license?",
                    "Applicants|" + _l.Husband.FullName + " & " + _l.Wife.FullName, "Issue date|" + MUi.D(d),
                    "Valid until|" + MUi.D(MarriageRules.Expiry(d, _s))))
                return;
            try
            {
                string no;
                List<RuleIssue> issues = MarriageService.IssueLicense(_l.Id, d, out no);
                if (issues.Count > 0) { _issues.SetIssues(issues); return; }
                LicensePrinter.Show(this, MarriageService.LoadLicense(_l.Id), false);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }
    }

    /// <summary>
    /// Prints the licence CROMS issued. A PREVIEW carries a banner and a diagonal wash on the
    /// page so it can never pass as an issued licence - the same rule the OCR form preview holds.
    /// </summary>
    internal static class LicensePrinter
    {
        public static void Show(IWin32Window owner, LicenseFacts l, bool preview)
        {
            string office = "Office of the Local Civil Registrar", muni = "", prov = "", registrar = "", title = "Municipal Civil Registrar";
            try
            {
                DataTable o = Db.Pull("SELECT * FROM office_profile LIMIT 1");
                if (o.Rows.Count > 0)
                {
                    DataRow r = o.Rows[0];
                    office = r["office_name"] as string ?? office; muni = r["municipality"] as string ?? ""; prov = r["province"] as string ?? "";
                    registrar = r["registrar_name"] as string ?? ""; title = r["registrar_title"] as string ?? title;
                }
            }
            catch { }

            var doc = new PrintDocument { DocumentName = (preview ? "PREVIEW " : "") + "Marriage License " + l.LicenseNo };
            doc.PrintPage += (s, e) => Draw(e, l, preview, office, muni, prov, registrar, title);
            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 1000, Text = preview ? "LICENSE PREVIEW (not a license)" : "Marriage License " + l.LicenseNo })
                dlg.ShowDialog(owner);
        }

        private static void Draw(PrintPageEventArgs e, LicenseFacts l, bool preview, string office, string muni, string prov, string registrar, string rtitle)
        {
            Graphics g = e.Graphics;
            Rectangle m = e.MarginBounds;
            float y = m.Top;
            using (var fS = new Font("Times New Roman", 10F))
            using (var fB = new Font("Times New Roman", 10.5F, FontStyle.Bold))
            using (var fT = new Font("Times New Roman", 18F, FontStyle.Bold))
            using (var fH = new Font("Times New Roman", 11F, FontStyle.Bold))
            {
                var center = new StringFormat { Alignment = StringAlignment.Center };
                Action<string, Font> C = (t, f) => { g.DrawString(t, f, Brushes.Black, new RectangleF(m.Left, y, m.Width, 40), center); y += f.GetHeight(g) + 2; };
                C("Republic of the Philippines", fS);
                if (!string.IsNullOrEmpty(prov)) C("Province of " + prov, fS);
                if (!string.IsNullOrEmpty(muni)) C("Municipality of " + muni, fS);
                C(office.ToUpperInvariant(), fB);
                y += 18; C("MARRIAGE LICENSE", fT); y += 4;
                C("License No. " + (l.LicenseNo ?? "-") + "     Application No. " + (l.ApplicationNo ?? "-"), fB);
                y += 16;
                g.DrawString("This is to certify that, the requirements of law having been complied with, a license to contract marriage is hereby issued to:",
                    fS, Brushes.Black, new RectangleF(m.Left, y, m.Width, 40));
                y += 42;
                float colW = m.Width / 2f - 10;
                float y0 = y;
                y = Party(g, l.Husband, "HUSBAND / CONTRACTING PARTY", m.Left, y0, colW, fH, fS, l.IssueDate ?? DateTime.Today);
                float y2 = Party(g, l.Wife, "WIFE / CONTRACTING PARTY", m.Left + colW + 20, y0, colW, fH, fS, l.IssueDate ?? DateTime.Today);
                y = Math.Max(y, y2) + 20;
                g.DrawString("Date of issue:  " + MarriageRules.D(l.IssueDate), fB, Brushes.Black, m.Left, y); y += 22;
                g.DrawString("Valid until:      " + MarriageRules.D(l.ExpiryDate), fB, Brushes.Black, m.Left, y); y += 30;
                g.DrawString("This license is valid in any part of the Philippines for a period of one hundred twenty (120) days from the date of issue, " +
                             "and shall be deemed automatically cancelled at the expiration of that period if the contracting parties have not made use of it " +
                             "(Family Code, Art. 20).", fS, Brushes.Black, new RectangleF(m.Left, y, m.Width, 60));
                y += 90;
                float sx = m.Right - 280;
                g.DrawLine(Pens.Black, sx, y, m.Right, y);
                g.DrawString(string.IsNullOrEmpty(registrar) ? "(name of the Civil Registrar)" : registrar.ToUpperInvariant(), fB, Brushes.Black, new RectangleF(sx, y + 2, 280, 20), center);
                g.DrawString(rtitle, fS, Brushes.Black, new RectangleF(sx, y + 20, 280, 20), center);
                g.DrawString("Printed from CROMS " + DateTime.Now.ToString("dd MMM yyyy HH:mm"), new Font("Segoe UI", 7F), Brushes.Gray, m.Left, m.Bottom - 12);
            }
            if (preview)
            {
                using (var red = new SolidBrush(Color.FromArgb(198, 50, 63)))
                using (var f = new Font("Segoe UI", 12F, FontStyle.Bold))
                {
                    g.FillRectangle(red, e.PageBounds.Left, e.PageBounds.Top, e.PageBounds.Width, 34);
                    g.DrawString("PREVIEW - NOT A MARRIAGE LICENSE - NOT ISSUED", f, Brushes.White, e.PageBounds.Left + 20, e.PageBounds.Top + 7);
                }
                var st = g.Save();
                g.TranslateTransform(e.PageBounds.Width / 2f, e.PageBounds.Height / 2f);
                g.RotateTransform(-35);
                using (var wash = new SolidBrush(Color.FromArgb(40, 198, 50, 63)))
                using (var f = new Font("Segoe UI", 54F, FontStyle.Bold))
                    g.DrawString("PREVIEW", f, wash, -170, -40);
                g.Restore(st);
            }
        }

        private static float Party(Graphics g, Party p, string head, float x, float y, float w, Font fH, Font fS, DateTime on)
        {
            g.DrawString(head, fH, Brushes.Black, x, y); y += 22;
            Action<string, string> L = (k, v) => { g.DrawString(k + ":  " + (string.IsNullOrWhiteSpace(v) ? "-" : v), fS, Brushes.Black, new RectangleF(x, y, w, 34)); y += 19; };
            L("Name", p.FullName.ToUpperInvariant());
            L("Date of birth", MarriageRules.D(p.Dob) + (p.Dob.HasValue ? "  (age " + MarriageRules.AgeOn(p.Dob.Value, on) + ")" : ""));
            L("Place of birth", p.PlaceOfBirth);
            L("Sex", p.Sex);
            L("Citizenship", p.Citizenship);
            L("Civil status", p.CivilStatus);
            L("Residence", p.Residence);
            // Print the structured first/middle/last blocks (migration 38) rather than the
            // single joined line - falls back to p.Father/p.Mother only for a pre-38 licence
            // that has no separate cells (see MarriageRules.Party.Father/Mother comment).
            string fatherName = MarriageRules.JoinName(p.FatherFirst, p.FatherMiddle, p.FatherLast) ?? p.Father;
            string motherName = MarriageRules.JoinName(p.MotherFirst, p.MotherMiddle, p.MotherLast) ?? p.Mother;
            L("Father's Name", fatherName);
            L("Father's Citizenship", p.FatherCitizenship);
            L("Father's Residence", p.FatherResidence);
            L("Mother's Name", motherName);
            L("Mother's Citizenship", p.MotherCitizenship);
            L("Mother's Residence", p.MotherResidence);
            return y;
        }
    }
}
