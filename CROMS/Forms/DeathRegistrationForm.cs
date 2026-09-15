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

        /// <summary>
        /// WHICH FORM this record is — stored on the row so the certificate can be
        /// reprinted in the layout it came off. Defaults to the revision the office issues
        /// today; a scan routed here through Intelligent Document Processing supplies its
        /// own through PrimeFromExtraction.
        /// </summary>
        private string _formCode = FormCatalog.Current(DocKind.Death)?.FormCode;
        private string _formName = FormCatalog.Current(DocKind.Death)?.FormName;

        // Master-File dropdowns that replace free-text lookup fields at runtime.
        private ComboBox _cboDCit, _cboDRel, _cboDImm, _cboDAnt, _cboDUnd;
        /// <summary>
        /// Place of Death, in the order the boxes appear on screen:
        /// facility, PROVINCE, MUNICIPALITY.
        /// <para/>
        /// Province comes before municipality because the two now CASCADE - the
        /// municipality list is the municipalities of the chosen province, out of the
        /// 1,647 the country has, and a list cannot be narrowed by a choice that has not
        /// been made yet.
        /// <para/>
        /// The STORED string keeps its original order, "facility, municipality, province",
        /// which is what every existing record holds and what the death-certificate view
        /// rebuilds when the text column is empty. Reordering the boxes is a screen
        /// change; reordering the storage would silently re-read every row already
        /// written. <see cref="JoinPod"/> and <see cref="SetPod"/> hold that mapping.
        /// </summary>
        private ComboBox[] _pod;

        /// <summary>Relationship to the Deceased, item 26.</summary>
        private ComboBox _cboInfRel;

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
                if (_editingId != null)
                {
                    // No scanned original on file — fall back to the official Municipal
                    // Form 103 blank already in Assets, filled in from the saved record.
                    CertificateReport.Show(_formCode, _editingId.Value, this);
                    return;
                }
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
            // Relationship to the DECEASED, so the list is filtered to the entries that
            // belong on Municipal Form 103 - the shared master file also carries the
            // birth-side answers (Attending Midwife, Clinic Administrator), and offering
            // those here is what BR-16 was raised about.
            _cboInfRel = LookupOver(txtCInfRel, "SELECT name FROM relationships " +
                "WHERE applies_to IN ('Death','Both') ORDER BY name");
            OthersBox.Bind(_cboInfRel, txtCInfRelOther, lblCInfRelOther);

            _pod = LookupTriple(txtPlace, "hospitals", null, null,
                "Hospital / Clinic", "Province", "Municipality");
            GeoLookup.LoadProvinces(_pod[1]);
            GeoLookup.CascadePlace(_pod[1], _pod[2]);
        }

        /// <summary>
        /// The same overlay as <see cref="Lookup"/>, but the list comes from a query rather
        /// than a whole table - used where only part of a master file belongs on this form.
        /// </summary>
        private ComboBox LookupOver(TextBox tb, string sql)
        {
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Location = tb.Location,
                Size = tb.Size,
                Font = tb.Font,
                Anchor = tb.Anchor
            };
            cbo.Items.Add("");
            try
            {
                using (DataTable dt = Db.Pull(sql))
                    foreach (DataRow r in dt.Rows) cbo.Items.Add(r[0].ToString());
            }
            catch { }
            tb.Parent.Controls.Add(cbo);
            cbo.BringToFront();
            tb.Visible = false;
            return cbo;
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
                DropDownStyle = masterTable == null
                    ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(x, tb.Top),
                Size = new System.Drawing.Size(w, tb.Height),
                Font = tb.Font,
                Anchor = tb.Anchor
            };
            // A null table means the cell is filled by GeoLookup, which loads it from the
            // parent choice rather than from the whole master file.
            if (masterTable != null) FillLookup(cbo, masterTable);
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

        /// <summary>Screen order (facility, province, municipality) written out in the
        /// stored order (facility, municipality, province).</summary>
        private object JoinPod()
        {
            return ComboJoin(new[] { _pod[0], _pod[2], _pod[1] });
        }

        /// <summary>The stored "facility, municipality, province" put back on screen, with
        /// the municipality list loaded before the municipality is selected into it.</summary>
        private void SetPod(string value)
        {
            string[] bits = (value ?? "").Split(new[] { "," }, StringSplitOptions.None);
            string facility = bits.Length > 0 ? bits[0].Trim() : "";
            string municipality = bits.Length > 1 ? bits[1].Trim() : "";
            string province = bits.Length > 2 ? bits[2].Trim() : "";

            SetLookup(_pod[0], facility);
            GeoLookup.Select(_pod[1], province);
            GeoLookup.LoadMunicipalities(_pod[2], province);
            GeoLookup.Select(_pod[2], municipality);
        }

        /// <summary>A certification date the sheet may simply not carry: an unticked
        /// picker writes NULL rather than inventing today.</summary>
        private static object Picked(DateTimePicker dtp)
        {
            return dtp.Checked ? (object)dtp.Value.Date : DBNull.Value;
        }

        private static void SetPicked(DateTimePicker dtp, object v)
        {
            if (v != DBNull.Value && v != null) { dtp.Value = Convert.ToDateTime(v); dtp.Checked = true; }
            else dtp.Checked = false;
        }

        private static object NullIfEmpty(string v)
        {
            return string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v.Trim();
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
                "date_of_death AS 'Date of Death', book_volume AS Book, book_page AS Page, " +
                "permit_type AS Permit, status AS Status " +
                "FROM deaths ORDER BY id DESC");
            if (dgvDeaths.Columns.Contains("id")) dgvDeaths.Columns["id"].Visible = false;
        }

        // ---------- CREATE ----------
        private void btnSave_Click(object sender, EventArgs e) { Register(); }

        /// <summary>
        /// Register the death on the form.
        /// <para/>
        /// <paramref name="keepOpen"/> changes what happens afterwards: normally the form
        /// is cleared for the next entry, but a caller that has to act on the record it
        /// just created - Print, which prints from the saved registry entry - reloads it
        /// instead, so the form stays on that record and <c>_editingId</c> is set.
        /// Returns the new id, or null if nothing was written.
        /// </summary>
        private long? Register(bool keepOpen = false)
        {
            if (!ValidateName()) return null;
            string registryNo = NextRegistryNo();
            try
            {
                long newId;
                // registry_no is UNIQUE (migration 27), so if another workstation took this
                // number between the MAX+1 read above and this write, the insert is refused
                // rather than duplicating the record's legal key. Take the next and retry.
                for (int attempt = 0; ; attempt++)
                {
                    var ps = new List<MySqlParameter>(FieldParams())
                    {
                        new MySqlParameter("@reg", registryNo),
                        new MySqlParameter("@status", "Registered")
                    };
                    try
                    {
                        newId = Db.Insert(
                            "INSERT INTO deaths (registry_no, status, " + Columns + ") " +
                            "VALUES (@reg, @status, " + ValuePlaceholders + ")", ps.ToArray());
                        break;
                    }
                    catch (MySqlException ex)
                        when (RegistryNumber.WasTaken(ex, "deaths") && attempt < RegistryNumber.MaxRetries)
                    {
                        registryNo = NextRegistryNo();
                    }
                }
                SaveScan(newId);
                Audit.Write(Audit.Create, "deaths", registryNo, txtFullName.Text.Trim());
                if (!keepOpen)
                {
                    MessageBox.Show("Death registered.  Registry No: " + registryNo, "Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                }
                LoadDeaths();
                // Reload from the row actually written, so what is shown - and printed -
                // is the registry entry, registry number included.
                if (keepOpen) LoadDeath((int)newId);
                return newId;
            }
            catch (Exception ex) { Fail(ex); }
            return null;
        }

        // ---------- READ (row -> form) ----------
        private void dgvDeaths_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            LoadDeath(Convert.ToInt32(dgvDeaths.Rows[e.RowIndex].Cells["id"].Value));
        }

        /// <summary>Load one death record into the form for editing or printing.</summary>
        private void LoadDeath(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            // Keep the revision this record was registered on, rather than migrating it
            // to today's when it is edited or reprinted.
            if (dt.Columns.Contains("form_code") && r["form_code"] != DBNull.Value)
                _formCode = r["form_code"].ToString();
            if (dt.Columns.Contains("form_name") && r["form_name"] != DBNull.Value)
                _formName = r["form_name"].ToString();

            txtFullName.Text = Str(r["full_name"]);
            txtBookVol.Text = Str(r["book_volume"]);
            txtBookPage.Text = Str(r["book_page"]);
            SetCombo(cboSex, r["sex"]);
            SetCombo(cboCivil, r["civil_status"]);
            txtAge.Text = Str(r["age"]);
            SetLookup(_cboDCit, Str(r["citizenship"]));
            SetDate(dtpDod, r["date_of_death"]);
            SetTime(dtpTod, r["time_of_death"]);
            SetPod(Str(r["place_of_death"]));
            txtCInfName.Text = Str(r["informant_name"]);
            OthersBox.Apply(_cboInfRel, txtCInfRelOther, lblCInfRelOther,
                Str(r["informant_relationship"]), SetLookup);
            txtCInfAddr.Text = Str(r["informant_address"]);
            SetPicked(dtpCInfDate, r["informant_date"]);
            txtCPrepBy.Text = Str(r["prepared_by"]);
            txtCPrepTitle.Text = Str(r["prepared_by_title"]);
            SetPicked(dtpCPrepDate, r["prepared_by_date"]);
            txtCRecvBy.Text = Str(r["received_by"]);
            txtCRecvTitle.Text = Str(r["received_by_title"]);
            SetPicked(dtpCRecvDate, r["received_by_date"]);
            txtCRegBy.Text = Str(r["registered_by"]);
            txtCRegTitle.Text = Str(r["registered_by_title"]);
            SetPicked(dtpCRegDate, r["registered_by_date"]);
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
            // A certificate is printed from the REGISTRY ENTRY, not from the boxes on
            // screen: an unsaved form has no registry number, no audit row and no record
            // anyone can look up. But the operator should not be sent hunting in the list
            // — least of all straight after Intelligent Document Processing filled this
            // form from a scan, where no record exists yet — so the missing step is
            // offered here instead of merely reported.
            if (_editingId == null)
            {
                if (!ValidateName()) return;
                if (MessageBox.Show(
                        "This death has not been registered yet, and the certificate is " +
                        "printed from the saved registry entry — not from the form on " +
                        "screen.\n\nRegister it now and print the certificate?\n\n" +
                        "A registry number will be assigned.",
                        "Register and print certificate", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes) return;

                long? created = Register(keepOpen: true);
                // Register's reload sets _editingId; verify rather than assume, so a
                // failed save cannot fall through into printing nothing.
                if (!created.HasValue || _editingId == null) return;
            }

            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = @id",
                new MySqlParameter("@id", _editingId.Value));
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Record not found.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataRow r = dt.Rows[0];

            // The CERTIFICATE goes through the shared report path, so it gets the same
            // treatment as every other form: Crystal when a .rpt exists for this
            // revision, the logo and stamp, and the form identity in its header.
            CertificateReport.Show(_formCode, _editingId.Value, this);

            // The BURIAL / TRANSFER PERMIT is a different document, not a rendering of the
            // Certificate of Death, so it stays a separate print rather than being folded
            // into the certificate's report — and it is only offered when a permit was
            // actually issued.
            if (string.IsNullOrWhiteSpace(V(r, "permit_type"))) return;

            if (MessageBox.Show(
                    "Also print the " + V(r, "permit_type") + " permit for this record?",
                    "Burial / Transfer Permit", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (var doc = new PrintDocument())
                {
                    doc.DocumentName = "Permit " + V(r, "registry_no");
                    doc.PrintPage += (s, ev) => { DrawPermit(ev, r); ev.HasMorePages = false; };
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
            "informant_name, informant_relationship, informant_address, informant_date, " +
            "prepared_by, prepared_by_title, prepared_by_date, " +
            "received_by, received_by_title, received_by_date, " +
            "registered_by, registered_by_title, registered_by_date, " +
            "form_code, form_name, full_name, book_volume, book_page, sex, civil_status, age, citizenship, date_of_death, time_of_death, place_of_death, " +
            "religion_name, immediate_cause, antecedent_cause, underlying_cause, medical_certifier, " +
            "certifier_license_no, disposal_method, place_of_disposal, date_of_disposal, permit_type";

        private const string ValuePlaceholders =
            "@form_code, @form_name, @name, @bookvol, @bookpage, @sex, @civil, @age, @citizen, @dod, @tod, @place, @religion, @imm, @ant, @und, @cert, " +
            "@lic, @disp, @dplace, @ddate, @permit";

        private const string SetClause =
            "form_code=@form_code, form_name=@form_name, full_name=@name, book_volume=@bookvol, book_page=@bookpage, " +
            "sex=@sex, civil_status=@civil, age=@age, citizenship=@citizen, " +
            "informant_name=@iname, informant_relationship=@irel, informant_address=@iaddr, " +
            "informant_date=@idate, prepared_by=@prep, prepared_by_title=@preptitle, " +
            "prepared_by_date=@prepdate, received_by=@recv, received_by_title=@recvtitle, " +
            "received_by_date=@recvdate, registered_by=@regby, registered_by_title=@regbytitle, " +
            "registered_by_date=@regbydate, " +
            "date_of_death=@dod, time_of_death=@tod, place_of_death=@place, religion_name=@religion, " +
            "immediate_cause=@imm, antecedent_cause=@ant, underlying_cause=@und, medical_certifier=@cert, " +
            "certifier_license_no=@lic, disposal_method=@disp, place_of_disposal=@dplace, " +
            "date_of_disposal=@ddate, permit_type=@permit";

        private MySqlParameter[] FieldParams()
        {
            return new[]
            {
                new MySqlParameter("@form_code", _formCode),
                new MySqlParameter("@form_name", _formName),
                new MySqlParameter("@name", txtFullName.Text.Trim()),
                new MySqlParameter("@bookvol", S(txtBookVol)),
                new MySqlParameter("@bookpage", S(txtBookPage)),
                new MySqlParameter("@sex", Combo(cboSex)),
                new MySqlParameter("@civil", Combo(cboCivil)),
                new MySqlParameter("@age", I(txtAge)),
                new MySqlParameter("@citizen", ComboVal(_cboDCit)),
                new MySqlParameter("@dod", dtpDod.Value.Date),
                new MySqlParameter("@tod", dtpTod.Value.ToString("HH:mm")),
                new MySqlParameter("@place", JoinPod()),
                // Items 26-29. The dates show their own check box, so "no date on this
                // sheet" stays sayable instead of every record claiming today.
                new MySqlParameter("@iname", S(txtCInfName)),
                new MySqlParameter("@irel", NullIfEmpty(OthersBox.Compose(_cboInfRel, txtCInfRelOther))),
                new MySqlParameter("@iaddr", S(txtCInfAddr)),
                new MySqlParameter("@idate", Picked(dtpCInfDate)),
                new MySqlParameter("@prep", S(txtCPrepBy)),
                new MySqlParameter("@preptitle", S(txtCPrepTitle)),
                new MySqlParameter("@prepdate", Picked(dtpCPrepDate)),
                new MySqlParameter("@recv", S(txtCRecvBy)),
                new MySqlParameter("@recvtitle", S(txtCRecvTitle)),
                new MySqlParameter("@recvdate", Picked(dtpCRecvDate)),
                new MySqlParameter("@regby", S(txtCRegBy)),
                new MySqlParameter("@regbytitle", S(txtCRegTitle)),
                new MySqlParameter("@regbydate", Picked(dtpCRegDate)),
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
            return RegistryNumber.Next("deaths", 'D');
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

            // The scan's own form revision, when one was identified. ClearForm has just
            // reset this to today's, so it must be applied AFTER that call.
            if (f.TryGetValue("FormCode", out string fcode) && !string.IsNullOrWhiteSpace(fcode))
                _formCode = fcode.Trim();
            if (f.TryGetValue("FormName", out string fname) && !string.IsNullOrWhiteSpace(fname))
                _formName = fname.Trim();

            string Get(string key) => f.TryGetValue(key, out string v) && v != null ? v.Trim() : "";

            void SelectItem(ComboBox c, string val)
            {
                if (string.IsNullOrWhiteSpace(val)) return;
                foreach (object it in c.Items)
                    if (string.Equals(it.ToString(), val, StringComparison.OrdinalIgnoreCase))
                    { c.SelectedItem = it; return; }
            }

            // Items 26-29, read off the scan like everything else. A date the extractor
            // could not resolve to yyyy-MM-dd is AMBIGUOUS, not merely unread - "2-6-2018"
            // is either 2 June or 6 February - so the picker stays unticked and the
            // operator enters it rather than the form guessing.
            void SetText(TextBox t, string key) { string v = Get(key); if (v.Length > 0) t.Text = v; }
            void SetPick(DateTimePicker picker, string key)
            {
                if (DateTime.TryParseExact(Get(key), "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime parsed))
                { picker.Value = parsed; picker.Checked = true; }
            }

            SetText(txtCInfName, "Informant");
            OthersBox.Apply(_cboInfRel, txtCInfRelOther, lblCInfRelOther,
                Get("InformantRelationship"), SetLookup);
            SetText(txtCInfAddr, "InformantAddress");
            SetPick(dtpCInfDate, "InformantDate");
            SetText(txtCPrepBy, "PreparedByName");
            SetText(txtCPrepTitle, "PreparedByTitle");
            SetPick(dtpCPrepDate, "PreparedByDate");
            SetText(txtCRecvBy, "ReceivedByName");
            SetText(txtCRecvTitle, "ReceivedByTitle");
            SetPick(dtpCRecvDate, "ReceivedByDate");
            SetText(txtCRegBy, "RegisteredByName");
            SetText(txtCRegTitle, "RegisteredByTitle");
            SetPick(dtpCRegDate, "RegisteredByDate");

            string full = Get("FullName");
            if (full.Length == 0)
                full = string.Join(" ", new[] { Get("DeceasedFirst"), Get("DeceasedMiddle"), Get("DeceasedLast") }
                    .Where(s => s.Length > 0));
            if (full.Length > 0) txtFullName.Text = full;

            if (Get("Age").Length > 0) txtAge.Text = Get("Age");
            SelectItem(cboSex, Get("Sex"));
            SelectItem(cboCivil, Get("CivilStatus"));
            if (Get("Citizenship").Length > 0) SetLookup(_cboDCit, Get("Citizenship"));
            if (Get("PlaceOfDeath").Length > 0) SetPod(Get("PlaceOfDeath"));

            if (Get("DateOfDeath").Length > 0 &&
                DateTime.TryParse(Get("DateOfDeath"), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime d))
                dtpDod.Value = d;
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            _formCode = FormCatalog.Current(DocKind.Death)?.FormCode;
            _formName = FormCatalog.Current(DocKind.Death)?.FormName;
            txtFullName.Clear();
            txtBookVol.Clear();
            txtBookPage.Clear();
            cboSex.SelectedIndex = -1;
            cboCivil.SelectedIndex = -1;
            txtAge.Clear();
            SetLookup(_cboDCit, "");
            dtpDod.Value = DateTime.Today;
            SetPod("");
            OthersBox.Apply(_cboInfRel, txtCInfRelOther, lblCInfRelOther, "", SetLookup);
            foreach (DateTimePicker d in new[] { dtpCInfDate, dtpCPrepDate, dtpCRecvDate, dtpCRegDate })
                d.Checked = false;
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
