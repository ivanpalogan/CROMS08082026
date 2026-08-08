using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Display
{
    /// <summary>
    /// Minimal read-only data access for the display app. Uses the same "Croms"
    /// connection string as the main system (in App.config), so both programs read
    /// and write the one shared database — that is the only link between them.
    /// </summary>
    internal static class Db
    {
        // Uses the server address the main CROMS app saved (%APPDATA%\CROMS\server.cfg),
        // falling back to App.config. So the display follows the staff app's server IP.
        private static string ConnectionString => ServerConfig.EffectiveConnectionString;

        /// <summary>Runs a SELECT and returns the result as a DataTable.</summary>
        public static DataTable Pull(string sql)
        {
            var table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            using (var adapter = new MySqlDataAdapter(sql, conn))
                adapter.Fill(table);
            return table;
        }
    }
}
