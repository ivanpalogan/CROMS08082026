using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;

namespace CROMS
{
    static class Program
    {
        /// <summary>
        /// The main entry point. Auto-starts the Ionic dev server in the background
        /// (so staff never open a Command Prompt), shows the login screen first; on
        /// successful sign-in the operator picks a service window + the transactions
        /// they will handle (Window Assignment), then the main shell opens. The Ionic
        /// server is always killed when the app exits (no orphan node/cmd).
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Launcher: pick what opens on THIS computer. Display / Kiosk launch as their own
            // sibling .exe and this process exits; Admin (and "Run All Three") continue here.
            LauncherChoice choice = LauncherForm.Ask();
            switch (choice)
            {
                case LauncherChoice.Exit:
                    return;
                case LauncherChoice.Display:
                    LauncherForm.Start("CROMS.Display");
                    return;
                case LauncherChoice.Kiosk:
                    LauncherForm.Start("CROMS.Kiosk");
                    return;
                case LauncherChoice.All:
                    LauncherForm.Start("CROMS.Display");
                    LauncherForm.Start("CROMS.Kiosk");
                    break;   // then continue into the Admin app below
            }

            // Put a clickable CROMS icon on the desktop (first run only).
            DesktopShortcut.Ensure();

            // Make sure we know where the database server is AND can reach it.
            // First run: ask for the server IP. Later: if the server moved / is
            // unreachable, re-ask instead of failing with a cryptic error.
            // If THIS PC hosts the database, say so before going looking for a server:
            // the search then sees a live beacon and returns instantly instead of
            // sweeping the LAN on every launch of the server machine.
            ServerBeacon.BeatNow();

            if (!EnsureServerReachable()) return;   // user chose Exit

            // Keep reconnecting if the server's Wi-Fi/hotspot IP changes mid-session,
            // or if this PC drifts onto a database nobody is serving.
            ServerConfig.StartAutoReconnect();

            // Tell every kiosk / display / client on the LAN which machine is serving
            // the registry. Writes only while this PC is the host (see ServerBeacon).
            ServerBeacon.Start();

            // Auto-start the Ionic mobile dev server (background). The Login screen
            // shows its live status + mobile URL + QR. Guaranteed shutdown below.
            bool autoStart = (ConfigurationManager.AppSettings["IonicAutoStart"] ?? "true")
                .Trim().ToLowerInvariant() != "false";
            if (autoStart)
            {
                // Save-API first (port 3000 — DB writes + phone pairing), then BOTH
                // mobile dev servers: the certificate scanner (:4200) and the claimapp
                // ID-upload app (:4300). All proxy /api to the save-API.
                //
                // Both managers already catch their OWN startup exceptions internally
                // (Fail() -> Status=Error), so the try/catch here is a last-resort net,
                // not the real safety mechanism. What used to be missing is a durable
                // record of an Error status: the only place it showed was a passive
                // Dashboard label nobody was looking at, and a failure left no trace
                // once the app was closed. WatchMobileServer logs every status change
                // to a persistent file and retries once automatically, so a one-off
                // failure (e.g. a leftover process still releasing the port) self-heals
                // instead of silently leaving that app dead for the whole session.
                WatchMobileServer(IonicServerManager.Instance);
                WatchMobileServer(IonicServerManager.ClaimApp);
                try { ApiServerManager.Instance.Start(); } catch { }
                try { IonicServerManager.Instance.Start(); } catch { }
                try { IonicServerManager.ClaimApp.Start(); } catch { }
                Action stopAll = () =>
                {
                    IonicServerManager.ClaimApp.Stop();
                    IonicServerManager.Instance.Stop();
                    ApiServerManager.Instance.Stop();
                };
                AppDomain.CurrentDomain.ProcessExit += (s, e) => stopAll();
                Application.ApplicationExit += (s, e) => stopAll();
            }

