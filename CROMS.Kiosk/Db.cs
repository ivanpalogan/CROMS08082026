using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Data access for the kiosk. Uses the same "Croms" connection string as the main
    /// system (App.config), so a ticket the client submits here appears instantly in
    /// the staff app — the shared database is the only link between the two.
    /// </summary>
    internal static class Db
    {
        // Uses the server address the main CROMS app saved (%APPDATA%\CROMS\server.cfg),
        // falling back to App.config. So the kiosk follows the staff app's server IP.
        private static string ConnectionString => ServerConfig.EffectiveConnectionString;

        public static DataTable Pull(string sql)
        {
            var table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            using (var adapter = new MySqlDataAdapter(sql, conn))
                adapter.Fill(table);
            return table;
        }

        /// <summary>Parameterized SELECT — same as Pull(sql) but SQL-injection safe.</summary>
        public static DataTable Pull(string sql, params MySqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(table);
            }
            return table;
        }

        public static int Push(string sql, params MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Runs a parameterized INSERT and returns the new row's auto-increment id.
        /// Used to link the per-service rows to the queue ticket they belong to.
        /// </summary>
        public static long Insert(string sql, params MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }
    }
}
