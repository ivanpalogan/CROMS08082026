using System;
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
    public enum MobilePhase { NotConfigured, NetworkUnavailable, Working, Failed, Ready }

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
    /// The hostname and token are entered once in the Mobile Capture Setup window and kept by
    /// <see cref="MobileCaptureConfig"/> (token encrypted per Windows account). Nothing is edited
    /// in App.config. Not configured = Mobile Capture asks for the setup instead of handing out a link.
    /// </summary>
    public static class TrustedHost
    {
        private const string Suffix = ".duckdns.org";

        /// <summary>The save-API's HTTPS port (the Mobile Capture page and QR).</summary>
        public const int HttpsPort = 3443;

        private static readonly object _gate = new object();
        private static bool _issuing;
        private static bool _pushing;
        private static string _lastPushedIp = "";
        private static DateTime _lastPushAt = DateTime.MinValue;
        private static Timer _maint;
        private static Timer _netWatch;
        private static string _lastError = "";
        private static string _lastSeenIp = "";

        /// <summary>Raised (any thread) after a NEW certificate was written, so the servers that
        /// read it at launch can restart.</summary>
        public static event Action CertificateChanged;

        /// <summary>Raised (any thread) when the setup state changes (configured, issuing, failed, ready, IP moved).</summary>
        public static event Action StateChanged;

        private static void RaiseState()
        {
            var h = StateChanged;
            if (h != null) { try { h(); } catch { } }
        }

        public static string Host { get { return MobileCaptureConfig.Hostname; } }

        private static string Token { get { return MobileCaptureConfig.Token; } }

        public static bool IsConfigured { get { return Host.Length > 0 && Token.Length > 0; } }

        public static bool IsIssuing { get { return _issuing; } }

        /// <summary>The actual reason the last certificate attempt failed ("" = none). Never contains the token.</summary>
        public static string LastError { get { return _lastError; } }

        private static string SslDir { get { return Path.Combine(IonicServerManager.Instance.AppPath, "ssl"); } }
        private static string CertFile { get { return Path.Combine(SslDir, "trusted-cert.pem"); } }
        private static string KeyFile { get { return Path.Combine(SslDir, "trusted-key.pem"); } }

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

        /// <summary>Where Mobile Capture setup currently stands, in the order the operator meets it.</summary>
        public static MobilePhase Phase
        {
            get
            {
                if (!IsConfigured) return MobilePhase.NotConfigured;
                if (IonicServerManager.DetectLanIp().StartsWith("127.")) return MobilePhase.NetworkUnavailable;
                if (CertReady) return MobilePhase.Ready;
                if (_issuing) return MobilePhase.Working;
                if (_lastError.Length > 0) return MobilePhase.Failed;
                return MobilePhase.Working;
            }
        }

        // ------------------------------------------------------------------ DNS

        /// <summary>One DuckDNS update. The token travels only in this request; every failure is turned
        /// into a plain sentence that never repeats the URL or the token.</summary>
        private static bool PushDns(string host, string token, string ip, out string error)
        {
            error = "";
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                string sub = host.Substring(0, host.Length - Suffix.Length);
                string url = "https://www.duckdns.org/update?domains=" + Uri.EscapeDataString(sub) +
                             "&token=" + Uri.EscapeDataString(token) + "&ip=" + Uri.EscapeDataString(ip);
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 15000;
                using (var resp = req.GetResponse())
                using (var rd = new StreamReader(resp.GetResponseStream()))
                {
                    if (rd.ReadToEnd().Trim().StartsWith("OK", StringComparison.OrdinalIgnoreCase)) return true;
                }
                error = "DuckDNS did not accept the name and token. Check both are typed exactly as shown on your DuckDNS page.";
                return false;
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                    error = "DuckDNS answered with an error (HTTP " + (int)((HttpWebResponse)wex.Response).StatusCode + "). Try again in a moment.";
                else if (wex.Status == WebExceptionStatus.NameResolutionFailure || wex.Status == WebExceptionStatus.ConnectFailure ||
                         wex.Status == WebExceptionStatus.Timeout)
                    error = "Could not reach duckdns.org. Check that this PC has an Internet connection.";
                else
                    error = "Could not contact DuckDNS (" + wex.Status + ").";
                return false;
            }
            catch (Exception)
            {
                error = "Could not contact DuckDNS. Check the Internet connection and try again.";
                return false;
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
                string err;
                if (!PushDns(Host, Token, lanIp, out err)) return false;   // offline: retried on the next IP poll
                _lastPushedIp = lanIp; _lastPushAt = DateTime.Now;
                return true;
            }
        }

        /// <summary>Setup-window "Test Connection": checks the network, DuckDNS and the runtime WITHOUT
        /// saving anything. A blank token uses the saved one. Blocking - call off the UI thread.</summary>
        public static bool Test(string hostInput, string tokenInput, out string message)
        {
            string err;
            string host = MobileCaptureConfig.NormalizeHost(hostInput, out err);
            if (host.Length == 0) { message = err; return false; }

            string token = (tokenInput ?? "").Trim();
            if (token.Length == 0) token = MobileCaptureConfig.Token;
            if (!MobileCaptureConfig.TokenLooksValid(token, out err)) { message = err; return false; }

            string ip = IonicServerManager.DetectLanIp();
            if (ip.StartsWith("127."))
            {
                message = "Mobile Capture network unavailable. This PC is not connected to a Wi-Fi or LAN network.";
                return false;
            }
            if (!PushDns(host, token, ip, out err)) { message = err; return false; }

            if (MobileRuntime.FindNode() == null)
            {
                message = "DuckDNS accepted the name and token, but the Mobile Capture runtime (Node.js) was not found on this PC. " +
                          "Use the CROMS release that includes the MobileApp folder.";
                return false;
            }
            message = "Connected. DuckDNS accepted the name and token, and " + host + " now points at this PC (" + ip + ").";
            return true;
        }

        // ------------------------------------------------------------------ certificate

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string t = text;
            string token = Token;
            if (token.Length > 0) t = t.Replace(token, "****");
            return t;
        }

        /// <summary>The single most useful line out of the certificate script's output.</summary>
        private static string ReasonFrom(string output, string errors)
        {
            string all = Clean((errors ?? "") + "\n" + (output ?? ""));
            foreach (string line in all.Split('\n'))
            {
                string l = line.Trim();
                if (l.StartsWith("get-cert failed:", StringComparison.OrdinalIgnoreCase))
                    return l.Substring("get-cert failed:".Length).Trim();
            }
            if (all.IndexOf("Cannot find module", StringComparison.OrdinalIgnoreCase) >= 0)
                return "The Mobile Capture runtime is incomplete (a required Node module is missing). Use the CROMS release that includes the MobileApp folder.";
            foreach (string line in all.Split('\n'))
            {
                string l = line.Trim();
                if (l.Length > 0) return l.Length > 300 ? l.Substring(0, 300) : l;
            }
            return "The certificate request did not complete (no details were reported).";
        }

        /// <summary>Issue/renew the certificate in the background (single-flight). Raises
        /// <see cref="CertificateChanged"/> only if a new one was actually written.</summary>
        public static void EnsureCertificateAsync()
        {
            if (!IsConfigured) return;
            lock (_gate) { if (_issuing) return; _issuing = true; }
            RaiseState();
            Task.Run(() =>
            {
                string failure = "";
                bool issued = false;
                try
                {
                    // The A record must exist before the certificate matters to a phone; do it first.
                    UpdateDns(IonicServerManager.DetectLanIp());

                    string script = Path.Combine(SslDir, "get-cert.js");
                    string node = MobileRuntime.FindNode();
                    if (!File.Exists(script))
                        failure = "The Mobile Capture runtime is missing its certificate tool (" + script + ").";
                    else if (node == null)
                        failure = "Node.js was not found on this PC. Use the CROMS release that includes the MobileApp folder.";
                    else
                    {
                        var psi = new ProcessStartInfo(node, "\"" + Path.Combine("ssl", "get-cert.js") + "\"")
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
                            if (!p.WaitForExit(300000))
                            {
                                try { p.Kill(); } catch { }
                                failure = "The certificate request timed out after 5 minutes. Check the Internet connection and try again.";
                            }
                            else
                            {
                                string errText = err.Result;
                                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "croms-trusted-cert.log"),
                                        DateTime.Now + "\r\n" + Clean(output) + "\r\n" + Clean(errText)); } catch { }
                                if (p.ExitCode == 0) issued = output.Contains("ISSUED");
                                else failure = ReasonFrom(output, errText);
                            }
                        }
                    }
                }
                catch (Exception ex) { failure = Clean(ex.Message); }
                finally
                {
                    _lastError = failure;
                    lock (_gate) _issuing = false;
                    RaiseState();
                }
                if (issued)
                {
                    var h = CertificateChanged;
                    if (h != null) { try { h(); } catch { } }
                }
            });
        }

        // ------------------------------------------------------------------ lifecycle

        /// <summary>Starts everything Mobile Capture needs at launch, independent of the Ionic servers:
        /// points the hostname at this PC, issues/renews the certificate, and keeps both current when
        /// the Wi-Fi or IP changes. Safe to call repeatedly; does nothing until configured.</summary>
        public static void Start()
        {
            if (!IsConfigured) return;
            string ip = IonicServerManager.DetectLanIp();
            Task.Run(() => UpdateDns(ip));
            EnsureCertificateAsync();
            StartMaintenance();
        }

        /// <summary>Called after the setup window saved new settings: forgets everything learned from the
        /// previous name, starts the pieces, and relaunches the servers if a matching certificate is
        /// already on disk (they may have started in plain-HTTP mode before there was a name).</summary>
        public static void Apply()
        {
            _lastPushedIp = ""; _lastPushAt = DateTime.MinValue; _dnsNote = ""; _dnsCheckedAt = DateTime.MinValue;
            _lastError = "";
            try { ApiServerManager.Instance.Start(); } catch { }
            Start();
            RaiseState();
            if (CertReady)
            {
                var h = CertificateChanged;
                if (h != null) { try { h(); } catch { } }
            }
        }

        /// <summary>Daily certificate renewal check (renewed 30 days before expiry) plus a 10-second
        /// watch on the PC's network address, so a new Wi-Fi/IP re-points the same hostname without
        /// a restart, a re-issued certificate or any reconfiguration.</summary>
        public static void StartMaintenance()
        {
            if (!IsConfigured) return;
            if (_maint == null)
                _maint = new Timer(_ => EnsureCertificateAsync(), null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
            if (_netWatch == null)
            {
                _lastSeenIp = IonicServerManager.DetectLanIp();
                _netWatch = new Timer(_ => WatchNetwork(), null, 10000, 10000);
            }
        }

        private static void WatchNetwork()
        {
            try
            {
                if (!IsConfigured) return;
                string ip = IonicServerManager.DetectLanIp();
                if (ip != _lastSeenIp) { _lastSeenIp = ip; RaiseState(); }
                if (ip.StartsWith("127.")) return;

                // A push that failed (PC still offline right after joining the new Wi-Fi) is retried every
                // poll until DuckDNS confirms it; an unchanged address is re-asserted every 30 minutes.
                if (!_pushing)
                {
                    _pushing = true;
                    Task.Run(() => { try { UpdateDns(ip); } finally { _pushing = false; } });
                }
                VerifyDnsAsync(ip);
            }
            catch { }
        }

        /// <summary>The IP the DuckDNS record was last successfully pointed at ("" = never).</summary>
        public static string PushedIp { get { return _lastPushedIp; } }

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
                    if (note != _dnsNote) { _dnsNote = note; RaiseState(); }
                }
                finally { _dnsCheckedAt = DateTime.Now; _dnsChecking = false; }
            });
        }

        /// <summary>Opens https://Host:3443 exactly as a phone would, with Windows' normal certificate
        /// validation (nothing is bypassed), to prove the name resolves, the certificate is accepted and
        /// the service answers. Blocking - call off the UI thread.</summary>
        public static bool VerifyHttps(out string message)
        {
            message = "";
            if (!IsConfigured) { message = "Mobile Capture is not configured."; return false; }
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var req = (HttpWebRequest)WebRequest.Create("https://" + Host + ":" + HttpsPort + "/api/health");
                req.Timeout = 8000;
                using (req.GetResponse()) { }
                return true;
            }
            catch (WebException wex)
            {
                // Any HTTP status means TLS itself succeeded (the API may report its own database state).
                if (wex.Status == WebExceptionStatus.ProtocolError) return true;
                switch (wex.Status)
                {
                    case WebExceptionStatus.TrustFailure:
                    case WebExceptionStatus.SecureChannelFailure:
                        message = "Windows rejected the HTTPS certificate for " + Host + ": " +
                                  (wex.InnerException != null ? wex.InnerException.Message : wex.Message);
                        break;
                    case WebExceptionStatus.NameResolutionFailure:
                        message = "This PC cannot resolve " + Host + " yet. Wait a minute; if it stays this way the router is blocking " +
                                  "public names that resolve to private IPs (DNS rebind protection) - allow duckdns.org in the router.";
                        break;
                    case WebExceptionStatus.ConnectFailure:
                        message = "The Mobile Capture service is not accepting HTTPS connections on port " + HttpsPort +
                                  ". It may still be starting, or Windows Firewall may be blocking it.";
                        break;
                    case WebExceptionStatus.Timeout:
                        message = "The Mobile Capture service did not answer on port " + HttpsPort + " in time.";
                        break;
                    default:
                        message = "The HTTPS check failed (" + wex.Status + ").";
                        break;
                }
                return false;
            }
            catch (Exception ex) { message = "The HTTPS check failed: " + ex.Message; return false; }
        }

        /// <summary>One-line status for the Dashboard hint and the setup window.</summary>
        public static string StatusNote()
        {
            switch (Phase)
            {
                case MobilePhase.NotConfigured:
                    return MobileCaptureConfig.TokenUnreadable
                        ? "Mobile Capture setup was saved by a different Windows account - open Mobile Capture Setup and enter the token again."
                        : "Mobile Capture is not set up on this PC yet. It asks for a one-time setup the first time it is used.";
                case MobilePhase.NetworkUnavailable:
                    return "Mobile Capture network unavailable.";
                case MobilePhase.Working:
                    return "Getting the trusted certificate for " + Host + " (first run takes a few minutes, needs Internet)...";
                case MobilePhase.Failed:
                    return "Mobile Capture setup failed: " + _lastError;
                default:
                    if (_dnsNote.Length > 0) return _dnsNote;
                    return "If a phone shows \"CROMS server is not reachable from this Wi-Fi network\", the Wi-Fi may block device-to-device traffic (client/AP isolation) - use another Wi-Fi or this PC's hotspot.";
            }
        }
    }
}
