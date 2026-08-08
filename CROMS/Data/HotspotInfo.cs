using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>
    /// Works out which network the phone should join to reach the laptop, and its
    /// name/password (so the Dashboard can show it). Preference order:
    ///   1. App.config HotspotSsid / HotspotPassword (manual override).
    ///   2. The Wi-Fi the laptop is CURRENTLY connected to (primary) — read via
    ///      netsh; the phone joins the same Wi-Fi.
    ///   3. The Windows Mobile Hotspot (fallback) — read via WinRT.
    /// All guarded; if nothing is readable the caller just omits the network hint.
    /// <c>source</c> is "config" / "wifi" / "hotspot".
    /// </summary>
    public static class HotspotInfo
    {
        public static bool TryGet(out string ssid, out string password, out string source)
        {
            ssid = null; password = null; source = null;

            // 1) manual override
            string cs = (ConfigurationManager.AppSettings["HotspotSsid"] ?? "").Trim();
            if (cs.Length > 0)
            {
                ssid = cs;
                password = (ConfigurationManager.AppSettings["HotspotPassword"] ?? "").Trim();
                source = "config";
                return true;
            }

            // 2) PRIMARY: the Wi-Fi the laptop is on right now
            if (CurrentWifi(out ssid, out password)) { source = "wifi"; return true; }

            // 3) FALLBACK: the Windows Mobile Hotspot
            if (HotspotAp(out ssid, out password)) { source = "hotspot"; return true; }

            return false;
        }

        // ---- current Wi-Fi (netsh) ------------------------------------------
        private static bool CurrentWifi(out string ssid, out string password)
        {
            ssid = null; password = null;
            try
            {
                string ifaces = RunNetsh("wlan show interfaces");
                // "State : connected" must be present, and grab the SSID (not BSSID).
                if (!Regex.IsMatch(ifaces, @"^\s*State\s*:\s*connected", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    return false;
                var m = Regex.Match(ifaces, @"^\s*SSID\s*:\s*(.+?)\s*$", RegexOptions.Multiline);
                if (!m.Success) return false;
                ssid = m.Groups[1].Value.Trim();
                if (ssid.Length == 0) return false;

                // password from the saved profile (needs no elevation for own profiles)
                try
                {
                    string prof = RunNetsh("wlan show profile name=\"" + ssid + "\" key=clear");
                    var pm = Regex.Match(prof, @"^\s*Key Content\s*:\s*(.+?)\s*$", RegexOptions.Multiline);
                    if (pm.Success) password = pm.Groups[1].Value.Trim();
                }
                catch { }
                return true;
            }
            catch { return false; }
        }

        private static string RunNetsh(string args)
        {
            var psi = new ProcessStartInfo("netsh", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var p = Process.Start(psi))
            {
                string outp = p.StandardOutput.ReadToEnd();
                p.WaitForExit(4000);
                return outp;
            }
        }

        // ---- Windows Mobile Hotspot (WinRT) --------------------------------
        private static bool HotspotAp(out string ssid, out string password)
        {
            ssid = null; password = null;
            try
            {
                var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                if (profile == null) return false;
                var mgr = Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager
                    .CreateFromConnectionProfile(profile);
                var cfg = mgr?.GetCurrentAccessPointConfiguration();
                if (cfg != null && !string.IsNullOrEmpty(cfg.Ssid))
                {
                    ssid = cfg.Ssid;
                    password = cfg.Passphrase;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Standard Wi-Fi QR text (unused by the single-QR UI, kept for reference).</summary>
        public static string WifiQrPayload(string ssid, string password)
        {
            string auth = string.IsNullOrEmpty(password) ? "nopass" : "WPA";
            return "WIFI:T:" + auth + ";S:" + Esc(ssid) + ";P:" + Esc(password ?? "") + ";;";
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '\\' || c == ';' || c == ',' || c == ':' || c == '"') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
