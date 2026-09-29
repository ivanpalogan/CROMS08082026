using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>
    /// Where the Mobile Capture settings (the stable DuckDNS hostname and its token) live on THIS
    /// office PC: %APPDATA%\CROMS\mobile.cfg, beside server.cfg. The operator enters them once in
    /// the Mobile Capture Setup window; nobody edits App.config or source.
    ///
    /// The token is a secret (it can re-point the name's DNS), so it is stored encrypted with
    /// Windows DPAPI, bound to the signed-in Windows account - the file is useless if copied to
    /// another PC or read by another account. It is never written to a log, a URL, a QR code or a
    /// message, and the UI never shows it back (only "saved").
    ///
    /// Legacy: the old MobileHostname / DuckDnsToken keys in App.config are still honoured when
    /// nothing has been stored, so an already-configured PC keeps working after this change.
    /// </summary>
    internal static class MobileCaptureConfig
    {
        private const string Suffix = ".duckdns.org";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CROMS.MobileCapture.v1");
        private static readonly object _gate = new object();
        private static bool _loaded;
        private static string _host = "";
        private static string _token = "";
        private static bool _tokenUnreadable;

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CROMS");
                return Path.Combine(dir, "mobile.cfg");
            }
        }

        /// <summary>Full DuckDNS name (name.duckdns.org), or "" when none is set.</summary>
        public static string Hostname { get { Load(); return _host; } }

        /// <summary>The token, for the two places that must send it to DuckDNS. Never log or display it.</summary>
        internal static string Token { get { Load(); return _token; } }

        public static bool HasToken { get { Load(); return _token.Length > 0; } }

        /// <summary>True when a token WAS stored but this Windows account cannot decrypt it.</summary>
        public static bool TokenUnreadable { get { Load(); return _tokenUnreadable; } }

        /// <summary>True when something was saved through the setup window (vs. only legacy App.config).</summary>
        public static bool IsStored { get { return File.Exists(FilePath); } }

        public static bool IsConfigured { get { Load(); return _host.Length > 0 && _token.Length > 0; } }

        /// <summary>Accepts "croms-lcro", "croms-lcro.duckdns.org" or a pasted https:// address.</summary>
        public static string NormalizeHost(string input, out string error)
        {
            error = "";
            string h = (input ?? "").Trim().ToLowerInvariant();
            h = Regex.Replace(h, @"^https?://", "");
            int cut = h.IndexOfAny(new[] { '/', ':', '?' });
            if (cut >= 0) h = h.Substring(0, cut);
            if (h.Length == 0) { error = "Enter your DuckDNS name (for example croms-lcro)."; return ""; }
            if (!h.Contains(".")) h += Suffix;
            if (!Regex.IsMatch(h, @"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.duckdns\.org$"))
            {
                error = "That is not a valid DuckDNS name. Use only letters, digits and hyphens, for example croms-lcro.";
                return "";
            }
            return h;
        }

        public static bool TokenLooksValid(string token, out string error)
        {
            error = "";
            string t = (token ?? "").Trim();
            if (t.Length == 0) { error = "Enter your DuckDNS token."; return false; }
            if (!Regex.IsMatch(t, @"^[A-Za-z0-9-]{8,64}$"))
            {
                error = "The token should be the long code from your DuckDNS page (letters, digits and hyphens, no spaces).";
                return false;
            }
            return true;
        }

        /// <summary>Stores the settings. A null/blank token keeps the one already saved.</summary>
        public static bool Save(string hostInput, string tokenOrNull, out string error)
        {
            string host = NormalizeHost(hostInput, out error);
            if (host.Length == 0) return false;

            string token = (tokenOrNull ?? "").Trim();
            if (token.Length == 0)
            {
                token = Token;   // keep what is already saved
                if (token.Length == 0) { error = "Enter your DuckDNS token."; return false; }
            }
            else if (!TokenLooksValid(token, out error)) return false;

            try
            {
                byte[] blob = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
                string dir = Path.GetDirectoryName(FilePath);
                Directory.CreateDirectory(dir);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, "hostname=" + host + "\r\ntoken=" + Convert.ToBase64String(blob) + "\r\n", Encoding.ASCII);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception ex)
            {
                error = "Could not save the Mobile Capture settings on this PC: " + ex.Message;
                return false;
            }
            lock (_gate) { _loaded = false; }
            return true;
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
            lock (_gate) { _loaded = false; }
        }

        private static void Load()
        {
            lock (_gate)
            {
                if (_loaded) return;
                _host = ""; _token = ""; _tokenUnreadable = false;

                try
                {
                    if (File.Exists(FilePath))
                    {
                        foreach (string line in File.ReadAllLines(FilePath))
                        {
                            int eq = line.IndexOf('=');
                            if (eq <= 0) continue;
                            string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                            string val = line.Substring(eq + 1).Trim();
                            if (key == "hostname")
                            {
                                string err; _host = NormalizeHost(val, out err);
                            }
                            else if (key == "token" && val.Length > 0)
                            {
                                try
                                {
                                    byte[] plain = ProtectedData.Unprotect(Convert.FromBase64String(val), Entropy, DataProtectionScope.CurrentUser);
                                    _token = Encoding.UTF8.GetString(plain);
                                }
                                catch { _tokenUnreadable = true; }
                            }
                        }
                    }
                }
                catch { /* unreadable file = not configured */ }

                // Legacy App.config values, only where nothing was stored.
                if (_host.Length == 0)
                {
                    string err; _host = NormalizeHost(ConfigurationManager.AppSettings["MobileHostname"], out err);
                }
                if (_token.Length == 0 && !_tokenUnreadable)
                    _token = (ConfigurationManager.AppSettings["DuckDnsToken"] ?? "").Trim();

                _loaded = true;
            }
        }
    }
}
