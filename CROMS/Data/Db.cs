using System.Configuration;
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
                return cmd.ExecuteNonQuery();
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
                return cmd.ExecuteNonQuery();
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
                cmd.ExecuteNonQuery();
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
