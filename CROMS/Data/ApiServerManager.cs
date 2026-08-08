using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Auto-starts the CROMS mobile save-API (the Node server in
    /// &lt;IonicAppPath&gt;\server) in the background when the desktop app launches,
    /// so pairing + the DB writes work with a single launch (no separate CMD).
    /// Headless (no window); the whole process tree is killed on exit (no orphan
    /// node/cmd). It has no status UI of its own — the Dashboard already shows
    /// "Save-API offline" if it isn't reachable.
    /// </summary>
    public sealed class ApiServerManager
    {
        public static readonly ApiServerManager Instance = new ApiServerManager();
        private ApiServerManager() { }

        private Process _proc;
        private string _logFile;

        /// <summary>Folder holding the Node save-API (index.js + package.json).</summary>
        public string ServerDir => Path.Combine(IonicServerManager.Instance.AppPath, "server");

        /// <summary>Command run after `cmd /c` (configurable). Default: node index.js.</summary>
        public string Command =>
            (ConfigurationManager.AppSettings["MobileApiCommand"] ?? "").Trim().Length > 0
                ? ConfigurationManager.AppSettings["MobileApiCommand"].Trim()
                : "node index.js";

        public void Start()
        {
            if (_proc != null && !SafeHasExited(_proc)) return;

            if (!Directory.Exists(ServerDir) || !File.Exists(Path.Combine(ServerDir, "index.js")))
                return; // no save-API here — nothing to start (Dashboard will show offline)

            try
            {
                _logFile = Path.Combine(Path.GetTempPath(), "croms-save-api.log");
                try { File.WriteAllText(_logFile, "CROMS save-API — " + DateTime.Now + Environment.NewLine); } catch { }

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + Command,
                    WorkingDirectory = ServerDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                _proc = new Process { StartInfo = psi };
                _proc.OutputDataReceived += OnLog;
                _proc.ErrorDataReceived += OnLog;
                _proc.Start();
                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();
            }
            catch { /* best-effort; Dashboard shows offline if it didn't come up */ }
        }

        public void Stop()
        {
            if (_proc == null) return;
            try { if (!SafeHasExited(_proc)) KillTree(_proc.Id); } catch { }
            try { _proc.Dispose(); } catch { }
            _proc = null;
        }

        private void OnLog(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            try { File.AppendAllText(_logFile, e.Data + Environment.NewLine); } catch { }
        }

        private static bool SafeHasExited(Process p)
        {
            try { return p.HasExited; } catch { return true; }
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
