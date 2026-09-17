// BirthRegistrationForm.cs - runtime logic, events, database operations, and dynamic UI helpers

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

        /// <summary>
        /// WHICH FORM this record is. A record typed in here is on the revision the office
        /// issues today; a record auto-filled from a scan is on whatever revision that
        /// scan was, which PrimeFromExtraction supplies. It is stored on the row because
        /// `births` is shared by every MF-102 revision — the revision cannot be recovered
        /// from the data, and a certificate cannot be reprinted in the right layout
        /// without it.
        /// </summary>
        private string _formCode = FormCatalog.Current(DocKind.Birth)?.FormCode;
        private string _formName = FormCatalog.Current(DocKind.Birth)?.FormName;
        private int _queueTicketId;   // set when a "New Registration" queue ticket is served here

        /// <summary>
        /// The date the loaded record was registered, or null for a new entry and for any
        /// record that predates the column. Held here so an edit keeps the record's own
        /// registration date instead of silently re-dating it to today, which would make a
        /// long-registered birth read as delayed the moment someone fixes a typo in it.
        /// </summary>
        private DateTime? _dateRegistered;

        /// <summary>
        /// Set while <see cref="RecomputeDelayed"/> is writing the checkbox, so its own
        /// change does not re-enter the recompute.
        /// </summary>
        private bool _suppressDelayedRecompute;

        // Step-by-step registration wizard
        private Button _btnAddAnotherBirth;
        private Button _btnDelayedCase;

        // Form-90-style wizard chrome: a numbered step strip above the tabs and an
        // "at a glance" summary rail beside them, built in code (like the rest of this
        // wizard) rather than the Designer - see InitializeWizardChrome / RefreshRail.
        private StepStrip _stepStrip;
        private Panel _wizardHost;
        private Panel _tabHost;
        private Panel _railPanel;

        // Cascading location lookup controls:
        // address = House/Street, Province, Municipality, Barangay
        // place of birth = Hospital/Clinic, Province, Municipality
        private ComboBox _mProvince, _mMunicipality, _mBarangay;
        private ComboBox _fProvince, _fMunicipality, _fBarangay;
        private ComboBox _attProvince, _attMunicipality, _attBarangay;
        private ComboBox _infProvince, _infMunicipality, _infBarangay;
        private ComboBox _pobProvince, _pobMunicipality;
        private ComboBox _pomProvince, _pomMunicipality;

        public void RefreshData()
        {
            _queueTicketId = 0;   // fresh view — drop any stale queue link
            LoadBirths();
            ShowListView();
        }

        /// <summary>
        /// Called by Queue Management when a "New Registration" ticket is opened here.
        /// Submitting for approval then parks that queue ticket as "Pending Approval";
        /// once the registrar approves the record it returns to the queue as
        /// "For Receiving" so the client can be called back for their copy.
        /// <para/>
        /// Also opens the full registration panel directly — the operator came here to
        /// register this walk-in client's birth, not to look at the list first.
        /// </summary>
        public void PrepareForQueueTicket(int ticketId)
        {
            _queueTicketId = ticketId;
            ShowEntryView();
        }

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

            // The scan's own form revision, when Intelligent Document Processing
            // identified one. Without this an auto-filled record would be filed under the
            // revision the office issues today, which may not be the sheet in hand.
            if (f.TryGetValue("FormCode", out string fc) && !string.IsNullOrWhiteSpace(fc))
                _formCode = fc.Trim();
            if (f.TryGetValue("FormName", out string fn) && !string.IsNullOrWhiteSpace(fn))
                _formName = fn.Trim();

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
            PrimeLookup(_pob[1], "provinces", LearningLibrary.Province, pp);
            GeoLookup.LoadMunicipalities(_pob[2], pp);
            PrimeLookup(_pob[2], "municipalities", LearningLibrary.Municipality, pm);

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


            // The certification blocks at the foot of the form - 19b/21b attendant,
            // 20/22 informant, 21/23 prepared by, 22/24 received, 25 registered. These are
            // read off the scan like every other field; a value the scan did not carry
            // stays blank for the operator to type.
            Set(txtAttName, "AttendantName");
            Set(txtAttTitle, "AttendantTitle");
            Set(txtInfName, "Informant");
            Set(txtPreparedBy, "PreparedByName");
            Set(txtPreparedTitle, "PreparedByTitle");
            Set(txtReceivedBy, "ReceivedByName");
            Set(txtReceivedTitle, "ReceivedByTitle");
            Set(txtRegisteredBy, "RegisteredByName");
            Set(txtRegisteredTitle, "RegisteredByTitle");

            OthersBox.Apply(_cboInfRel, txtInfRelOther, lblInfRelOther,
                Val("InformantRelationship"), SetLookup);
            PrimeAddress(_attAddr, Val("AttendantAddress"));
            PrimeAddress(_infAddr, Val("InformantAddress"));

            OthersBox.Apply(cboAttType, txtAttTypeOther, lblAttTypeOther,
                Val("Attendant"), (c, v) => c.Text = v);

            // A scan that produced neither a marriage date nor a place has NOT told us
            // the parents are unmarried - it has told us it could not read item 18. The
            // toggle therefore stays on its default and the operator answers it; guessing
            // "not married" from a failed read would put illegitimacy on the record.
            PrimeDate(dtpAttDate, Val("AttendantDate"));
            PrimeDate(dtpInfDate, Val("InformantDate"));
            PrimeDate(dtpPreparedDate, Val("PreparedByDate"));
            PrimeDate(dtpReceivedDate, Val("ReceivedByDate"));
            PrimeDate(dtpRegisteredDate, Val("RegisteredByDate"));

            cboStatus.SelectedItem = "Draft";
            tabControl.SelectedTab = tabChild;
            ShowEntryView();   // the auto-filled record needs to be reviewed, not left in the list
        }

        /// <summary>
        /// Fill an address triple (province / municipality / barangay) from one scanned
        /// place string. The cells CASCADE, so they must be filled parent-first and the
        /// child list reloaded in between - setting the barangay before its municipality
        /// is chosen would select into a list that is still empty.
        /// </summary>
        private void PrimeAddress(ComboBox[] cells, string value)
        {
            if (cells == null || cells.Length < 3 || string.IsNullOrWhiteSpace(value)) return;
            string[] parts = value.Split(',');
            string province = parts.Length > 0 ? parts[0].Trim() : "";
            string municipality = parts.Length > 1 ? parts[1].Trim() : "";
            string barangay = parts.Length > 2 ? parts[2].Trim() : "";

            PrimeLookup(cells[0], "provinces", LearningLibrary.Province, province);
            GeoLookup.LoadMunicipalities(cells[1], province);
            PrimeLookup(cells[1], "municipalities", LearningLibrary.Municipality, municipality);
            GeoLookup.LoadBarangays(cells[2], province, municipality);
            PrimeLookup(cells[2], "barangays", LearningLibrary.Barangay, barangay);
        }

        /// <summary>
        /// Set an optional date picker from a scanned value.
        /// <para/>
        /// yyyy-MM-dd only: the extractor emits that form only when a named month made the
        /// order certain. A numeric reading like "2-6-2018" is ambiguous, and ticking the
        /// box on a guess would put a date on the record that nobody read off the paper. It
        /// stays unticked for the operator to enter.
        /// </summary>
        private static void PrimeDate(DateTimePicker dtp, string value)
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd",
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None, out DateTime d))
            { dtp.Value = d; dtp.Checked = true; }
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

        public BirthRegistrationForm()
        {
            InitializeComponent();
            dtpInfDate.ShowCheckBox = true;
            dtpInfDate.Checked = false;

            // Runtime-only UI setup. Keep dynamic control creation and resizing out of
            // InitializeComponent so the WinForms Designer does not try to invoke them.
            InitializeLookupControls();
            WireGeography();
            InitializeParentsMarried();
            // Every dropdown that still offers "Others" gets its specify box. Without one
            // the record says "Others" and what the paper actually says is lost.
            OthersBox.Bind(cboAttType, txtAttTypeOther, lblAttTypeOther);
            OthersBox.Bind(_cboInfRel, txtInfRelOther, lblInfRelOther);
            InitializeAddAnotherBirthButton();
            InitializeDelayedCaseButton();
            InitializeWizardChrome();
            chkDelayed.CheckedChanged += (s, e) => RefreshDelayedCaseButton();

            this.Resize += new EventHandler(this.BirthRegistrationForm_Resize);
            CenterContent();
            UpdateStepNavigation();
            ShowListView();

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;

            LoadLookupData();
            LoadBirths();
            WireLearningAutocomplete();

            // Subscribe after initialization and data loading so the initial values
            // do not trigger delayed-registration calculations during construction.
            dtpDob.ValueChanged += dtpDob_ValueChanged;
            RecomputeDelayed();
        }

        // ---------- list view / full registration panel ----------
        //
        // The screen shows ONE of two things at a time: the list of recent registrations
        // (cardRecords), or the full Municipal Form 102 entry panel (cardForm) — never both
        // at once, so the grid and the record being worked on don't compete for space.
        // Which card is visible is driven by collapsing the OTHER card's row in layoutMain
        // to zero height, the same RowStyle-swap technique CenterContent already uses on
        // the column dimension.

        /// <summary>Shows the records list; hides the full registration panel.</summary>
        private void ShowListView()
        {
            cardRecords.Visible = true;
            cardForm.Visible = false;
            layoutMain.RowStyles[1].SizeType = SizeType.Absolute;
            layoutMain.RowStyles[1].Height = 0F;
            layoutMain.RowStyles[2].SizeType = SizeType.Percent;
            layoutMain.RowStyles[2].Height = 100F;

            btnSaveDraft.Visible = false;
            btnSubmit.Visible = false;
            btnOCRLiveBirth.Visible = false;
            btnBackToList.Visible = false;
            chkDelayed.Visible = false;
            lblSubtitle.Text = "Search recent registrations, or start a new one.";
        }

        /// <summary>Shows the full registration panel; hides the records list.</summary>
        private void ShowEntryView()
        {
            cardForm.Visible = true;
            cardRecords.Visible = false;
            layoutMain.RowStyles[1].SizeType = SizeType.Percent;
            layoutMain.RowStyles[1].Height = 100F;
            layoutMain.RowStyles[2].SizeType = SizeType.Absolute;
            layoutMain.RowStyles[2].Height = 0F;

            btnSaveDraft.Visible = true;
            btnSubmit.Visible = true;
            btnOCRLiveBirth.Visible = true;
            btnBackToList.Visible = true;
            chkDelayed.Visible = true;
            lblSubtitle.Text = "MUNICIPAL FORM 102  •  NEW & DELAYED REGISTRATION";

            // Syncs the step strip / at-a-glance rail to whatever tab and record are
            // showing - matters whether we arrived here fresh (New Registration) or with
            // a record just loaded (Open Record / a queue ticket / an OCR auto-fill).
            UpdateStepNavigation();
        }

        private void btnNewRegistration_Click(object sender, EventArgs e)
        {
            ClearForm();
            ShowEntryView();
            tabControl.SelectedTab = tabChild;
            txtFirstName.Focus();
        }

        private void btnOpenRecord_Click(object sender, EventArgs e)
        {
            if (dgvBirths.CurrentRow == null)
            {
                MessageBox.Show("Click a record in the list first.", "Open Record",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id = Convert.ToInt32(dgvBirths.CurrentRow.Cells["id"].Value);
            LoadBirth(id);
            ShowEntryView();
        }

        private void btnBackToList_Click(object sender, EventArgs e)
        {
            ShowListView();
            LoadBirths();
        }

        /// <summary>
        /// Print Certificate and View Softcopy are two things done with the SAME saved
        /// record, so they are one control: printing on the main half, the softcopy under
        /// the chevron. Side by side they read as two unrelated choices and cost twice the
        /// width; printing is what the operator is nearly always here for, so it keeps the
        /// label and the softcopy moves one click away rather than out of reach.
        /// </summary>
        private void certificateMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mnuViewSoftcopy.Enabled = _scanImage != null || _editingId != null;
            mnuFactsCert.Enabled = _editingId != null;
        }

        /// <summary>Civil Registry Form No. 1A - CERTIFICATION (Birth Available), the birth
        /// counterpart of the marriage desk's Form 3A: a "TO WHOM IT MAY CONCERN" letter
        /// certifying facts already in the Register of Births - not a copy of the Certificate of
        /// Live Birth. Like Print Certificate, this is a saved-record action; if opened with no
        /// record selected the screen still opens and lets the operator search for one.</summary>
        private void mnuFactsCert_Click(object sender, EventArgs e)
        {
            using (Form3BCertForm f = _editingId != null ? new Form3BCertForm(_editingId.Value) : new Form3BCertForm())
                f.ShowDialog(this);
        }

        /// <summary>Opens Intelligent Document Processing so a scanned certificate can be read and auto-filled into this form.</summary>
        private void btnOCRLiveBirth_Click(object sender, EventArgs e)
        {
            try
            {
                MainForm shell = null;
                for (Control parent = Parent; parent != null; parent = parent.Parent)
                {
                    shell = parent as MainForm;
                    if (shell != null) break;
                }
                if (shell == null) shell = Owner as MainForm;

                if (shell != null)
                {
                    // The registered "ocr" module is OcrDigitizationForm. Reuse it so
                    // extracted birth fields can return through the existing workflow.
                    shell.GoToModule("ocr");
                    return;
                }

                // Also support opening Birth Registration as a standalone form.
                using (OcrDigitizationForm ocr = CreateStandaloneOcrWindow())
                    ocr.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
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
                if (_editingId != null)
                {
                    // No scanned original was ever attached — fall back to the official
                    // Municipal Form 102 blank already in Assets, filled in from the saved
                    // record, so "view the softcopy" always shows something usable instead
                    // of a dead end.
                    CROMS.Data.CertificateReport.Show(_formCode, _editingId.Value, this);
                    return;
                }
                MessageBox.Show("No softcopy is saved for this record. Open a record from the list, " +
                    "or use Document AI to attach a scanned copy.", "Softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(bytes, "Birth Certificate — Original Softcopy", this);
        }

        private void btnPrintCert_Click(object sender, EventArgs e)
        {
            // A certificate is printed from the REGISTRY ENTRY, not from the boxes on
            // screen. That is not bureaucracy for its own sake: an unsaved form has no
            // registry number, no audit row and no record anyone can look up, so a
            // certificate printed from it would be an official-looking document the
            // registry cannot account for.
            //
            // But the operator should never be sent to hunt for the record in the list —
            // especially right after Intelligent Document Processing filled this form from
            // a scan, where the record does not exist yet at all. So the missing step is
            // OFFERED instead of just reported.
            if (_editingId == null)
            {
                if (!SaveThenPrint()) return;
            }

            // One reusable report path for every form: Crystal when a .rpt exists for
            // this revision, otherwise CROMS's own replica/structured renderer. The old
            // BirthCertificatePrinter's measured coordinates now live in FormCatalog as
            // this form's print map, so the replica is unchanged — it is just no longer
            // hardcoded to Municipal Form 102.
            CROMS.Data.CertificateReport.Show(_formCode, _editingId.Value, this);
        }

        /// <summary>
        /// Register what is on the form and stay on it, so the certificate can be printed
        /// in the same click. Returns true when a record now exists to print.
        /// <para/>
        /// The record is saved with the status the operator has already chosen on the form
        /// rather than a status invented here — pressing Print must not silently promote a
        /// draft into a registered birth. A Draft has no registry number by design, so the
        /// prompt says so before printing a certificate with that line blank.
        /// </summary>
        private bool SaveThenPrint()
        {
            if (!ValidateChild()) return false;

            string status = cboStatus.SelectedItem?.ToString() ?? "Draft";
            bool draft = status == "Draft";

            string message =
                "This record has not been saved yet, and a certificate is printed from the " +
                "saved registry entry — not from the form on screen.\n\n" +
                "Save it now as \"" + status + "\" and print the certificate?\n\n" +
                (draft
                    ? "Note: a Draft is not yet an official record, so the certificate will " +
                      "print with no registry number. Choose \"Registered\" or \"Pending " +
                      "Approval\" in the Status box first if it should have one."
                    : "A registry number will be assigned if the record does not have one.");

            if (MessageBox.Show(message, "Save and print certificate",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return false;

            long? id = Create(status, keepOpen: true);
            // LoadBirth in Create sets _editingId; verify rather than assume, so a failed
            // save can never fall through into printing a certificate for nothing.
            return id.HasValue && _editingId != null;
        }

        /// <summary>Give the free-text place/name boxes live suggestions from the shared
        /// Learning Library, and learn whatever the registrar types.</summary>
        private void WireLearningAutocomplete()
        {
            // txtPlace is the hidden placeholder CreateLookupCells left behind — it is
            // replaced on screen by the 3 Place of Birth comboboxes (_pob), so attaching here
            // could neither suggest nor learn anything. The real input is the hospital cell.
            CROMS.Data.LearningLibrary.Attach(_pob[0], CROMS.Data.LearningLibrary.PlaceOfBirth);
            CROMS.Data.LearningLibrary.Attach(txtFirstName, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtLastName, CROMS.Data.LearningLibrary.Surname);
            CROMS.Data.LearningLibrary.Attach(txtFFirst, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtFLast, CROMS.Data.LearningLibrary.Surname);
            CROMS.Data.LearningLibrary.Attach(txtMFirst, CROMS.Data.LearningLibrary.GivenName);
            CROMS.Data.LearningLibrary.Attach(txtMLast, CROMS.Data.LearningLibrary.Surname);
        }

        /// <summary>
        /// Caps the content column at a comfortable reading width and lets the two gutter
        /// columns share whatever is left, so on a wide monitor the whitespace is balanced on
        /// both sides instead of stretching a sparse tab across the screen.
        /// <para/>
        /// Everything INSIDE the column is laid out by nested TableLayoutPanels, so this is the
        /// only measurement the form makes: one number, applied to one column style.
        /// </summary>
        private void BirthRegistrationForm_Resize(object sender, EventArgs e)
        {
            CenterContent();
        }

        /// <summary>Loads the Master File choices into the designed lookup controls.</summary>
        private void LoadLookupData()
        {
            foreach (var field in LookupFields())
                FillLookup(field.Key, field.Value);
            LoadRelationships();
        }

        /// <summary>
        /// "Relationship to the Child" - only the entries that belong on a birth
        /// certificate. `relationships` is shared with Municipal Form 103, so an unfiltered
        /// read offers a newborn's informant "Wife", "Widower" and "Funeral Director"
        /// (BR-16). Migration 32 marks each entry with the form it belongs on.
        /// </summary>
        private void LoadRelationships()
        {
            if (_cboInfRel == null) return;
            _cboInfRel.Items.Clear();
            _cboInfRel.Items.Add("");
            try
            {
                using (DataTable dt = Db.Pull(
                    "SELECT name FROM relationships WHERE applies_to IN ('Birth','Both') ORDER BY name"))
                    foreach (DataRow r in dt.Rows) _cboInfRel.Items.Add(r["name"].ToString());
            }
            catch { }
        }

        private IEnumerable<KeyValuePair<ComboBox, string>> LookupFields()
        {
            yield return new KeyValuePair<ComboBox, string>(_cboMCit, "nationalities");
            yield return new KeyValuePair<ComboBox, string>(_cboMRel, "religions");
            yield return new KeyValuePair<ComboBox, string>(_cboMOcc, "occupations");
            yield return new KeyValuePair<ComboBox, string>(_mres[0], "residences");
            yield return new KeyValuePair<ComboBox, string>(_cboFCit, "nationalities");
            yield return new KeyValuePair<ComboBox, string>(_cboFRel, "religions");
            yield return new KeyValuePair<ComboBox, string>(_cboFOcc, "occupations");
            yield return new KeyValuePair<ComboBox, string>(_fres[0], "residences");
            // Filled by LoadRelationships instead: this list is scoped to the entries that
            // belong on Form 102, which a whole-table read cannot express.

            yield return new KeyValuePair<ComboBox, string>(_cboBirthOrder, "birth_orders");
            yield return new KeyValuePair<ComboBox, string>(_pob[0], "hospitals");
            yield return new KeyValuePair<ComboBox, string>(_pom[0], "churches");
        }

        // Called on submission, never from typing or autocomplete events.
        private void SaveTypedLookupValues()
        {
            var fields = new List<KeyValuePair<ComboBox, string>>(LookupFields());
            var typed = new Dictionary<ComboBox, string>();
            var namesByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields)
                typed[field.Key] = (field.Key.Text ?? "").Trim();

            foreach (var field in fields)
            {
                string value = typed[field.Key];
                if (value.Length == 0) continue;

                List<string> names;
                if (!namesByTable.TryGetValue(field.Value, out names))
                {
                    names = ReadLookupNames(field.Value);
                    namesByTable.Add(field.Value, names);
                }

                string canonical = MatchingLookupName(names, value);
                if (canonical == null)
                {
                    // Existing application helper inserts only missing, normalized names.
                    if (LookupStore.Ensure(field.Value, value))
                    {
                        names.Add(value);
                        canonical = value;
                    }
                    else
                    {
                        // Ensure returns false for both duplicates and failures. Confirm
                        // a concurrent insert exists before reporting a successful save.
                        names = ReadLookupNames(field.Value);
                        namesByTable[field.Value] = names;
                        canonical = MatchingLookupName(names, value);
                        if (canonical == null)
                            throw new InvalidOperationException(
                                "Could not save '" + value + "' to " + field.Value +
                                ". The birth record has not been submitted. Please try again.");
                    }
                }
                typed[field.Key] = canonical;
            }

            // Make the saved choices available in every field sharing that Master File,
            // while preserving each field's own entered value.
            foreach (var field in fields)
            {
                List<string> names;
                if (namesByTable.TryGetValue(field.Value, out names))
                    foreach (string name in names)
                        if (field.Key.FindStringExact(name) < 0) field.Key.Items.Add(name);
                SetLookup(field.Key, typed[field.Key]);
            }
        }

        private static List<string> ReadLookupNames(string masterTable)
        {
            // Table names come only from the fixed LookupFields mapping above.
            var names = new List<string>();
            using (DataTable rows = Db.Pull("SELECT name FROM " + masterTable + " ORDER BY name"))
                foreach (DataRow row in rows.Rows)
                    if (row["name"] != DBNull.Value) names.Add(row["name"].ToString());
            return names;
        }

        private static string MatchingLookupName(IEnumerable<string> names, string value)
        {
            string normalized = LearningLibrary.Normalize(value);
            foreach (string name in names)
                if (string.Equals(LearningLibrary.Normalize(name), normalized,
                    StringComparison.OrdinalIgnoreCase)) return name;
            return null;
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
            string v = (c.Text ?? "").Trim();
            return string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v;
        }

        private static object ComboJoin(ComboBox[] parts)
        {
            var vals = new List<string>();
            foreach (var c in parts)
            {
                string v = (c.Text ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(v)) vals.Add(v);
            }
            return vals.Count == 0 ? (object)DBNull.Value : string.Join(", ", vals);
        }

        private static void SetLookup(ComboBox c, string value)
        {
            if (string.IsNullOrEmpty(value)) { c.SelectedIndex = c.Items.Count > 0 ? 0 : -1; c.Text = ""; return; }
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

        private void LoadBirths()
        {
            DataTable dt = Db.Pull(
                "SELECT id, registry_no AS 'Registry No', " +
                "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Child, " +
                "sex AS Sex, date_of_birth AS DOB, book_volume AS Book, book_page AS Page, status AS Status " +
                "FROM births ORDER BY id DESC");
            dgvBirths.DataSource = dt;
            if (dgvBirths.Columns.Contains("id")) dgvBirths.Columns["id"].Visible = false;
            ApplySearchFilter();
        }

        /// <summary>
        /// Filters the already-loaded grid by Registry No / Child name / Sex / Status —
        /// client-side over the bound DataTable's DataView, so typing never re-queries the
        /// database. Column names in the filter are the SQL aliases from LoadBirths.
        /// <para/>
        /// Every column name is BRACKETED, including ones that look harmless: `Child` and
        /// `Parent` are reserved words in DataColumn expression syntax (they address a
        /// DataRelation, as in Child.Column), so an unbracketed `Child` throws
        /// SyntaxErrorException rather than matching the column of that name.
        /// </summary>
        private void ApplySearchFilter()
        {
            if (!(dgvBirths.DataSource is DataTable dt)) return;

            string q = EscapeFilterValue((txtSearch?.Text ?? "").Trim());
            if (q.Length == 0) { dt.DefaultView.RowFilter = ""; return; }

            var parts = new List<string>();
            foreach (string col in new[] { "Registry No", "Child", "Sex", "Status" })
                if (dt.Columns.Contains(col))
                    parts.Add("[" + col + "] LIKE '%" + q + "%'");

            dt.DefaultView.RowFilter = parts.Count == 0 ? "" : string.Join(" OR ", parts);
        }

        /// <summary>
        /// Makes typed text safe inside a DataView RowFilter LIKE pattern. A quote has to be
        /// doubled, and the wildcard characters have to be wrapped in brackets so that typing
        /// one searches for that literal character instead of silently widening the match.
        /// </summary>
        private static string EscapeFilterValue(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\'': sb.Append("''"); break;
                    case '[': sb.Append("[[]"); break;
                    case '%': sb.Append("[%]"); break;
                    case '*': sb.Append("[*]"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e) => ApplySearchFilter();

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        // ---------- CREATE ----------
        private void btnSaveDraft_Click(object sender, EventArgs e) => Create("Draft");

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!ValidateChild()) return;
            Create("Pending Approval");
        }

        /// <summary>
        /// Write a new record.
        /// <para/>
        /// <paramref name="keepOpen"/> changes what happens afterwards: normally the form
        /// is cleared for the next entry, but a caller that needs to act on the record it
        /// just created — Print Certificate, which has to print from the saved registry
        /// entry — reloads it instead, so the form stays on that record and
        /// <c>_editingId</c> is set. Returns the new id, or null if nothing was written.
        /// </summary>
        private long? Create(string status, bool keepOpen = false)
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
                if (status == "Pending Approval") SaveTypedLookupValues();
                long newId = InsertTakingNextFreeNumber(sql, status);
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
                        "UPDATE queue_tickets SET birth_id = @bid WHERE id = @tid",
                        new MySqlParameter("@bid", newId),
                        new MySqlParameter("@tid", _queueTicketId));
                    extra = "\nThe queue ticket now waits for approval, then returns for releasing.";
                    _queueTicketId = 0;
                }

                if (!keepOpen)
                {
                    MessageBox.Show(
                        (status == "Draft" ? "Saved as draft." : "Submitted for approval.") + extra,
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    ShowListView();
                }
                LoadBirths();
                // Reload from the row that was actually written, so what the operator now
                // sees (and prints) is the registry entry, including the registry number
                // assigned during the save.
                if (keepOpen) LoadBirth((int)newId);
                return newId;
            }
            catch (Exception ex) { Fail(ex); }
            return null;
        }

        /// <summary>
        /// Inserts the record, and if another workstation claimed the same registry number
        /// in the meantime, takes the next one and tries again.
        ///
        /// <para>The number is read (MAX+1) in one statement and written in another, so two
        /// registrars saving in the same moment can compute the same number. Since migration
        /// 27 <c>registry_no</c> is UNIQUE, so the loser now gets error 1062 instead of
        /// writing a duplicate — the constraint prevents the corruption and this loop turns
        /// that refusal back into a correct save. Neither half works alone: without the
        /// constraint two records share the key, without the retry the second registrar sees
        /// a crash.</para>
        ///
        /// <para>Only a clash on the registry-number index is retried
        /// (<see cref="RegistryNumber.WasTaken"/>) — any other duplicate-key error is a real
        /// fault and is left to surface.</para>
        /// </summary>
        private long InsertTakingNextFreeNumber(string sql, string status)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return Db.Insert(sql, FieldParams(status));
                }
                catch (MySqlException ex)
                    when (RegistryNumber.WasTaken(ex, "births") && attempt < RegistryNumber.MaxRetries)
                {
                    txtRegNo.Text = NextRegistryNo();
                }
            }
        }

        // ---------- READ (row -> form) ----------
        private void dgvBirths_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(dgvBirths.Rows[e.RowIndex].Cells["id"].Value);
            LoadBirth(id);
            ShowEntryView();
        }

        private void LoadBirth(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM births WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            // The row's own form identity, so an edit or a reprint keeps the revision the
            // record was registered on rather than silently migrating it to today's.
            if (dt.Columns.Contains("form_code") && dt.Rows[0]["form_code"] != DBNull.Value)
                _formCode = dt.Rows[0]["form_code"].ToString();
            if (dt.Columns.Contains("form_name") && dt.Rows[0]["form_name"] != DBNull.Value)
                _formName = dt.Rows[0]["form_name"].ToString();
            DataRow r = dt.Rows[0];
            _editingId = id;

            // The record's OWN registration date, so editing it later does not re-date the
            // registration to today (which would turn a timely birth into a delayed one).
            _dateRegistered = dt.Columns.Contains("date_registered") &&
                              r["date_registered"] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(r["date_registered"])
                : null;

            // The registrar's own determination is shown as stored, not recomputed — they
            // may have overridden it, and the affidavits are filed on their decision.
            // Suppression is held across the WHOLE load, not just this line: setting
            // dtpDob further down raises ValueChanged, which would otherwise recompute the
            // flag and overwrite what was actually recorded.
            _suppressDelayedRecompute = true;
            chkDelayed.Checked = ToInt(r["is_delayed"]) == 1;
            txtRegNo.Text = Str(r["registry_no"]);
            txtBook.Text = Str(r["book_volume"]);
            txtBookPage.Text = Str(r["book_page"]);
            SetCombo(cboStatus, r["status"]);
            txtFirstName.Text = Str(r["first_name"]);
            txtMiddleName.Text = Str(r["middle_name"]);
            txtLastName.Text = Str(r["last_name"]);
            SetCombo(cboSex, r["sex"]);
            SetDate(dtpDob, r["date_of_birth"]);

            // Country first: it rebuilds the province list the place cells select into.
            GeoLookup.Select(_pobCountry, Str(r["birth_country"]));
            SetPlace3(_pob, Str(r["place_of_birth"]));
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
            SetResidence(_mres, Str(r["mother_residence"]));
            txtFFirst.Text = Str(r["father_first_name"]);
            txtFMiddle.Text = Str(r["father_middle_name"]);
            txtFLast.Text = Str(r["father_last_name"]);
            SetLookup(_cboFCit, Str(r["father_citizenship"]));
            SetLookup(_cboFRel, Str(r["father_religion"]));
            SetLookup(_cboFOcc, Str(r["father_occupation"]));
            txtFAge.Text = Str(r["father_age"]);
            SetResidence(_fres, Str(r["father_residence"]));
            // NULL means the record predates the column - "not stated", not "not
            // married" - so it shows as the default ON with its fields live, exactly as it
            // did before. Only a stored 0 greys them out.
            tglParentsMarried.SetCheckedSilently(
                !dt.Columns.Contains("parents_married") ||
                r["parents_married"] == DBNull.Value ||
                ToInt(r["parents_married"]) == 1);
            SetOptionalDate(dtpMarrDate, r["parents_marriage_date"]);
            SetPlace3(_pom, Str(r["parents_marriage_place"]));
            ApplyParentsMarried();
            OthersBox.Apply(cboAttType, txtAttTypeOther, lblAttTypeOther,
                Str(r["attendant_type"]), (c, v) => c.Text = v);
            txtAttName.Text = Str(r["attendant_name"]);
            txtAttTitle.Text = Str(r["attendant_title"]);
            SetAddress3(_attAddr, Str(r["attendant_address"]));
            SetOptionalDate(dtpAttDate, r["attendant_date"]);
            txtInfName.Text = Str(r["informant_name"]);
            OthersBox.Apply(_cboInfRel, txtInfRelOther, lblInfRelOther,
                Str(r["informant_relationship"]), SetLookup);
            SetAddress3(_infAddr, Str(r["informant_address"]));
            SetOptionalDate(dtpInfDate, r["informant_date"]);
            txtPreparedBy.Text = Str(r["prepared_by"]);
            txtPreparedTitle.Text = Str(r["prepared_by_title"]);
            SetOptionalDate(dtpPreparedDate, r["prepared_by_date"]);
            txtReceivedBy.Text = Str(r["received_by"]);
            txtReceivedTitle.Text = Str(r["received_by_title"]);
            SetOptionalDate(dtpReceivedDate, r["received_by_date"]);
            txtRegisteredBy.Text = Str(r["registered_by"]);
            txtRegisteredTitle.Text = Str(r["registered_by_title"]);
            SetOptionalDate(dtpRegisteredDate, r["registered_by_date"]);
            txtRemarks.Text = Str(r["remarks"]);
            _scanImage = dt.Columns.Contains("scan_image") && r["scan_image"] != DBNull.Value
                ? (byte[])r["scan_image"] : null;

            // Release the suppression taken above and restate the arithmetic on the label,
            // leaving the stored determination itself untouched.
            _suppressDelayedRecompute = false;
            UpdateDelayedLabel();
            RefreshDelayedCaseButton();
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

            try
            {
                if (status == "Pending Approval") SaveTypedLookupValues();
                // Same collision as on create: promoting a draft assigns a number that
                // another workstation may have taken between the read and this write.
                for (int attempt = 0; ; attempt++)
                {
                    var ps = new List<MySqlParameter>(FieldParams(status))
                    {
                        new MySqlParameter("@id", _editingId.Value)
                    };
                    try
                    {
                        Db.Push("UPDATE births SET " + SetClause + " WHERE id = @id", ps.ToArray());
                        break;
                    }
                    catch (MySqlException ex)
                        when (RegistryNumber.WasTaken(ex, "births") && attempt < RegistryNumber.MaxRetries)
                    {
                        txtRegNo.Text = NextRegistryNo();
                    }
                }
                SaveScan(_editingId.Value);   // keep/refresh the softcopy on edit
                Audit.Write(Audit.Update, "births", _editingId.Value,
                    txtLastName.Text.Trim() + ", " + txtFirstName.Text.Trim());
                MessageBox.Show("Record updated.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                ShowListView();
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
                ShowListView();
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
            "form_code, form_name, is_delayed, date_registered, registry_no, book_volume, book_page, status, first_name, middle_name, last_name, sex, " +
            "date_of_birth, place_of_birth, birth_country, type_of_birth, birth_order, weight_grams, " +
            "mother_first_name, mother_middle_name, mother_last_name, mother_citizenship, mother_religion, " +
            "mother_occupation, mother_age, mother_children_born_alive, mother_children_living, " +
            "mother_children_dead, mother_residence, father_first_name, father_middle_name, father_last_name, " +
            "father_citizenship, father_religion, father_occupation, father_age, father_residence, " +
            "parents_marriage_date, parents_marriage_place, parents_married, attendant_type, attendant_name, attendant_title, " +
            "attendant_address, attendant_date, informant_name, informant_relationship, informant_address, informant_date, " +
            "prepared_by, prepared_by_title, prepared_by_date, " +
            "received_by, received_by_title, received_by_date, " +
            "registered_by, registered_by_title, registered_by_date, remarks";

        private const string ValuePlaceholders =
            "@form_code, @form_name, @is_delayed, @date_registered, @registry_no, @book_volume, @book_page, @status, @fn, @mn, @ln, @sex, @dob, @place, @country, " +
            "@type, @order, @weight, @mfn, @mmn, @mln, @mcit, @mrel, @mocc, @mage, @mba, @mlv, @mdd, @mres, " +
            "@ffn, @fmn, @fln, @fcit, @frel, @focc, @fage, @fres, @pmdate, @pmplace, @pmarried, @atype, @aname, @atitle, " +
            "@aaddr, @adate, @iname, @irel, @iaddr, @idate, " +
            "@prep, @preptitle, @prepdate, @recv, @recvtitle, @recvdate, " +
            "@regby, @regbytitle, @regbydate, @remarks";

        private const string SetClause =
            "form_code=@form_code, form_name=@form_name, is_delayed=@is_delayed, date_registered=@date_registered, registry_no=@registry_no, book_volume=@book_volume, book_page=@book_page, status=@status, " +
            "first_name=@fn, middle_name=@mn, last_name=@ln, sex=@sex, date_of_birth=@dob, " +
            "place_of_birth=@place, birth_country=@country, type_of_birth=@type, birth_order=@order, weight_grams=@weight, " +
            "mother_first_name=@mfn, mother_middle_name=@mmn, mother_last_name=@mln, mother_citizenship=@mcit, " +
            "mother_religion=@mrel, mother_occupation=@mocc, mother_age=@mage, mother_children_born_alive=@mba, " +
            "mother_children_living=@mlv, mother_children_dead=@mdd, mother_residence=@mres, " +
            "father_first_name=@ffn, father_middle_name=@fmn, father_last_name=@fln, father_citizenship=@fcit, " +
            "father_religion=@frel, father_occupation=@focc, father_age=@fage, father_residence=@fres, " +
            "parents_marriage_date=@pmdate, parents_marriage_place=@pmplace, " +
            "parents_married=@pmarried, attendant_type=@atype, " +
            "attendant_name=@aname, attendant_title=@atitle, attendant_address=@aaddr, " +
            "attendant_date=@adate, informant_name=@iname, " +
            "informant_relationship=@irel, informant_address=@iaddr, informant_date=@idate, " +
            "prepared_by=@prep, prepared_by_title=@preptitle, prepared_by_date=@prepdate, " +
            "received_by=@recv, received_by_title=@recvtitle, received_by_date=@recvdate, " +
            "registered_by=@regby, registered_by_title=@regbytitle, registered_by_date=@regbydate, " +
            "remarks=@remarks";

        private MySqlParameter[] FieldParams(string status)
        {
            return new[]
            {
                new MySqlParameter("@form_code", _formCode),
                new MySqlParameter("@form_name", _formName),
                new MySqlParameter("@is_delayed", chkDelayed.Checked ? 1 : 0),
                new MySqlParameter("@date_registered",
                    (object)RegistrationDateFor(status) ?? DBNull.Value),
                new MySqlParameter("@registry_no", S(txtRegNo)),
                new MySqlParameter("@book_volume", S(txtBook)),
                new MySqlParameter("@book_page", S(txtBookPage)),
                new MySqlParameter("@status", status),
                new MySqlParameter("@fn", txtFirstName.Text.Trim()),
                new MySqlParameter("@mn", S(txtMiddleName)),
                new MySqlParameter("@ln", txtLastName.Text.Trim()),
                new MySqlParameter("@sex", Combo(cboSex)),
                new MySqlParameter("@dob", dtpDob.Value.Date),

                new MySqlParameter("@place", ComboJoin(_pob)),
                new MySqlParameter("@country", NullIfBlank(_pobCountry.Text)),
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
                // Both are forced NULL when the parents are not married, so a value left
                // over from before the toggle was switched can never be written onto a
                // record that says there was no marriage.
                new MySqlParameter("@pmdate", tglParentsMarried.Checked && dtpMarrDate.Checked
                    ? (object)dtpMarrDate.Value.Date : DBNull.Value),
                new MySqlParameter("@pmplace", tglParentsMarried.Checked
                    ? ComboJoin(_pom) : DBNull.Value),
                new MySqlParameter("@pmarried", tglParentsMarried.Checked ? 1 : 0),
                new MySqlParameter("@atype", NullIfBlank(OthersBox.Compose(cboAttType, txtAttTypeOther))),
                new MySqlParameter("@aname", S(txtAttName)),
                new MySqlParameter("@atitle", S(txtAttTitle)),
                new MySqlParameter("@aaddr", ComboJoin(_attAddr)),
                new MySqlParameter("@adate", PickedDate(dtpAttDate)),
                new MySqlParameter("@iname", S(txtInfName)),
                new MySqlParameter("@irel", NullIfBlank(OthersBox.Compose(_cboInfRel, txtInfRelOther))),
                new MySqlParameter("@iaddr", ComboJoin(_infAddr)),
                new MySqlParameter("@idate", dtpInfDate.Checked ? (object)dtpInfDate.Value.Date : DBNull.Value),
                new MySqlParameter("@prep", S(txtPreparedBy)),
                new MySqlParameter("@preptitle", S(txtPreparedTitle)),
                new MySqlParameter("@prepdate", PickedDate(dtpPreparedDate)),
                new MySqlParameter("@recv", S(txtReceivedBy)),
                new MySqlParameter("@recvtitle", S(txtReceivedTitle)),
                new MySqlParameter("@recvdate", PickedDate(dtpReceivedDate)),
                new MySqlParameter("@regby", S(txtRegisteredBy)),
                new MySqlParameter("@regbytitle", S(txtRegisteredTitle)),
                new MySqlParameter("@regbydate", PickedDate(dtpRegisteredDate)),
                new MySqlParameter("@remarks", S(txtRemarks)),
            };
        }

        /// <summary>
        /// A date the certification block may not carry. These pickers show their check box,
        /// so an unticked one means the sheet states no date there and the column stays NULL
        /// rather than recording a signing date nobody wrote.
        /// </summary>
        private static object PickedDate(DateTimePicker dtp)
        {
            return dtp.Checked ? (object)dtp.Value.Date : DBNull.Value;
        }

        /// <summary>
        /// Next birth registry number for the current year, as YYYY-B-####. Derived from
        /// the highest sequence already issued this year + 1 (not a row count), so a
        /// delete never causes a duplicate. Year-scoped: restarts each January.
        /// </summary>
        private static string NextRegistryNo()
        {
            return RegistryNumber.Next("births", 'B');
        }

        // ---------- delayed registration (RA 3753) ----------

        /// <summary>
        /// Days the law allows between the birth and its registration. RA 3753 gives 30;
        /// past that the registration is "delayed" and needs the extra affidavits, which
        /// is why the monthly PSA report has to split the two.
        /// </summary>
        private const int ReglementaryDays = 30;

        /// <summary>
        /// The date this record was (or is being) registered, or null when that is genuinely
        /// unknown.
        ///
        /// <para>A Draft is not registered yet, so it has no registration date. A record
        /// being registered now gets today. An existing record keeps the date it was
        /// registered on — reopening a 2019 record to correct a spelling must not re-date
        /// its registration to today and turn it delayed.</para>
        ///
        /// <para>Null for every row that predates this column: those were either migrated
        /// from the old system or digitized from the paper books, and in neither case does
        /// the database know when the office actually registered them. See migration 27 —
        /// created_at is a row-creation date, and for a scan that is the DIGITIZATION date,
        /// so deriving a registration date from it would mark the whole backlog delayed.</para>
        /// </summary>
        private DateTime? RegistrationDateFor(string status)
        {
            if (status == "Draft") return null;
            return _dateRegistered ?? DateTime.Today;
        }

        /// <summary>
        /// Sets the Delayed Registration box from the dates rather than waiting for someone
        /// to remember it, and says on the label why.
        ///
        /// <para>THIS IS THE FIX for a wrong statutory figure, not a convenience. The box
        /// was purely manual, nobody ticked it, and <c>ReportsPsaForm</c> reads that flag —
        /// so the monthly PSA submission was reporting every registration as timely while
        /// the dates said otherwise. Computing it means the report can only be wrong if the
        /// dates are.</para>
        ///
        /// <para>Still overridable: whether a registration is delayed is the registrar's
        /// determination (they are the one collecting the affidavits), so this sets the
        /// default and shows the arithmetic — it does not lock the box.</para>
        /// </summary>
        private void RecomputeDelayed()
        {
            _suppressDelayedRecompute = true;
            try { chkDelayed.Checked = LagDays() > ReglementaryDays; }
            finally { _suppressDelayedRecompute = false; }
            UpdateDelayedLabel();
        }

        /// <summary>Days between the birth and the registration date being used.</summary>
        private int LagDays()
        {
            DateTime asOf = (_dateRegistered ?? DateTime.Today).Date;
            return (int)(asOf - dtpDob.Value.Date).TotalDays;
        }

        /// <summary>
        /// Shows the arithmetic behind the box, so a registrar who disagrees can see what
        /// the system counted before overriding it. Label only — never touches the flag,
        /// which is why loading a record can call this without rewriting what was recorded.
        /// </summary>
        private void UpdateDelayedLabel()
        {
            int lag = LagDays();
            chkDelayed.Text = lag < 0
                ? "Delayed Registration — check the date of birth, it is in the future"
                : lag > ReglementaryDays
                    ? string.Format(
                        "Delayed Registration — birth was {0:N0} days ago, past the {1}-day period",
                        lag, ReglementaryDays)
                    : string.Format(
                        "Delayed Registration — not delayed, {0:N0} of {1} days used",
                        lag, ReglementaryDays);
        }

        private void dtpDob_ValueChanged(object sender, EventArgs e)
        {
            if (!_suppressDelayedRecompute) RecomputeDelayed();
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
            RefreshDelayedCaseButton();
            _scanImage = null;
            // A new record is on the revision the office issues today. Without this reset
            // the form would keep the revision of the last scan it was primed from.
            _formCode = FormCatalog.Current(DocKind.Birth)?.FormCode;
            _formName = FormCatalog.Current(DocKind.Birth)?.FormName;
            foreach (Control c in EnumerateInputs(this)) ClearControl(c);
            cboStatus.SelectedItem = "Draft";
            dtpMarrDate.Checked = false;
            // Back to the form's own default: married, with items 18a/18b live.
            tglParentsMarried.SetCheckedSilently(true);
            ApplyParentsMarried();

            // Date pickers are not touched by ClearControl (it handles TextBox and ComboBox
            // only), so New used to leave the PREVIOUS record's date of birth on screen — a
            // registrar who did not notice would date a newborn 2018. Reset to today, which
            // is also the only sane basis for the delayed calculation below.
            dtpDob.Value = DateTime.Today;

            dtpInfDate.Value = DateTime.Today;

            // The certification-block dates are optional (ShowCheckBox), so a fresh record
            // must clear the TICK as well as the value - a left-over tick would carry the
            // previous record's signing date onto a blank form.
            dtpInfDate.Checked = false;
            dtpAttDate.Checked = false;
            dtpPreparedDate.Checked = false;
            dtpReceivedDate.Checked = false;
            dtpRegisteredDate.Checked = false;

            // ClearControl empties every combo, including the country, which would leave a
            // fresh record with no country rather than the one this office almost always
            // registers. A loaded record still shows whatever it actually holds.
            GeoLookup.Select(_pobCountry, GeoLookup.HomeCountry);

            _dateRegistered = null;   // a fresh entry is registered today, not on some past date
            RecomputeDelayed();       // sets the box from the (now reset) date of birth
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
            else if (c is ComboBox cb) { cb.SelectedIndex = -1; cb.Text = ""; }
        }

        // ---------- runtime UI helpers / dynamic lookup controls ----------
        // These belong in BirthRegistrationForm.cs, not the generated Designer file.
        private ComboBox _cboMCit, _cboMRel, _cboMOcc;
        private ComboBox _cboFCit, _cboFRel, _cboFOcc;
        private ComboBox _cboInfRel, _cboBirthOrder;
        private ComboBox _pobCountry;  // Place of Birth country — its OWN column, never joined into place_of_birth
        private ComboBox[] _pob;   // Place of Birth:  hospital, municipality, province
        private ComboBox[] _pom;   // Marriage Place:  church,  municipality, province
        private ComboBox[] _mres;  // Mother Residence: house/st, barangay, municipality, province
        private ComboBox[] _fres;  // Father Residence: house/st, barangay, municipality, province
        private ComboBox[] _attAddr;  // Attendant Address: barangay, municipality, province
        private ComboBox[] _infAddr;  // Informant Address: barangay, municipality, province

        private void InitializeLookupControls()
        {
            _cboMCit = CreateLookupCells(txtMCitizen, 1, null)[0];
            _cboMRel = CreateLookupCells(txtMReligion, 1, null)[0];
            _cboMOcc = CreateLookupCells(txtMOccupation, 1, null)[0];
            _mres = CreateLookupCells(txtMResidence, 4, new[] { "House / St.", "Province", "Municipality", "Barangay" });
            _mProvince = _mres[1];
            _mMunicipality = _mres[2];
            _mBarangay = _mres[3];

            _cboFCit = CreateLookupCells(txtFCitizen, 1, null)[0];
            _cboFRel = CreateLookupCells(txtFReligion, 1, null)[0];
            _cboFOcc = CreateLookupCells(txtFOccupation, 1, null)[0];
            _fres = CreateLookupCells(txtFResidence, 4, new[] { "House / St.", "Province", "Municipality", "Barangay" });
            _fProvince = _fres[1];
            _fMunicipality = _fres[2];
            _fBarangay = _fres[3];
            _cboInfRel = CreateLookupCells(txtInfRel, 1, null)[0];
            _cboInfRel.Tag = "relationships:birth";
            _cboBirthOrder = CreateLookupCells(txtBirthOrder, 1, null)[0];
            // Four cells, but only the last three are the PLACE. The country is kept out of
            // _pob deliberately: _pob is comma-joined into births.place_of_birth and split
            // back out on load, so a fourth part would re-split every row already written.
            ComboBox[] pobCells = CreateLookupCells(txtPlace, 4,
                new[] { "Country", "Hospital / Clinic", "Province", "Municipality" });
            _pobCountry = pobCells[0];
            _pob = new[] { pobCells[1], pobCells[2], pobCells[3] };
            _pobProvince = _pob[1];
            _pobMunicipality = _pob[2];

            _pom = CreateLookupCells(txtMarrPlace, 3, new[] { "Church", "Province", "Municipality" });
            _pomProvince = _pom[1];
            _pomMunicipality = _pom[2];

            _attAddr = CreateLookupCells(txtAttAddress, 3, new[] { "Province", "Municipality", "Barangay" });
            _attProvince = _attAddr[0];
            _attMunicipality = _attAddr[1];
            _attBarangay = _attAddr[2];

            _infAddr = CreateLookupCells(txtInfAddress, 3, new[] { "Province", "Municipality", "Barangay" });
            _infProvince = _infAddr[0];
            _infMunicipality = _infAddr[1];
            _infBarangay = _infAddr[2];
        }


        /// <summary>
        /// Item 18 of Municipal Form 102 - the marriage of the parents - behind a toggle
        /// that DEFAULTS TO ON, because most registrations are of married parents.
        /// <para/>
        /// Turned off, the date and place are not merely cleared: they are DISABLED, and
        /// that difference is the whole point. A blank box says "nobody filled this in";
        /// a greyed box says "this question does not apply to this child". On a birth
        /// certificate that is not presentation - whether the parents were married is what
        /// determines the child's legitimacy under the Family Code, and it is what an
        /// RA 9255 acknowledgement later attaches to. The answer is stored in its own
        /// column (migration 31) rather than inferred from two empty fields.
        /// </summary>
        private void InitializeParentsMarried()
        {
            tglParentsMarried.SetCheckedSilently(true);
            tglParentsMarried.CheckedChanged += tglParentsMarried_CheckedChanged;
            ApplyParentsMarried();
        }

        private void tglParentsMarried_CheckedChanged(object sender, EventArgs e)
        {
            ApplyParentsMarried();
        }

        /// <summary>
        /// Enable or grey out items 18a/18b, and say in words which state the record is in.
        /// <para/>
        /// Switching to "not married" CLEARS the two fields as well as disabling them. A
        /// disabled control still holds its text and would still be read by FieldParams,
        /// so leaving a date behind would write a marriage date onto a record that states
        /// there was no marriage.
        /// </summary>
        private void ApplyParentsMarried()
        {
            bool married = tglParentsMarried.Checked;

            if (!married)
            {
                dtpMarrDate.Checked = false;
                dtpMarrDate.Value = DateTime.Today;
                foreach (ComboBox c in _pom) GeoLookup.Select(c, "");
            }

            dtpMarrDate.Enabled = married;
            lblMarrDate.Enabled = married;
            lblMarrPlace.Enabled = married;
            foreach (ComboBox c in _pom)
            {
                c.Enabled = married;
                // The caption under each cell is a sibling in the same grid, so it has to
                // be greyed with it or the block reads half-live.
                if (c.Parent != null)
                    foreach (Control sibling in c.Parent.Controls)
                        if (sibling is Label) sibling.Enabled = married;
            }

            lblParentsMarriedState.Text = married
                ? "Married - state the date and place below"
                : "Not married - items 18a and 18b do not apply";
            lblParentsMarriedState.ForeColor = married ? UiTheme.Ink : UiTheme.Muted;
        }

        /// <summary>
        /// Province -> Municipality -> Barangay, from the national PSGC set loaded by
        /// migration 29. The lists and the cascade itself live in <see cref="GeoLookup"/>
        /// so Birth, Marriage and Death behave identically; this method only says which
        /// cells on THIS form form a trio.
        /// </summary>
        private void WireGeography()
        {
            GeoLookup.CascadeAddress(_mres[1], _mres[2], _mres[3]);
            GeoLookup.CascadeAddress(_fres[1], _fres[2], _fres[3]);
            GeoLookup.CascadeAddress(_attAddr[0], _attAddr[1], _attAddr[2]);
            GeoLookup.CascadeAddress(_infAddr[0], _infAddr[1], _infAddr[2]);
            GeoLookup.CascadePlace(_pob[1], _pob[2]);
            GeoLookup.CascadePlace(_pom[1], _pom[2]);

            foreach (ComboBox province in new[]
                     { _mres[1], _fres[1], _attAddr[0], _infAddr[0], _pob[1], _pom[1] })
                GeoLookup.LoadProvinces(province);

            // Country last: wiring it before the province list exists would have the first
            // selection fire into an unbuilt cascade.
            GeoLookup.LoadCountries(_pobCountry);
            GeoLookup.CascadeCountry(_pobCountry, _pob[1], _pob[2], null);
            GeoLookup.Select(_pobCountry, GeoLookup.HomeCountry);
        }

        /// <summary>
        /// Put a stored "House/St., Province, Municipality, Barangay" back on screen.
        /// <para/>
        /// Written through <see cref="GeoLookup.SetAddress"/> rather than by setting the
        /// four boxes independently, because the cells CASCADE: selecting a barangay whose
        /// municipality has not been chosen selects into an empty list, so the value shows
        /// while the dropdown behind it belongs to nowhere.
        /// </summary>
        private static void SetResidence(ComboBox[] cells, string value)
        {
            string[] b = Parts(value, 4);
            GeoLookup.Select(cells[0], b[0]);
            GeoLookup.SetAddress(cells[1], cells[2], cells[3], b[1], b[2], b[3]);
        }

        /// <summary>A stored "Province, Municipality, Barangay" trio.</summary>
        private static void SetAddress3(ComboBox[] cells, string value)
        {
            string[] b = Parts(value, 3);
            GeoLookup.SetAddress(cells[0], cells[1], cells[2], b[0], b[1], b[2]);
        }

        /// <summary>A stored "Facility, Province, Municipality" place.</summary>
        private static void SetPlace3(ComboBox[] cells, string value)
        {
            string[] b = Parts(value, 3);
            GeoLookup.Select(cells[0], b[0]);
            GeoLookup.Select(cells[1], b[1]);
            GeoLookup.LoadMunicipalities(cells[2], b[1]);
            GeoLookup.Select(cells[2], b[2]);
        }

        private static string[] Parts(string value, int count)
        {
            string[] bits = (value ?? "").Split(new[] { "," }, StringSplitOptions.None);
            var outp = new string[count];
            for (int i = 0; i < count; i++) outp[i] = i < bits.Length ? bits[i].Trim() : "";
            return outp;
        }

        /// <summary>
        /// Rebuilds the entry panel around a numbered step strip (top) and an "at a glance"
        /// summary rail (right) - the same StepStrip / IssueList / Banner pieces the marriage
        /// license window (Municipal Form 90) already uses, reused rather than reinvented so
        /// the two wizards read as one system. Built in code, not the Designer, for the same
        /// reason every other piece of this wizard is: a Designer regeneration has silently
        /// deleted hand-added controls here before (2026-09-02, 2026-09-13).
        /// <para/>
        /// The native TabControl headers are hidden (SizeMode=Fixed, then the control is
        /// positioned OUTSIDE its host's top edge so the header band falls off-screen) - the
        /// step strip is the only navigator now, matching the reference screen exactly rather
        /// than showing two competing tab strips.
        /// </summary>
        private void InitializeWizardChrome()
        {
            if (_wizardHost != null) return;

            cardForm.Controls.Remove(tabControl);
            tabControl.Dock = DockStyle.None;

            _tabHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = System.Drawing.Color.White };
            _tabHost.Controls.Add(tabControl);
            _tabHost.Resize += delegate { PositionHiddenTabHeader(); };
            tabControl.Multiline = false;
            tabControl.SizeMode = TabSizeMode.Fixed;

            _stepStrip = new StepStrip(true);
            string[] subs =
            {
                "child's own particulars",
                "mother's particulars",
                "father's particulars",
                "Family Code Art. 164-176",
                "who attended the birth",
                "who is reporting it",
                "signatures & registry"
            };
            var pages = new[] { tabChild, tabMother, tabFather, tabMarriage, tabAttendant, tabInformant, tabCert };
            for (int i = 0; i < pages.Length; i++)
                _stepStrip.AddStep(pages[i].Text, i < subs.Length ? subs[i] : "");
            _stepStrip.StepClicked += i => GoToStep(i);
            tabControl.SelectedIndexChanged += delegate { UpdateStepNavigation(); };

            _railPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 14, 16, 14),
                BackColor = System.Drawing.Color.FromArgb(250, 251, 253)
            };
            _railPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var p = new System.Drawing.Pen(UiTheme.CardLine))
                    e.Graphics.DrawLine(p, 0, 0, 0, _railPanel.Height);
            };

            var wizardBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            wizardBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            wizardBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            wizardBody.Controls.Add(_tabHost, 0, 0);
            wizardBody.Controls.Add(_railPanel, 1, 0);

            _wizardHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _wizardHost.Controls.Add(wizardBody);
            _wizardHost.Controls.Add(_stepStrip);
            cardForm.Controls.Add(_wizardHost);

            PositionHiddenTabHeader();

            // Keeps the rail current as the operator types - the same live-refresh
            // convention the marriage license window uses for its own "at a glance" rail.
            EventHandler railRefresh = delegate { RefreshRail(); };
            txtFirstName.TextChanged += railRefresh;
            txtLastName.TextChanged += railRefresh;
            cboSex.SelectedIndexChanged += railRefresh;
            cboStatus.SelectedIndexChanged += railRefresh;
            txtMFirst.TextChanged += railRefresh;
            txtMLast.TextChanged += railRefresh;
            txtFFirst.TextChanged += railRefresh;
            txtFLast.TextChanged += railRefresh;
            txtRegNo.TextChanged += railRefresh;
            if (_pob != null && _pob.Length > 0) _pob[0].TextChanged += railRefresh;
        }

        /// <summary>
        /// Pushes the native TabControl header band above tabHost's visible top edge, so only
        /// the page content shows - a child positioned outside its parent's client rectangle
        /// is simply not drawn there, which is what removes the header without needing any
        /// Win32 message trickery. Re-run on every resize since the header's true position is
        /// relative to tabHost's current size.
        /// </summary>
        private void PositionHiddenTabHeader()
        {
            if (_tabHost == null || tabControl == null) return;
            const int headerH = 36;
            tabControl.SetBounds(-2, -headerH, _tabHost.Width + 4, _tabHost.Height + headerH);
        }

        /// <summary>
        /// Rebuilds the "at a glance" rail from the record as it stands right now - registry
        /// identity, current status/step, and an Outstanding checklist built from the SAME
        /// required fields <see cref="ValidateChild"/> enforces (plus a few recommended-but-
        /// not-required fields, marked as such) so the rail can never promise more than Submit
        /// actually checks.
        /// </summary>
        private void RefreshRail()
        {
            if (_railPanel == null) return;

            _railPanel.SuspendLayout();
            var toDispose = new List<Control>();
            foreach (Control c in _railPanel.Controls) toDispose.Add(c);
            foreach (Control c in toDispose) c.Dispose();
            _railPanel.Controls.Clear();

            var items = new List<Control>();
            items.Add(MUi.Cap("Registration at a glance"));
            items.Add(MUi.Kv("Registry No.", string.IsNullOrWhiteSpace(txtRegNo.Text) ? "(assigned on save)" : txtRegNo.Text));

            string childName = ((txtLastName.Text ?? "").Trim() + ", " + (txtFirstName.Text ?? "").Trim()).Trim(' ', ',');
            items.Add(MUi.Kv("Child", string.IsNullOrWhiteSpace(childName) ? "-" : childName));
            items.Add(MUi.Kv("Date of birth", dtpDob.Value.ToString("dd MMM yyyy")));
            items.Add(MUi.Kv("Delayed?", chkDelayed.Checked ? "Yes" : "No"));

            var stage = new FlowLayoutPanel { Height = 34, BackColor = System.Drawing.Color.Transparent, Padding = new Padding(0, 6, 0, 0), Dock = DockStyle.Top };
            stage.Controls.Add(MUi.Txt("Current status", 9F, System.Drawing.FontStyle.Regular, UiTheme.Muted));
            stage.Controls.Add(MUi.Pill((cboStatus.Text ?? "Draft").ToUpperInvariant(), cboStatus.Text));
            items.Add(stage);

            if (tabControl.SelectedIndex >= 0)
            {
                var stepLine = new FlowLayoutPanel { Height = 22, BackColor = System.Drawing.Color.Transparent, Dock = DockStyle.Top };
                int stepNo = tabControl.SelectedIndex + 1, stepTotal = tabControl.TabPages.Count;
                stepLine.Controls.Add(MUi.Txt("Step " + stepNo + " of " + stepTotal + " - " + tabControl.TabPages[tabControl.SelectedIndex].Text,
                    9F, System.Drawing.FontStyle.Regular, UiTheme.Muted));
                items.Add(stepLine);
            }

            items.Add(MUi.Cap("Outstanding"));
            var issues = new List<RuleIssue>();
            if (string.IsNullOrWhiteSpace(txtFirstName.Text)) issues.Add(new RuleIssue(RuleSeverity.Blocking, "CHILD_FIRST", "Child's first name", "Child"));
            if (string.IsNullOrWhiteSpace(txtLastName.Text)) issues.Add(new RuleIssue(RuleSeverity.Blocking, "CHILD_LAST", "Child's last name", "Child"));
            if (cboSex.SelectedItem == null) issues.Add(new RuleIssue(RuleSeverity.Blocking, "CHILD_SEX", "Child's sex", "Child"));
            if (_pob != null && _pob.Length > 0 && string.IsNullOrWhiteSpace(_pob[0].Text))
                issues.Add(new RuleIssue(RuleSeverity.Warning, "POB", "Place of birth (hospital/facility)", "Attendant"));
            if (string.IsNullOrWhiteSpace(txtMFirst.Text) || string.IsNullOrWhiteSpace(txtMLast.Text))
                issues.Add(new RuleIssue(RuleSeverity.Warning, "MOTHER", "Mother's name", "Mother"));
            if (string.IsNullOrWhiteSpace(txtFFirst.Text) || string.IsNullOrWhiteSpace(txtFLast.Text))
                issues.Add(new RuleIssue(RuleSeverity.Warning, "FATHER", "Father's name", "Father"));

            var issueList = new IssueList { Height = 190, Dock = DockStyle.Top };
            issueList.FixRequested += tabName => { int idx = TabIndexByText(tabName); if (idx >= 0) GoToStep(idx); };
            issueList.SetIssues(issues, "Every required field for this record is filled in.");
            items.Add(issueList);

            int blocking = 0;
            foreach (RuleIssue i in issues) if (i.Severity == RuleSeverity.Blocking) blocking++;

            var banner = new Banner();
            if (blocking == 0 && issues.Count == 0) banner.Set(RuleSeverity.Info, "Ready to save.", "All checks pass.", true);
            else if (blocking == 0) banner.Set(RuleSeverity.Warning, "Recommended fields missing.", "Can still be saved as a draft.");
            else banner.Set(RuleSeverity.Blocking, blocking + " REQUIRED FIELD" + (blocking == 1 ? "" : "S") + " MISSING", "Submit for Approval will be refused.");
            items.Add(new Panel { Height = 10, BackColor = System.Drawing.Color.Transparent, Dock = DockStyle.Top });
            items.Add(banner);

            StackRail(_railPanel, items.ToArray());
            _railPanel.ResumeLayout();
        }

        /// <summary>
        /// Adds controls to a Dock=Top host in visual top-to-bottom order by adding them
        /// BOTTOM-FIRST - the same convention the marriage license window's own Stack()
        /// establishes for this codebase, kept identical here rather than re-derived.
        /// </summary>
        private static void StackRail(Control host, params Control[] topToBottom)
        {
            for (int i = topToBottom.Length - 1; i >= 0; i--)
            {
                topToBottom[i].Dock = DockStyle.Top;
                host.Controls.Add(topToBottom[i]);
            }
        }

        private int TabIndexByText(string text)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
                if (string.Equals(tabControl.TabPages[i].Text, text, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private void InitializeAddAnotherBirthButton()
        {
            if (_btnAddAnotherBirth != null || pnlRecordActions == null) return;
            _btnAddAnotherBirth = new Button
            {
                Width = 190,
                Height = 30,
                Text = "+ Add Another Birth Form",
                Margin = btnNew.Margin
            };
            _btnAddAnotherBirth.Click += btnAddAnotherBirth_Click;
            pnlRecordActions.Controls.Add(_btnAddAnotherBirth);
        }

        /// <summary>
        /// Only relevant for a SAVED record that is actually over the reglementary period - a
        /// blank form or a timely one has no delayed-registration case to open. Built in code
        /// (not the Designer) so a future VS designer regeneration cannot silently drop it, the
        /// same trap that repeatedly deleted hand-added controls in this form.
        /// </summary>
        private void InitializeDelayedCaseButton()
        {
            if (_btnDelayedCase != null || pnlRecordActions == null) return;
            _btnDelayedCase = new Button { Width = 220, Height = 30, Text = "Delayed Birth Registration...", Margin = btnNew.Margin };
            _btnDelayedCase.Click += (s, e) =>
            {
                if (_editingId == null) return;
                using (var f = new DelayedBirthCaseForm(_editingId.Value)) f.ShowDialog(this);
            };
            pnlRecordActions.Controls.Add(_btnDelayedCase);
            RefreshDelayedCaseButton();
        }

        private void RefreshDelayedCaseButton()
        {
            if (_btnDelayedCase == null) return;
            _btnDelayedCase.Enabled = _editingId != null && chkDelayed.Checked;
        }

        private void btnAddAnotherBirth_Click(object sender, EventArgs e)
        {
            if (_editingId == null)
            {
                MessageBox.Show("Save or submit the current child's form first. Then add the next child.",
                    "Add Another Birth Form", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string mFirst = txtMFirst.Text, mMiddle = txtMMiddle.Text, mLast = txtMLast.Text;
            string mCit = _cboMCit.Text, mRel = _cboMRel.Text, mOcc = _cboMOcc.Text, mAge = txtMAge.Text;
            object mRes = ComboJoin(_mres);

            string fFirst = txtFFirst.Text, fMiddle = txtFMiddle.Text, fLast = txtFLast.Text;
            string fCit = _cboFCit.Text, fRel = _cboFRel.Text, fOcc = _cboFOcc.Text, fAge = txtFAge.Text;
            object fRes = ComboJoin(_fres);

            bool parentsMarried = tglParentsMarried.Checked;
            bool marrKnown = dtpMarrDate.Checked;
            DateTime marrDate = dtpMarrDate.Value;
            object marrPlace = ComboJoin(_pom);

            string attType = cboAttType.Text, attName = txtAttName.Text, attTitle = txtAttTitle.Text;
            object attAddr = ComboJoin(_attAddr);

            string infName = txtInfName.Text, infRel = _cboInfRel.Text;
            object infAddr = ComboJoin(_infAddr);
            bool infDateKnown = dtpInfDate.Checked;
            DateTime infDate = dtpInfDate.Value;

            string prepared = txtPreparedBy.Text, received = txtReceivedBy.Text;
            object place = ComboJoin(_pob);
            string placeCountry = _pobCountry.Text;

            // New separate Form 102 / births row. This does NOT depend on Type of Birth.
            // ClearForm does not reset the queue ticket, so the parent stays in the same service flow.
            ClearForm();

            txtMFirst.Text = mFirst; txtMMiddle.Text = mMiddle; txtMLast.Text = mLast;
            SetLookup(_cboMCit, mCit); SetLookup(_cboMRel, mRel); SetLookup(_cboMOcc, mOcc);
            txtMAge.Text = mAge; SetResidence(_mres, mRes == DBNull.Value ? "" : mRes.ToString());

            txtFFirst.Text = fFirst; txtFMiddle.Text = fMiddle; txtFLast.Text = fLast;
            SetLookup(_cboFCit, fCit); SetLookup(_cboFRel, fRel); SetLookup(_cboFOcc, fOcc);
            txtFAge.Text = fAge; SetResidence(_fres, fRes == DBNull.Value ? "" : fRes.ToString());

            // Siblings share their parents, so they share the answer to item 18 - and it
            // has to be restored BEFORE the date and place, or ApplyParentsMarried would
            // clear what was just copied in.
            tglParentsMarried.SetCheckedSilently(parentsMarried);
            ApplyParentsMarried();
            dtpMarrDate.Checked = marrKnown;
            if (marrKnown) dtpMarrDate.Value = marrDate;
            SetPlace3(_pom, marrPlace == DBNull.Value ? "" : marrPlace.ToString());

            SetCombo(cboAttType, attType); txtAttName.Text = attName; txtAttTitle.Text = attTitle;
            SetAddress3(_attAddr, attAddr == DBNull.Value ? "" : attAddr.ToString());

            txtInfName.Text = infName; SetLookup(_cboInfRel, infRel);
            SetAddress3(_infAddr, infAddr == DBNull.Value ? "" : infAddr.ToString());
            dtpInfDate.Checked = infDateKnown;
            if (infDateKnown) dtpInfDate.Value = infDate;

            txtPreparedBy.Text = prepared; txtReceivedBy.Text = received;
            GeoLookup.Select(_pobCountry, placeCountry);
            SetPlace3(_pob, place == DBNull.Value ? "" : place.ToString());

            // Child-specific values remain fresh for the next twin/triplet/etc.
            txtFirstName.Clear(); txtMiddleName.Clear(); txtLastName.Clear();
            cboSex.SelectedIndex = -1; cboSex.Text = "";
            cboTypeOfBirth.SelectedIndex = -1; cboTypeOfBirth.Text = "";
            _cboBirthOrder.SelectedIndex = -1; _cboBirthOrder.Text = "";
            txtWeight.Clear();
            txtRegNo.Clear();
            cboStatus.SelectedItem = "Draft";

            tabControl.SelectedTab = tabChild;
            txtFirstName.Focus();
        }

        private void GoToStep(int index)
        {
            if (index < 0 || index >= tabControl.TabPages.Count) return;

            tabControl.SelectedIndex = index;
            UpdateStepNavigation();
        }

        private void UpdateStepNavigation()
        {
            if (tabControl.SelectedIndex < 0) return;

            if (_stepStrip != null)
            {
                for (int i = 0; i < _stepStrip.Count; i++)
                    _stepStrip.SetState(i, i == tabControl.SelectedIndex ? StepStrip.State.Current
                        : i < tabControl.SelectedIndex ? StepStrip.State.Done : StepStrip.State.Todo);
            }
            RefreshRail();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MainForm shell = null;
                for (Control parent = Parent; parent != null; parent = parent.Parent)
                {
                    shell = parent as MainForm;
                    if (shell != null) break;
                }
                if (shell == null) shell = Owner as MainForm;

                if (shell != null)
                {
                    // The registered "ocr" module is OcrDigitizationForm. Reuse it so
                    // extracted birth fields can return through the existing workflow.
                    shell.GoToModule("ocr");
                    return;
                }

                // Also support opening Birth Registration as a standalone form.
                using (OcrDigitizationForm ocr = CreateStandaloneOcrWindow())
                    ocr.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private ComboBox[] CreateLookupCells(TextBox tb, int count, string[] captions)
        {
            var owner = tb.Parent as TableLayoutPanel;
            if (owner == null) throw new InvalidOperationException(
                "Lookup placeholder '" + tb.Name + "' must sit in a TableLayoutPanel cell.");

            // Read the cell BEFORE removing the control — the position and span are lost with it.
            TableLayoutPanelCellPosition cell = owner.GetPositionFromControl(tb);
            int span = owner.GetColumnSpan(tb);
            bool captioned = captions != null;

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                ColumnCount = count,
                RowCount = captioned ? 2 : 1
            };
            for (int i = 0; i < count; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / count));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27f));
            if (captioned) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16f));

            var host = new Panel
            {
                Margin = tb.Margin,
                Anchor = tb.Anchor,
                Height = captioned ? 43 : 25,
                BackColor = System.Drawing.Color.Transparent
            };
            // A placeholder anchored Left only is a deliberately narrow field (an age, a birth
            // order); one anchored to both sides is meant to fill its column.
            if ((tb.Anchor & AnchorStyles.Right) == 0) host.Width = tb.Width;

            var made = new ComboBox[count];
            for (int i = 0; i < count; i++)
            {
                var cbo = new ComboBox
                {
                    Name = tb.Name + "Lookup" + i,
                    DropDownStyle = ComboBoxStyle.DropDown,
                    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                    AutoCompleteSource = AutoCompleteSource.ListItems,
                    Dock = DockStyle.Top,
                    Font = tb.Font,
                    Margin = new Padding(0, 0, i == count - 1 ? 0 : 6, 0)
                };
                grid.Controls.Add(cbo, i, 0);
                made[i] = cbo;

                if (captioned)
                    grid.Controls.Add(new Label
                    {
                        Text = captions[i],
                        Dock = DockStyle.Fill,
                        ForeColor = UiTheme.Faint,
                        Font = new System.Drawing.Font("Segoe UI", 7.5F),
                        Margin = new Padding(1, 1, 6, 0)
                    }, i, 1);
            }

            owner.Controls.Remove(tb);
            host.Controls.Add(grid);
            host.Controls.Add(tb);
            tb.Visible = false;
            owner.Controls.Add(host, cell.Column, cell.Row);
            if (span > 1) owner.SetColumnSpan(host, span);
            return made;
        }

        /// <summary>
        /// The content column now fills the whole available width - Birth Registration is a
        /// maximizable module like every other screen in the shell, not a narrow reading
        /// column with side gutters. The two gutter columns collapse to nothing and the
        /// middle column takes 100%; the AutoScrollMinSize floor set in the Designer is what
        /// keeps the form usable on a small window (it scrolls rather than the content
        /// shrinking away). Kept as a method (rather than deleted) so the existing Resize
        /// wiring and Load-time call need no other change.
        /// </summary>
        private void CenterContent()
        {
            layoutRoot.ColumnStyles[0].SizeType = SizeType.Absolute;
            layoutRoot.ColumnStyles[0].Width = 0F;
            layoutRoot.ColumnStyles[1].SizeType = SizeType.Percent;
            layoutRoot.ColumnStyles[1].Width = 100F;
            layoutRoot.ColumnStyles[2].SizeType = SizeType.Absolute;
            layoutRoot.ColumnStyles[2].Width = 0F;
        }

        private static OcrDigitizationForm CreateStandaloneOcrWindow()
        {
            return new OcrDigitizationForm
            {
                FormBorderStyle = FormBorderStyle.Sizable,  
                StartPosition = FormStartPosition.CenterParent,
                ShowInTaskbar = false
            };
        }

        // ---------- helpers ----------
        private static object NullIfBlank(string v) =>
            string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v.Trim();

        private static object S(TextBox t) =>
            string.IsNullOrWhiteSpace(t.Text) ? (object)DBNull.Value : t.Text.Trim();

        private static object I(TextBox t) =>
            int.TryParse(t.Text, out int n) ? (object)n : DBNull.Value;

        private static object Combo(ComboBox c) =>
            c.SelectedItem == null ? (object)DBNull.Value : c.SelectedItem.ToString();

        private static string Str(object v) => v == DBNull.Value || v == null ? "" : v.ToString();
        private static int ToInt(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);

        /// <summary>
        /// Thin divider between the document/case actions (Delayed Registration, Print
        /// Certificate) and the record CRUD group (New, Update, Delete) — the two groups do
        /// different jobs and read clearer apart than run together in one unbroken row.
        /// </summary>
        private void pnlRecordActions_Paint(object sender, PaintEventArgs e)
        {
            if (btnCertificate == null || btnNew == null) return;
            int x = (btnCertificate.Right + btnNew.Left) / 2;
            using (var pen = new System.Drawing.Pen(UiTheme.CardLine))
                e.Graphics.DrawLine(pen, x, 8, x, pnlRecordActions.Height - 8);
        }

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
