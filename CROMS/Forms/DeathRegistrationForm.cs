using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Death Registration — PSA Municipal Form 103, with full CRUD on the `deaths`
    /// table. Register Death creates (auto registry no); clicking a grid row loads
    /// that record; Update saves changes; Delete removes it; New clears the form.
    /// Controls are placed in the Designer.
    /// </summary>
    public partial class DeathRegistrationForm : Form, IRefreshable
    {
        private int? _editingId;

        // Master-File dropdowns that replace free-text lookup fields at runtime.
        private ComboBox _cboDCit, _cboDRel, _cboDImm, _cboDAnt, _cboDUnd;
        private ComboBox[] _pod;   // Place of Death: hospital, municipality, province

        public void RefreshData() => LoadDeaths();

        public DeathRegistrationForm()
        {
            InitializeComponent();
            LoadCombos();
            BuildLookups();
            dgvDeaths.CellClick += dgvDeaths_CellClick;
            LoadDeaths();
            LearningLibrary.Attach(txtFullName, LearningLibrary.Surname);
            LearningLibrary.Attach(txtDispPlace, LearningLibrary.Cemetery);
            LearningLibrary.Attach(txtCertifier, LearningLibrary.Officer);
            BuildSoftcopyButton();
        }

        // Softcopy of the source certificate (scan from Document AI), saved with the record.
        private byte[] _scanImage;
        private Button btnViewScan;

        /// <summary>Attach the original scanned document to the next saved record.</summary>
        public void SetScanImage(byte[] bytes) => _scanImage = bytes;

        private void SaveScan(long id)
        {
            if (_scanImage == null || _scanImage.Length == 0) return;
            Db.Push("UPDATE deaths SET scan_image = @img WHERE id = @id",
                new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = _scanImage },
                new MySqlParameter("@id", id));
        }

        private void BuildSoftcopyButton()
        {
            btnViewScan = new Button
            {
                Text = "View Softcopy",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Size = new System.Drawing.Size(120, btnSave.Height),
                Location = new System.Drawing.Point(btnSave.Left - 6 - 120, btnSave.Top),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnViewScan.Click += btnViewScan_Click;
            btnSave.Parent.Controls.Add(btnViewScan);
            btnViewScan.BringToFront();
        }

        private void btnViewScan_Click(object sender, EventArgs e)
        {
            byte[] bytes = _scanImage;
            if (bytes == null && _editingId != null)
            {
                DataTable dt = Db.Pull("SELECT scan_image FROM deaths WHERE id = " + _editingId.Value);
                if (dt.Rows.Count > 0 && dt.Rows[0]["scan_image"] != DBNull.Value)
                    bytes = (byte[])dt.Rows[0]["scan_image"];
            }
            if (bytes == null)
            {
                MessageBox.Show("No softcopy is saved for this record.", "Softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(bytes, "Death Certificate — Original Softcopy", this);
        }

        /// <summary>
        /// Replaces the free-text lookup fields with selection-only comboboxes fed from
        /// the Master Files. Comboboxes overlay the (now hidden) textboxes and save into
        /// the same columns, so no schema change is needed.
        /// </summary>
        private void BuildLookups()
        {
            _cboDCit = Lookup(txtCitizen, "nationalities");
            _cboDRel = Lookup(txtReligion, "religions");
            _cboDImm = Lookup(txtImm, "causes_of_death");
            _cboDAnt = Lookup(txtAnt, "causes_of_death");
            _cboDUnd = Lookup(txtUnd, "causes_of_death");
            _pod = LookupTriple(txtPlace, "hospitals", "municipalities", "provinces",
                "Hospital / Clinic", "Municipality", "Province");
        }

        private ComboBox Lookup(TextBox tb, string masterTable)
        {
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = tb.Location,
                Size = tb.Size,
                Font = tb.Font,
                Anchor = tb.Anchor
            };
            FillLookup(cbo, masterTable);
            tb.Parent.Controls.Add(cbo);
            cbo.BringToFront();
            tb.Visible = false;
            return cbo;
        }

        private ComboBox[] LookupTriple(TextBox tb, string t1, string t2, string t3,
            string cap1, string cap2, string cap3)
        {
            const int gap = 6;
            int w = (tb.Width - 2 * gap) / 3;
            var a = TripleCombo(tb, tb.Left, w, t1, cap1);
            var b = TripleCombo(tb, tb.Left + w + gap, w, t2, cap2);
            var c = TripleCombo(tb, tb.Left + 2 * (w + gap), tb.Width - 2 * (w + gap), t3, cap3);
            tb.Visible = false;
            return new[] { a, b, c };
        }

        private ComboBox TripleCombo(TextBox tb, int x, int w, string masterTable, string caption)
        {
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(x, tb.Top),
                Size = new System.Drawing.Size(w, tb.Height),
                Font = tb.Font,
                Anchor = tb.Anchor
            };
            FillLookup(cbo, masterTable);
            tb.Parent.Controls.Add(cbo);
            cbo.BringToFront();

            var lbl = new Label
            {
                Text = caption,
                AutoSize = true,
                ForeColor = System.Drawing.Color.FromArgb(108, 117, 125),
                Font = new System.Drawing.Font("Segoe UI", 7.5F),
                Location = new System.Drawing.Point(x, tb.Bottom + 2),
                Anchor = tb.Anchor
            };
            tb.Parent.Controls.Add(lbl);
            lbl.BringToFront();
            return cbo;
        }

        private static void FillLookup(ComboBox cbo, string masterTable)
        {
            cbo.Items.Add("");   // blank = none chosen
            try
            {
                DataTable dt = Db.Pull("SELECT name FROM " + masterTable + " ORDER BY name");
                foreach (DataRow r in dt.Rows) cbo.Items.Add(r["name"].ToString());
            }
            catch { /* master list missing — leave just the blank entry */ }
        }

        private static object ComboVal(ComboBox c)
        {
            string v = c.SelectedItem?.ToString() ?? "";
            return string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v;
        }

        private static object ComboJoin(ComboBox[] parts)
        {
            var vals = new List<string>();
            foreach (var c in parts)
            {
                string v = c.SelectedItem?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(v)) vals.Add(v);
            }
            return vals.Count == 0 ? (object)DBNull.Value : string.Join(", ", vals);
        }

        private static void SetLookup(ComboBox c, string value)
        {
            if (string.IsNullOrEmpty(value)) { c.SelectedIndex = c.Items.Count > 0 ? 0 : -1; return; }
            int idx = c.Items.IndexOf(value);
            if (idx < 0) { c.Items.Add(value); idx = c.Items.Count - 1; }
            c.SelectedIndex = idx;
        }

        private static void SplitToTriple(ComboBox[] parts, string value)
        {
            string[] bits = (value ?? "").Split(new[] { ", " }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
                SetLookup(parts[i], i < bits.Length ? bits[i].Trim() : "");
        }

        private void LoadCombos()
        {
            cboSex.Items.AddRange(new object[] { "Male", "Female" });
            cboCivil.Items.AddRange(new object[] { "Single", "Married", "Widowed", "Separated", "Divorced", "Annulled" });
            cboDisposal.Items.AddRange(new object[] { "Burial", "Cremation", "Transfer", "Embalming" });
            cboPermit.Items.AddRange(new object[] { "Burial Permit", "Transfer Permit" });
        }

        private void LoadDeaths()
        {
            dgvDeaths.DataSource = Db.Pull(
                "SELECT id, registry_no AS 'Registry No', full_name AS Deceased, age AS Age, " +
                "date_of_death AS 'Date of Death', permit_type AS Permit, status AS Status " +
                "FROM deaths ORDER BY id DESC");
            if (dgvDeaths.Columns.Contains("id")) dgvDeaths.Columns["id"].Visible = false;
        }

        // ---------- CREATE ----------
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateName()) return;
            string registryNo = NextRegistryNo();
            try
            {
                var ps = new List<MySqlParameter>(FieldParams())
                {
                    new MySqlParameter("@reg", registryNo),
                    new MySqlParameter("@status", "Registered")
                };
                long newId = Db.Insert(
                    "INSERT INTO deaths (registry_no, status, " + Columns + ") " +
                    "VALUES (@reg, @status, " + ValuePlaceholders + ")", ps.ToArray());
                SaveScan(newId);
                Audit.Write(Audit.Create, "deaths", registryNo, txtFullName.Text.Trim());
                MessageBox.Show("Death registered.  Registry No: " + registryNo, "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadDeaths();
            }
            catch (Exception ex) { Fail(ex); }
        }

        // ---------- READ (row -> form) ----------
        private void dgvDeaths_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(dgvDeaths.Rows[e.RowIndex].Cells["id"].Value);
            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;

            txtFullName.Text = Str(r["full_name"]);
            SetCombo(cboSex, r["sex"]);
            SetCombo(cboCivil, r["civil_status"]);
            txtAge.Text = Str(r["age"]);
            SetLookup(_cboDCit, Str(r["citizenship"]));
            SetDate(dtpDod, r["date_of_death"]);
            SetTime(dtpTod, r["time_of_death"]);
            SplitToTriple(_pod, Str(r["place_of_death"]));
            SetLookup(_cboDRel, Str(r["religion_name"]));
            SetLookup(_cboDImm, Str(r["immediate_cause"]));
            SetLookup(_cboDAnt, Str(r["antecedent_cause"]));
            SetLookup(_cboDUnd, Str(r["underlying_cause"]));
            txtCertifier.Text = Str(r["medical_certifier"]);
            txtLicense.Text = Str(r["certifier_license_no"]);
            SetCombo(cboDisposal, r["disposal_method"]);
            txtDispPlace.Text = Str(r["place_of_disposal"]);
            SetOptionalDate(dtpDispDate, r["date_of_disposal"]);
            SetCombo(cboPermit, r["permit_type"]);
            _scanImage = dt.Columns.Contains("scan_image") && r["scan_image"] != DBNull.Value
                ? (byte[])r["scan_image"] : null;
        }

        // ---------- UPDATE ----------
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_editingId == null)
            {
                MessageBox.Show("Click a record in the list to edit it first.", "Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateName()) return;
            var ps = new List<MySqlParameter>(FieldParams())
            {
                new MySqlParameter("@id", _editingId.Value)
            };
            try
            {
                Db.Push("UPDATE deaths SET " + SetClause + " WHERE id = @id", ps.ToArray());
                SaveScan(_editingId.Value);
                Audit.Write(Audit.Update, "deaths", _editingId.Value, txtFullName.Text.Trim());
                MessageBox.Show("Record updated.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadDeaths();
            }
            catch (Exception ex) { Fail(ex); }
        }

        // ---------- DELETE ----------
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_editingId == null)
            {
                MessageBox.Show("Click a record in the list to delete it first.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete this death record?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM deaths WHERE id = @id",
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "deaths", _editingId.Value, null);
                MessageBox.Show("Record deleted.", "Deleted",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadDeaths();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void btnNew_Click(object sender, EventArgs e) => ClearForm();

        // ---------- PRINT (Certificate of Death + Burial/Transfer Permit) ----------
        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (_editingId == null)
            {
                MessageBox.Show("Click a registered death record in the list first, then print.",
                    "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = @id",
                new MySqlParameter("@id", _editingId.Value));
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Record not found.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataRow r = dt.Rows[0];

            try
            {
                using (var doc = new PrintDocument())
                {
                    bool hasPermit = !string.IsNullOrWhiteSpace(V(r, "permit_type"));
                    int page = 0;
                    doc.DocumentName = "Death " + V(r, "registry_no");
                    doc.PrintPage += (s, ev) =>
                    {
                        if (page == 0) DrawCertificate(ev, r);
                        else DrawPermit(ev, r);
                        page++;
                        ev.HasMorePages = hasPermit && page < 2;
                    };
                    using (var dlg = new PrintDialog { Document = doc, UseEXDialog = true })
                    {
                        if (dlg.ShowDialog() == DialogResult.OK) doc.Print();
                    }
                }
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static string V(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : r[col].ToString();

        /// <summary>Page 1 — the Certificate of Death (Municipal Form 103).</summary>
        private static void DrawCertificate(PrintPageEventArgs e, DataRow r)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left, width = e.MarginBounds.Width, right = e.MarginBounds.Right;
            float y = e.MarginBounds.Top;

            using (var fHead   = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fSub    = new Font("Segoe UI", 9F))
            using (var fTitle  = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (var fLabel  = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fVal    = new Font("Segoe UI", 10F))
            using (var fSmall  = new Font("Segoe UI", 8F))
            using (var center  = new StringFormat { Alignment = StringAlignment.Center })
            {
                void Mid(string t, Font f, float dy) { g.DrawString(t, f, Brushes.Black, new RectangleF(left, y, width, f.GetHeight() + 4), center); y += f.GetHeight() + dy; }
                void Rule() { using (var p = new Pen(Color.Black)) g.DrawLine(p, left, y + 2, right, y + 2); y += 12; }
                void Field(string label, string val)
                {
                    g.DrawString(label, fLabel, Brushes.Black, left, y);
                    g.DrawString(string.IsNullOrEmpty(val) ? "—" : val, fVal, Brushes.Black, left + 180, y);
                    y += 24;
                }

                Mid("Republic of the Philippines", fSub, 2);
                Mid("Municipality of Peñablanca, Cagayan", fHead, 2);
                Mid("Local Civil Registry Office", fSub, 10);
                Mid("CERTIFICATE OF DEATH", fTitle, 2);
                Mid("(Municipal Form No. 103)", fSmall, 8);
                Rule();

                Field("Registry No.", V(r, "registry_no"));
                Field("Status", V(r, "status"));
                Field("Name of Deceased", V(r, "full_name"));
                Field("Sex", V(r, "sex"));
                Field("Civil Status", V(r, "civil_status"));
                Field("Age", V(r, "age"));
                Field("Citizenship", V(r, "citizenship"));
                Field("Date of Death", FmtDate(V(r, "date_of_death")));
                Field("Time of Death", V(r, "time_of_death"));
                Field("Place of Death", V(r, "place_of_death"));
                Field("Religion", V(r, "religion_name"));
                y += 4; Rule();
                Field("Immediate Cause", V(r, "immediate_cause"));
                Field("Antecedent Cause", V(r, "antecedent_cause"));
                Field("Underlying Cause", V(r, "underlying_cause"));
                Field("Medical Certifier", V(r, "medical_certifier"));
                Field("Certifier License No.", V(r, "certifier_license_no"));
                y += 4; Rule();
                Field("Disposal Method", V(r, "disposal_method"));
                Field("Place of Disposal", V(r, "place_of_disposal"));
                Field("Date of Disposal", FmtDate(V(r, "date_of_disposal")));
                Field("Permit Type", V(r, "permit_type"));

                y += 40;
                g.DrawString("_______________________________", fVal, Brushes.Black, right - 300, y); y += 20;
                g.DrawString("MUNICIPAL CIVIL REGISTRAR", fLabel, Brushes.Black, right - 300, y);

                float fy = e.MarginBounds.Bottom - 16;
                g.DrawString("Printed " + DateTime.Now.ToString("ddd, dd MMM yyyy  h:mm tt") + " · CROMS", fSmall, Brushes.Gray, left, fy);
            }
        }

        /// <summary>Page 2 — the Burial / Transfer Permit (printed only when a permit type is set).</summary>
        private static void DrawPermit(PrintPageEventArgs e, DataRow r)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left, width = e.MarginBounds.Width, right = e.MarginBounds.Right;
            float y = e.MarginBounds.Top;

            using (var fHead  = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fSub   = new Font("Segoe UI", 9F))
            using (var fTitle = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (var fBody  = new Font("Segoe UI", 11F))
            using (var fName  = new Font("Segoe UI", 14F, FontStyle.Bold))
            using (var fLabel = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fSmall = new Font("Segoe UI", 8F))
            using (var center = new StringFormat { Alignment = StringAlignment.Center })
            {
                void Mid(string t, Font f, float dy) { g.DrawString(t, f, Brushes.Black, new RectangleF(left, y, width, f.GetHeight() + 4), center); y += f.GetHeight() + dy; }

                Mid("Republic of the Philippines", fSub, 2);
                Mid("Municipality of Peñablanca, Cagayan", fHead, 2);
                Mid("Local Civil Registry Office", fSub, 14);

                string disposal = V(r, "permit_type");
                if (string.IsNullOrEmpty(disposal)) disposal = "Burial";
                Mid(disposal.ToUpperInvariant(), fTitle, 20);

                g.DrawString("Permit No.:  " + V(r, "registry_no"), fLabel, Brushes.Black, left, y); y += 34;

                string method = V(r, "disposal_method");
                if (string.IsNullOrEmpty(method)) method = "disposal";
                g.DrawString("Permission is hereby granted for the " + method.ToLowerInvariant() +
                             " of the remains of:", fBody, Brushes.Black, new RectangleF(left, y, width, 28)); y += 34;

                Mid(V(r, "full_name"), fName, 18);

                g.DrawString("who died on " + FmtDate(V(r, "date_of_death")) +
                             " at " + Dash(V(r, "place_of_death")) + ".", fBody, Brushes.Black,
                             new RectangleF(left, y, width, 28)); y += 34;

                g.DrawString("Place of disposal:  " + Dash(V(r, "place_of_disposal")), fBody, Brushes.Black, left, y); y += 26;
                g.DrawString("Date of disposal:   " + Dash(FmtDate(V(r, "date_of_disposal"))), fBody, Brushes.Black, left, y); y += 50;

                g.DrawString("_______________________________", fBody, Brushes.Black, right - 320, y); y += 22;
                g.DrawString("MUNICIPAL CIVIL REGISTRAR", fLabel, Brushes.Black, right - 320, y);

                float fy = e.MarginBounds.Bottom - 16;
                g.DrawString("Printed " + DateTime.Now.ToString("ddd, dd MMM yyyy  h:mm tt") + " · CROMS", fSmall, Brushes.Gray, left, fy);
            }
        }

        private static string Dash(string s) => string.IsNullOrEmpty(s) ? "—" : s;
        private static string FmtDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "—";
            return DateTime.TryParse(raw, out DateTime d) ? d.ToString("dd MMMM yyyy") : raw;
        }

        // ---------- shared SQL ----------
        private const string Columns =
            "full_name, sex, civil_status, age, citizenship, date_of_death, time_of_death, place_of_death, " +
            "religion_name, immediate_cause, antecedent_cause, underlying_cause, medical_certifier, " +
            "certifier_license_no, disposal_method, place_of_disposal, date_of_disposal, permit_type";

        private const string ValuePlaceholders =
            "@name, @sex, @civil, @age, @citizen, @dod, @tod, @place, @religion, @imm, @ant, @und, @cert, " +
            "@lic, @disp, @dplace, @ddate, @permit";

        private const string SetClause =
            "full_name=@name, sex=@sex, civil_status=@civil, age=@age, citizenship=@citizen, " +
            "date_of_death=@dod, time_of_death=@tod, place_of_death=@place, religion_name=@religion, " +
            "immediate_cause=@imm, antecedent_cause=@ant, underlying_cause=@und, medical_certifier=@cert, " +
            "certifier_license_no=@lic, disposal_method=@disp, place_of_disposal=@dplace, " +
            "date_of_disposal=@ddate, permit_type=@permit";

        private MySqlParameter[] FieldParams()
        {
            return new[]
            {
                new MySqlParameter("@name", txtFullName.Text.Trim()),
                new MySqlParameter("@sex", Combo(cboSex)),
                new MySqlParameter("@civil", Combo(cboCivil)),
                new MySqlParameter("@age", I(txtAge)),
                new MySqlParameter("@citizen", ComboVal(_cboDCit)),
                new MySqlParameter("@dod", dtpDod.Value.Date),
                new MySqlParameter("@tod", dtpTod.Value.ToString("HH:mm")),
                new MySqlParameter("@place", ComboJoin(_pod)),
                new MySqlParameter("@religion", ComboVal(_cboDRel)),
                new MySqlParameter("@imm", ComboVal(_cboDImm)),
                new MySqlParameter("@ant", ComboVal(_cboDAnt)),
                new MySqlParameter("@und", ComboVal(_cboDUnd)),
                new MySqlParameter("@cert", S(txtCertifier)),
                new MySqlParameter("@lic", S(txtLicense)),
                new MySqlParameter("@disp", Combo(cboDisposal)),
                new MySqlParameter("@dplace", S(txtDispPlace)),
                new MySqlParameter("@ddate", dtpDispDate.Checked ? (object)dtpDispDate.Value.Date : DBNull.Value),
                new MySqlParameter("@permit", Combo(cboPermit)),
            };
        }

        /// <summary>
        /// Next death registry number for the current year, as YYYY-D-####. Uses the
        /// highest sequence already issued this year + 1 (NOT a row count) so deleting a
        /// record never causes the next insert to reuse a number. Year-scoped, so the
        /// sequence restarts each January.
        /// </summary>
        private static string NextRegistryNo()
        {
            int year = DateTime.Now.Year;
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM deaths WHERE registry_no LIKE @p",
                new MySqlParameter("@p", year + "-D-%"));
            int next = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value)
                ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-D-{1:D4}", year, next);
        }

        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("The deceased's full name is required.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        /// <summary>Fill the Death form (Municipal Form 103) from Document AI extraction.</summary>
        public void PrimeFromExtraction(System.Collections.Generic.IDictionary<string, string> f)
        {
            ClearForm();

            string Get(string key) => f.TryGetValue(key, out string v) && v != null ? v.Trim() : "";

            void SelectItem(ComboBox c, string val)
            {
                if (string.IsNullOrWhiteSpace(val)) return;
                foreach (object it in c.Items)
                    if (string.Equals(it.ToString(), val, StringComparison.OrdinalIgnoreCase))
                    { c.SelectedItem = it; return; }
            }

            string full = Get("FullName");
            if (full.Length == 0)
                full = string.Join(" ", new[] { Get("DeceasedFirst"), Get("DeceasedMiddle"), Get("DeceasedLast") }
                    .Where(s => s.Length > 0));
            if (full.Length > 0) txtFullName.Text = full;

            if (Get("Age").Length > 0) txtAge.Text = Get("Age");
            SelectItem(cboSex, Get("Sex"));
            SelectItem(cboCivil, Get("CivilStatus"));
            if (Get("Citizenship").Length > 0) SetLookup(_cboDCit, Get("Citizenship"));
            if (Get("PlaceOfDeath").Length > 0) SplitToTriple(_pod, Get("PlaceOfDeath"));

            if (Get("DateOfDeath").Length > 0 &&
                DateTime.TryParse(Get("DateOfDeath"), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime d))
                dtpDod.Value = d;
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            txtFullName.Clear();
            cboSex.SelectedIndex = -1;
            cboCivil.SelectedIndex = -1;
            txtAge.Clear();
            SetLookup(_cboDCit, "");
            dtpDod.Value = DateTime.Today;
            SplitToTriple(_pod, "");
            SetLookup(_cboDRel, "");
            SetLookup(_cboDImm, "");
            SetLookup(_cboDAnt, "");
            SetLookup(_cboDUnd, "");
            txtCertifier.Clear();
            txtLicense.Clear();
            cboDisposal.SelectedIndex = -1;
            txtDispPlace.Clear();
            dtpDispDate.Checked = false;
            cboPermit.SelectedIndex = -1;
        }

        // ---------- helpers ----------
        private static object S(TextBox t) =>
            string.IsNullOrWhiteSpace(t.Text) ? (object)DBNull.Value : t.Text.Trim();
        private static object I(TextBox t) =>
            int.TryParse(t.Text, out int n) ? (object)n : DBNull.Value;
        private static object Combo(ComboBox c) =>
            c.SelectedItem == null ? (object)DBNull.Value : c.SelectedItem.ToString();
        private static string Str(object v) => v == DBNull.Value || v == null ? "" : v.ToString();
        private static void SetCombo(ComboBox c, object v) =>
            c.SelectedItem = v == DBNull.Value || v == null ? null : v.ToString();
        private static void SetDate(DateTimePicker dtp, object v)
        {
            if (v != DBNull.Value && v != null) dtp.Value = Convert.ToDateTime(v);
        }
        private static void SetOptionalDate(DateTimePicker dtp, object v)
        {
            if (v != DBNull.Value && v != null) { dtp.Value = Convert.ToDateTime(v); dtp.Checked = true; }
            else dtp.Checked = false;
        }
        private static void SetTime(DateTimePicker dtp, object v)
        {
            if (v != DBNull.Value && v != null && TimeSpan.TryParse(v.ToString(), out TimeSpan ts))
                dtp.Value = DateTime.Today.Add(ts);
        }
        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
