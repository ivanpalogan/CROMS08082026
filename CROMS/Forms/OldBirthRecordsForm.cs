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
    /// Old, already-registered birth records digitized through Intelligent Document
    /// Processing (OCR) — committed straight to <c>births</c> tagged
    /// <c>record_source = 'OCR-Backlog'</c>, and managed here instead of on the live
    /// Birth Registration screen. That screen is for today's walk-in registrations; this one
    /// is a full Add / View / Edit / Delete workbench over the backlog alone, so an old
    /// scanned record never clutters — and never needs — the live registration form.
    ///
    /// Layout mirrors Birth Registration's own card/list <-> card/entry pattern: a full-width
    /// list card with a search box and a grid, and — once a row is opened — a full-width
    /// entry card with the record's fields grouped into tabs (Registration / Child / Mother /
    /// Father / Remarks), the same grouping the live form uses.
    /// </summary>
    public partial class OldBirthRecordsForm : Form, IRefreshable
    {
        // which book is currently opened (null = the "(no volume recorded)" group)
        private string _bookVolRaw;
        private string _bookVolDisplay;
        private bool _bookIsNullGroup;

        private long? _editingId;
        private byte[] _scanImage;
        private bool _viewOnly;
        // True for a record digitized from the old books (record_source 'OCR-Backlog') and for a
        // brand-new one typed here. A record registered at the counter is shown but stays
        // view-only: it is edited in Birth Registration, which owns its workflow and audit.
        private bool _isBacklog = true;

        private const string BacklogSource = "OCR-Backlog";

        public OldBirthRecordsForm()
        {
            Text = "Birth Record";
            InitializeComponent();
            LoadBooks();
        }

        public void RefreshData()
        {
            if (cardEntry.Visible) return; // don't yank focus off an open record
            if (cardList.Visible) LoadGrid();
            else LoadBooks();
        }

        /// <summary>
        /// Cross-module hand-off: after a Birth Record Digitization commit/draft/update,
        /// land here on the record's own book (creating the book's card the moment this
        /// re-queries, if it's brand new) with the record itself opened — Step 11's
        /// "Result After Saving".
        /// </summary>
        public void OpenToRecord(long id)
        {
            DataTable dt = Db.Pull(
                "SELECT book_volume FROM births WHERE id = @id",
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
        /// with OCR (Step 4, reuses the existing Intelligent Document Processing screen,
        /// which already writes straight into `births` tagged OCR-Backlog and asks before
        /// creating a brand-new registry book) or Manual Entry (Step 5, the blank entry form
        /// already on this screen). Neither path is a Birth Registration.
        /// </summary>
        private void StartDigitizeWizard()
        {
            using (var dlg = new DigitizeChoiceForm("Birth"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.UseOcr)
                {
                    OcrDigitizationForm.RunLegacyDigitization(this, DocKind.Birth);
                    LoadBooks(); // Step 11 — refresh regardless, in case a record was committed
                }
                else
                {
                    _bookVolRaw = null;
                    _bookVolDisplay = null;
                    _bookIsNullGroup = true;
                    ClearForm();
                    SetMode(view: false);
                    lblEntryTitle.Text = "New old birth record";
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








        // ---- event handlers wired in the Designer ----------------------------------

        private void btnBackToBooks_Click(object sender, EventArgs e) => ShowBooksGallery();
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
            if (e.RowIndex >= 0) OpenSelected(view: true);
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
            lblListHeader.Text = "Book " + (_bookVolDisplay ?? "(no volume recorded)");
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
            // pre-fill with the book this record is being added under, unless the
            // opened group is the "no volume recorded" bucket — nothing to prefill there.
            if (!_bookIsNullGroup && !string.IsNullOrEmpty(_bookVolRaw)) txtBookVol.Text = _bookVolRaw;
            SetMode(view: false);
            lblEntryTitle.Text = "New old birth record";
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
            RecordFullDetail.Show(this, "v_birth_certificate", _editingId.Value,
                "Birth Record — " + (lblEntryTitle.Text ?? ""));
        }

        private void EnterEditMode() => SetMode(view: false);

        private void CancelEdit()
        {
            if (_editingId != null) { LoadRecord(_editingId.Value); SetMode(view: true); }
            else ShowListView();
        }

        // ---- data ---------------------------------------------------------------

        private void LoadGrid()
        {
            string term = (txtSearch?.Text ?? "").Trim();
            string sql =
                "SELECT id, registry_no AS 'Registry No.', " +
                "CONCAT_WS(' ', first_name, middle_name, last_name) AS 'Child Name', " +
                "sex AS Sex, date_of_birth AS 'Date of Birth', book_volume AS 'Book', book_page AS 'Page', " +
                "status AS Status, " +
                "CASE WHEN record_source = '" + BacklogSource + "' THEN 'Digitized' ELSE 'Registered' END AS Source " +
                "FROM births WHERE 1 = 1";
            var ps = new List<MySqlParameter>();
            sql += _bookIsNullGroup ? " AND book_volume IS NULL" : " AND book_volume = @vol";
            if (!_bookIsNullGroup) ps.Add(new MySqlParameter("@vol", _bookVolRaw));
            if (term.Length > 0)
            {
                sql += " AND (registry_no LIKE @t OR first_name LIKE @t OR middle_name LIKE @t OR last_name LIKE @t)";
                ps.Add(new MySqlParameter("@t", "%" + term + "%"));
            }
            sql += " ORDER BY book_page, id DESC";

            DataTable dt = ps.Count > 0 ? Db.Pull(sql, ps.ToArray()) : Db.Pull(sql);
            dgv.DataSource = dt;
            if (dgv.Columns.Contains("id")) dgv.Columns["id"].Visible = false;
            lblListHeader.Text = "Book " + (_bookVolDisplay ?? "(no volume recorded)") + "  ·  " + dt.Rows.Count + " record(s)";
            UpdateListButtons();
        }

        private void LoadRecord(long id)
        {
            DataTable dt = Db.Pull("SELECT * FROM births WHERE id = @id",
                new MySqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            _isBacklog = dt.Columns.Contains("record_source") && Str(r, "record_source") == BacklogSource;
            _scanImage = r["scan_image"] == DBNull.Value ? null : (byte[])r["scan_image"];

            txtReg.Text = Str(r, "registry_no");
            txtBookVol.Text = Str(r, "book_volume");
            txtBookPage.Text = Str(r, "book_page");
            SetCombo(cboStatus, Str(r, "status"));
            txtFirst.Text = Str(r, "first_name");
            txtMiddle.Text = Str(r, "middle_name");
            txtLast.Text = Str(r, "last_name");
            SetCombo(cboSex, Str(r, "sex"));
            SetDate(dtpDob, r["date_of_birth"]);
            txtTob.Text = Str(r, "time_of_birth");
            txtPlace.Text = Str(r, "place_of_birth");
            txtTypeOfBirth.Text = Str(r, "type_of_birth");
            txtWeight.Text = Str(r, "weight_grams");
            txtMFirst.Text = Str(r, "mother_first_name");
            txtMMiddle.Text = Str(r, "mother_middle_name");
            txtMLast.Text = Str(r, "mother_last_name");
            txtMOcc.Text = Str(r, "mother_occupation");
            txtMRel.Text = Str(r, "mother_religion");
            txtMCit.Text = Str(r, "mother_citizenship");
            txtFFirst.Text = Str(r, "father_first_name");
            txtFMiddle.Text = Str(r, "father_middle_name");
            txtFLast.Text = Str(r, "father_last_name");
            txtFOcc.Text = Str(r, "father_occupation");
            txtFRel.Text = Str(r, "father_religion");
            txtFCit.Text = Str(r, "father_citizenship");
            txtRemarks.Text = Str(r, "remarks");

            // Guarded: migration 70 may not be applied yet on every database.
            txtDigitizedBy.Text = dt.Columns.Contains("digitized_by") ? Str(r, "digitized_by") : "";
            txtDateDigitized.Text = dt.Columns.Contains("date_digitized") && r["date_digitized"] != DBNull.Value
                ? Convert.ToDateTime(r["date_digitized"]).ToString("MMM d, yyyy h:mm tt") : "";
            txtEncodingMethod.Text = dt.Columns.Contains("encoding_method") ? Str(r, "encoding_method") : "";
            txtSourceRef.Text = dt.Columns.Contains("source_reference") ? Str(r, "source_reference") : "";

            lblEntryTitle.Text = (txtFirst.Text + " " + txtLast.Text).Trim();
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
                txtReg, txtBookVol, txtBookPage, txtFirst, txtMiddle, txtLast, txtTob, txtPlace,
                txtTypeOfBirth, txtWeight, txtMFirst, txtMMiddle, txtMLast, txtMOcc, txtMRel, txtMCit,
                txtFFirst, txtFMiddle, txtFLast, txtFOcc, txtFRel, txtFCit, txtRemarks })
                if (c is TextBox tb) tb.Clear();
            cboStatus.SelectedIndex = -1;
            cboSex.SelectedIndex = -1;
            dtpDob.Checked = false;

            var u = Session.User;
            txtDigitizedBy.Text = u != null ? (u.FullName ?? u.Username) : "";
            txtDateDigitized.Text = "(on save)";
            txtEncodingMethod.Text = "Manual";
            txtSourceRef.Text = "";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Enter at least the child's first and last name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ps = new List<MySqlParameter>
            {
                new MySqlParameter("@reg", NullIfEmpty(txtReg.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtBookVol.Text)),
                new MySqlParameter("@bookpage", NullIfEmpty(txtBookPage.Text)),
                new MySqlParameter("@status", cboStatus.SelectedItem?.ToString() ?? "Registered"),
                new MySqlParameter("@fn", txtFirst.Text.Trim()),
                new MySqlParameter("@mn", NullIfEmpty(txtMiddle.Text)),
                new MySqlParameter("@ln", txtLast.Text.Trim()),
                new MySqlParameter("@sex", (object)cboSex.SelectedItem ?? DBNull.Value),
                new MySqlParameter("@dob", dtpDob.Checked ? (object)dtpDob.Value.Date : DBNull.Value),
                new MySqlParameter("@tob", NullIfEmpty(txtTob.Text)),
                new MySqlParameter("@place", NullIfEmpty(txtPlace.Text)),
                new MySqlParameter("@btype", NullIfEmpty(txtTypeOfBirth.Text)),
                new MySqlParameter("@weight", int.TryParse(txtWeight.Text, out int w) ? (object)w : DBNull.Value),
                new MySqlParameter("@mf", NullIfEmpty(txtMFirst.Text)),
                new MySqlParameter("@mm", NullIfEmpty(txtMMiddle.Text)),
                new MySqlParameter("@ml", NullIfEmpty(txtMLast.Text)),
                new MySqlParameter("@mocc", NullIfEmpty(txtMOcc.Text)),
                new MySqlParameter("@mrel", NullIfEmpty(txtMRel.Text)),
                new MySqlParameter("@mcit", NullIfEmpty(txtMCit.Text)),
                new MySqlParameter("@ff", NullIfEmpty(txtFFirst.Text)),
                new MySqlParameter("@fm", NullIfEmpty(txtFMiddle.Text)),
                new MySqlParameter("@fl", NullIfEmpty(txtFLast.Text)),
                new MySqlParameter("@focc", NullIfEmpty(txtFOcc.Text)),
                new MySqlParameter("@frel", NullIfEmpty(txtFRel.Text)),
                new MySqlParameter("@fcit", NullIfEmpty(txtFCit.Text)),
                new MySqlParameter("@remarks", NullIfEmpty(txtRemarks.Text)),
            };

            if (_editingId == null)
            {
                ps.Add(new MySqlParameter("@img", MySqlDbType.LongBlob)
                    { Value = _scanImage == null ? (object)DBNull.Value : _scanImage });
                // Step 10: this screen is hand-transcription with no scan behind it —
                // encoding_method states that plainly, so it is never mistaken for an
                // OCR-read record on a later audit.
                ps.Add(new MySqlParameter("@digby", DigitizedBy()));
                ps.Add(new MySqlParameter("@digdate", DateTime.Now));
                ps.Add(new MySqlParameter("@encmethod", "Manual"));
                long id = Db.Insert(
                    "INSERT INTO births (registry_no, book_volume, book_page, status, first_name, middle_name, " +
                    "last_name, sex, date_of_birth, time_of_birth, place_of_birth, type_of_birth, weight_grams, " +
                    "mother_first_name, mother_middle_name, mother_last_name, mother_occupation, mother_religion, mother_citizenship, " +
                    "father_first_name, father_middle_name, father_last_name, father_occupation, father_religion, father_citizenship, " +
                    "remarks, scan_image, record_source, digitized_by, date_digitized, encoding_method) " +
                    "VALUES (@reg, @book, @bookpage, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, @btype, @weight, " +
                    "@mf, @mm, @ml, @mocc, @mrel, @mcit, @ff, @fm, @fl, @focc, @frel, @fcit, @remarks, @img, 'OCR-Backlog', " +
                    "@digby, @digdate, @encmethod)",
                    ps.ToArray());
                Audit.Write(Audit.Create, "births", id, "Old birth record added by hand (OCR-Backlog)");
                MessageBox.Show("Old birth record saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Step 11 — result after saving: back to Birth Records (the books gallery),
                // reloaded so the record's book (new or existing) shows its updated count.
                LoadBooks();
            }
            else
            {
                ps.Add(new MySqlParameter("@id", _editingId.Value));
                Db.Push(
                    "UPDATE births SET registry_no=@reg, book_volume=@book, book_page=@bookpage, status=@status, " +
                    "first_name=@fn, middle_name=@mn, last_name=@ln, sex=@sex, date_of_birth=@dob, time_of_birth=@tob, " +
                    "place_of_birth=@place, type_of_birth=@btype, weight_grams=@weight, " +
                    "mother_first_name=@mf, mother_middle_name=@mm, mother_last_name=@ml, mother_occupation=@mocc, " +
                    "mother_religion=@mrel, mother_citizenship=@mcit, " +
                    "father_first_name=@ff, father_middle_name=@fm, father_last_name=@fl, father_occupation=@focc, " +
                    "father_religion=@frel, father_citizenship=@fcit, remarks=@remarks " +
                    "WHERE id = @id AND record_source = 'OCR-Backlog'",
                    ps.ToArray());
                Audit.Write(Audit.Update, "births", _editingId.Value, "Old birth record updated (OCR-Backlog)");
                MessageBox.Show("Old birth record updated.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Step 11 — same result-after-saving path: the edit may have moved the
                // record to a different book, so land back on the gallery, not the old list.
                LoadBooks();
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
            if (MessageBox.Show("Delete this old birth record? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;

            Db.Push("DELETE FROM births WHERE id = @id AND record_source = 'OCR-Backlog'",
                new MySqlParameter("@id", id));
            Audit.Write(Audit.Delete, "births", id, "Old birth record deleted (OCR-Backlog)");
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
            SoftcopyViewer.Show(_scanImage, "Old Birth Record — " + txtFirst.Text + " " + txtLast.Text, this);
        }

        // ---- helpers ---------------------------------------------------------

        private static string Str(DataRow r, string col) => r[col] == DBNull.Value ? "" : r[col].ToString();

        private static void SetCombo(ComboBox cbo, string value)
        {
            cbo.SelectedIndex = -1;
            for (int i = 0; i < cbo.Items.Count; i++)
                if (string.Equals(cbo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                { cbo.SelectedIndex = i; return; }
            // A registered record can carry a status the short digitization list lacks
            // (e.g. "Pending Approval"); show it rather than a blank box.
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
    }
}
