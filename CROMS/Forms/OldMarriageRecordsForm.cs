using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Old, already-registered marriage records digitized (or hand-transcribed) from the
    /// paper registry books — committed straight to <c>marriages</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Marriage Registration (Form 97) screen. That screen is for a solemnization actually
    /// being registered today; this one is a full Add / View / Edit / Delete workbench over
    /// the backlog alone, so a decades-old registry-book entry never clutters — and never
    /// needs — the live Form 97 workflow (licence linking, OCR review, PSA endorsement,
    /// etc. all belong to a marriage being registered NOW, not one being typed in from a
    /// 1980s ledger).
    ///
    /// Layout mirrors Old Birth/Death Records' own card/list &lt;-&gt; card/entry pattern:
    /// a full-width list card with a search box and a grid, and — once a row is opened —
    /// a full-width entry card with the record's fields grouped into tabs (Registration /
    /// Husband / Wife / Marriage Details).
    /// </summary>
    public partial class OldMarriageRecordsForm : Form, IRefreshable
    {
        // which book is currently opened (null = the "(no volume recorded)" group)
        private string _bookVolRaw;
        private string _bookVolDisplay;
        private bool _bookIsNullGroup;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;
        // True for a digitized (OCR-Backlog) record and a brand-new one typed here; a record
        // registered at the counter is shown but view-only (edited in Marriage Registration).
        private bool _isBacklog = true;

        private const string BacklogSource = "OCR-Backlog";

        public OldMarriageRecordsForm()
        {
            Text = "Marriage Record";
            InitializeComponent();

            FieldLimit.Cap(3, txtHAge, txtWAge);
            // Boxes stop at the width of the column they save into (read from the database).
            FieldLimit.FromDb("marriages",
                "book_page", txtBookPage,
                "book_volume", txtBookVol,
                "digitized_by", txtDigitizedBy,
                "encoding_method", txtEncodingMethod,
                "husband_age", txtHAge,
                "husband_civil_status", txtHCivil,
                "husband_first_name", txtHFirst,
                "husband_last_name", txtHLast,
                "husband_middle_name", txtHMiddle,
                "husband_place_of_birth", txtHPlace,
                "place_of_marriage", txtPlaceOfMarriage,
                "registry_no", txtReg,
                "remarks", txtRemarks,
                "solemnizer", txtSolemnizer,
                "source_reference", txtSourceRef,
                "time_of_marriage", txtMarriageTime,
                "wife_age", txtWAge,
                "wife_civil_status", txtWCivil,
                "wife_first_name", txtWFirst,
                "wife_last_name", txtWLast,
                "wife_middle_name", txtWMiddle,
                "wife_place_of_birth", txtWPlace);
            LoadBooks();
        }

        public void RefreshData()
        {
            if (cardEntry.Visible) return; // don't yank focus off an open record
            if (cardList.Visible) LoadGrid();
            else LoadBooks();
        }

        /// <summary>
        /// Cross-module hand-off: after a Marriage Record Digitization commit/draft, land
        /// here on the record's own book (creating the book's card the moment this
        /// re-queries, if it's brand new) with the record itself opened — Step 11's
        /// "Result After Saving".
        /// </summary>
        public void OpenToRecord(long id)
        {
            DataTable dt = Db.Pull(
                "SELECT book_volume FROM marriages WHERE id = @id",
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
        /// into `marriages` tagged OCR-Backlog in legacy digitization mode and asks before
        /// creating a brand-new registry book) or Manual Entry (Step 5, the blank entry form
        /// already on this screen). Neither path is a Marriage Registration.
        /// </summary>
        private void StartDigitizeWizard()
        {
            using (var dlg = new DigitizeChoiceForm("Marriage"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.UseOcr)
                {
                    OcrDigitizationForm.RunLegacyDigitization(this, DocKind.Marriage);
                    LoadBooks();
                }
                else
                {
                    _bookVolRaw = null;
                    _bookVolDisplay = null;
                    _bookIsNullGroup = true;
                    ClearForm();
                    SetMode(view: false);
                    lblEntryTitle.Text = "New old marriage record";
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

        private const string PickType = "Marriage";

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
            string name = (Cell("Husband") + "  &  " + Cell("Wife")).Trim();
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
            lblEntryTitle.Text = "New old marriage record";
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
                RecordFullDetail.Show(this, "v_marriage_certificate", id, "Marriage Record — Full Details");
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
            RecordFullDetail.Show(this, "v_marriage_certificate", _editingId.Value,
                "Marriage Record — " + (lblEntryTitle.Text ?? ""));
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
                if (col.Name == "Husband" || col.Name == "Wife") { col.MinimumWidth = 220; col.FillWeight = 230; }
                if (col.Name == "Registry No.") { col.MinimumWidth = 140; col.FillWeight = 130; }
                if (col.Name == "Date of Marriage")
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
                "SELECT id, registry_no AS 'Registry No.', " +
                "TRIM(CONCAT(husband_last_name,', ',husband_first_name)) AS Husband, " +
                "TRIM(CONCAT(wife_last_name,', ',wife_first_name)) AS Wife, " +
                "date_of_marriage AS 'Date of Marriage', book_volume AS 'Book', book_page AS 'Page', " +
                "status AS Status, " +
                "CASE WHEN record_source = '" + BacklogSource + "' THEN 'Digitized' ELSE 'Registered' END AS Source " +
                "FROM marriages WHERE 1 = 1";
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
                sql += " AND (registry_no LIKE @t OR husband_first_name LIKE @t OR husband_last_name LIKE @t " +
                       "OR wife_first_name LIKE @t OR wife_last_name LIKE @t)";
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
            DataTable dt = Db.Pull("SELECT * FROM marriages WHERE id = @id",
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

            txtHFirst.Text = Str(r, "husband_first_name");
            txtHMiddle.Text = Str(r, "husband_middle_name");
            txtHLast.Text = Str(r, "husband_last_name");
            SetCombo(cboHSex, dt.Columns.Contains("husband_sex") ? Str(r, "husband_sex") : "");
            txtHAge.Text = Str(r, "husband_age");
            SetDate(dtpHDob, r["husband_date_of_birth"]);
            txtHPlace.Text = dt.Columns.Contains("husband_place_of_birth") ? Str(r, "husband_place_of_birth") : "";
            txtHCivil.Text = Str(r, "husband_civil_status");

            txtWFirst.Text = Str(r, "wife_first_name");
            txtWMiddle.Text = Str(r, "wife_middle_name");
            txtWLast.Text = Str(r, "wife_last_name");
            SetCombo(cboWSex, dt.Columns.Contains("wife_sex") ? Str(r, "wife_sex") : "");
            txtWAge.Text = Str(r, "wife_age");
            SetDate(dtpWDob, r["wife_date_of_birth"]);
            txtWPlace.Text = dt.Columns.Contains("wife_place_of_birth") ? Str(r, "wife_place_of_birth") : "";
            txtWCivil.Text = Str(r, "wife_civil_status");

            SetDate(dtpMarriage, r["date_of_marriage"]);
            txtMarriageTime.Text = Str(r, "time_of_marriage");
            txtPlaceOfMarriage.Text = dt.Columns.Contains("place_of_marriage") ? Str(r, "place_of_marriage") : "";
            txtSolemnizer.Text = Str(r, "solemnizer");
            txtRemarks.Text = dt.Columns.Contains("remarks") ? Str(r, "remarks") : "";

            // Guarded: migration 71 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = (txtHLast.Text + " & " + txtWLast.Text).Trim(new[] { ' ', '&' });
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
                txtReg, txtBookVol, txtBookPage,
                txtHFirst, txtHMiddle, txtHLast, txtHAge, txtHPlace, txtHCivil,
                txtWFirst, txtWMiddle, txtWLast, txtWAge, txtWPlace, txtWCivil,
                txtMarriageTime, txtPlaceOfMarriage, txtSolemnizer, txtRemarks })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboHSex.SelectedIndex = -1;
            cboWSex.SelectedIndex = -1;
            foreach (DateTimePicker dtp in new[] { dtpHDob, dtpWDob, dtpMarriage })
                dtp.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtHFirst.Text) || string.IsNullOrWhiteSpace(txtHLast.Text) ||
                string.IsNullOrWhiteSpace(txtWFirst.Text) || string.IsNullOrWhiteSpace(txtWLast.Text))
            {
                MessageBox.Show("Enter at least the husband's and wife's first and last names.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),

                new MySqlParameter("@hfn", txtHFirst.Text.Trim()),
                new MySqlParameter("@hmn", NullIfEmpty(txtHMiddle.Text)),
                new MySqlParameter("@hln", txtHLast.Text.Trim()),
                new MySqlParameter("@hsex", (object)cboHSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@hage", int.TryParse(txtHAge.Text, out int ha) ? (object)ha : DBNull.Value),
                new MySqlParameter("@hdob", dtpHDob.Checked ? (object)dtpHDob.Value.Date : DBNull.Value),
                new MySqlParameter("@hplace", NullIfEmpty(txtHPlace.Text)),
                new MySqlParameter("@hcivil", NullIfEmpty(txtHCivil.Text)),

                new MySqlParameter("@wfn", txtWFirst.Text.Trim()),
                new MySqlParameter("@wmn", NullIfEmpty(txtWMiddle.Text)),
                new MySqlParameter("@wln", txtWLast.Text.Trim()),
                new MySqlParameter("@wsex", (object)cboWSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@wage", int.TryParse(txtWAge.Text, out int wa) ? (object)wa : DBNull.Value),
                new MySqlParameter("@wdob", dtpWDob.Checked ? (object)dtpWDob.Value.Date : DBNull.Value),
                new MySqlParameter("@wplace", NullIfEmpty(txtWPlace.Text)),
                new MySqlParameter("@wcivil", NullIfEmpty(txtWCivil.Text)),

                new MySqlParameter("@mdate", dtpMarriage.Checked ? (object)dtpMarriage.Value.Date : DBNull.Value),
                new MySqlParameter("@mtime", NullIfEmpty(txtMarriageTime.Text)),
                new MySqlParameter("@mplace", NullIfEmpty(txtPlaceOfMarriage.Text)),
                new MySqlParameter("@solemnizer", NullIfEmpty(txtSolemnizer.Text)),
                new MySqlParameter("@remarks", NullIfEmpty(txtRemarks.Text)),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: this screen is hand-transcription with no scan behind it —
                // encoding_method states that plainly, so it is never mistaken for an
                // OCR-read record on a later audit.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO marriages (registry_no, book_volume, book_page, status, " +
                    "husband_first_name, husband_middle_name, husband_last_name, husband_sex, husband_age, " +
                    "husband_date_of_birth, husband_place_of_birth, husband_civil_status, " +
                    "wife_first_name, wife_middle_name, wife_last_name, wife_sex, wife_age, " +
                    "wife_date_of_birth, wife_place_of_birth, wife_civil_status, " +
                    "date_of_marriage, time_of_marriage, place_of_marriage, solemnizer, remarks, " +
                    "scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, " +
                    "@hfn, @hmn, @hln, @hsex, @hage, @hdob, @hplace, @hcivil, " +
                    "@wfn, @wmn, @wln, @wsex, @wage, @wdob, @wplace, @wcivil, " +
                    "@mdate, @mtime, @mplace, @solemnizer, @remarks, " +
                    "@scan, 'OCR-Backlog', @digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "marriages", id, "Old marriage record added by hand (OCR-Backlog)");
                MessageBox.Show("Old marriage record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
                LoadRecord(id);
                SetMode(view: true);
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE marriages SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "husband_first_name=@hfn, husband_middle_name=@hmn, husband_last_name=@hln, husband_sex=@hsex, " +
                    "husband_age=@hage, husband_date_of_birth=@hdob, husband_place_of_birth=@hplace, husband_civil_status=@hcivil, " +
                    "wife_first_name=@wfn, wife_middle_name=@wmn, wife_last_name=@wln, wife_sex=@wsex, " +
                    "wife_age=@wage, wife_date_of_birth=@wdob, wife_place_of_birth=@wplace, wife_civil_status=@wcivil, " +
                    "date_of_marriage=@mdate, time_of_marriage=@mtime, place_of_marriage=@mplace, " +
                    "solemnizer=@solemnizer, remarks=@remarks " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "marriages", _editingId.Value, "Old marriage record updated (OCR-Backlog)");
                MessageBox.Show("Old marriage record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (MessageBox.Show("Delete this old marriage record? An administrator can restore it later (Settings > Audit Trail > Deleted Records).", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            string reason = CROMS.Modules.ReasonPrompt.Ask(this, "Delete", "this old marriage record");
            if (reason == null) return false;     // cancelled or left blank - nothing is deleted

            try
            {
                // Archived whole (scans included) and removed in one transaction, and only a
                // digitized backlog record - never a registered one - is allowed through here.
                CROMS.Data.RecordRecycle.Delete("marriages", id, reason, "OCR-Backlog");
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
            Audit.Write(Audit.Delete, "marriages", id, "Old marriage record deleted (OCR-Backlog). Reason: " + reason);
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
            SoftcopyViewer.Show(_scanImage, "Old Marriage Record — " + txtHLast.Text + " & " + txtWLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : r[col].ToString();

        private static void SetCombo(ComboBox cbo, string value)
        {
            cbo.SelectedIndex = -1;
            if (string.IsNullOrEmpty(value)) return;
            for (int i = 0; i < cbo.Items.Count; i++)
                if (string.Equals(cbo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                { cbo.SelectedIndex = i; return; }
            // A registered record can carry a status the short list lacks (e.g. "Pending
            // Approval"); show it rather than a blank box.
            cbo.SelectedIndex = cbo.Items.Add(value);
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
    }
}
