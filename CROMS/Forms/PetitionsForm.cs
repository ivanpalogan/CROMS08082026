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
    /// Petitions (RA 9048 / RA 10172) — tracks correction-of-entry and change-of-first-name
    /// petitions through their legal stages: Filed → Posted → Decision → PSA Endorsement.
    /// Left: the petitions list (with the linked record's name resolved). Right: create /
    /// edit a petition and advance its stage. UI is declared in PetitionsForm.Designer.cs so
    /// every control is visible/editable on the design canvas; this file holds only the data
    /// access + event handlers. Backed by the `petitions` table.
    /// </summary>
    public partial class PetitionsForm : Form, IRefreshable
    {
        private int? _editingId;

        // enum code <-> friendly label maps (DB stores the codes). Order MUST match the
        // cboType / cboStage item order set in the Designer.
        private static readonly string[] TypeCodes = { "RA9048", "RA10172" };
        private static readonly string[] StageCodes = { "Filed", "Posted", "Decision", "PSA_Endorsement" };
        private static readonly string[] StageLabels = { "Filed", "Posted", "Decision", "PSA Endorsement" };

        public PetitionsForm()
        {
            InitializeComponent();
            LoadGrid();
        }

        public void RefreshData() => LoadGrid();

        // ------------------------------------------------------------ event handlers
        private void cboRecordType_SelectedIndexChanged(object sender, EventArgs e) => LoadRecords();
        private void btnSave_Click(object sender, EventArgs e) => Save();
        private void btnAdvance_Click(object sender, EventArgs e) => AdvanceStage();
        private void btnNew_Click(object sender, EventArgs e) => ClearForm();
        private void btnDelete_Click(object sender, EventArgs e) => Delete();

        // ---------------------------------------------------------------- data
        private void LoadGrid()
        {
            grid.DataSource = Db.Pull(
                "SELECT p.id, " +
                "CASE p.petition_type WHEN 'RA9048' THEN 'RA 9048' WHEN 'RA10172' THEN 'RA 10172' " +
                "  ELSE p.petition_type END AS Type, " +
                "CASE p.record_type " +
                "  WHEN 'Birth'    THEN (SELECT TRIM(CONCAT(last_name,', ',first_name)) FROM births b WHERE b.id = p.record_id) " +
                "  WHEN 'Death'    THEN (SELECT full_name FROM deaths d WHERE d.id = p.record_id) " +
                "  WHEN 'Marriage' THEN (SELECT TRIM(CONCAT(husband_last_name,' & ',wife_last_name)) FROM marriages m WHERE m.id = p.record_id) " +
                "  ELSE '—' END AS Record, " +
                "REPLACE(p.stage,'_',' ') AS Stage, p.filed_date AS Filed, p.remarks AS Remarks " +
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
            lblSel.Text = "Editing Petition #" + id;

            cboType.SelectedIndex = Array.IndexOf(TypeCodes, Str(r["petition_type"]));
            cboRecordType.SelectedItem = Str(r["record_type"]);   // triggers LoadRecords
            if (r["record_id"] != DBNull.Value) cboRecord.SelectedValue = Convert.ToInt32(r["record_id"]);
            cboStage.SelectedIndex = Math.Max(0, Array.IndexOf(StageCodes, Str(r["stage"])));
            if (r["filed_date"] != DBNull.Value) dtpFiled.Value = Convert.ToDateTime(r["filed_date"]);
            txtRemarks.Text = Str(r["remarks"]);
        }

        private void Save()
        {
            if (cboType.SelectedIndex < 0 || cboRecordType.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a petition type and a record type.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            object recordId = cboRecord.SelectedValue is int rid ? (object)rid : DBNull.Value;
            var ps = new[]
            {
                new MySqlParameter("@pt", TypeCodes[cboType.SelectedIndex]),
                new MySqlParameter("@rt", cboRecordType.SelectedItem.ToString()),
                new MySqlParameter("@rid", recordId),
                new MySqlParameter("@stage", StageCodes[Math.Max(0, cboStage.SelectedIndex)]),
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
                    Audit.Write(Audit.Create, "petitions", null,
                        TypeCodes[cboType.SelectedIndex] + " on " + cboRecordType.SelectedItem);
                    MessageBox.Show("Petition filed.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var up = new List<MySqlParameter>(ps) { new MySqlParameter("@id", _editingId.Value) };
                    Db.Push("UPDATE petitions SET petition_type=@pt, record_type=@rt, record_id=@rid, " +
                            "stage=@stage, filed_date=@filed, remarks=@remarks WHERE id=@id", up.ToArray());
                    Audit.Write(Audit.Update, "petitions", _editingId.Value, null);
                    MessageBox.Show("Petition updated.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        /// <summary>Moves the selected petition to the next legal stage.</summary>
        private void AdvanceStage()
        {
            if (_editingId == null)
            {
                MessageBox.Show("Pick a petition from the list first.", "Advance Stage",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int cur = Math.Max(0, cboStage.SelectedIndex);
            if (cur >= StageCodes.Length - 1)
            {
                MessageBox.Show("This petition is already at the final stage (PSA Endorsement).",
                    "Advance Stage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string next = StageCodes[cur + 1];
            try
            {
                Db.Push("UPDATE petitions SET stage=@s WHERE id=@id",
                    new MySqlParameter("@s", next),
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Update, "petitions", _editingId.Value, "Stage → " + StageLabels[cur + 1]);
                cboStage.SelectedIndex = cur + 1;
                MessageBox.Show("Stage advanced to: " + StageLabels[cur + 1], "Advance Stage",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void Delete()
        {
            if (_editingId == null)
            {
                MessageBox.Show("Pick a petition from the list first.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete this petition?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM petitions WHERE id=@id", new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "petitions", _editingId.Value, null);
                MessageBox.Show("Petition deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void ClearForm()
        {
            _editingId = null;
            lblSel.Text = "New Petition";
            cboType.SelectedIndex = -1;
            cboRecordType.SelectedIndex = -1;
            cboRecord.DataSource = null;
            cboStage.SelectedIndex = 0;
            dtpFiled.Value = DateTime.Today;
            txtRemarks.Clear();
        }

        private static string Str(object v) => v == null || v == DBNull.Value ? "" : v.ToString();
        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
