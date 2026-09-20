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

namespace CROMS.Data
{
    /// <summary>
    /// Where the CROMS database lives, stored per-user in
    /// <c>%APPDATA%\CROMS\server.cfg</c> so a client PC never has to hand-edit
    /// App.config. The saved host/port OVERRIDE the server/port in the App.config
    /// "Croms" connection string; the user id / password / database / options in
    /// App.config are kept (one place for credentials). If nothing is saved yet
    /// (e.g. the dev/server machine), the App.config string is used as-is.
    ///
    /// Set by <c>ServerSetupForm</c>. Read by <c>Db</c>.
    ///
    /// IMPORTANT: "the App.config connection opens" is NOT proof that this PC is
    /// looking at the live registry — see the server-beacon section below.
    /// </summary>
    public static class ServerConfig
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CROMS");
        private static readonly string File = Path.Combine(Dir, "server.cfg");

        /// <summary>Saved server host (IP or name), or null if never configured.</summary>
        public static string Host { get; private set; }
        /// <summary>Saved port (defaults to 3306).</summary>
        public static int Port { get; private set; } = 3306;

        static ServerConfig() { Load(); }

        /// <summary>True once a server address has been saved on this PC.</summary>
        public static bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

        /// <summary>
        /// The DB server host actually in effect: the saved host if configured,
        /// otherwise the Server value from the App.config connection string. Null
        /// only if neither is available. Used to point the mobile QR / device list
        /// at the server even when the setup screen was never shown (the config IP
        /// connected on the first try).
        /// </summary>
        public static string EffectiveHost
        {
            get
            {
                if (IsConfigured) return Host;
                try { return new MySqlConnectionStringBuilder(BaseConnectionString).Server; }
                catch { return null; }
            }
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
                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();
                    if (key == "host") Host = val;
                    else if (key == "port" && int.TryParse(val, out int p)) Port = p;
                }
            }
            catch { /* unreadable config = treat as not configured */ }
        }

        /// <summary>Persist the server address for next launch and use it now.</summary>
        public static void Save(string host, int port)
        {
            Host = string.IsNullOrWhiteSpace(host) ? null : host.Trim();
            Port = port > 0 ? port : 3306;
            try
            {
                Directory.CreateDirectory(Dir);
                System.IO.File.WriteAllText(File,
                    "# CROMS database server (edit via the app's Connect-to-Server screen)\r\n" +
                    "host=" + (Host ?? "") + "\r\n" +
                    "port=" + Port + "\r\n");
            }
            catch { /* best-effort; the in-memory value still applies this run */ }
        }

        /// <summary>The App.config "Croms" connection string (the credential template).</summary>
        public static string BaseConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["Croms"];
                return cs != null ? cs.ConnectionString : "";
            }
        }

        // LAN account used automatically when connecting to a REMOTE server. The
        // App.config account (root) is granted only for localhost; croms_user is
        // granted for the private subnets. Overridable via App.config so no code
        // change is needed to rotate it. This lets ONE config work on every PC:
        // the server machine (localhost) keeps root; any client pointed at a remote
        // IP silently uses croms_user — no special client config, no wrong-creds bug.
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

        /// <summary>On a remote host, swap in the LAN account (root is localhost-only).</summary>
        private static void ApplyLanCreds(MySqlConnectionStringBuilder b, string host)
        {
            if (IsLocalHost(host)) return;
            b.UserID = LanUser;
            b.Password = LanPassword;
        }

        /// <summary>
        /// The connection string CROMS actually uses: the App.config template with
        /// Server/Port replaced by the saved values (when configured).
        /// </summary>
        public static string EffectiveConnectionString
        {
            get
            {
                string bas = BaseConnectionString;
                if (!IsConfigured) return bas;   // dev/server machine: use App.config as-is
                try
                {
                    var b = new MySqlConnectionStringBuilder(bas)
                    {
                        Server = Host,
                        Port = (uint)Port
                    };
                    ApplyLanCreds(b, Host);
                    return b.ConnectionString;
                }
                catch
                {
                    return bas;
                }
            }
        }

        /// <summary>
        /// Build a connection string for an arbitrary host/port off the same
        /// credential template (used by the setup screen's Test button).
        /// </summary>
        public static string BuildFor(string host, int port)
        {
            try
            {
                var b = new MySqlConnectionStringBuilder(BaseConnectionString)
                {
                    Server = host,
                    Port = (uint)(port > 0 ? port : 3306)
                };
                ApplyLanCreds(b, host);
                return b.ConnectionString;
            }
            catch { return BaseConnectionString; }
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

        /// <summary>Try to open a connection to host:port. Returns true if it works.</summary>
        public static bool TestConnection(string host, int port, out string error)
        {
            error = null;
            try
            {
                using (var conn = new MySqlConnection(BuildFor(host, port)))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>True if the current effective connection opens.</summary>
        public static bool IsReachable()
        {
            try { using (var c = new MySqlConnection(EffectiveConnectionString)) { c.Open(); return true; } }
            catch { return false; }
        }

        // ---- Which machine is actually SERVING the registry ---------------------
        // Every office PC has MySQL and a `croms` database installed, so "localhost
        // opens" proves NOTHING: a kiosk or display on a client laptop will happily
        // read its own empty local copy, look like it is running, and sync with
        // nobody. server_beacon (migration 56) is written by the CROMS app on the
        // machine that HOSTS the database; every app then asks each candidate server
        // how old its beacon is and takes the freshest. The age is computed by the
        // answering server against its OWN clock (TIMESTAMPDIFF vs NOW()), so two
        // laptops that disagree about the time still compare correctly.

        /// <summary>A beacon at least this fresh means that server is being served right now.</summary>
        public const int LiveBeaconSeconds = 150;

        private const string BeaconAgeSql =
            "SELECT TIMESTAMPDIFF(SECOND, updated_at, NOW()) FROM server_beacon WHERE id = 1";

        /// <summary>
        /// Seconds since the beacon on the database reached by
        /// <paramref name="connectionString"/> was last written. Null when that server
        /// cannot be reached, has no beacon table (never migrated), or has never been
        /// served by anyone — and null always LOSES the comparison, which is the whole
        /// point: an untouched local copy must never win against the real server.
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
                        return age < 0 ? 0 : age;   // a clock nudged forward is not "fresher than now"
                    }
                }
            }
            catch { return null; }
        }

        /// <summary>Age of the beacon on the server this PC is currently using.</summary>
        public static int? CurrentBeaconAge() => BeaconAge(EffectiveConnectionString);

        /// <summary>Every IPv4 address this PC currently holds.</summary>
        public static HashSet<string> LocalIPv4()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                            set.Add(ua.Address.ToString());
                }
            }
            catch { }
            return set;
        }

        /// <summary>
        /// True when the database in use is hosted on THIS machine — named either as
        /// localhost or by one of this PC's own LAN addresses. Only the app running on
        /// that machine writes the beacon; a client must never advertise somebody
        /// else's server as its own.
        /// </summary>
        public static bool IsSelfHosted(string host)
        {
            if (IsLocalHost(host)) return true;
            return LocalIPv4().Contains((host ?? "").Trim());
        }

        /// <summary>One server the sweep found, with the age of its beacon (null = none).</summary>
        public sealed class ServerCandidate
        {
            public string Host;
            public int? Age;
            public int Rank;          // preference order when neither candidate has a beacon
            public bool IsLive => Age.HasValue && Age.Value <= LiveBeaconSeconds;
        }

        private static bool Beats(ServerCandidate a, ServerCandidate b)
        {
            if (b == null) return true;
            if (a.Age.HasValue != b.Age.HasValue) return a.Age.HasValue;   // any beacon beats none
            if (a.Age.HasValue && a.Age.Value != b.Age.Value) return a.Age.Value < b.Age.Value;
            return a.Rank < b.Rank;                                        // stable, order-independent
        }

        /// <summary>
        /// Sweep every /24 subnet this PC is on (plus the saved host, the App.config
        /// host and localhost) and return the server with the FRESHEST beacon — i.e.
        /// the machine somebody is actually running CROMS on. Falls back to any host
        /// that accepts the CROMS credentials when no beacon is found anywhere.
        /// </summary>
        public static async Task<ServerCandidate> DiscoverBestAsync(
            int port = 3306, Action<int, int> progress = null, CancellationToken cancel = default(CancellationToken))
        {
            var hosts = new List<string>();
            Action<string> add = h =>
            {
                if (string.IsNullOrWhiteSpace(h)) return;
                h = h.Trim();
                if (!hosts.Any(x => string.Equals(x, h, StringComparison.OrdinalIgnoreCase))) hosts.Add(h);
            };
            add(Host);                                                     // whatever we used last
            try { add(new MySqlConnectionStringBuilder(BaseConnectionString).Server); } catch { }
            add("127.0.0.1");                                              // this PC, if it IS the server
            foreach (var h in CandidateHosts()) add(h);
            if (hosts.Count == 0) return null;

            ServerCandidate best = null;
            object bestLock = new object();
            int scanned = 0;
            var gate = new SemaphoreSlim(64);   // bound concurrency: don't open 700 sockets at once

            var tasks = hosts.Select(async (ip, index) =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (cancel.IsCancellationRequested) return;
                    if (!await PortOpenAsync(ip, port, 400).ConfigureAwait(false)) return;
                    string cs = ProbeFor(ip, port);
                    int? age = BeaconAge(cs);
                    if (!age.HasValue && !Opens(cs)) return;   // port answered but it isn't our DB
                    var c = new ServerCandidate { Host = ip, Age = age, Rank = index };
                    lock (bestLock) { if (Beats(c, best)) best = c; }
                }
                catch { }
                finally
                {
                    Interlocked.Increment(ref scanned);
                    progress?.Invoke(scanned, hosts.Count);
                    gate.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return best;
        }

        private static bool Opens(string connectionString)
        {
            try { using (var c = new MySqlConnection(connectionString)) { c.Open(); return true; } }
            catch { return false; }
        }

        /// <summary>
        /// Auto-discover the CROMS database server on the local network and return its
        /// address (null if none). Kept for the Connect-to-Server screen; it now returns
        /// the machine with the freshest beacon rather than whichever host answered first.
        /// </summary>
        public static async Task<string> DiscoverServerAsync(
            int port = 3306, Action<int, int> progress = null, CancellationToken cancel = default(CancellationToken))
        {
            var best = await DiscoverBestAsync(port, progress, cancel).ConfigureAwait(false);
            return best?.Host;
        }

        /// <summary>
        /// Make sure this PC is pointed at the machine that is really serving the
        /// registry, adopting it when it is not. Returns true if a usable database is
        /// in effect afterwards.
        ///
        /// Cheap in the normal case: when the current server's beacon is live nothing
        /// is scanned at all, so the serving machine and a correctly-pointed client
        /// both start instantly. The LAN sweep only runs when the current database is
        /// unreachable OR nobody is serving it — which is exactly the case where a
        /// client was silently reading its own local copy.
        /// </summary>
        public static bool EnsureBestServer(Action<int, int> progress = null)
        {
            int port = Port > 0 ? Port : 3306;
            int? cur = CurrentBeaconAge();
            if (cur.HasValue && cur.Value <= LiveBeaconSeconds) return true;

            ServerCandidate best;
            try { best = DiscoverBestAsync(port, progress).GetAwaiter().GetResult(); }
            catch { best = null; }

            if (best != null && !string.IsNullOrEmpty(best.Host))
            {
                bool fresher    = best.Age.HasValue && (!cur.HasValue || best.Age.Value < cur.Value);
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
        /// Background watcher: keeps this PC on the machine that is serving the registry.
        /// Acts on BOTH failures — the server's Wi-Fi/hotspot IP changing (unreachable),
        /// and the quieter one, still reaching a database that nobody is serving (a local
        /// copy). Idle while the current server's beacon is live. Safe to call once at
        /// startup.
        /// </summary>
        public static void StartAutoReconnect(int intervalMs = 15000)
        {
            if (_reconnect != null) return;
            _reconnect = new Timer(_ => TryReconnect(), null, intervalMs, intervalMs);
        }

        private static void TryReconnect()
        {
            if (Interlocked.Exchange(ref _reconnecting, 1) == 1) return;   // one at a time
            try
            {
                int? age = CurrentBeaconAge();
                if (age.HasValue && age.Value <= LiveBeaconSeconds) return;   // healthy — skip
                // Reachable but unserved: re-sweeping the LAN every 15s would be wasteful
                // (the admin app may simply be closed), so throttle that case to 2 minutes.
                // A database we cannot reach at all keeps the fast retry.
                if (IsReachable() && (DateTime.UtcNow - _lastSweep).TotalSeconds < 120) return;
                _lastSweep = DateTime.UtcNow;
                EnsureBestServer();
            }
            catch { }
            finally { Interlocked.Exchange(ref _reconnecting, 0); }
        }

        /// <summary>Every /24 address to probe, from this PC's active IPv4 NICs (self excluded).</summary>
        private static List<string> CandidateHosts()
        {
            var list = new List<string>();
            var seenSubnets = new HashSet<string>();
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
                        if (b[0] == 169 && b[1] == 254) continue; // APIPA, no real link
                        string subnet = b[0] + "." + b[1] + "." + b[2] + ".";
                        if (!seenSubnets.Add(subnet)) continue;
                        for (int h = 1; h <= 254; h++)
                        {
                            if (h == b[3]) continue; // skip self (reached as 127.0.0.1 above)
                            list.Add(subnet + h);
                        }
                    }
                }
            }
            catch { /* fall through with whatever we have */ }
            return list;
        }

        /// <summary>True if a TCP connection to host:port opens within timeoutMs.</summary>
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
