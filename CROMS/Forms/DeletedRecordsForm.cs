using System;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// The log of deleted civil registry records, with a Restore button. Administrators only -
    /// it is opened from Settings, which is itself an admin area, and Restore re-checks the role.
    /// Nothing here deletes anything; <see cref="RecordRecycle"/> already holds every deleted
    /// record whole.
    /// </summary>
    public partial class DeletedRecordsForm : Form
    {
        public DeletedRecordsForm()
        {
            InitializeComponent();
            UiTheme.Polish(this);
            LoadList();
        }

        private void LoadList()
        {
            try
            {
                DataTable raw = RecordRecycle.List(chkRestored.Checked);
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("Type");
                dt.Columns.Add("Registry No.");
                dt.Columns.Add("Record");
                dt.Columns.Add("Deleted by");
                dt.Columns.Add("Deleted on");
                dt.Columns.Add("Reason");
                dt.Columns.Add("Status");
                foreach (DataRow r in raw.Rows)
                {
                    bool back = r["restored_at"] != DBNull.Value;
                    dt.Rows.Add(
                        Convert.ToInt32(r["id"]),
                        RecordRecycle.KindName(Convert.ToString(r["source_table"])),
                        Convert.ToString(r["registry_no"]),
                        Convert.ToString(r["record_label"]),
                        Convert.ToString(r["deleted_by_name"]),
                        Convert.ToDateTime(r["deleted_at"]).ToString("dd MMM yyyy h:mm tt"),
                        Convert.ToString(r["reason"]),
                        back ? "Restored " + Convert.ToDateTime(r["restored_at"]).ToString("dd MMM yyyy") : "Deleted");
                }
                dgvDeleted.DataSource = dt;
                if (dgvDeleted.Columns.Contains("id")) dgvDeleted.Columns["id"].Visible = false;
                // Reason and the record name are the long ones; give them the room.
                var weights = new System.Collections.Generic.Dictionary<string, float>
                    { { "Type", 8 }, { "Registry No.", 15 }, { "Record", 22 }, { "Deleted by", 17 }, { "Deleted on", 19 }, { "Reason", 30 }, { "Status", 13 } };
                foreach (var w in weights) if (dgvDeleted.Columns.Contains(w.Key)) dgvDeleted.Columns[w.Key].FillWeight = w.Value;
                lblCount.Text = dt.Rows.Count == 0
                    ? "No deleted records."
                    : dt.Rows.Count + (dt.Rows.Count == 1 ? " record" : " records");
            }
            catch (Exception ex)
            {
                dgvDeleted.DataSource = null;
                lblCount.Text = "Could not load the deleted records: " + ErrorLog.Reason(ex);
            }
            UpdateRestoreButton();
        }

        private void chkRestored_CheckedChanged(object sender, EventArgs e) { LoadList(); }

        private void dgvDeleted_SelectionChanged(object sender, EventArgs e) { UpdateRestoreButton(); }

        private DataRowView Selected()
        {
            return dgvDeleted.CurrentRow == null ? null : dgvDeleted.CurrentRow.DataBoundItem as DataRowView;
        }

        private void UpdateRestoreButton()
        {
            DataRowView v = Selected();
            btnRestore.Enabled = v != null && Convert.ToString(v["Status"]) == "Deleted";
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (!Session.IsAdmin)
            {
                MessageBox.Show(this, "Only an administrator can restore a deleted record.", "Restore",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DataRowView v = Selected();
            if (v == null || Convert.ToString(v["Status"]) != "Deleted") return;

            string what = Convert.ToString(v["Type"]) + " record " + Convert.ToString(v["Record"]) +
                          (Convert.ToString(v["Registry No."]).Length > 0 ? " (" + v["Registry No."] + ")" : "");
            if (MessageBox.Show(this, "Restore " + what + "?\n\nIt goes back exactly as it was deleted, with its " +
                    "original record and registry number.", "Restore",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                RecordRecycle.Restored r = RecordRecycle.Restore(Convert.ToInt32(v["id"]));
                Audit.Write(Audit.Create, r.Table, r.RecordId,
                    "Restored from Deleted Records: " + r.Label + (string.IsNullOrEmpty(r.RegistryNo) ? "" : " (" + r.RegistryNo + ")"));
                MessageBox.Show(this, "Restored. The record is back in its register.", "Restore",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ErrorLog.Text(ex), "Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            LoadList();
        }
    }
}
