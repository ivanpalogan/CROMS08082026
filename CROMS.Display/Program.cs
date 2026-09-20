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

            // Find the machine that is actually SERVING the registry before opening the
            // board. Checking only "can a database be opened" is not enough: this PC
            // very likely has its own MySQL and its own croms database, and a board
            // showing an empty local copy looks exactly like a board that is working.
            // EnsureBestServer takes the server whose beacon is freshest, and sweeps the
            // LAN only when the one in effect is unreachable or unserved.
            try { ServerConfig.EnsureBestServer(); }
            catch { /* board will show "waiting for connection…" and keep polling */ }

            // Keep following the server while the board runs — its IP can change, and
            // the office can move the registry to the other laptop mid-day.
            ServerConfig.StartAutoReconnect();

            Application.Run(new DisplayForm());
        }
    }
}
