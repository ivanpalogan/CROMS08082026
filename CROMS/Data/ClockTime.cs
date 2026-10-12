using System;
using System.Data;
using System.Globalization;

namespace CROMS.Data
{
    /// <summary>
    /// One rule for a clock time kept as text (births.time_of_birth): it is STORED as 24-hour
    /// "HH:mm" and SHOWN as "h:mm AM/PM", the way the certificate words it ("born alive at ___
    /// a.m./p.m."). Every screen, list and printout goes through here so they cannot disagree.
    ///
    /// A value with a one-digit hour and no AM/PM ("3:40", as a scan may read it) is left exactly
    /// as it is: the paper did not say morning or afternoon, so neither do we.
    /// </summary>
    public static class ClockTime
    {
        private static readonly string[] Stored = { "HH:mm", "HH:mm:ss" };
        private static readonly string[] Spoken =
            { "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "h:mm:ss tt", "hh:mm:ss tt", "h tt", "htt" };

        /// <summary>True when the text states an unambiguous time of day.</summary>
        public static bool TryParse(string text, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim().Replace(".", "");   // "1:40 p.m." -> "1:40 pm"
            DateTime d;
            if (DateTime.TryParseExact(t, Stored, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ||
                DateTime.TryParseExact(t, Spoken, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out d))
            {
                time = d.TimeOfDay;
                return true;
            }
            return false;
        }

        /// <summary>"15:15" -> "3:15 PM". Anything that is not a clear time is returned as stored.</summary>
        public static string Display(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return "";
            TimeSpan t;
            return TryParse(stored, out t)
                ? DateTime.Today.Add(t).ToString("h:mm tt", CultureInfo.InvariantCulture)
                : stored.Trim();
        }

        /// <summary>"3:15 PM" -> "15:15" for saving. Anything that is not a clear time is kept as typed.</summary>
        public static string Normalize(string typed)
        {
            if (string.IsNullOrWhiteSpace(typed)) return "";
            TimeSpan t;
            return TryParse(typed, out t)
                ? DateTime.Today.Add(t).ToString("HH:mm", CultureInfo.InvariantCulture)
                : typed.Trim();
        }

        /// <summary>Rewrites a text column of a loaded list so it reads "h:mm AM/PM".</summary>
        public static void DisplayColumn(DataTable table, string column)
        {
            if (table == null || !table.Columns.Contains(column)) return;
            DataColumn c = table.Columns[column];
            if (c.DataType != typeof(string)) return;
            c.ReadOnly = false;
            foreach (DataRow r in table.Rows)
                if (r[c] != DBNull.Value) r[c] = Display(Convert.ToString(r[c]));
            table.AcceptChanges();
        }
    }
}
