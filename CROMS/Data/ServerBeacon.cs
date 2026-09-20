using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Announces, in the database itself, WHICH MACHINE IS SERVING IT.
    ///
    /// Why this exists: every office PC has MySQL and a <c>croms</c> database
    /// installed, so "localhost connects" is not evidence that a PC is looking at
    /// the live registry. A kiosk or display on a client laptop would open its own
    /// empty local copy, run perfectly, and sync with nobody — the failure looks
    /// like the app working, which is the worst kind. So the app running ON the
    /// serving machine stamps <c>server_beacon</c> every 30 seconds, and the other
    /// apps compare beacon ages to find the real server (see
    /// <see cref="ServerConfig.DiscoverBestAsync"/>).
    ///
    /// Only the app on the HOST machine writes it. A CROMS running as a client is
    /// pointed at somebody else's server and must not advertise it as its own —
    /// otherwise every client would keep the beacon alive on a database nobody is
    /// actually serving, and the signal would mean nothing.
    ///
    /// Requires migration 56. On a database that never received it every write
    /// fails silently and the beacon simply reads as absent, which is the correct
    /// answer rather than an error the operator cannot act on.
    /// </summary>
    public static class ServerBeacon
    {
        private const string Sql =
            "INSERT INTO server_beacon (id, machine_name, ip_list, app_version, updated_at) " +
            "VALUES (1, @m, @i, @v, NOW()) " +
            "ON DUPLICATE KEY UPDATE machine_name = VALUES(machine_name), " +
            "ip_list = VALUES(ip_list), app_version = VALUES(app_version), updated_at = NOW()";

        private static Timer _timer;
        private static int _writing;

        /// <summary>Start stamping the beacon (no-op if already started).</summary>
        public static void Start(int intervalMs = 30000)
        {
            if (_timer != null) return;
            _timer = new Timer(_ => Beat(), null, 0, intervalMs);
        }

        /// <summary>
        /// Stamp the beacon once, right now, on the calling thread. Called before the
        /// app goes looking for a server: if this PC is the one hosting the database,
        /// saying so FIRST means the search sees a live beacon and returns instantly
        /// instead of sweeping the whole LAN on every launch of the server machine.
        /// </summary>
        public static void BeatNow() { Beat(); }

        /// <summary>True when the database in use is hosted on this machine.</summary>
        public static bool ThisPcIsTheServer()
        {
            try { return ServerConfig.IsSelfHosted(ServerConfig.EffectiveHost); }
            catch { return false; }
        }

        private static void Beat()
        {
            if (Interlocked.Exchange(ref _writing, 1) == 1) return;
            try
            {
                // Re-checked on EVERY beat, not once at startup: the auto-reconnect
                // watcher can move this PC onto a remote server mid-session, and from
                // that moment we are a client and must stop claiming to serve.
                if (!ThisPcIsTheServer()) return;
                Write();
            }
            catch { /* the beacon is a hint, never a reason to interrupt the operator */ }
            finally { Interlocked.Exchange(ref _writing, 0); }
        }

        private static void Write()
        {
            using (var conn = new MySqlConnection(ServerConfig.EffectiveConnectionString))
            using (var cmd = new MySqlCommand(Sql, conn))
            {
                cmd.CommandTimeout = 5;
                cmd.Parameters.AddWithValue("@m", Environment.MachineName);
                cmd.Parameters.AddWithValue("@i", Ips());
                cmd.Parameters.AddWithValue("@v", Version());
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static string Ips()
        {
            try
            {
                string s = string.Join(", ", ServerConfig.LocalIPv4()
                    .Where(ip => !ip.StartsWith("127.") && !ip.StartsWith("169.254."))
                    .OrderBy(ip => ip));
                return s.Length > 255 ? s.Substring(0, 255) : s;
            }
            catch { return null; }
        }

        private static string Version()
        {
            try { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
            catch { return null; }
        }

        /// <summary>
        /// Who is serving the database this PC is using, for the Settings screen:
        /// the machine name and how long ago it last said so. Null when nobody is —
        /// which is worth showing plainly, because it means the registry everyone
        /// believes they share may be a local copy.
        /// </summary>
        public static string Describe()
        {
            try
            {
                using (var conn = new MySqlConnection(ServerConfig.EffectiveConnectionString))
                using (var cmd = new MySqlCommand(
                    "SELECT machine_name, TIMESTAMPDIFF(SECOND, updated_at, NOW()) FROM server_beacon WHERE id = 1", conn))
                {
                    cmd.CommandTimeout = 5;
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        string machine = r.IsDBNull(0) ? "(unnamed PC)" : r.GetString(0);
                        int age = r.IsDBNull(1) ? int.MaxValue : Convert.ToInt32(r.GetValue(1));
                        if (age < 0) age = 0;
                        if (age <= ServerConfig.LiveBeaconSeconds)
                            return "Served by " + machine + " (live)";
                        return "Served by " + machine + " — last seen " + Ago(age) + " ago";
                    }
                }
            }
            catch { return null; }
        }

        private static string Ago(int seconds)
        {
            if (seconds < 120) return seconds + "s";
            if (seconds < 7200) return (seconds / 60) + " min";
            return (seconds / 3600) + " hr";
        }
    }
}
