using System;

namespace CROMS.Data
{
    /// <summary>
    /// Builds the claimapp deep-link URL a phone's stock camera can open directly:
    /// <c>https://&lt;server-ip&gt;:4300/?claim=&lt;token&gt;</c>. claimapp reads the
    /// <c>?claim=</c> query and jumps straight to the ID-upload page — no in-app scan needed.
    ///
    /// Host/port/scheme come from the auto-started claimapp server
    /// (<see cref="IonicServerManager.ClaimApp"/>): its detected LAN IPv4 (the address a phone
    /// on the same Wi-Fi/hotspot can reach), port 4300, https (ng serve --ssl). Falls back to a
    /// fresh LAN detection if the server hasn't reported its IP yet.
    /// </summary>
    public static class ClaimLink
    {
        public static string Build(string token)
        {
            return BaseUrl() + "/?claim=" + Uri.EscapeDataString(token ?? "");
        }

        /// <summary>The base app URL (no token) — short enough for a client to type by hand.</summary>
        public static string BaseUrl()
        {
            var mgr = IonicServerManager.ClaimApp;
            string host = mgr.LanIp;
            if (string.IsNullOrWhiteSpace(host) || host == "127.0.0.1")
                host = IonicServerManager.DetectLanIp();
            int port = mgr.Port > 0 ? mgr.Port : 4300;
            string scheme = string.IsNullOrWhiteSpace(mgr.Scheme) ? "https" : mgr.Scheme;
            return scheme + "://" + host + ":" + port;
        }
    }
}
