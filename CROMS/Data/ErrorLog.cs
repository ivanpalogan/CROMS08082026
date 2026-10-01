using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// What happens when something goes wrong while a clerk is mid-entry: the technical detail
    /// goes to a log file in the Windows temp folder, and the person at the keyboard sees one
    /// short sentence they can act on.
    /// <para/>
    /// Exception text, SQL errors and stack traces are for whoever maintains the system. Showing
    /// them to a clerk helps nobody - they cannot fix "Unknown column 'birth_country' in 'field
    /// list'" - and a raw database message can quote the names a record was being saved under.
    /// The first line of the sentence says what to do next; the log says what happened.
    /// </summary>
    internal static class ErrorLog
    {
        private static readonly object Gate = new object();

        /// <summary>Where the detail goes: %TEMP%\croms-error.log (never beside the registry data).</summary>
        public static string FilePath
        {
            get { return Path.Combine(Path.GetTempPath(), "croms-error.log"); }
        }

        /// <summary>Appends one entry. Never throws - logging must not become a second failure.</summary>
        public static void Write(string where, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ").AppendLine(where ?? "");
                string who = null;
                try { who = Session.User == null ? null : Session.User.Username; } catch { }
                if (!string.IsNullOrEmpty(who)) sb.Append("  user: ").AppendLine(who);
                sb.AppendLine(ex == null ? "  (no exception)" : ex.ToString());
                sb.AppendLine();
                lock (Gate) File.AppendAllText(FilePath, sb.ToString());
            }
            catch { /* a full disk or a locked file must not break the screen that called us */ }
        }

        /// <summary>
        /// The sentence to show. <paramref name="doing"/> completes "we could not ___ this" -
        /// for example "save", "open" or "print".
        /// </summary>
        public static string Friendly(Exception ex, string doing)
        {
            doing = string.IsNullOrWhiteSpace(doing) ? "save" : doing;

            MySqlException my = Find(ex);
            if (my != null)
            {
                // 1054 / 1146: a column or table a newer build expects is not in this database yet.
                // The rest of the project degrades on this rather than crashing; here it is a
                // sentence that names the real remedy.
                if (my.Number == 1054 || my.Number == 1146 || my.Number == 1364)
                    return "This computer's database has not been updated for this version yet. " +
                           "Please ask the administrator to run the latest database update, then try again.";

                // Connection refused / host unknown / server gone / lock wait / access denied.
                if (my.Number == 0 || my.Number == 1042 || my.Number == 1043 || my.Number == 1045 ||
                    my.Number == 1053 || my.Number == 2002 || my.Number == 2003 || my.Number == 2006 ||
                    my.Number == 2013)
                    return "Sorry, we could not reach the records database just now. Check that the " +
                           "server PC is on and connected, then try again.";
            }

            return "Sorry, we could not " + doing + " this just now. Please try again, or ask the staff for help.";
        }

        /// <summary>Logs, then tells the user. The one call a catch block needs.</summary>
        public static void Report(IWin32Window owner, string where, Exception ex, string doing)
        {
            Write(where, ex);
            string msg = Friendly(ex, doing);
            try
            {
                if (owner != null) MessageBox.Show(owner, msg, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else MessageBox.Show(msg, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { /* nothing more can be done if even a message box fails */ }
        }

        private static MySqlException Find(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                MySqlException m = e as MySqlException;
                if (m != null) return m;
            }
            return null;
        }
    }
}
