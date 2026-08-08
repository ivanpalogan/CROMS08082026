using System;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Service Windows admin module. Add / rename / activate / deactivate / reorder /
    /// delete the service windows that the Now Serving board and queue assignment use.
    /// Everything is driven from the `windows` table — there is no fixed number of
    /// windows anywhere in the app. A window can only be deleted when no ticket is
    /// currently assigned to it.
    /// <para/>
    /// UI layout lives in WindowManagementForm.Designer.cs; this file holds only the
    /// event handlers, database operations and validation.
    /// </summary>
    public partial class WindowManagementForm : Form, IRefreshable
    {
        public WindowManagementForm()
        {
            InitializeComponent();
            LoadWindows();
        }

        public void RefreshData() => LoadWindows();

        private void LoadWindows()
        {
            dgvWindows.DataSource = Db.Pull(
                "SELECT id AS Id, window_name AS 'Window Name', status AS Status, " +
                "display_order AS 'Order' FROM windows ORDER BY display_order, id");
            if (dgvWindows.Columns.Contains("Id")) dgvWindows.Columns["Id"].Visible = false;
        }

        private bool TryGetSelected(out int id, out string name, out string status, out int order)
        {
            id = 0; name = null; status = null; order = 0;
            if (dgvWindows.CurrentRow == null) { Warn("Select a window first."); return false; }
            id = Convert.ToInt32(dgvWindows.CurrentRow.Cells["Id"].Value);
            name = dgvWindows.CurrentRow.Cells["Window Name"].Value?.ToString();
            status = dgvWindows.CurrentRow.Cells["Status"].Value?.ToString();
            order = Convert.ToInt32(dgvWindows.CurrentRow.Cells["Order"].Value);
            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int nextOrder = Db.GetCount("SELECT id FROM windows") + 1;
            string name = Prompt("New window", "Window name:", "Window " + nextOrder);
            if (string.IsNullOrWhiteSpace(name)) return;

            long id = Db.Insert(
                "INSERT INTO windows (window_name, status, display_order) VALUES (@n, 'Active', @o)",
                new MySqlParameter("@n", name.Trim()),
                new MySqlParameter("@o", nextOrder));
            Audit.Write("Create", "windows", (int)id, "Added window '" + name.Trim() + "'");
            LoadWindows();
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            if (!TryGetSelected(out int id, out string name, out _, out _)) return;
            string newName = Prompt("Rename window", "Window name:", name);
            if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == name) return;

            Db.Push("UPDATE windows SET window_name = @n WHERE id = @id",
                new MySqlParameter("@n", newName.Trim()),
                new MySqlParameter("@id", id));
            Audit.Write("Update", "windows", id, "Renamed to '" + newName.Trim() + "'");
            LoadWindows();
        }

        private void btnToggle_Click(object sender, EventArgs e)
        {
            if (!TryGetSelected(out int id, out string name, out string status, out _)) return;
            string next = status == "Active" ? "Inactive" : "Active";

            if (next == "Inactive" && IsAssigned(id))
            {
                Warn("'" + name + "' is currently serving a ticket. Finish it before deactivating.");
                return;
            }

            Db.Push("UPDATE windows SET status = @s WHERE id = @id",
                new MySqlParameter("@s", next),
                new MySqlParameter("@id", id));
            Audit.Write("Update", "windows", id, "Set " + next);
            LoadWindows();
        }

        private void btnUp_Click(object sender, EventArgs e) => MoveWindow(-1);

        private void btnDown_Click(object sender, EventArgs e) => MoveWindow(1);

        private void MoveWindow(int direction)
        {
            if (!TryGetSelected(out int id, out _, out _, out int order)) return;

            // Find the neighbour in the move direction and swap display_order.
            DataTable nb = Db.Pull(
                "SELECT id, display_order FROM windows WHERE display_order " +
                (direction < 0 ? "< @o ORDER BY display_order DESC" : "> @o ORDER BY display_order ASC") +
                " LIMIT 1", new MySqlParameter("@o", order));
            if (nb.Rows.Count == 0) return;   // already at the edge

            int nbId = Convert.ToInt32(nb.Rows[0]["id"]);
            int nbOrder = Convert.ToInt32(nb.Rows[0]["display_order"]);

            Db.Push("UPDATE windows SET display_order = @o WHERE id = @id",
                new MySqlParameter("@o", nbOrder), new MySqlParameter("@id", id));
            Db.Push("UPDATE windows SET display_order = @o WHERE id = @id",
                new MySqlParameter("@o", order), new MySqlParameter("@id", nbId));
            LoadWindows();

            // Keep the moved row selected.
            foreach (DataGridViewRow r in dgvWindows.Rows)
                if (Convert.ToInt32(r.Cells["Id"].Value) == id) { r.Selected = true; dgvWindows.CurrentCell = r.Cells[1]; break; }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!TryGetSelected(out int id, out string name, out _, out _)) return;
            if (IsAssigned(id))
            {
                Warn("'" + name + "' has a ticket assigned. Reassign / finish it before deleting.");
                return;
            }
            if (MessageBox.Show("Delete '" + name + "'? This cannot be undone.", "Delete window",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            Db.Push("DELETE FROM windows WHERE id = @id", new MySqlParameter("@id", id));
            Audit.Write("Delete", "windows", id, "Deleted window '" + name + "'");
            LoadWindows();
        }

        /// <summary>True if a ticket is currently being served at this window (today).</summary>
        private static bool IsAssigned(int windowId)
        {
            return Db.GetCount(
                "SELECT id FROM queue_tickets WHERE status = 'Serving' AND window_no = " + windowId +
                " AND DATE(created_at) = CURDATE()") > 0;
        }

        // ---- tiny inline text prompt (no extra Designer files) ----
        private static string Prompt(string title, string label, string initial)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;
                dlg.ClientSize = new System.Drawing.Size(360, 130);

                dlg.Controls.Add(new Label { Text = label, AutoSize = true, Location = new System.Drawing.Point(16, 16), Font = new System.Drawing.Font("Segoe UI", 9.75F) });
                var txt = new TextBox { Text = initial, Location = new System.Drawing.Point(16, 44), Size = new System.Drawing.Size(328, 28), Font = new System.Drawing.Font("Segoe UI", 11F) };
                dlg.Controls.Add(txt);

                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(168, 84), Size = new System.Drawing.Size(80, 32), FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(256, 84), Size = new System.Drawing.Size(88, 32), FlatStyle = FlatStyle.Flat };
                dlg.Controls.Add(ok); dlg.Controls.Add(cancel);
                dlg.AcceptButton = ok; dlg.CancelButton = cancel;

                return dlg.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }

        private void Warn(string m) =>
            MessageBox.Show(m, "Service Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
