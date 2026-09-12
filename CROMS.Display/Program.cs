using System;
using System.Windows.Forms;

namespace CROMS.Display
{
    /// <summary>
    /// Entry point for the public "Now Serving" display — a separate, view-only
    /// program that shares the CROMS database. Run this on the waiting-area PC/TV;
    /// staff drive the queue from the main CROMS app.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // If the saved/localhost database isn't reachable (e.g. the server's
            // Wi-Fi/hotspot IP changed), auto-scan the LAN to find it before opening
            // the board — same behaviour as the main app, no IP to type.
            try
            {
                if (!ServerConfig.IsReachable())
                {
                    string ip = ServerConfig.DiscoverServerAsync(3306).GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(ip)) ServerConfig.Save(ip, 3306);
                }
            }
            catch { /* board will show "waiting for connection…" and keep polling */ }

            // Keep reconnecting if the server's IP changes while the board is running.
            ServerConfig.StartAutoReconnect();

            Application.Run(new DisplayForm());
        }
    }
}
