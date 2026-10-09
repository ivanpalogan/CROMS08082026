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
    /// Master / Library Files — one screen to manage every lookup list (the values
    /// that feed the dropdowns in the registration forms). Pick a category, then
    /// add / edit / delete its values. Replaces the old system's 14 separate forms.
    /// The category → table map is a fixed whitelist (never user input), so the
    /// generic SQL is safe.
    /// </summary>
    public partial class MasterFilesForm : Form
    {
        // Display name -> real table name (whitelist; all these tables are id + name).
        private static readonly Dictionary<string, string> Tables = new Dictionary<string, string>
        {
            { "Barangays", "barangays" },
            { "Municipalities", "municipalities" },
            { "Provinces", "provinces" },
            { "Countries", "countries" },
            { "Hospitals", "hospitals" },
            { "Churches", "churches" },
            { "Causes of Death", "causes_of_death" },
            { "Religions", "religions" },
            { "Nationalities / Citizenship", "nationalities" },
            { "Occupations", "occupations" },
            { "Civil Statuses", "civil_statuses" },
            { "Birth Orders", "birth_orders" },
            { "Type of Births", "type_of_births" },
            { "Relationships", "relationships" },
            { "Residences", "residences" },
        };

        // The geography tables hold the whole country since migration 29 (42,029 barangays,
        // 1,647 municipalities). Binding all of that to a grid froze the screen, and nobody
        // scrolls 42,000 rows anyway - so the list shows at most this many and the search box
        // narrows it. The count label says when rows are being held back.
        private const int MaxRows = 500;

        private int? _selectedId;

        // Debounce: one query when the operator stops typing, not one per keystroke.
        private readonly Timer _searchTimer = new Timer { Interval = 300 };

        private Control[] _editPane;
        private int _editLeft, _gridLeft, _editWidth;

        /// <summary>Grid gets the width the editor does not need; the editor follows the grid's right edge.</summary>
        private void LayoutPanes()
        {
            if (_editPane == null || ClientSize.Width <= 0) return;
            dgvItems.Left = _gridLeft;
            dgvItems.Width = Math.Max(460, ClientSize.Width - _gridLeft - _editWidth - 48);
            int delta = dgvItems.Right + 24 - _editLeft;
            if (delta == 0) return;
            foreach (Control c in _editPane) c.Left += delta;
            _editLeft += delta;
        }

        public MasterFilesForm()
        {
            InitializeComponent();

            // The list takes the width left over after the editor and the editor sits right beside
            // it. Pinning both to the right edge (the earlier approach) kept each at its DESIGN
            // distance from that edge, so on a narrower window the list collapsed to ~265px and
            // clipped every column (Barangays: Name / Municipality / Province).
            dgvItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _editPane = new Control[] { lblName, txtName, btnAdd, btnUpdate, btnDelete, btnNew, lblHint, lblCount };
            foreach (Control c in _editPane) c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _editLeft = lblName.Left;
            _gridLeft = dgvItems.Left;
            _editWidth = 400;
            Resize += (s, e) => LayoutPanes();
            Load += (s, e) => LayoutPanes();
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); LoadItems(); };
            Disposed += (s, e) => _searchTimer.Dispose();

            foreach (var name in Tables.Keys)
                cboCategory.Items.Add(name);
            cboCategory.SelectedIndexChanged += (s, e) =>
            {
                txtSearch.Clear();   // a search typed for barangays means nothing for religions
                _searchTimer.Stop(); // Clear raised TextChanged; this load already covers it
                LoadItems();
            };
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
        }

        /// <summary>Current table for the selected category (whitelisted).</summary>
        private string Table()
        {
            return Tables.TryGetValue(cboCategory.Text, out string t) ? t : null;
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void LoadItems()
        {
            string table = Table();
            if (table == null) return;

            // Barangay and municipality names repeat across the country (3,942 barangay names
            // and 109 municipality names occur more than once), so a bare name cannot tell two
            // rows apart. Show the parent place beside it.
            string select, from;
            switch (table)
            {
                case "barangays":
                    select = "t.id AS ID, t.name AS Name, m.name AS Municipality, p.name AS Province";
                    from = "barangays t LEFT JOIN municipalities m ON m.id = t.municipality_id " +
                           "LEFT JOIN provinces p ON p.id = m.province_id";
                    break;
                case "municipalities":
                    select = "t.id AS ID, t.name AS Name, p.name AS Province";
                    from = "municipalities t LEFT JOIN provinces p ON p.id = t.province_id";
                    break;
                default:
                    select = "t.id AS ID, t.name AS Name";
                    from = table + " t";
                    break;
            }

            string term = txtSearch.Text.Trim();
            string where = term.Length > 0 ? " WHERE t.name LIKE @q" : "";
            var q = new MySqlParameter("@q", "%" + term + "%");

            try
            {
                DataTable dt = Db.Pull("SELECT " + select + " FROM " + from + where +
                                       " ORDER BY t.name LIMIT " + MaxRows, q);
                int total = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM " + table + " t" + where,
                                       new MySqlParameter("@q", "%" + term + "%")).Rows[0][0]);

                dgvItems.DataSource = dt;
                if (dgvItems.Columns.Contains("ID")) dgvItems.Columns["ID"].FillWeight = 38;

                lblCount.Text = total > dt.Rows.Count
                    ? string.Format("Showing {0:N0} of {1:N0} — type in Search to narrow the list.", dt.Rows.Count, total)
                    : string.Format("{0:N0} {1}.", total, total == 1 ? "entry" : "entries");
            }
            catch (Exception ex)
            {
                dgvItems.DataSource = null;
                lblCount.Text = "Could not load this list: " + CROMS.Data.ErrorLog.Reason(ex);
            }

            _selectedId = null;
            txtName.Clear();
            FieldLimit.FromDb(table, "name", txtName);   // each master table has its own name width
        }

        private void dgvItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvItems.Rows[e.RowIndex];
            _selectedId = Convert.ToInt32(row.Cells["ID"].Value);
            txtName.Text = row.Cells["Name"].Value?.ToString() ?? "";
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string table = Table();
            if (table == null || !Require()) return;
            if (IsDuplicate(table, txtName.Text, null)) return;
            try
            {
                Db.Push("INSERT INTO " + table + " (name) VALUES (@n)",
                    new MySqlParameter("@n", txtName.Text.Trim()));
                Audit.Write(Audit.Create, table, null, txtName.Text.Trim());
                LoadItems();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string table = Table();
            if (table == null || !Require()) return;
            if (_selectedId == null)
            {
                MessageBox.Show("Click a row in the list to edit it first.", "Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (IsDuplicate(table, txtName.Text, _selectedId)) return;
            try
            {
                Db.Push("UPDATE " + table + " SET name = @n WHERE id = @id",
                    new MySqlParameter("@n", txtName.Text.Trim()),
                    new MySqlParameter("@id", _selectedId.Value));
                Audit.Write(Audit.Update, table, _selectedId.Value, txtName.Text.Trim());
                LoadItems();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            string table = Table();
            if (table == null) return;
            if (_selectedId == null)
            {
                MessageBox.Show("Click a row in the list to delete it first.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete \"" + txtName.Text + "\"?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM " + table + " WHERE id = @id",
                    new MySqlParameter("@id", _selectedId.Value));
                Audit.Write(Audit.Delete, table, _selectedId.Value, null);
                LoadItems();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("This value is in use by existing records and cannot be deleted.",
                    "In use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            _selectedId = null;
            txtName.Clear();
            txtName.Focus();
        }

        /// <summary>
        /// A plain pick-list must not hold the same value twice: the dropdowns on every form would
        /// show it twice and records would split between the two. The column collation is case- and
        /// accent-insensitive, so "catholic" and "Catholic" count as the same. Barangay and
        /// municipality names are exempt - the same name legitimately exists in many places (the
        /// PSGC list repeats thousands of them), so those are told apart by their parent, not by name.
        /// </summary>
        private bool IsDuplicate(string table, string name, int? exceptId)
        {
            if (table == "barangays" || table == "municipalities") return false;
            try
            {
                DataTable dt = Db.Pull("SELECT id FROM " + table + " WHERE name = @n" + (exceptId.HasValue ? " AND id <> @id" : ""),
                    exceptId.HasValue
                        ? new[] { new MySqlParameter("@n", name.Trim()), new MySqlParameter("@id", exceptId.Value) }
                        : new[] { new MySqlParameter("@n", name.Trim()) });
                if (dt.Rows.Count == 0) return false;
                MessageBox.Show("\"" + name.Trim() + "\" is already in this list.", "Already exists",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex) { Fail(ex); return true; }
        }

        private bool Require()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Enter a name.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static void Fail(Exception ex)
        {
            ErrorLog.Report(null, "MasterFiles", ex, "save that change");
        }
    }
}
