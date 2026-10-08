using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.Modules
{
    /// <summary>
    /// Sets MaxLength on text boxes / editable combos so a screen cannot take more than the
    /// database column behind it can store (typing and pasting both stop at the limit).
    /// One call per width instead of a literal on every control keeps the widths readable
    /// next to the column they mirror.
    /// </summary>
    internal static class FieldLimit
    {
        public static void Cap(int length, params Control[] controls)
        {
            if (controls == null) return;
            foreach (Control c in controls)
            {
                TextBox t = c as TextBox;
                if (t != null) { t.MaxLength = length; continue; }
                ComboBox b = c as ComboBox;
                if (b != null) b.MaxLength = length;
            }
        }
        private static readonly Dictionary<string, Dictionary<string, int>> _widths =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Declared width of a VARCHAR column, or 0 when unknown (not a VARCHAR, or no database).</summary>
        public static int WidthOf(string table, string column)
        {
            Dictionary<string, int> cols;
            if (!_widths.TryGetValue(table, out cols))
            {
                cols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using (DataTable dt = Db.Pull("SELECT column_name, character_maximum_length FROM information_schema.columns " +
                        "WHERE table_schema = DATABASE() AND table_name = @t AND data_type = 'varchar'",
                        new MySql.Data.MySqlClient.MySqlParameter("@t", table)))
                        foreach (DataRow r in dt.Rows)
                        {
                            long n = Convert.ToInt64(r[1]);
                            cols[r[0].ToString()] = (int)Math.Min(n, 32767);
                        }
                    _widths[table] = cols;   // cache only a real answer, so a failed lookup is retried
                }
                catch { return 0; }
            }
            int w;
            return cols.TryGetValue(column, out w) ? w : 0;
        }

        /// <summary>
        /// Cap boxes at the width of the column they save into, read from the database itself:
        /// FromDb("births", "registry_no", txtReg, "first_name", txtFirst, ...). Pairs whose column
        /// is not a VARCHAR (ages, weights) or is unknown are left alone.
        /// </summary>
        public static void FromDb(string table, params object[] columnControlPairs)
        {
            for (int i = 0; i + 1 < columnControlPairs.Length; i += 2)
            {
                string col = columnControlPairs[i] as string;
                Control c = columnControlPairs[i + 1] as Control;
                if (col == null || c == null) continue;
                int w = WidthOf(table, col);
                if (w > 0) Cap(w, c);
            }
        }
    }
}
