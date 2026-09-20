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

namespace CROMS.Kiosk
{
    /// <summary>
    /// Where the shared CROMS database lives, from <c>%APPDATA%\CROMS\server.cfg</c>
    /// (the same file the main CROMS app writes), overriding the Server/Port of this
    /// app's App.config "Croms" string.
    ///
    /// TWO THINGS THIS SCREEN HAS TO GET RIGHT, both of which it used to get wrong:
    ///
    /// (1) CREDENTIALS. The App.config account (root) is granted for localhost only;
    ///     a remote server answers it with "Access denied". So a remote host silently
    ///     switches to the LAN account (croms_user), exactly as the main app does.
    ///
    /// (2) WHICH DATABASE IS THE REAL ONE. Every office PC has MySQL and a `croms`
    ///     database installed, so "localhost opened" is not proof of anything: a kiosk
    ///     on a client laptop would take its own empty local copy, issue tickets into
    ///     it, print them, and sync with nobody — a failure that looks precisely like
    ///     the kiosk working. server_beacon (migration 56) is stamped by the CROMS app
    ///     on the machine that HOSTS the database, and the kiosk takes the server with
    ///     the freshest beacon. The age is computed by the answering server against its
    ///     OWN clock, so two laptops that disagree about the time still compare
    ///     correctly.
    ///
    /// Mirrors CROMS/Data/ServerConfig.cs.
    /// </summary>
    internal static class ServerConfig
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CROMS");
        private static readonly string File = Path.Combine(Dir, "server.cfg");

        private static string _host;
        private static int _port = 3306;
        private static DateTime _lastLoad = DateTime.MinValue;
        private static DateTime _lastWrite = DateTime.MinValue;

        static ServerConfig() { Load(); }

        private static bool Configured => !string.IsNullOrWhiteSpace(_host);

        /// <summary>
        /// The saved DB server host (empty when unconfigured / local). Re-reads
        /// server.cfg from disk whenever its last-write time has changed (checked at
        /// most once/5s, so this stays cheap) — the MAIN CROMS app can rewrite this file
        /// at any point during its own run (startup auto-discovery, or a manual
        /// Connect-to-Server save) and the kiosk is a SEPARATE process that would
        /// otherwise keep using whatever IP was on disk when it first launched, even
        /// after the real server address changed underneath it.
        /// </summary>
        public static string Host { get { RefreshIfChanged(); return _host; } }

        public static int Port { get { RefreshIfChanged(); return _port; } }

        private static void RefreshIfChanged()
        {
            if ((DateTime.UtcNow - _lastLoad).TotalSeconds < 5) return;
            _lastLoad = DateTime.UtcNow;
            try
            {
                if (!System.IO.File.Exists(File)) return;
                var wt = System.IO.File.GetLastWriteTimeUtc(File);
                if (wt == _lastWrite) return;
                _lastWrite = wt;
                Load();
            }
            catch { }
        }

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

        /// <summary>Persist the found server for next launch and use it now.</summary>
        public static void Save(string host, int port)
        {
            _host = string.IsNullOrWhiteSpace(host) ? null : host.Trim();
            _port = port > 0 ? port : 3306;
            try
            {
                Directory.CreateDirectory(Dir);
                System.IO.File.WriteAllText(File,
                    "# CROMS database server (shared with the main app)\r\n" +
                    "host=" + (_host ?? "") + "\r\n" +
                    "port=" + _port + "\r\n");
                _lastWrite = System.IO.File.GetLastWriteTimeUtc(File);   // our own write is not a change to re-read
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
                RefreshIfChanged();
                if (!Configured) return Base;
                try
                {
                    var b = new MySqlConnectionStringBuilder(Base) { Server = _host, Port = (uint)_port };
                    ApplyLanCreds(b, _host);
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

        public static bool IsReachable() { return Opens(EffectiveConnectionString); }

        public static bool TestConnection(string host, int port) { return Opens(BuildFor(host, port)); }

        private static bool Opens(string connectionString)
        {
            try { using (var c = new MySqlConnection(connectionString)) { c.Open(); return true; } }
            catch { return false; }
        }

        // ---- Which machine is actually SERVING the registry ---------------------

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
            add(_host);
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
        /// Point the kiosk at the machine that is really serving the registry. Cheap
        /// when already correct (a live beacon skips the sweep entirely); sweeps only
        /// when the database is unreachable OR nobody is serving the one we are on —
        /// which is exactly the "kiosk issuing tickets into its own local copy" case.
        /// </summary>
        public static bool EnsureBestServer()
        {
            int port = _port > 0 ? _port : 3306;
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
                    if (!string.Equals(best.Host, _host, StringComparison.OrdinalIgnoreCase))
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
        /// Background watcher: keeps the kiosk on the machine that is serving the
        /// registry — the server's Wi-Fi/hotspot IP changing, or a database nobody is
        /// serving. Idle while healthy. Call once at startup.
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
