using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Birth Registration — PSA Municipal Form 102, with full CRUD on the `births`
    /// table. Save Draft / Submit create; clicking a grid row loads that record for
    /// editing; Update saves changes; Delete removes it; New clears for a fresh entry.
    /// Controls are placed in the Designer.
    /// </summary>
    public partial class BirthRegistrationForm : Form, IRefreshable
    {
        private int? _editingId;
        private int _queueTicketId;   // set when a "New Registration" queue ticket is served here

        public void RefreshData()
        {
            _queueTicketId = 0;   // fresh view — drop any stale queue link
            LoadBirths();
        }

        /// <summary>
        /// Called by Queue Management when a "New Registration" ticket is opened here.
        /// Submitting for approval then parks that queue ticket as "Pending Approval";
        /// once the registrar approves the record it returns to the queue as
        /// "For Receiving" so the client can be called back for their copy.
        /// </summary>
        public void PrepareForQueueTicket(int ticketId) => _queueTicketId = ticketId;

        /// <summary>
        /// Fills the Form-102 fields from values extracted by Document AI (keyed by the
        /// canonical keys DocumentAI produces). Starts a fresh Draft, maps what it can,
        /// and leaves the rest for the operator to complete. The operator must review
        /// and Save — nothing is written to the database here.
        /// </summary>
        public void PrimeFromExtraction(System.Collections.Generic.IDictionary<string, string> f)
        {
            ClearForm();

            void Set(TextBox t, string key)
            {
                if (t != null && f.TryGetValue(key, out string v) && !string.IsNullOrWhiteSpace(v)) t.Text = v.Trim();
            }

            Set(txtRegNo, "RegistryNo");
            Set(txtFirstName, "ChildFirst");
            Set(txtMiddleName, "ChildMiddle");
            Set(txtLastName, "ChildLast");
            Set(txtFFirst, "FatherFirst");
            Set(txtFMiddle, "FatherMiddle");
            Set(txtFLast, "FatherLast");
            Set(txtMFirst, "MotherFirst");
            Set(txtMMiddle, "MotherMiddle");
            Set(txtMLast, "MotherLast");
            Set(txtWeight, "Weight");

            if (f.TryGetValue("TypeOfBirth", out string bt))
                foreach (object item in cboTypeOfBirth.Items)
                    if (string.Equals(item.ToString(), bt, StringComparison.OrdinalIgnoreCase))
                    { cboTypeOfBirth.SelectedItem = item; break; }

            if (f.TryGetValue("Sex", out string sex))
            {
                foreach (object item in cboSex.Items)
                    if (string.Equals(item.ToString(), sex, StringComparison.OrdinalIgnoreCase))
                    { cboSex.SelectedItem = item; break; }
            }

            // Lookup combos: fill from the split place + parents' details. Each new value is
            // persisted into its Master-File table (LookupStore) so it becomes a permanent
            // selectable option in every registration form, and added to this combo now.
            string Val(string key) => f.TryGetValue(key, out string v) && v != null ? v.Trim() : "";

            // Place of Birth: prefer the pre-split cells; fall back to splitting the combined text.
            string ph = Val("PlaceHospital"), pm = Val("PlaceMunicipality"), pp = Val("PlaceProvince");
            if (ph.Length == 0 && pm.Length == 0 && pp.Length == 0)
            {
                string[] bits = Val("PlaceOfBirth").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                if (bits.Length > 0) ph = bits[0].Trim();
                if (bits.Length > 1) pm = bits[1].Trim();
                if (bits.Length > 2) pp = bits[2].Trim();
            }
            PrimeLookup(_pob[0], "hospitals", LearningLibrary.Hospital, ph);
            PrimeLookup(_pob[1], "municipalities", LearningLibrary.Municipality, pm);
            PrimeLookup(_pob[2], "provinces", LearningLibrary.Province, pp);

            string nat = Val("Nationality");
            PrimeLookup(_cboMCit, "nationalities", LearningLibrary.Nationality, nat);
            PrimeLookup(_cboFCit, "nationalities", LearningLibrary.Nationality, nat);

            string rel = Val("Religion");
            PrimeLookup(_cboMRel, "religions", null, rel);
            PrimeLookup(_cboFRel, "religions", null, rel);

            PrimeLookup(_cboMOcc, "occupations", LearningLibrary.Occupation, Val("MotherOccupation"));
            PrimeLookup(_cboFOcc, "occupations", LearningLibrary.Occupation, Val("FatherOccupation"));

            if (f.TryGetValue("DateOfBirth", out string dob) &&
                DateTime.TryParse(dob, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime d))
                dtpDob.Value = d;

            if (f.TryGetValue("TimeOfBirth", out string tob) &&
                DateTime.TryParse(tob, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime tt))
                dtpTime.Value = tt;

            cboStatus.SelectedItem = "Draft";
            tabControl.SelectedTab = tabChild;
        }

        /// <summary>
        /// Fill a lookup combo from a Document AI value: persist the value into its Master-File
        /// table (so it becomes a permanent option everywhere), remember it in the Learning
        /// Library, and select it in this combo now (SetLookup adds it if the list is stale).
        /// </summary>
        private void PrimeLookup(ComboBox combo, string masterTable, string libCategory, string value)
        {
            if (combo == null || string.IsNullOrWhiteSpace(value)) return;
            value = value.Trim();
            LookupStore.Ensure(masterTable, value);                       // permanent, dedup'd
            if (libCategory != null) LearningLibrary.Learn(libCategory, value);
            SetLookup(combo, value);                                       // visible + selected now
        }

        // Softcopy of the source certificate (the scanned image from Document AI), saved
        // with the record so the original can be reprinted later.
        private byte[] _scanImage;

        /// <summary>Attach the original scanned document to the next saved record.</summary>
        public void SetScanImage(byte[] bytes) => _scanImage = bytes;

        /// <summary>Persist the in-memory softcopy onto a births row (if one is loaded).</summary>
        private void SaveScan(long id)
        {
            if (_scanImage == null || _scanImage.Length == 0) return;
            Db.Push("UPDATE births SET scan_image = @img WHERE id = @id",
                new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = _scanImage },
                new MySqlParameter("@id", id));
        }

        // Master-File dropdowns that replace free-text lookup fields at runtime.
        private ComboBox _cboMCit, _cboMRel, _cboMOcc;
        private ComboBox _cboFCit, _cboFRel, _cboFOcc;
        private ComboBox _cboInfRel, _cboBirthOrder;
        private ComboBox[] _pob;   // Place of Birth:  hospital, municipality, province
        private ComboBox[] _pom;   // Marriage Place:  church,  municipality, province
        private ComboBox[] _mres;  // Mother Residence: house/st, barangay, municipality, province
        private ComboBox[] _fres;  // Father Residence: house/st, barangay, municipality, province
        private ComboBox[] _attAddr;  // Attendant Address: barangay, municipality, province
        private ComboBox[] _infAddr;  // Informant Address: barangay, municipality, province

        public BirthRegistrationForm()
        {
            InitializeComponent();
            LoadCombos();
            BuildLookups();
            dgvBirths.CellClick += dgvBirths_CellClick;
            LoadBirths();
            BuildSoftcopyButton();
            SetupCenteredLayout();
            WireLearningAutocomplete();
        }

        private Button btnViewScan;

        /// <summary>Adds a "View Softcopy" button beside the record actions.</summary>
        private void BuildSoftcopyButton()
        {
            btnViewScan = new Button
            {
                Text = "View Softcopy",
                Font = new System.Drawing.Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Size = new System.Drawing.Size(110, btnNew.Height),
                Top = btnNew.Top,
                Anchor = AnchorStyles.Top
            };
            btnViewScan.Click += btnViewScan_Click;
            btnNew.Parent.Controls.Add(btnViewScan);
            btnViewScan.BringToFront();
        }

        private void btnViewScan_Click(object sender, EventArgs e)
        {
            byte[] bytes = _scanImage;
            if (bytes == null && _editingId != null)
            {
                DataTable dt = Db.Pull("SELECT scan_image FROM births WHERE id = " + _editingId.Value);
                if (dt.Rows.Count > 0 && dt.Rows[0]["scan_image"] != DBNull.Value)
                    bytes = (byte[])dt.Rows[0]["scan_image"];
            }
            if (bytes == null)
            {
                MessageBox.Show("No softcopy is saved for this record. Open a record from the list, " +
                    "or use Document AI to attach a scanned copy.", "Softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(bytes, "Birth Certificate — Original Softcopy", this);
        }

        /// <summary>Give the free-text place/name boxes live suggestions from the shared
        /// Learning Library, and learn whatever the registrar types.</summary>
        private void WireLearningAutocomplete()
        {
            CROMS.Data.LearningLibrary.Attach(txtPlace, CROMS.Data.LearningLibrary.PlaceOfBirth);
            CROMS.Data.LearningLibrary.Attach(txtFirstName, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtLastName, CROMS.Data.LearningLibrary.Surname);
            CROMS.Data.LearningLibrary.Attach(txtFFirst, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtFLast, CROMS.Data.LearningLibrary.Surname);
            CROMS.Data.LearningLibrary.Attach(txtMFirst, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtMLast, CROMS.Data.LearningLibrary.Surname);
        }

        /// <summary>
        /// Keeps the Form-102 tab + records grid centered at a comfortable max width so the
        /// whitespace is balanced on both sides instead of piling up on the right (the Child
        /// tab is sparse). The fields inside the tabs are untouched — layout only.
        /// </summary>
        private void SetupCenteredLayout()
        {
            tabControl.Anchor = AnchorStyles.Top;                       // width/left controlled below
            dgvBirths.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;  // keep filling vertically
            lblRecent.Anchor = AnchorStyles.Top;
            btnNew.Anchor = AnchorStyles.Top;
            btnUpdate.Anchor = AnchorStyles.Top;
            btnDelete.Anchor = AnchorStyles.Top;
            Resize += (s, e) => CenterContent();
            CenterContent();
        }

        private void CenterContent()
        {
            const int margin = 24, maxW = 1280;
            int w = Math.Min(maxW, ClientSize.Width - margin * 2);
            if (w < 400) w = 400;
            int left = Math.Max(margin, (ClientSize.Width - w) / 2);
            int right = left + w;

            tabControl.Left = left; tabControl.Width = w;
            dgvBirths.Left = left; dgvBirths.Width = w;
            lblRecent.Left = left;

            // Right-align the record action buttons to the content's right edge.
            btnDelete.Left = right - btnDelete.Width;
            btnUpdate.Left = btnDelete.Left - 6 - btnUpdate.Width;
            btnNew.Left = btnUpdate.Left - 6 - btnNew.Width;
            if (btnViewScan != null) btnViewScan.Left = btnNew.Left - 6 - btnViewScan.Width;
        }

        /// <summary>
        /// Replaces the free-text lookup fields with selection-only comboboxes fed from
        /// the Master Files, so staff can only pick registered values. The comboboxes
        /// are laid over the original textboxes (which are hidden); values still save
        /// into the same database columns, so no schema change is needed.
        /// </summary>
        private void BuildLookups()
        {
            _cboMCit = Lookup(txtMCitizen, "nationalities");
            _cboMRel = Lookup(txtMReligion, "religions");
            _cboMOcc = Lookup(txtMOccupation, "occupations");
            _mres = LookupQuad(txtMResidence, "residences", "barangays", "municipalities", "provinces",
                "House / St.", "Barangay", "Municipality", "Province");
            _cboFCit = Lookup(txtFCitizen, "nationalities");
            _cboFRel = Lookup(txtFReligion, "religions");
            _cboFOcc = Lookup(txtFOccupation, "occupations");
            _fres = LookupQuad(txtFResidence, "residences", "barangays", "municipalities", "provinces",
                "House / St.", "Barangay", "Municipality", "Province");
            _cboInfRel = Lookup(txtInfRel, "relationships");
            _cboBirthOrder = Lookup(txtBirthOrder, "birth_orders");
            _pob = LookupTriple(txtPlace, "hospitals", "municipalities", "provinces",
                "Hospital / Clinic", "Municipality", "Province");
            _pom = LookupTriple(txtMarrPlace, "churches", "municipalities", "provinces",
                "Church", "Municipality", "Province");
            _attAddr = LookupTriple(txtAttAddress, "barangays", "municipalities", "provinces",
                "Barangay", "Municipality", "Province");
            _infAddr = LookupTriple(txtInfAddress, "barangays", "municipalities", "provinces",
                "Barangay", "Municipality", "Province");
        }

        /// <summary>Creates a pick-only combobox over a textbox and hides the textbox.</summary>
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

        /// <summary>Splits a textbox's width into three pick-only comboboxes, each with
        /// a small caption underneath so it's clear what to pick.</summary>
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

        /// <summary>Splits a textbox's width into four pick-only comboboxes with captions.</summary>
        private ComboBox[] LookupQuad(TextBox tb, string t1, string t2, string t3, string t4,
            string cap1, string cap2, string cap3, string cap4)
        {
            const int gap = 5;
            int w = (tb.Width - 3 * gap) / 4;
            var a = TripleCombo(tb, tb.Left, w, t1, cap1);
            var b = TripleCombo(tb, tb.Left + (w + gap), w, t2, cap2);
            var c = TripleCombo(tb, tb.Left + 2 * (w + gap), w, t3, cap3);
            var d = TripleCombo(tb, tb.Left + 3 * (w + gap), tb.Width - 3 * (w + gap), t4, cap4);
            tb.Visible = false;
            return new[] { a, b, c, d };
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

        // ---- read/write helpers for the lookup comboboxes ----
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
            if (idx < 0) { c.Items.Add(value); idx = c.Items.Count - 1; }   // keep legacy values visible
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
            cboTypeOfBirth.Items.AddRange(new object[] { "Single", "Twin", "Triplet", "Quadruplet" });
            cboAttType.Items.AddRange(new object[] { "Physician", "Nurse", "Midwife", "Hilot (Traditional)", "Others" });
            cboStatus.Items.AddRange(new object[] { "Draft", "Pending Approval", "Registered", "Delayed Posting" });
            cboStatus.SelectedItem = "Draft";
        }

        private void LoadBirths()
        {
            dgvBirths.DataSource = Db.Pull(
                "SELECT id, registry_no AS 'Registry No', " +
                "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Child, " +
                "sex AS Sex, date_of_birth AS DOB, book_volume AS Book, status AS Status " +
                "FROM births ORDER BY id DESC");
            if (dgvBirths.Columns.Contains("id")) dgvBirths.Columns["id"].Visible = false;
        }

        // ---------- CREATE ----------
        private void btnSaveDraft_Click(object sender, EventArgs e) => Create("Draft");

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!ValidateChild()) return;
            Create("Pending Approval");
        }

        private void Create(string status)
        {
            // Assign an official registry number the moment the record becomes more than
            // a draft (Submit / Registered), unless the registrar typed one already.
            // Drafts stay unnumbered — they are not yet official records.
            if (status != "Draft" && string.IsNullOrWhiteSpace(txtRegNo.Text))
                txtRegNo.Text = NextRegistryNo();

            const string sql =
                "INSERT INTO births (" + Columns + ") VALUES (" + ValuePlaceholders + ")";
            try
            {
                long newId = Db.Insert(sql, FieldParams(status));
                SaveScan(newId);   // attach the scanned softcopy, if this came from Document AI
                Audit.Write(Audit.Create, "births", newId,
                    txtLastName.Text.Trim() + ", " + txtFirstName.Text.Trim() + " (" + status + ")");

                string extra = "";
                // If this entry came from a queue ticket and is being submitted (not a
                // draft), park the ticket as Pending Approval and link it to this record.
                // It returns to the queue as "For Receiving" once the record is approved.
                if (status == "Pending Approval" && _queueTicketId > 0)
                {
                    Db.Push(
                        "UPDATE queue_tickets SET status = 'Pending Approval', birth_id = @bid, " +
                        "window_no = NULL WHERE id = @tid",
                        new MySqlParameter("@bid", newId),
                        new MySqlParameter("@tid", _queueTicketId));
                    extra = "\nThe queue ticket now waits for approval, then returns for releasing.";
                    _queueTicketId = 0;
                }

                MessageBox.Show(
                    (status == "Draft" ? "Saved as draft." : "Submitted for approval.") + extra,
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadBirths();
            }
            catch (Exception ex) { Fail(ex); }
        }

        // ---------- READ (row -> form) ----------
        private void dgvBirths_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(dgvBirths.Rows[e.RowIndex].Cells["id"].Value);
            LoadBirth(id);
        }

        private void LoadBirth(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM births WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;

            chkDelayed.Checked = ToInt(r["is_delayed"]) == 1;
            txtRegNo.Text = Str(r["registry_no"]);
            txtBook.Text = Str(r["book_volume"]);
            SetCombo(cboStatus, r["status"]);
            txtFirstName.Text = Str(r["first_name"]);
            txtMiddleName.Text = Str(r["middle_name"]);
            txtLastName.Text = Str(r["last_name"]);
            SetCombo(cboSex, r["sex"]);
            SetDate(dtpDob, r["date_of_birth"]);
            SetTime(dtpTime, r["time_of_birth"]);
            SplitToTriple(_pob, Str(r["place_of_birth"]));
            SetCombo(cboTypeOfBirth, r["type_of_birth"]);
            SetLookup(_cboBirthOrder, Str(r["birth_order"]));
            txtWeight.Text = Str(r["weight_grams"]);
            txtMFirst.Text = Str(r["mother_first_name"]);
            txtMMiddle.Text = Str(r["mother_middle_name"]);
            txtMLast.Text = Str(r["mother_last_name"]);
            SetLookup(_cboMCit, Str(r["mother_citizenship"]));
            SetLookup(_cboMRel, Str(r["mother_religion"]));
            SetLookup(_cboMOcc, Str(r["mother_occupation"]));
            txtMAge.Text = Str(r["mother_age"]);
            txtMBornAlive.Text = Str(r["mother_children_born_alive"]);
            txtMLiving.Text = Str(r["mother_children_living"]);
            txtMDead.Text = Str(r["mother_children_dead"]);
            SplitToTriple(_mres, Str(r["mother_residence"]));
            txtFFirst.Text = Str(r["father_first_name"]);
            txtFMiddle.Text = Str(r["father_middle_name"]);
            txtFLast.Text = Str(r["father_last_name"]);
            SetLookup(_cboFCit, Str(r["father_citizenship"]));
            SetLookup(_cboFRel, Str(r["father_religion"]));
            SetLookup(_cboFOcc, Str(r["father_occupation"]));
            txtFAge.Text = Str(r["father_age"]);
            SplitToTriple(_fres, Str(r["father_residence"]));
            SetOptionalDate(dtpMarrDate, r["parents_marriage_date"]);
            SplitToTriple(_pom, Str(r["parents_marriage_place"]));
            SetCombo(cboAttType, r["attendant_type"]);
            txtAttName.Text = Str(r["attendant_name"]);
            txtAttTitle.Text = Str(r["attendant_title"]);
            SplitToTriple(_attAddr, Str(r["attendant_address"]));
            txtInfName.Text = Str(r["informant_name"]);
            SetLookup(_cboInfRel, Str(r["informant_relationship"]));
            SplitToTriple(_infAddr, Str(r["informant_address"]));
            SetDate(dtpInfDate, r["informant_date"]);
            txtPreparedBy.Text = Str(r["prepared_by"]);
            txtReceivedBy.Text = Str(r["received_by"]);
            txtRemarks.Text = Str(r["remarks"]);
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
            if (!ValidateChild()) return;

            string status = cboStatus.SelectedItem?.ToString() ?? "Draft";
            // If this edit promotes a draft to an official status and it still has no
            // registry number, assign one now (same year-scoped sequence as create).
            if (status != "Draft" && string.IsNullOrWhiteSpace(txtRegNo.Text))
                txtRegNo.Text = NextRegistryNo();

            var ps = new List<MySqlParameter>(FieldParams(status))
            {
                new MySqlParameter("@id", _editingId.Value)
            };
            try
            {
                Db.Push("UPDATE births SET " + SetClause + " WHERE id = @id", ps.ToArray());
                SaveScan(_editingId.Value);   // keep/refresh the softcopy on edit
                Audit.Write(Audit.Update, "births", _editingId.Value,
                    txtLastName.Text.Trim() + ", " + txtFirstName.Text.Trim());
                MessageBox.Show("Record updated.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadBirths();
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
            if (MessageBox.Show("Delete this birth record?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM births WHERE id = @id",
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "births", _editingId.Value, null);
                MessageBox.Show("Record deleted.", "Deleted",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadBirths();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("This record is referenced elsewhere and can't be deleted.",
                    "In use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void btnNew_Click(object sender, EventArgs e) => ClearForm();

        // ---------- shared SQL fragments ----------
        private const string Columns =
            "is_delayed, registry_no, book_volume, status, first_name, middle_name, last_name, sex, " +
            "date_of_birth, time_of_birth, place_of_birth, type_of_birth, birth_order, weight_grams, " +
            "mother_first_name, mother_middle_name, mother_last_name, mother_citizenship, mother_religion, " +
            "mother_occupation, mother_age, mother_children_born_alive, mother_children_living, " +
            "mother_children_dead, mother_residence, father_first_name, father_middle_name, father_last_name, " +
            "father_citizenship, father_religion, father_occupation, father_age, father_residence, " +
            "parents_marriage_date, parents_marriage_place, attendant_type, attendant_name, attendant_title, " +
            "attendant_address, informant_name, informant_relationship, informant_address, informant_date, " +
            "prepared_by, received_by, remarks";

        private const string ValuePlaceholders =
            "@is_delayed, @registry_no, @book_volume, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, " +
            "@type, @order, @weight, @mfn, @mmn, @mln, @mcit, @mrel, @mocc, @mage, @mba, @mlv, @mdd, @mres, " +
            "@ffn, @fmn, @fln, @fcit, @frel, @focc, @fage, @fres, @pmdate, @pmplace, @atype, @aname, @atitle, " +
            "@aaddr, @iname, @irel, @iaddr, @idate, @prep, @recv, @remarks";

        private const string SetClause =
            "is_delayed=@is_delayed, registry_no=@registry_no, book_volume=@book_volume, status=@status, " +
            "first_name=@fn, middle_name=@mn, last_name=@ln, sex=@sex, date_of_birth=@dob, time_of_birth=@tob, " +
            "place_of_birth=@place, type_of_birth=@type, birth_order=@order, weight_grams=@weight, " +
            "mother_first_name=@mfn, mother_middle_name=@mmn, mother_last_name=@mln, mother_citizenship=@mcit, " +
            "mother_religion=@mrel, mother_occupation=@mocc, mother_age=@mage, mother_children_born_alive=@mba, " +
            "mother_children_living=@mlv, mother_children_dead=@mdd, mother_residence=@mres, " +
            "father_first_name=@ffn, father_middle_name=@fmn, father_last_name=@fln, father_citizenship=@fcit, " +
            "father_religion=@frel, father_occupation=@focc, father_age=@fage, father_residence=@fres, " +
            "parents_marriage_date=@pmdate, parents_marriage_place=@pmplace, attendant_type=@atype, " +
            "attendant_name=@aname, attendant_title=@atitle, attendant_address=@aaddr, informant_name=@iname, " +
            "informant_relationship=@irel, informant_address=@iaddr, informant_date=@idate, " +
            "prepared_by=@prep, received_by=@recv, remarks=@remarks";

        private MySqlParameter[] FieldParams(string status)
        {
            return new[]
            {
                new MySqlParameter("@is_delayed", chkDelayed.Checked ? 1 : 0),
                new MySqlParameter("@registry_no", S(txtRegNo)),
                new MySqlParameter("@book_volume", S(txtBook)),
                new MySqlParameter("@status", status),
                new MySqlParameter("@fn", txtFirstName.Text.Trim()),
                new MySqlParameter("@mn", S(txtMiddleName)),
                new MySqlParameter("@ln", txtLastName.Text.Trim()),
                new MySqlParameter("@sex", Combo(cboSex)),
                new MySqlParameter("@dob", dtpDob.Value.Date),
                new MySqlParameter("@tob", dtpTime.Value.ToString("HH:mm")),
                new MySqlParameter("@place", ComboJoin(_pob)),
                new MySqlParameter("@type", Combo(cboTypeOfBirth)),
                new MySqlParameter("@order", ComboVal(_cboBirthOrder)),
                new MySqlParameter("@weight", I(txtWeight)),
                new MySqlParameter("@mfn", S(txtMFirst)),
                new MySqlParameter("@mmn", S(txtMMiddle)),
                new MySqlParameter("@mln", S(txtMLast)),
                new MySqlParameter("@mcit", ComboVal(_cboMCit)),
                new MySqlParameter("@mrel", ComboVal(_cboMRel)),
                new MySqlParameter("@mocc", ComboVal(_cboMOcc)),
                new MySqlParameter("@mage", I(txtMAge)),
                new MySqlParameter("@mba", I(txtMBornAlive)),
                new MySqlParameter("@mlv", I(txtMLiving)),
                new MySqlParameter("@mdd", I(txtMDead)),
                new MySqlParameter("@mres", ComboJoin(_mres)),
                new MySqlParameter("@ffn", S(txtFFirst)),
                new MySqlParameter("@fmn", S(txtFMiddle)),
                new MySqlParameter("@fln", S(txtFLast)),
                new MySqlParameter("@fcit", ComboVal(_cboFCit)),
                new MySqlParameter("@frel", ComboVal(_cboFRel)),
                new MySqlParameter("@focc", ComboVal(_cboFOcc)),
                new MySqlParameter("@fage", I(txtFAge)),
                new MySqlParameter("@fres", ComboJoin(_fres)),
                new MySqlParameter("@pmdate", dtpMarrDate.Checked ? (object)dtpMarrDate.Value.Date : DBNull.Value),
                new MySqlParameter("@pmplace", ComboJoin(_pom)),
                new MySqlParameter("@atype", Combo(cboAttType)),
                new MySqlParameter("@aname", S(txtAttName)),
                new MySqlParameter("@atitle", S(txtAttTitle)),
                new MySqlParameter("@aaddr", ComboJoin(_attAddr)),
                new MySqlParameter("@iname", S(txtInfName)),
                new MySqlParameter("@irel", ComboVal(_cboInfRel)),
                new MySqlParameter("@iaddr", ComboJoin(_infAddr)),
                new MySqlParameter("@idate", dtpInfDate.Value.Date),
                new MySqlParameter("@prep", S(txtPreparedBy)),
                new MySqlParameter("@recv", S(txtReceivedBy)),
                new MySqlParameter("@remarks", S(txtRemarks)),
            };
        }

        /// <summary>
        /// Next birth registry number for the current year, as YYYY-B-####. Derived from
        /// the highest sequence already issued this year + 1 (not a row count), so a
        /// delete never causes a duplicate. Year-scoped: restarts each January.
        /// </summary>
        private static string NextRegistryNo()
        {
            int year = DateTime.Now.Year;
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM births WHERE registry_no LIKE @p",
                new MySqlParameter("@p", year + "-B-%"));
            int next = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value)
                ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-B-{1:D4}", year, next);
        }

        private bool ValidateChild()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                cboSex.SelectedItem == null)
            {
                MessageBox.Show("Child's first name, last name and sex are required.",
                    "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabChild;
                return false;
            }
            return true;
        }

        private void ClearForm()
        {
            _editingId = null;
            _scanImage = null;
            foreach (Control c in EnumerateInputs(this)) ClearControl(c);
            cboStatus.SelectedItem = "Draft";
            chkDelayed.Checked = false;
            dtpMarrDate.Checked = false;
        }

        private IEnumerable<Control> EnumerateInputs(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox || c is ComboBox) yield return c;
                foreach (Control child in EnumerateInputs(c)) yield return child;
            }
        }

        private static void ClearControl(Control c)
        {
            if (c is TextBox t) t.Clear();
            else if (c is ComboBox cb) cb.SelectedIndex = -1;
        }

        // ---------- helpers ----------
        private static object S(TextBox t) =>
            string.IsNullOrWhiteSpace(t.Text) ? (object)DBNull.Value : t.Text.Trim();

        private static object I(TextBox t) =>
            int.TryParse(t.Text, out int n) ? (object)n : DBNull.Value;

        private static object Combo(ComboBox c) =>
            c.SelectedItem == null ? (object)DBNull.Value : c.SelectedItem.ToString();

        private static string Str(object v) => v == DBNull.Value || v == null ? "" : v.ToString();
        private static int ToInt(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);

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
