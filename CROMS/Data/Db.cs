using System.Configuration;
using System.Text.RegularExpressions;
using System.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Central MySQL data-access helper for CROMS. Same simple pattern the
    /// team's earlier system used (Push / Pull / GetCount / IsConnected), but
    /// the connection string is read from App.config (the "Croms" entry) instead
    /// of hard-coded settings.
    ///
    /// Usage:
    ///   DataTable dt = Db.Pull("SELECT * FROM births");
    ///   Db.Push("INSERT INTO ...");
    ///   int n = Db.GetCount("SELECT id FROM queue_tickets");
    /// </summary>
    public static class Db
    {
        // Resolved each use so a server address chosen at runtime (ServerSetupForm)
        // takes effect without a restart. Falls back to the App.config "Croms"
        // string when no server has been configured on this PC.
        private static string ConnectionString => ServerConfig.EffectiveConnectionString;

        /// <summary>
        /// Exception-filter helper (always returns false, so nothing is caught or rethrown):
        /// notes which table a "Data too long" (1406) write was aimed at on <c>ex.Data["table"]</c>,
        /// so <c>ErrorLog</c> can name the column AND its limit. Costs nothing on any other error.
        /// </summary>
        internal static bool TagTable(MySqlException ex, string sql)
        {
            try
            {
                if (ex != null && ex.Number == 1406 && sql != null)
                {
                    Match m = Regex.Match(sql, @"^\s*(?:INSERT\s+(?:IGNORE\s+)?INTO|REPLACE\s+INTO|UPDATE)\s+`?(\w+)`?",
                        RegexOptions.IgnoreCase);
                    if (m.Success) ex.Data["table"] = m.Groups[1].Value;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Returns true if a connection to the database can be opened.</summary>
        public static bool IsConnected()
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Runs an INSERT / UPDATE / DELETE and returns rows affected.</summary>
        public static int Push(string sql)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                conn.Open();
                try { return cmd.ExecuteNonQuery(); }
                catch (MySqlException ex) when (TagTable(ex, sql)) { throw; }
            }
        }

        /// <summary>
        /// Runs a parameterized INSERT / UPDATE / DELETE. Prefer this over string
        /// concatenation — it is safe against SQL injection and quoting bugs.
        /// Example:
        ///   Db.Push("INSERT INTO births (first_name) VALUES (@fn)",
        ///           new MySqlParameter("@fn", firstName));
        /// </summary>
        public static int Push(string sql, params MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                try { return cmd.ExecuteNonQuery(); }
                catch (MySqlException ex) when (TagTable(ex, sql)) { throw; }
            }
        }

        /// <summary>Runs a SELECT and returns the result as a DataTable.</summary>
        public static DataTable Pull(string sql)
        {
            var table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            using (var adapter = new MySqlDataAdapter(sql, conn))
            {
                adapter.Fill(table);
            }
            return table;
        }

        /// <summary>Runs a parameterized SELECT and returns the result as a DataTable.</summary>
        public static DataTable Pull(string sql, params MySqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(table);
            }
            return table;
        }

        /// <summary>
        /// Runs a parameterized INSERT and returns the new row's auto-increment id.
        /// Use when the inserted id is needed to link related rows (e.g. a transaction
        /// to its certificate request / release).
        /// </summary>
        public static long Insert(string sql, params MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                try { cmd.ExecuteNonQuery(); }
                catch (MySqlException ex) when (TagTable(ex, sql)) { throw; }
                return cmd.LastInsertedId;
            }
        }

        /// <summary>
        /// Returns the number of ROWS the given SELECT would return.
        /// The query is wrapped as <c>SELECT COUNT(*) FROM (&lt;sql&gt;)</c>, so the usual
        /// callers — which pass <c>"SELECT id FROM ... WHERE ..."</c> — get a true row
        /// count. (Running their SELECT directly through ExecuteScalar would return the
        /// first row's id instead of the count.) Pass a plain row-returning SELECT, not
        /// one that already aggregates.
        /// Example: <c>int n = Db.GetCount("SELECT id FROM queue_tickets");</c>
        /// </summary>
        public static int GetCount(string sql)
        {
            string countSql = "SELECT COUNT(*) FROM (" + sql + ") AS _croms_count";
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(countSql, conn))
            {
                conn.Open();
                object result = cmd.ExecuteScalar();
                return (result == null || result == System.DBNull.Value)
                    ? 0
                    : System.Convert.ToInt32(result);
            }
        }
    }
}
