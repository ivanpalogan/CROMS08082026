using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Best-effort auto-start for the claimapp ID-upload web app (port 4300) and its
    /// Node save-API (port 3000), so running the kiosk alone — without the main CROMS.exe
    /// also running somewhere — still brings up the claim-QR path.
    /// <para/>
    /// On a real multi-PC deployment the kiosk runs on a CLIENT PC that has neither
    /// folder installed; that is fine — <see cref="Start"/> just finds nothing and skips
    /// silently, exactly as before this class existed, and the kiosk's own functions
    /// (ticket issuing, printing) are unaffected either way. It only matters on a
    /// single-PC setup (kiosk + server on the same machine) where CROMS.exe was never
    /// launched to start these itself.
    /// <para/>
    /// Never touches a server that is ALREADY running (checked by port, not by process) —
    /// so it can't double-start anything CROMS.exe already brought up, and it only kills
    /// what IT started, never a process it merely found already listening.
    /// </summary>
    internal static class LocalServices
    {
        private static Process _claimApp;
        private static Process _saveApi;
        private static bool _started;

        public static void Start()
        {
            if (_started) return;
            _started = true;

            try
            {
                string mobilePath = Cfg("IonicAppPath", @"C:\Users\ivan palogan\ORCMobile_Application");
                string claimPath = Cfg("ClaimAppPath", @"C:\Users\ivan palogan\claimapp");
                string apiDir = Path.Combine(mobilePath, "server");

                if (!PortOpen(3000) && Directory.Exists(apiDir) && File.Exists(Path.Combine(apiDir, "index.js")))
                    _saveApi = Spawn(apiDir, Cfg("MobileApiCommand", "node index.js"), "croms-kiosk-saveapi.log");

                if (!PortOpen(4300) && Directory.Exists(claimPath) &&
                    (File.Exists(Path.Combine(claimPath, "angular.json")) || File.Exists(Path.Combine(claimPath, "package.json"))))
                    _claimApp = Spawn(claimPath, Cfg("ClaimAppServeCommand",
                        "npx ng serve --host 0.0.0.0 --port 4300 --disable-host-check"), "croms-kiosk-claimapp.log");

                AppDomain.CurrentDomain.ProcessExit += (s, e) => Stop();
            }
            catch { /* best-effort — the kiosk still works without either server */ }
        }

        private static void Stop()
        {
            KillIfOurs(ref _claimApp);
            KillIfOurs(ref _saveApi);
        }

        private static void KillIfOurs(ref Process p)
        {
            if (p == null) return;
            try { if (!p.HasExited) KillTree(p.Id); } catch { }
            try { p.Dispose(); } catch { }
            p = null;
        }

        private static Process Spawn(string workDir, string command, string logName)
        {
            try
            {
                string log = Path.Combine(Path.GetTempPath(), logName);
                try { File.WriteAllText(log, "CROMS kiosk local-start — " + DateTime.Now + Environment.NewLine); } catch { }

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    WorkingDirectory = workDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                var proc = new Process { StartInfo = psi, EnableRaisingEvents = false };
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) TryAppend(log, e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) TryAppend(log, e.Data); };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                return proc;
            }
            catch { return null; }
        }

        private static void TryAppend(string file, string line)
        {
            try { File.AppendAllText(file, line + Environment.NewLine); } catch { }
        }

        private static bool PortOpen(int port)
        {
            try
            {
                using (var c = new TcpClient())
                {
                    var ar = c.BeginConnect("127.0.0.1", port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(300);
                    if (ok) { c.EndConnect(ar); return true; }
                    return false;
                }
            }
            catch { return false; }
        }

        private static string Cfg(string key, string def)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? def : v.Trim();
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
    }
}
