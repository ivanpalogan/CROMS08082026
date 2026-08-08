using System;
using System.Configuration;
using System.IO;
using MySql.Data.MySqlClient;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Reads the database server address that the MAIN CROMS app saved in
    /// <c>%APPDATA%\CROMS\server.cfg</c> (via its Connect-to-Server screen), and
    /// overrides the Server/Port of this app's App.config "Croms" connection
    /// string with it. So the kiosk automatically points at the same server the
    /// staff app was configured with — set the IP once, all three apps follow.
    /// If the file isn't there, the App.config value is used as-is.
    /// </summary>
    internal static class ServerConfig
    {
        private static readonly string File = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CROMS", "server.cfg");

        private static string _host;
        private static int _port = 3306;

        static ServerConfig() { Load(); }

        private static bool Configured => !string.IsNullOrWhiteSpace(_host);

        /// <summary>The saved DB server host (empty when unconfigured / local).</summary>
        public static string Host => _host;

        private static void Load()
        {
            try
            {
                if (!System.IO.File.Exists(File)) return;
                foreach (var raw in System.IO.File.ReadAllLines(File))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string v = line.Substring(eq + 1).Trim();
                    if (k == "host") _host = v;
                    else if (k == "port" && int.TryParse(v, out int p)) _port = p;
                }
            }
            catch { }
        }

        private static string Base
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["Croms"];
                return cs != null ? cs.ConnectionString : "";
            }
        }

        public static string EffectiveConnectionString
        {
            get
            {
                if (!Configured) return Base;
                try
                {
                    return new MySqlConnectionStringBuilder(Base)
                    {
                        Server = _host,
                        Port = (uint)_port
                    }.ConnectionString;
                }
                catch { return Base; }
            }
        }
    }
}
