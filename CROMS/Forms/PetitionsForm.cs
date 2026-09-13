using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Petitions (RA 9048 / RA 10172) plus the four TRACK-ONLY case types from backlog Phase 4 —
    /// Legitimation (RA 9858), Supplemental Report, Legal Instrument (RA 9255 Acknowledgment /
    /// AUSF) and Court Order annotation. One generic tracker, not four near-copies, per the
    /// backlog's own build-order note — researched (see Database/44_case_tracking_types.sql) and
    /// the stage shape is the same for all six: Filed → the LCR reviews it → it is registered/
    /// annotated → endorsed to PSA. The RA correction petitions keep their statutory 15-day
    /// 'Posted' step; the four track-only types have no posting period and use 'Under Review'
    /// instead. CROMS does not perform the legal procedure for the four new types (no
    /// requirements checklist, no posting-clock engine like the marriage licence or delayed
    /// birth registration) — it only tracks where a case the LCRO is handling stands.
    /// Left: the case list (with the linked record's name resolved). Right: create / edit a
    /// case and advance its stage. UI is declared in PetitionsForm.Designer.cs so every control
    /// is visible/editable on the design canvas; this file holds only the data access + event
    /// handlers. Backed by the `petitions` table.
    /// </summary>
    public partial class PetitionsForm : Form, IRefreshable
    {
        private int? _editingId;

        // enum code <-> friendly label maps (DB stores the codes). Order MUST match the
        // cboType item order set in the Designer.
        private static readonly string[] TypeCodes =
            { "RA9048", "RA10172", "Legitimation", "SupplementalReport", "LegalInstrument", "CourtOrder" };

        // RA9048/RA10172 carry a real statutory 15-day public-posting requirement; the other
        // four case types have no posting period, so they get "Under Review" in its place.
        private static readonly string[] StageCodesRA = { "Filed", "Posted", "Decision", "PSA_Endorsement" };
        private static readonly string[] StageLabelsRA = { "Filed", "Posted", "Decision", "PSA Endorsement" };
        private static readonly string[] StageCodesTrack = { "Filed", "UnderReview", "Decision", "PSA_Endorsement" };
        private static readonly string[] StageLabelsTrack = { "Filed", "Under Review", "Decision", "PSA Endorsement" };

        private static bool IsRa(string typeCode) => typeCode == "RA9048" || typeCode == "RA10172";
        private static string[] StageCodesFor(string typeCode) => IsRa(typeCode) ? StageCodesRA : StageCodesTrack;
        private static string[] StageLabelsFor(string typeCode) => IsRa(typeCode) ? StageLabelsRA : StageLabelsTrack;

        public PetitionsForm()
        {
            InitializeComponent();
            LoadGrid();
        }

        public void RefreshData() => LoadGrid();

        // ------------------------------------------------------------ event handlers
        private void cboRecordType_SelectedIndexChanged(object sender, EventArgs e) => LoadRecords();

        /// <summary>Swaps the Stage list to whichever sequence applies to the newly chosen
        /// case type (Posted for RA petitions, Under Review for the track-only types). Fires
        /// from both a user pick and LoadPetition setting cboType.SelectedIndex.</summary>
        private void cboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboType.SelectedIndex < 0) return;
            RepopulateStage(TypeCodes[cboType.SelectedIndex], 0);
        }

        private void RepopulateStage(string typeCode, int index)
        {
            var labels = StageLabelsFor(typeCode);
            cboStage.Items.Clear();
            cboStage.Items.AddRange(labels);
            cboStage.SelectedIndex = Math.Min(Math.Max(index, 0), labels.Length - 1);
        }

        private void btnSave_Click(object sender, EventArgs e) => Save();
        private void btnAdvance_Click(object sender, EventArgs e) => AdvanceStage();
        private void btnNew_Click(object sender, EventArgs e) => ClearForm();
        private void btnDelete_Click(object sender, EventArgs e) => Delete();

        // ---------------------------------------------------------------- data
        private void LoadGrid()
        {
            grid.DataSource = Db.Pull(
                "SELECT p.id, " +
                "CASE p.petition_type " +
                "  WHEN 'RA9048' THEN 'RA 9048' WHEN 'RA10172' THEN 'RA 10172' " +
                "  WHEN 'Legitimation' THEN 'Legitimation (RA 9858)' " +
                "  WHEN 'SupplementalReport' THEN 'Supplemental Report' " +
                "  WHEN 'LegalInstrument' THEN 'Legal Instrument' " +
                "  WHEN 'CourtOrder' THEN 'Court Order' " +
                "  ELSE p.petition_type END AS Type, " +
                "CASE p.record_type " +
                "  WHEN 'Birth'    THEN (SELECT TRIM(CONCAT(last_name,', ',first_name)) FROM births b WHERE b.id = p.record_id) " +
                "  WHEN 'Death'    THEN (SELECT full_name FROM deaths d WHERE d.id = p.record_id) " +
                "  WHEN 'Marriage' THEN (SELECT TRIM(CONCAT(husband_last_name,' & ',wife_last_name)) FROM marriages m WHERE m.id = p.record_id) " +
                "  ELSE '—' END AS Record, " +
                "CASE p.stage " +
                "  WHEN 'UnderReview' THEN 'Under Review' WHEN 'PSA_Endorsement' THEN 'PSA Endorsement' " +
                "  ELSE p.stage END AS Stage, " +
                "p.filed_date AS Filed, p.remarks AS Remarks " +
                "FROM petitions p ORDER BY p.id DESC");
            if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
        }

        /// <summary>Fills the Record dropdown with records of the chosen type.</summary>
        private void LoadRecords()
        {
            string type = cboRecordType.SelectedItem?.ToString();
            string sql;
            switch (type)
            {
                case "Birth":
                    sql = "SELECT id, TRIM(CONCAT(last_name, ', ', first_name)) AS name FROM births ORDER BY last_name"; break;
                case "Death":
                    sql = "SELECT id, full_name AS name FROM deaths ORDER BY full_name"; break;
                case "Marriage":
                    sql = "SELECT id, TRIM(CONCAT(husband_last_name, ' & ', wife_last_name)) AS name FROM marriages ORDER BY id DESC"; break;
                default:
                    cboRecord.DataSource = null; return;
            }
            DataTable dt = Db.Pull(sql);
            cboRecord.DataSource = dt;
            cboRecord.DisplayMember = "name";
            cboRecord.ValueMember = "id";
            cboRecord.SelectedIndex = -1;
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            object idCell = grid.Rows[e.RowIndex].Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            LoadPetition(Convert.ToInt32(idCell));
        }

        private void LoadPetition(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM petitions WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;
            lblSel.Text = "Editing Case #" + id;

            string typeCode = Str(r["petition_type"]);
            cboType.SelectedIndex = Array.IndexOf(TypeCodes, typeCode);   // triggers RepopulateStage(...,0)
            cboRecordType.SelectedItem = Str(r["record_type"]);            // triggers LoadRecords
            if (r["record_id"] != DBNull.Value) cboRecord.SelectedValue = Convert.ToInt32(r["record_id"]);
            RepopulateStage(typeCode, Math.Max(0, Array.IndexOf(StageCodesFor(typeCode), Str(r["stage"]))));
            if (r["filed_date"] != DBNull.Value) dtpFiled.Value = Convert.ToDateTime(r["filed_date"]);
            txtRemarks.Text = Str(r["remarks"]);
        }

        private void Save()
        {
            if (cboType.SelectedIndex < 0 || cboRecordType.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a case type and a record type.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string typeCode = TypeCodes[cboType.SelectedIndex];
            object recordId = cboRecord.SelectedValue is int rid ? (object)rid : DBNull.Value;
            var ps = new[]
            {
                new MySqlParameter("@pt", typeCode),
                new MySqlParameter("@rt", cboRecordType.SelectedItem.ToString()),
                new MySqlParameter("@rid", recordId),
                new MySqlParameter("@stage", StageCodesFor(typeCode)[Math.Max(0, cboStage.SelectedIndex)]),
                new MySqlParameter("@filed", dtpFiled.Value.Date),
                new MySqlParameter("@remarks", string.IsNullOrWhiteSpace(txtRemarks.Text)
                    ? (object)DBNull.Value : txtRemarks.Text.Trim()),
            };
            try
            {
                if (_editingId == null)
                {
                    Db.Push("INSERT INTO petitions (petition_type, record_type, record_id, stage, filed_date, remarks) " +
                            "VALUES (@pt, @rt, @rid, @stage, @filed, @remarks)", ps);
                    Audit.Write(Audit.Create, "petitions", null, typeCode + " on " + cboRecordType.SelectedItem);
                    MessageBox.Show("Case filed.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var up = new List<MySqlParameter>(ps) { new MySqlParameter("@id", _editingId.Value) };
                    Db.Push("UPDATE petitions SET petition_type=@pt, record_type=@rt, record_id=@rid, " +
                            "stage=@stage, filed_date=@filed, remarks=@remarks WHERE id=@id", up.ToArray());
                    Audit.Write(Audit.Update, "petitions", _editingId.Value, null);
                    MessageBox.Show("Case updated.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        /// <summary>Moves the selected case to the next stage of ITS OWN sequence (RA petitions
        /// go through Posted; the four track-only case types go through Under Review instead).</summary>
        private void AdvanceStage()
        {
            if (_editingId == null || cboType.SelectedIndex < 0)
            {
                MessageBox.Show("Pick a case from the list first.", "Advance Stage",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string typeCode = TypeCodes[cboType.SelectedIndex];
            string[] codes = StageCodesFor(typeCode);
            string[] labels = StageLabelsFor(typeCode);
            int cur = Math.Max(0, cboStage.SelectedIndex);
            if (cur >= codes.Length - 1)
            {
                MessageBox.Show("This case is already at the final stage (PSA Endorsement).",
                    "Advance Stage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string next = codes[cur + 1];
            try
            {
                Db.Push("UPDATE petitions SET stage=@s WHERE id=@id",
                    new MySqlParameter("@s", next),
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Update, "petitions", _editingId.Value, "Stage → " + labels[cur + 1]);
                cboStage.SelectedIndex = cur + 1;
                MessageBox.Show("Stage advanced to: " + labels[cur + 1], "Advance Stage",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void Delete()
        {
            if (_editingId == null)
            {
                MessageBox.Show("Pick a case from the list first.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete this case?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM petitions WHERE id=@id", new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "petitions", _editingId.Value, null);
                MessageBox.Show("Case deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void ClearForm()
        {
            _editingId = null;
            lblSel.Text = "New Case";
            cboType.SelectedIndex = -1;
            cboRecordType.SelectedIndex = -1;
            cboRecord.DataSource = null;
            RepopulateStage("RA9048", 0);   // neutral default list until a type is chosen
            dtpFiled.Value = DateTime.Today;
            txtRemarks.Clear();
        }

        private static string Str(object v) => v == null || v == DBNull.Value ? "" : v.ToString();
        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
