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
                return new MySqlConnectionStringBuilder(BaseConnectionString)
                {
                    Server = host,
                    Port = (uint)(port > 0 ? port : 3306)
                }.ConnectionString;
            }
            catch { return BaseConnectionString; }
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

        /// <summary>
        /// Auto-discover the CROMS database server on the local network. Sweeps
        /// every /24 subnet this PC is on (e.g. 192.168.1.x, 192.168.137.x for a
        /// hotspot), TCP-probes each address on <paramref name="port"/>, and returns
        /// the first host that ALSO accepts the CROMS credentials (a bare open port
        /// isn't enough — some other MySQL could answer). This is how a client copes
        /// with the server's Wi-Fi/hotspot IP changing: no one has to know the number.
        /// </summary>
        /// <param name="port">MySQL port (3306).</param>
        /// <param name="progress">Optional "scanned/total" callback for the UI.</param>
        /// <param name="cancel">Cancels the sweep early (e.g. a Stop button / found).</param>
        /// <returns>The server IP if found, else null.</returns>
        public static async Task<string> DiscoverServerAsync(
            int port = 3306, Action<int, int> progress = null, CancellationToken cancel = default)
        {
            var hosts = CandidateHosts();
            if (hosts.Count == 0) return null;

            int scanned = 0;
            var found = new TaskCompletionSource<string>();
            // Bound concurrency so we don't open 500 sockets at once.
            var gate = new SemaphoreSlim(64);

            var tasks = hosts.Select(async ip =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (cancel.IsCancellationRequested || found.Task.IsCompleted) return;
                    if (await PortOpenAsync(ip, port, 400).ConfigureAwait(false))
                    {
                        // Port answered — confirm it's really CROMS (auth + DB).
                        if (TestConnection(ip, port, out _))
                            found.TrySetResult(ip);
                    }
                }
                catch { /* unreachable host — ignore */ }
                finally
                {
                    Interlocked.Increment(ref scanned);
                    progress?.Invoke(scanned, hosts.Count);
                    gate.Release();
                }
            }).ToArray();

            var all = Task.WhenAll(tasks);
            var winner = await Task.WhenAny(found.Task, all).ConfigureAwait(false);
            return winner == found.Task ? found.Task.Result : null;
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
                            if (h == b[3]) continue; // skip self
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
