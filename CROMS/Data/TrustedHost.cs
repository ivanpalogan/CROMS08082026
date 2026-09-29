using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CROMS.Data
{
    /// <summary>
    /// The stable, publicly trusted HTTPS name the phones use (e.g. croms-lcro.duckdns.org).
    ///
    /// * The name's A record is pointed at this PC's PRIVATE LAN IP through the DuckDNS API, and
    ///   re-pointed whenever the IP changes (new Wi-Fi / router). That address is unroutable from
    ///   the Internet, so the service stays reachable only from the office network.
    /// * A Let's Encrypt certificate for that name is issued/renewed with the DNS-01 challenge
    ///   (ssl/get-cert.js in the mobile app folder), so nothing inbound is ever opened.
    /// * The certificate is tied to the NAME, not the IP: an IP change never needs a new one.
    /// * Phones already trust Let's Encrypt - no CA, profile or "proceed anyway" tap.
    ///
    /// Config (App.config): MobileHostname = name.duckdns.org, DuckDnsToken = the DuckDNS token.
    /// Blank = not configured; the mobile app then falls back to plain HTTP on the LAN IP.
    /// </summary>
    public static class TrustedHost
    {
        private const string Suffix = ".duckdns.org";
        private static readonly object _gate = new object();
        private static bool _issuing;
        private static string _lastPushedIp = "";
        private static DateTime _lastPushAt = DateTime.MinValue;
        private static Timer _maint;

        /// <summary>Raised (any thread) after a NEW certificate was written, so the servers that
        /// read it at launch can restart.</summary>
        public static event Action CertificateChanged;

        public static string Host
        {
            get
            {
                string h = (ConfigurationManager.AppSettings["MobileHostname"] ?? "").Trim().ToLowerInvariant();
                if (h.Length > 0 && !h.Contains(".")) h += Suffix;   // "croms-lcro" -> full name
                return Regex.IsMatch(h, @"^[a-z0-9-]+\.duckdns\.org$") ? h : "";
            }
        }

        private static string Token => (ConfigurationManager.AppSettings["DuckDnsToken"] ?? "").Trim();

        public static bool IsConfigured => Host.Length > 0 && Token.Length > 0;

        private static string SslDir => Path.Combine(IonicServerManager.Instance.AppPath, "ssl");
        private static string CertFile => Path.Combine(SslDir, "trusted-cert.pem");
        private static string KeyFile => Path.Combine(SslDir, "trusted-key.pem");

        /// <summary>True when a non-expired certificate for <see cref="Host"/> is on disk.</summary>
        public static bool CertReady
        {
            get
            {
                if (!IsConfigured || !File.Exists(CertFile) || !File.Exists(KeyFile)) return false;
                try
                {
                    string pem = File.ReadAllText(CertFile);
                    var m = Regex.Match(pem, @"-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----", RegexOptions.Singleline);
                    if (!m.Success) return false;
                    using (var c = new X509Certificate2(Convert.FromBase64String(Regex.Replace(m.Groups[1].Value, @"\s+", ""))))
                    {
                        if (c.NotAfter <= DateTime.Now.AddDays(1)) return false;
                        return c.GetNameInfo(X509NameType.DnsName, false).Equals(Host, StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch { return false; }
            }
        }

        /// <summary>Points Host's A record at <paramref name="lanIp"/>. Skips when unchanged
        /// (re-asserted every 30 minutes in case DuckDNS lost it). Blocking - call off the UI thread.</summary>
        public static bool UpdateDns(string lanIp)
        {
            if (!IsConfigured || string.IsNullOrEmpty(lanIp) || lanIp.StartsWith("127.")) return false;
            lock (_gate)
            {
                if (lanIp == _lastPushedIp && (DateTime.Now - _lastPushAt).TotalMinutes < 30) return true;
                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    string sub = Host.Substring(0, Host.Length - Suffix.Length);
                    string url = "https://www.duckdns.org/update?domains=" + Uri.EscapeDataString(sub) +
                                 "&token=" + Uri.EscapeDataString(Token) + "&ip=" + Uri.EscapeDataString(lanIp);
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 15000;
                    using (var resp = req.GetResponse())
                    using (var rd = new StreamReader(resp.GetResponseStream()))
                    {
                        if (!rd.ReadToEnd().Trim().StartsWith("OK", StringComparison.OrdinalIgnoreCase)) return false;
                    }
                    _lastPushedIp = lanIp; _lastPushAt = DateTime.Now;
                    return true;
                }
                catch { return false; }   // offline: try again on the next IP poll
            }
        }

        /// <summary>Issue/renew the certificate in the background (single-flight). Raises
        /// <see cref="CertificateChanged"/> only if a new one was actually written.</summary>
        public static void EnsureCertificateAsync()
        {
            if (!IsConfigured) return;
            lock (_gate) { if (_issuing) return; _issuing = true; }
            Task.Run(() =>
            {
                try
                {
                    // The A record must exist before the certificate matters to a phone; do it first.
                    UpdateDns(IonicServerManager.DetectLanIp());

                    string script = Path.Combine(SslDir, "get-cert.js");
                    if (!File.Exists(script)) return;
                    var psi = new ProcessStartInfo("cmd.exe", "/c node \"ssl/get-cert.js\"")
                    {
                        WorkingDirectory = IonicServerManager.Instance.AppPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                    };
                    psi.EnvironmentVariables["DUCKDNS_HOST"] = Host;
                    psi.EnvironmentVariables["DUCKDNS_TOKEN"] = Token;
                    using (var p = Process.Start(psi))
                    {
                        var err = p.StandardError.ReadToEndAsync();
                        string output = p.StandardOutput.ReadToEnd();
                        if (!p.WaitForExit(300000)) { try { p.Kill(); } catch { } return; }
                        try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "croms-trusted-cert.log"),
                                DateTime.Now + "\r\n" + output + "\r\n" + err.Result); } catch { }
                        if (p.ExitCode == 0 && output.Contains("ISSUED"))
                        {
                            var h = CertificateChanged;
                            if (h != null) { try { h(); } catch { } }
                        }
                    }
                }
                catch { /* stays on whatever certificate/HTTP fallback is in place */ }
                finally { lock (_gate) _issuing = false; }
            });
        }

        /// <summary>Daily renewal check (certificate is renewed 30 days before expiry).</summary>
        public static void StartMaintenance()
        {
            if (_maint != null || !IsConfigured) return;
            _maint = new Timer(_ => EnsureCertificateAsync(), null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
        }

        /// <summary>The IP the DuckDNS record was last successfully pointed at ("" = never).</summary>
        public static string PushedIp => _lastPushedIp;

        /// <summary>True when the record has NOT yet been confirmed for <paramref name="lanIp"/>
        /// (a previous push failed, e.g. the PC was offline right after joining the new Wi-Fi).</summary>
        public static bool NeedsDnsPush(string lanIp)
        {
            return IsConfigured && !string.IsNullOrEmpty(lanIp) && !lanIp.StartsWith("127.") && _lastPushedIp != lanIp;
        }

        private static string _dnsNote = "";
        private static DateTime _dnsCheckedAt = DateTime.MinValue;
        private static bool _dnsChecking;

        /// <summary>Resolves Host the way a phone will and compares it to the PC's current LAN IP.
        /// Cached 30s, runs in the background. Empty note = resolves correctly.</summary>
        public static void VerifyDnsAsync(string lanIp)
        {
            if (!IsConfigured || string.IsNullOrEmpty(lanIp)) { _dnsNote = ""; return; }
            if ((DateTime.Now - _dnsCheckedAt).TotalSeconds < 30 || _dnsChecking) return;
            _dnsChecking = true;
            Task.Run(() =>
            {
                try
                {
                    string note;
                    try
                    {
                        bool ok = false; bool any = false;
                        foreach (var a in Dns.GetHostAddresses(Host))
                        {
                            any = true;
                            if (a.ToString() == lanIp) ok = true;
                        }
                        note = ok ? "" : any
                            ? Host + " still points at an old address - it updates within a minute. If it stays this way the router is blocking public names that resolve to private IPs (DNS rebind protection): allow duckdns.org in the router."
                            : "";
                    }
                    catch
                    {
                        note = "Cannot resolve " + Host + " yet (offline, or the router blocks it as DNS rebinding - allow duckdns.org in the router).";
                    }
                    _dnsNote = note;
                }
                finally { _dnsCheckedAt = DateTime.Now; _dnsChecking = false; }
            });
        }

        /// <summary>One-line status for the Dashboard hint.</summary>
        public static string StatusNote()
        {
            if (!IsConfigured)
                return "Trusted HTTPS is not set up: set MobileHostname and DuckDnsToken in App.config (see ssl/README.md).";
            if (!CertReady) return "Getting the trusted certificate for " + Host + " (first run takes a few minutes, needs Internet)...";
            if (_dnsNote.Length > 0) return _dnsNote;
            return "If a phone cannot open the page on this Wi-Fi, the network may block device-to-device traffic (client/AP isolation) - use another Wi-Fi or the PC's hotspot.";
        }
    }
}
