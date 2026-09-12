using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// WINDOW 5 - a registered marriage: the record, how it was registered, where each of its
    /// four copies went (Family Code Art. 23) and where it stands with PSA/OCRG. "Sent" and
    /// "available in PSA" are kept apart - the second is shown only against an authoritative
    /// reference someone recorded.
    /// </summary>
    internal sealed class MarriageRecordForm : Form
    {
        private readonly int _id;
        private readonly Panel _left = new Panel(), _right = new Panel();
        private readonly DataGridView _copies = new DataGridView();

        public MarriageRecordForm(int marriageId)
        {
            _id = marriageId;
            Text = "Marriage Record";
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(1120, 780); MinimumSize = new Size(980, 640);
            BackColor = UiTheme.PageBg; ShowInTaskbar = false;
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), BackColor = UiTheme.PageBg };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            _left.Dock = DockStyle.Fill; _left.AutoScroll = true; _left.Padding = new Padding(0, 0, 10, 0);
            _right.Dock = DockStyle.Fill; _right.AutoScroll = true; _right.Padding = new Padding(10, 0, 0, 0);
            grid.Controls.Add(_left, 0, 0); grid.Controls.Add(_right, 1, 0);
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(16, 11, 16, 8), BackColor = UiTheme.Surface };
            foot.Controls.Add(Btn("View Form 97", (s, e) => { using (var f = new MarriageEntryForm(_id)) f.ShowDialog(this); Rebuild(); }));
            foot.Controls.Add(Btn("View License", (s, e) => OpenLicense()));
            foot.Controls.Add(Btn("View Scan", (s, e) => ViewScan()));
            foot.Controls.Add(Btn("Print / Preview", (s, e) => CertificateReport.ShowFor(DocKind.Marriage, _id, this)));
            foot.Controls.Add(Btn("Certified Copy Workflow", (s, e) => CertifiedCopy()));
            foot.Controls.Add(Btn("View History", (s, e) => MUi.HistoryDialog(this, "Marriage", _id, "History - marriage record")));
            var close = MUi.Btn("Close", MUi.Kind.Ghost, 90); close.Click += (s, e) => Close(); foot.Controls.Add(close);
            Controls.Add(grid); Controls.Add(foot);
            Rebuild();
        }

        private static Button Btn(string text, EventHandler h)
        {
            var b = MUi.Btn(text, MUi.Kind.Secondary); b.Click += h; return b;
        }

        private DataRow LoadRow()
        {
            DataTable dt = Db.Pull(
                "SELECT m.*, ml.license_no AS l_no, ml.status AS l_status, ml.issue_date AS l_issue, ml.expiry_date AS l_expiry, " +
                "pm.name AS pm, pp.name AS pp, ch.name AS church, COALESCE(u.full_name, u.username) AS reg_by, " +
                "COALESCE(ru.full_name, ru.username) AS rev_by FROM marriages m " +
                "LEFT JOIN marriage_licenses ml ON ml.id = m.license_id LEFT JOIN municipalities pm ON pm.id = m.place_municipality_id " +
                "LEFT JOIN provinces pp ON pp.id = m.place_province_id LEFT JOIN churches ch ON ch.id = m.church_id " +
                "LEFT JOIN users u ON u.id = m.registered_by LEFT JOIN users ru ON ru.id = m.registrar_review_by WHERE m.id = @id",
                new MySqlParameter("@id", _id));
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private static string S(DataRow r, string c) { return r[c] == DBNull.Value ? null : r[c].ToString(); }
        private static DateTime? Dd(DataRow r, string c) { return r[c] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[c]); }

        private void Rebuild()
        {
            DataRow r = LoadRow();
            if (r == null) { Close(); return; }
            foreach (Panel p in new[] { _left, _right }) { foreach (Control c in p.Controls.Cast<Control>().ToList()) if (c != _copies) c.Dispose(); p.Controls.Clear(); }

            string status = S(r, "status");
            Text = "Marriage Record - " + (S(r, "registry_no") ?? "draft #" + _id);

            // ---- left: the record
            var L = new List<Control>();
            var head = new FlowLayoutPanel { Height = 58, BackColor = Color.Transparent };
            head.Controls.Add(MUi.Txt(S(r, "registry_no") ?? "(no registry number)", 18F, FontStyle.Bold));
            head.Controls.Add(MUi.Pill(status.ToUpperInvariant(), status));
            if (S(r, "registration_type") == "Delayed") head.Controls.Add(MUi.Pill("DELAYED REGISTRATION", "Delayed"));
            L.Add(head);
            L.Add(MUi.Cap("Marriage"));
            L.Add(MUi.Kv("Husband / Party 1", PartyName(r, "husband")));
            L.Add(MUi.Kv("Wife / Party 2", PartyName(r, "wife")));
            L.Add(MUi.Kv("Marriage date", MUi.D(Dd(r, "date_of_marriage")) + (S(r, "time_of_marriage") == null ? "" : "  " + S(r, "time_of_marriage"))));
            L.Add(MUi.Kv("Marriage place", string.Join(", ", new[] { S(r, "church"), S(r, "pm"), S(r, "pp") }.Where(x => !string.IsNullOrEmpty(x)))));
            string basis = S(r, "license_basis");
            if (basis == "Exempt") L.Add(MUi.Kv("Marriage license", "EXEMPT - " + MarriageRules.ExemptionLabel(S(r, "exemption_basis"))));
            else if (S(r, "l_no") != null)
            {
                L.Add(MUi.Kv("Marriage license", S(r, "l_no")));
                L.Add(MUi.Kv("License status", (S(r, "l_status") ?? "-").ToUpperInvariant()));
                L.Add(MUi.Kv("License issued / valid until", MUi.D(Dd(r, "l_issue")) + "  /  " + MUi.D(Dd(r, "l_expiry"))));
            }
            else L.Add(MUi.Kv("Marriage license", S(r, "license_no") != null ? S(r, "license_no") + " (typed - legacy, not linked)" : "Not recorded (legacy record)"));
            L.Add(MUi.Kv("Solemnizing officer", (S(r, "solemnizer") ?? "-") + (S(r, "solemnizer_position") == null ? "" : ", " + S(r, "solemnizer_position"))));
            L.Add(MUi.Kv("Witnesses", string.Join(" / ", new[] { S(r, "witness1_name"), S(r, "witness2_name") }.Where(x => x != null))));

            L.Add(MUi.Cap("Registration"));
            L.Add(MUi.Kv("Received", MUi.D(Dd(r, "received_by_date")) + (S(r, "received_by") == null ? "" : " - " + S(r, "received_by"))));
            L.Add(MUi.Kv("Registered", MUi.D(Dd(r, "date_registered"))));
            L.Add(MUi.Kv("Registered by", S(r, "reg_by") ?? (Dd(r, "date_registered") == null ? "(legacy record - not recorded)" : "-")));
            L.Add(MUi.Kv("Registration", S(r, "registration_type") ?? "(legacy - not recorded)"));
            if (S(r, "registrar_review_status") != null)
                L.Add(MUi.Kv("Registrar review", S(r, "registrar_review_status") + (S(r, "rev_by") == null ? "" : " - " + S(r, "rev_by"))));

            L.Add(MUi.Cap("Document"));
            L.Add(MUi.Kv("Source scan", r["scan_image"] == DBNull.Value ? "none on file" : "on file (View Scan)"));
            L.Add(MUi.Kv("OCR review", S(r, "ocr_review_status") == null ? "no scan-based entry" :
                S(r, "ocr_review_status") + (r["ocr_confidence"] == DBNull.Value ? "" : " - " + r["ocr_confidence"] + "% confidence")));
            Stack(_left, L);

            // ---- right: copies + PSA
            var R = new List<Control>();
            R.Add(MUi.Cap("Copy distribution (Family Code Art. 23)"));
            _copies.ReadOnly = true; _copies.AllowUserToAddRows = false; _copies.RowHeadersVisible = false;
            _copies.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _copies.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _copies.Height = 180; _copies.BackgroundColor = UiTheme.Surface; _copies.BorderStyle = BorderStyle.None;
            DataTable copies = MarriageService.Copies(_id);
            var view = new DataTable();
            view.Columns.Add("id", typeof(int)); view.Columns.Add("Copy"); view.Columns.Add("For"); view.Columns.Add("Status");
            view.Columns.Add("Recipient"); view.Columns.Add("Date");
            foreach (DataRow c in copies.Rows)
                view.Rows.Add(c["id"], c["copy_type"], c["intended_for"], c["status"], c["recipient"],
                    c["disposition_date"] == DBNull.Value ? "" : MUi.D(Convert.ToDateTime(c["disposition_date"])));
            _copies.DataSource = view;
            _copies.DataBindingComplete += (s, e) =>
            {
                if (_copies.Columns.Contains("id")) _copies.Columns["id"].Visible = false;
                if (_copies.Columns.Contains("For")) _copies.Columns["For"].FillWeight = 190;
            };
            _copies.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && _copies.Columns[e.ColumnIndex].Name == "Status")
                { e.CellStyle.ForeColor = MUi.InkOf(Convert.ToString(e.Value)); e.CellStyle.Font = MUiFonts.Bold9; }
            };
            _copies.CellDoubleClick += (s, e) => EditCopy();
            R.Add(_copies);
            if (copies.Rows.Count == 0)
                R.Add(MUi.Txt(status == "Registered" ? "No copy rows - this record was registered before copy tracking existed." : "Copies are opened when the marriage is registered.",
                    9F, FontStyle.Regular, UiTheme.Muted));
            else
            {
                var edit = MUi.Btn("Update selected copy...", MUi.Kind.Secondary); edit.Click += (s, e) => EditCopy();
                R.Add(Row(edit));
            }

            R.Add(MUi.Cap("PSA / OCRG transmittal"));
            string batch = null;
            string psa = status == "Registered" ? MarriageService.PsaStatus(_id, out batch) : "Not registered";
            batch = status == "Registered" ? batch : null;
            var pp = new FlowLayoutPanel { Height = 34, BackColor = Color.Transparent };
            pp.Controls.Add(MUi.Txt("Status", 9F, FontStyle.Regular, UiTheme.Muted));
            pp.Controls.Add(MUi.Pill(psa.ToUpperInvariant(), psa));
            R.Add(pp);
            R.Add(MUi.Kv("Batch", batch ?? "-"));
            R.Add(MUi.Kv("PSA availability", S(r, "psa_available_reference") != null
                ? "CONFIRMED " + MUi.D(Dd(r, "psa_available_date")) + " - ref " + S(r, "psa_available_reference")
                : "not confirmed (sending is not the same as availability)"));
            if (status == "Registered")
            {
                var add = MUi.Btn("Add to PSA batch", MUi.Kind.Primary);
                add.Enabled = psa == "Pending Transmittal" || psa.StartsWith("Returned");
                add.Click += (s, e) => { using (var f = new PsaTransmittalForm(_id)) f.ShowDialog(this); Rebuild(); };
                var ret = MUi.Btn("Record returned by PSA", MUi.Kind.Secondary);
                ret.Enabled = psa == "Sent to PSA/OCRG" || psa == "Acknowledged";
                ret.Click += (s, e) =>
                {
                    string why = MUi.Ask(this, "Returned by PSA/OCRG", "What did PSA/OCRG ask to be corrected?");
                    if (why == null) return;
                    try { MarriageService.ReturnItem(_id, why); Rebuild(); } catch (Exception ex) { MUi.Fail(this, ex); }
                };
                var conf = MUi.Btn("Confirm PSA availability", MUi.Kind.Secondary);
                conf.Enabled = MarriageService.IsRegistrar && S(r, "psa_available_reference") == null;
                conf.Click += (s, e) =>
                {
                    string refNo = MUi.Ask(this, "PSA availability", "Authoritative reference that the record is available at PSA (e.g. SECPA serial of a PSA copy, PSA acknowledgement no.):");
                    if (refNo == null) return;
                    try { MarriageService.ConfirmPsaAvailability(_id, DateTime.Today, refNo); Rebuild(); } catch (Exception ex) { MUi.Fail(this, ex); }
                };
                R.Add(Row(add, ret));
                R.Add(Row(conf));
            }
            Stack(_right, R);
            UiTheme.Polish(this);
        }

        private static string PartyName(DataRow r, string pre)
        {
            return string.Join(" ", new[] { S(r, pre + "_first_name"), S(r, pre + "_middle_name"), S(r, pre + "_last_name") }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static FlowLayoutPanel Row(params Control[] c)
        {
            var f = new FlowLayoutPanel { Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
            f.Controls.AddRange(c);
            return f;
        }

        private static void Stack(Control host, List<Control> items)
        {
            for (int i = items.Count - 1; i >= 0; i--) { items[i].Dock = DockStyle.Top; host.Controls.Add(items[i]); }
        }

        private void EditCopy()
        {
            if (_copies.CurrentRow == null) return;
            int copyId = Convert.ToInt32(_copies.CurrentRow.Cells["id"].Value);
            DataRow c = MarriageService.Copies(_id).AsEnumerable().First(x => Convert.ToInt32(x["id"]) == copyId);
            using (var f = new Form
            {
                Text = "Copy - " + c["copy_type"], StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(480, 330), MinimizeBox = false, MaximizeBox = false, BackColor = UiTheme.Surface, ShowInTaskbar = false
            })
            {
                var st = MUi.Combo(false, MarriageService.CopyStatuses); st.SelectedItem = c["status"].ToString();
                var rec = MUi.Box(); rec.Text = Convert.ToString(c["recipient"]);
                var dt = MUi.Date(true); MUi.Put(dt, c["disposition_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(c["disposition_date"]));
                var refNo = MUi.Box(); refNo.Text = Convert.ToString(c["reference_no"]);
                var rem = MUi.Box(); rem.Text = Convert.ToString(c["remarks"]);
                var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 12, 18, 0) };
                var items = new List<Control>
                {
                    MUi.Txt(c["intended_for"].ToString(), 9F, FontStyle.Italic, UiTheme.Muted),
                    Wrap(MUi.Field("Status", st)), Wrap(MUi.Field("Released / sent to", rec)), Wrap(MUi.Field("Date", dt)),
                    Wrap(MUi.Field("Reference", refNo)), Wrap(MUi.Field("Remarks", rem))
                };
                Stack(body, items);
                var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 9, 12, 8) };
                var ok = MUi.Btn("Save", MUi.Kind.Primary, 90); var cancel = MUi.Btn("Cancel", MUi.Kind.Secondary, 90);
                cancel.DialogResult = DialogResult.Cancel;
                ok.Click += (s, e) =>
                {
                    try
                    {
                        MarriageService.UpdateCopy(copyId, st.SelectedItem as string, rec.Text.Trim(), MUi.Val(dt), refNo.Text.Trim(), rem.Text.Trim());
                        f.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex) { MUi.Fail(f, ex); }
                };
                foot.Controls.Add(ok); foot.Controls.Add(cancel);
                f.Controls.Add(body); f.Controls.Add(foot);
                f.CancelButton = cancel;
                UiTheme.Polish(f);
                if (f.ShowDialog(this) == DialogResult.OK) Rebuild();
            }
        }

        private static Control Wrap(Control field) { field.Height = 54; return field; }

        private void OpenLicense()
        {
            object lid = Db.Pull("SELECT license_id FROM marriages WHERE id=@id", new MySqlParameter("@id", _id)).Rows[0][0];
            if (lid == DBNull.Value) { MessageBox.Show(this, "No licence is linked to this record (licence-exempt or legacy).", "License", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var f = new MarriageLicenseForm(Convert.ToInt32(lid))) f.ShowDialog(this);
        }

        private void ViewScan()
        {
            object b = Db.Pull("SELECT scan_image FROM marriages WHERE id=@id", new MySqlParameter("@id", _id)).Rows[0][0];
            if (b == DBNull.Value) { MessageBox.Show(this, "No softcopy is saved for this record.", "Scan", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            SoftcopyViewer.Show((byte[])b, "Certificate of Marriage - original softcopy", this);
        }

        private void CertifiedCopy()
        {
            MainForm shell = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (shell == null) return;
            Close();
            shell.GoToModule("certrequest");
        }
    }
}
