using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Old, already-registered death records digitized through Intelligent Document
    /// Processing (OCR) — committed straight to <c>deaths</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Death Registration screen. That screen is for today's walk-in registrations; this one
    /// is a full Add / View / Edit / Delete workbench over the backlog alone.
    ///
    /// Layout mirrors Birth/Death Registration's own card/list <-> card/entry pattern: a
    /// full-width list card with a search box and a grid, and — once a row is opened — a
    /// full-width entry card with the record's fields grouped into tabs (Registration /
    /// Deceased / Cause &amp; Disposal / Informant / Certification), the same grouping the
    /// live forms use.
    /// </summary>
    public partial class OldDeathRecordsForm : Form, IRefreshable
    {
        // which book is currently opened (null = the "(no volume recorded)" group)
        private string _bookVolRaw;
        private string _bookVolDisplay;
        private bool _bookIsNullGroup;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;
        // True for a digitized (OCR-Backlog) record and a brand-new one typed here; a record
        // registered at the counter is shown but view-only (edited in Death Registration).
        private bool _isBacklog = true;

        private const string BacklogSource = "OCR-Backlog";

        public OldDeathRecordsForm()
        {
            Text = "Death Record";
            InitializeComponent();

            FieldLimit.Cap(3, txtAge);
            // Boxes stop at the width of the column they save into (read from the database).
            FieldLimit.FromDb("deaths",
                "age", txtAge,
                "antecedent_cause", txtAntecedent,
                "book_page", txtBookPage,
                "book_volume", txtBookVol,
                "citizenship", txtCitizenship,
                "civil_status", txtCivil,
                "digitized_by", txtDigitizedBy,
                "disposal_method", txtDisposal,
                "encoding_method", txtEncodingMethod,
                "first_name", txtFirst,
                "immediate_cause", txtImmediate,
                "informant_address", txtInfAddr,
                "informant_name", txtInfName,
                "informant_relationship", txtInfRel,
                "last_name", txtLast,
                "middle_name", txtMiddle,
                "place_of_death", txtPlace,
                "place_of_disposal", txtDisposalPlace,
                "prepared_by", txtPrepName,
                "prepared_by_title", txtPrepTitle,
                "received_by", txtRecvName,
                "received_by_title", txtRecvTitle,
                "registered_by", txtRegByName,
                "registered_by_title", txtRegByTitle,
                "registry_no", txtReg,
                "religion_name", txtReligion,
                "source_reference", txtSourceRef,
                "underlying_cause", txtUnderlying);
            LoadBooks();
        }

        public void RefreshData()
        {
            if (cardEntry.Visible) return; // don't yank focus off an open record
            if (cardList.Visible) LoadGrid();
            else LoadBooks();
        }

        /// <summary>
        /// Cross-module hand-off: after a Death Record Digitization commit/draft, land here
        /// on the record's own book (creating the book's card the moment this re-queries, if
        /// it's brand new) with the record itself opened — Step 11's "Result After Saving".
        /// </summary>
        public void OpenToRecord(long id)
        {
            DataTable dt = Db.Pull(
                "SELECT book_volume FROM deaths WHERE id = @id",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            object volObj = dt.Rows[0]["book_volume"];
            string volRaw = volObj == DBNull.Value ? null : volObj.ToString();
            string volDisplay = string.IsNullOrEmpty(volRaw) ? "(no volume recorded)" : volRaw;

            LoadBooks();
            OpenBook(volRaw, volDisplay);
            OpenSelected(id, view: true);
        }

        // ---- UI: shell ---------------------------------------------------------


        // ---- UI: books gallery (top level) ---------------------------------------


        private void LoadBooks()
        {
            _gallery.Reload();
            ShowBooksGallery();
        }

        /// <summary>
        /// Step 3 — "+ Digitize Old Record" on the Registry Books gallery. Offers Scan/Upload
        /// with OCR (Step 4, reuses Intelligent Document Processing, which writes straight
        /// into `deaths` tagged OCR-Backlog and asks before creating a brand-new registry
        /// book) or Manual Entry (Step 5, the blank entry form already on this screen).
        /// Neither path is a Death Registration.
        /// </summary>
        private void StartDigitizeWizard()
        {
            using (var dlg = new DigitizeChoiceForm("Death"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.UseOcr)
                {
                    OcrDigitizationForm.RunLegacyDigitization(this, DocKind.Death);
                    LoadBooks();
                }
                else
                {
                    _bookVolRaw = null;
                    _bookVolDisplay = null;
                    _bookIsNullGroup = true;
                    ClearForm();
                    SetMode(view: false);
                    lblEntryTitle.Text = "New old death record";
                    lblEntrySub.Text = "Not yet saved — type the Registry Number, Book/Volume and " +
                                        "Page from the ledger on the Registration tab";
                    ShowEntryView();
                }
            }
        }

        private void OpenBook(string volRaw, string volDisplay)
        {
            _bookVolRaw = volRaw;
            _bookVolDisplay = volDisplay;
            _bookIsNullGroup = volRaw == null;
            txtSearch.Text = "";
            ShowListView();
            LoadGrid();
        }

        // ---- UI: list card ---------------------------------------------------------


        private void UpdateListButtons()
        {
            bool has = dgv.CurrentRow != null;
            bool backlog = has && SelectedIsBacklog();
            btnViewFromList.Enabled = has;
            btnUsePick.Enabled = has;
            btnListSoftcopy.Enabled = has;
            btnEditFromList.Enabled = backlog;
            btnDeleteFromList.Enabled = backlog;
        }

        private bool SelectedIsBacklog()
        {
            if (dgv.CurrentRow == null || !dgv.Columns.Contains("Source")) return false;
            return string.Equals(Convert.ToString(dgv.CurrentRow.Cells["Source"].Value), "Digitized",
                StringComparison.Ordinal);
        }

        private long? SelectedId()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt64(dgv.CurrentRow.Cells["id"].Value);
        }

        // ---- UI: entry card ---------------------------------------------------------








        // ---- pick mode: Certificate Request > Find Record sends the clerk here ----------------
        // The clerk searches with this section's own search bar — across EVERY book, not just
        // one — and picks a record; the callback hands it back to the request form.

        private Action<int, string> _pickCallback;
        private Action _pickCancelled;
        private bool _allBooks;
        private RecordCriteria _criteria;   // what the client already told the kiosk

        private const string PickType = "Death";

        public bool PickMode => _pickCallback != null;

        private string PickHeader() => "Choose the record for the certificate request — all books";

        public void BeginPick(RecordCriteria criteria, Action<int, string> onPicked, Action onCancelled)
        {
            _pickCallback = onPicked;
            _pickCancelled = onCancelled;
            _allBooks = true;
            _bookVolRaw = null;
            _bookVolDisplay = null;
            _bookIsNullGroup = false;
            ApplyPickChrome();
            _criteria = criteria;
            txtSearch.Text = "";
            RecordMatch.Cue(txtSearch, RecordMatch.Applies(PickType, criteria)
                ? "Client's matches shown — type to search wider"
                : "Search every book by name or registry number");
            ShowListView();
            LoadGrid();
            txtSearch.Focus();
            txtSearch.SelectAll();
        }

        private void ApplyPickChrome()
        {
            bool pick = PickMode;
            btnUsePick.Visible = pick;
            btnNewFromList.Visible = !pick;
            btnEditFromList.Visible = !pick;
            btnDeleteFromList.Visible = !pick;
            btnBackToBooks.Text = pick ? "← Cancel" : "← Books";
        }

        private void EndPick()
        {
            _pickCallback = null;
            _pickCancelled = null;
            _allBooks = false;
            _criteria = null;
            ApplyPickChrome();
            RecordMatch.Cue(txtSearch, "");
            txtSearch.Text = "";
            ShowBooksGallery();
        }

        private void CancelPick()
        {
            Action cancelled = _pickCancelled;
            EndPick();
            if (cancelled != null) cancelled();
        }

        /// <summary>
        /// Pick mode with the client's kiosk details: tries the tightest match first (registry
        /// number, then every name given, then same surname / sounds alike) and shows the first
        /// level that finds anything - a handful of likely records, not the whole register.
        /// Anything typed in the search bar replaces this with a plain search.
        /// </summary>
        private void LoadCriteriaMatches(string baseSql)
        {
            DataTable dt = null;
            int used = RecordMatch.LevelSurname;
            foreach (int level in RecordMatch.Levels(PickType, _criteria))
            {
                string where = RecordMatch.Where(PickType, _criteria, level);
                if (where == null) continue;
                string sql = baseSql + " AND " + where +
                             " ORDER BY " + RecordMatch.OrderBy(PickType, _criteria) + " LIMIT 100";
                dt = Db.Pull(sql, RecordMatch.Params(PickType, _criteria).ToArray());
                used = level;
                if (dt.Rows.Count > 0) break;
            }
            if (dt == null) dt = new DataTable();

            dgv.DataSource = dt;
            if (dgv.Columns.Contains("id")) dgv.Columns["id"].Visible = false;
            string summary = _criteria.Summary(PickType);
            lblListHeader.Text = dt.Rows.Count == 0
                ? "No record matches what the client asked for (" + summary + ") — type in the search bar to search wider."
                : "The client asked for: " + summary + "  —  " + RecordMatch.Describe(used) +
                  "  ·  " + dt.Rows.Count + " record(s)";
            UpdateListButtons();
        }

        private string Cell(string column)
        {
            if (dgv.CurrentRow == null || !dgv.Columns.Contains(column)) return "";
            return Convert.ToString(dgv.CurrentRow.Cells[column].Value) ?? "";
        }

        private void ChoosePick()
        {
            long? id = SelectedId();
            if (id == null) return;
            string name = Cell("Deceased");
            string reg = Cell("Registry No.");
            string label = reg.Length > 0 ? name + "  (" + reg + ")" : name;
            Action<int, string> picked = _pickCallback;
            EndPick();
            if (picked != null) picked((int)id.Value, label);
        }

        // ---- event handlers wired in the Designer ----------------------------------

        private void btnBackToBooks_Click(object sender, EventArgs e)
        {
            if (PickMode) CancelPick();      // "← Cancel": back to Certificate Request, nothing chosen
            else ShowBooksGallery();
        }
        private void btnUsePick_Click(object sender, EventArgs e) => ChoosePick();
        private void txtSearch_TextChanged(object sender, EventArgs e) => LoadGrid();
        private void btnNewFromList_Click(object sender, EventArgs e) => OpenNew();
        private void btnViewFromList_Click(object sender, EventArgs e) => OpenSelected(view: true);
        private void btnEditFromList_Click(object sender, EventArgs e) => OpenSelected(view: false);
        private void btnDeleteFromList_Click(object sender, EventArgs e) => DeleteFromList();
        private void btnRefresh_Click(object sender, EventArgs e) => LoadGrid();
        private void dgv_SelectionChanged(object sender, EventArgs e) => UpdateListButtons();
        private void btnBack_Click(object sender, EventArgs e) => ShowListView();
        private void btnEdit_Click(object sender, EventArgs e) => EnterEditMode();
        private void btnSave_Click(object sender, EventArgs e) => Save();
        private void btnCancel_Click(object sender, EventArgs e) => CancelEdit();
        private void btnDeleteEntry_Click(object sender, EventArgs e) => DeleteFromEntry();
        private void btnSoftcopy_Click(object sender, EventArgs e) => ShowSoftcopy();
        private void dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (PickMode) ChoosePick();
            else OpenSelected(view: true);
        }
        private void _gallery_BookOpened(RegistryBookGallery.BookInfo b) => OpenBook(b.VolRaw, b.VolDisplay);

        // ---- view switching ---------------------------------------------------------

        private void ShowBooksGallery()
        {
            cardList.Visible = false;
            cardEntry.Visible = false;
            cardBooks.Visible = true;
        }

        private void ShowListView()
        {
            cardBooks.Visible = false;
            cardEntry.Visible = false;
            cardList.Visible = true;
            lblListHeader.Text = _allBooks ? PickHeader() : "Book " + (_bookVolDisplay ?? "(no volume recorded)");
        }

        private void ShowEntryView()
        {
            cardBooks.Visible = false;
            cardList.Visible = false;
            cardEntry.Visible = true;
        }

        private void OpenNew()
        {
            ClearForm();
            if (!_bookIsNullGroup && !string.IsNullOrEmpty(_bookVolRaw)) txtBookVol.Text = _bookVolRaw;
            SetMode(view: false);
            lblEntryTitle.Text = "New old death record";
            lblEntrySub.Text = "Not yet saved";
            ShowEntryView();
        }

        private void OpenSelected(bool view)
        {
            long? id = SelectedId();
            if (id == null) return;
            OpenSelected(id.Value, view);
        }

        private void OpenSelected(long id, bool view)
        {
            if (view)
            {
                RecordFullDetail.Show(this, "v_death_certificate", id, "Death Record — Full Details");
                return;
            }
            LoadRecord(id);
            SetMode(view);
            ShowEntryView();
        }

        private void SetMode(bool view)
        {
            _viewOnly = view;
            foreach (var c in _inputs) c.Enabled = !view;
            btnEdit.Visible = view && _isBacklog;
            btnSave.Visible = !view;
            btnCancel.Visible = !view && _editingId != null;
            btnDeleteEntry.Visible = view && _isBacklog;
            btnFullRecord.Visible = _editingId != null;
        }

        private void btnFullRecord_Click(object sender, EventArgs e)
        {
            if (_editingId == null) return;
            RecordFullDetail.Show(this, "v_death_certificate", _editingId.Value,
                "Death Record — " + (lblEntryTitle.Text ?? ""));
        }

        private void EnterEditMode() => SetMode(view: false);

        private void CancelEdit()
        {
            if (_editingId != null) { LoadRecord(_editingId.Value); SetMode(view: true); }
            else ShowListView();
        }

        // ---- data ---------------------------------------------------------------

        private void StyleArchiveTable()
        {
            dgv.ColumnHeadersHeight = 46;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.DefaultCellStyle.Padding = new Padding(12, 8, 12, 8);
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (DataGridViewRow row in dgv.Rows) row.MinimumHeight = 46;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name == "id") { col.Visible = false; continue; }
                col.MinimumWidth = col.Name == "Status" || col.Name == "Source" ? 120 : 90;
                col.FillWeight = 85;
                if (col.Name == "Deceased") { col.MinimumWidth = 220; col.FillWeight = 230; }
                if (col.Name == "Registry No.") { col.MinimumWidth = 140; col.FillWeight = 130; }
                if (col.Name == "Date of Death")
                {
                    col.MinimumWidth = 140;
                    col.FillWeight = 130;
                    col.DefaultCellStyle.Format = "dd MMM yyyy";
                }
            }
        }
        private void LoadGrid()
        {
            string term = (txtSearch?.Text ?? "").Trim();
            string sql =
                "SELECT id, registry_no AS 'Registry No.', full_name AS 'Deceased', sex AS Sex, " +
                "date_of_death AS 'Date of Death', book_volume AS 'Book', book_page AS 'Page', " +
                "status AS Status, " +
                "CASE WHEN record_source = '" + BacklogSource + "' THEN 'Digitized' ELSE 'Registered' END AS Source " +
                "FROM deaths WHERE 1 = 1";
            if (_allBooks && term.Length == 0 && RecordMatch.Applies(PickType, _criteria))
            {
                LoadCriteriaMatches(sql);   // the client's own details, until staff type something else
                return;
            }
            var ps = new List<MySqlParameter>();
            if (!_allBooks)   // pick mode searches every book at once
            {
                sql += _bookIsNullGroup ? " AND book_volume IS NULL" : " AND book_volume = @vol";
                if (!_bookIsNullGroup) ps.Add(new MySqlParameter("@vol", _bookVolRaw));
            }
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR full_name LIKE @t)";
                ps.Add(new MySqlParameter("@t", "%" + term + "%"));
            }
            sql += _allBooks ? " ORDER BY id DESC LIMIT 300" : " ORDER BY book_page, id DESC";

            DataTable dt = ps.Count > 0 ? Db.Pull(sql, ps.ToArray()) : Db.Pull(sql);
            dgv.DataSource = dt;
            if (dgv.Columns.Contains("id")) dgv.Columns["id"].Visible = false;
            lblListHeader.Text = (_allBooks ? PickHeader() : "Book " + (_bookVolDisplay ?? "(no volume recorded)"))
                                 + "  ·  " + dt.Rows.Count + " record(s)";
            UpdateListButtons();
        }

        private void LoadRecord(long id)
        {
            DataTable dt = Db.Pull("SELECT * FROM deaths WHERE id = @id",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            _isBacklog = dt.Columns.Contains("record_source") && Str(r, "record_source") == BacklogSource;
            _scanImage = dt.Columns.Contains("scan_image") && r["scan_image"] != DBNull.Value
                ? (byte[])r["scan_image"] : null;

            txtReg.Text = Str(r, "registry_no");
            txtBookVol.Text = Str(r, "book_volume");
            txtBookPage.Text = Str(r, "book_page");
            SetCombo(cboStatus, Str(r, "status"));

            SplitFullName(Str(r, "full_name"), out string first, out string middle, out string last);
            txtFirst.Text = first;
            txtMiddle.Text = middle;
            txtLast.Text = last;

            SetCombo(cboSex, Str(r, "sex"));
            txtCivil.Text = Str(r, "civil_status");
            txtAge.Text = Str(r, "age");
            txtCitizenship.Text = Str(r, "citizenship");
            SetDate(dtpDod, r["date_of_death"]);
            txtPlace.Text = Str(r, "place_of_death");
            txtReligion.Text = Str(r, "religion_name");
            txtImmediate.Text = Str(r, "immediate_cause");
            txtAntecedent.Text = Str(r, "antecedent_cause");
            txtUnderlying.Text = Str(r, "underlying_cause");
            txtDisposal.Text = Str(r, "disposal_method");
            txtDisposalPlace.Text = Str(r, "place_of_disposal");
            SetDate(dtpDisposalDate, r["date_of_disposal"]);
            txtInfName.Text = Str(r, "informant_name");
            txtInfRel.Text = Str(r, "informant_relationship");
            txtInfAddr.Text = Str(r, "informant_address");
            SetDate(dtpInfDate, r["informant_date"]);
            txtPrepName.Text = Str(r, "prepared_by");
            txtPrepTitle.Text = Str(r, "prepared_by_title");
            SetDate(dtpPrepDate, r["prepared_by_date"]);
            txtRecvName.Text = Str(r, "received_by");
            txtRecvTitle.Text = Str(r, "received_by_title");
            SetDate(dtpRecvDate, r["received_by_date"]);
            txtRegByName.Text = Str(r, "registered_by");
            txtRegByTitle.Text = Str(r, "registered_by_title");
            SetDate(dtpRegByDate, r["registered_by_date"]);

            // Guarded: migration 70 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = Str(r, "full_name");
            if (lblEntryTitle.Text.Length == 0) lblEntryTitle.Text = "#" + id;
            lblEntrySub.Text = "Registry No. " + (txtReg.Text.Length > 0 ? txtReg.Text : "(none)") +
                                "  ·  Status: " + (cboStatus.SelectedItem?.ToString() ?? "—") +
                                (_isBacklog ? "" : "  ·  Registered record — view only here");
        }

        private void ClearForm()
        {
            _editingId = null;
            _isBacklog = true;
            _scanImage = null;
            foreach (Control c in new Control[] {
                txtReg, txtBookVol, txtBookPage, txtFirst, txtMiddle, txtLast, txtCivil, txtAge,
                txtCitizenship, txtPlace, txtReligion, txtImmediate, txtAntecedent, txtUnderlying,
                txtDisposal, txtDisposalPlace, txtInfName, txtInfRel, txtInfAddr,
                txtPrepName, txtPrepTitle, txtRecvName, txtRecvTitle, txtRegByName, txtRegByTitle })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboSex.SelectedIndex = -1;
            foreach (DateTimePicker dtp in new[] { dtpDod, dtpDisposalDate, dtpInfDate, dtpPrepDate, dtpRecvDate, dtpRegByDate })
                dtp.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) && string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the deceased's first or last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fullName = string.Join(" ", new[] { txtFirst.Text, txtMiddle.Text, txtLast.Text }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),
                new MySqlParameter("@name", NullIfEmpty(fullName)),
                new MySqlParameter("@sex", (object)cboSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@civil", NullIfEmpty(txtCivil.Text)),
                new MySqlParameter("@age", int.TryParse(txtAge.Text, out int a) ? (object)a : DBNull.Value),
                new MySqlParameter("@cit", NullIfEmpty(txtCitizenship.Text)),
                new MySqlParameter("@dod", dtpDod.Checked ? (object)dtpDod.Value.Date : DBNull.Value),
                new MySqlParameter("@place", NullIfEmpty(txtPlace.Text)),
                new MySqlParameter("@religion", NullIfEmpty(txtReligion.Text)),
                new MySqlParameter("@imm", NullIfEmpty(txtImmediate.Text)),
                new MySqlParameter("@ant", NullIfEmpty(txtAntecedent.Text)),
                new MySqlParameter("@und", NullIfEmpty(txtUnderlying.Text)),
                new MySqlParameter("@disp", NullIfEmpty(txtDisposal.Text)),
                new MySqlParameter("@dplace", NullIfEmpty(txtDisposalPlace.Text)),
                new MySqlParameter("@ddate", dtpDisposalDate.Checked ? (object)dtpDisposalDate.Value.Date : DBNull.Value),
                new MySqlParameter("@iname", NullIfEmpty(txtInfName.Text)),
                new MySqlParameter("@irel", NullIfEmpty(txtInfRel.Text)),
                new MySqlParameter("@iaddr", NullIfEmpty(txtInfAddr.Text)),
                new MySqlParameter("@idate", dtpInfDate.Checked ? (object)dtpInfDate.Value.Date : DBNull.Value),
                new MySqlParameter("@prep", NullIfEmpty(txtPrepName.Text)),
                new MySqlParameter("@preptitle", NullIfEmpty(txtPrepTitle.Text)),
                new MySqlParameter("@prepdate", dtpPrepDate.Checked ? (object)dtpPrepDate.Value.Date : DBNull.Value),
                new MySqlParameter("@recv", NullIfEmpty(txtRecvName.Text)),
                new MySqlParameter("@recvtitle", NullIfEmpty(txtRecvTitle.Text)),
                new MySqlParameter("@recvdate", dtpRecvDate.Checked ? (object)dtpRecvDate.Value.Date : DBNull.Value),
                new MySqlParameter("@regby", NullIfEmpty(txtRegByName.Text)),
                new MySqlParameter("@regbytitle", NullIfEmpty(txtRegByTitle.Text)),
                new MySqlParameter("@regbydate", dtpRegByDate.Checked ? (object)dtpRegByDate.Value.Date : DBNull.Value),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: hand-transcription, no scan behind it — encoding_method says so.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO deaths (registry_no, book_volume, book_page, status, full_name, sex, " +
                    "civil_status, age, citizenship, date_of_death, place_of_death, religion_name, " +
                    "immediate_cause, antecedent_cause, underlying_cause, disposal_method, place_of_disposal, date_of_disposal, " +
                    "informant_name, informant_relationship, informant_address, informant_date, " +
                    "prepared_by, prepared_by_title, prepared_by_date, " +
                    "received_by, received_by_title, received_by_date, " +
                    "registered_by, registered_by_title, registered_by_date, " +
                    "scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, @name, @sex, " +
                    "@civil, @age, @cit, @dod, @place, @religion, " +
                    "@imm, @ant, @und, @disp, @dplace, @ddate, " +
                    "@iname, @irel, @iaddr, @idate, " +
                    "@prep, @preptitle, @prepdate, " +
                    "@recv, @recvtitle, @recvdate, " +
                    "@regby, @regbytitle, @regbydate, " +
                    "@scan, 'OCR-Backlog', @digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "deaths", id, "Old death record added by hand (OCR-Backlog)");
                MessageBox.Show("Old death record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
                SetMode(view: true);
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE deaths SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "full_name=@name, sex=@sex, civil_status=@civil, age=@age, citizenship=@cit, " +
                    "date_of_death=@dod, place_of_death=@place, religion_name=@religion, " +
                    "immediate_cause=@imm, antecedent_cause=@ant, underlying_cause=@und, " +
                    "disposal_method=@disp, place_of_disposal=@dplace, date_of_disposal=@ddate, " +
                    "informant_name=@iname, informant_relationship=@irel, informant_address=@iaddr, informant_date=@idate, " +
                    "prepared_by=@prep, prepared_by_title=@preptitle, prepared_by_date=@prepdate, " +
                    "received_by=@recv, received_by_title=@recvtitle, received_by_date=@recvdate, " +
                    "registered_by=@regby, registered_by_title=@regbytitle, registered_by_date=@regbydate " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "deaths", _editingId.Value, "Old death record updated (OCR-Backlog)");
                MessageBox.Show("Old death record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(_editingId.Value);
                SetMode(view: true);
            }
        }

        private void DeleteFromList()
        {
            long? id = SelectedId();
            if (id == null) return;
            DoDelete(id.Value);
        }

        private void DeleteFromEntry()
        {
            if (_editingId == null) return;
            if (DoDelete(_editingId.Value)) ShowListView();
        }

        private bool DoDelete(long id)
        {
            if (MessageBox.Show("Delete this old death record? An administrator can restore it later (Settings > Audit Trail > Deleted Records).", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            string reason = CROMS.Modules.ReasonPrompt.Ask(this, "Delete", "this old death record");
            if (reason == null) return false;     // cancelled or left blank - nothing is deleted

            try
            {
                // Archived whole (scans included) and removed in one transaction, and only a
                // digitized backlog record - never a registered one - is allowed through here.
                CROMS.Data.RecordRecycle.Delete("deaths", id, reason, "OCR-Backlog");
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("This record is referenced elsewhere and can't be deleted.", "In use",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(CROMS.Data.ErrorLog.Text(ex), "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            Audit.Write(Audit.Delete, "deaths", id, "Old death record deleted (OCR-Backlog). Reason: " + reason);
            LoadGrid();
            return true;
        }

        private void ShowSoftcopy()
        {
            if (_scanImage == null)
            {
                MessageBox.Show("No scanned image saved with this record.", "No softcopy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoftcopyViewer.Show(_scanImage, "Old Death Record — " + txtFirst.Text + " " + txtLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : r[col].ToString();

        private static void SetCombo(ComboBox cbo, string value)
        {
            cbo.SelectedIndex = -1;
            for (int i = 0; i < cbo.Items.Count; i++)
                if (string.Equals(cbo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                { cbo.SelectedIndex = i; return; }
            // A registered record can carry a status the short list lacks (e.g. "Pending
            // Approval"); show it rather than a blank box.
            if (!string.IsNullOrWhiteSpace(value)) cbo.SelectedIndex = cbo.Items.Add(value);
        }

        private static void SetDate(DateTimePicker dtp, object value)
        {
            if (value == null || value == DBNull.Value) { dtp.Checked = false; return; }
            dtp.Value = Convert.ToDateTime(value);
            dtp.Checked = true;
        }

        private static object NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static object DigitizedBy()
        {
            var u = Session.User;
            if (u == null) return DBNull.Value;
            string name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
            return NullIfEmpty(name);
        }

        /// <summary>
        /// `deaths` keeps one joined <c>full_name</c> column, so the screen splits it back
        /// for editing (BR-2026-09-19 convention): two tokens = First/Last with no Middle,
        /// three or more = first token First, last token Last, everything between is
        /// Middle; a single unsplittable token is put whole in Last for the clerk to correct.
        /// </summary>
        private static void SplitFullName(string full, out string first, out string middle, out string last)
        {
            first = middle = last = "";
            if (string.IsNullOrWhiteSpace(full)) return;
            string[] parts = full.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) { last = parts[0]; return; }
            if (parts.Length == 2) { first = parts[0]; last = parts[1]; return; }
            first = parts[0];
            last = parts[parts.Length - 1];
            middle = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
        }
    }
}
