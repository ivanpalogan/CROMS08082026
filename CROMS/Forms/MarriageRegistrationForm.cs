using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Marriage Registration &amp; License overview (PSA Form 90 license + Form 97
    /// certificate). Full-width dashboard: KPI cards, the license posting-period
    /// tracker, and the marriage records grid — all read from the database. The
    /// action buttons open the entry forms (to be built next). All controls are
    /// placed in the Designer.
    /// </summary>
    public partial class MarriageRegistrationForm : Form
    {
        public MarriageRegistrationForm()
        {
            InitializeComponent();
            dgvMarriages.CellClick += dgvMarriages_CellClick;
            dgvLicenses.CellClick += dgvLicenses_CellClick;
            RefreshAll();
        }

        private void RefreshAll()
        {
            LoadKpis();
            LoadLicenses();
            LoadMarriages();
        }

        private void LoadKpis()
        {
            if (!Db.IsConnected()) return;

            lblPostingVal.Text = Db.GetCount(
                "SELECT id FROM marriage_licenses WHERE status = 'Posting'").ToString("00");
            lblIssuedVal.Text = Db.GetCount(
                "SELECT id FROM marriage_licenses WHERE status = 'Issued' " +
                "AND MONTH(created_at) = MONTH(CURDATE()) AND YEAR(created_at) = YEAR(CURDATE())").ToString("00");
            lblExpiringVal.Text = Db.GetCount(
                "SELECT id FROM marriage_licenses WHERE status = 'Posting' " +
                "AND posting_ends BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)").ToString("00");
            lblComsVal.Text = Db.GetCount(
                "SELECT id FROM marriages WHERE status = 'Registered' " +
                "AND MONTH(date_of_marriage) = MONTH(CURDATE()) AND YEAR(date_of_marriage) = YEAR(CURDATE())").ToString("00");
        }

        private void LoadLicenses()
        {
            dgvLicenses.DataSource = Db.Pull(
                "SELECT id, license_no AS 'License No', " +
                "TRIM(CONCAT(COALESCE(husband_name,''), ' & ', COALESCE(wife_name,''))) AS Applicants, " +
                "filed_date AS Filed, posting_ends AS 'Posting Ends', " +
                "DATEDIFF(posting_ends, CURDATE()) AS 'Days Left', status AS Status " +
                "FROM marriage_licenses ORDER BY id DESC");
            if (dgvLicenses.Columns.Contains("id")) dgvLicenses.Columns["id"].Visible = false;
        }

        private void LoadMarriages()
        {
            dgvMarriages.DataSource = Db.Pull(
                "SELECT id, registry_no AS 'Registry No', " +
                "TRIM(CONCAT(COALESCE(husband_last_name,''), ', ', COALESCE(husband_first_name,''), " +
                "'  &  ', COALESCE(wife_last_name,''), ', ', COALESCE(wife_first_name,''))) AS 'Husband & Wife', " +
                "date_of_marriage AS 'Date of Marriage', solemnizer AS Solemnizer, " +
                "book_volume AS Book, status AS Status " +
                "FROM marriages ORDER BY id DESC");
            if (dgvMarriages.Columns.Contains("id")) dgvMarriages.Columns["id"].Visible = false;
        }

        /// <summary>Click a marriage record row to open it in the Form 97 editor.</summary>
        private void dgvMarriages_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            object idCell = dgvMarriages.Rows[e.RowIndex].Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            OpenEditor(Convert.ToInt32(idCell));
        }

        /// <summary>Opens the Form 97 entry dialog (blank for new, loaded for edit),
        /// then refreshes the KPIs + grids when it closes.</summary>
        private void OpenEditor(int? marriageId)
        {
            using (var dlg = new MarriageEntryForm(marriageId))
            {
                dlg.ShowDialog(this);
            }
            RefreshAll();
        }

        private void btnRegister_Click(object sender, EventArgs e) => OpenEditor(null);

        private void btnLicense_Click(object sender, EventArgs e) => OpenLicense(null);

        /// <summary>Click a license row in the posting tracker to open it for edit.</summary>
        private void dgvLicenses_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            object idCell = dgvLicenses.Rows[e.RowIndex].Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            OpenLicense(Convert.ToInt32(idCell));
        }

        /// <summary>Opens the Form 90 license dialog (blank for new, loaded for edit),
        /// then refreshes the KPIs + grids when it closes.</summary>
        private void OpenLicense(int? licenseId)
        {
            using (var dlg = new MarriageLicenseForm(licenseId))
            {
                dlg.ShowDialog(this);
            }
            RefreshAll();
        }
    }

    /// <summary>
    /// Marriage Registration entry dialog — PSA Municipal Form 97 (Certificate of
    /// Marriage). Create a new marriage or edit/delete an existing one. UI is built in
    /// code (self-contained, no designer/.resx/.csproj changes needed), following the
    /// same lookup-combo idea as the Birth/Death forms but binding to the marriages
    /// table's FK id columns (citizenship/religion/residence/church/place). Opened
    /// modally by <see cref="MarriageRegistrationForm"/>.
    /// </summary>
    public class MarriageEntryForm : Form
    {
        /// <summary>The nine per-spouse controls (built identically for husband + wife).</summary>
        private class Spouse
        {
            public TextBox First, Middle, Last, Age;
            public DateTimePicker Dob;
            public ComboBox Citizenship, Religion, CivilStatus, Residence;
        }

        private readonly int? _editingId;

        private TextBox txtReg, txtBook, txtSolemnizer, txtTime;
        private ComboBox cboStatus, cboChurch, cboMuni, cboProv;
        private DateTimePicker dtpDate;
        private Spouse _h, _w;

        private static readonly string[] CivilStatuses =
            { "Single", "Married", "Widowed", "Separated", "Divorced", "Annulled" };

        public MarriageEntryForm(int? marriageId)
        {
            _editingId = marriageId;
            BuildUi();
            if (_editingId != null) LoadMarriage(_editingId.Value);
        }

        /// <summary>Fill the Form-97 dialog from Document AI extraction (new records only).</summary>
        public void PrimeFromExtraction(IDictionary<string, string> f)
        {
            void Set(TextBox t, string key)
            {
                if (t != null && f.TryGetValue(key, out string v) && !string.IsNullOrWhiteSpace(v)) t.Text = v.Trim();
            }

            Set(txtReg, "RegistryNo");
            Set(txtSolemnizer, "Solemnizer");
            Set(_h.First, "HusbandFirst"); Set(_h.Middle, "HusbandMiddle"); Set(_h.Last, "HusbandLast");
            Set(_w.First, "WifeFirst");    Set(_w.Middle, "WifeMiddle");    Set(_w.Last, "WifeLast");

            if (f.TryGetValue("DateOfMarriage", out string dm) &&
                DateTime.TryParse(dm, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime d))
                dtpDate.Value = d;

            if (f.TryGetValue("Nationality", out string nat) && !string.IsNullOrWhiteSpace(nat))
            {
                SelectFkByName(_h.Citizenship, nat);
                SelectFkByName(_w.Citizenship, nat);
            }
            if (f.TryGetValue("PlaceOfMarriage", out string pm) && !string.IsNullOrWhiteSpace(pm))
                SelectFkByName(cboChurch, pm);

            cboStatus.SelectedItem = "Draft";
        }

        /// <summary>Select an FK-bound combo (DataTable of id/name) by its display name.</summary>
        private static void SelectFkByName(ComboBox c, string name)
        {
            if (c == null || c.Items.Count == 0 || string.IsNullOrWhiteSpace(name)) return;
            for (int i = 0; i < c.Items.Count; i++)
            {
                if (c.Items[i] is DataRowView drv &&
                    string.Equals((drv["name"] ?? "").ToString(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                { c.SelectedIndex = i; return; }
            }
        }

        // Softcopy of the source certificate (scan from Document AI), saved with the record.
        private byte[] _scanImage;

        /// <summary>Attach the original scanned document to the next saved record.</summary>
        public void SetScanImage(byte[] bytes) => _scanImage = bytes;

        private void SaveScan(long id)
        {
            if (_scanImage == null || _scanImage.Length == 0) return;
            Db.Push("UPDATE marriages SET scan_image = @img WHERE id = @id",
                new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = _scanImage },
                new MySqlParameter("@id", id));
        }

        // ---------------- UI ----------------
        private void BuildUi()
        {
            Text = _editingId == null ? "Register Marriage — Municipal Form 97"
                                      : "Edit Marriage — Municipal Form 97";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(900, 720);
            BackColor = Color.White;

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
            Controls.Add(scroll);

            // ---- top block: registry / book / status / date / time / solemnizer ----
            int y = 10;
            scroll.Controls.Add(Lbl("Registry No", 15, y));
            scroll.Controls.Add(Lbl("Book / Volume", 235, y));
            scroll.Controls.Add(Lbl("Status", 455, y));
            scroll.Controls.Add(Lbl("Date of Marriage", 605, y));
            y += 20;
            txtReg = Txt(15, y, 200); scroll.Controls.Add(txtReg);
            txtBook = Txt(235, y, 200); scroll.Controls.Add(txtBook);
            cboStatus = Cbo(455, y, 130); cboStatus.Items.AddRange(new object[] { "Registered", "Draft" });
            cboStatus.SelectedItem = "Registered"; scroll.Controls.Add(cboStatus);
            dtpDate = new DateTimePicker { Location = new Point(605, y), Size = new Size(160, 24), Format = DateTimePickerFormat.Short };
            scroll.Controls.Add(dtpDate);
            y += 46;
            scroll.Controls.Add(Lbl("Time of Marriage", 15, y));
            scroll.Controls.Add(Lbl("Solemnizing Officer", 235, y));
            y += 20;
            txtTime = Txt(15, y, 200); scroll.Controls.Add(txtTime);
            txtSolemnizer = Txt(235, y, 350); scroll.Controls.Add(txtSolemnizer);
            y += 54;

            // ---- husband + wife groups side by side ----
            GroupBox gh = BuildSpouse("HUSBAND", 15, y, out _h);
            GroupBox gw = BuildSpouse("WIFE", 455, y, out _w);
            scroll.Controls.Add(gh);
            scroll.Controls.Add(gw);
            y += gh.Height + 14;

            // ---- place of marriage ----
            var gp = new GroupBox
            {
                Text = "PLACE OF MARRIAGE",
                Location = new Point(15, y),
                Size = new Size(865, 90),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87)
            };
            gp.Controls.Add(Lbl("Church / Venue", 15, 22));
            gp.Controls.Add(Lbl("Municipality", 305, 22));
            gp.Controls.Add(Lbl("Province", 595, 22));
            cboChurch = FkCombo(15, 42, 270, "churches"); gp.Controls.Add(cboChurch);
            cboMuni = FkCombo(305, 42, 270, "municipalities"); gp.Controls.Add(cboMuni);
            cboProv = FkCombo(595, 42, 255, "provinces"); gp.Controls.Add(cboProv);
            scroll.Controls.Add(gp);
            y += 104;

            // ---- action buttons ----
            var btnSave = Button("Save Marriage", 15, y, Color.FromArgb(25, 135, 84));
            btnSave.Visible = _editingId == null;
            btnSave.Click += (s, e) => Save();
            var btnUpdate = Button("Update", 15, y, Color.FromArgb(13, 110, 253));
            btnUpdate.Visible = _editingId != null;
            btnUpdate.Click += (s, e) => UpdateRecord();
            var btnDelete = Button("Delete", 175, y, Color.FromArgb(220, 53, 69));
            btnDelete.Visible = _editingId != null;
            btnDelete.Click += (s, e) => Delete();
            var btnCancel = Button("Cancel", 335, y, Color.FromArgb(108, 117, 125));
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            var btnScan = Button("View Softcopy", 495, y, Color.FromArgb(108, 117, 125));
            btnScan.Visible = _editingId != null;
            btnScan.Click += (s, e) =>
            {
                byte[] bytes = _scanImage;
                if (bytes == null && _editingId != null)
                {
                    DataTable dt = Db.Pull("SELECT scan_image FROM marriages WHERE id = " + _editingId.Value);
                    if (dt.Rows.Count > 0 && dt.Rows[0]["scan_image"] != DBNull.Value)
                        bytes = (byte[])dt.Rows[0]["scan_image"];
                }
                if (bytes == null)
                    MessageBox.Show("No softcopy is saved for this record.", "Softcopy",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    SoftcopyViewer.Show(bytes, "Marriage Certificate — Original Softcopy", this);
            };
            scroll.Controls.Add(btnSave);
            scroll.Controls.Add(btnUpdate);
            scroll.Controls.Add(btnDelete);
            scroll.Controls.Add(btnCancel);
            scroll.Controls.Add(btnScan);

            // Shared Learning Library autocomplete on the free-text name/officer boxes.
            LearningLibrary.Attach(_h.First, LearningLibrary.GivenName);
            LearningLibrary.Attach(_h.Last, LearningLibrary.Surname);
            LearningLibrary.Attach(_w.First, LearningLibrary.GivenName);
            LearningLibrary.Attach(_w.Last, LearningLibrary.Surname);
            LearningLibrary.Attach(txtSolemnizer, LearningLibrary.Officer);
        }

        private GroupBox BuildSpouse(string title, int gx, int gy, out Spouse sp)
        {
            var g = new GroupBox
            {
                Text = title,
                Location = new Point(gx, gy),
                Size = new Size(425, 470),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(73, 80, 87)
            };
            sp = new Spouse();
            int iy = 24;
            g.Controls.Add(Lbl("First Name", 15, iy)); sp.First = Txt(15, iy + 18, 380); g.Controls.Add(sp.First); iy += 50;
            g.Controls.Add(Lbl("Middle Name", 15, iy)); sp.Middle = Txt(15, iy + 18, 380); g.Controls.Add(sp.Middle); iy += 50;
            g.Controls.Add(Lbl("Last Name", 15, iy)); sp.Last = Txt(15, iy + 18, 380); g.Controls.Add(sp.Last); iy += 50;
            g.Controls.Add(Lbl("Age", 15, iy)); sp.Age = Txt(15, iy + 18, 80); g.Controls.Add(sp.Age);
            g.Controls.Add(Lbl("Date of Birth", 115, iy));
            sp.Dob = new DateTimePicker { Location = new Point(115, iy + 18), Size = new Size(280, 24), Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
            g.Controls.Add(sp.Dob); iy += 50;
            g.Controls.Add(Lbl("Citizenship", 15, iy)); sp.Citizenship = FkCombo(15, iy + 18, 380, "nationalities"); g.Controls.Add(sp.Citizenship); iy += 50;
            g.Controls.Add(Lbl("Religion", 15, iy)); sp.Religion = FkCombo(15, iy + 18, 380, "religions"); g.Controls.Add(sp.Religion); iy += 50;
            g.Controls.Add(Lbl("Civil Status", 15, iy));
            sp.CivilStatus = Cbo(15, iy + 18, 380); sp.CivilStatus.Items.AddRange(CivilStatuses); g.Controls.Add(sp.CivilStatus); iy += 50;
            g.Controls.Add(Lbl("Residence", 15, iy)); sp.Residence = FkCombo(15, iy + 18, 380, "residences"); g.Controls.Add(sp.Residence);
            return g;
        }

        // ---- small control factories ----
        private static Label Lbl(string t, int x, int y) => new Label
        {
            Text = t,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(108, 117, 125),
            Location = new Point(x, y)
        };
        private static TextBox Txt(int x, int y, int w) => new TextBox
        { Location = new Point(x, y), Size = new Size(w, 24), Font = new Font("Segoe UI", 10F) };
        private static ComboBox Cbo(int x, int y, int w) => new ComboBox
        { Location = new Point(x, y), Size = new Size(w, 24), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList };
        private static Button Button(string t, int x, int y, Color back) => new Button
        {
            Text = t,
            Location = new Point(x, y),
            Size = new Size(150, 40),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = back,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        /// <summary>Pick-only combo bound to a lookup table (id + name), with a leading
        /// blank (id 0 = "none chosen"). Table names are fixed literals, never user input.</summary>
        private static ComboBox FkCombo(int x, int y, int w, string table)
        {
            var c = Cbo(x, y, w);
            DataTable dt;
            try { dt = Db.Pull("SELECT id, name FROM " + table + " ORDER BY name"); }
            catch { dt = new DataTable(); dt.Columns.Add("id", typeof(int)); dt.Columns.Add("name", typeof(string)); }
            DataRow blank = dt.NewRow();
            blank["id"] = 0;
            blank["name"] = "";
            dt.Rows.InsertAt(blank, 0);
            c.DataSource = dt;
            c.DisplayMember = "name";
            c.ValueMember = "id";
            c.SelectedValue = 0;
            return c;
        }

        // ---------------- data ----------------
        private void LoadMarriage(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM marriages WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];

            txtReg.Text = Str(r["registry_no"]);
            txtBook.Text = Str(r["book_volume"]);
            SetCombo(cboStatus, r["status"]);
            SetDate(dtpDate, r["date_of_marriage"]);
            txtTime.Text = Str(r["time_of_marriage"]);
            txtSolemnizer.Text = Str(r["solemnizer"]);

            LoadSpouse(_h, r, "husband");
            LoadSpouse(_w, r, "wife");

            SetFk(cboChurch, r["church_id"]);
            SetFk(cboMuni, r["place_municipality_id"]);
            SetFk(cboProv, r["place_province_id"]);

            _scanImage = dt.Columns.Contains("scan_image") && r["scan_image"] != DBNull.Value
                ? (byte[])r["scan_image"] : null;
        }

        private void LoadSpouse(Spouse sp, DataRow r, string p)
        {
            sp.First.Text = Str(r[p + "_first_name"]);
            sp.Middle.Text = Str(r[p + "_middle_name"]);
            sp.Last.Text = Str(r[p + "_last_name"]);
            sp.Age.Text = Str(r[p + "_age"]);
            SetOptionalDate(sp.Dob, r[p + "_date_of_birth"]);
            SetFk(sp.Citizenship, r[p + "_citizenship_id"]);
            SetFk(sp.Religion, r[p + "_religion_id"]);
            SetCombo(sp.CivilStatus, r[p + "_civil_status"]);
            SetFk(sp.Residence, r[p + "_residence_id"]);
        }

        private void Save()
        {
            if (!ValidateNames()) return;
            string status = cboStatus.SelectedItem?.ToString() ?? "Registered";
            if (status != "Draft" && string.IsNullOrWhiteSpace(txtReg.Text))
                txtReg.Text = NextRegNo();
            try
            {
                long newId = Db.Insert(
                    "INSERT INTO marriages (" + Columns + ") VALUES (" + Placeholders + ")",
                    FieldParams());
                SaveScan(newId);
                Audit.Write(Audit.Create, "marriages", txtReg.Text,
                    _h.Last.Text.Trim() + " & " + _w.Last.Text.Trim());
                MessageBox.Show("Marriage registered." +
                    (string.IsNullOrWhiteSpace(txtReg.Text) ? "" : "  Registry No: " + txtReg.Text),
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void UpdateRecord()
        {
            if (_editingId == null) return;
            if (!ValidateNames()) return;
            string status = cboStatus.SelectedItem?.ToString() ?? "Registered";
            if (status != "Draft" && string.IsNullOrWhiteSpace(txtReg.Text))
                txtReg.Text = NextRegNo();
            var ps = new List<MySqlParameter>(FieldParams()) { new MySqlParameter("@id", _editingId.Value) };
            try
            {
                Db.Push("UPDATE marriages SET " + SetClause + " WHERE id = @id", ps.ToArray());
                SaveScan(_editingId.Value);
                Audit.Write(Audit.Update, "marriages", _editingId.Value,
                    _h.Last.Text.Trim() + " & " + _w.Last.Text.Trim());
                MessageBox.Show("Marriage record updated.", "Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void Delete()
        {
            if (_editingId == null) return;
            if (MessageBox.Show("Delete this marriage record?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM marriages WHERE id = @id",
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "marriages", _editingId.Value, null);
                MessageBox.Show("Record deleted.", "Deleted",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("This record is referenced elsewhere and can't be deleted.",
                    "In use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Fail(ex); }
        }

        // ---- shared SQL ----
        private const string Columns =
            "registry_no, book_volume, status, solemnizer, " +
            "husband_first_name, husband_middle_name, husband_last_name, " +
            "wife_first_name, wife_middle_name, wife_last_name, " +
            "husband_age, husband_date_of_birth, wife_age, wife_date_of_birth, " +
            "husband_citizenship_id, wife_citizenship_id, husband_religion_id, wife_religion_id, " +
            "husband_civil_status, wife_civil_status, husband_residence_id, wife_residence_id, " +
            "church_id, place_municipality_id, place_province_id, date_of_marriage, time_of_marriage";

        private const string Placeholders =
            "@reg, @book, @status, @sol, @hf, @hm, @hl, @wf, @wm, @wl, @hage, @hdob, @wage, @wdob, " +
            "@hcit, @wcit, @hrel, @wrel, @hciv, @wciv, @hres, @wres, @church, @muni, @prov, @dom, @tom";

        private const string SetClause =
            "registry_no=@reg, book_volume=@book, status=@status, solemnizer=@sol, " +
            "husband_first_name=@hf, husband_middle_name=@hm, husband_last_name=@hl, " +
            "wife_first_name=@wf, wife_middle_name=@wm, wife_last_name=@wl, " +
            "husband_age=@hage, husband_date_of_birth=@hdob, wife_age=@wage, wife_date_of_birth=@wdob, " +
            "husband_citizenship_id=@hcit, wife_citizenship_id=@wcit, husband_religion_id=@hrel, " +
            "wife_religion_id=@wrel, husband_civil_status=@hciv, wife_civil_status=@wciv, " +
            "husband_residence_id=@hres, wife_residence_id=@wres, church_id=@church, " +
            "place_municipality_id=@muni, place_province_id=@prov, date_of_marriage=@dom, time_of_marriage=@tom";

        private MySqlParameter[] FieldParams()
        {
            return new[]
            {
                new MySqlParameter("@reg", Nz(txtReg.Text)),
                new MySqlParameter("@book", Nz(txtBook.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),
                new MySqlParameter("@sol", Nz(txtSolemnizer.Text)),
                new MySqlParameter("@hf", _h.First.Text.Trim()),
                new MySqlParameter("@hm", Nz(_h.Middle.Text)),
                new MySqlParameter("@hl", _h.Last.Text.Trim()),
                new MySqlParameter("@wf", _w.First.Text.Trim()),
                new MySqlParameter("@wm", Nz(_w.Middle.Text)),
                new MySqlParameter("@wl", _w.Last.Text.Trim()),
                new MySqlParameter("@hage", IntVal(_h.Age)),
                new MySqlParameter("@hdob", DateVal(_h.Dob)),
                new MySqlParameter("@wage", IntVal(_w.Age)),
                new MySqlParameter("@wdob", DateVal(_w.Dob)),
                new MySqlParameter("@hcit", FkVal(_h.Citizenship)),
                new MySqlParameter("@wcit", FkVal(_w.Citizenship)),
                new MySqlParameter("@hrel", FkVal(_h.Religion)),
                new MySqlParameter("@wrel", FkVal(_w.Religion)),
                new MySqlParameter("@hciv", ComboText(_h.CivilStatus)),
                new MySqlParameter("@wciv", ComboText(_w.CivilStatus)),
                new MySqlParameter("@hres", FkVal(_h.Residence)),
                new MySqlParameter("@wres", FkVal(_w.Residence)),
                new MySqlParameter("@church", FkVal(cboChurch)),
                new MySqlParameter("@muni", FkVal(cboMuni)),
                new MySqlParameter("@prov", FkVal(cboProv)),
                new MySqlParameter("@dom", dtpDate.Value.Date),
                new MySqlParameter("@tom", Nz(txtTime.Text)),
            };
        }

        private bool ValidateNames()
        {
            if (string.IsNullOrWhiteSpace(_h.First.Text) || string.IsNullOrWhiteSpace(_h.Last.Text) ||
                string.IsNullOrWhiteSpace(_w.First.Text) || string.IsNullOrWhiteSpace(_w.Last.Text))
            {
                MessageBox.Show("Husband and wife first + last names are required.",
                    "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static string NextRegNo()
        {
            int year = DateTime.Now.Year;
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM marriages WHERE registry_no LIKE @p",
                new MySqlParameter("@p", year + "-M-%"));
            int next = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value)
                ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-M-{1:D4}", year, next);
        }

        // ---- value helpers ----
        private static object Nz(string s) => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        private static object IntVal(TextBox t) => int.TryParse(t.Text, out int n) ? (object)n : DBNull.Value;
        private static object DateVal(DateTimePicker d) => d.Checked ? (object)d.Value.Date : DBNull.Value;
        private static object ComboText(ComboBox c) => c.SelectedItem == null ? (object)DBNull.Value : c.SelectedItem.ToString();
        private static object FkVal(ComboBox c)
        {
            if (c.SelectedValue == null) return DBNull.Value;
            int id;
            try { id = Convert.ToInt32(c.SelectedValue); } catch { return DBNull.Value; }
            return id == 0 ? (object)DBNull.Value : id;
        }
        private static string Str(object v) => v == null || v == DBNull.Value ? "" : v.ToString();
        private static void SetCombo(ComboBox c, object v) =>
            c.SelectedItem = v == null || v == DBNull.Value ? null : v.ToString();
        private static void SetFk(ComboBox c, object v) =>
            c.SelectedValue = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
        private static void SetDate(DateTimePicker d, object v)
        { if (v != null && v != DBNull.Value) d.Value = Convert.ToDateTime(v); }
        private static void SetOptionalDate(DateTimePicker d, object v)
        {
            if (v != null && v != DBNull.Value) { d.Value = Convert.ToDateTime(v); d.Checked = true; }
            else d.Checked = false;
        }
        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Marriage License application dialog — PSA Municipal Form 90. Create / edit / delete a
    /// row in <c>marriage_licenses</c> (the posting-period tracker). Self-contained code-built
    /// modal (no designer/.resx/.csproj change), opened by <see cref="MarriageRegistrationForm"/>.
    /// The 10-day posting period (Family Code Art. 17) auto-fills Posting Ends = Filed + 10 days
    /// for a new application (editable).
    /// </summary>
    internal sealed class MarriageLicenseForm : Form
    {
        private readonly int? _editingId;
        private TextBox txtLicenseNo, txtHusband, txtWife;
        private DateTimePicker dtpFiled, dtpPosting;
        private ComboBox cboStatus;

        public MarriageLicenseForm(int? licenseId)
        {
            _editingId = licenseId;
            BuildUi();
            if (_editingId != null) LoadLicense(_editingId.Value);
            else
            {
                txtLicenseNo.Text = NextLicenseNo();
                dtpPosting.Value = dtpFiled.Value.Date.AddDays(10);
            }
        }

        private void BuildUi()
        {
            Text = _editingId == null ? "New Marriage License — Form 90" : "Edit Marriage License — Form 90";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 470);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.75F);

            int x = 24, w = 412, y = 20;
            Controls.Add(Head("Marriage License Application (Form 90)", x, y)); y += 44;

            Controls.Add(Cap("License No.", x, y)); y += 20;
            txtLicenseNo = Box(x, y, w); Controls.Add(txtLicenseNo); y += 46;

            Controls.Add(Cap("Husband — Full Name", x, y)); y += 20;
            txtHusband = Box(x, y, w); Controls.Add(txtHusband); y += 46;

            Controls.Add(Cap("Wife — Full Name", x, y)); y += 20;
            txtWife = Box(x, y, w); Controls.Add(txtWife); y += 46;

            Controls.Add(Cap("Filed Date", x, y));
            Controls.Add(Cap("Posting Ends (Filed + 10 days)", x + 210, y)); y += 20;
            dtpFiled = new DateTimePicker { Location = new Point(x, y), Size = new Size(196, 26), Format = DateTimePickerFormat.Short };
            dtpFiled.ValueChanged += (s, e) => { if (_editingId == null) dtpPosting.Value = dtpFiled.Value.Date.AddDays(10); };
            Controls.Add(dtpFiled);
            dtpPosting = new DateTimePicker { Location = new Point(x + 210, y), Size = new Size(202, 26), Format = DateTimePickerFormat.Short };
            Controls.Add(dtpPosting); y += 46;

            Controls.Add(Cap("Status", x, y)); y += 20;
            cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y), Size = new Size(196, 26), Font = new Font("Segoe UI", 10F) };
            cboStatus.Items.AddRange(new object[] { "Posting", "Issued" });
            cboStatus.SelectedItem = "Posting";
            Controls.Add(cboStatus); y += 52;

            var save = Btn(_editingId == null ? "Save License" : "Update", x, y, Color.FromArgb(25, 135, 84), 130);
            save.Click += (s, e) => Save();
            Controls.Add(save);
            if (_editingId != null)
            {
                var del = Btn("Delete", x + 142, y, Color.FromArgb(220, 53, 69), 100);
                del.Click += (s, e) => Delete();
                Controls.Add(del);
            }
            var cancel = Btn("Cancel", x + 282, y, Color.FromArgb(108, 117, 125), 130);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
            AcceptButton = save;
        }

        // ---- control factories ----
        private static Label Head(string t, int x, int y) => new Label
        { Text = t, AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41), Location = new Point(x, y) };
        private static Label Cap(string t, int x, int y) => new Label
        { Text = t, AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(108, 117, 125), Location = new Point(x, y) };
        private static TextBox Box(int x, int y, int w) => new TextBox
        { Location = new Point(x, y), Size = new Size(w, 26), Font = new Font("Segoe UI", 10F) };
        private static Button Btn(string t, int x, int y, Color back, int w) => new Button
        {
            Text = t, Location = new Point(x, y), Size = new Size(w, 40), FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White, BackColor = back, Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        // ---- data ----
        private void LoadLicense(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM marriage_licenses WHERE id = @id",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            txtLicenseNo.Text = SafeStr(r["license_no"]);
            txtHusband.Text = SafeStr(r["husband_name"]);
            txtWife.Text = SafeStr(r["wife_name"]);
            if (r["filed_date"] != DBNull.Value) dtpFiled.Value = Convert.ToDateTime(r["filed_date"]);
            if (r["posting_ends"] != DBNull.Value) dtpPosting.Value = Convert.ToDateTime(r["posting_ends"]);
            cboStatus.SelectedItem = SafeStr(r["status"]);
            if (cboStatus.SelectedIndex < 0) cboStatus.SelectedItem = "Posting";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtHusband.Text) || string.IsNullOrWhiteSpace(txtWife.Text))
            {
                MessageBox.Show("Enter both the husband's and wife's names.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLicenseNo.Text)) txtLicenseNo.Text = NextLicenseNo();

            var ps = new[]
            {
                new MySqlParameter("@no", txtLicenseNo.Text.Trim()),
                new MySqlParameter("@h", txtHusband.Text.Trim()),
                new MySqlParameter("@w", txtWife.Text.Trim()),
                new MySqlParameter("@filed", dtpFiled.Value.Date),
                new MySqlParameter("@ends", dtpPosting.Value.Date),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Posting"),
            };
            try
            {
                if (_editingId == null)
                {
                    Db.Push("INSERT INTO marriage_licenses (license_no, husband_name, wife_name, filed_date, posting_ends, status) " +
                            "VALUES (@no, @h, @w, @filed, @ends, @status)", ps);
                    Audit.Write(Audit.Create, "marriage_licenses", null, "License " + txtLicenseNo.Text.Trim());
                }
                else
                {
                    var up = new List<MySqlParameter>(ps) { new MySqlParameter("@id", _editingId.Value) };
                    Db.Push("UPDATE marriage_licenses SET license_no=@no, husband_name=@h, wife_name=@w, " +
                            "filed_date=@filed, posting_ends=@ends, status=@status WHERE id=@id", up.ToArray());
                    Audit.Write(Audit.Update, "marriage_licenses", _editingId.Value, "License " + txtLicenseNo.Text.Trim());
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Delete()
        {
            if (MessageBox.Show("Delete this marriage license?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM marriage_licenses WHERE id=@id",
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "marriage_licenses", _editingId.Value, null);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not delete: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Next license number for the year as YYYY-L-#### (max sequence + 1, delete-safe).</summary>
        private static string NextLicenseNo()
        {
            int year = DateTime.Now.Year;
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(license_no, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM marriage_licenses WHERE license_no LIKE @p",
                new MySqlParameter("@p", year + "-L-%"));
            int next = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value) ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-L-{1:D4}", year, next);
        }

        private static string SafeStr(object v) => v == null || v == DBNull.Value ? "" : v.ToString();
    }
}
