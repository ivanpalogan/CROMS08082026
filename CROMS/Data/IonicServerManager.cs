using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CROMS.Data
{
    public enum IonicStatus { Stopped, Starting, Running, Error }

    /// <summary>
    /// Auto-starts the Ionic dev server (`ionic serve --host=0.0.0.0`) in the
    /// background when the desktop app launches, so staff never open a separate
    /// Command Prompt. It detects the PC's current Wi-Fi/LAN IPv4 (recomputed on
    /// every start, so a new network just works), watches the server until its
    /// port is actually listening, exposes the single mobile-connection URL for
    /// the Login screen (and QR), auto-restarts on an unexpected crash, and kills
    /// the whole process tree (no orphan node/cmd) when the app closes.
    ///
    /// Singleton: use <see cref="Instance"/>. All state changes raise
    /// <see cref="Changed"/> (on a background thread — subscribers marshal to UI).
    /// </summary>
    public sealed class IonicServerManager
    {
        // Two servers can auto-start: the certificate-scanner app (Instance, :4200) and
        // the claimapp ID-upload app (ClaimApp, :4300). Each is an independent instance
        // with its own folder / serve command / port, so one CROMS launch brings up both.
        // Served over plain HTTP (no --ssl) so the scanner QR opens with no cert warning.
        // Its capture uses the native camera file-input (no secure context needed).
        public static readonly IonicServerManager Instance = new IonicServerManager(
            "Mobile", "IonicAppPath", @"C:\Users\ivan palogan\ORCMobile_Application",
            "MobileServeCommand", "npx ng serve --host 0.0.0.0 --port 4200 --disable-host-check", 4200,
            "MobileScheme", "http");
        // claimapp is served over HTTPS (angular.json sets ssl:true, cert in claimapp/ssl/)
        // because the ID-upload page now runs a LIVE in-page camera (edge detection while
        // framing the ID) via getUserMedia, which browsers refuse outside a secure context.
        // The self-signed cert means one "connection is not private" tap-through per phone;
        // the client can still fall back to picking a photo from the gallery with no camera
        // permission at all if they decline it.
        public static readonly IonicServerManager ClaimApp = new IonicServerManager(
            "ClaimApp", "ClaimAppPath", @"C:\Users\ivan palogan\claimapp",
            "ClaimAppServeCommand", "npx ng serve --host 0.0.0.0 --port 4300 --disable-host-check", 4300,
            "ClaimAppScheme", "https");

        private readonly string _label, _appPathKey, _appPathDefault, _serveCmdKey, _serveCmdDefault,
                                _schemeKey, _schemeDefault;
        private IonicServerManager(string label, string appPathKey, string appPathDefault,
            string serveCmdKey, string serveCmdDefault, int defaultPort,
            string schemeKey, string schemeDefault)
        {
            _label = label;
            _appPathKey = appPathKey; _appPathDefault = appPathDefault;
            _serveCmdKey = serveCmdKey; _serveCmdDefault = serveCmdDefault;
            _schemeKey = schemeKey; _schemeDefault = schemeDefault;
            Port = defaultPort;
        }

        // ---- public state ----
        public IonicStatus Status { get; private set; } = IonicStatus.Stopped;
        public string MobileUrl { get; private set; } = "";
        public string LanIp { get; private set; } = "";
        public int Port { get; private set; }
        public string LastError { get; private set; } = "";
        public string LogFile { get; private set; }
        public string NetworkType { get; private set; } = "";   // Wi-Fi / Hotspot / LAN
        public string SessionToken { get; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public string ApiPort =>
            (ConfigurationManager.AppSettings["MobileApiPort"] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings["MobileApiPort"].Trim()
                : "3000";

        // What the QR encodes: the app URL PLUS the connection details a native
        // app needs to auto-pair (server host, api port, session id). A browser
        // that is served the app ignores these and uses same-origin.
        public string QrPayload =>
            (Status == IonicStatus.Running && !string.IsNullOrEmpty(MobileUrl))
                ? MobileUrl + "?sid=" + SessionToken + "&host=" + LanIp + "&api=" + ApiPort
                : MobileUrl;

        /// <summary>Raised whenever Status / MobileUrl changes (any thread).</summary>
        public event Action Changed;

        // ---- config (per-instance keys, so Mobile and ClaimApp differ) ----
        public string AppPath =>
            (ConfigurationManager.AppSettings[_appPathKey] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings[_appPathKey].Trim()
                : _appPathDefault;

        // Full command run after `cmd /c`. Defaults to an HTTPS Angular dev server
        // so the phone (a secure context is required for the camera) can connect.
        // --disable-host-check lets the phone reach it by LAN IP.
        public string ServeCommand =>
            (ConfigurationManager.AppSettings[_serveCmdKey] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings[_serveCmdKey].Trim()
                : _serveCmdDefault;

        // URL scheme the QR/URL is built with. Per-instance (Mobile=https for its --ssl
        // camera server, ClaimApp=http so the QR opens with no cert warning).
        public string Scheme =>
            (ConfigurationManager.AppSettings[_schemeKey] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings[_schemeKey].Trim()
                : _schemeDefault;

        private const int MaxRestarts = 3;

        // ---- internals ----
        private readonly object _lock = new object();
        private Process _proc;
        private Timer _portPoll;
        private Timer _restartTimer;
        private Timer _ipPoll;
        private bool _shuttingDown;
        private int _restartAttempts;
        private DateTime _startedAt;
        private readonly StringBuilder _recentOutput = new StringBuilder();

        // -----------------------------------------------------------------
        public void Start()
        {
            lock (_lock)
            {
                if (Status == IonicStatus.Starting || Status == IonicStatus.Running) return;
                _shuttingDown = false;

                if (!Directory.Exists(AppPath) ||
                    !(File.Exists(Path.Combine(AppPath, "ionic.config.json")) ||
                      File.Exists(Path.Combine(AppPath, "package.json"))))
                {
                    Fail("Ionic app folder not found: " + AppPath +
                         "\r\nSet <appSettings> key 'IonicAppPath' in App.config.");
                    return;
                }

                var lan = DetectLan();
                LanIp = lan.ip;
                NetworkType = lan.type;
                MobileUrl = "";
                LastError = "";
                _recentOutput.Clear();
                SetStatus(IonicStatus.Starting);

                // HTTPS instances (the camera needs a secure context) carry a self-signed
                // dev cert whose SAN list is baked in at generation time. A laptop that
                // moves to a genuinely new network (new router, hotspot vs Wi-Fi) gets an
                // IP the old cert never listed, and the phone's TLS handshake fails outright
                // ("can't be reached") rather than the usual click-through warning. Cover
                // THIS ip before launching, so the server never starts serving a cert that
                // is already known to be wrong for the network it is on.
                if (Scheme == "https") EnsureCertCoversIp(LanIp);

                try
                {
                    LogFile = Path.Combine(Path.GetTempPath(), "croms-ionic-serve-" + _label + ".log");
                    try { File.WriteAllText(LogFile, "CROMS ionic serve — " + DateTime.Now + Environment.NewLine); } catch { }

                    // Free the port first: a leftover node from a previous run holding it makes
                    // `ng serve` fail with "Port is already in use" and give up. Kill whatever
                    // is listening on our port so startup always succeeds.
                    FreePort(Port);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        // /c so cmd exits when the server exits. The command is
                        // configurable (App.config 'MobileServeCommand') so it can be
                        // changed without a rebuild; default is an HTTPS ng serve.
                        Arguments = "/c " + ServeCommand,
                        WorkingDirectory = AppPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    };

                    _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                    _proc.OutputDataReceived += OnOutput;
                    _proc.ErrorDataReceived += OnOutput;
                    _proc.Exited += OnProcessExited;

                    _proc.Start();
                    _proc.BeginOutputReadLine();
                    _proc.BeginErrorReadLine();
                    _startedAt = DateTime.Now;

                    StartPortPolling();

                    // Watch for the laptop's IP changing (switched wifi/hotspot) and
                    // regenerate the URL/QR live, without a restart.
                    try { _ipPoll?.Dispose(); } catch { }
                    _ipPoll = new Timer(_ => CheckIpChange(), null, 10000, 10000);
                }
                catch (Exception ex)
                {
                    Fail("Could not launch ionic serve: " + ex.Message);
                }
            }
        }

        /// <summary>User-triggered retry from the Login screen.</summary>
        public void Retry()
        {
            Stop();
            lock (_lock) { _restartAttempts = 0; _shuttingDown = false; }
            Start();
        }

        /// <summary>Stop the server and kill the whole cmd/node process tree.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                _shuttingDown = true;
                try { _portPoll?.Dispose(); } catch { } _portPoll = null;
                try { _restartTimer?.Dispose(); } catch { } _restartTimer = null;
                try { _ipPoll?.Dispose(); } catch { } _ipPoll = null;

                if (_proc != null)
                {
                    try { if (!_proc.HasExited) KillTree(_proc.Id); } catch { }
                    try { _proc.Dispose(); } catch { }
                    _proc = null;
                }
                SetStatus(IonicStatus.Stopped);
            }
        }

        // ---- output handling --------------------------------------------
        private void OnOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            try { File.AppendAllText(LogFile, e.Data + Environment.NewLine); } catch { }
            lock (_lock)
            {
                _recentOutput.AppendLine(e.Data);
                if (_recentOutput.Length > 4000) _recentOutput.Remove(0, _recentOutput.Length - 4000);
            }

            // Pick up a non-default port if ionic chose one (e.g. 8101 when 8100 busy).
            Match m = Regex.Match(e.Data, @"localhost:(\d{2,5})", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int p) && p != Port)
            {
                lock (_lock) { Port = p; }
            }

            if (Regex.IsMatch(e.Data, @"is not recognized|command not found", RegexOptions.IgnoreCase))
            {
                Fail("Mobile dev server did not start (command not found). Run 'npm install' in the mobile app folder, and check Node/Angular CLI are installed.");
            }
        }

        // ---- readiness: poll the port until it accepts connections -------
        private void StartPortPolling()
        {
            _portPoll = new Timer(_ =>
            {
                if (_shuttingDown) return;
                if (IsPortOpen("127.0.0.1", Port))
                {
                    lock (_lock)
                    {
                        try { _portPoll?.Dispose(); } catch { } _portPoll = null;
                        _restartAttempts = 0;
                        MobileUrl = Scheme + "://" + LanIp + ":" + Port;
                        SetStatus(IonicStatus.Running);
                    }
                }
                else if ((DateTime.Now - _startedAt).TotalSeconds > 150)
                {
                    // Took too long — if the process already died, surface an error.
                    if (_proc == null || _proc.HasExited)
                        Fail("Ionic server did not start. See log: " + LogFile);
                }
            }, null, 2000, 1500);
        }

        private static bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var c = new TcpClient())
                {
                    var ar = c.BeginConnect(host, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(600);
                    if (ok) { c.EndConnect(ar); return true; }
                    return false;
                }
            }
            catch { return false; }
        }

        // ---- crash handling / auto-restart ------------------------------
        private void OnProcessExited(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (_shuttingDown) { SetStatus(IonicStatus.Stopped); return; }

                // Very fast exit during startup usually means a bad command.
                if (Status == IonicStatus.Starting && (DateTime.Now - _startedAt).TotalSeconds < 4 &&
                    _recentOutput.ToString().IndexOf("not recognized", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Fail("Mobile dev server did not start (command not found). Run 'npm install' in the mobile app folder, and check Node/Angular CLI are installed.");
                    return;
                }

                if (_restartAttempts < MaxRestarts)
                {
                    _restartAttempts++;
                    SetStatus(IonicStatus.Starting);
                    _restartTimer = new Timer(__ =>
                    {
                        try { _restartTimer?.Dispose(); } catch { } _restartTimer = null;
                        if (!_shuttingDown) Start();
                    }, null, 2500, Timeout.Infinite);
                }
                else
                {
                    Fail("Ionic server stopped unexpectedly and could not be restarted. Log: " + LogFile);
                }
            }
        }

        // ---- helpers -----------------------------------------------------
        private void Fail(string message)
        {
            LastError = message;
            SetStatus(IonicStatus.Error);
        }

        private void SetStatus(IonicStatus s)
        {
            Status = s;
            Raise();
        }

        private void Raise()
        {
            var h = Changed;
            if (h != null) { try { h(); } catch { } }
        }

        /// <summary>
        /// If the laptop's IP changed (new Wi-Fi/hotspot), update the URL/QR live. For an
        /// HTTPS instance, a genuinely new network means a new IP the dev cert's SAN list
        /// was never generated for — updating the URL alone would point the QR at an
        /// address the phone's TLS handshake refuses. So this REGENERATES the cert for the
        /// new IP and restarts the server (the only way `ng serve` picks up a changed cert
        /// file) whenever the IP isn't already covered; when it IS already covered (the
        /// cert was made with several addresses, or the network is one seen before) it
        /// stays on the fast, no-downtime path of just updating the URL in place.
        /// </summary>
        private void CheckIpChange()
        {
            if (_shuttingDown || Status != IonicStatus.Running) return;
            var lan = DetectLan();
            if (lan.ip == LanIp) return;

            if (Scheme == "https" && !CertCoversIp(lan.ip))
            {
                EnsureCertCoversIp(lan.ip);   // regenerate to include the new address
                Restart();                    // ng serve only reads the cert file at launch
                return;
            }

            LanIp = lan.ip;
            NetworkType = lan.type;
            MobileUrl = Scheme + "://" + LanIp + ":" + Port;
            Raise();   // dashboard regenerates the QR + URL
        }

        /// <summary>Stop then Start — used when the network changed enough that the
        /// server has to relaunch (a new cert) rather than just report a new URL.</summary>
        private void Restart()
        {
            Stop();
            lock (_lock) { _restartAttempts = 0; _shuttingDown = false; }
            Start();
        }

        // ---- HTTPS dev-cert auto-renewal ---------------------------------

        /// <summary>Full path to this instance's self-signed dev cert, or null if this
        /// app has no ssl/ folder (it isn't served over HTTPS).</summary>
        private string CertPath => Path.Combine(AppPath, "ssl", "dev-cert.pem");

        /// <summary>True if the cert at <see cref="CertPath"/> already lists <paramref
        /// name="ip"/> in its Subject Alternative Name — read directly off the DER-decoded
        /// certificate rather than re-parsing the PEM text, so it can't be fooled by the
        /// ip appearing anywhere else in the file (a comment, the CN, ...).</summary>
        private bool CertCoversIp(string ip)
        {
            try
            {
                string path = CertPath;
                if (!File.Exists(path)) return false;
                byte[] der = PemToDer(File.ReadAllText(path), "CERTIFICATE");
                if (der == null) return false;
                using (var cert = new X509Certificate2(der))
                {
                    foreach (X509Extension ext in cert.Extensions)
                    {
                        if (ext.Oid == null || ext.Oid.Value != "2.5.29.17") continue; // subjectAltName
                        string text = ext.Format(false);
                        if (text.IndexOf(ip, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>Regenerates the dev cert to include <paramref name="ip"/> (on top of
        /// whatever it already covers — <c>ssl/make-cert.sh</c> always lists every IPv4
        /// this machine currently has, plus the one passed in) by running the app's own
        /// cert script through Git's bundled bash. Best-effort: if bash/openssl aren't on
        /// this PC, the server just keeps whatever cert it already had — same as before
        /// this method existed, no regression, only a missed opportunity to self-heal.</summary>
        private void EnsureCertCoversIp(string ip)
        {
            try
            {
                if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1") return;
                if (CertCoversIp(ip)) return; // already fine - nothing to do

                // 1) ssl/make-cert.js - pure Node (the selfsigned package the app already
                //    ships with), so it works on any PC that can run `ng serve`.
                string js = Path.Combine(AppPath, "ssl", "make-cert.js");
                if (File.Exists(js) && RunCertTool("cmd.exe", "/c node \"ssl/make-cert.js\" " + ip) && CertCoversIp(ip))
                    return;

                // 2) older apps: ssl/make-cert.sh through Git's bash + openssl, if installed.
                string script = Path.Combine(AppPath, "ssl", "make-cert.sh");
                if (!File.Exists(script)) return;
                string bash = FindBash();
                if (bash == null) return;
                RunCertTool(bash, "\"" + script.Replace('\\', '/') + "\" " + ip);
            }
            catch { }
        }

        private bool RunCertTool(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    WorkingDirectory = AppPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.OutputDataReceived += (a, b) => { };
                    p.ErrorDataReceived += (a, b) => { };
                    p.BeginOutputReadLine(); p.BeginErrorReadLine();
                    if (!p.WaitForExit(30000)) { try { p.Kill(); } catch { } return false; }
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        /// <summary>Locates Git for Windows' bash.exe (the one <c>ssl/make-cert.sh</c>
        /// needs), checking the usual install paths before falling back to PATH.</summary>
        private static string FindBash()
        {
            string[] candidates =
            {
                @"C:\Program Files\Git\bin\bash.exe",
                @"C:\Program Files\Git\usr\bin\bash.exe",
                @"C:\Program Files (x86)\Git\bin\bash.exe",
            };
            foreach (var c in candidates) if (File.Exists(c)) return c;
            try
            {
                var psi = new ProcessStartInfo("where", "bash")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    string line = outp.Split('\n').FirstOrDefault(l => l.Trim().Length > 0);
                    if (!string.IsNullOrWhiteSpace(line)) return line.Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>Decodes the base64 body of a PEM block (between the BEGIN/END
        /// &lt;label&gt; markers) into raw DER bytes.</summary>
        private static byte[] PemToDer(string pem, string label)
        {
            var m = Regex.Match(pem, @"-----BEGIN " + label + @"-----(.*?)-----END " + label + @"-----",
                RegexOptions.Singleline);
            if (!m.Success) return null;
            string body = Regex.Replace(m.Groups[1].Value, @"\s+", "");
            try { return Convert.FromBase64String(body); } catch { return null; }
        }

        /// <summary>
        /// Kill any process currently LISTENING on <paramref name="port"/> (a leftover
        /// node/ng from an earlier run). Never touches this app's own process.
        /// </summary>
        private void FreePort(int port)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c netstat -ano -p tcp")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                string output;
                using (var p = Process.Start(psi)) { output = p.StandardOutput.ReadToEnd(); p.WaitForExit(4000); }

                int self = Process.GetCurrentProcess().Id;
                var pids = new System.Collections.Generic.HashSet<int>();
                foreach (var line in output.Split('\n'))
                {
                    if (line.IndexOf("LISTENING", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    // e.g. "  TCP    0.0.0.0:4300   0.0.0.0:0   LISTENING   17764"
                    var m = Regex.Match(line, @":" + port + @"\s+\S+\s+LISTENING\s+(\d+)", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int pid) && pid > 0 && pid != self)
                        pids.Add(pid);
                }
                foreach (var pid in pids) KillTree(pid);
                if (pids.Count > 0) Thread.Sleep(400);   // let the OS release the socket
            }
            catch { }
        }

        private static void KillTree(int pid)
        {
            try
            {
                var psi = new ProcessStartInfo("taskkill", "/PID " + pid + " /T /F")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi)) { p.WaitForExit(4000); }
            }
            catch { }
        }

        /// <summary>
        /// Best current IPv4 that a phone on the same Wi-Fi can reach. Recomputed
        /// each start, so switching networks is handled automatically. Prefers a
        /// real Wi-Fi/Ethernet adapter that has a gateway and a private-range
        /// address; skips loopback, APIPA (169.254.*) and obvious virtual adapters.
        /// </summary>
        public static string DetectLanIp() => DetectLan().ip;

        /// <summary>Best reachable IPv4 + the kind of network (Wi-Fi / Hotspot / LAN).</summary>
        public static (string ip, string type) DetectLan()
        {
            string best = null, bestType = "Network"; int bestScore = int.MinValue;
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    var ipProps = ni.GetIPProperties();
                    bool hasGateway = ipProps.GatewayAddresses
                        .Any(g => g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork &&
                                  !g.Address.Equals(IPAddress.Any));
                    string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                    bool virtualAdapter = desc.Contains("virtual") || desc.Contains("vmware") ||
                                          desc.Contains("virtualbox") || desc.Contains("hyper-v") ||
                                          desc.Contains("loopback") || desc.Contains("vethernet");
                    bool hotspotAdapter = desc.Contains("hosted network") || desc.Contains("wi-fi direct") ||
                                          desc.Contains("mobile hotspot") || desc.Contains("microsoft wi-fi");

                    foreach (var ua in ipProps.UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip)) continue;
                        string s = ip.ToString();
                        if (s.StartsWith("169.254.")) continue; // APIPA (no DHCP)

                        bool isHotspot = s.StartsWith("192.168.137.") || hotspotAdapter;

                        int score = 0;
                        if (hasGateway) score += 8;
                        if (isHotspot) score += 7;   // a live hotspot is a valid phone target even w/o gateway
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) score += 4;
                        else if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) score += 3;
                        if (IsPrivate(s)) score += 2;
                        if (virtualAdapter && !isHotspot) score -= 6;

                        if (score > bestScore)
                        {
                            bestScore = score; best = s;
                            bestType = isHotspot ? "Hotspot"
                                     : ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi"
                                     : ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "LAN" : "Network";
                        }
                    }
                }
            }
            catch { }

            if (best != null) return (best, bestType);

            // Fallback: any non-loopback IPv4 for this host.
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a =>
                    a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a) &&
                    !a.ToString().StartsWith("169.254."));
                if (ip != null) return (ip.ToString(), "Network");
            }
            catch { }

            return ("127.0.0.1", "Local");
        }

        private static bool IsPrivate(string ip)
        {
            return ip.StartsWith("192.168.") || ip.StartsWith("10.") ||
                   Regex.IsMatch(ip, @"^172\.(1[6-9]|2\d|3[01])\.");
        }
    }
}
