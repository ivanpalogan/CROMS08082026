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
    /// If nothing is saved and localhost fails, <see cref="DiscoverServerAsync"/> sweeps
    /// the LAN to auto-find the server on the current Wi-Fi/hotspot (no IP to type).
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
                ? ConfigurationManager.AppSettings["LanDbPassword"].Trim() : "Croms#2026";

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

        /// <summary>True if the current effective connection opens.</summary>
        public static bool IsReachable()
        {
            try { using (var c = new MySqlConnection(EffectiveConnectionString)) { c.Open(); return true; } }
            catch { return false; }
        }

        /// <summary>True if host:port accepts the CROMS credentials.</summary>
        public static bool TestConnection(string host, int port)
        {
            try { using (var c = new MySqlConnection(BuildFor(host, port))) { c.Open(); return true; } }
            catch { return false; }
        }

        private static Timer _reconnect;
        private static int _reconnecting;

        /// <summary>
        /// Background watcher: while the DB is unreachable (server Wi-Fi/hotspot IP changed
        /// mid-session), re-scan the LAN and adopt the new IP — the board reconnects with no
        /// restart. Idle while healthy. Call once at startup.
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
                if (IsReachable()) return;
                string ip = DiscoverServerAsync(Port).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(ip) &&
                    !string.Equals(ip, Host, StringComparison.OrdinalIgnoreCase))
                    Save(ip, Port);
            }
            catch { }
            finally { Interlocked.Exchange(ref _reconnecting, 0); }
        }

        /// <summary>
        /// Sweep every /24 subnet this PC is on, TCP-probe :port, and return the first
        /// host that also accepts the CROMS credentials. Copes with the server's
        /// Wi-Fi/hotspot IP changing — no one has to know the number.
        /// </summary>
        public static async Task<string> DiscoverServerAsync(int port = 3306)
        {
            var hosts = CandidateHosts();
            if (hosts.Count == 0) return null;

            var found = new TaskCompletionSource<string>();
            var gate = new SemaphoreSlim(64);
            var tasks = hosts.Select(async ip =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (found.Task.IsCompleted) return;
                    if (await PortOpenAsync(ip, port, 400).ConfigureAwait(false) && TestConnection(ip, port))
                        found.TrySetResult(ip);
                }
                catch { }
                finally { gate.Release(); }
            }).ToArray();

            var winner = await Task.WhenAny(found.Task, Task.WhenAll(tasks)).ConfigureAwait(false);
            return winner == found.Task ? found.Task.Result : null;
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
