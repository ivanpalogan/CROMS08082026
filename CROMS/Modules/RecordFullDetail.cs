using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Modules
{
    /// <summary>
    /// Read-only "everything saved for this record" dialog for Birth / Marriage / Death.
    /// Reads the record's certificate view (v_birth_certificate / v_marriage_certificate /
    /// v_death_certificate), which already resolves every lookup id to its name, so the
    /// operator sees the same facts the certificate carries — not raw foreign-key numbers.
    /// Blank values and image blobs are left out; dates print as dates.
    /// </summary>
    public static class RecordFullDetail
    {
        public static void Show(IWin32Window owner, string view, long recordId, string title)
        {
            DataTable dt;
            try
            {
                dt = Db.Pull("SELECT * FROM " + SafeView(view) + " WHERE record_id = @id",
                    new MySqlParameter("@id", recordId));
            }
            catch (Exception ex)
            {
                ErrorLog.Report(owner, "RecordFullDetail", ex, "open the full record");
                return;
            }
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(owner, "That record no longer exists.", "Full Details",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataRow row = dt.Rows[0];
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(780, 680);
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;

                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    ColumnHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };
                grid.Columns.Add("f", "Field");
                grid.Columns.Add("v", "Value");
                grid.Columns[0].FillWeight = 34;
                grid.Columns[1].FillWeight = 66;
                grid.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                foreach (DataColumn c in dt.Columns)
                {
                    object v = row[c];
                    if (v == null || v == DBNull.Value || v is byte[]) continue;
                    string text = Format(v);
                    if (text.Length == 0) continue;
                    if (c.ColumnName == "record_id" || c.ColumnName.EndsWith("_asset")) continue;
                    int i = grid.Rows.Add(Pretty(c.ColumnName), text);
                    grid.Rows[i].Cells[0].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }

                var btnClose = new Button
                {
                    Text = "Close",
                    DialogResult = DialogResult.OK,
                    Dock = DockStyle.Bottom,
                    Height = 42
                };
                dlg.Controls.Add(grid);
                dlg.Controls.Add(btnClose);
                dlg.AcceptButton = btnClose;
                UiTheme.Polish(dlg);
                dlg.ShowDialog(owner);
            }
        }

        // The view name is always a literal from the three callers; this just refuses
        // anything else so the string concatenation above can never carry user text.
        private static string SafeView(string view)
        {
            switch (view)
            {
                case "v_birth_certificate":
                case "v_marriage_certificate":
                case "v_death_certificate":
                    return view;
                default:
                    throw new ArgumentException("Unknown record view.");
            }
        }

        private static string Format(object v)
        {
            if (v is DateTime d)
                return d.TimeOfDay == TimeSpan.Zero
                    ? d.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)
                    : d.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
            if (v is TimeSpan t) return DateTime.Today.Add(t).ToString("h:mm tt", CultureInfo.InvariantCulture);
            if (v is bool b) return b ? "Yes" : "No";
            return Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }

        private static string Pretty(string column)
        {
            string s = column.Replace('_', ' ').Trim();
            return s.Length == 0 ? column : char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
