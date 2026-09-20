using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace CROMS.Display
{
    /// <summary>
    /// Where the shared CROMS database lives, from <c>%APPDATA%\CROMS\server.cfg</c>
    /// (the same file the main CROMS app writes). Overrides the Server/Port of this
    /// app's App.config "Croms" string. When pointed at a REMOTE host it automatically
    /// uses the LAN account (croms_user) — the App.config root account is localhost-only.
    ///
    /// The board does NOT trust "localhost opened" as proof it has the live registry:
    /// this PC almost certainly has its own MySQL and its own <c>croms</c> database, and
    /// a board quietly showing an empty local copy looks exactly like a board working.
    /// See the server-beacon section below; mirrors CROMS/Data/ServerConfig.cs.
    /// </summary>
    internal static class ServerConfig
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CROMS");
        private static readonly string File = Path.Combine(Dir, "server.cfg");

        public static string Host { get; private set; }
        public static int Port { get; private set; } = 3306;
        private static bool Configured => !string.IsNullOrWhiteSpace(Host);

        static ServerConfig() { Load(); }

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
                    if (k == "host") Host = v;
                    else if (k == "port" && int.TryParse(v, out int p)) Port = p;
                }
            }
            catch { }
        }

        /// <summary>Persist the found/entered server for next launch and use it now.</summary>
        public static void Save(string host, int port)
        {
            Host = string.IsNullOrWhiteSpace(host) ? null : host.Trim();
            Port = port > 0 ? port : 3306;
            try
            {
                Directory.CreateDirectory(Dir);
                System.IO.File.WriteAllText(File,
                    "# CROMS database server (shared with the main app)\r\n" +
                    "host=" + (Host ?? "") + "\r\n" +
                    "port=" + Port + "\r\n");
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

        // LAN account used automatically for a REMOTE server (root is localhost-only;
        // croms_user is granted on the private subnets). Overridable via App.config.
        private static string LanUser =>
            (ConfigurationManager.AppSettings["LanDbUser"] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings["LanDbUser"].Trim() : "croms_user";
        private static string LanPassword =>
            (ConfigurationManager.AppSettings["LanDbPassword"] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings["LanDbPassword"].Trim() : "y1MqyIrdLew23Sok00B2";

        private static bool IsLocalHost(string h)
        {
            if (string.IsNullOrWhiteSpace(h)) return true;
            h = h.Trim().ToLowerInvariant();
            return h == "localhost" || h == "127.0.0.1" || h == "::1" || h == ".";
        }

        private static void ApplyLanCreds(MySqlConnectionStringBuilder b, string host)
        {
            if (IsLocalHost(host)) return;
            b.UserID = LanUser;
            b.Password = LanPassword;
        }

        public static string EffectiveConnectionString
        {
            get
            {
                if (!Configured) return Base;
                try
                {
                    var b = new MySqlConnectionStringBuilder(Base) { Server = Host, Port = (uint)Port };
                    ApplyLanCreds(b, Host);
                    return b.ConnectionString;
                }
                catch { return Base; }
            }
        }

        /// <summary>Connection string for an arbitrary host/port off the same template.</summary>
        public static string BuildFor(string host, int port)
        {
            try
            {
                var b = new MySqlConnectionStringBuilder(Base)
                {
                    Server = host,
                    Port = (uint)(port > 0 ? port : 3306)
                };
                ApplyLanCreds(b, host);
                return b.ConnectionString;
            }
            catch { return Base; }
        }


        /// <summary>
        /// Same connection string, but with a short connect timeout: a sweep touches
        /// every answering host on the LAN and must not stall for MySQL's default
        /// 15 seconds on one router or printer that happens to listen on 3306.
        /// Probing only — the real connection keeps the normal timeout.
        /// </summary>
        private static string ProbeFor(string host, int port)
        {
            try
            {
                var b = new MySqlConnectionStringBuilder(BuildFor(host, port));
                b.ConnectionTimeout = 4;
                return b.ConnectionString;
            }
            catch { return BuildFor(host, port); }
        }

        /// <summary>True if the current effective connection opens.</summary>
        public static bool IsReachable() { return Opens(EffectiveConnectionString); }

        /// <summary>True if host:port accepts the CROMS credentials.</summary>
        public static bool TestConnection(string host, int port) { return Opens(BuildFor(host, port)); }

        private static bool Opens(string connectionString)
        {
            try { using (var c = new MySqlConnection(connectionString)) { c.Open(); return true; } }
            catch { return false; }
        }

        // ---- Which machine is actually SERVING the registry ---------------------
        // server_beacon (migration 56) is stamped by the CROMS app running on the
        // machine that HOSTS the database. Comparing beacon ages is what tells this
        // board apart from its own idle local copy of `croms`. The age is computed by
        // the answering server against its OWN clock, so clock skew between two
        // laptops cannot distort the comparison.

        public const int LiveBeaconSeconds = 150;

        private const string BeaconAgeSql =
            "SELECT TIMESTAMPDIFF(SECOND, updated_at, NOW()) FROM server_beacon WHERE id = 1";

        /// <summary>
        /// Seconds since that database's beacon was written; null when unreachable, not
        /// migrated, or never served. Null always LOSES — an untouched local copy must
        /// never beat the real server.
        /// </summary>
        public static int? BeaconAge(string connectionString)
        {
            try
            {
                using (var c = new MySqlConnection(connectionString))
                {
                    c.Open();
                    using (var cmd = new MySqlCommand(BeaconAgeSql, c))
                    {
                        cmd.CommandTimeout = 5;
                        object o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) return null;
                        int age = Convert.ToInt32(o);
                        return age < 0 ? 0 : age;
                    }
                }
            }
            catch { return null; }
        }

        public static int? CurrentBeaconAge() { return BeaconAge(EffectiveConnectionString); }

        public sealed class ServerCandidate
        {
            public string Host;
            public int? Age;
            public int Rank;
            public bool IsLive { get { return Age.HasValue && Age.Value <= LiveBeaconSeconds; } }
        }

        private static bool Beats(ServerCandidate a, ServerCandidate b)
        {
            if (b == null) return true;
            if (a.Age.HasValue != b.Age.HasValue) return a.Age.HasValue;
            if (a.Age.HasValue && a.Age.Value != b.Age.Value) return a.Age.Value < b.Age.Value;
            return a.Rank < b.Rank;
        }

        /// <summary>
        /// Sweep every /24 this PC is on (plus the saved host, the App.config host and
        /// localhost) and return the server with the FRESHEST beacon. Falls back to any
        /// host that accepts the CROMS credentials when no beacon is found anywhere.
        /// </summary>
        public static async Task<ServerCandidate> DiscoverBestAsync(int port = 3306)
        {
            var hosts = new List<string>();
            Action<string> add = h =>
            {
                if (string.IsNullOrWhiteSpace(h)) return;
                h = h.Trim();
                if (!hosts.Any(x => string.Equals(x, h, StringComparison.OrdinalIgnoreCase))) hosts.Add(h);
            };
            add(Host);
            try { add(new MySqlConnectionStringBuilder(Base).Server); } catch { }
            add("127.0.0.1");
            foreach (var h in CandidateHosts()) add(h);
            if (hosts.Count == 0) return null;

            ServerCandidate best = null;
            object bestLock = new object();
            var gate = new SemaphoreSlim(64);

            var tasks = hosts.Select(async (ip, index) =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!await PortOpenAsync(ip, port, 400).ConfigureAwait(false)) return;
                    string cs = ProbeFor(ip, port);
                    int? age = BeaconAge(cs);
                    if (!age.HasValue && !Opens(cs)) return;
                    var c = new ServerCandidate { Host = ip, Age = age, Rank = index };
                    lock (bestLock) { if (Beats(c, best)) best = c; }
                }
                catch { }
                finally { gate.Release(); }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return best;
        }

        /// <summary>Address of the best server found, or null.</summary>
        public static async Task<string> DiscoverServerAsync(int port = 3306)
        {
            var best = await DiscoverBestAsync(port).ConfigureAwait(false);
            return best == null ? null : best.Host;
        }

        /// <summary>
        /// Point the board at the machine that is really serving the registry. Cheap
        /// when already correct (a live beacon skips the sweep entirely); sweeps only
        /// when the database is unreachable OR nobody is serving the one we are on —
        /// which is exactly the "board showing its own empty local copy" case.
        /// </summary>
        public static bool EnsureBestServer()
        {
            int port = Port > 0 ? Port : 3306;
            int? cur = CurrentBeaconAge();
            if (cur.HasValue && cur.Value <= LiveBeaconSeconds) return true;

            ServerCandidate best;
            try { best = DiscoverBestAsync(port).GetAwaiter().GetResult(); }
            catch { best = null; }

            if (best != null && !string.IsNullOrEmpty(best.Host))
            {
                bool fresher = best.Age.HasValue && (!cur.HasValue || best.Age.Value < cur.Value);
                bool nothingNow = !cur.HasValue && !IsReachable();
                if (fresher || nothingNow)
                {
                    if (!string.Equals(best.Host, Host, StringComparison.OrdinalIgnoreCase))
                        Save(best.Host, port);
                    return IsReachable();
                }
            }
            return cur.HasValue || IsReachable();
        }

        private static Timer _reconnect;
        private static int _reconnecting;
        private static DateTime _lastSweep = DateTime.MinValue;

        /// <summary>
        /// Background watcher: keeps the board on the machine that is serving the
        /// registry. Handles both the server's IP changing and the quieter failure of
        /// still reaching a database nobody is serving. Idle while healthy.
        /// </summary>
        public static void StartAutoReconnect(int intervalMs = 15000)
        {
            if (_reconnect != null) return;
            _reconnect = new Timer(_ => TryReconnect(), null, intervalMs, intervalMs);
        }

        private static void TryReconnect()
        {
            if (Interlocked.Exchange(ref _reconnecting, 1) == 1) return;
            try
            {
                int? age = CurrentBeaconAge();
                if (age.HasValue && age.Value <= LiveBeaconSeconds) return;
                if (IsReachable() && (DateTime.UtcNow - _lastSweep).TotalSeconds < 120) return;
                _lastSweep = DateTime.UtcNow;
                EnsureBestServer();
            }
            catch { }
            finally { Interlocked.Exchange(ref _reconnecting, 0); }
        }

        private static List<string> CandidateHosts()
        {
            var list = new List<string>();
            var seen = new HashSet<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var b = ua.Address.GetAddressBytes();
                        if (b[0] == 169 && b[1] == 254) continue;
                        string subnet = b[0] + "." + b[1] + "." + b[2] + ".";
                        if (!seen.Add(subnet)) continue;
                        for (int h = 1; h <= 254; h++)
                            if (h != b[3]) list.Add(subnet + h);
                    }
                }
            }
            catch { }
            return list;
        }

        private static async Task<bool> PortOpenAsync(string host, int port, int timeoutMs)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    var connect = client.ConnectAsync(host, port);
                    var done = await Task.WhenAny(connect, Task.Delay(timeoutMs)).ConfigureAwait(false);
                    return done == connect && client.Connected;
                }
                catch { return false; }
            }
        }
    }
}