            // If this launch is the restart from an in-app ⟳ Update that the operator
            // just authorized, resume their session (and window) so they aren't forced
            // to re-enter the password. One-time, short-lived, account-bound token;
            // false for every normal launch → login is required as usual.
            if (!SessionResume.TryConsume())
            {
                // Sign-in and window selection are one loop: the window screen's Log out button
                // comes straight back here rather than dropping the operator into the app.
                while (true)
                {
                    using (var login = new LoginForm())
                    {
                        if (login.ShowDialog() != DialogResult.OK)
                        {
                            IonicServerManager.ClaimApp.Stop();
                            IonicServerManager.Instance.Stop();
                            ApiServerManager.Instance.Stop();
                            return;   // not authenticated — exit without opening the app
                        }
                    }

                    // Window selection: claim a window (Online) + confirm what it handles.
                    // Skippable for staff not manning a window (e.g. admins).
                    using (var assign = new WindowAssignmentForm())
                    {
                        if (assign.ShowDialog() != DialogResult.Abort) break;   // proceed into the app
                    }

                    // Logged out from the window screen — drop the session and ask again.
                    Session.User = null;
                    Session.WindowId = 0;
                    Session.WindowName = null;
                }
            }

            Application.Run(new MainForm());
        }

        /// <summary>
        /// Guarantees CROMS can reach its database before the app opens. If the
        /// current setting already connects (e.g. the server PC on localhost, or a
        /// client whose saved server is up), returns immediately. Otherwise it
        /// shows the Connect-to-Server screen and loops until the operator saves a
        /// reachable address or chooses Exit. Returns false only on Exit.
        /// </summary>
        private static bool EnsureServerReachable()
        {
            // NOT just "does a connection open". Every office PC has MySQL and a croms
            // database installed, so opening one proves nothing about whether it is the
            // live registry — a client PC that answers on localhost would quietly run
            // against its own empty copy. EnsureBestServer adopts the machine whose
            // server_beacon is freshest, and only sweeps the LAN when the database in
            // effect is unreachable or unserved.
            if (ServerConfig.EnsureBestServer() && Db.IsConnected()) return true;

            string msg = ServerConfig.IsConfigured
                ? "Couldn't reach the CROMS database at " + ServerConfig.Host +
                  ". The server PC may be off, on a different Wi-Fi/hotspot, or its IP " +
                  "changed. Use \"Find Server Automatically\" or enter the current IP below."
                : "Welcome! Enter the IP address of the computer that has the CROMS " +
                  "database (the \"server\" PC) to get started.";

            while (true)
            {
                using (var setup = new ServerSetupForm(msg))
                {
                    if (setup.ShowDialog() != DialogResult.OK) return false;   // Exit
                }
                if (Db.IsConnected()) return true;
                msg = "Still can't reach the database. Check that the server PC is on and " +
                      "that this PC is on the same Wi-Fi/hotspot, then try again.";
            }
        }

        private static readonly object _mobileLogLock = new object();

        /// <summary>
        /// Subscribes to an IonicServerManager's status changes so a failure is (1) written
        /// to a durable log instead of vanishing with the process, and (2) retried once
        /// automatically. Call BEFORE Start() so the very first transition is captured too.
        /// </summary>
        private static void WatchMobileServer(IonicServerManager mgr)
        {
            bool retried = false;
            mgr.Changed += () =>
            {
                LogMobileServerEvent(mgr.Label + ": " + mgr.Status +
                    (mgr.Status == IonicStatus.Error ? " — " + mgr.LastError : ""));

                if (mgr.Status == IonicStatus.Error && !retried)
                {
                    retried = true;
                    LogMobileServerEvent(mgr.Label + ": retrying once after Error");
                    try { mgr.Retry(); } catch (Exception ex) { LogMobileServerEvent(mgr.Label + ": retry threw — " + ex.Message); }
                }
            };
        }

        /// <summary>
        /// Appends one timestamped line to %APPDATA%\CROMS\mobile-servers.log. Best-effort —
        /// a logging failure must never affect startup.
        /// </summary>
        private static void LogMobileServerEvent(string line)
        {
            try
            {
                lock (_mobileLogLock)
                {
                    string dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CROMS");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(dir, "mobile-servers.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine);
                }
            }
            catch { /* logging must never block startup */ }
        }
    }
}
