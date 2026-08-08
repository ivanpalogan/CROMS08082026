using System;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Builds the claimapp deep-link URL a phone's stock camera can open directly:
    /// <c>https://&lt;server-ip&gt;:4300/?claim=&lt;token&gt;</c>. claimapp reads the
    /// <c>?claim=</c> query and jumps straight to the ID-upload page — no in-app scan needed.
    ///
    /// Host = the DB server the kiosk points at (claimapp runs on that same PC). When the
    /// kiosk runs ON the server (local DB, no saved host) the kiosk's own LAN IPv4 is used,
    /// which is the address a phone on the same Wi-Fi/hotspot can reach. Scheme/port are
    /// App.config-overridable (ClaimAppScheme / ClaimAppPort), defaulting to https:4300 to
    /// match the ng serve --ssl --port 4300 that CROMS auto-starts.
    /// </summary>
    internal static class ClaimLink
    {
        public static string Build(string token)
        {
            return BaseUrl() + "/?claim=" + Uri.EscapeDataString(token ?? "");
        }

        /// <summary>The base app URL (no token) — short enough for a client to type by hand.</summary>
        public static string BaseUrl()
        {
            string host = ServerConfig.Host;
            if (string.IsNullOrWhiteSpace(host) || IsLoopback(host)) host = LanIp();
            string scheme = Cfg("ClaimAppScheme", "https");
            string port = Cfg("ClaimAppPort", "4300");
            return scheme + "://" + host + ":" + port;
        }

        private static string Cfg(string key, string def)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? def : v.Trim();
        }

        private static bool IsLoopback(string h)
        {
            h = h.Trim().ToLowerInvariant();
            return h == "localhost" || h == "127.0.0.1" || h == "::1";
        }

        /// <summary>
        /// Best private-range IPv4 a phone on the same Wi-Fi can reach. Scores each adapter so
        /// the active Wi-Fi/Ethernet (has a default gateway) wins over an idle hotspot or a
        /// secondary NIC — important on a PC with several IPs. Mirrors the main app's DetectLan.
        /// </summary>
        private static string LanIp()
        {
            string best = "localhost"; int bestScore = int.MinValue;
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    var props = ni.GetIPProperties();
                    bool hasGateway = false;
                    foreach (var g in props.GatewayAddresses)
                        if (g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !g.Address.Equals(IPAddress.Any)) { hasGateway = true; break; }
                    string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                    bool virtualAdapter = desc.Contains("virtual") || desc.Contains("vmware") ||
                                          desc.Contains("virtualbox") || desc.Contains("hyper-v") ||
                                          desc.Contains("vethernet") || desc.Contains("loopback");

                    foreach (var ua in props.UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip)) continue;
                        string s = ip.ToString();
                        if (s.StartsWith("169.254.")) continue;                 // APIPA (no DHCP)
                        bool isPrivate = s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("172.");
                        if (!isPrivate) continue;
                        bool isHotspot = s.StartsWith("192.168.137.");

                        int score = 0;
                        if (hasGateway) score += 8;   // the adapter actually routing traffic (phone's Wi-Fi)
                        if (isHotspot) score += 7;     // a live PC hotspot is a valid phone target
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) score += 4;
                        else if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) score += 3;
                        if (virtualAdapter && !isHotspot) score -= 6;

                        if (score > bestScore) { bestScore = score; best = s; }
                    }
                }
            }
            catch { }
            return best;
        }
    }
}
